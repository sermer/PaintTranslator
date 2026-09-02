using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PaintTranslator.Imaging;
using PaintTranslator.Web.Interop;
using PaintTranslator.Web.Session;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace PaintTranslator.Web.Components;

/// <summary>
/// The WinForms ImageCanvas with GDI replaced by interop.js. All geometry stays
/// in ImageViewport; the gesture table below is copied from ImageCanvas.HandleWheel
/// and StepMagnifier so the two apps feel identical. A pinch on a Mac trackpad
/// arrives as a wheel event with ctrlKey set in every browser, which is why
/// ctrl-wheel is zoom rather than a modifier the user has to know.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class ImageCanvas
{
    public const string CanvasId = "paint-canvas";
    private const float WheelDetent = 120f;
    private const float PanPixelsPerDetent = 100f;
    private const double ZoomPerWheelUnit = 1.0015;
    private const float DragThreshold = 3f;
    private const float ScaleEpsilon = 0.001f;
    private static readonly float[] MagnifierSteps = { 2f, 4f, 8f };

    private readonly ImageViewport viewport = new();
    private DotNetObjectReference<ImageCanvas>? self;
    private bool panning, dragging;
    private PointF dragStart, lastPointer;
    private bool bound;

    /// <summary>Raised with the client point and lines, or (null, null) to hide the tooltip.</summary>
    [Parameter] public EventCallback<(Point? At, string[]? Lines)> HoverChanged { get; set; }

    /// <summary>The host's CSS size, which the tooltip needs to flip near the edges.</summary>
    [Parameter] public EventCallback<(int Width, int Height)> Resized { get; set; }

    // DynamicDependencyAttribute is only valid on a constructor, method or field, not
    // a class (the brief's placement does not compile), so it is anchored to
    // OnInitialized instead: whenever a live ImageCanvas reaches that lifecycle point,
    // its JSInvokable methods are kept, which is what a Release/AOT trim would
    // otherwise remove since JS calls them by name rather than through a visible
    // C# call site.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ImageCanvas))]
    protected override void OnInitialized()
    {
        Session.FrameReady += OnFrameReady;
        Session.Changed += OnSessionChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || bound) return;
        bound = true;
        self = DotNetObjectReference.Create(this);
        // Same URL as CanvasInterop.ImportAsync resolves to, so the browser hands back
        // the same module instance and both paths share its canvas map.
        var module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/interop.js");
        await module.InvokeVoidAsync("bind", CanvasId, self);
        if (Session.Displayed != null) OnFrameReady(Session.Displayed);
    }

    // FrameReady can fire off the UI thread: PipelineRenderer's Task.Run is a no-op
    // under configuration A (single UI thread) but a real background render under
    // configuration B, and JS interop must run on the renderer's own sync context.
    private void OnFrameReady(PixelImage frame) => InvokeAsync(() => ApplyFrame(frame));

    private void ApplyFrame(PixelImage frame)
    {
        // Called via a discarded InvokeAsync task (OnFrameReady), so an exception here
        // would otherwise unwind silently and never reach the browser console: imaging
        // failures throughout this codebase are meant to be visual, not fatal.
        try
        {
            // ImageViewport.ImageSize's setter already calls Fit() when the size changes
            // (a same-size replacement, such as swapping a photo for its paint conversion,
            // keeps the view in place), so there is nothing left to do here.
            viewport.ImageSize = frame.Size;
            CanvasInterop.PutFrame(CanvasId, frame.Width, frame.Height, PixelCodec.ToRgba(frame));
            PushView();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Frame push failed: {ex}");
        }
    }

    // Changed can fire off the UI thread for the same reason OnFrameReady can (see
    // above), so PushGrid's JS interop call is routed through InvokeAsync rather than
    // run synchronously.
    private void OnSessionChanged() => InvokeAsync(() =>
    {
        // Same exposure as ApplyFrame: this runs on a discarded InvokeAsync task, so a
        // JS failure in ClearFrame or SetGrid would otherwise vanish without a console line.
        try
        {
            if (Session.Displayed == null)
            {
                // ShowPhoto() can leave nothing displayed (Color Wheel -> Traditional -> Back
                // to photo, reached without ever loading a photo); without this the canvas
                // keeps the last painted wheel showing underneath the "Drop a photo" card.
                viewport.ImageSize = Size.Empty;
                CanvasInterop.ClearFrame(CanvasId);
            }
            PushGrid();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Session change push failed: {ex}");
        }
        StateHasChanged();
    });

    private void PushView()
    {
        RectangleF bounds = viewport.GetImageBounds();
        bool smooth = !(viewport.Scale > 1f && !viewport.IsFitted);
        CanvasInterop.SetView(CanvasId, viewport.Scale, bounds.X, bounds.Y, smooth);
        PushGrid();
    }

    private void PushGrid()
    {
        if (!Session.ShowGrid || Session.Displayed == null)
        {
            CanvasInterop.SetGrid(CanvasId, Array.Empty<double>());
            return;
        }
        RectangleF bounds = viewport.GetImageBounds();
        var flat = new List<double>();
        // GridGeometry.Segments already appends the four border edges to the interior
        // dividers, which is what GridOverlayRenderer draws, so there is no separate
        // border rectangle to build here.
        foreach (GridGeometry.Segment segment in GridGeometry.Segments(bounds, Session.GridColumns, Session.GridRows))
        {
            flat.Add(segment.Start.X); flat.Add(segment.Start.Y); flat.Add(segment.End.X); flat.Add(segment.End.Y);
        }
        CanvasInterop.SetGrid(CanvasId, flat.ToArray());
    }

    [JSInvokable]
    public async Task OnResize(double width, double height)
    {
        viewport.ContainerSize = new Size((int)width, (int)height);
        PushView();
        await Resized.InvokeAsync(((int)width, (int)height));
    }

    [JSInvokable]
    public async Task OnWheel(double deltaX, double deltaY, bool ctrl, bool shift, double x, double y)
    {
        if (Session.Displayed == null) return;
        var cursor = new PointF((float)x, (float)y);
        if (ctrl)
        {
            // Browsers report wheel deltas in pixels; -deltaY is "zoom in", as in WinForms.
            viewport.ZoomTo(viewport.Scale * (float)Math.Pow(ZoomPerWheelUnit, -deltaY), cursor);
        }
        else if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            viewport.PanBy((float)(-deltaX / WheelDetent * PanPixelsPerDetent), 0f);
        }
        else if (shift)
        {
            viewport.PanBy((float)(-deltaY / WheelDetent * PanPixelsPerDetent), 0f);
        }
        else
        {
            viewport.PanBy(0f, (float)(-deltaY / WheelDetent * PanPixelsPerDetent));
        }
        PushView();
        await UpdateHover(cursor);
    }

    [JSInvokable]
    public async Task OnPointer(string kind, double x, double y, int buttons)
    {
        var p = new PointF((float)x, (float)y);
        switch (kind)
        {
            case "pointerdown" when (buttons & 1) != 0:
                dragging = true; panning = false; dragStart = p; lastPointer = p;
                await HoverChanged.InvokeAsync((null, null));
                break;
            case "pointermove":
                if (dragging)
                {
                    if (!panning && Distance(dragStart, p) > DragThreshold) { panning = true; StateHasChanged(); }
                    if (panning)
                    {
                        viewport.PanBy(p.X - lastPointer.X, p.Y - lastPointer.Y);
                        PushView();
                    }
                    lastPointer = p;
                }
                else
                {
                    await UpdateHover(p);
                }
                break;
            case "pointerup":
                if (dragging && !panning && Session.MagnifierActive && Session.Displayed != null)
                {
                    StepMagnifier(p);
                }
                dragging = false; panning = false; StateHasChanged();
                await UpdateHover(p);
                break;
            case "pointercancel":
            case "pointerleave":
                dragging = false; panning = false; StateHasChanged();
                await HoverChanged.InvokeAsync((null, null));
                break;
        }
    }

    private void StepMagnifier(PointF anchor)
    {
        float fit = viewport.FitScale;
        float target = fit;
        foreach (float step in MagnifierSteps)
        {
            if (viewport.Scale < fit * step - ScaleEpsilon) { target = fit * step; break; }
        }
        viewport.ZoomTo(target, anchor);
        PushView();
    }

    private async Task UpdateHover(PointF cursor)
    {
        if (!viewport.TryGetImagePixel(new Point((int)cursor.X, (int)cursor.Y), out Point pixel))
        {
            await HoverChanged.InvokeAsync((null, null));
            return;
        }
        string[]? lines = Session.RecipeAt(pixel.X, pixel.Y);
        await HoverChanged.InvokeAsync(lines == null ? (null, null) : (new Point((int)cursor.X, (int)cursor.Y), lines));
    }

    private static float Distance(PointF a, PointF b) => MathF.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    public void Dispose()
    {
        Session.FrameReady -= OnFrameReady;
        Session.Changed -= OnSessionChanged;
        self?.Dispose();
    }
}
