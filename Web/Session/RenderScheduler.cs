using PaintTranslator.Imaging;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Port of MainForm's preview loop: debounce, then a small preview, then the full
/// frame, dropping anything a newer generation has superseded. The debounce is
/// injectable because the coalescing and supersede rules are what the tests pin,
/// and they cannot be pinned against wall-clock timers.
/// </summary>
public sealed class RenderScheduler
{
    private readonly Func<bool, long, RenderRequest?> capture;
    private readonly IFrameRenderer renderer;
    private readonly Func<bool> canRun;
    private readonly Action<RenderRequest, PixelImage> publish;
    private readonly TimeSpan debounce;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;

    private CancellationTokenSource? debounceCts;
    private CancellationTokenSource? renderCts;
    private bool pending;
    private bool running;
    private int outstandingDebounces;
    private TaskCompletionSource idle = CompletedSource();

    public RenderScheduler(
        Func<bool, long, RenderRequest?> capture,
        IFrameRenderer renderer,
        Func<bool> canRun,
        Action<RenderRequest, PixelImage> publish,
        TimeSpan debounce,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.capture = capture;
        this.renderer = renderer;
        this.canRun = canRun;
        this.publish = publish;
        this.debounce = debounce;
        this.delay = delay ?? Task.Delay;
    }

    public long Generation { get; private set; }
    public bool FullRenderInProgress { get; private set; }
    public event Action? StateChanged;

    /// <summary>
    /// Completes only once no debounce is pending and no loop is running. It must span the
    /// whole debounce window, not just the loop, because the host running the tests can defer
    /// a resumed continuation onto another thread — if <c>Idle</c> went complete the instant a
    /// loop finished, a caller could observe it between "debounce scheduled" and "loop actually
    /// started" and stop waiting before the scheduled work ever ran.
    /// </summary>
    public Task Idle => idle.Task;

    /// <summary>Bumps the generation so an in-flight result is discarded, then restarts the debounce.</summary>
    public void Schedule()
    {
        if (!canRun())
        {
            return;
        }
        Generation++;
        if (idle.Task.IsCompleted)
        {
            idle = new TaskCompletionSource();
        }
        Interlocked.Increment(ref outstandingDebounces);
        renderCts?.Cancel();
        pending = false;
        debounceCts?.Cancel();
        debounceCts = new CancellationTokenSource();
        _ = RunAfterDebounceAsync(debounceCts.Token);
    }

    /// <summary>Stops everything without starting a new render (image load, wheel display).</summary>
    public void Cancel()
    {
        Generation++;
        renderCts?.Cancel();
        pending = false;
        debounceCts?.Cancel();
    }

    private async Task RunAfterDebounceAsync(CancellationToken token)
    {
        // Every exit path below — a cancelled debounce, staleness caught right after it, the
        // "another loop already owns this" bail-out, and falling out of the loop — must retire
        // this invocation's share of outstandingDebounces exactly once and re-check whether
        // Idle can now complete. That single accounting point is the outer finally.
        try
        {
            try
            {
                await delay(debounce, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (token.IsCancellationRequested)
            {
                return;
            }
            pending = true;
            if (running)
            {
                return;
            }
            running = true;
            try
            {
                try
                {
                    while (pending && canRun())
                    {
                        pending = false;
                        using var cts = new CancellationTokenSource();
                        renderCts = cts;
                        try
                        {
                            RenderRequest? preview = capture(true, Generation);
                            if (preview == null)
                            {
                                return;
                            }
                            PixelImage? previewFrame = await RenderGuardedAsync(preview, cts.Token);
                            if (previewFrame == null || !CanDisplay(preview, cts.Token))
                            {
                                continue;
                            }
                            publish(preview, previewFrame);

                            RenderRequest? full = capture(false, Generation);
                            if (full == null)
                            {
                                continue;
                            }
                            SetFullInProgress(true);
                            // Configuration A renders on the single UI thread, so nothing
                            // else can repaint while the (blocking) full render below runs;
                            // yielding here lets the browser actually paint the wait-cursor
                            // CSS class SetFullInProgress just turned on.
                            await Task.Yield();
                            try
                            {
                                PixelImage? fullFrame = await RenderGuardedAsync(full, cts.Token);
                                if (fullFrame != null && CanDisplay(full, cts.Token))
                                {
                                    publish(full, fullFrame);
                                }
                            }
                            finally
                            {
                                SetFullInProgress(false);
                            }
                        }
                        finally
                        {
                            if (ReferenceEquals(renderCts, cts))
                            {
                                renderCts = null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // capture(...) and publish(...) run outside RenderGuardedAsync's own
                    // try/catch (that only wraps the renderer call), so a throw from either
                    // would otherwise unwind this fire-and-forget task with nothing to
                    // observe it: Idle would still complete "successfully" and the exception
                    // would simply vanish. Log it and let the loop exit; running/outstanding
                    // bookkeeping below still settles Idle correctly.
                    Console.Error.WriteLine($"Render loop failed: {ex}");
                }
            }
            finally
            {
                // No SettleIdle() here: this invocation's own share of outstandingDebounces is
                // still outstanding at this point (it is retired below, in the outer finally),
                // so the count can never be zero yet — checking here would always be a no-op.
                running = false;
            }
        }
        finally
        {
            // Interlocked because a lost update between two invocations' decrements — one
            // possibly running on a thread-pool thread the host deferred a continuation onto,
            // the other on whichever thread cancelled it — could otherwise leave the count
            // permanently above zero and hang Idle forever.
            int remaining = Interlocked.Decrement(ref outstandingDebounces);
            SettleIdle(remaining);
        }
    }

    /// <summary>The one place Idle's completion is decided, so every exit path agrees on the rule.</summary>
    private void SettleIdle(int outstandingDebounceCount)
    {
        if (outstandingDebounceCount == 0 && !running)
        {
            idle.TrySetResult();
        }
    }

    private async Task<PixelImage?> RenderGuardedAsync(RenderRequest request, CancellationToken token)
    {
        try
        {
            return await renderer.RenderAsync(request, token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Render failed: {ex}");
            return null;
        }
    }

    private bool CanDisplay(RenderRequest request, CancellationToken token) =>
        !token.IsCancellationRequested && request.Generation == Generation && canRun();

    private void SetFullInProgress(bool value)
    {
        FullRenderInProgress = value;
        StateChanged?.Invoke();
    }

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource();
        source.SetResult();
        return source;
    }
}
