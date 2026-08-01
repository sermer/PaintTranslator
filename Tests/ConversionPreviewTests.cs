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
            using var source = new Bitmap(800, 600);
            using Bitmap preview = ConversionPreview.CreateSource(source, 400);

            Assert.Equal(new Size(400, 300), preview.Size);
        }

        [Fact]
        public void SmallSourcesAreNotUpscaled()
        {
            using var source = new Bitmap(100, 50);
            using Bitmap preview = ConversionPreview.CreateSource(source, 400);

            Assert.Equal(source.Size, preview.Size);
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
