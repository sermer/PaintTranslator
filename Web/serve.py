#!/usr/bin/env python3
"""Static server for the published site.

The launcher uses this instead of a global tool so a fresh Mac needs only the
.NET SDK and the Python that ships with macOS. It does the four things a
deployed host does that http.server does not: correct MIME types for the
WebAssembly payload; brotli negotiation for the pre-compressed files the
publish step emits; when threads are enabled, the two cross-origin
isolation headers without which SharedArrayBuffer is unavailable; and a
single-page-app fallback to index.html for client-side routes.
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
        path = self.translate_path(self.path)
        # Blazor's router handles paths like /bench client-side; there is no such
        # file, so a reload or deep link 404s unless we hand back index.html and
        # let the SPA take over. An extensionless miss is a route; a missing file
        # with an extension (.js, .png, .wasm) is a broken asset link and should
        # keep 404ing so it stays visible instead of silently becoming HTML.
        if not os.path.exists(path) and os.path.splitext(path)[1] == "":
            self.path = "/index.html"
            path = self.translate_path(self.path)

        # Serve foo.js.br as foo.js with Content-Encoding when the browser accepts
        # brotli, which is what the ASP.NET host does for a deployed site.
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
