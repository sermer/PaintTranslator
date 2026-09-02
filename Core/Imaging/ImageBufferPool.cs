using System.Buffers;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Reuses the large per-pixel planes the imaging pipeline needs. A dedicated
    /// pool is used because .NET 5's shared pool does not retain full-HD arrays; the
    /// filters run sequentially and can therefore share the same bounded buckets.
    /// <para>
    /// Rented planes are not cleared, so every consumer either initialises the plane
    /// itself or only reads elements it has written. Renting can return an array
    /// longer than requested; no consumer may size its work from the array's length.
    /// </para>
    /// </summary>
    internal static class ImageBufferPool
    {
        // Supports one float per pixel through 16 MP and retains enough arrays for
        // the guided filter's six simultaneously rented planes.
        internal static ArrayPool<float> Float { get; } =
            ArrayPool<float>.Create(16 * 1024 * 1024, 12);

        // Candidate-index and region-label planes: the pipeline holds at most the
        // mapping plane plus one stage-local plane at a time.
        internal static ArrayPool<int> Int { get; } =
            ArrayPool<int>.Create(16 * 1024 * 1024, 4);

        // Boundary and dilation masks for the post-map line stages.
        internal static ArrayPool<bool> Bool { get; } =
            ArrayPool<bool>.Create(16 * 1024 * 1024, 4);
    }
}
