using System;
using System.Collections.Generic;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Where the grid overlay's lines go, with no drawing. The WinForms app strokes
    /// these with GDI pens and the web canvas will stroke the same list, so the two
    /// surfaces cannot disagree about where a division falls.
    /// </summary>
    public static class GridGeometry
    {
        public readonly record struct Segment(PointF Start, PointF End);

        /// <summary>
        /// Interior dividers first, then the four border edges. Positions are computed
        /// as fractions of the full span rather than by accumulating a step, so
        /// rounding cannot drift across many segments.
        /// </summary>
        public static IReadOnlyList<Segment> Segments(RectangleF bounds, int columns, int rows)
        {
            var segments = new List<Segment>(Dividers(bounds, columns, rows));
            segments.Add(new Segment(new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Top)));
            segments.Add(new Segment(new PointF(bounds.Left, bounds.Bottom), new PointF(bounds.Right, bounds.Bottom)));
            segments.Add(new Segment(new PointF(bounds.Left, bounds.Top), new PointF(bounds.Left, bounds.Bottom)));
            segments.Add(new Segment(new PointF(bounds.Right, bounds.Top), new PointF(bounds.Right, bounds.Bottom)));
            return segments;
        }

        /// <summary>
        /// Interior divider lines only, excluding the border. A GDI surface renders a
        /// rectangle border as a single mitered path, so the app strokes dividers as
        /// individual lines but draws the border as a rectangle instead of four
        /// separate lines, matching the old renderer's corner pixels exactly; the web
        /// canvas will stroke <see cref="Segments"/> in full instead.
        /// </summary>
        public static IReadOnlyList<Segment> Dividers(RectangleF bounds, int columns, int rows)
        {
            if (columns < 1 || rows < 1)
            {
                throw new ArgumentOutOfRangeException(columns < 1 ? nameof(columns) : nameof(rows),
                    "Grid must have at least one segment in each direction.");
            }

            var segments = new List<Segment>((columns - 1) + (rows - 1));
            for (int i = 1; i < columns; i++)
            {
                float x = bounds.Left + bounds.Width * i / columns;
                segments.Add(new Segment(new PointF(x, bounds.Top), new PointF(x, bounds.Bottom)));
            }

            for (int i = 1; i < rows; i++)
            {
                float y = bounds.Top + bounds.Height * i / rows;
                segments.Add(new Segment(new PointF(bounds.Left, y), new PointF(bounds.Right, y)));
            }

            return segments;
        }
    }
}
