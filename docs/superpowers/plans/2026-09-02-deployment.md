# Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One command (`Tools/Deploy/deploy.sh`) produces a clean static folder (`deploy/site/`) and a Caddy container image (`painttranslator:latest`) from a single Docker-side Release publish; the site gains a Credits dialog, the ten parked polish findings are fixed, and a 24-megapixel photo is measured against the WebAssembly heap.

**Architecture:** A three-stage Dockerfile under `Tools/Deploy/` (SDK + `wasm-tools` publish → `scratch` export stage → `caddy:2-alpine` serve stage) is the only build path; the script exports the `site` stage to a folder and tags the `serve` stage. The Caddyfile reproduces what `Web/serve.py` does for the launcher (brotli, SPA fallback, optional isolation headers) and adds the immutable/no-cache split the fingerprinted publish allows. App changes are confined to `Web/` and `Tests.Web/`; Core is untouched.

**Tech Stack:** Docker 29 with BuildKit (`docker build --output`), `mcr.microsoft.com/dotnet/sdk:10.0`, `caddy:2-alpine`, bash, .NET 10 SDK 10.0.400 on macOS, Blazor WebAssembly, xUnit 2.9.3, bUnit 2.9.0, headless Google Chrome, `sips` (macOS built-in).

**Spec:** `docs/superpowers/specs/2026-09-02-deployment-design.md`

## Global Constraints

- **Never commit. Never branch. Never create a worktree.** Stage with `git add` and stop. (`CLAUDE.md`)
- Working directory for every command: `/Users/sean/Desktop/ADHD Meadows/PaintTranslator` (the path has a space; quote it).
- `Core/` is not edited by this plan. `Web/serve.py` and `PaintTranslator.command` are not edited by this plan (spec "Non-goals").
- No host-specific files: no `_headers`, no `CNAME`, no `.github/workflows`. (spec "Decisions")
- Nothing under `Web/` may reference `System.Drawing.Common`.
- `wwwroot/js/interop.js` stays decision-free glue: it marshals and forwards, and makes no choice `Web/Session` doesn't already make (`CLAUDE.md`). Unit normalisation of wheel deltas is marshalling.
- Doc comments carry reasoning, not signature restatements (`CLAUDE.md`). Follow the `csharp-code-comments` skill for every new class and method.
- A clean build is 0 errors; the only accepted warning is the ImageSharp licence notice from `Tests/`.
- Run tests from the repo root: `dotnet test Tests/PaintTranslator.Tests.csproj` (403, must stay green) and `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj` (46 at the start of this plan; the count grows in Tasks 3 and 4 and the observed final count goes into `CLAUDE.md` in Task 7).
- Never run a bench or a heap measurement while an AOT publish is running (timings skew).
- No size cap or resample of input photos (spec "Heap measurement").
- Update `.claude/handoff/PROJECT.md` at the end of every task (status row, next step, problems).

---

## File map

**Create**
- `.dockerignore` — build-context exclusions (Task 1)
- `Tools/Deploy/Dockerfile` — `build`, `site`, `serve` stages (Task 1 adds `build` + `site`; Task 2 adds `serve`)
- `Tools/Deploy/deploy.sh` — the one command (Task 1 writes it with the export; Task 2 adds the image build and the summary)
- `Tools/Deploy/Caddyfile` (Task 2)
- `Tools/Deploy/README.md` (Task 2)
- `Web/Session/VendoredLibrary.cs` — `VendoredLibrary` record + `Credits` list (Task 3)
- `Web/Components/CreditsDialog.razor` (Task 3)
- `Tests.Web/CreditsTests.cs` (Task 3)
- `Tests.Web/SessionDoubles.cs` — shared `MemoryStore`, `FlakyStore`, `NullRenderer`, `NeverDelay`, `NewSession` (Task 4)
- `Tests.Web/ToolbarTests.cs`, `Tests.Web/SidebarTests.cs` (Task 4)
- `Web/wwwroot/favicon.svg` (Task 5)

**Modify**
- `.gitignore` — `/deploy/` (Task 1)
- `Web/Components/Sidebar.razor` — Credits button + dialog (Task 3); `aria-disabled` value, banner dismiss (Task 4)
- `Web/wwwroot/css/app.css` — credits styles (Task 3); `[aria-disabled="true"]`, `.menu-backdrop`, `.banner` (Task 4); residue removal + `.toolbar` merge (Task 5)
- `Web/Components/Toolbar.razor` — menu dismiss (Task 4)
- `Web/Session/ConversionSession.cs` — `DismissPaletteSaveWarning` (Task 4)
- `Web/Components/ImageCanvas.razor.cs` — guard `OnSessionChanged` (Task 4)
- `Tests.Web/ConversionSessionTests.cs` — use the shared doubles; add the dismiss test (Task 4)
- `Web/wwwroot/js/interop.js` — `deltaMode`, paste filter, dragover comment, grid alpha (Task 5)
- `Web/wwwroot/index.html` — favicon link, template comment removal (Task 5)
- `Web/PaintTranslator.Web.csproj` — `EmccMaximumHeapSize` only if Task 6's measurement fails (Task 6)
- `docs/superpowers/specs/2026-09-02-deployment-design.md` — "Measurement" section (Task 6)
- `CLAUDE.md` — deploy command, deployment paragraph, `blazor.boot.json` correction, `Tests.Web` count (Task 7)
- `.claude/handoff/PROJECT.md` (every task)

---

### Task 1: Docker build context and the static-folder export

**Files:**
- Create: `.dockerignore`, `Tools/Deploy/Dockerfile`, `Tools/Deploy/deploy.sh`
- Modify: `.gitignore`

**Interfaces:**
- Produces: Dockerfile stages `build` (publish at `/out`) and `site` (`FROM scratch`, published `wwwroot` at `/`); `deploy/site/` at the repo root; `deploy.sh` exit code 0 on success.

- [ ] **Step 1: Write `.dockerignore`**

`COPY Web/ Web/` would otherwise ship the host's `Web/bin` and `Web/obj` into the container, where a stale `obj/Release` can make the relinked runtime keep an old configuration (the same trap `CLAUDE.md` records for `WasmEnableThreads`).

```
# Build context = repo root (docker build ... .). Only Core/ and Web/ are COPYed,
# but host build output under them must not reach the container: a stale obj/
# would be picked up by the publish and a stale relink kept silently.
**/bin/
**/obj/
.git/
.claude/
.superpowers/
deploy/
docs/
Tests/
Tests.Web/
Tests.Windows/
BlendTests/
Tools/BuildDecoders/node_modules/
Tools/IngestSpectra/data/
```

- [ ] **Step 2: Write the Dockerfile with the `build` and `site` stages**

```dockerfile
# syntax=docker/dockerfile:1
# Three stages: build (SDK + wasm-tools, the same Release publish the launcher runs),
# site (FROM scratch, so `docker build --target site --output` exports the published
# wwwroot as a plain folder) and serve (Task 2). One AOT compile feeds both outputs,
# which is why the folder and the image never drift.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# AOT, relinking and IL stripping all need the wasm-tools workload; without it the
# publish silently falls back to the interpreter, which is several times slower on
# the mixing kernel (spike, sub-project 2).
RUN dotnet workload install wasm-tools
WORKDIR /src
# Restore in its own layer keyed on the two project files, so editing a .cs file
# does not re-download packages.
COPY Core/PaintTranslator.Core.csproj Core/
COPY Web/PaintTranslator.Web.csproj Web/
RUN dotnet restore Web/PaintTranslator.Web.csproj
COPY Core/ Core/
COPY Web/ Web/
# Every publish decision (RunAOTCompilation, WasmStripILAfterAOT, InvariantGlobalization,
# OverrideHtmlAssetPlaceholders, WasmEnableThreads) lives in the csproj; nothing is
# added here so the launcher and the deploy cannot disagree.
RUN dotnet publish Web/PaintTranslator.Web.csproj -c Release -o /out --nologo

FROM scratch AS site
COPY --from=build /out/wwwroot /
```

- [ ] **Step 3: Write `deploy.sh` (export half)**

```bash
#!/usr/bin/env bash
# One command: a clean static folder in deploy/site and (Task 2) a Caddy image.
# Needs Docker only; the .NET SDK and wasm-tools live inside the build stage.
set -euo pipefail
cd "$(dirname "$0")/../.."

if ! command -v docker >/dev/null 2>&1; then
  echo "docker was not found. Install Docker Desktop from https://www.docker.com/products/docker-desktop/" >&2
  exit 1
fi
if ! docker info >/dev/null 2>&1; then
  echo "The Docker daemon is not running. Start Docker Desktop and try again." >&2
  exit 1
fi

# Deleting first is what makes the folder clean: `dotnet publish -o` into an existing
# directory leaves stale files behind (the launcher's wwwroot picked up a samples/
# directory that way), and --output only adds files, it never removes them.
rm -rf deploy/site
mkdir -p deploy
docker build -f Tools/Deploy/Dockerfile --target site --output "type=local,dest=deploy/site" .

echo
echo "Static site:  deploy/site"
```

Then `chmod +x Tools/Deploy/deploy.sh`.

- [ ] **Step 4: Ignore the output folder**

Append to `.gitignore`:

```
/deploy/
```

- [ ] **Step 5: Run it**

Run: `Tools/Deploy/deploy.sh`
Expected: exit 0. The first run downloads the SDK image and the workload and does a cold AOT compile (several minutes, allow up to 15); print progress but do not interrupt it. `bash` is used rather than `zsh` so the script also runs unchanged on a Linux server.

- [ ] **Step 6: Verify the folder**

```bash
ls deploy/site                      # css  index.html  index.html.br  index.html.gz  js  _framework  (nothing else)
ls deploy/site/_framework | grep -c '\.br$'     # > 0
ls deploy/site/_framework/dotnet.native.*.wasm  # one fingerprinted file
test ! -e deploy/site/samples && echo "clean"
grep -c 'fingerprint' deploy/site/index.html    # 0 — the placeholder was resolved
git status --short | grep -c '^?? deploy' || true   # 0 — ignored
```

- [ ] **Step 7: Run a second time and confirm the cache**

Run: `Tools/Deploy/deploy.sh` again. Expected: completes in well under a minute with every stage `CACHED`.

- [ ] **Step 8: Stage**

```bash
git add .dockerignore .gitignore Tools/Deploy/Dockerfile Tools/Deploy/deploy.sh
```

Update `.claude/handoff/PROJECT.md`.

---

### Task 2: Caddy serve stage, Caddyfile, README, end-to-end verification

**Files:**
- Create: `Tools/Deploy/Caddyfile`, `Tools/Deploy/README.md`
- Modify: `Tools/Deploy/Dockerfile` (append the `serve` stage), `Tools/Deploy/deploy.sh` (add the image build and the summary)

**Interfaces:**
- Consumes: the `build` stage's `/out/wwwroot` from Task 1.
- Produces: image `painttranslator:latest` listening on port 80.

- [ ] **Step 1: Write the Caddyfile**

```
# What Web/serve.py does for the launcher, for Caddy. serve.py is the local
# reference; this file must keep doing the same four things plus caching.
:80 {
	root * /srv

	# Client-side routes (/bench) arrive on reload as requests for files that do not
	# exist. Hand back index.html only when the path has no extension, so a missing
	# .js or .wasm still 404s and a broken asset link stays visible.
	@route {
		not file
		not path *.*
	}
	rewrite @route /index.html

	# Everything under _framework is fingerprinted at publish time
	# (OverrideHtmlAssetPlaceholders): a republish changes the names, so the old
	# entries are never requested again and a year-long immutable cache is safe.
	# index.html, css/ and js/ are not fingerprinted and must be revalidated on
	# every load, which is what no-cache (not no-store) asks for.
	@fingerprinted path /_framework/*
	header @fingerprinted Cache-Control "public, max-age=31536000, immutable"
	@plain not path /_framework/*
	header @plain Cache-Control "no-cache"

	# Only with <WasmEnableThreads>true</WasmEnableThreads> in Web/PaintTranslator.Web.csproj:
	# SharedArrayBuffer needs cross-origin isolation. The launcher reads the csproj at
	# run time and adds these itself; a Caddyfile cannot, so this is a manual switch.
	# header Cross-Origin-Opener-Policy "same-origin"
	# header Cross-Origin-Embedder-Policy "require-corp"

	# Serves foo.wasm.br as foo.wasm with Content-Encoding: br when the browser accepts
	# it (gzip otherwise), which is what dotnet publish emits and serve.py negotiates.
	file_server {
		precompressed br gzip
	}
}
```

Tabs are Caddy's conventional indentation; keep them.

- [ ] **Step 2: Append the `serve` stage to the Dockerfile**

```dockerfile

FROM caddy:2-alpine AS serve
COPY Tools/Deploy/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /out/wwwroot /srv
EXPOSE 80
```

- [ ] **Step 3: Extend `deploy.sh`**

Replace the final two `echo` lines with:

```bash
docker build -f Tools/Deploy/Dockerfile --target serve -t painttranslator:latest .

echo
echo "Static site:  deploy/site            (upload to any static host; see Tools/Deploy/README.md)"
echo "Image:        painttranslator:latest (docker run --rm -p 8080:80 painttranslator:latest)"
```

- [ ] **Step 4: Write the README**

```markdown
# Deploying Paint Translator

One command, needs Docker only:

    Tools/Deploy/deploy.sh

It produces two things from a single Release (AOT) publish that runs inside Docker:

- `deploy/site/` — the static site, rebuilt from empty every run.
- `painttranslator:latest` — a Caddy image serving that same folder on port 80.

The first run downloads the .NET SDK image and the `wasm-tools` workload and does a cold
AOT compile (several minutes). Later runs reuse the cache; only a change under `Core/`
or `Web/` triggers another compile.

## Try the image locally

    docker run --rm -p 8080:80 painttranslator:latest

Open http://127.0.0.1:8080/.

## Upload the folder to a static host

Copy the contents of `deploy/site/` to the host's root. Two things make it fast; both
are optional for correctness:

1. **Pre-compressed files.** Every file has a `.br` and a `.gz` sibling. A host that
   serves `foo.wasm.br` as `foo.wasm` with `Content-Encoding: br` cuts the first load
   from ~25 MB to a few MB. A host that ignores them still works, only slower.
2. **Caching.** Everything under `_framework/` is fingerprinted, so it can be cached
   as `Cache-Control: public, max-age=31536000, immutable`. `index.html`, `css/` and
   `js/` are not fingerprinted and should be `Cache-Control: no-cache`.

`Tools/Deploy/Caddyfile` is the reference for both rules and for the single-page-app
fallback (an extensionless path that matches no file serves `index.html`).

## Run the image on a rented server

Build for the server's architecture if it is not the same as the machine running
`deploy.sh` (an Apple-silicon Mac builds arm64 by default):

    docker build -f Tools/Deploy/Dockerfile --target serve --platform linux/amd64 -t painttranslator:latest .

Then on the server, with a domain pointed at it, change the first line of the
Caddyfile from `:80` to the domain (for example `paint.example.com`) and run with
ports 80 and 443 published. Caddy obtains and renews the certificate itself:

    docker run -d --restart unless-stopped -p 80:80 -p 443:443 painttranslator:latest

## Threads

`WasmEnableThreads` is `false` (see `Web/PaintTranslator.Web.csproj`). If it is ever set
to `true`, uncomment the two cross-origin isolation headers in the Caddyfile; without
them `SharedArrayBuffer` is unavailable and the runtime will not start.
```

- [ ] **Step 5: Build and run**

```bash
Tools/Deploy/deploy.sh
docker run --rm -d --name pt-verify -p 8080:80 \
  -v "$PWD/Web/bin/publish/wwwroot/samples:/srv/samples:ro" painttranslator:latest
```

(`Web/bin/publish/wwwroot/samples/sample.jpg` was left there by sub-project 2's headless checks. If it is missing, publish the launcher build once with `dotnet publish Web/PaintTranslator.Web.csproj -c Release -o Web/bin/publish` and create `samples/sample.jpg` from any JPEG.)

- [ ] **Step 6: Header checks**

```bash
WASM=$(cd deploy/site/_framework && ls dotnet.native.*.wasm)
curl -s -o /dev/null -D - -H 'Accept-Encoding: br' "http://127.0.0.1:8080/_framework/$WASM" | grep -i -E '^(content-encoding|cache-control|content-type)'
# expect: content-encoding: br / cache-control: public, max-age=31536000, immutable / content-type: application/wasm
curl -s -o /dev/null -D - http://127.0.0.1:8080/index.html | grep -i '^cache-control'
# expect: cache-control: no-cache
curl -s -o /dev/null -w '%{http_code} %{content_type}\n' http://127.0.0.1:8080/bench     # 200 text/html...
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8080/nope.js                  # 404
```

- [ ] **Step 7: Headless Chrome through the container**

```bash
PROFILE=$(mktemp -d)
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --disable-gpu \
  --no-first-run --user-data-dir="$PROFILE" --enable-logging=stderr --v=0 \
  --remote-debugging-port=0 "http://127.0.0.1:8080/?autofile=samples/sample.jpg" 2> "$PROFILE/log" &
sleep 40; pkill -f headless=new
grep -o 'CONSOLE.*' "$PROFILE/log" | grep -E 'HOST TITLE|HOST ERROR|rror|Uncaught'
```

Expected: a `HOST TITLE Paint Translator - samples/sample.jpg` line followed by the converted title, no `HOST ERROR`, no `Uncaught`. Then `docker rm -f pt-verify`.

- [ ] **Step 8: Stage**

```bash
git add Tools/Deploy/Dockerfile Tools/Deploy/deploy.sh Tools/Deploy/Caddyfile Tools/Deploy/README.md
```

Update `.claude/handoff/PROJECT.md`.

---

### Task 3: Credits dialog

**Files:**
- Create: `Web/Session/VendoredLibrary.cs`, `Web/Components/CreditsDialog.razor`, `Tests.Web/CreditsTests.cs`
- Modify: `Web/Components/Sidebar.razor`, `Web/wwwroot/css/app.css`

**Interfaces:**
- Produces: `PaintTranslator.Web.Session.VendoredLibrary(string Name, string Version, string Licence, string Url)` record; `static class Credits { IReadOnlyList<VendoredLibrary> Decoders; VendoredLibrary Runtime; IReadOnlyList<VendoredLibrary> All; }`; component `CreditsDialog` with `[Parameter] bool Open` and `[Parameter] EventCallback OnClose`.

- [ ] **Step 1: Write the failing tests**

`Tests.Web/CreditsTests.cs`:

```csharp
using PaintTranslator.Web.Components;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class CreditsTests : BunitContext
{
    /// <summary>Walks up from the test binary to the directory holding the solution, so the
    /// test reads the real LICENSES.md rather than a copy that could drift.</summary>
    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "PaintTranslator.sln")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("PaintTranslator.sln not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void DecoderRowsMatchLicensesMd()
    {
        string path = Path.Combine(RepoRoot(), "Web", "wwwroot", "js", "decoders", "LICENSES.md");
        // Each line: "- <file> — <name> <version>[ extra words] — <licence> — <url>"
        var fromFile = File.ReadAllLines(path)
            .Where(l => l.StartsWith("- "))
            .Select(l => l[2..].Split(" — "))
            .Select(parts => (Name: parts[1].Split(' ')[0], Version: parts[1].Split(' ')[1], Licence: parts[2], Url: parts[3]))
            .OrderBy(t => t.Name)
            .ToList();
        var fromCode = Credits.Decoders
            .Select(d => (d.Name, d.Version, d.Licence, d.Url))
            .OrderBy(t => t.Name)
            .ToList();

        Assert.Equal(3, fromFile.Count);
        Assert.Equal(fromFile, fromCode);
    }

    [Fact]
    public void RuntimeRowIsDotNetUnderMit()
    {
        Assert.Equal(".NET", Credits.Runtime.Name);
        Assert.Equal("MIT", Credits.Runtime.Licence);
        Assert.Equal(Credits.Decoders.Count + 1, Credits.All.Count);
    }

    [Fact]
    public void OpenDialogListsEveryLibraryAndCloseInvokesCallback()
    {
        bool closed = false;
        var cut = Render<CreditsDialog>(p => p.Add(x => x.Open, true).Add(x => x.OnClose, () => closed = true));

        Assert.Equal(Credits.All.Count, cut.FindAll("li.credit").Count);
        foreach (VendoredLibrary lib in Credits.All) Assert.Contains(lib.Name, cut.Markup);
        Assert.Contains("LGPL", cut.Markup);

        cut.Find("button.close").Click();
        Assert.True(closed);
    }

    [Fact]
    public void ClosedDialogRendersNothing()
    {
        var cut = Render<CreditsDialog>(p => p.Add(x => x.Open, false));
        Assert.Equal(string.Empty, cut.Markup.Trim());
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~CreditsTests"`
Expected: build error, `Credits`/`CreditsDialog` not found.

- [ ] **Step 3: Write `Web/Session/VendoredLibrary.cs`**

```csharp
namespace PaintTranslator.Web.Session;

/// <summary>One third-party component the shipped site contains, as shown in the Credits
/// dialog. Name and version are spelled the way LICENSES.md spells them because
/// CreditsTests compares the two files literally; that comparison, not this list, is what
/// keeps the dialog honest when Tools/BuildDecoders/build.sh bumps a decoder.</summary>
public sealed record VendoredLibrary(string Name, string Version, string Licence, string Url);

/// <summary>The rows behind the Credits dialog. The decoders are the reason the dialog
/// exists: libheif-js is LGPL-3.0, and a public site should say so where a visitor can
/// see it rather than only in a markdown file in the repository.</summary>
public static class Credits
{
    public static readonly IReadOnlyList<VendoredLibrary> Decoders =
    [
        new("libheif-js", "1.18.2", "LGPL-3.0", "https://github.com/catdad-experiments/libheif-js"),
        new("utif", "3.1.0", "MIT", "https://github.com/photopea/UTIF.js"),
        new("@webtoon/psd", "0.4.0", "MIT", "https://github.com/webtoon/psd"),
    ];

    public static readonly VendoredLibrary Runtime =
        new(".NET", "10", "MIT", "https://github.com/dotnet/runtime");

    public static readonly IReadOnlyList<VendoredLibrary> All = [.. Decoders, Runtime];
}
```

- [ ] **Step 4: Write `Web/Components/CreditsDialog.razor`**

```razor
@using PaintTranslator.Web.Session

@if (Open)
{
    <div class="modal-backdrop">
        <div class="modal credits">
            <h2>Credits</h2>
            <ul class="credits-list">
                @foreach (VendoredLibrary lib in Credits.All)
                {
                    <li class="credit">
                        <a href="@lib.Url" target="_blank" rel="noopener">@lib.Name</a>
                        <span class="version">@lib.Version</span>
                        <span class="licence">@lib.Licence</span>
                    </li>
                }
            </ul>
            <p class="credits-note">
                libheif-js is LGPL-3.0. It is loaded unmodified as a separate script
                (<code>js/decoders/libheif-bundle.js</code>), so it can be replaced with another
                build of the same library without touching the rest of the app.
            </p>
            <div class="buttons">
                <button class="close primary" @onclick="OnClose">Close</button>
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
```

- [ ] **Step 5: Add the button to the sidebar**

In `Web/Components/Sidebar.razor`, replace the Edit Palette button line with a row holding both buttons, and add the dialog next to the palette editor:

```razor
    <div class="button-row">
        <button disabled="@Session.ImageOperationInProgress" @onclick="() => editorOpen = true">Edit Palette</button>
        <button class="credits-button" @onclick="() => creditsOpen = true">Credits</button>
    </div>
```

```razor
    <CreditsDialog Open="creditsOpen" OnClose="() => creditsOpen = false" />
```

and in `@code` add `private bool creditsOpen;`. Credits is not gated by `ImageOperationInProgress`: it changes no state.

- [ ] **Step 6: CSS**

Append to `Web/wwwroot/css/app.css`:

```css
.button-row {
    display: flex;
    gap: 8px;
}

.credits-list {
    list-style: none;
    padding: 0;
    margin: 0 0 12px;
}

    .credits-list .credit {
        display: flex;
        gap: 8px;
        padding: 4px 0;
    }

    .credits-list a {
        color: var(--accent);
    }

    .credits-list .version,
    .credits-list .licence {
        color: var(--text-muted);
    }

.credits-note {
    color: var(--text-muted);
    font-size: 12px;
}
```

- [ ] **Step 7: Run the tests**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: all green, 50 tests (46 + 4).

- [ ] **Step 8: Stage**

```bash
git add Web/Session/VendoredLibrary.cs Web/Components/CreditsDialog.razor Web/Components/Sidebar.razor Web/wwwroot/css/app.css Tests.Web/CreditsTests.cs
```

Update `.claude/handoff/PROJECT.md`.

---

### Task 4: C#-side polish (menu dismiss, aria-disabled, banner dismiss, guarded interop)

**Files:**
- Create: `Tests.Web/SessionDoubles.cs`, `Tests.Web/ToolbarTests.cs`, `Tests.Web/SidebarTests.cs`
- Modify: `Tests.Web/ConversionSessionTests.cs`, `Web/Components/Toolbar.razor`, `Web/Components/Sidebar.razor`, `Web/Session/ConversionSession.cs`, `Web/Components/ImageCanvas.razor.cs`, `Web/wwwroot/css/app.css`

**Interfaces:**
- Produces: `ConversionSession.DismissPaletteSaveWarning()`; test doubles `MemoryStore`, `FlakyStore` (with `bool FailNextSet`), `NullRenderer` (with `int Calls`), `SessionDoubles.NeverDelay`, `SessionDoubles.NewSession(IKeyValueStore? store = null, IFrameRenderer? renderer = null)` in namespace `PaintTranslator.Web.Tests`.

- [ ] **Step 1: Move the session doubles to a shared file**

Create `Tests.Web/SessionDoubles.cs` with the three classes currently nested in `ConversionSessionTests` (same bodies, now top-level `internal sealed`) plus the two helpers:

```csharp
using PaintTranslator.Imaging;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

internal sealed class MemoryStore : IKeyValueStore
{
    private readonly Dictionary<string, string> values = new();
    public string? Get(string key) => values.TryGetValue(key, out string? v) ? v : null;
    public bool Set(string key, string value) { values[key] = value; return true; }
}

/// <summary>Lets one test fail a save on demand, to pin ConversionSession.ApplyPalette's
/// PaletteSaveFailed bookkeeping without a real localStorage.</summary>
internal sealed class FlakyStore : IKeyValueStore
{
    private readonly Dictionary<string, string> values = new();
    public bool FailNextSet;
    public string? Get(string key) => values.TryGetValue(key, out string? v) ? v : null;
    public bool Set(string key, string value)
    {
        if (FailNextSet) return false;
        values[key] = value;
        return true;
    }
}

internal sealed class NullRenderer : IFrameRenderer
{
    public int Calls;
    public Task<PixelImage?> RenderAsync(RenderRequest request, CancellationToken token)
    {
        Calls++;
        return Task.FromResult<PixelImage?>(request.Source);
    }
}

/// <summary>Shared by the session tests and the bUnit component tests that inject a
/// ConversionSession: a session whose debounce never elapses, so nothing renders unless
/// a test drives the scheduler itself.</summary>
internal static class SessionDoubles
{
    public static Task NeverDelay(TimeSpan _, CancellationToken token) => Task.Delay(Timeout.Infinite, token);

    public static ConversionSession NewSession(IKeyValueStore? store = null, IFrameRenderer? renderer = null) =>
        new(renderer ?? new NullRenderer(), new PaletteStore(store ?? new MemoryStore()), NeverDelay);
}
```

In `Tests.Web/ConversionSessionTests.cs` delete the nested `MemoryStore`, `FlakyStore`, `NullRenderer` classes and the private `NeverDelay` and `NewSession` members, and add `using static PaintTranslator.Web.Tests.SessionDoubles;` at the top so the existing calls `NewSession()`, `NewSession(renderer)` and `NeverDelay` keep compiling. Existing calls of the form `NewSession(renderer)` must become `NewSession(renderer: renderer)` because the shared helper's first parameter is the store.

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: 50 green (no behaviour change).

- [ ] **Step 2: Write the failing session test for the banner dismiss**

Append to `ConversionSessionTests`:

```csharp
    [Fact]
    public void DismissPaletteSaveWarningClearsTheFlagAndRaisesChangedOnce()
    {
        var store = new FlakyStore { FailNextSet = true };
        var session = NewSession(store);
        session.ApplyPalette(PigmentLibrary.Selectable.Take(2).Select(p => p.Name));
        Assert.True(session.PaletteSaveFailed);

        int changed = 0;
        session.Changed += () => changed++;
        session.DismissPaletteSaveWarning();
        Assert.False(session.PaletteSaveFailed);
        Assert.Equal(1, changed);

        session.DismissPaletteSaveWarning(); // already clear: no second Changed
        Assert.Equal(1, changed);
    }
```

- [ ] **Step 3: Write the failing Sidebar tests**

`Tests.Web/SidebarTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PaintTranslator.Pigments;
using PaintTranslator.Web.Components;
using PaintTranslator.Web.Session;
using static PaintTranslator.Web.Tests.SessionDoubles;

namespace PaintTranslator.Web.Tests;

public class SidebarTests : BunitContext
{
    private ConversionSession Inject(ConversionSession session)
    {
        Services.AddSingleton(session);
        return session;
    }

    [Fact]
    public void AriaDisabledCarriesAnExplicitTrueOrFalse()
    {
        var session = Inject(NewSession());
        var cut = Render<Sidebar>();
        Assert.Equal("false", cut.Find("aside.sidebar").GetAttribute("aria-disabled"));

        session.BeginImageOperation();
        cut.Render();
        Assert.Equal("true", cut.Find("aside.sidebar").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void PaletteSaveBannerShowsAfterAFailedSaveAndDismisses()
    {
        var store = new FlakyStore { FailNextSet = true };
        var session = Inject(NewSession(store));
        session.ApplyPalette(PigmentLibrary.Selectable.Take(2).Select(p => p.Name));

        var cut = Render<Sidebar>();
        Assert.NotEmpty(cut.FindAll(".banner"));

        cut.Find(".banner .dismiss").Click();
        Assert.Empty(cut.FindAll(".banner"));
        Assert.False(session.PaletteSaveFailed);
    }
}
```

- [ ] **Step 4: Write the failing Toolbar tests**

`Tests.Web/ToolbarTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PaintTranslator.Web.Components;
using static PaintTranslator.Web.Tests.SessionDoubles;

namespace PaintTranslator.Web.Tests;

public class ToolbarTests : BunitContext
{
    public ToolbarTests()
    {
        // InputFile calls into JavaScript after its first render; there is no browser here
        // and nothing in these tests opens a file, so unplanned JS calls return defaults.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(NewSession());
    }

    [Fact]
    public void ClickingOutsideTheOpenWheelMenuClosesIt()
    {
        var cut = Render<Toolbar>();
        cut.Find(".menu > button").Click();
        Assert.NotEmpty(cut.FindAll(".menu-items"));

        cut.Find(".menu-backdrop").Click();
        Assert.Empty(cut.FindAll(".menu-items"));
    }

    [Fact]
    public void EscapeClosesTheOpenWheelMenu()
    {
        var cut = Render<Toolbar>();
        cut.Find(".menu > button").Click();

        cut.Find(".menu").KeyDown(Key.Escape);
        Assert.Empty(cut.FindAll(".menu-items"));
    }
}
```

- [ ] **Step 5: Run to verify they fail**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj --filter "FullyQualifiedName~SidebarTests|FullyQualifiedName~ToolbarTests|FullyQualifiedName~DismissPaletteSaveWarning"`
Expected: build error on `DismissPaletteSaveWarning`; after a stub, the Sidebar and Toolbar tests fail on missing elements.

- [ ] **Step 6: Session method**

In `Web/Session/ConversionSession.cs`, after `ApplyPalette`:

```csharp
    /// <summary>The banner's close button. Without this the warning sat in the sidebar until
    /// the next successful save, which for a user who never reopens the palette editor is
    /// forever; WinForms' MessageBox for the same failure is dismissed by definition.</summary>
    public void DismissPaletteSaveWarning()
    {
        if (!PaletteSaveFailed) return;
        PaletteSaveFailed = false;
        Changed?.Invoke();
    }
```

- [ ] **Step 7: Sidebar markup**

Replace the `<aside ...>` opening tag and the banner in `Web/Components/Sidebar.razor`:

```razor
<aside class="sidebar" aria-disabled="@(Session.ImageOperationInProgress ? "true" : "false")">
```

Blazor renders a `bool` attribute value bare when true and omits it when false, which is the boolean-attribute convention; `aria-disabled` is a token attribute whose valid values are the strings `true` and `false`, so a bare attribute was invalid ARIA even though the CSS selector happened to match it.

```razor
    @if (Session.PaletteSaveFailed)
    {
        <div class="error banner" role="alert">
            <span>Could not save your palette, so it won't be remembered next time.</span>
            <button class="dismiss" aria-label="Dismiss" @onclick="Session.DismissPaletteSaveWarning">×</button>
        </div>
    }
```

- [ ] **Step 8: Toolbar markup**

In `Web/Components/Toolbar.razor`, replace the `<div class="menu">` block:

```razor
    <div class="menu" @onkeydown="OnMenuKey">
        <button @onclick="() => wheelMenu = !wheelMenu" disabled="@Session.ImageOperationInProgress">Color Wheel ▾</button>
        @if (wheelMenu)
        {
            @* A full-viewport transparent layer under the items: any click that is not on an
               item lands here and closes the menu, without a document-level listener in JS. *@
            <div class="menu-backdrop" @onclick="() => wheelMenu = false"></div>
            <div class="menu-items">
                <button @onclick="() => Pick(WheelDisplay.Traditional)">Traditional</button>
                <button @onclick="() => Pick(WheelDisplay.SelectedPaints)">Selected Golden Paints</button>
                <button @onclick="() => { Session.ShowPhoto(); wheelMenu = false; }" disabled="@(Session.Wheel == WheelDisplay.None)">Back to photo</button>
            </div>
        }
    </div>
```

and in `@code`:

```csharp
    private void OnMenuKey(KeyboardEventArgs e) { if (e.Key == "Escape") wheelMenu = false; }
```

- [ ] **Step 9: CSS**

In `Web/wwwroot/css/app.css` change `.sidebar[aria-disabled]` to `.sidebar[aria-disabled="true"]`, and append:

```css
.menu-backdrop {
    position: fixed;
    inset: 0;
    z-index: 5;
}

.banner {
    display: flex;
    align-items: flex-start;
    gap: 6px;
}

    .banner .dismiss {
        padding: 0 6px;
        line-height: 1;
    }
```

`.menu-items` already sits at `z-index: 6`, above the backdrop.

- [ ] **Step 10: Guard the session-change push**

In `Web/Components/ImageCanvas.razor.cs`, wrap the body of `OnSessionChanged`'s lambda:

```csharp
    private void OnSessionChanged() => InvokeAsync(() =>
    {
        // Same exposure as ApplyFrame: this runs on a discarded InvokeAsync task, so a
        // JS failure in ClearFrame or SetGrid would otherwise vanish without a console line.
        try
        {
            if (Session.Displayed == null)
            {
                // ShowPhoto() can leave nothing displayed (Color Wheel -> Traditional -> Back
                // to photo, reached without ever loading a photo); without this the canvas
                // keeps the last painted wheel showing underneath the "Drop a photo" card.
                viewport.ImageSize = Size.Empty;
                CanvasInterop.ClearFrame(CanvasId);
            }
            PushGrid();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Session change push failed: {ex}");
        }
        StateHasChanged();
    });
```

- [ ] **Step 11: Run everything**

Run: `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: 55 green (50 + 1 session + 2 sidebar + 2 toolbar). If `KeyDown(Key.Escape)` does not compile against bUnit 2.9.0, use `cut.Find(".menu").KeyDown(new KeyboardEventArgs { Key = "Escape" })`.

Run: `dotnet build PaintTranslator.sln` — 0 errors, 1 warning.

- [ ] **Step 12: Stage**

```bash
git add Tests.Web/SessionDoubles.cs Tests.Web/ConversionSessionTests.cs Tests.Web/SidebarTests.cs Tests.Web/ToolbarTests.cs \
  Web/Components/Toolbar.razor Web/Components/Sidebar.razor Web/Session/ConversionSession.cs Web/Components/ImageCanvas.razor.cs Web/wwwroot/css/app.css
```

Update `.claude/handoff/PROJECT.md`.

---

### Task 5: JavaScript, CSS and asset polish

**Files:**
- Create: `Web/wwwroot/favicon.svg`
- Modify: `Web/wwwroot/js/interop.js`, `Web/wwwroot/index.html`, `Web/wwwroot/css/app.css`

No unit tests can reach these; verification is the headless run in Step 6 plus review.

- [ ] **Step 1: Wheel `deltaMode`**

In `interop.js` `bind`, replace the wheel listener:

```js
  // deltaMode 0 is pixels (every browser for trackpads and Chrome/Safari for wheels);
  // Firefox reports a mouse wheel in lines (1) and a page-scroll in pages (2). C# is
  // told deltas are pixels, so the conversion happens here: it is unit marshalling,
  // not a gesture decision. 16 px per line is the line height Firefox itself assumes.
  const LINE_PIXELS = 16;
  canvas.addEventListener("wheel", (e) => {
    e.preventDefault();
    const [x, y] = local(e);
    const unit = e.deltaMode === 1 ? LINE_PIXELS : e.deltaMode === 2 ? canvas.clientHeight : 1;
    dotnet.invokeMethodAsync("OnWheel", e.deltaX * unit, e.deltaY * unit, e.ctrlKey || e.metaKey, e.shiftKey, x, y);
  }, { passive: false });
```

- [ ] **Step 2: Paste filter and dragover comment**

In `bindFileInputs`:

```js
  document.addEventListener("paste", (e) => {
    // Only image items: a pasted text file or a URL would otherwise reach the decoder
    // and fail with a misleading "unsupported format" toast.
    const item = Array.from(e.clipboardData?.items ?? []).find((i) => i.kind === "file" && i.type.startsWith("image/"));
    if (item) { e.preventDefault(); send(item.getAsFile()); }
  });
  // Cancelled unconditionally: the browser only dispatches `drop` to a target whose
  // dragover was cancelled, and the drag's file list is not readable here anyway
  // (dataTransfer.files is empty until drop). The drop handler ignores non-files.
  document.addEventListener("dragover", (e) => { e.preventDefault(); e.dataTransfer.dropEffect = "copy"; });
```

- [ ] **Step 3: Grid under-stroke alpha**

In `redraw`:

```js
    // Two strokes, translucent black under white, so the grid reads on any image.
    // 0.59 is GridOverlayRenderer's under-pen (alpha 150/255), for parity with WinForms.
    for (const [width, style] of [[3, "rgba(0,0,0,0.59)"], [1, "rgba(255,255,255,0.9)"]]) {
```

- [ ] **Step 4: Favicon**

`Web/wwwroot/favicon.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32">
  <rect width="32" height="32" rx="7" fill="#0f1217"/>
  <circle cx="16" cy="16" r="10" fill="#d6a64e"/>
  <circle cx="12.5" cy="12.5" r="3" fill="#0f1217"/>
</svg>
```

In `index.html` `<head>`, after the viewport meta:

```html
    <link rel="icon" type="image/svg+xml" href="favicon.svg" />
```

and delete the template comment block `<!-- If you add any scoped CSS files, uncomment the following to load them ... -->`.

- [ ] **Step 5: CSS residue and the duplicate `.toolbar` rule**

In `app.css`:
- Delete the first `.toolbar { grid-column: 1 / 3; }` block (lines 32–34) and add `grid-column: 1 / 3;` as the first declaration of the later `.toolbar` block (the one under the "Toolbar (Task 12)" comment).
- Delete the template rules that nothing in `Web/` uses: `.valid.modified:not([type=checkbox])`, `.invalid`, `.validation-message`, `.blazor-error-boundary` and its `::after`, `code`, and both `.form-floating` rules. (`grep -rn 'ErrorBoundary\|form-floating\|validation-message' Web/` finds nothing; keep `#blazor-error-ui` and `.loading-progress*`, which `index.html` uses.)

- [ ] **Step 6: Headless check**

```bash
dotnet publish Web/PaintTranslator.Web.csproj -c Release -o Web/bin/publish --nologo -v quiet
python3 Web/serve.py Web/bin/publish/wwwroot --port 5181 &
PROFILE=$(mktemp -d)
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --disable-gpu \
  --no-first-run --user-data-dir="$PROFILE" --enable-logging=stderr --v=0 \
  --remote-debugging-port=0 "http://127.0.0.1:5181/?autofile=samples/sample.jpg" 2> "$PROFILE/log" &
sleep 40; pkill -f headless=new; pkill -f Web/serve.py
grep -o 'CONSOLE.*' "$PROFILE/log" | grep -E 'HOST TITLE|HOST ERROR|rror|Uncaught'
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:5181/favicon.svg   # run before killing serve.py: 200
```

Expected: titles as in Task 2, no errors, favicon 200. (`samples/` survives in `Web/bin/publish/wwwroot` because publish never deletes.)

- [ ] **Step 7: Tests and build still green**

Run: `dotnet build PaintTranslator.sln && dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj`
Expected: 0 errors, 1 warning; 55 green.

- [ ] **Step 8: Stage**

```bash
git add Web/wwwroot/favicon.svg Web/wwwroot/js/interop.js Web/wwwroot/index.html Web/wwwroot/css/app.css
```

Update `.claude/handoff/PROJECT.md`.

---

### Task 6: Heap measurement on a 24-megapixel photo

**Files:**
- Modify: `docs/superpowers/specs/2026-09-02-deployment-design.md` ("Measurement" section); `Web/PaintTranslator.Web.csproj` only on failure.

- [ ] **Step 1: Make the large sample**

```bash
S=Web/bin/publish/wwwroot/samples
sips -z 4000 6000 "$S/sample.jpg" --out "$S/large.jpg"     # -z height width
sips -g pixelWidth -g pixelHeight "$S/large.jpg"            # 6000 × 4000
```

(If `Web/bin/publish` is missing, publish first as in Task 5 Step 6.)

- [ ] **Step 2: Run it headless with a long wait**

```bash
python3 Web/serve.py Web/bin/publish/wwwroot --port 5181 &
PROFILE=$(mktemp -d)
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --disable-gpu \
  --no-first-run --user-data-dir="$PROFILE" --enable-logging=stderr --v=0 \
  --remote-debugging-port=0 "http://127.0.0.1:5181/?autofile=samples/large.jpg" 2> "$PROFILE/log" &
sleep 150; pkill -f headless=new; pkill -f Web/serve.py
grep -E 'HOST TITLE|HOST ERROR|Out of memory|OutOfMemory|RuntimeError|Uncaught|MONO_WASM' "$PROFILE/log" | sed 's/^.*CONSOLE/CONSOLE/' | head -20
```

Chrome's log lines start with a timestamp; the gap between the `HOST TITLE ... large.jpg` line (photo shown) and the final title (conversion shown) is the time to record.

- [ ] **Step 3a: It converted**

Add to the spec's "Measurement" section: the sample size, the elapsed time between the two title lines, the Chrome version (`"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --version`), and "heap limit not reached at the default 2 GB (`EmccMaximumHeapSize` unset)". Change nothing else.

- [ ] **Step 3b: It failed with an out-of-memory or `RuntimeError`**

Add to `Web/PaintTranslator.Web.csproj` inside the `PropertyGroup`, after `WasmEnableThreads`:

```xml
    <!-- A 6000×4000 photo (24 MP, the size of a current phone JPEG) exhausted the default
         2 GB heap in the measurement recorded in docs/superpowers/specs/2026-09-02-deployment-design.md;
         4 GB is the wasm32 ceiling. Changing this relinks the runtime: clean Web/obj/Release
         before the next publish or the old limit is kept silently. -->
    <EmccMaximumHeapSize>4294967296</EmccMaximumHeapSize>
```

Then `rm -rf Web/obj/Release`, republish, and repeat Step 2. Record both runs in the spec's "Measurement" section. If it still fails at 4 GB, record that as an open problem in the spec and in `.claude/handoff/PROJECT.md`; do not add a cap.

- [ ] **Step 4: Tests still green**

If the csproj changed: `dotnet build PaintTranslator.sln && dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj` — 0 errors, 1 warning; 55 green.

- [ ] **Step 5: Stage**

```bash
git add docs/superpowers/specs/2026-09-02-deployment-design.md
git add Web/PaintTranslator.Web.csproj   # only if it changed
```

Update `.claude/handoff/PROJECT.md`.

---

### Task 7: Documentation

**Files:**
- Modify: `CLAUDE.md`, `.claude/handoff/PROJECT.md`

- [ ] **Step 1: Commands**

In `CLAUDE.md`'s Commands block, after the `Tools/BuildDecoders/build.sh` line, add:

```
Tools/Deploy/deploy.sh                                   # Docker: clean static site in deploy/site + painttranslator:latest (Caddy) image
```

- [ ] **Step 2: Deployment paragraph**

At the end of the "Web app" subsection under Architecture, add:

```markdown
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
```

- [ ] **Step 3: Correct the boot-manifest note**

In the same subsection, replace the clause "glob for it or read the file name back out of `_framework/blazor.boot.json` instead" with "glob for it (a .NET 10 publish emits no `blazor.boot.json`; the boot manifest is embedded in the fingerprinted `dotnet.<hash>.js`)".

- [ ] **Step 4: Test counts**

Replace every `Tests.Web/` count in `CLAUDE.md` (the Tests section says 45) with the number Task 5 Step 7 observed. Keep "`Tests/` stays at 403".

- [ ] **Step 5: Verify the doc's commands**

Run: `dotnet build PaintTranslator.sln` (0 errors, 1 warning), `dotnet test Tests/PaintTranslator.Tests.csproj` (403), `dotnet test Tests.Web/PaintTranslator.Web.Tests.csproj` (the recorded count).

- [ ] **Step 6: Stage and close out**

```bash
git add CLAUDE.md
```

In `.claude/handoff/PROJECT.md`: task 5 → done; next step → "owner reviews the staged tree and runs the owner-only browser checks (menu dismiss, Firefox wheel, paste, favicon, Credits dialog); WinForms retirement is a separate decision"; remove the parked-polish bullet from Open problems.

---

## Self-review

- **Spec coverage.** Layout → T1; one command → T1+T2; Dockerfile stages → T1+T2; Caddyfile rules (MIME, brotli, SPA, isolation comment, caching, port) → T2; README → T2; Credits (button, dialog, record list, parity test, LGPL sentence) → T3; the ten polish items → T4 (1, 8, 9, 10) and T5 (2, 3, 4, 5, 6, 7); heap measurement → T6; Tests section → T3/T4; Verification (folder contents, curl headers, `/bench`, `/nope.js`, headless through the container, build + test counts) → T1 Step 6, T2 Steps 6–7, T7 Step 5; Docs (`CLAUDE.md` command, paragraph, `blazor.boot.json`, count; spec Measurement) → T6 + T7. Non-goals respected: no launcher or `serve.py` edits, no host files, no cap.
- **Placeholders.** None; every code step carries its content. The one conditional (T6 Step 3a/3b) is a real branch on a measurement, with both arms written out.
- **Type consistency.** `VendoredLibrary(Name, Version, Licence, Url)` and `Credits.Decoders/Runtime/All` are used identically in T3's code, test and T7's doc. `SessionDoubles.NewSession(store, renderer)` order is used consistently in T4 (`NewSession(store)`, `NewSession(renderer: renderer)`). `DismissPaletteSaveWarning` appears in the session, the Sidebar markup and both tests with the same name. CSS classes `menu-backdrop`, `banner`, `dismiss`, `credit`, `close` match between markup and tests.
