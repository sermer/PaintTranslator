using System;
using System.Drawing;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// Turns a reflectance spectrum into a colour, by integrating it against the CIE
    /// standard observer under D65.
    /// <para>
    /// Lab and the display colour are separate outputs on purpose. Every comparison —
    /// recipe search, match quality, the invariant tests — runs on the unmapped Lab,
    /// because two different out-of-gamut mixtures can compress to the same screen
    /// colour, and a search that compared mapped values would sometimes prefer the wrong
    /// paint. Gamut mapping is a display concern and appears nowhere else.
    /// </para>
    /// </summary>
    public static class SpectralRenderer
    {
        /// <summary>
        /// The sum of the luminance weights, used to normalise the integration so a
        /// perfect diffuser yields Y = 1 and therefore L* = 100. Computing it rather
        /// than assuming it keeps the renderer correct whatever scale the observer
        /// tables were published at.
        /// </summary>
        private static readonly double LuminanceNormalisation = SumLuminanceWeights();

        /// <summary>
        /// Integrates a reflectance spectrum to CIE XYZ.
        /// </summary>
        /// <param name="reflectance">The spectrum, length <see cref="SpectralBands.Count"/>.</param>
        /// <param name="x">The resulting X tristimulus value.</param>
        /// <param name="y">The resulting Y tristimulus value.</param>
        /// <param name="z">The resulting Z tristimulus value.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="reflectance"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the spectrum is the wrong length.</exception>
        public static void ToXyz(double[] reflectance, out double x, out double y, out double z)
        {
            if (reflectance == null)
            {
                throw new ArgumentNullException(nameof(reflectance));
            }
            if (reflectance.Length != SpectralBands.Count)
            {
                throw new ArgumentException(
                    $"A spectrum must have {SpectralBands.Count} bands.", nameof(reflectance));
            }

            double sumX = 0.0;
            double sumY = 0.0;
            double sumZ = 0.0;
            for (int band = 0; band < SpectralBands.Count; band++)
            {
                sumX += SpectralBands.ObserverX[band] * reflectance[band];
                sumY += SpectralBands.ObserverY[band] * reflectance[band];
                sumZ += SpectralBands.ObserverZ[band] * reflectance[band];
            }

            x = sumX / LuminanceNormalisation;
            y = sumY / LuminanceNormalisation;
            z = sumZ / LuminanceNormalisation;
        }

        /// <summary>
        /// Integrates a reflectance spectrum to CIELAB, without gamut mapping. This is
        /// the colour a mixture actually is, which may be outside anything a screen can
        /// show, and it is what every comparison in the application uses.
        /// </summary>
        /// <param name="reflectance">The spectrum, length <see cref="SpectralBands.Count"/>.</param>
        /// <param name="lightness">The resulting L* component.</param>
        /// <param name="aStar">The resulting a* component.</param>
        /// <param name="bStar">The resulting b* component.</param>
        public static void ToLab(
            double[] reflectance, out double lightness, out double aStar, out double bStar)
        {
            ToXyz(reflectance, out double x, out double y, out double z);
            ColorSpace.XyzToLab(x, y, z, out lightness, out aStar, out bStar);
        }

        /// <summary>
        /// Integrates a reflectance spectrum to a colour a screen can display.
        /// </summary>
        /// <param name="reflectance">The spectrum, length <see cref="SpectralBands.Count"/>.</param>
        /// <param name="chromaLost">The Oklab chroma given up to fit the sRGB gamut;
        /// zero when the colour was already representable. The interface uses this to
        /// mark swatches that are more vivid in the tube than on the screen.</param>
        /// <returns>The displayable colour with full alpha.</returns>
        public static Color ToDisplayColor(double[] reflectance, out double chromaLost)
        {
            ToXyz(reflectance, out double x, out double y, out double z);
            ColorSpace.XyzToLinearRgb(x, y, z, out double r, out double g, out double b);

            return GamutMapper.ToDisplayColor(r, g, b, out chromaLost);
        }

        /// <summary>
        /// Sums the luminance weights across every band.
        /// </summary>
        /// <returns>The sum used to normalise the integration.</returns>
        private static double SumLuminanceWeights()
        {
            double sum = 0.0;
            for (int band = 0; band < SpectralBands.Count; band++)
            {
                sum += SpectralBands.ObserverY[band];
            }

            return sum;
        }
    }
}
