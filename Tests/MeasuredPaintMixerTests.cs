using System;
using System.Drawing;
using PaintTranslator.Data;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Covers mixing driven by spectrophotometer measurements rather than by spectra
    /// reconstructed from a paint's sRGB value. Each assertion here is a behaviour real
    /// acrylics have and the reconstructed model demonstrably does not, so together they
    /// are the case for the measured path existing at all.
    /// </summary>
    public class MeasuredPaintMixerTests
    {
        /// <summary>
        /// Confirms the palette carries the full measured set, so a gap in the data is
        /// caught here rather than showing up as a missing paint in the picker.
        /// </summary>
        [Fact]
        public void ProvidesEveryMeasuredPaint()
        {
            Assert.Equal(19, MeasuredPalette.Paints.Count);
        }

        /// <summary>
        /// Confirms titanium white's mass tone reproduces Golden's own published
        /// measurement of L* 98.25. The paint data and the manufacturer's chart are
        /// independent sources, so agreement between them is a genuine check that the
        /// spectra are being integrated correctly rather than merely self-consistently.
        /// </summary>
        [Fact]
        public void ReproducesTheManufacturersPublishedLightnessForTitaniumWhite()
        {
            Color masstone = MeasuredPaintMixer.Mix(
                new[] { Find("Titanium White") }, new[] { 1.0 });

            PalettePhotoConverter.RgbToLab(masstone.R, masstone.G, masstone.B,
                out double lightness, out _, out _);

            Assert.InRange(lightness, 97.0, 99.5);
        }

        /// <summary>
        /// Confirms phthalo blue tints toward a brilliant cyan. This is the mass tone and
        /// undertone split that makes a single colour per paint insufficient: phthalo blue
        /// is nearly black from the tube, and mixed into white it becomes one of the most
        /// intense cyans available. Reproducing it requires knowing how little the pigment
        /// scatters relative to white, which is exactly what the measured scattering
        /// coefficient supplies and what a paint's sRGB value cannot imply.
        /// </summary>
        [Fact]
        public void TintsPhthaloBlueTowardBrilliantCyan()
        {
            Color tint = MeasuredPaintMixer.Mix(
                new[] { Find("Phthalo Blue (G.S.)"), Find("Titanium White") },
                new[] { 1.0, 20.0 });

            PalettePhotoConverter.RgbToLab(tint.R, tint.G, tint.B,
                out double lightness, out double a, out double b);

            Assert.True(a < -10.0, $"expected a green-cyan bias (a* < -10), got a* = {a:F1}");
            Assert.True(b < -10.0, $"expected a blue bias (b* < -10), got b* = {b:F1}");
            Assert.True(lightness > 55.0, $"expected a light tint (L* > 55), got L* = {lightness:F1}");
        }

        /// <summary>
        /// Confirms a small amount of white does not overwhelm a strong dark paint. With
        /// luminance standing in for scattering, white carries roughly twenty-four times
        /// the pull of a dark paint and one part in twenty visibly lifts the mixture;
        /// measured scattering puts white's influence where it belongs.
        /// </summary>
        [Fact]
        public void KeepsWhiteFromOverwhelmingAStrongDarkPaint()
        {
            Color mostlyBlack = MeasuredPaintMixer.Mix(
                new[] { Find("Titanium White"), Find("Bone Black") },
                new[] { 1.0, 20.0 });

            PalettePhotoConverter.RgbToLab(mostlyBlack.R, mostlyBlack.G, mostlyBlack.B,
                out double lightness, out _, out _);

            Assert.True(lightness < 35.0, $"expected one part white in twenty to stay dark, got L* = {lightness:F1}");
        }

        /// <summary>
        /// Confirms ultramarine and a warm yellow mix to a definite green, the classic
        /// check that a mixing model is spectral rather than additive.
        /// </summary>
        [Fact]
        public void MixesUltramarineAndYellowToGreen()
        {
            Color green = MeasuredPaintMixer.Mix(
                new[] { Find("Ultramarine Blue"), Find("Hansa Yellow Opaque") },
                new[] { 1.0, 1.0 });

            PalettePhotoConverter.RgbToLab(green.R, green.G, green.B,
                out _, out double a, out double b);

            Assert.True(a < -15.0, $"expected a strong green bias (a* < -15), got a* = {a:F1}");
            Assert.True(b > 5.0, $"expected a warm green (b* > 5), got b* = {b:F1}");
        }

        /// <summary>
        /// Confirms mixing a paint with itself returns its own mass tone at any ratio.
        /// </summary>
        [Fact]
        public void ReturnsTheMassToneWhenMixingAPaintWithItself()
        {
            MeasuredPaint ultramarine = Find("Ultramarine Blue");

            Color alone = MeasuredPaintMixer.Mix(new[] { ultramarine }, new[] { 1.0 });
            Color withItself = MeasuredPaintMixer.Mix(
                new[] { ultramarine, ultramarine }, new[] { 1.0, 3.0 });

            Assert.Equal(alone.ToArgb(), withItself.ToArgb());
        }

        /// <summary>
        /// Confirms weights are treated as relative shares, so scaling a whole recipe
        /// changes nothing. Recipes arrive as whole parts of varying totals, and a mixer
        /// that responded to the total rather than the ratio would give a different colour
        /// for 1:1 than for 2:2.
        /// </summary>
        [Fact]
        public void TreatsWeightsAsRelativeShares()
        {
            MeasuredPaint white = Find("Titanium White");
            MeasuredPaint black = Find("Bone Black");

            Color small = MeasuredPaintMixer.Mix(new[] { white, black }, new[] { 1.0, 3.0 });
            Color scaled = MeasuredPaintMixer.Mix(new[] { white, black }, new[] { 5.0, 15.0 });

            Assert.Equal(small.ToArgb(), scaled.ToArgb());
        }

        /// <summary>
        /// Confirms an empty recipe is rejected rather than silently producing a colour.
        /// </summary>
        [Fact]
        public void RejectsAnEmptyRecipe()
        {
            Assert.Throws<ArgumentException>(
                () => MeasuredPaintMixer.Mix(Array.Empty<MeasuredPaint>(), Array.Empty<double>()));
        }

        /// <summary>
        /// Looks up a measured paint by its manufacturer name.
        /// </summary>
        /// <param name="name">The paint's name in the measured palette.</param>
        /// <returns>The matching paint.</returns>
        private static MeasuredPaint Find(string name)
        {
            foreach (MeasuredPaint paint in MeasuredPalette.Paints)
            {
                if (paint.Name == name)
                {
                    return paint;
                }
            }

            throw new InvalidOperationException($"No measured paint named '{name}'.");
        }
    }
}
