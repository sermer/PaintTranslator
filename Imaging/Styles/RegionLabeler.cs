using System.Collections.Generic;
using System.Threading;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// Labels the four-connected regions of equal value in a candidate-index buffer.
    /// The post-map stages that reason about regions share this one sweep so that
    /// connectivity, the cancellation cadence, and the traversal itself cannot
    /// drift apart between stages.
    /// </summary>
    internal static class RegionLabeler
    {
        /// <summary>
        /// Flood-fills every four-connected region of identical candidate index,
        /// writing each pixel's region label and recording each region's candidate
        /// value and pixel count. Returns early, leaving the outputs partially
        /// filled, when cancellation is observed; callers must check the token
        /// before using the results.
        /// </summary>
        /// <param name="indices">The candidate index of each pixel.</param>
        /// <param name="labels">The buffer region labels are written into; every
        /// element inside the image must be negative on entry.</param>
        /// <param name="strideInts">The number of ints per pixel row.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="values">Receives each region's candidate value, indexed by label.</param>
        /// <param name="areas">Receives each region's area in pixels, indexed by label.</param>
        /// <param name="cancellationToken">The signal that the render was superseded.</param>
        internal static void Label(
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

        /// <summary>
        /// Joins a neighbouring pixel into the region being filled when it is still
        /// unlabelled and holds the region's candidate value.
        /// </summary>
        /// <param name="at">The neighbour's flat buffer offset.</param>
        /// <param name="value">The candidate value the region is made of.</param>
        /// <param name="region">The label being assigned.</param>
        /// <param name="indices">The candidate index of each pixel.</param>
        /// <param name="labels">The label of each pixel so far.</param>
        /// <param name="queue">The flood fill's frontier.</param>
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
