using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Replaces the dominant border-connected region with a quieter paint candidate,
    /// making the largest open area read as a painted field rather than accidental
    /// leftover photo background.
    /// </summary>
    internal sealed class GroundFill : IPostMapStage
    {
        public string DisplayName => "Ground field";

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
            int[] labels = ImageBufferPool.Int.Rent(strideInts * height);
            try
            {
                Array.Fill(labels, -1, 0, strideInts * height);
                Refine(indices, labels, strideInts, width, height, candidates, in context);
            }
            finally
            {
                ImageBufferPool.Int.Return(labels);
            }
        }

        /// <summary>
        /// Finds the dominant border-connected region over a prepared label plane
        /// and rewrites it to a quieter candidate.
        /// </summary>
        private static void Refine(
            int[] indices, int[] labels, int strideInts, int width, int height,
            CandidateSet candidates, in RenderContext context)
        {
            int minimumArea = Math.Max(1, (int)Math.Ceiling(context.MarkPixels * context.MarkPixels * 4.0));
            var valuesByRegion = new List<int>();
            var areas = new List<int>();

            RegionLabeler.Label(
                indices, labels, strideInts, width, height,
                valuesByRegion, areas, context.CancellationToken);

            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            // A region touches the border exactly when one of its pixels lies on
            // it, so walking the four edges once marks every such region.
            var touchesBorder = new bool[valuesByRegion.Count];
            for (int x = 0; x < width; x++)
            {
                touchesBorder[labels[x]] = true;
                touchesBorder[labels[((height - 1) * strideInts) + x]] = true;
            }
            for (int y = 0; y < height; y++)
            {
                touchesBorder[labels[y * strideInts]] = true;
                touchesBorder[labels[(y * strideInts) + width - 1]] = true;
            }

            int field = -1;
            for (int region = 0; region < areas.Count; region++)
            {
                if (!touchesBorder[region] || areas[region] < minimumArea)
                {
                    continue;
                }

                if (field < 0 || areas[region] > areas[field])
                {
                    field = region;
                }
            }

            if (field < 0)
            {
                return;
            }

            // Every pixel of a region holds the same candidate, so the region's
            // colour is that candidate's own Lab coordinates.
            double fieldA = candidates.A[valuesByRegion[field]];
            double fieldB = candidates.B[valuesByRegion[field]];
            double chroma = Math.Sqrt((fieldA * fieldA) + (fieldB * fieldB));
            double targetChroma = Math.Min(chroma * 0.35, 25.0);
            double scale = chroma <= 1e-9 ? 0.0 : targetChroma / chroma;
            int replacement = candidates.FindNearest(58.0, fieldA * scale, fieldB * scale);
            for (int y = 0; y < height; y++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    int at = row + x;
                    if (labels[at] == field)
                    {
                        indices[at] = replacement;
                    }
                }
            }
        }
    }
}
