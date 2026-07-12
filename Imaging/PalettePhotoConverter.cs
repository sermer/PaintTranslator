using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Recreates a photo using only a given set of paints and their physical
    /// mixtures. The achievable gamut is sampled by blending the paints
    /// subtractively (via <see cref="SubtractivePaintMixer"/>) alone, in pairs,
    /// and in triples at several ratios; each pixel is then replaced with the
    /// achievable color nearest to it in CIELAB space, so "closest" matches
    /// human perception rather than raw RGB distance. Optionally the residual
    /// error of each substitution is diffused to neighboring pixels
    /// (Floyd-Steinberg), trading the flat posterized patches of plain nearest-
    /// color mapping for a slight texture whose local average tracks the
    /// original color.
    /// </summary>
    public static class PalettePhotoConverter
    {
        // Pixels are cached by their color quantized to 6 bits per channel: fine
        // enough that the 4-step rounding is invisible next to the snapping onto
        // the discrete mixture gamut, while capping the cache at 2^18 entries.
        private const int BitsPerChannel = 6;

        // Number of distinct cache keys: (2^6)^3 quantized colors.
        private const int CacheSize = 1 << (3 * BitsPerChannel);

        // Interior sample points of the two-paint mixing line, as the share of
        // the second paint. Endpoints are covered by the single-paint entries.
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

        /// <summary>
        /// Holds the sampled achievable-gamut colors: the sRGB value of each
        /// mixture alongside its precomputed CIELAB coordinates, stored as
        /// parallel arrays sorted by L* so the nearest-color search can prune
        /// candidates whose lightness alone puts them out of reach.
        /// </summary>
        private sealed class CandidateSet
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CandidateSet"/> class.
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
        }

        /// <summary>
        /// Converts a photo so every pixel uses only colors achievable by mixing
        /// the given paints, choosing the perceptually nearest achievable color
        /// for each pixel. Alpha is preserved from the source.
        /// </summary>
        /// <param name="source">The photo to convert; it is not modified.</param>
        /// <param name="paintColors">The mass-tone colors of the paints available for mixing.</param>
        /// <param name="dither">True to diffuse each substitution's residual error to
        /// neighboring pixels, smoothing gradients at the cost of a slight texture;
        /// false to map every pixel independently, giving flat color regions.</param>
        /// <returns>A new 32-bit ARGB bitmap containing the converted photo.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="paintColors"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="paintColors"/> is empty.</exception>
        public static Bitmap Convert(Bitmap source, IReadOnlyList<Color> paintColors, bool dither = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (paintColors == null)
            {
                throw new ArgumentNullException(nameof(paintColors));
            }
            if (paintColors.Count == 0)
            {
                throw new ArgumentException("At least one paint is required.", nameof(paintColors));
            }

            CandidateSet candidates = BuildCandidates(paintColors);

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
        /// Samples the gamut of colors achievable with the given paints: each
        /// paint alone, every pair at several mixing ratios, and every triple at
        /// a few interior weightings, all blended subtractively. Duplicate
        /// resulting colors are collapsed to keep the search set small.
        /// </summary>
        /// <param name="paintColors">The mass-tone colors of the available paints.</param>
        /// <returns>The deduplicated candidate colors with precomputed CIELAB coordinates.</returns>
        private static CandidateSet BuildCandidates(IReadOnlyList<Color> paintColors)
        {
            int count = paintColors.Count;

            // Mixing happens in absorbance space, so convert each paint once.
            var absorption = new double[count][];
            for (int i = 0; i < count; i++)
            {
                absorption[i] = SubtractivePaintMixer.ToAbsorption(paintColors[i]);
            }

            var seen = new HashSet<int>();
            var argbs = new List<int>();

            // Each paint straight from the tube.
            for (int i = 0; i < count; i++)
            {
                AddCandidate(paintColors[i], seen, argbs);
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
                        AddCandidate(mixed, seen, argbs);
                    }
                }
            }

            // Every unordered triple, sampled at interior points of its mixing
            // triangle; combined with the pair samples this covers the achievable
            // gamut densely enough that finer sampling is not visible.
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
                            AddCandidate(mixed, seen, argbs);
                        }
                    }
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

            // Order everything by L*: the nearest-color search walks outward from
            // the target's lightness and stops once the lightness gap alone
            // exceeds the best distance found, skipping most candidates.
            var order = new int[argbArray.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }
            var sortKeys = (double[])l.Clone();
            Array.Sort(sortKeys, order);

            var sortedArgb = new int[order.Length];
            var sortedL = new double[order.Length];
            var sortedA = new double[order.Length];
            var sortedB = new double[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                sortedArgb[i] = argbArray[order[i]];
                sortedL[i] = l[order[i]];
                sortedA[i] = a[order[i]];
                sortedB[i] = b[order[i]];
            }

            return new CandidateSet(sortedArgb, sortedL, sortedA, sortedB);
        }

        /// <summary>
        /// Records a candidate color unless an identical color is already present.
        /// </summary>
        /// <param name="color">The mixture color to record.</param>
        /// <param name="seen">The set of ARGB values already recorded.</param>
        /// <param name="argbs">The list of recorded candidate ARGB values.</param>
        private static void AddCandidate(Color color, HashSet<int> seen, List<int> argbs)
        {
            int argb = color.ToArgb();
            if (seen.Add(argb))
            {
                argbs.Add(argb);
            }
        }

        /// <summary>
        /// Finds the candidate color perceptually nearest (squared CIELAB
        /// distance) to a quantized source color. Walks the L*-sorted candidates
        /// outward from the target's lightness in both directions, stopping each
        /// direction once the lightness gap alone rules out everything beyond it.
        /// </summary>
        /// <param name="candidates">The achievable-gamut colors to search, sorted by L*.</param>
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
            int count = candL.Length;

            // Locate where the target's L* would insert into the sorted list;
            // the nearest candidate is likeliest near this position.
            int position = Array.BinarySearch(candL, targetL);
            if (position < 0)
            {
                position = ~position;
            }

            double bestDistance = double.MaxValue;
            int bestIndex = 0;
            int above = position;
            int below = position - 1;

            while (above < count || below >= 0)
            {
                if (above < count)
                {
                    double dl = candL[above] - targetL;

                    // Everything further up is even lighter, so once the L* gap
                    // alone exceeds the best distance this direction is done.
                    if (dl * dl >= bestDistance)
                    {
                        above = count;
                    }
                    else
                    {
                        double da = candA[above] - targetA;
                        double db = candB[above] - targetB;
                        double distance = dl * dl + da * da + db * db;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestIndex = above;
                        }
                        above++;
                    }
                }

                if (below >= 0)
                {
                    double dl = candL[below] - targetL;

                    // Same cutoff going darker: the L* gap only grows below here.
                    if (dl * dl >= bestDistance)
                    {
                        below = -1;
                    }
                    else
                    {
                        double da = candA[below] - targetA;
                        double db = candB[below] - targetB;
                        double distance = dl * dl + da * da + db * db;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestIndex = below;
                        }
                        below--;
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
        private static void RgbToLab(int r, int g, int b, out double labL, out double labA, out double labB)
        {
            double rl = SrgbToLinear(r);
            double gl = SrgbToLinear(g);
            double bl = SrgbToLinear(b);

            // Linear sRGB to CIE XYZ using the standard D65 matrix.
            double x = 0.4124564 * rl + 0.3575761 * gl + 0.1804375 * bl;
            double y = 0.2126729 * rl + 0.7151522 * gl + 0.0721750 * bl;
            double z = 0.0193339 * rl + 0.1191920 * gl + 0.9503041 * bl;

            // Normalize by the D65 reference white before the Lab transfer curve.
            double fx = LabTransfer(x / 0.95047);
            double fy = LabTransfer(y / 1.00000);
            double fz = LabTransfer(z / 1.08883);

            labL = 116.0 * fy - 16.0;
            labA = 500.0 * (fx - fy);
            labB = 200.0 * (fy - fz);
        }

        /// <summary>
        /// Applies the CIELAB transfer curve: a cube root with a linear segment
        /// near zero to keep the slope finite for very dark values.
        /// </summary>
        /// <param name="t">The white-point-normalized tristimulus value.</param>
        /// <returns>The transfer-curve output used by the L*, a*, b* formulas.</returns>
        private static double LabTransfer(double t)
        {
            const double Epsilon = 216.0 / 24389.0;
            const double Kappa = 24389.0 / 27.0;
            return t > Epsilon ? Math.Cbrt(t) : (Kappa * t + 16.0) / 116.0;
        }

        /// <summary>
        /// Decodes an 8-bit sRGB channel to linear light in [0, 1].
        /// </summary>
        /// <param name="channel">The sRGB-encoded channel value, 0 to 255.</param>
        /// <returns>The linear-light value of the channel.</returns>
        private static double SrgbToLinear(int channel)
        {
            double c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }
}
