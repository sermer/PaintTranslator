using System;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Builds the small <see cref="PixelImage"/> used for interactive conversion and
    /// translates source-pixel controls into that image's coordinate system. Resizing happens
    /// before palette mapping, so every preview output pixel still comes directly from
    /// the achievable candidate set.
    /// </summary>
    public static class ConversionPreview
    {
        /// <summary>The longest edge rendered while a control is being adjusted.</summary>
        public const int MaximumDimension = 384;

        /// <summary>
        /// Returns the source itself when it already fits, since a
        /// <see cref="PixelImage"/> is immutable and sharing it costs nothing.
        /// </summary>
        public static PixelImage CreateSource(PixelImage source, int maximumDimension = MaximumDimension)
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
            if (width == source.Width && height == source.Height)
            {
                return source;
            }

            return Downsample(source, width, height);
        }

        /// <summary>
        /// Area-averaging reduction: each output pixel is the coverage-weighted mean of
        /// the straight (not premultiplied) A, R, G and B of the source pixels under it.
        /// Chosen over GDI's bicubic, which the WinForms app used before, because it
        /// runs identically on every platform, never rings or overshoots, and keeps a
        /// flat region exactly flat. No gamma linearisation, matching the pipeline's
        /// own blur stages so the preview and the full render smooth the same way.
        /// </summary>
        internal static PixelImage Downsample(PixelImage source, int width, int height)
        {
            if (width <= 0 || height <= 0 || width > source.Width || height > source.Height)
            {
                throw new ArgumentOutOfRangeException(width > source.Width || width <= 0 ? nameof(width) : nameof(height));
            }

            double xRatio = source.Width / (double)width;
            double yRatio = source.Height / (double)height;
            var output = new int[width * height];

            for (int oy = 0; oy < height; oy++)
            {
                double top = oy * yRatio;
                double bottom = (oy + 1) * yRatio;
                int firstRow = (int)top;
                int lastRow = Math.Min(source.Height - 1, (int)Math.Ceiling(bottom) - 1);

                for (int ox = 0; ox < width; ox++)
                {
                    double left = ox * xRatio;
                    double right = (ox + 1) * xRatio;
                    int firstColumn = (int)left;
                    int lastColumn = Math.Min(source.Width - 1, (int)Math.Ceiling(right) - 1);

                    double a = 0.0, r = 0.0, g = 0.0, b = 0.0, total = 0.0;
                    for (int sy = firstRow; sy <= lastRow; sy++)
                    {
                        double rowWeight = Math.Min(bottom, sy + 1) - Math.Max(top, sy);
                        for (int sx = firstColumn; sx <= lastColumn; sx++)
                        {
                            double weight = rowWeight * (Math.Min(right, sx + 1) - Math.Max(left, sx));
                            int pixel = source[sx, sy];
                            a += weight * ((pixel >> 24) & 0xFF);
                            r += weight * ((pixel >> 16) & 0xFF);
                            g += weight * ((pixel >> 8) & 0xFF);
                            b += weight * (pixel & 0xFF);
                            total += weight;
                        }
                    }

                    output[(oy * width) + ox] =
                        (Channel(a / total) << 24) | (Channel(r / total) << 16) | (Channel(g / total) << 8) | Channel(b / total);
                }
            }

            return PixelImage.FromPixels(width, height, output);
        }

        /// <summary>
        /// Rounds half away from zero so a 50/50 straddle lands on 128, not on
        /// whichever neighbour banker's rounding happens to prefer. The 1e-9 nudge
        /// corrects for accumulated floating-point error in the weighted sum: an
        /// exact half such as 127.5 can arrive as 127.49999999999999 after summing
        /// weights derived from a ratio like 4/3, which would silently round down
        /// instead of landing on the boundary. The nudge is far smaller than one
        /// 8-bit step, so it cannot change a result that was not already on a
        /// boundary.
        /// </summary>
        private static int Channel(double value)
        {
            return Math.Clamp((int)Math.Round(value + 1e-9, MidpointRounding.AwayFromZero), 0, 255);
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
