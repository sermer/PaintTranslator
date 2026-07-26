using System;
using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Data;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Finds, for an arbitrary color, the paint mixture that comes perceptually
    /// closest to it among the mixtures achievable with a fixed set of paints.
    /// The achievable gamut is sampled the same way <see cref="PalettePhotoConverter"/>
    /// samples it — each paint alone, every pair at several ratios, and every
    /// triple at a few interior weightings — except that each sample keeps the
    /// recipe (which paints at which shares) that produced it, so the nearest
    /// sample can report its mixing percentages.
    /// </summary>
    public sealed class PaintBlendMatcher
    {
        // Triple sampling grows with the cube of the paint count; beyond this
        // many paints only singles and pairs are sampled so construction stays
        // fast enough to run lazily on the first mouse hover.
        private const int MaxPaintsForTriples = 30;

        // How much more a lightness difference counts than a chromatic one when
        // ranking candidates. Measured perceptibility thresholds sit at about 1.04
        // for lightness against 1.58 for chroma, a ratio near three to two, and a
        // painting's value structure is what carries its legibility: when no mixture
        // can reach a target, being the right lightness and the wrong hue reads far
        // better than the reverse. Note this is the opposite of the industrial
        // convention, which discounts lightness because it is asking whether a batch
        // is within tolerance rather than whether an image reads correctly.
        private const double LightnessWeight = 1.5;

        // Two-paint recipes, in whole parts of each paint. The ladder is geometric
        // rather than evenly spaced because the visible error from rounding a ratio
        // scales with the ratio: near even, one part either way is an obvious shift,
        // while past about 1:8 the same absolute step is barely perceptible. Evenly
        // spaced eighths therefore waste candidates at the wide end and miss useful
        // ones near even, and no sampling finer than this survives being scooped out
        // of a tube by hand. Endpoints are covered by the single-paint recipes.
        private static readonly int[][] PairParts =
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

        // Three-paint recipes, in whole parts: equal thirds plus each paint doubled
        // against the other two. Edges of the mixing triangle are covered by the pair
        // recipes. Past three paints a mixture keeps barely a quarter of its parents'
        // chroma, so deeper combinations are not worth sampling.
        private static readonly int[][] TripleParts =
        {
            new[] { 1, 1, 1 },
            new[] { 2, 1, 1 },
            new[] { 1, 2, 1 },
            new[] { 1, 1, 2 },
        };

        // Parallel candidate arrays: each mixture's sRGB value, its CIELAB
        // coordinates, and the recipe (paint indices with matching parts)
        // that produced it. Populated once by SampleAchievableMixtures, which every
        // constructor calls exactly once and nothing else calls, so these are
        // effectively readonly even though the compiler cannot see it.
        private int[] candidateArgb;
        private double[] candidateL;
        private double[] candidateA;
        private double[] candidateB;
        private int[][] candidatePaints;
        private int[][] candidateParts;

        // The most recent query and its result: a hovering cursor samples the
        // same color many times in a row, so one cached entry skips most scans.
        private int lastTargetArgb;
        private BlendMatch lastMatch;

        /// <summary>
        /// Describes the achievable mixture closest to a queried color: the
        /// mixture's own color and the recipe that produces it.
        /// </summary>
        public sealed class BlendMatch
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BlendMatch"/> class.
            /// </summary>
            /// <param name="mixedColor">The color the recipe mixes to.</param>
            /// <param name="paintIndices">The indices of the participating paints in the matcher's paint list.</param>
            /// <param name="parts">Each participating paint's whole number of parts, index-aligned with <paramref name="paintIndices"/>.</param>
            public BlendMatch(Color mixedColor, IReadOnlyList<int> paintIndices, IReadOnlyList<int> parts)
            {
                MixedColor = mixedColor;
                PaintIndices = paintIndices;
                Parts = parts;

                // The mixer works in fractional shares while the user reads whole
                // parts, so both views are derived from the one source here rather
                // than tracked separately and risking disagreement.
                int totalParts = 0;
                for (int i = 0; i < parts.Count; i++)
                {
                    totalParts += parts[i];
                }

                var shares = new double[parts.Count];
                for (int i = 0; i < parts.Count; i++)
                {
                    shares[i] = (double)parts[i] / totalParts;
                }
                Weights = shares;
            }

            /// <summary>
            /// Gets each participating paint's whole number of parts in the recipe,
            /// index-aligned with <see cref="PaintIndices"/>. This is the form the
            /// recipe is reported in, because parts are what a person can measure out.
            /// </summary>
            public IReadOnlyList<int> Parts { get; }

            /// <summary>
            /// Gets the color the recipe mixes to.
            /// </summary>
            public Color MixedColor { get; }

            /// <summary>
            /// Gets the indices of the participating paints in the paint list the
            /// matcher was constructed with.
            /// </summary>
            public IReadOnlyList<int> PaintIndices { get; }

            /// <summary>
            /// Gets each participating paint's share of the mix, summing to 1 and
            /// index-aligned with <see cref="PaintIndices"/>.
            /// </summary>
            public IReadOnlyList<double> Weights { get; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaintBlendMatcher"/> class,
        /// sampling the gamut of mixtures achievable with the given paints.
        /// </summary>
        /// <param name="paintColors">The mass-tone colors of the available paints.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paintColors"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paintColors"/> is empty.</exception>
        public PaintBlendMatcher(IReadOnlyList<Color> paintColors)
        {
            if (paintColors == null)
            {
                throw new ArgumentNullException(nameof(paintColors));
            }
            if (paintColors.Count == 0)
            {
                throw new ArgumentException("At least one paint is required.", nameof(paintColors));
            }

            // Convert each paint to its mixing spectrum once; every sampled
            // mixture below reuses these.
            var spectra = new PaintSpectrum[paintColors.Count];
            for (int i = 0; i < paintColors.Count; i++)
            {
                spectra[i] = SubtractivePaintMixer.ToSpectrum(paintColors[i]);
            }

            SampleAchievableMixtures(
                paintColors.Count,
                (indices, parts) => MixSpectraByParts(spectra, indices, parts));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaintBlendMatcher"/> class from
        /// paints with spectrophotometer measurements, sampling the gamut of mixtures
        /// achievable with them.
        /// <para>
        /// Recipes found this way are worth more than recipes over the same paints
        /// described only by colour, because the sampled mixtures are where the paint
        /// actually lands. The reconstructed path has to guess how much each paint
        /// scatters and lets titanium white dominate as a result, so it proposes tints
        /// that do not come out that way on the palette.
        /// </para>
        /// </summary>
        /// <param name="paints">The available measured paints.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paints"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paints"/> is empty.</exception>
        public PaintBlendMatcher(IReadOnlyList<MeasuredPaint> paints)
        {
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (paints.Count == 0)
            {
                throw new ArgumentException("At least one paint is required.", nameof(paints));
            }

            SampleAchievableMixtures(
                paints.Count,
                (indices, parts) => MixMeasuredByParts(paints, indices, parts));
        }

        /// <summary>
        /// Builds the candidate set of achievable mixtures: each paint alone, every pair
        /// at each ratio on the ladder, and every triple at a few interior weightings.
        /// </summary>
        /// <param name="count">The number of available paints.</param>
        /// <param name="mixRecipe">Mixes the paints at the given indices in the given whole parts.</param>
        private void SampleAchievableMixtures(int count, Func<int[], int[], Color> mixRecipe)
        {
            var seen = new HashSet<int>();
            var argbs = new List<int>();
            var recipePaints = new List<int[]>();
            var recipeParts = new List<int[]>();

            // Each paint straight from the tube. Singles are added first so that
            // when several recipes collapse to the same color, the simplest
            // recipe is the one that survives deduplication.
            for (int i = 0; i < count; i++)
            {
                var single = new[] { i };
                var whole = new[] { 1 };
                AddCandidate(mixRecipe(single, whole), single, whole, seen, argbs, recipePaints, recipeParts);
            }

            // Every unordered pair, at each ratio on the ladder.
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    var pair = new[] { i, j };
                    foreach (int[] parts in PairParts)
                    {
                        AddCandidate(mixRecipe(pair, parts), pair, parts, seen, argbs, recipePaints, recipeParts);
                    }
                }
            }

            // Every unordered triple at a few interior weightings, skipped for
            // large paint sets where the combinations would take too long to
            // build for an interactive tooltip.
            if (count <= MaxPaintsForTriples)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        for (int k = j + 1; k < count; k++)
                        {
                            var triple = new[] { i, j, k };
                            foreach (int[] parts in TripleParts)
                            {
                                AddCandidate(mixRecipe(triple, parts), triple, parts, seen, argbs, recipePaints, recipeParts);
                            }
                        }
                    }
                }
            }

            // Precompute CIELAB for every surviving candidate so each query is
            // pure arithmetic over flat arrays.
            candidateArgb = argbs.ToArray();
            candidatePaints = recipePaints.ToArray();
            candidateParts = recipeParts.ToArray();
            candidateL = new double[candidateArgb.Length];
            candidateA = new double[candidateArgb.Length];
            candidateB = new double[candidateArgb.Length];
            for (int i = 0; i < candidateArgb.Length; i++)
            {
                int argb = candidateArgb[i];
                PalettePhotoConverter.RgbToLab(
                    (argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF,
                    out candidateL[i], out candidateA[i], out candidateB[i]);
            }
        }

        /// <summary>
        /// Measures the perceived difference between two CIELAB colors.
        /// </summary>
        /// <param name="firstL">The first color's lightness.</param>
        /// <param name="firstA">The first color's green-red coordinate.</param>
        /// <param name="firstB">The first color's blue-yellow coordinate.</param>
        /// <param name="secondL">The second color's lightness.</param>
        /// <param name="secondA">The second color's green-red coordinate.</param>
        /// <param name="secondB">The second color's blue-yellow coordinate.</param>
        /// <returns>The weighted distance between the two colors.</returns>
        public static double PerceptualDistance(
            double firstL, double firstA, double firstB,
            double secondL, double secondA, double secondB)
        {
            double lightnessDifference = Math.Abs(firstL - secondL);
            double da = firstA - secondA;
            double db = firstB - secondB;

            return (LightnessWeight * lightnessDifference) + Math.Sqrt((da * da) + (db * db));
        }

        /// <summary>
        /// Finds the sampled mixture perceptually nearest to the given color.
        /// </summary>
        /// <param name="target">The color to approximate with a paint mixture.</param>
        /// <returns>The closest mixture and its recipe.</returns>
        public BlendMatch FindClosestBlend(Color target)
        {
            int targetArgb = target.ToArgb();
            if (lastMatch != null && targetArgb == lastTargetArgb)
            {
                return lastMatch;
            }

            PalettePhotoConverter.RgbToLab(target.R, target.G, target.B,
                out double targetL, out double targetA, out double targetB);

            // A linear scan is fast enough here: the candidate set is tens of
            // thousands of entries at most and queries only run on color change.
            double bestDistance = double.MaxValue;
            int bestIndex = 0;
            for (int i = 0; i < candidateL.Length; i++)
            {
                double distance = PerceptualDistance(
                    candidateL[i], candidateA[i], candidateB[i],
                    targetL, targetA, targetB);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            lastTargetArgb = targetArgb;
            lastMatch = new BlendMatch(
                Color.FromArgb(candidateArgb[bestIndex]),
                candidatePaints[bestIndex],
                candidateParts[bestIndex]);
            return lastMatch;
        }

        /// <summary>
        /// Mixes reconstructed-spectrum paints in the given whole-part proportions.
        /// </summary>
        /// <param name="spectra">The spectra of all available paints.</param>
        /// <param name="paintIndices">The indices of the participating paints.</param>
        /// <param name="parts">Each participating paint's whole number of parts.</param>
        /// <returns>The mixed color.</returns>
        private static Color MixSpectraByParts(PaintSpectrum[] spectra, int[] paintIndices, int[] parts)
        {
            var participating = new PaintSpectrum[paintIndices.Length];
            for (int i = 0; i < paintIndices.Length; i++)
            {
                participating[i] = spectra[paintIndices[i]];
            }

            return SubtractivePaintMixer.Mix(participating, ToShares(parts));
        }

        /// <summary>
        /// Mixes measured paints in the given whole-part proportions.
        /// </summary>
        /// <param name="paints">All available measured paints.</param>
        /// <param name="paintIndices">The indices of the participating paints.</param>
        /// <param name="parts">Each participating paint's whole number of parts.</param>
        /// <returns>The mixed color.</returns>
        private static Color MixMeasuredByParts(
            IReadOnlyList<MeasuredPaint> paints, int[] paintIndices, int[] parts)
        {
            var participating = new MeasuredPaint[paintIndices.Length];
            for (int i = 0; i < paintIndices.Length; i++)
            {
                participating[i] = paints[paintIndices[i]];
            }

            return MeasuredPaintMixer.Mix(participating, ToShares(parts));
        }

        /// <summary>
        /// Converts whole parts to the fractional shares the mixers take. Both mixers
        /// normalise the shares themselves, so raw part counts pass through unchanged.
        /// </summary>
        /// <param name="parts">Each paint's whole number of parts.</param>
        /// <returns>The parts as mixing shares.</returns>
        private static double[] ToShares(int[] parts)
        {
            var shares = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                shares[i] = parts[i];
            }

            return shares;
        }

        /// <summary>
        /// Records a candidate mixture and its recipe unless an identical color is
        /// already present.
        /// </summary>
        /// <param name="color">The mixture color to record.</param>
        /// <param name="paints">The indices of the paints in the recipe.</param>
        /// <param name="parts">Each recipe paint's whole number of parts.</param>
        /// <param name="seen">The set of ARGB values already recorded.</param>
        /// <param name="argbs">The list of recorded candidate ARGB values.</param>
        /// <param name="recipePaints">The list of recorded recipe paint indices.</param>
        /// <param name="recipeParts">The list of recorded recipe parts.</param>
        private static void AddCandidate(Color color, int[] paints, int[] parts,
            HashSet<int> seen, List<int> argbs, List<int[]> recipePaints, List<int[]> recipeParts)
        {
            int argb = color.ToArgb();
            if (seen.Add(argb))
            {
                argbs.Add(argb);
                recipePaints.Add(paints);
                recipeParts.Add(parts);
            }
        }
    }
}
