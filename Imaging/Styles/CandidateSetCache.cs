using System;
using System.Collections.Generic;
using System.Threading;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// Retains the most recently sampled spectral gamut. Palette construction is a
    /// large fixed cost and most interactive controls do not alter it, so repeating it
    /// for every preview frame wastes more time than rendering the preview itself.
    /// </summary>
    internal sealed class CandidateSetCache
    {
        private const int MaximumEntries = 4;
        private readonly LinkedList<Entry> entries = new LinkedList<Entry>();
        private readonly object sync = new object();

        /// <summary>
        /// Returns the candidates for the supplied palette state, building them only
        /// when the selected paints or a build-affecting transform parameter changed.
        /// </summary>
        /// <returns>The cached or newly built set, or null when cancellation is observed.</returns>
        public CandidateSet GetOrCreate(
            IReadOnlyList<PigmentCoefficients> paints,
            StyleDefinition style,
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values,
            CancellationToken cancellationToken = default)
        {
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            var key = new CandidateKey(paints, style.Candidates, values[style.Candidates]);
            lock (sync)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                for (LinkedListNode<Entry> node = entries.First; node != null; node = node.Next)
                {
                    if (!key.Equals(node.Value.Key))
                    {
                        continue;
                    }

                    CandidateSet cached = node.Value.Candidates;
                    entries.Remove(node);
                    entries.AddFirst(node);
                    return cached;
                }

                CandidateSet built = StylePipeline.PrepareCandidates(paints, style, values, cancellationToken);
                if (built == null || cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                entries.AddFirst(new Entry(key, built));
                if (entries.Count > MaximumEntries)
                {
                    entries.RemoveLast();
                }

                return built;
            }
        }

        private sealed class Entry
        {
            public Entry(CandidateKey key, CandidateSet candidates)
            {
                Key = key;
                Candidates = candidates;
            }

            public CandidateKey Key { get; }

            public CandidateSet Candidates { get; }
        }

        private sealed class CandidateKey : IEquatable<CandidateKey>
        {
            private readonly PigmentCoefficients[] paints;
            private readonly Type transformType;
            private readonly long[] parameterBits;

            public CandidateKey(
                IReadOnlyList<PigmentCoefficients> paints,
                ICandidateTransform transform,
                ParameterValues values)
            {
                this.paints = new PigmentCoefficients[paints.Count];
                for (int i = 0; i < paints.Count; i++)
                {
                    this.paints[i] = paints[i];
                }

                transformType = transform.GetType();
                parameterBits = new long[transform.BuildParameters.Count];
                for (int i = 0; i < parameterBits.Length; i++)
                {
                    parameterBits[i] = BitConverter.DoubleToInt64Bits(values[transform.BuildParameters[i].Id]);
                }
            }

            public bool Equals(CandidateKey other)
            {
                if (other == null || transformType != other.transformType ||
                    paints.Length != other.paints.Length || parameterBits.Length != other.parameterBits.Length)
                {
                    return false;
                }

                for (int i = 0; i < paints.Length; i++)
                {
                    if (!ReferenceEquals(paints[i], other.paints[i]))
                    {
                        return false;
                    }
                }

                for (int i = 0; i < parameterBits.Length; i++)
                {
                    if (parameterBits[i] != other.parameterBits[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
