using System;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the colour-space conversions every other part of the pigment pipeline is
    /// built on. An error here moves every rendered swatch and every match, so these
    /// assert against published reference values rather than against each other.
    /// </summary>
    public class ColorSpaceTests
    {
        /// <summary>
        /// Confirms the sRGB transfer function round-trips every 8-bit value. The curve
        /// has a linear segment near zero and a power segment above it, and getting the
        /// junction wrong is invisible except in shadows.
        /// </summary>
        [Fact]
        public void SrgbTransferRoundTripsEveryByteValue()
        {
            for (int channel = 0; channel <= 255; channel++)
            {
                double encoded = channel / 255.0;
                double linear = ColorSpace.SrgbToLinear(encoded);
                Assert.InRange(ColorSpace.LinearToSrgb(linear), encoded - 1e-12, encoded + 1e-12);
            }
        }

        /// <summary>
        /// Confirms a perfect white maps to L* 100 on the neutral axis. This is the
        /// anchor for the whole Lab pipeline: if the white point is wrong, every colour
        /// is tinted and nothing else in the suite will say so clearly.
        /// </summary>
        [Fact]
        public void PerfectWhiteIsLightness100OnTheNeutralAxis()
        {
            ColorSpace.LinearRgbToXyz(1.0, 1.0, 1.0, out double x, out double y, out double z);
            ColorSpace.XyzToLab(x, y, z, out double lightness, out double aStar, out double bStar);

            Assert.InRange(lightness, 99.999, 100.001);
            Assert.InRange(aStar, -1e-3, 1e-3);
            Assert.InRange(bStar, -1e-3, 1e-3);
        }

        /// <summary>
        /// Confirms XYZ and linear RGB are exact inverses. The gamut mapper crosses this
        /// boundary twice per binary-search iteration, so drift here compounds.
        /// </summary>
        [Theory]
        [InlineData(0.2, 0.4, 0.6)]
        [InlineData(1.0, 0.0, 0.0)]
        [InlineData(0.0, 0.0, 1.0)]
        [InlineData(0.05, 0.05, 0.05)]
        public void XyzRoundTripsLinearRgb(double r, double g, double b)
        {
            ColorSpace.LinearRgbToXyz(r, g, b, out double x, out double y, out double z);
            ColorSpace.XyzToLinearRgb(x, y, z, out double r2, out double g2, out double b2);

            // The published sRGB matrices are each rounded to seven decimals and are
            // therefore not exact inverses of one another: their product differs from
            // the identity by about 2e-7. The tolerance bounds that rounding rather than
            // the conversion, which is why it is not tighter. At 1e-6 a transposed row
            // or a wrong digit still fails by orders of magnitude, while the residual is
            // three thousandths of one 8-bit code value.
            Assert.InRange(r2, r - 1e-6, r + 1e-6);
            Assert.InRange(g2, g - 1e-6, g + 1e-6);
            Assert.InRange(b2, b - 1e-6, b + 1e-6);
        }

        /// <summary>
        /// Confirms Oklab round-trips linear RGB, including for values outside the
        /// gamut. The gamut mapper starts from out-of-range channels by definition, so a
        /// conversion that only works inside [0,1] would be useless to it.
        /// </summary>
        [Theory]
        [InlineData(0.3, 0.5, 0.7)]
        [InlineData(1.4, 0.2, -0.3)]
        [InlineData(0.0, 0.0, 0.0)]
        public void OklabRoundTripsLinearRgb(double r, double g, double b)
        {
            ColorSpace.LinearRgbToOklab(r, g, b, out double l, out double a, out double bAxis);
            ColorSpace.OklabToLinearRgb(l, a, bAxis, out double r2, out double g2, out double b2);

            // Oklab's published matrix pairs are rounded in the same way the sRGB ones
            // are, and the cube root between them amplifies the residual, so the same
            // 1e-6 bound applies here and for the same reason.
            Assert.InRange(r2, r - 1e-6, r + 1e-6);
            Assert.InRange(g2, g - 1e-6, g + 1e-6);
            Assert.InRange(b2, b - 1e-6, b + 1e-6);
        }

        /// <summary>
        /// Confirms mid-grey lands near L* 53.6, the published value for sRGB 128. This
        /// catches a transfer function applied in the wrong direction, which the
        /// round-trip tests cannot see because they would still be self-consistent.
        /// </summary>
        [Fact]
        public void MidGreyIsAboutLightness53()
        {
            double linear = ColorSpace.SrgbToLinear(128 / 255.0);
            ColorSpace.LinearRgbToXyz(linear, linear, linear, out double x, out double y, out double z);
            ColorSpace.XyzToLab(x, y, z, out double lightness, out _, out _);

            Assert.InRange(lightness, 53.3, 53.9);
        }
    }
}
