using System;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// The transform between a displayed image and the control showing it: how far the
    /// image is magnified and where it sits. Holds no drawing code and no control
    /// reference, so the mapping the grid overlay and the blend tooltip depend on can be
    /// reasoned about, and tested, on its own.
    /// </summary>
    public class ImageViewport
    {
        /// <summary>
        /// How far past the fitted scale the image may be magnified. Beyond this a single
        /// image pixel covers so much of the window that panning loses all context.
        /// </summary>
        public const float MaxZoomFactor = 16f;

        /// <summary>
        /// The pixel dimensions of the image being displayed.
        /// </summary>
        private Size imageSize;

        /// <summary>
        /// The client size of the control displaying the image.
        /// </summary>
        private Size containerSize;

        /// <summary>
        /// Screen pixels per image pixel. Zero until both sizes are known.
        /// </summary>
        private float scale;

        /// <summary>
        /// Where the image's top-left corner lands in client coordinates.
        /// </summary>
        private PointF offset;

        /// <summary>
        /// Gets or sets the pixel dimensions of the displayed image. Setting a different
        /// size refits; setting the same size leaves the view untouched, so replacing a
        /// photo with its paint conversion keeps the user looking at the same region.
        /// </summary>
        public Size ImageSize
        {
            get => imageSize;
            set
            {
                if (imageSize == value)
                {
                    return;
                }

                imageSize = value;
                Fit();
            }
        }

        /// <summary>
        /// Gets or sets the client size of the control displaying the image. A fitted view
        /// refits to the new size; a magnified one keeps its scale and only re-clamps, so
        /// resizing the window does not throw away the user's zoom.
        /// </summary>
        public Size ContainerSize
        {
            get => containerSize;
            set
            {
                if (containerSize == value)
                {
                    return;
                }

                bool wasFitted = IsFitted;
                containerSize = value;

                if (wasFitted)
                {
                    Fit();
                }
                else
                {
                    ClampScaleAndOffset();
                }
            }
        }

        /// <summary>
        /// Gets the current magnification in screen pixels per image pixel.
        /// </summary>
        public float Scale => scale;

        /// <summary>
        /// Gets the scale at which the whole image fits inside the container, which is
        /// also the furthest the view is allowed to zoom out.
        /// </summary>
        public float FitScale
        {
            get
            {
                // A collapsed container or a missing image has no meaningful fit; 1 keeps
                // the arithmetic in the rest of the class finite until real sizes arrive.
                if (!HasContent)
                {
                    return 1f;
                }

                // The tighter of the two axes decides, so the whole image fits.
                return Math.Min(
                    (float)containerSize.Width / imageSize.Width,
                    (float)containerSize.Height / imageSize.Height);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the view is zoomed out as far as it goes, which
        /// is the state in which panning has no effect.
        /// </summary>
        public bool IsFitted => scale <= FitScale;

        /// <summary>
        /// Gets a value indicating whether there is both an image and somewhere to put it.
        /// </summary>
        private bool HasContent =>
            imageSize.Width > 0 && imageSize.Height > 0 &&
            containerSize.Width > 0 && containerSize.Height > 0;

        /// <summary>
        /// Zooms out until the whole image fits, and centers it.
        /// </summary>
        public void Fit()
        {
            scale = FitScale;

            // At the fitted scale neither axis is larger than the container, so clamping
            // centers both - the same placement the fitted PictureBox produced.
            ClampOffset();
        }

        /// <summary>
        /// Changes the magnification while holding one point of the image still.
        /// </summary>
        /// <param name="targetScale">The requested scale, clamped to between the fitted
        /// scale and <see cref="MaxZoomFactor"/> times it.</param>
        /// <param name="anchor">The point in client coordinates to zoom around, normally
        /// the cursor.</param>
        public void ZoomTo(float targetScale, PointF anchor)
        {
            if (!HasContent)
            {
                return;
            }

            float fit = FitScale;
            float clamped = Math.Clamp(targetScale, fit, fit * MaxZoomFactor);

            // Find the image point the anchor is over, then place the offset so that same
            // point lands back under the anchor at the new scale. Scaling without this
            // walks the view toward the top-left corner as it magnifies.
            float imageX = (anchor.X - offset.X) / scale;
            float imageY = (anchor.Y - offset.Y) / scale;

            scale = clamped;
            offset = new PointF(anchor.X - imageX * scale, anchor.Y - imageY * scale);
            ClampOffset();
        }

        /// <summary>
        /// Slides the image within the container.
        /// </summary>
        /// <param name="dx">Horizontal movement in client pixels; positive moves the image
        /// right.</param>
        /// <param name="dy">Vertical movement in client pixels; positive moves the image
        /// down.</param>
        public void PanBy(float dx, float dy)
        {
            if (!HasContent)
            {
                return;
            }

            offset = new PointF(offset.X + dx, offset.Y + dy);
            ClampOffset();
        }

        /// <summary>
        /// Computes the rectangle the image occupies in client coordinates.
        /// </summary>
        /// <returns>The displayed image bounds, or an empty rectangle when there is no
        /// image or nowhere to draw it.</returns>
        public RectangleF GetImageBounds()
        {
            if (!HasContent)
            {
                return RectangleF.Empty;
            }

            return new RectangleF(offset.X, offset.Y, imageSize.Width * scale, imageSize.Height * scale);
        }

        /// <summary>
        /// Finds the image pixel displayed at a point in the control.
        /// </summary>
        /// <param name="client">The point in client coordinates.</param>
        /// <param name="pixel">The image pixel under that point, when there is one.</param>
        /// <returns>True when the point lies over the image; false over the empty space
        /// around it.</returns>
        public bool TryGetImagePixel(Point client, out Point pixel)
        {
            pixel = Point.Empty;

            RectangleF bounds = GetImageBounds();
            if (bounds.IsEmpty || !bounds.Contains(client))
            {
                return false;
            }

            // The clamp guards the bottom and right edges, where rounding can otherwise
            // land one pixel past the image and throw when the bitmap is read.
            pixel = new Point(
                Math.Clamp((int)((client.X - bounds.Left) / scale), 0, imageSize.Width - 1),
                Math.Clamp((int)((client.Y - bounds.Top) / scale), 0, imageSize.Height - 1));
            return true;
        }

        /// <summary>
        /// Pulls the scale back to the fitted scale if the container has grown past it,
        /// then re-clamps the offset. Needed after a resize, which changes what fits.
        /// </summary>
        private void ClampScaleAndOffset()
        {
            scale = Math.Max(scale, FitScale);
            ClampOffset();
        }

        /// <summary>
        /// Moves the offset back into the range that keeps the image sensibly placed.
        /// </summary>
        private void ClampOffset()
        {
            offset = new PointF(
                ClampAxis(offset.X, imageSize.Width * scale, containerSize.Width),
                ClampAxis(offset.Y, imageSize.Height * scale, containerSize.Height));
        }

        /// <summary>
        /// Clamps one axis of the offset.
        /// </summary>
        /// <param name="value">The proposed offset on this axis.</param>
        /// <param name="imageExtent">The displayed size of the image on this axis.</param>
        /// <param name="containerExtent">The container's size on this axis.</param>
        /// <returns>The offset to use.</returns>
        private static float ClampAxis(float value, float imageExtent, float containerExtent)
        {
            // With room to spare the image is centered rather than free to drift, which is
            // how the fitted view behaved before zoom existed.
            if (imageExtent <= containerExtent)
            {
                return (containerExtent - imageExtent) / 2f;
            }

            // Larger than the container, the image must keep covering it: neither edge may
            // be dragged inside the matching container edge.
            return Math.Min(0f, Math.Max(containerExtent - imageExtent, value));
        }
    }
}
