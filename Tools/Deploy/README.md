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
`deploy.sh` (an Apple-silicon Mac builds arm64 by default). Run this from the repo
root — the trailing `.` is the build context, so the paths the Dockerfile `COPY`s
(`Core/`, `Web/`) resolve from wherever the command is run:

    docker build -f Tools/Deploy/Dockerfile --target serve --platform linux/amd64 -t painttranslator:latest .

An emulated amd64 AOT compile on Apple silicon is much slower than the native build
the timing note above describes; expect the cold compile to take substantially longer
than the "several minutes" quoted for a native run.

Then on the server, with a domain pointed at it, change the first line of the
Caddyfile from `:80` to the domain (for example `paint.example.com`) and run with
ports 80 and 443 published. Caddy obtains and renews the certificate itself:

    docker run -d --restart unless-stopped -p 80:80 -p 443:443 painttranslator:latest

## Threads

`WasmEnableThreads` is `false` (see `Web/PaintTranslator.Web.csproj`). If it is ever set
to `true`, uncomment the two cross-origin isolation headers in the Caddyfile; without
them `SharedArrayBuffer` is unavailable and the runtime will not start.
