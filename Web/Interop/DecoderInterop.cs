using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace PaintTranslator.Web.Interop;

/// <summary>
/// The image-decode half of the interop module. Split from <see cref="CanvasInterop"/>
/// only as a C# file, not a JS one: both wrappers import the same "interop" module, so
/// JS stays a single decision-free file while the generated marshaling code here has a
/// name that matches what it does.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class DecoderInterop
{
    // Span<byte>/[JSMarshalAs<JSType.MemoryView>] (what CanvasInterop's synchronous
    // PutFrame uses) is rejected by the source generator on a method returning Task
    // (SYSLIB1072: a Span cannot be captured across the call). ArraySegment<byte> is
    // the async-safe equivalent the generator does support for a zero-copy view.
    [JSImport("decode", CanvasInterop.ModuleName)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> DecodeAsync(
        [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> bytes, string format);
}
