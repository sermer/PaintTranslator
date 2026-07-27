using System;
using System.Drawing;
using System.Drawing.Imaging;
using PaintTranslator.Pigments;
using PaintTranslator.Imaging;

namespace PaintTranslator.BlendTests
{
    /// <summary>
    /// Renders labeled gradient strips that sweep between two paints using the
    /// application's measured Kubelka-Munk kernel, so blend behavior can be
    /// judged visually.
    /// </summary>
    public static class GradientStripRenderer
    {
        // Strip height shared by every strip so the test window lines up in
        // neat, comparable rows.
        private const int GradientHeight = 40;

        // Perceived-brightness cutoff (0-255) below which a paint is dark
        // enough that overlaid text needs to be white instead of black.
        private const int DarkPaintBrightness = 140;

        /// <summary>
        /// Renders a horizontal gradient strip blending from 100% of the left
        /// paint to 100% of the right paint, with each paint's name drawn on top
        /// of the gradient above the edge it owns.
        /// </summary>
        /// <param name="left">The paint at full concentration on the left edge.</param>
        /// <param name="right">The paint at full concentration on the right edge.</param>
        /// <param name="width">The strip width in pixels; must be at least 2.</param>
        /// <returns>A bitmap containing the labeled gradient.</returns>
        public static Bitmap Render(PigmentCoefficients left, PigmentCoefficients right, int width)
        {
            if (width < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Strip width must be at least 2 pixels.");
            }

            var bitmap = new Bitmap(width, GradientHeight, PixelFormat.Format24bppRgb);
            var reflectance = new double[SpectralBands.Count];

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // One mixed color per column: the right paint's share rises
                // linearly from 0 at the left edge to 1 at the right edge. The
                // gradient is drawn first so the labels land on top of it.
                using (var pen = new Pen(Color.Black))
                {
                    for (int x = 0; x < width; x++)
                    {
                        double weightOfRight = x / (double)(width - 1);
                        KubelkaMunk.Mix(
                            new[] { left, right },
                            new[] { 1.0 - weightOfRight, weightOfRight },
                            reflectance);
                        pen.Color = SpectralRenderer.ToDisplayColor(reflectance, out _);
                        graphics.DrawLine(pen, x, 0, x, GradientHeight);
                    }
                }

                // Name each end above the edge it owns so the reader can tell at a
                // glance which direction the mix runs, in whichever of black or
                // white stays legible against that end's paint.
                using (var font = new Font("Segoe UI", 9f))
                {
                    using (var leftBrush = new SolidBrush(ContrastingTextColor(MassTone(left, reflectance))))
                    {
                        graphics.DrawString(left.Name, font, leftBrush, 0f, 1f);
                    }

                    using (var rightBrush = new SolidBrush(ContrastingTextColor(MassTone(right, reflectance))))
                    {
                        SizeF rightSize = graphics.MeasureString(right.Name, font);
                        graphics.DrawString(right.Name, font, rightBrush, width - rightSize.Width, 1f);
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Renders a paint's mass tone, which is what the kernel produces from its
        /// curves at full concentration.
        /// </summary>
        /// <param name="paint">The paint to render.</param>
        /// <param name="reflectance">A scratch spectrum buffer.</param>
        /// <returns>The paint's colour straight from the tube.</returns>
        private static Color MassTone(PigmentCoefficients paint, double[] reflectance)
        {
            KubelkaMunk.Mix(new[] { paint }, new[] { 1.0 }, reflectance);

            return SpectralRenderer.ToDisplayColor(reflectance, out _);
        }

        /// <summary>
        /// Picks black or white, whichever reads better against the given
        /// background color.
        /// </summary>
        /// <param name="background">The color the text will be drawn over.</param>
        /// <returns>White for dark backgrounds, black for light ones.</returns>
        private static Color ContrastingTextColor(Color background)
        {
            // Perceived brightness weights the channels by how strongly the eye
            // responds to each (ITU-R BT.601 luma).
            double brightness = 0.299 * background.R + 0.587 * background.G + 0.114 * background.B;
            return brightness < DarkPaintBrightness ? Color.White : Color.Black;
        }
    }
}
