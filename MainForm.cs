using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PaintTranslator.Data;
using PaintTranslator.Imaging;

namespace PaintTranslator
{
    /// <summary>
    /// Main application window. Displays a loaded image with a configurable grid
    /// overlay whose column and row counts are set by the user.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Tracks whether the picture box is currently showing a generated color
        /// wheel (as opposed to a loaded image), so paint selection changes know
        /// whether the wheel needs regenerating.
        /// </summary>
        private bool wheelDisplayed;

        /// <summary>
        /// Suppresses the paint check handlers while the select-all checkbox and the
        /// paint list synchronize each other, so a programmatic change on one side
        /// doesn't re-trigger the other in a loop.
        /// </summary>
        private bool suppressPaintCheckEvents;

        /// <summary>
        /// The most recently loaded photo, kept unmodified and separate from the
        /// displayed image so paint conversions always start from the original
        /// pixels, even after a previous conversion has replaced the display.
        /// </summary>
        private Bitmap sourcePhoto;

        /// <summary>
        /// The file name of the loaded photo, used to rebuild the window title
        /// after a conversion replaces the displayed image.
        /// </summary>
        private string sourcePhotoName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Item objects carry their swatch color, so they can't be expressed as
            // Designer literals; populate the list in code from the saved palette.
            PopulatePaintList(UserPaletteStore.Load());
        }

        /// <summary>
        /// Fills the paint list with the palette paints, all checked. Paints are
        /// taken from the full catalog so they keep catalog (color wheel) order.
        /// </summary>
        /// <param name="paletteNames">The names of the paints in the user's palette,
        /// or null to show the full catalog.</param>
        private void PopulatePaintList(ISet<string> paletteNames)
        {
            // Adding checked items fires ItemCheck per item; suppress the
            // select-all sync during the rebuild and set it once at the end.
            suppressPaintCheckEvents = true;
            try
            {
                paintsCheckedListBox.BeginUpdate();
                paintsCheckedListBox.Items.Clear();
                foreach (GoldenPaint paint in GoldenPalette.Paints)
                {
                    if (paletteNames == null || paletteNames.Contains(paint.Name))
                    {
                        paintsCheckedListBox.Items.Add(paint, true);
                    }
                }

                // A saved palette whose names no longer match any catalog paint
                // would leave the app with no paints; fall back to the catalog.
                if (paintsCheckedListBox.Items.Count == 0)
                {
                    foreach (GoldenPaint paint in GoldenPalette.Paints)
                    {
                        paintsCheckedListBox.Items.Add(paint, true);
                    }
                }

                paintsCheckedListBox.EndUpdate();
                selectAllCheckBox.Checked = true;
            }
            finally
            {
                suppressPaintCheckEvents = false;
            }
        }

        /// <summary>
        /// Opens the palette editor dialog and, if confirmed, saves the new palette
        /// to disk and rebuilds the paint list to show only the chosen paints.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void EditPaletteButton_Click(object sender, EventArgs e)
        {
            // The list's items are the current palette; hand their names to the
            // editor so it can pre-check them against the full catalog.
            var currentNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (object item in paintsCheckedListBox.Items)
            {
                if (item is GoldenPaint paint)
                {
                    currentNames.Add(paint.Name);
                }
            }

            using (var editor = new PaletteEditorForm(currentNames))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                List<string> chosen = editor.SelectedPaintNames;

                try
                {
                    UserPaletteStore.Save(chosen);
                }
                catch (Exception ex)
                {
                    // The palette still applies for this session even when the
                    // save fails; only future launches lose the selection.
                    MessageBox.Show(this, $"Could not save your palette, so it won't be remembered next time:\n{ex.Message}",
                        "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                PopulatePaintList(new HashSet<string>(chosen, StringComparer.Ordinal));

                // A displayed wheel reflects the old palette; regenerate it from
                // the rebuilt list.
                if (wheelDisplayed)
                {
                    SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaintColors(null)));
                }
            }
        }

        /// <summary>
        /// Opens a file dialog and loads the selected image into the picture box.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an image";
                dialog.Filter = "PNG images (*.png)|*.png|All images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    // Load through a memory copy so the file handle is released
                    // immediately instead of staying locked while displayed.
                    Bitmap loaded;
                    using (var source = Image.FromFile(dialog.FileName))
                    {
                        loaded = new Bitmap(source);
                    }

                    // Keep the original separate from the displayed copy: the
                    // display gets disposed on every image swap, while the
                    // original must survive as the source for conversions.
                    sourcePhoto?.Dispose();
                    sourcePhoto = loaded;
                    sourcePhotoName = System.IO.Path.GetFileName(dialog.FileName);

                    SetDisplayedImage(new Bitmap(sourcePhoto));
                    wheelDisplayed = false;
                    Text = $"Paint Translator - {sourcePhotoName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Could not load the image:\n{ex.Message}",
                        "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Generates a color wheel from the currently checked paints and displays it.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void GenerateWheelButton_Click(object sender, EventArgs e)
        {
            SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaintColors(null)));
            wheelDisplayed = true;
            Text = "Paint Translator - Color Wheel (generated)";
        }

        /// <summary>
        /// Converts the loaded photo to use only colors mixable from the checked
        /// paints and displays the result. Runs the conversion off the UI thread,
        /// with the image and paint controls disabled until it finishes.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void ConvertPhotoButton_Click(object sender, EventArgs e)
        {
            if (sourcePhoto == null)
            {
                MessageBox.Show(this, "Load a photo first, then convert it.",
                    "No photo loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<Color> selected = GetSelectedPaintColors(null);
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one paint to convert with.",
                    "No paints selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Block image swaps while the background task reads sourcePhoto;
            // loading a new photo mid-conversion would dispose it out from under
            // the worker.
            loadImageButton.Enabled = false;
            generateWheelButton.Enabled = false;
            convertPhotoButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                // Read the option on the UI thread; the conversion itself runs
                // on a worker where touching controls isn't allowed.
                bool dither = ditherCheckBox.Checked;

                Bitmap converted = await Task.Run(() => PalettePhotoConverter.Convert(sourcePhoto, selected, dither));

                SetDisplayedImage(converted);
                wheelDisplayed = false;
                Text = $"Paint Translator - {sourcePhotoName} (converted to paints)";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not convert the photo:\n{ex.Message}",
                    "Conversion failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                loadImageButton.Enabled = true;
                generateWheelButton.Enabled = true;
                convertPhotoButton.Enabled = true;
                UseWaitCursor = false;
            }
        }

        /// <summary>
        /// Regenerates the displayed color wheel when a paint is checked or unchecked,
        /// so deselected paints disappear from the wheel and reselected ones return.
        /// </summary>
        /// <param name="sender">The checked list box whose item changed.</param>
        /// <param name="e">The event arguments describing the pending check change.</param>
        private void PaintsCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // A select-all bulk update fires this once per item; the checkbox
            // handler regenerates the wheel once at the end instead.
            if (suppressPaintCheckEvents)
            {
                return;
            }

            List<Color> selected = GetSelectedPaintColors(e);

            // Mirror the list state onto the select-all checkbox without letting
            // its CheckedChanged handler fan back out over every item.
            suppressPaintCheckEvents = true;
            try
            {
                selectAllCheckBox.Checked = selected.Count == paintsCheckedListBox.Items.Count;
            }
            finally
            {
                suppressPaintCheckEvents = false;
            }

            // Only refresh when the wheel is showing; a loaded photo is unaffected
            // by paint selection, and the next generated wheel reads the list anyway.
            if (!wheelDisplayed)
            {
                return;
            }

            SetDisplayedImage(ColorWheelGenerator.Create(512, selected));
        }

        /// <summary>
        /// Checks or unchecks every paint in the list when the select-all checkbox
        /// is toggled, then regenerates the displayed color wheel once.
        /// </summary>
        /// <param name="sender">The select-all checkbox.</param>
        /// <param name="e">The event arguments.</param>
        private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Programmatic syncs from ItemCheck must not fan out over the list.
            if (suppressPaintCheckEvents)
            {
                return;
            }

            suppressPaintCheckEvents = true;
            try
            {
                for (int i = 0; i < paintsCheckedListBox.Items.Count; i++)
                {
                    paintsCheckedListBox.SetItemChecked(i, selectAllCheckBox.Checked);
                }
            }
            finally
            {
                suppressPaintCheckEvents = false;
            }

            // One regeneration for the whole bulk change; the item states are
            // already committed here, so no pending change needs merging in.
            if (wheelDisplayed)
            {
                SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaintColors(null)));
            }
        }

        /// <summary>
        /// Collects the colors of all checked paints, in palette order.
        /// </summary>
        /// <param name="pendingChange">A check change that has not been applied yet
        /// (ItemCheck fires before the state updates), or null to read the current
        /// states as-is.</param>
        /// <returns>The mass-tone colors of the checked paints.</returns>
        private List<Color> GetSelectedPaintColors(ItemCheckEventArgs pendingChange)
        {
            var colors = new List<Color>(paintsCheckedListBox.Items.Count);

            for (int i = 0; i < paintsCheckedListBox.Items.Count; i++)
            {
                // Substitute the pending state for the item being toggled, since
                // GetItemChecked still reports its old value during ItemCheck.
                bool isChecked = pendingChange != null && pendingChange.Index == i
                    ? pendingChange.NewValue == CheckState.Checked
                    : paintsCheckedListBox.GetItemChecked(i);

                if (isChecked && paintsCheckedListBox.Items[i] is GoldenPaint paint)
                {
                    colors.Add(paint.Color);
                }
            }

            return colors;
        }

        /// <summary>
        /// Redraws the grid overlay when any grid setting (columns, rows, visibility) changes.
        /// </summary>
        /// <param name="sender">The control whose value changed.</param>
        /// <param name="e">The event arguments.</param>
        private void GridSettingsChanged(object sender, EventArgs e)
        {
            imagePictureBox.Invalidate();
        }

        /// <summary>
        /// Paints the grid overlay on top of the displayed image.
        /// </summary>
        /// <param name="sender">The picture box being painted.</param>
        /// <param name="e">The paint event arguments providing the graphics surface.</param>
        private void ImagePictureBox_Paint(object sender, PaintEventArgs e)
        {
            // The base PictureBox paint has already drawn the image; nothing to
            // overlay when no image is loaded or the grid is toggled off.
            if (imagePictureBox.Image == null || !showGridCheckBox.Checked)
            {
                return;
            }

            // The grid must cover the image itself, not the whole control, so compute
            // where Zoom mode actually placed the image within the client area.
            RectangleF imageBounds = GridOverlayRenderer.GetZoomedImageBounds(
                imagePictureBox.ClientSize, imagePictureBox.Image.Size);
            if (imageBounds.IsEmpty)
            {
                return;
            }

            GridOverlayRenderer.DrawGrid(
                e.Graphics,
                imageBounds,
                (int)columnsNumericUpDown.Value,
                (int)rowsNumericUpDown.Value);
        }

        /// <summary>
        /// Repaints the overlay when the picture box is resized, since the displayed
        /// image bounds change with the control size.
        /// </summary>
        /// <param name="sender">The picture box that was resized.</param>
        /// <param name="e">The event arguments.</param>
        private void ImagePictureBox_Resize(object sender, EventArgs e)
        {
            imagePictureBox.Invalidate();
        }

        /// <summary>
        /// Replaces the currently displayed image, disposing the previous one to
        /// avoid leaking GDI handles.
        /// </summary>
        /// <param name="image">The new image to display.</param>
        private void SetDisplayedImage(Image image)
        {
            Image previous = imagePictureBox.Image;
            imagePictureBox.Image = image;
            previous?.Dispose();
        }
    }
}
