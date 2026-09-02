using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// The one place the WinForms app converts between GDI bitmaps and the kernel's
    /// <see cref="PixelImage"/>. Keeping both directions here means the kernel never
    /// references <c>System.Drawing.Common</c>, which only exists on Windows.
    /// </summary>
    public static class GdiImageAdapter
    {
        /// <summary>
        /// Snapshots a bitmap of any pixel format into packed ARGB. Drawing it onto a
        /// fresh 32bppArgb surface first is what normalises indexed, 24-bit and
        /// premultiplied sources; reading <c>LockBits</c> on the original would hand
        /// back whatever format it happened to be in.
        /// </summary>
        public static PixelImage FromBitmap(Bitmap source)
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

                return PixelImage.FromPixels(width, height, packed);
            }
            finally
            {
                normalized.UnlockBits(data);
            }
        }

        /// <summary>
        /// Copies straight from <see cref="PixelImage.Pixels"/> into the locked bits
        /// instead of through a <c>CopyPixels()</c> clone; a converted photo's frame can
        /// run tens of megabytes, and this is called on every displayed frame, so the
        /// extra full-size buffer the clone used to cost is not free to repeat.
        /// </summary>
        public static unsafe Bitmap ToBitmap(PixelImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            int width = image.Width;
            int height = image.Height;
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                if (data.Stride == width * 4)
                {
                    image.Pixels.CopyTo(new Span<int>((void*)data.Scan0, width * height));
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        image.Pixels.Slice(y * width, width)
                            .CopyTo(new Span<int>((void*)(data.Scan0 + (y * data.Stride)), width));
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
