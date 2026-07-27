using System;
using System.Collections.Generic;
using System.Linq;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Properties every mixture must have, whatever the paints are. These run across
    /// the whole library rather than on chosen examples, so they catch a paint whose
    /// coefficients are individually plausible but behave wrongly in company.
    /// </summary>
    public class MixingInvariantTests
    {
        /// <summary>Every paint offered to the user.</summary>
        /// <returns>Each selectable paint's name, one per theory row.</returns>
        public static IEnumerable<object[]> EveryPaint()
        {
            return PigmentLibrary.Selectable.Select(paint => new object[] { paint.Name });
        }

        /// <summary>
        /// Confirms a mixture at an endpoint is exactly the paint at that endpoint. A
        /// model that only approaches its endpoints has an error that grows as
        /// concentrations become uneven, which is precisely where recipes live.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        [Theory]
        [MemberData(nameof(EveryPaint))]
        public void AZeroConcentrationPartnerChangesNothing(string name)
        {
            PigmentCoefficients paint = Paint(name);
            PigmentCoefficients other = Paint(name == "Bone Black" ? "Titanium White" : "Bone Black");

            var alone = new double[SpectralBands.Count];
            var withPartner = new double[SpectralBands.Count];
            KubelkaMunk.Mix(new[] { paint }, new[] { 1.0 }, alone);
            KubelkaMunk.Mix(new[] { paint, other }, new[] { 1.0, 0.0 }, withPartner);

            for (int band = 0; band < SpectralBands.Count; band++)
            {
                Assert.InRange(withPartner[band], alone[band] - 1e-12, alone[band] + 1e-12);
            }
        }

        /// <summary>
        /// Confirms adding titanium white raises lightness monotonically for every
        /// paint. White scatters enormously and absorbs almost nothing, so this must
        /// hold; a failure means scattering is not being carried through the mix.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        [Theory]
        [MemberData(nameof(EveryPaint))]
        public void AddingWhiteRaisesLightnessMonotonically(string name)
        {
            AssertLightnessMonotonic(name, "Titanium White", rising: true);
        }

        /// <summary>
        /// Confirms adding bone black never produces something lighter than the lighter
        /// of the two paints, and lands exactly on black at full strength.
        /// <para>
        /// Deliberately not a monotonicity assertion, unlike the white sweep. Bone black
        /// is PBk9, a real pigment with its own absorption spectrum, not a neutral
        /// darkener — and three paints in the library (Ultramarine Blue at L* 7.4 and
        /// both red-shade and green-shade Phthalo Blue) are darker than it is, so adding
        /// it to them raises lightness. Both phthalo greens start above black and still
        /// dip below it mid-sweep, because two strong absorbers covering complementary
        /// regions leave less light than either alone. That is the same effect the spec
        /// relies on for mixed complements, so forbidding it here would contradict it.
        /// </para>
        /// </summary>
        /// <param name="name">The paint's name.</param>
        [Theory]
        [MemberData(nameof(EveryPaint))]
        public void AddingBlackNeverExceedsTheLighterParent(string name)
        {
            if (name == "Bone Black")
            {
                return;
            }

            PigmentCoefficients paint = Paint(name);
            PigmentCoefficients black = Paint("Bone Black");
            var reflectance = new double[SpectralBands.Count];

            KubelkaMunk.Mix(new[] { paint }, new[] { 1.0 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out double paintLightness, out _, out _);

            KubelkaMunk.Mix(new[] { black }, new[] { 1.0 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out double blackLightness, out _, out _);

            double ceiling = Math.Max(paintLightness, blackLightness);
            double final = double.NaN;

            for (int step = 0; step <= 20; step++)
            {
                double share = step / 20.0;
                KubelkaMunk.Mix(new[] { paint, black }, new[] { 1.0 - share, share }, reflectance);
                SpectralRenderer.ToLab(reflectance, out double lightness, out _, out _);

                Assert.True(
                    lightness <= ceiling + 1e-6,
                    $"{name} + Bone Black reached L* {lightness:F2} at {share:F2}, " +
                    $"above both parents' {ceiling:F2}");

                final = lightness;
            }

            Assert.InRange(final, blackLightness - 1e-6, blackLightness + 1e-6);
        }

        /// <summary>
        /// Confirms every paint renders to a lightness inside the CIELAB range. A value
        /// outside it means the integration or the inversion left its valid regime,
        /// which no downstream code guards against.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        [Theory]
        [MemberData(nameof(EveryPaint))]
        public void EveryPaintRendersToALegalLightness(string name)
        {
            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(new[] { Paint(name) }, new[] { 1.0 }, reflectance);
            SpectralRenderer.ToLab(reflectance, out double lightness, out double a, out double b);

            Assert.InRange(lightness, 0.0, 100.0);
            Assert.True(double.IsFinite(a) && double.IsFinite(b), $"{name} rendered a non-finite chroma");
        }

        /// <summary>
        /// Confirms the mixture moves continuously with concentration, with no jumps.
        /// A discontinuity would show in the colour wheel as a visible seam and in the
        /// recipe search as an optimum the polish step cannot reach.
        /// <para>
        /// Continuity is tested by refinement rather than by a ceiling on the step size,
        /// because a large step and a discontinuity are not the same thing. One percent
        /// of a strong yellow in titanium white really does move the colour by 15 units
        /// of Lab — that is what tinting strength is, and capping the step would fail the
        /// pigments the measured data exists to represent. What separates the two is how
        /// the step behaves as the sweep is refined: a smooth curve's largest step shrinks
        /// with the sampling interval, while a genuine jump keeps its size however finely
        /// it is sampled. Across the whole library the observed ratio is at most 0.81.
        /// </para>
        /// </summary>
        [Fact]
        public void MixturesMoveContinuouslyWithConcentration()
        {
            // Comfortably above the 0.81 seen across every pair, and far below the ~1.0 a
            // real discontinuity would hold at.
            const double RequiredShrinkage = 0.9;

            // Below this the sweep is already seamless and the ratio is dominated by
            // floating-point noise rather than by the shape of the curve.
            const double NegligibleJump = 0.5;

            var paints = PigmentLibrary.Selectable;

            for (int i = 0; i < paints.Count; i++)
            {
                for (int j = i + 1; j < paints.Count; j++)
                {
                    double coarse = WorstJump(paints[i], paints[j], 100);
                    if (coarse < NegligibleJump)
                    {
                        continue;
                    }

                    double fine = WorstJump(paints[i], paints[j], 200);

                    Assert.True(
                        fine < coarse * RequiredShrinkage,
                        $"{paints[i].Name} + {paints[j].Name} jumped {coarse:F1} at 100 steps and " +
                        $"still {fine:F1} at 200, which is a discontinuity rather than a steep curve");
                }
            }
        }

        /// <summary>
        /// Finds the largest distance between consecutive samples of a two-paint sweep.
        /// </summary>
        /// <param name="first">The first paint.</param>
        /// <param name="second">The second paint.</param>
        /// <param name="steps">How many intervals to divide the sweep into.</param>
        /// <returns>The largest CIELAB distance between neighbouring samples.</returns>
        private static double WorstJump(PigmentCoefficients first, PigmentCoefficients second, int steps)
        {
            var reflectance = new double[SpectralBands.Count];
            double previousLightness = double.NaN;
            double previousA = double.NaN;
            double previousB = double.NaN;
            double worst = 0.0;

            for (int step = 0; step <= steps; step++)
            {
                double share = (double)step / steps;
                KubelkaMunk.Mix(new[] { first, second }, new[] { 1.0 - share, share }, reflectance);
                SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

                if (step > 0)
                {
                    worst = Math.Max(worst, Math.Sqrt(
                        ((l - previousLightness) * (l - previousLightness))
                        + ((a - previousA) * (a - previousA))
                        + ((b - previousB) * (b - previousB))));
                }

                previousLightness = l;
                previousA = a;
                previousB = b;
            }

            return worst;
        }

        /// <summary>
        /// Asserts lightness moves in one direction as a modifier is added.
        /// </summary>
        /// <param name="name">The paint being modified.</param>
        /// <param name="modifierName">The white or black being added.</param>
        /// <param name="rising">True when lightness must rise, false when it must fall.</param>
        private static void AssertLightnessMonotonic(string name, string modifierName, bool rising)
        {
            if (name == modifierName)
            {
                return;
            }

            PigmentCoefficients paint = Paint(name);
            PigmentCoefficients modifier = Paint(modifierName);
            var reflectance = new double[SpectralBands.Count];
            double previous = rising ? double.NegativeInfinity : double.PositiveInfinity;

            for (int step = 0; step <= 20; step++)
            {
                double share = step / 20.0;
                KubelkaMunk.Mix(new[] { paint, modifier }, new[] { 1.0 - share, share }, reflectance);
                SpectralRenderer.ToLab(reflectance, out double lightness, out _, out _);

                if (rising)
                {
                    Assert.True(
                        lightness >= previous - 1e-6,
                        $"{name} + {modifierName} fell from L* {previous:F2} to {lightness:F2} at {share:F2}");
                }
                else
                {
                    Assert.True(
                        lightness <= previous + 1e-6,
                        $"{name} + {modifierName} rose from L* {previous:F2} to {lightness:F2} at {share:F2}");
                }

                previous = lightness;
            }
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
