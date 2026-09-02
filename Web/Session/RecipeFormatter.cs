using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The hover tooltip's text, ported from MainForm so the web tooltip says exactly
/// what the WinForms one did. Kept free of any UI type so the strings are tested.
/// </summary>
public static class RecipeFormatter
{
    public static string RgbLine(Color pixel) => $"RGB: {pixel.R}, {pixel.G}, {pixel.B}";

    public static string[] ClosestMix(
        Color pixel, IReadOnlyList<PigmentCoefficients> paints, PaintBlendMatcher.BlendMatch match)
    {
        var lines = new List<string> { RgbLine(pixel), "Closest mix:" };
        // Largest share first, so the paint the user reaches for first is listed first.
        var order = Enumerable.Range(0, match.PaintIndices.Count).ToList();
        order.Sort((first, second) => match.Percentages[second].CompareTo(match.Percentages[first]));
        foreach (int i in order)
        {
            lines.Add($"{match.Percentages[i]}% {paints[match.PaintIndices[i]].Name}");
        }
        PalettePhotoConverter.RgbToLab(pixel.R, pixel.G, pixel.B, out double targetL, out double targetA, out double targetB);
        PalettePhotoConverter.RgbToLab(match.MixedColor.R, match.MixedColor.G, match.MixedColor.B, out double mixL, out double mixA, out double mixB);
        double deltaE = ColorDifference.CieDe2000(targetL, targetA, targetB, mixL, mixA, mixB);
        lines.Add($"Match: {ColorDifference.DescribeQuality(deltaE)} (dE {deltaE:0.0})");
        string? shift = ColorDifference.DescribeShift(targetL, targetA, targetB, mixL, mixA, mixB);
        if (shift != null)
        {
            lines.Add($"Mix reads {shift}");
        }
        if (match.ChromaLost > 0.001)
        {
            lines.Add("More vivid than this screen can show");
        }
        // Weighted HyAB distances, what the matcher minimises; deliberately not labelled dE00.
        double roundingCost = match.SnappedDistance - match.ExactDistance;
        if (roundingCost > 0.5)
        {
            lines.Add($"Rounded to whole percent: {match.ExactDistance:0.0} → {match.SnappedDistance:0.0}");
        }
        return lines.ToArray();
    }

    public static string[] WheelBlend(Color pixel, IReadOnlyList<PigmentCoefficients> paints, double[] weights)
    {
        const int MaxNamedPaints = 5;
        // Shares below half a percent would display as 0%, so they only count toward the remainder.
        const double MinVisibleShare = 0.005;
        var order = Enumerable.Range(0, weights.Length).ToList();
        order.Sort((first, second) => weights[second].CompareTo(weights[first]));
        var lines = new List<string> { RgbLine(pixel) };
        int named = 0, others = 0;
        double othersShare = 0.0;
        foreach (int index in order)
        {
            if (named < MaxNamedPaints && weights[index] >= MinVisibleShare)
            {
                lines.Add($"{paints[index].Name}: {weights[index] * 100:0}%");
                named++;
            }
            else if (weights[index] > 0.0)
            {
                others++;
                othersShare += weights[index];
            }
        }
        if (others > 0 && othersShare >= MinVisibleShare)
        {
            lines.Add($"+{others} more: {othersShare * 100:0}%");
        }
        return lines.ToArray();
    }
}
