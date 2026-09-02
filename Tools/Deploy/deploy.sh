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

# A silent partial export (e.g. an interrupted --output) would otherwise look like
# success until someone uploads the folder and finds it half-empty.
[ -f deploy/site/index.html ] || { echo "deploy/site/index.html is missing after the export; the site build did not complete." >&2; exit 1; }

docker build -f Tools/Deploy/Dockerfile --target serve -t painttranslator:latest .

echo
echo "Static site:  deploy/site            (upload to any static host; see Tools/Deploy/README.md)"
echo "Image:        painttranslator:latest (docker run --rm -p 8080:80 painttranslator:latest)"
