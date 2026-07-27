using System;
using System.Drawing;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the gamut mapper. Real paints routinely fall outside sRGB — cadmium
    /// yellow's measured b* alone exceeds anything a screen represents — so how a colour
    /// is brought back inside decides what the user sees for the most saturated paints
    /// in the palette.
    /// </summary>
    public class GamutMapperTests
    {
        /// <summary>
        /// Confirms a colour already inside the gamut is passed through untouched, so
        /// mapping costs nothing and changes nothing for the ordinary case.
        /// </summary>
        [Theory]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(1.0, 1.0, 1.0)]
        [InlineData(0.2, 0.5, 0.8)]
        public void InGamutColoursArePassedThroughUnchanged(double r, double g, double b)
        {
            Color mapped = GamutMapper.ToDisplayColor(r, g, b, out double chromaLost);

            Assert.Equal(0.0, chromaLost);
            Assert.Equal((int)Math.Round(ColorSpace.LinearToSrgb(r) * 255.0), mapped.R);
            Assert.Equal((int)Math.Round(ColorSpace.LinearToSrgb(g) * 255.0), mapped.G);
            Assert.Equal((int)Math.Round(ColorSpace.LinearToSrgb(b) * 255.0), mapped.B);
        }

        /// <summary>
        /// Confirms an out-of-gamut colour comes back inside the gamut, reports that it
        /// was compressed, and keeps its hue. Hue is the property a per-channel clamp
        /// destroys: clamping a saturated yellow's negative blue channel to zero drags
        /// it toward white unevenly and changes which colour it claims to be.
        /// </summary>
        // Every vector here must be out of gamut by chroma alone, with an Oklab
        // lightness inside [0, 1]. A colour brighter than white has no in-gamut point at
        // any chroma, so it compresses all the way to achromatic and has no hue left to
        // preserve; that path is covered by ImpossibleLightnessStillProducesALegalColour.
        [Theory]
        [InlineData(1.2, 0.8, -0.3)]
        [InlineData(-0.3, 0.9, 1.4)]
        [InlineData(1.2, -0.2, 0.6)]
        public void OutOfGamutColoursAreCompressedAtConstantHue(double r, double g, double b)
        {
            ColorSpace.LinearRgbToOklab(r, g, b, out _, out double aBefore, out double bBefore);
            double hueBefore = Math.Atan2(bBefore, aBefore);

            GamutMapper.Compress(r, g, b,
                out double mappedR, out double mappedG, out double mappedB, out double chromaLost);

            Assert.True(chromaLost > 0.0, "an out-of-gamut colour must report compression");
            Assert.True(GamutMapper.IsInGamut(mappedR, mappedG, mappedB), "result must be in gamut");

            ColorSpace.LinearRgbToOklab(mappedR, mappedG, mappedB,
                out _, out double aAfter, out double bAfter);
            double hueAfter = Math.Atan2(bAfter, aAfter);

            Assert.InRange(hueAfter, hueBefore - 1e-4, hueBefore + 1e-4);
        }

        /// <summary>
        /// Confirms compression stops as soon as the colour is representable, rather
        /// than desaturating further than it has to. A mapper that overshoots makes
        /// every saturated paint look chalky.
        /// </summary>
        [Fact]
        public void CompressionKeepsAsMuchChromaAsTheGamutAllows()
        {
            // Out of gamut by chroma with an in-range lightness, for the same reason the
            // hue theory above needs one: a colour that collapses to achromatic would
            // satisfy this assertion without the search having done anything.
            GamutMapper.Compress(1.2, 0.8, -0.3,
                out double mappedR, out double mappedG, out double mappedB, out _);

            ColorSpace.LinearRgbToOklab(mappedR, mappedG, mappedB,
                out double lightness, out double aAxis, out double bAxis);

            // A few percent more chroma along the same hue must leave the gamut,
            // otherwise the search stopped early.
            ColorSpace.OklabToLinearRgb(lightness, aAxis * 1.05, bAxis * 1.05,
                out double moreR, out double moreG, out double moreB);

            Assert.False(GamutMapper.IsInGamut(moreR, moreG, moreB));
        }

        /// <summary>
        /// Confirms a colour brighter than the display can show still yields a legal
        /// colour rather than an overflowed byte. Lightness outside the representable
        /// range has no in-gamut point at any chroma, so it must be clamped before the
        /// chroma search rather than during it.
        /// </summary>
        [Fact]
        public void ImpossibleLightnessStillProducesALegalColour()
        {
            Color mapped = GamutMapper.ToDisplayColor(4.0, 4.0, 4.0, out double chromaLost);

            Assert.Equal(Color.FromArgb(255, 255, 255, 255).ToArgb(), mapped.ToArgb());
            Assert.True(chromaLost >= 0.0);
        }
    }
}
