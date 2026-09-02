using System.Text.Json;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Port of Data/UserPaletteStore: the same JSON string array, in localStorage
/// instead of %APPDATA%. Missing, empty and corrupt values all load as null so
/// the caller falls back to the full catalogue; an empty saved palette would
/// otherwise leave the app with nothing to mix.
/// </summary>
public sealed class PaletteStore
{
    public const string Key = "paintTranslator.palette";
    private readonly IKeyValueStore store;

    public PaletteStore(IKeyValueStore store) => this.store = store;

    public HashSet<string>? Load()
    {
        string? json = store.Get(Key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            string[]? names = JsonSerializer.Deserialize<string[]>(json);
            return names == null || names.Length == 0 ? null : new HashSet<string>(names, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>True when the palette was actually persisted; false means the choice
    /// only applies for this session, mirroring WinForms' warning for the same failure
    /// (MainForm.cs's save-failed MessageBox).</summary>
    public bool Save(IEnumerable<string> names) => store.Set(Key, JsonSerializer.Serialize(names));
}
