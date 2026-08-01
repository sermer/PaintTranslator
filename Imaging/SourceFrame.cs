using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// An immutable, normalized snapshot of a source image. It is captured once when
    /// an image is loaded and can then be shared safely by canceled and replacement
    /// renders without cloning a GDI bitmap on the UI thread.
    /// </summary>
    internal sealed class SourceFrame
    {
        private readonly int[] pixels;

        private SourceFrame(int width, int height, int[] pixels)
        {
            Width = width;
            Height = height;
            this.pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public Size Size => new Size(Width, Height);

        public static SourceFrame Create(Bitmap source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int width = source.Width;
            int height = source.Height;
            using var normalized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(normalized))
            {
                // Matches the normalization formerly performed inside every render.
                graphics.DrawImage(source, 0, 0, width, height);
            }

            BitmapData data = normalized.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int strideInts = data.Stride / 4;
                var packed = new int[width * height];
                var row = new int[strideInts];
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(data.Scan0 + (y * data.Stride), row, 0, strideInts);
                    Array.Copy(row, 0, packed, y * width, width);
                }

                return new SourceFrame(width, height, packed);
            }
            finally
            {
                normalized.UnlockBits(data);
            }
        }

        public int[] CopyPixels()
        {
            return (int[])pixels.Clone();
        }

        public int AlphaAt(int index)
        {
            return pixels[index] & unchecked((int)0xFF000000);
        }

        public Bitmap CreateBitmap(int[] framePixels = null)
        {
            framePixels ??= pixels;
            if (framePixels.Length != pixels.Length)
            {
                throw new ArgumentException("Pixel count does not match this frame.", nameof(framePixels));
            }

            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, Width, Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int strideInts = data.Stride / 4;
                if (strideInts == Width)
                {
                    Marshal.Copy(framePixels, 0, data.Scan0, framePixels.Length);
                }
                else
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Marshal.Copy(framePixels, y * Width, data.Scan0 + (y * data.Stride), Width);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }
    }
}
