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
