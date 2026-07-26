using System;
using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Data;
using Wacton.Unicolour;
using Wacton.Unicolour.Datasets;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Mixes paints from spectrophotometer measurements using two-constant Kubelka-Munk
    /// theory, with absorption and scattering tracked separately per wavelength.
    /// <para>
    /// This is the difference that matters against
    /// <see cref="SubtractivePaintMixer"/>, which reconstructs a spectrum from a paint's
    /// sRGB value and can only guess at scattering from luminance. Guessing puts titanium
    /// white — the most luminous paint there is — at roughly twenty-four times the
    /// influence of a dark paint, so a tenth of white swamps a mixture. Measured
    /// scattering fixes that, and it is also what produces a paint's tint correctly:
    /// mass tone and undertone are the same coefficients evaluated at different
    /// concentrations, not two separate colours to store.
    /// </para>
    /// <para>
    /// The mixing itself is delegated to Unicolour (MIT License, William Acton), whose
    /// implementation includes the Saunderson correction for light reflected off the film
    /// surface before it reaches any pigment.
    /// </para>
    /// </summary>
    public static class MeasuredPaintMixer
    {
        /// <summary>
        /// Mixes measured paints in the given proportions.
        /// </summary>
        /// <param name="paints">The participating paints.</param>
        /// <param name="weights">Each paint's share of the mix, index-aligned with
        /// <paramref name="paints"/>; shares are relative, so they need not sum to 1.</param>
        /// <returns>The mixed colour with full alpha, clamped into the sRGB gamut.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paints"/> or <paramref name="weights"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the lists are empty or their lengths differ.</exception>
        public static Color Mix(IReadOnlyList<MeasuredPaint> paints, IReadOnlyList<double> weights)
        {
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }
            if (paints.Count == 0 || paints.Count != weights.Count)
            {
                throw new ArgumentException("Each paint needs exactly one mixing weight.", nameof(weights));
            }

            var pigments = new Pigment[paints.Count];
            var shares = new double[weights.Count];
            for (int i = 0; i < paints.Count; i++)
            {
                pigments[i] = paints[i].Pigment;
                shares[i] = weights[i];
            }

            var mixed = new Unicolour(ArtistPaint.Configuration, pigments, shares);
            ColourTriplet rgb = mixed.Rgb.Byte255.Triplet;

            // Mass tones and saturated mixtures routinely fall outside sRGB — cadmium
            // yellow's measured b* alone exceeds anything a screen can show — so the
            // conversion can return channels beyond 0-255. Clamping keeps the displayed
            // swatch legal; the mixture's real colour is simply more saturated than the
            // screen can represent.
            return Color.FromArgb(
                ToChannel(rgb.First),
                ToChannel(rgb.Second),
                ToChannel(rgb.Third));
        }

        /// <summary>
        /// Rounds and clamps one channel of a converted colour into the 0-255 range.
        /// </summary>
        /// <param name="value">The channel value, which may fall outside the sRGB gamut.</param>
        /// <returns>The channel as a byte value.</returns>
        private static int ToChannel(double value)
        {
            if (double.IsNaN(value))
            {
                return 0;
            }

            return (int)Math.Round(Math.Clamp(value, 0.0, 255.0));
        }
    }
}
