using System;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Approximates subtractive (pigment) color mixing by blending in absorbance
    /// (log-reflectance) space: each channel's reflectance is converted to an
    /// absorbance, absorbances combine linearly by mixing weight — equivalent to a
    /// weighted geometric mean of the reflectances — and the result converts back
    /// to a displayable color. Blends darken the way physical paints do (yellow
    /// and blue mix toward green rather than gray), while the influence of each
    /// paint stays proportional to its share of the mix, so a trace of a strong
    /// dark pigment only nudges the result instead of overwhelming it.
    /// </summary>
    public static class SubtractivePaintMixer
    {
        // Floor keeping absorbance finite for zero-reflectance channels; real
        // paints always reflect at least a little light in every band.
        private const double MinReflectance = 0.0005;

        /// <summary>
        /// Mixes two paint colors subtractively.
        /// </summary>
        /// <param name="a">The first paint color.</param>
        /// <param name="b">The second paint color.</param>
        /// <param name="weightOfB">The share of <paramref name="b"/> in the mix, from 0 (all a) to 1 (all b).</param>
        /// <returns>The mixed color with full alpha.</returns>
        public static Color Mix(Color a, Color b, double weightOfB)
        {
            double[] absorbanceA = ToAbsorption(a);
            double[] absorbanceB = ToAbsorption(b);
            double w = Math.Clamp(weightOfB, 0.0, 1.0);

            return FromAbsorption(
                (1.0 - w) * absorbanceA[0] + w * absorbanceB[0],
                (1.0 - w) * absorbanceA[1] + w * absorbanceB[1],
                (1.0 - w) * absorbanceA[2] + w * absorbanceB[2]);
        }

        /// <summary>
        /// Converts a color to its per-channel absorbances (negative log of linear
        /// reflectance) — the space in which paint mixtures combine linearly.
        /// </summary>
        /// <param name="color">The paint color to convert.</param>
        /// <returns>The absorbances for the red, green, and blue channels, in that order.</returns>
        public static double[] ToAbsorption(Color color)
        {
            return new[]
            {
                AbsorbanceFromReflectance(SrgbToLinear(color.R)),
                AbsorbanceFromReflectance(SrgbToLinear(color.G)),
                AbsorbanceFromReflectance(SrgbToLinear(color.B)),
            };
        }

        /// <summary>
        /// Converts per-channel absorbances back to a displayable color.
        /// </summary>
        /// <param name="absorbanceRed">The absorbance for the red channel.</param>
        /// <param name="absorbanceGreen">The absorbance for the green channel.</param>
        /// <param name="absorbanceBlue">The absorbance for the blue channel.</param>
        /// <returns>The equivalent sRGB color with full alpha.</returns>
        public static Color FromAbsorption(double absorbanceRed, double absorbanceGreen, double absorbanceBlue)
        {
            return Color.FromArgb(
                255,
                LinearToSrgb(ReflectanceFromAbsorbance(absorbanceRed)),
                LinearToSrgb(ReflectanceFromAbsorbance(absorbanceGreen)),
                LinearToSrgb(ReflectanceFromAbsorbance(absorbanceBlue)));
        }

        /// <summary>
        /// Decodes an 8-bit sRGB channel to linear reflectance in [0, 1].
        /// </summary>
        /// <param name="channel">The sRGB-encoded channel value.</param>
        /// <returns>The linear-light reflectance of the channel.</returns>
        private static double SrgbToLinear(byte channel)
        {
            double c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Encodes linear reflectance back to an 8-bit sRGB channel.
        /// </summary>
        /// <param name="linear">The linear-light reflectance, clamped to [0, 1].</param>
        /// <returns>The sRGB-encoded channel value.</returns>
        private static int LinearToSrgb(double linear)
        {
            double clamped = Math.Clamp(linear, 0.0, 1.0);
            double c = clamped <= 0.0031308 ? clamped * 12.92 : 1.055 * Math.Pow(clamped, 1.0 / 2.4) - 0.055;
            return (int)Math.Round(c * 255.0);
        }

        /// <summary>
        /// Computes the absorbance for a reflectance value.
        /// </summary>
        /// <param name="reflectance">The linear reflectance, floored to keep the absorbance finite.</param>
        /// <returns>The absorbance; 0 for a perfect reflector.</returns>
        private static double AbsorbanceFromReflectance(double reflectance)
        {
            double r = Math.Clamp(reflectance, MinReflectance, 1.0);
            return -Math.Log(r);
        }

        /// <summary>
        /// Inverts an absorbance back to linear reflectance.
        /// </summary>
        /// <param name="absorbance">The absorbance, 0 or greater.</param>
        /// <returns>The linear reflectance in [0, 1].</returns>
        private static double ReflectanceFromAbsorbance(double absorbance)
        {
            return Math.Exp(-absorbance);
        }
    }
}
