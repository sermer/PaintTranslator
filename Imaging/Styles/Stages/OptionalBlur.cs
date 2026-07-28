using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Gaussian-blurs the pixel buffer for <see cref="PalettePhotoConverter.Convert"/>'s
    /// <c>blurRadius</c> parameter, which predates the style pipeline and so has no
    /// fixed slot in <see cref="StyleRegistry"/> — <see cref="PalettePhotoConverter"/>
    /// appends an instance of this stage to <see cref="StyleDefinition.PreMap"/> only
    /// when a caller asks for more simplification than the mandatory floor alone gives.
    /// <para>
    /// It is appended <em>after</em> the mandatory floor, never before: the floor and
    /// this blur do not commute. Blurring first lowers local contrast, so true edges
    /// can fall below the floor's variance threshold and stop being protected, and it
    /// turns pixel-independent sensor noise into spatially correlated blotches the
    /// floor's variance test reads as signal rather than noise — both effects grow
    /// with the blur radius. Floor-then-blur is also the order the converter used
    /// before the style pipeline existed. <see cref="StylePipeline.Render"/> runs
    /// <see cref="StyleDefinition.PreMap"/> stages in list order, so getting the order
    /// right is entirely the composing caller's responsibility, not this stage's.
    /// </para>
    /// </summary>
    internal sealed class OptionalBlur : IPreMapStage
    {
        private static readonly IReadOnlyList<StyleParameter> ParameterList = new[]
        {
            // Matches blurTrackBar's Maximum in MainForm.Designer.cs, so a value taken
            // straight from that slider never gets silently clamped down.
            new StyleParameter("radius", "Blur radius", 0.0, 20.0, 0.0, "px"),
        };

        /// <summary>Gets "Blur", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Blur";

        /// <summary>Gets this stage's one parameter: the blur radius in pixels.</summary>
        public IReadOnlyList<StyleParameter> Parameters => ParameterList;

        /// <summary>
        /// Gaussian-blurs the pixel buffer at the configured radius. A radius of zero
        /// leaves the buffer untouched, since <see cref="GaussianBlur.Apply"/> treats a
        /// non-positive radius as a no-op rather than an error.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="context">Unused; this stage's blur radius comes from its own
        /// parameter rather than the render's geometry.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        public void Apply(
            int[] pixels, int strideInts, int width, int height,
            in RenderContext context, ParameterValues values)
        {
            GaussianBlur.Apply(pixels, strideInts, width, height, (int)values["radius"]);
        }
    }
}
