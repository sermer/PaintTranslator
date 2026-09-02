using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Benchmarks
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(Options.Usage);
                return 2;
            }

#if DEBUG
            Console.Error.WriteLine("Warning: benchmark results are meaningful only with -c Release.");
#endif

            IReadOnlyList<PigmentCoefficients> paints = PigmentLibrary.Selectable
                .Take(Math.Min(options.PaintCount, PigmentLibrary.Selectable.Count))
                .ToArray();
            IReadOnlyList<StyleDefinition> styles = string.Equals(
                options.StyleName, "all", StringComparison.OrdinalIgnoreCase)
                ? StyleRegistry.All
                : new[] { StyleRegistry.ByName(options.StyleName) };

            PixelImage sourceFrame = BuildNoisyGradient(options.Width, options.Height);
            Console.WriteLine(
                $"source={options.Width}x{options.Height} paints={paints.Count} " +
                $"iterations={options.Iterations} blur={options.BlurRadius} mark={options.MarkPixels}");

            foreach (StyleDefinition style in styles)
            {
                RunStyle(sourceFrame, paints, style, options);
            }

            return 0;
        }

        private static void RunStyle(
            PixelImage source,
            IReadOnlyList<PigmentCoefficients> paints,
            StyleDefinition style,
            Options options)
        {
            IReadOnlyDictionary<IPipelineStage, ParameterValues> baseValues =
                StylePipeline.DefaultValues(style);
            (StyleDefinition renderStyle, IReadOnlyDictionary<IPipelineStage, ParameterValues> values) =
                PalettePhotoConverter.ComposeWithBlur(style, baseValues, options.BlurRadius);

            var cache = new CandidateSetCache();
            var stopwatch = Stopwatch.StartNew();
            CandidateSet candidates = cache.GetOrCreate(paints, renderStyle, values);
            stopwatch.Stop();
            double coldCandidateMs = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            CandidateSet warmCandidates = cache.GetOrCreate(paints, renderStyle, values);
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine(
                $"[{style.Name}] candidates={candidates.Argb.Length:N0} " +
                $"cold={coldCandidateMs:F2}ms warm={stopwatch.Elapsed.TotalMilliseconds:F3}ms " +
                $"same={ReferenceEquals(candidates, warmCandidates)}");

            var colourMapCache = new ColourMapCache();
            var totals = new List<double>(options.Iterations);
            for (int iteration = 1; iteration <= options.Iterations; iteration++)
            {
                var diagnostics = new RenderDiagnostics();
                long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
                stopwatch.Restart();
                PixelImage result = StylePipeline.Render(
                    source,
                    paints,
                    renderStyle,
                    options.MarkPixels,
                    values,
                    candidates,
                    diagnostics: diagnostics,
                    colourMapCache: colourMapCache);
                stopwatch.Stop();
                long allocated = GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore;
                totals.Add(stopwatch.Elapsed.TotalMilliseconds);

                ulong checksum = Checksum(result);
                Console.WriteLine(
                    $"  run={iteration} total={stopwatch.Elapsed.TotalMilliseconds:F2}ms " +
                    $"allocated={allocated / (1024.0 * 1024.0):F1}MiB checksum={checksum:X16}");
                foreach (RenderPhaseTiming timing in diagnostics.Timings)
                {
                    Console.WriteLine($"    {timing.Phase,-32} {timing.Elapsed.TotalMilliseconds,10:F2}ms");
                }
            }

            totals.Sort();
            Console.WriteLine(
                $"  median={Median(totals):F2}ms " +
                $"working-set={Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0):F1}MiB");
        }

        private static double Median(IReadOnlyList<double> sorted)
        {
            int middle = sorted.Count / 2;
            return (sorted.Count & 1) == 0
                ? 0.5 * (sorted[middle - 1] + sorted[middle])
                : sorted[middle];
        }

        private static ulong Checksum(PixelImage image)
        {
            ulong hash = 14695981039346656037UL;
            foreach (int pixel in image.Pixels)
            {
                hash ^= (uint)pixel;
                hash *= 1099511628211UL;
            }

            return hash;
        }

        private static PixelImage BuildNoisyGradient(int width, int height)
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

        private sealed class Options
        {
            public const string Usage =
                "Usage: dotnet run -c Release --project Tools/BenchmarkConversion -- " +
                "[--width N] [--height N] [--paints N] [--iterations N] " +
                "[--style all|NAME] [--blur N] [--mark N]";

            public int Width { get; private set; } = 1920;
            public int Height { get; private set; } = 1080;
            public int PaintCount { get; private set; } = 6;
            public int Iterations { get; private set; } = 3;
            public string StyleName { get; private set; } = "all";
            public int BlurRadius { get; private set; }
            public int MarkPixels { get; private set; }

            public static Options Parse(string[] args)
            {
                var result = new Options();
                for (int i = 0; i < args.Length; i += 2)
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException($"Missing value for '{args[i]}'.");
                    }

                    string value = args[i + 1];
                    switch (args[i])
                    {
                        case "--width": result.Width = Positive(value, args[i]); break;
                        case "--height": result.Height = Positive(value, args[i]); break;
                        case "--paints": result.PaintCount = Positive(value, args[i]); break;
                        case "--iterations": result.Iterations = Positive(value, args[i]); break;
                        case "--style": result.StyleName = value; break;
                        case "--blur": result.BlurRadius = NonNegative(value, args[i]); break;
                        case "--mark": result.MarkPixels = NonNegative(value, args[i]); break;
                        default: throw new ArgumentException($"Unknown option '{args[i]}'.");
                    }
                }

                return result;
            }

            private static int Positive(string value, string option)
            {
                int parsed = NonNegative(value, option);
                return parsed > 0 ? parsed : throw new ArgumentException($"{option} must be greater than zero.");
            }

            private static int NonNegative(string value, string option)
            {
                if (!int.TryParse(value, out int parsed) || parsed < 0)
                {
                    throw new ArgumentException($"{option} must be a non-negative integer.");
                }

                return parsed;
            }
        }
    }
}
