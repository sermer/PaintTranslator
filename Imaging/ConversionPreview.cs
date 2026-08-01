using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Builds the small source bitmap used for interactive conversion and translates
    /// source-pixel controls into that bitmap's coordinate system. Resizing happens
    /// before palette mapping, so every preview output pixel still comes directly from
    /// the achievable candidate set.
    /// </summary>
    internal static class ConversionPreview
    {
        /// <summary>The longest edge rendered while a control is being adjusted.</summary>
        public const int MaximumDimension = 384;

        public static Bitmap CreateSource(Bitmap source, int maximumDimension = MaximumDimension)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (maximumDimension <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumDimension));
            }

            double scale = Math.Min(1.0, maximumDimension / (double)Math.Max(source.Width, source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var preview = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(preview))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            }

            return preview;
        }

        /// <summary>
        /// Scales a source-image radius to a preview radius. A positive control stays
        /// positive even after a large downsample; zero retains its no-op meaning.
        /// </summary>
        public static int ScaleRadius(int sourcePixels, Size sourceSize, Size previewSize)
        {
            if (sourcePixels <= 0)
            {
                return 0;
            }
            if (sourceSize.Width <= 0 || sourceSize.Height <= 0 ||
                previewSize.Width <= 0 || previewSize.Height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceSize));
            }

            double scale = Math.Min(
                previewSize.Width / (double)sourceSize.Width,
                previewSize.Height / (double)sourceSize.Height);
            return Math.Max(1, (int)Math.Round(sourcePixels * scale));
        }
    }
}
