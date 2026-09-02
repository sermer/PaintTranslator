# Blazor WebAssembly App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a standalone Blazor WebAssembly site with full parity to the WinForms app, plus a Download PNG button, and a double-clickable `PaintTranslator.command` that publishes, serves and opens it on the owner's Mac.

**Architecture:** One Blazor WebAssembly project (`Web/`) consumes `PaintTranslator.Core` unchanged. UI-neutral logic ported out of `MainForm` lives in `PaintTranslator.Web.Session` (scheduler, session state, palette store, pixel codec, recipe formatter, slider scale) and is unit-tested in `Tests.Web/`. A single `<canvas>` is driven by C# geometry (`ImageViewport`, `GridGeometry`) through a small JavaScript module; pixel buffers cross the boundary with `[JSImport]` memory views, events come back through `[JSInvokable]`. Image decoding is browser-native with vendored JavaScript decoders for HEIC, PSD and TIFF. A spike (Tasks 1–3) decides single- vs multi-threaded before any UI is built.

**Tech Stack:** .NET 10 SDK 10.0.400 on macOS, Blazor WebAssembly standalone (`Microsoft.NET.Sdk.BlazorWebAssembly`), `System.Runtime.InteropServices.JavaScript` (`[JSImport]`/`[JSExport]`), xUnit 2.9.3, bUnit 2.9.0, Python 3 stdlib (`Web/serve.py`), zsh (`PaintTranslator.command`), Node (one-off, offline, `Tools/BuildDecoders` only), libheif-js (LGPL-3.0), @webtoon/psd 0.4.0 (MIT), UTIF.js (MIT).

**Spec:** `docs/superpowers/specs/2026-09-01-blazor-app-design.md`

## Global Constraints

- **Never commit. Never branch. Never create a worktree.** Stage with `git add` and stop. (`CLAUDE.md`)
- Working directory for every command: `/Users/sean/Desktop/ADHD Meadows/PaintTranslator` (the path has a space; quote it).
- `Core/` is not edited by this plan. If the spike proves a Core change necessary, stop and report; it is a spec change.
- Nothing under `Web/` may reference `System.Drawing.Common`; `System.Drawing.Primitives` (`Color`, `Point`, `PointF`, `Size`, `RectangleF`) is fine.
- Namespaces: `PaintTranslator.Web` (components, pages, interop), `PaintTranslator.Web.Session` (UI-neutral logic).
- `PixelImage` byte order is `0xAARRGGBB` per `int`. Browser buffers are RGBA bytes. Only `PixelCodec` converts between them.
- Thresholds (spec): preview at 384 px under 300 ms and 1920×1080 full render under 5 s, per style, in Chrome and Safari.
- Doc comments carry reasoning, not signature restatements (`CLAUDE.md`). Follow the `csharp-code-comments` skill for every new class and method.
- A clean build is 0 errors; the only accepted warning is the ImageSharp licence notice from `Tests/`.
- Run tests from the repo root: `dotnet test Tests/PaintTranslator.Tests.csproj` (403, must stay green) and `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`.
- The WinForms behaviours listed in the spec's "Components and state" section 1–5 are requirements, not suggestions.
- Update `.claude/handoff/PROJECT.md` at the end of every task (status row, next step, problems).

---

## File map

**Create**
- `Web/PaintTranslator.Web.csproj`, `Web/Program.cs`, `Web/App.razor`, `Web/_Imports.razor`, `Web/Layout/MainLayout.razor` — the site (Task 1)
- `Web/wwwroot/index.html`, `Web/wwwroot/css/app.css` (Task 1; styled in Task 11)
- `Web/serve.py` — static server with MIME, no-store, brotli and optional isolation headers (Task 1)
- `Web/Session/BenchRunner.cs`, `Web/Pages/Bench.razor` — spike harness (Task 2)
- `Tests.Web/PaintTranslator.Web.Tests.csproj` (Task 2)
- `Web/Session/PixelCodec.cs` (Task 4)
- `Web/Session/IKeyValueStore.cs`, `Web/Session/PaletteStore.cs`, `Web/Interop/LocalStorageStore.cs` (Task 5)
- `Web/Session/RecipeFormatter.cs`, `Web/Session/StyleSliderScale.cs` (Task 6)
- `Web/Session/RenderRequest.cs`, `Web/Session/IFrameRenderer.cs`, `Web/Session/RenderScheduler.cs` (Task 7)
- `Web/Session/ConversionSession.cs`, `Web/Session/PipelineRenderer.cs`, `Web/Session/WheelDisplay.cs` (Task 8)
- `Web/wwwroot/js/interop.js`, `Web/Interop/CanvasInterop.cs`, `Web/Components/ImageCanvas.razor(.cs)` (Task 9)
- `Web/Session/ImageLoader.cs`, `Web/Interop/DecoderInterop.cs`, `Web/wwwroot/js/decoders/*`, `Tools/BuildDecoders/*` (Task 10)
- `Web/Components/PaintList.razor`, `Web/Components/PaletteEditorDialog.razor`, `Web/Components/StylePanel.razor`, `Web/Components/Sidebar.razor` (Task 11)
- `Web/Components/Toolbar.razor`, `Web/Components/RecipeTooltip.razor`, `Web/Pages/Index.razor` (Task 12)
- `PaintTranslator.command` (Task 13)
- Tests: `Tests.Web/BenchRunnerTests.cs`, `PixelCodecTests.cs`, `PaletteStoreTests.cs`, `RecipeFormatterTests.cs`, `StyleSliderScaleTests.cs`, `RenderSchedulerTests.cs`, `ConversionSessionTests.cs`, `StylePanelTests.cs`, `PaintListTests.cs`, `PaletteEditorDialogTests.cs`

**Modify**
- `PaintTranslator.sln` (Tasks 1, 2)
- `PaintTranslator.csproj` — glob exclusions for `Web/**`, `Tests.Web/**` (Task 1)
- `.gitignore` — `Tools/BuildDecoders/node_modules/` (Task 10)
- `docs/superpowers/specs/2026-09-01-blazor-app-design.md` — "Spike result" section (Task 3)
- `CLAUDE.md` — commands and architecture for the Web project (Task 13)
- `.claude/handoff/PROJECT.md` (every task)

---

### Task 1: Scaffold the Web project and the static server

**Files:**
- Create: `Web/PaintTranslator.Web.csproj`, `Web/Program.cs`, `Web/App.razor`, `Web/_Imports.razor`, `Web/Layout/MainLayout.razor`, `Web/Pages/Index.razor`, `Web/wwwroot/index.html`, `Web/wwwroot/css/app.css`, `Web/serve.py`
- Modify: `PaintTranslator.sln`, `PaintTranslator.csproj`

**Interfaces:**
- Produces: a building, runnable Blazor WASM project referencing Core; `python3 Web/serve.py <dir> [--port N] [--isolate] [--open]` serving static files.

- [ ] **Step 1: Generate the project from the template**

```bash
cd "/Users/sean/Desktop/ADHD Meadows/PaintTranslator"
dotnet new blazorwasm -n PaintTranslator.Web -o Web -f net10.0 --empty --no-https
dotnet sln PaintTranslator.sln add Web/PaintTranslator.Web.csproj
```

Expected: `Web/` contains `PaintTranslator.Web.csproj`, `Program.cs`, `App.razor`, `_Imports.razor`, `Layout/MainLayout.razor`, `Pages/Index.razor`, `wwwroot/index.html`, `wwwroot/css/app.css`. Delete `wwwroot/sample-data` if the template created it.

- [ ] **Step 2: Replace the csproj**

Overwrite `Web/PaintTranslator.Web.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PaintTranslator.Web</RootNamespace>
    <AssemblyName>PaintTranslator.Web</AssemblyName>
    <!-- The kernel formats nothing culture-specific; dropping ICU removes ~1.5 MB
         from the download and is required for the smallest relinked runtime. -->
    <InvariantGlobalization>true</InvariantGlobalization>
    <!-- AOT only on publish: the interpreter is several times slower on the
         mixing kernel (spike, Task 3), and a Debug run is for UI work only. -->
    <RunAOTCompilation Condition="'$(Configuration)' == 'Release'">true</RunAOTCompilation>
    <WasmStripILAfterAOT Condition="'$(Configuration)' == 'Release'">true</WasmStripILAfterAOT>
    <!-- Set to true only if the spike adopts configuration B (see the spec). The
         launcher reads this line to decide whether to send isolation headers. -->
    <WasmEnableThreads>false</WasmEnableThreads>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.*" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Core\PaintTranslator.Core.csproj" />
  </ItemGroup>

</Project>
```

Keep whatever exact package versions the template pinned if they are newer than `10.0.0`; the wildcard is only a floor.

- [ ] **Step 3: Exclude the new folders from the root app project**

In `PaintTranslator.csproj`, find the existing `<Compile Remove="Core\**" />`-style exclusions (they also cover `Tests.Windows\**`) and add the same three lines (`Compile`, `EmbeddedResource`, `None`) for `Web\**` and `Tests.Web\**`.

- [ ] **Step 4: Minimal Program.cs and index page**

`Web/Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PaintTranslator.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
```

`Web/Pages/Index.razor`:

```razor
@page "/"
<h1>PaintTranslator</h1>
<p>Web port scaffold. Bench page: <a href="bench">/bench</a></p>
```

- [ ] **Step 5: Build the whole solution**

Run: `dotnet build PaintTranslator.sln`
Expected: 0 errors; only the ImageSharp notice as a warning. If restore fails on the WebAssembly packages, run `dotnet restore Web/PaintTranslator.Web.csproj` once online.

- [ ] **Step 6: Write `Web/serve.py`**

```python
#!/usr/bin/env python3
"""Static server for the published site.

The launcher uses this instead of a global tool so a fresh Mac needs only the
.NET SDK and the Python that ships with macOS. It does the three things a
deployed host does that http.server does not: correct MIME types for the
WebAssembly payload, brotli negotiation for the pre-compressed files the
publish step emits, and, when threads are enabled, the two cross-origin
isolation headers without which SharedArrayBuffer is unavailable.
"""
import argparse
import os
import socket
import sys
import threading
import webbrowser
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

MIME = {
    ".wasm": "application/wasm",
    ".js": "text/javascript",
    ".mjs": "text/javascript",
    ".json": "application/json",
    ".dat": "application/octet-stream",
    ".blat": "application/octet-stream",
    ".dll": "application/octet-stream",
    ".pdb": "application/octet-stream",
    ".woff2": "font/woff2",
}


class Handler(SimpleHTTPRequestHandler):
    isolate = False

    def end_headers(self):
        # A republished build must never be served stale; the site is local.
        self.send_header("Cache-Control", "no-store")
        if self.isolate:
            self.send_header("Cross-Origin-Opener-Policy", "same-origin")
            self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        super().end_headers()

    def guess_type(self, path):
        _, ext = os.path.splitext(path)
        return MIME.get(ext.lower()) or super().guess_type(path)

    def send_head(self):
        # Serve foo.js.br as foo.js with Content-Encoding when the browser accepts
        # brotli, which is what the ASP.NET host does for a deployed site.
        path = self.translate_path(self.path)
        accepts = "br" in self.headers.get("Accept-Encoding", "")
        if accepts and os.path.isfile(path + ".br") and not path.endswith(".br"):
            try:
                f = open(path + ".br", "rb")
            except OSError:
                self.send_error(HTTPStatus.NOT_FOUND, "File not found")
                return None
            self.send_response(HTTPStatus.OK)
            self.send_header("Content-Type", self.guess_type(path))
            self.send_header("Content-Encoding", "br")
            self.send_header("Content-Length", str(os.fstat(f.fileno()).st_size))
            self.end_headers()
            return f
        return super().send_head()

    def log_message(self, fmt, *args):
        # One line per request is noise in a launcher window; only errors matter.
        if args and str(args[1]).startswith(("4", "5")):
            super().log_message(fmt, *args)


def free_port(preferred):
    with socket.socket() as s:
        try:
            s.bind(("127.0.0.1", preferred))
            return preferred
        except OSError:
            s.bind(("127.0.0.1", 0))
            return s.getsockname()[1]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory")
    parser.add_argument("--port", type=int, default=5180)
    parser.add_argument("--isolate", action="store_true",
                        help="send COOP/COEP headers (needed for WasmEnableThreads)")
    parser.add_argument("--open", action="store_true", help="open the URL in the default browser")
    args = parser.parse_args()

    if not os.path.isfile(os.path.join(args.directory, "index.html")):
        sys.exit(f"No index.html in {args.directory}; publish first.")

    port = free_port(args.port)
    Handler.isolate = args.isolate
    handler = lambda *a, **k: Handler(*a, directory=args.directory, **k)
    server = ThreadingHTTPServer(("127.0.0.1", port), handler)
    url = f"http://127.0.0.1:{port}/"
    print(f"Serving {args.directory} at {url}  (Ctrl+C to stop)")
    if args.open:
        threading.Timer(0.5, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
```

- [ ] **Step 7: Smoke-test the server against a Debug publish**

```bash
dotnet publish Web/PaintTranslator.Web.csproj -c Debug -o Web/bin/publish-debug
python3 Web/serve.py Web/bin/publish-debug/wwwroot --port 5180 &
sleep 1; curl -sI http://127.0.0.1:5180/_framework/dotnet.js | grep -i -e "content-type" -e "cache-control"
curl -sI -H "Accept-Encoding: br" http://127.0.0.1:5180/_framework/dotnet.js | grep -i "content-encoding"
kill %1
```

Expected: `Content-Type: text/javascript`, `Cache-Control: no-store`, `Content-Encoding: br`. Open `http://127.0.0.1:5180/` in Chrome by hand once: the scaffold heading renders.

- [ ] **Step 8: Stage and update the handoff doc**

```bash
git add Web PaintTranslator.sln PaintTranslator.csproj
```

Mark Task 1 done in `.claude/handoff/PROJECT.md`.

---

### Task 2: The `/bench` spike harness and the `Tests.Web` project

**Files:**
- Create: `Web/Session/BenchRunner.cs`, `Web/Pages/Bench.razor`, `Tests.Web/PaintTranslator.Web.Tests.csproj`, `Tests.Web/BenchRunnerTests.cs`
- Modify: `PaintTranslator.sln`

**Interfaces:**
- Produces: `BenchRunner.BuildNoisyGradient(int width, int height) : PixelImage`; `BenchRunner.Checksum(PixelImage) : ulong`; `BenchRunner.Median(List<double>) : double`; `BenchRunner.Run(int fullWidth, int fullHeight, int iterations, Action<string>? log) : IReadOnlyList<BenchRow>`; `record BenchRow(string Style, double PreviewMs, double FullMs, ulong FullChecksum)`.

- [ ] **Step 1: Create the test project**

```bash
mkdir -p Tests.Web
cat > Tests.Web/PaintTranslator.Web.Tests.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>PaintTranslator.Web.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <!-- bUnit renders Razor components without a browser, so the components with
         real logic (style panel, paint list, palette editor) are tested here on
         the Mac. Components that talk to JavaScript are verified by hand. -->
    <PackageReference Include="bunit" Version="2.9.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Web\PaintTranslator.Web.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="Bunit" />
  </ItemGroup>

</Project>
EOF
dotnet sln PaintTranslator.sln add Tests.Web/PaintTranslator.Web.Tests.csproj
```

- [ ] **Step 2: Write the failing tests**

`Tests.Web/BenchRunnerTests.cs`:

```csharp
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
            Assert.Equal(255, a.AlphaAt(i));
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
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: build error, `BenchRunner` does not exist.

- [ ] **Step 4: Implement `BenchRunner`**

`Web/Session/BenchRunner.cs`:

```csharp
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
```

- [ ] **Step 5: Run the tests**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: 4 passed.

- [ ] **Step 6: The bench page**

`Web/Pages/Bench.razor`:

```razor
@page "/bench"
@using PaintTranslator.Web.Session
@inject IJSRuntime Js

<h1>Bench</h1>
<p>
    Processors: @Environment.ProcessorCount ·
    SharedArrayBuffer: @(sharedArrayBuffer?.ToString() ?? "?") ·
    Iterations: <input type="number" @bind="iterations" min="1" max="9" style="width:3em" />
    <button @onclick="RunAsync" disabled="@running">Run</button>
</p>
@if (rows.Count > 0)
{
    <table>
        <thead><tr><th>Style</th><th>Preview ms</th><th>Full 1920×1080 ms</th><th>Checksum</th></tr></thead>
        <tbody>
        @foreach (BenchRow row in rows)
        {
            <tr><td>@row.Style</td><td>@row.PreviewMs.ToString("F0")</td><td>@row.FullMs.ToString("F0")</td><td>@row.FullChecksum.ToString("X16")</td></tr>
        }
        </tbody>
    </table>
}
<pre>@log</pre>

@code {
    private readonly List<BenchRow> rows = new();
    private string log = "";
    private int iterations = 3;
    private bool running;
    private bool? sharedArrayBuffer;

    protected override async Task OnInitializedAsync()
    {
        sharedArrayBuffer = await Js.InvokeAsync<bool>("eval", "typeof SharedArrayBuffer !== 'undefined'");
    }

    private async Task RunAsync()
    {
        running = true;
        rows.Clear();
        log = "";
        StateHasChanged();
        await Task.Yield();
        rows.AddRange(BenchRunner.Run(1920, 1080, iterations, line =>
        {
            log += line + "\n";
            Console.WriteLine(line);
        }));
        running = false;
    }
}
```

- [ ] **Step 7: Build, run in Debug, confirm the page works**

```bash
dotnet build PaintTranslator.sln
dotnet run --project Web/PaintTranslator.Web.csproj
```

Open the printed URL + `/bench` in Chrome, set iterations to 1, click Run. Expected: five rows appear (this will be slow in Debug; that is fine). Stop the server.

- [ ] **Step 8: Stage and update the handoff doc**

```bash
git add Web Tests.Web PaintTranslator.sln
```

---

### Task 3: Run the spike and record the decision

**Files:**
- Modify: `docs/superpowers/specs/2026-09-01-blazor-app-design.md` ("Spike result"), `.claude/handoff/PROJECT.md`, `Web/PaintTranslator.Web.csproj` (only if B is adopted)

**Interfaces:**
- Produces: the threading decision (A, B or "stop, design C") that Tasks 7–9 and 13 read from the spec.

- [ ] **Step 1: Install the WebAssembly build tools**

Run: `dotnet workload install wasm-tools`
Expected: `dotnet workload list` shows `wasm-tools`. This needs network once; it is also what the launcher will check for.

- [ ] **Step 2: Configuration 1, interpreter**

```bash
dotnet publish Web/PaintTranslator.Web.csproj -c Release -o Web/bin/spike1 -p:RunAOTCompilation=false -p:WasmStripILAfterAOT=false
python3 Web/serve.py Web/bin/spike1/wwwroot --port 5181 --open
```

In Chrome, then in Safari: open `/bench`, iterations 3, Run. Copy the five rows into a scratch note. Stop the server.

- [ ] **Step 3: Configuration 2, AOT**

```bash
dotnet publish Web/PaintTranslator.Web.csproj -c Release -o Web/bin/spike2
python3 Web/serve.py Web/bin/spike2/wwwroot --port 5182 --open
```

Same measurement in both browsers. Also note the size of `Web/bin/spike2/wwwroot/_framework` (`du -sh`).

- [ ] **Step 4: Configuration 3, AOT + threads**

```bash
dotnet publish Web/PaintTranslator.Web.csproj -c Release -o Web/bin/spike3 -p:WasmEnableThreads=true
python3 Web/serve.py Web/bin/spike3/wwwroot --port 5183 --isolate --open
```

Expected on the page: `SharedArrayBuffer: True` and `Processors` greater than 1. If the page fails to boot, record the console error and treat configuration 3 as failed. If it boots, measure in both browsers, and compare every style's checksum with configuration 2's. Any mismatch fails configuration 3.

- [ ] **Step 5: Apply the decision rule**

- 1 or 2 meets the thresholds in both browsers → adopt it (2 is expected). Leave `WasmEnableThreads` false.
- Only 3 meets them, boots, and checksums match → set `<WasmEnableThreads>true</WasmEnableThreads>` in the csproj. Tasks 7 and 13 then follow their "configuration B" branches.
- None meets them → stop this plan after this task; report to the owner that approach C needs its own spec.

- [ ] **Step 6: Record the result**

Append to the spec under `## Spike result` a table with columns Config, Browser, Style, Preview ms, Full ms, Checksum, and a one-line decision. Copy the decision line and the AOT publish size into `.claude/handoff/PROJECT.md` under Decisions. Delete `Web/bin/spike*`.

```bash
git add docs/superpowers/specs/2026-09-01-blazor-app-design.md Web/PaintTranslator.Web.csproj
```

---

### Task 4: `PixelCodec`

**Files:**
- Create: `Web/Session/PixelCodec.cs`, `Tests.Web/PixelCodecTests.cs`

**Interfaces:**
- Produces: `PixelCodec.ToPixelImage(ReadOnlySpan<byte> rgba, int width, int height) : PixelImage`; `PixelCodec.ToRgba(PixelImage image) : byte[]`.

- [ ] **Step 1: Write the failing tests**

```csharp
using PaintTranslator.Imaging;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PixelCodecTests
{
    [Fact]
    public void PacksRgbaBytesIntoArgbInts()
    {
        byte[] rgba = { 0x11, 0x22, 0x33, 0xFF,   0x00, 0x00, 0x00, 0x00,
                        0xFF, 0x00, 0x00, 0x80,   0x00, 0xFF, 0x00, 0x01 };
        PixelImage image = PixelCodec.ToPixelImage(rgba, 2, 2);
        Assert.Equal(unchecked((int)0xFF112233), image[0, 0]);
        Assert.Equal(0x00000000, image[1, 0]);
        Assert.Equal(unchecked((int)0x80FF0000), image[0, 1]);
        Assert.Equal(0x0100FF00, image[1, 1]);
    }

    [Fact]
    public void RoundTripIsLosslessIncludingTransparentColour()
    {
        byte[] rgba = { 10, 20, 30, 0,  200, 100, 50, 255,  1, 2, 3, 128,  255, 255, 255, 7 };
        PixelImage image = PixelCodec.ToPixelImage(rgba, 4, 1);
        Assert.Equal(rgba, PixelCodec.ToRgba(image));
    }

    [Fact]
    public void RejectsBufferOfWrongLength()
    {
        Assert.Throws<ArgumentException>(() => PixelCodec.ToPixelImage(new byte[7], 2, 1));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~PixelCodecTests"`
Expected: build error, `PixelCodec` missing.

- [ ] **Step 3: Implement**

```csharp
using PaintTranslator.Imaging;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The only place RGBA byte buffers (what canvas ImageData and every decoder
/// produce) meet PixelImage's packed 0xAARRGGBB ints. Straight alpha in both
/// directions: a transparent pixel keeps its colour bytes because the tooltip
/// distinguishes "alpha 0" from "black", and premultiplying would fold them.
/// </summary>
public static class PixelCodec
{
    public static PixelImage ToPixelImage(ReadOnlySpan<byte> rgba, int width, int height)
    {
        if (rgba.Length != width * height * 4)
        {
            throw new ArgumentException(
                $"Expected {width * height * 4} bytes for {width}x{height}, got {rgba.Length}.", nameof(rgba));
        }
        var pixels = new int[width * height];
        for (int i = 0, b = 0; i < pixels.Length; i++, b += 4)
        {
            pixels[i] = (rgba[b + 3] << 24) | (rgba[b] << 16) | (rgba[b + 1] << 8) | rgba[b + 2];
        }
        return PixelImage.FromPixels(width, height, pixels);
    }

    public static byte[] ToRgba(PixelImage image)
    {
        ReadOnlySpan<int> pixels = image.Pixels;
        var rgba = new byte[pixels.Length * 4];
        for (int i = 0, b = 0; i < pixels.Length; i++, b += 4)
        {
            int argb = pixels[i];
            rgba[b] = (byte)(argb >> 16);
            rgba[b + 1] = (byte)(argb >> 8);
            rgba[b + 2] = (byte)argb;
            rgba[b + 3] = (byte)(argb >> 24);
        }
        return rgba;
    }
}
```

- [ ] **Step 4: Run the tests**

Expected: 3 passed. Then `git add Web/Session/PixelCodec.cs Tests.Web/PixelCodecTests.cs`.

---

### Task 5: `PaletteStore` over a key-value abstraction

**Files:**
- Create: `Web/Session/IKeyValueStore.cs`, `Web/Session/PaletteStore.cs`, `Web/Interop/LocalStorageStore.cs`, `Tests.Web/PaletteStoreTests.cs`

**Interfaces:**
- Produces: `interface IKeyValueStore { string? Get(string key); void Set(string key, string value); }`; `PaletteStore(IKeyValueStore)` with `HashSet<string>? Load()` and `void Save(IEnumerable<string> names)`; `PaletteStore.Key == "paintTranslator.palette"`; `LocalStorageStore(IJSInProcessRuntime) : IKeyValueStore`.

- [ ] **Step 1: Write the failing tests**

```csharp
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PaletteStoreTests
{
    private sealed class MemoryStore : IKeyValueStore
    {
        public Dictionary<string, string> Values { get; } = new();
        public string? Get(string key) => Values.TryGetValue(key, out string? v) ? v : null;
        public void Set(string key, string value) => Values[key] = value;
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
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~PaletteStoreTests"`
Expected: build error.

- [ ] **Step 3: Implement**

`Web/Session/IKeyValueStore.cs`:

```csharp
namespace PaintTranslator.Web.Session;

/// <summary>
/// The persistence seam. The browser's localStorage is synchronous and
/// string-only, so the abstraction is too; tests substitute a dictionary.
/// </summary>
public interface IKeyValueStore
{
    string? Get(string key);
    void Set(string key, string value);
}
```

`Web/Session/PaletteStore.cs`:

```csharp
using System.Text.Json;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Port of Data/UserPaletteStore: the same JSON string array, in localStorage
/// instead of %APPDATA%. Missing, empty and corrupt values all load as null so
/// the caller falls back to the full catalogue; an empty saved palette would
/// otherwise leave the app with nothing to mix.
/// </summary>
public sealed class PaletteStore
{
    public const string Key = "paintTranslator.palette";
    private readonly IKeyValueStore store;

    public PaletteStore(IKeyValueStore store) => this.store = store;

    public HashSet<string>? Load()
    {
        string? json = store.Get(Key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            string[]? names = JsonSerializer.Deserialize<string[]>(json);
            return names == null || names.Length == 0 ? null : new HashSet<string>(names, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(IEnumerable<string> names) => store.Set(Key, JsonSerializer.Serialize(names));
}
```

`Web/Interop/LocalStorageStore.cs`:

```csharp
using Microsoft.JSInterop;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Interop;

/// <summary>
/// localStorage through the in-process JS runtime, which WebAssembly always has;
/// the synchronous call keeps PaletteStore free of async plumbing. Storage can
/// throw in private windows, and that must read as "nothing saved", not a crash.
/// </summary>
public sealed class LocalStorageStore : IKeyValueStore
{
    private readonly IJSInProcessRuntime js;

    public LocalStorageStore(IJSRuntime js) => this.js = (IJSInProcessRuntime)js;

    public string? Get(string key)
    {
        try { return js.Invoke<string?>("localStorage.getItem", key); }
        catch (JSException) { return null; }
    }

    public void Set(string key, string value)
    {
        try { js.InvokeVoid("localStorage.setItem", key, value); }
        catch (JSException) { }
    }
}
```

- [ ] **Step 4: Run the tests, stage**

Expected: 6 passed. `git add Web/Session Web/Interop Tests.Web/PaletteStoreTests.cs`.

---

### Task 6: `RecipeFormatter` and `StyleSliderScale`

**Files:**
- Create: `Web/Session/RecipeFormatter.cs`, `Web/Session/StyleSliderScale.cs`, `Tests.Web/RecipeFormatterTests.cs`, `Tests.Web/StyleSliderScaleTests.cs`

**Interfaces:**
- Produces: `RecipeFormatter.RgbLine(Color) : string`; `RecipeFormatter.ClosestMix(Color pixel, IReadOnlyList<PigmentCoefficients> paints, PaintBlendMatcher.BlendMatch match) : string[]`; `RecipeFormatter.WheelBlend(Color pixel, IReadOnlyList<PigmentCoefficients> paints, double[] weights) : string[]`. `StyleSliderScale.Steps == 100`; `ToPosition(StyleParameter, double) : int`; `ToValue(StyleParameter, int) : double`; `Caption(StyleParameter, double) : string`.

- [ ] **Step 1: Write the failing tests**

`Tests.Web/RecipeFormatterTests.cs`:

```csharp
using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class RecipeFormatterTests
{
    private static readonly IReadOnlyList<PigmentCoefficients> Paints = PigmentLibrary.Selectable.Take(6).ToList();

    [Fact]
    public void ClosestMixListsPaintsLargestShareFirstThenMatchLine()
    {
        var match = new PaintBlendMatcher.BlendMatch(
            Color.FromArgb(120, 80, 60), paintIndices: new[] { 0, 1 }, weights: new[] { 0.25, 0.75 });
        string[] lines = RecipeFormatter.ClosestMix(Color.FromArgb(120, 80, 60), Paints, match);
        Assert.Equal("RGB: 120, 80, 60", lines[0]);
        Assert.Equal("Closest mix:", lines[1]);
        Assert.Equal($"75% {Paints[1].Name}", lines[2]);
        Assert.Equal($"25% {Paints[0].Name}", lines[3]);
        Assert.StartsWith("Match: ", lines[4]);
        Assert.Contains("(dE 0.0)", lines[4]);
        Assert.Equal(5, lines.Length); // identical colours: no shift, no gamut, no rounding line
    }

    [Fact]
    public void ClosestMixAddsGamutAndRoundingLinesOnlyPastThresholds()
    {
        var match = new PaintBlendMatcher.BlendMatch(
            Color.FromArgb(120, 80, 60), new[] { 0 }, new[] { 1.0 },
            exactDistance: 1.0, snappedDistance: 1.6, chromaLost: 0.01);
        string[] lines = RecipeFormatter.ClosestMix(Color.FromArgb(120, 80, 60), Paints, match);
        Assert.Contains("More vivid than this screen can show", lines);
        Assert.Contains("Rounded to whole percent: 1.0 → 1.6", lines);

        var quiet = new PaintBlendMatcher.BlendMatch(
            Color.FromArgb(120, 80, 60), new[] { 0 }, new[] { 1.0 },
            exactDistance: 1.0, snappedDistance: 1.4, chromaLost: 0.0005);
        string[] quietLines = RecipeFormatter.ClosestMix(Color.FromArgb(120, 80, 60), Paints, quiet);
        Assert.DoesNotContain("More vivid than this screen can show", quietLines);
        Assert.DoesNotContain(quietLines, l => l.StartsWith("Rounded"));
    }

    [Fact]
    public void ClosestMixReportsShiftWhenMixDiffersVisibly()
    {
        var match = new PaintBlendMatcher.BlendMatch(Color.FromArgb(200, 200, 200), new[] { 0 }, new[] { 1.0 });
        string[] lines = RecipeFormatter.ClosestMix(Color.FromArgb(60, 60, 60), Paints, match);
        Assert.Contains(lines, l => l.StartsWith("Mix reads "));
    }

    [Fact]
    public void WheelBlendNamesAtMostFivePaintsAndRollsUpTheRest()
    {
        double[] weights = { 0.30, 0.25, 0.20, 0.10, 0.08, 0.04, 0.03 };
        IReadOnlyList<PigmentCoefficients> seven = PigmentLibrary.Selectable.Take(7).ToList();
        string[] lines = RecipeFormatter.WheelBlend(Color.FromArgb(1, 2, 3), seven, weights);
        Assert.Equal("RGB: 1, 2, 3", lines[0]);
        Assert.Equal($"{seven[0].Name}: 30%", lines[1]);
        Assert.Equal($"{seven[4].Name}: 8%", lines[5]);
        Assert.Equal("+2 more: 7%", lines[6]);
        Assert.Equal(7, lines.Length);
    }

    [Fact]
    public void WheelBlendSkipsSharesBelowHalfAPercentFromTheNamedList()
    {
        double[] weights = { 0.996, 0.004 };
        IReadOnlyList<PigmentCoefficients> two = PigmentLibrary.Selectable.Take(2).ToList();
        string[] lines = RecipeFormatter.WheelBlend(Color.Black, two, weights);
        Assert.Equal(2, lines.Length); // RGB + one named paint; 0.4% remainder is below the visible share
    }
}
```

`Tests.Web/StyleSliderScaleTests.cs`:

```csharp
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class StyleSliderScaleTests
{
    private static readonly StyleParameter Strength = new("strength", "Strength", 1, 5, 3, "");
    private static readonly StyleParameter Edge = new("edge", "Edge", 0.01, 0.30, 0.08, "L*");

    [Fact]
    public void EndpointsMapToZeroAndSteps()
    {
        Assert.Equal(0, StyleSliderScale.ToPosition(Strength, 1));
        Assert.Equal(StyleSliderScale.Steps, StyleSliderScale.ToPosition(Strength, 5));
        Assert.Equal(1, StyleSliderScale.ToValue(Strength, 0));
        Assert.Equal(5, StyleSliderScale.ToValue(Strength, StyleSliderScale.Steps));
    }

    [Fact]
    public void PositionRoundTripsThroughValue()
    {
        for (int p = 0; p <= StyleSliderScale.Steps; p++)
        {
            Assert.Equal(p, StyleSliderScale.ToPosition(Edge, StyleSliderScale.ToValue(Edge, p)));
        }
    }

    [Fact]
    public void CaptionMatchesWinFormsFormat()
    {
        Assert.Equal("Edge: 0.08 L*", StyleSliderScale.Caption(Edge, 0.08));
        Assert.Equal("Strength: 3", StyleSliderScale.Caption(Strength, 3));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~RecipeFormatterTests|FullyQualifiedName~StyleSliderScaleTests"`
Expected: build error.

- [ ] **Step 3: Implement**

`Web/Session/RecipeFormatter.cs` — a line-for-line port of `MainForm.ComposeRecipeLines`, `FormatRgbLine`, `ComposeBlendLines` (MainForm.cs:1517–1660), with `List<PigmentCoefficients>` widened to `IReadOnlyList<>`:

```csharp
using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The hover tooltip's text, ported from MainForm so the web tooltip says exactly
/// what the WinForms one did. Kept free of any UI type so the strings are tested.
/// </summary>
public static class RecipeFormatter
{
    public static string RgbLine(Color pixel) => $"RGB: {pixel.R}, {pixel.G}, {pixel.B}";

    public static string[] ClosestMix(
        Color pixel, IReadOnlyList<PigmentCoefficients> paints, PaintBlendMatcher.BlendMatch match)
    {
        var lines = new List<string> { RgbLine(pixel), "Closest mix:" };
        // Largest share first, so the paint the user reaches for first is listed first.
        var order = Enumerable.Range(0, match.PaintIndices.Count).ToList();
        order.Sort((first, second) => match.Percentages[second].CompareTo(match.Percentages[first]));
        foreach (int i in order)
        {
            lines.Add($"{match.Percentages[i]}% {paints[match.PaintIndices[i]].Name}");
        }
        PalettePhotoConverter.RgbToLab(pixel.R, pixel.G, pixel.B, out double targetL, out double targetA, out double targetB);
        PalettePhotoConverter.RgbToLab(match.MixedColor.R, match.MixedColor.G, match.MixedColor.B, out double mixL, out double mixA, out double mixB);
        double deltaE = ColorDifference.CieDe2000(targetL, targetA, targetB, mixL, mixA, mixB);
        lines.Add($"Match: {ColorDifference.DescribeQuality(deltaE)} (dE {deltaE:0.0})");
        string? shift = ColorDifference.DescribeShift(targetL, targetA, targetB, mixL, mixA, mixB);
        if (shift != null)
        {
            lines.Add($"Mix reads {shift}");
        }
        if (match.ChromaLost > 0.001)
        {
            lines.Add("More vivid than this screen can show");
        }
        // Weighted HyAB distances, what the matcher minimises; deliberately not labelled dE00.
        double roundingCost = match.SnappedDistance - match.ExactDistance;
        if (roundingCost > 0.5)
        {
            lines.Add($"Rounded to whole percent: {match.ExactDistance:0.0} → {match.SnappedDistance:0.0}");
        }
        return lines.ToArray();
    }

    public static string[] WheelBlend(Color pixel, IReadOnlyList<PigmentCoefficients> paints, double[] weights)
    {
        const int MaxNamedPaints = 5;
        // Shares below half a percent would display as 0%, so they only count toward the remainder.
        const double MinVisibleShare = 0.005;
        var order = Enumerable.Range(0, weights.Length).ToList();
        order.Sort((first, second) => weights[second].CompareTo(weights[first]));
        var lines = new List<string> { RgbLine(pixel) };
        int named = 0, others = 0;
        double othersShare = 0.0;
        foreach (int index in order)
        {
            if (named < MaxNamedPaints && weights[index] >= MinVisibleShare)
            {
                lines.Add($"{paints[index].Name}: {weights[index] * 100:0}%");
                named++;
            }
            else if (weights[index] > 0.0)
            {
                others++;
                othersShare += weights[index];
            }
        }
        if (others > 0 && othersShare >= MinVisibleShare)
        {
            lines.Add($"+{others} more: {othersShare * 100:0}%");
        }
        return lines.ToArray();
    }
}
```

`Web/Session/StyleSliderScale.cs`:

```csharp
using PaintTranslator.Imaging.Styles;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Every style slider is a 0–100 integer control mapped onto the parameter's
/// declared range, as in MainForm; a hundred steps is finer than any of the
/// declared ranges needs and keeps the range inputs uniform.
/// </summary>
public static class StyleSliderScale
{
    public const int Steps = 100;

    public static int ToPosition(StyleParameter parameter, double value) =>
        (int)Math.Round((value - parameter.Minimum) / (parameter.Maximum - parameter.Minimum) * Steps);

    public static double ToValue(StyleParameter parameter, int position) =>
        parameter.Minimum + (position / (double)Steps) * (parameter.Maximum - parameter.Minimum);

    public static string Caption(StyleParameter parameter, double value) =>
        $"{parameter.Label}: {value:0.##} {parameter.Unit}".TrimEnd();
}
```

- [ ] **Step 4: Run the tests, stage**

Expected: 8 passed. `git add Web/Session Tests.Web`.

---

### Task 7: `RenderScheduler`

**Files:**
- Create: `Web/Session/RenderRequest.cs`, `Web/Session/IFrameRenderer.cs`, `Web/Session/RenderScheduler.cs`, `Tests.Web/RenderSchedulerTests.cs`

**Interfaces:**
- Consumes: nothing from Web yet; Core's `PixelImage`, `PigmentCoefficients`, `StyleDefinition`, `IPipelineStage`, `ParameterValues`.
- Produces:
  - `sealed record RenderRequest(PixelImage Source, IReadOnlyList<PigmentCoefficients> Paints, StyleDefinition Style, int MarkPixels, IReadOnlyDictionary<IPipelineStage, ParameterValues> Values, long Generation, bool IsPreview)`
  - `interface IFrameRenderer { Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token); }`
  - `RenderScheduler(Func<bool, long, RenderRequest?> capture, IFrameRenderer renderer, Func<bool> canRun, Action<RenderRequest, PixelImage> publish, TimeSpan debounce, Func<TimeSpan, CancellationToken, Task>? delay = null)` with `long Generation`, `bool FullRenderInProgress`, `event Action? StateChanged`, `void Schedule()`, `void Cancel()`, `Task Idle` (completes when no loop is running; tests await it).

- [ ] **Step 1: Write the failing tests**

```csharp
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class RenderSchedulerTests
{
    private sealed class ManualDelay
    {
        private TaskCompletionSource current = new();
        public int Started { get; private set; }
        public Task Wait(TimeSpan _, CancellationToken token)
        {
            Started++;
            var tcs = current;
            token.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }
        public void Fire() { var tcs = current; current = new TaskCompletionSource(); tcs.TrySetResult(); }
    }

    private sealed class FakeRenderer : IFrameRenderer
    {
        public List<RenderRequest> Seen { get; } = new();
        public Func<RenderRequest, Task>? Gate { get; set; }
        public async Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token)
        {
            Seen.Add(request);
            if (Gate != null) await Gate(request);
            token.ThrowIfCancellationRequested();
            return PixelImage.Filled(1, 1, unchecked((int)0xFF000000));
        }
    }

    private static readonly IReadOnlyList<PigmentCoefficients> Paints = PigmentLibrary.Selectable.Take(2).ToList();
    private static readonly PixelImage Source = PixelImage.Filled(4, 4, unchecked((int)0xFF808080));

    private static RenderRequest Capture(bool preview, long generation) => new(
        Source, Paints, StyleRegistry.Default, 3, StylePipeline.DefaultValues(StyleRegistry.Default), generation, preview);

    [Fact]
    public async Task TwoSchedulesInsideTheDebounceRenderOnce()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        var scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.FromMilliseconds(125), delay.Wait);

        scheduler.Schedule();
        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;

        Assert.Equal(2, renderer.Seen.Count);           // one preview + one full
        Assert.True(renderer.Seen[0].IsPreview);
        Assert.False(renderer.Seen[1].IsPreview);
        Assert.Equal(2, published.Count);
        Assert.Equal(2, delay.Started);                 // second Schedule restarted the debounce
    }

    [Fact]
    public async Task StaleGenerationIsNeverPublished()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        var gate = new TaskCompletionSource();
        RenderScheduler scheduler = null!;
        renderer.Gate = r =>
        {
            if (r.IsPreview && r.Generation == 1) { scheduler.Schedule(); }  // superseded mid-render
            return Task.CompletedTask;
        };
        scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        delay.Fire();
        await Task.Yield();
        delay.Fire();                                    // the re-schedule's debounce
        await scheduler.Idle;

        Assert.DoesNotContain(published, r => r.Generation == 1);
        Assert.Contains(published, r => r.Generation == 2 && !r.IsPreview);
    }

    [Fact]
    public async Task ScheduleDuringARunningRenderProducesExactlyOneFollowUp()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var published = new List<RenderRequest>();
        RenderScheduler scheduler = null!;
        int extra = 0;
        renderer.Gate = r =>
        {
            if (!r.IsPreview && extra++ == 0) { scheduler.Schedule(); scheduler.Schedule(); }
            return Task.CompletedTask;
        };
        scheduler = new RenderScheduler(Capture, renderer, () => true, (r, _) => published.Add(r),
            TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        delay.Fire();
        await Task.Yield();
        delay.Fire();
        await scheduler.Idle;

        // Generation 1 (first loop) and generation 3 (one follow-up for the two schedules).
        Assert.Equal(2, published.Count(r => r.IsPreview));
        Assert.Equal(1, published.Count(r => r.Generation == 3 && r.IsPreview));
    }

    [Fact]
    public async Task ScheduleIsIgnoredWhileTheGateIsClosed()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        bool open = false;
        var scheduler = new RenderScheduler(Capture, renderer, () => open, (_, _) => { }, TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        Assert.Equal(0, delay.Started);
        Assert.Equal(0, scheduler.Generation);

        open = true;
        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;
        Assert.Equal(2, renderer.Seen.Count);
    }

    [Fact]
    public async Task FullRenderInProgressIsRaisedAroundTheFullRenderOnly()
    {
        var delay = new ManualDelay();
        var renderer = new FakeRenderer();
        var states = new List<bool>();
        RenderScheduler scheduler = null!;
        renderer.Gate = r => { states.Add(scheduler.FullRenderInProgress); return Task.CompletedTask; };
        scheduler = new RenderScheduler(Capture, renderer, () => true, (_, _) => { }, TimeSpan.Zero, delay.Wait);

        scheduler.Schedule();
        delay.Fire();
        await scheduler.Idle;

        Assert.Equal(new[] { false, true }, states);
        Assert.False(scheduler.FullRenderInProgress);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~RenderSchedulerTests"`
Expected: build error.

- [ ] **Step 3: Implement**

`Web/Session/RenderRequest.cs`:

```csharp
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Everything a render needs, snapshotted when it is scheduled. Immutable so a
/// request already in flight is unaffected when the user changes a slider or
/// loads another image; the generation is how a stale result is recognised.
/// </summary>
public sealed record RenderRequest(
    PixelImage Source,
    IReadOnlyList<PigmentCoefficients> Paints,
    StyleDefinition Style,
    int MarkPixels,
    IReadOnlyDictionary<IPipelineStage, ParameterValues> Values,
    long Generation,
    bool IsPreview);
```

`Web/Session/IFrameRenderer.cs`:

```csharp
using PaintTranslator.Imaging;

namespace PaintTranslator.Web.Session;

/// <summary>The pipeline behind the scheduler; a fake in tests, PipelineRenderer in the app.</summary>
public interface IFrameRenderer
{
    Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token);
}
```

`Web/Session/RenderScheduler.cs`:

```csharp
namespace PaintTranslator.Web.Session;

/// <summary>
/// Port of MainForm's preview loop: debounce, then a small preview, then the full
/// frame, dropping anything a newer generation has superseded. The debounce is
/// injectable because the coalescing and supersede rules are what the tests pin,
/// and they cannot be pinned against wall-clock timers.
/// </summary>
public sealed class RenderScheduler
{
    private readonly Func<bool, long, RenderRequest?> capture;
    private readonly IFrameRenderer renderer;
    private readonly Func<bool> canRun;
    private readonly Action<RenderRequest, PixelImage> publish;
    private readonly TimeSpan debounce;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;

    private CancellationTokenSource? debounceCts;
    private CancellationTokenSource? renderCts;
    private bool pending;
    private bool running;
    private TaskCompletionSource idle = CompletedSource();

    public RenderScheduler(
        Func<bool, long, RenderRequest?> capture,
        IFrameRenderer renderer,
        Func<bool> canRun,
        Action<RenderRequest, PixelImage> publish,
        TimeSpan debounce,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.capture = capture;
        this.renderer = renderer;
        this.canRun = canRun;
        this.publish = publish;
        this.debounce = debounce;
        this.delay = delay ?? Task.Delay;
    }

    public long Generation { get; private set; }
    public bool FullRenderInProgress { get; private set; }
    public event Action? StateChanged;
    public Task Idle => idle.Task;

    /// <summary>Bumps the generation so an in-flight result is discarded, then restarts the debounce.</summary>
    public void Schedule()
    {
        if (!canRun())
        {
            return;
        }
        Generation++;
        renderCts?.Cancel();
        pending = false;
        debounceCts?.Cancel();
        debounceCts = new CancellationTokenSource();
        _ = RunAfterDebounceAsync(debounceCts.Token);
    }

    /// <summary>Stops everything without starting a new render (image load, wheel display).</summary>
    public void Cancel()
    {
        Generation++;
        renderCts?.Cancel();
        pending = false;
        debounceCts?.Cancel();
    }

    private async Task RunAfterDebounceAsync(CancellationToken token)
    {
        try
        {
            await delay(debounce, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (token.IsCancellationRequested)
        {
            return;
        }
        pending = true;
        if (running)
        {
            return;
        }
        running = true;
        idle = new TaskCompletionSource();
        try
        {
            while (pending && canRun())
            {
                pending = false;
                using var cts = new CancellationTokenSource();
                renderCts = cts;
                try
                {
                    RenderRequest? preview = capture(true, Generation);
                    if (preview == null)
                    {
                        return;
                    }
                    PixelImage? previewFrame = await RenderGuardedAsync(preview, cts.Token);
                    if (previewFrame == null || !CanDisplay(preview, cts.Token))
                    {
                        continue;
                    }
                    publish(preview, previewFrame);

                    RenderRequest? full = capture(false, Generation);
                    if (full == null)
                    {
                        continue;
                    }
                    SetFullInProgress(true);
                    try
                    {
                        PixelImage? fullFrame = await RenderGuardedAsync(full, cts.Token);
                        if (fullFrame != null && CanDisplay(full, cts.Token))
                        {
                            publish(full, fullFrame);
                        }
                    }
                    finally
                    {
                        SetFullInProgress(false);
                    }
                }
                finally
                {
                    if (ReferenceEquals(renderCts, cts))
                    {
                        renderCts = null;
                    }
                }
            }
        }
        finally
        {
            running = false;
            idle.TrySetResult();
        }
    }

    private async Task<PixelImage?> RenderGuardedAsync(RenderRequest request, CancellationToken token)
    {
        try
        {
            return await renderer.RenderAsync(request, token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Render failed: {ex}");
            return null;
        }
    }

    private bool CanDisplay(RenderRequest request, CancellationToken token) =>
        !token.IsCancellationRequested && request.Generation == Generation && canRun();

    private void SetFullInProgress(bool value)
    {
        FullRenderInProgress = value;
        StateChanged?.Invoke();
    }

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource();
        source.SetResult();
        return source;
    }
}
```

Add `using PaintTranslator.Imaging;` at the top of `RenderScheduler.cs`.

- [ ] **Step 4: Run the tests**

Expected: 5 passed.

- [ ] **Step 5: Stage**

`git add Web/Session Tests.Web/RenderSchedulerTests.cs`

---

### Task 8: `ConversionSession` and `PipelineRenderer`

**Files:**
- Create: `Web/Session/WheelDisplay.cs`, `Web/Session/PipelineRenderer.cs`, `Web/Session/ConversionSession.cs`, `Tests.Web/ConversionSessionTests.cs`
- Modify: `Web/Program.cs` (DI registration)

**Interfaces:**
- Consumes: `RenderScheduler`, `IFrameRenderer`, `RenderRequest`, `PaletteStore`.
- Produces:
  - `enum WheelDisplay { None, Traditional, SelectedPaints }`
  - `PipelineRenderer(CandidateSetCache, ColourMapCache) : IFrameRenderer`
  - `ConversionSession(IFrameRenderer renderer, PaletteStore palette, Func<TimeSpan, CancellationToken, Task>? delay = null)` with:
    - state: `PixelImage? SourcePhoto`, `PixelImage? PreviewSource`, `string? PhotoName`, `PixelImage? Displayed`, `string Title`, `WheelDisplay Wheel`, `IReadOnlyList<PigmentCoefficients> AvailablePaints` (the palette), `IReadOnlyList<PigmentCoefficients> SelectedPaints`, `StyleDefinition Style`, `int MarkPixels` (1–128), `int BlurRadius` (0–20), `int GridColumns`, `int GridRows`, `bool ShowGrid`, `bool MagnifierActive`, `bool FullRenderInProgress`, `bool ImageOperationInProgress`
    - methods: `void LoadPhoto(PixelImage photo, string name)`, `IReadOnlyDictionary<IPipelineStage, ParameterValues> ValuesFor(StyleDefinition)`, `void SetStyle(string name)`, `void SetParameter(IPipelineStage stage, string id, double value)`, `void ResetActiveStyle()`, `void SetMark(int)`, `void SetBlur(int)`, `void SetSelectedPaints(IEnumerable<PigmentCoefficients>)`, `void ApplyPalette(IEnumerable<string> names)` (saves + repopulates), `void ShowWheel(WheelDisplay)`, `void ShowPhoto()`, `void SetGrid(int columns, int rows, bool show)`, `void SetMagnifier(bool)`, `PaintBlendMatcher Matcher` (lazy), `string[]? RecipeAt(int x, int y)`
    - events: `event Action? Changed`, `event Action<PixelImage>? FrameReady`

- [ ] **Step 1: Write the failing tests**

```csharp
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class ConversionSessionTests
{
    private sealed class MemoryStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> values = new();
        public string? Get(string key) => values.TryGetValue(key, out string? v) ? v : null;
        public void Set(string key, string value) => values[key] = value;
    }

    private sealed class NullRenderer : IFrameRenderer
    {
        public int Calls;
        public Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult<PixelImage?>(request.Source);
        }
    }

    private static Task NeverDelay(TimeSpan _, CancellationToken token) =>
        Task.Delay(Timeout.Infinite, token);

    private static ConversionSession NewSession(NullRenderer? renderer = null) =>
        new(renderer ?? new NullRenderer(), new PaletteStore(new MemoryStore()), NeverDelay);

    private static IPipelineStage FirstParameterisedStage(StyleDefinition style) =>
        style.Stages.First(s => s.Parameters.Count > 0);

    [Fact]
    public void StartsWithFullCatalogueDefaultStyleAndDefaults()
    {
        var session = NewSession();
        Assert.Equal(PigmentLibrary.Selectable.Count, session.SelectedPaints.Count);
        Assert.Equal(StyleRegistry.Default.Name, session.Style.Name);
        Assert.Equal(3, session.MarkPixels);
        Assert.Equal(2, session.BlurRadius);
        Assert.Equal(WheelDisplay.None, session.Wheel);
    }

    [Fact]
    public void ParameterValuesSurviveStyleSwitchAndImageLoad()
    {
        var session = NewSession();
        StyleDefinition first = session.Style;
        IPipelineStage stage = FirstParameterisedStage(first);
        StyleParameter parameter = stage.Parameters[0];
        double changed = parameter.Minimum + 0.5 * (parameter.Maximum - parameter.Minimum);

        session.SetParameter(stage, parameter.Id, changed);
        session.SetStyle(StyleRegistry.All[1].Name);
        session.LoadPhoto(PixelImage.Filled(8, 8, unchecked((int)0xFF404040)), "x.png");
        session.SetStyle(first.Name);

        Assert.Equal(changed, session.ValuesFor(first)[stage][parameter.Id], 6);
    }

    [Fact]
    public void ResetTouchesOnlyTheActiveStyle()
    {
        var session = NewSession();
        StyleDefinition a = StyleRegistry.All[0];
        StyleDefinition b = StyleRegistry.All[1];
        IPipelineStage stageA = FirstParameterisedStage(a);
        IPipelineStage stageB = FirstParameterisedStage(b);
        StyleParameter pa = stageA.Parameters[0];
        StyleParameter pb = stageB.Parameters[0];

        session.SetStyle(a.Name);
        session.SetParameter(stageA, pa.Id, pa.Maximum);
        session.SetStyle(b.Name);
        session.SetParameter(stageB, pb.Id, pb.Maximum);
        session.ResetActiveStyle();

        Assert.Equal(StylePipeline.DefaultValues(b)[stageB][pb.Id], session.ValuesFor(b)[stageB][pb.Id], 6);
        Assert.Equal(pa.Maximum, session.ValuesFor(a)[stageA][pa.Id], 6);
    }

    [Fact]
    public void LoadingAPhotoResetsMarkToTheImageDefaultAndClearsAnyWheel()
    {
        var session = NewSession();
        session.SetMark(77);
        session.ShowWheel(WheelDisplay.Traditional);
        var photo = PixelImage.Filled(1200, 800, unchecked((int)0xFF404040));

        session.LoadPhoto(photo, "photo.jpg");

        Assert.Equal(Math.Clamp(RenderContext.DefaultMarkPixels(1200, 800), 1, 128), session.MarkPixels);
        Assert.Equal(WheelDisplay.None, session.Wheel);
        Assert.Same(photo, session.Displayed);
        Assert.Equal("Paint Translator - photo.jpg", session.Title);
        Assert.NotNull(session.PreviewSource);
        Assert.True(session.PreviewSource!.Width <= ConversionPreview.MaximumDimension);
    }

    [Fact]
    public void ChangingPaintsWhileTheSelectedWheelShowsRegeneratesTheWheel()
    {
        var session = NewSession();
        session.ShowWheel(WheelDisplay.SelectedPaints);
        PixelImage before = session.Displayed!;
        session.SetSelectedPaints(PigmentLibrary.Selectable.Take(3));
        Assert.NotSame(before, session.Displayed);
        Assert.Equal(512, session.Displayed!.Width);
        Assert.Equal(WheelDisplay.SelectedPaints, session.Wheel);
    }

    [Fact]
    public void ApplyPaletteSavesAndRepopulatesAndEmptyFallsBackToCatalogue()
    {
        var store = new MemoryStore();
        var session = new ConversionSession(new NullRenderer(), new PaletteStore(store), NeverDelay);
        string[] two = PigmentLibrary.Selectable.Take(2).Select(p => p.Name).ToArray();

        session.ApplyPalette(two);
        Assert.Equal(two, session.AvailablePaints.Select(p => p.Name));
        Assert.Contains(two[0], store.Get(PaletteStore.Key)!);

        session.ApplyPalette(new[] { "No Such Paint" });
        Assert.Equal(PigmentLibrary.Selectable.Count, session.AvailablePaints.Count);
    }

    [Fact]
    public void RecipeAtReturnsNullOutsideOrOnTransparentPixels()
    {
        var session = NewSession();
        session.ShowWheel(WheelDisplay.Traditional);
        Assert.Null(session.RecipeAt(0, 0));                 // wheel corner is transparent
        Assert.NotNull(session.RecipeAt(256, 256));          // centre has colour
        Assert.Null(session.RecipeAt(-1, 0));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~ConversionSessionTests"`
Expected: build error.

- [ ] **Step 3: Implement `WheelDisplay` and `PipelineRenderer`**

`Web/Session/WheelDisplay.cs`:

```csharp
namespace PaintTranslator.Web.Session;

public enum WheelDisplay { None, Traditional, SelectedPaints }
```

`Web/Session/PipelineRenderer.cs`:

```csharp
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The real renderer. Task.Run is kept even though single-threaded WebAssembly
/// executes it on the same thread: with WasmEnableThreads (configuration B) it
/// becomes a genuine background render with no other change.
/// </summary>
public sealed class PipelineRenderer : IFrameRenderer
{
    private readonly CandidateSetCache candidates;
    private readonly ColourMapCache colourMaps;

    public PipelineRenderer(CandidateSetCache candidates, ColourMapCache colourMaps)
    {
        this.candidates = candidates;
        this.colourMaps = colourMaps;
    }

    public Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token) => Task.Run(() =>
    {
        CandidateSet set = candidates.GetOrCreate(request.Paints, request.Style, request.Values, token);
        if (set == null || token.IsCancellationRequested)
        {
            return null;
        }
        return StylePipeline.Render(
            request.Source, request.Paints, request.Style, request.MarkPixels,
            request.Values, set, token, colourMapCache: colourMaps);
    }, token);
}
```

- [ ] **Step 4: Implement `ConversionSession`**

```csharp
using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The state MainForm kept in fields, with the WinForms re-entrancy guards gone.
/// Components read properties and call methods; they never hold pipeline state.
/// Every mutation ends by raising Changed, and every new frame by raising
/// FrameReady, so the canvas and the sidebar cannot drift apart.
/// </summary>
public sealed class ConversionSession
{
    public const int MarkMinimum = 1, MarkMaximum = 128, BlurMinimum = 0, BlurMaximum = 20;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(125);

    private readonly PaletteStore palette;
    private readonly RenderScheduler scheduler;
    private readonly Dictionary<string, Dictionary<IPipelineStage, ParameterValues>> styleValues = new();
    private List<PigmentCoefficients> available = new();
    private List<PigmentCoefficients> selected = new();
    private PaintBlendMatcher? matcher;

    public ConversionSession(IFrameRenderer renderer, PaletteStore palette,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.palette = palette;
        scheduler = new RenderScheduler(Capture, renderer, CanRender, Publish, Debounce, delay);
        scheduler.StateChanged += () => Changed?.Invoke();
        Style = StyleRegistry.Default;
        Populate(palette.Load());
    }

    public PixelImage? SourcePhoto { get; private set; }
    public PixelImage? PreviewSource { get; private set; }
    public string? PhotoName { get; private set; }
    public PixelImage? Displayed { get; private set; }
    public string Title { get; private set; } = "Paint Translator";
    public WheelDisplay Wheel { get; private set; }
    public IReadOnlyList<PigmentCoefficients> AvailablePaints => available;
    public IReadOnlyList<PigmentCoefficients> SelectedPaints => selected;
    public StyleDefinition Style { get; private set; }
    public int MarkPixels { get; private set; } = 3;
    public int BlurRadius { get; private set; } = 2;
    public int GridColumns { get; private set; } = 2;
    public int GridRows { get; private set; } = 2;
    public bool ShowGrid { get; private set; }
    public bool MagnifierActive { get; private set; }
    public bool ImageOperationInProgress { get; private set; }
    public bool FullRenderInProgress => scheduler.FullRenderInProgress;

    public event Action? Changed;
    public event Action<PixelImage>? FrameReady;

    /// <summary>Lazily built: the matcher is costly and only hovering needs it.</summary>
    public PaintBlendMatcher Matcher => matcher ??= new PaintBlendMatcher(selected);

    public void BeginImageOperation() { ImageOperationInProgress = true; scheduler.Cancel(); Changed?.Invoke(); }
    public void EndImageOperation() { ImageOperationInProgress = false; Changed?.Invoke(); }

    public void LoadPhoto(PixelImage photo, string name)
    {
        scheduler.Cancel();
        SourcePhoto = photo;
        PreviewSource = ConversionPreview.CreateSource(photo);
        PhotoName = name;
        // A brush covers a roughly constant fraction of a canvas whatever the file's
        // resolution, so the default follows the image rather than the last one.
        MarkPixels = Math.Clamp(RenderContext.DefaultMarkPixels(photo.Width, photo.Height), MarkMinimum, MarkMaximum);
        Wheel = WheelDisplay.None;
        Display(photo, $"Paint Translator - {name}");
        scheduler.Schedule();
    }

    public IReadOnlyDictionary<IPipelineStage, ParameterValues> ValuesFor(StyleDefinition style) => Values(style);

    public void SetStyle(string name)
    {
        Style = StyleRegistry.ByName(name);
        Changed?.Invoke();
        scheduler.Schedule();
    }

    public void SetParameter(IPipelineStage stage, string id, double value)
    {
        Values(Style)[stage].Set(id, value);
        Changed?.Invoke();
        scheduler.Schedule();
    }

    public void ResetActiveStyle()
    {
        styleValues[Style.Name] = new Dictionary<IPipelineStage, ParameterValues>(StylePipeline.DefaultValues(Style));
        Changed?.Invoke();
        scheduler.Schedule();
    }

    public void SetMark(int pixels) { MarkPixels = Math.Clamp(pixels, MarkMinimum, MarkMaximum); Changed?.Invoke(); scheduler.Schedule(); }
    public void SetBlur(int radius) { BlurRadius = Math.Clamp(radius, BlurMinimum, BlurMaximum); Changed?.Invoke(); scheduler.Schedule(); }
    public void SetGrid(int columns, int rows, bool show) { GridColumns = columns; GridRows = rows; ShowGrid = show; Changed?.Invoke(); }
    public void SetMagnifier(bool active) { MagnifierActive = active; Changed?.Invoke(); }

    public void SetSelectedPaints(IEnumerable<PigmentCoefficients> paints)
    {
        selected = paints.ToList();
        matcher = null;
        if (Wheel == WheelDisplay.SelectedPaints)
        {
            Display(ColorWheelGenerator.Create(512, selected), "Paint Translator - Selected Golden Paint Wheel");
        }
        else
        {
            Changed?.Invoke();
            scheduler.Schedule();
        }
    }

    /// <summary>The palette editor's OK: persist, then rebuild the list with everything checked.</summary>
    public void ApplyPalette(IEnumerable<string> names)
    {
        var set = new HashSet<string>(names, StringComparer.Ordinal);
        palette.Save(set);
        Populate(set);
        SetSelectedPaints(available);
    }

    public void ShowWheel(WheelDisplay kind)
    {
        scheduler.Cancel();
        Wheel = kind;
        if (kind == WheelDisplay.Traditional)
        {
            Display(ColorWheelGenerator.CreateTraditional(512), "Paint Translator - Traditional Color Wheel");
        }
        else
        {
            Display(ColorWheelGenerator.Create(512, selected), "Paint Translator - Selected Golden Paint Wheel");
        }
    }

    /// <summary>The one addition to WinForms, which could only leave a wheel by loading a photo.</summary>
    public void ShowPhoto()
    {
        Wheel = WheelDisplay.None;
        if (SourcePhoto != null)
        {
            Display(SourcePhoto, $"Paint Translator - {PhotoName}");
            scheduler.Schedule();
        }
        else
        {
            Displayed = null;
            Title = "Paint Translator";
            Changed?.Invoke();
        }
    }

    /// <summary>Tooltip text for a displayed-image pixel, or null where there is nothing to say.</summary>
    public string[]? RecipeAt(int x, int y)
    {
        PixelImage? image = Displayed;
        if (image == null || x < 0 || y < 0 || x >= image.Width || y >= image.Height)
        {
            return null;
        }
        int argb = image[x, y];
        // Fully transparent pixels are the empty surround of a colour wheel.
        if ((argb >>> 24) == 0)
        {
            return null;
        }
        Color pixel = Color.FromArgb(argb);
        switch (Wheel)
        {
            case WheelDisplay.SelectedPaints:
                double[]? weights = ColorWheelGenerator.GetBlendWeights(image.Width, selected.Count, x, y);
                return weights == null ? null : RecipeFormatter.WheelBlend(pixel, selected, weights);
            case WheelDisplay.Traditional:
                return new[] { RecipeFormatter.RgbLine(pixel) };
            default:
                if (selected.Count == 0)
                {
                    return new[] { RecipeFormatter.RgbLine(pixel) };
                }
                return RecipeFormatter.ClosestMix(pixel, selected, Matcher.FindClosestBlend(pixel));
        }
    }

    private Dictionary<IPipelineStage, ParameterValues> Values(StyleDefinition style)
    {
        if (!styleValues.TryGetValue(style.Name, out Dictionary<IPipelineStage, ParameterValues>? values))
        {
            values = new Dictionary<IPipelineStage, ParameterValues>(StylePipeline.DefaultValues(style));
            styleValues[style.Name] = values;
        }
        return values;
    }

    private void Populate(ISet<string>? names)
    {
        matcher = null;
        available = PigmentLibrary.Selectable.Where(p => names == null || names.Contains(p.Name)).ToList();
        // A saved palette whose names no longer match any catalogue paint would leave
        // the app with no paints; fall back to the catalogue.
        if (available.Count == 0)
        {
            available = PigmentLibrary.Selectable.ToList();
        }
        selected = available.ToList();
        Changed?.Invoke();
    }

    private bool CanRender() =>
        SourcePhoto != null && PreviewSource != null && Wheel == WheelDisplay.None && !ImageOperationInProgress;

    private RenderRequest? Capture(bool preview, long generation)
    {
        PixelImage? source = preview ? PreviewSource : SourcePhoto;
        if (source == null || SourcePhoto == null || selected.Count == 0)
        {
            return null;
        }
        IReadOnlyDictionary<IPipelineStage, ParameterValues> values = StylePipeline.SnapshotValues(Style, Values(Style));
        int blur = BlurRadius, mark = MarkPixels;
        if (preview)
        {
            blur = ConversionPreview.ScaleRadius(blur, SourcePhoto.Size, source.Size);
            mark = ConversionPreview.ScaleRadius(mark, SourcePhoto.Size, source.Size);
        }
        (StyleDefinition style, IReadOnlyDictionary<IPipelineStage, ParameterValues> renderValues) =
            PalettePhotoConverter.ComposeWithBlur(Style, values, blur);
        return new RenderRequest(source, selected.ToList(), style, mark, renderValues, generation, preview);
    }

    private void Publish(RenderRequest request, PixelImage frame)
    {
        Wheel = WheelDisplay.None;
        Display(frame, request.IsPreview
            ? $"Paint Translator - {PhotoName} (live preview)"
            : $"Paint Translator - {PhotoName} (converted to paints)");
    }

    private void Display(PixelImage frame, string title)
    {
        Displayed = frame;
        Title = title;
        Changed?.Invoke();
        FrameReady?.Invoke(frame);
    }
}
```

- [ ] **Step 5: Register in DI**

In `Web/Program.cs`, before `builder.Build()`:

```csharp
builder.Services.AddSingleton<IKeyValueStore, LocalStorageStore>();
builder.Services.AddSingleton<PaletteStore>();
builder.Services.AddSingleton(new CandidateSetCache());
builder.Services.AddSingleton(new ColourMapCache());
builder.Services.AddSingleton<IFrameRenderer, PipelineRenderer>();
builder.Services.AddSingleton<ConversionSession>();
```

with `using PaintTranslator.Imaging.Styles; using PaintTranslator.Web.Interop; using PaintTranslator.Web.Session;`.

- [ ] **Step 6: Run all Web tests and the solution build**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj` then `dotnet build PaintTranslator.sln`
Expected: all green, 0 errors. Stage: `git add Web Tests.Web`.

---

### Task 9: Canvas interop and the `ImageCanvas` component

**Files:**
- Create: `Web/wwwroot/js/interop.js`, `Web/Interop/CanvasInterop.cs`, `Web/Components/ImageCanvas.razor`, `Web/Components/ImageCanvas.razor.cs`
- Modify: `Web/Program.cs` (import the JS module), `Web/wwwroot/index.html`, `Web/Pages/Index.razor` (temporary host for manual check)

**Interfaces:**
- Consumes: `ConversionSession` (`Displayed`, `FrameReady`, `Changed`, `ShowGrid`, `GridColumns`, `GridRows`, `MagnifierActive`, `RecipeAt`).
- Produces: `CanvasInterop` static partial class with `[JSImport]` methods `PutFrame(string canvasId, int width, int height, Span<byte> rgba)`, `SetView(string canvasId, double scale, double offsetX, double offsetY, bool smooth)`, `SetGrid(string canvasId, double[] segments)` (flat x1,y1,x2,y2,… in CSS px, or empty), `Bind(string canvasId, object dotNetRef)` via IJSRuntime; `ImageCanvas` component parameters: none (reads the session), events into the tooltip via `HoverChanged(Point? clientPoint, string[]? lines)` callback parameter.

- [ ] **Step 1: The JavaScript module**

`Web/wwwroot/js/interop.js`:

```javascript
// Canvas, input and clipboard glue. C# owns every decision (viewport maths,
// gesture table, grid geometry); this file only draws what it is told and
// forwards raw events. Keeping it decision-free is what lets the C# side be
// unit-tested without a browser.

const canvases = new Map(); // id -> { canvas, ctx, frame, view, grid, dotnet, dpr }

function state(id) {
  let s = canvases.get(id);
  if (!s) {
    const canvas = document.getElementById(id);
    s = { canvas, ctx: canvas.getContext("2d"), frame: null, view: null, grid: [], dotnet: null, dpr: window.devicePixelRatio || 1 };
    canvases.set(id, s);
  }
  return s;
}

export function putFrame(id, width, height, view) {
  const s = state(id);
  const bytes = new Uint8ClampedArray(width * height * 4);
  view.copyTo(bytes);
  const frame = new OffscreenCanvas(width, height);
  frame.getContext("2d").putImageData(new ImageData(bytes, width, height), 0, 0);
  s.frame = frame;
  redraw(s);
}

export function setView(id, scale, offsetX, offsetY, smooth) {
  const s = state(id);
  s.view = { scale, offsetX, offsetY, smooth };
  redraw(s);
}

export function setGrid(id, segments) {
  const s = state(id);
  s.grid = Array.from(segments);
  redraw(s);
}

export function clearFrame(id) {
  const s = state(id);
  s.frame = null;
  redraw(s);
}

function redraw(s) {
  const { canvas, ctx, dpr } = s;
  const w = canvas.clientWidth, h = canvas.clientHeight;
  if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
    canvas.width = Math.round(w * dpr);
    canvas.height = Math.round(h * dpr);
  }
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.fillStyle = "#0a0c10";
  ctx.fillRect(0, 0, w, h);
  if (s.frame && s.view) {
    const { scale, offsetX, offsetY, smooth } = s.view;
    ctx.imageSmoothingEnabled = smooth;
    ctx.imageSmoothingQuality = "high";
    ctx.drawImage(s.frame, offsetX, offsetY, s.frame.width * scale, s.frame.height * scale);
  }
  if (s.grid.length >= 4) {
    // Two strokes, translucent black under white, so the grid reads on any image.
    for (const [width, style] of [[3, "rgba(0,0,0,0.45)"], [1, "rgba(255,255,255,0.9)"]]) {
      ctx.lineWidth = width;
      ctx.strokeStyle = style;
      ctx.beginPath();
      for (let i = 0; i + 3 < s.grid.length; i += 4) {
        ctx.moveTo(s.grid[i], s.grid[i + 1]);
        ctx.lineTo(s.grid[i + 2], s.grid[i + 3]);
      }
      ctx.stroke();
    }
  }
}

// Events: forwarded with CSS-pixel coordinates relative to the canvas.
export function bind(id, dotnet) {
  const s = state(id);
  s.dotnet = dotnet;
  const canvas = s.canvas;
  const local = (e) => { const r = canvas.getBoundingClientRect(); return [e.clientX - r.left, e.clientY - r.top]; };

  canvas.addEventListener("wheel", (e) => {
    e.preventDefault();
    const [x, y] = local(e);
    dotnet.invokeMethodAsync("OnWheel", e.deltaX, e.deltaY, e.ctrlKey || e.metaKey, e.shiftKey, x, y);
  }, { passive: false });
  for (const kind of ["pointerdown", "pointermove", "pointerup", "pointercancel", "pointerleave"]) {
    canvas.addEventListener(kind, (e) => {
      const [x, y] = local(e);
      if (kind === "pointerdown") canvas.setPointerCapture(e.pointerId);
      dotnet.invokeMethodAsync("OnPointer", kind, x, y, e.buttons);
    });
  }
  new ResizeObserver(() => {
    redraw(s);
    dotnet.invokeMethodAsync("OnResize", canvas.clientWidth, canvas.clientHeight);
  }).observe(canvas);
  dotnet.invokeMethodAsync("OnResize", canvas.clientWidth, canvas.clientHeight);
}

export function downloadPng(id, fileName) {
  const s = state(id);
  if (!s.frame) return;
  const out = document.createElement("canvas");
  out.width = s.frame.width;
  out.height = s.frame.height;
  out.getContext("2d").drawImage(s.frame, 0, 0);
  out.toBlob((blob) => {
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = fileName;
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
  }, "image/png");
}
```

- [ ] **Step 2: `CanvasInterop`**

`Web/Interop/CanvasInterop.cs`:

```csharp
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace PaintTranslator.Web.Interop;

/// <summary>
/// Pixel buffers cross to JavaScript as memory views, not JSON: a 1920×1080 frame
/// is 8 MB and the IJSRuntime path would base64 it. Everything else here is small
/// and goes the same way for consistency.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class CanvasInterop
{
    public const string ModuleName = "interop";

    public static Task ImportAsync() => JSHost.ImportAsync(ModuleName, "../js/interop.js");

    [JSImport("putFrame", ModuleName)]
    public static partial void PutFrame(string canvasId, int width, int height,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> rgba);

    [JSImport("setView", ModuleName)]
    public static partial void SetView(string canvasId, double scale, double offsetX, double offsetY, bool smooth);

    [JSImport("setGrid", ModuleName)]
    public static partial void SetGrid(string canvasId, [JSMarshalAs<JSType.Array<JSType.Number>>] double[] segments);

    [JSImport("clearFrame", ModuleName)]
    public static partial void ClearFrame(string canvasId);

    [JSImport("downloadPng", ModuleName)]
    public static partial void DownloadPng(string canvasId, string fileName);
}
```

In `Web/Program.cs`, before `RunAsync()`: `await CanvasInterop.ImportAsync();` (with `using PaintTranslator.Web.Interop;`).

- [ ] **Step 3: The component**

`Web/Components/ImageCanvas.razor`:

```razor
@using PaintTranslator.Web.Session
@implements IDisposable
@inject ConversionSession Session
@inject IJSRuntime Js

<div class="canvas-host @(Session.MagnifierActive ? "magnifier" : "") @(panning ? "panning" : "") @(Session.FullRenderInProgress ? "busy" : "")">
    <canvas id="@CanvasId"></canvas>
    @if (Session.Displayed == null)
    {
        <div class="empty-state">
            <div class="empty-title">Drop a photo to begin</div>
            <div class="empty-body">Drag an image here, paste from the clipboard, or choose Open Photo.</div>
        </div>
    }
</div>
```

`Web/Components/ImageCanvas.razor.cs`:

```csharp
using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PaintTranslator.Imaging;
using PaintTranslator.Web.Interop;
using PaintTranslator.Web.Session;
using System.Diagnostics.CodeAnalysis;

namespace PaintTranslator.Web.Components;

/// <summary>
/// The WinForms ImageCanvas with GDI replaced by interop.js. All geometry stays
/// in ImageViewport; the gesture table below is copied from ImageCanvas.HandleWheel
/// and StepMagnifier so the two apps feel identical. A pinch on a Mac trackpad
/// arrives as a wheel event with ctrlKey set in every browser, which is why
/// ctrl-wheel is zoom rather than a modifier the user has to know.
/// </summary>
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ImageCanvas))]
public partial class ImageCanvas
{
    public const string CanvasId = "paint-canvas";
    private const float WheelDetent = 120f;
    private const float PanPixelsPerDetent = 100f;
    private const double ZoomPerWheelUnit = 1.0015;
    private const float DragThreshold = 3f;
    private const float ScaleEpsilon = 0.001f;
    private static readonly float[] MagnifierSteps = { 2f, 4f, 8f };

    private readonly ImageViewport viewport = new();
    private DotNetObjectReference<ImageCanvas>? self;
    private bool panning, dragging;
    private PointF dragStart, lastPointer;
    private bool bound;

    /// <summary>Raised with the client point and lines, or (null, null) to hide the tooltip.</summary>
    [Parameter] public EventCallback<(Point? At, string[]? Lines)> HoverChanged { get; set; }

    /// <summary>The host's CSS size, which the tooltip needs to flip near the edges.</summary>
    [Parameter] public EventCallback<(int Width, int Height)> Resized { get; set; }

    protected override void OnInitialized()
    {
        Session.FrameReady += OnFrameReady;
        Session.Changed += OnSessionChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || bound) return;
        bound = true;
        self = DotNetObjectReference.Create(this);
        // Same URL as CanvasInterop.ImportAsync resolves to, so the browser hands back
        // the same module instance and both paths share its canvas map.
        var module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/interop.js");
        await module.InvokeVoidAsync("bind", CanvasId, self);
        if (Session.Displayed != null) OnFrameReady(Session.Displayed);
    }

    private void OnFrameReady(PixelImage frame)
    {
        bool sizeChanged = viewport.ImageSize != frame.Size;
        viewport.ImageSize = frame.Size;
        if (sizeChanged) viewport.Fit();
        CanvasInterop.PutFrame(CanvasId, frame.Width, frame.Height, PixelCodec.ToRgba(frame));
        PushView();
    }

    private void OnSessionChanged()
    {
        PushGrid();
        InvokeAsync(StateHasChanged);
    }

    private void PushView()
    {
        RectangleF bounds = viewport.GetImageBounds();
        bool smooth = !(viewport.Scale > 1f && !viewport.IsFitted);
        CanvasInterop.SetView(CanvasId, viewport.Scale, bounds.X, bounds.Y, smooth);
        PushGrid();
    }

    private void PushGrid()
    {
        if (!Session.ShowGrid || Session.Displayed == null)
        {
            CanvasInterop.SetGrid(CanvasId, Array.Empty<double>());
            return;
        }
        RectangleF bounds = viewport.GetImageBounds();
        var flat = new List<double>();
        foreach (GridGeometry.Segment segment in GridGeometry.Dividers(bounds, Session.GridColumns, Session.GridRows))
        {
            flat.Add(segment.Start.X); flat.Add(segment.Start.Y); flat.Add(segment.End.X); flat.Add(segment.End.Y);
        }
        // The border rectangle, as GridOverlayRenderer draws it.
        flat.AddRange(new double[] { bounds.Left, bounds.Top, bounds.Right, bounds.Top,
                                     bounds.Right, bounds.Top, bounds.Right, bounds.Bottom,
                                     bounds.Right, bounds.Bottom, bounds.Left, bounds.Bottom,
                                     bounds.Left, bounds.Bottom, bounds.Left, bounds.Top });
        CanvasInterop.SetGrid(CanvasId, flat.ToArray());
    }

    [JSInvokable]
    public async Task OnResize(double width, double height)
    {
        viewport.ContainerSize = new Size((int)width, (int)height);
        PushView();
        await Resized.InvokeAsync(((int)width, (int)height));
    }

    [JSInvokable]
    public async Task OnWheel(double deltaX, double deltaY, bool ctrl, bool shift, double x, double y)
    {
        if (Session.Displayed == null) return;
        var cursor = new PointF((float)x, (float)y);
        if (ctrl)
        {
            // Browsers report wheel deltas in pixels; -deltaY is "zoom in", as in WinForms.
            viewport.ZoomTo(viewport.Scale * (float)Math.Pow(ZoomPerWheelUnit, -deltaY), cursor);
        }
        else if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            viewport.PanBy((float)(-deltaX / WheelDetent * PanPixelsPerDetent), 0f);
        }
        else if (shift)
        {
            viewport.PanBy((float)(-deltaY / WheelDetent * PanPixelsPerDetent), 0f);
        }
        else
        {
            viewport.PanBy(0f, (float)(-deltaY / WheelDetent * PanPixelsPerDetent));
        }
        PushView();
        await UpdateHover(cursor);
    }

    [JSInvokable]
    public async Task OnPointer(string kind, double x, double y, int buttons)
    {
        var p = new PointF((float)x, (float)y);
        switch (kind)
        {
            case "pointerdown" when (buttons & 1) != 0:
                dragging = true; panning = false; dragStart = p; lastPointer = p;
                await HoverChanged.InvokeAsync((null, null));
                break;
            case "pointermove":
                if (dragging)
                {
                    if (!panning && Distance(dragStart, p) > DragThreshold) { panning = true; StateHasChanged(); }
                    if (panning)
                    {
                        viewport.PanBy(p.X - lastPointer.X, p.Y - lastPointer.Y);
                        PushView();
                    }
                    lastPointer = p;
                }
                else
                {
                    await UpdateHover(p);
                }
                break;
            case "pointerup":
                if (dragging && !panning && Session.MagnifierActive && Session.Displayed != null)
                {
                    StepMagnifier(p);
                }
                dragging = false; panning = false; StateHasChanged();
                await UpdateHover(p);
                break;
            case "pointercancel":
            case "pointerleave":
                dragging = false; panning = false; StateHasChanged();
                await HoverChanged.InvokeAsync((null, null));
                break;
        }
    }

    private void StepMagnifier(PointF anchor)
    {
        float fit = viewport.FitScale;
        float target = fit;
        foreach (float step in MagnifierSteps)
        {
            if (viewport.Scale < fit * step - ScaleEpsilon) { target = fit * step; break; }
        }
        viewport.ZoomTo(target, anchor);
        PushView();
    }

    private async Task UpdateHover(PointF cursor)
    {
        if (!viewport.TryGetImagePixel(new Point((int)cursor.X, (int)cursor.Y), out Point pixel))
        {
            await HoverChanged.InvokeAsync((null, null));
            return;
        }
        string[]? lines = Session.RecipeAt(pixel.X, pixel.Y);
        await HoverChanged.InvokeAsync(lines == null ? (null, null) : (new Point((int)cursor.X, (int)cursor.Y), lines));
    }

    private static float Distance(PointF a, PointF b) => MathF.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    public void Dispose()
    {
        Session.FrameReady -= OnFrameReady;
        Session.Changed -= OnSessionChanged;
        self?.Dispose();
    }
}
```

Check `ImageViewport.ImageSize`/`ContainerSize` setter semantics in `Core/Imaging/ImageViewport.cs:45-90` (whether setting `ImageSize` already refits) and drop the explicit `Fit()` if it does.

- [ ] **Step 4: Temporary host page and a smoke test**

Replace `Web/Pages/Index.razor` with a temporary host that loads the noisy gradient so the canvas can be exercised before image loading exists:

```razor
@page "/"
@using PaintTranslator.Web.Components
@using PaintTranslator.Web.Session
@inject ConversionSession Session

<div style="height:100vh;display:flex;flex-direction:column">
    <div><button @onclick="Load">Load gradient</button> <label><input type="checkbox" @onchange="e => Session.SetGrid(4, 3, (bool)e.Value!)" /> grid</label> <label><input type="checkbox" @onchange="e => Session.SetMagnifier((bool)e.Value!)" /> magnifier</label> @Session.Title</div>
    <div style="flex:1;min-height:0"><ImageCanvas HoverChanged="h => hover = h" /></div>
    <pre>@(hover.Lines == null ? "" : string.Join("\n", hover.Lines))</pre>
</div>

@code {
    private (System.Drawing.Point? At, string[]? Lines) hover;
    private void Load() => Session.LoadPhoto(BenchRunner.BuildNoisyGradient(1600, 1000), "gradient");
}
```

Add to `Web/wwwroot/css/app.css`:

```css
.canvas-host { position: relative; width: 100%; height: 100%; background: #0a0c10; }
.canvas-host canvas { width: 100%; height: 100%; display: block; touch-action: none; }
.canvas-host.magnifier canvas { cursor: crosshair; }
.canvas-host.panning canvas { cursor: grabbing; }
.canvas-host.busy canvas { cursor: wait; }
.empty-state { position: absolute; inset: 0; display: grid; place-content: center; text-align: center; color: #9ea9b9; pointer-events: none; }
.empty-title { font-size: 18px; font-weight: 600; color: #ebeef4; margin-bottom: 6px; }
```

Run: `dotnet run --project Web/PaintTranslator.Web.csproj`, open in Chrome. Expected: Load gradient shows the image fitted; after the preview delay the converted frame replaces it; ctrl-wheel/pinch zooms about the cursor; drag pans; grid checkbox draws a 4×3 grid that tracks zoom; magnifier click steps 2×/4×/8×/fit; hovering prints recipe lines under the canvas; hover outside the image clears them. Fix anything that does not match before moving on.

- [ ] **Step 5: Stage**

`git add Web`

---

### Task 10: Image loading: decoders, open, paste, drop

**Files:**
- Create: `Tools/BuildDecoders/package.json`, `Tools/BuildDecoders/build.sh`, `Web/wwwroot/js/decoders/libheif-bundle.js`, `Web/wwwroot/js/decoders/UTIF.js`, `Web/wwwroot/js/decoders/psd.js`, `Web/wwwroot/js/decoders/LICENSES.md`, `Web/Interop/DecoderInterop.cs`, `Web/Session/ImageLoader.cs`, `Web/Session/ImageLoadException.cs`
- Modify: `Web/wwwroot/js/interop.js` (decode + paste/drop), `.gitignore`

**Interfaces:**
- Consumes: `PixelCodec`, `ImageFormatSniffer.Detect(ReadOnlySpan<byte>) : ImageFileFormat` (check the exact signature in `Core/Imaging/ImageFormatSniffer.cs`).
- Produces: `ImageLoader.LoadAsync(byte[] bytes, string name) : Task<PixelImage>` throwing `ImageLoadException(string message)`; JS `decode(bytes, format) : Promise<{width, height, rgba}>`; JS `bindFileInputs(dotnet)` raising `OnFileBytes(byte[] bytes, string name)` for paste and drop; `ImageLoader.AcceptList : string` (the `<InputFile accept>` value).

- [ ] **Step 1: Vendor the decoders**

`Tools/BuildDecoders/package.json`:

```json
{
  "name": "painttranslator-build-decoders",
  "private": true,
  "devDependencies": {
    "@webtoon/psd": "0.4.0",
    "esbuild": "0.24.0",
    "libheif-js": "1.18.2",
    "utif": "3.1.0"
  }
}
```

`Tools/BuildDecoders/build.sh`:

```bash
#!/bin/zsh
# One-off, offline after the first npm install. Copies the HEIC and TIFF bundles
# as published and bundles @webtoon/psd (ESM with an inlined wasm) into a single
# file. Outputs are committed under Web/wwwroot/js/decoders so neither the build
# nor the launcher needs Node.
set -euo pipefail
cd "$(dirname "$0")"
OUT="../../Web/wwwroot/js/decoders"
npm install
mkdir -p "$OUT"
cp node_modules/libheif-js/libheif-wasm/libheif-bundle.js "$OUT/libheif-bundle.js"
cp node_modules/utif/UTIF.js "$OUT/UTIF.js"
npx esbuild node_modules/@webtoon/psd/dist/index.js --bundle --format=esm --minify \
  --loader:.wasm=binary --outfile="$OUT/psd.js"
{
  echo "# Vendored decoders"; echo
  echo "- libheif-bundle.js — libheif-js $(node -p "require('libheif-js/package.json').version") — LGPL-3.0 — https://github.com/catdad-experiments/libheif-js"
  echo "- UTIF.js — utif $(node -p "require('utif/package.json').version") — MIT — https://github.com/photopea/UTIF.js"
  echo "- psd.js — @webtoon/psd $(node -p "require('@webtoon/psd/package.json').version") bundled with esbuild — MIT — https://github.com/webtoon/psd"
} > "$OUT/LICENSES.md"
echo "done"
```

Run `chmod +x Tools/BuildDecoders/build.sh && Tools/BuildDecoders/build.sh`. If `npm install` reports a version that does not exist, use `npm view <pkg> version` and pin the current one in `package.json`. Add `Tools/BuildDecoders/node_modules/` and `Tools/BuildDecoders/package-lock.json` to `.gitignore`.

If esbuild fails on `@webtoon/psd` because the wasm is referenced by URL, check `node_modules/@webtoon/psd/dist/` for a `.wasm` file; if present, add `--loader:.wasm=dataurl` instead. Verify in Step 5 that a PSD actually decodes.

- [ ] **Step 2: JavaScript decode and file input**

Append to `Web/wwwroot/js/interop.js`:

```javascript
// Decoding. Native formats go through createImageBitmap; the rest through the
// vendored libraries, each loaded on first use so the common path pays nothing.
const loaded = {};
async function loadScript(src) {
  if (loaded[src]) return loaded[src];
  loaded[src] = new Promise((resolve, reject) => {
    const s = document.createElement("script");
    s.src = src; s.onload = resolve; s.onerror = () => reject(new Error(`Failed to load ${src}`));
    document.head.appendChild(s);
  });
  return loaded[src];
}

async function decodeNative(bytes) {
  const bitmap = await createImageBitmap(new Blob([bytes]), { premultiplyAlpha: "none", colorSpaceConversion: "none" });
  const c = new OffscreenCanvas(bitmap.width, bitmap.height);
  const ctx = c.getContext("2d", { colorSpace: "srgb", willReadFrequently: true });
  ctx.drawImage(bitmap, 0, 0);
  const data = ctx.getImageData(0, 0, bitmap.width, bitmap.height).data;
  bitmap.close();
  return { width: c.width, height: c.height, rgba: data };
}

async function decodeHeic(bytes) {
  await loadScript("js/decoders/libheif-bundle.js");
  const decoder = new libheif.HeifDecoder();
  const images = decoder.decode(bytes);
  if (!images.length) throw new Error("HEIC file holds no image.");
  const image = images[0];
  const width = image.get_width(), height = image.get_height();
  const rgba = new Uint8ClampedArray(width * height * 4);
  await new Promise((resolve, reject) =>
    image.display({ data: rgba, width, height }, (out) => out ? resolve() : reject(new Error("HEIC decode failed."))));
  images.forEach((i) => i.free());
  return { width, height, rgba };
}

async function decodeTiff(bytes) {
  await loadScript("js/decoders/UTIF.js");
  const ifds = UTIF.decode(bytes.buffer);
  if (!ifds.length) throw new Error("TIFF file holds no image.");
  UTIF.decodeImage(bytes.buffer, ifds[0]);
  const rgba = new Uint8ClampedArray(UTIF.toRGBA8(ifds[0]).buffer);
  return { width: ifds[0].width, height: ifds[0].height, rgba };
}

async function decodePsd(bytes) {
  const { default: Psd } = await import("./decoders/psd.js");
  const psd = Psd.parse(bytes.buffer);
  let rgba;
  try { rgba = await psd.composite(); }
  catch { throw new Error("PSD has no composite image. Save it with 'Maximize Compatibility' on."); }
  return { width: psd.width, height: psd.height, rgba };
}

export async function decode(view, format) {
  // The memory view is only valid during this call; copy before the first await.
  const bytes = new Uint8Array(view.slice());
  let out;
  switch (format) {
    case "Heic": out = await decodeHeic(bytes); break;
    case "Tiff": out = await decodeTiff(bytes); break;
    case "Psd": out = await decodePsd(bytes); break;
    default: out = await decodeNative(bytes); break;
  }
  // GetPropertyAsByteArray on the C# side reads a Uint8Array, not a clamped one.
  const rgba = out.rgba;
  return { width: out.width, height: out.height, rgba: new Uint8Array(rgba.buffer, rgba.byteOffset, rgba.byteLength) };
}

export function bindFileInputs(dotnet) {
  const send = async (file) => {
    const bytes = new Uint8Array(await file.arrayBuffer());
    await dotnet.invokeMethodAsync("OnFileBytes", bytes, file.name || "pasted image");
  };
  document.addEventListener("paste", (e) => {
    const item = Array.from(e.clipboardData?.items ?? []).find((i) => i.kind === "file");
    if (item) { e.preventDefault(); send(item.getAsFile()); }
  });
  document.addEventListener("dragover", (e) => { e.preventDefault(); e.dataTransfer.dropEffect = "copy"; });
  document.addEventListener("drop", (e) => {
    e.preventDefault();
    const file = e.dataTransfer?.files?.[0];
    if (file) send(file);
  });
}
```

- [ ] **Step 3: `DecoderInterop`, `ImageLoader`, `ImageLoadException`**

`Web/Interop/DecoderInterop.cs`:

```csharp
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace PaintTranslator.Web.Interop;

[SupportedOSPlatform("browser")]
public static partial class DecoderInterop
{
    [JSImport("decode", CanvasInterop.ModuleName)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> DecodeAsync(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> bytes, string format);
}
```

`Web/Session/ImageLoadException.cs`:

```csharp
namespace PaintTranslator.Web.Session;

/// <summary>A user-facing failure: the message is shown as-is, so it must be plain English.</summary>
public sealed class ImageLoadException : Exception
{
    public ImageLoadException(string message, Exception? inner = null) : base(message, inner) { }
}
```

`Web/Session/ImageLoader.cs`:

```csharp
using System.Runtime.InteropServices.JavaScript;
using PaintTranslator.Imaging;
using PaintTranslator.Web.Interop;

namespace PaintTranslator.Web.Session;

/// <summary>
/// The funnel every input path ends in. The format is sniffed from the bytes, as
/// Windows/ImageDecoder does, because extensions lie and the clipboard has none.
/// </summary>
public static class ImageLoader
{
    /// <summary>Same list as Windows/ImageDecoder.SupportedExtensions.</summary>
    public const string AcceptList = ".png,.jpg,.jpeg,.jfif,.bmp,.gif,.tif,.tiff,.webp,.avif,.heic,.heif,.psd";

    public static async Task<PixelImage> LoadAsync(byte[] bytes, string name)
    {
        ImageFileFormat format = ImageFormatSniffer.Detect(bytes);
        if (format == ImageFileFormat.Unknown)
        {
            throw new ImageLoadException($"'{name}' is not a supported image.");
        }
        JSObject result;
        try
        {
            result = await DecoderInterop.DecodeAsync(bytes, format.ToString());
        }
        catch (JSException ex)
        {
            throw new ImageLoadException($"Could not open '{name}': {ex.Message}", ex);
        }
        int width = result.GetPropertyAsInt32("width");
        int height = result.GetPropertyAsInt32("height");
        byte[] rgba = result.GetPropertyAsByteArray("rgba")
            ?? throw new ImageLoadException($"Could not open '{name}': decoder returned no pixels.");
        return PixelCodec.ToPixelImage(rgba, width, height);
    }
}
```

Check `ImageFileFormat` in `Core/Imaging/ImageFormatSniffer.cs` for the exact member names (`Heic`/`Heif`, `Tiff`, `Psd`, `Avif`, `WebP`) and match the JS `switch` strings to `format.ToString()`. If the sniffer distinguishes `Heif` from `Heic`, route both to `decodeHeic`.

- [ ] **Step 4: Wire open, paste and drop into the host page**

In `Web/Pages/Index.razor` (still the temporary host), add:

```razor
<InputFile OnChange="OnFile" accept="@ImageLoader.AcceptList" />
@if (error != null) { <div class="error">@error</div> }
```

and in `@code`:

```csharp
private string? error;
private DotNetObjectReference<Index>? self;

protected override async Task OnAfterRenderAsync(bool first)
{
    if (!first) return;
    self = DotNetObjectReference.Create(this);
    var module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/interop.js");
    await module.InvokeVoidAsync("bindFileInputs", self);
}

private async Task OnFile(InputFileChangeEventArgs e)
{
    using var stream = e.File.OpenReadStream(maxAllowedSize: 512L * 1024 * 1024);
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer);
    await LoadBytes(buffer.ToArray(), e.File.Name);
}

[JSInvokable]
public Task OnFileBytes(byte[] bytes, string name) => LoadBytes(bytes, name);

private async Task LoadBytes(byte[] bytes, string name)
{
    error = null;
    Session.BeginImageOperation();
    try
    {
        PixelImage photo = await ImageLoader.LoadAsync(bytes, name);
        Session.LoadPhoto(photo, name);
    }
    catch (ImageLoadException ex) { error = ex.Message; }
    finally { Session.EndImageOperation(); }
}
```

with `@inject IJSRuntime Js`, `@using Microsoft.AspNetCore.Components.Forms`, `@using PaintTranslator.Imaging`, and `[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Index))]` on the page's partial class (add a `Index.razor.cs` with just the attribute if needed).

- [ ] **Step 5: Manual verification, every format**

Run `dotnet run --project Web/PaintTranslator.Web.csproj`. For each of PNG (with transparency), JPEG, GIF, BMP, WEBP, AVIF, TIFF, HEIC, PSD: open via the file input, then paste one, then drop one. Use the sample files under `Tests/Assets/` where present (there is an HEIC there) and `Tests/TestImages.cs`'s Magick.NET generator for the others if needed. Expected: each loads, the title shows the name, the preview converts. A PSD saved without Maximize Compatibility shows the exact message from `decodePsd`. A `.txt` renamed to `.png` shows "is not a supported image".

- [ ] **Step 6: Stage**

`git add Web Tools/BuildDecoders .gitignore`

---

### Task 11: Sidebar: paint list, palette editor, style panel, sliders

**Files:**
- Create: `Web/Components/PaintList.razor`, `Web/Components/PaletteEditorDialog.razor`, `Web/Components/StylePanel.razor`, `Web/Components/Sidebar.razor`, `Tests.Web/StylePanelTests.cs`, `Tests.Web/PaintListTests.cs`, `Tests.Web/PaletteEditorDialogTests.cs`
- Modify: `Web/wwwroot/css/app.css`

**Interfaces:**
- Consumes: `ConversionSession`, `StyleSliderScale`, `PaletteStore`.
- Produces: `PaintList` (parameters `Paints`, `Selected`, `SelectedChanged : EventCallback<IReadOnlyList<PigmentCoefficients>>`); `PaletteEditorDialog` (parameters `Catalogue`, `Current`, `Open`, `OnApply : EventCallback<IReadOnlyList<string>>`, `OnCancel : EventCallback`); `StylePanel` (parameters `Style`, `Values`, `OnChange : EventCallback<(IPipelineStage Stage, string Id, double Value)>`); `Sidebar` (no parameters; composes them over the session).

- [ ] **Step 1: Write the failing bUnit tests**

`Tests.Web/StylePanelTests.cs`:

```csharp
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Web.Components;

namespace PaintTranslator.Web.Tests;

public class StylePanelTests : BunitContext
{
    [Fact]
    public void RendersOneRangePerDeclaredParameterAndSkipsParameterlessStages()
    {
        foreach (StyleDefinition style in StyleRegistry.All)
        {
            var values = StylePipeline.DefaultValues(style);
            var cut = Render<StylePanel>(p => p.Add(x => x.Style, style).Add(x => x.Values, values));
            int expected = style.Stages.Sum(s => s.Parameters.Count);
            Assert.Equal(expected, cut.FindAll("input[type=range]").Count);
            int headings = style.Stages.Count(s => s.Parameters.Count > 0);
            Assert.Equal(headings, cut.FindAll("h3").Count);
        }
    }

    [Fact]
    public void CaptionShowsCurrentValueAndChangeReportsStageIdValue()
    {
        StyleDefinition style = StyleRegistry.Default;
        IPipelineStage stage = style.Stages.First(s => s.Parameters.Count > 0);
        StyleParameter parameter = stage.Parameters[0];
        (IPipelineStage, string, double)? reported = null;
        var cut = Render<StylePanel>(p => p
            .Add(x => x.Style, style)
            .Add(x => x.Values, StylePipeline.DefaultValues(style))
            .Add(x => x.OnChange, v => reported = v));

        cut.Find("input[type=range]").Input("100");

        Assert.NotNull(reported);
        Assert.Same(stage, reported!.Value.Item1);
        Assert.Equal(parameter.Id, reported.Value.Item2);
        Assert.Equal(parameter.Maximum, reported.Value.Item3, 6);
    }
}
```

`Tests.Web/PaintListTests.cs`:

```csharp
using PaintTranslator.Pigments;
using PaintTranslator.Web.Components;

namespace PaintTranslator.Web.Tests;

public class PaintListTests : BunitContext
{
    private static readonly IReadOnlyList<PigmentCoefficients> Three = PigmentLibrary.Selectable.Take(3).ToList();

    [Fact]
    public void UncheckingOnePaintClearsSelectAllAndReportsTheRest()
    {
        IReadOnlyList<PigmentCoefficients>? reported = null;
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, Three).Add(x => x.Selected, Three)
            .Add(x => x.SelectedChanged, s => reported = s));

        cut.FindAll("input.paint").Skip(1).First().Change(false);

        Assert.Equal(new[] { Three[0], Three[2] }, reported);
        Assert.False(cut.Find("input.select-all").IsChecked());
    }

    [Fact]
    public void SelectAllChecksEveryPaint()
    {
        IReadOnlyList<PigmentCoefficients>? reported = null;
        var cut = Render<PaintList>(p => p.Add(x => x.Paints, Three).Add(x => x.Selected, new[] { Three[0] })
            .Add(x => x.SelectedChanged, s => reported = s));

        cut.Find("input.select-all").Change(true);

        Assert.Equal(Three, reported);
    }
}
```

`Tests.Web/PaletteEditorDialogTests.cs`:

```csharp
using PaintTranslator.Pigments;
using PaintTranslator.Web.Components;

namespace PaintTranslator.Web.Tests;

public class PaletteEditorDialogTests : BunitContext
{
    [Fact]
    public void OkWithNothingCheckedShowsRefusalAndDoesNotApply()
    {
        bool applied = false;
        var cut = Render<PaletteEditorDialog>(p => p
            .Add(x => x.Catalogue, PigmentLibrary.Selectable)
            .Add(x => x.Current, Array.Empty<string>())
            .Add(x => x.Open, true)
            .Add(x => x.OnApply, _ => applied = true));

        cut.Find("button.ok").Click();

        Assert.False(applied);
        Assert.Contains("Select at least one paint", cut.Markup);
    }

    [Fact]
    public void OkAppliesTheCheckedNames()
    {
        IReadOnlyList<string>? applied = null;
        string first = PigmentLibrary.Selectable[0].Name;
        var cut = Render<PaletteEditorDialog>(p => p
            .Add(x => x.Catalogue, PigmentLibrary.Selectable)
            .Add(x => x.Current, new[] { first })
            .Add(x => x.Open, true)
            .Add(x => x.OnApply, names => applied = names));

        cut.Find("button.ok").Click();

        Assert.Equal(new[] { first }, applied);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~StylePanelTests|FullyQualifiedName~PaintListTests|FullyQualifiedName~PaletteEditorDialogTests"`
Expected: build errors (components missing). If bUnit 2.9's base class is not `BunitContext`, use the name its README gives (`TestContext` in 1.x) throughout.

- [ ] **Step 3: Implement the components**

`Web/Components/StylePanel.razor`:

```razor
@using PaintTranslator.Imaging.Styles
@using PaintTranslator.Web.Session

<div class="style-panel">
@foreach (IPipelineStage stage in Style.Stages)
{
    if (stage.Parameters.Count == 0) { continue; }
    <h3>@stage.DisplayName</h3>
    @foreach (StyleParameter parameter in stage.Parameters)
    {
        double value = Values[stage][parameter.Id];
        <label class="slider">
            <span>@StyleSliderScale.Caption(parameter, value)</span>
            <input type="range" min="0" max="@StyleSliderScale.Steps"
                   value="@StyleSliderScale.ToPosition(parameter, value)"
                   @oninput="e => Report(stage, parameter, e)" />
        </label>
    }
}
</div>

@code {
    [Parameter, EditorRequired] public StyleDefinition Style { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyDictionary<IPipelineStage, ParameterValues> Values { get; set; } = default!;
    [Parameter] public EventCallback<(IPipelineStage Stage, string Id, double Value)> OnChange { get; set; }

    private Task Report(IPipelineStage stage, StyleParameter parameter, ChangeEventArgs e) =>
        OnChange.InvokeAsync((stage, parameter.Id, StyleSliderScale.ToValue(parameter, int.Parse((string)e.Value!))));
}
```

`Web/Components/PaintList.razor`:

```razor
@using System.Drawing
@using PaintTranslator.Pigments

<div class="paint-list">
    <label class="select-all-row">
        <input type="checkbox" class="select-all" checked="@(Selected.Count == Paints.Count)" @onchange="e => ToggleAll((bool)e.Value!)" />
        Select all
    </label>
    @foreach (PigmentCoefficients paint in Paints)
    {
        <label class="paint-row">
            <input type="checkbox" class="paint" checked="@Selected.Contains(paint)" @onchange="e => Toggle(paint, (bool)e.Value!)" />
            <span class="swatch" style="background:@Css(paint)"></span>
            <span>@paint.Name</span>
        </label>
    }
</div>

@code {
    private static readonly Dictionary<PigmentCoefficients, string> swatches = new();

    [Parameter, EditorRequired] public IReadOnlyList<PigmentCoefficients> Paints { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<PigmentCoefficients> Selected { get; set; } = default!;
    [Parameter] public EventCallback<IReadOnlyList<PigmentCoefficients>> SelectedChanged { get; set; }

    private Task Toggle(PigmentCoefficients paint, bool on) =>
        SelectedChanged.InvokeAsync(Paints.Where(p => p == paint ? on : Selected.Contains(p)).ToList());

    private Task ToggleAll(bool on) =>
        SelectedChanged.InvokeAsync(on ? Paints.ToList() : Array.Empty<PigmentCoefficients>());

    // Mass tone from the same physics the converter uses, computed once per pigment
    // as PaintCheckedListBox does.
    private static string Css(PigmentCoefficients paint)
    {
        if (!swatches.TryGetValue(paint, out string? css))
        {
            Color c = SpectralRenderer.ToDisplayColor(KubelkaMunk.Mix(new[] { paint }, new[] { 1.0 }));
            css = $"rgb({c.R},{c.G},{c.B})";
            swatches[paint] = css;
        }
        return css;
    }
}
```

Check `KubelkaMunk.Mix` and `SpectralRenderer.ToDisplayColor` signatures in `Core/Pigments/` and copy the exact call `Controls/PaintCheckedListBox.cs:119-131` makes.

`Web/Components/PaletteEditorDialog.razor`:

```razor
@using PaintTranslator.Pigments

@if (Open)
{
    <div class="modal-backdrop">
        <div class="modal palette-editor">
            <h2>Edit Palette</h2>
            <label class="select-all-row">
                <input type="checkbox" checked="@(chosen.Count == Catalogue.Count)" @onchange="e => ToggleAll((bool)e.Value!)" /> Select all
            </label>
            <div class="catalogue">
            @foreach (PigmentCoefficients paint in Catalogue)
            {
                <label class="paint-row">
                    <input type="checkbox" checked="@chosen.Contains(paint.Name)" @onchange="e => Toggle(paint.Name, (bool)e.Value!)" />
                    <span>@paint.Name</span>
                </label>
            }
            </div>
            @if (refusal != null) { <div class="error">@refusal</div> }
            <div class="buttons">
                <button class="ok primary" @onclick="Ok">OK</button>
                <button class="cancel" @onclick="OnCancel">Cancel</button>
            </div>
        </div>
    </div>
}

@code {
    private HashSet<string> chosen = new();
    private string? refusal;

    [Parameter, EditorRequired] public IReadOnlyList<PigmentCoefficients> Catalogue { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyCollection<string> Current { get; set; } = default!;
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<string>> OnApply { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected override void OnParametersSet()
    {
        if (Open) { chosen = new HashSet<string>(Current, StringComparer.Ordinal); refusal = null; }
    }

    private void Toggle(string name, bool on) { if (on) chosen.Add(name); else chosen.Remove(name); }
    private void ToggleAll(bool on) { chosen = on ? Catalogue.Select(p => p.Name).ToHashSet(StringComparer.Ordinal) : new(); }

    private Task Ok()
    {
        if (chosen.Count == 0)
        {
            refusal = "Select at least one paint.";
            return Task.CompletedTask;
        }
        return OnApply.InvokeAsync(Catalogue.Select(p => p.Name).Where(chosen.Contains).ToList());
    }
}
```

`OnParametersSet` resetting `chosen` on every render while open would discard clicks; guard it with a `wasOpen` field so the reset happens only on the closed→open transition.

`Web/Components/Sidebar.razor`:

```razor
@using PaintTranslator.Imaging.Styles
@using PaintTranslator.Web.Session
@inject ConversionSession Session

<aside class="sidebar" aria-disabled="@Session.ImageOperationInProgress">
    <button @onclick="() => editorOpen = true">Edit Palette</button>
    <PaintList Paints="Session.AvailablePaints" Selected="Session.SelectedPaints" SelectedChanged="s => Session.SetSelectedPaints(s)" />
    <StylePanel Style="Session.Style" Values="Session.ValuesFor(Session.Style)" OnChange="v => Session.SetParameter(v.Stage, v.Id, v.Value)" />
    <button @onclick="Session.ResetActiveStyle">Reset to defaults</button>
    <label>Style
        <select value="@Session.Style.Name" @onchange="e => Session.SetStyle((string)e.Value!)">
            @foreach (StyleDefinition style in StyleRegistry.All) { <option value="@style.Name">@style.Name</option> }
        </select>
    </label>
    <label class="slider"><span>Brush mark: @Session.MarkPixels px</span>
        <input type="range" min="@ConversionSession.MarkMinimum" max="@ConversionSession.MarkMaximum" value="@Session.MarkPixels" @oninput="e => Session.SetMark(int.Parse((string)e.Value!))" /></label>
    <label class="slider"><span>@(Session.BlurRadius == 0 ? "Blur: off" : $"Blur: {Session.BlurRadius} px")</span>
        <input type="range" min="@ConversionSession.BlurMinimum" max="@ConversionSession.BlurMaximum" value="@Session.BlurRadius" @oninput="e => Session.SetBlur(int.Parse((string)e.Value!))" /></label>
    <PaletteEditorDialog Catalogue="PaintTranslator.Pigments.PigmentLibrary.Selectable" Current="Session.AvailablePaints.Select(p => p.Name).ToList()"
        Open="editorOpen" OnApply="Apply" OnCancel="() => editorOpen = false" />
</aside>

@code {
    private bool editorOpen;
    protected override void OnInitialized() => Session.Changed += () => InvokeAsync(StateHasChanged);
    private void Apply(IReadOnlyList<string> names) { Session.ApplyPalette(names); editorOpen = false; }
}
```

- [ ] **Step 4: Theme CSS**

Replace `Web/wwwroot/css/app.css` with the UiTheme palette (values from `UiTheme.cs:14-25`): variables `--window:#0f1217; --canvas:#0a0c10; --surface:#171c24; --surface-raised:#1f252f; --surface-hover:#2a323f; --border:#394251; --text:#ebeef4; --text-muted:#9ea9b9; --accent:#d6a64e; --accent-hover:#e7b962; --accent-pressed:#b8893a; --selection:#37465b;`. Body: `background:var(--window); color:var(--text); font: 13px/1.4 -apple-system, "Segoe UI", system-ui, sans-serif; margin:0; min-width:900px; min-height:600px`. Layout: `.app{display:grid; grid-template-rows:64px 1fr; grid-template-columns:1fr 300px; height:100vh}`, `.toolbar{grid-column:1/3}`, `.sidebar{overflow:auto; background:var(--surface); border-left:1px solid var(--border); padding:12px; display:flex; flex-direction:column; gap:10px}`, `.paint-list{flex:1; min-height:120px; overflow:auto}`, `.swatch{display:inline-block; width:14px; height:14px; border:1px solid var(--border); margin:0 6px; vertical-align:middle}`, buttons and `.primary` in accent, `input[type=range]{accent-color:var(--accent); width:100%}`, `.modal-backdrop{position:fixed; inset:0; background:rgba(0,0,0,.6); display:grid; place-content:center}`, `.modal{width:420px; max-height:620px; background:var(--surface-raised); border:1px solid var(--border); padding:16px; display:flex; flex-direction:column}`, `.catalogue{overflow:auto; flex:1}`, `.error{color:#f28b82}`, plus the canvas rules from Task 9.

- [ ] **Step 5: Run the tests, then look at it**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: all green. Then put `<Sidebar />` into the temporary host page's grid and run the app: unchecking paints re-renders; the editor refuses empty; applying a smaller palette persists across a reload (check `localStorage` in devtools); each style shows its own sliders; Reset only resets the current style.

- [ ] **Step 6: Stage**

`git add Web Tests.Web`

---

### Task 12: Toolbar, tooltip, wheels, download, and the real page

**Files:**
- Create: `Web/Components/Toolbar.razor`, `Web/Components/RecipeTooltip.razor`
- Modify: `Web/Pages/Index.razor` (replace the temporary host), `Web/wwwroot/index.html` (title), `Web/wwwroot/css/app.css`

**Interfaces:**
- Consumes: `ConversionSession`, `ImageLoader`, `CanvasInterop.DownloadPng`, `ImageCanvas.HoverChanged`.
- Produces: the finished single page.

- [ ] **Step 1: `RecipeTooltip`**

```razor
@if (At != null && Lines != null)
{
    <div class="recipe-tooltip" style="@Position()">
        @foreach (string line in Lines) { <div>@line</div> }
    </div>
}

@code {
    private const int Offset = 16;
    [Parameter] public System.Drawing.Point? At { get; set; }
    [Parameter] public string[]? Lines { get; set; }
    [Parameter] public int HostWidth { get; set; }
    [Parameter] public int HostHeight { get; set; }

    // Flip to the other side of the cursor near the right and bottom edges, as
    // MainForm.GetBlendTooltipBounds does; sizes are estimated from line count.
    private string Position()
    {
        int width = 260, height = (Lines!.Length * 18) + 12;
        int x = At!.Value.X + Offset, y = At.Value.Y + Offset;
        if (x + width > HostWidth) x = At.Value.X - Offset - width;
        if (y + height > HostHeight) y = At.Value.Y - Offset - height;
        return $"left:{x}px;top:{y}px";
    }
}
```

CSS: `.recipe-tooltip{position:absolute; background:var(--surface-raised); border:1px solid var(--border); color:var(--text); padding:6px 8px; font-size:12px; white-space:nowrap; pointer-events:none; z-index:5}`.

- [ ] **Step 2: `Toolbar`**

```razor
@using Microsoft.AspNetCore.Components.Forms
@using PaintTranslator.Web.Interop
@using PaintTranslator.Web.Session
@inject ConversionSession Session

<div class="toolbar">
    <label class="file-button primary">Open Photo
        <InputFile OnChange="OnFileChanged" accept="@ImageLoader.AcceptList" disabled="@Session.ImageOperationInProgress" />
    </label>
    <div class="menu">
        <button @onclick="() => wheelMenu = !wheelMenu" disabled="@Session.ImageOperationInProgress">Color Wheel ▾</button>
        @if (wheelMenu)
        {
            <div class="menu-items">
                <button @onclick="() => Pick(WheelDisplay.Traditional)">Traditional</button>
                <button @onclick="() => Pick(WheelDisplay.SelectedPaints)">Selected Golden Paints</button>
                <button @onclick="() => { Session.ShowPhoto(); wheelMenu = false; }" disabled="@(Session.Wheel == WheelDisplay.None)">Back to photo</button>
            </div>
        }
    </div>
    <label>Grid <input type="number" min="1" max="200" value="@Session.GridColumns" @onchange="e => Session.SetGrid(Clamp(e), Session.GridRows, Session.ShowGrid)" /></label>
    <label>Rows <input type="number" min="1" max="200" value="@Session.GridRows" @onchange="e => Session.SetGrid(Session.GridColumns, Clamp(e), Session.ShowGrid)" /></label>
    <label><input type="checkbox" checked="@Session.ShowGrid" @onchange="e => Session.SetGrid(Session.GridColumns, Session.GridRows, (bool)e.Value!)" /> Show grid</label>
    <button class="@(Session.MagnifierActive ? "toggle on" : "toggle")" @onclick="() => Session.SetMagnifier(!Session.MagnifierActive)">🔍 Zoom</button>
    <span class="spacer"></span>
    <button @onclick="Download" disabled="@(Session.Displayed == null || Session.Displayed == Session.SourcePhoto)">Download PNG</button>
    <span class="title">@Session.Title</span>
</div>

@code {
    private bool wheelMenu;
    [Parameter] public EventCallback<(byte[] Bytes, string Name)> FileChosen { get; set; }

    protected override void OnInitialized() => Session.Changed += () => InvokeAsync(StateHasChanged);

    private void Pick(WheelDisplay kind) { Session.ShowWheel(kind); wheelMenu = false; }
    private static int Clamp(ChangeEventArgs e) => Math.Clamp(int.TryParse((string?)e.Value, out int v) ? v : 2, 1, 200);

    private async Task OnFileChanged(InputFileChangeEventArgs e)
    {
        using var stream = e.File.OpenReadStream(maxAllowedSize: 512L * 1024 * 1024);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        await FileChosen.InvokeAsync((buffer.ToArray(), e.File.Name));
    }

    private void Download()
    {
        string stem = Session.Wheel != WheelDisplay.None
            ? "colour-wheel"
            : $"{System.IO.Path.GetFileNameWithoutExtension(Session.PhotoName ?? "image")}-{Session.Style.Name.ToLowerInvariant().Replace(' ', '-')}";
        CanvasInterop.DownloadPng(ImageCanvas.CanvasId, stem + ".png");
    }
}
```

The download button is disabled while the raw photo is displayed: only a converted frame or a wheel is worth saving, and the source is what the user already has.

- [ ] **Step 3: The real page**

`Web/Pages/Index.razor`:

```razor
@page "/"
@using System.Diagnostics.CodeAnalysis
@using PaintTranslator.Imaging
@using PaintTranslator.Web.Components
@using PaintTranslator.Web.Session
@inject ConversionSession Session
@inject IJSRuntime Js

<PageTitle>@Session.Title</PageTitle>
<div class="app">
    <Toolbar FileChosen="f => LoadBytes(f.Bytes, f.Name)" />
    <div class="canvas-column" @ref="canvasColumn">
        <ImageCanvas HoverChanged="h => hover = h" Resized="r => { hostWidth = r.Width; hostHeight = r.Height; }" />
        <RecipeTooltip At="hover.At" Lines="hover.Lines" HostWidth="hostWidth" HostHeight="hostHeight" />
        @if (error != null) { <div class="toast error" @onclick="() => error = null">@error</div> }
    </div>
    <Sidebar />
</div>

@code {
    private (System.Drawing.Point? At, string[]? Lines) hover;
    private string? error;
    private ElementReference canvasColumn;
    private int hostWidth = 1000, hostHeight = 700;
    private DotNetObjectReference<Index>? self;

    protected override void OnInitialized() => Session.Changed += () => InvokeAsync(StateHasChanged);

    protected override async Task OnAfterRenderAsync(bool first)
    {
        if (!first) return;
        self = DotNetObjectReference.Create(this);
        var module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/interop.js");
        await module.InvokeVoidAsync("bindFileInputs", self);
    }

    [JSInvokable]
    public Task OnFileBytes(byte[] bytes, string name) => LoadBytes(bytes, name);

    private async Task LoadBytes(byte[] bytes, string name)
    {
        error = null;
        Session.BeginImageOperation();
        try
        {
            PixelImage photo = await ImageLoader.LoadAsync(bytes, name);
            Session.LoadPhoto(photo, name);
        }
        catch (ImageLoadException ex) { error = ex.Message; }
        finally { Session.EndImageOperation(); }
    }
}
```

Give the page an `Index.razor.cs` partial carrying `[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Index))]`.

Set `<title>Paint Translator</title>` and the dark `background-color` on `<body>` in `index.html`; keep the Blazor loading placeholder.

- [ ] **Step 4: Manual parity checklist**

Run `dotnet run --project Web/PaintTranslator.Web.csproj` and, in Chrome, walk the WinForms behaviours: load a photo → preview then full; every slider re-renders; style switch keeps per-style values; Reset resets only the current style; a new photo resets the mark slider; both wheels display, hovering the selected wheel lists paints, the traditional wheel shows RGB only, hovering transparent surround shows nothing; sliders do nothing while a wheel shows; Back to photo resumes rendering; unchecking paints regenerates the selected wheel; grid tracks zoom; magnifier steps; Download PNG saves a file that reopens as the converted image; the error toast appears for an unsupported file. Then repeat load, zoom, pan, pinch, download in Safari.

- [ ] **Step 5: Stage**

`git add Web`

---

### Task 13: The launcher, docs, and the Release run

**Files:**
- Create: `PaintTranslator.command`
- Modify: `CLAUDE.md`, `.claude/handoff/PROJECT.md`

**Interfaces:**
- Consumes: `Web/serve.py` (Task 1), the `WasmEnableThreads` line in the csproj (Task 3).

- [ ] **Step 1: Write the launcher**

`PaintTranslator.command`:

```zsh
#!/bin/zsh
# Double-click from Finder: publishes the web app in Release, serves it locally
# and opens the browser. Release because Debug WebAssembly runs the mixing
# kernel several times slower than what the deployed site will do.
set -euo pipefail
cd "$(dirname "$0")"
printf '\033]0;PaintTranslator\007'

pause_and_exit() { echo; echo "Press any key to close."; read -k1 -s; exit 1; }

if ! command -v dotnet >/dev/null 2>&1; then
  echo "The .NET SDK was not found. Install .NET 10 from https://dotnet.microsoft.com/download"
  pause_and_exit
fi
if ! dotnet workload list 2>/dev/null | grep -q '^wasm-tools'; then
  echo "The wasm-tools workload is missing (needed for the AOT build). Run:"
  echo "    dotnet workload install wasm-tools"
  pause_and_exit
fi

PROJECT="Web/PaintTranslator.Web.csproj"
OUT="Web/bin/publish"
echo "Publishing $PROJECT (the first AOT publish takes several minutes)..."
if ! dotnet publish "$PROJECT" -c Release -o "$OUT" --nologo -v quiet; then
  echo "Publish failed; see the errors above."
  pause_and_exit
fi

ISOLATE=()
if grep -q '<WasmEnableThreads>true</WasmEnableThreads>' "$PROJECT"; then
  ISOLATE=(--isolate)
fi
exec python3 Web/serve.py "$OUT/wwwroot" --port 5180 --open "${ISOLATE[@]}"
```

```bash
chmod +x PaintTranslator.command
```

- [ ] **Step 2: Run it both ways**

From a terminal: `./PaintTranslator.command`. Expected: publish output, then `Serving ... at http://127.0.0.1:5180/`, the default browser opens, the app loads and converts a photo. Ctrl+C stops it. Then double-click the file in Finder: a Terminal window titled PaintTranslator opens and does the same. If macOS refuses to open it, run `xattr -d com.apple.quarantine PaintTranslator.command` once and note that in the CLAUDE.md commands section.

Re-run `./PaintTranslator.command` with nothing changed: the publish step should take seconds, not minutes.

- [ ] **Step 3: Update `CLAUDE.md`**

Add to Commands:

```
dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj   # bUnit + session tests, cross-platform
dotnet run --project Web/PaintTranslator.Web.csproj      # the web app, Debug, dev server
./PaintTranslator.command                                # Release publish + local serve + open browser
Tools/BuildDecoders/build.sh                             # regenerates Web/wwwroot/js/decoders (needs Node; offline after first install)
```

Add to Architecture a short "Web app" paragraph: `Web/` is the Blazor WebAssembly consumer of Core; `Web/Session` holds the UI-neutral port of `MainForm` (scheduler, session, formatter, codec); `wwwroot/js/interop.js` is decision-free glue; decoders are vendored and licensed per `js/decoders/LICENSES.md`; the threading configuration and the spike numbers are in the spec. Mention that `Tests.Web` is the third test project and that `Tests/` stays at 403.

- [ ] **Step 4: Full verification**

```bash
dotnet build PaintTranslator.sln
dotnet test Tests/PaintTranslator.Tests.csproj
dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj
```

Expected: 0 errors, one ImageSharp notice, 403 green, all Web tests green.

- [ ] **Step 5: Stage and close the handoff**

```bash
git add PaintTranslator.command CLAUDE.md
git status --short | head -40
```

In `.claude/handoff/PROJECT.md`: Task 4 done (spec + plan), add "Task 6: execute Blazor plan — done" with the spike decision, and set the next step to sub-project 3 (deployment) plus the owner's manual review of the staged tree. Move any open problems (Safari gestures, memory on large photos) into Open problems with what was observed.

---

## Self-review notes

- **Spec coverage.** Spike (T1–T3), image input incl. all formats and the dropped URL path (T10), render loop and configuration A/B behaviour (T7, T8 `PipelineRenderer`), canvas/viewport/overlays/magnifier/cursor (T9), tooltip (T6, T12), wheels + Back to photo (T8, T12), download (T9 JS, T12), palette/styles/sliders (T5, T6, T11), launcher and `serve.py` (T1, T13), tests listed in the spec (T2, T4–T8, T11), CLAUDE.md (T13). The WinForms behaviours 1–5 are tested in T8. The "wait cursor with `Task.Yield()`" from the spec is delivered by `.busy` in T9 plus `StateChanged` firing before the full render in T7; verify visually in T12 that the cursor changes on configuration A.
- **Deviation from the spec, flagged for the owner:** a "Back to photo" menu item (T8 `ShowPhoto`, T12). WinForms can only leave a wheel by loading another photo. Remove the two lines if the owner objects.
- **Type consistency.** `RenderRequest`, `IFrameRenderer`, `RenderScheduler` signatures are used identically in T7, T8; `PixelCodec.ToRgba`/`ToPixelImage` in T9, T10; `CanvasInterop.CanvasId` lives on `ImageCanvas` (T9) and is referenced from T12; `ImageLoader.AcceptList` in T10, T12; `PaletteStore.Key` in T5, T8 tests.
- **Known uncertainties an implementer must resolve on the spot, each marked inline:** bUnit 2.9 base-class name (T11 step 2); `ImageViewport.ImageSize` refit semantics (T9 step 3); `ImageFileFormat` member names (T10 step 3); `KubelkaMunk.Mix`/`SpectralRenderer.ToDisplayColor` exact call (T11 step 3); esbuild loader for the PSD wasm (T10 step 1); the arithmetic in one scheduler test (T7 step 4).
