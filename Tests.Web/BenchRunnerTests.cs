using PaintTranslator.Imaging;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class BenchRunnerTests
{
    [Fact]
    public void MedianOfEvenCountAveragesTheMiddlePair()
    {
        Assert.Equal(2.5, BenchRunner.Median(new List<double> { 4, 1, 3, 2 }));
    }

    [Fact]
    public void MedianOfOddCountIsTheMiddleValue()
    {
        Assert.Equal(3, BenchRunner.Median(new List<double> { 5, 1, 3 }));
    }

    [Fact]
    public void GradientIsDeterministicAndOpaque()
    {
        PixelImage a = BenchRunner.BuildNoisyGradient(64, 32);
        PixelImage b = BenchRunner.BuildNoisyGradient(64, 32);
        Assert.Equal(BenchRunner.Checksum(a), BenchRunner.Checksum(b));
        for (int i = 0; i < a.Pixels.Length; i++)
        {
            // AlphaAt returns the alpha byte still in its packed position (bits
            // 24-31), not shifted down to 0-255 - see Tests/PixelImageTests.cs's
            // AlphaAtMasksEverythingButTheAlphaByte. BuildNoisyGradient always sets
            // the top byte to 0xFF, so every pixel's masked value is 0xFF000000.
            Assert.Equal(unchecked((int)0xFF000000), a.AlphaAt(i));
        }
    }

    [Fact]
    public void RunProducesOneRowPerStyleWithPositiveTimings()
    {
        IReadOnlyList<BenchRow> rows = BenchRunner.Run(96, 64, iterations: 1, log: null);
        Assert.Equal(PaintTranslator.Imaging.Styles.StyleRegistry.All.Count, rows.Count);
        Assert.All(rows, r => Assert.True(r.PreviewMs > 0 && r.FullMs > 0));
    }
}
