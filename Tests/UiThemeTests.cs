using System;
using System.Windows.Forms;
using PaintTranslator.Controls;
using Xunit;

namespace PaintTranslator.Tests
{
    public class UiThemeTests
    {
        [Fact]
        public void MainWindowUsesDarkSurfacesAndModernInputs()
        {
            using var form = new MainForm();
            form.PerformLayout();

            Assert.Equal(UiTheme.Window, form.BackColor);
            Assert.Equal(UiTheme.SurfaceRaised, Find(form, "toolbarPanel").BackColor);
            Control palette = Find(form, "palettePanel");
            Assert.Equal(UiTheme.Surface, palette.BackColor);
            Assert.Equal(300, palette.Width);
            Assert.Equal(UiTheme.Canvas, Find(form, "imageCanvas").BackColor);
            Assert.Empty(form.Controls.Find("convertPhotoButton", searchAllChildren: true));
            Assert.IsType<ModernTrackBar>(Find(form, "blurTrackBar"));
            Assert.IsType<ModernTrackBar>(Find(form, "markTrackBar"));
            var comboBox = Assert.IsType<ModernComboBox>(Find(form, "styleComboBox"));
            Assert.True(comboBox.ItemHeight >= comboBox.Font.Height + 10);

            Control stylePanel = Find(form, "stylePanel");
            int styleContentHeight = 0;
            foreach (Control control in stylePanel.Controls)
            {
                styleContentHeight += control.Height + control.Margin.Vertical;
            }

            Assert.True(stylePanel.Height <= styleContentHeight + 4);
            Assert.True(Find(form, "paintsCheckedListBox").Height >= 120);
        }

        [Fact]
        public void ModernTrackBarClampsValuesAndRaisesOneEventPerChange()
        {
            using var slider = new ModernTrackBar
            {
                Minimum = 10,
                Maximum = 20,
            };
            int changes = 0;
            slider.ValueChanged += (sender, args) => changes++;

            slider.Value = 15;
            slider.Value = 15;
            slider.Value = 100;

            Assert.Equal(20, slider.Value);
            Assert.Equal(2, changes);
        }

        private static Control Find(Control root, string name)
        {
            Control[] matches = root.Controls.Find(name, searchAllChildren: true);
            return Assert.Single(matches);
        }
    }
}
