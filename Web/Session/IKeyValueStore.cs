namespace PaintTranslator.Web.Session;

/// <summary>
/// The persistence seam. The browser's localStorage is synchronous and
/// string-only, so the abstraction is too; tests substitute a dictionary.
/// </summary>
public interface IKeyValueStore
{
    string? Get(string key);

    /// <summary>True on success; false when the underlying store threw (a full or
    /// disabled localStorage), so a caller that promised to remember something can
    /// tell the user it didn't, rather than failing silently.</summary>
    bool Set(string key, string value);
}
