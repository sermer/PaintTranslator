using System;
using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Imaging;
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
            using Bitmap source = StyleTestFixtures.BuildNoisyGradient(256, 256, 0.0);
            using Bitmap converted = PalettePhotoConverter.Convert(source, StyleTestFixtures.SixPaints(), 0);

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
            using Bitmap source = StyleTestFixtures.BuildNoisyGradient(256, 256, 0.0);
            using Bitmap converted = PalettePhotoConverter.Convert(source, StyleTestFixtures.SixPaints(), 0);

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
            using Bitmap source = StyleTestFixtures.BuildNoisyGradient(256, 256, 3.0);
            using Bitmap converted = PalettePhotoConverter.Convert(source, StyleTestFixtures.SixPaints(), 0);

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
            using Bitmap source = StyleTestFixtures.BuildNoisyGradient(256, 256, 3.0);
            using Bitmap fine = PalettePhotoConverter.Convert(source, StyleTestFixtures.SixPaints(), 0, 2);
            using Bitmap coarse = PalettePhotoConverter.Convert(source, StyleTestFixtures.SixPaints(), 0, 12);

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

        private static double FractionInSmallRegions(Bitmap bitmap, int markSquared)
        {
            int[] pixels = StyleTestFixtures.ReadPixels(bitmap, out int stride);

            return PaintabilityMetrics.FractionInRegionsSmallerThan(
                pixels, stride, bitmap.Width, bitmap.Height,
                Math.Max(RenderContext.DefaultMarkPixels(bitmap.Width, bitmap.Height), 2)
                    * Math.Max(RenderContext.DefaultMarkPixels(bitmap.Width, bitmap.Height), 2));
        }

        private static int CountRegions(Bitmap bitmap)
        {
            int[] pixels = StyleTestFixtures.ReadPixels(bitmap, out int stride);

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
            int[] pixels = StyleTestFixtures.ReadPixels(bitmap, out int stride);
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
    }
}
