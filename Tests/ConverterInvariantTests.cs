using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the converter's central promise: every pixel it emits is a colour the
    /// selected paints can genuinely be mixed to. Nothing tested this before, and it
    /// is the one property the whole application rests on — a violation looks like a
    /// slightly wrong picture, never like an exception.
    /// </summary>
    public class ConverterInvariantTests
    {
        /// <summary>
        /// Every converted pixel must appear in the sampled achievable gamut. Alpha is
        /// carried through from the source rather than from the candidate, so the
        /// comparison is on the colour channels only.
        /// </summary>
        [Fact]
        public void EveryConvertedPixelIsAColourThePaintsCanMix()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            var achievable = new HashSet<int>();
            foreach (int argb in PalettePhotoConverter.SampleAchievableColors(paints))
            {
                achievable.Add(argb & 0x00FFFFFF);
            }

            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(64, 64);
            using Bitmap converted = PalettePhotoConverter.Convert(source, paints, 0);

            for (int y = 0; y < converted.Height; y++)
            {
                for (int x = 0; x < converted.Width; x++)
                {
                    int pixel = converted.GetPixel(x, y).ToArgb() & 0x00FFFFFF;
                    Assert.Contains(pixel, achievable);
                }
            }
        }

        /// <summary>
        /// The same must hold when the pre-map filter has run, because smoothing changes
        /// which colours are asked for but not which are reachable.
        /// </summary>
        [Fact]
        public void TheInvariantSurvivesPreMapSmoothing()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            var achievable = new HashSet<int>();
            foreach (int argb in PalettePhotoConverter.SampleAchievableColors(paints))
            {
                achievable.Add(argb & 0x00FFFFFF);
            }

            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(64, 64);
            using Bitmap converted = PalettePhotoConverter.Convert(source, paints, 4);

            for (int y = 0; y < converted.Height; y++)
            {
                for (int x = 0; x < converted.Width; x++)
                {
                    Assert.Contains(converted.GetPixel(x, y).ToArgb() & 0x00FFFFFF, achievable);
                }
            }
        }

    }
}
