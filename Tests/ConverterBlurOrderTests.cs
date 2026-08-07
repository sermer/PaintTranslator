using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the order <see cref="PalettePhotoConverter.Convert"/> runs its two pre-map
    /// filters in when a caller asks for both: the mandatory
    /// <see cref="GuidedFilter"/> floor first, then the optional
    /// <see cref="GaussianBlur"/> on top of it. This is the order the converter used
    /// before the style pipeline existed (see <c>git show f0bbb3b</c>), and it matters
    /// because the two filters do not commute — blurring first lowers local contrast,
    /// letting real edges fall below the floor's variance threshold and lose the
    /// protection the floor exists to give them, and turns independent sensor noise
    /// into spatially correlated blotches the floor's variance test then reads as
    /// signal. Reversing the order is not a rounding-level difference: it is computed
    /// here independently, pixel for pixel, and compared byte for byte against
    /// <see cref="PalettePhotoConverter.Convert"/>'s actual output.
    /// </summary>
    public class ConverterBlurOrderTests
    {
        /// <summary>
        /// Reproduces floor-then-blur independently of <see cref="PalettePhotoConverter"/>
        /// and checks <see cref="PalettePhotoConverter.Convert"/> agrees exactly.
        /// <para>
        /// Run against the ordering in place before this fix (blur, then floor), this
        /// test failed: 71 of the 4,096 pixels in the 64x64 source differed between
        /// the two orderings (see the fix report for the exact command and output).
        /// </para>
        /// </summary>
        [Fact]
        public void TheMandatoryFloorRunsBeforeTheOptionalBlurNotAfter()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            using Bitmap source = BuildEdgeWithNoise(64, 64);

            const int Mark = 8;
            const int BlurRadius = 6;

            int[] expected = StyleTestFixtures.ReadPixels(source, out int stride);
            int floorRadius = PalettePhotoConverter.FloorRadius(Mark);

            // The order this test insists on: the mandatory floor first, exactly as
            // Convert called it before the style pipeline existed, then the optional
            // blur on top of the floor's own output.
            // The default converter resolves the Realism style, whose research-tuned
            // edge threshold is 0.10 rather than the reusable stage default of 0.05.
            GuidedFilter.Apply(expected, stride, source.Width, source.Height, floorRadius, 0.10, 1);
            GaussianBlur.Apply(expected, stride, source.Width, source.Height, BlurRadius);

            int[] expectedMapped = PalettePhotoConverter.MapThroughIndex(paints, expected);

            using Bitmap converted = PalettePhotoConverter.Convert(source, paints, BlurRadius, Mark);
            int[] actual = StyleTestFixtures.ReadPixels(converted, out int actualStride);

            int mismatches = 0;
            for (int y = 0; y < source.Height; y++)
            {
                int expectedRow = y * stride;
                int actualRow = y * actualStride;
                for (int x = 0; x < source.Width; x++)
                {
                    int expectedArgb = unchecked((int)0xFF000000) | (expectedMapped[expectedRow + x] & 0x00FFFFFF);
                    int actualArgb = actual[actualRow + x];
                    if (expectedArgb != actualArgb)
                    {
                        mismatches++;
                    }
                }
            }

            Assert.True(
                mismatches == 0,
                $"{mismatches} of {source.Width * source.Height} pixels differed from the floor-then-blur " +
                "ordering computed independently here — Convert is not running the floor before the blur.");
        }

        /// <summary>
        /// A hard vertical edge at the image's midline with Gaussian sensor noise on
        /// both sides, so the guided filter's edge-preservation and the blur's
        /// contrast-lowering have something concrete to disagree about depending on
        /// which runs first.
        /// </summary>
        private static Bitmap BuildEdgeWithNoise(int width, int height)
        {
            var rng = new Random(11);
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double baseValue = x < width / 2 ? 40.0 : 220.0;
                    var channel = new int[3];
                    for (int c = 0; c < 3; c++)
                    {
                        double u1 = 1.0 - rng.NextDouble();
                        double u2 = rng.NextDouble();
                        double noisy = baseValue + (3.0 * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
                        channel[c] = Math.Clamp((int)Math.Round(noisy), 0, 255);
                    }

                    bitmap.SetPixel(x, y, Color.FromArgb(255, channel[0], channel[1], channel[2]));
                }
            }

            return bitmap;
        }
    }
}
