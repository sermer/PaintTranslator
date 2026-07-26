using System.Drawing;
using System.Windows.Forms;
using PaintTranslator.Controls;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the canvas that draws the image beneath the grid and blend tooltip. Those
    /// overlays repaint only the rectangle they occupy, so anything the canvas leaves on
    /// the graphics surface that shifts where they land shows up as trails of stale
    /// pixels outside the invalidated area.
    /// </summary>
    public class ImageCanvasTests
    {
        /// <summary>
        /// An image canvas whose paint pass can be driven directly, since a test has no
        /// message loop to deliver WM_PAINT.
        /// </summary>
        private class TestableImageCanvas : ImageCanvas
        {
            /// <summary>
            /// Runs the canvas paint pass against a caller-supplied surface.
            /// </summary>
            /// <param name="e">The paint event arguments to paint with.</param>
            public void RaisePaint(PaintEventArgs e)
            {
                OnPaint(e);
            }
        }

        /// <summary>
        /// Confirms a rectangle drawn by a paint subscriber covers exactly the pixels of
        /// the bounds it was given. The blend tooltip erases its old position by
        /// invalidating the rectangle it reported, so a border that renders even one pixel
        /// outside those bounds is never repainted and leaves a trail behind the cursor.
        /// </summary>
        [Fact]
        public void OverlayDrawnByPaintSubscriberStaysWithinItsBounds()
        {
            var box = new Rectangle(50, 50, 60, 40);
            Color borderColor = Color.FromArgb(255, 180, 180, 180);

            using (var canvas = new TestableImageCanvas { Size = new Size(200, 200) })
            using (var image = new Bitmap(100, 100))
            using (var surface = new Bitmap(200, 200))
            {
                // A solid image fills the canvas, so any pixel still showing it is one the
                // overlay did not touch.
                using (Graphics imageGraphics = Graphics.FromImage(image))
                {
                    imageGraphics.Clear(Color.Red);
                }

                canvas.Image = image;
                canvas.Paint += (sender, e) =>
                {
                    using (var border = new Pen(borderColor))
                    {
                        e.Graphics.DrawRectangle(border, box.X, box.Y, box.Width - 1, box.Height - 1);
                    }
                };

                using (Graphics graphics = Graphics.FromImage(surface))
                {
                    canvas.RaisePaint(new PaintEventArgs(graphics, new Rectangle(Point.Empty, canvas.Size)));
                }

                // The row above and the column left of the box belong to the image; the
                // box's own top-left corner is where the border is meant to be.
                Assert.Equal(Color.Red.ToArgb(), surface.GetPixel(box.X + 5, box.Y - 1).ToArgb());
                Assert.Equal(Color.Red.ToArgb(), surface.GetPixel(box.X - 1, box.Y + 5).ToArgb());
                Assert.Equal(borderColor.ToArgb(), surface.GetPixel(box.X + 5, box.Y).ToArgb());
                Assert.Equal(borderColor.ToArgb(), surface.GetPixel(box.X, box.Y + 5).ToArgb());
            }
        }
    }
}
