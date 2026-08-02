using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PaintTranslator.Controls;

namespace PaintTranslator
{
    /// <summary>
    /// Shared visual language for the application's production windows.
    /// </summary>
    internal static class UiTheme
    {
        public static readonly Color Window = Color.FromArgb(15, 18, 23);
        public static readonly Color Canvas = Color.FromArgb(10, 12, 16);
        public static readonly Color Surface = Color.FromArgb(23, 28, 36);
        public static readonly Color SurfaceRaised = Color.FromArgb(31, 37, 47);
        public static readonly Color SurfaceHover = Color.FromArgb(42, 50, 63);
        public static readonly Color Border = Color.FromArgb(57, 66, 81);
        public static readonly Color Text = Color.FromArgb(235, 238, 244);
        public static readonly Color TextMuted = Color.FromArgb(158, 169, 185);
        public static readonly Color Accent = Color.FromArgb(214, 166, 78);
        public static readonly Color AccentHover = Color.FromArgb(231, 185, 98);
        public static readonly Color AccentPressed = Color.FromArgb(184, 137, 58);
        public static readonly Color Selection = Color.FromArgb(55, 70, 91);

        public static readonly Font DefaultFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font EmptyStateTitleFont = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);

        /// <summary>Applies the theme recursively to a form or control subtree.</summary>
        public static void Apply(Control root)
        {
            if (root == null)
            {
                return;
            }

            root.ForeColor = Text;

            switch (root)
            {
                case Form form:
                    form.BackColor = Window;
                    form.Font = DefaultFont;
                    EnableDarkTitleBar(form);
                    break;
                case ImageCanvas:
                    root.BackColor = Canvas;
                    break;
                case PaintCheckedListBox:
                    root.BackColor = Surface;
                    break;
                case ModernTrackBar:
                    root.BackColor = Surface;
                    break;
                case ModernComboBox:
                    root.BackColor = SurfaceRaised;
                    break;
                case FlowLayoutPanel:
                    root.BackColor = Surface;
                    break;
                case Panel:
                    root.BackColor = Surface;
                    break;
                case Button button:
                    StyleButton(button);
                    break;
                case CheckBox checkBox when checkBox.Appearance == Appearance.Button:
                    StyleToggle(checkBox);
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = root.Parent?.BackColor ?? Surface;
                    checkBox.ForeColor = Text;
                    checkBox.FlatStyle = FlatStyle.Flat;
                    checkBox.UseVisualStyleBackColor = false;
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = SurfaceRaised;
                    numeric.ForeColor = Text;
                    numeric.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case Label label:
                    label.BackColor = root.Parent?.BackColor ?? Surface;
                    label.ForeColor = label.Font.Bold ? Accent : TextMuted;
                    break;
            }

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }
        }

        public static void StyleButton(Button button, bool primary = false)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.ForeColor = primary ? Window : Text;
            button.BackColor = primary ? Accent : SurfaceRaised;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? Accent : Border;
            button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = primary ? AccentPressed : Selection;
        }

        public static void StylePrimaryButton(Button button)
        {
            StyleButton(button, primary: true);
        }

        public static void StyleToggle(CheckBox checkBox)
        {
            checkBox.UseVisualStyleBackColor = false;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.Cursor = Cursors.Hand;
            checkBox.ForeColor = Text;
            checkBox.BackColor = SurfaceRaised;
            checkBox.FlatAppearance.BorderSize = 1;
            checkBox.FlatAppearance.BorderColor = Border;
            checkBox.FlatAppearance.MouseOverBackColor = SurfaceHover;
            checkBox.FlatAppearance.MouseDownBackColor = Selection;
            checkBox.FlatAppearance.CheckedBackColor = AccentPressed;
        }

        public static void StyleMenu(ContextMenuStrip menu)
        {
            menu.BackColor = SurfaceRaised;
            menu.ForeColor = Text;
            menu.ShowImageMargin = false;
            menu.Padding = new Padding(4);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            foreach (ToolStripItem item in menu.Items)
            {
                item.BackColor = SurfaceRaised;
                item.ForeColor = Text;
                item.Padding = new Padding(8, 5, 12, 5);
            }
        }

        private static void EnableDarkTitleBar(Form form)
        {
            void ApplyTitleBar(object sender, EventArgs e)
            {
                try
                {
                    int enabled = 1;
                    if (DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
                    {
                        DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
                    }
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
            }

            form.HandleCreated += ApplyTitleBar;
            if (form.IsHandleCreated)
            {
                ApplyTitleBar(form, EventArgs.Empty);
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr window, int attribute, ref int value, int valueSize);

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => SurfaceRaised;
            public override Color ImageMarginGradientBegin => SurfaceRaised;
            public override Color ImageMarginGradientMiddle => SurfaceRaised;
            public override Color ImageMarginGradientEnd => SurfaceRaised;
            public override Color MenuItemSelected => SurfaceHover;
            public override Color MenuItemBorder => Accent;
            public override Color MenuBorder => Border;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Border;
        }
    }
}
