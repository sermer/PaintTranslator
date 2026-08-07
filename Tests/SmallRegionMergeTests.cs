using System;
using System.Collections.Generic;
using System.Threading;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Imaging.Styles.Stages;
using Xunit;

namespace PaintTranslator.Tests
{
    public class SmallRegionMergeTests
    {
        [Fact]
        public void OneSweepAccumulatesChainedSmallRegionsUntilTheyReachMarkArea()
        {
            const int width = 5;
            const int height = 5;
            var indices = new[]
            {
                0, 1, 0, 1, 0,
                1, 0, 1, 0, 1,
                0, 1, 0, 1, 0,
                1, 0, 1, 0, 1,
                0, 1, 0, 1, 0,
            };
            var candidates = new CandidateSet(
                new[] { unchecked((int)0xFF202020), unchecked((int)0xFFC04040) },
                new[] { 12.0, 52.0 },
                new[] { 0.0, 45.0 },
                new[] { 0.0, 25.0 });
            var stage = new SmallRegionMerge();
            var values = new ParameterValues(Array.Empty<StyleParameter>());
            var context = new RenderContext(width, height, 2.0, 50.0);

            stage.Refine(indices, width, width, height, candidates, in context, values);

            foreach (int area in RegionAreas(indices, width, height))
            {
                Assert.True(area >= 4, $"region area {area} remained below mark²");
            }
        }

        [Fact]
        public void CanceledRefinementStopsWithoutThrowing()
        {
            const int width = 3;
            const int height = 3;
            var indices = new[]
            {
                0, 1, 0,
                1, 0, 1,
                0, 1, 0,
            };
            var original = (int[])indices.Clone();
            var candidates = new CandidateSet(
                new[] { unchecked((int)0xFF202020), unchecked((int)0xFFC04040) },
                new[] { 12.0, 52.0 },
                new[] { 0.0, 45.0 },
                new[] { 0.0, 25.0 });
            var stage = new SmallRegionMerge();
            var values = new ParameterValues(Array.Empty<StyleParameter>());
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var context = new RenderContext(
                width, height, 2.0, 50.0, cancellation.Token);

            stage.Refine(indices, width, width, height, candidates, in context, values);

            Assert.Equal(original, indices);
        }

        private static IReadOnlyList<int> RegionAreas(int[] indices, int width, int height)
        {
            var visited = new bool[indices.Length];
            var areas = new List<int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int at = y * width + x;
                    if (visited[at])
                    {
                        continue;
                    }

                    int value = indices[at];
                    int area = 0;
                    var queue = new Queue<(int X, int Y)>();
                    queue.Enqueue((x, y));
                    visited[at] = true;
                    while (queue.Count > 0)
                    {
                        (int currentX, int currentY) = queue.Dequeue();
                        area++;
                        TryVisit(currentX - 1, currentY, value, indices, visited, width, height, queue);
                        TryVisit(currentX + 1, currentY, value, indices, visited, width, height, queue);
                        TryVisit(currentX, currentY - 1, value, indices, visited, width, height, queue);
                        TryVisit(currentX, currentY + 1, value, indices, visited, width, height, queue);
                    }

                    areas.Add(area);
                }
            }

            return areas;
        }

        private static void TryVisit(
            int x,
            int y,
            int value,
            int[] indices,
            bool[] visited,
            int width,
            int height,
            Queue<(int X, int Y)> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int at = y * width + x;
            if (!visited[at] && indices[at] == value)
            {
                visited[at] = true;
                queue.Enqueue((x, y));
            }
        }
    }
}
