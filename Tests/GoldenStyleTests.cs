using System;
using System.Collections.Generic;
using System.IO;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Renders one deterministic source through every registered style and checks
    /// the result against a committed reference image, so a shared stage that
    /// starts drifting one style's picture shows up as a failure on that style
    /// specifically rather than as a subtle shift nobody notices until a later
    /// bug report.
    /// <para>
    /// <b>A change detector, not a correctness proof</b> (the ruling from Task 10's
    /// review, restated here because this is the file it applies to most
    /// directly). Byte-identical output against yesterday's PNG says only that
    /// nothing changed; it says nothing about whether yesterday's PNG was right.
    /// The only thing that makes these five references more than an arbitrary
    /// pinned snapshot is that a human looked at all five once, at the moment
    /// they were generated, and confirmed each one actually reads as its style's
    /// name implies. That inspection is recorded in the Task 16 report rather
    /// than in this file, because a code comment cannot be re-verified the way a
    /// dated report entry can, but the claim is asserted here so a future reader
    /// knows to go find it rather than assume the PNGs are self-evidently
    /// correct.
    /// </para>
    /// <para>
    /// <b>The palette.</b> All five renders share
    /// <see cref="StyleTestFixtures.SixPaints"/> — Titanium White, Hansa Yellow
    /// Opaque, C.P. Cadmium Red Light, Quinacridone Magenta, Ultramarine Blue and
    /// Bone Black — the same set <c>StyleBehaviourTests</c> and
    /// <c>StylePipelineTests</c> already use, and for the same reason: it is the
    /// smallest palette in this project with both a true near-neutral (Bone
    /// Black, not merely a light colour) and wide hue coverage, so a
    /// chroma-lowering style has somewhere desaturated to land and a
    /// chroma-raising one has saturated hues to move toward. A muted palette —
    /// say, three earth tones — would leave Fauvism and Tonalism rendering
    /// pictures that differ only slightly, since neither style could reach a
    /// visual extreme the achievable gamut does not contain; the difference the
    /// five references exist to show would then be too small to see even though
    /// the underlying numbers (chroma gain 0.45 through 2.2 across the five
    /// styles) are unchanged. See <see cref="StyleTestFixtures.SixPaints"/> for
    /// the fuller comparison against the three-paint palette this project tried
    /// first.
    /// </para>
    /// </summary>
    public class GoldenStyleTests
    {
        /// <summary>
        /// Regenerates the five committed references from current output instead
        /// of comparing against them, when set to <c>true</c>. Regenerating and
        /// never looking at the result pins whatever the pipeline currently does,
        /// bug included, as the new "correct" answer forever after — so the only
        /// safe procedure is: set this to <c>true</c>, run
        /// <see cref="StyleMatchesItsCommittedGolden"/> once for every style, open
        /// all five PNGs under <c>Tests/Golden</c> and look at them, and only then
        /// set this back to <c>false</c> before committing.
        /// </summary>
        private const bool Regenerate = false;

        /// <summary>
        /// One brushmark's width in pixels for every style's render. A small,
        /// fixed value rather than each style's own default mark, so every style
        /// is compared at a size the others also use — the goldens exist to show
        /// how the styles differ from each other, and a differing mark size would
        /// be one more variable folded into that difference instead of held
        /// constant against it.
        /// </summary>
        private const int MarkPixels = 4;

        /// <summary>
        /// The directory the five committed references live in, resolved next to
        /// the running test assembly rather than the source tree, because the
        /// PNGs are copied there by <c>CopyToOutputDirectory</c> and a clean
        /// checkout has no <c>Golden</c> folder anywhere else at test time.
        /// </summary>
        private static readonly string GoldenDirectory = Path.Combine(AppContext.BaseDirectory, "Golden");

        /// <summary>
        /// Supplies one theory case per style currently in <see cref="StyleRegistry.All"/>,
        /// so a failure report names the one style that regressed instead of
        /// reporting "some style in a loop failed" the way a single <c>[Fact]</c>
        /// iterating the registry would.
        /// </summary>
        /// <returns>One single-element array per registered style, holding that
        /// style's <see cref="StyleDefinition.Name"/>.</returns>
        public static IEnumerable<object[]> RegisteredStyleNames()
        {
            foreach (StyleDefinition style in StyleRegistry.All)
            {
                yield return new object[] { style.Name };
            }
        }

        /// <summary>
        /// Renders the fixed source through one registered style and compares the
        /// result byte-for-byte against <c>Tests/Golden/{styleName}.png</c>.
        /// </summary>
        /// <param name="styleName">The <see cref="StyleDefinition.Name"/> of the
        /// style to render and check, supplied by <see cref="RegisteredStyleNames"/>.</param>
        [Theory]
        [MemberData(nameof(RegisteredStyleNames))]
        public void StyleMatchesItsCommittedGolden(string styleName)
        {
            StyleDefinition style = StyleRegistry.ByName(styleName);
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.SixPaints();
            PixelImage source = StyleTestFixtures.BuildNoisyGradient(128, 128, 2.0);
            PixelImage converted = StylePipeline.Render(
                source, paints, style, MarkPixels, StylePipeline.DefaultValues(style));
            string path = Path.Combine(GoldenDirectory, style.Name + ".png");

            // Regenerate is a const, so with it committed as false the compiler can prove this
            // branch dead and would otherwise warn CS0162 on every build — suppressed here rather
            // than restructured, because the whole point of a const switch is that flipping one
            // literal revives this branch for a local, one-off regeneration run.
#pragma warning disable CS0162
            if (Regenerate)
            {
                PngCodec.Save(converted, path);
                return;
            }
#pragma warning restore CS0162

            Assert.True(
                File.Exists(path),
                $"no golden reference at {path} — set Regenerate = true, run this test once for " +
                "every style, look at the five PNGs it writes, then set Regenerate back to false");

            PixelImage golden = PngCodec.Load(path);
            AssertPixelsIdentical(golden, converted, style.Name);
        }

        /// <summary>
        /// For each style, renders it alone and renders it again after every
        /// other registered style has rendered in between, and requires the two
        /// results to be byte-identical. This is the isolation claim made
        /// checkable: the only way a shared stage could still couple two styles,
        /// despite never being told which style invoked it, is by keeping state
        /// across calls — and that is exactly what re-rendering the same style
        /// with three unrelated renders in between would expose.
        /// <para>
        /// <see cref="StylePipelineTests.RenderingOneStyleAfterAnotherMatchesRenderingItAlone"/>
        /// already checks this for one style pair (Realism, then the registry's
        /// last style, then Realism again). This test is the full version: every
        /// style gets to be the one compared, and every other style runs between
        /// its two renders, so a stage that only misbehaves after a particular
        /// style — not just "some other style" — has nowhere left to hide.
        /// </para>
        /// </summary>
        [Fact]
        public void RenderingEveryStyleInSequenceMatchesRenderingEachAlone()
        {
            IReadOnlyList<PigmentCoefficients> paints = StyleTestFixtures.SixPaints();
            PixelImage source = StyleTestFixtures.BuildNoisyGradient(128, 128, 2.0);
            IReadOnlyList<StyleDefinition> styles = StyleRegistry.All;

            foreach (StyleDefinition style in styles)
            {
                PixelImage alone = StylePipeline.Render(
                    source, paints, style, MarkPixels, StylePipeline.DefaultValues(style));

                foreach (StyleDefinition other in styles)
                {
                    if (!ReferenceEquals(other, style))
                    {
                        PixelImage discard = StylePipeline.Render(
                            source, paints, other, MarkPixels, StylePipeline.DefaultValues(other));
                    }
                }

                PixelImage again = StylePipeline.Render(
                    source, paints, style, MarkPixels, StylePipeline.DefaultValues(style));

                AssertPixelsIdentical(alone, again, style.Name);
            }
        }

        /// <summary>
        /// Compares two images pixel for pixel and fails on the first mismatch
        /// found while counting the rest, so a failure message names both where
        /// the images first diverged and how widespread the divergence is.
        /// </summary>
        /// <param name="expected">The reference image.</param>
        /// <param name="actual">The freshly rendered image being checked.</param>
        /// <param name="context">The style name, included in the failure message
        /// so a run across every style in a loop still identifies which one
        /// failed.</param>
        private static void AssertPixelsIdentical(PixelImage expected, PixelImage actual, string context)
        {
            Assert.True(
                expected.Width == actual.Width && expected.Height == actual.Height,
                $"'{context}': size differs — expected {expected.Width}x{expected.Height}, " +
                $"got {actual.Width}x{actual.Height}");

            int[] expectedPixels = StyleTestFixtures.ReadPixels(expected, out int expectedStride);
            int[] actualPixels = StyleTestFixtures.ReadPixels(actual, out int actualStride);

            int mismatches = 0;
            int firstX = -1;
            int firstY = -1;
            for (int y = 0; y < expected.Height; y++)
            {
                int expectedRow = y * expectedStride;
                int actualRow = y * actualStride;
                for (int x = 0; x < expected.Width; x++)
                {
                    if (expectedPixels[expectedRow + x] != actualPixels[actualRow + x])
                    {
                        if (mismatches == 0)
                        {
                            firstX = x;
                            firstY = y;
                        }

                        mismatches++;
                    }
                }
            }

            Assert.True(
                mismatches == 0,
                $"'{context}': {mismatches} of {expected.Width * expected.Height} pixels differed from " +
                $"the reference, first at ({firstX},{firstY})");
        }
    }
}
