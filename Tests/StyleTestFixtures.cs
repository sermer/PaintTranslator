using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PaintTranslator.Pigments;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Shared building blocks for the style-pipeline tests: a palette wide enough to
    /// show every style's chroma and lightness moves, two deterministic source
    /// images, and the raw-pixel reader every test built on either one needs.
    /// <see cref="StyleBehaviourTests"/> and <see cref="StylePipelineTests"/> both
    /// carried their own copies of these before this class existed; they now call
    /// this one instead so a change to the gradient construction or the pixel
    /// layout cannot silently diverge between them.
    /// </summary>
    internal static class StyleTestFixtures
    {
        /// <summary>
        /// A six-paint palette with both an achromatic anchor and wide hue coverage,
        /// so a style that raises or lowers chroma has real headroom to move in
        /// either direction rather than being pulled back toward whatever colour the
        /// achievable gamut happens to already sit near.
        /// <para>
        /// A three-paint palette (white, red, blue) was tried first and understated
        /// Tonalism's desaturation: with no achromatic paint beyond white itself, the
        /// achievable gamut had no near-neutral candidates for a desaturated target
        /// to land on, so nearest-candidate matching pulled even heavily desaturated
        /// pixels back up toward whatever chroma the nearest achievable colour
        /// happened to have. Adding Bone Black — a true near-neutral, not merely a
        /// light one — gives the gamut real near-neutral candidates, which is what a
        /// chroma-lowering style needs to have somewhere to put a pixel. The
        /// remaining four paints span red, yellow, magenta and blue so a
        /// chroma-raising style has hue-rich candidates to move toward as well.
        /// </para>
        /// </summary>
        internal static IReadOnlyList<PigmentCoefficients> SixPaints()
        {
            return new[]
            {
                PigmentLibrary.Selectable[0],   // Titanium White
                PigmentLibrary.Selectable[2],   // Hansa Yellow Opaque
                PigmentLibrary.Selectable[6],   // C.P. Cadmium Red Light
                PigmentLibrary.Selectable[9],   // Quinacridone Magenta
                PigmentLibrary.Selectable[11],  // Ultramarine Blue
                PigmentLibrary.Selectable[18],  // Bone Black
            };
        }

        /// <summary>
        /// A three-paint palette spanning light, warm and dark, which is enough for
        /// the candidate set to have interior structure without making a test slow.
        /// </summary>
        internal static IReadOnlyList<PigmentCoefficients> ThreePaints()
        {
            return new[]
            {
                PigmentLibrary.Selectable[0],   // Titanium White
                PigmentLibrary.Selectable[6],   // C.P. Cadmium Red Light
                PigmentLibrary.Selectable[11],  // Ultramarine Blue
            };
        }

        /// <summary>
        /// A smooth bilinear-ish gradient spanning a wide range of hue and
        /// lightness, large enough that a style collapsing chroma to a handful of
        /// boundary candidates shows up as a sharp drop in distinct-colour count
        /// rather than being lost in a small sample.
        /// </summary>
        /// <param name="width">The bitmap width in pixels.</param>
        /// <param name="height">The bitmap height in pixels.</param>
        /// <returns>A new opaque 32bpp ARGB bitmap.</returns>
        internal static Bitmap BuildGradientBitmap(int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = (x * 255) / (width - 1);
                    int g = (y * 255) / (height - 1);
                    int b = ((x + y) * 255) / (width + height - 2);
                    bitmap.SetPixel(x, y, Color.FromArgb(255, r, g, b));
                }
            }

            return bitmap;
        }

        /// <summary>
        /// A smooth bilinear field between four plausible photographic colours, with
        /// Gaussian sensor noise layered on top at a fixed seed so the same call
        /// always produces the same bitmap. Neighbouring pixels differ by well under
        /// one 8-bit code before the noise is added, so anything fragmented in a
        /// converted output came from the conversion rather than the source.
        /// </summary>
        /// <param name="width">The bitmap width in pixels.</param>
        /// <param name="height">The bitmap height in pixels.</param>
        /// <param name="sigma">The standard deviation of the Gaussian noise added to
        /// each channel, in 8-bit code values. Zero disables the noise term
        /// entirely, leaving the bare bilinear field.</param>
        /// <returns>A new opaque 32bpp ARGB bitmap.</returns>
        internal static Bitmap BuildNoisyGradient(int width, int height, double sigma)
        {
            var corners = new[]
            {
                new[] { 28.0, 38.0, 92.0 },
                new[] { 232.0, 214.0, 168.0 },
                new[] { 176.0, 62.0, 48.0 },
                new[] { 244.0, 242.0, 238.0 },
            };

            var rng = new Random(7);
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                double fy = y / (double)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    double fx = x / (double)(width - 1);
                    var channel = new int[3];
                    for (int c = 0; c < 3; c++)
                    {
                        double top = (corners[0][c] * (1 - fx)) + (corners[1][c] * fx);
                        double bottom = (corners[2][c] * (1 - fx)) + (corners[3][c] * fx);
                        double value = (top * (1 - fy)) + (bottom * fy);
                        if (sigma > 0.0)
                        {
                            double u1 = 1.0 - rng.NextDouble();
                            double u2 = rng.NextDouble();
                            value += sigma * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                        }

                        channel[c] = Math.Clamp((int)Math.Round(value), 0, 255);
                    }

                    bitmap.SetPixel(x, y, Color.FromArgb(255, channel[0], channel[1], channel[2]));
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Reads every pixel of a locked-then-copied bitmap into a flat ARGB array,
        /// so callers can inspect or compare pixels without holding the bitmap
        /// locked for the duration.
        /// </summary>
        /// <param name="bitmap">The bitmap to read.</param>
        /// <param name="strideInts">The number of ints per pixel row, i.e. the raw
        /// byte stride divided by four.</param>
        /// <returns>The bitmap's pixels as 32-bit ARGB values, row-major with each
        /// row padded to <paramref name="strideInts"/> ints.</returns>
        internal static int[] ReadPixels(Bitmap bitmap, out int strideInts)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                strideInts = data.Stride / 4;
                var pixels = new int[strideInts * bitmap.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                return pixels;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
