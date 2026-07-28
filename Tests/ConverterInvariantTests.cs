using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();
            var achievable = new HashSet<int>();
            foreach (int argb in PalettePhotoConverter.SampleAchievableColors(paints))
            {
                achievable.Add(argb & 0x00FFFFFF);
            }

            using Bitmap source = BuildGradientBitmap(64, 64);
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
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();
            var achievable = new HashSet<int>();
            foreach (int argb in PalettePhotoConverter.SampleAchievableColors(paints))
            {
                achievable.Add(argb & 0x00FFFFFF);
            }

            using Bitmap source = BuildGradientBitmap(64, 64);
            using Bitmap converted = PalettePhotoConverter.Convert(source, paints, 4);

            for (int y = 0; y < converted.Height; y++)
            {
                for (int x = 0; x < converted.Width; x++)
                {
                    Assert.Contains(converted.GetPixel(x, y).ToArgb() & 0x00FFFFFF, achievable);
                }
            }
        }

        /// <summary>
        /// Three paints spanning light, warm and dark, which is enough for the candidate
        /// set to have interior structure without making the test slow.
        /// </summary>
        private static IReadOnlyList<PigmentCoefficients> ThreePaints()
        {
            return new[]
            {
                PigmentLibrary.Selectable[0],   // Titanium White
                PigmentLibrary.Selectable[6],   // C.P. Cadmium Red Light
                PigmentLibrary.Selectable[11],  // Ultramarine Blue
            };
        }

        private static Bitmap BuildGradientBitmap(int width, int height)
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
    }
}
