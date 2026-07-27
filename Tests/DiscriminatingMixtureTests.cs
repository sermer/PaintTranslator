using System;
using System.Linq;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// The mixtures whose behaviour distinguishes a physically correct model from a
    /// self-consistent wrong one. Aggregate error against arbitrary screen colours does
    /// not: the reconstructed pipeline scored *better* on that measure while being
    /// unable to mix blue and yellow into green, because it built its spectra from sRGB
    /// and was therefore fitted to hit arbitrary colours.
    /// </summary>
    public class DiscriminatingMixtureTests
    {
        /// <summary>
        /// Confirms every blue mixed with every yellow lands on the green side of
        /// neutral. This is the direct regression test for the reported bug: the
        /// reconstructed pipeline produced no green anywhere in this matrix.
        /// </summary>
        /// <param name="blueName">The blue paint's name.</param>
        /// <param name="yellowName">The yellow paint's name.</param>
        /// <param name="yellowShare">The yellow's share of the mixture.</param>
        [Theory]
        [InlineData("Ultramarine Blue", "Diarylide Yellow", 0.4)]
        [InlineData("Ultramarine Blue", "Hansa Yellow Opaque", 0.5)]
        [InlineData("Ultramarine Blue", "Bismuth Vanadate Yellow", 0.6)]
        [InlineData("Cobalt Blue", "Diarylide Yellow", 0.4)]
        [InlineData("Cobalt Blue", "Hansa Yellow Opaque", 0.5)]
        [InlineData("Cobalt Blue", "Bismuth Vanadate Yellow", 0.6)]
        [InlineData("Phthalo Blue (R.S.)", "Diarylide Yellow", 0.4)]
        [InlineData("Phthalo Blue (R.S.)", "Hansa Yellow Opaque", 0.5)]
        [InlineData("Phthalo Blue (R.S.)", "Bismuth Vanadate Yellow", 0.6)]
        [InlineData("Phthalo Blue (G.S.)", "Diarylide Yellow", 0.4)]
        [InlineData("Phthalo Blue (G.S.)", "Hansa Yellow Opaque", 0.5)]
        [InlineData("Phthalo Blue (G.S.)", "Bismuth Vanadate Yellow", 0.6)]
        [InlineData("Cerulean Blue, Chromium", "Diarylide Yellow", 0.4)]
        [InlineData("Cerulean Blue, Chromium", "Hansa Yellow Opaque", 0.5)]
        [InlineData("Cerulean Blue, Chromium", "Bismuth Vanadate Yellow", 0.6)]
        public void EveryBlueMixedWithEveryYellowIsGreen(
            string blueName, string yellowName, double yellowShare)
        {
            Lab(blueName, yellowName, yellowShare, out _, out double aStar, out double bStar);

            Assert.True(aStar < 0.0, $"{blueName} + {yellowName} gave a* {aStar:F1}, not a green");
            Assert.True(bStar > 0.0, $"{blueName} + {yellowName} gave b* {bStar:F1}, not a green");
        }

        /// <summary>
        /// Confirms the specific pairing that first exposed the bug is not merely green
        /// but strongly green. Cadmium Yellow Medium is in the reflectance-derived tier
        /// and not yet selectable, so this uses the measured yellows, which are the ones
        /// a user can actually pick.
        /// </summary>
        [Fact]
        public void PhthaloBlueAndYellowIsVividlyGreen()
        {
            Lab("Phthalo Blue (G.S.)", "Diarylide Yellow", 0.5,
                out _, out double aStar, out double bStar);

            Assert.True(aStar < -15.0, $"a* was {aStar:F1}, expected a strongly green mixture");
            Assert.True(bStar > 5.0, $"b* was {bStar:F1}, expected a yellow-leaning green");
        }

        /// <summary>
        /// Confirms mixing complements loses chroma. This is the muddiness a painter
        /// expects and the reconstructed pipeline could not reproduce — it created
        /// chroma on 930 of 20,000 random pairs.
        /// </summary>
        /// <param name="redName">The red paint's name.</param>
        /// <param name="greenName">The green paint's name.</param>
        [Theory]
        [InlineData("C.P. Cadmium Red Light", "Phthalo Green (B.S.)")]
        [InlineData("Pyrrole Red", "Phthalo Green (Y.S.)")]
        public void MixingComplementsLosesChroma(string redName, string greenName)
        {
            double redChroma = Chroma(redName);
            double greenChroma = Chroma(greenName);

            Lab(redName, greenName, 0.5, out _, out double aStar, out double bStar);
            double mixedChroma = Math.Sqrt((aStar * aStar) + (bStar * bStar));

            Assert.True(
                mixedChroma < Math.Min(redChroma, greenChroma),
                $"{redName} + {greenName} kept chroma {mixedChroma:F1} against parents " +
                $"{redChroma:F1} and {greenChroma:F1}");
        }

        /// <summary>
        /// Confirms phthalo blue rotates from a violet-leaning mass tone to a cyan tint.
        /// Nothing but measured scattering produces this: a model that guesses scattering
        /// from luminance renders the tint as a washed-out version of the mass tone,
        /// with the same hue.
        /// </summary>
        [Fact]
        public void PhthaloBlueRotatesFromVioletMassToneToCyanTint()
        {
            var reflectance = new double[SpectralBands.Count];
            PigmentCoefficients blue = Paint("Phthalo Blue (G.S.)");
            PigmentCoefficients white = Paint("Titanium White");

            KubelkaMunk.Mix(new[] { blue }, new[] { 1.0 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out _, out double massToneA, out _);

            KubelkaMunk.Mix(new[] { blue, white }, new[] { 1.0, 10.0 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out _, out double tintA, out _);

            Assert.True(massToneA > 0.0, $"mass tone a* was {massToneA:F1}, expected violet-leaning");
            Assert.True(tintA < 0.0, $"tint a* was {tintA:F1}, expected cyan-leaning");
        }

        /// <summary>
        /// Confirms white and black mix along the neutral axis and monotonically in
        /// lightness. This sweep is where the old reflectance floor of 1e-15 governed
        /// black mixing through an accidental cancellation, with no reference available
        /// to judge it against; measured coefficients supply that reference.
        /// </summary>
        [Fact]
        public void WhiteAndBlackMixNeutrally()
        {
            var reflectance = new double[SpectralBands.Count];
            PigmentCoefficients white = Paint("Titanium White");
            PigmentCoefficients black = Paint("Bone Black");
            double previous = double.PositiveInfinity;

            for (int step = 0; step <= 20; step++)
            {
                double share = step / 20.0;
                KubelkaMunk.Mix(new[] { white, black }, new[] { 1.0 - share, share }, reflectance);
                SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

                Assert.InRange(a, -5.0, 5.0);
                Assert.InRange(b, -5.0, 5.0);
                Assert.True(l <= previous + 1e-6, $"lightness rose at {share:F2} black");
                previous = l;
            }
        }

        /// <summary>
        /// Renders a two-paint mixture to CIELAB.
        /// </summary>
        /// <param name="firstName">The first paint's name.</param>
        /// <param name="secondName">The second paint's name.</param>
        /// <param name="secondShare">The second paint's share of the mixture.</param>
        /// <param name="lightness">The resulting L*.</param>
        /// <param name="aStar">The resulting a*.</param>
        /// <param name="bStar">The resulting b*.</param>
        private static void Lab(
            string firstName, string secondName, double secondShare,
            out double lightness, out double aStar, out double bStar)
        {
            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(
                new[] { Paint(firstName), Paint(secondName) },
                new[] { 1.0 - secondShare, secondShare },
                reflectance);

            SpectralRenderer.ToLab(reflectance, out lightness, out aStar, out bStar);
        }

        /// <summary>
        /// Renders a paint's mass tone and returns its chroma.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The mass tone's C*.</returns>
        private static double Chroma(string name)
        {
            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(new[] { Paint(name) }, new[] { 1.0 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out _, out double aStar, out double bStar);

            return Math.Sqrt((aStar * aStar) + (bStar * bStar));
        }

        /// <summary>
        /// Looks a paint up by name.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }
    }
}
