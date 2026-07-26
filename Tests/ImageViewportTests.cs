using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the transform between an image and the control displaying it. The viewport
    /// decides where the image is drawn and which image pixel sits under the cursor, so
    /// an error here misplaces the grid overlay and makes the blend tooltip report the
    /// wrong pixel.
    /// </summary>
    public class ImageViewportTests
    {
        /// <summary>
        /// Builds a viewport holding a 400x200 image in an 800x600 container, the
        /// arrangement most of these tests reason about. Fitted, it displays at 2x as an
        /// 800x400 band with 100px of empty space above and below.
        /// </summary>
        /// <returns>A fitted viewport.</returns>
        private static ImageViewport CreateFittedViewport()
        {
            return new ImageViewport
            {
                ContainerSize = new Size(800, 600),
                ImageSize = new Size(400, 200),
            };
        }

        /// <summary>
        /// Confirms a fitted image is scaled by the tighter axis and centered on the
        /// other, which is how every image appeared before zoom existed. Regressing this
        /// changes the first thing the user sees on every load.
        /// </summary>
        /// <param name="imageWidth">The image width in pixels.</param>
        /// <param name="imageHeight">The image height in pixels.</param>
        /// <param name="x">The expected left edge of the displayed image.</param>
        /// <param name="y">The expected top edge of the displayed image.</param>
        /// <param name="width">The expected displayed width.</param>
        /// <param name="height">The expected displayed height.</param>
        [Theory]
        [InlineData(400, 200, 0f, 100f, 800f, 400f)]
        [InlineData(200, 400, 250f, 0f, 300f, 600f)]
        [InlineData(100, 100, 100f, 0f, 600f, 600f)]
        public void FitsTheImageToTheContainerAndCentersIt(
            int imageWidth, int imageHeight, float x, float y, float width, float height)
        {
            var viewport = new ImageViewport
            {
                ContainerSize = new Size(800, 600),
                ImageSize = new Size(imageWidth, imageHeight),
            };

            Assert.Equal(new RectangleF(x, y, width, height), viewport.GetImageBounds());
        }

        /// <summary>
        /// Confirms the image corners map to the first and last pixel, and that the empty
        /// space beside a letterboxed image maps to nothing. The tooltip reads a bitmap at
        /// the returned coordinates, so a value one past the edge throws.
        /// </summary>
        [Fact]
        public void MapsTheImageCornersToTheFirstAndLastPixel()
        {
            ImageViewport viewport = CreateFittedViewport();

            Assert.True(viewport.TryGetImagePixel(new Point(0, 100), out Point topLeft));
            Assert.Equal(new Point(0, 0), topLeft);

            Assert.True(viewport.TryGetImagePixel(new Point(799, 499), out Point bottomRight));
            Assert.Equal(new Point(399, 199), bottomRight);

            Assert.False(viewport.TryGetImagePixel(new Point(400, 50), out _));
        }

        /// <summary>
        /// Confirms replacing the image with one of identical dimensions preserves a
        /// magnified view, so converting a photo to paints keeps the user looking at the
        /// same region they had zoomed into.
        /// </summary>
        [Fact]
        public void KeepsTheViewWhenTheReplacementImageIsTheSameSize()
        {
            ImageViewport viewport = CreateFittedViewport();
            viewport.ZoomTo(8f, new PointF(300f, 200f));
            RectangleF before = viewport.GetImageBounds();

            viewport.ImageSize = new Size(400, 200);

            Assert.Equal(before, viewport.GetImageBounds());
        }

        /// <summary>
        /// Confirms an image of different dimensions refits. A view carried over from an
        /// unrelated image would leave the new one part-way off screen.
        /// </summary>
        [Fact]
        public void RefitsWhenTheReplacementImageIsADifferentSize()
        {
            ImageViewport viewport = CreateFittedViewport();

            viewport.ImageSize = new Size(200, 400);

            Assert.Equal(new RectangleF(250f, 0f, 300f, 600f), viewport.GetImageBounds());
        }

        /// <summary>
        /// Confirms a fitted view refits when the window is resized, rather than keeping a
        /// scale computed for the old container size.
        /// </summary>
        [Fact]
        public void RefitsOnResizeWhileTheViewIsFitted()
        {
            ImageViewport viewport = CreateFittedViewport();

            viewport.ContainerSize = new Size(400, 300);

            Assert.Equal(new RectangleF(0f, 50f, 400f, 200f), viewport.GetImageBounds());
        }

        /// <summary>
        /// Confirms both sizes being empty yields no bounds and no pixel rather than a
        /// divide by zero. A minimized window reports a zero client size, and the control
        /// exists before any image is loaded.
        /// </summary>
        [Fact]
        public void ReportsNothingWhileEitherSizeIsEmpty()
        {
            var viewport = new ImageViewport();

            Assert.True(viewport.GetImageBounds().IsEmpty);
            Assert.False(viewport.TryGetImagePixel(new Point(10, 10), out _));

            viewport.ImageSize = new Size(400, 200);

            Assert.True(viewport.GetImageBounds().IsEmpty);
            Assert.False(viewport.TryGetImagePixel(new Point(10, 10), out _));
        }

        /// <summary>
        /// Confirms the pixel under the cursor stays under the cursor across a zoom.
        /// Without this the view drifts toward the top-left as it magnifies, and pinching
        /// on a detail walks it off screen.
        /// </summary>
        [Fact]
        public void KeepsTheAnchoredPixelUnderTheCursorWhileZooming()
        {
            ImageViewport viewport = CreateFittedViewport();
            var cursor = new Point(300, 200);
            Assert.True(viewport.TryGetImagePixel(cursor, out Point before));

            viewport.ZoomTo(viewport.Scale * 2f, cursor);

            Assert.True(viewport.TryGetImagePixel(cursor, out Point after));
            Assert.Equal(before, after);
        }

        /// <summary>
        /// Confirms zooming out stops at the fitted scale and lands back on the fitted
        /// placement. Below fit the image only shrinks into a corner of empty space.
        /// </summary>
        [Fact]
        public void ClampsZoomOutToTheFittedScale()
        {
            ImageViewport viewport = CreateFittedViewport();

            viewport.ZoomTo(0.1f, new PointF(300f, 200f));

            Assert.Equal(viewport.FitScale, viewport.Scale);
            Assert.Equal(new RectangleF(0f, 100f, 800f, 400f), viewport.GetImageBounds());
        }

        /// <summary>
        /// Confirms zooming in stops at the documented ceiling, so a fast pinch cannot
        /// magnify one pixel to fill the window.
        /// </summary>
        [Fact]
        public void ClampsZoomInToTheMaximumFactor()
        {
            ImageViewport viewport = CreateFittedViewport();

            viewport.ZoomTo(10000f, new PointF(300f, 200f));

            Assert.Equal(viewport.FitScale * ImageViewport.MaxZoomFactor, viewport.Scale);
        }

        /// <summary>
        /// Confirms an axis with room to spare ignores panning and stays centered, so
        /// scrolling a fitted image cannot nudge it off center.
        /// </summary>
        [Fact]
        public void HoldsTheImageCenteredOnAnAxisWithRoomToSpare()
        {
            ImageViewport viewport = CreateFittedViewport();

            viewport.PanBy(500f, 500f);

            Assert.Equal(new RectangleF(0f, 100f, 800f, 400f), viewport.GetImageBounds());
        }

        /// <summary>
        /// Confirms panning stops with the image still covering the container in both
        /// directions, rather than letting the user drag it into empty space.
        /// </summary>
        [Fact]
        public void StopsPanningAtTheImageEdges()
        {
            ImageViewport viewport = CreateFittedViewport();

            // Four times the fitted scale of 2 displays the image at 3200x1600, larger
            // than the container on both axes.
            viewport.ZoomTo(8f, new PointF(400f, 300f));

            viewport.PanBy(-100000f, -100000f);
            RectangleF farCorner = viewport.GetImageBounds();
            Assert.Equal(800f - 3200f, farCorner.Left);
            Assert.Equal(600f - 1600f, farCorner.Top);

            viewport.PanBy(100000f, 100000f);
            RectangleF nearCorner = viewport.GetImageBounds();
            Assert.Equal(0f, nearCorner.Left);
            Assert.Equal(0f, nearCorner.Top);
        }

        /// <summary>
        /// Confirms a resize keeps a magnified view magnified, and that growing the
        /// container past the current scale pulls the scale up to the new fit rather than
        /// leaving the image stranded smaller than its own fitted size.
        /// </summary>
        [Fact]
        public void KeepsTheZoomAcrossAResizeButNeverBelowFit()
        {
            ImageViewport viewport = CreateFittedViewport();
            viewport.ZoomTo(8f, new PointF(400f, 300f));

            viewport.ContainerSize = new Size(400, 300);
            Assert.Equal(8f, viewport.Scale);

            viewport.ContainerSize = new Size(8000, 6000);
            Assert.Equal(20f, viewport.Scale);
        }

        /// <summary>
        /// Confirms zooming and panning a viewport that has no image yet is harmless. The
        /// control receives wheel messages before anything is loaded.
        /// </summary>
        [Fact]
        public void IgnoresZoomAndPanWhileEmpty()
        {
            var viewport = new ImageViewport();

            viewport.ZoomTo(4f, new PointF(10f, 10f));
            viewport.PanBy(10f, 10f);

            Assert.True(viewport.GetImageBounds().IsEmpty);
        }
    }
}
