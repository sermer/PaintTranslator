using System;
using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// Strokes <see cref="GridGeometry"/> onto a GDI surface. Each line is drawn twice,
    /// a wider translucent dark stroke under a thin light one, so the grid stays
    /// visible over both light and dark image areas. The border is drawn as a single
    /// GDI rectangle rather than as four <see cref="GridGeometry.Segment"/> lines
    /// because a rectangle's mitered corners fill the outer pixels the under-pen's
    /// 3px width would otherwise notch at the four corners.
    /// </summary>
    public static class GridOverlayRenderer
    {
        public static void DrawGrid(Graphics graphics, RectangleF bounds, int columns, int rows)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            IReadOnlyList<GridGeometry.Segment> dividers = GridGeometry.Dividers(bounds, columns, rows);
            using (var underPen = new Pen(Color.FromArgb(150, 0, 0, 0), 3f))
            using (var overPen = new Pen(Color.White, 1f))
            {
                Stroke(graphics, bounds, dividers, underPen);
                Stroke(graphics, bounds, dividers, overPen);
            }
        }

        private static void Stroke(Graphics graphics, RectangleF bounds, IReadOnlyList<GridGeometry.Segment> dividers, Pen pen)
        {
            foreach (GridGeometry.Segment segment in dividers)
            {
                graphics.DrawLine(pen, segment.Start, segment.End);
            }

            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }
}
