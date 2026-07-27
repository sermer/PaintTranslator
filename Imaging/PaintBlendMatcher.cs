using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Finds, for an arbitrary color, the paint mixture that comes perceptually
    /// closest to it among the mixtures achievable with a fixed set of paints.
    /// <para>
    /// Every subset of up to three paints is enumerated, and each one is solved for its
    /// own best proportions. The previous approach sampled a fixed ladder of ratios and
    /// took the nearest sample, which meant the subset it chose was whichever one the
    /// grid happened to favour rather than the one that could actually get closest.
    /// </para>
    /// <para>
    /// The winning proportions are then rounded to whole parts, because parts are what a
    /// person can measure out. Both distances are kept: what the mixture could have been
    /// and what the reported recipe actually achieves.
    /// </para>
    /// </summary>
    public sealed class PaintBlendMatcher
    {
        // How much more a lightness difference counts than a chromatic one when
        // ranking candidates. Measured perceptibility thresholds sit at about 1.04
        // for lightness against 1.58 for chroma, a ratio near three to two, and a
        // painting's value structure is what carries its legibility: when no mixture
        // can reach a target, being the right lightness and the wrong hue reads far
        // better than the reverse. Note this is the opposite of the industrial
        // convention, which discounts lightness because it is asking whether a batch
        // is within tolerance rather than whether an image reads correctly.
        private const double LightnessWeight = 1.5;

        // Single-paint recipes need no ladder; the paint is used straight from the tube.
        private static readonly int[][] SingleParts =
        {
            new[] { 1 },
        };

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

        // The paints this matcher mixes from, in the caller's order; BlendMatch reports
        // indices into this list.
        private readonly IReadOnlyList<PigmentCoefficients> paints;

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
            /// <param name="exactDistance">The distance at the unrounded proportions the
            /// solver found.</param>
            /// <param name="snappedDistance">The distance at the whole parts reported.</param>
            /// <param name="chromaLost">The Oklab chroma the mixture gave up to be shown
            /// on screen, or zero when it was already displayable.</param>
            public BlendMatch(
                Color mixedColor,
                IReadOnlyList<int> paintIndices,
                IReadOnlyList<int> parts,
                double exactDistance = 0.0,
                double snappedDistance = 0.0,
                double chromaLost = 0.0)
            {
                MixedColor = mixedColor;
                PaintIndices = paintIndices;
                Parts = parts;
                ExactDistance = exactDistance;
                SnappedDistance = snappedDistance;
                ChromaLost = chromaLost;

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

            /// <summary>
            /// Gets the perceptual distance at the unrounded proportions the solver
            /// found, before they were rounded to whole parts.
            /// </summary>
            public double ExactDistance { get; }

            /// <summary>
            /// Gets the perceptual distance at the whole parts actually reported. The gap
            /// between this and <see cref="ExactDistance"/> is what rounding to a recipe
            /// someone can measure out by hand cost, which is worth showing when it is
            /// large enough to see.
            /// </summary>
            public double SnappedDistance { get; }

            /// <summary>
            /// Gets the Oklab chroma the mixture gave up to be displayed, or zero when it
            /// was already inside the sRGB gamut. A positive value means the paint on the
            /// palette is more vivid than <see cref="MixedColor"/> can show.
            /// </summary>
            public double ChromaLost { get; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaintBlendMatcher"/> class over
        /// a set of measured paints.
        /// </summary>
        /// <param name="paints">The available paints.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paints"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paints"/> is empty.</exception>
        public PaintBlendMatcher(IReadOnlyList<PigmentCoefficients> paints)
        {
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (paints.Count == 0)
            {
                throw new ArgumentException("At least one paint is required.", nameof(paints));
            }

            this.paints = paints;
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
        /// Finds the achievable mixture perceptually nearest to the given color, over
        /// every subset of up to three paints.
        /// </summary>
        /// <param name="target">The color to approximate with a paint mixture.</param>
        /// <returns>The closest mixture and its recipe, or null when there are no paints.</returns>
        public BlendMatch FindClosestBlend(Color target)
        {
            int targetArgb = target.ToArgb();
            if (lastMatch != null && targetArgb == lastTargetArgb)
            {
                return lastMatch;
            }

            PalettePhotoConverter.RgbToLab(target.R, target.G, target.B,
                out double targetL, out double targetA, out double targetB);

            var winner = new Search(this.paints, targetL, targetA, targetB);

            // Subsets are independent, and there are over a thousand of them for a
            // nineteen-paint palette. Run them across cores and keep the best: the work
            // is identical either way, so this changes how long the tooltip takes to
            // appear and nothing about what it says. Each worker owns a Search, which is
            // where the scratch buffers live, so nothing is shared while solving.
            Parallel.For(
                0,
                this.paints.Count,
                () => new Search(this.paints, targetL, targetA, targetB),
                (first, state, search) =>
                {
                    search.Consider(1, first, 0, 0);

                    for (int second = first + 1; second < this.paints.Count; second++)
                    {
                        search.Consider(2, first, second, 0);

                        for (int third = second + 1; third < this.paints.Count; third++)
                        {
                            search.Consider(3, first, second, third);
                        }
                    }

                    return search;
                },
                search =>
                {
                    lock (winner)
                    {
                        winner.TakeIfBetter(search);
                    }
                });

            lastTargetArgb = targetArgb;
            lastMatch = winner.ToMatch();
            return lastMatch;
        }

        /// <summary>
        /// Converts whole parts to the fractional shares the kernel takes. The kernel
        /// normalises the shares itself, so raw part counts pass through unchanged.
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
        /// One query's exhaustive walk over the subsets, holding the scratch buffers and
        /// the best result found so far.
        /// <para>
        /// A class rather than local variables because the enumeration needs the buffers
        /// to outlive each subset: reallocating them per subset would dominate the cost
        /// of a search that visits over a thousand of them.
        /// </para>
        /// </summary>
        private sealed class Search
        {
            /// <summary>Every paint available to the search.</summary>
            private readonly IReadOnlyList<PigmentCoefficients> paints;

            /// <summary>The target's L*.</summary>
            private readonly double targetL;

            /// <summary>The target's a*.</summary>
            private readonly double targetA;

            /// <summary>The target's b*.</summary>
            private readonly double targetB;

            /// <summary>The paints of the subset currently being solved.</summary>
            private readonly PigmentCoefficients[] subset = new PigmentCoefficients[3];

            /// <summary>
            /// Share buffers, one per subset size. Sized exactly rather than sliced from
            /// a single buffer because the kernel requires one concentration per paint
            /// and would reject a longer array.
            /// </summary>
            private readonly double[][] shareBuffers =
            {
                new double[1], new double[2], new double[3],
            };

            /// <summary>A scratch spectrum shared by every mix in the search.</summary>
            private readonly double[] reflectance = new double[SpectralBands.Count];

            /// <summary>The winning subset's paint indices.</summary>
            private readonly int[] bestIndices = new int[3];

            /// <summary>How many paints the winning subset holds.</summary>
            private int bestSize;

            /// <summary>The winning subset's distance at unrounded proportions.</summary>
            private double bestDistance = double.MaxValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="Search"/> class.
            /// </summary>
            /// <param name="paints">Every paint available to the search.</param>
            /// <param name="targetL">The target's L*.</param>
            /// <param name="targetA">The target's a*.</param>
            /// <param name="targetB">The target's b*.</param>
            public Search(
                IReadOnlyList<PigmentCoefficients> paints,
                double targetL, double targetA, double targetB)
            {
                this.paints = paints;
                this.targetL = targetL;
                this.targetA = targetA;
                this.targetB = targetB;
            }

            /// <summary>
            /// Solves one subset and keeps it if it beats everything seen so far.
            /// </summary>
            /// <param name="size">How many of the indices participate, one to three.</param>
            /// <param name="first">The first paint's index.</param>
            /// <param name="second">The second paint's index, ignored when size is 1.</param>
            /// <param name="third">The third paint's index, ignored when size is under 3.</param>
            public void Consider(int size, int first, int second, int third)
            {
                this.subset[0] = this.paints[first];
                if (size > 1)
                {
                    this.subset[1] = this.paints[second];
                }
                if (size > 2)
                {
                    this.subset[2] = this.paints[third];
                }

                var view = new ArraySegment<PigmentCoefficients>(this.subset, 0, size);

                double distance = SubsetSolver.SolveReusing(
                    view, this.targetL, this.targetA, this.targetB,
                    this.shareBuffers[size - 1], this.reflectance);

                if (distance >= this.bestDistance)
                {
                    return;
                }

                this.bestDistance = distance;
                this.bestSize = size;
                this.bestIndices[0] = first;
                this.bestIndices[1] = second;
                this.bestIndices[2] = third;
            }

            /// <summary>
            /// Adopts another worker's winner when it beat this one.
            /// </summary>
            /// <param name="other">The worker's search to merge in.</param>
            public void TakeIfBetter(Search other)
            {
                if (other.bestSize == 0 || other.bestDistance >= this.bestDistance)
                {
                    return;
                }

                this.bestDistance = other.bestDistance;
                this.bestSize = other.bestSize;
                Array.Copy(other.bestIndices, this.bestIndices, other.bestIndices.Length);
            }

            /// <summary>
            /// Rounds the winning subset's proportions to whole parts and builds the
            /// match.
            /// <para>
            /// The rung chosen is the one whose mixture lands closest to the target, not
            /// the one whose numbers are closest to the solved proportions. Those differ:
            /// the paints in a subset rarely have equal tinting strength, so a share
            /// moved by a tenth matters far more in some directions than others, and it
            /// is the resulting colour that the user sees.
            /// </para>
            /// </summary>
            /// <returns>The best mixture and its recipe, or null when nothing was
            /// considered.</returns>
            public BlendMatch ToMatch()
            {
                if (this.bestSize == 0)
                {
                    return null;
                }

                for (int i = 0; i < this.bestSize; i++)
                {
                    this.subset[i] = this.paints[this.bestIndices[i]];
                }

                var view = new ArraySegment<PigmentCoefficients>(this.subset, 0, this.bestSize);
                int[][] ladder = this.bestSize == 1 ? SingleParts
                    : this.bestSize == 2 ? PairParts
                    : TripleParts;

                int[] bestParts = ladder[0];
                double snappedDistance = double.MaxValue;

                foreach (int[] parts in ladder)
                {
                    KubelkaMunk.Mix(view, ToShares(parts), this.reflectance);
                    SpectralRenderer.ToLab(this.reflectance, out double l, out double a, out double b);

                    double distance = PerceptualDistance(this.targetL, this.targetA, this.targetB, l, a, b);
                    if (distance < snappedDistance)
                    {
                        snappedDistance = distance;
                        bestParts = parts;
                    }
                }

                KubelkaMunk.Mix(view, ToShares(bestParts), this.reflectance);
                Color mixed = SpectralRenderer.ToDisplayColor(this.reflectance, out double chromaLost);

                var indices = new int[this.bestSize];
                Array.Copy(this.bestIndices, indices, this.bestSize);

                return new BlendMatch(
                    mixed, indices, bestParts, this.bestDistance, snappedDistance, chromaLost);
            }
        }
    }
}
