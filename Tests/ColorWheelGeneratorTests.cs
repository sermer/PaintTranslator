using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Guards the agreement between the color wheel and the mixer. The wheel inlines
    /// Kubelka-Munk mixing in its per-pixel loop instead of calling
    /// <see cref="SubtractivePaintMixer.Mix(IReadOnlyList{PaintSpectrum}, IReadOnlyList{double})"/>,
    /// because hoisting the all-paints sums out of the loop is what makes generation
    /// fast enough to be interactive. That duplication is the hazard these tests cover:
    /// a change to the mixing rule that misses the wheel leaves the two silently
    /// disagreeing, so the swatch a user picks off the wheel would not be the color the
    /// recipe mixes to.
    /// </summary>
    public class ColorWheelGeneratorTests
    {
        private static readonly IReadOnlyList<Color> Paints = new[]
        {
            Color.FromArgb(255, 200, 0),
            Color.FromArgb(187, 0, 0),
            Color.FromArgb(90, 0, 37),
            Color.FromArgb(50, 47, 75),
            Color.FromArgb(0, 31, 41),
            Color.FromArgb(0, 118, 75),
        };

        /// <summary>
        /// Confirms the wheel's rendered pixels match what the mixer produces from the
        /// weights the wheel reports for those same pixels. Sampling a grid across the
        /// bitmap covers pure rim wedges, the muddy all-paints center, and the graded
        /// region between them.
        /// </summary>
        [Fact]
        public void RendersPixelsThatMatchTheMixerAtTheWeightsItReports()
        {
            const int diameter = 129;

            var spectra = new PaintSpectrum[Paints.Count];
            for (int i = 0; i < Paints.Count; i++)
            {
                spectra[i] = SubtractivePaintMixer.ToSpectrum(Paints[i]);
            }

            using Bitmap wheel = ColorWheelGenerator.Create(diameter, Paints);

            int compared = 0;
            for (int y = 4; y < diameter; y += 7)
            {
                for (int x = 4; x < diameter; x += 7)
                {
                    double[] weights = ColorWheelGenerator.GetBlendWeights(diameter, Paints.Count, x, y);
                    if (weights == null)
                    {
                        continue;
                    }

                    Color expected = SubtractivePaintMixer.Mix(spectra, weights);
                    Color actual = wheel.GetPixel(x, y);
                    compared++;

                    Assert.Equal(
                        (expected.R, expected.G, expected.B),
                        (actual.R, actual.G, actual.B));
                }
            }

            // A silent drop to zero comparisons would make the test vacuous.
            Assert.True(compared > 50, $"expected a meaningful sample of wheel pixels, compared {compared}");
        }
    }
}
