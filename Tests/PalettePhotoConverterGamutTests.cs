using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Covers how closely the photo converter's sampled gamut tracks the colours the
    /// paints can really be mixed to, and that indexing that gamut for speed did not
    /// quietly change which colour a pixel lands on.
    /// </summary>
    public class PalettePhotoConverterGamutTests
    {
        // A white, a yellow, a red, a blue and a black. Wide enough that the mixing
        // triangles between them span most of the achievable gamut, small enough that
        // building the candidate set several times in one test stays quick.
        private static readonly IReadOnlyList<PigmentCoefficients> Paints = new[]
        {
            Paint("Titanium White"),
            Paint("Diarylide Yellow"),
            Paint("C.P. Cadmium Red Light"),
            Paint("Ultramarine Blue"),
            Paint("Bone Black"),
        };

        /// <summary>
        /// Looks a paint up by name.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }

        /// <summary>
        /// Confirms the grid-indexed search returns a colour exactly as close as an
        /// exhaustive scan of the same candidates would have.
        /// <para>
        /// The index exists purely to stop the denser sampling slowing a conversion down,
        /// so it has to be invisible in the result. A shell walk that stops one shell too
        /// early still returns a plausible colour, which is what makes this worth an
        /// explicit test rather than trusting it to look right. Distances are compared
        /// rather than identities because two candidates can sit equally close and either
        /// answer is then correct.
        /// </para>
        /// </summary>
        [Fact]
        public void IndexedSearchFindsExactlyWhatAnExhaustiveScanWould()
        {
            int[] achievable = PalettePhotoConverter.SampleAchievableColors(Paints);
            var pool = new (double L, double A, double B)[achievable.Length];
            for (int i = 0; i < achievable.Length; i++)
            {
                PalettePhotoConverter.RgbToLab(
                    (achievable[i] >> 16) & 0xFF, (achievable[i] >> 8) & 0xFF, achievable[i] & 0xFF,
                    out pool[i].L, out pool[i].A, out pool[i].B);
            }

            var random = new Random(20260726);
            var targets = new int[500];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = unchecked((int)0xFF000000)
                    | (random.Next(256) << 16) | (random.Next(256) << 8) | random.Next(256);
            }

            int[] indexed = PalettePhotoConverter.MapThroughIndex(Paints, targets);

            for (int i = 0; i < targets.Length; i++)
            {
                // The search resolves a quantisation bin, not the exact colour, so the
                // comparison has to be made against the same bin centre it works from.
                int r = (((targets[i] >> 18) & 0x3F) << 2) + 2;
                int g = (((targets[i] >> 10) & 0x3F) << 2) + 2;
                int b = (((targets[i] >> 2) & 0x3F) << 2) + 2;
                PalettePhotoConverter.RgbToLab(r, g, b,
                    out double targetL, out double targetA, out double targetB);

                double exhaustive = double.MaxValue;
                foreach ((double l, double a, double bb) in pool)
                {
                    double dl = l - targetL;
                    double da = a - targetA;
                    double db = bb - targetB;
                    exhaustive = Math.Min(exhaustive, (dl * dl) + (da * da) + (db * db));
                }

                PalettePhotoConverter.RgbToLab(
                    (indexed[i] >> 16) & 0xFF, (indexed[i] >> 8) & 0xFF, indexed[i] & 0xFF,
                    out double gotL, out double gotA, out double gotB);
                double got = ((gotL - targetL) * (gotL - targetL))
                    + ((gotA - targetA) * (gotA - targetA))
                    + ((gotB - targetB) * (gotB - targetB));

                Assert.True(
                    got <= exhaustive + 1e-9,
                    $"the index returned a colour {Math.Sqrt(got):0.000} away when one " +
                    $"{Math.Sqrt(exhaustive):0.000} away existed");
            }
        }

        /// <summary>
        /// Confirms the sampled gamut is dense enough that a real mixture is always close
        /// to something in it.
        /// <para>
        /// The converter replaces each pixel with the nearest sampled mixture, so any
        /// colour the paints can actually produce but the sampling skipped is an error no
        /// later stage can recover from. Probes are drawn from genuine mixtures at
        /// arbitrary proportions, which is what makes the residual here a measure of
        /// sampling coarseness rather than of the palette's reach. On this palette the
        /// fixed-ratio sampling this replaced left a mean of 3.60 and a worst case of
        /// 20.70; continuous sampling brings those to 0.84 and 5.82. The bounds below sit
        /// above the measured figures by enough that ordinary drift will not trip them,
        /// and far below what the old sampling managed.
        /// </para>
        /// </summary>
        [Fact]
        public void SamplesTheAchievableGamutCloselyEnoughToLandOnRealMixtures()
        {
            int[] achievable = PalettePhotoConverter.SampleAchievableColors(Paints);
            var pool = new (double L, double A, double B)[achievable.Length];
            for (int i = 0; i < achievable.Length; i++)
            {
                PalettePhotoConverter.RgbToLab(
                    (achievable[i] >> 16) & 0xFF, (achievable[i] >> 8) & 0xFF, achievable[i] & 0xFF,
                    out pool[i].L, out pool[i].A, out pool[i].B);
            }

            var random = new Random(4242);
            var reflectance = new double[SpectralBands.Count];
            var subset = new PigmentCoefficients[3];
            var shares = new double[3];
            double total = 0.0;
            double worst = 0.0;
            const int Probes = 400;

            for (int probe = 0; probe < Probes; probe++)
            {
                for (int i = 0; i < 3; i++)
                {
                    subset[i] = Paints[random.Next(Paints.Count)];
                    shares[i] = random.NextDouble();
                }

                if (shares[0] + shares[1] + shares[2] <= 0.0)
                {
                    shares[0] = 1.0;
                }

                KubelkaMunk.Mix(subset, shares, reflectance);
                Color mixed = SpectralRenderer.ToDisplayColor(reflectance, out _);
                PalettePhotoConverter.RgbToLab(mixed.R, mixed.G, mixed.B,
                    out double targetL, out double targetA, out double targetB);

                double nearest = double.MaxValue;
                foreach ((double l, double a, double b) in pool)
                {
                    double dl = l - targetL;
                    double da = a - targetA;
                    double db = b - targetB;
                    nearest = Math.Min(nearest, (dl * dl) + (da * da) + (db * db));
                }

                nearest = Math.Sqrt(nearest);
                total += nearest;
                worst = Math.Max(worst, nearest);
            }

            double mean = total / Probes;

            Assert.True(mean < 1.2, $"mean distance to the sampled gamut was {mean:0.000}");
            Assert.True(worst < 8.0, $"worst distance to the sampled gamut was {worst:0.000}");
        }

        /// <summary>
        /// Confirms a one-paint palette collapses to that paint's own colour rather than
        /// falling over. A single paint has no mixing line and no mixing triangle, so it
        /// exercises the degenerate case the pair and triple sampling loops never enter.
        /// </summary>
        [Fact]
        public void HandlesAPaletteOfOnePaint()
        {
            IReadOnlyList<PigmentCoefficients> single = new[] { Paints[3] };

            int[] achievable = PalettePhotoConverter.SampleAchievableColors(single);

            Assert.Single(achievable);

            int[] mapped = PalettePhotoConverter.MapThroughIndex(
                single, new[] { Color.Red.ToArgb(), Color.White.ToArgb(), Color.Black.ToArgb() });

            Assert.All(mapped, argb => Assert.Equal(achievable[0], argb));
        }
    }
}
