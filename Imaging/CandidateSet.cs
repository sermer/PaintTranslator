using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Holds the sampled achievable-gamut colors: the sRGB value of each mixture
    /// alongside its precomputed CIELAB coordinates, indexed by a uniform grid over
    /// CIELAB so a nearest-color query examines only the cells near the target.
    /// <para>
    /// A grid rather than the sort by L* this replaced. Sorting on one axis prunes
    /// only by that axis, so its cost grows with the number of candidates sharing a
    /// lightness; sampling the mixing proportions finely enough to matter puts tens
    /// of thousands of colors in that band and the scan comes to dominate a
    /// conversion. The grid prunes on all three axes at once, which is what lets the
    /// sampling get dense without the conversion getting slow.
    /// </para>
    /// </summary>
    internal sealed class CandidateSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CandidateSet"/> class,
        /// building the grid index over the given candidates.
        /// </summary>
        /// <param name="argb">The 32-bit ARGB value of each candidate color.</param>
        /// <param name="l">The CIELAB L* of each candidate, index-aligned with <paramref name="argb"/>.</param>
        /// <param name="a">The CIELAB a* of each candidate, index-aligned with <paramref name="argb"/>.</param>
        /// <param name="b">The CIELAB b* of each candidate, index-aligned with <paramref name="argb"/>.</param>
        public CandidateSet(int[] argb, double[] l, double[] a, double[] b)
        {
            Argb = argb;
            L = l;
            A = a;
            B = b;

            // The occupied region of CIELAB is far smaller than the space itself —
            // no paint mixture is a saturated cyan — so the grid is fitted to the
            // candidates rather than to the axes' nominal ranges.
            MinL = Minimum(l);
            MinA = Minimum(a);
            MinB = Minimum(b);
            double spanL = Math.Max(Maximum(l) - MinL, 1e-6);
            double spanA = Math.Max(Maximum(a) - MinA, 1e-6);
            double spanB = Math.Max(Maximum(b) - MinB, 1e-6);

            int perAxis = (int)Math.Cbrt(Math.Max(argb.Length / CandidatesPerCell, 1.0));
            CellsPerAxis = Math.Clamp(perAxis, 1, MaximumCellsPerAxis);

            CellL = spanL / CellsPerAxis;
            CellA = spanA / CellsPerAxis;
            CellB = spanB / CellsPerAxis;
            SmallestCell = Math.Min(CellL, Math.Min(CellA, CellB));

            // Counting sort into compressed rows: one pass to size each cell, a
            // prefix sum for the offsets, then a pass to place the members. This
            // keeps the whole index in two flat arrays with no per-cell allocation.
            int cellCount = CellsPerAxis * CellsPerAxis * CellsPerAxis;
            CellStart = new int[cellCount + 1];
            Members = new int[argb.Length];

            var cellOf = new int[argb.Length];
            for (int i = 0; i < argb.Length; i++)
            {
                cellOf[i] = CellIndex(l[i], a[i], b[i]);
                CellStart[cellOf[i] + 1]++;
            }

            for (int cell = 0; cell < cellCount; cell++)
            {
                CellStart[cell + 1] += CellStart[cell];
            }

            var cursor = new int[cellCount];
            for (int i = 0; i < argb.Length; i++)
            {
                int cell = cellOf[i];
                Members[CellStart[cell] + cursor[cell]] = i;
                cursor[cell]++;
            }
        }

        // Average candidates per grid cell. Cells are cheap to skip and expensive to
        // over-fill, but too fine a grid spends the whole query walking empty shells,
        // so a couple of candidates per occupied cell balances the two.
        private const double CandidatesPerCell = 2.0;

        // The most cells the index will use along any one axis, which bounds its memory
        // at this cubed regardless of how many candidates were sampled.
        private const int MaximumCellsPerAxis = 64;

        /// <summary>
        /// Gets the 32-bit ARGB value of each candidate color.
        /// </summary>
        public int[] Argb { get; }

        /// <summary>
        /// Gets the CIELAB L* component of each candidate.
        /// </summary>
        public double[] L { get; }

        /// <summary>
        /// Gets the CIELAB a* component of each candidate.
        /// </summary>
        public double[] A { get; }

        /// <summary>
        /// Gets the CIELAB b* component of each candidate.
        /// </summary>
        public double[] B { get; }

        /// <summary>Gets how many cells the grid spans along each axis.</summary>
        public int CellsPerAxis { get; }

        /// <summary>Gets the L* of the grid's lower corner.</summary>
        public double MinL { get; }

        /// <summary>Gets the a* of the grid's lower corner.</summary>
        public double MinA { get; }

        /// <summary>Gets the b* of the grid's lower corner.</summary>
        public double MinB { get; }

        /// <summary>Gets one cell's extent along L*.</summary>
        public double CellL { get; }

        /// <summary>Gets one cell's extent along a*.</summary>
        public double CellA { get; }

        /// <summary>Gets one cell's extent along b*.</summary>
        public double CellB { get; }

        /// <summary>
        /// Gets the shortest of the three cell extents, which is what bounds how
        /// close a candidate in a distant shell could possibly be.
        /// </summary>
        public double SmallestCell { get; }

        /// <summary>
        /// Gets each cell's first offset into <see cref="Members"/>, with one extra
        /// entry at the end so a cell's extent is always the next offset minus its own.
        /// </summary>
        public int[] CellStart { get; }

        /// <summary>
        /// Gets the candidate indices ordered so that each cell's members are
        /// contiguous.
        /// </summary>
        public int[] Members { get; }

        /// <summary>
        /// Finds the candidate nearest to a Lab colour, weighting lightness so
        /// palette reduction preserves value structure before hue separation.
        /// </summary>
        public int FindNearest(double l, double a, double b, double lightnessWeight = 1.5)
        {
            int best = 0;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < L.Length; i++)
            {
                double dl = L[i] - l;
                double da = A[i] - a;
                double db = B[i] - b;
                double distance = (lightnessWeight * dl * dl) + (da * da) + (db * db);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Creates a candidate set containing the supplied unique indices.</summary>
        public CandidateSet Select(IReadOnlyCollection<int> selected)
        {
            if (selected == null)
            {
                throw new ArgumentNullException(nameof(selected));
            }

            var ordered = new List<int>(selected);
            ordered.Sort();

            var argb = new List<int>(ordered.Count);
            var l = new List<double>(ordered.Count);
            var a = new List<double>(ordered.Count);
            var b = new List<double>(ordered.Count);
            foreach (int index in ordered)
            {
                if (index < 0 || index >= Argb.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(selected));
                }

                argb.Add(Argb[index]);
                l.Add(L[index]);
                a.Add(A[index]);
                b.Add(B[index]);
            }

            return new CandidateSet(argb.ToArray(), l.ToArray(), a.ToArray(), b.ToArray());
        }

        /// <summary>
        /// Finds the grid cell a CIELAB color falls in.
        /// </summary>
        /// <param name="labL">The color's L*.</param>
        /// <param name="labA">The color's a*.</param>
        /// <param name="labB">The color's b*.</param>
        /// <returns>The flattened cell index.</returns>
        public int CellIndex(double labL, double labA, double labB)
        {
            return Flatten(
                AxisCell(labL, MinL, CellL),
                AxisCell(labA, MinA, CellA),
                AxisCell(labB, MinB, CellB));
        }

        /// <summary>
        /// Flattens a cell's three axis coordinates into a single index.
        /// </summary>
        /// <param name="cellL">The cell's coordinate along L*.</param>
        /// <param name="cellA">The cell's coordinate along a*.</param>
        /// <param name="cellB">The cell's coordinate along b*.</param>
        /// <returns>The flattened cell index.</returns>
        public int Flatten(int cellL, int cellA, int cellB)
        {
            return ((cellL * CellsPerAxis) + cellA) * CellsPerAxis + cellB;
        }

        /// <summary>
        /// Locates a value's cell along one axis, clamped so colors outside the
        /// sampled region fall into the nearest edge cell rather than off the grid.
        /// </summary>
        /// <param name="value">The coordinate value.</param>
        /// <param name="minimum">The axis's lower bound.</param>
        /// <param name="cell">The axis's cell extent.</param>
        /// <returns>The cell coordinate along that axis.</returns>
        public int AxisCell(double value, double minimum, double cell)
        {
            return Math.Clamp((int)((value - minimum) / cell), 0, CellsPerAxis - 1);
        }

        /// <summary>
        /// Finds the smallest value in an array.
        /// </summary>
        /// <param name="values">The values to scan.</param>
        /// <returns>The smallest value, or zero when the array is empty.</returns>
        private static double Minimum(double[] values)
        {
            double smallest = values.Length == 0 ? 0.0 : values[0];
            foreach (double value in values)
            {
                smallest = Math.Min(smallest, value);
            }

            return smallest;
        }

        /// <summary>
        /// Finds the largest value in an array.
        /// </summary>
        /// <param name="values">The values to scan.</param>
        /// <returns>The largest value, or zero when the array is empty.</returns>
        private static double Maximum(double[] values)
        {
            double largest = values.Length == 0 ? 0.0 : values[0];
            foreach (double value in values)
            {
                largest = Math.Max(largest, value);
            }

            return largest;
        }
    }
}
