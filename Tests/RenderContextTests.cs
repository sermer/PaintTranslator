using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the mark-size default. It has to scale with the image: a brush covers a
    /// roughly constant fraction of a canvas whatever resolution the file happens to
    /// be, so a fixed pixel count would mean a phone photo and a scan of the same
    /// scene get different paintings.
    /// </summary>
    public class RenderContextTests
    {
        /// <summary>
        /// A full-resolution photo should land near 200 marks across its short edge,
        /// which is the range a detailed painting occupies.
        /// </summary>
        [Fact]
        public void AFullResolutionPhotoDefaultsToAboutTwoHundredMarksAcross()
        {
            Assert.Equal(20, RenderContext.DefaultMarkPixels(4000, 3000));
        }

        /// <summary>
        /// The short edge governs, not the long one, so a panorama does not get marks
        /// too coarse for its height, and a portrait does not get marks too fine for
        /// its width.
        /// </summary>
        [Fact]
        public void TheShortEdgeGovernsTheDefault()
        {
            // Landscape: width 9000, height 3000 → short edge is 3000
            Assert.Equal(RenderContext.DefaultMarkPixels(3000, 3000), RenderContext.DefaultMarkPixels(9000, 3000));

            // Portrait: width 3000, height 9000 → short edge is 3000
            Assert.Equal(RenderContext.DefaultMarkPixels(3000, 3000), RenderContext.DefaultMarkPixels(3000, 9000));
        }

        /// <summary>
        /// A small test image still gets a usable mark rather than a fractional one.
        /// </summary>
        [Fact]
        public void ASmallImageRoundsToWholePixels()
        {
            Assert.Equal(3, RenderContext.DefaultMarkPixels(512, 512));
        }

        /// <summary>
        /// Below the clamp a mark would be one pixel, which is precisely the
        /// unpaintable case the mark size exists to prevent.
        /// </summary>
        [Fact]
        public void TinyImagesClampToTwoPixelsRatherThanOne()
        {
            Assert.Equal(2, RenderContext.DefaultMarkPixels(100, 100));
        }

        /// <summary>
        /// The upper clamp stops an enormous scan from defaulting to a mark so large
        /// the picture becomes four blocks.
        /// </summary>
        [Fact]
        public void EnormousImagesClampAtTheCeiling()
        {
            Assert.Equal(128, RenderContext.DefaultMarkPixels(40000, 30000));
        }

        /// <summary>
        /// The context carries what it was given, unchanged. It is a record of the
        /// render's geometry, not a place where policy lives.
        /// </summary>
        [Fact]
        public void TheContextCarriesItsValuesUnchanged()
        {
            var context = new RenderContext(800, 600, 7.5, 62.5);

            Assert.Equal(800, context.Width);
            Assert.Equal(600, context.Height);
            Assert.Equal(7.5, context.MarkPixels);
            Assert.Equal(62.5, context.AchievableMaxChroma);
        }
    }
}
