using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Covers how the matcher measures "closest" and how it expresses the recipe it
    /// found. Both matter to the user for the same reason: the app's output is an
    /// instruction to mix paint by hand, so the match has to be judged the way an eye
    /// judges it and stated in amounts a hand can measure.
    /// </summary>
    public class PaintBlendMatcherTests
    {
        // A white, a yellow, a red, a blue and a black: enough spread that the probe
        // colours below land on single-paint, two-paint and three-paint recipes, and
        // small enough that an exhaustive subset search over 200 probes stays quick.
        private static readonly IReadOnlyList<PigmentCoefficients> Paints = new[]
        {
            Paint("Titanium White"),
            Paint("Diarylide Yellow"),
            Paint("C.P. Cadmium Red Light"),
            Paint("Ultramarine Blue"),
            Paint("Bone Black"),
        };

        /// <summary>
        /// Looks a paint up by name.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }

        /// <summary>
        /// Confirms a pure lightness difference is scaled up by half again. Lightness
        /// carries the value structure of a painting, and measured perceptibility
        /// thresholds put lightness ahead of chroma by about that ratio, so when no
        /// mixture can match a target exactly the matcher should give up chroma before
        /// it gives up value.
        /// </summary>
        [Fact]
        public void WeighsALightnessDifferenceHalfAgainAsHeavilyAsChroma()
        {
            double lightnessOnly = PaintBlendMatcher.PerceptualDistance(50.0, 0.0, 0.0, 60.0, 0.0, 0.0);

            Assert.Equal(15.0, lightnessOnly, 6);
        }

        /// <summary>
        /// Confirms a difference in hue and chroma is measured as a plain Euclidean
        /// distance in the a*b* plane, which is the chromatic half of the HyAB metric.
        /// </summary>
        [Fact]
        public void MeasuresChromaDifferenceAsEuclideanDistanceInTheAbPlane()
        {
            double chromaOnly = PaintBlendMatcher.PerceptualDistance(50.0, 0.0, 0.0, 50.0, 3.0, 4.0);

            Assert.Equal(5.0, chromaOnly, 6);
        }

        /// <summary>
        /// Confirms lightness and chroma differences add rather than combining in
        /// quadrature. Summing the two terms is what keeps the metric well behaved at
        /// the large differences a limited palette produces, where the squared form used
        /// by CIEDE2000 is outside its stated range of validity.
        /// </summary>
        [Fact]
        public void AddsTheLightnessAndChromaTermsRatherThanCombiningThemInQuadrature()
        {
            double both = PaintBlendMatcher.PerceptualDistance(50.0, 0.0, 0.0, 60.0, 3.0, 4.0);

            Assert.Equal(20.0, both, 6);
        }

        /// <summary>
        /// Confirms a colour matches itself at zero distance, the degenerate case a
        /// metric has to get right for the nearest-match scan to be meaningful.
        /// </summary>
        [Fact]
        public void ReportsZeroDistanceBetweenIdenticalColors()
        {
            Assert.Equal(0.0, PaintBlendMatcher.PerceptualDistance(42.0, -7.0, 13.0, 42.0, -7.0, 13.0), 6);
        }

        /// <summary>
        /// Confirms every recipe is stated as whole percentages that add up to 100. A
        /// recipe whose numbers do not total 100 reads as a bug however good the mixture
        /// behind it is, and independent rounding of each share would routinely produce
        /// 99 or 101.
        /// </summary>
        [Fact]
        public void StatesEveryRecipeInWholePercentagesSummingToOneHundred()
        {
            var matcher = new PaintBlendMatcher(Paints);

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);

                Assert.Equal(match.PaintIndices.Count, match.Percentages.Count);

                int total = 0;
                foreach (int percentage in match.Percentages)
                {
                    total += percentage;
                }

                Assert.Equal(100, total);
            }
        }

        /// <summary>
        /// Confirms no paint is named at a quantity nobody could act on. A three-paint
        /// subset whose solution has collapsed onto two of its paints must be reported as
        /// the two-paint recipe it really is, not as a triple with "0%" beside one of the
        /// tubes.
        /// </summary>
        [Fact]
        public void NamesNoPaintItDoesNotMeaningfullyUse()
        {
            var matcher = new PaintBlendMatcher(Paints);

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);

                foreach (int percentage in match.Percentages)
                {
                    Assert.True(
                        percentage >= 1,
                        $"a listed paint held {percentage}%, which is not a usable quantity");
                }
            }
        }

        /// <summary>
        /// Confirms the reported percentages describe the same mixture as the reported
        /// weights. The weights drive the mixer and the percentages are what the user
        /// reads, so a disagreement between them would show a swatch the stated recipe
        /// cannot make.
        /// </summary>
        [Fact]
        public void KeepsThePercentagesAndTheWeightsDescribingTheSameMixture()
        {
            var matcher = new PaintBlendMatcher(Paints);

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);

                for (int i = 0; i < match.Percentages.Count; i++)
                {
                    // A whole percentage can sit at most half a point from the share it
                    // came from, plus the one point the sum-to-100 correction may move.
                    Assert.True(
                        Math.Abs((match.Weights[i] * 100.0) - match.Percentages[i]) <= 1.5,
                        $"{match.Percentages[i]}% does not describe a share of {match.Weights[i]:0.0000}");
                }
            }
        }

        /// <summary>
        /// Confirms solving for the proportions beats snapping them to a ratio ladder.
        /// <para>
        /// The matcher used to round its solution onto a fixed geometric ladder of whole
        /// parts — 1:1, 3:2, 2:1 and so on up to 20:1. That ladder could only express the
        /// mixtures sitting on its rungs, and next to a pigment with the tinting strength
        /// of phthalo blue the gap between neighbouring rungs is an obvious shift in the
        /// mixed colour.
        /// </para>
        /// <para>
        /// The solved proportions dominate the ladder outright, which is the first
        /// assertion. The reported ones are then rounded to whole percentages, and that
        /// rounding can hand back a trace of the gain: a rung like 1:20 sits at 4.76%, a
        /// figure no whole percentage can name, so at the extreme ends of the ladder it
        /// can land a hair closer than 5% does. The second assertion bounds that
        /// giveback, and the third insists the whole exercise was worth doing.
        /// </para>
        /// </summary>
        [Fact]
        public void LandsAtLeastAsCloseAsTheRatioLadderItReplaced()
        {
            int[][] ladder =
            {
                new[] { 1, 1 },
                new[] { 3, 2 }, new[] { 2, 3 },
                new[] { 2, 1 }, new[] { 1, 2 },
                new[] { 3, 1 }, new[] { 1, 3 },
                new[] { 5, 1 }, new[] { 1, 5 },
                new[] { 8, 1 }, new[] { 1, 8 },
                new[] { 12, 1 }, new[] { 1, 12 },
                new[] { 20, 1 }, new[] { 1, 20 },
            };

            var matcher = new PaintBlendMatcher(Paints);
            var reflectance = new double[SpectralBands.Count];
            double largestGain = 0.0;
            double worstGiveback = 0.0;
            double totalGain = 0.0;
            int compared = 0;

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);
                if (match.PaintIndices.Count != 2)
                {
                    continue;
                }

                PalettePhotoConverter.RgbToLab(target.R, target.G, target.B,
                    out double targetL, out double targetA, out double targetB);

                var subset = new[]
                {
                    Paints[match.PaintIndices[0]],
                    Paints[match.PaintIndices[1]],
                };

                // The best the ladder could have done on this very subset, which is the
                // fairest comparison available: same paints, only the ratio differs.
                double bestLadder = double.MaxValue;
                foreach (int[] parts in ladder)
                {
                    KubelkaMunk.Mix(subset, new double[] { parts[0], parts[1] }, reflectance);
                    SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);
                    bestLadder = Math.Min(
                        bestLadder,
                        PaintBlendMatcher.PerceptualDistance(targetL, targetA, targetB, l, a, b));
                }

                // The solver stops refining once the shares are located to a few parts in
                // a thousand, so a ladder rung sitting inside that last interval can read
                // a hair closer. The tolerance is that residual and nothing more; before
                // the mixing line was swept finely enough to find the right basin, this
                // same comparison was out by twenty times as much.
                Assert.True(
                    match.ExactDistance <= bestLadder + 0.05,
                    $"the solved proportions landed at {match.ExactDistance:0.000} but a " +
                    $"ladder rung reached {bestLadder:0.000} for {target}");

                largestGain = Math.Max(largestGain, bestLadder - match.SnappedDistance);
                worstGiveback = Math.Max(worstGiveback, match.SnappedDistance - bestLadder);
                totalGain += bestLadder - match.SnappedDistance;
                compared++;
            }

            double meanGain = totalGain / compared;

            // A ladder-free search that never actually beats the ladder would satisfy
            // every assertion above while changing nothing the user can see.
            Assert.True(
                largestGain > 1.0 && meanGain > 0.0,
                $"over {compared} two-paint recipes, solving gained a mean of " +
                $"{meanGain:0.000} and at most {largestGain:0.000} over the ladder, so the " +
                "ladder was not the constraint it was believed to be");

            // Where a ladder rung falls between two whole percentages it can still edge
            // ahead of the rounded recipe. That is a property of reporting in whole
            // percent, not of the search, so it is bounded rather than forbidden — but
            // it must stay small enough to be invisible beside the gains above.
            Assert.True(
                worstGiveback < 1.0,
                $"rounding to whole percent cost up to {worstGiveback:0.000} against the " +
                $"ladder, which is too much beside a mean gain of {meanGain:0.000}");
        }

        /// <summary>
        /// Walks a spread of target colours wide enough to exercise single-paint,
        /// two-paint and three-paint recipes.
        /// </summary>
        /// <returns>The probe colours.</returns>
        private static IEnumerable<Color> EnumerateProbeColors()
        {
            var random = new Random(20260726);
            for (int i = 0; i < 200; i++)
            {
                yield return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
            }
        }
    }
}
