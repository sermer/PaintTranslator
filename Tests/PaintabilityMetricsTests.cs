using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the measurement that makes invariant I2 — every output region large enough
    /// for a brush to have made it — into a number rather than an opinion. The metric
    /// itself has to be trustworthy before any filter can be judged against it, which
    /// is what these tests are for.
    /// </summary>
    public class PaintabilityMetricsTests
    {
        /// <summary>
        /// A single flat field is one region covering everything, so nothing at all is
        /// in a small region however small the threshold.
        /// </summary>
        [Fact]
        public void AFlatFieldHasNothingInSmallRegions()
        {
            int[] pixels = Fill(32, 32, unchecked((int)0xFF804020));

            Assert.Equal(0.0, PaintabilityMetrics.FractionInRegionsSmallerThan(pixels, 32, 32, 32, 16));
        }

        /// <summary>
        /// A one-pixel checkerboard is the worst case the metric exists to detect:
        /// every region is a single pixel, so all of the image is unpaintable.
        /// </summary>
        [Fact]
        public void AOnePixelCheckerboardIsEntirelyUnpaintable()
        {
            var pixels = new int[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    pixels[(y * 32) + x] = ((x + y) % 2 == 0)
                        ? unchecked((int)0xFF000000)
                        : unchecked((int)0xFFFFFFFF);
                }
            }

            Assert.Equal(1.0, PaintabilityMetrics.FractionInRegionsSmallerThan(pixels, 32, 32, 32, 4));
            Assert.Equal(32 * 32, PaintabilityMetrics.CountRegions(pixels, 32, 32, 32));
        }

        /// <summary>
        /// Two big halves are two regions, both far above any brushmark, so the metric
        /// must not be fooled by there being more than one colour present.
        /// </summary>
        [Fact]
        public void TwoLargeRegionsCountAsPaintable()
        {
            var pixels = new int[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    pixels[(y * 32) + x] = x < 16
                        ? unchecked((int)0xFF000000)
                        : unchecked((int)0xFFFFFFFF);
                }
            }

            Assert.Equal(0.0, PaintabilityMetrics.FractionInRegionsSmallerThan(pixels, 32, 32, 32, 16));
            Assert.Equal(2, PaintabilityMetrics.CountRegions(pixels, 32, 32, 32));
        }

        /// <summary>
        /// Connectivity is four-way, not eight-way. Two diagonally touching pixels are
        /// separate marks — a brush cannot make a corner-to-corner join — so the metric
        /// must count them separately or it would under-report speckle.
        /// </summary>
        [Fact]
        public void DiagonalNeighboursAreSeparateRegions()
        {
            int[] pixels = Fill(4, 4, unchecked((int)0xFF000000));
            pixels[(1 * 4) + 1] = unchecked((int)0xFFFFFFFF);
            pixels[(2 * 4) + 2] = unchecked((int)0xFFFFFFFF);

            Assert.Equal(3, PaintabilityMetrics.CountRegions(pixels, 4, 4, 4));
            Assert.Equal(2.0 / 16.0, PaintabilityMetrics.FractionInRegionsSmallerThan(pixels, 4, 4, 4, 2), 6);
        }

        /// <summary>
        /// Alpha varies independently of the colour a painter would mix, so it must not
        /// split a region that is one flat colour.
        /// </summary>
        [Fact]
        public void AlphaDoesNotSplitRegions()
        {
            int[] pixels = Fill(8, 8, unchecked((int)0xFF204060));
            pixels[0] = unchecked((int)0x80204060);

            Assert.Equal(1, PaintabilityMetrics.CountRegions(pixels, 8, 8, 8));
        }

        /// <summary>
        /// Row padding is real on locked bitmap data, so the metric must read by stride
        /// and never assume rows are contiguous.
        /// </summary>
        [Fact]
        public void PaddingBetweenRowsIsIgnored()
        {
            const int width = 5;
            const int stride = 8;
            var pixels = new int[stride * 5];
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < stride; x++)
                {
                    // The padding carries a colour found nowhere in the image, so a
                    // metric that read it would report extra regions.
                    pixels[(y * stride) + x] = x < width
                        ? unchecked((int)0xFF112233)
                        : unchecked((int)0xFFAABBCC);
                }
            }

            Assert.Equal(1, PaintabilityMetrics.CountRegions(pixels, stride, width, 5));
        }

        private static int[] Fill(int width, int height, int argb)
        {
            var pixels = new int[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = argb;
            }

            return pixels;
        }
    }
}
