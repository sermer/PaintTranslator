namespace PaintTranslator.Web.Session;

/// <summary>One third-party component the shipped site contains, as shown in the Credits
/// dialog. Name and version are spelled the way LICENSES.md spells them because
/// CreditsTests compares the two files literally; that comparison, not this list, is what
/// keeps the dialog honest when Tools/BuildDecoders/build.sh bumps a decoder.</summary>
public sealed record VendoredLibrary(string Name, string Version, string Licence, string Url);

/// <summary>The rows behind the Credits dialog. The decoders are the reason the dialog
/// exists: libheif-js is LGPL-3.0, and a public site should say so where a visitor can
/// see it rather than only in a markdown file in the repository.</summary>
public static class Credits
{
    public static readonly IReadOnlyList<VendoredLibrary> Decoders =
    [
        new("libheif-js", "1.18.2", "LGPL-3.0", "https://github.com/catdad-experiments/libheif-js"),
        new("utif", "3.1.0", "MIT", "https://github.com/photopea/UTIF.js"),
        new("@webtoon/psd", "0.4.0", "MIT", "https://github.com/webtoon/psd"),
    ];

    public static readonly VendoredLibrary Runtime =
        new(".NET", "10", "MIT", "https://github.com/dotnet/runtime");

    public static readonly IReadOnlyList<VendoredLibrary> All = [.. Decoders, Runtime];
}
