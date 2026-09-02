using System;
using System.Collections.Generic;

namespace PaintTranslator.Imaging.Styles.Stages
{
    /// <summary>
    /// Reshapes lightness and chroma together: a contrast/key control that pivots L*
    /// about middle grey, and a chroma gain that follows a soft knee toward the
    /// palette's own achievable ceiling rather than a plain multiplier.
    /// <para>
    /// The two live in one stage rather than two because of the Hunt effect: perceived
    /// colourfulness rises with luminance, so compressing L* toward the low or high
    /// end without also easing chroma back looks wrong in a way a separate chroma
    /// stage could not correct for, since by the time it ran it would no longer know
    /// how much the value control had already moved that pixel.
    /// </para>
    /// </summary>
    internal sealed class ToneAndChromaRemap : ILabRemap
    {
        // A chroma this small carries no usable hue: atan2 over it is dominated by
        // floating-point noise, and dividing by it to build a scale factor would
        // divide by (near) zero. Below this, a* and b* pass through untouched instead.
        private const double NeutralThreshold = 1e-9;

        private static readonly StyleParameter ChromaParameter = new StyleParameter("chroma", "Chroma", 0.0, 3.0, 1.0, "");

        private static readonly IReadOnlyList<StyleParameter> ParameterList = new[]
        {
            new StyleParameter("contrast", "Contrast", 0.3, 2.0, 1.0, ""),
            new StyleParameter("key", "Key shift", -20.0, 20.0, 0.0, "L*"),
            ChromaParameter,
        };

        /// <summary>Gets "Tone &amp; chroma", the heading shown above this stage's controls.</summary>
        public string DisplayName => "Tone & chroma";

        /// <summary>Gets this stage's three parameters: contrast, key shift and chroma gain.</summary>
        public IReadOnlyList<StyleParameter> Parameters => ParameterList;

        /// <summary>
        /// Maps one colour by pivoting its lightness about middle grey and then
        /// reshaping its chroma toward this render's achievable ceiling.
        /// </summary>
        /// <param name="l">The source L*.</param>
        /// <param name="a">The source a*.</param>
        /// <param name="b">The source b*.</param>
        /// <param name="mappedL">The resulting L*, clamped to [0, 100].</param>
        /// <param name="mappedA">The resulting a*.</param>
        /// <param name="mappedB">The resulting b*.</param>
        /// <param name="context">This render's geometry and palette limits, supplying
        /// <see cref="RenderContext.AchievableMaxChroma"/> for the chroma knee.</param>
        /// <param name="values">This stage instance's parameter values.</param>
        public void Map(
            double l, double a, double b,
            out double mappedL, out double mappedA, out double mappedB,
            in RenderContext context, ParameterValues values)
        {
            // Value first, pivoted at mid-lightness so contrast opens and closes the
            // range without also shifting it. The key parameter shifts deliberately,
            // afterwards, so the two controls stay independent of one another.
            mappedL = Math.Clamp(50.0 + ((l - 50.0) * values["contrast"]) + values["key"], 0.0, 100.0);

            double chroma = Math.Sqrt((a * a) + (b * b));
            if (chroma <= NeutralThreshold)
            {
                // A neutral has no hue to preserve, and scaling it would divide by zero.
                mappedA = a;
                mappedB = b;
                return;
            }

            double scaled = ScaleChroma(chroma, values["chroma"], context.AchievableMaxChroma);
            double scale = scaled / chroma;

            mappedA = a * scale;
            mappedB = b * scale;
        }

        /// <summary>
        /// Reshapes one chroma value by a gain, blending from a plain multiplier at
        /// gain 1.0 toward a tanh knee against the achievable ceiling as gain rises
        /// toward its declared maximum.
        /// <para>
        /// The plan's first cut applied the tanh knee unconditionally at every gain,
        /// including 1.0 — but that cannot also satisfy "gain 1.0 is an identity": at
        /// ceiling 60 it already returns 27.7 for an input of 30 and 40.9 for an input
        /// of 50, because tanh is concave and pulls every input down, not just the ones
        /// above the ceiling. Identity at gain 1.0 is the property that lets a style
        /// leave this stage at its default and be
        /// indistinguishable from a style that omits it — the same guarantee every
        /// other stage in this pipeline makes — so the curve blends the two behaviours
        /// by gain instead of switching between them: below gain 1.0 chroma only ever
        /// needs to shrink, which a multiplier already does exactly, so the knee has
        /// nothing to correct there and is not applied at all; from gain 1.0 up to the
        /// parameter's own maximum, the knee's weight rises linearly from 0 to 1, so it
        /// is continuous with the identity below it and recovers the plan's pure tanh
        /// knee exactly at maximum gain.
        /// </para>
        /// <para>
        /// The ceiling itself is why the knee exists at all rather than a wider linear
        /// range: median masstone chroma across the pigment library is 33.6 and the
        /// best blue reaches only 70.7, so a plain multiplier at any gain above about 2
        /// sends most of a photograph to a chroma no mixture the paints can make
        /// actually has. Those pixels then all land on whichever few boundary
        /// candidates are chromatic enough, and the image bands instead of saturating.
        /// tanh is strictly increasing, so distinct inputs still map to distinct
        /// outputs and keep landing on distinct candidates, and it asymptotes to the
        /// ceiling rather than crossing it, so nothing chases chroma the palette does
        /// not have.
        /// </para>
        /// <para>
        /// Two consequences follow from choosing identity over an absolute ceiling
        /// guarantee, and both must be read precisely rather than as "the ceiling
        /// mostly holds". At gain 1.0 (and, by continuity, anywhere at or below it) an
        /// input chroma that already exceeds the ceiling passes through unreduced —
        /// not a bug this stage should paper over, since it is exactly what happens
        /// with no remap at all, which is what Realism does today, and the
        /// nearest-candidate search downstream resolves an unreachable target to its
        /// nearest achievable colour regardless. Separately, because the linear term
        /// above keeps a strictly positive weight <c>(1 - knee)</c> at every gain
        /// below the parameter's maximum, some large enough input chroma still exceeds
        /// the ceiling at any such gain; the weight reaches zero, and the bound
        /// becomes exact for every input, only at gain equal to the parameter's
        /// declared maximum.
        /// </para>
        /// </summary>
        /// <param name="chroma">The source C*ab; must be strictly positive.</param>
        /// <param name="gain">The user's chroma gain, in this stage's declared
        /// [0.0, 3.0] range.</param>
        /// <param name="achievableMaxChroma">This render's achievable ceiling, from
        /// <see cref="RenderContext.AchievableMaxChroma"/>.</param>
        /// <returns>The reshaped C*ab. Strictly increasing in <paramref name="chroma"/>
        /// for any fixed <paramref name="gain"/> greater than zero.</returns>
        private static double ScaleChroma(double chroma, double gain, double achievableMaxChroma)
        {
            if (gain <= 1.0)
            {
                return gain * chroma;
            }

            // A ceiling of zero (an empty or fully neutral palette) would divide by
            // zero inside tanh's argument. NeutralThreshold is not a meaningful floor
            // here — at 1e-9 it leaves no real headroom — it is simply the smallest
            // value that keeps the division defined; a real palette's achievable
            // chroma is always many orders of magnitude above it, so the fallback
            // never actually engages outside this degenerate case.
            double ceiling = Math.Max(achievableMaxChroma, NeutralThreshold);

            // Linear in gain from 0 at gain 1.0 (matches the plain multiplier exactly,
            // keeping this branch continuous with the one above) to 1 at the
            // parameter's own declared maximum, so cranking the slider all the way
            // recovers the plan's original pure-tanh knee rather than a permanently
            // softened version of it. Read from the declaration itself, rather than
            // repeated as a literal, so the two can never drift apart if the slider's
            // range is ever retuned.
            double knee = (gain - 1.0) / (ChromaParameter.Maximum - 1.0);

            double linear = gain * chroma;
            double kneed = ceiling * Math.Tanh(gain * chroma / ceiling);

            return ((1.0 - knee) * linear) + (knee * kneed);
        }
    }
}
