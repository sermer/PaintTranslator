using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Applies a Gaussian blur to a 32-bit ARGB pixel buffer, in linear light and
    /// separably: a horizontal pass followed by a vertical one, which costs 2r taps
    /// per pixel where the equivalent square kernel would cost r squared.
    /// <para>
    /// The averaging happens in linear light rather than on the sRGB-encoded channels
    /// the buffer stores. sRGB encoding is a power curve, so a mean taken across it is
    /// not the mean of the light being represented; blurring an edge in encoded space
    /// pulls the midpoint toward the darker side and leaves a visible dark seam wherever
    /// two bright colors meet. Decoding first costs one table lookup per channel and
    /// makes the blur physically the average it claims to be.
    /// </para>
    /// </summary>
    public static class GaussianBlur
    {
        // How many standard deviations the kernel extends before it is truncated. At
        // three the discarded tails hold 0.3% of the weight, far below what an 8-bit
        // channel can express, so the truncation is invisible while keeping the tap
        // count proportional to the radius the caller asked for.
        private const double RadiusInSigmas = 3.0;

        /// <summary>
        /// Blurs a pixel buffer in place. Alpha is left untouched, and each color
        /// channel is blurred independently.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="radius">The blur radius in pixels. Zero or less leaves the
        /// buffer exactly as it was.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pixels"/> is null.</exception>
        public static void Apply(
            int[] pixels,
            int strideInts,
            int width,
            int height,
            int radius,
            CancellationToken cancellationToken = default)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }
            if (radius <= 0 || width <= 0 || height <= 0)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            double[] kernel = BuildKernel(radius);

            // One channel at a time: the planes are the memory-hungry part of this, and
            // holding one channel's pair rather than all three keeps the peak at a third
            // of what a full-image float copy would need on a large photo.
            int count = width * height;
            float[] plane = ImageBufferPool.Float.Rent(count);
            float[] scratch = ImageBufferPool.Float.Rent(count);
            try
            {
                for (int shift = LinearPlanes.RedShift; shift >= LinearPlanes.BlueShift; shift -= 8)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    LinearPlanes.Decode(pixels, strideInts, width, height, shift, plane);
                    BlurHorizontal(plane, scratch, width, height, kernel, radius, cancellationToken);
                    BlurVertical(scratch, plane, width, height, kernel, radius, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    LinearPlanes.Encode(plane, pixels, strideInts, width, height, shift);
                }
            }
            finally
            {
                ImageBufferPool.Float.Return(plane);
                ImageBufferPool.Float.Return(scratch);
            }
        }

        /// <summary>
        /// Builds the normalized one-dimensional Gaussian kernel for a radius, sized
        /// so the radius spans <see cref="RadiusInSigmas"/> standard deviations.
        /// </summary>
        /// <param name="radius">The blur radius in pixels, greater than zero.</param>
        /// <returns>The 2r+1 weights, centered at index r and summing to one.</returns>
        private static double[] BuildKernel(int radius)
        {
            double sigma = radius / RadiusInSigmas;
            double denominator = 2.0 * sigma * sigma;

            var kernel = new double[(2 * radius) + 1];
            double total = 0.0;
            for (int offset = -radius; offset <= radius; offset++)
            {
                double weight = Math.Exp(-(offset * offset) / denominator);
                kernel[offset + radius] = weight;
                total += weight;
            }

            // Normalizing after truncation rather than using the analytic 1/(sigma*sqrt(2pi))
            // is what keeps a flat region flat: the weights must sum to exactly one, and
            // the tails this kernel dropped are not in the analytic constant.
            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] /= total;
            }

            return kernel;
        }

        /// <summary>
        /// Runs the kernel across each row of a plane.
        /// </summary>
        /// <param name="source">The plane to read.</param>
        /// <param name="destination">The plane to write, distinct from <paramref name="source"/>.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="kernel">The normalized kernel weights.</param>
        /// <param name="radius">The blur radius in pixels.</param>
        private static void BlurHorizontal(
            float[] source,
            float[] destination,
            int width,
            int height,
            double[] kernel,
            int radius,
            CancellationToken cancellationToken)
        {
            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, y =>
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    // Taps that fall off the end of the row repeat the edge pixel. Treating
                    // the outside as black instead would darken the border by however much
                    // of the kernel hangs over it.
                    double sum = 0.0;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        sum += kernel[offset + radius] * source[row + Math.Clamp(x + offset, 0, width - 1)];
                    }

                    destination[row + x] = (float)sum;
                }
            });
        }

        /// <summary>
        /// Runs the kernel down each column of a plane.
        /// </summary>
        /// <param name="source">The plane to read.</param>
        /// <param name="destination">The plane to write, distinct from <paramref name="source"/>.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="kernel">The normalized kernel weights.</param>
        /// <param name="radius">The blur radius in pixels.</param>
        private static void BlurVertical(
            float[] source,
            float[] destination,
            int width,
            int height,
            double[] kernel,
            int radius,
            CancellationToken cancellationToken)
        {
            // Accumulating a whole output row one source row at a time, rather than
            // gathering each pixel's 2r+1 taps where they sit, is what keeps this pass
            // from costing far more than the horizontal one: every read below is
            // sequential, where a per-pixel gather would stride a row width per tap and
            // miss the cache on every one of them.
            Parallel.For(0, height, new ParallelOptions
            {
                CancellationToken = cancellationToken,
            }, () => ArrayPool<double>.Shared.Rent(width), (y, state, row) =>
            {
                Array.Clear(row, 0, width);

                for (int offset = -radius; offset <= radius; offset++)
                {
                    double weight = kernel[offset + radius];
                    int sourceRow = Math.Clamp(y + offset, 0, height - 1) * width;
                    for (int x = 0; x < width; x++)
                    {
                        row[x] += weight * source[sourceRow + x];
                    }
                }

                int destinationRow = y * width;
                for (int x = 0; x < width; x++)
                {
                    destination[destinationRow + x] = (float)row[x];
                }

                return row;
            },
            row => ArrayPool<double>.Shared.Return(row));
        }
    }
}
