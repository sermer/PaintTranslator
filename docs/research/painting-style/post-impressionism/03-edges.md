# Research: Post-Impressionism — Edges

**Track:** Post-Impressionism, track 3 of 4 — the edges half.
**Date:** 2026-07-30
**Scope:** what Post-Impressionism's edge treatment should be, and what the five-slot
pipeline should do to produce it. Covers the boundary problem (four incompatible edge
treatments under one label), the cloisonnist key line, Cézanne's contour, edge hierarchy,
and a measurement of the committed golden against the other four styles.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(edge hierarchy, the filter families, the four-category invariant table, lever 1's
spatially varying blur), [`../fauvism/02-brushwork.md`](../fauvism/02-brushwork.md) (the
report that specified `ContourLines`, and the measurement trap), and
[`../abstract/README.md`](../abstract/README.md) (regions, slot rules). Where I correct
either, §7 says so explicitly.

**Verification convention** — matching the rest of `docs/research/`:
`[verified]` = read directly from the cited primary source, or computed in this repo ·
`[relayed]` = reported by a secondary source I did not confirm at the primary ·
`[inferred]` = my reasoning from the above, stated nowhere.

---

## 0. Headline

**Post-Impressionism has the widest gap in the app between the brush it asks for and the
picture it delivers, and the fix is not a line — it is that slot 5 is empty.** Fauvism and
Abstract were each given post-map stages in the last two rounds; Post-Impressionism still has
`Array.Empty<IPostMapStage>()` and the largest `MarkScale` of the three styles that do. It
asks for a brush 1.6× the slider and does nothing at all to produce one. On the
committed golden it is the worst of the five on every edge-density measure outright; on
photographs only Realism — which asks for the *smallest* brush — is more fragmented.

Measured over 14 sources (one HEIC photograph in `Tests/Assets` plus 13 Wikimedia
photographs), at each style's own default mark: `[verified — computed 2026-07-30]`

| | Realism | Tonalism | **Post-Imp.** | Fauvism (line excl.) | Abstract |
|---|---|---|---|---|---|
| MarkScale | 1.0 | 1.2 | **1.6** | 1.3 | 2.5 |
| Four-connected regions | 125,870 | 69,539 | **83,883** | 10,925 | 1,366 |
| Pixels below own mark² | 37.9% | 24.5% | **32.2%** | 7.3% | 0.9% |
| Boundary pairs per 1000 px | 793.7 | 535.0 | **621.0** | 262.4 | 66.5 |
| Pixels adjacent to a colour change | 62.4% | 47.4% | **54.1%** | 32.9% | 9.0% |
| Pixels within ½ mark of a boundary | 83.1% | 74.6% | **82.5%** | 77.7% | 39.7% |
| Mean boundary ΔE | 8.77 | 4.95 | **8.76** | 17.80 | 21.36 |

The last row of that table is the one to read twice. **82.5% of a Post-Impressionist render
is within half a brushmark of a colour change.** The style is almost entirely transition
and almost no plane, and its boundary contrast is statistically indistinguishable from
Realism's (8.76 vs 8.77) — it is a sharper picture than Tonalism at a brush 1.33× wider.

Four further results, in descending order of how much they should change a decision:

1. **`SmallRegionMerge` removes about 40% of the residual per pass, not all of it, and the
   Fauvism round asserted the postcondition holds after one.** Mean sub-mark share over the
   14 sources across a pass ladder: **32.2 → 19.7 → 13.5 → 6.9 → 2.3 → 0.3%** at 0, 1, 2, 4,
   8, 16 passes. Only 9 of 14 reach exactly zero by 8 passes; three are still at 0.9–1.7%
   after 16. On the committed synthetic golden one pass gives 1.11% and two give exactly
   0.00%, which is why the claim survived the Fauvism round — it was checked on the gradient.
   **`FractionInRegionsSmallerThan(mark²) == 0` is false after one pass on every real
   photograph tested**, and the cause is a single stale label map, which is fixable in one
   sweep. `[verified]` §2.3
2. **`ContourLines` draws a band of exactly 4 pixels for every `MarkPixels` from 2 to 12**,
   because `Math.Round(MarkPixels * 0.10)` collapses to 1 over that whole range. Its width
   in marks therefore runs from 2.00 at mark 2 down to 0.33 at mark 12 — a 6× swing, in the
   opposite direction to the intent. Measured by driving the real stage over a synthetic
   two-region buffer. `[verified]` §3.3
3. **The parent round's edge-hierarchy proposal names the wrong knob.** Report 03 lever 1
   is a spatially varying *blur radius*. The floor is now a guided filter, whose radius is
   not its softness parameter — its edge threshold is. Varying the radius across the frame
   moved mid-field boundary contrast by ≈0%; a Gaussian ladder after the floor moved it
   −4%; varying the guided filter's **edge threshold** moved it **−17%** while leaving
   focal-band contrast within 3%. Same architecture, same line count, four times the
   effect. `[verified]` §5
4. **The key line is not black, and the app's line is too light.** Over eight
   Post-Impressionist canvases the thin dark structures have mean C\* 2.0–18.9 and hues
   spanning 52° to 269°; 28–88% of line pixels sit above C\* 12. Van Gogh's redrawn
   contours are **Prussian blue**, identified by MA-XRF at the Van Gogh Museum
   `[verified]`. Only Bernard's *Le Pardon de Pont-Aven* measures near-neutral (C\* 2.0).
   Measured line lightness is L\* 19.6–44.0, median ≈26; `ContourLines` targets Lab
   (35, 5, −15) and lands at **L\* 37.0** on the six-paint fixture — lighter than every
   canvas but one. §3.2

**And the ruling that follows from all of it: Post-Impressionism should not adopt
`ContourLines`.** Fauvism already occupies the flat-plane-plus-drawn-contour territory —
its own phase ruling justifies 1906–08 as "areas of flat colour, **similar to Gauguin**" —
so the cloisonnist device is spent. Rendering the same photograph through shipped Fauvism
and through Post-Impressionism-plus-`ContourLines` produces two pictures a viewer would
not distinguish. The remaining differentiator, and the one the movement's own historiography
supports, is **planes with an edge hierarchy and no key line**. §1, §6.

---

## 1. The boundary problem

### 1.1 The label is a 1910 retrospective invention and its members disagree about edges

Roger Fry coined "Post-Impressionism" in 1910 for the Grafton Galleries exhibition *Manet
and the Post-Impressionists*. Tate confines the term to four figures — Cézanne, Gauguin,
Seurat, Van Gogh — and describes it as "the changes in impressionism from about 1886, the
date of last Impressionist group show in Paris". `[verified — read from
[Tate, "Post-impressionism"](https://www.tate.org.uk/art/art-terms/p/post-impressionism)]`
It was "never a specific movement with clearly defined goals or members"; the term was
applied "after the historical fact". `[relayed — search summaries of
[TheArtStory](https://www.theartstory.org/movement/post-impressionism/) and
[TheCollector](https://www.thecollector.com/manet-and-the-post-impressionists-roger-frys-1910-exhibition/);
the Berkowitz BRANCH article was located but not opened]`

Their edge treatments are not variations on a theme. They are four different answers:

| Figure | Edge treatment | Expressible in the five slots? |
|---|---|---|
| **Gauguin / Bernard / Anquetin** (cloisonnism, 1887–89) | Flat planes bounded by a drawn dark contour | **Yes, exactly** — `ContourLines`, slot 5, already built |
| **Van Gogh** | Directional strokes that follow form, plus contours redrawn in Prussian blue at the end | Contour yes; the stroke field needs 400–600 lines of stroke synthesis |
| **Cézanne** | Passage, doubled and searching contours, edges deliberately unresolved | Geometry yes, optical character **no** — §4 |
| **Seurat** | No contour at all; boundaries emerge from the density of touches | Needs broken colour at mark scale, already scoped to Impressionism/Pointillism |

`[inferred, from the slot signatures in `Imaging/Styles/PipelineStages.cs` and the
descriptions cited in §3–§4]`

### 1.2 The ruling: one row, aimed at planes without a key line

Three arguments, in descending weight.

**(a) Fauvism has already spent the cloisonnist device.** `[verified against
`StyleRegistry.cs:79–90` and the Fauvism README]` The registry has moved on since this
round's brief was written: Fauvism now carries
`new IPostMapStage[] { new SmallRegionMerge(), new ContourLines() }`, and the Fauvism
round's own phase ruling picks 1906–08 on the grounds that "the colour juxtapositions were
replaced with areas of flat color, **similar to Gauguin**". The app's Fauvism row *is* its
cloisonnist row in everything but the label. I rendered `Tests/Assets/sample.heic` through
shipped Fauvism and through a Post-Impressionism variant with `SmallRegionMerge` ×2 plus
`ContourLines` — both built from the real stage classes, not transcriptions — and inspected
both: flat planes, drawn violet-grey contours, the same reading of the same subject, differing
only in key. `[verified — rendered and looked at, 2026-07-30]` Adding the line to
Post-Impressionism would spend a second style row on a device the app already offers.

**(b) The one thing the four figures share is not an edge treatment, it is the plane.**
Cézanne's patch, Gauguin's flat area, Seurat's field of touches and Van Gogh's stroke
cluster are all *regions of held colour* where Impressionism had modulation. The pipeline
can produce regions (slot 5 merge) far more cheaply than it can produce any of the four
mark systems. **Region size is the shared property; contour style is the disagreement.**
`[inferred]`

**(c) The measurement says the defect is region size, not the absence of a line.** §0's
table: 32.2% of pixels below mark², 621 boundary pairs per 1000 px, 82.5% of the canvas in
transition. A contour does not fix any of that; it *hides* it, by painting over the small
regions. Measured: adding `ContourLines` alone to Post-Impressionism moves sub-mark share
from 32.2% to 20.7% — but a mean 23.8% of the canvas becomes line to buy it, and on one
photograph 55.8% does. Two merge passes get to 13.5% and paint nothing. §3.3, §6.
`[verified]`

**Does the evidence support splitting the row?** No, and for a different reason than the
Fauvism round's. The Abstract round split-or-commit question turned on a *distribution*
argument (edge-orientation entropy SD 3.4× that of Western oils). Here the argument is
capability: **three of the four branches are not buildable in the current pipeline**, so a
split would produce one real row and two or three that could only be tuned versions of it.
Revisit if stroke synthesis or broken colour ever ships. `[inferred]`

---

## 2. What the shipped style actually does to edges

### 2.1 The committed golden

`Tests/Golden/*.png`, at `MarkPixels = 4` with each style's own `MarkScale`, measured with
the real `PaintabilityMetrics.CountRegions` and `FractionInRegionsSmallerThan` plus my own
boundary walk. `[verified — computed 2026-07-30]`

| Style | mark | regions | median area | colours | below mark² | bound/1000px | transition px | within ½ mark | ΔE mean | soft <2 | hard ≥10 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Realism | 4.0 | 425 | 3 | 161 | 5.4% | 360.8 | 46.0% | 85.5% | 7.43 | 0.7% | 20.4% |
| Tonalism | 4.8 | 344 | 6 | 151 | 7.9% | 310.7 | 40.5% | 83.3% | 3.88 | 22.4% | 10.1% |
| Fauvism | 5.2 | 186 | 41 | 127 | 2.9% | 295.2 | 40.6% | 91.0% | 14.77 | 0.0% | 30.9% |
| Fauvism (line excluded) | 5.2 | 175 | 41 | 127 | 2.9% | — | — | — | — | — | — |
| **Post-Impressionism** | **6.4** | **486** | **5** | **205** | **16.9%** | **408.3** | **51.9%** | **91.6%** | **7.84** | **0.9%** | **22.1%** |
| Abstract | 10.0 | 8 | 1456 | 8 | 0.0% | 50.1 | 7.5% | 44.0% | 19.06 | 0.0% | 100% |

**Post-Impressionism is the highest of the five on every edge-density measure**: most
regions, most distinct colours, highest boundary length per pixel, largest transition
share. Fauvism's figures are transformed from the ones the Fauvism round recorded (1,035
regions, 30.87% below mark²) because that round's recommendations shipped. **Nothing
shipped for Post-Impressionism, and it inherited the title.** `[verified]`

The Fauvism round's measurement trap is real and I hit it. Masking the contour index by
replacing it with per-pixel sentinels turns every line pixel into its own region and
reports 3,003 regions and 19.68% below mark² — worse than the truth. The correct handling
is to drop line pixels from **both** the region walk and the denominator, which gives 175
regions and 2.93%. `[verified]` Record this: the trap has two sides, and the naive fix
overshoots as badly as no fix undershoots.

### 2.2 On real photographs

The Fauvism round's correction 2 — conclusions about floor strength drawn only from
`Tests/Golden` are unsafe — holds for edges too, and more strongly. On 14 photographic
sources Post-Impressionism goes from 16.9% below mark² to **32.2%**, and its boundary
density from 408 to 621 per 1000 px. The synthetic gradient understates the defect by
roughly a factor of two. `[verified]`

Raising the floor is not the lever. `strength` 3 → 5 moves the golden 16.86% → 16.69% and a
real photograph 18.20% → 14.43% — real but small, and it costs edges everywhere in the
picture to buy it. `[verified]`

### 2.3 `SmallRegionMerge` needs more than one pass — a correction

The Fauvism round called this "the single most valuable assertion available anywhere in
this feature": after an area opening at `MarkPixels²`,
`FractionInRegionsSmallerThan(MarkPixels²)` must be **exactly zero**. It is not, and the
shipped Fauvism and Abstract rows both run exactly one pass.

Mean over the 14 photographic sources, Post-Impressionism's stages otherwise unchanged,
each variant a chain of real `SmallRegionMerge` instances in slot 5: `[verified]`

| Passes | Mean below mark² | Worst source | Sources at exactly 0 |
|---|---|---|---|
| 0 (shipped) | 32.23% | 60.85% | 0 of 14 |
| 1 | 19.74% | 42.83% | 0 |
| 2 | 13.54% | 36.00% | 0 |
| 4 | 6.87% | 25.27% | 0 |
| 8 | 2.28% | 11.02% | 9 |
| 16 | 0.34% | 1.73% | 12 |

Each pass removes roughly 40% of what is left. A fixed point does exist — `sample` settles
at 1,135 regions from pass 8 and does not move at 16 — but reaching it costs a full flood
fill per pass, and at 1.1 s for two passes on a 1920×1200 photograph, sixteen is not a
shipping answer.

The mechanism is in the code and is not a bug so much as an unstated precondition
(`Imaging/Styles/Stages/SmallRegionMerge.cs`): region areas and the label map are computed
**once**, before any merge. `LargestNeighbour` prefers a neighbour already at or above
`minimumArea`, but falls back to the largest neighbour whatever its size, and the union it
creates is never re-checked. A cluster of adjacent sub-mark regions therefore merges into
another sub-mark region and survives the pass. `[verified — read from the source and
confirmed by the pass ladder]` On the smooth synthetic golden, sub-mark regions are
isolated speckles with large neighbours, so one pass suffices and the postcondition holds —
which is exactly why the claim survived.

**The right fix is inside one sweep, not a loop of passes.** The stale label map is the
whole cause: process regions smallest-first out of a priority queue, union each into its
largest neighbour, and update the merged region's area in a union-find as you go. Every
merge then makes the union bigger and re-queues it, so the sweep terminates with no region
below the threshold that has any neighbour at all — the postcondition the Fauvism round
wanted, in roughly the cost of the pass that exists today plus a union-find. `[inferred,
from the code's structure; not built]` Chaining passes is the cheap stopgap and its price is
in the table above.

---

## 3. Cloisonnism and the key line

This is the item the parent README lists as blocked: *"Key-line rendering — blocked on
research on line weight, colour and placement — no track covered it."* The stage now
exists, so the question is what its three parameters should be and whether
Post-Impressionism should take it.

### 3.1 What the sources say

- **The style and the date.** Cloisonnism was introduced by Émile Bernard and Louis
  Anquetin in 1887; the critic Édouard Dujardin named it, after the enamelling technique,
  in March 1888. `[relayed — [Wikipedia, "Cloisonnism"](https://en.wikipedia.org/wiki/Cloisonnism);
  [TheArtStory](https://www.theartstory.org/movement/cloisonnism-and-synthetism/)]`
- **The line is described as black, repeatedly and by everyone.** "Heavy black outlines",
  "thick black lines"; of Bernard's *Portrait of Bernard's Grandmother*, "Her face and her
  expression are outlined in strong black lines". `[relayed — TheArtStory, read via
  WebFetch]` Wikipedia has "dark contours".
- **Placement is total, not selective, in the descriptions.** "All of the human bodies and
  objects in the composition are similarly outlined." `[relayed — TheArtStory]` The
  suppression of perspective and shadow reduces the composition "to a series of silhouettes
  outlined against a flat-coloured background". `[relayed — search summary; I could not
  open the underlying page]`
- **Gauguin is disputed.** TheArtStory states flatly that "Gauguin never adopted the
  Cloisonnist practice of separating forms with heavy outlines", crediting *Vision after
  the Sermon* only with "dramatic juxtapositions of saturated colours". `[verified — read
  from that page]` Multiple other sources describe the same painting as using "thick black
  contours". `[relayed]` MyModernMet splits the difference: Gauguin's treatment was "more
  subtle in colour and line" than Bernard's and Anquetin's. `[relayed]` **I could not
  settle this from the literature — §3.2 settles it from the canvases.**
- **Gauguin's instruction to Sérusier (1888)** is the movement's nearest thing to a
  primary-source recipe and contains no line at all: "How do you see these trees? They're
  yellow. So, put some yellow. This shadow, it's rather blue, paint it with pure
  ultramarine. Those red leaves? Put vermillion." `[relayed — widely quoted; I did not
  reach a primary edition]` That is a **colour** instruction, and it is the one thing the
  app already does well.
- **Van Gogh drew contours and redrew them in Prussian blue.** "Finally, at the end of the
  process, he went over the contours of the trees and the irises again, using fine,
  drawing-like lines… He used Prussian blue paint for these lines." Detected by macro X-ray
  fluorescence: "MA-XRF reveals the iron in the paint. This enables us to see the fine
  contour lines clearly." `[verified — read from
  [unravel.vangogh.com, "Thick brushstrokes"](https://unravel.vangogh.com/en/story/29/thick-brushstrokes)]`
  This is the single strongest piece of evidence in the whole section, because it is a
  physical measurement of a named pigment in a named passage.
- **Cézanne's contours are coloured too**, and reinforced rather than drawn once: "the
  brown-colored contour reinforcements around the gardener's jacket", and "the coloured
  line and coloured plane in the creation of volume". `[verified — read from
  [Artforum, "Drawing in Cézanne"](https://www.artforum.com/features/drawing-in-cezanne-213674/)]`
- **Seurat is the negative case.** No contour; forms are held by the density and hue of the
  touches. `[relayed]`

### 3.2 What I measured on the canvases

Eight works downloaded from Wikimedia Commons, resampled to at most 1400 px long edge (the
Bernard *Le Pardon* is only available at 673 px and was used as-is), converted
through the app's own `PalettePhotoConverter.RgbToLab`. A pixel counts as a dark structure
when its L\* is more than 10 below the mean L\* of a window of short-edge/40 around it; a
city-block distance transform over that set gives each dark pixel its own local half-width;
"line" pixels are dark pixels whose half-width is at most 1.5 of the app's own default mark
(short edge / 150). `[verified — computed 2026-07-30]`

| Work | dark share | line share | line L\* | line a\* | line b\* | line C\* | line hue | field L\* | line px above C\* 12 |
|---|---|---|---|---|---|---|---|---|---|
| Anquetin, *Avenue de Clichy* (1887) | 6.3% | 6.3% | 26.9 | −0.1 | −7.2 | 7.2 | 269° | 37.3 | 58% |
| Bernard, *Le Pardon de Pont-Aven* (1888) | 17.1% | 16.8% | 20.5 | 2.0 | 0.1 | **2.0** | 2° | 48.9 | 28% |
| Bernard, *Two Breton Women in a Meadow* | 9.5% | 9.5% | 44.0 | −6.3 | 9.3 | 11.2 | 124° | 66.7 | 66% |
| Gauguin, *Vision after the Sermon* (1888) | 11.2% | 11.2% | 26.4 | 5.1 | 9.9 | 11.2 | 63° | 47.2 | 40% |
| Gauguin, *The Yellow Christ* (1889) | 11.6% | 11.5% | 22.5 | 10.4 | 13.4 | **16.9** | 52° | 43.9 | 73% |
| Sérusier, *The Talisman* (1888) | 3.8% | 3.8% | 30.3 | 1.6 | 18.8 | **18.9** | 85° | 43.9 | 66% |
| Cézanne, *Mont Sainte-Victoire and Château Noir* | 7.7% | 7.7% | 19.6 | −8.3 | −3.1 | 8.9 | 200° | 37.2 | 55% |
| Van Gogh, *Wheatfield with Crows* (1890) | 14.3% | 14.3% | 26.5 | 5.5 | 16.2 | 17.1 | 71° | 48.3 | 88% |

Four readings, and one negative result.

1. **Colour: the line is chromatic except in Bernard.** Seven of eight sit at C\* 7–19; only
   Bernard's *Le Pardon* is effectively neutral at C\* 2.0, which is exactly the work whose
   description says "dominated with yellow-green and black". **"Black outlines" is right
   about Bernard and wrong about the others**, and the hue is not one hue: 52°, 63°, 71°,
   85°, 124°, 200°, 269°. Warm darks outnumber cool ones five to two. `[verified]`
2. **Lightness: the line sits 10–28 L\* below its own field, mean 20.7.** That is a
   *relative* specification. `ContourLines` uses an absolute one — `FindNearest(35, 5, −15)`
   — which on the six-paint fixture resolves to L\* 37.02, a\* 6.32, b\* −16.78. Against a
   dark canvas that line is lighter than the field; against a light one it is barely darker.
   **Deriving the line as (mapped field L\*) − 20, hue free, is a strictly better rule and
   costs about six lines.** `[verified for the measurement; `[inferred]` for the rule]`
3. **Hue: violet-blue is a defensible default but not the only one.** The stage's target
   lands at hue 290°. The corpus median is around 70° (warm). Van Gogh's Prussian blue
   contours are cool; Gauguin's measured darks are warm. Since the *hue* varies per painter
   and per picture, it should be a parameter, not a constant.
4. **Area: 3.8%–17.1% of the canvas, mean 10.3% for the six cloisonnist works.** This is the
   number to hold against the app. §3.3.
5. **The negative result: this detector does not separate cloisonnist from non-cloisonnist
   work.** The six cloisonnist canvases average 9.9% dark share; the two controls (Cézanne
   7.7%, Van Gogh 14.3%) average 11.0%. **The outlined quality is not an area effect.**
   `[verified]` Any future attempt to validate a contour stage by "does it produce the same
   dark-area fraction as a Gauguin" is measuring nothing.

**Caveats, and they matter.** These are uncalibrated web reproductions with no physical
scale — the Fauvism round's warning applies unchanged. Area shares and width-relative-to-
canvas are scale-free ratios and are the only quantities I quote; the absolute Lab values
carry unknown reproduction error, so treat the *hue spread* and the *sign* of the
line-to-field lightness difference as the findings, not the individual coordinates. The
detector also catches dark subject matter (a black skirt, a shadow) alongside drawn
contours, which is part of why finding 5 is negative.

### 3.3 What `ContourLines` actually does — three defects

Read from `Imaging/Styles/Stages/ContourLines.cs` and measured by driving the real stage
over a synthetic two-region index buffer at a ladder of mark sizes. `[verified]`

**(a) The width is not mark-derived in practice.** `radius = Math.Max(1, (int)Math.Round(MarkPixels * 0.10))`
is 1 for every `MarkPixels` below 15. Measured drawn band, on a straight boundary:

| MarkPixels | 2 | 3 | 4 | 5.2 | 6.4 | 8 | 10 | 12 | 15 | 20 | 32 | 64 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| drawn width (px) | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 6 | 6 | 8 | 14 |
| width in marks | **2.00** | 1.33 | 1.00 | 0.77 | 0.62 | 0.50 | 0.40 | 0.33 | 0.40 | 0.30 | 0.25 | **0.22** |

At the smallest mark the line is **twice a brushmark wide** — the least paintable thing the
app can emit — and by mark 12 it is a third of one. The app's own default mark is short
edge / 150, so a 4000 px photograph gets mark ≈ 20 and a 600 px one gets mark 4: the line
means completely different things at the two ends of the range. `0.25 × MarkPixels` with no
`Math.Round` floor, clamped to a minimum of 2 px, gives a constant relative width; that is
the value the Fauvism round proposed and the code does not implement.

**(b) The canvas share is not a parameter at all.** It is boundary density × 4 px. Measured
line share of the output over 14 photographs: **mean 23.8%, range 1.4% to 55.8%**; 17.3% on
the committed Fauvism golden. `[verified]` Against §3.2's 3.8–17.1% for real canvases, the
mean is above the top of the range and the worst case is more than three times it. And the
causal chain is the wrong way round —
**the more fragmented the picture, the more of it becomes line**, so the contour is at its
heaviest exactly where the underlying render is worst. Running the merge first (as Fauvism
does) is what keeps it in range; running the contour without a merge does not.

**(c) The line index is chosen absolutely, and it is too light.** §3.2, finding 2.

Two things the stage gets **right** and that should not be changed:

- **`MinimumBoundaryDeltaE = 12.0` is a genuine selection criterion, and on average it
  selects sensibly.** Measured on Post-Impressionism's own output over 14 photographs, the
  share of boundary pairs at ΔE ≥ 12 is **mean 19.2%, range 1.6–40.9%**. One boundary in five
  gets drawn, which is much closer to a painter's "decide the hard edges first" than
  outlining everything would be — and closer than the literature's "all of the human bodies
  and objects are similarly outlined". Keep the gate; expose the threshold, because a
  fixed 12 means 1.6% of boundaries on one picture and 41% on another.
- **The invariant is structural.** The stage writes an index, so it cannot name a colour
  outside the candidate set. The Fauvism round verified this against the `Refine` signature
  and it still holds. `[verified]`

### 3.4 Ruling for Post-Impressionism

**Do not register `ContourLines` on Post-Impressionism.** §1.2(a) is the reason: it
duplicates shipped Fauvism. And there is a structural reason it *cannot* be shared on
different settings today — **all three post-map stages ship with
`Parameters => Array.Empty<StyleParameter>()`**: `ContourLines`, `SmallRegionMerge` and
`GroundFill`. Every stage in the app that exposes a control is pre-map or colour.
`[verified — grepped `Imaging/Styles/Stages/`]` So slot 5 currently has no tuning surface at
all, and a second style registering `ContourLines` would get *exactly* Fauvism's line, not a
version of it. That is a defect in the slot, not in this style: the pipeline's central claim
is that a stage generalises across styles because a style can retune it, and post-map stages
are the one place where that mechanism is missing.

Fix the three defects above **in the stage** anyway, because Fauvism uses it today and
Abstract may want it later.

If the owner overrules this and wants a key line on Post-Impressionism anyway, the
parameters the evidence supports are: **width 0.25 × mark (min 2 px), ΔE gate ≈ 12–15, line
lightness = field L\* − 20, hue exposed and defaulted warm (≈ 60–80°) rather than the
violet-blue 290° Fauvism uses** — a warm line is what five of the eight measured canvases
have, and it would at least make the two rows distinguishable.

---

## 4. Cézanne's contour

**Ruling: the geometry is expressible in slot 5; the optical character is not, and a flat
index write produces a railway track rather than an unresolved edge. Do not build it.**

The three devices, and where each one lands:

- **Passage** — "small, intersecting planes of patchlike brushwork… angled planes that break
  down the contours that seem to define two-dimensional forms". `[relayed —
  [Artsy, "Passage"](https://www.artsy.net/gene/passage), via search summary]` As an
  *operation on an index buffer* this is the lost edge: rewrite the pixels on one side of a
  low-contrast boundary to the neighbour's index over part of its length. That is a pure
  selection and slot 5 permits it. **It is also what `SmallRegionMerge` already half does**,
  with an area criterion instead of a contrast criterion.
- **The doubled or searching contour** — Cézanne drew Montagne Sainte-Victoire "with up to
  six repeating, overlapping contour lines, as if searching for their final placement", and
  in the *Blue Vase* "linear outline is clearly removed some distance outside the colour
  modulation". `[relayed — search summaries of the Art Institute of Chicago's Cézanne
  digital publication, which returned **403** on direct fetch; the six-contour figure is the
  one number in this section and it is unverified]` Two parallel bands at an offset is a
  dilation with a hole — about 40 lines on top of `ContourLines`.
- **The deliberately unresolved edge** — "the perimeter of the uppermost bottle actually
  seems to explode into the surrounding space". `[verified — read from Artforum]`

So why not build the doubled contour, given that the geometry is cheap?

Because **what makes Cézanne's doubled contour read as searching rather than as an error is
that each line is a partial, translucent, low-contrast trace with the colour underneath
still showing.** The app has exactly two ways to make a mark: write a candidate index (fully
opaque, one flat colour) or run post-map arithmetic (breaks the invariant). A doubled
contour written as two flat opaque bands of one index is a track, not a search. I did not
build it and measure it — that is the honest gap — but the mechanism is not in doubt: the
`Refine` signature admits no partial coverage, and §6 shows even a *single* line at 4 px
takes a mean 23.8% of the canvas and up to 55.8%, so two of them at any legible spacing
would take substantially more. `[inferred]`

The category that *could* carry it is report 03's **category D**: a thin translucent film
composited through the Kubelka–Munk kernel over the mapped colour. That is the glaze pass,
it is physically honest, and it enlarges the invariant from "mixable" to "applicable". If
Cézanne's contour is ever wanted, that is the route — and it is a much larger decision than
an edge stage.

**One Cézanne device is buildable and cheap, and I recommend it: the lost edge.** A
contrast-gated merge — for each boundary below a ΔE threshold, absorb the smaller region
into the larger — is the selection form of passage, is invariant-safe by the `Refine`
signature, and attacks the measured defect directly. Post-Impressionism's boundary ΔE
distribution says how much it would remove: on the 14-source corpus, 4.9% of boundary pairs
are already below ΔE 2 and the median is about 5, so a gate at ΔE 5 would retire roughly
half the boundaries in the picture. `[verified for the distribution; `[inferred]` for the
effect]` It is the same flood fill `SmallRegionMerge` already runs, with a different
predicate.

---

## 5. Edge hierarchy

### 5.1 The parent proposal names the wrong knob

Report 03's lever 1 — its own top pick — is *"spatially varying blur radius driven by a
focal point… build 3–5 blurred copies at geometric radii with the existing separable blur
and lerp per pixel in linear light, ~120 lines"*. That report was written when the pre-map
stage was `GaussianBlur`. It is now `EdgePreservingFloor`, a guided filter, and **a guided
filter's radius is not its softness parameter.** The local linear model is
`a = var / (var + ε)`: where local variance far exceeds ε the output is the input untouched,
whatever the window size. Enlarging the window flattens more *area* but preserves the same
*edges*. `[verified — read from `Imaging/GuidedFilter.cs`'s own doc comment, and confirmed
by the measurement below]`

### 5.2 Measured

I built three throwaway variants of the focal proposal, each calling the real
`GuidedFilter.Apply` / `GaussianBlur.Apply` and the real `LinearPlanes` encode/decode, and
differing only in which parameter the radial falloff drives: four filtered copies, a flat
sharp core out to 0.35 of the focal span, then a ramp, lerped per pixel in linear light.
Effect on mean boundary ΔE in the mid-field band (band 1 of four radial quartiles), against
the shipped style on the same source: `[verified — computed 2026-07-30, five sources: the
repo's own photograph plus four Windows stock wallpapers that later turned out to be
synthetic renders. Treat the size of these percentages as an optimistic bound and their
**ordering** as the finding; the 14-source corpus figures in §6 corroborate the ordering.]`

| Variant | what varies | mid-field boundary ΔE change | focal-band change |
|---|---|---|---|
| `focalRadius` | guided-filter window radius | **≈ 0%** (−1.1, −0.3, 0.0, 0.0, +2.2%) | ≈ 0% |
| `focalGauss` | Gaussian radius applied after the floor | **−4.1%** | ≈ 0% |
| `focalEdge` | guided-filter **edge threshold** | **−16.9%** (−12.8, −32.4, −11.6, −0.1, −27.7%) | **−1.8%** |

The edge-threshold variant does what the lever was for: it holds the focal region's edge
contrast within 2% while dropping the mid-field's by a sixth, and it **improves**
paintability rather than costing it — over the full 14-source corpus it takes sub-mark share
from 32.2% to 24.6%, mean boundary ΔE from 8.76 to 7.11, and hard-edge share from 25.5% to
17.8%, the only variant tested that softens rather than hardens (§6). The radius variant,
which is the one the parent report specified, does essentially nothing.

Two further mechanism findings that fall out of the same run and matter beyond this lever:

- **Nothing in slots 1 or 5 can soften an edge; every stage there preserves or hardens it.**
  Every operation I measured *raised* mean boundary ΔE: `SmallRegionMerge` takes
  Post-Impressionism from 9.33 to 11.30 on one photograph and from 8.15 to 9.99 on another,
  because it deletes the weak boundaries and leaves the strong ones. Edge-preserving
  smoothing does the same thing by construction. The one softening operator in the codebase
  is `OptionalBlur`, which no registered style uses. **"Soft edge" is currently not a thing
  the app can produce**, so an edge hierarchy has to be built out of *which* edges survive,
  not out of how gradual they are. `[verified]`
- **Automatic focal-point detection stays rejected**, and this measurement does not
  reopen it. My probe used a fixed image-centre focus and still produced the hierarchy —
  which is the parent README's centre-bias finding restated. Let the user click; default to
  centre.

### 5.3 Is uniform edge treatment wrong for this movement?

**Weaker here than the parent round assumed, but not wrong.** The edge-hierarchy literature
is painting instruction, not art history: "artists often save their hardest edges for the
focal point, then relax other areas"; "sharp edges come forward, soft edges recede";
"decide your hard edges first, then find opportunities for lost edges". `[relayed — Draw
Paint Academy, Art Studio Life, MontCarta, Oil Painters of America; the same sources report
03 §1.1 used]` None of it is specific to Post-Impressionism, and two of the four figures
work directly against it: **Seurat's whole method is an even, all-over field, and cloisonnism
suppresses depth cues on purpose.** Cézanne is the one who unambiguously varies edge
resolution across the canvas, and Van Gogh varies mark density rather than edge hardness.

So the case for the focal lever under this style label rests on Cézanne alone, which is
thinner support than report 03's blanket "the single largest gap". It is still worth
building, because it is the only remaining device that differentiates Post-Impressionism
from Fauvism without duplicating a stage — but it should be pitched as **Cézanne's varying
resolution**, not as a universal painterliness fix.

### 5.4 Cost

Measured on a 1920×1200 photograph, six-paint palette, base mark 8: `[verified]`

| | time |
|---|---|
| Post-Impressionism as shipped | 2,570 ms |
| + `SmallRegionMerge` ×2 | 3,673 ms |
| focal edge-threshold floor (4 copies) | 10,251 ms |
| both | 10,980 ms |

**The focal lever is a 4× render cost**, because it runs the guided filter four times at
strength 3 (twelve passes) plus a three-plane lerp. Three copies instead of four, or
strength 2 on the outer levels, are the obvious economies; either way this is not free and
the parent report's "~4× one blur" estimate was written for a Gaussian, not for an iterated
guided filter. The merge is cheap by comparison: +1.1 s for two passes.

---

## 6. What the pipeline should do, measured end to end

Every variant below was rendered through the real `StylePipeline.Render` with a
`StyleDefinition` assembled from the real stage classes — no stage was transcribed. Means
over the 14 photographic sources, each at its own default mark with Post-Impressionism's
`MarkScale` 1.6. `[verified — computed 2026-07-30]`

| Variant | below mark² | regions | bound/1000px | transition px | mean ΔE | line share | hard ≥10 |
|---|---|---|---|---|---|---|---|
| shipped | 32.2% | 83,883 | 621.0 | 54.1% | 8.76 | — | 25.5% |
| + `SmallRegionMerge` ×1 | 19.7% | 44,125 | 433.8 | 42.7% | 10.37 | — | 29.5% |
| + `SmallRegionMerge` ×2 | 13.5% | 27,656 | 349.8 | 37.5% | 11.22 | — | 31.4% |
| + `SmallRegionMerge` ×4 | 6.9% | 12,944 | 264.6 | 31.8% | 11.86 | — | 31.7% |
| + `ContourLines` only | 20.7% | 26,918 | 350.0 | 38.0% | 12.15 | **23.8% (1.4–55.8)** | 21.9% |
| + merge ×2 + `ContourLines` | 4.7% | 6,600 | 225.8 | 29.6% | 17.15 | 19.5% (1.6–40.2) | 34.3% |
| + focal edge-threshold floor | 24.6% | 50,319 | 511.9 | 50.7% | **7.11** | — | **17.8%** |
| + focal floor + merge ×2 | **5.6%** | 11,863 | 274.0 | 33.9% | 9.00 | — | 26.2% |

Three things to read off it.

- **The contour's canvas share is out of control.** Mean 23.8%, and on one photograph
  **55.8% of the output is line**. Against §3.2's 3.8–17.1% for real canvases that is not a
  stylistic choice, it is a failure mode — and it is caused by the underlying fragmentation,
  which is why running the merge first pulls it back to 19.5%.
- **Merging raises mean boundary ΔE and hardens the picture** (8.76 → 11.86 across four
  passes, hard share 25.5% → 31.7%), because it deletes weak boundaries and keeps strong
  ones. The focal floor is the only variant that moves the other way (ΔE 7.11, hard 17.8%).
- **The two together are complementary, not redundant**: focal floor plus two merge passes
  reaches 5.6% below mark² — better than four merge passes alone — while keeping the softest
  edge distribution of any variant that also fixes paintability.

---

## 7. Where this corrects or extends prior research

**Corrects:**

1. **The Fauvism round's "hard postcondition" for `SmallRegionMerge` is false after one
   pass.** §2.3. It holds on the synthetic golden and fails on every photograph. This is
   the same failure mode as that round's own correction 2 about floor strength, applied to
   its own top verification asset.
2. **Report 03's lever 1 names the wrong parameter.** §5.1–5.2. Spatially varying the
   *radius* of the current floor does nothing; varying its *edge threshold* does the job.
   The report was correct for the Gaussian it was written against.
3. **The Fauvism round's contour-measurement trap has a second side.** §2.1. Masking the
   line index by making each line pixel its own region over-reports fragmentation as badly
   as not masking under-reports it. Drop line pixels from the denominator too.
4. **"Cloisonnist outlines are black" is right for Bernard and wrong for Gauguin, Anquetin,
   Sérusier, Cézanne and Van Gogh.** §3.2. The one direct pigment identification available —
   MA-XRF on Van Gogh's redrawn contours — gives Prussian blue. The Fauvism round already
   warned that "a 'darkest candidate' default would be wrong as often as right"; that
   warning is now measured, and the shipped stage's violet-blue is a reasonable guess with
   the wrong lightness.
5. **`ContourLines`' width is not mark-derived below `MarkPixels` 15.** §3.3. The Fauvism
   round recommended "line width as a fraction of the mark (0 = off, default ~0.25)"; what
   shipped is `Math.Round(mark * 0.10)`, which rounds to a constant.

**Extends:**

- The abstract round's "`MarkPixels` reaches exactly one consumer" is now out of date —
  `ContourLines` and `SmallRegionMerge` both read it. But `ContourLines` reads it through a
  rounding that discards it (§3.3), so the *spirit* of the finding survives: mark size still
  reaches exactly one stage that honours it.
- Report 03's four-category invariant table needs no change. Everything recommended here is
  category A or category B, and the one thing I rejected (Cézanne's translucent contour) is
  rejected precisely because it is category C or D.

**New, and architectural rather than stylistic:** **slot 5 has no tuning surface.** All three
post-map stages — `ContourLines`, `SmallRegionMerge`, `GroundFill` — declare
`Parameters => Array.Empty<StyleParameter>()`, while every pre-map and colour stage exposes
controls. `[verified — grepped `Imaging/Styles/Stages/`]` The pipeline's design claim is that
a stage generalises across styles because a style can retune it via `WithDefaults`; in slot 5
that mechanism exists but nothing uses it, so two styles registering the same post-map stage
get byte-identical behaviour. That is the mechanical reason §1.2's "Fauvism has spent the
cloisonnist device" bites as hard as it does, and it is worth fixing independently of
anything in this report.

**Where I could not settle a question:**

- **Did Gauguin outline?** TheArtStory says never; several other sources say thick black
  contours; my own measurement finds dark thin structures over 11% of both *Vision after
  the Sermon* and *The Yellow Christ*, chromatic and warm, which is consistent with either
  "he outlined in a coloured dark" or "the detector is finding shadow and dark subject
  matter". A technical study of either canvas would settle it and I found none.
- **Whether a doubled contour would read as passage or as a track.** I argued it from the
  signature rather than building it. §4.

---

## 8. Three picks

Ranked. Line counts are C#-from-scratch estimates in the style of the existing
`Imaging/Styles/Stages/` files, excluding UI.

### 1. Register `SmallRegionMerge` on Post-Impressionism, and make one sweep converge

**Slot 5. ~10 lines in `StyleRegistry`, ~60 lines inside `SmallRegionMerge` for a
smallest-first union-find sweep, plus one `StyleParameter` for the threshold.**

Post-Impressionism has the largest `MarkScale` of the three styles with an empty slot 5, and
it is the most fragmented style in the app relative to the brush it asks for. One pass takes
it from 32.2% of pixels below its own mark² to 19.7%; the current stage needs sixteen chained
passes to approach zero (§2.3). Replacing the single stale label map with a smallest-first
union-find sweep should reach the postcondition in one pass at roughly today's cost. The
stage exists, is invariant-safe by the `Refine` signature, and is already registered on two
other styles. `[verified for the ladder; `[inferred]` for the sweep]`

The convergence fix is a correction to the stage, not a Post-Impressionism feature: Fauvism
and Abstract each run exactly one pass today and each is leaving fragments behind on real
photographs.

**Verification.** `FractionInRegionsSmallerThan(mark²)` after one invocation at threshold
1.0 must be zero on a real photograph, not only on the golden gradient — that is the
assertion the Fauvism round wanted, and pinning it on the gradient alone is what let the
current behaviour through. Regenerate `Tests/Golden/Post-Impressionism.png` and look at it.

### 2. A focal edge-threshold floor

**Slot 1. ~120 lines, replacing or wrapping `EdgePreservingFloor` for this style.**

Four guided-filter copies at geometrically spaced *edge thresholds* (ε, 2.5ε, 6.25ε,
15.6ε), lerped per pixel in linear light against a radial falloff from a user-clicked focal
point defaulting to centre, with a flat sharp core. Measured: focal-band boundary contrast
held within 2%, mid-field down 17%, paintability slightly improved. `[verified, §5.2]`

This is report 03's lever 1 with the parameter corrected, and it is the only device in this
report that differentiates Post-Impressionism from Fauvism without duplicating a stage.
Cost is the caveat: 4× render time as prototyped (§5.4). Drop to three levels and cap the
outer levels' iteration count before shipping.

**Verification.** Mean boundary ΔE inside the focal disc must be within a few percent of
the uniform render's while the outer band's falls measurably; a zero focal radius must
leave the buffer byte-identical to `EdgePreservingFloor`'s output.

### 3. Fix `ContourLines`' three defects and give it parameters

**Slot 5. ~25 lines inside the existing stage. Registered on Fauvism, not on
Post-Impressionism.**

- Width `0.25 × MarkPixels`, floored at 2 px, no `Math.Round` collapse — currently a
  constant 4 px for every mark from 2 to 12, i.e. 2.0 marks wide at the small end. `[verified, §3.3]`
- Line index from **field lightness minus 20 L\***, not from an absolute Lab target — the
  measured line-to-field lightness gap across eight canvases is 10–28, mean 20.7, and the
  current target lands at L\* 37 against a corpus median of ≈26. `[verified, §3.2]`
- Three `StyleParameter`s — width, hue, boundary ΔE gate — so two styles can share the stage
  on different settings. **All three post-map stages currently declare no parameters at all**
  (§3.4), so slot 5 is the one slot where the pipeline's "a stage generalises because a style
  can retune it" claim is not actually available. Fixing it here fixes it in the cheapest
  place.

Keep `MinimumBoundaryDeltaE` as a concept; it already selects about one boundary in five
rather than outlining everything, which is the painter's order of operations and better
than the literature's description of the movement.

**Runner-up, not ranked because I did not measure it: the lost edge** (§4). A contrast-gated
merge — absorb the smaller side of any boundary below a ΔE threshold into the larger — is the
selection form of Cézanne's passage, is the same flood fill `SmallRegionMerge` already runs
with a different predicate (~40 lines on top of pick 1), and is the only Cézanne device this
pipeline can express honestly. Build it if picks 1 and 2 land and the output still reads as a
posterised photograph.

---

## 9. What not to build

Each of these I went looking for and rejected. The parent, abstract and Fauvism lists all
still apply; these are additional.

- **`ContourLines` on Post-Impressionism.** §1.2(a), §3.4. Shipped Fauvism already renders
  flat planes with a drawn contour, and the two outputs are the same picture. Two style rows
  should not share their most conspicuous device.
- **A doubled or searching contour for Cézanne.** §4. The geometry is expressible; the
  optical character — partial, translucent, colour showing through — is category C or D, and
  a flat opaque double band reads as a railway track. One line already takes a mean 23.8% of
  the canvas and up to 55.8% (§6); two at a legible spacing would take more of the picture
  than the picture.
- **Splitting Post-Impressionism into two or more style rows.** §1.2. Three of the four
  branches (Van Gogh's stroke field, Seurat's optical boundary, Cézanne's unresolved edge)
  are not buildable in the current pipeline, so a split would ship one real row and several
  tuned copies. Revisit if stroke synthesis or broken colour ever lands.
- **Spatially varying the guided filter's *radius*.** §5.2. Measured at ≈0% effect on
  boundary contrast. This is the parent round's own lever 1 and it does not survive the
  change of pre-map filter.
- **A spatially varying Gaussian after the floor, as the hierarchy mechanism.** §5.2. −4%
  against the edge-threshold variant's −17%, and it re-introduces exactly the operator four
  independent tracks agreed to remove.
- **Raising `EdgePreservingFloor.strength` above 3 as the fix.** §2.2. On
  `Tests/Assets/sample.heic`, 3 → 5 buys 18.20% → 14.43% while softening the whole picture;
  two merge passes on the same photograph buy 18.20% → 1.76% and touch nothing else.
- **Automatic focal-point detection.** Unchanged from the parent README. My probe reached
  the whole measured effect with a fixed image-centre focus, which is the centre-bias
  finding restated rather than contradicted.
- **Validating a contour stage against paintings by dark-area fraction.** §3.2, finding 5.
  The measure does not separate cloisonnist canvases from Cézanne and Van Gogh (9.9% vs
  11.0%). Whatever makes a picture read as outlined, it is not how much of it is dark.
- **A "black outline" default anywhere in the app or its doc comments.** §3.2, correction 4.
  True of Bernard, false of the rest, and the one pigment identification available says
  Prussian blue.
- **Post-map anti-aliasing of a contour, or any repair re-map.** Inherited from the Fauvism
  round and unchanged: a 1 px staircase is a small fraction of a mark, and the repair pass
  costs a second mapping.
- **An "unpainted / reserved" edge treatment.** The abstract round's "the ground *is* paint"
  ruling covers it, and Cézanne's reserves are the same case as the Fauve ones already
  rejected there.

---

## 10. Verification debt

Ranked by how much clearing each would change a decision above.

1. **Build and look at the doubled contour.** §4's rejection is argued from the `Refine`
   signature, not measured. It is maybe 40 lines on top of `ContourLines` in a throwaway
   probe, and it is the only recommendation in this report resting purely on inference. If
   it reads as passage rather than as a track, pick 3 changes and Cézanne comes back into
   range.
2. **Build the smallest-first union-find sweep and check it converges in one pass.** Pick 1
   rests on the claim that the stale label map is the only reason the postcondition fails.
   The pass ladder is consistent with that and the code reads that way, but it is not built.
3. **The Art Institute of Chicago's Cézanne digital publication** — `artic.edu` returned
   **403** on both the "A Harmony Parallel to Nature" essay and "Cézanne's Still Lifes under
   the Microscope". These carry the "up to six repeating, overlapping contour lines" figure,
   which is the only quantitative statement about contour repetition anywhere in this report
   and is currently `[relayed]` from a search summary.
4. **A technical study of *Vision after the Sermon* or *The Yellow Christ*.** Would settle
   §7's open question about whether Gauguin outlined, and would put a pigment name on the
   line the way MA-XRF did for Van Gogh. The National Galleries of Scotland hold the former;
   I did not attempt their object pages after the Fauvism round recorded NGA 403s.
5. **Corpus curation for §3.2.** Eight works, self-selected, uncalibrated web reproductions
   at 1400 px. The hue *spread* and the sign of the line-to-field lightness gap are robust
   to that; the individual Lab coordinates are not. The Fauvism round's corpus warning
   applies and its cautionary tale (a "photograph" control that turned out to be a Derain)
   is the reason this is on the list.
6. **My photographic corpus is 14 images, one of them the repo's own HEIC and 13 Wikimedia
   photographs picked without a sampling frame.** Windows' bundled wallpapers, which I used
   for the first pass and whose per-source figures appear in §5.2's percentage changes,
   turned out to be synthetic 3-D renders rather than photographs — smooth, noiseless, and
   therefore flattering to the pipeline in exactly the way `Tests/Golden` is. §5.2's
   per-variant percentages come from that set and are an **optimistic bound**; every table
   labelled 14 sources is the real one. Re-running §5.2's radial-band comparison over the
   photographic corpus would close this.
7. **Berkowitz, "The 1910 'Manet and the Post-Impressionists' Exhibition"** (BRANCH) —
   located, not opened. Would firm up §1.1's historiography, which is currently three
   secondary sources agreeing.

---

## Appendix — how this was measured

A throwaway console project in the session scratchpad, assembly-named `PaintTranslator.Tests`
so the app's existing `InternalsVisibleTo` grant applies, with a `ProjectReference` to
`PaintTranslator.csproj`. Nothing was added to the repository.

Every pipeline result came from `StylePipeline.Render` with a `StyleDefinition` built from
the real stage instances (`EdgePreservingFloor`, `ToneAndChromaRemap`, `KeepAllCandidates`,
`NearestQuantiser`, `SmallRegionMerge`, `ContourLines`) at Post-Impressionism's registered
defaults. The contour colour was obtained by calling the real
`CandidateSet.FindNearest(35, 5, −15)` on a `MixtureBuilder` over the same six paints
`Tests/StyleTestFixtures.SixPaints` uses. Region counts and sub-mark share came from the
real `PaintabilityMetrics.CountRegions` and `FractionInRegionsSmallerThan`; a masked variant
of the same flood fill was written only because `ForEachRegion` is private and reports areas
only. Lab conversion throughout is `PalettePhotoConverter.RgbToLab`.

Boundary statistics are my own and are defined here so they can be reproduced: a *boundary
pair* is a four-adjacent pixel pair whose RGB differs; *boundary per 1000 px* is boundary
pairs ÷ pixels × 1000; a *transition pixel* has at least one differing four-neighbour; the
*within ½ mark* share is the fraction of pixels within `round(mark/2)` of a transition
pixel; boundary ΔE is plain Euclidean CIELAB between the two members of a boundary pair.
Radial bands are quartiles of distance from the image centre normalised by the half-diagonal.

Sources: 14 photographic images (`Tests/Assets/sample.heic` plus 13 Wikimedia photographs),
five Windows stock wallpapers used in the earlier passes and later found to be synthetic
renders, the committed `Tests/Golden/*.png`, and eight Wikimedia reproductions of
Post-Impressionist canvases. Scripts kept in the scratchpad, not committed.
