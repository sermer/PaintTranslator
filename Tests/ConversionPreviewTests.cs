using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class ConversionPreviewTests
    {
        [Fact]
        public void SourceIsFitInsideThePreviewBoundWithoutChangingAspectRatio()
        {
            PixelImage source = PixelImage.Filled(800, 600, unchecked((int)0xFF808080));
            PixelImage preview = ConversionPreview.CreateSource(source, 400);
            Assert.Equal(new Size(400, 300), preview.Size);
        }

        [Fact]
        public void SmallSourcesAreNotUpscaled()
        {
            PixelImage source = PixelImage.Filled(100, 50, unchecked((int)0xFF808080));
            PixelImage preview = ConversionPreview.CreateSource(source, 400);
            Assert.Equal(source.Size, preview.Size);
        }

        [Fact]
        public void AFlatImageStaysFlatWhenDownsampled()
        {
            int colour = unchecked((int)0xFF3C78B4);
            PixelImage source = PixelImage.Filled(90, 60, colour);
            PixelImage preview = ConversionPreview.Downsample(source, 27, 18);
            foreach (int pixel in preview.Pixels)
            {
                Assert.Equal(colour, pixel);
            }
        }

        [Fact]
        public void PartialCoverageAveragesTheStraddledPixels()
        {
            // Four source columns [black, black, white, white] into three output
            // columns: the middle output straddles one black and one white pixel
            // with equal weight, so it must be the exact midpoint.
            int black = unchecked((int)0xFF000000);
            int white = unchecked((int)0xFFFFFFFF);
            PixelImage source = PixelImage.FromPixels(4, 1, new[] { black, black, white, white });
            PixelImage preview = ConversionPreview.Downsample(source, 3, 1);

            Assert.Equal(black, preview[0, 0]);
            Assert.Equal(unchecked((int)0xFF808080), preview[1, 0]);
            Assert.Equal(white, preview[2, 0]);
        }

        [Fact]
        public void AlphaIsAveragedLikeAnyOtherChannel()
        {
            int transparent = 0x00000000;
            int opaque = unchecked((int)0xFF000000);
            PixelImage source = PixelImage.FromPixels(2, 1, new[] { transparent, opaque });
            PixelImage preview = ConversionPreview.Downsample(source, 1, 1);
            Assert.Equal(unchecked((int)0x80000000), preview[0, 0]);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(20, 10)]
        [InlineData(1, 1)]
        public void PixelRadiiFollowThePreviewScaleAndKeepZeroAsOff(int sourceRadius, int expected)
        {
            int actual = ConversionPreview.ScaleRadius(
                sourceRadius, new Size(800, 600), new Size(400, 300));
            Assert.Equal(expected, actual);
        }
    }
}
