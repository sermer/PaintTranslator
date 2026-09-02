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
  // MemoryView.copyTo() asserts its destination is exactly a Uint8Array (the
  // type a marshaled Span<byte> owns on the JS side); ImageData then requires
  // a Uint8ClampedArray, so the clamped view is layered over the same buffer
  // rather than allocating and copying a second time.
  const bytes = new Uint8Array(width * height * 4);
  view.copyTo(bytes);
  const frame = new OffscreenCanvas(width, height);
  frame.getContext("2d").putImageData(new ImageData(new Uint8ClampedArray(bytes.buffer), width, height), 0, 0);
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
    // 0.59 is GridOverlayRenderer's under-pen (alpha 150/255), for parity with WinForms.
    for (const [width, style] of [[3, "rgba(0,0,0,0.59)"], [1, "rgba(255,255,255,0.9)"]]) {
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

// The 2D canvas this reads back through premultiplies alpha on the way in and
// un-premultiplies on the way out: at alpha 0 the colour bytes always come back
// 0 regardless of what the source file held, and a partially transparent pixel's
// colour is rounded by that round trip. Nothing downstream depends on the exact
// colour under partial or zero alpha — ConversionSession.RecipeAt only ever
// tests alpha == 0 — so this is accepted rather than fixed. A lossless path
// exists (WebCodecs' ImageDecoder gives unpremultiplied pixels directly), but is
// out of scope: it would need its own decode function and feature-detection
// fallback for the one thing GIF/BMP/JPEG/PNG opaque decoding does not need.
async function decodeNative(bytes) {
  const bitmap = await createImageBitmap(new Blob([bytes]), { premultiplyAlpha: "none", colorSpaceConversion: "none" });
  const c = new OffscreenCanvas(bitmap.width, bitmap.height);
  const ctx = c.getContext("2d", { colorSpace: "srgb", willReadFrequently: true });
  ctx.drawImage(bitmap, 0, 0);
  const data = ctx.getImageData(0, 0, bitmap.width, bitmap.height).data;
  bitmap.close();
  return { width: c.width, height: c.height, rgba: data };
}

let heifModule;
async function decodeHeic(bytes) {
  await loadScript("js/decoders/libheif-bundle.js");
  // The vendored bundle's global `libheif` is the Emscripten module *factory*,
  // not the module itself: the README's `new libheif.HeifDecoder()` only works
  // one line down from `const libheif = require(...)()`, i.e. after the factory
  // has been called. WebAssembly instantiation is async in a browser (it was
  // observably synchronous under Node, which is what made this easy to miss),
  // so the call is awaited and cached rather than repeated on every decode.
  heifModule ??= await libheif();
  const decoder = new heifModule.HeifDecoder();
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
  // @webtoon/psd throws bare, minifier-mangled error classes with no message
  // text (confirmed against its own unminified npm dist, not just this esbuild
  // pass), so there is no raw .message worth surfacing to the user for either
  // failure. Psd.parse() itself can throw on a PSD this decoder does not
  // support (observed on a multi-layer file from an encoder other than
  // Photoshop), not only composite() on a missing merged image, so both calls
  // share one message. It is deliberately neutral rather than naming "no
  // composite image" as the cause: that is only one of the two failures it
  // covers, and the other (a file @webtoon/psd simply cannot parse) is not
  // fixed by Maximize Compatibility, so the message hints at that setting
  // without asserting it is the reason.
  try {
    const psd = Psd.parse(bytes.buffer);
    const rgba = await psd.composite();
    return { width: psd.width, height: psd.height, rgba };
  } catch {
    throw new Error("Could not read this PSD. If it was saved without 'Maximize Compatibility', re-save with that option and try again.");
  }
}

export async function decode(view, format) {
  // The memory view is only valid during this call; copy before the first await.
  const bytes = new Uint8Array(view.slice());
  let out;
  switch (format) {
    case "Heif": out = await decodeHeic(bytes); break;
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
    // Only image items: a pasted text file or a URL would otherwise reach the decoder
    // and fail with a misleading "unsupported format" toast.
    const item = Array.from(e.clipboardData?.items ?? []).find((i) => i.kind === "file" && i.type.startsWith("image/"));
    if (item) { e.preventDefault(); send(item.getAsFile()); }
  });
  // Cancelled unconditionally: the browser only dispatches `drop` to a target whose
  // dragover was cancelled, and the drag's file list is not readable here anyway
  // (dataTransfer.files is empty until drop). The drop handler ignores non-files.
  document.addEventListener("dragover", (e) => { e.preventDefault(); e.dataTransfer.dropEffect = "copy"; });
  document.addEventListener("drop", (e) => {
    e.preventDefault();
    const file = e.dataTransfer?.files?.[0];
    if (file) send(file);
  });
}

// Test-only: lets the temporary host page's `?autofile=` verification path fetch a
// sample from the site root without interop.js having to know why. Not part of the
// production load paths (open/paste/drop all get bytes handed to them directly).
export async function fetchBytes(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Failed to fetch ${url}: ${response.status}`);
  return new Uint8Array(await response.arrayBuffer());
}
