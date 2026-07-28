using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Blends a fixed fraction of the palette's own least-chromatic paint into every
    /// sampled mixture, the "mother colour" technique painters use to harmonise an
    /// otherwise disparate set of hues under one common grey note.
    /// <para>
    /// The paint is chosen from the palette by <see cref="MixtureBuilder.MostNeutralPaintIndex"/>
    /// rather than named here, because the user picks which paints are loaded and a
    /// style cannot assume any particular one — a black, a grey, a specific brand's
    /// paint — is present to name. Whatever the palette contains, its least chromatic
    /// member is the one that greys everything it touches toward a common note rather
    /// than tinting it toward some other hue, which is what makes it the correct
    /// choice regardless of which paints happen to be loaded.
    /// </para>
    /// </summary>
    internal sealed class MotherColourTransform : ICandidateTransform
    {
        private static readonly IReadOnlyList<StyleParameter> ParameterList = new[]
        {
            new StyleParameter("fraction", "Mother colour", 0.0, 0.6, 0.0, ""),
        };

        /// <summary>Gets "Palette", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Palette";

        /// <summary>Gets this stage's one parameter: the mother colour's share of every mixture.</summary>
        public IReadOnlyList<StyleParameter> Parameters => ParameterList;

        /// <summary>
        /// Finds the palette's least chromatic paint and blends it into every mixture
        /// at the configured fraction.
        /// </summary>
        /// <param name="builder">The mixtures about to be rendered.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        public void Transform(MixtureBuilder builder, ParameterValues values)
        {
            builder.BlendInto(builder.MostNeutralPaintIndex(), values["fraction"]);
        }
    }
}
