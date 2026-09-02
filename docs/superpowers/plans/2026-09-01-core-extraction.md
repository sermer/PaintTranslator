# Core Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the physics and imaging kernel into a platform-neutral `PaintTranslator.Core` library on .NET 10 so the test suite runs on macOS and the kernel can later compile to WebAssembly.

**Architecture:** Replace `System.Drawing.Bitmap` at the kernel's boundary with an immutable packed-ARGB `PixelImage`; keep the cross-platform `System.Drawing.Primitives` structs (`Color`, `Point`, `Size`, `RectangleF`). GDI conversions, Magick.NET decoding and `Graphics` drawing stay in the WinForms app behind a `GdiImageAdapter`. Tests that need GDI or WinForms move to a Windows-only test project.

**Tech Stack:** .NET 10 SDK (`dotnet` 10.0.400 on the Mac), xUnit, SixLabors.ImageSharp (tests only, PNG codec), Magick.NET-Q8-AnyCPU (tests only), Magick.NET-Q8-x64 (WinForms app only).

**Spec:** `docs/superpowers/specs/2026-09-01-core-extraction-design.md`

## Global Constraints

- **Never commit. Never branch. Never create a worktree.** Stage with `git add` and stop. (`CLAUDE.md`)
- Every project retargets from `net5.0` / `net5.0-windows` to `net10.0` / `net10.0-windows`.
- `Core/PaintTranslator.Core.csproj` targets `net10.0` with no `RuntimeIdentifier`, no `UseWindowsForms`, no Magick.NET reference.
- Windows-targeted projects set `<EnableWindowsTargeting>true</EnableWindowsTargeting>` so they compile on macOS.
- Namespaces `PaintTranslator.Pigments`, `PaintTranslator.Imaging`, `PaintTranslator.Imaging.Styles` do not change.
- `PixelImage` byte order is GDI `Format32bppArgb`: one `int` per pixel, `0xAARRGGBB`, row-major, stride equals width.
- No colour, mixing or mapping behaviour changes. Existing test assertions are preserved; only types and I/O helpers change.
- Doc comments carry reasoning, not signature restatements (`CLAUDE.md` conventions). Follow the `csharp-code-comments` skill for every new class and method.
- Run tests from the repo root: `dotnet test Tests/PaintTranslator.Tests.csproj`.
- Working directory for every command: `/Users/sean/Desktop/ADHD Meadows/PaintTranslator` (note the space in the path; quote it).

---

## File map

**Create**
- `Core/PaintTranslator.Core.csproj` — the kernel library (Task 7)
- `Core/Imaging/PixelImage.cs` — immutable packed-ARGB image (Task 2; created at `Imaging/PixelImage.cs`, moved in Task 7)
- `Core/Imaging/GridGeometry.cs` — grid line positions with no drawing (Task 6)
- `Windows/GdiImageAdapter.cs` — `Bitmap` ↔ `PixelImage` (Task 2)
- `Windows/ColorWheelExport.cs` — PNG save for the `--generate-colorwheel` CLI flag (Task 5)
- `Windows/GridOverlayRenderer.cs` — draws `GridGeometry` with GDI pens (Task 6)
- `Tests/PngCodec.cs` — ImageSharp-backed PNG read/write into `PixelImage` (Task 4)
- `Tests/PixelImageTests.cs` — replaces `SourceFrameTests.cs` (Task 2)
- `Tests.Windows/PaintTranslator.Windows.Tests.csproj` — Windows-only tests (Task 7)

**Move**
- `Imaging/SourceFrame.cs` → `Imaging/PixelImage.cs` (Task 2), then everything under `Pigments/` and `Imaging/` → `Core/` (Task 7)
- `Imaging/ImageDecoder.cs` → `Windows/ImageDecoder.cs` (Task 7)
- `Tests/{ImageDecoderTests,ImageCanvasTests,UiThemeTests,ContactSheetTests}.cs` → `Tests.Windows/` (Task 7)

**Modify**
- Every `.csproj` and `PaintTranslator.sln` (Tasks 1, 7)
- `Imaging/StylePipeline.cs`, `Imaging/PalettePhotoConverter.cs` (Task 3)
- `Imaging/ConversionPreview.cs` (Task 4)
- `Imaging/ColorWheelGenerator.cs` (Task 5)
- `Imaging/GridOverlayRenderer.cs` (deleted, split in Task 6)
- `MainForm.cs`, `Program.cs` (Tasks 2–6)
- `Tools/BenchmarkConversion/Program.cs` (Task 3)
- `Tests/StyleTestFixtures.cs` and every test that names `Bitmap` (Tasks 3–5)
- `CLAUDE.md` (Task 8)

---

### Task 1: Retarget the solution to .NET 10 and get the pure kernel tests running on the Mac

No code moves yet. After this task the WinForms app compiles on macOS and every test that never touches `Bitmap` passes here. Tests that do touch `Bitmap` fail with `PlatformNotSupportedException`; Tasks 2–7 remove them one group at a time.

**Files:**
- Modify: `PaintTranslator.csproj`
- Modify: `Tests/PaintTranslator.Tests.csproj`
- Modify: `BlendTests/PaintTranslator.BlendTests.csproj`
- Modify: `Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj`
- Modify: `Tools/IngestSpectra/IngestSpectra.csproj`

**Interfaces:**
- Produces: a solution that builds on macOS with `dotnet build PaintTranslator.sln`.

- [ ] **Step 1: Confirm the baseline does not build here**

Run: `dotnet build PaintTranslator.sln 2>&1 | tail -5`
Expected: errors mentioning `net5.0-windows` / `NETSDK1100` (Windows desktop targeting on a non-Windows host) or a missing .NET 5 targeting pack. Record the first error line in the handoff task doc so the "before" state is known.

- [ ] **Step 2: Retarget the app project**

Replace the `<PropertyGroup>` in `PaintTranslator.csproj` with:

```xml
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>PaintTranslator</RootNamespace>
    <AssemblyName>PaintTranslator</AssemblyName>
    <!-- Magick.NET ships native codecs per platform. Without a pinned RID, NuGet copies
         the Linux and macOS binaries alongside the Windows ones and the build output
         grows from 25 MB to 131 MB; this app is Windows-only, so pin it. -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
    <!-- The owner develops on macOS. This lets the Windows-only app compile there
         (the desktop reference pack is downloaded on restore); it still only runs on
         Windows. -->
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
  </PropertyGroup>
```

- [ ] **Step 3: Retarget the test project**

Replace the `<PropertyGroup>` and the package `<ItemGroup>` in `Tests/PaintTranslator.Tests.csproj` with:

```xml
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <!-- Still Windows-targeted until Task 7 splits the GDI-bound tests out; the
         runtime identifier is dropped so the test host is a native binary here. -->
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <IsPackable>false</IsPackable>
    <RootNamespace>PaintTranslator.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <!-- The parity gate compares this project's kernel against Unicolour's independent
         implementation. Referenced here explicitly because the application no longer
         depends on Unicolour, and the gate must outlive that removal. -->
    <PackageReference Include="Wacton.Unicolour" Version="8.0.0" />
    <PackageReference Include="Wacton.Unicolour.Datasets" Version="5.0.0" />
  </ItemGroup>
```

If NuGet rejects any of the three test package versions, run `dotnet add Tests/PaintTranslator.Tests.csproj package <name>` without a version to take the current one, and record the resolved version in the handoff doc.

- [ ] **Step 4: Retarget BlendTests, Benchmarks and IngestSpectra**

`BlendTests/PaintTranslator.BlendTests.csproj` `<PropertyGroup>`:

```xml
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <RootNamespace>PaintTranslator.BlendTests</RootNamespace>
    <AssemblyName>PaintTranslator.BlendTests</AssemblyName>
  </PropertyGroup>
```

`Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj` `<PropertyGroup>` (WinForms and the RID are dropped in Task 3 when it stops using `Bitmap`; for now it still needs them):

```xml
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <RootNamespace>PaintTranslator.Benchmarks</RootNamespace>
    <AssemblyName>PaintTranslator.Benchmarks</AssemblyName>
  </PropertyGroup>
```

`Tools/IngestSpectra/IngestSpectra.csproj`: change `<TargetFramework>net5.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>` and delete the whole `<ItemGroup>` containing `System.Drawing.Common` (the `Color` struct it needed lives in the shared framework's `System.Drawing.Primitives` on .NET 10).

- [ ] **Step 5: Build**

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E "error|Warn|Build succeeded" | head -20`
Expected: `Build succeeded`, 0 errors. `NETSDK1138` no longer appears. New `CA1416` (platform compatibility) warnings from the desktop analysers are acceptable and are not to be chased.

If the build fails on `Parallel.For` overload ambiguity or an obsolete API, fix that one call site and rebuild; do not refactor around it.

- [ ] **Step 6: Run the pure kernel tests**

Run:
```
dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~UnicolourParityTests|FullyQualifiedName~MixingInvariantTests|FullyQualifiedName~KubelkaMunkTests|FullyQualifiedName~SubsetSolverTests|FullyQualifiedName~ColorSpaceTests" 2>&1 | tail -5
```
Expected: `Passed!` with 0 failures. If the run reports zero tests, the adapter did not load; check the `xunit.runner.visualstudio` version first.

- [ ] **Step 7: Run the full suite and record the failing set**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj 2>&1 | grep -E "Failed |Passed!|Failed!" | head -60`
Expected: failures only in tests that construct or receive a `Bitmap` (message contains `PlatformNotSupportedException` or `System.Drawing.Common is not supported on this platform`). Write the failing count into the handoff task doc; Tasks 2–7 drive it to zero.

- [ ] **Step 8: Stage**

```bash
git add PaintTranslator.csproj Tests/PaintTranslator.Tests.csproj BlendTests/PaintTranslator.BlendTests.csproj Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj Tools/IngestSpectra/IngestSpectra.csproj
```

---

### Task 2: `PixelImage` replaces `SourceFrame`; `GdiImageAdapter` owns the `Bitmap` conversions

**Files:**
- Move: `Imaging/SourceFrame.cs` → `Imaging/PixelImage.cs` (via `git mv`)
- Create: `Windows/GdiImageAdapter.cs`
- Create: `Tests/PixelImageTests.cs`
- Delete: `Tests/SourceFrameTests.cs`
- Modify: `Imaging/StylePipeline.cs:49-80` (the two `Render` overloads' parameter types)
- Modify: `MainForm.cs:48,103,258,501,516,1048-1057,1087`
- Modify: `Tools/BenchmarkConversion/Program.cs:43,57`

**Interfaces:**
- Produces:
  ```csharp
  namespace PaintTranslator.Imaging
  public sealed class PixelImage
  {
      public int Width { get; }
      public int Height { get; }
      public Size Size { get; }
      public ReadOnlySpan<int> Pixels { get; }
      public int this[int x, int y] { get; }
      public int AlphaAt(int index);
      public int[] CopyPixels();
      public static PixelImage FromPixels(int width, int height, int[] pixels);
      public static PixelImage Filled(int width, int height, int argb);
  }
  namespace PaintTranslator.Windows
  public static class GdiImageAdapter
  {
      public static PixelImage FromBitmap(Bitmap source);
      public static Bitmap ToBitmap(PixelImage image);
  }
  ```

- [ ] **Step 1: Write the failing tests**

Create `Tests/PixelImageTests.cs`:

```csharp
using System;
using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class PixelImageTests
    {
        [Fact]
        public void FromPixelsRejectsABufferOfTheWrongLength()
        {
            Assert.Throws<ArgumentException>(() => PixelImage.FromPixels(3, 2, new int[5]));
        }

        [Fact]
        public void IndexerReadsRowMajor()
        {
            var pixels = new int[] { 1, 2, 3, 4, 5, 6 };
            PixelImage image = PixelImage.FromPixels(3, 2, pixels);

            Assert.Equal(4, image[0, 1]);
            Assert.Equal(3, image[2, 0]);
            Assert.Equal(new Size(3, 2), image.Size);
        }

        [Fact]
        public void PixelCopiesCannotMutateTheImage()
        {
            PixelImage image = PixelImage.FromPixels(3, 2, new int[6]);
            int[] changed = image.CopyPixels();
            changed[0] = 0x12345678;

            Assert.Equal(0, image.CopyPixels()[0]);
            Assert.Equal(0, image[0, 0]);
        }

        [Fact]
        public void AlphaAtMasksEverythingButTheAlphaByte()
        {
            PixelImage image = PixelImage.Filled(1, 1, unchecked((int)0x80FF00FF));
            Assert.Equal(unchecked((int)0x80000000), image.AlphaAt(0));
        }

        [Fact]
        public void FilledCoversEveryPixel()
        {
            PixelImage image = PixelImage.Filled(4, 3, 0x11223344);
            foreach (int pixel in image.Pixels)
            {
                Assert.Equal(0x11223344, pixel);
            }
        }
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~PixelImageTests" 2>&1 | grep -E "error|Passed!|Failed!" | head`
Expected: build error `The type or namespace name 'PixelImage' could not be found`.

- [ ] **Step 3: Rename and rewrite `SourceFrame` as `PixelImage`**

```bash
git mv Imaging/SourceFrame.cs Imaging/PixelImage.cs
```

Replace the file's contents:

```csharp
using System;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// An immutable image: width, height, and one packed ARGB <see cref="int"/> per
    /// pixel in row-major order with no stride padding. It is the only image type the
    /// kernel takes or returns, so the kernel compiles without <c>System.Drawing.Common</c>
    /// and can be shared safely by cancelled and replacement renders without cloning.
    /// </summary>
    /// <remarks>
    /// The byte layout is GDI's <c>Format32bppArgb</c> (<c>0xAARRGGBB</c>), so the
    /// Windows adapter is a straight memory copy and nothing downstream ever reorders
    /// channels. Every operation that needs a mutable buffer takes a
    /// <see cref="CopyPixels"/> and works on that; the image itself is never written.
    /// </remarks>
    public sealed class PixelImage
    {
        private readonly int[] pixels;

        private PixelImage(int width, int height, int[] pixels)
        {
            Width = width;
            Height = height;
            this.pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public Size Size => new Size(Width, Height);

        /// <summary>
        /// The packed pixels, read-only. Exposed as a span rather than the array so a
        /// caller cannot mutate an image another render is still reading.
        /// </summary>
        public ReadOnlySpan<int> Pixels => pixels;

        public int this[int x, int y] => pixels[(y * Width) + x];

        /// <summary>
        /// Wraps a caller-built buffer without copying it. The caller gives up the
        /// buffer: writing to it afterwards would break immutability for every reader.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the buffer length is not
        /// <paramref name="width"/> × <paramref name="height"/>.</exception>
        public static PixelImage FromPixels(int width, int height, int[] pixels)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height));
            }
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }
            if (pixels.Length != width * height)
            {
                throw new ArgumentException(
                    $"Expected {width * height} pixels for {width}x{height}, got {pixels.Length}.",
                    nameof(pixels));
            }

            return new PixelImage(width, height, pixels);
        }

        public static PixelImage Filled(int width, int height, int argb)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height));
            }

            var buffer = new int[width * height];
            Array.Fill(buffer, argb);
            return new PixelImage(width, height, buffer);
        }

        public int[] CopyPixels()
        {
            return (int[])pixels.Clone();
        }

        public int AlphaAt(int index)
        {
            return pixels[index] & unchecked((int)0xFF000000);
        }
    }
}
```

- [ ] **Step 4: Create the GDI adapter**

Create `Windows/GdiImageAdapter.cs`:

```csharp
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// The one place the WinForms app converts between GDI bitmaps and the kernel's
    /// <see cref="PixelImage"/>. Keeping both directions here means the kernel never
    /// references <c>System.Drawing.Common</c>, which only exists on Windows.
    /// </summary>
    public static class GdiImageAdapter
    {
        /// <summary>
        /// Snapshots a bitmap of any pixel format into packed ARGB. Drawing it onto a
        /// fresh 32bppArgb surface first is what normalises indexed, 24-bit and
        /// premultiplied sources; reading <c>LockBits</c> on the original would hand
        /// back whatever format it happened to be in.
        /// </summary>
        public static PixelImage FromBitmap(Bitmap source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int width = source.Width;
            int height = source.Height;
            using var normalized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(normalized))
            {
                graphics.DrawImage(source, 0, 0, width, height);
            }

            BitmapData data = normalized.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int strideInts = data.Stride / 4;
                var packed = new int[width * height];
                var row = new int[strideInts];
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(data.Scan0 + (y * data.Stride), row, 0, strideInts);
                    Array.Copy(row, 0, packed, y * width, width);
                }

                return PixelImage.FromPixels(width, height, packed);
            }
            finally
            {
                normalized.UnlockBits(data);
            }
        }

        public static Bitmap ToBitmap(PixelImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            int width = image.Width;
            int height = image.Height;
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int[] pixels = image.CopyPixels();
                int strideInts = data.Stride / 4;
                if (strideInts == width)
                {
                    Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(pixels, y * width, data.Scan0 + (y * data.Stride), width);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }
    }
}
```

- [ ] **Step 5: Update `StylePipeline.Render`**

In `Imaging/StylePipeline.cs`:
- Delete the first `Render` overload (the one whose first parameter is `Bitmap source`, lines 49–68 including its doc comment).
- In the remaining overload change `SourceFrame source,` to `PixelImage source,`.
- Leave `Bitmap result` and `source.CreateBitmap(pixels)` for now: temporarily replace `result = source.CreateBitmap(pixels);` with `result = PaintTranslator.Windows.GdiImageAdapter.ToBitmap(PixelImage.FromPixels(width, height, pixels));`. Task 3 removes this; it keeps the build green in between.

- [ ] **Step 6: Update `MainForm.cs` and the benchmark**

In `MainForm.cs`:
- Add `using PaintTranslator.Windows;`.
- Replace every `SourceFrame` with `PixelImage` (lines 48, 103, 258, 501, 516, 1048, 1053, 1055).
- Line 1053: `SourceFrame full = SourceFrame.Create(photo);` → `PixelImage full = GdiImageAdapter.FromBitmap(photo);`
- Lines 1054–1055: keep `using Bitmap preview = ConversionPreview.CreateSource(photo);` and change the next line to `PixelImage previewFrame = GdiImageAdapter.FromBitmap(preview);` (Task 4 replaces both with the Core downsampler).
- Line 1087: `SetDisplayedImage(sourceFrame.CreateBitmap());` → `SetDisplayedImage(GdiImageAdapter.ToBitmap(sourceFrame));`

In `Tools/BenchmarkConversion/Program.cs`:
- Add `using PaintTranslator.Windows;`.
- Line 43: `SourceFrame sourceFrame = SourceFrame.Create(source);` → `PixelImage sourceFrame = GdiImageAdapter.FromBitmap(source);`
- Line 57: `SourceFrame source,` → `PixelImage source,`

- [ ] **Step 7: Replace `SourceFrameTests`**

```bash
git rm Tests/SourceFrameTests.cs
```

Both of its tests are superseded: the immutability check is `PixelCopiesCannotMutateTheImage`, and the "bitmap and frame entry points agree" test has no subject once the `Bitmap` overload is gone.

- [ ] **Step 8: Build and run**

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E " error |Build succeeded" | head`
Expected: `Build succeeded`. Any remaining `SourceFrame` reference shows up here as CS0246; fix it the same way as Step 6.

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~PixelImageTests" 2>&1 | tail -3`
Expected: `Passed!  - Failed: 0, Passed: 5`.

- [ ] **Step 9: Stage**

```bash
git add Imaging/PixelImage.cs Windows/GdiImageAdapter.cs Imaging/StylePipeline.cs MainForm.cs Tools/BenchmarkConversion/Program.cs Tests/PixelImageTests.cs Tests/SourceFrameTests.cs
```

---

### Task 3: The pipeline and converter return `PixelImage`; tests stop building `Bitmap`s

After this task no test outside the four Windows-bound classes and the colour-wheel tests names `Bitmap`.

**Files:**
- Modify: `Imaging/StylePipeline.cs:132,239-250`
- Modify: `Imaging/PalettePhotoConverter.cs:103-112,151-185`
- Modify: `MainForm.cs:291-306` and the two `await` sites that consume `RenderCapturedRequest` (`PreviewTimer_Tick` around line 341 and `RenderCapturedRequestAsync` around line 439)
- Modify: `Tools/BenchmarkConversion/Program.cs` (`BuildNoisyGradient`, `Checksum`, `using Bitmap result`), `Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj`
- Modify: `Tests/StyleTestFixtures.cs`, `Tests/ConverterInvariantTests.cs`, `Tests/ConverterBlurOrderTests.cs`, `Tests/PaintabilityFloorTests.cs`, `Tests/StyleBehaviourTests.cs`, `Tests/StylePipelineTests.cs`, `Tests/GoldenStyleTests.cs`

**Interfaces:**
- Consumes: `PixelImage`, `GdiImageAdapter` (Task 2)
- Produces:
  ```csharp
  public static PixelImage StylePipeline.Render(PixelImage source, IReadOnlyList<PigmentCoefficients> paints, StyleDefinition style, int markPixels, IReadOnlyDictionary<IPipelineStage, ParameterValues> values, CandidateSet preparedCandidates = null, CancellationToken cancellationToken = default, RenderDiagnostics diagnostics = null, ColourMapCache colourMapCache = null)
  public static PixelImage PalettePhotoConverter.Convert(PixelImage source, IReadOnlyList<PigmentCoefficients> paints, int blurRadius = 0, int markPixels = 0)
  internal static PixelImage PalettePhotoConverter.Convert(PixelImage source, IReadOnlyList<PigmentCoefficients> paints, StyleDefinition style, int blurRadius = 0, int markPixels = 0)
  // test helpers
  internal static PixelImage StyleTestFixtures.BuildGradient(int width, int height)
  internal static PixelImage StyleTestFixtures.BuildNoisyGradient(int width, int height, double sigma)
  internal static int[] StyleTestFixtures.ReadPixels(PixelImage image, out int strideInts)   // stride == Width
  ```
  (Task 4 adds `PngCodec` so `GoldenStyleTests` can read and write PNGs; in this task `GoldenStyleTests` is edited but stays failing on the Mac.)

- [ ] **Step 1: Rewrite the test fixtures**

In `Tests/StyleTestFixtures.cs` replace the `using` block with:

```csharp
using System;
using System.Collections.Generic;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
```

Replace `BuildGradientBitmap`, `BuildNoisyGradient` and `ReadPixels` with:

```csharp
        internal static PixelImage BuildGradient(int width, int height)
        {
            var pixels = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = (x * 255) / (width - 1);
                    int g = (y * 255) / (height - 1);
                    int b = ((x + y) * 255) / (width + height - 2);
                    pixels[(y * width) + x] = Argb(255, r, g, b);
                }
            }

            return PixelImage.FromPixels(width, height, pixels);
        }

        internal static PixelImage BuildNoisyGradient(int width, int height, double sigma)
        {
            var corners = new[]
            {
                new[] { 28.0, 38.0, 92.0 },
                new[] { 232.0, 214.0, 168.0 },
                new[] { 176.0, 62.0, 48.0 },
                new[] { 244.0, 242.0, 238.0 },
            };

            var rng = new Random(7);
            var pixels = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                double fy = y / (double)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    double fx = x / (double)(width - 1);
                    var channel = new int[3];
                    for (int c = 0; c < 3; c++)
                    {
                        double top = (corners[0][c] * (1 - fx)) + (corners[1][c] * fx);
                        double bottom = (corners[2][c] * (1 - fx)) + (corners[3][c] * fx);
                        double value = (top * (1 - fy)) + (bottom * fy);
                        if (sigma > 0.0)
                        {
                            double u1 = 1.0 - rng.NextDouble();
                            double u2 = rng.NextDouble();
                            value += sigma * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                        }

                        channel[c] = Math.Clamp((int)Math.Round(value), 0, 255);
                    }

                    pixels[(y * width) + x] = Argb(255, channel[0], channel[1], channel[2]);
                }
            }

            return PixelImage.FromPixels(width, height, pixels);
        }

        /// <summary>
        /// Kept with the old <c>out</c> stride so the many call sites that index
        /// <c>row = y * stride</c> do not change; a <see cref="PixelImage"/> has no
        /// padding, so the stride is simply the width.
        /// </summary>
        internal static int[] ReadPixels(PixelImage image, out int strideInts)
        {
            strideInts = image.Width;
            return image.CopyPixels();
        }

        internal static int Argb(int a, int r, int g, int b)
        {
            return (a << 24) | (r << 16) | (g << 8) | b;
        }
```

The loop order and the `Random(7)` seed are unchanged on purpose: the golden PNGs were rendered from exactly this sequence of pixels, and the benchmark's own copy of the noisy gradient must match too.

- [ ] **Step 2: Mechanically convert the six pipeline/converter test files**

Apply these rules to `ConverterInvariantTests.cs`, `ConverterBlurOrderTests.cs`, `PaintabilityFloorTests.cs`, `StyleBehaviourTests.cs`, `StylePipelineTests.cs`, `GoldenStyleTests.cs`:

| Old | New |
|---|---|
| `using System.Drawing;` / `using System.Drawing.Imaging;` | delete, unless `Color` is still used in that file, in which case keep `using System.Drawing;` |
| `using Bitmap x = ` | `PixelImage x = ` |
| `Bitmap` in parameter and return types | `PixelImage` |
| `StyleTestFixtures.BuildGradientBitmap(` | `StyleTestFixtures.BuildGradient(` |
| `image.GetPixel(x, y).ToArgb()` | `image[x, y]` |
| `image.GetPixel(x, y)` (used as a `Color`) | `Color.FromArgb(image[x, y])` |
| `.Save(path, ImageFormat.Png)` | `PngCodec.Save(image, path)` (defined in Task 4) |
| `new Bitmap(path)` | `PngCodec.Load(path)` (defined in Task 4) |

Two private builders need rewriting rather than substitution:

`ConverterBlurOrderTests.BuildEdgeWithNoise` (line 89): keep its arithmetic; replace `new Bitmap(...)` with `var pixels = new int[width * height];`, replace `bitmap.SetPixel(x, y, Color.FromArgb(255, channel[0], channel[1], channel[2]));` with `pixels[(y * width) + x] = StyleTestFixtures.Argb(255, channel[0], channel[1], channel[2]);`, and return `PixelImage.FromPixels(width, height, pixels)`.

`StylePipelineTests.BuildGradientBitmapWithVaryingAlpha` (line 360): same treatment, with `StyleTestFixtures.Argb(alpha, r, g, b)`.

`StylePipelineTests.AssertBitmapsIdentical` and `GoldenStyleTests.AssertPixelsIdentical`: change parameter types to `PixelImage`; the bodies already work through `ReadPixels`.

- [ ] **Step 3: Run to verify the tests fail for the right reason**

Run: `dotnet build Tests/PaintTranslator.Tests.csproj 2>&1 | grep -E " error " | head`
Expected: errors are only `cannot convert from 'PaintTranslator.Imaging.PixelImage' to 'System.Drawing.Bitmap'` at `StylePipeline.Render` / `PalettePhotoConverter.Convert` call sites, plus `PngCodec` not found in `GoldenStyleTests`. Nothing else.

- [ ] **Step 4: Make `StylePipeline.Render` return `PixelImage`**

In `Imaging/StylePipeline.cs`:
- Change the remaining `Render`'s return type from `Bitmap` to `PixelImage` and its doc comment `<returns>` to "A new image, or null when cancellation is observed during any cooperative rendering phase."
- Line 132: `Bitmap result = null;` → `PixelImage result = null;`
- Replace the Task 2 placeholder `result = PaintTranslator.Windows.GdiImageAdapter.ToBitmap(...)` with `result = PixelImage.FromPixels(width, height, pixels);` and change the diagnostics label on the next line from `"Output: write bitmap"` to `"Output: wrap pixels"`.
- Delete the `catch { result?.Dispose(); throw; }` block, since `PixelImage` owns nothing to dispose. Keep the `finally` that returns `indices` to the pool.
- Remove `using System.Drawing;` and `using System.Drawing.Imaging;` if nothing else in the file needs them (the file still uses `Color` only if it does; check with the compiler).

`pixels` was rented from `source.CopyPixels()`, so handing it to `FromPixels` transfers ownership without a second copy.

- [ ] **Step 5: Make `PalettePhotoConverter.Convert` return `PixelImage`**

In `Imaging/PalettePhotoConverter.cs`, both `Convert` overloads (lines 103 and 151): change `Bitmap` to `PixelImage` in the return type and the `source` parameter. Update the `<returns>` doc lines to "A new image containing the converted photo." and the `<see cref="Convert(Bitmap, ...)"/>` on line 186 to `Convert(PixelImage, ...)`. Remove the `System.Drawing.Imaging` using if present.

- [ ] **Step 6: Update `MainForm`**

`RenderCapturedRequest` (line 291) currently returns `Bitmap`. Change its body's return to:

```csharp
            PixelImage rendered = StylePipeline.Render(
                request.Source, request.Paints, request.Style, request.MarkPixels,
                request.Values, candidates, cancellationToken,
                colourMapCache: colourMapCache);
            return rendered == null ? null : GdiImageAdapter.ToBitmap(rendered);
```

Its signature stays `Bitmap`, so `RenderCapturedRequestAsync`, `PreviewTimer_Tick` and `SetDisplayedImage` do not change.

- [ ] **Step 7: Update the benchmark and drop its WinForms dependency**

In `Tools/BenchmarkConversion/Program.cs`:
- Replace `using System.Drawing;`, `using System.Drawing.Imaging;`, `using System.Runtime.InteropServices;` and `using PaintTranslator.Windows;` with `using PaintTranslator.Imaging;` (keep any other usings).
- Line 42–43: `using Bitmap source = BuildNoisyGradient(options.Width, options.Height); PixelImage sourceFrame = GdiImageAdapter.FromBitmap(source);` → `PixelImage sourceFrame = BuildNoisyGradient(options.Width, options.Height);`
- Line 90: `using Bitmap result = StylePipeline.Render(` → `PixelImage result = StylePipeline.Render(`
- `BuildNoisyGradient`: convert exactly as `StyleTestFixtures.BuildNoisyGradient` in Step 1 (an `int[]` filled in the same loop order, returned through `PixelImage.FromPixels`). Its noise parameters stay whatever the file has today.
- `Checksum(Bitmap bitmap)` → `Checksum(PixelImage image)` with body:

```csharp
            ulong hash = 14695981039346656037UL;
            foreach (int pixel in image.Pixels)
            {
                hash ^= (uint)pixel;
                hash *= 1099511628211UL;
            }

            return hash;
```

(The old loop walked row by row within stride; with stride equal to width the flat walk visits the same pixels in the same order, so checksums are comparable to earlier runs.)

Because it still references `..\..\PaintTranslator.csproj` until Task 7, and a `net10.0` project cannot reference a `net10.0-windows` one, the framework stays `net10.0-windows` with `EnableWindowsTargeting` for now. So the actual edit in this task is: delete only the `<UseWindowsForms>true</UseWindowsForms>` line. Task 7 switches it to plain `net10.0` when the reference moves to Core.

- [ ] **Step 8: Build and run**

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E " error |Build succeeded" | head`
Expected: the only errors are `PngCodec` in `GoldenStyleTests.cs` (Task 4 supplies it). To check everything else compiles, temporarily exclude it: `dotnet build Tests/PaintTranslator.Tests.csproj -p:DefaultItemExcludes="bin/**;obj/**;GoldenStyleTests.cs"` should report `Build succeeded`.

Run: `dotnet test Tests/PaintTranslator.Tests.csproj -p:DefaultItemExcludes="bin/**;obj/**;GoldenStyleTests.cs" --filter "FullyQualifiedName~StylePipelineTests|FullyQualifiedName~StyleBehaviourTests|FullyQualifiedName~PaintabilityFloorTests|FullyQualifiedName~ConverterInvariantTests|FullyQualifiedName~ConverterBlurOrderTests" 2>&1 | tail -3`
Expected: `Passed!` with 0 failures. These classes ran against GDI bitmaps before; identical assertions passing on the Mac is the evidence the seam moved without changing pixels.

- [ ] **Step 9: Stage**

```bash
git add Imaging/StylePipeline.cs Imaging/PalettePhotoConverter.cs MainForm.cs Tools/BenchmarkConversion/ Tests/StyleTestFixtures.cs Tests/ConverterInvariantTests.cs Tests/ConverterBlurOrderTests.cs Tests/PaintabilityFloorTests.cs Tests/StyleBehaviourTests.cs Tests/StylePipelineTests.cs Tests/GoldenStyleTests.cs
```

---

### Task 4: PNG codec for the golden tests, and the Core downsampler for `ConversionPreview`

**Files:**
- Create: `Tests/PngCodec.cs`
- Modify: `Tests/PaintTranslator.Tests.csproj` (add ImageSharp)
- Modify: `Imaging/ConversionPreview.cs`
- Modify: `Tests/ConversionPreviewTests.cs`
- Modify: `MainForm.cs:1054-1055`

**Interfaces:**
- Consumes: `PixelImage`
- Produces:
  ```csharp
  internal static class PngCodec { static PixelImage Load(string path); static void Save(PixelImage image, string path); }
  public static PixelImage ConversionPreview.CreateSource(PixelImage source, int maximumDimension = 384)
  internal static PixelImage ConversionPreview.Downsample(PixelImage source, int width, int height)
  ```

- [ ] **Step 1: Add ImageSharp to the test project**

Run: `dotnet add Tests/PaintTranslator.Tests.csproj package SixLabors.ImageSharp`
Expected: a `<PackageReference Include="SixLabors.ImageSharp" Version="3.x.y" />` appears. Add this comment above it in the csproj:

```xml
    <!-- PNG read/write for the golden and contact-sheet images. System.Drawing.Common
         throws on macOS, and the app never needs a PNG codec of its own (GDI on
         Windows, the browser on the web), so the codec lives here only. -->
```

- [ ] **Step 2: Write the codec**

Create `Tests/PngCodec.cs`:

```csharp
using System.Runtime.InteropServices;
using PaintTranslator.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Reads and writes <see cref="PixelImage"/> as PNG through ImageSharp. Uses the
    /// <see cref="Bgra32"/> pixel type because its little-endian byte order is exactly
    /// a packed <c>0xAARRGGBB</c> int, so the copy is a reinterpretation rather than a
    /// per-channel shuffle that could silently swap red and blue.
    /// </summary>
    internal static class PngCodec
    {
        internal static PixelImage Load(string path)
        {
            using Image<Bgra32> image = Image.Load<Bgra32>(path);
            var pixels = new int[image.Width * image.Height];
            image.CopyPixelDataTo(MemoryMarshal.AsBytes(pixels.AsSpan()));
            return PixelImage.FromPixels(image.Width, image.Height, pixels);
        }

        internal static void Save(PixelImage image, string path)
        {
            using Image<Bgra32> encoded = Image.LoadPixelData<Bgra32>(
                MemoryMarshal.AsBytes(image.Pixels), image.Width, image.Height);
            encoded.SaveAsPng(path);
        }
    }
}
```

- [ ] **Step 3: Run the golden tests**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~GoldenStyleTests" 2>&1 | tail -5`
Expected: `Passed!  - Failed: 0, Passed: 6` (five styles plus the sequence test). If a style reports pixel mismatches, first check the failure's count: a mismatch on *every* pixel means a channel-order problem in `PngCodec`; a handful means the render changed, which must not happen in this plan. Do not regenerate goldens.

- [ ] **Step 4: Write the failing downsampler tests**

Replace `Tests/ConversionPreviewTests.cs` with:

```csharp
using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class ConversionPreviewTests
    {
        [Fact]
        public void SourceIsFitInsideThePreviewBoundWithoutChangingAspectRatio()
        {
            PixelImage source = PixelImage.Filled(800, 600, unchecked((int)0xFF808080));
            PixelImage preview = ConversionPreview.CreateSource(source, 400);
            Assert.Equal(new Size(400, 300), preview.Size);
        }

        [Fact]
        public void SmallSourcesAreNotUpscaled()
        {
            PixelImage source = PixelImage.Filled(100, 50, unchecked((int)0xFF808080));
            PixelImage preview = ConversionPreview.CreateSource(source, 400);
            Assert.Equal(source.Size, preview.Size);
        }

        [Fact]
        public void AFlatImageStaysFlatWhenDownsampled()
        {
            int colour = unchecked((int)0xFF3C78B4);
            PixelImage source = PixelImage.Filled(90, 60, colour);
            PixelImage preview = ConversionPreview.Downsample(source, 27, 18);
            foreach (int pixel in preview.Pixels)
            {
                Assert.Equal(colour, pixel);
            }
        }

        [Fact]
        public void PartialCoverageAveragesTheStraddledPixels()
        {
            // Four source columns [black, black, white, white] into three output
            // columns: the middle output straddles one black and one white pixel
            // with equal weight, so it must be the exact midpoint.
            int black = unchecked((int)0xFF000000);
            int white = unchecked((int)0xFFFFFFFF);
            PixelImage source = PixelImage.FromPixels(4, 1, new[] { black, black, white, white });
            PixelImage preview = ConversionPreview.Downsample(source, 3, 1);

            Assert.Equal(black, preview[0, 0]);
            Assert.Equal(unchecked((int)0xFF808080), preview[1, 0]);
            Assert.Equal(white, preview[2, 0]);
        }

        [Fact]
        public void AlphaIsAveragedLikeAnyOtherChannel()
        {
            int transparent = 0x00000000;
            int opaque = unchecked((int)0xFF000000);
            PixelImage source = PixelImage.FromPixels(2, 1, new[] { transparent, opaque });
            PixelImage preview = ConversionPreview.Downsample(source, 1, 1);
            Assert.Equal(unchecked((int)0x80000000), preview[0, 0]);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(20, 10)]
        [InlineData(1, 1)]
        public void PixelRadiiFollowThePreviewScaleAndKeepZeroAsOff(int sourceRadius, int expected)
        {
            int actual = ConversionPreview.ScaleRadius(
                sourceRadius, new Size(800, 600), new Size(400, 300));
            Assert.Equal(expected, actual);
        }
    }
}
```

- [ ] **Step 5: Run to verify they fail**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~ConversionPreviewTests" 2>&1 | grep -E " error " | head -3`
Expected: `'ConversionPreview' does not contain a definition for 'Downsample'` and a `PixelImage`→`Bitmap` conversion error on `CreateSource`.

- [ ] **Step 6: Implement the downsampler**

Replace the `using` block and `CreateSource` in `Imaging/ConversionPreview.cs` (leave `ScaleRadius` and the class doc comment as they are):

```csharp
using System;
using System.Drawing;
```

```csharp
        /// <summary>The longest edge rendered while a control is being adjusted.</summary>
        public const int MaximumDimension = 384;

        /// <summary>
        /// Returns the source itself when it already fits, since a
        /// <see cref="PixelImage"/> is immutable and sharing it costs nothing.
        /// </summary>
        public static PixelImage CreateSource(PixelImage source, int maximumDimension = MaximumDimension)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (maximumDimension <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumDimension));
            }

            double scale = Math.Min(1.0, maximumDimension / (double)Math.Max(source.Width, source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            if (width == source.Width && height == source.Height)
            {
                return source;
            }

            return Downsample(source, width, height);
        }

        /// <summary>
        /// Area-averaging reduction: each output pixel is the coverage-weighted mean of
        /// the straight (not premultiplied) A, R, G and B of the source pixels under it.
        /// Chosen over GDI's bicubic, which the WinForms app used before, because it
        /// runs identically on every platform, never rings or overshoots, and keeps a
        /// flat region exactly flat. No gamma linearisation, matching the pipeline's
        /// own blur stages so the preview and the full render smooth the same way.
        /// </summary>
        internal static PixelImage Downsample(PixelImage source, int width, int height)
        {
            if (width <= 0 || height <= 0 || width > source.Width || height > source.Height)
            {
                throw new ArgumentOutOfRangeException(width > source.Width || width <= 0 ? nameof(width) : nameof(height));
            }

            double xRatio = source.Width / (double)width;
            double yRatio = source.Height / (double)height;
            var output = new int[width * height];

            for (int oy = 0; oy < height; oy++)
            {
                double top = oy * yRatio;
                double bottom = (oy + 1) * yRatio;
                int firstRow = (int)top;
                int lastRow = Math.Min(source.Height - 1, (int)Math.Ceiling(bottom) - 1);

                for (int ox = 0; ox < width; ox++)
                {
                    double left = ox * xRatio;
                    double right = (ox + 1) * xRatio;
                    int firstColumn = (int)left;
                    int lastColumn = Math.Min(source.Width - 1, (int)Math.Ceiling(right) - 1);

                    double a = 0.0, r = 0.0, g = 0.0, b = 0.0, total = 0.0;
                    for (int sy = firstRow; sy <= lastRow; sy++)
                    {
                        double rowWeight = Math.Min(bottom, sy + 1) - Math.Max(top, sy);
                        for (int sx = firstColumn; sx <= lastColumn; sx++)
                        {
                            double weight = rowWeight * (Math.Min(right, sx + 1) - Math.Max(left, sx));
                            int pixel = source[sx, sy];
                            a += weight * ((pixel >> 24) & 0xFF);
                            r += weight * ((pixel >> 16) & 0xFF);
                            g += weight * ((pixel >> 8) & 0xFF);
                            b += weight * (pixel & 0xFF);
                            total += weight;
                        }
                    }

                    output[(oy * width) + ox] =
                        (Channel(a / total) << 24) | (Channel(r / total) << 16) | (Channel(g / total) << 8) | Channel(b / total);
                }
            }

            return PixelImage.FromPixels(width, height, output);
        }

        /// <summary>
        /// Rounds half away from zero so a 50/50 straddle lands on 128, not on
        /// whichever neighbour banker's rounding happens to prefer.
        /// </summary>
        private static int Channel(double value)
        {
            return Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
        }
```

Remove `using System.Drawing.Drawing2D;` and `using System.Drawing.Imaging;`.

- [ ] **Step 7: Update `MainForm`**

Lines 1053–1055 become:

```csharp
                    PixelImage full = GdiImageAdapter.FromBitmap(photo);
                    PixelImage previewFrame = ConversionPreview.CreateSource(full);
                    return (full, previewFrame);
```

- [ ] **Step 8: Run**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~ConversionPreviewTests" 2>&1 | tail -3`
Expected: `Passed!  - Failed: 0, Passed: 8`.

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E " error |Build succeeded" | head`
Expected: `Build succeeded`.

- [ ] **Step 9: Stage**

```bash
git add Tests/PngCodec.cs Tests/PaintTranslator.Tests.csproj Imaging/ConversionPreview.cs Tests/ConversionPreviewTests.cs MainForm.cs
```

---

### Task 5: `ColorWheelGenerator` returns `PixelImage`; PNG export moves to the app

**Files:**
- Modify: `Imaging/ColorWheelGenerator.cs:27-150,153-215,295-312`
- Create: `Windows/ColorWheelExport.cs`
- Modify: `Program.cs:26`, `MainForm.cs:1154,1163,1281`
- Modify: `Tests/ColorWheelGeneratorMeasuredTests.cs`, `Tests/TraditionalColorWheelTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public static PixelImage ColorWheelGenerator.Create(int diameter)
  public static PixelImage ColorWheelGenerator.Create(int diameter, IReadOnlyList<PigmentCoefficients> paints)
  public static PixelImage ColorWheelGenerator.CreateTraditional(int diameter)
  public static void ColorWheelExport.SaveToFile(string path, int diameter)   // PaintTranslator.Windows
  ```

- [ ] **Step 1: Convert the two colour-wheel test files**

Apply the Task 3 Step 2 substitution table to `Tests/ColorWheelGeneratorMeasuredTests.cs` and `Tests/TraditionalColorWheelTests.cs`. Both keep `using System.Drawing;` because they compare `Color` values. Specific lines:

- `using Bitmap wheel = ColorWheelGenerator.Create(...)` → `PixelImage wheel = ColorWheelGenerator.Create(...)`
- `Color actual = wheel.GetPixel(x, y);` → `Color actual = Color.FromArgb(wheel[x, y]);`
- `Assert.Equal(0, wheel.GetPixel(32, 32).A);` → `Assert.Equal(0, Color.FromArgb(wheel[32, 32]).A);`
- `using (Bitmap warmup = ...) { }` → `_ = ColorWheelGenerator.Create(64, PigmentLibrary.Selectable);` and similarly for the 512 wheel, keeping whatever timing assertion surrounds it.
- `AssertPrimary(Bitmap wheel, ...)` → `AssertPrimary(PixelImage wheel, ...)`

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet build Tests/PaintTranslator.Tests.csproj 2>&1 | grep -E " error " | head -3`
Expected: `cannot convert from 'System.Drawing.Bitmap' to 'PaintTranslator.Imaging.PixelImage'` at the `Create` calls.

- [ ] **Step 3: Rewrite the two generators**

In `Imaging/ColorWheelGenerator.cs`:

`Create(int diameter, IReadOnlyList<PigmentCoefficients> paints)` — replace from `var bitmap = new Bitmap(...)` through `return bitmap;` with:

```csharp
            var pixels = new int[diameter * diameter];

            // With every paint deselected there are no wedges to draw; an all-zero
            // buffer is already fully transparent, so return it as the empty wheel.
            if (paints.Count == 0)
            {
                return PixelImage.FromPixels(diameter, diameter, pixels);
            }

            var baselineAbsorption = new double[SpectralBands.Count];
            var baselineScattering = new double[SpectralBands.Count];
            KubelkaMunk.SumCoefficients(paints, baselineAbsorption, baselineScattering);

            Parallel.For(
                0,
                diameter,
                () => new double[SpectralBands.Count],
                (y, state, reflectance) =>
                {
                    for (int x = 0; x < diameter; x++)
                    {
                        if (!BlendGeometry.TryGetWedge(
                            diameter, paints.Count, x, y,
                            out BlendGeometry.Wedge wedge, out double alpha))
                        {
                            continue;
                        }

                        KubelkaMunk.MixWedge(
                            baselineAbsorption,
                            baselineScattering,
                            wedge.CentreShare,
                            paints[wedge.LowerPaint],
                            wedge.LowerSurplus,
                            paints[wedge.UpperPaint],
                            wedge.UpperSurplus,
                            reflectance);
                        Color colour = SpectralRenderer.ToDisplayColor(reflectance, out _);
                        pixels[(y * diameter) + x] = Pack((byte)(alpha * 255.0), colour);
                    }

                    return reflectance;
                },
                reflectance => { });

            return PixelImage.FromPixels(diameter, diameter, pixels);
```

Keep the existing explanatory comments about the baseline sum and the row-parallel split; only the buffer changes. `(byte)(alpha * 255.0)` is the truncation the old code used; do not "fix" it to a rounding, or wedge edges shift by one alpha step and the measured-wheel tests notice.

`CreateTraditional(int diameter)` — replace from `var bitmap = new Bitmap(...)` through `return bitmap;` with:

```csharp
            var pixels = new int[diameter * diameter];
            double centre = (diameter - 1) * 0.5;
            double radius = diameter * 0.5;

            Parallel.For(0, diameter, y =>
            {
                double dy = y - centre;
                for (int x = 0; x < diameter; x++)
                {
                    double dx = x - centre;
                    double distance = Math.Sqrt((dx * dx) + (dy * dy));
                    double coverage = Math.Clamp(radius + 0.5 - distance, 0.0, 1.0);
                    if (coverage <= 0.0)
                    {
                        continue;
                    }

                    // Zero degrees is red at twelve o'clock and angles advance
                    // clockwise, matching the familiar artist-wheel layout.
                    double artistHue = Math.Atan2(dx, -dy) * (180.0 / Math.PI);
                    if (artistHue < 0.0)
                    {
                        artistHue += 360.0;
                    }

                    double displayHue = TraditionalToDisplayHue(artistHue);
                    double saturation = Math.Min(1.0, distance / radius);
                    Color colour = HsvToColor(displayHue, saturation);
                    pixels[(y * diameter) + x] = Pack((byte)Math.Round(coverage * 255.0), colour);
                }
            });

            return PixelImage.FromPixels(diameter, diameter, pixels);
```

Add one private helper at the bottom of the class:

```csharp
        /// <summary>
        /// Packs a colour with a caller-chosen alpha in <see cref="PixelImage"/> order.
        /// The two wheels compute alpha differently (wedge edge coverage versus disc
        /// edge coverage) so it is a parameter rather than read from the colour.
        /// </summary>
        private static int Pack(byte alpha, Color colour)
        {
            return (alpha << 24) | (colour.R << 16) | (colour.G << 8) | colour.B;
        }
```

Change the three method return types from `Bitmap` to `PixelImage`, delete `SaveToFile` entirely, and remove `using System.Drawing.Imaging;` and `using System.IO;`.

- [ ] **Step 4: Create the export helper**

Create `Windows/ColorWheelExport.cs`:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// Backs the <c>--generate-colorwheel</c> command line flag. Lives in the app
    /// rather than the kernel because writing a PNG needs a codec, and the only one
    /// the desktop build carries is GDI's.
    /// </summary>
    public static class ColorWheelExport
    {
        public static void SaveToFile(string path, int diameter)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using Bitmap wheel = GdiImageAdapter.ToBitmap(ColorWheelGenerator.Create(diameter));
            wheel.Save(path, ImageFormat.Png);
        }
    }
}
```

- [ ] **Step 5: Update the app call sites**

- `Program.cs:26`: `ColorWheelGenerator.SaveToFile(outputPath, 512);` → `PaintTranslator.Windows.ColorWheelExport.SaveToFile(outputPath, 512);`
- `MainForm.cs:1154`: `ColorWheelGenerator.CreateTraditional(512),` → `GdiImageAdapter.ToBitmap(ColorWheelGenerator.CreateTraditional(512)),`
- `MainForm.cs:1163`: `ColorWheelGenerator.Create(512, GetSelectedPaints(null)),` → `GdiImageAdapter.ToBitmap(ColorWheelGenerator.Create(512, GetSelectedPaints(null))),`
- `MainForm.cs:1281`: `SetDisplayedImage(ColorWheelGenerator.Create(512, selected ?? GetSelectedPaints(null)));` → `SetDisplayedImage(GdiImageAdapter.ToBitmap(ColorWheelGenerator.Create(512, selected ?? GetSelectedPaints(null))));`

- [ ] **Step 6: Run**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~ColorWheelGeneratorMeasuredTests|FullyQualifiedName~TraditionalColorWheelTests" 2>&1 | tail -3`
Expected: `Passed!` with 0 failures.

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E " error |Build succeeded" | head`
Expected: `Build succeeded`.

- [ ] **Step 7: Stage**

```bash
git add Imaging/ColorWheelGenerator.cs Windows/ColorWheelExport.cs Program.cs MainForm.cs Tests/ColorWheelGeneratorMeasuredTests.cs Tests/TraditionalColorWheelTests.cs
```

---

### Task 6: Split the grid overlay into geometry (kernel) and drawing (app)

**Files:**
- Create: `Imaging/GridGeometry.cs`
- Create: `Windows/GridOverlayRenderer.cs`
- Delete: `Imaging/GridOverlayRenderer.cs`
- Create: `Tests/GridGeometryTests.cs`
- Modify: `MainForm.cs:1348` (only the `using` changes; the call is identical)

**Interfaces:**
- Produces:
  ```csharp
  namespace PaintTranslator.Imaging
  public static class GridGeometry
  {
      public readonly record struct Segment(PointF Start, PointF End);
      public static IReadOnlyList<Segment> Segments(RectangleF bounds, int columns, int rows);
  }
  namespace PaintTranslator.Windows
  public static class GridOverlayRenderer { public static void DrawGrid(Graphics graphics, RectangleF bounds, int columns, int rows); }
  ```

- [ ] **Step 1: Write the failing test**

Create `Tests/GridGeometryTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class GridGeometryTests
    {
        [Fact]
        public void TwoColumnsOneRowGivesOneDividerAndTheBorder()
        {
            var bounds = new RectangleF(10, 20, 100, 50);
            IReadOnlyList<GridGeometry.Segment> segments = GridGeometry.Segments(bounds, 2, 1);

            Assert.Equal(5, segments.Count);
            Assert.Contains(new GridGeometry.Segment(new PointF(60, 20), new PointF(60, 70)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(10, 20), new PointF(110, 20)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(10, 70), new PointF(110, 70)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(10, 20), new PointF(10, 70)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(110, 20), new PointF(110, 70)), segments);
        }

        [Fact]
        public void DividersAreFractionsOfTheSpanNotAccumulatedSteps()
        {
            var bounds = new RectangleF(0, 0, 10, 10);
            IReadOnlyList<GridGeometry.Segment> segments = GridGeometry.Segments(bounds, 3, 3);
            float[] xs = segments.Where(s => s.Start.X == s.End.X && s.Start.X > 0 && s.Start.X < 10)
                .Select(s => s.Start.X).OrderBy(x => x).ToArray();

            Assert.Equal(new[] { 10f / 3f, 20f / 3f }, xs);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        public void RejectsFewerThanOneSegment(int columns, int rows)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => GridGeometry.Segments(new RectangleF(0, 0, 10, 10), columns, rows));
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~GridGeometryTests" 2>&1 | grep -E " error " | head -2`
Expected: `'GridGeometry' could not be found`.

- [ ] **Step 3: Implement `GridGeometry`**

Create `Imaging/GridGeometry.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Where the grid overlay's lines go, with no drawing. The WinForms app strokes
    /// these with GDI pens and the web canvas will stroke the same list, so the two
    /// surfaces cannot disagree about where a division falls.
    /// </summary>
    public static class GridGeometry
    {
        public readonly record struct Segment(PointF Start, PointF End);

        /// <summary>
        /// Interior dividers first, then the four border edges. Positions are computed
        /// as fractions of the full span rather than by accumulating a step, so
        /// rounding cannot drift across many segments.
        /// </summary>
        public static IReadOnlyList<Segment> Segments(RectangleF bounds, int columns, int rows)
        {
            if (columns < 1 || rows < 1)
            {
                throw new ArgumentOutOfRangeException(columns < 1 ? nameof(columns) : nameof(rows),
                    "Grid must have at least one segment in each direction.");
            }

            var segments = new List<Segment>((columns - 1) + (rows - 1) + 4);
            for (int i = 1; i < columns; i++)
            {
                float x = bounds.Left + bounds.Width * i / columns;
                segments.Add(new Segment(new PointF(x, bounds.Top), new PointF(x, bounds.Bottom)));
            }

            for (int i = 1; i < rows; i++)
            {
                float y = bounds.Top + bounds.Height * i / rows;
                segments.Add(new Segment(new PointF(bounds.Left, y), new PointF(bounds.Right, y)));
            }

            segments.Add(new Segment(new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Top)));
            segments.Add(new Segment(new PointF(bounds.Left, bounds.Bottom), new PointF(bounds.Right, bounds.Bottom)));
            segments.Add(new Segment(new PointF(bounds.Left, bounds.Top), new PointF(bounds.Left, bounds.Bottom)));
            segments.Add(new Segment(new PointF(bounds.Right, bounds.Top), new PointF(bounds.Right, bounds.Bottom)));
            return segments;
        }
    }
}
```

- [ ] **Step 4: Move the drawing half to the app**

```bash
git mv Imaging/GridOverlayRenderer.cs Windows/GridOverlayRenderer.cs
```

Replace its contents:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// Strokes <see cref="GridGeometry"/> onto a GDI surface. Each line is drawn twice,
    /// a wider translucent dark stroke under a thin light one, so the grid stays
    /// visible over both light and dark image areas.
    /// </summary>
    public static class GridOverlayRenderer
    {
        public static void DrawGrid(Graphics graphics, RectangleF bounds, int columns, int rows)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            IReadOnlyList<GridGeometry.Segment> segments = GridGeometry.Segments(bounds, columns, rows);
            using (var underPen = new Pen(Color.FromArgb(150, 0, 0, 0), 3f))
            using (var overPen = new Pen(Color.White, 1f))
            {
                Stroke(graphics, segments, underPen);
                Stroke(graphics, segments, overPen);
            }
        }

        private static void Stroke(Graphics graphics, IReadOnlyList<GridGeometry.Segment> segments, Pen pen)
        {
            foreach (GridGeometry.Segment segment in segments)
            {
                graphics.DrawLine(pen, segment.Start, segment.End);
            }
        }
    }
}
```

`MainForm.cs` already has `using PaintTranslator.Windows;` from Task 2, so its `GridOverlayRenderer.DrawGrid(...)` call at line 1348 resolves unchanged.

- [ ] **Step 5: Run**

Run: `dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~GridGeometryTests" 2>&1 | tail -3`
Expected: `Passed!  - Failed: 0, Passed: 4`.

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E " error |Build succeeded" | head`
Expected: `Build succeeded`.

- [ ] **Step 6: Stage**

```bash
git add Imaging/GridGeometry.cs Windows/GridOverlayRenderer.cs Imaging/GridOverlayRenderer.cs Tests/GridGeometryTests.cs
```

---

### Task 7: Create `PaintTranslator.Core`, move the kernel, split the Windows-only tests

This is the structural task. It is large but has no design decisions left in it.

**Files:**
- Create: `Core/PaintTranslator.Core.csproj`
- Move: `Pigments/**` → `Core/Pigments/**`; `Imaging/**` → `Core/Imaging/**` (except `ImageDecoder.cs`)
- Move: `Imaging/ImageDecoder.cs` → `Windows/ImageDecoder.cs`
- Create: `Tests.Windows/PaintTranslator.Windows.Tests.csproj`
- Move: `Tests/ImageDecoderTests.cs`, `Tests/ImageCanvasTests.cs`, `Tests/UiThemeTests.cs`, `Tests/ContactSheetTests.cs` → `Tests.Windows/`
- Modify: `PaintTranslator.csproj`, `Tests/PaintTranslator.Tests.csproj`, `BlendTests/PaintTranslator.BlendTests.csproj`, `Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj`, `Tools/IngestSpectra/IngestSpectra.csproj`, `PaintTranslator.sln`
- Modify: visibility in `Core/Imaging/StylePipeline.cs`, `Core/Imaging/PalettePhotoConverter.cs`, `Core/Imaging/CandidateSet.cs`, `Core/Imaging/ConversionPreview.cs`, `Core/Imaging/RenderDiagnostics.cs`, `Core/Imaging/Styles/{StyleDefinition,StyleRegistry,StyleParameter,PipelineStages,CandidateSetCache,ColourMapCache}.cs`

**Interfaces:**
- Produces: `PaintTranslator.Core.dll` referenced by the app, tests, BlendTests, benchmarks; `InternalsVisibleTo("PaintTranslator.Tests")` on Core.

- [ ] **Step 1: Create the Core project and move the folders**

```bash
mkdir -p Core
git mv Pigments Core/Pigments
git mv Imaging Core/Imaging
git mv Core/Imaging/ImageDecoder.cs Windows/ImageDecoder.cs
```

Create `Core/PaintTranslator.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>PaintTranslator</RootNamespace>
    <AssemblyName>PaintTranslator.Core</AssemblyName>
    <!-- The kernel must stay free of anything platform-specific: this assembly is what
         the Blazor WebAssembly client compiles to wasm. Colour, Point and Size come
         from System.Drawing.Primitives in the shared framework, which is fine; Bitmap
         and Graphics from System.Drawing.Common are not, and belong in the app. -->
  </PropertyGroup>

  <ItemGroup>
    <!-- The measured Kubelka-Munk coefficients, generated by Tools/IngestSpectra and
         read at startup by PigmentLibrary. -->
    <EmbeddedResource Include="Pigments\PigmentData.bin" />
  </ItemGroup>

  <ItemGroup>
    <!-- The tests assert on internals (the CIELAB helper, the mixture builder, the
         individual stages). The app and the web client use only the public surface. -->
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>PaintTranslator.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

</Project>
```

Check `PigmentLibrary` loads the resource by a name that still resolves: `grep -n "GetManifestResourceStream\|PigmentData.bin" Core/Pigments/PigmentLibrary.cs Core/Pigments/PigmentData.cs`. The manifest name is `<RootNamespace>.Pigments.PigmentData.bin`; `RootNamespace` stays `PaintTranslator`, so it is unchanged. If the code uses `typeof(...).Assembly` it resolves to Core automatically.

- [ ] **Step 2: Point the app at Core**

In `PaintTranslator.csproj`:
- Add `<ProjectReference Include="Core\PaintTranslator.Core.csproj" />` in a new `<ItemGroup>`.
- Delete the `<EmbeddedResource Include="Pigments\PigmentData.bin" />` item group.
- Delete the `InternalsVisibleTo` item group (both entries).
- In the exclusions item group add `Core\**` and `Tests.Windows\**` to each of the three `Remove` patterns, e.g. `<Compile Remove="Core\**" />`, `<EmbeddedResource Remove="Core\**" />`, `<None Remove="Core\**" />` and the same for `Tests.Windows\**`.

`Windows/ImageDecoder.cs`: change its namespace from `PaintTranslator.Imaging` to `PaintTranslator.Windows`. Update `MainForm.cs` and `Input/ImageDataObjectReader.cs` if they lack `using PaintTranslator.Windows;` (MainForm has it since Task 2; add it to `Input/ImageDataObjectReader.cs`).

- [ ] **Step 3: Make the consumer-facing kernel types public**

Change `internal` to `public` on these declarations (and only these):

| File | Declarations |
|---|---|
| `Core/Imaging/StylePipeline.cs` | `class StylePipeline`; methods `Render`, `PrepareCandidates`, `DefaultValues`, `SnapshotValues` |
| `Core/Imaging/PalettePhotoConverter.cs` | the `Convert` overload taking `StyleDefinition`; `ComposeWithBlur`; `RgbToLab` |
| `Core/Imaging/CandidateSet.cs` | `class CandidateSet` |
| `Core/Imaging/ConversionPreview.cs` | `class ConversionPreview` |
| `Core/Imaging/RenderDiagnostics.cs` | `class RenderDiagnostics`, `struct RenderPhaseTiming` |
| `Core/Imaging/Styles/StyleDefinition.cs` | `record StyleDefinition` |
| `Core/Imaging/Styles/StyleRegistry.cs` | `class StyleRegistry` |
| `Core/Imaging/Styles/StyleParameter.cs` | `record StyleParameter`, `class ParameterValues` |
| `Core/Imaging/Styles/PipelineStages.cs` | every interface in the file |
| `Core/Imaging/Styles/CandidateSetCache.cs` | `class CandidateSetCache` |
| `Core/Imaging/Styles/ColourMapCache.cs` | `class ColourMapCache` |

Stage implementations (`OptionalBlur`, `ContourLines`, …), `MixtureBuilder`, `RegionLabeler`, `LinearPlanes`, `ColorQuantization` and `ImageBufferPool` stay internal.

Then update the remark on `PalettePhotoConverter.Convert` (line ~140, "Internal rather than public: StyleDefinition is itself internal…") to: "Public because the WinForms app and the web client live in other assemblies; the kernel's own stage implementations stay internal since no consumer constructs them directly."

- [ ] **Step 4: Build the app and fix any remaining inaccessible member**

Run: `dotnet build PaintTranslator.csproj 2>&1 | grep -E "CS0122|CS0051|CS0053| error " | head -20`
Expected: ideally `Build succeeded`. Each `CS0122` ("inaccessible due to its protection level") names one more member the app uses; make that member `public` and rebuild. `CS0051`/`CS0053` mean a public signature exposes an internal type: make that type public too. Record every addition beyond the table above in the handoff task doc.

- [ ] **Step 5: Split the test projects**

```bash
mkdir -p Tests.Windows
git mv Tests/ImageDecoderTests.cs Tests.Windows/ImageDecoderTests.cs
git mv Tests/ImageCanvasTests.cs Tests.Windows/ImageCanvasTests.cs
git mv Tests/UiThemeTests.cs Tests.Windows/UiThemeTests.cs
git mv Tests/ContactSheetTests.cs Tests.Windows/ContactSheetTests.cs
```

Replace `Tests/PaintTranslator.Tests.csproj` entirely:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <RootNamespace>PaintTranslator.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <!-- The parity gate compares this project's kernel against Unicolour's independent
         implementation. Referenced here explicitly because the application no longer
         depends on Unicolour, and the gate must outlive that removal. -->
    <PackageReference Include="Wacton.Unicolour" Version="8.0.0" />
    <PackageReference Include="Wacton.Unicolour.Datasets" Version="5.0.0" />
    <!-- PNG read/write for the golden images. System.Drawing.Common throws on macOS,
         and the app never needs a PNG codec of its own (GDI on Windows, the browser on
         the web), so the codec lives here only. -->
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.7" />  <!-- or whatever Task 4 resolved -->
    <!-- TestImages encodes sample files in every format the sniffer must recognise.
         The AnyCPU package carries native codecs for macOS arm64 as well as Windows;
         the app itself pins the smaller x64 package. -->
    <PackageReference Include="Magick.NET-Q8-AnyCPU" Version="14.15.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Core\PaintTranslator.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- The derivation is tested directly rather than through its output, so it is
         compiled in here. SpreadsheetReader comes with it because GoldenSpectraSource
         calls it; linking one without the other does not compile. -->
    <Compile Include="..\Tools\IngestSpectra\GoldenSpectraSource.cs" Link="Ingest\GoldenSpectraSource.cs" />
    <Compile Include="..\Tools\IngestSpectra\SpreadsheetReader.cs" Link="Ingest\SpreadsheetReader.cs" />
  </ItemGroup>

  <ItemGroup>
    <!-- HEIC is the one format the tests cannot generate: ImageMagick ships an HEVC
         decoder but no encoder. -->
    <None Update="Assets\**" CopyToOutputDirectory="PreserveNewest" />
    <!-- GoldenStyleTests compares against these at run time, so they must land
         beside the test assembly. -->
    <None Update="Golden\**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

If `Magick.NET-Q8-AnyCPU` 14.15.0 does not restore, run `dotnet add Tests/PaintTranslator.Tests.csproj package Magick.NET-Q8-AnyCPU` for the current version. If `ImageFormatSnifferTests` then fails at runtime with a native-library load error on this Mac, move `ImageFormatSnifferTests.cs` and `TestImages.cs` to `Tests.Windows` as well, drop the Magick reference here, and record it in the handoff doc.

Create `Tests.Windows/PaintTranslator.Windows.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <!-- These tests need GDI (ImageDecoder, the contact sheet's text), WinForms
         controls (ImageCanvas, UiTheme) or both. They compile on macOS through
         EnableWindowsTargeting and run only on Windows. -->
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <IsPackable>false</IsPackable>
    <RootNamespace>PaintTranslator.Tests</RootNamespace>
    <AssemblyName>PaintTranslator.Windows.Tests</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\PaintTranslator.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Shared with the cross-platform tests; ImageDecoderTests needs the same
         encoders the sniffer tests use. -->
    <Compile Include="..\Tests\TestImages.cs" Link="TestImages.cs" />
    <None Include="..\Tests\Assets\**" Link="Assets\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

`ContactSheetTests.cs` uses `KubelkaMunk` and friends from Core (transitively referenced) and GDI for drawing; it needs no code change beyond confirming its `RepositoryRoot()` helper still finds the repo root from the new output directory (it walks up looking for `PaintTranslator.sln`; check with `grep -n "RepositoryRoot" -A8 Tests.Windows/ContactSheetTests.cs`).

`ImageDecoderTests.cs`: add `using PaintTranslator.Windows;` for the moved `ImageDecoder`.

- [ ] **Step 6: Retarget BlendTests, Benchmarks, IngestSpectra to Core**

`BlendTests/PaintTranslator.BlendTests.csproj`: no change needed; it references the app, which references Core.

`Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj`: set `<TargetFramework>net10.0</TargetFramework>`, remove `EnableWindowsTargeting`, and change the project reference to `..\..\Core\PaintTranslator.Core.csproj`.

`Tools/IngestSpectra/IngestSpectra.csproj`: change the seven `Compile Include="..\..\Pigments\...` links to `..\..\Core\Pigments\...`.

- [ ] **Step 7: Register the projects in the solution**

```bash
dotnet sln PaintTranslator.sln add Core/PaintTranslator.Core.csproj
dotnet sln PaintTranslator.sln add --solution-folder Tests Tests.Windows/PaintTranslator.Windows.Tests.csproj
dotnet sln PaintTranslator.sln list
```

Expected: nine entries including `Core\PaintTranslator.Core.csproj` and `Tests.Windows\PaintTranslator.Windows.Tests.csproj`.

- [ ] **Step 8: Build everything and run the Mac suite**

Run: `dotnet build PaintTranslator.sln 2>&1 | grep -E " error |warning NETSDK|Build succeeded" | head`
Expected: `Build succeeded`, 0 errors, no `NETSDK1138`.

Run: `dotnet test Tests/PaintTranslator.Tests.csproj 2>&1 | tail -4`
Expected: `Passed!  - Failed: 0` and a total near 320 (317 original, minus 12 moved to Windows, plus 5 `PixelImage`, 2 new preview, 4 grid, minus the 2 removed `SourceFrame` tests, plus theory rows). Record the exact number.

Run: `dotnet build Tests.Windows/PaintTranslator.Windows.Tests.csproj 2>&1 | grep -E " error |Build succeeded"`
Expected: `Build succeeded`. Do **not** run it on the Mac; note "compiled, not run" in the handoff doc.

Run: `dotnet run --project Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj -- --iterations 1 2>&1 | head -8` (check `Tools/BenchmarkConversion/README.md` for the real flag names first).
Expected: one timing block per style with a checksum, proving the kernel runs natively on the Mac outside the test host.

- [ ] **Step 9: Stage**

```bash
git add -A Core Windows Tests Tests.Windows PaintTranslator.csproj PaintTranslator.sln BlendTests Tools Input MainForm.cs
git status --short | head -40
```

Expected: renames show as `R`, not as delete-plus-add, for every moved file.

---

### Task 8: Documentation and handoff

**Files:**
- Modify: `CLAUDE.md` (Commands, Architecture, Tests sections)
- Modify: `docs/superpowers/specs/2026-09-01-core-extraction-design.md` (status line)
- Modify: `.claude/handoff/PROJECT.md`

- [ ] **Step 1: Update `CLAUDE.md`**

In **Commands**, replace the PowerShell block and the sentence about `NETSDK1138` with:

```
dotnet build PaintTranslator.sln                      # builds all projects, on macOS too
dotnet test Tests/PaintTranslator.Tests.csproj        # the cross-platform suite
dotnet test Tests.Windows/PaintTranslator.Windows.Tests.csproj   # 12 GDI/WinForms tests, Windows only
dotnet run --project PaintTranslator.csproj           # the WinForms app, Windows only
```

and: "A clean build is 0 errors. The Windows-only projects compile on macOS through `EnableWindowsTargeting` but cannot run there."

In **Architecture**, after the first paragraph add:

"The kernel lives in `Core/` (`PaintTranslator.Core`, `net10.0`, no Windows dependencies). The WinForms app at the root is a thin consumer: `Windows/GdiImageAdapter` converts between `Bitmap` and Core's `PixelImage`, `Windows/ImageDecoder` wraps GDI and Magick.NET, and `Windows/GridOverlayRenderer` strokes `GridGeometry`. Nothing under `Core/` may reference `System.Drawing.Common`; `System.Drawing.Primitives` (`Color`, `Point`, `Size`) is fine."

Update the pipeline line to `Measured spectra → Tools/IngestSpectra → Core/Pigments/PigmentData.bin …`.

In **Tests**, replace "317 tests, ~13s" wherever it appears with the number recorded in Task 7 Step 8, and add: "Golden PNGs are read through `Tests/PngCodec.cs` (ImageSharp). The Windows-only project holds `ImageDecoderTests`, `ImageCanvasTests`, `UiThemeTests` and `ContactSheetTests`."

Update the `RID` bullet under **Conventions**: it now applies to the app project only.

- [ ] **Step 2: Mark the spec as implemented**

Change the spec's `**Status:**` line to `implemented <today's date>; WinForms run and Windows tests pending verification on the owner's PC`.

- [ ] **Step 3: Update the handoff doc**

In `.claude/handoff/PROJECT.md`: mark task 3 done, set **Next step** to "Owner verifies on the PC: `dotnet test Tests.Windows/...` and launching the app. Then sub-project 2 (Blazor), which starts with a WASM performance spike.", and list under **Open problems** the exact test count and anything Task 7 Step 4 had to make public beyond the table.

- [ ] **Step 4: Stage**

```bash
git add CLAUDE.md docs/superpowers/
git status --short
```

Everything is staged and nothing is committed. Tell the owner the working tree is ready for review.
