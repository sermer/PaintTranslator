# Deployment: design

**Date:** 2026-09-02
**Status:** approved by the owner in chat; not yet implemented
**Sub-project:** 3 of 3 in the move from WinForms to a web app
**Depends on:** `2026-09-01-blazor-app-design.md` (implemented, staged, uncommitted)

## Why

Sub-project 2 delivered the web app and a Mac launcher that publishes Release and serves
it from `Web/serve.py`. That output is only ever on the owner's machine. This
sub-project produces the two things any host can take — a clean static folder and a
container image — from one command, so the site can be uploaded to a static host or run
on a rented server without a second build path. It also closes the items the final
review of sub-project 2 parked as "best judged before going public": a visible licence
notice for the vendored decoders, the ten small polish findings, and a measurement of
what a full-resolution photo does to the WebAssembly heap.

## Decisions made with the owner (2026-09-02)

| Decision | Choice | Rejected |
|---|---|---|
| Host | **Not chosen; stay host-neutral.** No host-specific files (`_headers`, workflows, `CNAME`). | Cloudflare Pages, GitHub Pages, a VPS-only path. Any of them can be added later on top of the folder or the image. |
| Deliverable | **Clean publish folder + Docker image** from one script. | Folder only; image only. |
| Where the publish runs | **Inside Docker, multi-stage.** One AOT compile produces both outputs, so the folder and the image hold identical bytes and the Mac needs Docker but not `wasm-tools` to deploy. | Publishing on the host and copying the folder into the image (two compiles that can drift). |
| Server in the image | **Caddy.** Stock Caddy serves pre-compressed brotli, does the SPA fallback and headers in a few lines, and provisions HTTPS by itself once a domain is pointed at a server. | nginx: the stock image has no brotli module, so it would serve the gzip copies or need a third-party build. |
| Licence notice | **A Credits dialog in the app** listing the vendored decoders and the .NET runtime. libheif-js is LGPL-3.0, which the owner should not learn about from a visitor. | Leaving `LICENSES.md` in the repo as the only notice. |
| Parked polish | **Folded in** as one task, so the first public build ships with it. | A later pass. |
| Heap limit | **Measured in this sub-project**; `EmccMaximumHeapSize` raised only if the measurement fails. | Deferring to a later sub-project; adding a size cap (a parity change). |

## Non-goals

- No hosting account, DNS, TLS certificate or CI workflow. The README says how to get
  HTTPS from Caddy when a domain exists; nothing here creates one.
- No change to the launcher or to `Web/serve.py`. `PaintTranslator.command` stays the
  way the owner runs the app on the Mac, and its output directory (`Web/bin/publish`)
  is not the deploy folder.
- No retirement of the WinForms app.
- No change to colour, mixing or mapping behaviour. The heap task may change a build
  property; it may not cap or resample the input.

## Layout

```
Tools/Deploy/
  Dockerfile        # build → site → serve stages
  Caddyfile         # what serve.py does, for Caddy
  deploy.sh         # the one command
  README.md         # how to use the folder and the image
.dockerignore       # at the repo root: the build context is the repo
deploy/site/        # output; git-ignored; recreated from empty on every run
```

`Tools/Deploy` matches how `Tools/BuildDecoders` and `Tools/IngestSpectra` are laid
out. The Dockerfile is not at the repo root because `docker build -f` makes the
location free, and the root is already the place people look for the launcher.
`.dockerignore` must be at the root because that is the build context; it excludes
`bin`, `obj`, `Tests*`, `docs`, `Tools/BuildDecoders/node_modules`, `.superpowers`,
`.claude` and `deploy`. The context that reaches the daemon is then `Core/`, `Web/`,
the solution and the small root files, which is all the publish needs — Core has no
package references and there is no `Directory.Build.props`.

`deploy/` is lower-case and at the root; `Tools/Deploy` is capitalised and nested, so the
two cannot collide on the Mac's case-insensitive file system.

## The one command

`Tools/Deploy/deploy.sh` runs from any directory, `cd`s to the repo root, checks that
`docker` is on the path and the daemon answers, then:

1. `rm -rf deploy/site` and `docker build --target site --output type=local,dest=deploy/site`.
   Deleting first is what makes the folder clean: `dotnet publish -o` into an existing
   directory leaves stale files behind, which is how the launcher's `wwwroot` picked up
   a `samples/` directory during sub-project 2's headless checks.
2. `docker build --target serve -t painttranslator:latest`.
3. Prints the folder path, the image tag, and the `docker run` line.

Both builds share one cache, so the AOT publish runs once. A source change under
`Core/` or `Web/` invalidates the publish layer and costs a cold AOT compile (several
minutes); nothing else does. The script has no flags. The tag is fixed at
`painttranslator:latest`; a host that needs a registry name retags it.

## Dockerfile

Three stages:

- **`build`** — `mcr.microsoft.com/dotnet/sdk:10.0`. Installs the `wasm-tools` workload,
  copies `Core/` and `Web/`, and runs `dotnet publish Web/PaintTranslator.Web.csproj
  -c Release -o /out`, which is the launcher's command. The csproj already carries every
  publish decision (`RunAOTCompilation`, `WasmStripILAfterAOT`,
  `InvariantGlobalization`, `OverrideHtmlAssetPlaceholders`, `WasmEnableThreads`), so
  the Dockerfile adds none. Restore is a separate layer keyed on the two csproj files
  so an edit to a `.cs` file does not re-download packages.
- **`site`** — `FROM scratch`, `COPY --from=build /out/wwwroot /`. Exists only so
  `--output` can export the published tree by itself. It is never run.
- **`serve`** — `caddy:2-alpine`, the same `wwwroot` under `/srv`, the Caddyfile at
  `/etc/caddy/Caddyfile`, `EXPOSE 80`.

The SDK image tag is pinned to the major (`10.0`), matching the `net10.0` target; the
Caddy tag to the major (`2`).

## Caddyfile

It reproduces the four things `Web/serve.py` does that a bare file server does not,
and adds the caching rule a public site needs:

| Concern | Rule |
|---|---|
| MIME types | None needed; Caddy knows `.wasm`, `.js`, `.json`, `.woff2`. The `.dat`/`.blat`/`.dll`/`.pdb` entries in `serve.py` are for older runtimes and a .NET 10 publish emits none of them. |
| Brotli | `file_server { precompressed br gzip }` serves `foo.wasm.br` as `foo.wasm` with `Content-Encoding: br` when the browser accepts it, gzip otherwise. |
| SPA fallback | `try_files {path} /index.html` applied only to requests whose path has no extension and matches no file. A missing `.js` or `.wasm` still 404s so a broken asset link stays visible, the same rule `serve.py` enforces. |
| Cross-origin isolation | A commented-out `header` block with `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: require-corp`, carrying the same note as the launcher: enable it only when `WasmEnableThreads` is `true`. The launcher reads the csproj at run time; a Caddyfile cannot, so this stays a manual switch. |
| Caching | `/_framework/*` gets `Cache-Control: public, max-age=31536000, immutable`. Every file there is fingerprinted by `OverrideHtmlAssetPlaceholders` (`dotnet.<hash>.js`, `PaintTranslator.Core.<hash>.wasm`, and so on), so a republish changes the names and the old entries are simply never requested again. Everything else — `index.html`, `css/app.css`, `js/interop.js`, `js/decoders/*` — is not fingerprinted and gets `Cache-Control: no-cache`, which lets the browser keep a copy but revalidate it on every load. |
| Port | `:80`. HTTPS is one edit: replace `:80` with the domain and Caddy provisions the certificate. The README shows it. |

A .NET 10 publish emits no `blazor.boot.json`; the boot manifest is embedded in the
fingerprinted `dotnet.<hash>.js`. `CLAUDE.md` currently says to read file names back out
of `_framework/blazor.boot.json`; that line is corrected in this sub-project to "glob
for them", which is what actually works.

## README

`Tools/Deploy/README.md` is short and covers, in this order: the one command and what it
produces; running the image locally (`docker run --rm -p 8080:80 painttranslator`);
uploading `deploy/site/` to any static host, with the two caching rules and the brotli
note so a host that does not negotiate pre-compressed files still works, only slower;
running the image on a rented server with a domain, including the Caddyfile edit for
HTTPS; and the reminder that the isolation headers are needed only with threads.

## Credits

A **Credits** button in the sidebar, beside Edit Palette, opens a dialog built the way
`PaletteEditorDialog` is. It lists, one row each: libheif-js (LGPL-3.0), UTIF.js (MIT),
@webtoon/psd (MIT) and the .NET runtime (MIT), with version, licence and a link to the
upstream repository. A sentence under the list says the libheif bundle is loaded
unmodified as a separate script, which is the LGPL obligation the app relies on and the
reason it is bundled that way.

The rows come from a static list in `Web/Session` (`VendoredLibrary` records: name,
version, licence, URL). `Tests.Web` gets a test that parses
`Web/wwwroot/js/decoders/LICENSES.md` and asserts the decoder rows match it name for
name, version for version, licence for licence. The markdown file stays the source of
truth for the decoders, because `Tools/BuildDecoders/build.sh` is what changes versions,
and the test is what stops the dialog drifting. The .NET row is not in that file and is
pinned by a plain assertion on the list.

## Polish

The ten findings the final review of sub-project 2 parked, each fixed in this
sub-project (details and locations in `.superpowers/sdd/2026-09-01-blazor-app/progress.md`,
Ruling 23 and the two re-review minors):

1. Colour Wheel menu does not dismiss on an outside click or Escape.
2. Wheel zoom ignores `deltaMode`, so Firefox line- and page-mode deltas zoom wrongly.
3. Paste accepts any clipboard item instead of filtering `image/*`.
4. The unconditional `dragover` `preventDefault` has no comment saying why.
5. No favicon.
6. Grid under-stroke alpha is 0.45 where WinForms uses 150/255 (≈0.588).
7. Template CSS residue and a duplicated `.toolbar` rule in `app.css`.
8. `aria-disabled` is rendered bare by Blazor; the selector or the attribute must agree.
9. `ImageCanvas.OnSessionChanged` calls `ClearFrame` outside the try/catch that guards
   `PushGrid`.
10. The palette-save failure banner has no dismiss.

Each C#-side change gets a test in `Tests.Web` where a bUnit or session test can pin
it (1, 3, 6, 8, 10); the JavaScript-only ones (2, 4) and the asset/CSS ones (5, 7)
are verified by review and by the headless check still passing.

## Heap measurement

The spec for sub-project 2 left `EmccMaximumHeapSize` at its default and named it the
first lever if large photos fail. This sub-project takes the measurement:

1. Generate a large sample from the existing `sample.jpg` with `sips` (macOS built-in),
   about 6000×4000 — 24 megapixels, which is the size of a current phone or camera JPEG.
2. Load it through `?autofile=` against the Release build in headless Chrome, the way
   sub-project 2 verified every build, and read `HOST TITLE` / `HOST ERROR` plus any
   out-of-memory line from the console.
3. If it converts, record the size and the time in this spec's "Measurement" section
   and change nothing. (Peak heap is not observable from the console in headless
   Chrome; a failure shows up as an out-of-memory or `RuntimeError` line instead.)
4. If it fails, raise `EmccMaximumHeapSize` in the csproj (wasm32 allows up to 4 GB;
   the default is 2 GB), re-measure, and record the number and the reason in the
   csproj comment and here.

No cap on input size is added. If the largest usable photo turns out smaller than the
owner needs even at 4 GB, that is reported as an open problem, not fixed here, because
a cap or a resample is a parity change the owner has not agreed to.

## Tests

- `Tests.Web`: the Credits dialog renders the rows (bUnit); the rows match
  `LICENSES.md` (plain xUnit reading the file from the repo); one test per polish item
  that C# can pin (see "Polish").
- `Tests/` stays at 403; nothing in Core changes.
- The Dockerfile, Caddyfile and script are verified end to end rather than unit-tested
  (see "Verification").

## Verification

1. `Tools/Deploy/deploy.sh` completes on the Mac; `deploy/site/` contains `index.html`,
   `_framework/` with fingerprinted files and their `.br` copies, and nothing else that
   the launcher's `wwwroot` would not also contain minus `samples/`.
2. `docker run --rm -p 8080:80 painttranslator`, then:
   - `curl -sI -H 'Accept-Encoding: br' http://127.0.0.1:8080/_framework/dotnet.native.*.wasm`
     (name globbed from `deploy/site`) shows `content-encoding: br` and the immutable
     `cache-control`.
   - `curl -sI http://127.0.0.1:8080/index.html` shows `cache-control: no-cache`.
   - `curl -sI http://127.0.0.1:8080/bench` returns 200 with HTML; `/nope.js` returns 404.
   - The headless Chrome check from sub-project 2 (`?autofile=samples/sample.jpg` with
     the sample copied into a bind-mounted or second-run folder, `HOST TITLE` in the
     console) converts a photo through the container.
3. `dotnet build PaintTranslator.sln` is 0 errors and 1 warning (ImageSharp);
   `Tests/` 403 green; `Tests.Web` green at its new count.
4. The heap measurement is recorded below.

## Docs

`CLAUDE.md` gains the deploy command in Commands, a paragraph under the web-app
section describing `Tools/Deploy` and the folder/image split, the corrected
`blazor.boot.json` note, and the new `Tests.Web` count. This spec gets a "Measurement"
section once the heap task has run.

## Risks

- **Cold AOT inside Docker is slow** (several minutes, and the `wasm-tools` install is
  a few hundred MB on first build). Accepted: it is a deploy, not the launcher, and the
  layer cache makes a no-change rebuild seconds.
- **Emulation.** The SDK image runs natively on Apple silicon (arm64) and the publish
  output is platform-independent, so the folder is the same either way; the `serve`
  image is built for the Mac's architecture unless `--platform linux/amd64` is passed
  for an x86 server. The README says so; the script does not add the flag.
- **Caddy's `precompressed`** only serves the `.br` file when the request path has no
  encoding of its own and the file exists beside the original — exactly the shape
  `dotnet publish` emits, so no renaming is needed. If a host uploads the folder
  without negotiating, browsers get the uncompressed 25 MB; slower, still correct.
- **The LGPL notice** is a good-faith reading of the licence's requirements for a
  separately loaded, unmodified library with a link to its source. It is not legal
  advice; the owner should confirm before the site is public if it matters to them.
- **The heap measurement is Chrome-only**, like every other measurement so far; Safari
  has a lower per-tab memory ceiling and no automation hook here.

## Measurement

**[verified]** A 6000×4000 JPEG (24 MP, upscaled with `sips -z 4000 6000` from the
existing `Web/bin/publish/wwwroot/samples/sample.jpg`) converted successfully end to
end, headless, against the Release/AOT publish from Task 5. Chrome 152.0.7977.65.
**[verified]** Elapsed time between the `HOST TITLE ... samples/large.jpg` line (photo
shown) and the final `HOST TITLE ... (converted to paints)` line (conversion complete)
was about 5.06 s (`090406.062061` to `090411.123139` in the Chrome log timestamps).
**[inferred]** The heap limit was not reached at the default 2 GB (`EmccMaximumHeapSize`
unset) — inferred from the absence of any OOM or `RuntimeError` line in the console log,
since peak heap itself is not observable headless (see above).
