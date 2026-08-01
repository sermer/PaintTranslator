using System;
using System.Collections.Generic;
using System.Threading;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Absorbs connected candidate regions smaller than one brushmark's area into a
    /// neighbouring region. Components are processed smallest-first and their areas
    /// are accumulated as unions happen, so one invocation converges instead of
    /// repeatedly merging stale small-region labels.
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
            var valuesByRegion = new List<int>();
            var areas = new List<int>();

            LabelRegions(
                indices,
                labels,
                strideInts,
                width,
                height,
                valuesByRegion,
                areas,
                context.CancellationToken);

            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            int regionCount = valuesByRegion.Count;
            var parent = new int[regionCount];
            var neighbours = new List<HashSet<int>>(regionCount);
            for (int i = 0; i < regionCount; i++)
            {
                if ((i & 4095) == 0 && context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                parent[i] = i;
                neighbours.Add(new HashSet<int>());
            }

            BuildAdjacency(labels, strideInts, width, height, neighbours, context.CancellationToken);

            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var pending = new SortedSet<(int Area, int Region)>();
            for (int i = 0; i < regionCount; i++)
            {
                if (areas[i] < minimumArea)
                {
                    pending.Add((areas[i], i));
                }
            }

            while (pending.Count > 0)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                (int _, int candidate) = pending.Min;
                pending.Remove(pending.Min);
                int source = Find(parent, candidate);
                if (source != candidate || areas[source] >= minimumArea)
                {
                    continue;
                }

                int target = LargestNeighbour(source, parent, areas, neighbours, minimumArea);
                if (target < 0)
                {
                    continue;
                }

                int oldTargetArea = areas[target];
                pending.Remove((oldTargetArea, target));
                Merge(source, target, parent, areas, neighbours);
                if (areas[target] < minimumArea)
                {
                    pending.Add((areas[target], target));
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
                    int at = row + x;
                    indices[at] = valuesByRegion[Find(parent, labels[at])];
                }
            }
        }

        private static void LabelRegions(
            int[] indices,
            int[] labels,
            int strideInts,
            int width,
            int height,
            List<int> values,
            List<int> areas,
            CancellationToken cancellationToken)
        {
            var queue = new Queue<int>();
            for (int y = 0; y < height; y++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                for (int x = 0; x < width; x++)
                {
                    int at = y * strideInts + x;
                    if (labels[at] >= 0)
                    {
                        continue;
                    }

                    int region = values.Count;
                    int value = indices[at];
                    int area = 0;
                    queue.Enqueue(at);
                    labels[at] = region;

                    while (queue.Count > 0)
                    {
                        if ((area & 4095) == 0 && cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        int current = queue.Dequeue();
                        int currentY = current / strideInts;
                        int currentX = current - (currentY * strideInts);
                        area++;
                        if (currentX > 0)
                        {
                            TryEnqueue(current - 1, value, region, indices, labels, queue);
                        }
                        if (currentX + 1 < width)
                        {
                            TryEnqueue(current + 1, value, region, indices, labels, queue);
                        }
                        if (currentY > 0)
                        {
                            TryEnqueue(current - strideInts, value, region, indices, labels, queue);
                        }
                        if (currentY + 1 < height)
                        {
                            TryEnqueue(current + strideInts, value, region, indices, labels, queue);
                        }
                    }

                    values.Add(value);
                    areas.Add(area);
                }
            }
        }

        private static void BuildAdjacency(
            int[] labels,
            int strideInts,
            int width,
            int height,
            IReadOnlyList<HashSet<int>> neighbours,
            CancellationToken cancellationToken)
        {
            for (int y = 0; y < height; y++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                int row = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    int region = labels[row + x];
                    if (x + 1 < width)
                    {
                        AddEdge(region, labels[row + x + 1], neighbours);
                    }

                    if (y + 1 < height)
                    {
                        AddEdge(region, labels[row + strideInts + x], neighbours);
                    }
                }
            }
        }

        private static void AddEdge(int left, int right, IReadOnlyList<HashSet<int>> neighbours)
        {
            if (left == right)
            {
                return;
            }

            neighbours[left].Add(right);
            neighbours[right].Add(left);
        }

        private static int LargestNeighbour(
            int source,
            int[] parent,
            IReadOnlyList<int> areas,
            IReadOnlyList<HashSet<int>> neighbours,
            int minimumArea)
        {
            int bestLarge = -1;
            int bestLargeArea = -1;
            int bestAny = -1;
            int bestAnyArea = -1;

            foreach (int neighbour in neighbours[source])
            {
                int root = Find(parent, neighbour);
                if (root == source)
                {
                    continue;
                }

                if (areas[root] > bestAnyArea)
                {
                    bestAny = root;
                    bestAnyArea = areas[root];
                }

                if (areas[root] >= minimumArea && areas[root] > bestLargeArea)
                {
                    bestLarge = root;
                    bestLargeArea = areas[root];
                }
            }

            return bestLarge >= 0 ? bestLarge : bestAny;
        }

        private static void Merge(
            int source,
            int target,
            int[] parent,
            IList<int> areas,
            IList<HashSet<int>> neighbours)
        {
            parent[source] = target;
            areas[target] += areas[source];

            var sourceNeighbours = new List<int>(neighbours[source]);
            neighbours[target].Remove(source);
            foreach (int neighbour in sourceNeighbours)
            {
                int root = Find(parent, neighbour);
                if (root == target || root == source)
                {
                    continue;
                }

                neighbours[target].Add(root);
                neighbours[root].Remove(source);
                neighbours[root].Add(target);
            }

            neighbours[source].Clear();
        }

        private static int Find(int[] parent, int value)
        {
            int root = value;
            while (parent[root] != root)
            {
                root = parent[root];
            }

            while (parent[value] != value)
            {
                int next = parent[value];
                parent[value] = root;
                value = next;
            }

            return root;
        }

        private static void TryEnqueue(
            int at, int value, int region, int[] indices, int[] labels, Queue<int> queue)
        {
            if (labels[at] < 0 && indices[at] == value)
            {
                labels[at] = region;
                queue.Enqueue(at);
            }
        }
    }
}
