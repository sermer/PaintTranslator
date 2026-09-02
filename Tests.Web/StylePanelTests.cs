using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Web.Components;

namespace PaintTranslator.Web.Tests;

public class StylePanelTests : BunitContext
{
    [Fact]
    public void RendersOneRangePerDeclaredParameterAndSkipsParameterlessStages()
    {
        foreach (StyleDefinition style in StyleRegistry.All)
        {
            var values = StylePipeline.DefaultValues(style);
            var cut = Render<StylePanel>(p => p.Add(x => x.Style, style).Add(x => x.Values, values));
            int expected = style.Stages.Sum(s => s.Parameters.Count);
            Assert.Equal(expected, cut.FindAll("input[type=range]").Count);
            int headings = style.Stages.Count(s => s.Parameters.Count > 0);
            Assert.Equal(headings, cut.FindAll("h3").Count);
        }
    }

    [Fact]
    public void CaptionShowsCurrentValueAndChangeReportsStageIdValue()
    {
        StyleDefinition style = StyleRegistry.Default;
        IPipelineStage stage = style.Stages.First(s => s.Parameters.Count > 0);
        StyleParameter parameter = stage.Parameters[0];
        (IPipelineStage, string, double)? reported = null;
        var cut = Render<StylePanel>(p => p
            .Add(x => x.Style, style)
            .Add(x => x.Values, StylePipeline.DefaultValues(style))
            .Add(x => x.OnChange, v => reported = v));

        cut.Find("input[type=range]").Input("100");

        Assert.NotNull(reported);
        Assert.Same(stage, reported!.Value.Item1);
        Assert.Equal(parameter.Id, reported.Value.Item2);
        Assert.Equal(parameter.Maximum, reported.Value.Item3, 6);
    }
}
