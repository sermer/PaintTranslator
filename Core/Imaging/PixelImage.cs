using System;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// An immutable image: width, height, and one packed ARGB <see cref="int"/> per
    /// pixel in row-major order with no stride padding. It is the only image type the
    /// kernel takes or returns, so the kernel compiles without <c>System.Drawing.Common</c>
    /// and can be shared safely by cancelled and replacement renders without cloning.
    /// </summary>
    /// <remarks>
    /// The byte layout is GDI's <c>Format32bppArgb</c> (<c>0xAARRGGBB</c>), so the
    /// Windows adapter is a straight memory copy and nothing downstream ever reorders
    /// channels. Every operation that needs a mutable buffer takes a
    /// <see cref="CopyPixels"/> and works on that; the image itself is never written.
    /// </remarks>
    public sealed class PixelImage
    {
        private readonly int[] pixels;

        private PixelImage(int width, int height, int[] pixels)
        {
            Width = width;
            Height = height;
            this.pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public Size Size => new Size(Width, Height);

        /// <summary>
        /// The packed pixels, read-only. Exposed as a span rather than the array so a
        /// caller cannot mutate an image another render is still reading.
        /// </summary>
        public ReadOnlySpan<int> Pixels => pixels;

        public int this[int x, int y] => pixels[(y * Width) + x];

        /// <summary>
        /// Wraps a caller-built buffer without copying it. The caller gives up the
        /// buffer: writing to it afterwards would break immutability for every reader.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the buffer length is not
        /// <paramref name="width"/> × <paramref name="height"/>.</exception>
        public static PixelImage FromPixels(int width, int height, int[] pixels)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height));
            }
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }
            if (pixels.Length != width * height)
            {
                throw new ArgumentException(
                    $"Expected {width * height} pixels for {width}x{height}, got {pixels.Length}.",
                    nameof(pixels));
            }

            return new PixelImage(width, height, pixels);
        }

        public static PixelImage Filled(int width, int height, int argb)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height));
            }

            var buffer = new int[width * height];
            Array.Fill(buffer, argb);
            return new PixelImage(width, height, buffer);
        }

        public int[] CopyPixels()
        {
            return (int[])pixels.Clone();
        }

        public int AlphaAt(int index)
        {
            return pixels[index] & unchecked((int)0xFF000000);
        }
    }
}
