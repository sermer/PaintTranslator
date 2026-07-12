using System.Drawing;
using System.Windows.Forms;
using PaintTranslator.Data;

namespace PaintTranslator.Controls
{
    /// <summary>
    /// A checked list box that draws a color swatch on the right edge of each row
    /// when the item is a <see cref="GoldenPaint"/>, so the paint's actual color
    /// is visible next to its name.
    /// </summary>
    public class PaintCheckedListBox : CheckedListBox
    {
        // Fixed swatch footprint keeps the color chips aligned in a column
        // regardless of how long each paint name is.
        private const int SwatchWidth = 32;
        private const int SwatchMargin = 3;

        /// <summary>
        /// Draws the standard check box and item text, then overlays the paint's
        /// color swatch at the right edge of the row.
        /// </summary>
        /// <param name="e">The draw event arguments describing the item and its bounds.</param>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // Base draws the check box, selection highlight, and paint name.
            base.OnDrawItem(e);

            // Design-time and empty-list paints pass an index of -1; nothing to swatch.
            if (e.Index < 0 || e.Index >= Items.Count || !(Items[e.Index] is GoldenPaint paint))
            {
                return;
            }

            var swatch = new Rectangle(
                e.Bounds.Right - SwatchWidth - SwatchMargin,
                e.Bounds.Top + SwatchMargin,
                SwatchWidth,
                e.Bounds.Height - SwatchMargin * 2);

            using (var brush = new SolidBrush(paint.Color))
            {
                e.Graphics.FillRectangle(brush, swatch);
            }

            // Outline so near-white swatches (e.g. Titanium White) stay visible
            // against the list background.
            e.Graphics.DrawRectangle(Pens.Gray, swatch);
        }
    }
}
