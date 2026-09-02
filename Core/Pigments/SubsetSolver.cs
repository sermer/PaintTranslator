using System;
using System.Collections.Generic;
using PaintTranslator.Imaging;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// Finds the proportions of a small set of paints that come closest to a target
    /// colour.
    /// <para>
    /// The search is a coarse sweep of the concentration simplex followed by successive
    /// refinement around the best point found. Derivative-free by choice: the objective
    /// is not differentiable where chroma reaches zero, which is exactly where neutral
    /// photo colours sit, and a gradient method wanders there.
    /// </para>
    /// <para>
    /// Comparisons run on unmapped Lab. A gamut-mapped colour would make two different
    /// out-of-gamut mixtures look identical, and the search would sometimes prefer the
    /// wrong paint.
    /// </para>
    /// </summary>
    public static class SubsetSolver
    {
        /// <summary>
        /// How many divisions the first, exhaustive sweep uses along a two-paint subset's
        /// mixing line.
        /// <para>
        /// Far finer than the triangle below, because a line costs one evaluation per
        /// division where a triangle costs the square of that, and because the line is
        /// where the awkward objectives live: against a pigment as strong as phthalo
        /// blue, nearly all of the colour change happens in the first few percent, and
        /// the distance to a target can have more than one local minimum along the way.
        /// A coarse sweep lands in whichever basin it happens to sample and the
        /// refinement below cannot leave it, so the sweep has to be dense enough to find
        /// the right basin in the first place.
        /// </para>
        /// </summary>
        private const int LineCoarseDivisions = 48;

        /// <summary>
        /// How many divisions the first, exhaustive sweep uses per axis across a
        /// three-paint subset's mixing triangle. Held down because the cost is quadratic
        /// in this and a search visits far more triangles than lines; the interior of a
        /// triangle is also better behaved than its edges, which the pair subsets sweep
        /// densely in their own right.
        /// </summary>
        private const int TriangleCoarseDivisions = 10;

        /// <summary>How many divisions each refinement pass uses per axis.</summary>
        private const int RefineDivisions = 8;

        /// <summary>
        /// How many refinement passes run.
        /// <para>
        /// Each pass narrows the search window by a factor of four, so two passes locate
        /// each share to within about three parts in a thousand. That sits comfortably
        /// inside the whole percentage the recipe is reported to, which is what makes a
        /// third pass wasted rather than merely cheap: it cannot move a reported figure.
        /// The search visits over a thousand subsets per query, so passes that cannot
        /// change the answer are the difference between a tooltip that appears and one
        /// that stutters.
        /// </para>
        /// </summary>
        private const int RefinePasses = 2;

        /// <summary>
        /// Solves for the best proportions of a subset.
        /// </summary>
        /// <param name="subset">The paints in the subset, one to three of them.</param>
        /// <param name="targetL">The target's L*.</param>
        /// <param name="targetA">The target's a*.</param>
        /// <param name="targetB">The target's b*.</param>
        /// <param name="shares">The caller-owned buffer the proportions are written
        /// into, index-aligned with <paramref name="subset"/> and summing to 1.</param>
        /// <returns>The perceptual distance at the proportions found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the subset is empty, larger
        /// than three, or the buffer does not match it.</exception>
        public static double Solve(
            IReadOnlyList<PigmentCoefficients> subset,
            double targetL, double targetA, double targetB,
            double[] shares)
        {
            if (subset == null)
            {
                throw new ArgumentNullException(nameof(subset));
            }
            if (shares == null)
            {
                throw new ArgumentNullException(nameof(shares));
            }
            if (subset.Count < 1 || subset.Count > 3)
            {
                throw new ArgumentException("A subset holds one to three paints.", nameof(subset));
            }
            if (shares.Length != subset.Count)
            {
                throw new ArgumentException("One share per paint.", nameof(shares));
            }

            var reflectance = new double[SpectralBands.Count];

            if (subset.Count == 1)
            {
                shares[0] = 1.0;
                return Distance(subset, shares, reflectance, targetL, targetA, targetB);
            }

            return Sweep(subset, targetL, targetA, targetB, shares, reflectance);
        }

        /// <summary>
        /// Solves a subset reusing caller-owned scratch buffers, so an exhaustive
        /// enumeration over every subset allocates nothing per candidate.
        /// </summary>
        /// <param name="subset">The paints in the subset, one to three of them.</param>
        /// <param name="targetL">The target's L*.</param>
        /// <param name="targetA">The target's a*.</param>
        /// <param name="targetB">The target's b*.</param>
        /// <param name="shares">The buffer the proportions are written into.</param>
        /// <param name="reflectance">A scratch spectrum buffer.</param>
        /// <returns>The perceptual distance at the proportions found.</returns>
        internal static double SolveReusing(
            IReadOnlyList<PigmentCoefficients> subset,
            double targetL, double targetA, double targetB,
            double[] shares,
            double[] reflectance)
        {
            if (subset.Count == 1)
            {
                shares[0] = 1.0;
                return Distance(subset, shares, reflectance, targetL, targetA, targetB);
            }

            return Sweep(subset, targetL, targetA, targetB, shares, reflectance);
        }

        /// <summary>
        /// Sweeps and refines the concentration simplex of a two- or three-paint subset.
        /// </summary>
        /// <param name="subset">The paints in the subset.</param>
        /// <param name="targetL">The target's L*.</param>
        /// <param name="targetA">The target's a*.</param>
        /// <param name="targetB">The target's b*.</param>
        /// <param name="shares">The buffer the proportions are written into.</param>
        /// <param name="reflectance">A scratch spectrum buffer.</param>
        /// <returns>The perceptual distance at the proportions found.</returns>
        private static double Sweep(
            IReadOnlyList<PigmentCoefficients> subset,
            double targetL, double targetA, double targetB,
            double[] shares,
            double[] reflectance)
        {
            // The free parameters are one fewer than the paint count, because the shares
            // sum to 1. So a pair searches a line and a triple searches a triangle.
            var candidate = new double[subset.Count];
            var centre = new double[subset.Count - 1];
            var bestFree = new double[subset.Count - 1];

            for (int i = 0; i < centre.Length; i++)
            {
                centre[i] = 1.0 / subset.Count;
                bestFree[i] = centre[i];
            }

            int coarseDivisions = subset.Count == 2 ? LineCoarseDivisions : TriangleCoarseDivisions;
            double window = 1.0;
            double best = double.MaxValue;

            for (int pass = 0; pass <= RefinePasses; pass++)
            {
                int divisions = pass == 0 ? coarseDivisions : RefineDivisions;
                double low = pass == 0 ? 0.0 : Math.Max(0.0, centre[0] - window);
                double high = pass == 0 ? 1.0 : Math.Min(1.0, centre[0] + window);

                if (subset.Count == 2)
                {
                    for (int i = 0; i <= divisions; i++)
                    {
                        double first = low + ((high - low) * i / divisions);
                        candidate[0] = first;
                        candidate[1] = 1.0 - first;
                        if (candidate[1] < 0.0)
                        {
                            continue;
                        }

                        double distance = Distance(
                            subset, candidate, reflectance, targetL, targetA, targetB);
                        if (distance < best)
                        {
                            best = distance;
                            bestFree[0] = first;
                        }
                    }
                }
                else
                {
                    double secondLow = pass == 0 ? 0.0 : Math.Max(0.0, centre[1] - window);
                    double secondHigh = pass == 0 ? 1.0 : Math.Min(1.0, centre[1] + window);

                    for (int i = 0; i <= divisions; i++)
                    {
                        double first = low + ((high - low) * i / divisions);
                        for (int j = 0; j <= divisions; j++)
                        {
                            double second = secondLow + ((secondHigh - secondLow) * j / divisions);
                            double third = 1.0 - first - second;
                            if (third < 0.0)
                            {
                                continue;
                            }

                            candidate[0] = first;
                            candidate[1] = second;
                            candidate[2] = third;

                            double distance = Distance(
                                subset, candidate, reflectance, targetL, targetA, targetB);
                            if (distance < best)
                            {
                                best = distance;
                                bestFree[0] = first;
                                bestFree[1] = second;
                            }
                        }
                    }
                }

                Array.Copy(bestFree, centre, centre.Length);

                // The window has to span one whole step of the pass that just ran, or
                // refinement can exclude the very interval the true minimum sits in.
                window = pass == 0 ? 1.0 / coarseDivisions : window / RefineDivisions * 2.0;
            }

            shares[0] = centre[0];
            if (subset.Count == 2)
            {
                shares[1] = 1.0 - centre[0];
            }
            else
            {
                shares[1] = centre[1];
                shares[2] = 1.0 - centre[0] - centre[1];
            }

            // Refinement can leave a share a rounding error below zero at a simplex
            // edge; clamping and renormalising keeps the result a valid recipe.
            double total = 0.0;
            for (int i = 0; i < shares.Length; i++)
            {
                shares[i] = Math.Max(0.0, shares[i]);
                total += shares[i];
            }

            for (int i = 0; i < shares.Length; i++)
            {
                shares[i] /= total;
            }

            return Distance(subset, shares, reflectance, targetL, targetA, targetB);
        }

        /// <summary>
        /// Mixes a candidate and measures how far it lands from the target.
        /// </summary>
        /// <param name="subset">The paints being mixed.</param>
        /// <param name="shares">The candidate proportions.</param>
        /// <param name="reflectance">A scratch spectrum buffer.</param>
        /// <param name="targetL">The target's L*.</param>
        /// <param name="targetA">The target's a*.</param>
        /// <param name="targetB">The target's b*.</param>
        /// <returns>The perceptual distance.</returns>
        private static double Distance(
            IReadOnlyList<PigmentCoefficients> subset,
            double[] shares,
            double[] reflectance,
            double targetL, double targetA, double targetB)
        {
            KubelkaMunk.Mix(subset, shares, reflectance);
            SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

            return PaintBlendMatcher.PerceptualDistance(targetL, targetA, targetB, l, a, b);
        }
    }
}
