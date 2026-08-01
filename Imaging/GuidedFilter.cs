using System;
using System.Buffers;
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
            float[] image = ImageBufferPool.Float.Rent(count);
            float[] mean = ImageBufferPool.Float.Rent(count);
            float[] correlation = ImageBufferPool.Float.Rent(count);
            float[] slope = ImageBufferPool.Float.Rent(count);
            float[] offset = ImageBufferPool.Float.Rent(count);
            float[] scratch = ImageBufferPool.Float.Rent(count);
            try
            {
                for (int shift = LinearPlanes.RedShift; shift >= LinearPlanes.BlueShift; shift -= 8)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    LinearPlanes.Decode(pixels, strideInts, width, height, shift, image);

                    for (int pass = 0; pass < iterations; pass++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        BoxFilter(image, mean, scratch, width, height, radius, cancellationToken);
                        Square(image, correlation, width, height, cancellationToken);
                        BoxFilter(correlation, correlation, scratch, width, height, radius, cancellationToken);
                        CalculateCoefficients(
                            mean, correlation, slope, offset, width, height, epsilon, cancellationToken);
                        BoxFilter(slope, slope, scratch, width, height, radius, cancellationToken);
                        BoxFilter(offset, offset, scratch, width, height, radius, cancellationToken);
                        ApplyCoefficients(
                            image, slope, offset, width, height, cancellationToken);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    LinearPlanes.Encode(image, pixels, strideInts, width, height, shift);
                }
            }
            finally
            {
                ImageBufferPool.Float.Return(image);
                ImageBufferPool.Float.Return(mean);
                ImageBufferPool.Float.Return(correlation);
                ImageBufferPool.Float.Return(slope);
                ImageBufferPool.Float.Return(offset);
                ImageBufferPool.Float.Return(scratch);
            }
        }

        private static void Square(
            float[] image,
            float[] correlation,
            int width,
            int height,
            CancellationToken cancellationToken)
        {
            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, y =>
            {
                int first = y * width;
                int end = first + width;
                for (int i = first; i < end; i++)
                {
                    correlation[i] = image[i] * image[i];
                }
            });
        }

        private static void CalculateCoefficients(
            float[] mean,
            float[] correlation,
            float[] slope,
            float[] offset,
            int width,
            int height,
            double epsilon,
            CancellationToken cancellationToken)
        {
            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, y =>
            {
                int first = y * width;
                int end = first + width;
                for (int i = first; i < end; i++)
                {
                    // Clamped because independently computed means can leave the
                    // variance a hair below zero on a perfectly flat region.
                    double variance = Math.Max(
                        correlation[i] - ((double)mean[i] * mean[i]), 0.0);
                    double a = variance / (variance + epsilon);
                    slope[i] = (float)a;
                    offset[i] = (float)((1.0 - a) * mean[i]);
                }
            });
        }

        private static void ApplyCoefficients(
            float[] image,
            float[] slope,
            float[] offset,
            int width,
            int height,
            CancellationToken cancellationToken)
        {
            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, y =>
            {
                int first = y * width;
                int end = first + width;
                for (int i = first; i < end; i++)
                {
                    image[i] = (slope[i] * image[i]) + offset[i];
                }
            });
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

            const int ColumnsPerPartition = 128;
            int partitionCount = (width + ColumnsPerPartition - 1) / ColumnsPerPartition;
            Parallel.For(0, partitionCount, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, partition =>
            {
                int firstColumn = partition * ColumnsPerPartition;
                int lastColumn = Math.Min(firstColumn + ColumnsPerPartition, width);
                int columnCount = lastColumn - firstColumn;
                double[] running = ArrayPool<double>.Shared.Rent(columnCount);
                Array.Clear(running, 0, columnCount);
                try
                {
                    int initialRows = Math.Min(radius + 1, height);
                    for (int y = 0; y < initialRows; y++)
                    {
                        int row = (y * width) + firstColumn;
                        for (int column = 0; column < columnCount; column++)
                        {
                            running[column] += scratch[row + column];
                        }
                    }

                    for (int y = 0; y < height; y++)
                    {
                        int low = Math.Max(y - radius, 0);
                        int high = Math.Min(y + radius, height - 1);
                        int sampleCount = high - low + 1;
                        int destinationRow = (y * width) + firstColumn;
                        for (int column = 0; column < columnCount; column++)
                        {
                            destination[destinationRow + column] =
                                (float)(running[column] / sampleCount);
                        }

                        int leaving = y - radius;
                        if (leaving >= 0)
                        {
                            int leavingRow = (leaving * width) + firstColumn;
                            for (int column = 0; column < columnCount; column++)
                            {
                                running[column] -= scratch[leavingRow + column];
                            }
                        }

                        int arriving = y + radius + 1;
                        if (arriving < height)
                        {
                            int arrivingRow = (arriving * width) + firstColumn;
                            for (int column = 0; column < columnCount; column++)
                            {
                                running[column] += scratch[arrivingRow + column];
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<double>.Shared.Return(running);
                }
            });
        }
    }
}
