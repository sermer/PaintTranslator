using AngleSharp.Html.Dom;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Components;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PaintListTests : BunitContext
{
    private static readonly IReadOnlyList<PigmentCoefficients> Three = PigmentLibrary.Selectable.Take(3).ToList();

    [Fact]
    public void UncheckingOnePaintClearsSelectAllAndReportsTheRest()
    {
        IReadOnlyList<PigmentCoefficients>? reported = null;
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, Three).Add(x => x.Selected, Three)
            .Add(x => x.SelectedChanged, s => reported = s));

        cut.FindAll("input.paint").Skip(1).First().Change(false);

        Assert.Equal(new[] { Three[0], Three[2] }, reported);
        Assert.False(((IHtmlInputElement)cut.Find("input.select-all")).IsChecked);
    }

    [Fact]
    public void SelectAllChecksEveryPaint()
    {
        IReadOnlyList<PigmentCoefficients>? reported = null;
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, Three).Add(x => x.Selected, new[] { Three[0] })
            .Add(x => x.SelectedChanged, s => reported = s));

        cut.Find("input.select-all").Change(true);

        Assert.Equal(Three, reported);
    }

    [Fact]
    public void DisabledRendersEveryInputDisabled()
    {
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, Three).Add(x => x.Selected, Three).Add(x => x.Disabled, true));

        foreach (var input in cut.FindAll("input"))
        {
            Assert.True(((IHtmlInputElement)input).IsDisabled);
        }
    }
}

public class PaintListGroupingTests : BunitContext
{
    [Fact]
    public void RendersOneHeadingPerNonEmptyFamilyInWheelOrder()
    {
        var all = PigmentLibrary.Selectable;
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, all).Add(x => x.Selected, all));

        var headings = cut.FindAll(".family").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(
            new[] { "Whites", "Yellows", "Oranges", "Reds", "Violets", "Blues", "Greens", "Blacks" },
            headings);
    }

    [Fact]
    public void RowsFollowTheGroupedOrderNotTheInputOrder()
    {
        var reversed = PigmentLibrary.Selectable.Reverse().ToList();
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, reversed).Add(x => x.Selected, reversed));

        var names = cut.FindAll(".paint-row").Select(r => r.QuerySelector("span:last-child")!.TextContent).ToList();
        var expected = PaintFamilies.Group(reversed).SelectMany(g => g.Paints).Select(p => p.Name).ToList();
        Assert.Equal(expected, names);
        Assert.Equal("Titanium White", names.First());
        Assert.Equal("Bone Black", names.Last());
    }

    [Fact]
    public void SelectionIsStillReportedInTheParentsOrder()
    {
        // Grouping is display only: the parent's list order is what the session and the
        // colour wheel use, so a reordered display must not leak back through the callback.
        var reversed = PigmentLibrary.Selectable.Reverse().ToList();
        IReadOnlyList<PigmentCoefficients>? reported = null;
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, reversed).Add(x => x.Selected, reversed)
            .Add(x => x.SelectedChanged, s => reported = s));

        cut.FindAll("input.paint")[0].Change(false);

        Assert.NotNull(reported);
        Assert.Equal(reversed.Where(p => p.Name != "Titanium White"), reported);
    }
}
