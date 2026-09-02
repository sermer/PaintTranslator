# Blazor WebAssembly app: design

**Date:** 2026-09-01
**Status:** approved by the owner in chat; not yet implemented
**Sub-project:** 2 of 3 in the move from WinForms to a web app
**Depends on:** `2026-09-01-core-extraction-design.md` (implemented, staged)

## Why

Sub-project 1 made the kernel (`PaintTranslator.Core`) build and test on the owner's
Mac. The app itself is still WinForms and cannot run there. This sub-project ships the
replacement: a standalone Blazor WebAssembly site that does every computation in the
browser, reaches feature parity with the WinForms app, and can later be copied to any
static host. It also delivers the one thing the owner asked for directly in this
session: a double-clickable `.command` file that builds the site, serves it locally,
and opens it in the browser.

## Decisions made with the owner (2026-09-01)

| Decision | Choice | Rejected |
|---|---|---|
| Scope beyond parity | Add a **Download PNG** button (WinForms has no export at all). | Persisting UI settings other than the palette; keeping the dragged-URL download (CORS makes it unreliable in a browser). |
| Formats | **Match WinForms exactly**: PNG, JPEG, GIF, BMP, TIFF, WEBP, AVIF, HEIC/HEIF, PSD. Non-native formats decode through JavaScript libraries loaded on first use. | Browser-native only; HEIC-only add-on. |
| Threading | **Spike measures both**, then the simplest configuration that meets the thresholds is adopted: preview (384 px) under ~300 ms, full 1920×1080 render under ~5 s, per style, in Chrome and Safari on the owner's Mac. | Committing to single- or multi-threaded up front. |
| Compute architecture | **A** (one runtime, render on the UI thread) as the baseline; **B** (A + `WasmEnableThreads`) if the spike shows A misses and B passes and boots correctly; **C** (Core in a Web Worker under a second runtime) only if both miss. | C from the start. |
| Launcher | **Release publish + static serve + open browser.** Reflects deployed performance; Debug WebAssembly is several times slower. | Dev server with hot reload; two launchers. |
| UI stack | Plain Blazor components with hand-written CSS ported from `UiTheme`. | A component library; the app has a dozen controls and a fixed dark theme. |

## Non-goals

- No deployment automation, no Dockerfile, no public hosting (sub-project 3).
- No retirement of the WinForms app. It stays buildable and is removed only after
  the owner has used the web app.
- No change to colour, mixing or mapping behaviour. Core is consumed, not edited,
  except where the spike proves a change is required (see "Spike").
- No persistence beyond the palette. Style, sliders, grid and zoom reset on reload,
  as they do on restart today.
- No mobile layout. The minimum viewport is the WinForms minimum, 900×600.

## Solution layout

| Project | Target | Contents |
|---|---|---|
| `Web/PaintTranslator.Web.csproj` **(new)** | `net10.0`, `Microsoft.NET.Sdk.BlazorWebAssembly` | The site. References Core. `RunAOTCompilation`, `WasmStripILAfterAOT`, `InvariantGlobalization` on. `WasmEnableThreads` only if the spike adopts B. |
| `Web/wwwroot/` | | `index.html`, `css/app.css`, `js/interop.js` (canvas, input, clipboard, download), `js/decoders/` (vendored HEIC, PSD and TIFF bundles, loaded on demand), `favicon`. |
| `Tests.Web/PaintTranslator.Web.Tests.csproj` **(new)** | `net10.0` | xUnit + bUnit. Tests the UI-neutral classes and the components that have logic. Runs on the Mac. |
| `Tools/BuildDecoders/` **(new)** | Node script | One-off, offline: bundles `@webtoon/psd` to a single ES module and copies the libheif-js and UTIF bundles into `Web/wwwroot/js/decoders/`. Output is committed; Node is not needed to build or run the site. |
| `PaintTranslator.command` **(new, repo root)** | zsh | The launcher. |
| `Web/serve.py` **(new)** | Python 3 stdlib | Static server used by the launcher. |

`PaintTranslator.sln` gains the two new projects. The root `PaintTranslator.csproj`
adds `Web/**` and `Tests.Web/**` to its glob exclusions, as it already does for
`Core/**` and `Tests.Windows/**`.

Namespaces: `PaintTranslator.Web` for the app, `PaintTranslator.Web.Session` for the
UI-neutral logic described below.

## The spike

Nothing in the UI is built until the spike has answered the threading question, because
the answer changes the render loop, the launcher's headers and the hosting options.

**Harness.** The Web project is created first with a single diagnostics page at
`/bench`. It builds the same noisy gradient as `Tools/BenchmarkConversion`, takes the
first eight selectable paints, and for every style times `ConversionPreview.CreateSource`
+ `StylePipeline.Render` at 384 px and `StylePipeline.Render` at 1920×1080, three
iterations each, reporting the median in a table on the page and to the console. It
also reports `Environment.ProcessorCount` and whether `SharedArrayBuffer` exists, which
is the observable sign that cross-origin isolation and threads are on.

**Measurements**, each in Chrome and Safari:

1. Release, interpreter (`RunAOTCompilation=false`).
2. Release, AOT.
3. Release, AOT, `WasmEnableThreads=true`, served with
   `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: require-corp`.

**Decision rule.** Adopt the lowest-numbered configuration whose medians meet the
thresholds in both browsers. Configuration 3 also has to render the golden fixture
(`Tests/StyleTestFixtures`) to the same checksum as configuration 2, because Blazor's
own documentation does not cover multithreading and the runtime marks it experimental.
If 1 and 2 miss and 3 fails to boot or mismatches, approach C is designed as a
follow-up spec before UI work; this document does not design C.

**Output.** The numbers and the choice are appended to this document under
"Spike result" and copied into `.claude/handoff/PROJECT.md`. The `/bench` page stays
in the app as a hidden diagnostics route; it is cheap and useful for later regressions.

**Native reference** (Release, M5 Pro, 18 cores, 2026-09-01): 160–250 ms per style
at 1920×1080. Smoothing dominates. The browser will be slower; the thresholds above
are what the owner accepted as usable, not parity with native.

## Components and state

One page, one layout, mirroring `MainForm.Designer.cs`:

```
<Toolbar>          Open Photo · Color Wheel ▾ · Grid cols/rows · Show grid · Zoom · Download PNG
<ImageCanvas>      canvas + overlay layers (grid, tooltip, empty state)
<Sidebar>          Edit Palette · Select all · <PaintList> · <StylePanel> · Reset · Style ▾ · Brush mark · Blur
<PaletteEditorDialog>   modal, same rules as PaletteEditorForm
```

State lives in one scoped service, `ConversionSession` (`PaintTranslator.Web.Session`),
which the components read and call. It is a port of the state `MainForm` keeps in fields,
with the WinForms-only re-entrancy tricks removed:

- `SourcePhoto` (`PixelImage`), `PreviewSource` (384 px), `Displayed` (the frame on
  screen), `DisplayedWheel` (`Traditional`, `SelectedPaints` or none).
- `SelectedPaints`, per-style `Dictionary<IPipelineStage, ParameterValues>` seeded from
  `StylePipeline.DefaultValues`, `MarkPixels`, `BlurRadius`, grid columns/rows,
  `ShowGrid`, `MagnifierActive`.
- `CandidateSetCache`, `ColourMapCache`, and a lazily built `PaintBlendMatcher` that is
  dropped when the palette changes.
- Events: `Changed` (re-render UI), `FrameReady` (push pixels to the canvas).

Behaviours that must survive the port, from the WinForms inventory:

1. Parameter values are kept per style for the session and survive image loads; Reset
   replaces only the active style's values with defaults.
2. The mark slider resets to `RenderContext.DefaultMarkPixels(width, height)` on every
   image load, without triggering an extra render.
3. While a wheel is displayed, no preview is scheduled; the selected-paints wheel is
   regenerated when the palette changes.
4. Blur is not a panel slider. It is injected at render time through
   `PalettePhotoConverter.ComposeWithBlur`, and preview radii are rescaled with
   `ConversionPreview.ScaleRadius`.
5. An empty or stale saved palette falls back to the full selectable catalogue; the
   editor refuses to save an empty selection.

## Image input

Three entry points, one funnel.

| Entry | Mechanism |
|---|---|
| Open Photo | `<InputFile>` with an `accept` list built from the format table below. |
| Paste | A `paste` listener on `document` registered from `interop.js`; takes the first `File` in `clipboardData.items` whose type starts with `image/`, or a file with a recognised extension. |
| Drag and drop | `dragover`/`drop` on the page; first `File` in `dataTransfer.files`. |

The funnel reads the bytes into C#, runs `ImageFormatSniffer.Detect` (content, never
extension, as today), then dispatches:

| Sniffed format | Decoder | Notes |
|---|---|---|
| PNG, JPEG, GIF, BMP, WEBP, AVIF | `createImageBitmap` → offscreen canvas → `getImageData` | Alpha preserved; canvas must be created with `{ colorSpace: "srgb" }` and the bitmap with `premultiplyAlpha: "none"`. |
| HEIC, HEIF | libheif-js (`HeifDecoder.decode` → `display`) | LGPL-3.0, loaded as a separate script on first HEIC. Safari could decode natively, but one path is easier to test. |
| PSD | `@webtoon/psd` (`Psd.parse` → `composite()`) | MIT. Needs the file saved with "Maximize Compatibility"; otherwise the loader reports "PSD has no composite image". |
| TIFF | UTIF.js (`UTIF.decode`, `decodeImage`, `toRGBA8`) | MIT. Chrome does not decode TIFF natively; Safari does, but again one path. |
| Unknown | Error toast: "Not a supported image." | Same message set as WinForms's `MessageBox`es. |

Every decoder returns `{ width, height, rgba: Uint8ClampedArray }` to C# through a
`[JSImport]` call. `PixelCodec` (Session) repacks RGBA bytes into the `0xAARRGGBB`
ints `PixelImage.FromPixels` takes. Alpha 0 stays 0, because the tooltip reads it as
"no paint here".

There is no size cap, matching WinForms, but the spec records the cost: a 12 MP photo is
48 MB as `int[]` plus the browser's own copy, inside a 32-bit WebAssembly heap. The
`EmccMaximumHeapSize` is left at the default; if the owner's photos hit it, that is
the first knob.

The dragged-URL and `text/html` `<img src>` paths of `ImageDataObjectReader` are not
ported.

## Render loop

`RenderScheduler` (Session) is a port of `MainForm`'s `SchedulePreview`,
`PreviewTimer_Tick`, `CaptureRenderRequest`, `RenderCapturedRequestAsync` and
`CanDisplayAutomaticResult`:

1. Any control change calls `Schedule()`, which bumps the generation, cancels the
   in-flight token, and restarts a 125 ms debounce (`System.Threading.Timer` or
   `Task.Delay` with a token; either works on WebAssembly).
2. When the debounce fires it snapshots an immutable `RenderRequest` (preview source,
   paints, style, mark, `StylePipeline.SnapshotValues`, generation), renders it,
   and if the generation still matches, publishes `FrameReady` with the title
   suffix "(live preview)".
3. It then snapshots a second request for the full source and repeats with
   "(converted to paints)".
4. Results whose generation is stale are dropped.

**On configuration A** (single-threaded) the render runs on the only thread, so the
page cannot process input while a render executes. The consequences are stated
rather than hidden: the debounce cannot interrupt a running render, `Schedule()`
calls made during one are honoured when it finishes, and the wait cursor is shown
through a CSS class set before the render and cleared after, with a `Task.Yield()`
between so the browser paints the cursor. `Parallel.For` in Core degrades to a
sequential loop under the single-threaded runtime; no Core change is needed.

**On configuration B** the render moves to `Task.Run` exactly as in WinForms and the
supersede behaviour is identical to today.

The render gate (`SemaphoreSlim(1,1)` in WinForms) becomes a plain "in flight" flag,
since only one scheduler exists and it is single-consumer.

## Canvas, viewport, overlays

One `<canvas>` sized to its container by `ResizeObserver`. C# owns all geometry:

- `ImageViewport` is reused unchanged. JavaScript forwards `wheel` events as
  `(deltaX, deltaY, ctrlKey, shiftKey, clientX, clientY)` and pointer events as
  `(kind, clientX, clientY, buttons)`; C# applies the same gesture table as
  `ImageCanvas.HandleWheel` (Ctrl+wheel and pinch zoom at 1.0015 per unit, horizontal
  wheel or Shift+wheel pans X, plain wheel pans Y, left-drag pans with a 3 px threshold,
  a click below the threshold steps the magnifier through 2×, 4×, 8× fit, then back).
- After each viewport change C# sends `(scale, offsetX, offsetY, smoothing)` and
  JavaScript redraws the offscreen bitmap with `drawImage`; `imageSmoothingEnabled` is
  false above 1:1 when not fitted, true otherwise, matching the GDI interpolation
  choice.
- The displayed frame is pushed once per `FrameReady` as a `Span<byte>` RGBA buffer
  through `[JSImport]` into an offscreen canvas (`putImageData`). No pixel data crosses
  the boundary on mouse moves.
- Grid: C# computes `GridGeometry.Dividers(GetImageBounds(), columns, rows)` and sends
  the segment list; JavaScript strokes them twice (3 px translucent black under 1 px
  white) plus the border, as `GridOverlayRenderer` does.
- Empty state: an HTML card over the canvas with the same two lines of copy as
  `ImageCanvas.DrawEmptyState`, hidden once a photo loads.
- Cursor: CSS `cursor: crosshair` when the magnifier is active, `grabbing` during a
  pan, `wait` during a full render.

**Hover recipe tooltip.** On pointer move C# calls `ImageViewport.TryGetImagePixel`,
reads the displayed `PixelImage`, and asks `RecipeFormatter` (Session; the port of
`ComposeRecipeLines` and the wheel variants) for the lines. The tooltip is a
positioned `<div>` that flips left/up near the right/bottom edges. It is hidden during
a pan and recomputed on viewport change, because zooming under a stationary cursor
changes the pixel. `PaintBlendMatcher` is built lazily on first hover.

**Colour wheels.** The toolbar button opens a two-item menu: Traditional
(`ColorWheelGenerator.CreateTraditional(512)`) and Selected Golden Paints
(`ColorWheelGenerator.Create(512, selected)`). Displaying a wheel sets
`DisplayedWheel`, which gates the scheduler as in WinForms.

**Download PNG.** Enabled when a converted frame or wheel is displayed. JavaScript
draws the offscreen bitmap into a temporary canvas at native size, calls `toBlob`,
and triggers an anchor download named `<original name>-<style>.png` (or
`colour-wheel.png`). The viewer's browser handles the save.

## Palette and styles

- `PaletteStore` (Session) reads and writes `localStorage["paintTranslator.palette"]`
  as the same JSON string array `UserPaletteStore` writes to `palette.json`. Missing,
  empty or unparseable storage returns `null`, and the caller falls back to
  `PigmentLibrary.Selectable`, as today.
- `PaintList` renders `PigmentLibrary.Selectable` filtered by the palette, each with
  a mass-tone swatch computed once via `KubelkaMunk.Mix` + `SpectralRenderer.ToDisplayColor`
  and cached per pigment. Select-all mirrors the list without the WinForms `ItemCheck`
  pre-commit quirk.
- `PaletteEditorDialog` lists the full selectable catalogue with the current palette
  checked; OK with nothing checked shows the same refusal message and stays open.
- `StylePanel` iterates `StyleDefinition.Stages`, skips stages with no parameters, and
  renders a heading per stage and an `<input type="range" min="0" max="100">` per
  `StyleParameter`, with the same 0–100 ↔ value mapping and caption format
  (`"{Label}: {value:0.##} {Unit}"`) as `MainForm`. Brush mark (1–128) and Blur (0–20,
  "Blur: off" at zero) are separate ranges below the style picker.

## The launcher

`PaintTranslator.command` at the repository root, executable bit set:

1. `cd` to its own directory (`"$(dirname "$0")"`), so double-clicking from Finder
   works regardless of the shell's cwd.
2. Check `dotnet` is on `PATH` and that `dotnet workload list` includes `wasm-tools`.
   If not, print the exact install command and stop; do not attempt a slower fallback
   build silently.
3. `dotnet publish Web/PaintTranslator.Web.csproj -c Release -o Web/bin/publish`.
   Publish is incremental; when nothing changed it finishes in seconds. AOT publishes
   take minutes the first time, and the script says so before starting.
4. `python3 Web/serve.py Web/bin/publish/wwwroot`, which picks a free port on
   `127.0.0.1`, prints the URL, and serves with: correct MIME types (`.wasm`, `.js`,
   `.json`, `.dat`, `.br`, `.gz` are the ones that matter), `Cache-Control: no-store`
   so a republished build is never served stale, and, only when the project has
   `WasmEnableThreads` on, the two cross-origin isolation headers. It serves the
   pre-compressed `.br` file with `Content-Encoding: br` when the client accepts it,
   which is what the ASP.NET host does for a deployed site and cuts load time.
5. `open "$URL"` once the port answers.
6. Keep the terminal window open with the server in the foreground; closing it or
   Ctrl+C stops the server. The window title is set to "PaintTranslator".

No global tools and no Node at launch. Python 3.9 on the owner's Mac already maps
`.wasm` to `application/wasm`; the script sets it explicitly anyway.

## Tests

`Tests/` (Core) is untouched. `Tests.Web/` covers the logic that has moved out of
`MainForm` into testable classes:

- `PixelCodec`: RGBA→ARGB→RGBA round trip is lossless, including alpha 0 and 255
  with non-zero colour; a 2×2 fixture packs to the exact expected ints.
- `RecipeFormatter`: for a fixed `BlendMatch` the lines match the WinForms strings
  (percentages sorted largest first, quality word, shift direction, out-of-gamut and
  rounding lines only when their thresholds are crossed); wheel variant rolls up
  "+N more" beyond five paints.
- `RenderScheduler` with a fake renderer: two `Schedule()` calls inside the debounce
  produce one preview; a stale generation's frame is never published; a schedule during
  a running render produces exactly one follow-up; a displayed wheel suppresses
  scheduling.
- `PaletteStore` with a fake storage: round trip; empty, missing and corrupt values
  return `null`.
- `ConversionSession`: parameter values survive a style switch and an image load;
  Reset touches only the active style; mark resets on image load.
- bUnit: `StylePanel` renders one range per declared parameter for every style in
  `StyleRegistry.All` and skips parameterless stages; `PaintList` select-all mirroring;
  `PaletteEditorDialog` refuses an empty selection.

Rendering, gestures, decoders and the launcher are verified by hand on the Mac in
Chrome and Safari: load one photo per format in the table above, run all five styles,
compare against the WinForms screenshots the owner has, download a PNG and reopen it.
The `/bench` page is the regression check for performance.

## Verification

Done means, on the Mac:

```
dotnet build PaintTranslator.sln                       # 0 errors, only the ImageSharp notice
dotnet test Tests/PaintTranslator.Tests.csproj         # 403 green, unchanged
dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj # green
./PaintTranslator.command                              # publishes, serves, opens Chrome
```

and the manual checklist above passed in Chrome and Safari, with the spike result
recorded in this document.

## Risks

- **Interpreter speed.** The single biggest unknown; the spike exists to retire it
  before any UI is written. AOT is expected to be necessary and is on by default.
- **Threads.** Experimental in the runtime and undocumented for Blazor. The spike's
  checksum comparison is the guard; if it fails, B is not adopted regardless of speed.
- **`wasm-tools` workload** is not installed on the owner's Mac. AOT, relinking and
  threads all need it. The launcher checks and prints the install command.
- **libheif-js is LGPL-3.0.** Loaded as a separate, unmodified script, which keeps the
  app's own code under its own licence, but the owner should be aware before the site
  goes public.
- **`@webtoon/psd` needs a composite** in the file. Photoshop writes one only with
  "Maximize Compatibility" on. The error message says so.
- **Large photos** may exhaust the WebAssembly heap. The first fix is
  `EmccMaximumHeapSize`; the second is a size cap, which would be a parity change and
  needs the owner's agreement.
- **Safari gesture semantics** (pinch reports as `wheel` with `ctrlKey`, `gesturestart`
  events) need hand-testing; the gesture table is Chrome-first.
- **Trimming** can strip JS-invokable methods. `[JSExport]` methods are kept by the
  linker, but any `[JSInvokable]` on instances must be protected with
  `DynamicDependency`, per the Blazor docs.

## Spike result

Measured 2026-09-01 on the owner's Mac (M5 Pro, 18 cores), .NET SDK 10.0.400 with
`wasm-tools` 10.0.111, Chrome 152.0.7977.65. Chrome ran **headless** (`--headless=new`),
driven by the `/bench?autorun=3` query parameter added for the purpose, because no
browser could be operated interactively from the session. **Safari was not measured**:
it offers no scripting hook here ("Allow JavaScript from Apple Events" is off), so the
Safari column below is empty and is owed by the owner before the app is called done.
Medians of 3 iterations; the full render is 1920×1080; eight paints; `mark` = 7
(`RenderContext.DefaultMarkPixels`), `blur` = 0. The native reference in the last block
was run with the same inputs (`--paints 8 --mark 7`).

| Config | Browser | Style | Preview ms | Full ms | Checksum |
|---|---|---|---|---|---|
| 1 interpreter | Chrome | Realism | 46 | 1080 | 14992AD57916640D |
| 1 interpreter | Chrome | Tonalism | 74 | 1798 | A7F5A7FF7E6D1A25 |
| 1 interpreter | Chrome | Fauvism | 101 | 2481 | 1342DDAFD121D266 |
| 1 interpreter | Chrome | Post-Impressionism | 95 | 2339 | 700F04D0C7E2C62B |
| 1 interpreter | Chrome | Abstract | 180 | 3589 | 6F9A37BF740FFF5A |
| 2 AOT | Chrome | Realism | 10 | 260 | 14992AD57916640D |
| 2 AOT | Chrome | Tonalism | 17 | 487 | A7F5A7FF7E6D1A25 |
| 2 AOT | Chrome | Fauvism | 22 | 643 | 1342DDAFD121D266 |
| 2 AOT | Chrome | Post-Impressionism | 22 | 613 | 700F04D0C7E2C62B |
| 2 AOT | Chrome | Abstract | 63 | 991 | 6F9A37BF740FFF5A |
| 3 AOT + threads | Chrome | Realism | 5 | 41 | 14992AD57916640D |
| 3 AOT + threads | Chrome | Tonalism | 11 | 109 | A7F5A7FF7E6D1A25 |
| 3 AOT + threads | Chrome | Fauvism | 13 | 127 | 1342DDAFD121D266 |
| 3 AOT + threads | Chrome | Post-Impressionism | 12 | 126 | 700F04D0C7E2C62B |
| 3 AOT + threads | Chrome | Abstract | 55 | 230 | 6F9A37BF740FFF5A |
| 1 / 2 / 3 | Safari | all | not measured | not measured | — |

Other observations:

- Every checksum is identical across configurations 1, 2 and 3 **and** equals the native
  `Tools/BenchmarkConversion` checksum for the same inputs, so the WebAssembly build
  renders bit-for-bit what the WinForms app renders, threads or not.
- Configuration 3 reported `Processors: 18`, `SharedArrayBuffer: True` and booted
  cleanly under the COOP/COEP headers. Its publish fails with a `wasm-ld --shared-memory`
  error if `Web/obj/Release` still holds objects from a non-threaded publish; a clean
  `obj` fixes it. Recorded so nobody mistakes that for a runtime failure later.
- Published `_framework` size: 8.8 MB (1), 24 MB (2), 25 MB (3). AOT publish took ~50 s
  on this machine after the workload was installed.
- The static server needed a single-page-app fallback (extensionless paths that match no
  file serve `index.html`) before `/bench` could be reached by URL; `Web/serve.py` now
  does this.

**Decision: configuration 2 (AOT, `WasmEnableThreads=false`) — approach A.** Both 1 and 2
clear the thresholds in Chrome, so the rule points at the lowest passing number; 2 is
adopted over 1 because the csproj already builds it for Release, and 1's worst case
(Abstract, 3.6 s) leaves too little margin for the unmeasured Safari and for the blur
stage the bench does not exercise, whereas 2's worst case is 1.0 s. Configuration 3
works and is four to five times faster still; it is kept as a future option (flip
`WasmEnableThreads`, publish with a clean `obj`, serve with `--isolate`) rather than
adopted, because the spec adopts the simplest passing configuration and the runtime
still marks threads experimental. The `WasmEnableThreads` line in the csproj stays
`false`; Tasks 7 and 13 follow their configuration A branches.
