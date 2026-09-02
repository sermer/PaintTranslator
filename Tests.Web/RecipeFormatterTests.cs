using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class RecipeFormatterTests
{
    private static readonly IReadOnlyList<PigmentCoefficients> Paints = PigmentLibrary.Selectable.Take(6).ToList();

    [Fact]
    public void ClosestMixListsPaintsLargestShareFirstThenMatchLine()
    {
        var match = new PaintBlendMatcher.BlendMatch(
            Color.FromArgb(120, 80, 60), paintIndices: new[] { 0, 1 }, weights: new[] { 0.25, 0.75 });
        string[] lines = RecipeFormatter.ClosestMix(Color.FromArgb(120, 80, 60), Paints, match);
        Assert.Equal("RGB: 120, 80, 60", lines[0]);
        Assert.Equal("Closest mix:", lines[1]);
        Assert.Equal($"75% {Paints[1].Name}", lines[2]);
        Assert.Equal($"25% {Paints[0].Name}", lines[3]);
        Assert.StartsWith("Match: ", lines[4]);
        Assert.Contains("(dE 0.0)", lines[4]);
        Assert.Equal(5, lines.Length); // identical colours: no shift, no gamut, no rounding line
    }

    [Fact]
    public void ClosestMixAddsGamutAndRoundingLinesOnlyPastThresholds()
    {
        var match = new PaintBlendMatcher.BlendMatch(
            Color.FromArgb(120, 80, 60), new[] { 0 }, new[] { 1.0 },
            exactDistance: 1.0, snappedDistance: 1.6, chromaLost: 0.01);
        string[] lines = RecipeFormatter.ClosestMix(Color.FromArgb(120, 80, 60), Paints, match);
        Assert.Contains("More vivid than this screen can show", lines);
        Assert.Contains("Rounded to whole percent: 1.0 → 1.6", lines);

        var quiet = new PaintBlendMatcher.BlendMatch(
            Color.FromArgb(120, 80, 60), new[] { 0 }, new[] { 1.0 },
            exactDistance: 1.0, snappedDistance: 1.4, chromaLost: 0.0005);
        string[] quietLines = RecipeFormatter.ClosestMix(Color.FromArgb(120, 80, 60), Paints, quiet);
        Assert.DoesNotContain("More vivid than this screen can show", quietLines);
        Assert.DoesNotContain(quietLines, l => l.StartsWith("Rounded"));
    }

    [Fact]
    public void ClosestMixReportsShiftWhenMixDiffersVisibly()
    {
        var match = new PaintBlendMatcher.BlendMatch(Color.FromArgb(200, 200, 200), new[] { 0 }, new[] { 1.0 });
        string[] lines = RecipeFormatter.ClosestMix(Color.FromArgb(60, 60, 60), Paints, match);
        Assert.Contains(lines, l => l.StartsWith("Mix reads "));
    }

    [Fact]
    public void WheelBlendNamesAtMostFivePaintsAndRollsUpTheRest()
    {
        double[] weights = { 0.30, 0.25, 0.20, 0.10, 0.08, 0.04, 0.03 };
        IReadOnlyList<PigmentCoefficients> seven = PigmentLibrary.Selectable.Take(7).ToList();
        string[] lines = RecipeFormatter.WheelBlend(Color.FromArgb(1, 2, 3), seven, weights);
        Assert.Equal("RGB: 1, 2, 3", lines[0]);
        Assert.Equal($"{seven[0].Name}: 30%", lines[1]);
        Assert.Equal($"{seven[4].Name}: 8%", lines[5]);
        Assert.Equal("+2 more: 7%", lines[6]);
        Assert.Equal(7, lines.Length);
    }

    [Fact]
    public void WheelBlendSkipsSharesBelowHalfAPercentFromTheNamedList()
    {
        double[] weights = { 0.996, 0.004 };
        IReadOnlyList<PigmentCoefficients> two = PigmentLibrary.Selectable.Take(2).ToList();
        string[] lines = RecipeFormatter.WheelBlend(Color.Black, two, weights);
        Assert.Equal(2, lines.Length); // RGB + one named paint; 0.4% remainder is below the visible share
    }
}
