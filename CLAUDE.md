# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

```powershell
dotnet build PaintTranslator.sln                      # builds all four projects
dotnet test Tests/PaintTranslator.Tests.csproj        # 317 tests, ~13s
dotnet run --project PaintTranslator.csproj           # the app
```

Run a single test or class with a filter:

```powershell
dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~UnicolourParityTests"
dotnet test Tests/PaintTranslator.Tests.csproj --filter "FullyQualifiedName~ZeroRadiusLeavesEveryPixelUntouched"
```

Six `NETSDK1138` warnings about `net5.0-windows` being out of support are expected and
pre-existing. A clean build is 0 errors, 6 warnings.

Two auxiliary executables:

```powershell
dotnet run --project BlendTests/PaintTranslator.BlendTests.csproj   # visual gradient-strip harness
dotnet run --project Tools/IngestSpectra/IngestSpectra.csproj       # regenerates Pigments/PigmentData.bin
```

The ingest needs `Tools/IngestSpectra/data/GoldenSpectra.zip`, which is gitignored. It
runs offline and is not part of the build; run it only when coefficients change, and
review the manifest it emits alongside the binary.

## Architecture

A WinForms app (`net5.0-windows`, pinned to `win-x64`) that converts photos into the
colours a chosen set of real acrylic paints can actually be mixed to. The physics is real
measured data, not a colour-space approximation.

### The spectral pipeline

Measured spectra → `Tools/IngestSpectra` → `Pigments/PigmentData.bin` (embedded resource)
→ `PigmentLibrary` → `KubelkaMunk` → `SpectralRenderer` → `ColorSpace` (CIELAB).

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

xUnit. Package versions are pinned to the last releases targeting `net5.0`; newer ones
resolve against .NET Framework and leave the run with no adapter.

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
  image.

`Tests` compiles `GoldenSpectraSource.cs` and `SpreadsheetReader.cs` from the ingest tool
directly, so the derivation is tested rather than only its output. The app grants
`InternalsVisibleTo` to `PaintTranslator.Tests` so tests measure the same CIELAB conversion
the matcher uses instead of a duplicate.

## Conventions

- **Doc comments carry the reasoning, not restatements of the signature.** The existing
  ones explain why a constant has its value, what breaks otherwise, and what earlier
  approach failed. Match that; a comment that only names the method again is noise.
- The `win-x64` RID is pinned because Magick.NET otherwise copies Linux and macOS native
  codecs and the output grows from 25 MB to 131 MB.
- Failure modes throughout the imaging code are silent and visual — a wrong kernel or a
  mis-encoded average produces a slightly wrong picture rather than an exception. Prefer
  tests that pin numeric properties over tests that check nothing throws.
