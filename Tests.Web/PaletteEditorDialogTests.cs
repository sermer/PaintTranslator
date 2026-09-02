using PaintTranslator.Pigments;
using PaintTranslator.Web.Components;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PaletteEditorDialogTests : BunitContext
{
    [Fact]
    public void OkWithNothingCheckedShowsRefusalAndDoesNotApply()
    {
        bool applied = false;
        var cut = Render<PaletteEditorDialog>(p => p
            .Add(x => x.Catalogue, PigmentLibrary.Selectable)
            .Add(x => x.Current, Array.Empty<string>())
            .Add(x => x.Open, true)
            .Add(x => x.OnApply, _ => applied = true));

        cut.Find("button.ok").Click();

        Assert.False(applied);
        Assert.Contains("Select at least one paint", cut.Markup);
    }

    [Fact]
    public void OkAppliesTheCheckedNames()
    {
        IReadOnlyList<string>? applied = null;
        string first = PigmentLibrary.Selectable[0].Name;
        var cut = Render<PaletteEditorDialog>(p => p
            .Add(x => x.Catalogue, PigmentLibrary.Selectable)
            .Add(x => x.Current, new[] { first })
            .Add(x => x.Open, true)
            .Add(x => x.OnApply, names => applied = names));

        cut.Find("button.ok").Click();

        Assert.Equal(new[] { first }, applied);
    }
}

public class PaletteEditorDialogGroupingTests : BunitContext
{
    [Fact]
    public void CatalogueIsGroupedUnderFamilyHeadingsWithSwatches()
    {
        var all = PigmentLibrary.Selectable;
        var cut = Render<PaletteEditorDialog>(p => p.Add(x => x.Catalogue, all).Add(x => x.Current, all.Select(a => a.Name).ToList()).Add(x => x.Open, true));

        var headings = cut.FindAll(".catalogue .family").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(PaintFamilies.Group(all).Select(g => g.Family.ToString()), headings);
        Assert.Equal(all.Count, cut.FindAll(".catalogue .paint-row .swatch").Count);
    }
}
