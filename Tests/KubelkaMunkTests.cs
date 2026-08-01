using System;
using System.Collections.Generic;
using System.Linq;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the mixing kernel. Every colour the application shows comes through here,
    /// so these check the algebra directly rather than through a rendered colour, where
    /// an error in the physics and an error in the rendering look identical.
    /// </summary>
    public class KubelkaMunkTests
    {
        /// <summary>The reflectance buffer these tests mix into.</summary>
        private readonly double[] reflectance = new double[SpectralBands.Count];

        /// <summary>
        /// Confirms the Saunderson correction and its inverse are exact inverses. The
        /// ingest applies the inverse to raw reflectance and the kernel applies the
        /// forward form to every mixture; if the two ever stop being the same
        /// convention, the measured and derived tiers disagree with each other and
        /// nothing else in the suite would say why.
        /// </summary>
        [Theory]
        [InlineData(0.01)]
        [InlineData(0.05)]
        [InlineData(0.4)]
        [InlineData(0.9)]
        [InlineData(0.999)]
        public void SaundersonRoundTripsItsInverse(double measured)
        {
            double internalReflectance = KubelkaMunk.InverseSaunderson(measured);
            double roundTripped = KubelkaMunk.Saunderson(internalReflectance);

            Assert.InRange(roundTripped, measured - 1e-12, measured + 1e-12);
        }

        /// <summary>
        /// Confirms a paint mixed with itself in any proportion is unchanged. This is
        /// the sharpest possible check that concentrations are being normalised rather
        /// than scaled: any per-paint weighting term cancels here only if it is applied
        /// identically to both sides, and any renormalisation bug shows up immediately.
        /// </summary>
        [Theory]
        [InlineData(0.5, 0.5)]
        [InlineData(0.9, 0.1)]
        [InlineData(3.0, 1.0)]
        public void MixingAPaintWithItselfReturnsThatPaint(double first, double second)
        {
            PigmentCoefficients blue = Paint("Phthalo Blue (G.S.)");
            var alone = new double[SpectralBands.Count];

            KubelkaMunk.Mix(new[] { blue }, new[] { 1.0 }, alone);
            KubelkaMunk.Mix(new[] { blue, blue }, new[] { first, second }, this.reflectance);

            for (int band = 0; band < SpectralBands.Count; band++)
            {
                Assert.InRange(this.reflectance[band], alone[band] - 1e-12, alone[band] + 1e-12);
            }
        }

        /// <summary>
        /// Confirms weights are relative, so doubling every weight changes nothing. The
        /// wheel and the recipe search both hand over unnormalised shares.
        /// </summary>
        [Fact]
        public void ScalingEveryWeightChangesNothing()
        {
            PigmentCoefficients blue = Paint("Phthalo Blue (G.S.)");
            PigmentCoefficients yellow = Paint("Diarylide Yellow");
            var doubled = new double[SpectralBands.Count];

            KubelkaMunk.Mix(new[] { blue, yellow }, new[] { 0.25, 0.75 }, this.reflectance);
            KubelkaMunk.Mix(new[] { blue, yellow }, new[] { 2.5, 7.5 }, doubled);

            for (int band = 0; band < SpectralBands.Count; band++)
            {
                Assert.InRange(doubled[band], this.reflectance[band] - 1e-12, this.reflectance[band] + 1e-12);
            }
        }

        /// <summary>
        /// Confirms mixed reflectance stays inside the physically meaningful range at
        /// every band, including for a mixture of the two most extreme paints in the
        /// library. A reflectance above 1 or below 0 means the inversion left its valid
        /// regime, which is what the scattering floor exists to prevent.
        /// </summary>
        [Fact]
        public void MixedReflectanceStaysWithinZeroAndOne()
        {
            PigmentCoefficients white = Paint("Titanium White");
            PigmentCoefficients black = Paint("Bone Black");

            for (int step = 0; step <= 20; step++)
            {
                double share = step / 20.0;
                KubelkaMunk.Mix(new[] { white, black }, new[] { 1.0 - share, share }, this.reflectance);

                Assert.All(this.reflectance, r => Assert.InRange(r, 0.0, 1.0));
            }
        }

        /// <summary>
        /// Confirms adding a black to a white darkens it at every band, monotonically.
        /// This is the sweep where the old pipeline's 1e-15 reflectance floor produced
        /// nonsense, and it is the reason the floor now sits on scattering instead.
        /// </summary>
        [Fact]
        public void AddingBlackToWhiteDarkensMonotonically()
        {
            PigmentCoefficients white = Paint("Titanium White");
            PigmentCoefficients black = Paint("Bone Black");
            var previous = new double[SpectralBands.Count];
            KubelkaMunk.Mix(new[] { white }, new[] { 1.0 }, previous);

            for (int step = 1; step <= 20; step++)
            {
                double share = step / 20.0;
                KubelkaMunk.Mix(new[] { white, black }, new[] { 1.0 - share, share }, this.reflectance);

                for (int band = 0; band < SpectralBands.Count; band++)
                {
                    Assert.True(
                        this.reflectance[band] <= previous[band] + 1e-12,
                        $"band {band} rose from {previous[band]} to {this.reflectance[band]} at {share:F2} black");
                }

                Array.Copy(this.reflectance, previous, SpectralBands.Count);
            }
        }

        [Fact]
        public void IndexedMixMatchesTheGeneralKernelWithoutABlend()
        {
            var palette = new[]
            {
                Paint("Titanium White"),
                Paint("Diarylide Yellow"),
                Paint("Phthalo Blue (G.S.)"),
            };
            var expected = new double[SpectralBands.Count];

            KubelkaMunk.Mix(new[] { palette[0], palette[2] }, new[] { 0.35, 0.65 }, expected);
            KubelkaMunk.MixIndexed(
                palette, new[] { 0, 2 }, new[] { 0.35, 0.65 }, -1, 0.0, this.reflectance);

            AssertSpectraEqual(expected, this.reflectance);
        }

        [Fact]
        public void IndexedMixFoldsAnExistingOrNewMotherColourWithoutAllocatingASubset()
        {
            var palette = new[]
            {
                Paint("Titanium White"),
                Paint("Diarylide Yellow"),
                Paint("Phthalo Blue (G.S.)"),
            };
            var expected = new double[SpectralBands.Count];

            KubelkaMunk.Mix(new[] { palette[0], palette[1] }, new[] { 0.24, 0.76 }, expected);
            KubelkaMunk.MixIndexed(
                palette, new[] { 0, 1 }, new[] { 0.3, 0.7 }, 1, 0.2, this.reflectance);
            AssertSpectraEqual(expected, this.reflectance);

            KubelkaMunk.Mix(
                new[] { palette[0], palette[1], palette[2] }, new[] { 0.24, 0.56, 0.2 }, expected);
            KubelkaMunk.MixIndexed(
                palette, new[] { 0, 1 }, new[] { 0.3, 0.7 }, 2, 0.2, this.reflectance);
            AssertSpectraEqual(expected, this.reflectance);
        }

        /// <summary>
        /// Confirms the kernel rejects inputs that cannot mean anything, rather than
        /// returning a colour computed from nonsense.
        /// </summary>
        [Fact]
        public void RejectsMismatchedEmptyAndNegativeInput()
        {
            PigmentCoefficients blue = Paint("Phthalo Blue (G.S.)");

            Assert.Throws<ArgumentException>(
                () => KubelkaMunk.Mix(new[] { blue }, new[] { 1.0, 1.0 }, this.reflectance));
            Assert.Throws<ArgumentException>(
                () => KubelkaMunk.Mix(new PigmentCoefficients[0], new double[0], this.reflectance));
            Assert.Throws<ArgumentException>(
                () => KubelkaMunk.Mix(new[] { blue }, new[] { -1.0 }, this.reflectance));
            Assert.Throws<ArgumentException>(
                () => KubelkaMunk.Mix(new[] { blue }, new[] { 0.0 }, this.reflectance));
        }

        /// <summary>
        /// Looks a paint up by name.
        /// </summary>
        /// <param name="name">The paint's name in the library.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }

        private static void AssertSpectraEqual(double[] expected, double[] actual)
        {
            for (int band = 0; band < SpectralBands.Count; band++)
            {
                Assert.InRange(actual[band], expected[band] - 1e-12, expected[band] + 1e-12);
            }
        }
    }
}
