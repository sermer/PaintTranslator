using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins invariant I2 — every output region large enough for a brush to have made
    /// it — against the case that breaks it: a photograph with ordinary sensor noise.
    /// <para>
    /// Without a pre-map floor this same source converts to 92,326 regions with a
    /// median area of one pixel and 44.3% of pixels in regions of four or fewer. Every
    /// one of those pixels is a mixable colour, so invariant I1 catches none of it.
    /// </para>
    /// </summary>
    public class PaintabilityFloorTests
    {
        /// <summary>
        /// A noiseless source was already paintable, and adding the floor must not make
        /// it worse. This is the control.
        /// </summary>
        [Fact]
        public void ANoiselessSourceStaysPaintable()
        {
            using Bitmap source = BuildGradient(256, 256, 0.0);
            using Bitmap converted = PalettePhotoConverter.Convert(source, SixPaints(), 0);

            Assert.True(FractionInSmallRegions(converted, 256) < 0.02);
        }

        /// <summary>
        /// <see cref="ANoiselessSourceStaysPaintable"/> only bounds the small-region
        /// fraction from above, so a filter that flattened the whole picture to one
        /// colour would score 0% and pass it trivially. This bounds the other side:
        /// the mandatory floor must still leave real structure behind. The unit-level
        /// property that the filter preserves edges is already pinned in
        /// <c>GuidedFilterTests</c> — this is the integration-level guard that
        /// <see cref="PalettePhotoConverter.Convert"/> actually calls it that way.
        /// <para>
        /// Measured directly: at the default mark this image gets (radius 1), raising
        /// the iteration count alone barely moves these counts — 500 iterations only
        /// reaches 304 regions and 127 colours, because a radius-1 window diffuses a
        /// 256px image slowly. What this test does catch is the floor being called
        /// with a genuinely wrong window: a radius widened to 120px with edge
        /// preservation disabled collapses the same source to 37 regions and 18
        /// colours, well under the thresholds below.
        /// </para>
        /// </summary>
        [Fact]
        public void ANoiselessSourceKeepsPlentyOfStructure()
        {
            using Bitmap source = BuildGradient(256, 256, 0.0);
            using Bitmap converted = PalettePhotoConverter.Convert(source, SixPaints(), 0);

            int regions = CountRegions(converted);
            int distinctColours = CountDistinctColours(converted);

            // Measured on this exact source: 418 regions, 177 distinct colours. These
            // thresholds sit well under half of both, with headroom for legitimate
            // future changes while still catching a filter that over-flattens: a
            // deliberately broken call (radius widened to 120px with edge preservation
            // disabled) drove this same source down to 37 regions and 18 colours.
            Assert.True(regions > 100, $"only {regions} regions; the floor may have over-flattened the image");
            Assert.True(distinctColours > 50, $"only {distinctColours} distinct colours; the floor may have over-flattened the image");
        }

        /// <summary>
        /// The case the floor exists for. Sigma 3 is ordinary phone-photo noise.
        /// </summary>
        [Fact]
        public void ANoisySourceIsPaintableOnceTheFloorIsApplied()
        {
            using Bitmap source = BuildGradient(256, 256, 3.0);
            using Bitmap converted = PalettePhotoConverter.Convert(source, SixPaints(), 0);

            double fragmented = FractionInSmallRegions(converted, 256);

            Assert.True(fragmented < 0.05, $"{fragmented:P1} of pixels are in sub-mark regions; the floor is not holding");
        }

        /// <summary>
        /// A larger mark must produce larger regions. This is what makes the slider
        /// mean something rather than merely exist.
        /// </summary>
        [Fact]
        public void ALargerMarkProducesFewerRegions()
        {
            using Bitmap source = BuildGradient(256, 256, 3.0);
            using Bitmap fine = PalettePhotoConverter.Convert(source, SixPaints(), 0, 2);
            using Bitmap coarse = PalettePhotoConverter.Convert(source, SixPaints(), 0, 12);

            Assert.True(
                CountRegions(coarse) < CountRegions(fine),
                "a coarser mark did not produce fewer regions");
        }

        /// <summary>
        /// The floor radius has to grow with the mark but never reach zero, since a
        /// radius of zero is the unfiltered case the measurements condemned.
        /// </summary>
        [Fact]
        public void TheFloorRadiusIsAtLeastOneAndGrowsWithTheMark()
        {
            Assert.True(PalettePhotoConverter.FloorRadius(1.0) >= 1);
            Assert.True(PalettePhotoConverter.FloorRadius(2.0) >= 1);
            Assert.True(PalettePhotoConverter.FloorRadius(20.0) > PalettePhotoConverter.FloorRadius(4.0));
        }

        private static IReadOnlyList<PigmentCoefficients> SixPaints()
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
        /// A smooth bilinear field between four plausible photographic colours, with
        /// optional Gaussian noise. Neighbouring pixels differ by well under one 8-bit
        /// code before the noise is added, so anything fragmented in the output came
        /// from the conversion rather than the source.
        /// </summary>
        private static Bitmap BuildGradient(int width, int height, double sigma)
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

        private static double FractionInSmallRegions(Bitmap bitmap, int markSquared)
        {
            int[] pixels = ReadPixels(bitmap, out int stride);

            return PaintabilityMetrics.FractionInRegionsSmallerThan(
                pixels, stride, bitmap.Width, bitmap.Height,
                Math.Max(RenderContext.DefaultMarkPixels(bitmap.Width, bitmap.Height), 2)
                    * Math.Max(RenderContext.DefaultMarkPixels(bitmap.Width, bitmap.Height), 2));
        }

        private static int CountRegions(Bitmap bitmap)
        {
            int[] pixels = ReadPixels(bitmap, out int stride);

            return PaintabilityMetrics.CountRegions(pixels, stride, bitmap.Width, bitmap.Height);
        }

        /// <summary>
        /// Counts distinct colours, alpha masked off for the same reason
        /// <see cref="PaintabilityMetrics"/> masks it when comparing regions: alpha is
        /// constant across this test's bitmaps and carries no information about which
        /// mixture a pixel landed on.
        /// </summary>
        private static int CountDistinctColours(Bitmap bitmap)
        {
            int[] pixels = ReadPixels(bitmap, out int stride);
            var seen = new HashSet<int>();
            for (int y = 0; y < bitmap.Height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    seen.Add(pixels[row + x] & 0x00FFFFFF);
                }
            }

            return seen.Count;
        }

        private static int[] ReadPixels(Bitmap bitmap, out int strideInts)
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
