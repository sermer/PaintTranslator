using System.Collections.Concurrent;
using System.Drawing;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>The CSS colour of a paint's mass tone, from the same physics the converter
/// uses, so the swatch beside a name is the colour the app will actually paint with.
/// Shared by the sidebar list and the palette editor so the two cannot show different
/// swatches for the same paint.</summary>
public static class PaintSwatch
{
    // Depends only on the pigment, never on which component asked, and concurrent because
    // xUnit renders components from several test classes in parallel.
    private static readonly ConcurrentDictionary<PigmentCoefficients, string> swatches = new();

    public static string Css(PigmentCoefficients paint) => swatches.GetOrAdd(paint, static p =>
    {
        var reflectance = new double[SpectralBands.Count];
        KubelkaMunk.Mix(new[] { p }, new[] { 1.0 }, reflectance);
        Color c = SpectralRenderer.ToDisplayColor(reflectance, out _);
        return $"rgb({c.R},{c.G},{c.B})";
    });
}
