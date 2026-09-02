using Microsoft.Extensions.DependencyInjection;
using PaintTranslator.Web.Components;
using static PaintTranslator.Web.Tests.SessionDoubles;

namespace PaintTranslator.Web.Tests;

public class ToolbarTests : BunitContext
{
    public ToolbarTests()
    {
        // InputFile calls into JavaScript after its first render; there is no browser here
        // and nothing in these tests opens a file, so unplanned JS calls return defaults.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(NewSession());
    }

    [Fact]
    public void ClickingOutsideTheOpenWheelMenuClosesIt()
    {
        var cut = Render<Toolbar>();
        cut.Find(".menu > button").Click();
        Assert.NotEmpty(cut.FindAll(".menu-items"));

        cut.Find(".menu-backdrop").Click();
        Assert.Empty(cut.FindAll(".menu-items"));
    }

    [Fact]
    public void EscapeClosesTheOpenWheelMenu()
    {
        var cut = Render<Toolbar>();
        cut.Find(".menu > button").Click();

        cut.Find(".menu").KeyDown(Key.Escape);
        Assert.Empty(cut.FindAll(".menu-items"));
    }

    // Safari on macOS does not focus a <button> on click, so Escape's keydown only
    // reaches the menu if the menu div itself is focusable and gets focus when the
    // menu opens. bUnit dispatches KeyDown directly at .menu regardless of real focus,
    // so it cannot exercise that path itself; this pins the div's own focusability,
    // which is the part a direct-dispatch test can't otherwise catch.
    [Fact]
    public void MenuDivIsFocusableSoEscapeReachesItWithoutRelyingOnBubbling()
    {
        var cut = Render<Toolbar>();
        Assert.Equal("-1", cut.Find(".menu").GetAttribute("tabindex"));
    }
}
