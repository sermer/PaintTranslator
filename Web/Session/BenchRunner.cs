using System.Diagnostics;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>One style's spike measurement; timings are medians in milliseconds.</summary>
public sealed record BenchRow(string Style, double PreviewMs, double FullMs, ulong FullChecksum);

/// <summary>
/// The performance spike the spec requires before any UI exists. It repeats
/// Tools/BenchmarkConversion's input and checksum so browser numbers can be set
/// against the native ones, and so configuration B (threads) can be rejected if
/// its checksum drifts from configuration A's.
/// </summary>
public static class BenchRunner
{
    private const int PaintCount = 8;

    public static IReadOnlyList<BenchRow> Run(int fullWidth, int fullHeight, int iterations, Action<string>? log)
    {
        List<PigmentCoefficients> paints = PigmentLibrary.Selectable.Take(PaintCount).ToList();
        PixelImage full = BuildNoisyGradient(fullWidth, fullHeight);
        PixelImage preview = ConversionPreview.CreateSource(full);
        var rows = new List<BenchRow>();
        foreach (StyleDefinition style in StyleRegistry.All)
        {
            IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.DefaultValues(style);
            var candidates = new CandidateSetCache();
            var colourMaps = new ColourMapCache();
            int previewMark = ConversionPreview.ScaleRadius(
                RenderContext.DefaultMarkPixels(full.Width, full.Height), full.Size, preview.Size);
            int fullMark = RenderContext.DefaultMarkPixels(full.Width, full.Height);

            var previewTimes = new List<double>(iterations);
            var fullTimes = new List<double>(iterations);
            ulong checksum = 0;
            for (int i = 0; i < iterations; i++)
            {
                previewTimes.Add(Time(() => Render(preview, paints, style, previewMark, values, candidates, colourMaps)));
                fullTimes.Add(Time(() => checksum = Checksum(Render(full, paints, style, fullMark, values, candidates, colourMaps))));
            }
            var row = new BenchRow(style.Name, Median(previewTimes), Median(fullTimes), checksum);
            rows.Add(row);
            log?.Invoke($"{row.Style}: preview {row.PreviewMs:F0} ms, full {row.FullMs:F0} ms, checksum {row.FullChecksum:X16}");
        }
        return rows;
    }

    private static PixelImage Render(
        PixelImage source, IReadOnlyList<PigmentCoefficients> paints, StyleDefinition style, int mark,
        IReadOnlyDictionary<IPipelineStage, ParameterValues> values, CandidateSetCache candidates, ColourMapCache colourMaps)
    {
        CandidateSet set = candidates.GetOrCreate(paints, style, values);
        return StylePipeline.Render(source, paints, style, mark, values, set, colourMapCache: colourMaps);
    }

    private static double Time(Action action)
    {
        var watch = Stopwatch.StartNew();
        action();
        return watch.Elapsed.TotalMilliseconds;
    }

    public static double Median(List<double> values)
    {
        var sorted = new List<double>(values);
        sorted.Sort();
        int middle = sorted.Count / 2;
        return (sorted.Count & 1) == 0 ? 0.5 * (sorted[middle - 1] + sorted[middle]) : sorted[middle];
    }

    /// <summary>FNV-1a over the packed pixels, identical to the native benchmark's.</summary>
    public static ulong Checksum(PixelImage image)
    {
        ulong hash = 14695981039346656037UL;
        foreach (int pixel in image.Pixels)
        {
            hash ^= (uint)pixel;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    /// <summary>Same deterministic input as Tools/BenchmarkConversion, so checksums compare.</summary>
    public static PixelImage BuildNoisyGradient(int width, int height)
    {
        var pixels = new int[width * height];
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                int noise = (((x * 73856093) ^ (y * 19349663)) & 15) - 8;
                int r = Math.Clamp(((x * 255) / Math.Max(width - 1, 1)) + noise, 0, 255);
                int g = Math.Clamp(((y * 255) / Math.Max(height - 1, 1)) - noise, 0, 255);
                int b = Math.Clamp((((x + y) * 255) / Math.Max(width + height - 2, 1)) + noise, 0, 255);
                pixels[row + x] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
            }
        }
        return PixelImage.FromPixels(width, height, pixels);
    }
}
