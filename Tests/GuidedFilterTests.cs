using System;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the edge-preserving pre-map filter. Its whole reason to exist is doing two
    /// things a Gaussian cannot do at once — flattening noise while leaving edges
    /// sharp — so the tests measure both against a Gaussian of the same radius rather
    /// than checking the filter merely runs.
    /// </summary>
    public class GuidedFilterTests
    {
        /// <summary>
        /// A zero radius must leave the buffer byte-for-byte alone, so the lowest
        /// setting means "off" rather than "almost off".
        /// </summary>
        [Fact]
        public void ZeroRadiusLeavesEveryPixelUntouched()
        {
            int[] pixels = BuildNoisyFlatField(32, 32, unchecked((int)0xFF808080), 6, 1);
            var original = (int[])pixels.Clone();

            GuidedFilter.Apply(pixels, 32, 32, 32, 0, GuidedFilter.DefaultEdgeThreshold, 1);

            Assert.Equal(original, pixels);
        }

        /// <summary>
        /// Uniform colour must survive untouched right up to the border. This catches a
        /// box filter that normalises by the nominal window size rather than by how many
        /// samples it actually had, which would darken every edge of the image.
        /// </summary>
        [Fact]
        public void UniformColourSurvivesIncludingAtTheBorder()
        {
            const int size = 24;
            var pixels = new int[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = unchecked((int)0xFF4080C0);
            }

            GuidedFilter.Apply(pixels, size, size, size, 4, GuidedFilter.DefaultEdgeThreshold, 1);

            foreach (int pixel in pixels)
            {
                Assert.Equal(unchecked((int)0xFF4080C0), pixel);
            }
        }

        /// <summary>
        /// The point of the filter: noise on a flat field must collapse. Measured as the
        /// standard deviation of the red channel, which starts near 6 codes and must end
        /// far below that.
        /// </summary>
        [Fact]
        public void NoiseOnAFlatFieldIsSuppressed()
        {
            int[] pixels = BuildNoisyFlatField(64, 64, unchecked((int)0xFF808080), 6, 1);
            double before = StandardDeviationOfRed(pixels, 64, 64, 64);

            GuidedFilter.Apply(pixels, 64, 64, 64, 4, GuidedFilter.DefaultEdgeThreshold, 1);
            double after = StandardDeviationOfRed(pixels, 64, 64, 64);

            Assert.True(before > 4.0, $"test setup is wrong: input noise was only {before:F2}");
            Assert.True(after < before / 3.0, $"noise went from {before:F2} to {after:F2}, not suppressed enough");
        }

        /// <summary>
        /// The property a Gaussian cannot match: a hard edge must stay hard. The same
        /// radius of Gaussian is run on an identical buffer for comparison, and the
        /// guided filter must preserve substantially more of the step.
        /// </summary>
        [Fact]
        public void AHardEdgeSurvivesFarBetterThanUnderAGaussian()
        {
            const int width = 64;
            int[] guided = BuildVerticalStep(width, 16);
            int[] gaussian = (int[])guided.Clone();

            GuidedFilter.Apply(guided, width, width, 16, 4, GuidedFilter.DefaultEdgeThreshold, 1);
            GaussianBlur.Apply(gaussian, width, width, 16, 4);

            double guidedStep = StepSharpness(guided, width, width, 16);
            double gaussianStep = StepSharpness(gaussian, width, width, 16);

            Assert.True(guidedStep > 0.85, $"guided filter softened the edge to {guidedStep:F3} of its height");
            Assert.True(gaussianStep < 0.6, $"test is not discriminating: the Gaussian left {gaussianStep:F3}");
        }

        /// <summary>
        /// Iterating flattens further. The styles that want planes rather than gradients
        /// turn this up rather than reaching for a second algorithm, so more iterations
        /// must monotonically reduce residual variation.
        /// </summary>
        [Fact]
        public void MoreIterationsFlattenFurther()
        {
            int[] once = BuildNoisyFlatField(64, 64, unchecked((int)0xFF808080), 6, 1);
            int[] thrice = (int[])once.Clone();

            GuidedFilter.Apply(once, 64, 64, 64, 4, GuidedFilter.DefaultEdgeThreshold, 1);
            GuidedFilter.Apply(thrice, 64, 64, 64, 4, GuidedFilter.DefaultEdgeThreshold, 3);

            Assert.True(
                StandardDeviationOfRed(thrice, 64, 64, 64) < StandardDeviationOfRed(once, 64, 64, 64),
                "three iterations left more variation than one");
        }

        /// <summary>
        /// Alpha is not a colour channel and must come back exactly as it went in.
        /// </summary>
        [Fact]
        public void AlphaIsPreservedExactly()
        {
            var pixels = new int[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (((i * 7) % 256) << 24) | 0x00405060;
            }

            var expected = new int[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                expected[i] = pixels[i] & unchecked((int)0xFF000000);
            }

            GuidedFilter.Apply(pixels, 16, 16, 16, 3, GuidedFilter.DefaultEdgeThreshold, 1);

            for (int i = 0; i < pixels.Length; i++)
            {
                Assert.Equal(expected[i], pixels[i] & unchecked((int)0xFF000000));
            }
        }

        /// <summary>
        /// Builds a flat colour with deterministic pseudo-random noise added to every
        /// channel.
        /// </summary>
        private static int[] BuildNoisyFlatField(int width, int height, int argb, double sigma, int seed)
        {
            var rng = new Random(seed);
            var pixels = new int[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                int r = Math.Clamp((int)Math.Round(((argb >> 16) & 0xFF) + (Gauss(rng) * sigma)), 0, 255);
                int g = Math.Clamp((int)Math.Round(((argb >> 8) & 0xFF) + (Gauss(rng) * sigma)), 0, 255);
                int b = Math.Clamp((int)Math.Round((argb & 0xFF) + (Gauss(rng) * sigma)), 0, 255);
                pixels[i] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
            }

            return pixels;
        }

        private static double Gauss(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();

            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>
        /// Builds a buffer that is black on the left half and white on the right.
        /// </summary>
        private static int[] BuildVerticalStep(int width, int height)
        {
            var pixels = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[(y * width) + x] = x < width / 2
                        ? unchecked((int)0xFF000000)
                        : unchecked((int)0xFFFFFFFF);
                }
            }

            return pixels;
        }

        /// <summary>
        /// Measures how much of a black-to-white step survives, as the linear-light
        /// difference across the two pixels either side of the boundary, divided by the
        /// full step of 1.0 it started with.
        /// </summary>
        private static double StepSharpness(int[] pixels, int strideInts, int width, int height)
        {
            int middle = width / 2;
            double total = 0.0;
            for (int y = 0; y < height; y++)
            {
                int row = y * strideInts;
                double left = ColorSpace.SrgbToLinear(((pixels[row + middle - 1] >> 16) & 0xFF) / 255.0);
                double right = ColorSpace.SrgbToLinear(((pixels[row + middle] >> 16) & 0xFF) / 255.0);
                total += right - left;
            }

            return total / height;
        }

        private static double StandardDeviationOfRed(int[] pixels, int strideInts, int width, int height)
        {
            double sum = 0.0;
            double sumOfSquares = 0.0;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double value = (pixels[(y * strideInts) + x] >> 16) & 0xFF;
                    sum += value;
                    sumOfSquares += value * value;
                    count++;
                }
            }

            double mean = sum / count;

            return Math.Sqrt(Math.Max((sumOfSquares / count) - (mean * mean), 0.0));
        }
    }
}
