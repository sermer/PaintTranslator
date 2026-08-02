using System;
using System.Collections.Generic;

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
            var boundary = new bool[strideInts * height];

            for (int y = 0; y < height; y++)
            {
                if (context.CancellationToken.IsCancellationRequested)
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
            }

            for (int y = 0; y < height; y++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    bool nearBoundary = false;
                    for (int dy = -radius; dy <= radius && !nearBoundary; dy++)
                    {
                        int neighbourY = y + dy;
                        if (neighbourY < 0 || neighbourY >= height)
                        {
                            continue;
                        }

                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int neighbourX = x + dx;
                            if (neighbourX >= 0 && neighbourX < width && boundary[(neighbourY * strideInts) + neighbourX])
                            {
                                nearBoundary = true;
                                break;
                            }
                        }
                    }

                    if (nearBoundary)
                    {
                        indices[row + x] = lineIndex;
                    }
                }
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
            return Math.Sqrt((dl * dl) + (da * da) + (db * db)) >= MinimumBoundaryDeltaE;
        }
    }
}
