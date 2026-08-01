# Conversion benchmark

Runs the production style pipeline against a deterministic noisy gradient and reports
cold/warm candidate time, total time, managed allocation, working set, and every
instrumented render phase. Timing assertions deliberately do not live in the unit-test
suite because machine load would make them flaky.

Run the ordinary 1080p matrix in Release mode:

```powershell
dotnet run -c Release --project Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj
```

Run a 12 MP stress case for one style with all selectable paints:

```powershell
dotnet run -c Release --project Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj -- --width 4000 --height 3000 --paints 19 --iterations 3 --style Abstract
```

Use the same arguments before and after an optimization. Pixel checksums and the normal
test suite guard correctness; compare the median only after the first warm-up run.
