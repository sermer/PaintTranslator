using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Draws a narrow, paintable contour wherever neighbouring mapped regions meet.
    /// The stage writes only an existing candidate index, so the gamut invariant is
    /// preserved while the result gains Fauvist drawn boundaries.
    /// </summary>
    internal sealed class ContourLines : IPostMapStage
    {
        private const double MinimumBoundaryDeltaE = 12.0;

        public string DisplayName => "Contour lines";

        public IReadOnlyList<StyleParameter> Parameters { get; } = Array.Empty<StyleParameter>();

        public void Refine(
            int[] indices,
            int strideInts,
            int width,
            int height,
            CandidateSet candidates,
            in RenderContext context,
            ParameterValues values)
        {
            int radius = Math.Max(1, (int)Math.Round(context.MarkPixels * 0.10));
            // Fauvist contours are often a dark chromatic paint rather than a neutral
            // black. This target keeps the line legible while retaining colour when
            // the loaded palette contains a suitable blue, violet, or red.
            int lineIndex = candidates.FindNearest(35.0, 5.0, -15.0);
            // Copied out because an `in` parameter cannot be captured by the
            // row and column lambdas below.
            CancellationToken cancellationToken = context.CancellationToken;
            bool[] boundary = ImageBufferPool.Bool.Rent(strideInts * height);
            bool[] widened = ImageBufferPool.Bool.Rent(strideInts * height);

            try
            {
                Parallel.For(0, height, y =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    int row = y * strideInts;
                    for (int x = 0; x < width; x++)
                    {
                        int value = indices[row + x];
                        boundary[row + x] =
                            (x > 0 && IsStrongBoundary(value, indices[row + x - 1], candidates)) ||
                            (x + 1 < width && IsStrongBoundary(value, indices[row + x + 1], candidates)) ||
                            (y > 0 && IsStrongBoundary(value, indices[row - strideInts + x], candidates)) ||
                            (y + 1 < height && IsStrongBoundary(value, indices[row + strideInts + x], candidates));
                    }
                });

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // The square dilation window separates into a horizontal and a
                // vertical sliding-window pass, so widening the mask costs the same
                // whatever the radius. Each pass keeps a count of set pixels inside
                // its window rather than rescanning it per step.
                Parallel.For(0, height, y =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    int row = y * strideInts;
                    int inWindow = 0;
                    for (int x = 0; x < Math.Min(radius, width); x++)
                    {
                        inWindow += boundary[row + x] ? 1 : 0;
                    }
                    for (int x = 0; x < width; x++)
                    {
                        int entering = x + radius;
                        if (entering < width && boundary[row + entering])
                        {
                            inWindow++;
                        }

                        widened[row + x] = inWindow > 0;

                        int leaving = x - radius;
                        if (leaving >= 0 && boundary[row + leaving])
                        {
                            inWindow--;
                        }
                    }
                });

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Parallel.For(0, width, x =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    int inWindow = 0;
                    for (int y = 0; y < Math.Min(radius, height); y++)
                    {
                        inWindow += widened[(y * strideInts) + x] ? 1 : 0;
                    }
                    for (int y = 0; y < height; y++)
                    {
                        int entering = y + radius;
                        if (entering < height && widened[(entering * strideInts) + x])
                        {
                            inWindow++;
                        }

                        if (inWindow > 0)
                        {
                            indices[(y * strideInts) + x] = lineIndex;
                        }

                        int leaving = y - radius;
                        if (leaving >= 0 && widened[(leaving * strideInts) + x])
                        {
                            inWindow--;
                        }
                    }
                });
            }
            finally
            {
                ImageBufferPool.Bool.Return(boundary);
                ImageBufferPool.Bool.Return(widened);
            }
        }

        private static bool IsStrongBoundary(int left, int right, CandidateSet candidates)
        {
            if (left == right)
            {
                return false;
            }

            double dl = candidates.L[left] - candidates.L[right];
            double da = candidates.A[left] - candidates.A[right];
            double db = candidates.B[left] - candidates.B[right];

            // Compared in squared form; the square root of the distance would be
            // spent immediately on a threshold test.
            return (dl * dl) + (da * da) + (db * db) >= MinimumBoundaryDeltaE * MinimumBoundaryDeltaE;
        }
    }
}
