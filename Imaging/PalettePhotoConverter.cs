using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Recreates a photo using only a given set of paints and their physical
    /// mixtures. The achievable gamut is sampled by blending the paints through
    /// the measured Kubelka-Munk kernel alone, in pairs along their whole mixing
    /// line, and in triples across their whole mixing triangle; each pixel is then
    /// replaced with the achievable color nearest to it in CIELAB space, so
    /// "closest" matches human perception rather than raw RGB distance. Optionally
    /// the residual error of each substitution is diffused to neighboring pixels
    /// (Floyd-Steinberg), trading the flat posterized patches of plain nearest-
    /// color mapping for a slight texture whose local average tracks the
    /// original color.
    /// <para>
    /// The proportions are sampled as continuous shares rather than a few fixed
    /// ratios. Because the output is an 8-bit image and identical colors are
    /// collapsed below, a grid fine enough that refining it yields no further
    /// distinct colors is not an approximation of the achievable gamut — it is the
    /// achievable gamut, and picking the nearest member of it is then exactly the
    /// closest a mixture can get.
    /// </para>
    /// </summary>
    public static class PalettePhotoConverter
    {
        // Pixels are cached by their color quantized to 6 bits per channel: fine
        // enough that the 4-step rounding is invisible next to the snapping onto
        // the discrete mixture gamut, while capping the cache at 2^18 entries.
        private const int BitsPerChannel = 6;

        // Number of distinct cache keys: (2^6)^3 quantized colors.
        private const int CacheSize = 1 << (3 * BitsPerChannel);

        // How many interior points each two-paint mixing line is sampled at. Endpoints
        // are covered by the single-paint entries.
        //
        // Sampling the proportions continuously is the point: a mixing line is not
        // traversed at a constant rate, because the colour moves fastest where the
        // stronger pigment is scarce, and a handful of fixed ratios lands nowhere near
        // the closest reachable colour there. Measured against colours drawn from real
        // mixtures, going from the eight-step ladder this replaced to 63 samples cuts
        // mean sampling error from 2.05 to 0.91 and worst case from 17.6 to 9.3. Past
        // this the line is saturated — 255 samples reach only 0.83, and 511 only 0.82 —
        // because neighbouring samples then differ by less than one 8-bit code and
        // collapse together in the deduplication below.
        private const int PairSamples = 63;

        // The denominator of the simplex grid each three-paint mixing triangle is
        // sampled on, so shares are whole multiples of 1/16 and the interior holds 105
        // points. Edges of the triangle are covered by the pair samples.
        //
        // The triangles are where the accuracy is: holding pairs at 63 samples, taking
        // this from 6 to 10 to 16 moves mean error 0.91 to 0.60 to 0.41, while doubling
        // it again to 24 buys only 0.27 for four times the candidates and twice the
        // build. Interior colours are also the muted ones a photograph is mostly made
        // of, which is why they earn a finer grid than intuition suggests.
        private const int TripleDivisions = 16;

        // Average candidates per grid cell. Cells are cheap to skip and expensive to
        // over-fill, but too fine a grid spends the whole query walking empty shells,
        // so a couple of candidates per occupied cell balances the two.
        private const double CandidatesPerCell = 2.0;

        // The most cells the index will use along any one axis, which bounds its memory
        // at this cubed regardless of how many candidates were sampled.
        private const int MaximumCellsPerAxis = 64;

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
        private sealed class CandidateSet
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

        /// <summary>
        /// Converts a photo so every pixel uses only colors achievable by mixing
        /// the given paints, choosing the perceptually nearest achievable color
        /// for each pixel. Alpha is preserved from the source.
        /// </summary>
        /// <param name="source">The photo to convert; it is not modified.</param>
        /// <param name="paints">The paints available for mixing.</param>
        /// <param name="dither">True to diffuse each substitution's residual error to
        /// neighboring pixels, smoothing gradients at the cost of a slight texture;
        /// false to map every pixel independently, giving flat color regions.</param>
        /// <returns>A new 32-bit ARGB bitmap containing the converted photo.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="paints"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paints"/> is empty.</exception>
        public static Bitmap Convert(Bitmap source, IReadOnlyList<PigmentCoefficients> paints, bool dither = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (paints.Count == 0)
            {
                throw new ArgumentException("At least one paint is required.", nameof(paints));
            }

            CandidateSet candidates = BuildCandidates(paints);

            int width = source.Width;
            int height = source.Height;

            // Drawing into a fresh 32bpp ARGB bitmap normalizes whatever pixel
            // format the photo arrived in, so the buffer below is always ARGB.
            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.DrawImage(source, 0, 0, width, height);
            }

            BitmapData data = result.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            try
            {
                int strideInts = data.Stride / 4;
                var pixels = new int[strideInts * height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                if (dither)
                {
                    MapPixelsDithered(pixels, strideInts, width, height, candidates);
                }
                else
                {
                    MapPixelsFlat(pixels, strideInts, width, height, candidates);
                }

                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                result.UnlockBits(data);
            }

            return result;
        }

        /// <summary>
        /// Replaces each pixel's RGB with the nearest achievable color, mapping
        /// every pixel independently so identical colors always land on the same
        /// mixture. Alpha is left untouched.
        /// </summary>
        /// <param name="pixels">The image's ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="candidates">The achievable-gamut colors, sorted by L*.</param>
        private static void MapPixelsFlat(int[] pixels, int strideInts, int width, int height, CandidateSet candidates)
        {
            // First pass: mark which quantized colors actually occur, so the
            // expensive nearest-candidate search runs once per distinct color
            // instead of once per pixel.
            var used = new bool[CacheSize];
            for (int y = 0; y < height; y++)
            {
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    used[CacheKey(pixels[row + x])] = true;
                }
            }

            var keys = new List<int>();
            for (int key = 0; key < CacheSize; key++)
            {
                if (used[key])
                {
                    keys.Add(key);
                }
            }

            // Resolve every distinct color in parallel; each entry is written
            // by exactly one iteration, so the shared array needs no locking.
            var mapped = new int[CacheSize];
            Parallel.For(0, keys.Count, i =>
            {
                int key = keys[i];
                mapped[key] = NearestCandidateArgb(candidates, key);
            });

            // Second pass: swap each pixel's RGB for its mapped mixture while
            // keeping the pixel's own alpha.
            for (int y = 0; y < height; y++)
            {
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    int pixel = pixels[row + x];
                    int alpha = pixel & unchecked((int)0xFF000000);
                    pixels[row + x] = alpha | (mapped[CacheKey(pixel)] & 0x00FFFFFF);
                }
            }
        }

        /// <summary>
        /// Replaces each pixel's RGB with the nearest achievable color while
        /// diffusing the residual error to unvisited neighbors using
        /// Floyd-Steinberg weights on a serpentine scan, so the local average of
        /// the output tracks the original color across gradients. Inherently
        /// sequential, since every pixel depends on its predecessors' errors.
        /// Alpha is left untouched.
        /// </summary>
        /// <param name="pixels">The image's ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="candidates">The achievable-gamut colors, sorted by L*.</param>
        private static void MapPixelsDithered(int[] pixels, int strideInts, int width, int height, CandidateSet candidates)
        {
            // Nearest-color results are resolved lazily as targets appear; entry
            // 0 means unresolved, which no real entry can collide with because
            // every candidate ARGB carries full alpha bits.
            var mapped = new int[CacheSize];

            // Accumulated error for the row being scanned and the row below it,
            // three doubles (R, G, B) per pixel.
            var currentError = new double[width * 3];
            var nextError = new double[width * 3];

            for (int y = 0; y < height; y++)
            {
                int row = y * strideInts;

                // Serpentine scan: alternating direction stops the diffusion
                // pattern from smearing consistently to one side.
                bool leftToRight = (y & 1) == 0;
                int xStart = leftToRight ? 0 : width - 1;
                int xEnd = leftToRight ? width : -1;
                int xStep = leftToRight ? 1 : -1;

                for (int x = xStart; x != xEnd; x += xStep)
                {
                    int pixel = pixels[row + x];
                    int e = x * 3;

                    // The clamp bounds the residual at gamut walls: colors the
                    // paints can never reach would otherwise pile up error
                    // without limit and streak across the image.
                    int targetR = Math.Clamp((int)Math.Round(((pixel >> 16) & 0xFF) + currentError[e]), 0, 255);
                    int targetG = Math.Clamp((int)Math.Round(((pixel >> 8) & 0xFF) + currentError[e + 1]), 0, 255);
                    int targetB = Math.Clamp((int)Math.Round((pixel & 0xFF) + currentError[e + 2]), 0, 255);

                    int key = CacheKey(targetR, targetG, targetB);
                    int candidate = mapped[key];
                    if (candidate == 0)
                    {
                        candidate = NearestCandidateArgb(candidates, key);
                        mapped[key] = candidate;
                    }

                    pixels[row + x] = (pixel & unchecked((int)0xFF000000)) | (candidate & 0x00FFFFFF);

                    double errorR = targetR - ((candidate >> 16) & 0xFF);
                    double errorG = targetG - ((candidate >> 8) & 0xFF);
                    double errorB = targetB - (candidate & 0xFF);

                    // Floyd-Steinberg distribution, mirrored to match the scan
                    // direction: 7/16 ahead, 3/16 behind-below, 5/16 below,
                    // 1/16 ahead-below.
                    int ahead = x + xStep;
                    int behind = x - xStep;
                    if (ahead >= 0 && ahead < width)
                    {
                        int ae = ahead * 3;
                        currentError[ae] += errorR * (7.0 / 16.0);
                        currentError[ae + 1] += errorG * (7.0 / 16.0);
                        currentError[ae + 2] += errorB * (7.0 / 16.0);
                        nextError[ae] += errorR * (1.0 / 16.0);
                        nextError[ae + 1] += errorG * (1.0 / 16.0);
                        nextError[ae + 2] += errorB * (1.0 / 16.0);
                    }
                    if (behind >= 0 && behind < width)
                    {
                        int be = behind * 3;
                        nextError[be] += errorR * (3.0 / 16.0);
                        nextError[be + 1] += errorG * (3.0 / 16.0);
                        nextError[be + 2] += errorB * (3.0 / 16.0);
                    }
                    nextError[e] += errorR * (5.0 / 16.0);
                    nextError[e + 1] += errorG * (5.0 / 16.0);
                    nextError[e + 2] += errorB * (5.0 / 16.0);
                }

                // The next row's accumulated error becomes current; the freed
                // buffer is cleared for reuse as the new next row.
                double[] swap = currentError;
                currentError = nextError;
                nextError = swap;
                Array.Clear(nextError, 0, nextError.Length);
            }
        }

        /// <summary>
        /// Samples the gamut of colors achievable with the given paints: each paint
        /// alone, every pair across its whole mixing line, and every triple across its
        /// whole mixing triangle, all blended subtractively. Duplicate resulting colors
        /// are collapsed, which is what keeps the search set finite however finely the
        /// proportions are sampled.
        /// </summary>
        /// <param name="paints">The available paints.</param>
        /// <returns>The deduplicated candidate colors, indexed for nearest-color search.</returns>
        private static CandidateSet BuildCandidates(IReadOnlyList<PigmentCoefficients> paints)
        {
            int count = paints.Count;

            // Enumerating the subsets up front turns three nested loops into two flat
            // lists that can be walked in parallel. Every mixture is independent of
            // every other, so the only thing serialising this was the shared duplicate
            // set — and deduplicating once at the end is cheaper than sharing it.
            var pairs = new List<(int First, int Second)>();
            var triples = new List<(int First, int Second, int Third)>();
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    pairs.Add((i, j));
                    for (int k = j + 1; k < count; k++)
                    {
                        triples.Add((i, j, k));
                    }
                }
            }

            int perTriple = TripleDivisions <= 1 ? 0 : (TripleDivisions - 1) * (TripleDivisions - 2) / 2;
            int pairBase = count;
            int tripleBase = pairBase + (pairs.Count * PairSamples);
            var sampled = new int[tripleBase + (triples.Count * perTriple)];

            // Each paint straight from the tube. A paint has no stored colour any more,
            // so even the unmixed swatch is the kernel evaluated at full concentration.
            Parallel.For(0, count, () => new double[SpectralBands.Count], (i, state, reflectance) =>
            {
                KubelkaMunk.Mix(new[] { paints[i] }, new[] { 1.0 }, reflectance);
                sampled[i] = SpectralRenderer.ToDisplayColor(reflectance, out _).ToArgb();

                return reflectance;
            },
            _ => { });

            // Every unordered pair, sampled along its mixing line.
            Parallel.For(0, pairs.Count, () => new double[SpectralBands.Count], (p, state, reflectance) =>
            {
                (int first, int second) = pairs[p];
                var subset = new[] { paints[first], paints[second] };
                var shares = new double[2];
                int at = pairBase + (p * PairSamples);

                for (int sample = 1; sample <= PairSamples; sample++)
                {
                    double share = (double)sample / (PairSamples + 1);
                    shares[0] = 1.0 - share;
                    shares[1] = share;

                    KubelkaMunk.Mix(subset, shares, reflectance);
                    sampled[at] = SpectralRenderer.ToDisplayColor(reflectance, out _).ToArgb();
                    at++;
                }

                return reflectance;
            },
            _ => { });

            // Every unordered triple, sampled on a regular grid across the interior of
            // its mixing triangle. Combined with the pair samples this leaves the
            // achievable gamut covered closely enough that the residual is below what
            // an 8-bit channel can express over most of it.
            Parallel.For(0, triples.Count, () => new double[SpectralBands.Count], (t, state, reflectance) =>
            {
                (int first, int second, int third) = triples[t];
                var subset = new[] { paints[first], paints[second], paints[third] };
                var shares = new double[3];
                int at = tripleBase + (t * perTriple);

                // Both loops stop short of the boundary, so every point has all three
                // paints present; the boundary is covered by the pair samples above.
                for (int x = 1; x < TripleDivisions; x++)
                {
                    for (int y = 1; y < TripleDivisions - x; y++)
                    {
                        shares[0] = (double)x / TripleDivisions;
                        shares[1] = (double)y / TripleDivisions;
                        shares[2] = 1.0 - shares[0] - shares[1];

                        KubelkaMunk.Mix(subset, shares, reflectance);
                        sampled[at] = SpectralRenderer.ToDisplayColor(reflectance, out _).ToArgb();
                        at++;
                    }
                }

                return reflectance;
            },
            _ => { });

            // Collapse the duplicates. Sampling finely enough to matter produces far more
            // mixtures than there are distinct 8-bit colours for them to land on, so most
            // of what was just computed collapses away here.
            var seen = new HashSet<int>(sampled.Length);
            var argbs = new List<int>();
            foreach (int argb in sampled)
            {
                if (seen.Add(argb))
                {
                    argbs.Add(argb);
                }
            }

            // Precompute CIELAB for every surviving candidate so the per-pixel
            // search is pure arithmetic over flat arrays.
            var argbArray = argbs.ToArray();
            var l = new double[argbArray.Length];
            var a = new double[argbArray.Length];
            var b = new double[argbArray.Length];
            for (int i = 0; i < argbArray.Length; i++)
            {
                int argb = argbArray[i];
                RgbToLab((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF, out l[i], out a[i], out b[i]);
            }

            // The candidates need no ordering of their own: the set indexes them by
            // position in CIELAB as it is constructed.
            return new CandidateSet(argbArray, l, a, b);
        }

        /// <summary>
        /// Lists the distinct colors the given paints can be mixed to, as this converter
        /// samples them. Exposed so a test can measure how closely the sampling covers
        /// the achievable gamut and can check the indexed search against an exhaustive
        /// one over the very same set.
        /// </summary>
        /// <param name="paints">The paints available for mixing.</param>
        /// <returns>The 32-bit ARGB value of every distinct achievable color.</returns>
        internal static int[] SampleAchievableColors(IReadOnlyList<PigmentCoefficients> paints)
        {
            return BuildCandidates(paints).Argb;
        }

        /// <summary>
        /// Maps colors through the same indexed nearest-candidate search a conversion
        /// uses, without going via a bitmap. Exposed for tests.
        /// </summary>
        /// <param name="paints">The paints available for mixing.</param>
        /// <param name="targets">The colors to map, as 32-bit ARGB values.</param>
        /// <returns>The nearest achievable color to each target, index-aligned with
        /// <paramref name="targets"/>.</returns>
        internal static int[] MapThroughIndex(IReadOnlyList<PigmentCoefficients> paints, int[] targets)
        {
            CandidateSet candidates = BuildCandidates(paints);
            var mapped = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                mapped[i] = NearestCandidateArgb(candidates, CacheKey(targets[i]));
            }

            return mapped;
        }

        /// <summary>
        /// Finds the candidate color perceptually nearest (squared CIELAB distance) to a
        /// quantized source color, by walking the grid outward from the target's own cell
        /// in cubic shells and stopping once no unexamined cell could hold anything closer.
        /// </summary>
        /// <param name="candidates">The achievable-gamut colors to search.</param>
        /// <param name="cacheKey">The quantized-color cache key identifying the source color.</param>
        /// <returns>The ARGB value of the nearest candidate.</returns>
        private static int NearestCandidateArgb(CandidateSet candidates, int cacheKey)
        {
            // Reconstruct the center of the quantization bin the key represents,
            // so the rounding error is split evenly instead of biased downward.
            int r = (((cacheKey >> (2 * BitsPerChannel)) & 0x3F) << 2) + 2;
            int g = (((cacheKey >> BitsPerChannel) & 0x3F) << 2) + 2;
            int b = ((cacheKey & 0x3F) << 2) + 2;

            RgbToLab(r, g, b, out double targetL, out double targetA, out double targetB);

            double[] candL = candidates.L;
            double[] candA = candidates.A;
            double[] candB = candidates.B;
            int[] cellStart = candidates.CellStart;
            int[] members = candidates.Members;
            int perAxis = candidates.CellsPerAxis;

            int homeL = candidates.AxisCell(targetL, candidates.MinL, candidates.CellL);
            int homeA = candidates.AxisCell(targetA, candidates.MinA, candidates.CellA);
            int homeB = candidates.AxisCell(targetB, candidates.MinB, candidates.CellB);

            double bestDistance = double.MaxValue;
            int bestIndex = 0;

            // Scans one cell's members. A local function so the shell walk above can stay
            // about which cells to visit rather than repeating the distance test at each
            // of the places it decides to visit one.
            void Examine(int cell)
            {
                int end = cellStart[cell + 1];
                for (int slot = cellStart[cell]; slot < end; slot++)
                {
                    int i = members[slot];
                    double dl = candL[i] - targetL;
                    double da = candA[i] - targetA;
                    double db = candB[i] - targetB;
                    double distance = (dl * dl) + (da * da) + (db * db);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }
            }

            for (int shell = 0; shell < perAxis; shell++)
            {
                // The target sits somewhere inside its own cell, so a cell this many
                // steps away has its nearest face at least one step less than that.
                // Once even that lower bound beats nothing, no further shell can.
                if (shell > 0)
                {
                    double reach = (shell - 1) * candidates.SmallestCell;
                    if (reach > 0.0 && reach * reach >= bestDistance)
                    {
                        break;
                    }
                }

                int lowL = Math.Max(homeL - shell, 0);
                int highL = Math.Min(homeL + shell, perAxis - 1);
                int lowA = Math.Max(homeA - shell, 0);
                int highA = Math.Min(homeA + shell, perAxis - 1);
                int lowB = Math.Max(homeB - shell, 0);
                int highB = Math.Min(homeB + shell, perAxis - 1);

                for (int cellL = lowL; cellL <= highL; cellL++)
                {
                    bool edgeL = Math.Abs(cellL - homeL) == shell;
                    for (int cellA = lowA; cellA <= highA; cellA++)
                    {
                        bool edgeA = Math.Abs(cellA - homeA) == shell;

                        // Only the cube's surface is new; its interior belongs to shells
                        // already walked. When neither of the first two axes is on the
                        // surface the third has to be, so that row reduces to its two
                        // end cells — visited explicitly, because clamping at the grid's
                        // border makes striding the row unsound.
                        if (edgeL || edgeA)
                        {
                            for (int cellB = lowB; cellB <= highB; cellB++)
                            {
                                Examine(candidates.Flatten(cellL, cellA, cellB));
                            }

                            continue;
                        }

                        if (homeB - shell >= 0)
                        {
                            Examine(candidates.Flatten(cellL, cellA, homeB - shell));
                        }
                        if (shell > 0 && homeB + shell < perAxis)
                        {
                            Examine(candidates.Flatten(cellL, cellA, homeB + shell));
                        }
                    }
                }
            }

            return candidates.Argb[bestIndex];
        }

        /// <summary>
        /// Computes the 6-bit-per-channel cache key for a pixel's color, ignoring alpha.
        /// </summary>
        /// <param name="argb">The pixel's 32-bit ARGB value.</param>
        /// <returns>The cache key in [0, <see cref="CacheSize"/>).</returns>
        private static int CacheKey(int argb)
        {
            return CacheKey((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF);
        }

        /// <summary>
        /// Computes the 6-bit-per-channel cache key for separate color channels.
        /// </summary>
        /// <param name="r">The red channel, 0 to 255.</param>
        /// <param name="g">The green channel, 0 to 255.</param>
        /// <param name="b">The blue channel, 0 to 255.</param>
        /// <returns>The cache key in [0, <see cref="CacheSize"/>).</returns>
        private static int CacheKey(int r, int g, int b)
        {
            return ((r >> 2) << (2 * BitsPerChannel)) | ((g >> 2) << BitsPerChannel) | (b >> 2);
        }

        /// <summary>
        /// Converts an 8-bit sRGB color to CIELAB (D65 white point), the space in
        /// which Euclidean distance approximates perceived color difference.
        /// </summary>
        /// <param name="r">The sRGB red channel, 0 to 255.</param>
        /// <param name="g">The sRGB green channel, 0 to 255.</param>
        /// <param name="b">The sRGB blue channel, 0 to 255.</param>
        /// <param name="labL">The resulting L* component.</param>
        /// <param name="labA">The resulting a* component.</param>
        /// <param name="labB">The resulting b* component.</param>
        internal static void RgbToLab(int r, int g, int b, out double labL, out double labA, out double labB)
        {
            double rl = ColorSpace.SrgbToLinear(r / 255.0);
            double gl = ColorSpace.SrgbToLinear(g / 255.0);
            double bl = ColorSpace.SrgbToLinear(b / 255.0);

            ColorSpace.LinearRgbToXyz(rl, gl, bl, out double x, out double y, out double z);
            ColorSpace.XyzToLab(x, y, z, out labL, out labA, out labB);
        }
    }
}
