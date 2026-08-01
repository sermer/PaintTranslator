using System;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// Conversions between the colour spaces this project uses: sRGB encoding, linear
    /// sRGB, CIE XYZ under D65, CIELAB, and Oklab.
    /// <para>
    /// These conversions previously existed in two places with slightly different
    /// constants. The D65 matrix and the Lab transfer curve here are copied verbatim
    /// from <c>PalettePhotoConverter</c> so that consolidating them cannot move any
    /// existing match result.
    /// </para>
    /// </summary>
    public static class ColorSpace
    {
        private static readonly double[] LinearFromSrgbByte = BuildLinearFromSrgbByte();

        /// <summary>The D65 reference white's X tristimulus value.</summary>
        private const double WhiteX = 0.95047;

        /// <summary>The D65 reference white's Y tristimulus value.</summary>
        private const double WhiteY = 1.00000;

        /// <summary>The D65 reference white's Z tristimulus value.</summary>
        private const double WhiteZ = 1.08883;

        /// <summary>
        /// Decodes an sRGB-encoded channel to linear light.
        /// </summary>
        /// <param name="encoded">The sRGB-encoded channel, normally in [0, 1].</param>
        /// <returns>The linear-light value of the channel.</returns>
        public static double SrgbToLinear(double encoded)
        {
            return encoded <= 0.04045
                ? encoded / 12.92
                : Math.Pow((encoded + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Decodes an eight-bit sRGB channel without repeating its power operation.
        /// The values exactly match <see cref="SrgbToLinear(double)"/>.
        /// </summary>
        internal static double SrgbByteToLinear(int encoded)
        {
            return LinearFromSrgbByte[encoded];
        }

        /// <summary>
        /// Encodes a linear-light value back to sRGB.
        /// </summary>
        /// <param name="linear">The linear-light value. Values outside [0, 1] are
        /// returned outside [0, 1] rather than clamped, because the gamut mapper needs
        /// to see how far out of range a colour is.</param>
        /// <returns>The sRGB-encoded channel value.</returns>
        public static double LinearToSrgb(double linear)
        {
            return linear <= 0.0031308
                ? linear * 12.92
                : 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
        }

        /// <summary>
        /// Converts linear sRGB to CIE XYZ using the standard D65 matrix.
        /// </summary>
        /// <param name="r">The linear red channel.</param>
        /// <param name="g">The linear green channel.</param>
        /// <param name="b">The linear blue channel.</param>
        /// <param name="x">The resulting X tristimulus value.</param>
        /// <param name="y">The resulting Y tristimulus value.</param>
        /// <param name="z">The resulting Z tristimulus value.</param>
        public static void LinearRgbToXyz(
            double r, double g, double b, out double x, out double y, out double z)
        {
            x = 0.4124564 * r + 0.3575761 * g + 0.1804375 * b;
            y = 0.2126729 * r + 0.7151522 * g + 0.0721750 * b;
            z = 0.0193339 * r + 0.1191920 * g + 0.9503041 * b;
        }

        /// <summary>
        /// Converts CIE XYZ to linear sRGB. Channels may fall outside [0, 1] when the
        /// colour is outside the sRGB gamut, which is expected for saturated paints.
        /// </summary>
        /// <param name="x">The X tristimulus value.</param>
        /// <param name="y">The Y tristimulus value.</param>
        /// <param name="z">The Z tristimulus value.</param>
        /// <param name="r">The resulting linear red channel.</param>
        /// <param name="g">The resulting linear green channel.</param>
        /// <param name="b">The resulting linear blue channel.</param>
        public static void XyzToLinearRgb(
            double x, double y, double z, out double r, out double g, out double b)
        {
            r = 3.2404542 * x - 1.5371385 * y - 0.4985314 * z;
            g = -0.9692660 * x + 1.8760108 * y + 0.0415560 * z;
            b = 0.0556434 * x - 0.2040259 * y + 1.0572252 * z;
        }

        /// <summary>
        /// Converts CIE XYZ to CIELAB against the D65 reference white.
        /// </summary>
        /// <param name="x">The X tristimulus value.</param>
        /// <param name="y">The Y tristimulus value.</param>
        /// <param name="z">The Z tristimulus value.</param>
        /// <param name="lightness">The resulting L* component.</param>
        /// <param name="aStar">The resulting a* component.</param>
        /// <param name="bStar">The resulting b* component.</param>
        public static void XyzToLab(
            double x, double y, double z,
            out double lightness, out double aStar, out double bStar)
        {
            double fx = LabTransfer(x / WhiteX);
            double fy = LabTransfer(y / WhiteY);
            double fz = LabTransfer(z / WhiteZ);

            lightness = 116.0 * fy - 16.0;
            aStar = 500.0 * (fx - fy);
            bStar = 200.0 * (fy - fz);
        }

        /// <summary>
        /// Converts linear sRGB to Oklab, the space the gamut mapper compresses in
        /// because its hue lines stay straight under chroma changes.
        /// </summary>
        /// <param name="r">The linear red channel.</param>
        /// <param name="g">The linear green channel.</param>
        /// <param name="b">The linear blue channel.</param>
        /// <param name="lightness">The resulting L component.</param>
        /// <param name="aAxis">The resulting a component.</param>
        /// <param name="bAxis">The resulting b component.</param>
        public static void LinearRgbToOklab(
            double r, double g, double b,
            out double lightness, out double aAxis, out double bAxis)
        {
            double longCone = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
            double mediumCone = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
            double shortCone = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

            double l = Math.Cbrt(longCone);
            double m = Math.Cbrt(mediumCone);
            double s = Math.Cbrt(shortCone);

            lightness = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
            aAxis = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
            bAxis = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        }

        /// <summary>
        /// Converts Oklab back to linear sRGB.
        /// </summary>
        /// <param name="lightness">The L component.</param>
        /// <param name="aAxis">The a component.</param>
        /// <param name="bAxis">The b component.</param>
        /// <param name="r">The resulting linear red channel.</param>
        /// <param name="g">The resulting linear green channel.</param>
        /// <param name="b">The resulting linear blue channel.</param>
        public static void OklabToLinearRgb(
            double lightness, double aAxis, double bAxis,
            out double r, out double g, out double b)
        {
            double l = lightness + 0.3963377774 * aAxis + 0.2158037573 * bAxis;
            double m = lightness - 0.1055613458 * aAxis - 0.0638541728 * bAxis;
            double s = lightness - 0.0894841775 * aAxis - 1.2914855480 * bAxis;

            double longCone = l * l * l;
            double mediumCone = m * m * m;
            double shortCone = s * s * s;

            r = 4.0767416621 * longCone - 3.3077115913 * mediumCone + 0.2309699292 * shortCone;
            g = -1.2684380046 * longCone + 2.6097574011 * mediumCone - 0.3413193965 * shortCone;
            b = -0.0041960863 * longCone - 0.7034186147 * mediumCone + 1.7076147010 * shortCone;
        }

        /// <summary>
        /// Applies the CIELAB transfer curve: a cube root with a linear segment near
        /// zero to keep the slope finite for very dark values.
        /// </summary>
        /// <param name="t">The white-point-normalised tristimulus value.</param>
        /// <returns>The transfer-curve output used by the L*, a*, b* formulas.</returns>
        private static double LabTransfer(double t)
        {
            const double Epsilon = 216.0 / 24389.0;
            const double Kappa = 24389.0 / 27.0;
            return t > Epsilon ? Math.Cbrt(t) : (Kappa * t + 16.0) / 116.0;
        }

        private static double[] BuildLinearFromSrgbByte()
        {
            var table = new double[256];
            for (int encoded = 0; encoded < table.Length; encoded++)
            {
                table[encoded] = SrgbToLinear(encoded / 255.0);
            }

            return table;
        }
    }
}
