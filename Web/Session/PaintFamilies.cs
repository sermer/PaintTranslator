using System.Collections.Concurrent;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>The hue families the paint lists are grouped under, in the order they appear:
/// once around the colour wheel from white to black, the same walk PigmentLibrary.All
/// takes, so the grouped list reads the way the ungrouped one was already sorted.</summary>
public enum PaintFamily
{
    Whites,
    Yellows,
    Oranges,
    Reds,
    Violets,
    Blues,
    Greens,
    Blacks,
}

/// <summary>Assigns each paint to a hue family from its physics rather than its name or
/// Colour Index. The Colour Index would file Quinacridone Red (PV19) under violets. The
/// CIELAB hue angle is measured on the mass tone where that is honest and on a tint where
/// it is not: a light paint's mass tone is the colour in the tube, and it orders the
/// oranges and reds the way their names do (Pyrrole Orange 49°, Cadmium Red Light 42°),
/// whereas a tint of either is the same hue; but below about L* 30 the mass tone's hue
/// collapses towards the pigment's undertone (Dioxazine Purple reads as black, Phthalo
/// Green B.S. as blue, Phthalo Blue R.S. as violet), so dark paints are judged by an
/// equal-parts tint in white instead. PaintFamiliesTests pins all nineteen selectable
/// paints, which is what keeps these thresholds honest if the library changes.</summary>
public static class PaintFamilies
{
    /// <summary>Mass tones lighter than this keep a trustworthy hue; darker ones are
    /// judged by their tint. Quinacridone Magenta (25.8) and Cobalt Blue (27.5) fall just
    /// below and file the same either way; Quinacridone Red (34.1) and Cerulean (33.8)
    /// fall just above and also file the same either way, so the cut has margin.</summary>
    private const double DarkMassToneLightness = 30.0;

    /// <summary>Equal parts paint and white. A lighter tint (one in ten) drifts hue by
    /// another 10–20° and pulls Ultramarine from blue towards Cobalt's hue.</summary>
    private const double TintShare = 0.5;

    /// <summary>Below this chroma the sample has no hue worth filing under: it is a white
    /// or a black. Titanium White's mass tone is under 1, Bone Black's tint under 5, and
    /// the least saturated chromatic sample (Cerulean's mass tone) is 42.</summary>
    private const double NeutralChroma = 8.0;

    /// <summary>Whites and blacks are told apart by the sample's lightness: Titanium White
    /// is 98, Bone Black's equal-parts tint is 58.</summary>
    private const double WhiteLightness = 90.0;

    // Family boundaries in degrees of CIELAB hue (0° at +a*, 90° at +b*, 0–360). Each sits
    // in the gap between the two selectable paints it separates, named here so a future
    // paint that lands near a boundary can be checked against its neighbours:
    //   yellow/orange   65: Diarylide Yellow 75, C.P. Cadmium Orange 57
    //   orange/red      45: Pyrrole Orange 49, C.P. Cadmium Red Light 42 (the narrowest gap)
    //   green/yellow   130: Phthalo Green (Y.S.) 166, Bismuth Vanadate Yellow 97
    //   blue/green     220: Cerulean Blue 268, Phthalo Green (B.S.) 181
    //   violet/blue    295: Dioxazine Purple 306, Ultramarine Blue 285
    //   red/violet     330: Quinacridone Magenta 343, Dioxazine Purple 306
    private const double YellowFrom = 65.0;
    private const double OrangeFrom = 45.0;
    private const double GreenFrom = 130.0;
    private const double BlueFrom = 220.0;
    private const double VioletFrom = 295.0;
    private const double RedFrom = 330.0;

    /// <summary>The ideal white every tint is made with: no absorption, unit scattering.
    /// A synthetic white rather than the library's Titanium White, so a paint's family
    /// does not change if the library's white is swapped or removed.</summary>
    private static readonly PigmentCoefficients IdealWhite = new(
        "Ideal White", "PW", PigmentProvenance.TwoConstantMeasured,
        new double[SpectralBands.Count], Enumerable.Repeat(1.0, SpectralBands.Count).ToArray());

    // Both depend only on the pigment; cached for the same reason PaintList caches swatches.
    private static readonly ConcurrentDictionary<PigmentCoefficients, PaintFamily> families = new();
    private static readonly ConcurrentDictionary<PigmentCoefficients, double> lightnesses = new();

    public static PaintFamily Of(PigmentCoefficients paint) => families.GetOrAdd(paint, static p =>
    {
        (double lightness, double chroma, double hue) = Lab(p, 1.0);
        if (lightness < DarkMassToneLightness)
        {
            (lightness, chroma, hue) = Lab(p, TintShare);
        }
        if (chroma < NeutralChroma)
        {
            return lightness >= WhiteLightness ? PaintFamily.Whites : PaintFamily.Blacks;
        }
        if (hue >= RedFrom || hue < OrangeFrom) return PaintFamily.Reds;
        if (hue >= VioletFrom) return PaintFamily.Violets;
        if (hue >= BlueFrom) return PaintFamily.Blues;
        if (hue >= GreenFrom) return PaintFamily.Greens;
        if (hue >= YellowFrom) return PaintFamily.Yellows;
        return PaintFamily.Oranges;
    });

    /// <summary>The paints that are present, grouped by family in wheel order and, inside
    /// a family, from the lightest mass tone to the darkest, so each section reads like a
    /// row of a colour chart. Families with no paints are omitted rather than shown empty.</summary>
    public static IReadOnlyList<(PaintFamily Family, IReadOnlyList<PigmentCoefficients> Paints)> Group(
        IEnumerable<PigmentCoefficients> paints)
    {
        return paints
            .GroupBy(Of)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, (IReadOnlyList<PigmentCoefficients>)g.OrderByDescending(MassToneLightness).ToList()))
            .ToList();
    }

    private static double MassToneLightness(PigmentCoefficients paint) =>
        lightnesses.GetOrAdd(paint, static p => Lab(p, 1.0).Lightness);

    /// <summary>CIELAB lightness, chroma and hue (degrees, 0–360) of the paint at the given
    /// share in ideal white; a share of 1 is the mass tone.</summary>
    private static (double Lightness, double Chroma, double Hue) Lab(PigmentCoefficients paint, double share)
    {
        var reflectance = new double[SpectralBands.Count];
        KubelkaMunk.Mix(new[] { paint, IdealWhite }, new[] { share, 1.0 - share }, reflectance);
        SpectralRenderer.ToLab(reflectance, out double lightness, out double a, out double b);
        double hue = Math.Atan2(b, a) * (180.0 / Math.PI);
        if (hue < 0.0) hue += 360.0;
        return (lightness, Math.Sqrt((a * a) + (b * b)), hue);
    }
}
