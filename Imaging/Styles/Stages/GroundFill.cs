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
            var queue = new Queue<int>();

            for (int y = 0; y < height; y++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                for (int x = 0; x < width; x++)
                {
                    int at = (y * strideInts) + x;
                    if (labels[at] >= 0)
                    {
                        continue;
                    }

                    int label = regions.Count;
                    int value = indices[at];
                    int area = 0;
                    queue.Enqueue(at);
                    labels[at] = label;
                    double sumL = 0.0;
                    double sumA = 0.0;
                    double sumB = 0.0;
                    bool touchesBorder = false;

                    while (queue.Count > 0)
                    {
                        if ((area & 4095) == 0)
                        {
                            if (context.CancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                        }

                        int current = queue.Dequeue();
                        int currentY = current / strideInts;
                        int currentX = current - (currentY * strideInts);
                        area++;
                        sumL += candidates.L[value];
                        sumA += candidates.A[value];
                        sumB += candidates.B[value];
                        touchesBorder |= currentX == 0 || currentY == 0 || currentX == width - 1 || currentY == height - 1;

                        if (currentX > 0)
                        {
                            TryEnqueue(current - 1, value, label, indices, labels, queue);
                        }
                        if (currentX + 1 < width)
                        {
                            TryEnqueue(current + 1, value, label, indices, labels, queue);
                        }
                        if (currentY > 0)
                        {
                            TryEnqueue(current - strideInts, value, label, indices, labels, queue);
                        }
                        if (currentY + 1 < height)
                        {
                            TryEnqueue(current + strideInts, value, label, indices, labels, queue);
                        }
                    }

                    regions.Add(new Region(
                        label, value, area, sumL / area, sumA / area, sumB / area, touchesBorder));
                }
            }

            Region field = null;
            foreach (Region region in regions)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (!region.TouchesBorder || region.Area < minimumArea)
                {
                    continue;
                }

                if (field == null || region.Area > field.Area)
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
                    if (labels[at] == field.Label)
                    {
                        indices[at] = replacement;
                    }
                }
            }
        }

        private static void TryEnqueue(
            int at, int value, int label, int[] indices, int[] labels, Queue<int> queue)
        {
            if (labels[at] < 0 && indices[at] == value)
            {
                labels[at] = label;
                queue.Enqueue(at);
            }
        }

        private sealed class Region
        {
            public Region(
                int label, int value, int area, double l, double a, double b, bool touchesBorder)
            {
                Label = label;
                Value = value;
                Area = area;
                L = l;
                A = a;
                B = b;
                TouchesBorder = touchesBorder;
            }

            public int Label { get; }
            public int Value { get; }
            public int Area { get; }
            public double L { get; }
            public double A { get; }
            public double B { get; }
            public bool TouchesBorder { get; }
        }
    }
}
