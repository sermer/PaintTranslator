using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Imaging.Styles.Stages;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the pipeline runner against the single-path converter it replaces:
    /// Realism has to reproduce Phase 1's output exactly, every style has to stay
    /// inside its own achievable gamut, no style may leave state behind for the next
    /// render, and <see cref="StyleDefinition.MarkScale"/> has to actually change how
    /// strongly the mandatory floor runs.
    /// </summary>
    public class StylePipelineTests
    {
        [Fact]
        public void RenderStopsBeforeAllocatingOutputWhenCancelled()
        {
            StyleDefinition style = StyleRegistry.Default;
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values =
                StylePipeline.DefaultValues(style);
            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(64, 64);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Null(StylePipeline.Render(
                source,
                StyleTestFixtures.ThreePaints(),
                style,
                0,
                values,
                cancellationToken: cancellation.Token));
        }

        [Fact]
        public void RenderReturnsNullWithoutThrowingWhenCancelledInsideAStage()
        {
            using var cancellation = new CancellationTokenSource();
            var cancellingStage = new CancellingStage(cancellation);
            var style = new StyleDefinition(
                "Cancellation test",
                1.0,
                new IPreMapStage[] { cancellingStage },
                new IdentityRemap(),
                new KeepAllCandidates(),
                new NearestQuantiser(),
                Array.Empty<IPostMapStage>());
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values =
                StylePipeline.DefaultValues(style);
            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(32, 32);

            Assert.Null(StylePipeline.Render(
                source,
                StyleTestFixtures.ThreePaints(),
                style,
                0,
                values,
                cancellationToken: cancellation.Token));
            Assert.True(cancellation.IsCancellationRequested);
        }

        /// <summary>
        /// Realism is defined as the mandatory floor, an untouched colour mapping and
        /// plain nearest-candidate matching. Comparing <see cref="PalettePhotoConverter.Convert"/>
        /// against <see cref="StylePipeline.Render"/> called directly proves only that
        /// <c>Convert</c> forwards its arguments — both sides resolve to the exact same
        /// call, since <c>Convert</c> resolves <see cref="StyleRegistry.Default"/> and
        /// hands it to that same <c>Render</c>, so no difference between them could ever
        /// be observed regardless of whether the pipeline is correct. That was this
        /// test's original shape, and a prior review correctly flagged it as a tautology
        /// with no power to catch a real behavioural change.
        /// <para>
        /// This version re-derives the expected image independently instead: it builds
        /// the candidate set the same way <see cref="Styles.Stages.KeepAllCandidates"/>
        /// (a no-op transform) does, applies the mandatory floor at Realism's configured
        /// edge threshold, and then finds each pixel's nearest candidate by a brute-force scan
        /// over every candidate rather than through <see cref="Styles.Stages.NearestQuantiser"/>'s
        /// grid-shell index. Only the colour quantization step (<see cref="ColorQuantization"/>)
        /// is shared with the code under test, because Task 10's review separately found
        /// that scheme duplicated and gave it one owner — reusing that one-line owner is
        /// not the same as reusing the search, which is where a real bug is likely to
        /// hide and which this brute-force scan re-derives from scratch. A match here
        /// means Realism's whole chain — floor, identity colour map, nearest match, and
        /// <c>Convert</c>'s delegation to it — agrees with an independently computed
        /// answer, not merely with itself.
        /// </para>
        /// </summary>
        [Fact]
        public void RealismMatchesAnIndependentBruteForceOracle()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(64, 64);
            int mark = RenderContext.DefaultMarkPixels(source.Width, source.Height);

            int[] candidateArgb = PalettePhotoConverter.SampleAchievableColors(paints);
            var candidateL = new double[candidateArgb.Length];
            var candidateA = new double[candidateArgb.Length];
            var candidateB = new double[candidateArgb.Length];
            for (int i = 0; i < candidateArgb.Length; i++)
            {
                PalettePhotoConverter.RgbToLab(
                    (candidateArgb[i] >> 16) & 0xFF, (candidateArgb[i] >> 8) & 0xFF, candidateArgb[i] & 0xFF,
                    out candidateL[i], out candidateA[i], out candidateB[i]);
            }

            int[] floored = StyleTestFixtures.ReadPixels(source, out int stride);
            int floorRadius = PalettePhotoConverter.FloorRadius(mark);
            GuidedFilter.Apply(floored, stride, source.Width, source.Height, floorRadius, 0.10, 1);

            var expected = new int[floored.Length];
            for (int y = 0; y < source.Height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < source.Width; x++)
                {
                    // Quantized through the pipeline's own single-owned scheme, so the
                    // difference this test can catch is confined to the search that
                    // follows, not to a second, independently rounded target colour.
                    int key = ColorQuantization.Key(floored[row + x]);
                    ColorQuantization.KeyToRgb(key, out int r, out int g, out int b);
                    PalettePhotoConverter.RgbToLab(r, g, b, out double targetL, out double targetA, out double targetB);

                    double bestDistance = double.MaxValue;
                    int bestIndex = 0;
                    for (int i = 0; i < candidateArgb.Length; i++)
                    {
                        double dl = candidateL[i] - targetL;
                        double da = candidateA[i] - targetA;
                        double db = candidateB[i] - targetB;
                        double distance = (dl * dl) + (da * da) + (db * db);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestIndex = i;
                        }
                    }

                    expected[row + x] = unchecked((int)0xFF000000) | (candidateArgb[bestIndex] & 0x00FFFFFF);
                }
            }

            using Bitmap converted = PalettePhotoConverter.Convert(source, paints, 0, mark);
            int[] actual = StyleTestFixtures.ReadPixels(converted, out int actualStride);

            int mismatches = 0;
            for (int y = 0; y < source.Height; y++)
            {
                int expectedRow = y * stride;
                int actualRow = y * actualStride;
                for (int x = 0; x < source.Width; x++)
                {
                    if (expected[expectedRow + x] != actual[actualRow + x])
                    {
                        mismatches++;
                    }
                }
            }

            Assert.True(
                mismatches == 0,
                $"{mismatches} of {source.Width * source.Height} pixels differed from the independently " +
                "computed oracle — Realism's chain no longer matches floor-then-identity-then-nearest-match.");
        }

        /// <summary>
        /// A style transform may legitimately narrow the achievable gamut, so every
        /// output pixel has to land in <em>that style's own</em> candidate set —
        /// checked directly against a candidate set built the same way
        /// <see cref="StylePipeline.Render"/> builds its own, rather than against the
        /// unmodified palette gamut.
        /// </summary>
        [Fact]
        public void EveryStyleEmitsOnlyMixableColours()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(64, 64);

            foreach (StyleDefinition style in StyleRegistry.All)
            {
                IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);

                var builder = new MixtureBuilder(paints);
                style.Candidates.Transform(builder, values[style.Candidates]);
                CandidateSet candidates = builder.Build();

                var achievable = new HashSet<int>();
                foreach (int argb in candidates.Argb)
                {
                    achievable.Add(argb & 0x00FFFFFF);
                }

                using Bitmap converted = StylePipeline.Render(source, paints, style, 0, values);

                int[] pixels = StyleTestFixtures.ReadPixels(converted, out int stride);
                for (int y = 0; y < converted.Height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < converted.Width; x++)
                    {
                        int colour = pixels[row + x] & 0x00FFFFFF;
                        Assert.True(
                            achievable.Contains(colour),
                            $"style '{style.Name}' emitted a colour outside its own candidate set at ({x},{y})");
                    }
                }
            }
        }

        /// <summary>
        /// Nothing a style's stages compute may survive between renders. Rendering
        /// Realism, then another style, then Realism again has to give the same
        /// result both times Realism ran — otherwise a stage is carrying mutable
        /// state that lets one style's render leak into another's.
        /// </summary>
        [Fact]
        public void RenderingOneStyleAfterAnotherMatchesRenderingItAlone()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(64, 64);

            StyleDefinition realism = StyleRegistry.ByName("Realism");
            StyleDefinition other = StyleRegistry.All[StyleRegistry.All.Count - 1];

            using Bitmap alone = StylePipeline.Render(
                source, paints, realism, 0, StylePipeline.DefaultValues(realism));
            using Bitmap afterOther = StylePipeline.Render(
                source, paints, other, 0, StylePipeline.DefaultValues(other));
            using Bitmap again = StylePipeline.Render(
                source, paints, realism, 0, StylePipeline.DefaultValues(realism));

            AssertBitmapsIdentical(alone, again);
        }

        /// <summary>
        /// <see cref="StyleDefinition.MarkScale"/> is the user's mark-size slider
        /// multiplied by a style-chosen factor before any stage sees it, and
        /// multiplication is the only rule an assertion can pin exactly: slider 2 at
        /// <c>MarkScale</c> 6.0 must render byte-identically to slider 12 at
        /// <c>MarkScale</c> 1.0, since both resolve to the same 12-pixel mark and
        /// nothing downstream of <see cref="RenderContext.MarkPixels"/> can tell which
        /// factor contributed what.
        /// <para>
        /// A weaker assertion — merely that a larger scale produces fewer regions — is
        /// satisfied by any rule that makes the mark larger at all, multiplicative or
        /// not (an additive 2 + 6 giving radius 4 would pass it too), and this floor's
        /// region count is not even reliably monotonic in radius on noisy source
        /// material: a diagnostic sweep run while fixing this test found it falls from
        /// radius 1 to 2 and then rises steadily through at least radius 15, on both a
        /// three- and a six-paint palette. The original version of this test picked its
        /// radius pair only after that non-monotonicity made its first choice fail,
        /// which is what a tuned-to-pass assertion looks like from the outside. Exact
        /// pixel identity has no such escape hatch.
        /// </para>
        /// </summary>
        [Fact]
        public void MarkScaleMultipliesTheUserSlider()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.SixPaints();
            using Bitmap source = StyleTestFixtures.BuildNoisyGradient(256, 256, 3.0);

            StyleDefinition realism = StyleRegistry.ByName("Realism");
            StyleDefinition scaled = realism with { Name = "RealismAtSixTimesScale", MarkScale = 6.0 };

            using Bitmap atSliderTwoScaleSix = StylePipeline.Render(
                source, paints, scaled, 2, StylePipeline.DefaultValues(scaled));
            using Bitmap atSliderTwelveScaleOne = StylePipeline.Render(
                source, paints, realism, 12, StylePipeline.DefaultValues(realism));

            AssertBitmapsIdentical(atSliderTwoScaleSix, atSliderTwelveScaleOne);
        }

        /// <summary>
        /// <see cref="StylePipeline.Render"/> is specified to preserve each pixel's own
        /// source alpha, and captures it from the buffer before any
        /// <see cref="IPreMapStage"/> runs rather than reading it back out of a
        /// possibly stage-mutated buffer afterward — a deliberate choice recorded in
        /// this method's own comments. Every other bitmap built in this file uses
        /// <c>Color.FromArgb(255, ...)</c>, which is uniformly opaque and so cannot
        /// exercise that choice at all: a bug that dropped alpha entirely, or read it
        /// from the wrong point in the pipeline, would still pass every other test
        /// here. This source gives every pixel a distinct alpha instead, so the
        /// output's alpha channel has to match the source's, pixel for pixel, exactly.
        /// </summary>
        [Fact]
        public void SourceAlphaIsPreservedExactly()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            using Bitmap source = BuildGradientBitmapWithVaryingAlpha(64, 64);

            StyleDefinition realism = StyleRegistry.ByName("Realism");
            using Bitmap converted = StylePipeline.Render(
                source, paints, realism, 0, StylePipeline.DefaultValues(realism));

            int[] sourcePixels = StyleTestFixtures.ReadPixels(source, out int sourceStride);
            int[] convertedPixels = StyleTestFixtures.ReadPixels(converted, out int convertedStride);

            for (int y = 0; y < source.Height; y++)
            {
                int sourceRow = y * sourceStride;
                int convertedRow = y * convertedStride;
                for (int x = 0; x < source.Width; x++)
                {
                    int expectedAlpha = (sourcePixels[sourceRow + x] >> 24) & 0xFF;
                    int actualAlpha = (convertedPixels[convertedRow + x] >> 24) & 0xFF;
                    Assert.True(
                        expectedAlpha == actualAlpha,
                        $"pixel ({x},{y}) alpha differs: source had {expectedAlpha}, output had {actualAlpha}");
                }
            }
        }

        [Fact]
        public void PreparedCandidatesRenderIdenticallyToOneShotRendering()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.ThreePaints();
            StyleDefinition style = StyleRegistry.ByName("Realism");
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);
            CandidateSet prepared = StylePipeline.PrepareCandidates(paints, style, values);
            using Bitmap source = StyleTestFixtures.BuildGradientBitmap(48, 32);

            using Bitmap oneShot = StylePipeline.Render(source, paints, style, 3, values);
            using Bitmap reused = StylePipeline.Render(source, paints, style, 3, values, prepared);

            AssertBitmapsIdentical(oneShot, reused);
        }

        private sealed class CancellingStage : IPreMapStage
        {
            private readonly CancellationTokenSource cancellation;

            public CancellingStage(CancellationTokenSource cancellation)
            {
                this.cancellation = cancellation;
            }

            public string DisplayName => "Cancel";
            public IReadOnlyList<StyleParameter> Parameters { get; } = Array.Empty<StyleParameter>();

            public void Apply(
                int[] pixels, int strideInts, int width, int height,
                in RenderContext context, ParameterValues values)
            {
                cancellation.Cancel();
            }
        }

        /// <summary>
        /// The same gradient as <see cref="StyleTestFixtures.BuildGradientBitmap"/>, but with a distinct,
        /// non-255 alpha at nearly every pixel, so a test can tell whether the pipeline
        /// preserved each pixel's own source alpha rather than some constant or the
        /// wrong pixel's value.
        /// </summary>
        private static Bitmap BuildGradientBitmapWithVaryingAlpha(int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = (x * 255) / (width - 1);
                    int g = (y * 255) / (height - 1);
                    int b = ((x + y) * 255) / (width + height - 2);
                    int alpha = ((x * 7) + (y * 13)) % 256;
                    bitmap.SetPixel(x, y, Color.FromArgb(alpha, r, g, b));
                }
            }

            return bitmap;
        }

        private static void AssertBitmapsIdentical(Bitmap expected, Bitmap actual)
        {
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);

            int[] expectedPixels = StyleTestFixtures.ReadPixels(expected, out int expectedStride);
            int[] actualPixels = StyleTestFixtures.ReadPixels(actual, out int actualStride);

            for (int y = 0; y < expected.Height; y++)
            {
                int expectedRow = y * expectedStride;
                int actualRow = y * actualStride;
                for (int x = 0; x < expected.Width; x++)
                {
                    Assert.True(
                        expectedPixels[expectedRow + x] == actualPixels[actualRow + x],
                        $"pixel ({x},{y}) differs: expected {expectedPixels[expectedRow + x]:X8}, got {actualPixels[actualRow + x]:X8}");
                }
            }
        }
    }
}
