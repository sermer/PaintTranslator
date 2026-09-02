using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PaintFamiliesTests
{
    // Every selectable paint, pinned by hand. The three dark mass tones (Dioxazine
    // Purple, Phthalo Green B.S., Phthalo Blue R.S.) are the reason the classifier works
    // from a tint rather than the mass tone: judged by mass tone they read as black,
    // blue and violet respectively.
    [Theory]
    [InlineData("Titanium White", PaintFamily.Whites)]
    [InlineData("Bismuth Vanadate Yellow", PaintFamily.Yellows)]
    [InlineData("Hansa Yellow Opaque", PaintFamily.Yellows)]
    [InlineData("Diarylide Yellow", PaintFamily.Yellows)]
    [InlineData("C.P. Cadmium Orange", PaintFamily.Oranges)]
    [InlineData("Pyrrole Orange", PaintFamily.Oranges)]
    [InlineData("C.P. Cadmium Red Light", PaintFamily.Reds)]
    [InlineData("Pyrrole Red", PaintFamily.Reds)]
    [InlineData("Quinacridone Red", PaintFamily.Reds)]
    [InlineData("Quinacridone Magenta", PaintFamily.Reds)]
    [InlineData("Dioxazine Purple", PaintFamily.Violets)]
    [InlineData("Ultramarine Blue", PaintFamily.Blues)]
    [InlineData("Cobalt Blue", PaintFamily.Blues)]
    [InlineData("Phthalo Blue (R.S.)", PaintFamily.Blues)]
    [InlineData("Phthalo Blue (G.S.)", PaintFamily.Blues)]
    [InlineData("Cerulean Blue, Chromium", PaintFamily.Blues)]
    [InlineData("Phthalo Green (B.S.)", PaintFamily.Greens)]
    [InlineData("Phthalo Green (Y.S.)", PaintFamily.Greens)]
    [InlineData("Bone Black", PaintFamily.Blacks)]
    public void EverySelectablePaintLandsInItsFamily(string name, PaintFamily expected)
    {
        PigmentCoefficients paint = PigmentLibrary.Selectable.Single(p => p.Name == name);
        Assert.Equal(expected, PaintFamilies.Of(paint));
    }

    [Fact]
    public void TheTableAboveCoversTheWholeSelectableLibrary()
    {
        // If a paint is promoted into Selectable without a row above, the classifier
        // has an unpinned answer for it; this is the test that says so.
        Assert.Equal(19, PigmentLibrary.Selectable.Count);
    }

    [Fact]
    public void GroupReturnsNonEmptyFamiliesInWheelOrderWithLightestFirst()
    {
        // Deliberately shuffled input: grouping must not depend on the library's order.
        var input = PigmentLibrary.Selectable.Reverse().ToList();

        var groups = PaintFamilies.Group(input);

        Assert.Equal(
            new[] { PaintFamily.Whites, PaintFamily.Yellows, PaintFamily.Oranges, PaintFamily.Reds,
                    PaintFamily.Violets, PaintFamily.Blues, PaintFamily.Greens, PaintFamily.Blacks },
            groups.Select(g => g.Family));
        Assert.Equal(input.Count, groups.Sum(g => g.Paints.Count));

        var blues = groups.Single(g => g.Family == PaintFamily.Blues).Paints.Select(p => p.Name).ToList();
        // Cerulean is the lightest blue in mass tone and Ultramarine the darkest.
        Assert.Equal("Cerulean Blue, Chromium", blues.First());
        Assert.Equal("Ultramarine Blue", blues.Last());
    }

    [Fact]
    public void GroupOmitsFamiliesWithNoPaints()
    {
        var yellowsOnly = PigmentLibrary.Selectable.Where(p => p.ColourIndex.StartsWith("PY")).ToList();

        var groups = PaintFamilies.Group(yellowsOnly);

        Assert.Single(groups);
        Assert.Equal(PaintFamily.Yellows, groups[0].Family);
    }
}
