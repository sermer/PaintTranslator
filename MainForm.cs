using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using PaintTranslator.Data;
using PaintTranslator.Pigments;
using PaintTranslator.Imaging;
using PaintTranslator.Input;

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
        /// Space between the hover tooltip's border and its text, in pixels.
        /// </summary>
        private const int TooltipPadding = 6;

        /// <summary>
        /// Matches hovered photo pixels to their closest achievable paint mixture.
        /// Built lazily from the checked paints on first hover and reset to null
        /// whenever the selection changes, so it always reflects the current paints.
        /// </summary>
        private PaintBlendMatcher blendMatcher;

        /// <summary>
        /// The text lines of the hover tooltip (pixel RGB plus blend percentages),
        /// or null while no tooltip is showing.
        /// </summary>
        private string[] blendTooltipLines;

        /// <summary>
        /// The cursor position the tooltip is anchored to, in canvas client
        /// coordinates.
        /// </summary>
        private Point blendTooltipAnchor;

        /// <summary>
        /// The box the tooltip last painted into, kept so mouse movement can
        /// invalidate just the old and new tooltip areas instead of the whole image.
        /// </summary>
        private Rectangle blendTooltipDrawnBounds;

        /// <summary>
        /// The last cursor position seen over the canvas. Zooming under a stationary
        /// cursor changes which pixel is being read, and there is no mouse move to
        /// recompute the tooltip from.
        /// </summary>
        private Point lastCanvasCursor;

        /// <summary>
        /// Set while a load or conversion is running. Both read or replace
        /// <see cref="sourcePhoto"/>, so a second one starting mid-flight would dispose
        /// the bitmap the first is still working from.
        /// </summary>
        private bool imageOperationInProgress;

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
            // The list contents define which mixtures are achievable, so any
            // cached hover matcher no longer applies.
            blendMatcher = null;

            // Adding checked items fires ItemCheck per item; suppress the
            // select-all sync during the rebuild and set it once at the end.
            suppressPaintCheckEvents = true;
            try
            {
                paintsCheckedListBox.BeginUpdate();
                paintsCheckedListBox.Items.Clear();
                foreach (PigmentCoefficients paint in PigmentLibrary.Selectable)
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
                    foreach (PigmentCoefficients paint in PigmentLibrary.Selectable)
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
                if (item is PigmentCoefficients paint)
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
                    SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaints(null)));
                }
            }
        }

        /// <summary>
        /// Turns click-to-zoom on the image on and off.
        /// </summary>
        /// <param name="sender">The magnifier toggle.</param>
        /// <param name="e">The event arguments.</param>
        private void MagnifierCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            imageCanvas.MagnifierActive = magnifierCheckBox.Checked;
        }

        /// <summary>
        /// Opens a file dialog and loads the selected image into the picture box.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void LoadImageButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an image";
                dialog.Filter = BuildImageFilter();

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                // Capture the path before the dialog is disposed, since the decode runs
                // on a worker that outlives this block.
                string path = dialog.FileName;

                await LoadImageAsync(() => Task.Run(() => new LoadedImage(
                    ImageDecoder.DecodeFile(path), Path.GetFileName(path))));
            }
        }

        /// <summary>
        /// Loads an image from the clipboard when the user presses Ctrl+V.
        /// </summary>
        /// <param name="msg">The window message carrying the key press.</param>
        /// <param name="keyData">The key and modifiers that were pressed.</param>
        /// <returns>True when the key press was handled here; otherwise the result of the
        /// base implementation.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handled as a command key rather than a key press so it works no matter which
            // control has focus, including the paint list.
            if (keyData == (Keys.Control | Keys.V))
            {
                _ = PasteImageAsync();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Loads whatever image the clipboard currently holds.
        /// </summary>
        /// <returns>A task that completes once the paste has finished or failed.</returns>
        private async Task PasteImageAsync()
        {
            IDataObject data;
            try
            {
                data = Clipboard.GetDataObject();
            }
            catch (Exception)
            {
                // Another application can hold the clipboard open, which fails the read;
                // there is nothing useful to report for a keystroke the user may have
                // pressed out of habit.
                return;
            }

            if (data == null || !ImageDataObjectReader.ContainsImage(data))
            {
                MessageBox.Show(this, "The clipboard doesn't contain an image.",
                    "Nothing to paste", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await LoadImageAsync(() => ImageDataObjectReader.ReadAsync(data));
        }

        /// <summary>
        /// Signals whether a dragged payload can be dropped, which is what makes the
        /// cursor show a copy indicator instead of a rejection.
        /// </summary>
        /// <param name="sender">The control the payload was dragged over.</param>
        /// <param name="e">The event arguments carrying the payload.</param>
        private void ImageDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = !imageOperationInProgress && ImageDataObjectReader.ContainsImage(e.Data)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        /// <summary>
        /// Loads an image dropped onto the window, whether it came from a file, another
        /// application, or a web page.
        /// </summary>
        /// <param name="sender">The control the payload was dropped on.</param>
        /// <param name="e">The event arguments carrying the payload.</param>
        private async void ImageDragDrop(object sender, DragEventArgs e)
        {
            // The event arguments are reused once this handler returns, so hold the
            // payload itself across the await rather than reaching through them later.
            IDataObject data = e.Data;

            await LoadImageAsync(() => ImageDataObjectReader.ReadAsync(data));
        }

        /// <summary>
        /// Runs an image load: blocks competing operations, reports any failure, and
        /// adopts the result. Every way into the application funnels through here so the
        /// busy state and error handling stay identical across all of them.
        /// </summary>
        /// <param name="load">Produces the image to adopt, or null when the source turned
        /// out to hold nothing usable.</param>
        /// <returns>A task that completes once the load has finished or failed.</returns>
        private async Task LoadImageAsync(Func<Task<LoadedImage>> load)
        {
            if (imageOperationInProgress)
            {
                return;
            }

            SetImageOperationInProgress(true);
            try
            {
                LoadedImage loaded = await load();

                if (loaded == null)
                {
                    MessageBox.Show(this, "That didn't contain an image this app can read.",
                        "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                AdoptSourcePhoto(loaded.Image, loaded.Name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not load the image:\n{ex.Message}",
                    "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetImageOperationInProgress(false);
            }
        }

        /// <summary>
        /// Takes ownership of a freshly loaded photo and displays it.
        /// </summary>
        /// <param name="photo">The loaded image. The form disposes it when the next photo
        /// arrives.</param>
        /// <param name="name">The name to show in the window title.</param>
        private void AdoptSourcePhoto(Bitmap photo, string name)
        {
            // Keep the original separate from the displayed copy: the display gets
            // disposed on every image swap, while the original must survive as the
            // source for conversions.
            sourcePhoto?.Dispose();
            sourcePhoto = photo;
            sourcePhotoName = name;

            SetDisplayedImage(new Bitmap(sourcePhoto));
            wheelDisplayed = false;
            Text = $"Paint Translator - {sourcePhotoName}";
        }

        /// <summary>
        /// Enables or disables the controls that would disturb an image operation while
        /// one is running, and shows the wait cursor.
        /// </summary>
        /// <param name="inProgress">True when an operation is starting; false when it has
        /// finished.</param>
        private void SetImageOperationInProgress(bool inProgress)
        {
            imageOperationInProgress = inProgress;
            loadImageButton.Enabled = !inProgress;
            generateWheelButton.Enabled = !inProgress;
            convertPhotoButton.Enabled = !inProgress;
            UseWaitCursor = inProgress;
        }

        /// <summary>
        /// Builds the file dialog filter from the formats the decoder supports, so the
        /// dialog and the decoder cannot drift apart.
        /// </summary>
        /// <returns>A filter string listing all supported images, then all files.</returns>
        private static string BuildImageFilter()
        {
            var patterns = new List<string>(ImageDecoder.SupportedExtensions.Length);
            foreach (string extension in ImageDecoder.SupportedExtensions)
            {
                patterns.Add("*" + extension);
            }

            string joined = string.Join(";", patterns);
            return $"All supported images ({joined})|{joined}|All files (*.*)|*.*";
        }

        /// <summary>
        /// Generates a color wheel from the currently checked paints and displays it.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void GenerateWheelButton_Click(object sender, EventArgs e)
        {
            SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaints(null)));
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

            List<PigmentCoefficients> selected = GetSelectedPaints(null);
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one paint to convert with.",
                    "No paints selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Block image swaps while the background task reads sourcePhoto;
            // loading a new photo mid-conversion would dispose it out from under
            // the worker.
            SetImageOperationInProgress(true);
            try
            {
                // Read the option on the UI thread; the conversion itself runs
                // on a worker where touching controls isn't allowed.
                int blurRadius = blurTrackBar.Value;

                Bitmap converted = await Task.Run(() => PalettePhotoConverter.Convert(sourcePhoto, selected, blurRadius));

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
                SetImageOperationInProgress(false);
            }
        }

        /// <summary>
        /// Updates the blur label to read back the slider's current radius, since a
        /// bare slider gives no indication of the value it is sitting on.
        /// </summary>
        /// <param name="sender">The slider that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void BlurTrackBar_ValueChanged(object sender, EventArgs e)
        {
            int radius = blurTrackBar.Value;

            // "0 px" would read as a setting rather than as the blur being absent,
            // which is what a zero radius actually means.
            blurLabel.Text = radius == 0 ? "Blur: off" : $"Blur: {radius} px";
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

            // The check change alters which paints can mix, so the hover matcher
            // must be rebuilt from the new selection on next use.
            blendMatcher = null;

            List<PigmentCoefficients> selected = GetSelectedPaints(e);

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

            // Bulk check changes alter which paints can mix, so the hover matcher
            // must be rebuilt from the new selection on next use.
            blendMatcher = null;

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
                SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaints(null)));
            }
        }

        /// <summary>
        /// Collects all checked paints, in palette order.
        /// </summary>
        /// <param name="pendingChange">A check change that has not been applied yet
        /// (ItemCheck fires before the state updates), or null to read the current
        /// states as-is.</param>
        /// <returns>The checked paints.</returns>
        private List<PigmentCoefficients> GetSelectedPaints(ItemCheckEventArgs pendingChange)
        {
            var paints = new List<PigmentCoefficients>(paintsCheckedListBox.Items.Count);

            for (int i = 0; i < paintsCheckedListBox.Items.Count; i++)
            {
                // Substitute the pending state for the item being toggled, since
                // GetItemChecked still reports its old value during ItemCheck.
                bool isChecked = pendingChange != null && pendingChange.Index == i
                    ? pendingChange.NewValue == CheckState.Checked
                    : paintsCheckedListBox.GetItemChecked(i);

                if (isChecked && paintsCheckedListBox.Items[i] is PigmentCoefficients paint)
                {
                    paints.Add(paint);
                }
            }

            return paints;
        }

        /// <summary>
        /// Redraws the grid overlay when any grid setting (columns, rows, visibility) changes.
        /// </summary>
        /// <param name="sender">The control whose value changed.</param>
        /// <param name="e">The event arguments.</param>
        private void GridSettingsChanged(object sender, EventArgs e)
        {
            imageCanvas.Invalidate();
        }

        /// <summary>
        /// Paints the grid overlay and the blend tooltip on top of the displayed image.
        /// </summary>
        /// <param name="sender">The canvas being painted.</param>
        /// <param name="e">The paint event arguments providing the graphics surface.</param>
        private void ImageCanvas_Paint(object sender, PaintEventArgs e)
        {
            // The canvas has already drawn the image in its own OnPaint; with nothing loaded
            // there is no overlay to draw, so the empty area advertises how to load one.
            if (imageCanvas.Image == null)
            {
                DrawEmptyCanvasHint(e.Graphics);
                return;
            }

            if (showGridCheckBox.Checked)
            {
                // The grid must cover the image itself, not the whole control, and it
                // follows the image as that is zoomed and panned.
                RectangleF imageBounds = imageCanvas.Viewport.GetImageBounds();
                if (!imageBounds.IsEmpty)
                {
                    GridOverlayRenderer.DrawGrid(
                        e.Graphics,
                        imageBounds,
                        (int)columnsNumericUpDown.Value,
                        (int)rowsNumericUpDown.Value);
                }
            }

            // Drawn last so the tooltip sits above the grid lines.
            DrawBlendTooltip(e.Graphics);
        }

        /// <summary>
        /// Draws the prompt shown on the empty canvas, so the drop and paste gestures are
        /// discoverable rather than having to be guessed at.
        /// </summary>
        /// <param name="graphics">The graphics surface to draw on.</param>
        private void DrawEmptyCanvasHint(Graphics graphics)
        {
            const string Hint = "Drop an image here, paste one with Ctrl+V, or use Load Image...";

            // Dimmed rather than full white: the prompt should read as a placeholder and
            // not compete with an image once one is loaded over it.
            using (var brush = new SolidBrush(Color.FromArgb(150, 235, 235, 235)))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            })
            {
                graphics.DrawString(Hint, Font, brush, imageCanvas.ClientRectangle, format);
            }
        }

        /// <summary>
        /// Updates the blend tooltip as the mouse moves over the canvas, so it
        /// tracks the cursor and describes the pixel underneath it.
        /// </summary>
        /// <param name="sender">The canvas the mouse moved over.</param>
        /// <param name="e">The event arguments carrying the cursor position.</param>
        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            lastCanvasCursor = e.Location;

            // A pan drag moves the image out from under the tooltip on every mouse move;
            // reading a pixel per frame during a drag is both wrong and wasteful.
            if (imageCanvas.IsPanning)
            {
                HideBlendTooltip();
                return;
            }

            UpdateBlendTooltip(e.Location);
        }

        /// <summary>
        /// Hides the blend tooltip when the mouse leaves the canvas.
        /// </summary>
        /// <param name="sender">The canvas the mouse left.</param>
        /// <param name="e">The event arguments.</param>
        private void ImageCanvas_MouseLeave(object sender, EventArgs e)
        {
            HideBlendTooltip();
        }

        /// <summary>
        /// Recomputes the blend tooltip after a zoom or pan, since a different pixel is
        /// now under a cursor that never moved.
        /// </summary>
        /// <param name="sender">The canvas whose view changed.</param>
        /// <param name="e">The event arguments.</param>
        private void ImageCanvas_ViewChanged(object sender, EventArgs e)
        {
            if (imageCanvas.IsPanning)
            {
                return;
            }

            UpdateBlendTooltip(lastCanvasCursor);
        }

        /// <summary>
        /// Recomputes the blend tooltip for a cursor position: resolves which image
        /// pixel sits under the cursor, derives that pixel's paint blend (exact wheel
        /// weights for a generated wheel, the closest achievable mixture for a photo),
        /// and moves the tooltip beside the cursor.
        /// </summary>
        /// <param name="cursor">The cursor position in canvas client coordinates.</param>
        private void UpdateBlendTooltip(Point cursor)
        {
            // Every displayed image is created as a Bitmap; anything else (or no
            // image at all) has no pixels to sample.
            if (!(imageCanvas.Image is Bitmap bitmap))
            {
                HideBlendTooltip();
                return;
            }

            // Only the image area itself carries pixels; the space around it shows the
            // control's background.
            if (!imageCanvas.Viewport.TryGetImagePixel(cursor, out Point pixelPoint))
            {
                HideBlendTooltip();
                return;
            }

            Color pixel = bitmap.GetPixel(pixelPoint.X, pixelPoint.Y);

            // Fully transparent pixels are the empty surround of the color wheel;
            // there is no paint there to describe.
            if (pixel.A == 0)
            {
                HideBlendTooltip();
                return;
            }

            string[] lines = wheelDisplayed
                ? BuildWheelBlendLines(pixel, pixelPoint.X, pixelPoint.Y, bitmap.Width)
                : BuildClosestMixLines(pixel);
            if (lines == null)
            {
                HideBlendTooltip();
                return;
            }

            // Repaint only where the tooltip was and where it lands, so tracking
            // the mouse doesn't redraw the whole scaled image on every move.
            Rectangle previous = blendTooltipDrawnBounds;
            blendTooltipLines = lines;
            blendTooltipAnchor = cursor;
            blendTooltipDrawnBounds = GetBlendTooltipBounds();
            imageCanvas.Invalidate(previous.IsEmpty
                ? blendTooltipDrawnBounds
                : Rectangle.Union(previous, blendTooltipDrawnBounds));
        }

        /// <summary>
        /// Hides the blend tooltip and repaints the area it occupied.
        /// </summary>
        private void HideBlendTooltip()
        {
            if (blendTooltipLines == null)
            {
                return;
            }

            blendTooltipLines = null;
            Rectangle previous = blendTooltipDrawnBounds;
            blendTooltipDrawnBounds = Rectangle.Empty;
            imageCanvas.Invalidate(previous);
        }

        /// <summary>
        /// Builds the tooltip lines for a pixel of the generated color wheel, whose
        /// blend is known exactly from the wheel's geometry.
        /// </summary>
        /// <param name="pixel">The color of the hovered pixel.</param>
        /// <param name="pixelX">The pixel's horizontal position in the wheel bitmap.</param>
        /// <param name="pixelY">The pixel's vertical position in the wheel bitmap.</param>
        /// <param name="wheelDiameter">The wheel bitmap's diameter in pixels.</param>
        /// <returns>The tooltip lines, or null when the pixel lies outside the wheel.</returns>
        private string[] BuildWheelBlendLines(Color pixel, int pixelX, int pixelY, int wheelDiameter)
        {
            List<PigmentCoefficients> paints = GetSelectedPaints(null);
            double[] weights = ColorWheelGenerator.GetBlendWeights(wheelDiameter, paints.Count, pixelX, pixelY);
            return weights == null ? null : ComposeBlendLines(pixel, paints, weights, null);
        }

        /// <summary>
        /// Builds the tooltip lines for a photo pixel by finding the closest mixture
        /// of the checked paints, since an arbitrary photo color carries no known
        /// recipe of its own.
        /// </summary>
        /// <param name="pixel">The color of the hovered pixel.</param>
        /// <returns>The tooltip lines; only the RGB line when no paints are checked.</returns>
        private string[] BuildClosestMixLines(Color pixel)
        {
            List<PigmentCoefficients> paints = GetSelectedPaints(null);

            // With nothing checked there is no mix to suggest; still report the RGB.
            if (paints.Count == 0)
            {
                return new[] { FormatRgbLine(pixel) };
            }

            // The matcher is costly to build, so it is created on first hover and
            // reused until the paint selection changes.
            if (blendMatcher == null)
            {
                blendMatcher = new PaintBlendMatcher(paints);
            }

            PaintBlendMatcher.BlendMatch match = blendMatcher.FindClosestBlend(pixel);

            return ComposeRecipeLines(pixel, paints, match);
        }

        /// <summary>
        /// Composes the tooltip text for a mixable recipe: the pixel's RGB, each paint
        /// with its percentage of the mixture, and how close the mixture actually lands.
        /// <para>
        /// Percentages rather than whole parts, because a ratio ladder can only express
        /// the mixtures that happen to sit on its rungs, and the paint the ladder misses
        /// by is exactly the paint whose tinting strength makes it matter. What is
        /// reported is whatever proportion lands closest to the target colour, rounded
        /// only far enough to be readable.
        /// </para>
        /// <para>
        /// The closeness lines matter because a limited palette often cannot reach a
        /// photo's colour at all, and silently returning the nearest thing would leave
        /// the user believing the mix is exact. Saying which way it misses turns that
        /// limitation into something they can correct for by eye.
        /// </para>
        /// </summary>
        /// <param name="pixel">The hovered pixel color.</param>
        /// <param name="paints">The paints the recipe's indices refer to.</param>
        /// <param name="match">The closest mixture and its recipe.</param>
        /// <returns>The tooltip lines.</returns>
        private static string[] ComposeRecipeLines(
            Color pixel, List<PigmentCoefficients> paints, PaintBlendMatcher.BlendMatch match)
        {
            var lines = new List<string> { FormatRgbLine(pixel), "Closest mix:" };

            // Largest share first, so the paint the user reaches for first is listed first.
            var order = new List<int>(match.PaintIndices.Count);
            for (int i = 0; i < match.PaintIndices.Count; i++)
            {
                order.Add(i);
            }
            order.Sort((first, second) => match.Percentages[second].CompareTo(match.Percentages[first]));

            foreach (int i in order)
            {
                lines.Add($"{match.Percentages[i]}% {paints[match.PaintIndices[i]].Name}");
            }

            PalettePhotoConverter.RgbToLab(pixel.R, pixel.G, pixel.B,
                out double targetL, out double targetA, out double targetB);
            PalettePhotoConverter.RgbToLab(match.MixedColor.R, match.MixedColor.G, match.MixedColor.B,
                out double mixL, out double mixA, out double mixB);

            double deltaE = ColorDifference.CieDe2000(targetL, targetA, targetB, mixL, mixA, mixB);
            lines.Add($"Match: {ColorDifference.DescribeQuality(deltaE)} (dE {deltaE:0.0})");

            string shift = ColorDifference.DescribeShift(targetL, targetA, targetB, mixL, mixA, mixB);
            if (shift != null)
            {
                lines.Add($"Mix reads {shift}");
            }

            // Two things the reconstructed pipeline could not report, because it had no
            // notion of a colour existing outside the screen's gamut and no unrounded
            // solution to compare its recipe against.
            if (match.ChromaLost > 0.001)
            {
                lines.Add("More vivid than this screen can show");
            }

            // Deliberately not labelled dE00: these are the matcher's weighted HyAB
            // distances, which is what the search minimises, not the CIEDE2000 figure
            // reported on the line above.
            double roundingCost = match.SnappedDistance - match.ExactDistance;
            if (roundingCost > 0.5)
            {
                lines.Add($"Rounded to whole percent: {match.ExactDistance:0.0} → {match.SnappedDistance:0.0}");
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Formats the RGB header line of the tooltip.
        /// </summary>
        /// <param name="pixel">The hovered pixel color.</param>
        /// <returns>The formatted RGB line.</returns>
        private static string FormatRgbLine(Color pixel)
        {
            return $"RGB: {pixel.R}, {pixel.G}, {pixel.B}";
        }

        /// <summary>
        /// Composes the tooltip text for a color wheel pixel: the pixel's RGB line, an
        /// optional header, and the blend's paints with their percentage shares, largest
        /// first. Only the top five paints get their own line; smaller contributors are
        /// rolled into a single "+N more" line so wheels built from many paints stay
        /// readable.
        /// <para>
        /// Percentages are right here and wrong for a recipe. A wheel pixel is a point in
        /// a continuous field that can draw on every paint at once, so it describes where
        /// the user is looking rather than something they could mix; see
        /// <see cref="ComposeRecipeLines"/> for the mixable case.
        /// </para>
        /// </summary>
        /// <param name="pixel">The hovered pixel color.</param>
        /// <param name="paints">The paints the weights refer to, index-aligned.</param>
        /// <param name="weights">Each paint's share of the blend, summing to 1.</param>
        /// <param name="header">A line inserted between the RGB line and the paint
        /// lines, or null for none.</param>
        /// <returns>The tooltip lines.</returns>
        private static string[] ComposeBlendLines(Color pixel, List<PigmentCoefficients> paints, double[] weights, string header)
        {
            const int MaxNamedPaints = 5;

            // Shares below half a percent would display as 0%, so they only count
            // toward the aggregated remainder line.
            const double MinVisibleShare = 0.005;

            var order = new List<int>(weights.Length);
            for (int i = 0; i < weights.Length; i++)
            {
                order.Add(i);
            }
            order.Sort((first, second) => weights[second].CompareTo(weights[first]));

            var lines = new List<string> { FormatRgbLine(pixel) };
            if (header != null)
            {
                lines.Add(header);
            }

            int named = 0;
            int others = 0;
            double othersShare = 0.0;
            foreach (int index in order)
            {
                if (named < MaxNamedPaints && weights[index] >= MinVisibleShare)
                {
                    lines.Add($"{paints[index].Name}: {weights[index] * 100:0}%");
                    named++;
                }
                else if (weights[index] > 0.0)
                {
                    others++;
                    othersShare += weights[index];
                }
            }

            if (others > 0 && othersShare >= MinVisibleShare)
            {
                lines.Add($"+{others} more: {othersShare * 100:0}%");
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Computes where the tooltip box should render: offset below-right of the
        /// cursor, flipped to the opposite side when it would run past the canvas
        /// edge, and sized to its measured text.
        /// </summary>
        /// <returns>The tooltip bounds in canvas client coordinates, or an
        /// empty rectangle when no tooltip is showing.</returns>
        private Rectangle GetBlendTooltipBounds()
        {
            if (blendTooltipLines == null)
            {
                return Rectangle.Empty;
            }

            int textWidth = 0;
            foreach (string line in blendTooltipLines)
            {
                textWidth = Math.Max(textWidth, TextRenderer.MeasureText(
                    line, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width);
            }

            int width = textWidth + 2 * TooltipPadding;
            int height = blendTooltipLines.Length * Font.Height + 2 * TooltipPadding;

            // The offset clears the cursor arrow; flipping to the other side of the
            // cursor keeps the box inside the control near the right and bottom edges.
            int x = blendTooltipAnchor.X + 16;
            int y = blendTooltipAnchor.Y + 20;
            if (x + width > imageCanvas.ClientSize.Width)
            {
                x = blendTooltipAnchor.X - width - 8;
            }
            if (y + height > imageCanvas.ClientSize.Height)
            {
                y = blendTooltipAnchor.Y - height - 8;
            }

            return new Rectangle(Math.Max(0, x), Math.Max(0, y), width, height);
        }

        /// <summary>
        /// Draws the blend tooltip beside the cursor: a dark box listing the hovered
        /// pixel's RGB values and its paint blend percentages.
        /// </summary>
        /// <param name="graphics">The graphics surface to draw on.</param>
        private void DrawBlendTooltip(Graphics graphics)
        {
            if (blendTooltipLines == null)
            {
                return;
            }

            Rectangle box = GetBlendTooltipBounds();

            // A translucent dark box with a light border stays legible over both
            // light and dark image areas.
            using (var background = new SolidBrush(Color.FromArgb(220, 32, 32, 32)))
            {
                graphics.FillRectangle(background, box);
            }
            using (var border = new Pen(Color.FromArgb(220, 180, 180, 180)))
            {
                graphics.DrawRectangle(border, box.X, box.Y, box.Width - 1, box.Height - 1);
            }

            int textY = box.Y + TooltipPadding;
            foreach (string line in blendTooltipLines)
            {
                TextRenderer.DrawText(graphics, line, Font,
                    new Point(box.X + TooltipPadding, textY), Color.White, TextFormatFlags.NoPadding);
                textY += Font.Height;
            }
        }

        /// <summary>
        /// Replaces the currently displayed image, disposing the previous one to
        /// avoid leaking GDI handles.
        /// </summary>
        /// <param name="image">The new image to display.</param>
        private void SetDisplayedImage(Image image)
        {
            // Whatever blend the tooltip showed belonged to the old image's pixels.
            HideBlendTooltip();

            Image previous = imageCanvas.Image;
            imageCanvas.Image = image;
            previous?.Dispose();
        }
    }
}
