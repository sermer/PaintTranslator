using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Leaves the sampled achievable gamut untouched. The palette slot a style
    /// occupies when it has no mother colour to blend in and no region of the gamut
    /// to withhold.
    /// </summary>
    internal sealed class KeepAllCandidates : ICandidateTransform
    {
        /// <summary>Gets "Palette", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Palette";

        /// <summary>Gets the empty parameter list: leaving the gamut untouched has nothing to tune.</summary>
        public IReadOnlyList<StyleParameter> Parameters { get; } = Array.Empty<StyleParameter>();

        /// <summary>
        /// Does nothing, leaving <paramref name="builder"/> to sample the gamut
        /// exactly as it would with no style applied at all.
        /// </summary>
        /// <param name="builder">The mixtures about to be rendered.</param>
        /// <param name="values">Unused; this stage declares no parameters.</param>
        public void Transform(MixtureBuilder builder, ParameterValues values)
        {
        }
    }
}
