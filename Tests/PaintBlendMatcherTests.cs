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
        /// Confirms every recipe is stated in whole parts. A recipe is executed by
        /// scooping paint onto a palette, so "two parts to one" is followable and
        /// "63.4%" is not.
        /// </summary>
        [Fact]
        public void StatesEveryRecipeInWholeParts()
        {
            var matcher = new PaintBlendMatcher(Paints);

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);

                Assert.Equal(match.PaintIndices.Count, match.Parts.Count);
                foreach (int parts in match.Parts)
                {
                    Assert.True(parts >= 1, $"expected at least one part of each listed paint, got {parts}");
                }
            }
        }

        /// <summary>
        /// Confirms the reported parts describe the same mixture as the reported weights.
        /// The weights drive the mixer and the parts are what the user reads, so a
        /// disagreement between them would show a swatch the stated recipe cannot make.
        /// </summary>
        [Fact]
        public void KeepsThePartsAndTheWeightsDescribingTheSameMixture()
        {
            var matcher = new PaintBlendMatcher(Paints);

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);

                int totalParts = 0;
                foreach (int parts in match.Parts)
                {
                    totalParts += parts;
                }

                for (int i = 0; i < match.Parts.Count; i++)
                {
                    Assert.Equal((double)match.Parts[i] / totalParts, match.Weights[i], 9);
                }
            }
        }

        /// <summary>
        /// Confirms two-paint recipes only ever use ratios a person can measure out.
        /// The ladder is geometric because the error from rounding a ratio scales with
        /// the ratio itself: at wide ratios a whole part either way is barely visible,
        /// while near even the same absolute step is obvious.
        /// </summary>
        [Fact]
        public void LimitsTwoPaintRecipesToRatiosAPersonCanMeasure()
        {
            var allowed = new HashSet<(int, int)>
            {
                (1, 1), (3, 2), (2, 1), (3, 1), (5, 1), (8, 1), (12, 1), (20, 1),
            };
            var matcher = new PaintBlendMatcher(Paints);

            foreach (Color target in EnumerateProbeColors())
            {
                PaintBlendMatcher.BlendMatch match = matcher.FindClosestBlend(target);
                if (match.Parts.Count != 2)
                {
                    continue;
                }

                // Ratios are unordered, so compare with the larger share first.
                int high = Math.Max(match.Parts[0], match.Parts[1]);
                int low = Math.Min(match.Parts[0], match.Parts[1]);

                Assert.Contains((high, low), allowed);
            }
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
