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
            int minimumArea = Math.Max(1, (int)Math.Ceiling(context.MarkPixels * context.MarkPixels * 4.0));
            var labels = new int[strideInts * height];
            Array.Fill(labels, -1);
            var regions = new List<Region>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int at = (y * strideInts) + x;
                    if (labels[at] >= 0)
                    {
                        continue;
                    }

                    int label = regions.Count;
                    int value = indices[at];
                    var pixels = new List<int>();
                    var queue = new Queue<(int X, int Y)>();
                    queue.Enqueue((x, y));
                    labels[at] = label;
                    double sumL = 0.0;
                    double sumA = 0.0;
                    double sumB = 0.0;
                    bool touchesBorder = false;

                    while (queue.Count > 0)
                    {
                        (int currentX, int currentY) = queue.Dequeue();
                        int current = (currentY * strideInts) + currentX;
                        pixels.Add(current);
                        sumL += candidates.L[value];
                        sumA += candidates.A[value];
                        sumB += candidates.B[value];
                        touchesBorder |= currentX == 0 || currentY == 0 || currentX == width - 1 || currentY == height - 1;

                        TryEnqueue(currentX - 1, currentY, value, label, indices, labels, strideInts, width, height, queue);
                        TryEnqueue(currentX + 1, currentY, value, label, indices, labels, strideInts, width, height, queue);
                        TryEnqueue(currentX, currentY - 1, value, label, indices, labels, strideInts, width, height, queue);
                        TryEnqueue(currentX, currentY + 1, value, label, indices, labels, strideInts, width, height, queue);
                    }

                    regions.Add(new Region(value, pixels, sumL / pixels.Count, sumA / pixels.Count, sumB / pixels.Count, touchesBorder));
                }
            }

            Region field = null;
            foreach (Region region in regions)
            {
                if (!region.TouchesBorder || region.Pixels.Count < minimumArea)
                {
                    continue;
                }

                if (field == null || region.Pixels.Count > field.Pixels.Count)
                {
                    field = region;
                }
            }

            if (field == null)
            {
                return;
            }

            double chroma = Math.Sqrt((field.A * field.A) + (field.B * field.B));
            double targetChroma = Math.Min(chroma * 0.35, 25.0);
            double scale = chroma <= 1e-9 ? 0.0 : targetChroma / chroma;
            int replacement = candidates.FindNearest(58.0, field.A * scale, field.B * scale);
            foreach (int pixel in field.Pixels)
            {
                indices[pixel] = replacement;
            }
        }

        private static void TryEnqueue(
            int x,
            int y,
            int value,
            int label,
            int[] indices,
            int[] labels,
            int strideInts,
            int width,
            int height,
            Queue<(int X, int Y)> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int at = (y * strideInts) + x;
            if (labels[at] < 0 && indices[at] == value)
            {
                labels[at] = label;
                queue.Enqueue((x, y));
            }
        }

        private sealed class Region
        {
            public Region(int value, List<int> pixels, double l, double a, double b, bool touchesBorder)
            {
                Value = value;
                Pixels = pixels;
                L = l;
                A = a;
                B = b;
                TouchesBorder = touchesBorder;
            }

            public int Value { get; }
            public List<int> Pixels { get; }
            public double L { get; }
            public double A { get; }
            public double B { get; }
            public bool TouchesBorder { get; }
        }
    }
}
