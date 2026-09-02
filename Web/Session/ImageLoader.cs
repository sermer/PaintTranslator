using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using PaintTranslator.Imaging;
using PaintTranslator.Web.Interop;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The funnel every input path ends in. The format is sniffed from the bytes, as
/// Windows/ImageDecoder does, because extensions lie and the clipboard has none.
/// </summary>
public static class ImageLoader
{
    /// <summary>Same list as Windows/ImageDecoder.SupportedExtensions.</summary>
    public const string AcceptList = ".png,.jpg,.jpeg,.jfif,.bmp,.gif,.tif,.tiff,.webp,.avif,.heic,.heif,.psd";

    // Only this method touches JSObject/DecoderInterop, so only it is marked
    // browser-only; AcceptList stays plain so the host page can bind it to
    // <InputFile accept> without every caller needing the platform attribute too.
    [SupportedOSPlatform("browser")]
    public static async Task<PixelImage> LoadAsync(byte[] bytes, string name)
    {
        ImageFileFormat format = ImageFormatSniffer.Detect(bytes);
        if (format == ImageFileFormat.Unknown)
        {
            throw new ImageLoadException($"'{name}' is not a supported image.");
        }
        JSObject result;
        try
        {
            result = await DecoderInterop.DecodeAsync(bytes, format.ToString());
        }
        catch (JSException ex)
        {
            throw new ImageLoadException($"Could not open '{name}': {ex.Message}", ex);
        }
        int width = result.GetPropertyAsInt32("width");
        int height = result.GetPropertyAsInt32("height");
        byte[] rgba = result.GetPropertyAsByteArray("rgba")
            ?? throw new ImageLoadException($"Could not open '{name}': decoder returned no pixels.");
        return PixelCodec.ToPixelImage(rgba, width, height);
    }
}
