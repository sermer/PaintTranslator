using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Smooths a pixel buffer while leaving edges intact, using He, Sun and Tang's
    /// guided filter with the image as its own guide.
    /// <para>
    /// This exists because the converter maps each pixel independently and so
    /// amplifies input noise — measured at 1.7x, turning sub-visible sensor noise into
    /// an output where 44% of pixels sit in regions of four pixels or fewer. Those
    /// regions are all legitimately mixable colours and none of them could be painted.
    /// A Gaussian strong enough to suppress that noise needs a radius of about 5 and
    /// softens every edge in the picture to get there; this leaves them.
    /// </para>
    /// <para>
    /// Per channel, in linear light, the filter is a local linear model:
    /// <c>q = a*I + b</c> with <c>a = var / (var + eps)</c> over a window. Where local
    /// variance far exceeds <c>eps</c> — an edge — <c>a</c> approaches one and the
    /// output is the input untouched. Where it is far below — flat noise — <c>a</c>
    /// approaches zero and the output is the local mean. Every window operation is a
    /// box filter computed by running sums, so the cost per pixel does not grow with
    /// the radius at all.
    /// </para>
    /// </summary>
    public static class GuidedFilter
    {
        /// <summary>
        /// The linear-light contrast a step must exceed to count as an edge rather
        /// than noise, at the default setting.
        /// <para>
        /// Five percent sits above the sensor noise measured on ordinary photographs
        /// and below the contrast of any edge a painter would treat as an edge. It is
        /// squared before use because the filter compares against a variance.
        /// </para>
        /// </summary>
        public const double DefaultEdgeThreshold = 0.05;

        /// <summary>
        /// Filters a pixel buffer in place. Alpha is untouched and each colour channel
        /// is filtered independently.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="radius">The window radius in pixels. Zero or less leaves the
        /// buffer exactly as it was.</param>
        /// <param name="edgeThreshold">The linear-light contrast above which a step is
        /// preserved rather than smoothed.</param>
        /// <param name="iterations">How many times to run the filter, feeding each
        /// result back as the next guide. One denoises; more flatten progressively.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pixels"/> is null.</exception>
        public static void Apply(
            int[] pixels, int strideInts, int width, int height,
            int radius, double edgeThreshold, int iterations,
            CancellationToken cancellationToken = default)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }
            if (radius <= 0 || iterations <= 0 || width <= 0 || height <= 0)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Comparing against a variance, not against a difference, so the threshold
            // is squared exactly once here rather than at every pixel.
            double epsilon = edgeThreshold * edgeThreshold;

            // One channel at a time keeps peak memory at a third of what holding all
            // three planes would need on a large photo.
            int count = width * height;
            var image = new float[count];
            var mean = new float[count];
            var correlation = new float[count];
            var slope = new float[count];
            var offset = new float[count];
            var scratch = new float[count];

            for (int shift = LinearPlanes.RedShift; shift >= LinearPlanes.BlueShift; shift -= 8)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LinearPlanes.Decode(pixels, strideInts, width, height, shift, image);

                for (int pass = 0; pass < iterations; pass++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    BoxFilter(image, mean, scratch, width, height, radius, cancellationToken);

                    for (int i = 0; i < count; i++)
                    {
                        if ((i & 16383) == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        correlation[i] = image[i] * image[i];
                    }

                    BoxFilter(correlation, correlation, scratch, width, height, radius, cancellationToken);

                    for (int i = 0; i < count; i++)
                    {
                        if ((i & 16383) == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        // Clamped at zero because the two box filters are computed
                        // independently and their difference can land a hair below it
                        // on a perfectly flat region.
                        double variance = Math.Max(correlation[i] - ((double)mean[i] * mean[i]), 0.0);
                        double a = variance / (variance + epsilon);
                        slope[i] = (float)a;
                        offset[i] = (float)((1.0 - a) * mean[i]);
                    }

                    BoxFilter(slope, slope, scratch, width, height, radius, cancellationToken);
                    BoxFilter(offset, offset, scratch, width, height, radius, cancellationToken);

                    for (int i = 0; i < count; i++)
                    {
                        if ((i & 16383) == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        image[i] = (slope[i] * image[i]) + offset[i];
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                LinearPlanes.Encode(image, pixels, strideInts, width, height, shift);
            }
        }

        /// <summary>
        /// Averages each pixel over a square window, separably.
        /// <para>
        /// Each output divides by how many samples the window actually covered rather
        /// than by its nominal size, so a flat region stays flat right up to the
        /// border. Dividing by the nominal size instead would pull every edge of the
        /// image toward zero and leave a dark frame around every result.
        /// </para>
        /// </summary>
        /// <param name="source">The plane to read. May alias <paramref name="destination"/>.</param>
        /// <param name="destination">The plane to write.</param>
        /// <param name="scratch">A working plane, distinct from both others.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="radius">The window radius in pixels.</param>
        private static void BoxFilter(
            float[] source,
            float[] destination,
            float[] scratch,
            int width,
            int height,
            int radius,
            CancellationToken cancellationToken)
        {
            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, y =>
            {
                int row = y * width;
                double running = 0.0;
                for (int x = 0; x < Math.Min(radius + 1, width); x++)
                {
                    running += source[row + x];
                }

                for (int x = 0; x < width; x++)
                {
                    int low = Math.Max(x - radius, 0);
                    int high = Math.Min(x + radius, width - 1);
                    scratch[row + x] = (float)(running / (high - low + 1));

                    int leaving = x - radius;
                    int arriving = x + radius + 1;
                    if (leaving >= 0)
                    {
                        running -= source[row + leaving];
                    }
                    if (arriving < width)
                    {
                        running += source[row + arriving];
                    }
                }
            });

            Parallel.For(0, width, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, x =>
            {
                double running = 0.0;
                for (int y = 0; y < Math.Min(radius + 1, height); y++)
                {
                    running += scratch[(y * width) + x];
                }

                for (int y = 0; y < height; y++)
                {
                    int low = Math.Max(y - radius, 0);
                    int high = Math.Min(y + radius, height - 1);
                    destination[(y * width) + x] = (float)(running / (high - low + 1));

                    int leaving = y - radius;
                    int arriving = y + radius + 1;
                    if (leaving >= 0)
                    {
                        running -= scratch[(leaving * width) + x];
                    }
                    if (arriving < height)
                    {
                        running += scratch[(arriving * width) + x];
                    }
                }
            });
        }
    }
}
