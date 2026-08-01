using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// Retains RGB-bin-to-candidate answers across renders whose mapping state is
    /// identical. Pre-map and post-map controls deliberately do not participate in
    /// the key: they change which RGB bins occur or how mapped indices are grouped,
    /// not what a given RGB bin maps to.
    /// </summary>
    internal sealed class ColourMapCache
    {
        private const int MaximumEntries = 4;
        private readonly object sync = new object();
        private readonly LinkedList<Entry> entries = new LinkedList<Entry>();

        public int[] GetOrCreate(
            CandidateSet candidates,
            StyleDefinition style,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            in RenderContext context)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var key = new MappingKey(candidates, style, values, in context);
            lock (sync)
            {
                for (LinkedListNode<Entry> node = entries.First; node != null; node = node.Next)
                {
                    if (!key.Equals(node.Value.Key))
                    {
                        continue;
                    }

                    int[] cached = node.Value.Resolved;
                    entries.Remove(node);
                    entries.AddFirst(node);
                    return cached;
                }

                var resolved = new int[ColorQuantization.CacheSize];
                Array.Fill(resolved, -1);
                entries.AddFirst(new Entry(key, resolved));
                if (entries.Count > MaximumEntries)
                {
                    entries.RemoveLast();
                }

                return resolved;
            }
        }

        private sealed class Entry
        {
            public Entry(MappingKey key, int[] resolved)
            {
                Key = key;
                Resolved = resolved;
            }

            public MappingKey Key { get; }

            public int[] Resolved { get; }
        }

        private sealed class MappingKey : IEquatable<MappingKey>
        {
            private readonly CandidateSet candidates;
            private readonly ILabRemap remap;
            private readonly IQuantiser quantiser;
            private readonly long[] remapParameterBits;
            private readonly long[] quantiserParameterBits;
            private readonly int width;
            private readonly int height;
            private readonly long markPixelsBits;

            public MappingKey(
                CandidateSet candidates,
                StyleDefinition style,
                IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
                in RenderContext context)
            {
                this.candidates = candidates;
                remap = style.Remap;
                quantiser = style.Quantiser;
                remapParameterBits = CaptureParameterBits(remap.Parameters, values[remap]);
                quantiserParameterBits = CaptureParameterBits(quantiser.Parameters, values[quantiser]);
                width = context.Width;
                height = context.Height;
                markPixelsBits = BitConverter.DoubleToInt64Bits(context.MarkPixels);
            }

            public bool Equals(MappingKey other)
            {
                return other != null &&
                    ReferenceEquals(candidates, other.candidates) &&
                    ReferenceEquals(remap, other.remap) &&
                    ReferenceEquals(quantiser, other.quantiser) &&
                    width == other.width &&
                    height == other.height &&
                    markPixelsBits == other.markPixelsBits &&
                    EqualBits(remapParameterBits, other.remapParameterBits) &&
                    EqualBits(quantiserParameterBits, other.quantiserParameterBits);
            }

            private static long[] CaptureParameterBits(
                IReadOnlyList<StyleParameter> parameters,
                ParameterValues values)
            {
                var result = new long[parameters.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = BitConverter.DoubleToInt64Bits(values[parameters[i].Id]);
                }

                return result;
            }

            private static bool EqualBits(long[] left, long[] right)
            {
                if (left.Length != right.Length)
                {
                    return false;
                }

                for (int i = 0; i < left.Length; i++)
                {
                    if (left[i] != right[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
