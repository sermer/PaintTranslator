using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;
using static PaintTranslator.Web.Tests.SessionDoubles;

namespace PaintTranslator.Web.Tests;

public class ConversionSessionTests
{
    private static IPipelineStage FirstParameterisedStage(StyleDefinition style) =>
        style.Stages.First(s => s.Parameters.Count > 0);

    [Fact]
    public void StartsWithFullCatalogueDefaultStyleAndDefaults()
    {
        var session = NewSession();
        Assert.Equal(PigmentLibrary.Selectable.Count, session.SelectedPaints.Count);
        Assert.Equal(StyleRegistry.Default.Name, session.Style.Name);
        Assert.Equal(3, session.MarkPixels);
        Assert.Equal(2, session.BlurRadius);
        Assert.Equal(WheelDisplay.None, session.Wheel);
    }

    [Fact]
    public void ParameterValuesSurviveStyleSwitchAndImageLoad()
    {
        var session = NewSession();
        StyleDefinition first = session.Style;
        IPipelineStage stage = FirstParameterisedStage(first);
        StyleParameter parameter = stage.Parameters[0];
        double changed = parameter.Minimum + 0.5 * (parameter.Maximum - parameter.Minimum);

        session.SetParameter(stage, parameter.Id, changed);
        session.SetStyle(StyleRegistry.All[1].Name);
        session.LoadPhoto(PixelImage.Filled(8, 8, unchecked((int)0xFF404040)), "x.png");
        session.SetStyle(first.Name);

        Assert.Equal(changed, session.ValuesFor(first)[stage][parameter.Id], 6);
    }

    [Fact]
    public void ResetTouchesOnlyTheActiveStyle()
    {
        var session = NewSession();
        StyleDefinition a = StyleRegistry.All[0];
        StyleDefinition b = StyleRegistry.All[1];
        IPipelineStage stageA = FirstParameterisedStage(a);
        IPipelineStage stageB = FirstParameterisedStage(b);
        StyleParameter pa = stageA.Parameters[0];
        StyleParameter pb = stageB.Parameters[0];

        session.SetStyle(a.Name);
        session.SetParameter(stageA, pa.Id, pa.Maximum);
        session.SetStyle(b.Name);
        session.SetParameter(stageB, pb.Id, pb.Maximum);
        session.ResetActiveStyle();

        Assert.Equal(StylePipeline.DefaultValues(b)[stageB][pb.Id], session.ValuesFor(b)[stageB][pb.Id], 6);
        Assert.Equal(pa.Maximum, session.ValuesFor(a)[stageA][pa.Id], 6);
    }

    [Fact]
    public void LoadingAPhotoResetsMarkToTheImageDefaultAndClearsAnyWheel()
    {
        var session = NewSession();
        session.SetMark(77);
        session.ShowWheel(WheelDisplay.Traditional);
        var photo = PixelImage.Filled(1200, 800, unchecked((int)0xFF404040));

        session.LoadPhoto(photo, "photo.jpg");

        Assert.Equal(Math.Clamp(RenderContext.DefaultMarkPixels(1200, 800), 1, 128), session.MarkPixels);
        Assert.Equal(WheelDisplay.None, session.Wheel);
        Assert.Same(photo, session.Displayed);
        Assert.Equal("Paint Translator - photo.jpg", session.Title);
        Assert.NotNull(session.PreviewSource);
        Assert.True(session.PreviewSource!.Width <= ConversionPreview.MaximumDimension);
    }

    [Fact]
    public void ChangingPaintsWhileTheSelectedWheelShowsRegeneratesTheWheel()
    {
        var session = NewSession();
        session.ShowWheel(WheelDisplay.SelectedPaints);
        PixelImage before = session.Displayed!;
        session.SetSelectedPaints(PigmentLibrary.Selectable.Take(3));
        Assert.NotSame(before, session.Displayed);
        Assert.Equal(512, session.Displayed!.Width);
        Assert.Equal(WheelDisplay.SelectedPaints, session.Wheel);
    }

    [Fact]
    public void ApplyPaletteSavesAndRepopulatesAndEmptyFallsBackToCatalogue()
    {
        var store = new MemoryStore();
        var session = NewSession(store);
        string[] two = PigmentLibrary.Selectable.Take(2).Select(p => p.Name).ToArray();

        session.ApplyPalette(two);
        Assert.Equal(two, session.AvailablePaints.Select(p => p.Name));
        Assert.Contains(two[0], store.Get(PaletteStore.Key)!);

        session.ApplyPalette(new[] { "No Such Paint" });
        Assert.Equal(PigmentLibrary.Selectable.Count, session.AvailablePaints.Count);
    }

    [Fact]
    public void AFailedSaveSetsPaletteSaveFailedButStillAppliesThenClearsOnTheNextSuccess()
    {
        var store = new FlakyStore();
        var session = NewSession(store);
        string[] two = PigmentLibrary.Selectable.Take(2).Select(p => p.Name).ToArray();

        store.FailNextSet = true;
        session.ApplyPalette(two);
        Assert.True(session.PaletteSaveFailed);
        Assert.Equal(two, session.AvailablePaints.Select(p => p.Name)); // still applies in-session

        store.FailNextSet = false;
        session.ApplyPalette(two);
        Assert.False(session.PaletteSaveFailed);
    }

    [Fact]
    public void RecipeAtReturnsNullOutsideOrOnTransparentPixels()
    {
        var session = NewSession();
        session.ShowWheel(WheelDisplay.Traditional);
        Assert.Null(session.RecipeAt(0, 0));                 // wheel corner is transparent
        Assert.NotNull(session.RecipeAt(256, 256));          // centre has colour
        Assert.Null(session.RecipeAt(-1, 0));
    }

    // Rewritten per RenderSchedulerTests' own ManualDelay: each Wait() call gets its
    // own TaskCompletionSource, so a later Schedule() canceling an earlier debounce's
    // token cannot poison the source Fire() is about to complete. Not shared with
    // RenderSchedulerTests' copy — this class keeps its own per the controller's
    // ruling against sharing test code across test classes.
    private sealed class ManualDelay
    {
        private TaskCompletionSource? current;

        /// <summary>How many debounce windows actually started, i.e. how many times
        /// RenderScheduler.Schedule got past its synchronous CanRun gate rather than
        /// returning immediately — the signal LoadingDuringAnImageOperationRendersOnceTheOperationEnds
        /// needs, since a gated Schedule call never reaches this method at all.</summary>
        public int Started { get; private set; }

        public Task Wait(TimeSpan _, CancellationToken token)
        {
            Started++;
            var tcs = new TaskCompletionSource();
            current = tcs;
            token.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }
        public void Fire() => current?.TrySetResult();
    }

    /// <summary>Captures every request it is asked to render, and returns a frame whose
    /// fill colour differs between a preview and a full request so a test can tell
    /// which one produced <see cref="ConversionSession.Displayed"/>.</summary>
    private sealed class RecordingRenderer : IFrameRenderer
    {
        public List<RenderRequest> Seen { get; } = new();
        public PixelImage? FullFrame { get; private set; }

        public Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token)
        {
            Seen.Add(request);
            PixelImage frame = PixelImage.Filled(
                request.Source.Width, request.Source.Height,
                request.IsPreview ? unchecked((int)0xFF112233) : unchecked((int)0xFF445566));
            if (!request.IsPreview)
            {
                FullFrame = frame;
            }
            return Task.FromResult<PixelImage?>(frame);
        }
    }

    [Fact]
    public async Task ADebouncedCycleRendersPreviewFromThePreviewSourceThenFullFromThePhoto()
    {
        var delay = new ManualDelay();
        var renderer = new RecordingRenderer();
        var session = new ConversionSession(renderer, new PaletteStore(new MemoryStore()), delay.Wait);

        // Race-free by construction: rather than guessing how many continuations an
        // already-completed render task needs before Publish has run, wait on the one
        // state Publish sets synchronously — the title reaching its final form.
        var converted = new TaskCompletionSource();
        var titlesSeen = new List<string>();
        session.Changed += () =>
        {
            titlesSeen.Add(session.Title);
            if (session.Title == "Paint Translator - photo.jpg (converted to paints)")
            {
                converted.TrySetResult();
            }
        };

        var photo = PixelImage.Filled(1200, 800, unchecked((int)0xFF404040));
        session.LoadPhoto(photo, "photo.jpg");
        session.SetBlur(4);
        session.SetMark(20);   // each restart of the debounce is fine; only the last one fires
        delay.Fire();
        await converted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(2, renderer.Seen.Count);
        RenderRequest preview = renderer.Seen[0];
        RenderRequest full = renderer.Seen[1];

        Assert.True(preview.IsPreview);
        Assert.Same(session.PreviewSource, preview.Source);
        Assert.Equal(
            ConversionPreview.ScaleRadius(20, new Size(1200, 800), session.PreviewSource!.Size),
            preview.MarkPixels);

        Assert.False(full.IsPreview);
        Assert.Same(session.SourcePhoto, full.Source);
        Assert.Equal(20, full.MarkPixels);

        // ComposeWithBlur(..., 4) appends an OptionalBlur pre-map stage, so the composed
        // style both requests render with has one more stage than the plain active style.
        Assert.True(preview.Style.Stages.Count() > session.Style.Stages.Count());
        Assert.True(full.Style.Stages.Count() > session.Style.Stages.Count());

        Assert.Contains("Paint Translator - photo.jpg (live preview)", titlesSeen);
        Assert.Equal("Paint Translator - photo.jpg (converted to paints)", session.Title);
        Assert.Same(renderer.FullFrame, session.Displayed);
    }

    // Pins the EndImageOperation fix: LoadPhoto calls Schedule while the caller (e.g.
    // Index.razor.cs's LoadBytes, which wraps the whole async decode in Begin/End) is
    // still inside the gate, so that call must be a no-op — and EndImageOperation must
    // schedule again itself once the gate opens, or the render loop never starts.
    [Fact]
    public async Task LoadingDuringAnImageOperationRendersOnceTheOperationEnds()
    {
        var delay = new ManualDelay();
        var renderer = new RecordingRenderer();
        var session = new ConversionSession(renderer, new PaletteStore(new MemoryStore()), delay.Wait);

        var converted = new TaskCompletionSource();
        session.Changed += () =>
        {
            if (session.Title == "Paint Translator - photo.jpg (converted to paints)")
            {
                converted.TrySetResult();
            }
        };

        var photo = PixelImage.Filled(1200, 800, unchecked((int)0xFF404040));
        session.BeginImageOperation();
        session.LoadPhoto(photo, "photo.jpg");
        Assert.Equal(0, delay.Started); // gated: CanRender() saw ImageOperationInProgress still true

        session.EndImageOperation();
        Assert.Equal(1, delay.Started); // the fix: End reschedules once the gate opens

        delay.Fire();
        await converted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(2, renderer.Seen.Count);
    }

    // Pins the conditional-reschedule fix: EndImageOperation must not restart the
    // render loop when the operation it closes never actually called LoadPhoto (a
    // failed second load still reaches EndImageOperation through the caller's
    // finally block) — otherwise the photo already on screen would needlessly be
    // re-run through another preview/full cycle next to the error toast.
    [Fact]
    public async Task AnOperationThatLoadsNothingDoesNotReRenderTheExistingPhoto()
    {
        var delay = new ManualDelay();
        var renderer = new RecordingRenderer();
        var session = new ConversionSession(renderer, new PaletteStore(new MemoryStore()), delay.Wait);

        var converted = new TaskCompletionSource();
        session.Changed += () =>
        {
            if (session.Title == "Paint Translator - photo.jpg (converted to paints)")
            {
                converted.TrySetResult();
            }
        };

        var photo = PixelImage.Filled(1200, 800, unchecked((int)0xFF404040));
        session.LoadPhoto(photo, "photo.jpg");
        delay.Fire();
        await converted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        int startedAfterFirstCycle = delay.Started;

        // No LoadPhoto call between Begin and End: as if the decode had thrown.
        session.BeginImageOperation();
        session.EndImageOperation();

        Assert.Equal(startedAfterFirstCycle, delay.Started);
        Assert.EndsWith("(converted to paints)", session.Title);
    }

    [Fact]
    public void DismissPaletteSaveWarningClearsTheFlagAndRaisesChangedOnce()
    {
        var store = new FlakyStore { FailNextSet = true };
        var session = NewSession(store);
        session.ApplyPalette(PigmentLibrary.Selectable.Take(2).Select(p => p.Name));
        Assert.True(session.PaletteSaveFailed);

        int changed = 0;
        session.Changed += () => changed++;
        session.DismissPaletteSaveWarning();
        Assert.False(session.PaletteSaveFailed);
        Assert.Equal(1, changed);

        session.DismissPaletteSaveWarning(); // already clear: no second Changed
        Assert.Equal(1, changed);
    }
}
