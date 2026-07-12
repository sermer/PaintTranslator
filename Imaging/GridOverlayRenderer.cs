using System;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Draws a configurable grid overlay on top of a displayed image, and computes
    /// where a zoomed image actually renders inside its containing control.
    /// </summary>
    public static class GridOverlayRenderer
    {
        /// <summary>
        /// Draws evenly spaced grid lines over the given bounds, including a border
        /// around the outer edge.
        /// </summary>
        /// <param name="graphics">The graphics surface to draw on.</param>
        /// <param name="bounds">The rectangle the grid should cover, typically the displayed image area.</param>
        /// <param name="columns">The number of grid segments across the width. Must be at least 1.</param>
        /// <param name="rows">The number of grid segments down the height. Must be at least 1.</param>
        public static void DrawGrid(Graphics graphics, RectangleF bounds, int columns, int rows)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }
            if (columns < 1 || rows < 1)
            {
                throw new ArgumentOutOfRangeException(columns < 1 ? nameof(columns) : nameof(rows),
                    "Grid must have at least one segment in each direction.");
            }

            // Each line is drawn twice - a wider translucent dark stroke under a thin
            // light stroke - so the grid stays visible over both light and dark image areas.
            using (var underPen = new Pen(Color.FromArgb(150, 0, 0, 0), 3f))
            using (var overPen = new Pen(Color.White, 1f))
            {
                DrawGridLines(graphics, bounds, columns, rows, underPen);
                DrawGridLines(graphics, bounds, columns, rows, overPen);
            }
        }

        /// <summary>
        /// Draws one pass of grid lines (verticals, horizontals, and the outer border)
        /// with the supplied pen.
        /// </summary>
        /// <param name="graphics">The graphics surface to draw on.</param>
        /// <param name="bounds">The rectangle the grid should cover.</param>
        /// <param name="columns">The number of grid segments across the width.</param>
        /// <param name="rows">The number of grid segments down the height.</param>
        /// <param name="pen">The pen to draw the lines with.</param>
        private static void DrawGridLines(Graphics graphics, RectangleF bounds, int columns, int rows, Pen pen)
        {
            // Positions are computed as fractions of the full span rather than by
            // accumulating a step, so rounding errors cannot drift across many segments.
            for (int i = 1; i < columns; i++)
            {
                float x = bounds.Left + bounds.Width * i / columns;
                graphics.DrawLine(pen, x, bounds.Top, x, bounds.Bottom);
            }

            for (int i = 1; i < rows; i++)
            {
                float y = bounds.Top + bounds.Height * i / rows;
                graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
            }

            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>
        /// Computes the rectangle an image occupies inside a container when scaled to
        /// fit while preserving aspect ratio and centered, matching the behavior of
        /// <see cref="System.Windows.Forms.PictureBoxSizeMode"/>.Zoom.
        /// </summary>
        /// <param name="containerSize">The client size of the containing control.</param>
        /// <param name="imageSize">The pixel dimensions of the image being displayed.</param>
        /// <returns>The displayed image bounds, or an empty rectangle if either size has a zero dimension.</returns>
        public static RectangleF GetZoomedImageBounds(Size containerSize, Size imageSize)
        {
            if (containerSize.Width <= 0 || containerSize.Height <= 0 ||
                imageSize.Width <= 0 || imageSize.Height <= 0)
            {
                return RectangleF.Empty;
            }

            // Zoom mode scales by the tighter of the two axes so the whole image fits,
            // then centers the result along the slack axis.
            float scale = Math.Min(
                (float)containerSize.Width / imageSize.Width,
                (float)containerSize.Height / imageSize.Height);

            float width = imageSize.Width * scale;
            float height = imageSize.Height * scale;
            float left = (containerSize.Width - width) / 2f;
            float top = (containerSize.Height - height) / 2f;

            return new RectangleF(left, top, width, height);
        }
    }
}
