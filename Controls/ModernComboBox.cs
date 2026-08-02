using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaintTranslator.Controls
{
    /// <summary>Owner-drawn drop-down list with dark selection and focus states.</summary>
    public class ModernComboBox : ComboBox
    {
        public ModernComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            IntegralHeight = false;
            UpdateItemHeight();
            BackColor = UiTheme.SurfaceRaised;
            ForeColor = UiTheme.Text;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdateItemHeight();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? UiTheme.Selection : UiTheme.SurfaceRaised;
            Color foreground = Enabled ? UiTheme.Text : UiTheme.TextMuted;
            using (var brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string text = GetItemText(Items[e.Index]);
            var textBounds = new Rectangle(
                e.Bounds.X + 9,
                e.Bounds.Y + 1,
                Math.Max(1, e.Bounds.Width - 18),
                Math.Max(1, e.Bounds.Height - 2));
            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                textBounds,
                foreground,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.NoPadding);

            if ((e.State & DrawItemState.Focus) != 0)
            {
                using var pen = new Pen(UiTheme.Accent);
                var focus = e.Bounds;
                focus.Width -= 1;
                focus.Height -= 1;
                e.Graphics.DrawRectangle(pen, focus);
            }
        }

        private void UpdateItemHeight()
        {
            ItemHeight = Math.Max(30, Font.Height + 10);
        }
    }
}
