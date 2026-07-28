using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// One named painting style: a fixed choice of stage for each of the pipeline's
    /// five slots, plus how strongly the mandatory pre-map floor runs for this style
    /// relative to the user's own mark-size slider.
    /// <para>
    /// A style is data, not behaviour. It names which stage instances
    /// <see cref="StylePipeline.Render"/> runs and in what order, but every stage
    /// still receives only a <see cref="RenderContext"/> and its own
    /// <see cref="ParameterValues"/> — never this record. That is what keeps a stage
    /// reusable across styles: it has no way to branch on which one invoked it, so
    /// tuning one style's numbers can never silently change another's.
    /// </para>
    /// </summary>
    /// <param name="Name">The style's identifier, looked up by
    /// <see cref="StyleRegistry.ByName"/> and shown to the user.</param>
    /// <param name="MarkScale">The factor applied to the user's mark-size slider
    /// before any stage sees it, so a style that wants coarser or finer marks than
    /// the slider alone implies can ask for that without exposing a second slider.</param>
    /// <param name="PreMap">Slot 1: pixel-buffer stages, run in order before mapping.</param>
    /// <param name="Remap">Slot 2: the colour remap.</param>
    /// <param name="Candidates">Slot 3: the candidate-set transform.</param>
    /// <param name="Quantiser">Slot 4: the candidate chooser.</param>
    /// <param name="PostMap">Slot 5: index-buffer stages, run in order after mapping.</param>
    internal sealed record StyleDefinition(
        string Name,
        double MarkScale,
        IReadOnlyList<IPreMapStage> PreMap,
        ILabRemap Remap,
        ICandidateTransform Candidates,
        IQuantiser Quantiser,
        IReadOnlyList<IPostMapStage> PostMap)
    {
        /// <summary>
        /// Gets the per-style overrides to a stage's own declared parameter defaults,
        /// keyed by which stage instance and which of its parameters.
        /// <para>
        /// A stage's <see cref="StyleParameter.Default"/> is the value that keeps it a
        /// reusable no-op — the number a style leaves it at when the style has nothing
        /// to say about that control. A style that instead wants a stage to render
        /// noticeably different from a no-op the moment it is selected, without asking
        /// the user to move a slider first, records that tuning here rather than
        /// inside the stage: the stage stays the same reusable instance whichever
        /// style hands it these values, so tuning one style's numbers can never
        /// silently change what another style using the same stage type sees.
        /// </para>
        /// </summary>
        public IReadOnlyDictionary<(IPipelineStage Stage, string ParameterId), double> DefaultOverrides
        {
            get;
            private init;
        } = new Dictionary<(IPipelineStage, string), double>();

        /// <summary>
        /// Returns a copy of this style with additional stage-parameter defaults
        /// recorded in <see cref="DefaultOverrides"/>.
        /// </summary>
        /// <param name="overrides">Each override as a (stage instance, parameter
        /// identifier, value) triple. The stage instance must be one this style
        /// already names in one of its slots, and the identifier must be one of that
        /// stage's own declared <see cref="StyleParameter"/> identifiers — neither is
        /// checked here, since the mismatch would already surface as a
        /// <see cref="KeyNotFoundException"/> the first time
        /// <see cref="StylePipeline.DefaultValues"/> tried to apply it.</param>
        /// <returns>A new <see cref="StyleDefinition"/>, otherwise identical to this
        /// one, with <paramref name="overrides"/> merged into its existing
        /// <see cref="DefaultOverrides"/>.</returns>
        public StyleDefinition WithDefaults(params (IPipelineStage Stage, string ParameterId, double Value)[] overrides)
        {
            var merged = new Dictionary<(IPipelineStage, string), double>(DefaultOverrides);
            foreach ((IPipelineStage stage, string parameterId, double value) in overrides)
            {
                merged[(stage, parameterId)] = value;
            }

            return this with { DefaultOverrides = merged };
        }
    }
}
