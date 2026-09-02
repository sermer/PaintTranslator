using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The state MainForm kept in fields, with the WinForms re-entrancy guards gone.
/// Components read properties and call methods; they never hold pipeline state.
/// Every mutation ends by raising Changed, and every new frame by raising
/// FrameReady, so the canvas and the sidebar cannot drift apart.
/// </summary>
public sealed class ConversionSession
{
    public const int MarkMinimum = 1, MarkMaximum = 128, BlurMinimum = 0, BlurMaximum = 20;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(125);

    private readonly PaletteStore palette;
    private readonly RenderScheduler scheduler;
    private readonly Dictionary<string, Dictionary<IPipelineStage, ParameterValues>> styleValues = new();
    private List<PigmentCoefficients> available = new();
    private List<PigmentCoefficients> selected = new();
    private PaintBlendMatcher? matcher;

    /// <summary>Set by LoadPhoto when it runs inside a Begin/End window, since its own
    /// Schedule() call is gated then; tells EndImageOperation a render is actually
    /// owed, as opposed to a load attempt that failed before LoadPhoto ever ran.</summary>
    private bool renderPendingAfterOperation;

    public ConversionSession(IFrameRenderer renderer, PaletteStore palette,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.palette = palette;
        scheduler = new RenderScheduler(Capture, renderer, CanRender, Publish, Debounce, delay);
        scheduler.StateChanged += () => Changed?.Invoke();
        Style = StyleRegistry.Default;
        Populate(palette.Load());
    }

    public PixelImage? SourcePhoto { get; private set; }
    public PixelImage? PreviewSource { get; private set; }
    public string? PhotoName { get; private set; }
    public PixelImage? Displayed { get; private set; }
    public string Title { get; private set; } = "Paint Translator";
    public WheelDisplay Wheel { get; private set; }
    public IReadOnlyList<PigmentCoefficients> AvailablePaints => available;
    public IReadOnlyList<PigmentCoefficients> SelectedPaints => selected;
    public StyleDefinition Style { get; private set; }
    public int MarkPixels { get; private set; } = 3;
    public int BlurRadius { get; private set; } = 2;
    public int GridColumns { get; private set; } = 2;
    public int GridRows { get; private set; } = 2;
    public bool ShowGrid { get; private set; }
    public bool MagnifierActive { get; private set; }
    public bool ImageOperationInProgress { get; private set; }
    public bool FullRenderInProgress => scheduler.FullRenderInProgress;

    /// <summary>True when the last ApplyPalette's save to localStorage failed (a full or
    /// disabled store): the palette still applies for this session, but Sidebar needs to
    /// tell the user it won't survive a reload, same as WinForms' MessageBox for the
    /// equivalent failure.</summary>
    public bool PaletteSaveFailed { get; private set; }

    public event Action? Changed;
    public event Action<PixelImage>? FrameReady;

    /// <summary>Lazily built: the matcher is costly and only hovering needs it.</summary>
    public PaintBlendMatcher Matcher => matcher ??= new PaintBlendMatcher(selected);

    public void BeginImageOperation() { ImageOperationInProgress = true; scheduler.Cancel(); Changed?.Invoke(); }

    // LoadPhoto's own Schedule() call runs while the caller is still inside the
    // Begin/End window that wraps an async decode (Index.razor.cs's LoadBytes), so
    // CanRender's !ImageOperationInProgress check makes that call a no-op every time;
    // LoadPhoto records the debt in renderPendingAfterOperation instead. Rescheduling
    // here must stay conditional on that flag rather than running unconditionally: a
    // failed second load (ImageLoadException, LoadPhoto never called) still reaches
    // this method through the caller's finally block, and with a photo already
    // displayed CanRender would happily be true — an unconditional Schedule() would
    // needlessly re-run the existing photo through another preview/full cycle right
    // next to the error toast.
    public void EndImageOperation()
    {
        ImageOperationInProgress = false;
        Changed?.Invoke();
        if (renderPendingAfterOperation)
        {
            renderPendingAfterOperation = false;
            scheduler.Schedule();
        }
    }

    public void LoadPhoto(PixelImage photo, string name)
    {
        scheduler.Cancel();
        SourcePhoto = photo;
        PreviewSource = ConversionPreview.CreateSource(photo);
        PhotoName = name;
        // A brush covers a roughly constant fraction of a canvas whatever the file's
        // resolution, so the default follows the image rather than the last one.
        MarkPixels = Math.Clamp(RenderContext.DefaultMarkPixels(photo.Width, photo.Height), MarkMinimum, MarkMaximum);
        Wheel = WheelDisplay.None;
        if (ImageOperationInProgress)
        {
            renderPendingAfterOperation = true;
        }
        Display(photo, $"Paint Translator - {name}");
        scheduler.Schedule();
    }

    public IReadOnlyDictionary<IPipelineStage, ParameterValues> ValuesFor(StyleDefinition style) => Values(style);

    public void SetStyle(string name)
    {
        Style = StyleRegistry.ByName(name);
        Changed?.Invoke();
        scheduler.Schedule();
    }

    public void SetParameter(IPipelineStage stage, string id, double value)
    {
        Values(Style)[stage].Set(id, value);
        Changed?.Invoke();
        scheduler.Schedule();
    }

    public void ResetActiveStyle()
    {
        styleValues[Style.Name] = new Dictionary<IPipelineStage, ParameterValues>(StylePipeline.DefaultValues(Style));
        Changed?.Invoke();
        scheduler.Schedule();
    }

    public void SetMark(int pixels) { MarkPixels = Math.Clamp(pixels, MarkMinimum, MarkMaximum); Changed?.Invoke(); scheduler.Schedule(); }
    public void SetBlur(int radius) { BlurRadius = Math.Clamp(radius, BlurMinimum, BlurMaximum); Changed?.Invoke(); scheduler.Schedule(); }
    public void SetGrid(int columns, int rows, bool show) { GridColumns = columns; GridRows = rows; ShowGrid = show; Changed?.Invoke(); }
    public void SetMagnifier(bool active) { MagnifierActive = active; Changed?.Invoke(); }

    public void SetSelectedPaints(IEnumerable<PigmentCoefficients> paints)
    {
        selected = paints.ToList();
        matcher = null;
        if (Wheel == WheelDisplay.SelectedPaints)
        {
            Display(ColorWheelGenerator.Create(512, selected), "Paint Translator - Selected Golden Paint Wheel");
        }
        else
        {
            Changed?.Invoke();
            scheduler.Schedule();
        }
    }

    /// <summary>The palette editor's OK: persist, then rebuild the list with everything checked.</summary>
    public void ApplyPalette(IEnumerable<string> names)
    {
        var set = new HashSet<string>(names, StringComparer.Ordinal);
        // Populate/SetSelectedPaints below both raise Changed, which is what carries this
        // flag's new value to Sidebar; the palette is applied in-session regardless of
        // whether the save itself succeeded.
        PaletteSaveFailed = !palette.Save(set);
        Populate(set);
        SetSelectedPaints(available);
    }

    /// <summary>The banner's close button. Without this the warning sat in the sidebar until
    /// the next successful save, which for a user who never reopens the palette editor is
    /// forever; WinForms' MessageBox for the same failure is dismissed by definition.</summary>
    public void DismissPaletteSaveWarning()
    {
        if (!PaletteSaveFailed) return;
        PaletteSaveFailed = false;
        Changed?.Invoke();
    }

    public void ShowWheel(WheelDisplay kind)
    {
        scheduler.Cancel();
        Wheel = kind;
        if (kind == WheelDisplay.Traditional)
        {
            Display(ColorWheelGenerator.CreateTraditional(512), "Paint Translator - Traditional Color Wheel");
        }
        else
        {
            Display(ColorWheelGenerator.Create(512, selected), "Paint Translator - Selected Golden Paint Wheel");
        }
    }

    /// <summary>The one addition to WinForms, which could only leave a wheel by loading a photo.</summary>
    public void ShowPhoto()
    {
        Wheel = WheelDisplay.None;
        if (SourcePhoto != null)
        {
            Display(SourcePhoto, $"Paint Translator - {PhotoName}");
            scheduler.Schedule();
        }
        else
        {
            Displayed = null;
            Title = "Paint Translator";
            Changed?.Invoke();
        }
    }

    /// <summary>Tooltip text for a displayed-image pixel, or null where there is nothing to say.</summary>
    public string[]? RecipeAt(int x, int y)
    {
        PixelImage? image = Displayed;
        if (image == null || x < 0 || y < 0 || x >= image.Width || y >= image.Height)
        {
            return null;
        }
        int argb = image[x, y];
        // Fully transparent pixels are the empty surround of a colour wheel.
        if ((argb >>> 24) == 0)
        {
            return null;
        }
        Color pixel = Color.FromArgb(argb);
        switch (Wheel)
        {
            case WheelDisplay.SelectedPaints:
                double[]? weights = ColorWheelGenerator.GetBlendWeights(image.Width, selected.Count, x, y);
                return weights == null ? null : RecipeFormatter.WheelBlend(pixel, selected, weights);
            case WheelDisplay.Traditional:
                return new[] { RecipeFormatter.RgbLine(pixel) };
            default:
                if (selected.Count == 0)
                {
                    return new[] { RecipeFormatter.RgbLine(pixel) };
                }
                return RecipeFormatter.ClosestMix(pixel, selected, Matcher.FindClosestBlend(pixel));
        }
    }

    private Dictionary<IPipelineStage, ParameterValues> Values(StyleDefinition style)
    {
        if (!styleValues.TryGetValue(style.Name, out Dictionary<IPipelineStage, ParameterValues>? values))
        {
            values = new Dictionary<IPipelineStage, ParameterValues>(StylePipeline.DefaultValues(style));
            styleValues[style.Name] = values;
        }
        return values;
    }

    private void Populate(ISet<string>? names)
    {
        matcher = null;
        available = PigmentLibrary.Selectable.Where(p => names == null || names.Contains(p.Name)).ToList();
        // A saved palette whose names no longer match any catalogue paint would leave
        // the app with no paints; fall back to the catalogue.
        if (available.Count == 0)
        {
            available = PigmentLibrary.Selectable.ToList();
        }
        selected = available.ToList();
        Changed?.Invoke();
    }

    private bool CanRender() =>
        SourcePhoto != null && PreviewSource != null && Wheel == WheelDisplay.None && !ImageOperationInProgress;

    private RenderRequest? Capture(bool preview, long generation)
    {
        PixelImage? source = preview ? PreviewSource : SourcePhoto;
        if (source == null || SourcePhoto == null || selected.Count == 0)
        {
            return null;
        }
        IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.SnapshotValues(Style, Values(Style));
        int blur = BlurRadius, mark = MarkPixels;
        if (preview)
        {
            blur = ConversionPreview.ScaleRadius(blur, SourcePhoto.Size, source.Size);
            mark = ConversionPreview.ScaleRadius(mark, SourcePhoto.Size, source.Size);
        }
        (StyleDefinition style, IReadOnlyDictionary<IPipelineStage, ParameterValues> renderValues) =
            PalettePhotoConverter.ComposeWithBlur(Style, values, blur);
        return new RenderRequest(source, selected.ToList(), style, mark, renderValues, generation, preview);
    }

    private void Publish(RenderRequest request, PixelImage frame)
    {
        // No Wheel = WheelDisplay.None here: Publish only ever runs on a request the
        // scheduler already re-checked with CanDisplay, which requires CanRender, which
        // requires Wheel == WheelDisplay.None to be true already — reassigning it here
        // was dead.
        Display(frame, request.IsPreview
            ? $"Paint Translator - {PhotoName} (live preview)"
            : $"Paint Translator - {PhotoName} (converted to paints)");
    }

    private void Display(PixelImage frame, string title)
    {
        Displayed = frame;
        Title = title;
        Changed?.Invoke();
        FrameReady?.Invoke(frame);
    }
}
