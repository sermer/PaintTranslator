using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Collects coarse phase timings from the real conversion pipeline. Kept optional
    /// and allocation-free when absent so production renders pay only a null check,
    /// while the benchmark tool measures the shipped implementation rather than a
    /// separately transcribed approximation of it.
    /// </summary>
    internal sealed class RenderDiagnostics
    {
        private readonly List<RenderPhaseTiming> timings = new List<RenderPhaseTiming>();

        public IReadOnlyList<RenderPhaseTiming> Timings => timings;

        internal long Begin()
        {
            return Stopwatch.GetTimestamp();
        }

        internal void End(string phase, long started)
        {
            long elapsed = Stopwatch.GetTimestamp() - started;
            timings.Add(new RenderPhaseTiming(
                phase,
                TimeSpan.FromSeconds(elapsed / (double)Stopwatch.Frequency)));
        }
    }

    /// <summary>One named interval captured during a conversion.</summary>
    internal readonly struct RenderPhaseTiming
    {
        public RenderPhaseTiming(string phase, TimeSpan elapsed)
        {
            Phase = phase;
            Elapsed = elapsed;
        }

        public string Phase { get; }

        public TimeSpan Elapsed { get; }
    }
}
