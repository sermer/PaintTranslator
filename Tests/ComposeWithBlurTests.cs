using System;
using System.Collections.Generic;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Imaging.Styles.Stages;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Covers <see cref="PalettePhotoConverter.ComposeWithBlur"/>, the helper Task 13
    /// extracted so <see cref="PalettePhotoConverter"/>'s own style-and-blur-radius
    /// overload of <c>Convert</c> and <c>MainForm</c>'s convert button compose a
    /// legacy <c>blurRadius</c> onto a style and its values dictionary identically,
    /// instead of each keeping its own copy of the composition to drift out of sync.
    /// <para>
    /// The property most worth pinning here is that composing must never mutate the
    /// caller's own values dictionary: <c>MainForm</c> keeps one dictionary per style
    /// for the lifetime of a session, reused across every conversion of that style. A
    /// helper that added the blur-stage entry in place would leave it there after a
    /// blurred conversion, so the next unblurred conversion of the same style would
    /// still silently run the old blur — exactly the accumulation bug this composition
    /// was extracted to avoid.
    /// </para>
    /// </summary>
    public class ComposeWithBlurTests
    {
        /// <summary>
        /// Builds a minimal, real style (not a mock) so the test exercises the actual
        /// stage types <see cref="StylePipeline"/> runs.
        /// </summary>
        /// <returns>A style with one pre-map stage ahead of where blur would append.</returns>
        private static StyleDefinition BuildTestStyle()
        {
            return new StyleDefinition(
                "ComposeWithBlurTestStyle",
                1.0,
                new IPreMapStage[] { new EdgePreservingFloor() },
                new IdentityRemap(),
                new KeepAllCandidates(),
                new NearestQuantiser(),
                Array.Empty<IPostMapStage>());
        }

        [Fact]
        public void ZeroRadiusReturnsTheSameStyleAndValuesInstances()
        {
            StyleDefinition style = BuildTestStyle();
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);

            (StyleDefinition resultStyle, IReadOnlyDictionary<IPipelineStage, ParameterValues> resultValues) =
                PalettePhotoConverter.ComposeWithBlur(style, values, 0);

            // Same instances, not merely equal ones: a caller with a zero blur radius
            // must be able to keep using its own dictionary reference afterward.
            Assert.Same(style, resultStyle);
            Assert.Same(values, resultValues);
        }

        [Fact]
        public void NegativeRadiusIsTreatedTheSameAsZero()
        {
            StyleDefinition style = BuildTestStyle();
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);

            (StyleDefinition resultStyle, IReadOnlyDictionary<IPipelineStage, ParameterValues> resultValues) =
                PalettePhotoConverter.ComposeWithBlur(style, values, -3);

            Assert.Same(style, resultStyle);
            Assert.Same(values, resultValues);
        }

        [Fact]
        public void PositiveRadiusAppendsOptionalBlurAfterExistingPreMapStages()
        {
            StyleDefinition style = BuildTestStyle();
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);

            (StyleDefinition resultStyle, _) = PalettePhotoConverter.ComposeWithBlur(style, values, 5);

            // Appended after the existing stage (here, the mandatory floor), never
            // before: OptionalBlur's own doc records why the two do not commute.
            Assert.Equal(style.PreMap.Count + 1, resultStyle.PreMap.Count);
            Assert.Same(style.PreMap[0], resultStyle.PreMap[0]);
            Assert.IsType<OptionalBlur>(resultStyle.PreMap[resultStyle.PreMap.Count - 1]);
        }

        [Fact]
        public void PositiveRadiusLeavesTheCallersOriginalValuesDictionaryUntouched()
        {
            StyleDefinition style = BuildTestStyle();
            var originalValues = new Dictionary<IPipelineStage, ParameterValues>(StylePipeline.DefaultValues(style));
            int originalCount = originalValues.Count;

            (StyleDefinition resultStyle, IReadOnlyDictionary<IPipelineStage, ParameterValues> resultValues) =
                PalettePhotoConverter.ComposeWithBlur(style, originalValues, 5);

            IPipelineStage blurStage = resultStyle.PreMap[resultStyle.PreMap.Count - 1];

            // The composed answer is a distinct dictionary carrying the new stage...
            Assert.NotSame(originalValues, resultValues);
            Assert.True(resultValues.ContainsKey(blurStage));

            // ...and the caller's own dictionary is exactly as it was before the call,
            // so reusing it for a later, unblurred conversion of the same style would
            // not still carry this blur stage's entry.
            Assert.Equal(originalCount, originalValues.Count);
            Assert.False(originalValues.ContainsKey(blurStage));
        }

        [Fact]
        public void PositiveRadiusIsWrittenIntoTheAppendedBlurStage()
        {
            StyleDefinition style = BuildTestStyle();
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);

            (StyleDefinition resultStyle, IReadOnlyDictionary<IPipelineStage, ParameterValues> resultValues) =
                PalettePhotoConverter.ComposeWithBlur(style, values, 7);

            IPipelineStage blurStage = resultStyle.PreMap[resultStyle.PreMap.Count - 1];
            Assert.Equal(7.0, resultValues[blurStage]["radius"]);
        }
    }
}
