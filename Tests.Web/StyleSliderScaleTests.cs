using PaintTranslator.Imaging.Styles;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class StyleSliderScaleTests
{
    private static readonly StyleParameter Strength = new("strength", "Strength", 1, 5, 3, "");
    private static readonly StyleParameter Edge = new("edge", "Edge", 0.01, 0.30, 0.08, "L*");

    [Fact]
    public void EndpointsMapToZeroAndSteps()
    {
        Assert.Equal(0, StyleSliderScale.ToPosition(Strength, 1));
        Assert.Equal(StyleSliderScale.Steps, StyleSliderScale.ToPosition(Strength, 5));
        Assert.Equal(1, StyleSliderScale.ToValue(Strength, 0));
        Assert.Equal(5, StyleSliderScale.ToValue(Strength, StyleSliderScale.Steps));
    }

    [Fact]
    public void PositionRoundTripsThroughValue()
    {
        for (int p = 0; p <= StyleSliderScale.Steps; p++)
        {
            Assert.Equal(p, StyleSliderScale.ToPosition(Edge, StyleSliderScale.ToValue(Edge, p)));
        }
    }

    [Fact]
    public void CaptionMatchesWinFormsFormat()
    {
        Assert.Equal("Edge: 0.08 L*", StyleSliderScale.Caption(Edge, 0.08));
        Assert.Equal("Strength: 3", StyleSliderScale.Caption(Strength, 3));
    }
}
