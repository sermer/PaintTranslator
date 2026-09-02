using PaintTranslator.Imaging;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

internal sealed class MemoryStore : IKeyValueStore
{
    private readonly Dictionary<string, string> values = new();
    public string? Get(string key) => values.TryGetValue(key, out string? v) ? v : null;
    public bool Set(string key, string value) { values[key] = value; return true; }
}

/// <summary>Lets one test fail a save on demand, to pin ConversionSession.ApplyPalette's
/// PaletteSaveFailed bookkeeping without a real localStorage.</summary>
internal sealed class FlakyStore : IKeyValueStore
{
    private readonly Dictionary<string, string> values = new();
    public bool FailNextSet;
    public string? Get(string key) => values.TryGetValue(key, out string? v) ? v : null;
    public bool Set(string key, string value)
    {
        if (FailNextSet) return false;
        values[key] = value;
        return true;
    }
}

internal sealed class NullRenderer : IFrameRenderer
{
    public Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token) =>
        Task.FromResult<PixelImage?>(request.Source);
}

/// <summary>Shared by the session tests and the bUnit component tests that inject a
/// ConversionSession: a session whose debounce never elapses, so nothing renders unless
/// a test drives the scheduler itself.</summary>
internal static class SessionDoubles
{
    public static Task NeverDelay(TimeSpan _, CancellationToken token) => Task.Delay(Timeout.Infinite, token);

    public static ConversionSession NewSession(IKeyValueStore? store = null) =>
        new(new NullRenderer(), new PaletteStore(store ?? new MemoryStore()), NeverDelay);
}
