using PaintTranslator.Web.Components;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class CreditsTests : BunitContext
{
    /// <summary>Walks up from the test binary to the directory holding the solution, so the
    /// test reads the real LICENSES.md rather than a copy that could drift.</summary>
    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "PaintTranslator.sln")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("PaintTranslator.sln not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void DecoderRowsMatchLicensesMd()
    {
        string path = Path.Combine(RepoRoot(), "Web", "wwwroot", "js", "decoders", "LICENSES.md");
        // Each line: "- <file> — <name> <version>[ extra words] — <licence> — <url>"
        var fromFile = File.ReadAllLines(path)
            .Where(l => l.StartsWith("- "))
            .Select(l => l[2..].Split(" — "))
            .Where(parts => parts.Length >= 4 && parts[1].Split(' ').Length >= 2)
            .Select(parts => (Name: parts[1].Split(' ')[0], Version: parts[1].Split(' ')[1], Licence: parts[2], Url: parts[3]))
            .OrderBy(t => t.Name)
            .ToList();
        var fromCode = Credits.Decoders
            .Select(d => (d.Name, d.Version, d.Licence, d.Url))
            .OrderBy(t => t.Name)
            .ToList();

        // A count mismatch here means a bullet in LICENSES.md didn't parse (missing an
        // " — " separator or a "<name> <version>" pair) rather than a genuine content
        // drift, so name the file explicitly instead of leaving a bare 3 != n.
        Assert.True(fromFile.Count == 3, $"Expected 3 parsed rows from LICENSES.md, found {fromFile.Count}. Check the bullet formatting in {path}.");
        Assert.Equal(fromFile, fromCode);
    }

    [Fact]
    public void RuntimeRowIsDotNetUnderMit()
    {
        Assert.Equal(".NET", Credits.Runtime.Name);
        Assert.Equal("MIT", Credits.Runtime.Licence);
        Assert.Equal(Credits.Decoders.Count + 1, Credits.All.Count);
    }

    [Fact]
    public void OpenDialogListsEveryLibraryAndCloseInvokesCallback()
    {
        bool closed = false;
        var cut = Render<CreditsDialog>(p => p.Add(x => x.Open, true).Add(x => x.OnClose, () => closed = true));

        Assert.Equal(Credits.All.Count, cut.FindAll("li.credit").Count);
        foreach (VendoredLibrary lib in Credits.All) Assert.Contains(lib.Name, cut.Markup);
        Assert.Contains("LGPL", cut.Markup);

        cut.Find("button.close").Click();
        Assert.True(closed);
    }

    [Fact]
    public void ClosedDialogRendersNothing()
    {
        var cut = Render<CreditsDialog>(p => p.Add(x => x.Open, false));
        Assert.Equal(string.Empty, cut.Markup.Trim());
    }
}
