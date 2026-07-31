using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// What every pipeline stage has in common: a name for the control panel, and the
    /// parameters it lets the user adjust.
    /// <para>
    /// Note what a stage is <em>not</em> given: any way to learn which style invoked
    /// it. No enum, no name, no context object carrying one. A stage that could ask
    /// would grow a branch per style, and tuning one style would then silently change
    /// every other style sharing that stage — the exact failure this pipeline exists
    /// to make impossible. A stage that seems to need the answer should be two stages.
    /// </para>
    /// </summary>
    internal interface IPipelineStage
    {
        /// <summary>Gets the heading shown above this stage's controls.</summary>
        string DisplayName { get; }

        /// <summary>Gets the values this stage lets the user adjust, possibly none.</summary>
        IReadOnlyList<StyleParameter> Parameters { get; }
    }

    /// <summary>
    /// Slot 1. Works on the pixel buffer before any mapping happens, so it may do
    /// anything at all — it cannot violate the colour invariant, because everything it
    /// produces is still mapped onto the achievable gamut afterwards.
    /// <para>
    /// This is also the only slot that knows where a pixel is, which is why anything
    /// spatially varying belongs here rather than in the remap.
    /// </para>
    /// </summary>
    internal interface IPreMapStage : IPipelineStage
    {
        /// <summary>
        /// Transforms the pixel buffer in place.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="context">This render's geometry.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        void Apply(
            int[] pixels, int strideInts, int width, int height,
            in RenderContext context, ParameterValues values);
    }

    /// <summary>
    /// Slot 2. A pure CIELAB-to-CIELAB function with no access to position.
    /// <para>
    /// The purity is load-bearing rather than stylistic: the converter resolves each
    /// distinct quantized colour once and caches it, so a remap that depended on where
    /// the pixel was would have to run per pixel instead and forfeit that cache.
    /// Anything position-dependent belongs in slot 1.
    /// </para>
    /// </summary>
    internal interface ILabRemap : IPipelineStage
    {
        /// <summary>
        /// Maps one colour to another.
        /// </summary>
        /// <param name="l">The source L*.</param>
        /// <param name="a">The source a*.</param>
        /// <param name="b">The source b*.</param>
        /// <param name="mappedL">The resulting L*.</param>
        /// <param name="mappedA">The resulting a*.</param>
        /// <param name="mappedB">The resulting b*.</param>
        /// <param name="context">This render's geometry and palette limits.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        void Map(
            double l, double a, double b,
            out double mappedL, out double mappedA, out double mappedB,
            in RenderContext context, ParameterValues values);
    }

    /// <summary>
    /// Slot 3. Rewrites which mixtures the gamut sampler will render, before any of
    /// them becomes a colour.
    /// </summary>
    internal interface ICandidateTransform : IPipelineStage
    {
        /// <summary>
        /// Adjusts the mixture list.
        /// </summary>
        /// <param name="builder">The mixtures about to be rendered.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        void Transform(MixtureBuilder builder, ParameterValues values);
    }

    /// <summary>
    /// Optional second pass for a candidate transform that needs the already-filtered
    /// image to choose a smaller palette. It runs after slot 1 and after the initial
    /// gamut has been built, while the data is still represented as candidate indices.
    /// </summary>
    internal interface IImageAwareCandidateTransform
    {
        CandidateSet Transform(
            CandidateSet candidates,
            int[] pixels,
            int strideInts,
            int width,
            int height,
            in RenderContext context,
            ParameterValues values);
    }

    /// <summary>
    /// Slot 4. Chooses which candidate a colour becomes, by index.
    /// </summary>
    internal interface IQuantiser : IPipelineStage
    {
        /// <summary>
        /// Gets whether the choice depends on where the pixel is.
        /// <para>
        /// True forces the converter to resolve every pixel separately rather than
        /// once per distinct colour, which costs the cache. Declaring it lets the
        /// pipeline keep the cache whenever it can, instead of giving it up for every
        /// style because one style might need to.
        /// </para>
        /// </summary>
        bool IsPositionDependent { get; }

        /// <summary>
        /// Picks the candidate for one colour.
        /// </summary>
        /// <param name="l">The target L*.</param>
        /// <param name="a">The target a*.</param>
        /// <param name="b">The target b*.</param>
        /// <param name="candidates">The achievable colours to choose from.</param>
        /// <param name="x">The pixel's column, meaningful only when
        /// <see cref="IsPositionDependent"/> is true.</param>
        /// <param name="y">The pixel's row, on the same condition.</param>
        /// <param name="context">This render's geometry.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        /// <returns>The chosen candidate's index.</returns>
        int Map(
            double l, double a, double b, CandidateSet candidates,
            int x, int y, in RenderContext context, ParameterValues values);
    }

    /// <summary>
    /// Slot 5. Rewrites candidate indices after the mapping.
    /// <para>
    /// Takes and returns indices rather than colours, which is what makes the colour
    /// invariant structural: a stage here has no way to name a colour outside the
    /// candidate set, so it cannot emit one. Post-mapping arithmetic — averaging,
    /// anti-aliasing, filtered downsampling — is not forbidden by a rule anybody has
    /// to remember; it simply cannot be expressed through this signature.
    /// </para>
    /// </summary>
    internal interface IPostMapStage : IPipelineStage
    {
        /// <summary>
        /// Rewrites the index buffer in place.
        /// </summary>
        /// <param name="indices">One candidate index per pixel, row-major with the given
        /// stride, modified in place.</param>
        /// <param name="strideInts">The number of ints per row.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="candidates">The achievable colours the indices refer to.</param>
        /// <param name="context">This render's geometry.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        void Refine(
            int[] indices, int strideInts, int width, int height,
            CandidateSet candidates, in RenderContext context, ParameterValues values);
    }
}
