using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PaintTranslator.Controls
{
    /// <summary>
    /// Keyboard-accessible, owner-drawn slider that remains legible on dark surfaces.
    /// </summary>
    public class ModernTrackBar : Control, ISupportInitialize
    {
        private int minimum;
        private int maximum = 100;
        private int value;
        private int tickFrequency = 10;

        public ModernTrackBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);
            TabStop = true;
            AccessibleRole = AccessibleRole.Slider;
            Height = 36;
            BackColor = UiTheme.Surface;
            ForeColor = UiTheme.Text;
        }

        public void BeginInit()
        {
        }

        public void EndInit()
        {
            Value = value;
            Invalidate();
        }

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get => minimum;
            set
            {
                if (minimum == value)
                {
                    return;
                }

                minimum = value;
                if (maximum < minimum)
                {
                    maximum = minimum;
                }

                Value = this.value;
                Invalidate();
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {
                if (maximum == value)
                {
                    return;
                }

                maximum = value;
                if (minimum > maximum)
                {
                    minimum = maximum;
                }

                Value = this.value;
                Invalidate();
            }
        }

        public int Value
        {
            get => value;
            set
            {
                int clamped = Math.Max(minimum, Math.Min(maximum, value));
                if (this.value == clamped)
                {
                    return;
                }

                this.value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int TickFrequency
        {
            get => tickFrequency;
            set
            {
                tickFrequency = Math.Max(1, value);
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                Focus();
                Capture = true;
                SetValueFromX(e.X);
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Capture && e.Button == MouseButtons.Left && Enabled)
            {
                SetValueFromX(e.X);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Capture = false;
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Enabled)
            {
                Value += Math.Sign(e.Delta);
            }

            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }

            base.OnMouseWheel(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int smallChange = 1;
            int largeChange = Math.Max(1, (maximum - minimum) / 10);
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    Value -= smallChange;
                    e.Handled = true;
                    break;
                case Keys.Right:
                case Keys.Up:
                    Value += smallChange;
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    Value -= largeChange;
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    Value += largeChange;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    Value = minimum;
                    e.Handled = true;
                    break;
                case Keys.End:
                    Value = maximum;
                    e.Handled = true;
                    break;
            }

            if (e.Handled)
            {
                e.SuppressKeyPress = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            const float sidePadding = 12f;
            float left = sidePadding;
            float right = Math.Max(left + 1f, ClientSize.Width - sidePadding);
            float centreY = ClientSize.Height * 0.5f;
            float fraction = maximum == minimum
                ? 0f
                : (value - minimum) / (float)(maximum - minimum);
            float thumbX = left + ((right - left) * fraction);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color empty = Enabled ? UiTheme.Border : Color.FromArgb(43, 48, 57);
            Color filled = Enabled ? UiTheme.Accent : UiTheme.TextMuted;
            using (var emptyPen = new Pen(empty, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var filledPen = new Pen(filled, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                e.Graphics.DrawLine(emptyPen, left, centreY, right, centreY);
                e.Graphics.DrawLine(filledPen, left, centreY, thumbX, centreY);
            }

            DrawTicks(e.Graphics, left, right, centreY + 7f);

            float radius = Focused ? 8f : 7f;
            var thumb = new RectangleF(thumbX - radius, centreY - radius, radius * 2f, radius * 2f);
            using (var brush = new SolidBrush(filled))
            {
                e.Graphics.FillEllipse(brush, thumb);
            }

            if (Focused && ShowFocusCues)
            {
                thumb.Inflate(2f, 2f);
                using var focusPen = new Pen(Color.FromArgb(150, UiTheme.Accent), 1f);
                e.Graphics.DrawEllipse(focusPen, thumb);
            }
        }

        private void DrawTicks(Graphics graphics, float left, float right, float y)
        {
            int range = maximum - minimum;
            if (range <= 0 || tickFrequency <= 0 || range / tickFrequency > 24)
            {
                return;
            }

            using var pen = new Pen(Color.FromArgb(100, UiTheme.TextMuted));
            for (int tick = minimum; tick <= maximum; tick += tickFrequency)
            {
                float fraction = (tick - minimum) / (float)range;
                float x = left + ((right - left) * fraction);
                graphics.DrawLine(pen, x, y, x, y + 2f);
            }
        }

        private void SetValueFromX(int x)
        {
            const float sidePadding = 12f;
            float usable = Math.Max(1f, ClientSize.Width - (sidePadding * 2f));
            double fraction = Math.Max(0.0, Math.Min(1.0, (x - sidePadding) / usable));
            Value = minimum + (int)Math.Round(fraction * (maximum - minimum));
        }
    }
}
