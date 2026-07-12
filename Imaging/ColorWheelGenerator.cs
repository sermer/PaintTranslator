using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PaintTranslator.Data;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Generates color wheel images built from real paint colors: each paint owns a
    /// wedge at the rim, neighboring wedges blend into each other subtractively, and
    /// every color darkens toward the equal-parts mixture of all the paints at the
    /// center, the way physical pigments mix.
    /// </summary>
    public static class ColorWheelGenerator
    {
        /// <summary>
        /// Creates a color wheel bitmap using the full Golden Heavy Body palette.
        /// </summary>
        /// <param name="diameter">The width and height of the square bitmap, in pixels.</param>
        /// <returns>A 32-bit ARGB bitmap containing the color wheel.</returns>
        public static Bitmap Create(int diameter)
        {
            var colors = new Color[GoldenPalette.Paints.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = GoldenPalette.Paints[i].Color;
            }

            return Create(diameter, colors);
        }

        /// <summary>
        /// Creates a color wheel bitmap from the given paint colors. Each color sits at
        /// full concentration at an anchor point on the rim, evenly spaced in list order
        /// clockwise from the 3 o'clock position. Between anchors, the two flanking
        /// paints mix subtractively (in absorbance space) in proportion to the angle,
        /// giving each paint a wedge that blends into its neighbors; moving inward,
        /// every color mixes toward the equal-parts blend of all the paints at the
        /// center, the way pigments darken as more paints join the mix. Pixels outside
        /// the wheel are transparent.
        /// </summary>
        /// <param name="diameter">The width and height of the square bitmap, in pixels.</param>
        /// <param name="paintColors">The paint colors to distribute around the wheel.</param>
        /// <returns>A 32-bit ARGB bitmap containing the color wheel, or a fully
        /// transparent bitmap when <paramref name="paintColors"/> is empty.</returns>
        public static Bitmap Create(int diameter, IReadOnlyList<Color> paintColors)
        {
            if (diameter < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(diameter), "Diameter must be at least 2 pixels.");
            }
            if (paintColors == null)
            {
                throw new ArgumentNullException(nameof(paintColors));
            }

            var bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);

            // With every paint deselected there are no wedges to draw; a new ARGB
            // bitmap is already fully transparent, so return it as the empty wheel.
            if (paintColors.Count == 0)
            {
                return bitmap;
            }

            float center = (diameter - 1) / 2f;
            float radius = diameter / 2f;
            int count = paintColors.Count;

            // Precompute each paint's per-channel absorbances once; the hot per-pixel
            // loop then only blends numbers in that space.
            var absorption = new double[count][];
            for (int i = 0; i < count; i++)
            {
                absorption[i] = SubtractivePaintMixer.ToAbsorption(paintColors[i]);
            }

            // The equal-parts mixture of every paint — the color at the wheel's
            // center, which each wedge darkens toward as it approaches the middle.
            double centerRed = 0.0, centerGreen = 0.0, centerBlue = 0.0;
            for (int i = 0; i < count; i++)
            {
                centerRed += absorption[i][0];
                centerGreen += absorption[i][1];
                centerBlue += absorption[i][2];
            }
            centerRed /= count;
            centerGreen /= count;
            centerBlue /= count;

            // LockBits with direct buffer writes keeps generation fast; SetPixel would be
            // hundreds of times slower for a per-pixel fill like this.
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, diameter, diameter),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                byte[] buffer = new byte[data.Stride * diameter];

                for (int y = 0; y < diameter; y++)
                {
                    for (int x = 0; x < diameter; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                        // Fade alpha over the final pixel of the radius for a smooth,
                        // anti-aliased rim instead of a hard stair-stepped edge.
                        float alpha = Math.Clamp(radius - distance, 0f, 1f);
                        if (alpha <= 0f)
                        {
                            continue;
                        }

                        // The hue comes from the angle alone: only the two paints whose
                        // anchors flank this pixel's angle contribute, mixed in
                        // proportion to how far across the wedge the pixel sits, as if
                        // the two neighboring paints were blended on a palette.
                        double angle = Math.Atan2(dy, dx);
                        if (angle < 0.0)
                        {
                            angle += 2.0 * Math.PI;
                        }

                        double position = angle * count / (2.0 * Math.PI);
                        int lower = (int)position;
                        double blend = position - lower;

                        // Rounding can push an angle just below 2π up to a position of
                        // exactly count; that pixel belongs at the start of wedge 0.
                        if (lower >= count)
                        {
                            lower = 0;
                            blend = 0.0;
                        }
                        int upper = (lower + 1) % count;

                        double rimRed = (1.0 - blend) * absorption[lower][0] + blend * absorption[upper][0];
                        double rimGreen = (1.0 - blend) * absorption[lower][1] + blend * absorption[upper][1];
                        double rimBlue = (1.0 - blend) * absorption[lower][2] + blend * absorption[upper][2];

                        // Moving inward mixes the rim color with the all-paints center
                        // mixture, so wedges stay pure at the rim and darken toward the
                        // muddy equal-parts blend in the middle.
                        double rimShare = distance / radius;
                        Color color = SubtractivePaintMixer.FromAbsorption(
                            centerRed + rimShare * (rimRed - centerRed),
                            centerGreen + rimShare * (rimGreen - centerGreen),
                            centerBlue + rimShare * (rimBlue - centerBlue));

                        // Pixel layout for Format32bppArgb is B, G, R, A in memory.
                        int offset = y * data.Stride + x * 4;
                        buffer[offset] = color.B;
                        buffer[offset + 1] = color.G;
                        buffer[offset + 2] = color.R;
                        buffer[offset + 3] = (byte)(alpha * 255f);
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        /// <summary>
        /// Computes each paint's share of the mixture at a given pixel of a wheel
        /// produced by <see cref="Create(int, IReadOnlyList{Color})"/>, using the same
        /// geometry: the two paints flanking the pixel's angle contribute in proportion
        /// to its distance from the center (the rim share), and the remaining center
        /// share is split equally among all the paints.
        /// </summary>
        /// <param name="diameter">The diameter the wheel was generated with, in pixels.</param>
        /// <param name="paintCount">The number of paints the wheel was generated from.</param>
        /// <param name="x">The pixel's horizontal position within the wheel bitmap.</param>
        /// <param name="y">The pixel's vertical position within the wheel bitmap.</param>
        /// <returns>Each paint's mixing weight, index-aligned with the generating paint
        /// list and summing to 1, or null when the pixel lies outside the wheel or
        /// there are no paints.</returns>
        public static double[] GetBlendWeights(int diameter, int paintCount, int x, int y)
        {
            if (paintCount < 1)
            {
                return null;
            }

            float center = (diameter - 1) / 2f;
            float radius = diameter / 2f;
            float dx = x - center;
            float dy = y - center;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            // Match Create's alpha cutoff so weights exist exactly where the wheel
            // has visible pixels.
            if (radius - distance <= 0f)
            {
                return null;
            }

            // Same wedge math as Create: the angle picks the two flanking paints
            // and how far between their anchors the pixel sits.
            double angle = Math.Atan2(dy, dx);
            if (angle < 0.0)
            {
                angle += 2.0 * Math.PI;
            }

            double position = angle * paintCount / (2.0 * Math.PI);
            int lower = (int)position;
            double blend = position - lower;

            // Rounding can push an angle just below 2π up to a position of
            // exactly paintCount; that pixel belongs at the start of wedge 0.
            if (lower >= paintCount)
            {
                lower = 0;
                blend = 0.0;
            }
            int upper = (lower + 1) % paintCount;

            // Absorbances mix linearly, so the pixel's color decomposes exactly:
            // the center share spreads equally over every paint and the rim share
            // splits between the two flanking paints.
            double rimShare = Math.Min(distance / radius, 1.0);
            var weights = new double[paintCount];
            double centerShare = (1.0 - rimShare) / paintCount;
            for (int i = 0; i < paintCount; i++)
            {
                weights[i] = centerShare;
            }
            weights[lower] += rimShare * (1.0 - blend);
            weights[upper] += rimShare * blend;

            return weights;
        }

        /// <summary>
        /// Creates a color wheel from the full Golden palette and saves it to disk as
        /// a PNG, creating the target directory if it does not exist.
        /// </summary>
        /// <param name="path">The file path to write the PNG to.</param>
        /// <param name="diameter">The width and height of the square image, in pixels.</param>
        public static void SaveToFile(string path, int diameter)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (Bitmap wheel = Create(diameter))
            {
                wheel.Save(path, ImageFormat.Png);
            }
        }
    }
}
