using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PaintTranslator.Pigments;

namespace PaintTranslator.Controls
{
    /// <summary>
    /// Dark owner-drawn paint checklist with modern checks and measured colour chips.
    /// </summary>
    public class PaintCheckedListBox : CheckedListBox
    {
        private const int CheckSize = 16;
        private const int CheckMargin = 8;
        private const int SwatchWidth = 34;
        private const int SwatchMargin = 5;

        private readonly Dictionary<PigmentCoefficients, Color> swatchColors =
            new Dictionary<PigmentCoefficients, Color>();

        public PaintCheckedListBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 30;
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = UiTheme.Surface;
            ForeColor = UiTheme.Text;
        }

        /// <summary>Draws the row, checkbox, label, and measured mass-tone swatch.</summary>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? UiTheme.Selection : BackColor;
            using (var backgroundBrush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            int checkTop = e.Bounds.Top + ((e.Bounds.Height - CheckSize) / 2);
            var checkBounds = new Rectangle(e.Bounds.Left + CheckMargin, checkTop, CheckSize, CheckSize);
            using (var checkBackground = new SolidBrush(UiTheme.SurfaceRaised))
            using (var checkBorder = new Pen(UiTheme.Border))
            {
                e.Graphics.FillRectangle(checkBackground, checkBounds);
                e.Graphics.DrawRectangle(checkBorder, checkBounds);
            }

            if (GetItemChecked(e.Index))
            {
                using (var checkedBrush = new SolidBrush(UiTheme.Accent))
                {
                    e.Graphics.FillRectangle(checkedBrush, checkBounds);
                }

                using var checkPen = new Pen(UiTheme.Window, 2f);
                checkPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                checkPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                e.Graphics.DrawLines(checkPen, new[]
                {
                    new Point(checkBounds.Left + 3, checkBounds.Top + 8),
                    new Point(checkBounds.Left + 7, checkBounds.Bottom - 4),
                    new Point(checkBounds.Right - 3, checkBounds.Top + 4),
                });
            }

            int swatchHeight = Math.Max(8, e.Bounds.Height - (SwatchMargin * 2));
            var swatch = new Rectangle(
                e.Bounds.Right - SwatchWidth - SwatchMargin,
                e.Bounds.Top + SwatchMargin,
                SwatchWidth,
                swatchHeight);

            if (Items[e.Index] is PigmentCoefficients paint)
            {
                Color colour = GetSwatchColor(paint);
                using (var swatchBrush = new SolidBrush(colour))
                {
                    e.Graphics.FillRectangle(swatchBrush, swatch);
                }

                using var swatchBorder = new Pen(UiTheme.Border);
                e.Graphics.DrawRectangle(swatchBorder, swatch);
            }

            int textLeft = checkBounds.Right + 9;
            var textBounds = new Rectangle(
                textLeft,
                e.Bounds.Top,
                Math.Max(1, swatch.Left - textLeft - 7),
                e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                GetItemText(Items[e.Index]),
                Font,
                textBounds,
                Enabled ? UiTheme.Text : UiTheme.TextMuted,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            if ((e.State & DrawItemState.Focus) != 0)
            {
                var focus = e.Bounds;
                focus.Width -= 1;
                focus.Height -= 1;
                using var focusPen = new Pen(UiTheme.Accent);
                e.Graphics.DrawRectangle(focusPen, focus);
            }
        }

        private Color GetSwatchColor(PigmentCoefficients paint)
        {
            if (swatchColors.TryGetValue(paint, out Color colour))
            {
                return colour;
            }

            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(new[] { paint }, new[] { 1.0 }, reflectance);
            colour = SpectralRenderer.ToDisplayColor(reflectance, out _);
            swatchColors.Add(paint, colour);
            return colour;
        }
    }
}
