using System;
using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the pigment-mixing behaviour that distinguishes a spectral mixer from a
    /// channel-wise average. Every assertion here is chosen so that replacing
    /// <see cref="SubtractivePaintMixer"/> with a linear RGB blend would break it:
    /// that is the regression these tests exist to catch, because a channel average
    /// produces plausible-looking colours that are wrong in exactly the ways paint
    /// is not. Paint colours are Golden Heavy Body mass tones.
    /// </summary>
    public class SubtractivePaintMixerTests
    {
        private static readonly Color Ultramarine = Color.FromArgb(50, 47, 75);
        private static readonly Color CadmiumYellowMedium = Color.FromArgb(255, 200, 0);
        private static readonly Color PhthaloBlueGreenShade = Color.FromArgb(16, 3, 62);
        private static readonly Color HansaYellowLight = Color.FromArgb(250, 226, 0);
        private static readonly Color PyrroleRed = Color.FromArgb(187, 0, 0);
        private static readonly Color PermanentGreenLight = Color.FromArgb(0, 118, 75);

        /// <summary>
        /// Confirms blue and yellow mix toward green rather than toward grey. This is the
        /// single behaviour that justifies the spectral model existing: ultramarine's
        /// reflectance keeps a green shoulder even though its sRGB green channel is low,
        /// and that shoulder is what survives the mix. A linear RGB average of these two
        /// paints lands at a* = +5.4, on the magenta side of neutral, so requiring a
        /// negative a* fails immediately if the spectral path is bypassed.
        /// </summary>
        [Fact]
        public void MixesBlueAndYellowTowardGreenRatherThanGrey()
        {
            Color mixed = SubtractivePaintMixer.Mix(Ultramarine, CadmiumYellowMedium, 0.5);

            PalettePhotoConverter.RgbToLab(mixed.R, mixed.G, mixed.B, out _, out double a, out double b);

            Assert.True(a < -3.0, $"expected a green bias (a* < -3), got a* = {a:F1}");
            Assert.True(b > 20.0, $"expected a yellow bias (b* > 20), got b* = {b:F1}");
        }

        /// <summary>
        /// Confirms a blue-plus-yellow mix is markedly darker than the same blend computed
        /// as a linear RGB average. Subtractive mixing loses light that additive averaging
        /// keeps, and that loss is the other half of behaving like paint. Phthalo blue is
        /// the sharpest case because its mass tone is nearly black.
        /// </summary>
        [Fact]
        public void MixesDarkerThanALinearRgbAverage()
        {
            Color mixed = SubtractivePaintMixer.Mix(PhthaloBlueGreenShade, HansaYellowLight, 0.5);
            Color averaged = AverageInLinearRgb(PhthaloBlueGreenShade, HansaYellowLight);

            PalettePhotoConverter.RgbToLab(mixed.R, mixed.G, mixed.B, out double mixedL, out _, out _);
            PalettePhotoConverter.RgbToLab(averaged.R, averaged.G, averaged.B, out double averagedL, out _, out _);

            Assert.True(
                mixedL < averagedL - 5.0,
                $"expected the subtractive mix to be at least 5 L* darker than the average, got {mixedL:F1} vs {averagedL:F1}");
        }

        /// <summary>
        /// Confirms complementary paints mix to a dull chromatic colour rather than to
        /// black or to a saturated intermediate. Red and green absorb across most of the
        /// spectrum between them, so little is left to reflect; the result must lose most
        /// of the chroma of both parents while keeping enough lightness to be a colour
        /// rather than a hole in the painting.
        /// </summary>
        [Fact]
        public void MixesComplementariesToDullChromaWithoutGoingBlack()
        {
            Color mixed = SubtractivePaintMixer.Mix(PyrroleRed, PermanentGreenLight, 0.5);

            double mixedChroma = ChromaOf(mixed);
            double redChroma = ChromaOf(PyrroleRed);
            double greenChroma = ChromaOf(PermanentGreenLight);
            PalettePhotoConverter.RgbToLab(mixed.R, mixed.G, mixed.B, out double lightness, out _, out _);

            Assert.True(
                mixedChroma < Math.Min(redChroma, greenChroma) / 2.0,
                $"expected chroma below half the duller parent ({Math.Min(redChroma, greenChroma) / 2.0:F1}), got {mixedChroma:F1}");
            Assert.True(lightness > 5.0, $"expected a colour rather than black, got L* = {lightness:F1}");
        }

        /// <summary>
        /// Confirms mixing a paint with itself returns that paint at every ratio, which
        /// exercises the full sRGB to spectrum to Kubelka-Munk to sRGB round trip. Any
        /// drift in the reflectance reconstruction, the observer tables, or the gamut
        /// handling shows up here as an off-by-one channel, so this is the cheapest
        /// guard against a numerically broken pipeline.
        /// </summary>
        /// <param name="weightOfB">The share of the second paint in the mix.</param>
        [Theory]
        [InlineData(0.0)]
        [InlineData(0.125)]
        [InlineData(0.5)]
        [InlineData(0.75)]
        [InlineData(1.0)]
        public void ReturnsTheSameColorWhenMixingAPaintWithItself(double weightOfB)
        {
            // A fixed seed keeps a failure reproducible; the sweep is wide enough to
            // catch reconstruction errors confined to one region of the cube.
            var random = new Random(20260726);
            for (int i = 0; i < 512; i++)
            {
                Color paint = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));

                Color mixed = SubtractivePaintMixer.Mix(paint, paint, weightOfB);

                Assert.Equal(paint.ToArgb(), mixed.ToArgb());
            }
        }

        /// <summary>
        /// Confirms that asking for three parts of a paint is the same as adding three
        /// single parts of it. This is what a mixing ratio means on a palette, and it holds
        /// only when a weight is used as a concentration directly. Squaring the weights
        /// first — as spectral.js does to ease its gradients — breaks it, because three
        /// parts then carries nine parts of influence while three separate parts still
        /// carry three. The recipes this app prints are ratios, so this equivalence is the
        /// property that makes them mean anything.
        /// </summary>
        [Fact]
        public void TreatsThreePartsOfAPaintAsThreeSingleParts()
        {
            PaintSpectrum blue = SubtractivePaintMixer.ToSpectrum(Ultramarine);
            PaintSpectrum yellow = SubtractivePaintMixer.ToSpectrum(CadmiumYellowMedium);

            Color asRatio = SubtractivePaintMixer.Mix(
                new[] { blue, yellow },
                new[] { 1.0, 3.0 });
            Color asSeparateParts = SubtractivePaintMixer.Mix(
                new[] { blue, yellow, yellow, yellow },
                new[] { 1.0, 1.0, 1.0, 1.0 });

            Assert.Equal(asSeparateParts.ToArgb(), asRatio.ToArgb());
        }

        /// <summary>
        /// Blends two colours channel-wise in linear light, the additive model these tests
        /// measure the spectral mixer against.
        /// </summary>
        /// <param name="a">The first colour.</param>
        /// <param name="b">The second colour.</param>
        /// <returns>The equal-parts linear-light average of the two colours.</returns>
        private static Color AverageInLinearRgb(Color a, Color b)
        {
            return Color.FromArgb(
                AverageChannel(a.R, b.R),
                AverageChannel(a.G, b.G),
                AverageChannel(a.B, b.B));
        }

        /// <summary>
        /// Averages one channel of two sRGB colours in linear light.
        /// </summary>
        /// <param name="first">The first colour's channel value.</param>
        /// <param name="second">The second colour's channel value.</param>
        /// <returns>The sRGB-encoded average of the two channel values.</returns>
        private static int AverageChannel(int first, int second)
        {
            double linear = (ToLinear(first) + ToLinear(second)) / 2.0;
            double encoded = linear <= 0.0031308
                ? linear * 12.92
                : 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
            return (int)Math.Round(Math.Clamp(encoded, 0.0, 1.0) * 255.0);
        }

        /// <summary>
        /// Decodes an 8-bit sRGB channel to linear light.
        /// </summary>
        /// <param name="channel">The sRGB-encoded channel value.</param>
        /// <returns>The linear-light value of the channel.</returns>
        private static double ToLinear(int channel)
        {
            double c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Measures a colour's CIELAB chroma, the distance of its hue from neutral.
        /// </summary>
        /// <param name="color">The colour to measure.</param>
        /// <returns>The colour's chroma.</returns>
        private static double ChromaOf(Color color)
        {
            PalettePhotoConverter.RgbToLab(color.R, color.G, color.B, out _, out double a, out double b);
            return Math.Sqrt((a * a) + (b * b));
        }
    }
}
