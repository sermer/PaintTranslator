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

        // The smallest share a paint can hold and still be named in a recipe. Below one
        // percent a paint is an artefact of where the solver's refinement happened to
        // stop rather than something anyone would put on a palette, and printing
        // "0% Cadmium Yellow" states a quantity nobody can act on. Such a paint is
        // dropped from the recipe and its share spread across the ones that remain.
        private const double MinimumShare = 0.005;

        // The most entries the query cache holds before it is reset. A converted
        // image contains at most a few hundred distinct candidate colours, so the
        // cap is only ever reached by hovering across an unconverted photo, where
        // the entries are unlikely to recur anyway.
        private const int MaximumCachedMatches = 4096;

        // The paints this matcher mixes from, in the caller's order; BlendMatch reports
        // indices into this list.
        private readonly IReadOnlyList<PigmentCoefficients> paints;

        // Every query answered so far, keyed by exact ARGB. A cursor sweeping a
        // converted image revisits the same few hundred candidate colours over and
        // over, so remembering all of them keeps the tooltip from re-running the
        // full subset search on nearly every mouse move.
        private readonly Dictionary<int, BlendMatch> matchesByArgb = new Dictionary<int, BlendMatch>();

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
            /// <param name="weights">Each participating paint's share of the mixture,
            /// summing to 1 and index-aligned with <paramref name="paintIndices"/>.</param>
            /// <param name="exactDistance">The distance at the unrounded proportions the
            /// solver found.</param>
            /// <param name="snappedDistance">The distance at the rounded percentages reported.</param>
            /// <param name="chromaLost">The Oklab chroma the mixture gave up to be shown
            /// on screen, or zero when it was already displayable.</param>
            public BlendMatch(
                Color mixedColor,
                IReadOnlyList<int> paintIndices,
                IReadOnlyList<double> weights,
                double exactDistance = 0.0,
                double snappedDistance = 0.0,
                double chromaLost = 0.0)
            {
                MixedColor = mixedColor;
                PaintIndices = paintIndices;
                Weights = weights;
                ExactDistance = exactDistance;
                SnappedDistance = snappedDistance;
                ChromaLost = chromaLost;
                Percentages = ToPercentages(weights);
            }

            /// <summary>
            /// Gets each participating paint's share of the recipe as a whole percentage,
            /// index-aligned with <see cref="PaintIndices"/> and summing to exactly 100.
            /// This is the form the recipe is reported in.
            /// </summary>
            public IReadOnlyList<int> Percentages { get; }

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
            /// Gets the perceptual distance at the rounded percentages actually reported.
            /// The gap between this and <see cref="ExactDistance"/> is what rounding to
            /// whole percentages cost, which is worth showing on the rare occasion it is
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
        /// Rounds fractional shares to whole percentages that still sum to exactly 100.
        /// </summary>
        /// <param name="weights">The shares to round, summing to 1.</param>
        /// <returns>The whole percentages, index-aligned with <paramref name="weights"/>.</returns>
        private static int[] ToPercentages(IReadOnlyList<double> weights)
        {
            // Largest remainder: floor every share, then hand the leftover points to
            // whichever shares were cut hardest. Rounding each share on its own would let
            // a recipe total 99 or 101, and a recipe whose numbers do not add up reads as
            // a bug even when the mixture behind it is right.
            var percentages = new int[weights.Count];
            var remainders = new double[weights.Count];
            int assigned = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                double exact = weights[i] * 100.0;
                percentages[i] = (int)Math.Floor(exact);
                remainders[i] = exact - percentages[i];
                assigned += percentages[i];
            }

            for (int point = assigned; point < 100; point++)
            {
                int target = 0;
                for (int i = 1; i < remainders.Length; i++)
                {
                    if (remainders[i] > remainders[target])
                    {
                        target = i;
                    }
                }

                percentages[target]++;

                // Spent remainders are pushed below any real one so a single share cannot
                // collect every leftover point.
                remainders[target] = -1.0;
            }

            // A share just above the drop threshold can still floor to zero and then lose
            // the contest for every leftover point. Lifting it off zero at the largest
            // share's expense keeps every named paint to a quantity someone can act on,
            // and costs the mixture one percentage point.
            for (int i = 0; i < percentages.Length; i++)
            {
                if (percentages[i] > 0)
                {
                    continue;
                }

                int largest = 0;
                for (int j = 1; j < percentages.Length; j++)
                {
                    if (percentages[j] > percentages[largest])
                    {
                        largest = j;
                    }
                }

                percentages[largest]--;
                percentages[i]++;
            }

            return percentages;
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
            if (matchesByArgb.TryGetValue(targetArgb, out BlendMatch cached))
            {
                return cached;
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

            BlendMatch match = winner.ToMatch();

            // A hard reset rather than an eviction policy: the cap exists only to
            // bound memory during an unusually long hover session, and the search
            // repopulates hot entries in a few mouse moves.
            if (matchesByArgb.Count >= MaximumCachedMatches)
            {
                matchesByArgb.Clear();
            }

            matchesByArgb[targetArgb] = match;
            return match;
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

            /// <summary>
            /// The winning subset's solved shares. Copied out when a subset wins because
            /// the share buffers are scratch: the very next subset considered overwrites
            /// them, and by the time the search ends the winner's proportions would
            /// otherwise be long gone.
            /// </summary>
            private readonly double[] bestShares = new double[3];

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
                double[] solved = this.shareBuffers[size - 1];

                double distance = SubsetSolver.SolveReusing(
                    view, this.targetL, this.targetA, this.targetB,
                    solved, this.reflectance);

                if (distance >= this.bestDistance)
                {
                    return;
                }

                this.bestDistance = distance;
                this.bestSize = size;
                this.bestIndices[0] = first;
                this.bestIndices[1] = second;
                this.bestIndices[2] = third;
                Array.Copy(solved, this.bestShares, size);
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
                Array.Copy(other.bestShares, this.bestShares, other.bestShares.Length);
            }

            /// <summary>
            /// Builds the match from the winning subset's solved proportions.
            /// <para>
            /// The proportions are reported as the solver found them, so the recipe is
            /// whatever gets closest to the target rather than whichever rung of a fixed
            /// ratio ladder happened to be nearest. A ladder cost real accuracy: the
            /// paints in a subset rarely have comparable tinting strength, and next to a
            /// pigment as strong as phthalo blue the gap between one part in six and one
            /// in eight is an obvious shift in the mixed colour.
            /// </para>
            /// <para>
            /// Paints holding a negligible share are dropped rather than named at zero,
            /// which is what turns a triple the solver has effectively reduced to a pair
            /// back into a two-paint recipe.
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

                // Discard the negligible paints first, so everything downstream — the
                // subset that gets mixed, the indices reported and the percentages
                // rounded — describes the same recipe.
                var indices = new int[this.bestSize];
                var weights = new double[this.bestSize];
                int kept = 0;
                double keptTotal = 0.0;

                for (int i = 0; i < this.bestSize; i++)
                {
                    if (this.bestShares[i] < MinimumShare)
                    {
                        continue;
                    }

                    indices[kept] = this.bestIndices[i];
                    weights[kept] = this.bestShares[i];
                    keptTotal += this.bestShares[i];
                    kept++;
                }

                // Every share falling below the threshold at once is only possible if the
                // solver returned nothing usable; keeping the largest leaves a recipe that
                // is still honest about which paint dominates.
                if (kept == 0)
                {
                    indices[0] = this.bestIndices[0];
                    weights[0] = 1.0;
                    kept = 1;
                    keptTotal = 1.0;
                }

                Array.Resize(ref indices, kept);
                Array.Resize(ref weights, kept);

                for (int i = 0; i < kept; i++)
                {
                    weights[i] /= keptTotal;
                    this.subset[i] = this.paints[indices[i]];
                }

                var view = new ArraySegment<PigmentCoefficients>(this.subset, 0, kept);

                // The swatch is mixed at the percentages the user is shown, not at the
                // solver's full precision, so what appears beside the recipe is what
                // following that recipe produces.
                int[] percentages = ToPercentages(weights);
                var rounded = new double[kept];
                for (int i = 0; i < kept; i++)
                {
                    rounded[i] = percentages[i] / 100.0;
                }

                KubelkaMunk.Mix(view, rounded, this.reflectance);
                SpectralRenderer.ToLab(this.reflectance, out double l, out double a, out double b);
                double snappedDistance = PerceptualDistance(
                    this.targetL, this.targetA, this.targetB, l, a, b);

                Color mixed = SpectralRenderer.ToDisplayColor(this.reflectance, out double chromaLost);

                return new BlendMatch(
                    mixed, indices, weights, this.bestDistance, snappedDistance, chromaLost);
            }
        }
    }
}
