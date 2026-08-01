using System.Collections.Generic;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using Xunit;

namespace PaintTranslator.Tests
{
    public class ColourMapCacheTests
    {
        [Fact]
        public void PreMapChangesReuseAnswersButRemapChangesInvalidateThem()
        {
            StyleDefinition style = StyleRegistry.ByName("Tonalism");
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values =
                StylePipeline.DefaultValues(style);
            var candidates = new CandidateSet(
                new[] { unchecked((int)0xFF000000) },
                new[] { 0.0 },
                new[] { 0.0 },
                new[] { 0.0 });
            var context = new RenderContext(100, 80, 2.0, candidates.MaximumChroma);
            var cache = new ColourMapCache();

            int[] first = cache.GetOrCreate(candidates, style, values, in context);

            values[style.PreMap[0]].Set("edge", 0.11);
            int[] afterPreMapChange = cache.GetOrCreate(candidates, style, values, in context);

            values[style.Remap].Set("contrast", 0.70);
            int[] afterRemapChange = cache.GetOrCreate(candidates, style, values, in context);

            Assert.Same(first, afterPreMapChange);
            Assert.NotSame(first, afterRemapChange);
        }
    }
}
