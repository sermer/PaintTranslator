# Research: Fauvism — Brushwork

**Track:** Fauvism, track 2 of 4 — the mark-making half.
**Date:** 2026-07-28
**Scope:** what Fauvist handling concretely is, whether the missing brushwork stage is the
gap in the shipped `Fauvism` style, and which spatial operations could supply it inside the
five-slot pipeline without breaking the gamut invariant.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(the general brushwork report — filters, stroke-based rendering, the four-category invariant
table) and [`../abstract/02-shape-and-composition.md`](../abstract/02-shape-and-composition.md)
(no pipeline stage computes a region; anisotropic Kuwahara is a pre-filter, not a segmenter;
aliasing costs 3% of a mark). Where I extend or contradict either, I say so explicitly in
§7.

**Verification convention** — matching the rest of `docs/research/`:
`[verified]` = read directly from the cited primary source, or measured in this repo ·
`[relayed]` = reported by a secondary source I did not confirm at the primary ·
`[inferred]` = my reasoning from the above, stated nowhere.

---

## 0. Headline

**The missing brushwork stage is real, but the highest-value Fauvist mark is not a brushstroke
— it is a drawn contour, and a contour is a post-map selection that the `IPostMapStage.Refine`
signature already makes invariant-safe for free.**

Four things support that, in descending strength:

1. **Fauvist handling is heterogeneous within a single canvas, and the flat-plus-outline half
   is the cheaper half.** The National Gallery of Art's own description of Derain's *Charing
   Cross Bridge, London* (1906) reports both devices in one picture: the buildings are
   "outlined with royal blue and filled in with mostly flat areas of color", while the water
   is "short, horizontal, disconnected strokes and dots… against the off white of the canvas
   below." `[relayed]` Outlining is separable from stroke synthesis, and it is roughly a
   quarter of the cost.
2. **There is no measured characterisation of Fauvist stroke geometry.** I searched for
   computational brushstroke analysis, conservation imaging and stroke-extraction work
   targeting Matisse, Derain or Vlaminck and found none. The measured literature is van Gogh
   (Li et al. 2012) and an Impressionist/Pointillist run-length classifier (JOCCH 2024), both
   of which report *relative* statistics on uncalibrated reproductions with no physical scale.
   Any stroke-geometry parameter this app ships would be invented, not derived. §3.
3. **The shipped Fauvism style is the most fragmented of the five, measured.** On the committed
   golden source it produces **1,035 four-connected regions and 331 distinct colours** — more
   of each than any other style, including Abstract — with **30.87% of pixels in regions below
   its own mark²**. Realism on the same source: 425 regions, 161 colours, 5.42%. `[verified —
   measured locally, §4]` A style with no brushwork stage is not merely neutral about marks; at
   `MarkScale 1.3` on the weakest floor strength it is actively emitting sub-mark speckle.
4. **`MarkPixels` still reaches exactly one consumer.** `grep` over the whole repo:
   `context.MarkPixels` is read at `Imaging/Styles/Stages/EdgePreservingFloor.cs:63`, and
   nowhere else outside `RenderContext` itself and the tests. It becomes
   `PalettePhotoConverter.FloorRadius(m) = m/2`, a guided-filter window. `[verified]` The
   abstract track's "mark size is a hope, not a guarantee" holds unchanged. The recommendations
   below are the first two consumers that would make a mark *at* mark scale rather than
   smoothing *below* it.

The three picks are in §8. In short: **contour lines (slot 5, ~130 lines), sub-mark region
merge (slot 5, ~100 lines), directional pre-map flattening (slot 1, ~200 lines)** — in that
order.

---

## 1. What Fauvist brushwork concretely is

### 1.1 Three phases, not one, and they do not share a handling

The movement's own historiography splits the handling cleanly, and every survey source I read
draws the line in the same place — at 1906.

| Phase | Handling | Canonical works |
|---|---|---|
| **1904–05, Divisionist-influenced** | Small discrete touches of unmixed colour laid side by side, not blended on the palette; optical mixture in the eye. Tessellation tightens around contours and opens out in sky and water. | Matisse, *Luxe, calme et volupté* (1904) |
| **1905, Collioure** | Transitional and unstable. Matisse "measured", still Neo-Impressionist in principle; Derain alternating "spare and heavily laden" brushwork within the same canvas, from patches of unmodulated colour to broken marks. | Matisse, *Open Window, Collioure*; Derain, *Mountains at Collioure* |
| **1906–08, broad and flat** | "By 1906, the colour juxtapositions were replaced with areas of flat colour, similar to Gauguin." Outlined planes, arabesque contours, large unmodulated fields. | Matisse, *Le Bonheur de vivre*; Derain's London series; Matisse, *Blue Nude* (1907) |

`[relayed]` — the 1906 break is stated in essentially those words by
[Art History Unstuffed, "Defining Fauvism"](https://arthistoryunstuffed.com/defining-fauvism/):
"the Neo-Impressionist broken brushwork, separated into colors, was apparent" in the early
work, and "by 1906, the color juxtapositions were replaced with areas of flat color, similar
to Gauguin." [Tate](https://www.tate.org.uk/art/art-terms/fauvism) puts the movement's end at
1908 and describes the general handling as "wild loose dabs of paint" with colour "often
applied directly from the tube" `[verified — read from the Tate page]`.

**Target the 1906–08 phase.** Four reasons, in order of weight `[inferred]`:

1. **The 1904–05 phase is Divisionism, and this project has already scoped it separately.**
   The parent README's planned-work table lists "broken colour at **mark scale** — not pixel
   dithering" as the unlock for "Impressionism, Pointillism, Divisionism", blocked on a
   ~30-line gamut measurement. Building it under the `Fauvism` label would make Fauvism
   indistinguishable from the Neo-Impressionism it defined itself against, and would duplicate
   a feature already planned for three other styles.
2. **It is what the word distinguishes.** What a viewer recognises as Fauve — flat saturated
   planes, drawn contours, arbitrary colour — is the post-1906 handling. The 1904 handling
   reads as Signac.
3. **It is the phase the existing four slots already point at.** `EdgePreservingFloor` builds
   plateaus; `ToneAndChromaRemap` at contrast 1.35 pushes toward flatness. Adding outlines and
   sub-mark merging completes a coherent flat-plane style. Adding a stroke field fights the
   floor.
4. **It is the executable one.** A human with a brush can lay a flat plane and draw a contour
   over it. Reproducing a specific 40,000-touch divisionist field from a printout is not a
   task anyone completes.

### 1.2 The three painters are three different problems, and only one generalises

- **Matisse (1905)** — "worked in a very measured style, believing that separate touches of
  interwoven pigment resulted in vibrant, pulsating color in the eye of the beholder"
  `[relayed]` (Met, *Vertigo of Color* material, reached via search snippet — see §9). In
  *Open Window, Collioure* "the paint is applied very loosely; even in reproduction we can see
  almost every brushstroke", with bare canvas visible in the lower right `[relayed]`
  ([Smarthistory](https://smarthistory.org/henri-matisse-open-window-collioure/) — the page
  itself 403'd; text reached through the search index).
- **Derain (1905–06)** — "long, isolated brushstrokes, influenced by Divisionist painting, to
  structure the trees and ground", and in the 1906 London work "pure colours straight from the
  tube to a canvas with a white ground, dabbing it with thick, square dots that give the
  picture surface the appearance of a mosaic" `[verified — read from the
  [Museo Thyssen-Bornemisza entry for *Waterloo Bridge* (1906)](https://www.museothyssen.org/en/collection/artists/derain-andre/waterloo-bridge)]`.
- **Vlaminck** — "pure, intense colour drawn straight from the tube and applied in thick
  daubs", with the stroke shape varying by passage: "rounded in the coloured areas in the
  foreground, longer in the tree and ductile in the buildings" `[relayed]` (Musée d'Orsay entry
  for *Restaurant de la Machine à Bougival*, 1905; the page 403'd, text via search snippet).

**The generalising observation is not any of the three individually — it is that all three vary
their handling by region within one canvas.** Derain's Charing Cross has flat outlined
architecture beside broken-stroke water. Vlaminck's stroke shape changes three times across one
picture. Matisse tightens the mesh around figures and opens it in sky. `[relayed, three
independent sources]`

**That is a direct problem for this pipeline and it is the same gap the abstract track found.**
Region-varying handling requires a region. No stage computes one; `PaintabilityMetrics` only
*measures* regions retrospectively. `[verified]` So any brushwork stage this app ships in the
near term will apply *one uniform handling to the whole frame*, which is the one thing all
three Fauves demonstrably did not do. That argues for the cheapest, most uniform-tolerant
device — a contour, which is uniform by nature — over the most region-dependent one, a stroke
field.

### 1.3 Stroke length, direction, separation — what the sources actually license

Pulling the concrete descriptors together, with their sources `[relayed unless noted]`:

| Property | What the sources say | Numeric? |
|---|---|---|
| Stroke **shape** | "thick, square dots"; "short, horizontal, disconnected strokes and dots"; "long, isolated brushstrokes"; "rounded… longer… ductile" | No |
| Stroke **length** | Varies within a canvas by passage; "long" for structuring trees, "short" for water | No |
| Stroke **direction** | Used to structure form ("to structure the trees and ground"); horizontal in water | No angle, no distribution |
| Stroke **separation** | Explicit — strokes "disconnected", "isolated", forming a "mosaic" with ground between | No spacing figure |
| **Coverage** | Ground visible "in places"; "bare edges of the canvas" (Dufy); bare canvas in one corner (Matisse) | **No fraction anywhere** |
| **Contour** | "outlined with royal blue"; black contour lines "like lead in stained glass"; "serpentine arabesques that define the contours" | No width |

Every one of these is a shape word, not a measurement. That is the finding of §3, previewed
here: the descriptive literature on Fauvist handling is entirely qualitative.

---

## 2. Reserved canvas

### 2.1 It is documented, deliberate, and unmeasured

The device is real and museums name it as intentional rather than as unfinish. The strongest
single statement is the Thyssen's on Derain's *Waterloo Bridge*: vivid colours applied "in a
sort of mosaic of loose brushstrokes that leave the canvas with its pale ground visible in
places **as an innovative pictorial device**" `[verified]`. The NGA's description of *Charing
Cross Bridge, London* is the most concrete: the water is "short, horizontal, disconnected
strokes and dots mostly in bumblebee yellow with some strokes in bubblegum pink and pale burnt
orange, all against **the off white of the canvas below**" `[relayed]`.

Where it appears is consistent across sources: **sky, water, and atmospheric passages — the
open, low-gradient, high-lightness parts of the picture.** Matisse "opens the mesh so that
flecks of canvas show through" in sky and sea while tightening it around figures `[relayed]`;
Cézanne, the immediate precedent, used "intermittent strokes which permit the use of the white
priming of the fabric as part of the design" for distant space `[relayed]`, Art Institute of
Chicago's Cézanne technical publication. It is *not* uniformly distributed noise.

**How much: nobody has published a number.** I searched conservation and technical-study
literature for any measured fraction of reserved support in Fauve or Cézanne oils and found
none — the Art Institute's Cézanne technical publication and the McCrone materials study both
describe reserves qualitatively without quantifying them. `[verified — searched, negative]`

### 2.2 Is it a brushwork property or a colour property? Neither — it is a coverage property

This is the useful reframing. Reserved ground is not a property of the stroke (a stroke that
leaves a gap is the same stroke) and not a property of the colour (the reserve has no colour of
its own; it shows the priming). It is a property of **coverage**: what fraction of the support
receives paint, and where the gaps fall.

That matters because coverage is orthogonal to the two axes the pipeline already has. Slots 2
and 3 are colour; slots 1 and 5 are spatial. Coverage is a *third* thing that happens to be
expressible in slot 5, because "leave this pixel as ground" is a selection: write the ground's
index instead of the mapped one.

### 2.3 What it means under the invariant

**The preceding investigation's conclusion holds and I extend it rather than contradict it.**
Track 3 of the abstract investigation concluded "stop trying to express *unpainted* — the ground
*is* paint", and that a toned ground makes physical execution *easier*. For Fauvism that
resolves cleanly:

- A Fauve reserve is **white or off-white gesso priming**, and Titanium White is in the library
  at L\* 98.25 — the lightest achievable candidate by a wide margin `[relayed, from the parent
  README's figures]`. So the reserve is representable as a candidate index with essentially no
  colorimetric error, which is a better case than the warm-earth ground track 3 had to argue
  for.
- Rendering it as a candidate index means every output pixel remains a mixable colour. The
  invariant never comes under strain. `[inferred]`
- The instruction it issues to a human — "prime the canvas white, then leave these areas
  alone" — is not merely executable but *cheaper* than painting them. That is the rare case
  where the honest representation and the easy execution coincide.

**But I do not recommend building it, and the reason is §1.2.** A reserve mask driven by noise
or by a regular grid will read as speckle, and Fauvism is already the most speckled of the five
styles by measurement (§4). A reserve mask that reads as *reserve* has to be region-aware —
open in the large, flat, light passages and closed around the figures — which is track 3's
`GroundFill` geometry, not a brushwork stage. **Fauvist reserved canvas is best served by
building `GroundFill` (already recommended, already costed at ~120–150 lines) with the ground
colour parameter allowed to reach the lightest candidate, and inverting its "open where the
interior gradient is low" test into "open where the interior gradient is low *and* mapped L\* is
high".** That is a parameter change to somebody else's recommendation, not a new stage.
`[inferred]`

---

## 3. Measured studies — a negative result

**There is no measured characterisation of Fauvist stroke geometry.** I searched for
quantitative analysis, brushstroke statistics, stroke extraction and conservation imaging
targeting Matisse, Derain or Vlaminck. Everything located about Fauve paintings materially is
*pigment* science, not *geometry*: cadmium-yellow photodegradation in *Le Bonheur de vivre*
(Applied Physics A, 2012 and 2015), madder and cochineal lake identification in 1906 works.
`[relayed]` None of it measures a stroke. `[verified — searched, negative]`

The nearest neighbours, and what they would and would not give you:

**Li, Yao, Hendriks & Wang, "Rhythmic Brushstrokes Distinguish van Gogh from His
Contemporaries: Findings via Automated Brushstroke Extraction", *IEEE TPAMI* 34(6):1159–1176,
2012.** The reference work. Automatic extraction by integrating edge detection with
clustering-based segmentation, then per-stroke orientation, length and width; the finding is
that van Gogh's strokes are "strongly rhythmic — regularly shaped brushstrokes are tightly
arranged". `[relayed]` **I could not obtain the full text** (Stanford mirror refused the
connection, IEEE and ACM paywalled), so I have no numeric distributions. Even if obtained, the
figures are van Gogh's, at his scale, on his canvases.

**"Computer-assisted analysis of painting brushstrokes", *EURASIP Journal on Image and Video
Processing* 2014:53.** Same family; the extraction produces "orientation, length, and width"
per stroke under "specific area and shape constraints". `[relayed]` SpringerOpen redirected to
an authorisation endpoint and did not yield the text.

**Georgoulaki, "Classification of Impressionist and Pointillist Paintings Based on Their
Brushstrokes Characteristics", *ACM Journal on Computing and Cultural Heritage*, 2024
(doi:10.1145/3665501).** The closest *stylistic* neighbour, and the most interesting for this
app because its feature is one I can compute locally. It uses **grey-level run-length features**
over 110 Impressionist and 110 Pointillist paintings from WikiArt, and finds that "in
Pointillist artworks short runs (short brushstrokes) of a high gray-level dominate, while in
Impressionist artworks, relatively longer runs of a lower gray-level occur more often", with the
two clusters "almost linearly separable". `[relayed]` **dl.acm.org returned 403**, so I have the
abstract only.

**The methodological point that survives all three, and it is the one that matters here:**
every measured study operates on **uncalibrated web reproductions with no physical scale**.
Run lengths and stroke lengths are reported in pixels of a JPEG, so they are ratios within an
image, never millimetres of paint. There is therefore **no published figure that could be used
to set a `MarkPixels` default or a stroke-length parameter for any style, let alone Fauvism.**
`[inferred, but a direct consequence of the datasets]`

Practical consequence: **any brushwork parameter this app ships is a design choice defended by
looking at the output, not a measurement.** That is an argument for choosing operations whose
parameters are already pinned by something else in the system — the contour width and the merge
threshold are both derivable from `MarkPixels`, which is derived from image geometry — over
operations that introduce free parameters nothing constrains (stroke length, curvature filter,
error threshold, jitter magnitude).

---

## 4. What the shipped Fauvism style actually produces — measured locally

Measured on the five committed golden renders in `Tests/Golden/` (128×128, the six-paint
fixture, source `BuildNoisyGradient(128,128, σ=2.0)`, `MarkPixels = 4` for all five with each
style's own `MarkScale` applied). Four-connected regions on the RGB channels, alpha masked, the
same convention as `PaintabilityMetrics`. `[verified — computed 2026-07-28]`

| Style | mark | mark² | Regions | Median area | Distinct colours | % px below mark² | Mean h-run (px) | Median region elongation |
|---|---|---|---|---|---|---|---|---|
| Realism | 4.0 | 16 | 425 | 3 | 161 | 5.42% | 3.82 | 2.77 |
| Tonalism | 4.8 | 23 | 344 | 6 | 151 | 7.85% | 4.35 | 2.77 |
| **Fauvism** | **5.2** | **27** | **1,035** | **4** | **331** | **30.87%** | **2.33** | **3.23** |
| Post-Impressionism | 6.4 | 41 | 486 | 5 | 205 | 16.86% | 3.30 | 2.81 |
| Abstract | 10.0 | 100 | 685 | 6 | 322 | 54.38% | 2.89 | 2.89 |

Four readings:

1. **Fauvism emits more fragments and more colours than any other style, including Abstract.**
   1,035 regions against Realism's 425; 331 distinct colours against Realism's 161. Abstract's
   higher *percentage* below mark² is an artefact of its far larger mark (100 px² vs 27), not
   of a worse image. On raw fragment count Fauvism is the worst of the five.
2. **The mechanism is documented in the code already, and my measurement confirms it.**
   `EdgePreservingFloor`'s own doc comment names this case: "a style that registers a large
   `MarkScale` without a floor strength to match — Fauvism runs this stage at its own weakest
   declared default". `[verified]` `StyleBehaviourTests.EveryRegisteredStyleIsPaintable` records
   Fauvism's ceiling at **8.5%**, against Tonalism's 0.9% and Post-Impressionism's 1.3% —
   between six and nine times looser, on a different source. `[verified]`
3. **The mean horizontal run is 2.33 px against a nominal mark of 5.2 px.** The image is
   changing colour more than twice per brushmark. Whatever "mark size" means downstream, it is
   not being honoured.
4. **No style produces any directional structure.** Median region elongation is 2.77 / 2.77 /
   3.23 / 2.81 / 2.89 across the five — statistically flat. The variation that exists comes
   from the source (a smooth bilinear field forms iso-colour bands, and vertical runs are
   ~2.3× horizontal for the same reason), not from any stage. **This is the empirical form of
   "no stage makes a mark":** all five styles produce the same region shape, differing only in
   how many regions and how many colours. `[verified — but read the caveat: elongation on this
   synthetic source measures the source's own banding, so the useful claim is the *absence of
   difference between styles*, not the absolute value.]`

**Caveat on all of the above:** the golden source is a synthetic four-corner gradient plus
Gaussian noise, chosen to make colour behaviour legible, not to be a photograph. Absolute
figures will differ on real images. The cross-style comparison at a fixed source is the sound
part.

---

## 5. Algorithms, by slot, with the invariant checked against the real signatures

### 5.1 Contour lines as a post-map stage — invariant-safe by signature, verified

**The reasoning in the brief is correct, and I checked it against the code rather than the
description.** `IPostMapStage.Refine` has this signature
(`Imaging/Styles/PipelineStages.cs:149`) `[verified]`:

```csharp
void Refine(
    int[] indices, int strideInts, int width, int height,
    CandidateSet candidates, in RenderContext context, ParameterValues values);
```

An outline stage needs exactly three things, and all three are present:

- **Boundary detection** — compare `indices[at]` against its four neighbours. Indices only; no
  colour involved.
- **A line colour** — `CandidateSet` exposes `Argb`, `L`, `A`, `B` as index-aligned arrays
  (`Imaging/CandidateSet.cs:93–108`) `[verified]`, so the stage can select "the darkest
  candidate", "the darkest candidate within 30° of hue *h*", or "the most chromatic candidate
  below L\* 40" by scanning those arrays once. It never constructs a colour; it picks an index.
- **A line width** — `context.MarkPixels`, already the product of the user's slider and the
  style's `MarkScale`.

`StylePipeline.Render` then composites via `candidates.Argb[indices[at]]`
(`Imaging/StylePipeline.cs:147`) `[verified]`, so an out-of-range index throws rather than
producing an unmixable colour. **The invariant is structural, not a rule anyone has to
remember** — which is what the interface's own doc comment claims, and it holds for this
operation.

Cost: one pass to mark boundary pixels, one dilation to width, one write. O(n) with a small
constant, `Parallel.For` over rows in the existing style. No structure tensor, no segmentation,
no second mapping pass, no cache impact — slot 5 runs after `ResolveOncePerColour` has already
finished, so the 6-bit colour cache is untouched. `[verified, from the ordering in `Render`]`

**This is the whole reason to prefer it.** It is the only Fauvist device on the list that is
free of every cost the other options carry.

**Two things to get right** `[inferred]`:

- **The line is frequently not black.** Derain's are royal blue; Matisse's *Green Stripe*
  runs green and deep blue through a face. A "darkest candidate" default would be wrong as
  often as right. Make the line colour a hue+lightness selection over `CandidateSet`, defaulting
  to the darkest candidate but exposing a hue parameter.
- **Width must be tied to the mark, not fixed.** A contour narrower than a brush is not
  paintable, and this would be the *second* consumer of `MarkPixels` — the first that produces
  something visible at mark scale.

### 5.2 Sub-mark region merge — slot 5, and the prerequisite for any mark claim

The abstract investigation already recommends this (its build-order item 4, ~100 lines, "turns
the mark invariant from a hope into a guarantee"). I am converging on it independently from the
brushwork side, and I can add the Fauvism-specific evidence: **Fauvism is the measured worst
case** (§4), and Hertzmann's central insight — paint the big shapes first, add small marks only
where the canvas is still wrong — is exactly a statement that sub-brush detail must not be
emitted. A converter that emits 1,035 fragments where 5.2 px is one brushmark has already
failed brush economy before any stroke synthesis is attempted.

Flood-fill the index buffer four-connected, and for every region below `MarkPixels²` rewrite it
to its largest neighbour. `PaintabilityMetrics.ForEachRegion` is already that flood fill with
the right connectivity and the right explicit-stack construction `[verified]`; it is `private`
and reports only areas, so it needs a member-collecting variant, not a new algorithm.

### 5.3 Stroke-based rendering — the honest assessment for Fauvism

Report 03 §3.5.3 covers Hertzmann's algorithm in full and I will not restate it. The
Fauvism-specific verdict is **no, and specifically not as the Fauvism default** `[inferred]`:

- **It targets the wrong phase.** Strokes normal to the intensity gradient, curvature-filtered,
  coarse-to-fine — that is Impressionist by construction, and the paper's own presets are named
  Impressionist / Expressionist / Colorist Wash / Pointillist. None of them is the 1906 flat
  phase. Applied to Fauvism it produces something between van Gogh and Monet.
- **It introduces four to six free parameters that §3 says nothing can calibrate** (stroke
  length min/max, curvature filter, grid factor, error threshold, jitter). Contour width and
  merge threshold both fall out of `MarkPixels`.
- **It is stochastic.** Strokes render in random order; reruns differ unless the RNG is seeded.
  The app's UI is a slider panel where the user compares settings, and a result that changes
  when nothing changed is a usability defect, not a stylistic one.
- **It is 400–600 lines** by report 03's estimate, against ~130 for the contour.
- **The invariant is manageable but not free.** Placed pre-map (slot 1) it is Category A and
  safe, but then every stroke colour is re-quantised afterwards, so two strokes the algorithm
  intended as one colour can snap apart — which *adds* fragments to the style that already has
  the most. Placed post-map it must be hard-edged and opaque, which the abstract track shows
  costs only 3% of a 33-px mark, but then stroke colours are already quantised and the coarse
  layers cannot carry an undertone.

Defer it. If a stroke field is ever wanted, it belongs to the planned Impressionism work, not
to Fauvism.

### 5.4 Directional flattening — slot 1, the only route to an actual mark

If the goal is a *mark* rather than a *plane*, the cheapest honest route is a structure tensor
plus a flow-aligned edge-preserving filter, replacing or following `EdgePreservingFloor`. Report
03 §3.2 and §3.4 specify both; the tensor is ~60–80 lines and the anisotropic Kuwahara sector
accumulation ~120.

**I am extending, not contradicting, the abstract track's demotion of anisotropic Kuwahara.**
That track demoted it *for shape*, on the correct grounds that it emits a continuous field and
no region representation, so you cannot count, area-constrain or contour-trace its output. For
**brushwork** that objection does not apply — brushwork does not need a region, it needs a
directional trace, which is precisely what the filter produces and what §4 shows the pipeline
currently produces none of. The two verdicts are compatible: it is a poor segmenter and a good
brush.

Support for it being *Fauvist*, specifically, is weaker than for the contour: Derain's "long,
isolated brushstrokes… to structure the trees and ground" and Vlaminck's per-passage stroke
shape are directional `[relayed]`, but the 1906–08 phase this report recommends targeting is
the least directional of the three phases. Hence third place.

**Cost note that matters:** slot 1 is position-aware by design and pre-map work makes the
6-bit colour cache *more* effective, not less (the abstract track verified this against the
code, and `StylePipeline` confirms — pre-map stages run before `ResolveOncePerColour` builds
its key histogram, `Imaging/StylePipeline.cs:121–134`) `[verified]`. There is no cache penalty.

### 5.5 What must not go in slot 4

Restating the abstract track's design rule because it bites here: a position-dependent
`IQuantiser` sets `IsPositionDependent`, which forces `ResolvePerPixel` and roughly 80× more
nearest-neighbour searches on 12 MP `[verified against `StylePipeline.ResolveOncePerColour` /
`ResolvePerPixel`]`. Every plausible brushwork operation — outlines, merges, stroke fields,
reserve masks, dither cells — can be expressed in slot 1 or slot 5. **None of them needs slot
4.** The only brushwork-adjacent thing that genuinely wants slot 4 is a positional dither
(pick candidate A or B depending on `(x,y)`), which is the planned broken-colour feature and
should pay that cost knowingly if it is ever built.

---

## 6. Weighing for physical execution

The app's output is an instruction to a person holding a brush. Ranking the options by what
they ask of that person `[inferred]`:

| Device | The instruction | Executable? |
|---|---|---|
| **Contour lines** | "Draw this line in this colour, one brush width" | Yes — it is the most literal painting instruction in the whole list, and painters draw contours last anyway |
| **Sub-mark merge** | "Do not paint anything smaller than your brush" | Yes — it *removes* work |
| **Reserved ground** | "Prime white, leave these areas alone" | Yes, and it removes work |
| **Directional flattening** | "Your strokes here run this way" | Partly — it changes the picture the painter copies, not an instruction they follow; a person copying a flow-filtered image will get the direction implicitly |
| **Stroke field (SBR)** | "Place these 40,000 strokes" | No — nobody executes a stroke list from a printout |
| **Impasto shading** | "Build height here" | No, and the shaded colours are not achievable colours (already rejected by the parent research) |

This ordering matches the cost ordering exactly, which is unusual and worth taking as
corroboration rather than coincidence: the devices that are cheap to compute are cheap because
they are *selections over regions*, and selections over regions are what a person can carry out.

---

## 7. Where this extends or contradicts the prior reports

**Extends:**

- Report 03 §3.6 argued that the Winnemöller edge overlay's multiply is Category C, and that
  the legal repair — "after mapping, replace edge pixels **wholesale** with a chosen dark
  candidate colour" — "is more faithful to painting" because "a painter's contour is a mark of
  one colour, not a multiplicative darkening". I am promoting that from a repair note on lever
  9 to the **first** recommendation, on the strength of Fauvism-specific evidence report 03 did
  not have (the NGA's description of outlined planes in Derain's 1906 work) plus a code check
  report 03 could not do, because `IPostMapStage` did not exist when it was written.
- Report 03's lever 9 assumed FDoG line detection and therefore a structure tensor, priced at
  ~200 lines *given* the tensor. **For an already-mapped index buffer, no line detector is
  needed at all**: the boundaries are exactly the pixels whose index differs from a neighbour's.
  That removes the tensor from the dependency chain and cuts the cost by more than half.
  `[inferred]`
- The abstract track's "`MarkPixels` reaches exactly one consumer" — re-verified, still true.
- The abstract track's "stop trying to express unpainted" — extended to the Fauve case, where
  it resolves more cleanly than in the abstract case because the reserve is white gesso and
  Titanium White is the palette's lightest candidate.

**Contradicts / qualifies:**

- **The abstract track's demotion of anisotropic Kuwahara does not carry to brushwork.** Its
  reason — no region representation — is a shape objection, not a mark objection. §5.4.
- **Report 03 ranked stroke-based rendering as lever 6 with "payoff: very high".** For Fauvism
  specifically it is not high payoff at any cost, because it renders the phase this style
  should not be targeting. §5.3.
- **Report 03's §3.7 recommendation to measure the dithering gain remains sound but should not
  be charged to Fauvism.** Divisionist broken colour is the 1904–05 phase and belongs to
  Impressionism/Pointillism.

---

## 8. Three recommendations

Priority order. Line counts are C#-from-scratch estimates for this codebase, in the style of
the existing `Imaging/Styles/Stages/` files, excluding UI.

---

### 1. `ContourLines` — a drawn outline over the mapped index buffer

**Slot 5, `IPostMapStage`. ~130 lines.**

**What it does.** Marks every pixel whose candidate index differs from a four-neighbour's,
dilates that boundary set to a width derived from `context.MarkPixels`, and writes one chosen
candidate index into it. Two or three parameters: line width as a fraction of the mark
(0 = off, default ~0.25), line hue (0–360, or "none" meaning darkest available), and optionally
a minimum ΔE across the boundary so only significant transitions get drawn.

**Evidence.**
- NGA on Derain's *Charing Cross Bridge, London* (1906): buildings "outlined with royal blue and
  filled in with mostly flat areas of color". `[relayed]`
- Matisse's *Woman with a Hat* (1905): black contour lines functioning "like lead in stained
  glass" to brace the colour areas. `[relayed, secondary]`
- *Le Bonheur de vivre* (1906): "the serpentine arabesques that define the contours of the
  women are heavily emphasized", with "flat expanses of color and a more linear treatment of
  the figures". `[relayed]`
- Report 03 §3.6's own conclusion that wholesale colour substitution is the faithful form of an
  edge line. `[verified — read from that report]`
- Invariant checked against the real `IPostMapStage.Refine` and `CandidateSet` signatures, not
  against a description. `[verified]`

**Verification.**
- A zero-width parameter leaves the index buffer byte-identical — the convention
  `ZeroRadiusLeavesEveryPixelUntouched` already establishes in this suite.
- On a synthetic two-region image, the drawn band is the expected width and every written value
  equals the single selected index.
- Regenerate `Tests/Golden/Fauvism.png` and look at it. This is the only real test.
- Re-run the §4 measurement. **Prediction, and it should be checked rather than assumed:** the
  outline swallows fragments smaller than the line width entirely, so `% px below mark²` should
  fall substantially from 30.87%. **But `CountRegions` will also collapse** because the line
  itself becomes one large connected region spanning the frame — so the fragmentation metric
  must be computed *excluding the line index*, or it will report a win it did not earn.

**Risk.** Outlining everything reads as illustration or cloisonnism rather than as painting —
report 03 §3.6 flags this and it is right. The ΔE threshold is the mitigation: draw only the
significant transitions, which is also what Matisse and Derain did.

---

### 2. `MergeSubMarkRegions` — area opening at `MarkPixels²` on the index buffer

**Slot 5, `IPostMapStage`. ~100 lines** (less if `PaintabilityMetrics.ForEachRegion` is
generalised to yield members rather than only areas).

**What it does.** Flood-fills the index buffer four-connected; every region below
`MarkPixels²` is rewritten to its largest four-connected neighbour. One parameter: the
threshold as a multiple of mark² (0 = off, default 1.0).

**Evidence.**
- Measured: Fauvism produces 1,035 regions and puts 30.87% of pixels below its own mark² on the
  golden source, both the worst raw figures of the five styles. `[verified, §4]`
- `StyleBehaviourTests` already records Fauvism's paintability ceiling at 8.5%, six to nine
  times looser than Tonalism's and Post-Impressionism's. `[verified]`
- `EdgePreservingFloor`'s own doc comment predicts exactly this failure for a style with a
  large `MarkScale` and a weak floor strength, and names Fauvism. `[verified]`
- Hertzmann 1998's brush economy — small marks only where the big ones are wrong — is the same
  principle stated as an algorithm. `[relayed, via report 03]`
- Converges with the abstract investigation's build-order item 4, reached independently there
  from shape and here from marks.

**Verification.**
- `PaintabilityMetrics.FractionInRegionsSmallerThan(pixels, …, mark²)` must be **exactly zero**
  after the stage runs at threshold 1.0. That is a hard postcondition, not a bound — it is what
  makes the mark a guarantee rather than a hope, and it is the single most valuable assertion
  available anywhere in this feature.
- Region count falls; median region area rises; distinct colour count falls.
- `EveryRegisteredStyleIsPaintable`'s Fauvism ceiling can then be tightened from 0.085 toward
  Realism's 0.030 — and the tightening is the regression test.

**Risk.** Merging erases genuinely thin features (a mast, a wire, a stem). Cap the threshold and
expose it; the abstract track's caveat about protecting thin structures applies unchanged.

---

### 3. `FlowFlatten` — structure tensor + flow-aligned edge-preserving filter

**Slot 1, `IPreMapStage`. ~80 lines tensor + ~120 lines filter ≈ 200 lines.**

**What it does.** Computes the Gaussian-smoothed multi-channel structure tensor on the photo in
linear light, extracts local orientation and anisotropy, and runs an anisotropic Kuwahara (or,
cheaper, a one-dimensional bilateral pass along the flow direction) at a radius tied to
`MarkPixels`. Produces flattening that is *elongated along feature direction*, which is the only
thing on this list that leaves a directional trace.

**Evidence.**
- Measured: median region elongation is indistinguishable across all five styles (2.77–3.23),
  so no stage currently produces any directional structure at all. `[verified, §4, with the
  source caveat noted there]`
- Derain 1905: "long, isolated brushstrokes… to structure the trees and ground". Vlaminck: the
  stroke shape changes by passage. `[relayed]`
- Report 03 §3.2: the anisotropic Kuwahara authors' own claim that it "generates a
  painting-like flattening effect along the local feature directions while preserving shape
  boundaries". `[relayed, via report 03]`
- The tensor is shared infrastructure — report 03's lever 5 — so its ~80 lines are amortised
  across any future flow-based work.

**Verification.**
- Median region elongation on the golden renders must rise measurably for Fauvism while the
  other four styles are unchanged (they do not register the stage). This is a numeric property,
  which is what the repo's conventions ask for over "nothing throws".
- A better test needs a source with real directional content — the golden gradient's banding
  confounds elongation. Add a fixture with an oriented sinusoid or a synthetic ridge field.
- Radius 0 leaves the buffer untouched.

**Risk.** Highest cost and weakest Fauvism-specific support of the three. It is also the one
whose support argues for a phase (1905, directional) that §1.1 recommends *not* targeting. Build
it third or not at all; if the first two land and the output still reads as "posterised photo
with outlines", this is the next lever.

---

## 9. What not to build

Each of these is plausible and does not survive the evidence or the invariant. The parent and
abstract "what not to build" lists still apply; these are additional and Fauvism-specific.

- **Full stroke-based rendering as the Fauvism style.** §5.3. Wrong phase, four to six
  uncalibratable parameters, stochastic output in a slider UI, 400–600 lines, and pre-map
  placement adds fragments to the style that already has the most.
- **Pointillist / divisionist dithering under the Fauvism label.** It is the 1904–05 phase, it
  is what Fauvism defined itself against by 1906, and it is already scoped as the shared
  broken-colour feature for Impressionism, Pointillism and Divisionism. Building it here would
  make two styles identical.
- **A noise-driven or grid-driven "reserved canvas" mask.** §2.3. Fauvist reserve is
  concentrated in the open, light, low-gradient passages, not scattered. A stochastic mask on
  the most fragmented style of the five adds speckle and calls it a device. The right home is
  track 3's `GroundFill` with a lightness-biased mask.
- **Impasto or any height-field/lighting pass.** Vlaminck's loaded surface is the single most
  characteristic thing about his handling and the single least representable — it is
  view-dependent geometry, and the shaded colours are not achievable colours. Already rejected
  by the parent research; Vlaminck does not reopen it.
- **A position-dependent quantiser for any of this.** §5.5. Everything here fits slot 1 or slot
  5; paying `ResolvePerPixel`'s ~80× cost for a brushwork effect is never necessary.
- **Anti-aliased outlines, or anti-aliased anything, followed by a repair re-map.** The abstract
  track showed a 1-px staircase is 3% of a 33-px mark. Draw aliased and skip the repair pass
  entirely.
- **Deriving stroke-geometry defaults from the van Gogh or Impressionist/Pointillist
  literature.** §3. Those figures are pixels of uncalibrated JPEGs, not millimetres of paint,
  and they are not Fauve. Quoting them as a parameter justification would be laundering.
- **Neural stroke-parameter prediction.** No ML runtime in a `net5.0-windows` WinForms app;
  already covered by report 03 §3.10.

---

## 10. Verification debt

Ranked by how much reaching the source would change a recommendation above.

1. **Georgoulaki, *ACM JOCCH* 2024, doi:10.1145/3665501** — dl.acm.org returned **403**. The
   only located study whose measured feature (grey-level run length) is directly comparable to
   a number I computed locally (Fauvism's 2.33 px mean horizontal run). If it reports run-length
   distributions for real Impressionist and Pointillist canvases, it would give recommendation 3
   the calibration §3 says does not exist, and might supply a target for the mark/run ratio.
   **Highest value of anything unreached.**
2. **The Met's *Vertigo of Color* exhibition catalogue and its "Collioure in Color" article** —
   `metmuseum.org` returned **429** on three attempts. The best modern scholarship on the 1905
   Collioure handling specifically, and the source that would firm up or overturn §1.1's
   recommendation to target 1906–08 rather than 1905. All my Collioure material is search-index
   snippets.
3. **National Gallery of Art object pages for Derain's *Charing Cross Bridge, London* and
   *Mountains at Collioure*** — `nga.gov` returned **403** on every URL form tried. The NGA's
   accessible descriptions are the most concrete stroke-level descriptions I located anywhere
   ("outlined with royal blue"; "against the off white of the canvas below"), and recommendation
   1's headline evidence rests on them, reached only through the search index. Marked
   `[relayed]` throughout for that reason. Confirming them in a browser would move
   recommendation 1's support from relayed to verified.
4. **Li, Yao, Hendriks & Wang, *IEEE TPAMI* 34(6), 2012** — Stanford mirror refused the
   connection; IEEE and ACM paywalled. Would supply the only real stroke-geometry distributions
   in the literature. Lower ranked than it looks because they are van Gogh's, at his scale.
5. **Smarthistory's Fauve pages** (*Open Window, Collioure*; *Fauve Landscapes and City Views*;
   *Bonheur de Vivre*) — all **403**, including the Pressbooks mirror. Would corroborate the
   Matisse bare-canvas observation at a better source than a search snippet.
6. **Musée d'Orsay entry for Vlaminck's *Restaurant de la Machine à Bougival*** — **403**. The
   "rounded / longer / ductile" per-passage stroke observation is the cleanest single piece of
   evidence for §1.2's heterogeneity finding and is currently a snippet.
7. **Any conservation study quantifying reserved support.** Searched across Fauve and Cézanne
   technical literature; nothing found. Recorded as a genuine negative, not as unreached — but
   if such a figure exists it would decide whether a reserve stage is worth building at all.
8. **Hertzmann 1998's own default parameter values** — inherited unchanged from report 03's
   debt list. Only matters if recommendation 3 is ever superseded by real stroke rendering.

---

## Appendix — how §4 was measured

`Tests/Golden/*.png` read to 32bpp ARGB, alpha masked, four-connected flood fill on the RGB
triple; region areas, per-region second-moment eigenvalue ratio (√(λ₁/λ₂), regions of ≥8 px
only), horizontal and vertical run counts, and distinct-colour counts. Mark for each style is
`4.0 × MarkScale`, matching `GoldenStyleTests.MarkPixels = 4` and
`StylePipeline`'s `baseMark * style.MarkScale`. Script kept in the session scratchpad, not
committed — it duplicates `PaintabilityMetrics` deliberately so the measurement does not depend
on the code it is measuring.
