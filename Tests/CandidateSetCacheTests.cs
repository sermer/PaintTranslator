using System;
using System.Collections.Generic;
using System.Threading;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    public class CandidateSetCacheTests
    {
        [Fact]
        public void CancelledBuildDoesNotEnterTheCache()
        {
            StyleDefinition style = StyleRegistry.Default;
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values =
                StylePipeline.DefaultValues(style);
            IReadOnlyList<PigmentCoefficients> paints = TwoPaints();
            var cache = new CandidateSetCache();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Null(cache.GetOrCreate(paints, style, values, cancellation.Token));

            Assert.NotNull(cache.GetOrCreate(paints, style, values));
        }

        [Fact]
        public void ImageAwareOnlyParameterDoesNotRebuildTheSpectralGamut()
        {
            StyleDefinition style = StyleRegistry.ByName("Abstract");
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);
            IReadOnlyList<PigmentCoefficients> paints = TwoPaints();
            var cache = new CandidateSetCache();

            CandidateSet first = cache.GetOrCreate(paints, style, values);
            values[style.Candidates].Set("colourCount", 3.0);
            CandidateSet second = cache.GetOrCreate(paints, style, values);

            Assert.Same(first, second);
        }

        [Fact]
        public void BuildParameterChangeProducesANewSpectralGamut()
        {
            StyleDefinition style = StyleRegistry.ByName("Abstract");
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);
            IReadOnlyList<PigmentCoefficients> paints = TwoPaints();
            var cache = new CandidateSetCache();

            CandidateSet first = cache.GetOrCreate(paints, style, values);
            values[style.Candidates].Set("motherFraction", 0.3);
            CandidateSet second = cache.GetOrCreate(paints, style, values);

            Assert.NotSame(first, second);
        }

        [Fact]
        public void ReturningToARecentPaletteReusesItsCandidateSet()
        {
            StyleDefinition style = StyleRegistry.Default;
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);
            IReadOnlyList<PigmentCoefficients> firstPaints = TwoPaints();
            IReadOnlyList<PigmentCoefficients> secondPaints = new[]
            {
                PigmentLibrary.Selectable[2],
                PigmentLibrary.Selectable[9]
            };
            var cache = new CandidateSetCache();

            CandidateSet first = cache.GetOrCreate(firstPaints, style, values);
            cache.GetOrCreate(secondPaints, style, values);
            CandidateSet returned = cache.GetOrCreate(firstPaints, style, values);

            Assert.Same(first, returned);
        }

        private static IReadOnlyList<PigmentCoefficients> TwoPaints()
        {
            return new[] { PigmentLibrary.Selectable[0], PigmentLibrary.Selectable[11] };
        }
    }
}
