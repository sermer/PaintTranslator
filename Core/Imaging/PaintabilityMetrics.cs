using System;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Measures whether a converted image could have been painted, by asking how much
    /// of it lands in regions too small for a brush to have made.
    /// <para>
    /// This exists because the converter maps every pixel independently and therefore
    /// amplifies input noise. Measured on a synthetic gradient with a six-paint
    /// palette: with a noiseless source, 0.1% of pixels fall in regions of four pixels
    /// or fewer and the median region is 38 pixels. Add sensor noise at sigma 3 and
    /// the same source produces 92,326 regions with a median area of <em>one pixel</em>
    /// and 44.3% of pixels in regions of four or fewer. The picture is still made
    /// entirely of mixable colours — invariant I1 holds — and is still impossible to
    /// paint.
    /// </para>
    /// <para>
    /// Connectivity is four-way rather than eight-way on purpose: two pixels touching
    /// only at a corner are two marks, because no brush makes that join, and counting
    /// them as one would under-report exactly the speckle this is looking for.
    /// </para>
    /// </summary>
    public static class PaintabilityMetrics
    {
        /// <summary>
        /// Computes what share of the image lies in same-coloured regions below a
        /// given area — in practice, below one brushmark squared.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels to measure; not modified.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="minimumArea">The smallest region area that counts as paintable.
        /// Regions strictly smaller than this are counted against the image.</param>
        /// <returns>The fraction of pixels in regions smaller than
        /// <paramref name="minimumArea"/>, from 0 to 1.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pixels"/> is null.</exception>
        public static double FractionInRegionsSmallerThan(
            int[] pixels, int strideInts, int width, int height, int minimumArea)
        {
            long tooSmall = 0;
            ForEachRegion(pixels, strideInts, width, height, area =>
            {
                if (area < minimumArea)
                {
                    tooSmall += area;
                }
            });

            long total = (long)width * height;

            return total == 0 ? 0.0 : (double)tooSmall / total;
        }

        /// <summary>
        /// Counts the same-coloured four-connected regions in an image.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels to measure; not modified.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <returns>The number of distinct regions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pixels"/> is null.</exception>
        public static int CountRegions(int[] pixels, int strideInts, int width, int height)
        {
            int regions = 0;
            ForEachRegion(pixels, strideInts, width, height, _ => regions++);

            return regions;
        }

        /// <summary>
        /// Flood-fills every region once and reports each one's area.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels to walk.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="report">Called once per region with its area in pixels.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pixels"/> is null.</exception>
        private static void ForEachRegion(
            int[] pixels, int strideInts, int width, int height, Action<int> report)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var visited = new bool[width * height];

            // An explicit stack rather than recursion: a flat region on a large photo
            // can span millions of pixels, and the recursive form overflows long before
            // that.
            var stack = new int[width * height];

            for (int seed = 0; seed < visited.Length; seed++)
            {
                if (visited[seed])
                {
                    continue;
                }

                // Alpha is deliberately masked off. It varies for reasons that have
                // nothing to do with which paint went where, and a region of one flat
                // colour is one mark whatever its transparency.
                int colour = Colour(pixels, strideInts, width, seed);
                int top = 0;
                stack[top++] = seed;
                visited[seed] = true;
                int area = 0;

                while (top > 0)
                {
                    int at = stack[--top];
                    area++;
                    int x = at % width;
                    int y = at / width;

                    if (x > 0)
                    {
                        TryPush(at - 1);
                    }
                    if (x < width - 1)
                    {
                        TryPush(at + 1);
                    }
                    if (y > 0)
                    {
                        TryPush(at - width);
                    }
                    if (y < height - 1)
                    {
                        TryPush(at + width);
                    }
                }

                report(area);

                void TryPush(int index)
                {
                    if (visited[index] || Colour(pixels, strideInts, width, index) != colour)
                    {
                        return;
                    }

                    visited[index] = true;
                    stack[top++] = index;
                }
            }
        }

        /// <summary>
        /// Reads one pixel's colour by flat index, translating through the row stride
        /// so padding between rows is never read.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="index">The pixel's index in width-major order.</param>
        /// <returns>The pixel's color channels, with alpha masked away.</returns>
        private static int Colour(int[] pixels, int strideInts, int width, int index)
        {
            return pixels[((index / width) * strideInts) + (index % width)] & 0x00FFFFFF;
        }
    }
}
