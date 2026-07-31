using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Absorbs connected candidate regions smaller than one brushmark's area into a
    /// neighbouring region. Because this stage rewrites indices only, it cannot
    /// synthesize a colour outside the achievable candidate set.
    /// </summary>
    internal sealed class SmallRegionMerge : IPostMapStage
    {
        public string DisplayName => "Region size";

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
            int minimumArea = Math.Max(1, (int)Math.Ceiling(context.MarkPixels * context.MarkPixels));
            if (minimumArea <= 1 || width <= 0 || height <= 0)
            {
                return;
            }

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

                    while (queue.Count > 0)
                    {
                        (int currentX, int currentY) = queue.Dequeue();
                        int current = (currentY * strideInts) + currentX;
                        pixels.Add(current);

                        TryEnqueue(currentX - 1, currentY, value, label, indices, labels, strideInts, width, height, queue);
                        TryEnqueue(currentX + 1, currentY, value, label, indices, labels, strideInts, width, height, queue);
                        TryEnqueue(currentX, currentY - 1, value, label, indices, labels, strideInts, width, height, queue);
                        TryEnqueue(currentX, currentY + 1, value, label, indices, labels, strideInts, width, height, queue);
                    }

                    regions.Add(new Region(value, pixels));
                }
            }

            for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
            {
                Region region = regions[regionIndex];
                if (region.Pixels.Count >= minimumArea)
                {
                    continue;
                }

                int target = LargestNeighbour(regionIndex, region, regions, labels, strideInts, width, height, minimumArea);
                if (target < 0)
                {
                    continue;
                }

                int replacement = regions[target].Value;
                foreach (int pixel in region.Pixels)
                {
                    indices[pixel] = replacement;
                }
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

        private static int LargestNeighbour(
            int regionIndex,
            Region region,
            IReadOnlyList<Region> regions,
            int[] labels,
            int strideInts,
            int width,
            int height,
            int minimumArea)
        {
            int best = -1;
            int bestSize = -1;
            int fallback = -1;
            int fallbackSize = -1;

            foreach (int pixel in region.Pixels)
            {
                int x = pixel % strideInts;
                int y = pixel / strideInts;
                ConsiderNeighbour(x - 1, y, regionIndex, regions, labels, strideInts, width, height, ref best, ref bestSize, ref fallback, ref fallbackSize, minimumArea);
                ConsiderNeighbour(x + 1, y, regionIndex, regions, labels, strideInts, width, height, ref best, ref bestSize, ref fallback, ref fallbackSize, minimumArea);
                ConsiderNeighbour(x, y - 1, regionIndex, regions, labels, strideInts, width, height, ref best, ref bestSize, ref fallback, ref fallbackSize, minimumArea);
                ConsiderNeighbour(x, y + 1, regionIndex, regions, labels, strideInts, width, height, ref best, ref bestSize, ref fallback, ref fallbackSize, minimumArea);
            }

            return best >= 0 ? best : fallback;
        }

        private static void ConsiderNeighbour(
            int x,
            int y,
            int regionIndex,
            IReadOnlyList<Region> regions,
            int[] labels,
            int strideInts,
            int width,
            int height,
            ref int best,
            ref int bestSize,
            ref int fallback,
            ref int fallbackSize,
            int minimumArea)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int neighbour = labels[(y * strideInts) + x];
            if (neighbour < 0 || neighbour == regionIndex)
            {
                return;
            }

            int size = regions[neighbour].Pixels.Count;
            if (size > fallbackSize)
            {
                fallback = neighbour;
                fallbackSize = size;
            }

            if (size >= minimumArea && size > bestSize)
            {
                best = neighbour;
                bestSize = size;
            }
        }

        private sealed class Region
        {
            public Region(int value, List<int> pixels)
            {
                Value = value;
                Pixels = pixels;
            }

            public int Value { get; }
            public List<int> Pixels { get; }
        }
    }
}
