using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace PaintTranslator.Web.Interop;

/// <summary>
/// Pixel buffers cross to JavaScript as memory views, not JSON: a 1920×1080 frame
/// is 8 MB and the IJSRuntime path would base64 it. Everything else here is small
/// and goes the same way for consistency.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class CanvasInterop
{
    public const string ModuleName = "interop";

    public static Task ImportAsync() => JSHost.ImportAsync(ModuleName, "../js/interop.js");

    [JSImport("putFrame", ModuleName)]
    public static partial void PutFrame(string canvasId, int width, int height,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> rgba);

    [JSImport("setView", ModuleName)]
    public static partial void SetView(string canvasId, double scale, double offsetX, double offsetY, bool smooth);

    [JSImport("setGrid", ModuleName)]
    public static partial void SetGrid(string canvasId, [JSMarshalAs<JSType.Array<JSType.Number>>] double[] segments);

    [JSImport("clearFrame", ModuleName)]
    public static partial void ClearFrame(string canvasId);

    [JSImport("downloadPng", ModuleName)]
    public static partial void DownloadPng(string canvasId, string fileName);
}
