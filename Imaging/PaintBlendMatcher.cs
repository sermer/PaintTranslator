using System;
using System.Collections.Generic;
using System.Drawing;

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

        // Interior sample points of the two-paint mixing line, as the share of
        // the second paint. Endpoints are covered by the single-paint recipes.
        private static readonly double[] PairRatios =
        {
            1 / 8.0, 2 / 8.0, 3 / 8.0, 4 / 8.0, 5 / 8.0, 6 / 8.0, 7 / 8.0,
        };

        // Interior sample points of the three-paint mixing triangle: the centroid
        // plus each vertex-leaning midpoint. Edges are covered by the pair samples.
        private static readonly double[][] TripleWeights =
        {
            new[] { 1 / 3.0, 1 / 3.0, 1 / 3.0 },
            new[] { 0.50, 0.25, 0.25 },
            new[] { 0.25, 0.50, 0.25 },
            new[] { 0.25, 0.25, 0.50 },
        };

        // Parallel candidate arrays: each mixture's sRGB value, its CIELAB
        // coordinates, and the recipe (paint indices with matching weights)
        // that produced it.
        private readonly int[] candidateArgb;
        private readonly double[] candidateL;
        private readonly double[] candidateA;
        private readonly double[] candidateB;
        private readonly int[][] candidatePaints;
        private readonly double[][] candidateWeights;

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
            /// <param name="weights">Each participating paint's share of the mix, index-aligned with <paramref name="paintIndices"/>.</param>
            public BlendMatch(Color mixedColor, IReadOnlyList<int> paintIndices, IReadOnlyList<double> weights)
            {
                MixedColor = mixedColor;
                PaintIndices = paintIndices;
                Weights = weights;
            }

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

            int count = paintColors.Count;

            // Mixing happens in absorbance space, so convert each paint once.
            var absorption = new double[count][];
            for (int i = 0; i < count; i++)
            {
                absorption[i] = SubtractivePaintMixer.ToAbsorption(paintColors[i]);
            }

            var seen = new HashSet<int>();
            var argbs = new List<int>();
            var recipePaints = new List<int[]>();
            var recipeWeights = new List<double[]>();

            // Each paint straight from the tube. Singles are added first so that
            // when several recipes collapse to the same color, the simplest
            // recipe is the one that survives deduplication.
            for (int i = 0; i < count; i++)
            {
                AddCandidate(paintColors[i], new[] { i }, new[] { 1.0 }, seen, argbs, recipePaints, recipeWeights);
            }

            // Every unordered pair, sampled along its mixing line.
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    foreach (double w in PairRatios)
                    {
                        Color mixed = SubtractivePaintMixer.FromAbsorption(
                            (1.0 - w) * absorption[i][0] + w * absorption[j][0],
                            (1.0 - w) * absorption[i][1] + w * absorption[j][1],
                            (1.0 - w) * absorption[i][2] + w * absorption[j][2]);
                        AddCandidate(mixed, new[] { i, j }, new[] { 1.0 - w, w }, seen, argbs, recipePaints, recipeWeights);
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
                            foreach (double[] w in TripleWeights)
                            {
                                Color mixed = SubtractivePaintMixer.FromAbsorption(
                                    w[0] * absorption[i][0] + w[1] * absorption[j][0] + w[2] * absorption[k][0],
                                    w[0] * absorption[i][1] + w[1] * absorption[j][1] + w[2] * absorption[k][1],
                                    w[0] * absorption[i][2] + w[1] * absorption[j][2] + w[2] * absorption[k][2]);
                                AddCandidate(mixed, new[] { i, j, k }, (double[])w.Clone(), seen, argbs, recipePaints, recipeWeights);
                            }
                        }
                    }
                }
            }

            // Precompute CIELAB for every surviving candidate so each query is
            // pure arithmetic over flat arrays.
            candidateArgb = argbs.ToArray();
            candidatePaints = recipePaints.ToArray();
            candidateWeights = recipeWeights.ToArray();
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
        /// Finds the sampled mixture perceptually nearest (squared CIELAB distance)
        /// to the given color.
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
                double dl = candidateL[i] - targetL;
                double da = candidateA[i] - targetA;
                double db = candidateB[i] - targetB;
                double distance = dl * dl + da * da + db * db;
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
                candidateWeights[bestIndex]);
            return lastMatch;
        }

        /// <summary>
        /// Records a candidate mixture and its recipe unless an identical color is
        /// already present.
        /// </summary>
        /// <param name="color">The mixture color to record.</param>
        /// <param name="paints">The indices of the paints in the recipe.</param>
        /// <param name="weights">Each recipe paint's share of the mix.</param>
        /// <param name="seen">The set of ARGB values already recorded.</param>
        /// <param name="argbs">The list of recorded candidate ARGB values.</param>
        /// <param name="recipePaints">The list of recorded recipe paint indices.</param>
        /// <param name="recipeWeights">The list of recorded recipe weights.</param>
        private static void AddCandidate(Color color, int[] paints, double[] weights,
            HashSet<int> seen, List<int> argbs, List<int[]> recipePaints, List<double[]> recipeWeights)
        {
            int argb = color.ToArgb();
            if (seen.Add(argb))
            {
                argbs.Add(argb);
                recipePaints.Add(paints);
                recipeWeights.Add(weights);
            }
        }
    }
}
