using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Denoises the pixel buffer with the guided filter, at a window sized to the
    /// render's own mark size, before any colour mapping runs.
    /// <para>
    /// Every style in <see cref="StyleRegistry"/> includes this stage at its declared
    /// or overridden defaults, and none may omit it: mapping each pixel independently
    /// amplifies input noise by roughly 1.7x, which on an ordinary photograph with
    /// sensor noise puts 44.3% of pixels into regions of four pixels or fewer. Those
    /// pixels are still legitimately mixable colours — the invariant that every
    /// output colour is achievable never breaks — but they sit in patches no brush
    /// could lay down. A style that skipped this stage would render unpaintable
    /// images however interesting its other four slots were. Including the stage
    /// keeps every registered style far short of that catastrophic case, but does not
    /// by itself guarantee any particular style clears a given fragmentation bar: a
    /// style that registers a large <c>MarkScale</c> without a floor strength to
    /// match — Fauvism runs this stage at its own weakest declared default; Abstract
    /// already registers this stage's strongest — can still leave more of its output
    /// fragmented than a style with a smaller mark and the same floor would. See
    /// <see cref="PaintabilityMetrics"/> and <see cref="GuidedFilter"/> for the
    /// measurements this is built on, and
    /// <c>StyleBehaviourTests.EveryRegisteredStyleIsPaintable</c> for the per-style
    /// figures.
    /// </para>
    /// </summary>
    internal sealed class EdgePreservingFloor : IPreMapStage
    {
        private static readonly IReadOnlyList<StyleParameter> ParameterList = new[]
        {
            new StyleParameter("strength", "Smoothing strength", 1.0, 5.0, 1.0, ""),
            new StyleParameter("edge", "Edge threshold", 0.01, 0.30, GuidedFilter.DefaultEdgeThreshold, ""),
        };

        /// <summary>Gets "Smoothing", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Smoothing";

        /// <summary>
        /// Gets this stage's two parameters: how many guided-filter passes to run,
        /// and the linear-light contrast a step must exceed to survive as an edge.
        /// </summary>
        public IReadOnlyList<StyleParameter> Parameters => ParameterList;

        /// <summary>
        /// Runs the guided filter over the pixel buffer, at a radius derived from
        /// this render's mark size rather than a value the stage stores itself, so
        /// the window always tracks whatever mark the active style resolved to.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="context">This render's geometry.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        public void Apply(
            int[] pixels, int strideInts, int width, int height,
            in RenderContext context, ParameterValues values)
        {
            GuidedFilter.Apply(
                pixels, strideInts, width, height,
                PalettePhotoConverter.FloorRadius(context.MarkPixels),
                values["edge"],
                (int)values["strength"]);
        }
    }
}
