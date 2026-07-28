using System;
using System.Threading.Tasks;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Moves one colour channel between the packed ARGB buffer and a plane of
    /// linear-light floats.
    /// <para>
    /// Every spatial filter in this application has to average in linear light rather
    /// than on the sRGB-encoded channels the buffer stores. sRGB encoding is a power
    /// curve, so a mean taken across it is not the mean of the light being
    /// represented: averaging an edge in encoded space pulls the midpoint toward the
    /// darker side and leaves a visible dark seam wherever two bright colours meet.
    /// This is shared rather than duplicated per filter precisely because a filter
    /// that quietly skipped it would produce a picture that merely looks a bit wrong.
    /// </para>
    /// </summary>
    internal static class LinearPlanes
    {
        /// <summary>The red channel's bit offset within a packed pixel.</summary>
        public const int RedShift = 16;

        /// <summary>The green channel's bit offset within a packed pixel.</summary>
        public const int GreenShift = 8;

        /// <summary>The blue channel's bit offset within a packed pixel.</summary>
        public const int BlueShift = 0;

        /// <summary>
        /// Linear-light value of each of the 256 sRGB channel codes. The decode is a
        /// branch and a Pow per channel, which at three channels per pixel dwarfs the
        /// filtering itself; there are only 256 possible inputs, so it is done once.
        /// </summary>
        private static readonly float[] LinearFromSrgb = BuildLinearTable();

        /// <summary>
        /// Decodes one colour channel of a pixel buffer into a linear-light plane.
        /// </summary>
        /// <param name="pixels">The 32-bit ARGB pixels to read.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="shift">The channel's bit offset: 16, 8, or 0.</param>
        /// <param name="plane">The width-by-height plane to fill.</param>
        public static void Decode(int[] pixels, int strideInts, int width, int height, int shift, float[] plane)
        {
            Parallel.For(0, height, y =>
            {
                int source = y * strideInts;
                int target = y * width;
                for (int x = 0; x < width; x++)
                {
                    plane[target + x] = LinearFromSrgb[(pixels[source + x] >> shift) & 0xFF];
                }
            });
        }

        /// <summary>
        /// Encodes a linear-light plane back into one colour channel, leaving the
        /// pixels' other channels and their alpha untouched.
        /// </summary>
        /// <param name="plane">The plane to read.</param>
        /// <param name="pixels">The 32-bit ARGB pixels, modified in place.</param>
        /// <param name="strideInts">The number of ints per pixel row (stride / 4).</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="shift">The channel's bit offset: 16, 8, or 0.</param>
        public static void Encode(float[] plane, int[] pixels, int strideInts, int width, int height, int shift)
        {
            int mask = ~(0xFF << shift);

            Parallel.For(0, height, y =>
            {
                int source = y * width;
                int target = y * strideInts;
                for (int x = 0; x < width; x++)
                {
                    // A weighted mean of values in [0, 1] cannot leave [0, 1], so the
                    // clamp is only guarding the last bit of floating-point slack at
                    // the extremes.
                    double linear = Math.Clamp((double)plane[source + x], 0.0, 1.0);
                    int channel = (int)Math.Round(ColorSpace.LinearToSrgb(linear) * 255.0);
                    pixels[target + x] = (pixels[target + x] & mask) | (Math.Clamp(channel, 0, 255) << shift);
                }
            });
        }

        /// <summary>
        /// Tabulates the linear-light value of every 8-bit sRGB channel code.
        /// </summary>
        /// <returns>The 256 decoded values, indexed by channel code.</returns>
        private static float[] BuildLinearTable()
        {
            var table = new float[256];
            for (int code = 0; code < table.Length; code++)
            {
                table[code] = (float)ColorSpace.SrgbToLinear(code / 255.0);
            }

            return table;
        }
    }
}
