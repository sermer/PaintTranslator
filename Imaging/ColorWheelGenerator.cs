using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Generates colour wheel images from measured paints: each paint owns a wedge at
    /// the rim, neighbouring wedges blend into each other, and every colour moves toward
    /// the equal-parts mixture of all the paints at the centre.
    /// <para>
    /// This is a view and nothing more. The geometry comes from
    /// <see cref="BlendGeometry"/> and the colour from <see cref="KubelkaMunk"/> and
    /// <see cref="SpectralRenderer"/>, which is what guarantees a pixel and the recipe
    /// reported for it are the same mixture.
    /// </para>
    /// </summary>
    public static class ColorWheelGenerator
    {
        private static readonly double[] TraditionalDisplayHues =
            { 0.0, 30.0, 60.0, 120.0, 240.0, 285.0, 360.0 };

        /// <summary>
        /// Creates a colour wheel from every paint the user can select.
        /// </summary>
        /// <param name="diameter">The width and height of the square bitmap, in pixels.</param>
        /// <returns>A 32-bit ARGB bitmap containing the colour wheel.</returns>
        public static Bitmap Create(int diameter)
        {
            return Create(diameter, PigmentLibrary.Selectable);
        }

        /// <summary>
        /// Creates a colour wheel from the given paints, evenly spaced clockwise from
        /// the three o'clock position in list order. Pixels outside the wheel are
        /// transparent.
        /// </summary>
        /// <param name="diameter">The width and height of the square bitmap, in pixels.</param>
        /// <param name="paints">The paints to distribute around the wheel.</param>
        /// <returns>A 32-bit ARGB bitmap containing the colour wheel, or a fully
        /// transparent bitmap when <paramref name="paints"/> is empty.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the diameter is
        /// under 2 pixels.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paints"/> is null.</exception>
        public static Bitmap Create(int diameter, IReadOnlyList<PigmentCoefficients> paints)
        {
            if (diameter < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(diameter), "Diameter must be at least 2 pixels.");
            }
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }

            var bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);

            // With every paint deselected there are no wedges to draw; a new ARGB bitmap
            // is already fully transparent, so return it as the empty wheel.
            if (paints.Count == 0)
            {
                return bitmap;
            }

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, diameter, diameter),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int stride = data.Stride;
                byte[] buffer = new byte[stride * diameter];

                // Every pixel gives every paint the same centre share, so that part of
                // the mix is identical across the whole wheel and is summed once here
                // instead of once per pixel.
                var baselineAbsorption = new double[SpectralBands.Count];
                var baselineScattering = new double[SpectralBands.Count];
                KubelkaMunk.SumCoefficients(paints, baselineAbsorption, baselineScattering);

                // Each scanline is independent, and a mixture costs a Kubelka-Munk
                // inversion plus three spectral integrations, so a full wheel is a few
                // hundred million operations. Splitting by row keeps that off the UI
                // thread's critical path without approximating anything: every pixel is
                // still an exact evaluation of the kernel. Workers share the buffer but
                // each owns its own rows, so their writes never overlap.
                Parallel.For(
                    0,
                    diameter,
                    () => new RowScratch(paints.Count),
                    (y, state, scratch) =>
                    {
                        for (int x = 0; x < diameter; x++)
                        {
                            if (!BlendGeometry.TryGetWedge(
                                diameter, paints.Count, x, y,
                                out BlendGeometry.Wedge wedge, out double alpha))
                            {
                                continue;
                            }

                            KubelkaMunk.MixWedge(
                                baselineAbsorption,
                                baselineScattering,
                                wedge.CentreShare,
                                paints[wedge.LowerPaint],
                                wedge.LowerSurplus,
                                paints[wedge.UpperPaint],
                                wedge.UpperSurplus,
                                scratch.Reflectance);
                            Color colour = SpectralRenderer.ToDisplayColor(scratch.Reflectance, out _);

                            // Pixel layout for Format32bppArgb is B, G, R, A in memory.
                            int offset = (y * stride) + (x * 4);
                            buffer[offset] = colour.B;
                            buffer[offset + 1] = colour.G;
                            buffer[offset + 2] = colour.R;
                            buffer[offset + 3] = (byte)(alpha * 255.0);
                        }

                        return scratch;
                    },
                    scratch => { });

                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        /// <summary>
        /// Creates a traditional artist's RYB colour wheel. Red, yellow, and blue
        /// occupy equally spaced primary positions; orange, green, and violet sit
        /// between them. Saturation increases from white at the centre to full colour
        /// at the rim, and pixels outside the wheel are transparent.
        /// </summary>
        /// <param name="diameter">The width and height of the square bitmap, in pixels.</param>
        /// <returns>A 32-bit ARGB bitmap containing the traditional colour wheel.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the diameter is
        /// under 2 pixels.</exception>
        public static Bitmap CreateTraditional(int diameter)
        {
            if (diameter < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(diameter), "Diameter must be at least 2 pixels.");
            }

            var bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, diameter, diameter),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int stride = data.Stride;
                var buffer = new byte[stride * diameter];
                double centre = (diameter - 1) * 0.5;
                double radius = diameter * 0.5;

                Parallel.For(0, diameter, y =>
                {
                    double dy = y - centre;
                    for (int x = 0; x < diameter; x++)
                    {
                        double dx = x - centre;
                        double distance = Math.Sqrt((dx * dx) + (dy * dy));
                        double coverage = Math.Clamp(radius + 0.5 - distance, 0.0, 1.0);
                        if (coverage <= 0.0)
                        {
                            continue;
                        }

                        // Zero degrees is red at twelve o'clock and angles advance
                        // clockwise, matching the familiar artist-wheel layout.
                        double artistHue = Math.Atan2(dx, -dy) * (180.0 / Math.PI);
                        if (artistHue < 0.0)
                        {
                            artistHue += 360.0;
                        }

                        double displayHue = TraditionalToDisplayHue(artistHue);
                        double saturation = Math.Min(1.0, distance / radius);
                        Color colour = HsvToColor(displayHue, saturation);

                        int offset = (y * stride) + (x * 4);
                        buffer[offset] = colour.B;
                        buffer[offset + 1] = colour.G;
                        buffer[offset + 2] = colour.R;
                        buffer[offset + 3] = (byte)Math.Round(coverage * 255.0);
                    }
                });

                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        private static double TraditionalToDisplayHue(double artistHue)
        {
            // RYB landmarks mapped onto display-space HSV:
            // red, orange, yellow, green, blue, violet, red.
            int segment = Math.Min(5, (int)(artistHue / 60.0));
            double fraction = (artistHue - (segment * 60.0)) / 60.0;
            return TraditionalDisplayHues[segment] +
                ((TraditionalDisplayHues[segment + 1] - TraditionalDisplayHues[segment]) * fraction);
        }

        private static Color HsvToColor(double hue, double saturation)
        {
            double chroma = saturation;
            double sector = hue / 60.0;
            double second = chroma * (1.0 - Math.Abs((sector % 2.0) - 1.0));
            double red;
            double green;
            double blue;

            if (sector < 1.0)
            {
                (red, green, blue) = (chroma, second, 0.0);
            }
            else if (sector < 2.0)
            {
                (red, green, blue) = (second, chroma, 0.0);
            }
            else if (sector < 3.0)
            {
                (red, green, blue) = (0.0, chroma, second);
            }
            else if (sector < 4.0)
            {
                (red, green, blue) = (0.0, second, chroma);
            }
            else if (sector < 5.0)
            {
                (red, green, blue) = (second, 0.0, chroma);
            }
            else
            {
                (red, green, blue) = (chroma, 0.0, second);
            }

            double minimum = 1.0 - chroma;
            return Color.FromArgb(
                (int)Math.Round((red + minimum) * 255.0),
                (int)Math.Round((green + minimum) * 255.0),
                (int)Math.Round((blue + minimum) * 255.0));
        }

        /// <summary>
        /// Computes each paint's share of the mixture at a given pixel of a wheel
        /// produced by <see cref="Create(int, IReadOnlyList{PigmentCoefficients})"/>,
        /// using the same geometry: the two paints flanking the pixel's angle contribute
        /// in proportion to its distance from the center (the rim share), and the
        /// remaining center share is split equally among all the paints.
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

            var weights = new double[paintCount];
            return BlendGeometry.TryGetWeights(diameter, paintCount, x, y, weights, out _)
                ? weights
                : null;
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

        /// <summary>
        /// Per-thread working buffers, so the parallel fill allocates once per worker
        /// rather than once per pixel.
        /// </summary>
        private sealed class RowScratch
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RowScratch"/> class.
            /// </summary>
            /// <param name="paintCount">How many paints the wheel is built from.</param>
            public RowScratch(int paintCount)
            {
                Reflectance = new double[SpectralBands.Count];
            }

            /// <summary>Gets the buffer the kernel writes a spectrum into.</summary>
            public double[] Reflectance { get; }
        }
    }
}
