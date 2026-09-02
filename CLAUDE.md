# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Git workflow

- **Work in the main checkout, on `main`**, unless told otherwise in that request.
- **Never create a git worktree, and never work inside one.** Not for isolation, not for a
  long-running task, not because a skill or a plan suggests one. If a worktree already
  exists, do not use it — say so and work in the main checkout instead. Worktrees have cost
  real time here: research written into one sat invisible to the main checkout, a branch was
  reset out from under committed work, and the handoff notes in each copy drifted apart.
  Isolation is not worth a second copy of the tree the owner has to remember to look in.
- **Don't create branches on your own initiative either.** Work parked on a side branch is
  work the owner has to go find.
- **Never commit.** Stage with `git add` and stop there. Every change is reviewed in the
  working tree before it enters history, so a passing test run, a finished task, or a tidy
  stopping point is not permission to commit.

## Check the research docs before planning

`docs/research/` holds substantial prior research. Read the relevant index before
designing or planning any change to colour matching, paint mixing, or image conversion —
several obvious-looking approaches have already been investigated and ruled out there, with
sources.

| Index | Covers |
|---|---|
| [docs/research/README.md](docs/research/README.md) | Physical accuracy: Kubelka-Munk mixing, the photo→paint pipeline, real acrylic behaviour, prior art. Includes an outstanding-work list. |
| [docs/research/painting-style/README.md](docs/research/painting-style/README.md) | Artistic style: colour theory in practice, styles and movements, brushwork and edges, what makes a painting appealing. Includes a "what not to build" list. |

Both synthesis READMEs carry accuracy warnings and verification debts. Claims in the
reports are marked `[verified]`, `[relayed]` or `[inferred]` — preserve that convention
when adding to them, and check the warnings before quoting a figure.

## Commands

```
dotnet build PaintTranslator.sln                      # builds all projects, on macOS too
dotnet test Tests/PaintTranslator.Tests.csproj        # the cross-platform suite
dotnet test Tests.Windows/PaintTranslator.Windows.Tests.csproj   # 12 GDI/WinForms tests, Windows only
dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj   # bUnit + session tests, cross-platform
dotnet run --project PaintTranslator.csproj           # the WinForms app, Windows only
dotnet run --project Web/PaintTranslator.Web.csproj      # the web app, Debug, dev server
./PaintTranslator.command                                # Release publish + local serve + open browser
Tools/BuildDecoders/build.sh                             # regenerates Web/wwwroot/js/decoders (needs Node; offline after first install)
Tools/Deploy/deploy.sh                                   # Docker: clean static site in deploy/site + painttranslator:latest (Caddy) image
```

If macOS refuses to open `PaintTranslator.command` from Finder (unidentified developer),
run `xattr -d com.apple.quarantine PaintTranslator.command` once; that clears the
quarantine flag Gatekeeper sets on files it hasn't seen before, and double-clicking works
from then on.

Run a single test or class with a filter:

```
dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~UnicolourParityTests"
dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~ZeroRadiusLeavesEveryPixelUntouched"
```

A clean build is 0 errors. The only expected warning is a Six Labors ImageSharp
licence notice from the test project (ImageSharp 4.x prints it at build time; it is a
test-only dependency) — a new warning beyond that one is a regression. The
Windows-only projects compile on macOS through `EnableWindowsTargeting` but cannot run
there.

Two auxiliary executables:

```
dotnet run --project BlendTests/PaintTranslator.BlendTests.csproj   # visual gradient-strip harness
dotnet run --project Tools/IngestSpectra/IngestSpectra.csproj       # regenerates Pigments/PigmentData.bin
```

The ingest needs `Tools/IngestSpectra/data/GoldenSpectra.zip`, which is gitignored. It
runs offline and is not part of the build; run it only when coefficients change, and
review the manifest it emits alongside the binary.

## Architecture

A WinForms app (`net10.0-windows`, pinned to `win-x64`) that converts photos into the
colours a chosen set of real acrylic paints can actually be mixed to. The physics is real
measured data, not a colour-space approximation.

The kernel lives in `Core/` (`PaintTranslator.Core`, `net10.0`, no Windows dependencies).
The WinForms app at the root is a thin consumer: `Windows/GdiImageAdapter` converts
between `Bitmap` and Core's `PixelImage`, `Windows/ImageDecoder` wraps GDI and Magick.NET,
and `Windows/GridOverlayRenderer` strokes `GridGeometry`. Nothing under `Core/` may
reference `System.Drawing.Common`; `System.Drawing.Primitives` (`Color`, `Point`, `Size`)
is fine.

### Web app

`Web/` is the Blazor WebAssembly consumer of Core, standalone and statically hosted, kept
at feature parity with the WinForms app rather than as a subset. `Web/Session` is the
UI-neutral port of `MainForm` — scheduler, session, formatter, codec — with no Blazor or
browser types in it, so it is covered by ordinary xUnit tests instead of bUnit.
`wwwroot/js/interop.js` is decision-free glue: it marshals canvas pixels and file bytes
across the JS boundary and makes no choices `Web/Session` doesn't already make. The image
decoders for formats the browser can't read natively (HEIC, PSD, TIFF) are vendored under
`wwwroot/js/decoders/`, each under its own licence recorded in
`wwwroot/js/decoders/LICENSES.md`; `Tools/BuildDecoders/build.sh` regenerates that bundle
from `package.json` and needs Node only for that regeneration, not to run the app. The
threading configuration (`WasmEnableThreads` in the csproj) and the perf-spike numbers
that chose it are in `docs/superpowers/specs/2026-09-01-blazor-app-design.md`; the current
setting is `false` (approach A, AOT without threads), so switching it to `true` needs a
clean `Web/obj/Release` before the next publish or the relinked runtime silently keeps the
old thread configuration. The csproj also sets `OverrideHtmlAssetPlaceholders=true`, so a
static publish resolves the boot-script placeholder in `index.html` and fingerprints every
`_framework/*` file (`dotnet.<hash>.js`, `dotnet.native.<hash>.wasm`, and so on) — nothing
in a curl check, a doc, or a script may hard-code a literal `_framework/dotnet.js` path;
glob for it (a .NET 10 publish emits no `blazor.boot.json`; the boot manifest is embedded
in the fingerprinted `dotnet.<hash>.js`).

`PaintTranslator.command` is the Mac launcher: it publishes Release (AOT; a Debug/interpreted
build is for UI work only, not for judging performance), then serves `wwwroot` with
`Web/serve.py` — a small `http.server` subclass that adds the WASM/brotli MIME types, brotli
negotiation, the cross-origin isolation headers `WasmEnableThreads=true` would need, and a
single-page-app fallback that serves `index.html` for extensionless paths matching no file
(client-side routes like `/bench`), while a missing file that has an extension still 404s so
a broken asset link stays visible. `Web/bin/publish` is the launcher's own output directory,
git-ignored via the top-level `bin/` rule. Two routes exist for diagnostics rather than
end-user use: `/bench` is the spike harness (`?autorun=N` runs N iterations headlessly), and
`?autofile=<path>` on the main page loads a file from the site root through the normal
pipeline and logs `HOST TITLE`/`HOST ERROR` lines to the console, which is how headless
Chrome verifies a build without a human clicking through it.

Deployment is host-neutral and lives in `Tools/Deploy/`: a three-stage `Dockerfile`
(SDK + `wasm-tools` publish → `scratch` export → `caddy:2-alpine`), the `Caddyfile` that
does what `Web/serve.py` does for the launcher plus the caching split (`/_framework/*`
immutable for a year because every file there is fingerprinted; everything else
`no-cache`), and `deploy.sh`, which exports the `site` stage to `deploy/site/` (deleted
first, so it is always clean, and git-ignored) and tags the `serve` stage
`painttranslator:latest`. The publish runs inside Docker so the folder and the image
come from one AOT compile and the Mac needs Docker but not `wasm-tools` to deploy;
`Tools/Deploy/README.md` covers uploading the folder, running the image and getting
HTTPS from Caddy on a rented server. The Credits dialog (`Web/Components/CreditsDialog.razor`)
lists the vendored decoders from `Web/Session/VendoredLibrary.cs`, and `CreditsTests`
asserts that list matches `wwwroot/js/decoders/LICENSES.md`, so a decoder bump must touch both.

### The spectral pipeline

Measured spectra → `Tools/IngestSpectra` → `Core/Pigments/PigmentData.bin` (embedded
resource) → `PigmentLibrary` → `KubelkaMunk` → `SpectralRenderer` → `ColorSpace` (CIELAB).

Everything downstream depends on properties established upstream, so read the doc comments
before changing any of it. Load-bearing decisions:

- **38 bands, 380–750nm at 10nm** (`SpectralBands`). One fixed layout; anything measured
  on a different grid is resampled during ingest. D65 illuminant, because the app shows
  paint on a screen.
- **Two-constant Kubelka-Munk.** Absorption and scattering are tracked separately per
  wavelength. Single-constant theory would assume every pigment scatters identically,
  which is what makes reconstructed-spectrum mixers overweight white.
- **`PigmentCoefficients` deliberately stores no colour.** Appearance is computed from the
  K/S curves at a concentration, so a mass tone and its tint are the same data evaluated
  twice rather than two values that can disagree. Storing a colour here caused the bug
  this pipeline replaced.
- **Tinting strength is not a stored number** — it emerges from absorption relative to
  scattering.
- **Provenance tiers gate the picker.** Only `TwoConstantMeasured` paints reach
  `PigmentLibrary.Selectable`; `ReflectanceDerived` paints assume white-like scattering and
  are withheld from the user while remaining in `All`.

### Two consumers of the achievable gamut

Both answer "what mixture is closest to this colour", and they do not use the same metric:

| | `Imaging/PaintBlendMatcher` | `Imaging/PalettePhotoConverter` |
|---|---|---|
| Drives | the hover tooltip, one colour | the converted image, every pixel |
| Method | enumerates subsets of ≤3 paints, solves each via `Pigments/SubsetSolver` | pre-samples the whole gamut, then nearest-neighbour over a 3-D grid index |
| Metric | weighted HyAB, `LightnessWeight` 1.5 | plain squared CIELAB |

**That metric divergence is known, deliberate so far, and documented** — the two surfaces
can disagree about the best mixture for the same colour. Don't "fix" it incidentally; see
the research docs.

### The converter's invariant

`PalettePhotoConverter` guarantees every output pixel is a colour the selected paints can
genuinely be mixed to. The operative rule is **whether an operation can synthesise a colour
outside the candidate set** — which is narrower than the "blur before mapping" wording
currently in the class doc comment. Pre-map operations are always safe; so are post-map
*selection-only* ones. Post-map arithmetic (averaging, anti-aliasing, filtered downsampling)
breaks it, though re-running the mapping repairs it cheaply. The four-category table is in
`docs/research/painting-style/README.md`.

Pixels are cached per colour quantised to 6 bits per channel. Any per-pixel operation that
depends on *position* rather than colour breaks that cache and needs the key extended.

## Tests

xUnit. `Tests/` targets `net10.0` and runs cross-platform; `Tests.Windows/` targets
`net10.0-windows` and holds only the GDI/WinForms-dependent tests. `Tests.Web/` is the
third test project, covering `Web/` — bUnit for the Razor components and plain xUnit for
`Web/Session`, both cross-platform since nothing in `Web/` needs Windows. Package versions
were pinned to the last releases targeting `net5.0` while the whole suite ran under it —
now historical, since both test projects moved to `net10.0`.

Three tests are structurally unusual and worth knowing before editing them:

- **`UnicolourParityTests` is the most valuable test in the suite.** It checks the mixing
  kernel against Unicolour's independent implementation of the same theory, validating the
  coefficients, the linear K/S mixing, the Saunderson convention and the integration to Lab
  all at once. It builds its own D65 configuration rather than using
  `ArtistPaint.Configuration`, which renders under D50 — comparing against the default
  would report an illuminant offset as a physics failure. Unicolour is referenced by the
  *test* project specifically so this gate outlives the app dropping the dependency.
- **`MixingInvariantTests`** runs properties across the whole library rather than chosen
  examples, catching paints that are individually plausible but behave wrongly in company.
- **`ContactSheetTests` asserts almost nothing.** It renders mixing sweeps to PNG for human
  inspection, and runs as a test only so it cannot silently break. Its real output is the
  image. It now lives in `Tests.Windows/`, alongside `ImageDecoderTests`, `ImageCanvasTests`
  and `UiThemeTests` — the Windows-only project.

`Tests/PaintTranslator.Tests.csproj` is 403 tests and runs cross-platform, including on
this Mac. Golden PNGs are read through `Tests/PngCodec.cs` (ImageSharp) rather than GDI, so
they stay comparable on both platforms. The Web port added `Tests.Web/` (82 tests) rather
than growing this count; `Tests/` stays at 403.

`Tests.Web`'s scheduler and session tests (`RenderSchedulerTests`, `ConversionSessionTests`)
use a manual delay double instead of real timers, and assert by waiting for the
`Idle`/title events the scheduler and session already raise on completion — not by sleeping
a fixed duration — so they run at full speed and don't flake under load.

`Tests` compiles `GoldenSpectraSource.cs` and `SpreadsheetReader.cs` from the ingest tool
directly, so the derivation is tested rather than only its output. Core grants
`InternalsVisibleTo` to `PaintTranslator.Tests` so tests measure the same CIELAB conversion
the matcher uses instead of a duplicate.

## Conventions

- **Doc comments carry the reasoning, not restatements of the signature.** The existing
  ones explain why a constant has its value, what breaks otherwise, and what earlier
  approach failed. Match that; a comment that only names the method again is noise.
- The `win-x64` RID is pinned on the app project (not Core) because Magick.NET otherwise
  copies Linux and macOS native codecs and the output grows from 25 MB to 131 MB.
- Failure modes throughout the imaging code are silent and visual — a wrong kernel or a
  mis-encoded average produces a slightly wrong picture rather than an exception. Prefer
  tests that pin numeric properties over tests that check nothing throws.
