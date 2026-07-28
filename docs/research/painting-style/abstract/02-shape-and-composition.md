# Research: Abstract Shape and Composition

**Track:** abstract art, track 2 of 4 — the *shape* half.
**Date:** 2026-07-28
**Scope:** what shapes abstract painting actually uses, how those shapes are arranged, how
many of them there are, and which classical algorithms can derive them from a photograph.

**Out of scope (covered elsewhere, do not duplicate):** edge softness and edge hierarchy,
mark size as a *filter radius*, stroke-based rendering, dithering, glazing, saliency —
all of [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md). Colour, palette and
chroma — [`../01-colour-theory-in-practice.md`](../01-colour-theory-in-practice.md) and
[`../02-styles-and-movements.md`](../02-styles-and-movements.md). Aesthetic scoring —
[`../04-appeal-and-perception.md`](../04-appeal-and-perception.md), whose rejections I
inherit rather than re-litigate.

**Verification convention** — matching the rest of `docs/research/`:
`[verified]` = read directly from the cited primary source or checked in this repo ·
`[relayed]` = asserted by a secondary source or search summary I could not confirm at the
primary · `[inferred]` = my own reasoning, stated nowhere.

---

## 0. The answer, first

**Abstract painting's shape vocabulary is not one thing, and the quantitative evidence says
so loudly enough that "Abstract" cannot be a single style row.** Geometric abstraction and
gestural abstraction are opposites on every measurable axis the prior reports already
collected: Sigaki et al. put Hard Edge painting at permutation entropy *H* ≈ 0.70 /
complexity *C* ≈ 0.24 and Minimalism at *H* ≈ 0.62 / *C* ≈ 0.20, against Impressionism at
*H* ≈ 0.90 / *C* ≈ 0.05 `[relayed, read off Figure 2 — see ../02-styles-and-movements.md §1.3]`;
Graham & Field measured "abstract" content at amplitude-spectrum slope **−1.13 (SE 0.0614)**,
*shallower* than landscape's −1.26 — yet **four Mondrian compositions came in at −1.4 ± 0.06**,
*steeper* than any representational class in the same paper `[verified, via report 02 §1.2]`.
The category's own dispersion is the finding: Redies' 572-work abstract subset has 1st-order
edge-orientation entropy **3.945 ± 0.722**, where the standard deviation is 3.4× that of the
1,629 Western oils (4.380 ± 0.214) `[verified, via report 02 §1.5]`. Averaging that
distribution produces a target no painting occupies.

**What all the geometric branches do share is one property, and it is the buildable one:
a small number of large regions of flat colour, each bounded by a boundary that is
*explicit* rather than emergent.** Langsner's 1959 coinage is exactly this — "the forms are
finite, flat, rimmed by a hard, clean edge" `[relayed]`. That is a *segmentation* statement,
not a filter statement, and it is the gap in the current pipeline: `EdgePreservingFloor`
runs the guided filter, which by its own doc comment produces `q = a·I + b` — a *smoothed*
image, never a *partitioned* one `[verified, `Imaging/GuidedFilter.cs`]`. No stage in
`StyleRegistry` computes a region. Mark size enters only as a filter radius
(`FloorRadius(markPixels) = markPixels/2`), so the number and shape of output regions is
whatever the palette map happens to produce and is measured after the fact by
`PaintabilityMetrics` `[verified]`.

**So: build a region stage.** My ranked recommendation, argued in §5:

1. **Pre-map region segmentation with a minimum-area constraint** — Statistical Region
   Merging or Felzenszwalb–Huttenlocher, fill each region with its CIELAB mean, then area-open
   the label map at `MarkPixels²`. One parameter, ~250 lines, O(n log n), Category A,
   *improves* the colour cache rather than breaking it, and makes the mark invariant
   constructive instead of hopeful.
2. **Polygonal simplification of those region boundaries** — Douglas–Peucker on traced
   contours, re-rasterised hard-edged. ~250 lines on top of (1). This is what turns a
   posterised photograph into planes.
3. **An orientation-snapping option on (2)** — collapse simplified edges onto the image's
   two dominant orientations, giving a rectilinear or 45°-constructive sub-mode. ~80 lines
   on top of (2).

And one negative result worth stating up front: **anisotropic Kuwahara, the prior research's
pick, does not solve this problem.** It flattens *along* feature direction and leaves no
boundary representation at all. It is the right pre-filter and the wrong shape generator;
for this track it is a complement, not a substitute (§4.6).

---

## Part I — The shape vocabulary

### 1.1 Geometric

| Branch | Formal signature | Quantified |
|---|---|---|
| **Neoplasticism** (Mondrian, 1920–40) | Black horizontal/vertical lines of varying width on white; a subset of the resulting cells filled with red, blue, yellow. No diagonals, no curves. | Feijs analysed **147** paintings of this period with a purpose-built interactive tool `[verified, arXiv abstract]`. *Composition with Red, Blue and Yellow* (1930) is 45 × 45 cm and is usually described as **seven** rectangular regions `[relayed]`. |
| **Suprematism** (Malevich, from 1915) | Floating quadrilaterals and bars, arbitrary rotation, no ground line, no grid; forms do not touch the frame. | No quantitative study located. `[verification debt]` |
| **Homage to the Square** (Albers, 1950–76) | Three or four nested squares, never overlapping, painted outward from the centre; the schema is fixed and only colour varies. | **Over 1,000 works**; sizes 406 × 406 mm to 1.22 × 1.22 m; "the position of the interior squares is determined by a regular schema, with the margin below the square being smaller than the space above it" `[verified, Wikipedia]`. A **1:2:3** ratio between bottom, side and top spacing is widely repeated `[relayed]` but is *not* stated in the Wikipedia article — treat as unconfirmed. Paint applied unmixed from the tube with a palette knife `[verified]`. |
| **Hard-edge painting** (Langsner 1959) | "Abrupt transitions… between color areas"; "color areas often consisting of one unvarying color"; edges frequently taped. Langsner's own terms: "economy of form", "fullness of color", "neatness of surface". | Sigaki et al.: *H* ≈ 0.70, *C* ≈ 0.24 — the low-entropy, high-complexity corner of the 92-style map `[relayed]`. |
| **Kandinsky's grammar** (*Point and Line to Plane*, Bauhaus book 9, 1926) | Not a style but a taxonomy: the point is "the smallest, indivisible… proto-element"; the line is "the track made by the moving point"; the plane is the ground that receives them. `[relayed]` | Kandinsky's own canvases are *not* low-count. *Composition VIII* (1923) carries well over a hundred discrete elements. `[inferred, from reproduction; no count located]` |

The important structural fact across this column: **every one of these is a partition or a
placement of closed regions with explicit boundaries.** None is a texture. That is why
smoothing filters cannot reach them.

Two cautions specific to Mondrian, because he is the obvious target and the obvious trap:

- **Recursive binary splitting is not how the paintings are organised.** Feijs tested the
  "splitting hypothesis" — that a composition is a white rectangle recursively bisected,
  each cut drawn as a black line — against 147 works and concluded "the hypothesis of
  splitting decomposition is in general not true", missing in particular "the
  keeping-distance to the canvas-edge" `[verified, arXiv 2011.00843 abstract]`. Every hobby
  "Mondrian generator" on the web is a splitting generator `[relayed]`; they are producing a
  plausible-looking thing by a method the source material demonstrably does not use.
- **Do not import the golden ratio through this door.** Searches for Mondrian proportion
  analysis surface golden-ratio claims immediately `[relayed]`. Report 04 already rejected
  golden ratio and dynamic symmetry on evidence; nothing here revives them.

### 1.2 Biomorphic and organic

Alfred Barr's 1936 MoMA definition is still the operative one: biomorphism is
"curvilinear rather than rectilinear, decorative rather than structural and romantic rather
than classical in its exaltation of mystical, the spontaneous and the irrational" `[relayed]`.
Formally: closed, smooth, asymmetric, non-convex contours with continuous curvature and no
straight segments — amoeboid rather than polygonal. Arp, Miró, Gorky, O'Keeffe's flower
abstractions, Calder's cutouts.

**I found no quantification of biomorphic shape at all** — no curvature statistics, no
convexity ratios, no size distributions. `[verification debt]` This matters for the
recommendation, because it means a "biomorphic" mode would have no target to hit; §6 rejects
synthesising one.

The one formal claim that *is* firm and useful: biomorphic contours have **no straight
segments and no corners**. Whatever you do to region boundaries, the biomorphic setting is
the one where you *don't* simplify them polygonally — you smooth them. That makes biomorphic
a zero-cost sibling of the polygonal mode (Chaikin corner-cutting instead of
Douglas–Peucker), not a separate feature. `[inferred]`

### 1.3 Gestural and calligraphic

Pollock, Kline, Motherwell, Twombly, Tobey. The formal signature is a *trace of an act*: mark
width varying along its length, velocity-dependent taper, spatter, drips, overlap, and
crucially **no correspondence between mark boundary and depicted-object boundary**.

This is where the category's spread comes from, and it is also where the shape question
stops being answerable from a photograph. A gestural mark's position is determined by the
painter's arm, not by image content. There is no function from a photograph to a Kline
stroke. `[inferred]`

Two guard rails from the existing literature:

- The Pollock fractal-dimension programme (Taylor's *D* rising 1.1 → 1.7 over the years) was
  refuted by Jones-Smith & Mathur in *Nature* 2006, partly by producing a Photoshop scribble
  in minutes that passed the published authentication criteria `[relayed]`. Report 04 already
  lists this as a cautionary case; do not target a fractal dimension.
- Pollock's canvases "lack the range of scales needed to be considered fractal" — the
  smallest marks are only ~1,000× smaller than the canvas `[relayed]`. That is roughly the
  same dynamic range as this app's own mark slider, which is a fair reminder that a
  three-decade scale range is not available in a paint-by-mixture plan either.

Gestural abstraction belongs to track 3's stroke-based rendering (report 03 lever 6), driven
by a procedural direction field rather than by image structure — Cézanne's constructive
stroke rather than Van Gogh's form-following one (report 03 §1.3). It is not a shape
derivation problem and I do not treat it as one.

---

## Part II — Composition and structure

### 2.1 The organising devices, and which of them a converter can touch

| Device | What it is | Derivable from a photo? |
|---|---|---|
| **Grid / rectilinear partition** | The picture plane divided by lines parallel to the frame. | **Yes** — §4.7. It is a partition, and a photo supplies the split evidence. |
| **All-over composition** | "The paint evenly covers the entire painting surface, leaving no empty gaps"; "no focal point, making all parts of equal importance." Coined by Greenberg, 1948, "The Crisis of the Easel Picture" `[relayed]`. | **Yes, trivially — and you should not.** See §6. |
| **Figure–ground ambiguity** | The same region readable as either form or field. | **No.** It is a property of the arrangement, not of the source. A photo has an unambiguous figure–ground by construction. `[inferred]` |
| **Asymmetric balance / dominance and subordination** | Visual weights in equilibrium about a fulcrum without mirror symmetry; one region clearly primary. | **Partly, and only as a report.** §2.2. |
| **Overlap and layering** | Shapes occluding shapes, building depth without perspective. | **No, not from a single image.** Occlusion recovery is amodal completion — a research problem. `[inferred]` |
| **Negative space as an active element** | Ground given the same compositional weight as figure. | **As a choice, yes:** the largest region can be assigned the ground colour rather than its own mean. Cheap, one line, once regions exist. `[inferred]` |

### 2.2 What survives of the rejected geometry

Report 04 killed golden ratio, dynamic symmetry, rule-of-thirds scoring, and automatic
focal-point detection as load-bearing. **Nothing in this track resurrects any of them.** What
survives in their place is narrower and honest:

**Deviation of the Centre of Mass (DCM)** is the one composition metric with real validation
on exactly the stimulus class this track is about — abstract arrangements of flat shapes.
In a four-way comparison (APB, DCM, mirror symmetry, homogeneity) over the 130-image
Wilson & Chatterjee set, DCM predicted perceived *balance* best at **r = −0.822, R² = 0.675**,
ahead of APB (−0.784), homogeneity (0.707) and mirror symmetry (0.418) `[verified]`. For
*preference*, APB led at R² = 0.752 with homogeneity close behind at 0.719, and on a fresh
144-image set the differences vanished `[verified]`.

Three reasons that does not become a feature:

1. **The study explicitly used only circles and hexagons on white — no real artworks**
   `[verified]`. Report 04's 6–15%-of-variance result for real paintings stands; these R²
   values describe dot patterns.
2. **A converter cannot act on it.** Rebalancing means moving content, and moving content in
   a paint-by-mixture plan means lying about the photograph.
3. The authors' own conclusion is that balance and preference are distinct constructs —
   "preference not only depends on balance, but also on homogeneity" `[verified]`.

The most it justifies is a *displayed* number, and even that invites the automated-quality-score
failure report 04 rejected. **Recommendation: compute nothing.**

### 2.3 Complexity preference is bimodal, which settles a design question

Güçlütürk et al. (2016), 30 participants × 144 geometric patterns: the classic inverted-U
between complexity and liking "comes about as the combination of different individual liking
functions" — clustering split the sample into **20 participants (67%) preferring simpler
stimuli and 10 (33%) preferring more complex**, with the cluster model fitting significantly
better than the quadratic `[verified]`.

**Therefore region count must be a user control, not an optimised target.** There is no
population optimum to solve for; two thirds of users want fewer regions and a third want
more. The app already has the right shape of control — a slider — and this is the evidence
that it should stay one. `[inferred]`

Counterweight, for honesty: a 2022 study reported eight statistical image properties
explaining **R²adj 0.50 (pleasing), 0.69 (harmonious), 0.50 (interesting)** for abstract
images — much higher than report 04's 6–15% `[verified]`. But the stimuli were 150
*neural-style-transferred random-phase patterns* built from only 25 style sources, i.e. a
narrow synthetic manifold along which a handful of statistics genuinely do vary
monotonically. It does not overturn the real-painting result and should not be cited as if
it did.

---

## Part III — Scale and region count

### 3.1 What the literature supports

Direct counts are thin. What exists:

- **Mondrian, *Composition with Red, Blue and Yellow* (1930): ~7 regions** `[relayed]`.
- **Albers, *Homage to the Square*: 3 or 4 regions**, over 1,000 works `[verified]`.
- **Malevich, *Black Square*: 1.** (Stated for the bound, not as a target.)
- **Fogleman's `primitive`: "around 50 to 200 shapes are needed to reach a result that is
  recognizable yet artistic and abstract"** `[verified, project README]`. This is the single
  most directly useful number in the report, because it is an *empirical* answer to "how few
  regions can still carry a photograph" from someone who tuned it against thousands of images.
- **Kandinsky, Pollock, Miró: hundreds to thousands.** No counts located.

The honest summary: **geometric abstraction lives at 3–200 regions; gestural and
Kandinsky-style abstraction lives two or three orders of magnitude above that.** The 50–200
band is where "still recognisably the photograph" and "clearly not the photograph" overlap,
and it is the band to design for.

### 3.2 What the app currently reaches — the arithmetic

`RenderContext.DefaultMarkPixels` = `clamp(round(min(w,h)/150), 2, 128)`; the slider runs
1–128; `StylePipeline` multiplies by `StyleDefinition.MarkScale`, which is **2.5** for
Abstract `[all verified in repo]`.

For a 3000 × 2000 photograph:

| Setting | Effective mark | Nominal mark cells over the frame |
|---|---|---|
| Default slider (13) × 2.5 | 33 px | ≈ 91 × 61 ≈ **5,500** |
| Slider 64 × 2.5 | 160 px | ≈ 19 × 13 ≈ **240** |
| Slider 128 × 2.5 | 320 px | ≈ 9 × 6 ≈ **59** |

`[inferred, arithmetic on verified constants]`

**Two conclusions.** First, the slider already spans the whole abstract region-count range —
59 to 5,500 covers Mondrian at one end and Kandinsky at the other. Scale is not the missing
piece. Second, and more important: **nothing in the pipeline makes the output actually have
that many regions.** `MarkPixels` reaches exactly one consumer, `FloorRadius(m) = m/2`, which
becomes a guided-filter window `[verified]`. The guided filter at radius 160 will flatten
enormously, but it emits a continuous field; the region count of the output is then decided
by where the palette map's Voronoi boundaries happen to fall, and is measured only
retrospectively by `PaintabilityMetrics.CountRegions`.

That is the whole argument for this track in one sentence: **the mark size is currently a
hope and a segmentation stage would make it a guarantee.** `[inferred]`

---

## Part IV — Algorithms

Costs are C#-from-scratch line estimates for this codebase, excluding UI, in the style of the
existing `Imaging/` files (`Parallel.For` over rows, `int[]` ARGB buffers, linear light).
"Category" refers to the four-way invariant table in [`../README.md`](../README.md).

### 4.0 Two clarifications that change several rows

**The colour cache is not at risk from pre-map work.** The brief warns that position-dependent
per-pixel operations break the 6-bit-per-channel cache. That warning applies to the *mapping*
stage: if which candidate a colour maps to depends on where the pixel is, the key must carry
position. A pre-map stage merely rewrites pixel values before `MapPixelsFlat` ever runs, so
the cache downstream still sees pure colour. **Region fill makes the cache strictly more
effective**, because it collapses millions of distinct colours into a few thousand region
means. Every recommendation in §5 is cache-positive. `[inferred, from the cache's position in
the pipeline]`

**Anti-aliasing is less of a problem here than it looks, because the output is not a screen
image.** A hard-edged diagonal boundary has a 1-pixel staircase. At an effective mark of 33 px
that staircase is 3% of a mark — far below the resolution of the instruction being issued. At
mark 320 it is 0.3%. So the Category-C trap (anti-aliased edges synthesise unmixable colours)
is avoidable at essentially no visual cost: **render every shape boundary aliased.** The
exception is small marks — at the slider's floor (mark 2–5 px) an aliased diagonal is visibly
ragged, and that is precisely the setting where geometric abstraction is not what the user
asked for anyway. `[inferred]`

### 4.1 Region segmentation

| Method | Produces | Cost | Placement | Mark invariant |
|---|---|---|---|---|
| **Statistical Region Merging** (Nock & Nielsen, PAMI 26(11) 2004) | A label map. Union-find over edges sorted by intensity difference, merging when a statistical predicate holds. Single parameter **Q**, described as "a rough estimate of the number of regions in the image" `[relayed, ImageJ wiki]`. | O(n log n) — actually O(n) with a radix sort on 8-bit differences. **~180 lines** (union-find 40, edge build 30, predicate 30, relabel 40). | **Pre-map, Category A.** | Yes, by area-opening the label map (§4.8). |
| **Felzenszwalb–Huttenlocher** (IJCV 59(2) 2004) | A label map. Same union-find shape; the predicate compares inter-component difference against internal difference plus `k/|C|`. Author's own demo parameters: **sigma 0.5, K 500 or 1000, min 50 or 100** `[verified, author's page]`. `min` is a built-in minimum-component-size pass — the mark invariant, already in the algorithm. | O(m log m) in edges. **~200 lines.** scikit-image's example yields **194 segments** on a standard test image `[verified]`. | **Pre-map, Category A.** | **Yes, natively** via `min`. |
| **SLIC** (Achanta et al.) | Compact, near-uniform superpixels — a *tiling*, not a segmentation. Local k-means in (L\*,a\*,b\*,x,y). | O(n). ~200 lines (report 02's estimate). 196 segments in the skimage example `[verified]`. | Pre-map, Category A. | Yes — S is literally the mark pitch. |
| **Watershed** | Basins from a gradient image; needs markers or it catastrophically over-segments. Compact watershed gave 256 segments in the skimage example `[verified]`. | O(n log n). ~150 lines plus a marker strategy. | Pre-map, Category A. | Only with markers placed on a mark-pitch grid — at which point it is a worse SLIC. |
| **Mean shift** (Comaniciu & Meer) | Modes in joint spatial+range density. `h_s` = mark size, `h_r` = edge threshold, `M` = minimum region area — report 03 §3.3 rightly calls that triple a direct encoding of the painter's controls. | **Expensive**; naive form iterates an `h_s`-radius window per pixel. ~250 lines. | Pre-map, Category A. | Yes, via M. |
| **Graph cuts** (Boykov–Jolly) | A binary or k-way labelling minimising an energy — **requires seeds or a learned model**. | Max-flow, ~500 lines for a competent implementation. | Pre-map, Category A. | n/a |

**Assessment.** SRM and Felzenszwalb are the same algorithmic skeleton — sort edges, union-find,
merge on a predicate — differing only in the predicate, and both are the cheapest thing on this
page per unit of result. Felzenszwalb has the advantage that its `min` parameter is the mark
invariant already written down, and that its author publishes concrete working parameter values.
SRM has the advantage that Q is closer to a region-count dial, which maps onto §2.3's
"make it a slider" conclusion. **Either is a good first build; I would take Felzenszwalb for
the `min` parameter alone.**

SLIC is a genuinely different thing and the difference matters for *this* track: it produces a
tiling of roughly equal cells, so region size carries no information. That is right for
brushwork (report 03 §3.3) and wrong for shape — abstract compositions have region *size
variation* as a primary device (dominance and subordination, §2.1). **Do not use SLIC for the
shape mode.**

Graph cuts is out: it needs seeds it has no way to obtain. Mean shift is out on cost, and its
parameter triple is reproduced by Felzenszwalb's `(sigma, k, min)` at a fraction of the price.

### 4.2 Polygonal simplification of region boundaries

Once you have a label map you have contours, and the contours are where "geometric" comes from.

- **Contour tracing:** Moore-neighbour / Theo Pavlidis boundary following. ~80 lines, O(perimeter).
- **Ramer–Douglas–Peucker:** recursive split on maximum perpendicular deviation from the
  chord; `ε` is the tolerance in pixels. Worst case **O(n²)** (when the split index lands at
  1 or n−2 each level), best case **Ω(n log n)**, and **O(n log n)** achievable with dynamic
  convex hulls `[verified, Wikipedia]`. For contours of a few thousand points the naive form is
  irrelevant to runtime. ~40 lines.
- **`ε` is the style knob.** ε ≈ 1 px is faithful; ε ≈ `MarkPixels/2` gives a polygon whose
  deviation from the true region is below one mark — exactly the tolerance the mark invariant
  already licenses. `[inferred]`
- **Visvalingam–Whyatt** is the alternative (drop the vertex with the smallest triangle area);
  it degrades more gracefully at aggressive settings and gives you a *vertex budget* rather than
  a tolerance, which is a better control if the UI wants "N sides per shape" `[relayed]`.
- **Re-rasterisation:** scanline polygon fill, aliased, one candidate colour per polygon.
  ~100 lines. Category B if the fill colour comes from the already-mapped image; Category A if
  you fill with the region's photo mean and map afterwards. **Prefer the latter** — averaging
  photographic colour is meaningful, averaging mapped colour is not (report 03 §3.3).

**Total for §4.2: ~250 lines on top of a label map.** This is the step that converts
"posterised photograph" into "planes of colour", and it is the highest-value 250 lines in
this report.

### 4.3 Orientation snapping (rectilinear / constructive)

Given a simplified polygon, quantise each edge's direction to the nearest of *k* allowed
orientations and re-intersect consecutive edges to recover vertices. *k* = 2 (axis-aligned)
gives the Neoplastic look; *k* = 4 (adding ±45°) gives a Suprematist/constructive one; *k* = 8
is barely distinguishable from unsnapped.

The technique is mature in a different field: building-footprint regularisation from aerial
imagery does exactly this, with the twist that the dominant orientation is *estimated* per
object rather than fixed to the axes — typically by Hough transform or by the variance of edge
angles `[relayed]`. One method "simplifies… by selecting a subsequence of original edges, with
the vertices of the simplified ring defined by intersections of consecutive edges" `[relayed]`,
which is the exact construction to copy.

**Cost: ~80 lines** given §4.2. Category A. Two failure modes `[inferred]`: consecutive
near-parallel edges intersect at absurd distances and need a fallback; and snapping a
sufficiently curved contour to two orientations produces a staircase, which must be
suppressed by simplifying *first* (large ε) and snapping *second*.

**This is the honest Mondrian-adjacent move.** It does not generate a Mondrian and does not
pretend to — Feijs showed the generative story is wrong anyway (§1.1) — but it produces
rectilinear planes derived from the actual photograph, which is what the app is for.

### 4.4 Primitive fitting (the greedy N-shapes approach)

Fogleman's `primitive`: propose random shapes, score by RMSE against the target, hill-climb by
mutation, keep the best, composite, repeat. Verified details from the README `[verified]`:

- "hill climbing multiple random shapes is nearly as good as annealing and faster";
- shape colour is **computed, not searched** — "optimal color computation based on affected
  pixels for each shape";
- alpha may be fixed or chosen per shape (`-a 0`);
- triangles, rectangles, rotated rectangles, ellipses, rotated ellipses, circles, Béziers,
  polygons, and a combo mode;
- **MIT licensed.**

**Assessment for this codebase.** It is the only method surveyed whose output is genuinely
*compositional* — overlapping shapes at arbitrary scale, dominance and subordination emerging
from the greedy ordering — and it lands squarely in the 50–200 region band the literature
supports. It is also, honestly, a **second renderer rather than a pipeline stage**: it does not
consume the pixel buffer and hand one back, it replaces the whole mapping with a shape list.

Cost: ~400 lines (shape rasterisers, incremental scanline RMSE, mutation, driver). Runtime is
the risk — each of ~200 shapes needs ~1,000 scored raster evaluations, so the constant matters
enormously; Fogleman's Go implementation scores only the changed scanlines. Single-threaded C#
at 2 MP, expect **tens of seconds to minutes**. `[inferred]`

Invariant: **Category A if you fit against the photo and map afterwards**, which is also what
lets you use alpha freely. Category B post-map only at alpha = 255. Mark invariant: satisfied
by construction if minimum shape area is clamped to `MarkPixels²`, and violated freely
otherwise — the algorithm's natural tendency is to spend late shapes on tiny high-error
details.

**Verdict: real, buildable, MIT-clean, and the highest-ceiling item here — but it is a
project, not a stage. Not first.**

### 4.5 Voronoi / Delaunay / low-poly

The standard pipeline is: detect edges → sample seed points densely near edges and sparsely
elsewhere → Delaunay-triangulate (Bowyer–Watson) → fill each triangle with the mean of the
pixels under it `[relayed]`. Variants sample by entropy map, by saliency, or by face detection
`[relayed]`. Cost: ~300 lines (Bowyer–Watson 150, sampling 60, fill 60). Category A pre-map.
Mark invariant: enforceable via a Poisson-disk minimum seed spacing.

Hausner's *Simulating Decorative Mosaics* (SIGGRAPH 2001) is the more interesting relative:
centroidal Voronoi under a **Manhattan metric whose main axis follows a local direction field**,
producing tiles that curve along the picture's features, with edge avoidance achieved by drawing
edges as thick lines so tile centroids are pushed off them `[relayed]`. That is a real
compositional device — tile *courses* that follow form — and it would compose with the
structure tensor report 03 already wants.

**But I rank both low, and for a stylistic reason rather than a technical one.** Uniform
triangulation is an all-over composition with no dominance, no subordination, and no region-size
variation — every failing §2.1 identifies. It also reads unmistakably as a 2013 wallpaper
filter. Hausner's mosaic is better but is a *mosaic*, a different craft with a different
invariant (tiles are objects, not mixtures).

### 4.6 Structure-tensor-driven directional flattening

Anisotropic Kuwahara is report 03's pick and I do not dispute it as a *filter*. For this track
the finding is negative and worth stating precisely: **it produces no region representation.**
Its output is a colour field with directional smearing; the boundaries in that field are
implicit, discovered only later by the palette map's own quantisation. You cannot count its
regions, constrain their areas, simplify their contours, or snap their orientations.

Where it *does* belong in a shape pipeline is as the pre-filter feeding the segmenter: an
edge-preserving flattening pass before Felzenszwalb's own Gaussian (sigma 0.5) would suppress
the texture that otherwise fragments the label map. That is a composition, not a competition.
`[inferred]`

### 4.7 Quadtree and BSP decomposition

**Quadtree:** split a block into four when its variance exceeds a threshold, recurse to a
minimum block size `[relayed, MATLAB `qtdecomp` semantics]`. **~120 lines**, O(n), Category A
pre-map, and the minimum block size *is* the mark invariant, exactly and constructively. The
output is axis-aligned rectangles of power-of-two sizes.

**BSP / recursive splitting driven by colour contrast** is the more image-faithful cousin.
Brandewinder's F# implementation is instructive precisely because it is naive and still works:
"Starting for the initial rectangle, I cut it into 2 rectangles, then pick one of the two
rectangles at random and split it… until I reach a certain depth level", where each split is
chosen by generating "a couple of random splits, measur[ing] the 'average' color on each side…
and pick[ing] the split with highest color distance", on a random pixel sample for speed
`[verified, read the post]`. The author's own summary is the right framing: "fundamentally, the
algorithm I implemented here is a crude clustering algorithm."

Improve it in two ways and it becomes respectable `[inferred]`: (a) split the rectangle with the
highest *total* variance rather than a random one, which is a priority queue and turns depth into
a **direct region-count control** — pop exactly N−1 times for N regions; (b) evaluate all
candidate split positions by a running-sum scan (O(w+h) per rectangle) instead of sampling.

**Cost: ~150 lines. This is the cheapest route to a genuinely rectilinear, region-counted,
mark-constrained abstraction in the entire report,** and unlike §4.3 it needs no contour tracing
or polygon rasteriser. Its failure mode is real and visible: everything is axis-aligned, so a
diagonal horizon becomes a staircase of rectangles. That is a *look* — it is roughly the look of
a Boogie-Woogie — but it is not general.

### 4.8 Morphological cleanup / minimum region size

Vincent's grayscale **area opening** (EURASIP MM Workshop, Barcelona 1993) removes connected
components with area below λ `[relayed]`. On a *label map* the operation is simpler still: run
connected components, and for every region below `λ = MarkPixels²`, reassign it to the
neighbouring label with the closest mean colour. Repeat until stable (2–3 passes).

**~120 lines**, O(n α(n)) with union-find. Category A pre-map, **Category B post-map** (it only
reassigns existing labels, never synthesises a colour). Report 03 lever 4 already proposes the
post-map modal-filter form of this; the label-map form is strictly better where a label map
exists, because it is exact rather than windowed and it cannot round corners off features it
should keep.

**This is the stage that converts the mark invariant from a measurement into a guarantee**, and
it should be attached to whichever segmenter gets built. `PaintabilityMetrics` already has the
four-connected flood fill it needs `[verified]`.

### 4.9 Summary table

| § | Method | Lines | Complexity | Category | Mark invariant | Cache |
|---|---|---|---|---|---|---|
| 4.1 | Felzenszwalb / SRM + region mean fill | ~200 | O(n log n) | A | via `min` / §4.8 | improves |
| 4.2 | Contour trace + Douglas–Peucker + fill | ~250 | O(p log p) | A | inherited | improves |
| 4.3 | Orientation snapping | ~80 | O(p) | A | inherited | improves |
| 4.4 | Greedy primitive fitting | ~400 | ~N·M raster scores | A (pre-map) | via min area | improves |
| 4.5 | Delaunay low-poly | ~300 | O(s log s) | A | via seed spacing | improves |
| 4.6 | Anisotropic Kuwahara | ~180 | O(n·r²) | A | **no** — no regions | improves |
| 4.7 | Variance-priority BSP | ~150 | O(n log N) | A | via min block | improves |
| 4.8 | Label-map area opening | ~120 | O(n α(n)) | A or B | **is** the guarantee | neutral |

---

## Part V — What I would build, ranked by payoff ÷ cost

**1. Felzenszwalb segmentation → CIELAB region-mean fill → label-map area opening at
`MarkPixels²`. ~320 lines. Build this first.**

It is the only item that changes what the output *is* rather than how it looks. Every shape
vocabulary in Part I is a set of flat regions with explicit boundaries, and this is the minimal
stage that produces them. It has three properties nothing else on the list combines: the region
count becomes a control the user turns (§2.3 says it must be); the mark invariant becomes
constructive rather than retrospective (§3.2 says it currently is not); and the colour cache
gets *better*, because a few thousand region means replace millions of distinct pixel colours.
Category A throughout. The author's published parameters (sigma 0.5, K 500–1000, min 50–100)
give a working starting point on day one, and `min` should simply be bound to `MarkPixels²`.

Fit to the existing architecture: it is an `IPreMapStage` with two parameters (`k` as
"Simplification", `min` derived from `RenderContext.MarkPixels`), exactly the shape of
`EdgePreservingFloor`. It stacks after the floor, not instead of it — the floor's job is noise
suppression and it is load-bearing for a different reason.

**2. Contour tracing + Douglas–Peucker + aliased polygon fill. ~250 lines. Build this second.**

Step 1 alone yields a posterised photograph with organic, slightly noisy boundaries — closer to
Post-Impressionism than to abstraction. Step 2 is what makes the regions read as *planes*: at
ε ≈ `MarkPixels/2` the boundary deviates by less than one mark, which the invariant already
licenses, and the visual difference between a traced blob and a 6-sided polygon is the entire
distance between "filter" and "composition". Aliased fill (§4.0) keeps it Category B, or fill
region means pre-map and keep it Category A. Chaikin smoothing instead of Douglas–Peucker is a
~20-line variant that gives the biomorphic setting (§1.2) for almost nothing.

**3. Orientation snapping, k ∈ {2, 4}. ~80 lines. Build this third, as a checkbox on step 2.**

The cheapest large stylistic jump available once step 2 exists, and the only route to
rectilinear structure that stays derived from the photograph.

**4. Variance-priority BSP as an alternative pre-map stage. ~150 lines.**

Ranked below 1–3 despite being cheaper than any of them, because it delivers exactly one look.
But it delivers it with a *direct integer region-count control*, which nothing else does, and it
is the right build if the goal is a distinct "Geometric" style row rather than a general shape
capability. Worth building *instead of* 1–3 only if time is very short.

**5. Greedy primitive fitting. ~400 lines plus a runtime problem.**

The highest ceiling and the only genuinely compositional method. Do not build it until 1–3 exist,
because it is a parallel renderer rather than a stage and it will not compose with the five-slot
pipeline without an architecture conversation. When it is built, fit against the photo and map
afterwards.

**6. Everything else.** Low-poly/Delaunay: cheap, and a cliché with no compositional structure.
Mosaics: a different craft. SLIC for shape: wrong tool, uniform cells destroy the size variation
that carries dominance. Mean shift, watershed, graph cuts: dominated on cost or need inputs that
do not exist.

**Honest labelling of what is a research project rather than a feature:** figure–ground
ambiguity, overlap/occlusion recovery, biomorphic shape synthesis, gestural mark derivation, and
any form of composition rebalancing. All five require either semantics the app does not have or
freedom to move content the app must not take.

---

## Part VI — What not to build

- **A Mondrian generator.** Recursive splitting with primary colours is neither the photograph
  nor Mondrian's method — Feijs tested the splitting hypothesis against 147 paintings and found
  it "in general not true", specifically missing "the keeping-distance to the canvas-edge"
  `[verified]`. What you would ship is a random-rectangle toy wearing a painter's name. Snap
  real photograph-derived regions to the axes instead (§4.3).
- **All-over composition as a goal.** It is well defined — "no focal point, making all parts of
  equal importance" `[relayed]` — trivially achievable, and directly opposed to report 03's
  highest-payoff lever, the spatially varying focal treatment. Building both means building a
  switch whose "on" position deletes the best feature in the app. `[inferred]`
- **Any composition scorer, balance metric, or auto-rebalancer.** DCM's R² = 0.675 is against
  perceived balance on **dot patterns**, in a study that used no real artworks `[verified]`.
  Report 04's rejection of automated quality scoring covers this; §2.2 explains why the
  converter could not act on the number even if it were valid.
- **Golden ratio, dynamic symmetry, rule-of-thirds — including via Mondrian.** Already rejected
  on evidence in report 04. Proportion analyses of Mondrian are the likeliest re-entry point and
  should be treated as such.
- **Fractal-dimension targeting for gestural modes.** The Pollock authentication programme was
  refuted by a Photoshop scribble that passed its criteria `[relayed]`, and Pollock's own work
  spans too few scale decades to be fractal in the box-counting sense `[relayed]`.
- **Biomorphic shape synthesis** — metaballs, superformula curves, blob fitting. There is no
  quantitative characterisation of biomorphic shape in the literature I could find, so there is
  no target, and there is no principled map from photograph content to an Arp contour. The
  defensible biomorphic move is *not simplifying* region boundaries (§1.2), which costs 20 lines.
- **A single "Abstract" target.** The measured spread — edge-orientation entropy SD 0.722 across
  572 works, and Mondrian's −1.4 spectral slope sitting on the opposite side of representational
  art from the abstract category mean of −1.13 `[verified, via report 02]` — means any single
  parameter set is aiming at an empty part of the distribution. If Abstract stays one style row,
  it should commit to the *geometric* branch and say so, because that is the branch a photograph
  can actually be pushed toward.
- **SLIC as the shape segmenter.** It is the right superpixel algorithm and the wrong shape
  algorithm: equal-area cells erase region-size variation, which is how abstract compositions
  express dominance and subordination.

---

## Part VII — Verification debt

- **`arXiv:2011.00843` (Feijs, *Analyzing the Structure of Mondrian's 1920–1940 Compositions*)
  — PDF would not decode** through WebFetch (binary stream returned). The abstract page fetched
  cleanly and is the source for the 147-painting count and the splitting-hypothesis result.
  **The paper's own statistics — line counts, rectangle counts, size distributions — are
  unread.** This is the single most valuable outstanding item in this report; it is the only
  located source that would put real numbers on §3.1. Needs a browser or a text-layer extraction.
- **`sigradi2021_72.pdf` (*Pixel-Based Geometric Decoding of Mondrian Compositions*) — PDF would
  not decode.** Likely contains per-painting geometry.
- **Feijs, *A program for Victory Boogie Woogie*, Journal of Mathematics and the Arts —
  tandfonline returned HTTP 403.** Would give block counts for the densest Mondrian.
- **`JOIG-V12N4-372.pdf` (Delaunay low-poly abstraction) and Felzenszwalb's IJCV PDF — both
  would not decode.** Felzenszwalb's parameter values in §4.1 come from the author's own demo
  page, which fetched cleanly; the **O(n log n) complexity claim is `[inferred]` from the
  algorithm's structure (sort + union-find), not read from the paper.**
- **Wilson & Chatterjee 2005 PDF would not decode.** The APB/DCM figures in §2.2 come from the
  2016 Frontiers comparison, which fetched cleanly and is arguably the better source anyway.
  **The APB formula itself is unread.**
- **The Albers 1:2:3 spacing ratio is `[relayed]` from search summaries and is contradicted by
  omission in the Wikipedia article**, which describes only "a regular schema" with a smaller
  margin below. Do not quote 1:2:3 as fact.
- **The "seven rectangles" count for *Composition with Red, Blue and Yellow* (1930) is
  `[relayed]`** from a search summary; the Wikipedia article explicitly does not give a count.
  Verify against a reproduction before using it as a design target.
- **No quantification of biomorphic shape was found at all** — no curvature, convexity or size
  statistics for Arp, Miró or Gorky. I searched; if it exists, it is not indexed under the terms
  I used. §6's rejection rests on this absence.
- **No element count for Kandinsky's *Composition VIII* or any Suprematist work was located.**
  The "hundreds" figure in §1.1 is `[inferred]` from reproductions.
- **Nock & Nielsen's SRM predicate and Q semantics are `[relayed]`** from the ImageJ wiki, which
  itself notes it omits the details. The primary PAMI paper was not opened. If SRM is chosen over
  Felzenszwalb, read it first.
- **Mean-shift and SLIC parameter semantics are inherited from report 03 §3.3**, which relayed
  them from IPOL and the SLIC paper. Not independently re-checked here.
- **Sigaki et al.'s H/C coordinates for Hard Edge and Minimalism are `[relayed]`** — report 02
  read them off a scatter plot, not a table. The qualitative claim ("simple design elements…
  well-delimited by abrupt transitions of colors") is `[verified]` in that report.
