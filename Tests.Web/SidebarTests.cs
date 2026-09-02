using Microsoft.Extensions.DependencyInjection;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Components;
using PaintTranslator.Web.Session;
using static PaintTranslator.Web.Tests.SessionDoubles;

namespace PaintTranslator.Web.Tests;

public class SidebarTests : BunitContext
{
    private ConversionSession Inject(ConversionSession session)
    {
        Services.AddSingleton(session);
        return session;
    }

    [Fact]
    public void AriaDisabledCarriesAnExplicitTrueOrFalse()
    {
        var session = Inject(NewSession());
        var cut = Render<Sidebar>();
        Assert.Equal("false", cut.Find("aside.sidebar").GetAttribute("aria-disabled"));

        session.BeginImageOperation();
        cut.Render();
        Assert.Equal("true", cut.Find("aside.sidebar").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void PaletteSaveBannerShowsAfterAFailedSaveAndDismisses()
    {
        var store = new FlakyStore { FailNextSet = true };
        var session = Inject(NewSession(store));
        session.ApplyPalette(PigmentLibrary.Selectable.Take(2).Select(p => p.Name));

        var cut = Render<Sidebar>();
        Assert.NotEmpty(cut.FindAll(".banner"));

        cut.Find(".banner .dismiss").Click();
        Assert.Empty(cut.FindAll(".banner"));
        Assert.False(session.PaletteSaveFailed);
    }
}
