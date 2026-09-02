# syntax=docker/dockerfile:1
# Three stages: build (SDK + wasm-tools, the same Release publish the launcher runs),
# site (FROM scratch, so `docker build --target site --output` exports the published
# wwwroot as a plain folder) and serve (Task 2). One AOT compile feeds both outputs,
# which is why the folder and the image never drift.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# The Emscripten toolchain the workload pulls in shells out to `python3` during the
# AOT compile step (WasmApp.Common.targets); the SDK base image does not ship one, so
# the publish fails ("unable to find python in $PATH") without this.
RUN apt-get update && apt-get install -y --no-install-recommends python3 && rm -rf /var/lib/apt/lists/*
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

FROM caddy:2-alpine AS serve
COPY Tools/Deploy/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /out/wwwroot /srv
EXPOSE 80
