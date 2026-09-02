using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Leaves every colour exactly where it is. The colour slot a style occupies
    /// when it has nothing to say about hue, lightness or chroma beyond what the
    /// source photo and the mandatory floor already produced.
    /// </summary>
    internal sealed class IdentityRemap : ILabRemap
    {
        /// <summary>Gets "Colour", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Colour";

        /// <summary>Gets the empty parameter list: an identity mapping has nothing to tune.</summary>
        public IReadOnlyList<StyleParameter> Parameters { get; } = Array.Empty<StyleParameter>();

        /// <summary>
        /// Copies the source coordinates straight through.
        /// </summary>
        /// <param name="l">The source L*.</param>
        /// <param name="a">The source a*.</param>
        /// <param name="b">The source b*.</param>
        /// <param name="mappedL">Set equal to <paramref name="l"/>.</param>
        /// <param name="mappedA">Set equal to <paramref name="a"/>.</param>
        /// <param name="mappedB">Set equal to <paramref name="b"/>.</param>
        /// <param name="context">Unused; an identity mapping needs no geometry.</param>
        /// <param name="values">Unused; this stage declares no parameters.</param>
        public void Map(
            double l, double a, double b,
            out double mappedL, out double mappedA, out double mappedB,
            in RenderContext context, ParameterValues values)
        {
            mappedL = l;
            mappedA = a;
            mappedB = b;
        }
    }
}
