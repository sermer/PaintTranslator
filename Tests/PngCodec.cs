using System;
using System.Runtime.InteropServices;
using PaintTranslator.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Reads and writes <see cref="PixelImage"/> as PNG through ImageSharp. Uses the
    /// <see cref="Bgra32"/> pixel type because its little-endian byte order is exactly
    /// a packed <c>0xAARRGGBB</c> int, so the copy is a reinterpretation rather than a
    /// per-channel shuffle that could silently swap red and blue.
    /// </summary>
    internal static class PngCodec
    {
        internal static PixelImage Load(string path)
        {
            using Image<Bgra32> image = Image.Load<Bgra32>(path);
            var pixels = new int[image.Width * image.Height];
            image.CopyPixelDataTo(MemoryMarshal.AsBytes(pixels.AsSpan()));
            return PixelImage.FromPixels(image.Width, image.Height, pixels);
        }

        internal static void Save(PixelImage image, string path)
        {
            using Image<Bgra32> encoded = Image.LoadPixelData<Bgra32>(
                MemoryMarshal.AsBytes(image.Pixels), image.Width, image.Height);
            encoded.SaveAsPng(path);
        }
    }
}
