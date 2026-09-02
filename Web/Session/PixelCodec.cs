using PaintTranslator.Imaging;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The only place RGBA byte buffers (what canvas ImageData and every decoder
/// produce) meet PixelImage's packed 0xAARRGGBB ints. This codec itself never
/// premultiplies or un-premultiplies in either direction — it only repacks the
/// four bytes it is given. Whether the bytes it receives already lost colour
/// information under partial or zero alpha is decided upstream, by whichever
/// browser API produced them: see the comment above interop.js's decodeNative
/// for what that path actually delivers.
/// </summary>
public static class PixelCodec
{
    public static PixelImage ToPixelImage(ReadOnlySpan<byte> rgba, int width, int height)
    {
        if (rgba.Length != width * height * 4)
        {
            throw new ArgumentException(
                $"Expected {width * height * 4} bytes for {width}x{height}, got {rgba.Length}.", nameof(rgba));
        }
        var pixels = new int[width * height];
        for (int i = 0, b = 0; i < pixels.Length; i++, b += 4)
        {
            pixels[i] = (rgba[b + 3] << 24) | (rgba[b] << 16) | (rgba[b + 1] << 8) | rgba[b + 2];
        }
        return PixelImage.FromPixels(width, height, pixels);
    }

    public static byte[] ToRgba(PixelImage image)
    {
        ReadOnlySpan<int> pixels = image.Pixels;
        var rgba = new byte[pixels.Length * 4];
        for (int i = 0, b = 0; i < pixels.Length; i++, b += 4)
        {
            int argb = pixels[i];
            rgba[b] = (byte)(argb >> 16);
            rgba[b + 1] = (byte)(argb >> 8);
            rgba[b + 2] = (byte)argb;
            rgba[b + 3] = (byte)(argb >> 24);
        }
        return rgba;
    }
}
