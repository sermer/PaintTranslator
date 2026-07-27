using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;
using Xunit.Abstractions;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the colour wheel now that it renders through the measured kernel. The wheel
    /// is how a user picks a mixture, so what matters is that a pixel's colour and the
    /// recipe reported for that pixel are the same mixture.
    /// </summary>
    public class ColorWheelGeneratorMeasuredTests
    {
        /// <summary>Writes generation timings so the performance claim is checkable.</summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorWheelGeneratorMeasuredTests"/> class.
        /// </summary>
        /// <param name="output">The xunit output sink.</param>
        public ColorWheelGeneratorMeasuredTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Confirms the pixel drawn at a point equals the colour of the recipe reported
        /// for that point. This is the property the old generator could not guarantee,
        /// because it computed the two with separate code.
        /// </summary>
        [Fact]
        public void EveryPixelMatchesTheRecipeReportedForIt()
        {
            const int Diameter = 129;
            IReadOnlyList<PigmentCoefficients> paints = PigmentLibrary.Selectable;
            var reflectance = new double[SpectralBands.Count];

            using Bitmap wheel = ColorWheelGenerator.Create(Diameter, paints);

            for (int y = 8; y < Diameter; y += 11)
            {
                for (int x = 8; x < Diameter; x += 11)
                {
                    double[] weights = ColorWheelGenerator.GetBlendWeights(Diameter, paints.Count, x, y);
                    if (weights == null)
                    {
                        continue;
                    }

                    KubelkaMunk.Mix(paints, weights, reflectance);
                    Color expected = SpectralRenderer.ToDisplayColor(reflectance, out _);
                    Color actual = wheel.GetPixel(x, y);

                    Assert.Equal(expected.R, actual.R);
                    Assert.Equal(expected.G, actual.G);
                    Assert.Equal(expected.B, actual.B);
                }
            }
        }

        /// <summary>
        /// Confirms a wheel of just a blue and a yellow contains green. The wheel is
        /// where the reported bug was seen, so this asserts the fix at the surface the
        /// user actually looked at, not only in the kernel underneath it.
        /// </summary>
        [Fact]
        public void AWheelOfBlueAndYellowContainsGreen()
        {
            const int Diameter = 129;
            var paints = new[]
            {
                PigmentLibrary.All.Single(p => p.Name == "Phthalo Blue (G.S.)"),
                PigmentLibrary.All.Single(p => p.Name == "Diarylide Yellow"),
            };

            using Bitmap wheel = ColorWheelGenerator.Create(Diameter, paints);

            int greenPixels = 0;
            for (int y = 0; y < Diameter; y++)
            {
                for (int x = 0; x < Diameter; x++)
                {
                    Color pixel = wheel.GetPixel(x, y);
                    if (pixel.A == 0)
                    {
                        continue;
                    }

                    PalettePhotoConverter.RgbToLab(pixel.R, pixel.G, pixel.B,
                        out _, out double aStar, out double bStar);

                    if (aStar < -10.0 && bStar > 10.0)
                    {
                        greenPixels++;
                    }
                }
            }

            Assert.True(greenPixels > 200, $"only {greenPixels} green pixels in a blue-and-yellow wheel");
        }

        /// <summary>
        /// Confirms an empty palette gives a transparent wheel rather than throwing,
        /// which is the state the application is in with every paint deselected.
        /// </summary>
        [Fact]
        public void AnEmptyPaletteGivesATransparentWheel()
        {
            using Bitmap wheel = ColorWheelGenerator.Create(64, new PigmentCoefficients[0]);

            Assert.Equal(0, wheel.GetPixel(32, 32).A);
        }

        /// <summary>
        /// Confirms a full-size wheel generates fast enough to sit behind a paint
        /// selection change without a visible freeze.
        /// </summary>
        [Fact]
        public void AFullSizeWheelGeneratesQuickly()
        {
            // Warm the JIT and the static library load before timing anything.
            using (Bitmap warmup = ColorWheelGenerator.Create(64, PigmentLibrary.Selectable))
            {
            }

            var stopwatch = Stopwatch.StartNew();
            using (Bitmap wheel = ColorWheelGenerator.Create(512, PigmentLibrary.Selectable))
            {
            }

            stopwatch.Stop();
            this.output.WriteLine($"512px wheel, 19 paints: {stopwatch.ElapsedMilliseconds} ms");

            Assert.True(
                stopwatch.ElapsedMilliseconds < 1000,
                $"wheel generation took {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
