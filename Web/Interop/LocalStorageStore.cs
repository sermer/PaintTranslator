using Microsoft.JSInterop;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Interop;

/// <summary>
/// localStorage through the in-process JS runtime, which WebAssembly always has;
/// the synchronous call keeps PaletteStore free of async plumbing. Storage can
/// throw in private windows, and that must read as "nothing saved", not a crash.
/// </summary>
public sealed class LocalStorageStore : IKeyValueStore
{
    private readonly IJSInProcessRuntime js;

    public LocalStorageStore(IJSRuntime js) => this.js = (IJSInProcessRuntime)js;

    public string? Get(string key)
    {
        try { return js.Invoke<string?>("localStorage.getItem", key); }
        catch (JSException) { return null; }
    }

    public bool Set(string key, string value)
    {
        try { js.InvokeVoid("localStorage.setItem", key, value); return true; }
        catch (JSException) { return false; }
    }
}
