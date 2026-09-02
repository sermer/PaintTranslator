using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PaletteStoreTests
{
    private sealed class MemoryStore : IKeyValueStore
    {
        public Dictionary<string, string> Values { get; } = new();
        public string? Get(string key) => Values.TryGetValue(key, out string? v) ? v : null;
        public bool Set(string key, string value) { Values[key] = value; return true; }
    }

    [Fact]
    public void RoundTripsNamesAsAJsonStringArray()
    {
        var memory = new MemoryStore();
        var store = new PaletteStore(memory);
        store.Save(new[] { "Titanium White", "Ultramarine Blue" });
        Assert.Equal("[\"Titanium White\",\"Ultramarine Blue\"]", memory.Values[PaletteStore.Key]);
        Assert.Equal(new HashSet<string> { "Titanium White", "Ultramarine Blue" }, store.Load());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("not json")]
    [InlineData("{\"a\":1}")]
    public void MissingEmptyOrCorruptStorageLoadsAsNull(string? stored)
    {
        var memory = new MemoryStore();
        if (stored != null) memory.Values[PaletteStore.Key] = stored;
        Assert.Null(new PaletteStore(memory).Load());
    }
}
