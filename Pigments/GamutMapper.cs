using System;
using System.Drawing;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// Brings colours outside the sRGB gamut back inside it by reducing chroma at
    /// constant hue and lightness.
    /// <para>
    /// The alternative, clamping each channel independently, rotates hue: a saturated
    /// cadmium yellow has a negative blue channel, and forcing it to zero drags the
    /// colour toward white unevenly so it no longer claims to be the same colour.
    /// Compression instead gives up only saturation, which is honest — the screen
    /// really cannot show it — and reports how much it gave up, so the interface can
    /// say so.
    /// </para>
    /// <para>
    /// The compression runs in Oklab because its hue lines stay straight as chroma
    /// changes, which is exactly the property being relied on here.
    /// </para>
    /// </summary>
    public static class GamutMapper
    {
        /// <summary>
        /// How far outside [0, 1] a channel may sit and still count as representable,
        /// absorbing the rounding error of two matrix multiplications.
        /// </summary>
        private const double GamutTolerance = 1e-9;

        /// <summary>
        /// How many halvings the chroma search takes. Twenty-four brings the interval
        /// below one part in sixteen million, far under a byte of output precision.
        /// </summary>
        private const int SearchIterations = 24;

        /// <summary>
        /// Reports whether a linear sRGB triplet is representable.
        /// </summary>
        /// <param name="r">The linear red channel.</param>
        /// <param name="g">The linear green channel.</param>
        /// <param name="b">The linear blue channel.</param>
        /// <returns>True when every channel lies within [0, 1].</returns>
        public static bool IsInGamut(double r, double g, double b)
        {
            return r >= -GamutTolerance && r <= 1.0 + GamutTolerance
                && g >= -GamutTolerance && g <= 1.0 + GamutTolerance
                && b >= -GamutTolerance && b <= 1.0 + GamutTolerance;
        }

        /// <summary>
        /// Maps a linear sRGB triplet into the gamut and encodes it for display.
        /// </summary>
        /// <param name="r">The linear red channel.</param>
        /// <param name="g">The linear green channel.</param>
        /// <param name="b">The linear blue channel.</param>
        /// <param name="chromaLost">The Oklab chroma given up to fit the gamut; zero
        /// when the colour was already representable.</param>
        /// <returns>The displayable colour with full alpha.</returns>
        public static Color ToDisplayColor(double r, double g, double b, out double chromaLost)
        {
            Compress(r, g, b, out double mappedR, out double mappedG, out double mappedB, out chromaLost);

            return Color.FromArgb(
                255,
                ToChannel(mappedR),
                ToChannel(mappedG),
                ToChannel(mappedB));
        }

        /// <summary>
        /// Compresses a linear sRGB triplet into the gamut, staying in linear light so
        /// tests can inspect the result before it is quantised to bytes.
        /// </summary>
        /// <param name="r">The linear red channel.</param>
        /// <param name="g">The linear green channel.</param>
        /// <param name="b">The linear blue channel.</param>
        /// <param name="mappedR">The resulting linear red channel.</param>
        /// <param name="mappedG">The resulting linear green channel.</param>
        /// <param name="mappedB">The resulting linear blue channel.</param>
        /// <param name="chromaLost">The Oklab chroma given up to fit the gamut.</param>
        internal static void Compress(
            double r, double g, double b,
            out double mappedR, out double mappedG, out double mappedB,
            out double chromaLost)
        {
            if (IsInGamut(r, g, b))
            {
                mappedR = r;
                mappedG = g;
                mappedB = b;
                chromaLost = 0.0;
                return;
            }

            ColorSpace.LinearRgbToOklab(r, g, b,
                out double lightness, out double aAxis, out double bAxis);

            // Lightness outside the representable range has no in-gamut point at any
            // chroma, so it has to be brought in first or the search below can never
            // succeed and would return the achromatic colour at an illegal lightness.
            lightness = Math.Clamp(lightness, 0.0, 1.0);

            double chroma = Math.Sqrt((aAxis * aAxis) + (bAxis * bAxis));
            if (chroma <= 0.0)
            {
                ColorSpace.OklabToLinearRgb(lightness, 0.0, 0.0, out mappedR, out mappedG, out mappedB);
                mappedR = Math.Clamp(mappedR, 0.0, 1.0);
                mappedG = Math.Clamp(mappedG, 0.0, 1.0);
                mappedB = Math.Clamp(mappedB, 0.0, 1.0);
                chromaLost = 0.0;
                return;
            }

            double hueA = aAxis / chroma;
            double hueB = bAxis / chroma;

            // Binary search the largest chroma along this hue that still fits. The
            // achromatic end is always representable once lightness is clamped, so the
            // low bound needs no separate check.
            double representable = 0.0;
            double tooMuch = chroma;
            for (int i = 0; i < SearchIterations; i++)
            {
                double candidate = 0.5 * (representable + tooMuch);
                ColorSpace.OklabToLinearRgb(lightness, hueA * candidate, hueB * candidate,
                    out double testR, out double testG, out double testB);

                if (IsInGamut(testR, testG, testB))
                {
                    representable = candidate;
                }
                else
                {
                    tooMuch = candidate;
                }
            }

            ColorSpace.OklabToLinearRgb(lightness, hueA * representable, hueB * representable,
                out mappedR, out mappedG, out mappedB);

            // The search leaves the result a rounding error outside the gamut at worst;
            // clamping here is a numerical guard, not a colour decision.
            mappedR = Math.Clamp(mappedR, 0.0, 1.0);
            mappedG = Math.Clamp(mappedG, 0.0, 1.0);
            mappedB = Math.Clamp(mappedB, 0.0, 1.0);
            chromaLost = chroma - representable;
        }

        /// <summary>
        /// Encodes one in-gamut linear channel as a byte.
        /// </summary>
        /// <param name="linear">The linear channel value, in [0, 1].</param>
        /// <returns>The sRGB-encoded channel, 0 to 255.</returns>
        private static int ToChannel(double linear)
        {
            double encoded = ColorSpace.LinearToSrgb(Math.Clamp(linear, 0.0, 1.0));
            return (int)Math.Round(Math.Clamp(encoded, 0.0, 1.0) * 255.0);
        }
    }
}
