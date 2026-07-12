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
        /// The cursor position the tooltip is anchored to, in picture box client
        /// coordinates.
        /// </summary>
        private Point blendTooltipAnchor;

        /// <summary>
        /// The box the tooltip last painted into, kept so mouse movement can
        /// invalidate just the old and new tooltip areas instead of the whole image.
        /// </summary>
        private Rectangle blendTooltipDrawnBounds;

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

            // The check change alters which paints can mix, so the hover matcher
            // must be rebuilt from the new selection on next use.
            blendMatcher = null;

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
                SetDisplayedImage(ColorWheelGenerator.Create(512, GetSelectedPaintColors(null)));
            }
        }

        /// <summary>
        /// Collects all checked paints, in palette order.
        /// </summary>
        /// <param name="pendingChange">A check change that has not been applied yet
        /// (ItemCheck fires before the state updates), or null to read the current
        /// states as-is.</param>
        /// <returns>The checked paints.</returns>
        private List<GoldenPaint> GetSelectedPaints(ItemCheckEventArgs pendingChange)
        {
            var paints = new List<GoldenPaint>(paintsCheckedListBox.Items.Count);

            for (int i = 0; i < paintsCheckedListBox.Items.Count; i++)
            {
                // Substitute the pending state for the item being toggled, since
                // GetItemChecked still reports its old value during ItemCheck.
                bool isChecked = pendingChange != null && pendingChange.Index == i
                    ? pendingChange.NewValue == CheckState.Checked
                    : paintsCheckedListBox.GetItemChecked(i);

                if (isChecked && paintsCheckedListBox.Items[i] is GoldenPaint paint)
                {
                    paints.Add(paint);
                }
            }

            return paints;
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
            List<GoldenPaint> paints = GetSelectedPaints(pendingChange);
            var colors = new List<Color>(paints.Count);
            foreach (GoldenPaint paint in paints)
            {
                colors.Add(paint.Color);
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
        /// Paints the grid overlay and the blend tooltip on top of the displayed image.
        /// </summary>
        /// <param name="sender">The picture box being painted.</param>
        /// <param name="e">The paint event arguments providing the graphics surface.</param>
        private void ImagePictureBox_Paint(object sender, PaintEventArgs e)
        {
            // The base PictureBox paint has already drawn the image; nothing to
            // overlay when no image is loaded.
            if (imagePictureBox.Image == null)
            {
                return;
            }

            if (showGridCheckBox.Checked)
            {
                // The grid must cover the image itself, not the whole control, so compute
                // where Zoom mode actually placed the image within the client area.
                RectangleF imageBounds = GridOverlayRenderer.GetZoomedImageBounds(
                    imagePictureBox.ClientSize, imagePictureBox.Image.Size);
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
        /// Updates the blend tooltip as the mouse moves over the picture box, so it
        /// tracks the cursor and describes the pixel underneath it.
        /// </summary>
        /// <param name="sender">The picture box the mouse moved over.</param>
        /// <param name="e">The event arguments carrying the cursor position.</param>
        private void ImagePictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateBlendTooltip(e.Location);
        }

        /// <summary>
        /// Hides the blend tooltip when the mouse leaves the picture box.
        /// </summary>
        /// <param name="sender">The picture box the mouse left.</param>
        /// <param name="e">The event arguments.</param>
        private void ImagePictureBox_MouseLeave(object sender, EventArgs e)
        {
            HideBlendTooltip();
        }

        /// <summary>
        /// Recomputes the blend tooltip for a cursor position: resolves which image
        /// pixel sits under the cursor, derives that pixel's paint blend (exact wheel
        /// weights for a generated wheel, the closest achievable mixture for a photo),
        /// and moves the tooltip beside the cursor.
        /// </summary>
        /// <param name="cursor">The cursor position in picture box client coordinates.</param>
        private void UpdateBlendTooltip(Point cursor)
        {
            // Every displayed image is created as a Bitmap; anything else (or no
            // image at all) has no pixels to sample.
            if (!(imagePictureBox.Image is Bitmap bitmap))
            {
                HideBlendTooltip();
                return;
            }

            // Only the zoomed image area counts; the letterbox around it shows
            // the control's background, not image pixels.
            RectangleF bounds = GridOverlayRenderer.GetZoomedImageBounds(
                imagePictureBox.ClientSize, bitmap.Size);
            if (bounds.IsEmpty || !bounds.Contains(cursor))
            {
                HideBlendTooltip();
                return;
            }

            // Map the cursor from control coordinates back to a source pixel; the
            // clamp guards the bottom and right edges, where rounding can land one
            // pixel past the image.
            int pixelX = Math.Clamp((int)((cursor.X - bounds.Left) * bitmap.Width / bounds.Width), 0, bitmap.Width - 1);
            int pixelY = Math.Clamp((int)((cursor.Y - bounds.Top) * bitmap.Height / bounds.Height), 0, bitmap.Height - 1);

            Color pixel = bitmap.GetPixel(pixelX, pixelY);

            // Fully transparent pixels are the empty surround of the color wheel;
            // there is no paint there to describe.
            if (pixel.A == 0)
            {
                HideBlendTooltip();
                return;
            }

            string[] lines = wheelDisplayed
                ? BuildWheelBlendLines(pixel, pixelX, pixelY, bitmap.Width)
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
            imagePictureBox.Invalidate(previous.IsEmpty
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
            imagePictureBox.Invalidate(previous);
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
            List<GoldenPaint> paints = GetSelectedPaints(null);
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
            List<GoldenPaint> paints = GetSelectedPaints(null);

            // With nothing checked there is no mix to suggest; still report the RGB.
            if (paints.Count == 0)
            {
                return new[] { FormatRgbLine(pixel) };
            }

            // The matcher is costly to build, so it is created on first hover and
            // reused until the paint selection changes.
            if (blendMatcher == null)
            {
                var colors = new List<Color>(paints.Count);
                foreach (GoldenPaint paint in paints)
                {
                    colors.Add(paint.Color);
                }
                blendMatcher = new PaintBlendMatcher(colors);
            }

            PaintBlendMatcher.BlendMatch match = blendMatcher.FindClosestBlend(pixel);

            // Spread the recipe back over the full paint list so the shared line
            // builder can treat wheel and photo blends identically.
            var weights = new double[paints.Count];
            for (int i = 0; i < match.PaintIndices.Count; i++)
            {
                weights[match.PaintIndices[i]] = match.Weights[i];
            }

            return ComposeBlendLines(pixel, paints, weights, "Closest mix:");
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
        /// Composes the tooltip text: the pixel's RGB line, an optional header, and
        /// the blend's paints with their percentage shares, largest first. Only the
        /// top five paints get their own line; smaller contributors are rolled into
        /// a single "+N more" line so wheels built from many paints stay readable.
        /// </summary>
        /// <param name="pixel">The hovered pixel color.</param>
        /// <param name="paints">The paints the weights refer to, index-aligned.</param>
        /// <param name="weights">Each paint's share of the blend, summing to 1.</param>
        /// <param name="header">A line inserted between the RGB line and the paint
        /// lines, or null for none.</param>
        /// <returns>The tooltip lines.</returns>
        private static string[] ComposeBlendLines(Color pixel, List<GoldenPaint> paints, double[] weights, string header)
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
        /// cursor, flipped to the opposite side when it would run past the picture
        /// box edge, and sized to its measured text.
        /// </summary>
        /// <returns>The tooltip bounds in picture box client coordinates, or an
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
            if (x + width > imagePictureBox.ClientSize.Width)
            {
                x = blendTooltipAnchor.X - width - 8;
            }
            if (y + height > imagePictureBox.ClientSize.Height)
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

            Image previous = imagePictureBox.Image;
            imagePictureBox.Image = image;
            previous?.Dispose();
        }
    }
}
