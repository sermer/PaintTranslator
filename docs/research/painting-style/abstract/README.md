# Research: Abstract

Research into abstract art aimed at one question: **what should the "Abstract" style actually
do?** The style as shipped is Post-Impressionism with bigger numbers — flattening strength 5,
contrast 1.5, chroma 1.5, plus a light mother colour — and a review had already flagged that it
may not earn its name.

Four parallel tracks, written by separate agents that did not see each other's work. This README
is the synthesis. The reports are long; read this first and go to them for detail.

> **Two corrections from the Fauvism round, 2026-07-28. Read before acting on the build order.**
>
> 1. **Correction 1 below — the per-hue chroma ceiling table — is wrong.** It reads masstone
>    figures off `pigments.manifest.txt`, and **masstone is not the chroma ceiling**: 13 of 18
>    chromatic selectable paints reach higher C\* in a white tint. Phthalo Green (Y.S.) is
>    **56.3 at L\* 75.6**, not 31.9 at L\* 18.9. Over the real candidate set there is **no empty
>    hue sector and none below C\* 35**. The related claim that **K-M mixing always lands below
>    both parents is also false** — 6.49% of sampled pair mixtures exceed both, which undercuts
>    the reasoning behind build item 6 (masstone-only mode) without killing the item itself.
>    Build item 1 stands; **build it from the candidate set, not the manifest.**
> 2. **"Raising strength has never helped: the floor is not the problem" is true only of the
>    golden gradient.** Strength 1→5 moves Fauvism 21.1% → 18.8% on the synthetic gradient but
>    **61.2% → 37.6% on a real photograph** — the gradient is smooth by construction, so the
>    guided filter has nothing to remove. **Any conclusion about floor strength drawn only from
>    `Tests/Golden` is unsafe**, including the one stated below.
>
> Full detail in [../fauvism/README.md](../fauvism/README.md). Note that report's own verification
> debt: its chroma probes transcribed `ScaleChroma` rather than calling it.

| Report | Covers |
|---|---|
| [01-what-defines-abstraction.md](01-what-defines-abstraction.md) | Definitions, the abstraction spectrum, the perceptual literature on what makes a viewer read an image as abstract, degrees of abstraction as a parameter. |
| [02-shape-and-composition.md](02-shape-and-composition.md) | Shape vocabulary, composition, region counts, and the segmentation / simplification / tessellation algorithms that derive shapes from a photo. |
| [03-grounds-and-background.md](03-grounds-and-background.md) | Ground vs field vs negative space, grounds as physical practice, figure-ground assignment, how to give a converted photo a ground. |
| [04-colour-and-palette.md](04-colour-and-palette.md) | Palette by movement, colour counts, whether abstract art is actually more saturated, per-hue achievable chroma. |

## The headline: the style operates on the wrong axis

**Three of the four tracks independently concluded that abstraction is a spatial property and
the shipped style is a colour operation.** The convergence is the strongest result in the set,
because the three arrived from unrelated directions:

- Track 1, from image statistics — Graham & Field 2008 found abstract works differ in
  amplitude-spectrum slope (−1.13 vs −1.25/−1.26, p < 0.03) while **mean, variance, skew and
  kurtosis did *not* differ by content**. Redies 2017 adds an edge-orientation-entropy collapse
  (3.945 vs 4.380). Contrast and chroma are the two knobs Abstract turns hardest and neither
  appears in any measured signature. `[verified]`
- Track 2, from the shape vocabularies — every geometric abstraction surveyed is a set of flat
  regions with *explicit* boundaries, and nothing in the pipeline computes a region. `[verified]`
- Track 4, from the aesthetics literature — over 150 abstract artworks, HSV saturation
  correlated **negatively** with beauty ratings (ρ = −0.217, p < 0.01; whole model R² 0.134).
  The only positive chromatic predictor located anywhere is the **standard deviation** of
  saturation (β = 0.404), not its mean. `[verified]`

That last one is worth stating plainly: **"abstract art is more saturated" appears never to have
been tested at scale, and the one study that bears on it points the other way.** The chroma 1.5
in `StyleRegistry` rests on a folk belief.

Track 1 adds the formal version. `ILabRemap` and `ICandidateTransform` are pointwise colour
functions, so they cannot destroy representation even in principle — scene category survives at
32×32 (80.8%) and 8×8 (65.1%), and line drawings are identified about as fast as colour
photographs, so colour is close to irrelevant to recognition. **Only slots 1 and 5 can produce
abstraction.** `[verified]`

## The second convergence: restriction, not expansion

Track 4 checked every abstract movement whose materials were documented and found each one
*restricts* something. De Stijl restricts hue to three primaries plus three achromatics — and in
this library all five working colours are **unmixed masstones** spanning L\* 98 / 85 / 50 / 27 /
11, so the value structure comes free. Albers restricts mixing to zero: tube plus palette knife,
with the tube number recorded on the verso. Vasarely restricts to a closed alphabet. The one
movement that *expands* — Abstract Expressionism, 15 paints in Pollock's *Alchemy* — has a
**narrower** realised colour distribution than any historical period (box-counting 2.35 vs
2.6–2.8). `[verified]`

Colour count converges from three independent directions: ~21 (SD 5) perceptually named colours
per painting; k = 5, range 3–7, for palette-based photo editing; 3–4 for an Albers *Homage*.
Track 4's recommended default is **N = 8**. `[relayed]`

**Tracks 1 and 4 both picked candidate-set thinning as their first recommendation, independently.**
Track 1 arrived there by measuring the committed golden renders; track 4 by surveying what
painters restrict.

## What the measurement actually says about the current style

Track 1 measured the five committed golden renders, and the earlier review's summary — "Abstract
is the least flat of the five" — turns out to be half right. The defect is **bimodal**:
`[verified]`

| | Abstract | Realism |
|---|---|---|
| Largest single region | **2009 px** (highest of five) | — |
| Top-5 region share | **30.2%** (highest of five) | — |
| Distinct colours | **322** | 161 |
| Regions | **685** | 425 |
| Colours to cover 90% of pixels | **159** | 88 |

The guided-filter floor is working — it builds the plateaus. `ToneAndChromaRemap` at contrast 1.5
and chroma 1.5 then sprays the transitions *between* those plateaus with extra mixtures. **The
fix is fewer available colours, not more smoothing.**

This also explains why raising `strength` further has never helped: the floor is not the problem.

## Corrections to the parent research

Four things in `docs/research/painting-style/README.md` and in the briefs need amending. Three
were found by agents; the fourth I verified locally.

**1. The chroma ceiling is worse than the parent README states, and it is per-hue.** The parent
says "the best green is Permanent Green Light at 56.0". That paint is `ReflectanceDerived` and is
filtered out of `PigmentLibrary.Selectable`, so a user can never choose it. Recomputed from
`pigments.manifest.txt` over the **19 selectable** paints: `[verified — computed locally
2026-07-28]`

| Hue sector | Best selectable masstone |
|---|---|
| Yellow ~89° | Hansa Yellow Opaque, C\* **106.4** |
| Orange ~49° | Pyrrole Orange, C\* **99.9** |
| Red ~37° | Pyrrole Red, C\* **84.7** |
| Blue ~297° | Cobalt Blue, C\* **70.7** |
| Green ~177° | Phthalo Green (Y.S.), C\* **31.9** at L\* 18.9 |
| 120–150° | **nothing** but Titanium White (C\* 0.75) |
| 180–210° | **empty** |
| 330–360° | **empty** across all 80 paints |

So a scalar `AchievableMaxChroma` makes "chroma × 1.5" mean a ceiling of 106 in yellow and 32 in
green. In practice the current setting means *make the yellows and oranges louder*. Track 4's
~36-bin per-hue lookup (~15 lines) fixes this, and it is a live defect in **Fauvism and
Post-Impressionism too**, not only Abstract.

**2. The colour cache is not the obstacle the briefs assumed.** Tracks 2 and 3 agree, from
different ends of the pipeline. Pre-map stages rewrite pixel values before `MapPixelsFlat` runs,
so region fill makes the cache strictly **more** effective by collapsing millions of colours into
a few thousand region means. Slot 5 runs after `ResolveOncePerColour` has already finished. The
position-dependence cost is real only in **slot 4**, where `IQuantiser.IsPositionDependent`
forces `ResolvePerPixel` — roughly **80×** more nearest-neighbour searches on a 12 MP image.
**Design rule: never put a positional operation in slot 4.** `[verified against the code]`

**3. Anti-aliasing is nearly free to skip.** A 1-px staircase is 3% of a 33-px mark, so rendering
every shape boundary aliased keeps the work in the safe post-map category and the arithmetic
problem never arises — except at the mark slider's floor, which is not the geometric setting
anyway. `[inferred, from the mark-size arithmetic]`

**4. Anisotropic Kuwahara is demoted.** It was the parent README's pick for the edge-preserving
filter. Track 2's negative result: it produces **no region representation**, so you cannot count,
area-constrain, contour-trace or orientation-snap its output. It is the right pre-filter *feeding*
a segmenter, not a substitute for one. `[inferred, from the algorithm's formulation]`

## "Abstract" is probably not one style

Tracks 1 and 2 reached this separately and it is the finding most likely to change the product,
so it is stated on its own.

- Redies' 572-work abstract subset has edge-orientation entropy 3.945 ± **0.722** — an SD **3.4×**
  that of Western oils. `[verified]`
- Graham & Field put abstract content at spectral slope −1.13, *shallower* than landscape's
  −1.26 — but four Mondrians sit at −1.4 ± 0.06, **steeper than any representational class**.
  Geometric and gestural abstraction fall on opposite sides of representational art. `[verified]`
- Hayn-Leichsenring et al. 2020 replicated that global image properties do not predict preference
  for abstract art — only semantic descriptors did — and closed with the recommendation that
  treating abstract paintings as a single category is not useful in empirical aesthetics.
  `[verified]`
- Vessel & Rubin 2010: between-observer preference agreement r = **0.20** for abstract work
  against **0.46** for real-world images. There is no shared default to tune toward. `[verified]`

**A single Abstract parameter row aims at an empty part of the distribution.** Either split it —
"Geometric" and "Gestural" as separate rows, which the architecture already supports at zero
cost — or commit the one row to the geometric branch and say so. Track 2 recommends the latter if
it stays one row. This is a product decision, not a research one.

## The one piece of shared infrastructure

**Three of the four tracks need connected-component labelling on the mapped index buffer**, and
none of them mentioned the others:

- Track 1's small-region merge — flood-fill, rewrite every region below mark² to its largest
  neighbour.
- Track 2's area opening at `MarkPixels²` — the same operation on a segmentation label map.
- Track 3's `GroundFill` — the mask is "any region larger than k·markPixels² with low interior
  gradient", built from connected components, deliberately **not** from saliency.

`PaintabilityMetrics.ForEachRegion` is already that flood fill. Build the shared labelling once
and three separate recommendations become cheap.

Note what this does to the mark invariant. Track 2's finding: `MarkPixels` currently reaches
exactly one consumer, `FloorRadius = m/2`, a filter window. Region count is then decided by
wherever the palette map's Voronoi boundaries happen to land, and only *measured* retrospectively
by `PaintabilityMetrics`. **The mark size is a hope, not a guarantee.** Any of the three
region-aware stages above turns it into one.

## The ground problem, restated

Track 3's reframing is the most useful thing in that report. "Background" conflates three
distinct things, and only separating them makes the problem tractable:

| | What it is | Status in the app |
|---|---|---|
| **Ground** | A physical layer under the whole canvas. No position. Unifies multiplicatively. | **Already built** — `MotherColourTransform`/`BlendInto` is exactly this mechanism. |
| **Field** | A region of paint a viewer reads as background. Positional. | **Missing entirely.** This is the actual gap. |
| **Negative space** | Unworked area. | Not expressible, and track 3 argues it shouldn't be. |

On that last point: **stop trying to express "unpainted."** The ground *is* paint. "Ground only
here" is the faithful representation, and it makes physical execution easier — tone the whole
canvas, then paint figures over it.

Two supporting results. Figure-ground assignment from geometry is weak (best single cue 67.8%,
convexity 60.1%, Fowlkes et al.) but **colour is strong and directional**: red on dark green
(Weber contrast 16.9) beat blue on dark blue (Weber 64.38) for figure status, F(1,79) = 33.92,
p < .001. So the field should be the **cooler, less chromatic** party. `[verified]`

And the chroma ceilings decide which famous grounds are buildable, asymmetrically. Newman's
cadmium-red field is a single tube (C\*ab 89.2) and reachable. Klein's IKB needs C\* 76.3 against
a best selectable blue of 70.7 — unreachable, and worse once mixed. Meanwhile every earth colour
(burnt sienna, raw and burnt umber, ochre) is `ReflectanceDerived` and withheld from the picker,
so a classic warm ground **must be mixed** — but easily can be, since all sit at C\* 4.7–43 and
mid-to-dark value. `[verified]`

## Suggested build order

Nothing here is decided. This is the order the evidence supports, cheapest and best-supported
first. Slot numbers refer to the five-slot pipeline.

| # | Item | Slot | Cost | Why here |
|---|---|---|---|---|
| 1 | **Per-hue chroma ceiling** — replace the scalar `AchievableMaxChroma` with a ~36-bin hue lookup built from the candidate set | 2 | ~15 lines | Cheapest item on the list, fixes a live defect in three styles, and unblocks any honest reasoning about chroma. |
| 2 | **Candidate-set thinning to N colours** — area-weighted k-means in Lab with L\* weighted 1.5, each centroid snapped to the nearest achievable candidate, L\* extremes force-included | 3 | ~80 lines | Both tracks 1 and 4 picked it first. Gamut-safe by construction, zero per-pixel cost, makes conversion *faster*, enlarges regions, and directly attacks the measured 322-colour defect. |
| 3 | **Connected-component labelling on the index buffer** | — | shared | Three tracks need it. `PaintabilityMetrics.ForEachRegion` is most of it already. |
| 4 | **Small-region merge** — rewrite every region below mark² to its largest neighbour | 5 | ~100 lines | Turns the mark invariant from a hope into a guarantee. Invariant-safe by the `Refine` signature. |
| 5 | **`GroundFill`** — one candidate index written into a geometric mask; ground colour derived as L\* → ~58, C\* = min(median C\* × 0.35, 25), hue = chroma-weighted image mean, then snapped to `CandidateSet` | 5 | ~120–150 lines | The field is the real gap. Strictly *increases* median region area, so it makes output more paintable. Expose a sign flip for Newman's loud-ground inversion. |
| 6 | **Masstone-only mode** | 3 | ~40 lines + share-aware predicate | The **only** operation in the app that *raises* reachable chroma, since K-M mixing always lands below both parents. Produces the most executable recipe possible. `KeepOnly` currently sees only L\*a\*b\* and needs extending. |
| 7 | **Mother colour from a *chosen* paint** | 3 | ~40 lines | `MostNeutralPaintIndex()` forbids the warm earth ground that seven of nine surveyed practitioner recommendations use. |
| 8 | **Felzenszwalb segmentation → Lab region-mean fill → area opening** | 1 | ~320 lines | The real shape answer. Its `min` parameter *is* the mark invariant — bind it to `MarkPixels²`. Author's published params: sigma 0.5, K 500–1000, min 50–100. Stacks after `EdgePreservingFloor`, shaped like it. |
| 9 | **Contour trace + Douglas–Peucker + aliased polygon fill** | 1 | ~250 lines | What makes regions read as *planes* rather than as a posterised photo. At ε ≈ `MarkPixels/2` the boundary deviates by under one mark. Chaikin instead of DP is a ~20-line variant giving the biomorphic setting. |
| 10 | **Orientation snapping, k ∈ {2,4}** | 1 | ~80 lines | A checkbox on #9. The honest Mondrian-adjacent move — real photo-derived regions snapped to the axes, not a Mondrian generator. |

**Cheaper substitute for 8–10 if the segmentation work is too much:** variance-priority BSP
(~150 lines) gives rectilinear planes with a *direct integer region-count control* and no contour
machinery, at the cost of delivering exactly one look. Tracks 1 and 2 both landed on this
independently, track 1 noting van Doesburg's documented *Composition VIII (The Cow)* sequence as
a direct warrant.

**On chroma specifically:** the evidence says lower the mean and widen the spread. Item 2 does
the second for free — fewer, better-separated colours *is* a higher saturation SD. Consider
dropping Abstract's chroma multiplier toward 1.0 at the same time and re-measuring.

## What not to build

Each of these sounds compelling and does not survive the evidence. The parent README's list still
applies; these are additional.

- **Neural style transfer.** Four independent sufficient reasons, but the non-obvious one is
  decisive: the Gram matrix averages over spatial position and is therefore **blind to global
  arrangement**, which is precisely what abstraction is. Wrong tool for this specific goal,
  regardless of the runtime and gamut objections. `[inferred, from the method's formulation]`
- **Colour Field as a style.** The parent research already rejected it; track 1 adds a structural
  reason — its stained soft edges are post-map *arithmetic*, so the invariant repair would
  destroy the style's defining feature.
- **Mondrian generators.** Feijs tested the recursive-splitting hypothesis against 147 paintings
  and found it "in general not true." `[relayed — the arXiv PDF would not decode]`
- **Any composition scorer**, including Dynamic Composition Model. R² 0.675, but on dot patterns;
  no real artworks tested.
- **Biomorphic shape synthesis.** No quantitative characterisation of biomorphic form exists to
  target.
- **Fractal-dimension targeting.** Same failure as the parent README's Pollock-authentication
  cautionary case.
- **Low-poly / Delaunay tessellation.** A visual cliché with no dominance structure.
- **SLIC for shape.** Equal-area cells erase exactly the size variation that carries dominance.
- **All-over composition as a goal.** It deletes report 03's best lever (edge and mark hierarchy).
- **Saliency-driven ground detection.** Track 3 prefers geometry on the already-mapped index
  buffer — consistent with the parent README's finding that centre bias outperforms image
  salience on paintings.
- **Kandinsky's colour theory as a design source.** His stated hue-shape associations fail an
  IAT (max D 0.12) and contradict his own practice, which runs to ten pigments per hue.
  `[verified]`
- **Complementary ground colour.** Schloss & Palmer already contradict complements; track 3's
  derived ground is desaturated *same*-hue.

**Deferred rather than rejected:** Fogleman's `primitive` is real, MIT-licensed, and the
highest-ceiling item in the set — 50–200 shapes is an empirically tuned answer to "how few
regions still carry a photograph." But it is a parallel renderer, not a five-slot stage, and
single-threaded C# runtime is the open risk. Worth a spike, not a plan.

## Accuracy warnings

Read these before quoting any figure.

- **The parent README's green chroma figure is wrong for any user-facing purpose.** See
  correction 1 above. Permanent Green Light at C\* 56.0 is not selectable.
- **The Albers 1:2:3 ratio and the "seven rectangles" count in report 02 are both `[relayed]`**
  and flagged by their author as unsafe to quote.
- **No measured CIELAB exists anywhere for Mondrian's actual painted planes.** Conservation
  studies give pigments, not colorimetry. Any Lab value for a Mondrian is from a reproduction.
- **Rothko reproductions are colorimetrically wrong by an unknown amount.** The lithol red has
  degraded. Track 3 flags this against its own best-documented ground.
- **Report 01's tiny-image recognition figures** (32×32 at 80.8%, 8×8 at 65.1%) come from PDFs
  that downloaded but could not be text-extracted in the agent environment.

## Verification debt

Ranked by how much each would change a decision.

1. **Feijs, arXiv 2011.00843** — the PDF would not decode. The only located source that puts real
   line and rectangle counts on canonical Mondrians, and it carries the Mondrian-generator
   rejection.
2. **The Rothko k-means / ΔE paper** — 403 on both ScienceDirect and SSRN. The one source that
   would put real Lab numbers on abstract fields; both tracks 3 and 4 name it.
3. **Biederman & Ju 1988** — paywalled, carries track 1's central recognition argument.
4. **Nascimento 2017, *Vision Research*** — 403; the institutional PDF would not render.
5. **Torralba's tiny-image figures** — PDFs downloaded, no text extraction available.
6. Four further PDFs failed to decode and tandfonline returned 403; each report's own debt list
   has the rest. Report 01 lists twelve items, report 02 lists seven.

The two worth clearing first are Feijs and the Rothko paper, since each is the sole support for a
recommendation above.
