# Research: Brushwork, Edges, and Mark-Making

**Track:** style-aware conversion — the *spatial* half.
**Date:** 2026-07-26
**Scope:** how painters handle edges, mark size, stroke direction, layering, broken colour
and grounds; which classical image-processing algorithms approximate each; and where each
one has to sit relative to `PalettePhotoConverter`'s palette mapping.

**Out of scope (covered elsewhere, do not duplicate):** pigment physics, Kubelka–Munk,
colour-difference metrics, gamut mapping, value compression, palette extraction and the
segmentation→quantization pipeline. See [`../README.md`](../README.md), and in particular
[`02-photo-to-paint-pipeline.md`](../source-reports/02-photo-to-paint-pipeline.md) §5 for
quantization/segmentation and [`04-prior-art-and-algorithms.md`](../source-reports/04-prior-art-and-algorithms.md)
§3.10 for Lindemeier's limited-palette painterly system. This report extends §5.3 of report
02 (dithering) with an argument that report did not make, and otherwise stays on the
spatial side.

**Verification convention** — matching the rest of `docs/research/`:
`[verified]` = read directly from the cited primary source or checked in this repo ·
`[relayed]` = reported by a secondary source I trust but did not confirm at the primary ·
`[inferred]` = my reasoning from the above, not stated anywhere.

---

## 0. Executive summary — the eight things that matter

1. **The single largest gap between the current output and a painting is not colour, it
   is that every pixel is treated as equally important.** A painting has an edge
   *hierarchy* — hardest at the focal point, softening outward, lost at the periphery —
   and a mark-size hierarchy that goes with it. The app currently applies one uniform
   Gaussian σ to the whole frame. Making that σ vary spatially is a small change with a
   disproportionate payoff. (§1.1, §5.1)
2. **Blur is the wrong smoothing operator for this job.** Gaussian blur destroys edges
   and detail together. Painterly abstraction wants the opposite: kill the detail,
   *keep or sharpen* the edges. Every serious NPR abstraction pipeline since 2006 uses an
   edge-preserving smoother instead. The best value-for-effort in this family is the
   **anisotropic Kuwahara filter**, which is the only one of the group that flattens
   *along* feature direction and therefore actually looks like paint rather than like
   denoising. (§3.2)
3. **The invariant is safer than the codebase comment implies, and the comment is
   subtly incomplete.** "Blur must happen before mapping" is right, but the real rule is
   sharper: *any operation that produces a new colour by arithmetic on mapped colours
   breaks the invariant; any operation that only selects among already-mapped colours
   preserves it.* That distinction unlocks several post-map spatial operations —
   morphological cleanup, dithering, hard-edged stroke rendering — that a blanket
   "everything before the map" rule would wrongly forbid. (§2)
4. **Dithering does not violate the invariant, and it genuinely expands the reachable
   gamut.** Every pixel in a dithered image is still exactly one achievable mixture. But
   the *perceived* colour of a dithered patch is the additive-average (partitive) mixture
   of its components, which is a straight line in linear light — a different and often
   much more chromatic path than the Kubelka–Munk mixing curve between the same two
   paints. Blue + yellow dithered reads grey; blue + yellow mixed reads green. This is
   the one technique on the list that can reach colours the current converter provably
   cannot. Its cost is that it stops being a *paintable* instruction unless the dither
   cell is a brush mark, not a pixel. (§3.7)
5. **Stroke-based rendering (Hertzmann 1998) is implementable here in a few hundred lines
   and its patent has expired**, but it is the highest-effort lever on this list and it
   sits awkwardly with the invariant: overlapping anti-aliased strokes composite, which
   is category-C arithmetic. The fix is cheap — render hard-edged at 2–4× and re-run the
   palette map on the downsampled result. (§3.5, §2.3)
6. **Stroke direction is nearly free once you have a structure tensor**, and a structure
   tensor is ~40 lines on top of the Sobel gradients you need anyway. It is the shared
   substrate for anisotropic Kuwahara, coherence-enhancing filtering, flow-based DoG, and
   stroke orientation. Build it once. (§3.4)
7. **Glazing has a real physical analogue in this codebase and nothing else on this list
   does.** The KM kernel already models opacity and layering. A "glaze pass" — composite a
   thin transparent film of one paint over the mapped image, through the kernel — produces
   colours that are physically achievable by *layering*, which is a legitimately larger set
   than achievable by *mixing*. That is a gamut expansion with a physical justification,
   unlike dithering's perceptual one. (§1.4, §2.4, §3.9)
8. **The task brief says the app "has a dithered mode already". It does not.** `grep -ri
   dither` over the repository matches only two files, both in `docs/research/`; no `.cs`
   file mentions dithering, error diffusion, Bayer, or blue noise. `MainForm.cs` exposes
   exactly one spatial control, `blurTrackBar`, feeding `PalettePhotoConverter.Convert`'s
   `blurRadius`. `[verified]` Any assessment below of "the existing dithered mode" should
   be read as an assessment of a mode that would have to be built.

---

## Part I — What painters actually do

### 1.1 Edge hierarchy: hard, firm, soft, lost

Painting instruction converges on a four-way classification, sometimes collapsed to
three: **hard** (a crisp value/colour break), **firm** (a definite but not razor edge),
**soft** (a graded transition of several stroke-widths), and **lost** (the form's value and
colour match its background closely enough that the boundary disappears entirely).
`[relayed]` — [Draw Paint Academy, "Edges in Art"](https://drawpaintacademy.com/edges/) ·
[Art Studio Life, "All about edges"](https://artstudiolife.com/edges-in-painting/) ·
[Learn to Paint Podcast, "Three Types of Edges"](https://www.learntopaintpodcast.com/blog/three-types-of-edges-and-where-to-use-them-in-your-painting).

Three rules recur across all of these sources and are what a style-aware converter should
encode `[relayed]`:

- **Hard edges advance, soft edges recede.** Edge quality is a depth cue independent of
  value and colour. [Draw Paint Academy](https://drawpaintacademy.com/edges/) ·
  [Painted by Natalia, "Beyond the Outline"](https://www.paintedbynatalia.com/post/beyond-the-outline-how-edges-in-landscape-art-shape-mood-focus).
- **The hardest edges belong at or near the focal point**, because the eye is drawn to
  sharp edges first — and because this mimics foveal vision, where only the fixated object
  is sharp. [Draw Paint Academy](https://drawpaintacademy.com/edges/) ·
  [MontCarta, "Edge Control"](https://montcarta.com/blogs/art-guide-inspiration/edge-control-visual-hierarchy-hand-painted-art).
- **Softness is graded, not binary.** Edges at the back of the picture plane are softer
  than edges at the front; the gradient of softness is itself the depth and attention cue.
  [Draw Paint Academy](https://drawpaintacademy.com/edges/).

The working order taught is: *decide the hard edges first, then hunt for opportunities to
lose edges, then let everything else be soft.* `[relayed]`
([Tucson Art Academy / Skip Whitcomb](https://tucsonartacademyonline.com/blog/2025/11/5/controlling-your-edges)).
Note the direction of that procedure — it is not "sharpen the important thing", it is
"soften and lose everything else". A converter that starts from a photograph is in exactly
that position: the photo is uniformly sharp, and the painterly move is subtractive.

**Lost-and-found edges** are the specific case where a contour alternately disappears into
and re-emerges from its surroundings along its length. Painters use it to tie a figure into
its ground and to keep a silhouette from reading as a cut-out. `[relayed]`
([Livia Dias, "Lost and Found Edges"](https://www.liviadias.com/blog/oil-painting-lost-and-found-edges) ·
[Louise De Masi](https://www.louisedemasi.com/tips/2023/11/23/how-to-paint-lost-and-found-edges-in-watercolour)).

**Why this matters computationally.** An edge is lost when the value difference across it
falls below the threshold at which the eye resolves it. The app's palette mapping already
*creates* lost edges accidentally — two neighbouring photo colours that snap to the same
mixture. Currently that happens wherever the gamut is sparse, which is arbitrary. The
opportunity is to make it happen where a painter would want it: away from the focal point,
in shadow, in the periphery. `[inferred]`

### 1.2 Brush economy and mark size

- **The brush sets the resolution floor.** Nothing smaller than the brush can be stated.
  This is the core of the standard advice to work with a brush that feels too big: *"use
  the biggest brush possible so that you think in the biggest shapes possible… a larger
  brush forces you to simplify."* `[relayed]`
  ([Carol L. Douglas](https://www.watch-me-paint.com/tag/alla-prima-painting/) ·
  [Paint With Mark, alla prima](https://paintwithmark.co.uk/blog/alla-prima-painting-method/)).
- **Economy of marks is an explicit discipline.** *"Every brushstroke should be deliberate…
  thinking that you're forking over a dollar for each touch of the brush raises your
  awareness."* When the large shapes are correct, detail can be suggested with a few marks
  — and that is described as *harder* than rendering it. `[relayed]`
  ([Carol L. Douglas](https://www.watch-me-paint.com/tag/alla-prima-painting/)).
- **Two or three brushes, not ten.** Practical advice converges on a small ladder of brush
  sizes worked large-to-small, which is precisely the coarse-to-fine layering that
  Hertzmann's algorithm formalises (§3.5). `[relayed]`
- **Mark size is not uniform across the canvas.** Foreground and focal areas get smaller,
  more numerous marks; background and periphery get fewer, larger ones. This is the same
  hierarchy as §1.1, expressed through mark size rather than edge quality — and in practice
  the two co-vary, because a large brush cannot make a hard small edge. `[inferred, from
  the two sets of sources above]`

**Computational reading:** mark size and edge softness are the *same* parameter seen from
two sides. A spatially-varying scale field `R(x,y)` — small at the focal point, large at the
periphery — drives both. `[inferred]`

### 1.3 Directional stroke fields

Two canonical, and importantly *different*, models:

- **Van Gogh: strokes follow the form.** Stroke direction aligns with the principal
  curvature / surface flow of the depicted object, so the marks themselves construct the
  perceived geometry. `[relayed]`
- **Cézanne: the constructive stroke is independent of the form.** From the late 1870s
  Cézanne used a system of parallel strokes — much of the foliage at roughly 45°
  *irrespective of the underlying form* — that conveys volume and space without following
  contours. `[relayed]`
  ([The Eclectic Light Company, "Cézanne and constructive strokes"](https://eclecticlight.co/2015/11/17/trees-in-the-landscape-6-paul-cezanne-and-constructive-strokes/) ·
  [Art Institute of Chicago, "Cézanne's Still Lifes under the Microscope"](https://www.artic.edu/articles/991/cezanne-s-still-lifes-under-the-microscope)).

This split is directly actionable: it is the difference between a **data-driven**
orientation field (derived from image structure) and a **constant or procedural** one
(a global angle, or a slow noise field). Both are cheap; they are different style presets,
not different amounts of quality. `[inferred]`

The image-processing counterpart of "follow the form" is *strokes perpendicular to the
image gradient*, i.e. along the isophotes. Both Litwinowicz and Hertzmann use exactly this
(§3.5). The counterpart of the constructive stroke is a fixed angle with jitter. `[verified,
from the algorithm descriptions in §3.5]`

### 1.4 Impasto, glazing, scumbling

| | What it is | Optical consequence |
|---|---|---|
| **Impasto** | Paint thick enough that the marks keep physical height; ridges catch raking light | Adds a *geometry* channel the screen has no equivalent for. Highlights and micro-shadows are view- and light-dependent, so the surface changes as you move. |
| **Glazing** | A thin, highly translucent layer over a dry underlayer | Acts like a coloured filter over what is below; light passes through, reflects off the underlayer, and passes back. Deepens and enriches; *goes darker*. |
| **Scumbling** | A thin, broken, dry or semi-dry lighter layer skimmed over a darker one, leaving the underlayer showing through the gaps | *Goes lighter*; produces a veiled, atmospheric, granular colour that is neither layer. |

`[relayed]` — [Jackie Garner, "Glazing and Scumbling"](https://garnerwildlifeart.wordpress.com/2022/06/03/glazing-and-scumbling-how-why-and-when/) ·
[Carol L. Douglas, "glazing vs. scumbling"](https://www.watch-me-paint.com/monday-morning-art-school-glazing-vs-scumbling/) ·
[MontCarta, "Impasto"](https://montcarta.com/blogs/art-guide-inspiration/impasto-painting-definition-history-identification).
The mnemonic in two of those sources: *"glazes go darker, scumbling goes lighter."*

**The one with a physical model already in this codebase is glazing.** Kubelka–Munk with a
finite thickness over a non-white background is exactly the glaze equation, and
`docs/research/acrylic-blending-findings.md` §"Glazing is different math from mixing"
already establishes that the project treats it as a distinct computation. `[verified]`
Scumbling is the same equation with a spatially-broken coverage mask; impasto is not a
colour operation at all but a height-field/lighting one (§3.9).

### 1.5 Broken colour and optical mixing

Divisionism/pointillism places small touches of unmixed paint side by side so they combine
in the viewer's eye rather than on the palette, on the theory that this yields more
luminous results than subtractive mixing. `[relayed]`
([The Art Story, Seurat](https://www.theartstory.org/artist/seurat-georges/) ·
[ideelart, "Divisionism"](https://ideelart.com/blogs/magazine/divisionism-and-its-influence-on-color-in-art)).

**But the mechanism is not what the popular account says, and the difference is the
important part.** Juxtaposed marks below the resolution limit produce **additive-averaging
(partitive) mixing**, not additive mixing. The result is the *average of the light*
arriving from the patch, weighted by area. Consequences `[relayed]`, from
[David Briggs, *The Dimensions of Colour*, "additive-averaging"](http://www.huevaluechroma.com/044.php)
(via search snippet; the page itself would not fetch from this environment — see §7)
and [Colour Literacy Project, "Comparative Mixing Explained"](https://colourliteracy.org/comparative-mixing-explained):

- Alternating blue and yellow stripes average to a **middle grey**, not to white (that
  would be simple additive mixing) and not to green (that would be subtractive mixing).
- More generally, *"optical mixtures of varied paint colours tend, due to the averaging
  principle, to lie towards the middle of colour space — medium value and low to medium
  chroma."*
- Scale matters and the transition is not clean: enlarged, yellow and blue dots read as
  separate dots; shrunk and repeated, the same arrangement can read as green (a
  non-linear, spatial-frequency-dependent perception, not a simple average).

There is also a documented sceptical position: the dots *"are not actually combined by the
human eye, which still sees them as separate colours, but they do appear to oscillate or
vibrate, creating a type of shimmer."* `[relayed]`
([zenmuseum, Seurat](https://www.zenmuseum.com/finder/page/georges-seurat-pointillism-technique)).
This is the honest reading: at real viewing distances the effect is *partly* fusion and
*partly* vibration, and the vibration is arguably the point.

Crucially for this project: **partitive mixing follows a straight line in linear light
between the component colours; Kubelka–Munk mixing follows a curve that is generally
darker and, for near-complementary pairs, far less chromatic.** These are two different
reachable sets from the same paints. `[inferred, but a direct consequence of the two
mixing laws]` See §3.7 for what to do with that.

### 1.6 Grounds, imprimatura, and letting the ground show

- **Imprimatura** is a thin transparent stain over the ground — literally "first paint
  layer" — that gives a toned, transparent base for everything above.
  [Wikipedia, Imprimatura](https://en.wikipedia.org/wiki/Imprimatura) `[relayed]`
- **It supplies the mid-tone.** Working from a toned ground means only lights and darks
  need stating; on white, every colour is judged against the brightest possible value,
  which is described as *"the hardest possible"* starting point. `[relayed]`
  ([SJB Fine Art](https://sjbfineart.com/blogs/art-blog/the-magic-of-toning-the-ground-a-simple-painters-secret) ·
  [KraftGeek, "Why professional painters never start on white"](https://kraftgeek.com/blogs/creator-inspiration/why-professional-painters-never-start-on-white-and-what-they-do-instead)).
- **It unifies.** *"The imprimatura not only provides an overall tonal optical unity in a
  painting"* — and care is taken **not to cover it completely**, so it shows through
  particularly in mid-to-dark shadow areas. `[relayed]` ([Wikipedia](https://en.wikipedia.org/wiki/Imprimatura)).

**Computationally this is the cheapest style lever on the entire list.** A single colour,
chosen from the achievable gamut, allowed to show through everywhere the coverage mask is
open, is a global harmoniser. It is a scumble with a constant colour, and it requires no
per-pixel analysis at all. `[inferred]`

### 1.7 Where paint physically cannot go

- **No pure black.** The blackest ordinary black paints sit around **5% light reflectance**
  `[relayed]` ([The Land of Color, LRV guide](https://thelandofcolor.com/lrv-light-reflectance-value-of-paint-colors/)).
  Report 02 §0.3 puts Golden Bone Black at **L\*23.82** against Titanium White's L\*98.25 —
  a ~24:1 contrast ratio against sRGB's effectively unbounded range `[relayed, from the
  sibling report]`. Note that L\*23.8 is a much stronger constraint than 5% LRV would
  suggest, because it is a *paint on a substrate under diffuse light*, not an idealised
  absorber.
- **Limited chroma in the mid-range.** Report 02 §0.4 establishes that the paint gamut is
  an *overlap* with sRGB, not a subset — some paints exceed AdobeRGB in chroma, while
  large regions of bright saturated screen colour are unreachable. `[relayed]`
- **Surface, not emission.** A painting's appearance is a function of the illuminant, the
  viewing angle, and the surface's own specular lobe. Impasto ridges produce
  view-dependent highlights that no flat image can encode; gloss/matte medium changes the
  perceived black point substantially more than any pigment choice. `[inferred, standard
  optics]`
- **Consequence for the app:** the converter cannot make the image look like a *painting
  in a room*. It can make it look like a *photograph of a painting under even light*. That
  is the honest target, and it is what every technique below is aiming at.

---

## Part II — The invariant, stated precisely

The codebase's rule, from `PalettePhotoConverter`'s doc comment `[verified]`:

> Blurring belongs before the mapping and not after it: averaging two mapped pixels
> together produces a color partway between two mixtures, which is not itself a color the
> paints can be mixed to.

That is correct but under-specified. The operative property is not "before vs after" — it
is **whether the operation can synthesise a colour that was not in the candidate set**.
Four categories follow. This taxonomy is the thing to reason with when placing any of the
techniques in Part III.

### 2.1 Category A — pre-map operations on the source photo. Always safe.

Anything at all. Blur, bilateral filter, anisotropic Kuwahara, L0 smoothing, mean shift,
SLIC + region fill, saliency-modulated variable blur, tone compression, DoG darkening,
structure-tensor-guided flow smoothing. The mapping is applied afterwards, so the output
is a candidate colour by construction.

**This is where the great majority of the techniques below belong**, and it is why the
current architecture is in good shape: `Convert` already has a "pre-map spatial stage"
slot, currently occupied by one `GaussianBlur.Apply` call. `[verified]`

**Caveat:** pre-map operations are *blind to the gamut*. A bilateral filter that carefully
preserves a 3-ΔE edge is wasted if both sides map to the same mixture; conversely it may
preserve an edge that the mapping will exaggerate into a 20-ΔE jump. Pre-map spatial work
and gamut-aware work do not compose cleanly, and neither this report nor report 02 has a
good answer for that. `[inferred]`

### 2.2 Category B — post-map operations that only *select* among existing colours. Safe.

An operation whose output at every pixel is *equal to some pixel's already-mapped value*
cannot leave the candidate set. This is a larger and more useful class than it sounds:

- **Median / modal / majority filters.** A median of mapped pixels per channel is *not*
  safe (independent per-channel medians can synthesise a new triple). A **modal filter**
  (most common ARGB value in the window) *is* safe, and is an excellent region-cleanup
  operator for exactly this kind of image. `[inferred]`
- **Morphological open/close on label maps** — safe if applied to indices into the
  candidate set, not to colours.
- **Ordered / blue-noise dithering between two candidates.** Every output pixel is one of
  the two, so it is safe. (§3.7)
- **Hard-edged (aliased) stroke rendering**, where each stroke is filled with one candidate
  colour and strokes are painted opaquely. Safe.
- **Nearest-neighbour resampling.** Safe. Bilinear/bicubic is not.

### 2.3 Category C — post-map arithmetic. Breaks the invariant.

Any averaging, alpha blend, multiply, or interpolation of mapped colours. This includes
several things that look innocuous:

- Anti-aliased stroke edges — the classic trap in stroke-based rendering.
- Winnemöller-style edge overlay, which *multiplies* the quantized colour by a DoG edge
  image (§3.6). The resulting dark edge pixels are usually not achievable.
- Stroke opacity `α < 1` composited in RGB.
- Any downsampling with a filter kernel.
- Bump/impasto shading of the mapped colours (§3.9).

**The universal repair is cheap: re-run the palette map after the offending step.** The
mapping is idempotent on already-mapped colours and costs one nearest-candidate lookup per
distinct quantized colour, which `MapPixelsFlat` already caches at 6 bits/channel
`[verified]`. So "render strokes anti-aliased at 3×, box-downsample, re-map" is a legal
pipeline, at the cost of a second mapping pass and some colour drift at stroke boundaries.
`[inferred]`

### 2.4 Category D — post-map layering through the KM kernel. A *different*, larger invariant.

Glazing and scumbling produce colours that no single mixture can produce, but that the
paints *can physically produce* by layering. Compositing a thin film of paint *i* at
thickness *d* over an already-mapped colour, evaluated through `KubelkaMunk`, yields a
physically achievable appearance.

This is worth naming explicitly because it is the one place where the project can honestly
enlarge its reachable set without weakening its claim. It requires the invariant to be
restated as **"every output pixel is a colour the selected paints can be applied to
produce"** rather than "…can be mixed to". `[inferred]` The candidate-set architecture
would need a second axis (base mixture × glaze paint × thickness), which multiplies the
candidate count — see §3.9 for the cost.

### 2.5 A note on where the mapping's own spatial behaviour bites

The mapping is a nearest-neighbour snap in CIELAB. Two consequences that any spatial
technique has to live with `[inferred]`:

- **It is a contrast amplifier at gamut boundaries.** Where candidates are sparse, a
  smooth photographic gradient becomes a hard banded edge. Pre-map smoothing does not fix
  this — it makes it *worse*, because a smoother gradient crosses the same Voronoi
  boundaries more cleanly, producing longer, straighter, more visible bands. This is the
  strongest single argument for dithering (§3.7) and for deliberate stroke texture (§3.5):
  both break up the bands with structure that reads as intentional.
- **It is a contrast destroyer inside dense regions.** Fine texture in a well-covered part
  of the gamut survives the mapping intact, which is why the output currently keeps
  photographic detail that no brush would state. That is what §3.1–3.3 are for.

---

## Part III — Computational analogues

### 3.1 Edge-preserving smoothing: which of these are painterly and which just denoise

All of these take an image and return a smoother image with edges intact. They differ
enormously in whether the result reads as *painted* or merely as *clean*. My assessment
column is `[inferred]` from the algorithm behaviour; the parameter values are cited.

| Filter | What it computes | Params & ranges | Cost | Failure modes | Painterly? |
|---|---|---|---|---|---|
| **Bilateral** (Tomasi & Manduchi 1998) | Weighted mean where weight = `exp(−‖Δx‖²/2σ_s²) · exp(−Δcolour²/2σ_r²)` | σ_s ≈ 2.5–3 px; σ_r ≈ 25/255 (or 4.25 in Lab units) | O(n·r²) naive; separable approximations O(n·r) | Staircase artefacts; gradient reversal near strong edges where few similar neighbours exist; halos | **Only when iterated.** One pass = denoise. 3–8 passes = "cartoon-like appearance… flattening the colors considerably, but without blurring edges" |
| **Separated orientation-aligned bilateral (OABF)** (Kyprianidis & Döllner 2008) | Bilateral applied 1-D along the gradient, then 1-D along the perpendicular flow | σ_d = 3.0, σ_r = 4.25, n_e = 1–2 pre-edge iterations, n_a total | O(n·r) per iteration | Needs the structure tensor (§3.4); wrong flow ⇒ smeared features | **Yes.** Removes the horizontal/vertical artefacts of naive separable bilateral and produces smooth output at curved boundaries |
| **Anisotropic diffusion** (Perona & Malik 1990) | Iterative PDE `∂I/∂t = ∇·(c(‖∇I‖)∇I)`, `c = exp(−(‖∇I‖/K)²)` or `1/(1+(‖∇I‖/K)²)` | K = gradient threshold (image-dependent); step λ ≤ 0.25 for 4-neighbour stability; 10–50 iterations | O(n) per iteration, many iterations | Slow; K is not scale-invariant so it needs retuning per image; can produce staircasing | Marginal. Bilateral is a non-iterative approximation of it and is easier to tune |
| **Guided filter** (He, Sun & Tang 2010/2013) | Local linear model `q = a·I + b` fitted in each r-window with ridge regularisation ε | r (window radius, px), ε (≈ (0.1–0.4)² in [0,1] units) | **O(n), independent of r** — box filters only | Halo artefacts remain near strong edges; produces *smooth* output, not flat output | **No.** Superb detail/base separation, poor abstraction |
| **Domain transform** (Gastal & Oliveira 2011) | Warps each scanline so geodesic distance becomes Euclidean, then 1-D filters in linear time; iterated H/V | σ_s, σ_r; typically 3 iterations with a geometric σ_s schedule | O(n) per pass, genuinely real-time | 1-D-along-scanlines nature leaves faint axis-aligned structure; licence unclear (§6) | Partly. Fast, but the look is closer to guided filter than to paint |
| **L0 gradient minimisation** (Xu et al. 2011) | Minimises `‖S−I‖² + λ·#{p : ∇S_p ≠ 0}` — counts non-zero gradients | λ ∈ [1e-3, 1e-1] (larger ⇒ fewer edges); κ ∈ [1.05, 8], default **2** | Alternating minimisation with an **FFT** solve per iteration; ~10–30 iterations | Needs a 2-D FFT (not in `System.Drawing`; would need Math.NET or a hand-rolled FFT); can over-flatten into blobby cartoon | **Yes, strongly.** *"Much better results for piecewise constant images"* than TV — it produces genuinely flat regions with sharp boundaries |
| **Rolling guidance** (Zhang et al. 2014) | Gaussian-blur the image, then iteratively joint-bilateral-filter the original *using the previous output as guide* | σ_s sets the **scale** below which structures are removed; σ_r; 4–5 iterations (converges fast) | Cost of the inner joint filter × iterations | Rounds corners; removes small strong-edged structures *by design*, which may delete the subject | **Yes, for scale control specifically.** It is the only filter here whose parameter is "size of thing to delete" rather than "amount of smoothing" |

Sources: [Tomasi & Manduchi, ICCV 1998 (PDF)](https://users.soe.ucsc.edu/~manduchi/Papers/ICCV98.pdf) ·
[Paris et al., *Bilateral Filtering: Theory and Applications* (FnT survey PDF)](https://www.cse.iitd.ac.in/~pkalra/col783-2017/bilateral-filtering.pdf) ·
[Kyprianidis & Döllner, NPAR 2008](https://www.kyprianidis.com/p/npar2008/) with parameters
relayed via [3D Stereoscopic Photography's implementation notes](http://3dstereophoto.blogspot.com/2018/05/non-photorealistic-rendering-image.html) ·
Perona–Malik parameters via [MATLAB Central 14995](https://www.mathworks.com/matlabcentral/fileexchange/14995-anisotropic-diffusion-perona-malik)
and [MedPy docs](https://loli.github.io/medpy/generated/medpy.filter.smoothing.anisotropic_diffusion.html) ·
[He, Sun & Tang, *Guided Image Filtering*, ECCV 2010](https://link.springer.com/chapter/10.1007/978-3-642-15549-9_1) and
[Fast Guided Filter, arXiv 1505.00996](https://arxiv.org/pdf/1505.00996) ·
[Gastal & Oliveira, SIGGRAPH 2011 (PDF)](https://www.inf.ufrgs.br/~eslgastal/DomainTransform/Gastal_Oliveira_SIGGRAPH2011_Domain_Transform.pdf) ·
[Xu et al., *Image Smoothing via L0 Gradient Minimization*, project page](https://www.cse.cuhk.edu.hk/~leojia/projects/L0smoothing/)
with parameter ranges relayed via [mexopencv `cv.l0Smooth`](http://amroamroamro.github.io/mexopencv/matlab/cv.l0Smooth.html) ·
[Zhang et al., *Rolling Guidance Filter*, ECCV 2014, project page](http://www.cse.cuhk.edu.hk/~leojia/projects/rollguidance/).
All parameter figures above `[relayed]`.

**Verdict for this codebase.** Iterated bilateral and L0 are the two that produce genuinely
painterly flattening; guided filter and domain transform are denoisers with an edge term.
But **none of them is the best choice here**, because all of them are *isotropic in
character* — they flatten regions but leave no directional trace. That is §3.2's job.

### 3.2 The anisotropic Kuwahara filter — the highest payoff single filter

**What it is.** The classical Kuwahara filter divides a window into 4 (later N) subregions,
computes mean and variance in each, and outputs the mean of the *lowest-variance* subregion.
That gives flat regions and crisp boundaries, but with severe blocking artefacts. The
**anisotropic** generalisation (Kyprianidis, Kang & Döllner, Pacific Graphics 2009) fixes
this by adapting the window's *shape, scale and orientation* to the local structure tensor,
and by replacing the hard subregion selection with a smooth weighted combination.
`[relayed]` — [project page](https://www.kyprianidis.com/p/pg2009/) ·
[Computer Graphics Forum 28(7)](https://onlinelibrary.wiley.com/doi/10.1111/j.1467-8659.2009.01574.x).

The reason it is on this list and the others are not, in the authors' own framing:
*"Contrary to conventional edge-preserving filters, [it] generates a painting-like
flattening effect along the local feature directions while preserving shape boundaries"*,
and the results *"combine the clearness of cartoon illustrations but also exhibit
directional information as found in oil paintings."* `[relayed]` That directional trace is
exactly §1.3.

**Algorithm sketch** `[relayed]`, sufficient to implement:

1. Compute the smoothed structure tensor (§3.4). Extract local orientation φ and anisotropy
   `A = (λ₁−λ₂)/(λ₁+λ₂) ∈ [0,1]`.
2. Build an **ellipse** at each pixel: major axis along the *minor* eigenvector (the feature
   direction), with eccentricity driven by A. A common parameterisation scales the axes by
   `(α+A)/α` and `α/(α+A)` for a tuning constant α.
3. Divide the ellipse into **N = 8** angular sectors with overlapping, smooth weighting
   functions (Gaussian-weighted char. functions, or the later polynomial weighting variant).
4. Per sector compute weighted mean `m_i` and weighted variance `s_i²`.
5. Output `Σ_i w_i·m_i / Σ_i w_i` with `w_i = 1/(1 + s_i^{q/2})` — the **sharpness exponent
   q** (typically 8) controls how hard the lowest-variance sector wins. Large q ⇒ crisp,
   near-classical Kuwahara; small q ⇒ soft blend.

**Parameters, from independent implementations** `[relayed]`:

| Param | Meaning | Range |
|---|---|---|
| Radius | Window size — **this is the "brush size"** | 2–20 px typical |
| Sharpness | How sharply edges are kept; 0 = heavy blur, 1 = edges perfectly sharp | 0–1 |
| Eccentricity | How thin/directional the ellipse gets | low = circular, high = thin directional |
| Uniformity | How much the direction field is regularised (structure tensor smoothing σ) | *"increase until the result stops changing"* |
| N sectors | 8 | fixed |
| Structure-tensor σ | Gaussian smoothing of the tensor | ≈ 2–3 px |

Sources for the parameter semantics: [Blender Kuwahara node manual](https://docs.blender.org/manual/en/4.0/compositing/types/filter/kuwahara.html) ·
[Blender PR #110786 implementation discussion](https://projects.blender.org/blender/blender/pulls/110786) ·
[Material Maker anisotropic Kuwahara node](https://rodzill4.github.io/material-maker/doc/node_filter_anisotropic_kuwahara.html) ·
[Kyprianidis et al., polynomial weighting variant (EG PDF)](https://diglib.eg.org/bitstreams/3309663a-3134-44bc-9297-2fa33554277d/download).

**Cost.** `O(n · r² )` per pixel for the naive sector accumulation, which for r=8 and
2 MP is ~400 M weighted samples — around 1–3 s single-threaded, well under 1 s under
`Parallel.For` at the same style as `GaussianBlur`. `[inferred]` The GPU implementations run
in real time `[relayed]`, which bounds the arithmetic as modest.

**Failure modes** `[inferred, plus the artefact discussion in the papers]`: in near-isotropic
regions (sky, skin) the ellipse degenerates and the output can look like ordinary Kuwahara —
blotchy and slightly "melted plastic". Very large radii swallow small dark features
entirely. Noise in the structure tensor produces swirling direction fields; the tensor
*must* be Gaussian-smoothed before eigen-decomposition.

**Placement: category A, pre-map.** Replace or supplement `GaussianBlur.Apply` in
`Convert`. Invariant fully preserved.

### 3.3 Mean shift and superpixels — flat regions with real boundaries

Report 02 §5.4 already specifies a bilateral → SLIC → RAG-merge → cleanup pipeline and
lists the C# options. `[verified]` I will not restate it. Two additions specific to the
brushwork question:

**Mean-shift filtering** (Comaniciu & Meer) moves each pixel toward the mode of a joint
spatial+range density, with bandwidths `h_s` (spatial) and `h_r` (range), then groups
convergence points. The published parameter semantics are exactly the two knobs a painter
would want `[relayed]` ([IPOL implementation article, PDF](https://www.ipol.im/pub/art/2019/255/article_lr.pdf)):

- *"Only features with large spatial support are represented in the segmented image when
  `h_s` is increased"* → **`h_s` is the mark size.**
- *"Only features with high contrast survive when `h_r` is large"* → **`h_r` is the edge
  threshold — the hard/soft/lost decision.**

Plus a **minimum region area** M below which regions are merged away. That triple
(`h_s`, `h_r`, M) is a startlingly direct encoding of §1.1 and §1.2. Cost is the problem:
naive mean shift is expensive (many iterations per pixel with an `h_s`-radius window), which
is why OpenCV's `pyrMeanShiftFiltering` is pyramidal. `[inferred]`

**SLIC** is a local k-means in 5-D (L\*, a\*, b\*, x, y) with the search restricted to a
2S×2S window around each centre, `S = √(N/k)`, and distance
`D = √(d_lab² + (m/S)²·d_xy²)` where **m is compactness** (higher = squarer, more
spatially regular, less colour-faithful superpixels). It does **not** enforce connectivity;
a connected-components relabel is required as a post-step. `[relayed]`
([Achanta et al., SLIC](https://infoscience.epfl.ch/record/149300?ln=en); parameters as
summarised in report 02 §5.4). SLIC is ~200 lines with no dependencies — report 02 already
recommends porting rather than taking an OpenCV dependency. `[verified, from report 02]`

**Why superpixels matter *for brushwork* specifically** `[inferred]`: a superpixel is a
plausible proxy for a single brush mark. Filling each superpixel with one candidate colour
gives you, for free: flat regions, a mark-size parameter (S), region boundaries that follow
image edges, and a Category-B-safe output if the fill colour is chosen from the candidate
set. It is the cheapest route to "this was applied, not resampled" that does not require
stroke geometry.

**Placement.** Either pre-map (segment the photo, average each region in CIELAB, then map
the region means — Category A) or post-map (map first, then modal-filter within superpixel
boundaries — Category B). The pre-map form is better: averaging happens on photographic
colour, where averaging is meaningful.

### 3.4 The structure tensor and the edge tangent flow — build this once

Everything directional in this report depends on one field. Build it as a shared
`Imaging/StructureTensor.cs`-shaped utility and four other features become cheap.

**Structure tensor** (Di Zenzo's multi-channel form) `[relayed, standard formulation]`:

```
Ix, Iy   := Sobel gradients, per channel, on linear-light R,G,B
E := Σ_c Ix_c²      F := Σ_c Ix_c·Iy_c      G := Σ_c Iy_c²
J := Gaussian_σ * [[E, F], [F, G]]        # smooth the TENSOR, not the gradients
λ₁,₂ := ((E+G) ± √((E−G)² + 4F²)) / 2      # λ₁ ≥ λ₂
gradient direction θ  := ½·atan2(2F, E−G)
flow (tangent) direction := θ + π/2        # minor eigenvector — the isophote direction
anisotropy A := (λ₁−λ₂)/(λ₁+λ₂) ∈ [0,1]
```

σ ≈ 2–3 px is the value used in the abstraction literature `[relayed, via the Kyprianidis
implementation notes above]`. Smoothing the tensor rather than the gradients is the whole
trick: gradient vectors of opposite sign cancel, tensors do not.

**Edge tangent flow (ETF)** (Kang, Lee & Chui, NPAR 2007) is a nonlinear iterative
smoothing of that direction field, designed to make weak-gradient vectors align with strong
nearby ones so lines come out coherent. The construction, as described in secondary
sources `[relayed]`
([Coherent Line Drawing, project/paper](https://cg.postech.ac.kr/papers/kang_npar07_hi.pdf) ·
[naotokimura/EdgeTangentFlow](https://github.com/naotokimura/EdgeTangentFlow)):

1. Take gradient vectors, rotate 90° CCW to get initial tangents; store normalised gradient
   magnitude ĝ.
2. Iterate `t_new(x) ∝ Σ_{y∈Ω_r(x)} φ(x,y)·t_cur(y)·w_s·w_m·w_d`, where
   `w_s` = 1 inside radius r; `w_m = ½(1 + tanh(η·(ĝ(y) − ĝ(x))))` (stronger edges dominate);
   `w_d = |t(x)·t(y)|` (aligned vectors dominate); `φ = sign(t(x)·t(y))` (resolves the ±
   ambiguity). Normalise. **r ≈ 5, 2–3 iterations.**
3. Result: a smooth field roughly tangent to edges everywhere.

`[unverified detail]` — the exact weight formulae above are reconstructed from secondary
descriptions; the primary PDF would not fetch from this environment (TLS failure on two
mirrors, see §7). Verify against the paper before relying on the exact `w_m` form. The
*structure* of the construction (rotate, then nonlinearly smooth with magnitude- and
alignment-dependent weights) is confirmed by multiple sources.

**Is ETF worth it over the plain smoothed structure tensor?** For line drawing, yes. For
stroke orientation and for anisotropic Kuwahara, the smoothed structure tensor is
sufficient and much cheaper. `[inferred]` Start with the tensor; add ETF only if a
line-drawing mode is built.

### 3.5 Stroke-based rendering — the real thing

This is the literature that directly answers "make it look applied with a brush".

#### 3.5.1 Haeberli 1990, *Paint by Numbers: Abstract Image Representations*

The origin. Represents an image as **an ordered collection of brush strokes**, each with
its own colour, shape, size and orientation, sampled from a source image; the ordering and
the parameters are what abstraction consists of. `[relayed]`
([SIGGRAPH '90, pp. 207–214; Stanford PDF](https://graphics.stanford.edu/courses/cs248-05/haeberli-paint-by-numbers-sig90.pdf)).
Interactive/user-guided rather than automatic. The lasting contribution is the framing —
*a painting is a list of marks, not a raster* — and the observation that stroke colour
should be **sampled from the source at the stroke's location**, which is what keeps the
result faithful without any error term.

#### 3.5.2 Litwinowicz 1997, *Processing Images and Video for an Impressionist Effect*

The first fully automatic version, and the one with the mechanisms most worth stealing
`[relayed]` ([SIGGRAPH 97, pp. 407–414; WPI course summary](https://davis.wpi.edu/~matt/courses/impressionist/)):

- Strokes placed on a **regular grid with random perturbation** of centre, and rendered in
  **random order** to break up the grid.
- Stroke colour = **bilinear sample of the pixels it covers**.
- Random perturbation of length, radius, RGB, intensity, and orientation, all
  user-controlled. (For video, the random values are held constant per stroke across frames
  to avoid jitter.)
- **Orientation normal to the intensity gradient**: `angle = atan2(Gy, Gx) + θ + 90°`, with
  a **thin-plate-spline interpolation** to fill in orientation where the gradient magnitude
  is near zero — an important detail; flat regions have no reliable gradient and must
  inherit direction from their neighbours.
- **Strokes are clipped at edges**: each stroke grows outward from its centre until it hits
  a Sobel edge or reaches max length. This is the mechanism that makes strokes respect form
  boundaries instead of smearing across them.
- Cost then: **81 s/frame for ~120,000 strokes on a 180 MHz machine** — i.e. trivially fast
  now.

The gradient-normal orientation *plus* edge clipping is, together, a very good approximation
of §1.3's "follow the form" and §1.1's hard edges, in maybe 150 lines. `[inferred]`

#### 3.5.3 Hertzmann 1998, *Painterly Rendering with Curved Brush Strokes of Multiple Sizes*

The coarse-to-fine layered algorithm, and the closest computational statement of §1.2's
brush economy: **paint the whole canvas with the biggest brush, then only add smaller
strokes where the canvas still differs from the reference.**
[SIGGRAPH '98 (paper PDF)](https://mrl.cs.nyu.edu/publications/painterly98/hertzmann-siggraph98.pdf) ·
[ACM DL](https://dl.acm.org/doi/10.1145/280814.280951).

Algorithm `[relayed]`, reconstructed from the paper's own pseudocode as restated in
secondary sources ([U. Waterloo CS798 A1](https://cs.uwaterloo.ca/~csk/Old/cs798/winter2008/a1/) ·
[DGP Toronto implementation notes](https://www.dgp.toronto.edu/~bastani/npr/painterly.html)):

```
paint(sourceImage, R1 > R2 > ... > Rn):
    canvas := blank (or a constant ground colour)
    for each brush radius Ri, largest first:
        referenceImage := GaussianBlur(sourceImage, sigma = f_sigma * Ri)
        paintLayer(canvas, referenceImage, Ri)

paintLayer(canvas, refImage, R):
    S := {}                                  # strokes for this layer
    D := |canvas - refImage|                 # per-pixel Euclidean RGB difference
    grid := f_g * R
    for each grid cell M of size grid x grid:
        areaError := (sum of D over M) / grid^2
        if areaError > T:                    # T = approximation threshold
            (x, y) := argmax of D within M
            S += makeSplineStroke(x, y, R, refImage)
    render all strokes in S onto canvas in RANDOM order

makeSplineStroke(x0, y0, R, refImage):
    strokeColour := refImage(x0, y0)
    K := stroke with radius R, colour strokeColour, first control point (x0,y0)
    (x,y) := (x0,y0);  (lastDx,lastDy) := (0,0)
    for i in 1 .. maxStrokeLength:
        if i > minStrokeLength and
           |refImage(x,y) - canvas(x,y)| < |refImage(x,y) - strokeColour|:
               return K                      # canvas already matches better; stop
        if |gradient(x,y)| == 0: return K
        (gx,gy) := gradient of refImage at (x,y)
        (dx,dy) := (-gy, gx)                 # normal to gradient
        if lastDx*dx + lastDy*dy < 0: (dx,dy) := (-dx,-dy)   # no reversals
        (dx,dy) := f_c*(dx,dy) + (1-f_c)*(lastDx,lastDy)     # curvature filter
        normalise (dx,dy)
        (x,y) := (x + R*dx, y + R*dy)
        append (x,y) to K
    return K
```

Three things in there are the algorithm's actual insight, and all three map onto §1
directly `[inferred]`:

- **The reference image is blurred by an amount proportional to the current brush radius.**
  A big brush is *shown a blurry photo* — it is literally not allowed to see detail it
  cannot state. This is the single most transferable idea in the whole paper.
- **Strokes are only placed where the canvas is already wrong by more than T.** That is
  brush economy as an algorithm.
- **`f_c` is the curvature filter**: a first-order low-pass on the direction, so strokes
  bend but do not kink. Small `f_c` ⇒ nearly straight (Expressionist), large ⇒ freely
  curving (Impressionist).

**Style presets** — from a reference implementation `[verified, read from that page]`
([DGP Toronto](https://www.dgp.toronto.edu/~bastani/npr/painterly.html)):

| Preset | brush radii | fCurve | fSigma | max len | min len | error thresh |
|---|---|---|---|---|---|---|
| Impressionist | {8, 4, 2} | 1.0 | 0.5 | 16 | 4 | 2.0 |
| Expressionist | {8, 4, 2} | 0.25 | 0.5 | 16 | 10 | 1.0 |
| Colorist Wash | {8, 4, 2} | 1.0 | 0.5 | 16 | 4 | 4.0 |

The paper also defines a **Pointillist** preset (small radii, zero-length strokes i.e.
dabs, reduced opacity, and deliberate hue/saturation jitter) `[relayed, via the patent
abstract and the DGP page's parameter list; the numeric values for Pointillist were not
recoverable — see §7]`.

**Patent status: EXPIRED.** US 6,011,536, *"Method and system for generating an image
having a hand-painted appearance"*, Hertzmann & Perlin, assigned to New York University,
priority 1998-04-17, filed 1998-05-22, granted 2000-01-04, **status "Expired – Lifetime"**.
`[verified]` ([Google Patents US6011536A](https://patents.google.com/patent/US6011536A/en)).
Hertzmann's own Java reference implementation is **MIT licensed** `[verified]`
([github.com/hertzmann/painterJava](https://github.com/hertzmann/painterJava)).

**Cost.** Dominated by stroke rasterisation. For a 2 MP image with radii {8,4,2} and
`f_g = 1`, the grid cell counts are roughly 2M/64 + 2M/16 + 2M/4 ≈ 660 k candidate cells,
of which only those exceeding T generate strokes. Expect tens of thousands of strokes and
sub-second rendering. `[inferred]`

**Failure modes** `[inferred]`: (a) with a blank canvas and a high threshold, background
gaps stay unpainted — hence the "start from a ground colour" variant, which is also §1.6;
(b) the difference metric is per-pixel Euclidean RGB, which for this project should be
CIELAB or the project's chosen metric; (c) strokes are opaque and drawn in random order,
so the result is stochastic — reruns differ, which is bad for a "tweak the slider and
compare" UI unless the RNG is seeded.

**Invariant.** Stroke colours are sampled from the *reference* (photo) image, so if you map
after rendering, Category A. If you sample from an already-mapped image and render
hard-edged and fully opaque, Category B. If strokes are anti-aliased or `α<1`, Category C —
repair with a post-render re-map (§2.3).

#### 3.5.4 The broader family

Hertzmann's own survey classifies stroke-based rendering into **greedy** methods (place
strokes to reduce error, never revisit — everything above) and **optimisation** methods
(place then iteratively adjust to minimise an energy). `[relayed]`
([*A Survey of Stroke-Based Rendering*, IEEE CG&A 23(4), 2003, PDF](https://www.dgp.toronto.edu/~hertzman/sbr02/hertzmann-cga03.pdf) ·
[tutorial page](https://www.dgp.toronto.edu/~hertzman/sbr02/)). Optimisation methods
produce better results and are far more expensive; greedy is the right choice here.

### 3.6 DoG, flow-based DoG, and the Winnemöller abstraction pipeline

**Winnemöller, Olsen & Gooch, *Real-Time Video Abstraction*, SIGGRAPH 2006.** The canonical
abstraction pipeline: *reduce contrast in low-contrast regions (approximating anisotropic
diffusion) and increase it in high-contrast regions (DoG edges)*, in CIELAB, with optional
soft luminance quantization. `[relayed]`
([ACM DL](https://dl.acm.org/doi/10.1145/1141911.1142018) ·
[SIGGRAPH history archive](https://history.siggraph.org/learning/real-time-video-abstraction-by-winnemoller-olsen-and-gooch/)).

Pipeline and parameters `[relayed]`, as documented by an implementer
([3D Stereoscopic Photography](http://3dstereophoto.blogspot.com/2018/05/non-photorealistic-rendering-image.html)),
which also covers the Kyprianidis & Döllner 2008 flow-based refinement:

1. Convert to CIELAB.
2. `n_e` iterations of (orientation-aligned) bilateral: **σ_d = 3.0, σ_r = 4.25**, `n_e = 1–2`.
3. **DoG edges** on luminance: `S_σe − τ·S_σr` with `σ_r = 1.6·σ_e`, soft-thresholded as
   `D = 1` if the difference is positive else `1 + tanh(φ_e · difference)`.
   **σ_e ≈ 1.0** (described by that implementer as *the* most important parameter — it sets
   edge thickness), **τ = 0.98–0.99**, **φ_e = 0.75–5.0**.
   *Flow-based* DoG (FDoG) instead applies the 1-D DoG across the flow and then smooths the
   response **along** the flow with **σ_m ≈ 3.0**, which is what makes lines coherent
   instead of speckled.
4. Continue bilateral to `n_a` total iterations.
5. **Soft luminance quantization**: nearest bin plus a `tanh` soft transition,
   `q_levels = 8–10`, **φ_q = 3.0–14.0** (softness). Sharp quantization bands; the tanh is
   what stops it looking like posterisation.
6. Multiply the quantized colour by the edge image `D`.

**FDoG** in its own right: Kang, Lee & Chui, *Flow-Based Image Abstraction*, IEEE TVCG 2009
— [PDF](https://pages.cs.wisc.edu/~dyer/cs534/papers/kang_tvcg09.pdf) — introduces an
anisotropic, flow-aligned, curve-shaped DoG kernel; results in the paper use an edge-amount
parameter r = 0.5 and 0.8. `[relayed]`

**Honest assessment for this project** `[inferred]`:

- **Step 6 is a Category-C violation** and cannot be used as written. The multiply produces
  darkened edge pixels that are almost never achievable mixtures. Two legal repairs:
  (a) apply the DoG darkening to the **photo**, before mapping (Category A), so the dark
  edges get mapped onto the darkest achievable mixture; or (b) after mapping, replace edge
  pixels **wholesale** with a chosen dark candidate colour (Category B) — which is what a
  painter drawing a dark contour line actually does anyway.
- **Repair (b) is more faithful to painting.** A painter's contour is a mark of one colour,
  not a multiplicative darkening. A hard-edged FDoG line filled with the darkest achievable
  mixture, at a width tied to the mark-size parameter, is both legal and correct.
- **Step 5 (quantization) is redundant here.** The palette mapping already quantizes, and
  far more meaningfully. Do not stack a second quantizer.
- **The outlined-shape look is a strong style but a narrow one.** It reads as illustration /
  cel animation, not as painting. Offer it as a distinct mode, not as part of the default
  path.

### 3.7 Dithering as broken colour — a critical assessment

Report 02 §5.3 argued against dithering on three grounds (no contiguous regions for a
paint-by-numbers guide; dither cells must be sub-mm to fuse; wet acrylic touching wet
acrylic mixes subtractively anyway) and recommended offering a "Divisionist" mode later as
a coarse ordered dither at the region level. `[verified, read from that report]` I agree
with the conclusion and think the reasoning under-sells the case. Three additions:

**(a) Dithering preserves the invariant exactly.** This is worth stating plainly because
the codebase comment's framing ("averaging two mapped pixels produces a colour the paints
cannot mix to") could easily be read as forbidding it. It does not. Every pixel of a
dithered image *is* one of the candidate mixtures. Dithering is Category B. `[inferred]`

**(b) Dithering reaches colours the current converter provably cannot.** This is the real
argument, and it follows from §1.5. Perceived colour of a fine juxtaposition is the
**area-weighted average of the radiance**, i.e. a straight-line interpolation in linear
RGB/XYZ. Kubelka–Munk mixture of the same two paints is a different curve. For most pairs
the partitive path is **lighter and more chromatic** than the KM path; for near-complements
the difference is dramatic (partitive → neutral grey of middling value; KM → a dark, dull,
tertiary). So a dithered mode is not a fidelity compromise — it is a **genuine gamut
extension**, in a direction (lightness and mid-chroma) where report 02 §0.3 identifies the
paint gamut as most constrained. `[inferred, from the two mixing laws; the partitive
averaging law itself is `[relayed]` from Briggs / Colour Literacy Project as cited in §1.5]`

**Worth measuring before building:** compute, for the current default palette, the volume
of CIELAB reachable by KM mixture versus the volume reachable by KM mixture ∪ partitive
average of pairs of KM mixtures. If the second is not meaningfully larger, this argument
collapses. That test is ~30 lines against the existing `SampleAchievableColors`.
`[inferred]`

**(c) Error diffusion is the wrong dither for this and blue noise is only half-right.**

| | Behaviour | Reads as |
|---|---|---|
| **Floyd–Steinberg** (7/16, 3/16, 5/16, 1/16) | Excellent tone preservation; but *"slightly blurred images with some loss of detail, as well as visually disruptive worm- and alignment-artifacts at certain intensity levels"*, plus scanning and start-up artefacts | Noise, and specifically *serial* noise — the worms have a direction that has nothing to do with the image |
| **Ordered / Bayer** | Deterministic, tileable, parallel-friendly | *"notably periodic patterns that are even much more obtrusive than those produced by error diffusion"* — reads as a screen door |
| **Blue-noise mask** | *"about as good as certain error diffusion algorithms while doing a noticeably better job of preserving details"*; no worms, no periodicity | Fine grain. The best *invisible* dither — which is the opposite of what broken colour wants |

`[relayed]` — [Wikipedia, Floyd–Steinberg](https://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering) ·
[dalpil/structure-aware-dithering](https://github.com/dalpil/structure-aware-dithering) ·
[ASCII Magic, Complete Guide to Dithering](https://www.ascii-magic.com/blog/complete-guide-to-dithering).

The critical point `[inferred]`: **all three of these are designed to be invisible at
viewing distance. Broken colour is designed to be visible.** Seurat's dots are 2–4 mm and
emphatically resolvable; the "shimmer" of §1.5 exists precisely *because* fusion is
incomplete. A dither that succeeds by halftoning standards fails by divisionist standards.

What actually corresponds to broken colour is a **clustered-dot dither at the mark scale**:
cells of `R` pixels (the same `R` as the brush radius elsewhere in the pipeline), each cell
filled entirely with one of two candidate colours, with the *proportion of cells* — not the
proportion of pixels within a cell — carrying the tone. That is a halftone screen with a
very coarse ruling, and it is both Category-B-safe and physically paintable: each cell is
one brush touch. `[inferred]`

**Two-colour choice.** For a target colour C, pick the two candidates whose *linear-light
average* at some ratio best matches C, rather than the single nearest candidate. That is a
different and slightly larger search than the existing `NearestCandidateArgb` — a pair
search over the candidate set, which for ~10⁵ candidates is too big to brute-force but is
tractable if restricted to the k nearest candidates (k ≈ 32) and their pairwise averages.
`[inferred]`

**Blue-noise patent note:** several *"Method and apparatus for halftone rendering of a gray
scale image using a blue noise mask"* patents exist (US 5,111,310; 5,341,228; 5,477,305;
5,543,941; 5,708,518; 5,726,772). Priority dates are early 1990s, so all are long expired.
`[relayed, from the USPTO listings surfaced in search]` — confirm before shipping if this
matters to you, but the risk is effectively nil.

### 3.8 Saliency — finding the focal point cheaply

§1.1's rule needs to know *where the focal point is*. Options, cheapest first `[inferred]`:

1. **Let the user click it.** One click, one radius slider. Zero algorithm, better result
   than any automatic method, and it matches how a painter actually decides. This should be
   the default.
2. **Centre-weighting.** A radial falloff from the image centre. Two lines. Right most of
   the time for portraits and snapshots.
3. **Frequency-tuned saliency** (Achanta et al., CVPR 2009): saliency `= ‖I_μ − I_ωhc(x)‖`
   in CIELAB, where `I_μ` is the whole-image mean Lab vector and `I_ωhc` is a small
   Gaussian-blurred (e.g. 5×5 binomial) version of the image. Full resolution, well-defined
   boundaries, *"simple to implement and computationally efficient"*. `[relayed]`
   ([EPFL Infoscience record](https://infoscience.epfl.ch/entities/publication/6cffdd63-3d89-48e3-bc2a-04a314a6f675)).
   **This is roughly 20 lines** and reuses the existing `RgbToLab`. Its known weakness is
   that it flags anything colour-distinct from the global average, so a bright background
   defeats it.
4. Anything learned — out of scope (§3.10).

Once you have a saliency/attention field `s(x,y) ∈ [0,1]`, the whole of §1.1 and §1.2 is
one substitution: **make the existing blur radius a function of `s`.** `R(x,y) = R_max·(1−s)`.
That is the highest payoff/effort item in this entire report (§8).

### 3.9 Impasto, glazing, and scumbling — computational forms

**Impasto** requires a height field and a lighting pass. Hertzmann's *Fast Paint Texture*
(NPAR 2002) is the reference method: assign a height map to each stroke, render the strokes
textured with those height maps to build a painting-wide height field, compute a normal map
from it, and bump-map the painting's colours. `[relayed]`
([PDF](https://www.dgp.toronto.edu/papers/ahertzmann_NPAR2002.pdf) ·
[ACM DL](https://dl.acm.org/doi/abs/10.1145/508530.508546)). See also IMPaSTo (Baxter et
al., NPAR 2004) for a full interactive paint model `[relayed]`
([PDF](http://gamma.cs.unc.edu/IMPASTO/publications/Baxter-IMPaSTo_Web-NPAR04.pdf)).

**For this project impasto is a trap.** `[inferred]` The lighting pass multiplies the mapped
colours by a shading term — Category C, and unlike the DoG case there is no clean Category-B
repair, because the whole point is a continuous shading gradient. Re-mapping after shading
would work but would quantize the highlights into visible steps. And the app's stated goal
is "an image painted with only a chosen set of real acrylic paints" — a fake lighting term
actively undermines the claim that the colours are achievable. If it is built at all, it
should be an explicitly-labelled **presentation** effect applied last, outside the
invariant, with a checkbox that says so.

**Glazing is the opposite case — build this one.** `[inferred]` The KM kernel already
supports it. A glaze pass is:

```
for each pixel:
    base := the already-mapped mixture's reflectance spectrum
    result := KM_composite(glaze_paint, thickness d, over background = base)
    output := SpectralRenderer.ToDisplayColor(result)
```

Every output colour is physically achievable by layering (Category D, §2.4). A single
global glaze paint and thickness is one slider and one paint picker, and it does what §1.6's
imprimatura does at the other end of the process: it unifies. A **spatially varying** `d`
driven by depth or saliency gives atmospheric perspective almost for free.

**Scumbling** is the same operation with a broken coverage mask and a *lighter, opaquer*
paint: composite only where a noise/texture mask exceeds a threshold, leaving the underlayer
showing through elsewhere. If the mask is binary, each pixel is either the base candidate or
the (base + full scumble) candidate — Category B if both are pre-computed as candidates.
`[inferred]`

**Letting the ground show through** is scumbling's inverse and the cheapest of the three: a
single ground colour chosen from the candidate set, revealed wherever a coverage mask is
open. If the mask is binary, Category B, zero risk. `[inferred]`

### 3.10 Neural methods — background context only

The target is `net5.0-windows` WinForms with no ML runtime `[verified, from
`PaintTranslator.csproj`]`, so this is orientation, not a recommendation.

- **Gatys-style optimisation NST and its feed-forward successors** transfer *texture
  statistics*, not marks. They have no notion of a stroke, a palette, or an achievable
  colour, and they routinely emit colours far outside any paint gamut. Fundamentally
  incompatible with the invariant unless followed by a palette map, at which point the
  palette map is doing the interesting work.
- **The genuinely relevant neural line is stroke-*parameter* prediction**, not pixel
  synthesis: models that output a list of (position, angle, length, width, colour) strokes,
  which could then be palette-mapped and rendered by classical code. Examples:
  [Rethinking Style Transfer: From Pixels to Parameterized Brushstrokes (arXiv 2103.17185)](https://arxiv.org/pdf/2103.17185) ·
  [AttentionPainter (arXiv 2410.16418)](https://arxiv.org/pdf/2410.16418) ·
  [Hybridizing Expressive Rendering: SBR with Classic and Neural Methods (arXiv 2506.00870)](https://arxiv.org/pdf/2506.00870).
  `[relayed]` If this project ever grows an ONNX Runtime dependency, *that* is the shape of
  the integration — and note that the renderer and the palette mapper would both still be
  the classical code written for §3.5.
- **Verdict:** nothing here changes the near-term plan. Hertzmann + structure tensor +
  anisotropic Kuwahara covers most of the visual gap at a fraction of the complexity.

---

## Part IV — Licences and encumbrances

| Technique | Status |
|---|---|
| **Hertzmann painterly rendering** | US 6,011,536 (NYU) **expired** `[verified]` ([Google Patents](https://patents.google.com/patent/US6011536A/en)). Reference Java implementation **MIT** `[verified]` ([painterJava](https://github.com/hertzmann/painterJava)). Clear to implement. |
| **Bilateral filter** | No patent encumbrance known; ubiquitous in permissively-licensed code. `[inferred]` |
| **Anisotropic diffusion (Perona–Malik)** | 1990 publication; no live encumbrance. `[inferred]` |
| **Guided filter** | No patent found in this research. Authors were at Microsoft Research; treat as `[unverified]` and check before commercial release. |
| **Domain transform** | Project page carries only the ACM author's-version copyright notice; **no code licence is stated** `[verified]` ([project page](https://www.inf.ufrgs.br/~eslgastal/DomainTransform/)). Do not port their code; reimplement from the paper if wanted. |
| **L0 gradient minimisation** | CUHK project page offers code with no licence stated on the page `[verified]` ([project page](https://www.cse.cuhk.edu.hk/~leojia/projects/L0smoothing/)). Reimplement from the paper. |
| **SLIC** | EPFL page carries a **disclaimer only, no licence** `[verified]` ([IVRL page](https://www.epfl.ch/labs/ivrl/research/slic-superpixels/)). Report 02 already points at [SLICOSharp](https://github.com/junjiez/SLICOSharp) as a C# option; check *its* licence. |
| **Anisotropic Kuwahara** | No patent found. Note the widely-cited Blender implementation is **GPL** ([PR #110786](https://projects.blender.org/blender/blender/pulls/110786)) — read the papers and the permissively-licensed shader ports, not the Blender source, unless this project is GPL-compatible. |
| **Blue-noise masks** | Multiple early-1990s US patents (5,111,310 etc.), all long expired. `[relayed]` |
| **Floyd–Steinberg** | 1975/76 publication. Unencumbered. `[inferred]` |

---

## Part V — What I could not verify

- **Hertzmann's own default parameter values from the paper** (T, f_g, f_c, f_σ, opacity,
  jitter ranges, and the Pointillist preset numbers). Three PDF mirrors either 403'd, failed
  TLS, or returned undecodable binary from this environment. The preset table in §3.5.3 is
  read from one implementation's page, whose error-threshold scale (2.0/1.0/4.0) is clearly
  not the paper's own. **Verify against the paper before hard-coding defaults.**
- **The exact ETF weight formulae** (`w_m`, `w_d`, φ, η, r, iteration count) in §3.4 — the
  Kang/Lee/Chui PDF would not fetch (TLS `KEY_USAGE_BIT_INCORRECT` on the NCKU mirror,
  binary garbage on the POSTECH mirror). The structure is confirmed by several secondary
  sources; the constants are not.
- **The anisotropic Kuwahara paper's own recommended α, q, and radius values.** The PDF at
  kyprianidis.com returned undecodable binary. The parameter *semantics* in §3.2 are
  verified from three independent implementations (Blender, Material Maker, Godot shader);
  the numeric defaults are `[inferred]` from those implementations' UI ranges.
- **Winnemöller's σ_e, τ, φ_e, φ_q and q_levels** are relayed from an implementer's notes,
  not the paper (the Colby mirror failed TLS certificate verification). They are internally
  consistent with the Kyprianidis & Döllner values from the same source, which is mild
  corroboration.
- **David Briggs' huevaluechroma.com additive-averaging page would not fetch** (empty
  response on both http and https). The partitive-averaging claims in §1.5 rest on the
  search-result excerpt plus the Colour Literacy Project page. The *physics* (area-weighted
  radiance average) is not in doubt; the specific "middle value, low to medium chroma"
  phrasing is `[relayed]` from an excerpt I could not open in full.
- **Guided filter patent status** — searched, nothing found, but absence of evidence.

---

## Actionable levers

Ranked by visual payoff ÷ implementation cost. "Invariant" column: **kept** = every output
pixel is provably a colour the selected paints can be mixed to; **kept (layering)** =
Category D, achievable by layering rather than mixing (§2.4); **needs repair** = Category C
unless a re-map pass is added.

---

**1. Spatially varying blur radius driven by a focal point.**
*Payoff: very high. Cost: very low.*

**What it does.** Implements §1.1 and §1.2 at once. The focal region keeps its detail and
hard edges; the periphery loses both. This is the difference between "a photo with a
uniform soft-focus filter" and "a painting". It also improves *fidelity where it matters*
by spending the palette's discriminating power on the subject.

**Where it slots.** Replaces the single `GaussianBlur.Apply(..., blurRadius)` call inside
`Convert`, strictly **before** `MapPixelsFlat`. Category A.

**Parameters.** Focal centre (x, y) — a click on the preview, defaulting to image centre;
focal radius as a fraction of the short edge (0.1–0.6); `R_min` (0–3 px) and `R_max`
(current slider, 0–30 px); falloff exponent (1–3). Optionally replace the click with
frequency-tuned saliency (§3.8, ~20 lines).

**Effort.** Small. The cleanest implementation is *not* a per-pixel variable kernel — it is
to build 3–5 blurred copies at geometrically-spaced radii using the existing separable
`GaussianBlur`, then per-pixel lerp between the two bracketing levels according to
`R(x,y)`. That reuses `GaussianBlur.Apply` unchanged and costs ~4× one blur. Roughly one
new file of ~120 lines plus two toolbar controls. Note the lerp between blur levels must
happen in linear light, consistent with `GaussianBlur`'s existing convention.

**Invariant: kept.** Everything happens on the photo.

---

**2. Anisotropic Kuwahara as an alternative to Gaussian pre-blur.**
*Payoff: very high. Cost: low–medium.*

**What it does.** Flattens the image into paint-like patches that are *elongated along
feature direction*, with edges preserved or sharpened. It is the only single filter on this
list whose output already looks like brushwork rather than like a blur or a denoise (§3.2).
Combined with lever 1 (radius varying with saliency), it delivers most of the perceived gap
in one step.

**Where it slots.** Same position as lever 1 — pre-map, in place of or after `GaussianBlur`.
Category A.

**Parameters.** Radius 2–20 px (the mark size; make it the same field as lever 1's `R(x,y)`);
sharpness/q exponent 1–16 (default 8); eccentricity/α (default ~1); structure-tensor σ 2–3;
N = 8 sectors fixed; 1–2 iterations.

**Effort.** Medium. Structure tensor (~60 lines, shared — see lever 5), then the sector
accumulation (~120 lines). `Parallel.For` over rows in the same style as `GaussianBlur`.
Watch the r² inner loop; precompute the sector weight lookup per (dx, dy) offset once per
radius rather than per pixel.

**Invariant: kept.**

---

**3. A toned ground with a coverage mask.**
*Payoff: high. Cost: very low.*

**What it does.** Implements §1.6. One achievable colour showing through everywhere the
coverage mask is open acts as a global harmoniser and immediately reads as "painted on
something" rather than "computed per pixel". It also directly attacks the banding problem
of §2.5 by inserting a common colour into every transition.

**Where it slots.** After the map. Choose the ground colour from the candidate set (a
mid-value neutral or a warm earth), build a binary coverage mask (Perlin/value noise
thresholded, or "open wherever the local gradient magnitude is low", or "open wherever the
mapped colour changed least from the photo"), and write the ground colour into the open
pixels. Category B — every pixel is either its mapped colour or the ground colour.

**Parameters.** Ground colour (picker restricted to the candidate set); coverage fraction
(0–30%); mask scale in px (tie to mark size); optionally bias the mask toward shadows, per
§1.6's "particularly in mid-to-dark shadow areas".

**Effort.** Very small — under 80 lines, no new maths beyond a value-noise function.

**Invariant: kept**, provided the ground colour is drawn from `CandidateSet.Argb` and the
mask is binary (no feathering — feathering is Category C).

---

**4. Modal / region cleanup after mapping.**
*Payoff: medium-high. Cost: very low.*

**What it does.** Removes the salt-and-pepper speckle where neighbouring photo colours land
on different mixtures — the most obviously non-painterly artefact the current output has.
It enforces §1.2's "a passage is stated in as few marks as possible" without any stroke
machinery.

**Where it slots.** Immediately after `MapPixelsFlat`. A modal filter (most frequent ARGB
in a `k×k` window) followed optionally by a minimum-region-area pass over connected
components. Category B by construction.

**Parameters.** Window size 3–9 px; minimum region area in px (10–200); number of passes
(1–3).

**Effort.** Small. A modal filter over an image with a *small* number of distinct colours
is cheap: map ARGB → candidate index once, then histogram over a sliding window of indices.
~100 lines. The connected-components min-area pass is another ~120.

**Invariant: kept** — output values are drawn from the input's own values.

**Caveat:** modal filtering rounds corners and can erase genuinely thin features (a mast, a
whisker). Cap the window size and consider protecting high-saliency regions.

---

**5. Structure tensor as shared infrastructure.**
*Payoff: enabling (zero on its own). Cost: low.*

**What it does.** Nothing visible. It is the prerequisite for levers 2, 6, 7 and for any
future flow-based work (§3.4). Building it once, well, avoids three half-implementations.

**Where it slots.** A new `Imaging/StructureTensor.cs` computing, on the *photo* (pre-map,
linear light): E/F/G, Gaussian-smoothed with σ, then per-pixel `λ₁`, `λ₂`, flow angle, and
anisotropy `A`, into four `float[]` planes.

**Parameters.** σ (2–3 px). That is all.

**Effort.** Small — ~80 lines including the closed-form 2×2 eigendecomposition. Reuse
`GaussianBlur`'s separable machinery on the three tensor planes.

**Invariant: n/a** (produces no colours).

---

**6. Stroke-based rendering, Hertzmann-style, coarse-to-fine.**
*Payoff: very high. Cost: high.*

**What it does.** The real thing (§3.5.3): large brush first over the whole canvas, smaller
brushes only where the canvas is still wrong. Delivers mark size, mark direction, edge
character, brush economy, and visible facture in one algorithm — and its "blur the reference
by `f_σ·R`" rule is the correct formalisation of §1.2.

**Where it slots.** Two viable placements, and the choice matters:
- **(i) Pre-map:** render the painting from the *photo*, then run the existing
  `MapPixelsFlat` over the result. Simplest, Category A, and lets strokes be anti-aliased
  and blended freely because the map cleans up afterwards. **Recommended.**
- **(ii) Post-map:** sample stroke colours from the mapped image and render hard-edged and
  opaque. Category B, no second map needed, but aliased stroke edges look worse and the
  stroke colours are already quantized so the coarse layers cannot carry smooth
  under-tones.

**Parameters.** Brush radii (default {8, 4, 2}, scaled to image size); `f_σ` blur factor
(0.5); `f_g` grid factor (1.0); `f_c` curvature filter (0.25 Expressionist – 1.0
Impressionist); min/max stroke length (4–10 / 16); approximation threshold T; RNG seed
(expose it, or fix it — see failure modes in §3.5.3); optional colour jitter for a
Pointillist preset.

**Effort.** High for this codebase — the largest item on the list. Needed: a stroke
rasteriser (round-capped thick polyline over an `int[]` buffer, no `System.Drawing.Graphics`
if you want determinism and speed), the per-layer difference/grid/argmax pass, the spline
walk, and the layered driver. Realistically 400–600 lines plus UI. Use the existing gradient
from lever 5 for the stroke direction rather than recomputing.

**Invariant: kept** under placement (i); **kept** under (ii) if strokes are hard-edged and
opaque; **needs repair** (a second `MapPixelsFlat` pass) if strokes are anti-aliased or
translucent after mapping.

---

**7. A glaze pass through the Kubelka–Munk kernel.**
*Payoff: medium-high, and unique — nothing else on this list changes the colour physics.
Cost: low–medium.*

**What it does.** Implements §1.4's glazing. A thin transparent film of one paint over the
mapped image warms/cools/unifies the whole picture and deepens the shadows in a way no
per-pixel remapping can, because the result is a *layered* colour, not a mixed one. With a
spatially varying thickness driven by depth or saliency, it becomes atmospheric perspective.

**Where it slots.** After `MapPixelsFlat`, as a per-pixel spectral composite through the
existing `KubelkaMunk` / `SpectralRenderer` code. Category D — it enlarges the reachable set
in a physically honest direction, but it requires the project to restate its invariant as
"achievable by application" rather than "achievable by mixing" (§2.4).

**Parameters.** Glaze paint (from the selected palette); thickness `d` (0 = off, up to full
opacity); optional thickness field (constant / linear vertical / saliency-driven).

**Effort.** Low–medium *if* the KM code already supports finite thickness over a
non-unit background; report 04 §3.10 shows Lindemeier's formulation does, and
`acrylic-blending-findings.md` already flags glazing as distinct maths. The expensive part
is that the composite must be evaluated per *distinct mapped colour* — but there are only as
many of those as there are candidates, so it is a one-time table build, not a per-pixel
spectral evaluation. That makes it genuinely cheap. ~150 lines.

**Invariant: kept (layering).** Explicitly document the change of invariant, and consider
gating the whole feature behind a "layering" checkbox so the strict-mixing claim survives
when it is off.

---

**8. Divisionist / broken-colour mode: coarse two-candidate halftone at mark scale.**
*Payoff: medium — high on the right image, actively bad on the wrong one. Cost: medium.*

**What it does.** Implements §1.5 and reaches colours the mapping cannot (§3.7b). Do the
gamut-volume measurement in §3.7 **first**; if partitive averaging does not measurably
enlarge the reachable set for realistic palettes, skip this entirely.

**Where it slots.** Replaces `MapPixelsFlat`'s single-nearest choice with: for each target,
find the best (candidate A, candidate B, ratio) whose linear-light average matches; then
fill mark-sized cells with A or B according to a clustered-dot screen. Category B.

**Parameters.** Cell size in px (tie to mark size, 4–16); screen angle; pair-search
neighbourhood k (≈32); a "purity" weight trading off pair-average accuracy against
preferring a single paint.

**Effort.** Medium. The pair search is the interesting part; restrict it to the k nearest
candidates by CIELAB and evaluate their pairwise linear-light averages — `O(k²)` per
distinct quantized colour, cached exactly as `MapPixelsFlat` already caches.

**Invariant: kept** — each pixel is one candidate. But note the mode changes what the output
*means*: it is now an instruction to place two paints side by side, not one mixed paint.

---

**9. FDoG contour lines filled with the darkest achievable candidate.**
*Payoff: medium, and stylistically narrow. Cost: medium.*

**What it does.** The outlined look of §3.6, done legally: detect coherent lines with
flow-based DoG, then paint them as hard-edged marks of a single dark candidate colour rather
than multiplying the image by an edge mask.

**Where it slots.** After mapping, using the structure tensor from lever 5. Category B if
the line colour is a candidate and the lines are aliased.

**Parameters.** σ_e ≈ 1.0 (edge thickness — the dominant knob); τ = 0.98–0.99; φ_e =
0.75–5.0; σ_m ≈ 3.0 (along-flow smoothing); line width tied to mark size.

**Effort.** Medium — ~200 lines given the structure tensor. Skip the separate ETF (§3.4) on
the first pass and use the smoothed tensor's flow direction.

**Invariant: kept**, provided the multiply in Winnemöller step 6 is replaced with a
wholesale colour substitution.

---

**10. Superpixel region fill.**
*Payoff: medium. Cost: medium. Largely superseded by levers 2 and 4.*

**What it does.** Explicit flat regions of brush-mark size with boundaries that follow image
edges (§3.3). Report 02 §5.4 already specifies the full pipeline and the C# options.

**Where it slots.** Pre-map: SLIC on the photo → average each region in CIELAB → map the
region means. Category A.

**Parameters.** k or step size S (the mark size); compactness m; min region area; RAG merge
threshold in ΔE.

**Effort.** Medium (~250 lines for SLIC + connectivity relabel + region means), and it
overlaps heavily with what anisotropic Kuwahara (lever 2) plus modal cleanup (lever 4)
achieve for less code. **Build it only if the paint-by-numbers / region-guide output from
report 02 §5.4 is also wanted**, in which case the region structure is needed anyway and
this lever becomes nearly free.

**Invariant: kept.**

---

**11. Impasto height field and bump lighting.**
*Payoff: high visually, but it is a lie. Cost: high.*

**What it does.** §1.4/§3.9. Looks impressive in a screenshot and undermines the app's
central claim, because the shaded colours are not achievable colours.

**Where it slots.** Last, outside the invariant, behind an explicit "surface preview"
toggle. Requires lever 6 (strokes) to have any height field to build from.

**Invariant: BROKEN**, unrepairably in spirit even if a re-map is applied. Recommend
deferring indefinitely, or shipping it clearly labelled as a presentation effect that does
not describe the paint.

---

### If only three things get built

Levers **1** (saliency-driven variable blur), **2** (anisotropic Kuwahara), and **3** (toned
ground). Together they cost perhaps 350 lines, touch nothing outside the pre-map stage and
one post-map colour substitution, keep the invariant absolutely intact, and address the
three most conspicuous ways the current output announces that it was computed rather than
applied: uniform sharpness, isotropic smoothing, and the absence of any unifying colour.
