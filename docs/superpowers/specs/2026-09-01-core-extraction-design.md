# Core extraction: design

**Date:** 2026-09-01
**Status:** implemented 2026-09-01; WinForms run and Windows tests pending verification on the owner's PC
**Sub-project:** 1 of 3 in the move from WinForms to a web app

## Why

The owner has moved from a Windows PC to a Mac and wants PaintTranslator to run
locally on the Mac and, later, as a public website. The chosen architecture is a
Blazor WebAssembly app with all computation in the browser, deployed as static files
(see "The larger plan" at the end). Everything in that architecture depends on the
physics and imaging code compiling without Windows, which it cannot today: the
solution targets `net5.0-windows`, the imaging code takes and returns
`System.Drawing.Bitmap`, and `System.Drawing.Common` throws on every platform but
Windows since .NET 7.

This sub-project moves the kernel into a platform-neutral class library, retargets the
whole solution to .NET 10, and gets the test suite running on the Mac. It ships no
web code. Its value on its own is that the 317-case suite, including the Unicolour
parity gate, becomes runnable on the owner's machine.

## Non-goals

- No Blazor, no browser image decoding, no browser palette storage, no deployment.
  Those are sub-projects 2 and 3.
- No change to any colour, mixing, or mapping behaviour. The one place output may
  differ is the interactive preview downsampler (see "Downsampling").
- No refactoring beyond what the seam demands. `MainForm.cs` stays 1,780 lines.
- The WinForms app is kept, not retired. Retirement is a separate decision once the
  web app is tested.

## Solution layout

| Project | Target | Contents |
|---|---|---|
| `Core/PaintTranslator.Core.csproj` **(new)** | `net10.0` | `Pigments/*`, `Imaging/*`, `Imaging/Styles/**`, `PigmentData.bin` as an embedded resource. No runtime identifier, no Windows references, no Magick.NET. |
| `PaintTranslator.csproj` (existing app) | `net10.0-windows`, `win-x64` | `MainForm`, `PaletteEditorForm`, `UiTheme`, `Program`, `Controls/*`, `Input/*`, `Data/UserPaletteStore`, `ImageDecoder` (GDI + Magick.NET), grid-line drawing, and a new `GdiImageAdapter`. Sets `EnableWindowsTargeting` so it compiles on macOS; it runs only on Windows. |
| `Tests/PaintTranslator.Tests.csproj` | `net10.0` | Every test that does not need GDI or WinForms. References Core. Runs on macOS. |
| `Tests.Windows/PaintTranslator.Windows.Tests.csproj` **(new)** | `net10.0-windows` | The 12 test methods that need GDI or WinForms: `ImageDecoderTests` (8), `ImageCanvasTests` (1), `UiThemeTests` (2) and `ContactSheetTests` (1, draws text with GDI fonts). References the app. Compiles on macOS via `EnableWindowsTargeting`, runs only on Windows. |
| `BlendTests/PaintTranslator.BlendTests.csproj` | `net10.0-windows` | Unchanged in purpose. References the app for `GdiImageAdapter` and Core for the kernel. |
| `Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj` | `net10.0` | Drops WinForms. Builds its noisy-gradient input as a `PixelImage` and checksums the `PixelImage` result. Runs on macOS. |
| `Tools/IngestSpectra/IngestSpectra.csproj` | `net10.0` | Retarget only. |

Folder moves: `Pigments/` and `Imaging/` move under `Core/`. The GDI-bound code
that stays with the app (`ImageDecoder.cs`, and the `Graphics` half of
`GridOverlayRenderer.cs`) moves to a `Windows/` folder in the app project so the
app's own `Imaging/` folder disappears and nothing has to be excluded from Core's
globs. Namespaces stay `PaintTranslator.Pigments` and `PaintTranslator.Imaging` so
the diff in consumers is `using` lines only.

The `PaintTranslator.csproj` glob exclusions for `Core/**`, `Tests.Windows/**` and
the existing ones are kept, since the app sits at the repository root.

## The seam: `PixelImage`

Today the kernel's real work happens on packed `int[]` ARGB buffers. `SourceFrame`
is already an immutable width/height/`int[]` snapshot; `Bitmap` appears only at its
two ends (`Create(Bitmap)`, `CreateBitmap`). The design promotes that snapshot into
Core's single image type and moves the two ends into the app.

```csharp
namespace PaintTranslator.Imaging
{
    /// Immutable. Packed ARGB, one int per pixel, row-major, no stride padding.
    /// Byte order matches GDI's Format32bppArgb so the Windows adapter is a
    /// straight memory copy and the kernel never reorders channels.
    public sealed class PixelImage
    {
        public int Width { get; }
        public int Height { get; }
        public Size Size { get; }
        public ReadOnlySpan<int> Pixels { get; }
        public int this[int x, int y] { get; }
        public int AlphaAt(int index);
        public int[] CopyPixels();
        public static PixelImage FromPixels(int width, int height, int[] pixels); // takes ownership
        public static PixelImage Filled(int width, int height, int argb);
    }
}
```

- `SourceFrame` is renamed to `PixelImage` and its two GDI members are removed.
  There is one buffer type, not an input type and an output type.
- `StylePipeline.Render`, `PalettePhotoConverter.Convert`,
  `ColorWheelGenerator.Create`, `CreateTraditional`, and `ConversionPreview.CreateSource`
  return `PixelImage`. `StylePipeline.Render`'s `Bitmap` overload is deleted; callers
  build a `PixelImage` first.
- `System.Drawing.Color`, `Point`, `PointF`, `Size`, `Rectangle`, `RectangleF` stay.
  They live in `System.Drawing.Primitives`, which is part of the shared framework on
  every platform including Blazor WebAssembly. `GamutMapper`, `SpectralRenderer`,
  `PaintBlendMatcher`, `ImageViewport`, and `ConversionPreview.ScaleRadius` do not
  change.
- `GdiImageAdapter` (app project, `PaintTranslator.Windows` namespace) owns the two
  conversions: `PixelImage FromBitmap(Bitmap)` (normalising through a 32bppArgb
  `Graphics.DrawImage` exactly as `SourceFrame.Create` does today) and
  `Bitmap ToBitmap(PixelImage)`.
- `GridOverlayRenderer` splits. Core keeps a `GridGeometry` that yields the line
  segments for a bounds rectangle, columns and rows; the app keeps a `GridOverlayRenderer`
  that draws them with a `Pen`. The web canvas will draw from the same geometry.

### Downsampling

`ConversionPreview.CreateSource` shrinks the source to 384 px on its longest edge
using GDI's `HighQualityBicubic`. Core cannot call GDI, so it gets its own resampler:
area averaging. Each output pixel is the mean of the straight (not premultiplied)
A, R, G and B channels of the source pixels that map into it, with fractional edge
coverage weighted. No gamma linearisation, which matches how the pipeline's existing
blur stages average and keeps the invariant that a flat image stays flat. Upscaling
never happens here (`scale` is clamped to 1.0), so no interpolation kernel is needed.

This is the only place output pixels can differ from the current app, and only for
the interactive preview, never the full render or the golden tests (which never
resample). Both the WinForms app and the future web app use Core's resampler, so
previews are identical across platforms from this point on.

### Visibility

The types the UI drives are public in Core: `StyleDefinition`, `StyleRegistry`,
`StylePipeline` (`Render`, `PrepareCandidates`, `DefaultValues`), `IPipelineStage`,
`ParameterValues`, `StyleParameter`, `CandidateSet`, `CandidateSetCache`,
`ColourMapCache`, `RenderDiagnostics`, `ConversionPreview`, `PixelImage`. Today they
are internal with `InternalsVisibleTo` standing in for an API surface; with a second
consumer arriving in sub-project 2 that would mean four grants. `InternalsVisibleTo`
is kept for `PaintTranslator.Tests` only, for the CIELAB helper the tests measure.

Anything not needed by a consumer stays internal. This is a visibility change, not a
redesign of the API.

## Tests

- All existing assertions are preserved. Edits are mechanical: `Bitmap` becomes
  `PixelImage` in fixtures and call sites; `using` lines change.
- PNG read and write in `GoldenStyleTests` go through **SixLabors.ImageSharp**,
  referenced by the test project only. The app never needs a PNG codec: GDI has one
  on Windows and the browser has one in sub-project 2. The five golden PNGs are read
  pixel-for-pixel into `PixelImage`; they do not need regenerating because their
  inputs are built procedurally by `StyleTestFixtures`. `ContactSheetTests` draws
  labels with GDI fonts, so it moves to the Windows project instead.
- `ImageFormatSnifferTests` builds its sample bytes with Magick.NET via `TestImages`.
  The test project references **Magick.NET-Q8-AnyCPU**, which carries macOS arm64
  native codecs, so those four tests still run on the Mac. The app keeps the smaller
  x64 package.
- `UnicolourParityTests` and `MixingInvariantTests` run unchanged. They are the
  evidence that the kernel survived the move.
- The twelve Windows-bound tests move to `Tests.Windows` and are reported as
  "compiled, not run on this machine" until the owner runs them on the PC.
- New tests for `PixelImage` and the downsampler:
  - `FromPixels` rejects a buffer whose length is not `width * height`.
  - A flat-colour image downsamples to the same colour at every output pixel.
  - A left-half-black, right-half-white image downsampled by 2 has exactly the
    expected mid-grey column where the halves meet and pure black/white elsewhere.
  - `ScaleRadius` behaviour is already tested and unchanged.
- Test packages move to current xUnit (`xunit` 2.9.x, `xunit.runner.visualstudio`
  3.x, `Microsoft.NET.Test.Sdk` 17.13+). The old pins existed because those versions
  mis-resolved on .NET 5.

## Verification

Done means, on the Mac:

```
dotnet build PaintTranslator.sln      # 0 errors; NETSDK1138 warnings gone
dotnet test Tests/PaintTranslator.Tests.csproj   # all green, ~315 cases
dotnet run --project Tools/BenchmarkConversion/PaintTranslator.Benchmarks.csproj
```

The WinForms app launching and the twelve Windows tests passing can only be confirmed
on the PC. The handoff doc records them as unverified rather than claiming them.

## Risks

- **Retargeting .NET 5 to .NET 10** may surface unrelated compiler and analyzer
  changes (nullable warnings, obsolete APIs, `Parallel.For` overload resolution).
  Fix what blocks the build; do not chase warnings that were already present.
- **`EnableWindowsTargeting` on macOS** downloads the WindowsDesktop reference pack.
  If the pack is unavailable offline the app project fails to restore; the fix is
  running restore once online.
- **Test adapter.** If `dotnet test` reports zero tests, the adapter did not load;
  check the `xunit.runner.visualstudio` version before anything else.
- **Magick.NET on .NET 10.** `Magick.NET-Q8-x64` 14.x targets `net8.0` and should run
  on 10; if the package manifest rejects the target, bump to the current 14.x.
- **WASM performance is out of scope here** but is the first thing sub-project 2
  must spike, before any UI: the kernel uses `Parallel.For` throughout, and WASM is
  single-threaded unless `WasmEnableThreads` is turned on or the work is offloaded.

## The larger plan

1. **Core extraction** (this document).
2. **Blazor WebAssembly app with full parity**: image load via file, paste and
   drag-drop; paint checklist; palette editor; styles with per-stage sliders; blur and
   mark sliders; grid overlay; magnifier; hover recipe tooltip; colour wheels.
   Browser-native decoding replaces Magick.NET; palette persists in browser storage.
   Begins with a performance spike.
3. **Deployment**: static build, one-command deploy, and a Dockerfile so the same
   files can be served from a rented server later.

The WinForms app is retired only after 2 is tested by the owner.
