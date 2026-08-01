using System.Buffers;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Reuses the large floating-point planes needed by spatial filters. A dedicated
    /// pool is used because .NET 5's shared pool does not retain full-HD arrays; the
    /// filters run sequentially and can therefore share the same bounded buckets.
    /// </summary>
    internal static class ImageBufferPool
    {
        // Supports one float per pixel through 16 MP and retains enough arrays for
        // the guided filter's six simultaneously rented planes.
        internal static ArrayPool<float> Float { get; } =
            ArrayPool<float>.Create(16 * 1024 * 1024, 12);
    }
}
