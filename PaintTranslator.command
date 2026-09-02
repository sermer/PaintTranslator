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
