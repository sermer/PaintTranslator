using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class RenderSchedulerTests
{
    // Rewritten per controller ruling: the brief's version handed every Wait() call the
    // same TaskCompletionSource, so a second Schedule() inside one debounce canceled the
    // token that the first Wait() was already keyed to, which canceled the shared source
    // and made the second Wait() return an already-canceled task — the loop never ran.
    // Each Wait() now gets its own source; Fire() completes only the most recent one.
    private sealed class ManualDelay
    {
        private TaskCompletionSource? current;
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

    private sealed class FakeRenderer : IFrameRenderer
    {
        public List<RenderRequest> Seen { get; } = new();
        public Func<RenderRequest, Task>? Gate { get; set; }
        public async Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token)
        {
            Seen.Add(request);
            if (Gate != null) await Gate(request);
            token.ThrowIfCancellationRequested();
            return PixelImage.Filled(1, 1, unchecked((int)0xFF000000));
        }
    }

    private static readonly IReadOnlyList<PigmentCoefficients> Paints = PigmentLibrary.Selectable.Take(2).ToList();
    private static readonly PixelImage Source = PixelImage.Filled(4, 4, unchecked((int)0xFF808080));

    private static RenderRequest Capture(bool preview, long generation) => new(
        Source, Paints, StyleRegistry.Default, 3, StylePipeline.DefaultValues(StyleRegistry.Default), generation, preview);

    [Fact]
    public async Task TwoSchedulesInsideTheDebounceRenderOnce()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        var scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.FromMilliseconds(125), delay.Wait);

        scheduler.Schedule();
        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;

        Assert.Equal(2, renderer.Seen.Count);           // one preview + one full
        Assert.True(renderer.Seen[0].IsPreview);
        Assert.False(renderer.Seen[1].IsPreview);
        Assert.Equal(2, published.Count);
        Assert.Equal(2, delay.Started);                 // second Schedule restarted the debounce
    }

    [Fact]
    public async Task StaleGenerationIsNeverPublished()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        var gate = new TaskCompletionSource();
        RenderScheduler scheduler = null!;
        renderer.Gate = r =>
        {
            if (r.IsPreview && r.Generation == 1) { scheduler.Schedule(); }  // superseded mid-render
            return Task.CompletedTask;
        };
        scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        delay.Fire();
        await Task.Yield();
        delay.Fire();                                    // the re-schedule's debounce
        await scheduler.Idle;

        Assert.DoesNotContain(published, r => r.Generation == 1);
        Assert.Contains(published, r => r.Generation == 2 && !r.IsPreview);
    }

    [Fact]
    public async Task ScheduleDuringARunningRenderProducesExactlyOneFollowUp()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        RenderScheduler scheduler = null!;
        int extra = 0;
        renderer.Gate = r =>
        {
            if (!r.IsPreview && extra++ == 0) { scheduler.Schedule(); scheduler.Schedule(); }
            return Task.CompletedTask;
        };
        scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        delay.Fire();
        await Task.Yield();
        delay.Fire();
        await scheduler.Idle;

        // Generation 1 (first loop) and generation 3 (one follow-up for the two schedules).
        Assert.Equal(2, published.Count(r => r.IsPreview));
        Assert.Equal(1, published.Count(r => r.Generation == 3 && r.IsPreview));
    }

    [Fact]
    public async Task ScheduleIsIgnoredWhileTheGateIsClosed()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        bool open = false;
        var scheduler = new RenderScheduler(Capture, renderer, () => open, (_, _) => { }, TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        Assert.Equal(0, delay.Started);
        Assert.Equal(0, scheduler.Generation);

        open = true;
        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;
        Assert.Equal(2, renderer.Seen.Count);
    }

    [Fact]
    public async Task FullRenderInProgressIsRaisedAroundTheFullRenderOnly()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var states = new List<bool>();
        RenderScheduler scheduler = null!;
        renderer.Gate = r => { states.Add(scheduler.FullRenderInProgress); return Task.CompletedTask; };
        scheduler = new RenderScheduler(Capture, renderer, () => true, (_, _) => { }, TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;

        Assert.Equal(new[] { false, true }, states);
        Assert.False(scheduler.FullRenderInProgress);
    }

    [Fact]
    public async Task CancelDuringTheDebounceStopsEverythingButLeavesTheSchedulerUsable()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        var scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        scheduler.Cancel();
        await scheduler.Idle;

        Assert.Empty(renderer.Seen);
        Assert.Empty(published);
        Assert.Equal(2, scheduler.Generation);          // Schedule() then Cancel() each bump it

        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;

        Assert.Equal(2, renderer.Seen.Count);           // Cancel left the scheduler usable
        Assert.Single(published, r => !r.IsPreview);
    }

    [Fact]
    public async Task StateChangedFiresExactlyTwiceAroundTheFullRender()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var states = new List<bool>();
        RenderScheduler scheduler = null!;
        scheduler = new RenderScheduler(Capture, renderer, () => true, (_, _) => { }, TimeSpan.Zero, delay.Wait);
        scheduler.StateChanged += () => states.Add(scheduler.FullRenderInProgress);

        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;

        Assert.Equal(new[] { true, false }, states);
    }
}
