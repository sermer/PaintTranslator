using System;
using System.Collections.Generic;
using PaintTranslator.Imaging;
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
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <returns>A new opaque ARGB image.</returns>
        internal static PixelImage BuildGradient(int width, int height)
        {
            var pixels = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = (x * 255) / (width - 1);
                    int g = (y * 255) / (height - 1);
                    int b = ((x + y) * 255) / (width + height - 2);
                    pixels[(y * width) + x] = Argb(255, r, g, b);
                }
            }

            return PixelImage.FromPixels(width, height, pixels);
        }

        /// <summary>
        /// A smooth bilinear field between four plausible photographic colours, with
        /// Gaussian sensor noise layered on top at a fixed seed so the same call
        /// always produces the same image. Neighbouring pixels differ by well under
        /// one 8-bit code before the noise is added, so anything fragmented in a
        /// converted output came from the conversion rather than the source.
        /// <para>
        /// The loop order and the <c>Random(7)</c> seed are load-bearing: the golden
        /// PNGs were rendered from exactly this sequence of pixels, and the
        /// benchmark keeps its own copy of this generator that must match.
        /// </para>
        /// </summary>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="sigma">The standard deviation of the Gaussian noise added to
        /// each channel, in 8-bit code values. Zero disables the noise term
        /// entirely, leaving the bare bilinear field.</param>
        /// <returns>A new opaque ARGB image.</returns>
        internal static PixelImage BuildNoisyGradient(int width, int height, double sigma)
        {
            var corners = new[]
            {
                new[] { 28.0, 38.0, 92.0 },
                new[] { 232.0, 214.0, 168.0 },
                new[] { 176.0, 62.0, 48.0 },
                new[] { 244.0, 242.0, 238.0 },
            };

            var rng = new Random(7);
            var pixels = new int[width * height];
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

                    pixels[(y * width) + x] = Argb(255, channel[0], channel[1], channel[2]);
                }
            }

            return PixelImage.FromPixels(width, height, pixels);
        }

        /// <summary>
        /// Kept with the old <c>out</c> stride so the many call sites that index
        /// <c>row = y * stride</c> do not change; a <see cref="PixelImage"/> has no
        /// padding, so the stride is simply the width.
        /// </summary>
        internal static int[] ReadPixels(PixelImage image, out int strideInts)
        {
            strideInts = image.Width;
            return image.CopyPixels();
        }

        /// <summary>Packs channels into <c>0xAARRGGBB</c>, matching <see cref="PixelImage"/>'s layout.</summary>
        internal static int Argb(int a, int r, int g, int b)
        {
            return (a << 24) | (r << 16) | (g << 8) | b;
        }
    }
}
