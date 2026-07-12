using System;
using System.Drawing;
using System.Drawing.Imaging;
using PaintTranslator.Data;
using PaintTranslator.Imaging;

namespace PaintTranslator.BlendTests
{
    /// <summary>
    /// Renders labeled gradient strips that sweep between two paints using the
    /// application's subtractive mixer, so blend behavior can be judged visually.
    /// </summary>
    public static class GradientStripRenderer
    {
        // Layout constants shared by every strip so the test window lines up in
        // neat, comparable rows.
        private const int LabelHeight = 18;
        private const int GradientHeight = 40;

        /// <summary>
        /// Renders a horizontal gradient strip blending from 100% of the left
        /// paint to 100% of the right paint, with a label band above it naming
        /// both paints.
        /// </summary>
        /// <param name="left">The paint at full concentration on the left edge.</param>
        /// <param name="right">The paint at full concentration on the right edge.</param>
        /// <param name="width">The strip width in pixels; must be at least 2.</param>
        /// <returns>A bitmap containing the label band and the gradient.</returns>
        public static Bitmap Render(GoldenPaint left, GoldenPaint right, int width)
        {
            if (width < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Strip width must be at least 2 pixels.");
            }

            var bitmap = new Bitmap(width, LabelHeight + GradientHeight, PixelFormat.Format24bppRgb);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);

                // Name each end above the edge it owns so the reader can tell at a
                // glance which direction the mix runs.
                using (var font = new Font("Segoe UI", 9f))
                {
                    graphics.DrawString(left.Name, font, Brushes.Black, 0f, 1f);

                    SizeF rightSize = graphics.MeasureString(right.Name, font);
                    graphics.DrawString(right.Name, font, Brushes.Black, width - rightSize.Width, 1f);
                }

                // One mixed color per column: the right paint's share rises
                // linearly from 0 at the left edge to 1 at the right edge.
                using (var pen = new Pen(Color.Black))
                {
                    for (int x = 0; x < width; x++)
                    {
                        double weightOfRight = x / (double)(width - 1);
                        pen.Color = SubtractivePaintMixer.Mix(left.Color, right.Color, weightOfRight);
                        graphics.DrawLine(pen, x, LabelHeight, x, LabelHeight + GradientHeight);
                    }
                }
            }

            return bitmap;
        }
    }
}
