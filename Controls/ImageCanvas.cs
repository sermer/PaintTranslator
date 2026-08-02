using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PaintTranslator.Imaging;

namespace PaintTranslator.Controls
{
    /// <summary>
    /// Displays an image under a zoom and pan transform the application controls.
    /// Replaces PictureBox, whose Zoom size mode always scales an image to fit and offers
    /// no way to magnify part of it.
    /// </summary>
    public class ImageCanvas : Control, IMessageFilter
    {
        /// <summary>
        /// The transform deciding where the image is drawn.
        /// </summary>
        private readonly ImageViewport viewport = new ImageViewport();

        /// <summary>
        /// The image being displayed, or null when nothing is loaded. Owned by the caller;
        /// this control never disposes it.
        /// </summary>
        private Image image;

        /// <summary>
        /// Windows message for the vertical wheel or a two-finger up-and-down swipe.
        /// </summary>
        private const int WmMouseWheel = 0x020A;

        /// <summary>
        /// Windows message for a horizontal wheel tilt or a sideways trackpad swipe.
        /// WinForms raises MouseWheel for the vertical wheel but has no equivalent event
        /// for this one.
        /// </summary>
        private const int WmMouseHWheel = 0x020E;

        /// <summary>
        /// The wheel units one detent produces, which Windows fixes at 120.
        /// </summary>
        private const float WheelDetent = 120f;

        /// <summary>
        /// How far one wheel detent pans, in client pixels.
        /// </summary>
        private const float PanPixelsPerDetent = 100f;

        /// <summary>
        /// Magnification per wheel unit. A detent is 120 units, so one notch zooms about
        /// 20%. A precision trackpad pinch arrives as a stream of much smaller deltas,
        /// which the same exponential turns into smooth continuous zoom.
        /// </summary>
        private const double ZoomPerWheelUnit = 1.0015;

        /// <summary>
        /// How far the pointer may move between press and release and still count as a
        /// click rather than a drag, in client pixels.
        /// </summary>
        private const float DragThreshold = 3f;

        /// <summary>
        /// Whether the left button is currently held down over the canvas.
        /// </summary>
        private bool dragging;

        /// <summary>
        /// Whether the pointer has moved far enough since the press to be a drag. Lets a
        /// press and release in place stay available as a click.
        /// </summary>
        private bool dragMoved;

        /// <summary>
        /// Where the left button went down, in client coordinates.
        /// </summary>
        private Point dragOrigin;

        /// <summary>
        /// The pointer position the last pan step was measured from.
        /// </summary>
        private Point dragLast;

        /// <summary>
        /// The zoom steps a magnifier click walks through, as multiples of the fitted
        /// scale. Multiples of fit rather than absolute percentages so the ladder behaves
        /// the same for a 4000px photo and the 512px color wheel.
        /// </summary>
        private static readonly float[] MagnifierSteps = { 2f, 4f, 8f };

        /// <summary>
        /// Guards the comparison that picks the next step, so a scale sitting exactly on
        /// a step advances past it instead of selecting itself.
        /// </summary>
        private const float ScaleEpsilon = 0.001f;

        /// <summary>
        /// Whether clicks on the image step the zoom.
        /// </summary>
        private bool magnifierActive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCanvas"/> class.
        /// </summary>
        public ImageCanvas()
        {
            // Everything - image, grid, tooltip - is painted into a back buffer on each
            // WM_PAINT. Without this the overlays flicker on every mouse move.
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,
                true);

            // The displayed image depends on the control size, so a resize is a full repaint.
            ResizeRedraw = true;
        }

        /// <summary>
        /// Gets or sets the image to display. Setting an image of different pixel
        /// dimensions refits the view; the same dimensions leave it where the user put it.
        /// </summary>
        public Image Image
        {
            get => image;
            set
            {
                image = value;
                viewport.ImageSize = value?.Size ?? Size.Empty;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets the transform placing the image, for callers that need to map between
        /// client coordinates and image pixels.
        /// </summary>
        public ImageViewport Viewport => viewport;

        /// <summary>
        /// Occurs when the zoom or position of the image changes, so overlays drawn over
        /// the image can be recomputed.
        /// </summary>
        public event EventHandler ViewChanged;

        /// <summary>
        /// Gets a value indicating whether a pan drag is in progress.
        /// </summary>
        public bool IsPanning => dragging;

        /// <summary>
        /// Gets or sets a value indicating whether a click on the image steps the zoom in.
        /// </summary>
        public bool MagnifierActive
        {
            get => magnifierActive;
            set
            {
                magnifierActive = value;
                UpdateCursor();
            }
        }

        /// <summary>
        /// Keeps the viewport's idea of the container in step with the control.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnResize(EventArgs e)
        {
            viewport.ContainerSize = ClientSize;
            base.OnResize(e);
        }

        /// <summary>
        /// Repaints and announces that the view moved.
        /// </summary>
        protected void OnViewChanged()
        {
            Invalidate();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Starts a pan drag.
        /// </summary>
        /// <param name="e">The event arguments carrying the button and position.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && image != null)
            {
                dragging = true;
                dragMoved = false;
                dragOrigin = e.Location;
                dragLast = e.Location;

                // Capture keeps the pan following the pointer when it leaves the control
                // mid-drag, which happens constantly when dragging toward an edge.
                Capture = true;
                UpdateCursor();
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Pans the image while the left button is held.
        /// </summary>
        /// <param name="e">The event arguments carrying the position.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                if (Math.Abs(e.X - dragOrigin.X) > DragThreshold || Math.Abs(e.Y - dragOrigin.Y) > DragThreshold)
                {
                    dragMoved = true;
                }

                // Measured against the previous position rather than the origin, so the
                // image tracks the pointer one to one even after clamping stops it.
                viewport.PanBy(e.X - dragLast.X, e.Y - dragLast.Y);
                dragLast = e.Location;
                UpdateCursor();
                OnViewChanged();
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Ends a pan drag.
        /// </summary>
        /// <param name="e">The event arguments carrying the button and position.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragging)
            {
                dragging = false;
                Capture = false;

                // A press and release that never moved is a click, which is the magnifier's
                // gesture; anything further was a pan and must not also zoom.
                if (!dragMoved && magnifierActive)
                {
                    StepMagnifier(e.Location);
                }

                UpdateCursor();
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Abandons a drag when the capture is taken away, so alt-tabbing mid-drag does
        /// not leave the canvas stuck panning.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (!Capture && dragging)
            {
                dragging = false;
                UpdateCursor();
            }

            base.OnMouseCaptureChanged(e);
        }

        /// <summary>
        /// Shows what the pointer will do: move the image while a pan is under way, and
        /// nothing special when the whole image already fits.
        /// </summary>
        private void UpdateCursor()
        {
            Cursor = dragging && !viewport.IsFitted ? Cursors.SizeAll
                : magnifierActive ? Cursors.Cross
                : Cursors.Default;
        }

        /// <summary>
        /// Applies one wheel message: zoom when Ctrl is held or a trackpad pinch is being
        /// translated into one, and pan otherwise.
        /// </summary>
        /// <param name="delta">The wheel delta, in units of 120 per detent.</param>
        /// <param name="cursor">The pointer position in client coordinates.</param>
        /// <param name="horizontal">True for a sideways wheel or swipe.</param>
        private void HandleWheel(int delta, Point cursor, bool horizontal)
        {
            if (image == null)
            {
                return;
            }

            // Windows reports a precision trackpad pinch as Ctrl plus wheel, so the mouse
            // and trackpad zoom gestures arrive through the same path.
            if (!horizontal && ModifierKeys.HasFlag(Keys.Control))
            {
                viewport.ZoomTo(viewport.Scale * (float)Math.Pow(ZoomPerWheelUnit, delta), cursor);
                OnViewChanged();
                return;
            }

            float distance = delta / WheelDetent * PanPixelsPerDetent;

            if (horizontal)
            {
                // A positive delta here means the wheel was tilted, or the trackpad
                // swiped, to the right; scrolling right moves the content the other way,
                // matching how a browser scrolls a page.
                viewport.PanBy(-distance, 0f);
            }
            else if (ModifierKeys.HasFlag(Keys.Shift))
            {
                // Shift only redirects the vertical wheel message onto the horizontal
                // axis - it does not turn it into the horizontal message above, so it
                // keeps that message's own sign convention rather than borrowing the
                // horizontal one.
                viewport.PanBy(distance, 0f);
            }
            else
            {
                viewport.PanBy(0f, distance);
            }

            OnViewChanged();
        }

        /// <summary>
        /// Zooms to the next magnifier step above the current scale, wrapping back to a
        /// fitted view once past the last one.
        /// </summary>
        /// <param name="anchor">The clicked point, which stays put across the zoom.</param>
        private void StepMagnifier(Point anchor)
        {
            float fit = viewport.FitScale;

            // Taking the first step strictly above the current scale means the ladder also
            // serves as the way back to a fitted view after a freehand pinch, so no
            // separate reset control is needed.
            float target = fit;
            foreach (float step in MagnifierSteps)
            {
                if (viewport.Scale < fit * step - ScaleEpsilon)
                {
                    target = fit * step;
                    break;
                }
            }

            viewport.ZoomTo(target, anchor);
            OnViewChanged();
        }

        /// <summary>
        /// Draws the image, then lets subscribers draw over it.
        /// </summary>
        /// <param name="e">The paint event arguments providing the graphics surface.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // The control can be painted before any resize has fired, so the container
            // size is confirmed here too rather than only in OnResize.
            viewport.ContainerSize = ClientSize;

            if (image != null)
            {
                // Drawing the image leaves the interpolation and pixel offset modes it
                // needs on the shared surface. Half pixel offset in particular shifts
                // everything drawn afterwards up and left by a pixel, which would push an
                // overlay's border outside the bounds it invalidates to erase itself and
                // leave a trail of stale edges behind. Subscribers get the surface back
                // in the state they were handed it.
                GraphicsState state = e.Graphics.Save();
                DrawImage(e.Graphics);
                e.Graphics.Restore(state);
            }
            else
            {
                DrawEmptyState(e.Graphics);
            }

            // Raised last so the grid and tooltip land on top of the image, matching the
            // order PictureBox gave those overlays.
            base.OnPaint(e);
        }

        private void DrawEmptyState(Graphics graphics)
        {
            int width = Math.Min(420, Math.Max(260, ClientSize.Width - 64));
            int height = 168;
            var card = new Rectangle(
                (ClientSize.Width - width) / 2,
                (ClientSize.Height - height) / 2,
                width,
                height);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var background = new SolidBrush(Color.FromArgb(155, UiTheme.Surface)))
            using (var border = new Pen(UiTheme.Border) { DashStyle = DashStyle.Dash })
            {
                graphics.FillRectangle(background, card);
                graphics.DrawRectangle(border, card);
            }

            var accent = new Rectangle(card.Left + 28, card.Top + 31, 48, 4);
            using (var accentBrush = new SolidBrush(UiTheme.Accent))
            {
                graphics.FillRectangle(accentBrush, accent);
            }

            var title = new Rectangle(card.Left + 28, card.Top + 51, card.Width - 56, 38);
            TextRenderer.DrawText(
                graphics,
                "Drop a photo to begin",
                UiTheme.EmptyStateTitleFont,
                title,
                UiTheme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            var subtitle = new Rectangle(card.Left + 30, card.Top + 98, card.Width - 60, 44);
            TextRenderer.DrawText(
                graphics,
                "Drag an image here, paste from the clipboard, or choose Open Photo.",
                UiTheme.DefaultFont,
                subtitle,
                UiTheme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
        /// <summary>
        /// Draws the visible part of the image at its current scale and position.
        /// </summary>
        /// <param name="graphics">The graphics surface to draw on.</param>
        private void DrawImage(Graphics graphics)
        {
            RectangleF bounds = viewport.GetImageBounds();
            if (bounds.IsEmpty)
            {
                return;
            }

            // Only the part of the image inside the control is worth drawing. Handing GDI+
            // the whole bitmap at high magnification scales millions of pixels nobody sees,
            // which shows up as stutter while panning a large photo.
            RectangleF destination = RectangleF.Intersect(bounds, ClientRectangle);
            if (destination.IsEmpty)
            {
                return;
            }

            float scale = viewport.Scale;
            var source = new RectangleF(
                (destination.Left - bounds.Left) / scale,
                (destination.Top - bounds.Top) / scale,
                destination.Width / scale,
                destination.Height / scale);

            // A fitted view can still sit above 1:1 - fitting enlarges an image smaller than
            // the canvas, exactly as PictureBoxSizeMode.Zoom did - and that enlargement should
            // stay smooth rather than blocky. Only a deliberate zoom past actual pixel size
            // means the user is inspecting individual pixels, where a paint conversion's flat
            // color regions should read as crisp blocks; smoothing only helps on the way down,
            // so a below-1:1 zoom also stays smooth. Half pixel offset keeps nearest-neighbor
            // sampling centered - without it the image shifts half a pixel.
            graphics.InterpolationMode = scale > 1f && !viewport.IsFitted
                ? InterpolationMode.NearestNeighbor
                : InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            graphics.DrawImage(image, destination, source, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// Starts watching for wheel messages once there is a window to compare the
        /// pointer against.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Application.AddMessageFilter(this);
        }

        /// <summary>
        /// Stops watching for wheel messages, so a destroyed canvas is not left holding a
        /// filter registration for the life of the application.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            Application.RemoveMessageFilter(this);
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Claims wheel messages aimed at the pointer's position over this canvas.
        /// Windows delivers them to whichever control has focus, which is never this one,
        /// so without the filter scrolling over the image would do nothing.
        /// </summary>
        /// <param name="m">The message about to be dispatched.</param>
        /// <returns>True when the message was handled here and should go no further.</returns>
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmMouseWheel && m.Msg != WmMouseHWheel)
            {
                return false;
            }

            if (!IsHandleCreated || !Visible || !Enabled)
            {
                return false;
            }

            // The filter sees every message the application pumps, not just this
            // window's, so a modal dialog sitting over the canvas would otherwise have
            // its own wheel input stolen by the canvas underneath it.
            Form form = FindForm();
            if (form == null || Form.ActiveForm != form)
            {
                return false;
            }

            Point cursor = PointToClient(Cursor.Position);
            if (!ClientRectangle.Contains(cursor))
            {
                return false;
            }

            // The delta is the signed high word of wParam, in units of 120 per detent.
            int delta = (short)(m.WParam.ToInt64() >> 16);
            HandleWheel(delta, cursor, m.Msg == WmMouseHWheel);
            return true;
        }
    }
}
