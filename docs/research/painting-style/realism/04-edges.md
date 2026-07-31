# Research: Realism — Edges

**Track:** Realism, track 4 of 4 — the edges track.
**Date:** 2026-07-31
**Scope:** what realist edges measure as, where the shipped Realism row sits against them, what the
mandatory floor's two knobs do at Realism's settings, whether edge hierarchy is this movement's
device, and — the question nobody has asked before — what the nearest-candidate quantiser does to a
boundary when it is the last thing that touches one.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(the edge-hierarchy vocabulary, the filter families, the four-category invariant table),
[`../post-impressionism/03-edges.md`](../post-impressionism/03-edges.md) (the boundary-statistics
definitions, which this report reuses unchanged), and
[`../tonalism/03-edges.md`](../tonalism/03-edges.md) (the source-domain edge statistics, the
range-normalisation method, and the ε-versus-strength result). Where I correct any of them, §8 says
so.

**Verification convention** — matching the rest of `docs/research/`:
`[verified]` = read directly from the cited primary source, or computed in this repo ·
`[relayed]` = reported by a secondary source I did not confirm at the primary ·
`[inferred]` = my reasoning from the above, stated nowhere.

---

## 0. Headline

**Realist canvases really are lower-contrast at their edges than photographs, and unlike Tonalism's
this is not a tonal-range artefact. The shipped Realism row misses that envelope by more than an
unprocessed photograph does — and the stage responsible is not the floor, it is the quantiser.**

Five results, in descending order of how much they should change a decision.

1. **The naive claim is false, and it survives the normalisation that killed the Tonalist version
   of it.** Over 21 realist canvases against 17 subject-matched photographs, pixels with a
   neighbour at ΔE ≥ 20 run **2.09% vs 9.72%** (ratio 0.215, t = −2.65). Rescale every image to a
   common 60 L\* range — the correction that dissolved four-fifths of Tonalism's edge gap — and it
   becomes **2.42% vs 6.50%** (0.373, t = −2.45). About a third of the gap is tonal compression;
   **two-thirds is a real difference in how much contrast a realist puts across a boundary.** The
   reason the correction bites so much less here is that realist canvases are *not* tonally
   compressed: L\* 5–95 spread **60.9 vs 70.2**, a ratio of 0.87 against Tonalism's 0.59.
   `[verified]` §2.2, §2.4
2. **Shipped Realism sits further from the realist-canvas envelope than the photograph it started
   from, and the quantiser is why.** On an RMS z-distance over seven edge statistics in units of
   the canvas corpus's own SD: the shipped row **1.415**, an unfiltered photograph **1.293**, the
   median individual realist canvas **0.58**. Measured through the real pipeline in three stages,
   the floor does exactly the right thing and the quantiser undoes it and overshoots — median edge
   width **1.55 (photo) → 1.84 (after the floor) → 1.33 (rendered)**, hard-width share
   **50.8% → 42.1% → 60.9%**, against a canvas target of 1.75 and 38.4%. `[verified]` §4.2, §5
3. **"Lost and found" is a contrast instruction, not a width instruction — confirmed on a second
   movement and a second corpus, and sharpened.** Median realist edge width is **1.75 px** against
   photographs' 1.55, a difference of 0.20 px, not significant (t = 1.48); the *soft* class is
   **9.51% vs 11.11%**, i.e. realist canvases have slightly **fewer** soft edges than photographs
   (t = −0.51). The one width statistic that does separate them is the **firm** class —
   **52.10% vs 37.99%, t = +3.83**, the largest t in the table. **The realist move is hard → firm,
   one class, not hard → soft, two.** `[verified]` §2.3
4. **Edge hierarchy is not a realist device.** Centre-to-frame high-contrast edge ratio: canvases
   median **1.48**, photographs median **1.41** (means 1.99 vs 1.58, t = 1.01, n.s.). Six of 21
   canvases run it backwards. **A photograph already carries as much focal edge hierarchy as a
   realist canvas does**, because photographers centre their subjects. This is a stronger negative
   than the Tonalism round's, which at least found a 2.4× differential. `[verified]` §3
5. **`SmallRegionMerge` fixes Realism's paintability and destroys its edges.** Registered in
   Realism's empty slot 5 it takes sub-mark share **52.69% → 0.00%** and takes the width mix to
   **94.1 / 5.8 / 0.1** against a canvas target of 38.4 / 52.1 / 9.5, mean boundary ΔE 9.24 →
   14.12, and z-distance **1.415 → 2.024** — worse than 14 of the 17 photographs. Halving its area
   threshold does not help the width mix. Rendered and looked at, a flock of sheep becomes salmon
   blobs. **Realism cannot be both paintable and realist-edged with what slots 1 and 5 hold
   today.** `[verified]` §7

---

## 1. What the movement says about edges, and what it does not

Tonalism had a treatise (Birge Harrison's chapter on refraction). **Realism has nothing equivalent,
and the honest finding is that its own doctrine is about subject, not handling.**

- **Courbet's programme is about what to paint.** His 1855 statement is "to translate the customs,
  the ideas, the appearance of my epoch according to my own estimation… to be not only a painter
  but a man as well; in short, to create living art". Nothing about edges, finish or definition.
  `[relayed — Wikipedia, "Gustave Courbet", quoting the 1855 pavilion catalogue, which I did not
  obtain]`
- **But the handling is part of the received definition.** For Courbet, realism "entailed
  spontaneous and rough handling of paint, suggesting direct observation by the artist while
  portraying the irregularities in nature"; contemporaries "accused Courbet of a deliberate pursuit
  of ugliness" and Paris judged the work "like an upstart in dirty boots crashing a genteel party".
  `[relayed — same source, quoting Sarah Faunce]`
- **The single most useful sentence in the secondary literature, and it is the one that predicts my
  measurement:** "In the 19th century, Realism art movement painters such as Gustave Courbet were
  not especially noted for fully precise and careful depiction of visual appearances; **in
  Courbet's time that was more often a characteristic of academic painting**." `[relayed —
  Wikipedia, "Realism (arts)"]` §2.5 measures exactly that split and finds it.
- **The contemporaneous English theory of indistinctness is Ruskin's, not a Realist's.** *Modern
  Painters* IV contains "Of Turnerian Mystery: First, as Essential", whose argument is that
  "if you watch any object as it fades in distance, it will lose gradually its force, its
  intelligibility, its anatomy, its whole comprehensible being", and that in a real landscape "if
  we look at any foreground object, so as to receive a distinct impression of it, the distance and
  middle distance become all disorder and mystery". `[relayed — search-index excerpts; the
  archive.org plain text of volume IV would not return chapter IV through this environment, and
  Gutenberg's volume III does not contain it]` This is the same foveal argument Harrison makes for
  Tonalism, from a hostile witness to Realism, and it is **not** a Realist source.
- **The modern "lost and found edges" instruction is painting-blog folklore**, exactly as the
  parent report's §1.1 records it. I went looking for a realist or academic primary text that
  teaches it and did not find one. Harold Speed's *The Practice and Science of Drawing* (1913) —
  the closest academic candidate — mentions "a play of lost-and-foundness on the edges" and that
  boundaries are "continually merging into the surrounding mass and losing themselves to be caught
  up again later on and defined once more", but devotes no chapter to it. `[verified — read via the
  Project Gutenberg text of ebook 14264]` Solomon J. Solomon's *The Practice of Oil Painting*
  (1910), a Royal Academician's manual, contains **no edge instruction at all** on the text I could
  retrieve. `[verified for the retrieved text; the retrieval may have been partial — §12 debt 5]`
- **Realists used photographs.** Courbet worked from photographs for *The Bathers*; Eakins
  incorporated photographic research directly into his paintings. `[relayed — search summaries;
  I did not reach a primary technical study]` So "realism = the photograph's edges" is not merely
  a naive reading of the label, it is a reading the painters themselves would have had to reject
  deliberately. §2 shows they did.

**Nothing in the literature measures realist edges.** Everything numeric below is this report's.
The nearest published result is Redies' group's, which reports that plain edge density does not
separate artworks from natural images while edge-*orientation entropy* does, and that art and
natural scenes share similar amplitude-spectrum slopes. `[relayed — search summaries of Redies et
al., *Vision Research* 2017 and 2010; I did not open either]` My §2 is consistent with the first
half and sharper: edge density *at high contrast* separates the two corpora by 4.6×.

---

## 2. Measured: 21 realist canvases against 17 photographs

### 2.1 Method, stated so it can be reproduced

Identical to the Tonalism round's §2.1, deliberately, so the two rounds' tables sit on one scale.
Every image is cropped **3% off each edge** and resampled so its **short edge is exactly 800 px**.
Conversion to CIELAB is the app's own `PalettePhotoConverter.RgbToLab`. Definitions:

- **g(x,y)** — the larger of the CIELAB ΔE to the right neighbour and to the neighbour below.
  **D2 / D5 / D10 / D20** are the share of pixels with g ≥ 2, 5, 10, 20.
- **Edge span** — an 8-px horizontal or vertical run whose end-to-end ΔE is ≥ 12. **Edge-span
  share** is what fraction of all spans qualify.
- **Edge width** — for a qualifying span, (end-to-end ΔE) ÷ (largest single adjacent step inside
  it), clamped to [1, 8]. A step gives 1; a linear ramp of width *k* gives *k*. Classified
  **hard** < 1.5, **firm** 1.5–3, **soft** 3–6, **lost** ≥ 6.
- **Range normalisation** — every local contrast multiplied by 60 ÷ that image's own L\* 5–95
  spread before the D-series is recomputed.

**The photographic control is subject-matched on purpose.** The Tonalism round records that its
corpora were not matched and that the mismatch inflated its raw gap. Mine pairs field labour with
the Millets and Bastien-Lepage, an industrial interior with the Menzel, a herd with the Bonheur, a
boat at sea with the Homers, market and workshop interiors with the Courbets, and rural landscape
with the Shishkin and Levitan. §13 lists both corpora and the rejections.

### 2.2 The distributions `[verified — computed 2026-07-31]`

Means over 21 canvases and 17 photographs, SD in brackets, median after "md". Welch *t* on the two
means.

| | Realist canvases (21) | Photographs (17) | ratio | t |
|---|---|---|---|---|
| Mean local change g (ΔE) | 4.78 (2.05) md 4.22 | 7.44 (4.71) md 6.46 | 0.643 | −2.16 |
| **D2** — pixels with g ≥ 2 | **71.66 (16.21)** | **65.02 (17.75)** | **1.102** | +1.19 |
| D5 | 30.44 (17.34) md 24.54 | 41.62 (21.77) md 38.93 | 0.732 | −1.72 |
| **D10** | **10.33 (9.67) md 7.26** | **24.14 (19.20) md 20.19** | **0.428** | **−2.70** |
| **D20** | **2.09 (2.96) md 1.22** | **9.72 (11.54) md 6.76** | **0.215** | **−2.65** |
| Edge-span share (ΔE ≥ 12 over 8 px) | 20.20 (10.96) | 33.24 (15.70) | 0.608 | **−2.90** |
| Edge width hard / **firm** / soft | 38.39 / **52.10** / 9.51 | 50.82 / **37.99** / 11.11 | — | −2.08 / **+3.83** / −0.51 |
| Median edge width (px) | 1.75 (0.33) | 1.55 (0.46) | 1.126 | +1.48 |
| **L\* 5–95 spread** | **60.91 (13.06)** | **70.20 (9.71)** | **0.868** | −2.51 |
| L\* 5th / 95th percentile | 11.87 / 72.77 | 10.32 / 80.53 | — | +0.76 / −1.86 |
| Median C\*ab | 12.09 (5.08) | 16.28 (11.04) | 0.742 | −1.45 |
| Range-normalised D10 / D20 | **11.15 / 2.42** | **20.27 / 6.50** | 0.550 / **0.373** | −2.07 / **−2.45** |
| Range-normalised mean g | 4.93 | 6.20 | 0.796 | −1.39 |

**The D-row crosses over between ΔE 2 and ΔE 5, exactly as it does for Tonalism.** A realist canvas
has *more* places where the colour changes a little (71.7% vs 65.0%) and dramatically fewer where
it changes a lot. That shape — lots of small incident, little large incident — is the thing to aim
at, and no low-pass filter produces it, because a low-pass takes both ends down together.

Per canvas, so the spread is visible (all 21; the two poles are the Millet *Angelus* and the
Gérôme):

| Work | g | D2 | D5 | D10 | D20 | spans | med width | L\* range | c÷o D10 |
|---|---|---|---|---|---|---|---|---|---|
| Courbet, *A Burial at Ornans* | 5.46 | 84.4 | 39.9 | 10.6 | 1.96 | 15.9 | 1.49 | 57.5 | 1.10 |
| Courbet, *L'Atelier du peintre* | 4.82 | 79.7 | 30.6 | 8.7 | 1.74 | 17.9 | 1.70 | 60.4 | 4.71 |
| Courbet, *Bonjour Monsieur Courbet* | 3.37 | 48.7 | 19.1 | 5.9 | 1.08 | 18.3 | 1.95 | 73.7 | 2.62 |
| Millet, *The Gleaners* | 3.77 | 68.2 | 22.0 | 4.8 | 0.55 | 12.9 | 1.84 | 69.5 | 1.71 |
| **Millet, *The Angelus*** | 2.76 | 61.8 | 8.4 | 0.9 | **0.07** | **5.10** | 2.16 | 59.4 | 0.68 |
| Eakins, *The Gross Clinic* | 2.73 | 41.2 | 12.4 | 3.9 | 0.76 | 11.7 | 1.90 | 48.4 | 0.78 |
| Eakins, *Max Schmitt in a Single Scull* | 3.85 | 59.0 | 21.5 | 7.5 | 1.41 | 13.5 | 1.61 | 37.8 | 4.76 |
| Eakins, *Swimming* | 5.10 | 86.9 | 36.2 | 9.2 | 1.12 | 19.8 | 1.75 | 61.9 | 1.88 |
| Homer, *Snap the Whip* | 4.34 | 82.7 | 27.3 | 5.6 | 0.56 | 18.4 | 1.86 | 41.2 | 1.67 |
| Homer, *The Gulf Stream* | 5.48 | 83.1 | 38.1 | 12.2 | 2.23 | 22.9 | 1.52 | 74.0 | 2.18 |
| Homer, *The Fog Warning* | 4.11 | 68.8 | 24.2 | 6.9 | 1.22 | 17.0 | 1.67 | 75.2 | 2.22 |
| Repin, *Barge Haulers on the Volga* | 4.69 | 81.4 | 28.4 | 8.3 | 1.55 | 18.1 | 1.69 | 74.4 | 6.07 |
| **Bastien-Lepage, *Joan of Arc*** | **8.81** | 95.8 | 68.3 | **31.4** | **6.31** | **50.2** | 1.36 | 51.3 | 1.48 |
| Bastien-Lepage, *Hay making* | 5.84 | 86.5 | 43.3 | 14.0 | 2.07 | 27.5 | 1.58 | 58.5 | 0.62 |
| Menzel, *Das Eisenwalzwerk* | 4.22 | 72.0 | 24.5 | 7.3 | 1.25 | 19.0 | 1.90 | 34.3 | 3.55 |
| Bonheur, *The Horse Fair* | 3.98 | 71.2 | 23.1 | 5.8 | 0.80 | 16.3 | 1.86 | 58.4 | 1.35 |
| **Bouguereau, *The Nut Gatherers*** | 3.25 | 67.4 | 14.9 | 2.5 | 0.13 | 19.9 | **2.69** | 75.5 | 1.11 |
| **Gérôme, *Pollice Verso*** | **11.22** | 98.4 | 77.5 | **39.9** | **13.27** | **46.1** | **1.20** | 66.8 | 0.86 |
| Shishkin, *Rye* | 6.36 | 64.1 | 44.6 | 22.2 | 4.74 | 31.8 | 1.39 | 65.0 | 1.43 |
| Zorn, *Midsummer Dance* | 3.32 | 64.3 | 17.3 | 3.0 | 0.21 | 13.3 | 2.16 | 81.2 | 0.63 |
| Levitan, *Vladimirka* | 3.01 | 39.1 | 17.7 | 6.1 | 0.94 | 8.7 | 1.38 | 54.8 | 0.42 |

### 2.3 Lost-and-found, measured: one class, not two

The Tonalism round established that Tonalist "soft edges" are low-ΔE narrow transitions rather than
wide ones. **The same holds for Realism, and this corpus adds the shape of the shift.**

- **Few hard edges: confirmed.** Hard edges occupy `20.20% × 38.39% = 7.8%` of a realist canvas's
  spans against `33.24% × 50.82% = 16.9%` of a photograph's — **2.2× fewer.** `[verified]`
- **Mostly soft or lost: refuted, again.** Soft (width 3–6 px) is **9.51%** on the canvases and
  **11.11%** on the photographs — the canvases have *fewer*, and the difference is nothing
  (t = −0.51). Lost (≥ 6 px) is 0.0% on both. Median width 1.75 vs 1.55, +0.20 px, t = 1.48.
  `[verified]`
- **What actually moves is the firm class.** 52.10% vs 37.99%, **t = +3.83, the largest in the
  table.** A realist boundary is a firm boundary: a definite transition two pixels wide carrying
  half the contrast a photograph would put across it. `[verified]`

**Consequence for the pipeline.** The target is not "blur the edges". It is
**(a) take a third off the contrast carried across a boundary, and (b) move the width distribution
by one class, from a step to a two-pixel transition.** A guided filter does both if it is allowed
to; §6 measures how much of it survives the quantiser.

### 2.4 How much is tonal compression? About a third — and that is the difference from Tonalism

The Tonalism round's most transferable result is that not normalising for tonal range manufactured
an 8.8× difference that was four-fifths artefact. Applied here: `[verified]`

| | canvases | photographs | raw ratio | normalised ratio |
|---|---|---|---|---|
| D10 | 10.33 → 11.15 | 24.14 → 20.27 | 0.428 | **0.550** |
| D20 | 2.09 → 2.42 | 9.72 → 6.50 | 0.215 | **0.373** |
| mean g | 4.78 → 4.93 | 7.44 → 6.20 | 0.643 | 0.796 |

In log terms the D20 gap shrinks from 4.65× to 2.69× — **64% of it survives.** The D10 gap survives
at t = −2.07 where Tonalism's vanished outright (22.43 vs 23.69, indistinguishable).

**The reason is measured and simple: realist canvases are not tonally compressed.** L\* 5–95 spread
**60.91 vs 70.20 (0.868)**, against Tonalism's 39.5 vs 67.2 (0.588). Tonalism buys its softness
with its value range; **Realism does not, so its softness has to be real.** That makes the edge
lever load-bearing for this style in a way it is not for Tonalism, where three of four tracks found
the answer was in the Lab remap.

### 2.5 Realism proper against the academics — the split the literature predicts

Splitting the corpus into the four academic/naturalist works (Bouguereau, Gérôme, both
Bastien-Lepages) and the other seventeen: `[verified]`

| | Realism proper (17) | academic / naturalist (4) | photographs (17) |
|---|---|---|---|
| D10 | **7.59** md 6.94 | 21.97 md 22.72 | 24.14 md 20.19 |
| D20 | **1.31** md 1.12 | 5.45 md 4.19 | 9.72 md 6.76 |
| Edge-span share | **16.50** | 35.93 | 33.24 |
| Hard-width share | **36.78** | 45.24 | 50.82 |
| Median width | 1.75 | 1.71 | 1.55 |
| centre÷outer D10 | 2.22 md 1.71 | **1.02 md 0.99** | 1.58 md 1.41 |

**On every edge-density measure the academic group is statistically a photograph.** That is the
Wikipedia claim in §1 turned into numbers, and it has a direct consequence for the app: **the
"Realism" row has to decide which of the two it is naming.** If it means Courbet, Millet, Homer,
Repin and Eakins, its target is D20 ≈ 1.3% and the shipped row is 7.5× over it. If it means Gérôme
and Bouguereau, the target is the photograph and the row should do nothing at all — but then it is
not one style, because those two sit at opposite ends of the corpus (D20 13.27 and 0.13).

`[verified for the numbers; n = 4 in the academic group, and it is driven by Gérôme and the
Bastien *Joan of Arc*. Treat as suggestive.]`

---

## 3. Is there an edge hierarchy on the canvases? No more than on a photograph

Radial quartiles of distance from the image centre, normalised by the half-diagonal, pooled over
each corpus: `[verified]`

| band (0 = centre) | canvases D10 | canvases D20 | photos D10 | photos D20 |
|---|---|---|---|---|
| 0 | 12.66% | 2.81% | 27.43% | 11.24% |
| 1 | 10.23% | 2.14% | 26.33% | 10.87% |
| 2 | 10.71% | 2.16% | 22.78% | 9.04% |
| 3 | 8.51% | 1.50% | 21.43% | 8.34% |
| **centre ÷ outer** | **1.49** | **1.87** | **1.28** | **1.35** |

Per image, the centre÷outer D10 ratio: **canvases median 1.48, mean 1.99 (SD 1.55); photographs
median 1.41, mean 1.58 (SD 0.98); t = 1.01, not significant.** On D20, canvases median 2.51 against
photographs' 1.53, t = 0.47. **Six of 21 canvases run the hierarchy backwards** (Levitan 0.42, Zorn
0.63, Bastien *Hay making* 0.62, Millet *Angelus* 0.68, Eakins *Gross Clinic* 0.78, Gérôme 0.86).

**Ruling: edge hierarchy is not Realism's device, and the reason is stronger than Tonalism's.** The
Tonalism round rejected it because five of fifteen canvases ran it backwards while the corpus still
showed a 2.4× differential over photographs. Here the differential is **1.16× on D10 and 1.39× on
D20 and neither is significant** — a photograph already has as much focal edge hierarchy as a
realist canvas, because photographers centre their subjects and so did the painters. The queued
focal-threshold build item **gets no support from this round**, and §10 records that. (The two
differentials are 1.49 ÷ 1.28 = **1.16×** on D10 and 1.87 ÷ 1.35 = **1.39×** on D20, against the
Tonalism round's 2.4× on the same statistic.)

A curiosity worth one line: the academic four have centre÷outer median **0.99** — no hierarchy at
all — while Realism proper has 1.71. If anything the device belongs to the rougher, less finished
half of the corpus, which is the opposite of the way the doctrine is usually taught. `[verified,
n = 4]`

---

## 4. What the shipped row actually does to edges

The audited row is unchanged from `HEAD`: MarkScale **1.0**; pre-map `EdgePreservingFloor` at its
own declared defaults, **strength 1.0 / edge 0.05**, with no `WithDefaults` call; `IdentityRemap`;
`KeepAllCandidates`; `NearestQuantiser`; **slot 5 empty**. `[verified — `StyleRegistry.cs:33-40`,
`EdgePreservingFloor.cs:31-35`, `GuidedFilter.cs:38`]` The declared ranges are strength 1.0–5.0 and
edge 0.01–0.30, so **Realism runs the app's only mandatory stage at the bottom of both.**

> **Working-tree caveat.** `git diff` shows `StyleRegistry.cs` changed for Tonalism, Fauvism and
> Post-Impressionism and **not for Realism**. Realism's figures below are `HEAD` figures; the other
> four rows' are working-tree figures, which is why Tonalism, Fauvism, Post-Impressionism and
> Abstract all report 0.00% sub-mark share — all four now register the rewritten
> `SmallRegionMerge`. `[verified]`

### 4.1 The five styles on 17 photographs, each at its own default mark `[verified]`

| | **Realism** | Tonalism\* | Fauvism\* | Post-Imp.\* | Abstract\* | *canvas target* |
|---|---|---|---|---|---|---|
| MarkScale | **1.0** | 1.2 | 1.3 | 1.6 | 2.5 | — |
| mark (px, 800-px short edge) | **5.00** | 6.00 | 6.50 | 8.00 | 12.50 | — |
| Four-connected regions | **299,218** | 3,358 | 1,959 | 2,008 | 187 | — |
| **Pixels below own mark²** | **52.69%** | 0.00% | 0.00% | 0.00% | 0.00% | — |
| Boundary pairs per 1000 px | **967.7** | 244.6 | 168.4 | 197.4 | 54.5 | — |
| Mean boundary ΔE | 9.24 | 8.44 | 20.82 | 13.28 | 22.80 | — |
| Boundary pairs at ΔE ≥ 10 | 28.5% | 22.3% | 42.7% | 36.0% | 90.3% | — |
| Distinct colours | 1,075 | 324 | 300 | 360 | 9 | — |
| D10 / D20 | 22.33 / 9.76 | 4.64 / 1.90 | 5.50 / 4.21 | 5.94 / 3.23 | 4.23 / 2.23 | **10.33 / 2.09** |
| Edge-span share | 30.75 | 11.69 | 15.10 | 14.59 | 12.50 | **20.20** |
| Width hard / firm / soft | 60.9 / 34.5 / **4.6** | 89.0 / 10.8 / 0.2 | 96.4 / 3.5 / 0.1 | 94.8 / 5.1 / 0.0 | 97.8 / 2.2 / 0.0 | **38.4 / 52.1 / 9.5** |
| **Median edge width** | **1.33** | 1.00 | 1.00 | 1.00 | 1.00 | **1.75** |
| L\* 5–95 spread | **64.1** | 45.6 | 47.1 | 56.9 | 49.9 | **60.9** |
| Range-normalised D20 | 8.02 | 3.40 | 4.50 | 3.58 | 2.74 | **2.42** |

\* working tree, all four with `SmallRegionMerge` registered.

Three readings.

**Realism is the only row in the app whose median edge is not a bare step.** Median width 1.33
against exactly 1.00 for every other style; soft-width share 4.6% against 0.0–0.2%. It is also the
only row that keeps the photograph's tonal range (64.1 against a canvas 60.9). **On the two
statistics where Realism is the app's best row it is best by a wide margin, and it gets there by
doing nothing.**

**And it is the worst row in the app on density by an order of magnitude.** 52.69% of its pixels sit
in regions smaller than one of its own brushmarks squared, and 968 differing four-neighbour pairs
per 1000 pixels — where every other style now sits at exactly zero and 55–245. That is track 3's
measurement and I record it only because slot 5 is where the two tracks collide (§7).

**On contrast it is 4.7× over the canvas target.** D20 9.76 against 2.09; range-normalised 8.02
against 2.42.

### 4.2 The z-distance, and the number that should decide this round `[verified]`

Distance to the realist-canvas envelope: the RMS of seven z-scores {D2, D5, D10, D20, edge-span
share, hard-width share, median width}, each in units of the **canvas corpus's own SD**. Computed
on the mean statistics over the 17 photographs.

The calibration matters and it is cheap: **measure each individual realist canvas against its own
corpus and the median is z = 0.58** (range 0.23 for the Courbet *Atelier* to 2.57 for the Gérôme).
**Each individual photograph against the canvas envelope has median z = 1.13.** So anything under
about 0.8 is inside the realist spread and anything over about 1.2 is at photograph distance.

| variant | **z** | below mark² |
|---|---|---|
| *median individual realist canvas* | *0.58* | — |
| **floor s1 ε 0.30** | **0.548** | 42.61% |
| floor s1 ε 0.20 | 0.634 | 44.44% |
| **floor s2 ε 0.15** | **0.660** | 35.60% |
| floor s1 ε 0.15 | 0.752 | 46.11% |
| floor s2 ε 0.10 | 0.753 | 38.33% |
| floor s3 ε 0.10 | 0.757 | 32.20% |
| floor s5 ε 0.10 | 0.851 | 25.66% |
| floor s5 ε 0.05 | 0.960 | 31.04% |
| floor s1 ε 0.10 | 0.978 | 48.58% |
| floor s3 ε 0.05 | 1.011 | 39.03% |
| floor s1 ε 0.08 | 1.117 | 49.91% |
| floor s2 ε 0.05 | 1.133 | 44.88% |
| *median individual photograph* | *1.13* | — |
| **unfiltered photograph** | **1.293** | — |
| **floor s1 ε 0.05 — as shipped** | **1.415** | 52.69% |
| floor s1 ε 0.02 | 1.820 | 57.83% |
| **+ `SmallRegionMerge`, any ε 0.05–0.30** | **1.90 – 2.08** | **0.00%** |

**The shipped Realism row is further from a realist canvas than an unprocessed photograph is.**
That is the answer to "does doing least land closest?" — no. Doing least is not doing nothing,
because the quantiser is not nothing, and §5 measures what it does.

The Tonalism round found Realism landing *closer to a Whistler nocturne* than the Tonalism row did,
and read that as "does nothing can beat doing the wrong thing". On its own target the reading
inverts: **Realism does the wrong thing too, quietly, in slot 4.**

---

## 5. What the quantiser does to an edge

Realism has no post-map stage, so the nearest-candidate match writes the final boundaries. Measured
on 17 photographs in three stages — raw, after the **real `EdgePreservingFloor`** run on the real
buffer at Realism's registered values, and after the **real `StylePipeline.Render`**:
`[verified — computed 2026-07-31]`

| | raw photograph | after the floor (ε 0.05) | rendered | *canvas target* |
|---|---|---|---|---|
| **Median edge width** | 1.550 | **1.840** | **1.326** | *1.75* |
| Hard-width share | 50.82% | **42.11%** | **60.94%** | *38.39%* |
| Soft-width share | 11.11% | **19.77%** | **4.58%** | *9.51%* |
| D10 | 24.14 | 16.53 | 22.33 | *10.33* |
| D20 | 9.72 | 6.68 | 9.76 | *2.09* |
| Boundary pairs per 1000 px | — | 1691.6 | **967.7** | — |
| Mean ΔE across a surviving boundary | — | 4.39 | **9.24** | — |

**The floor does exactly what a realist edge treatment should, and the quantiser undoes it and
overshoots.** The floor moves median width 1.55 → 1.84, past the canvas target of 1.75; moves 8.7
points of edges out of hard and into firm and soft; and takes a third off D10 and D20. Then the
quantiser puts width back to **1.33 — below the raw photograph's 1.55** — pushes hard-width share
to 60.9% (above the photograph's 50.8%) and returns D10 and D20 to within a few percent of the
values the raw photograph had.

The mechanism, measured pair by pair over every four-adjacent pixel pair: `[verified]`

- **43.5% of the floor's boundaries are erased outright.** Boundary pairs per 1000 px go 1691.6 →
  967.7. Nearest-candidate matching manufactures lost edges wholesale — the accident the parent
  report's §1.1 predicted, "two neighbouring photo colours that snap to the same mixture".
- **At the boundaries that survive, mean ΔE goes 4.39 → 9.24, ×2.11.**
- **Amplification is not uniform.** Over all pairs, mean output ΔE ÷ mean input ΔE is **×1.328**; at
  pairs that were already an edge (input ΔE ≥ 10) it is **×1.081**. **The quantiser leaves real
  edges roughly alone and doubles everything that was not an edge.**
- **5.60% of all adjacent pairs go from ΔE < 2 to ΔE ≥ 5, and 5.20% go from below ΔE 10 to at or
  above it.** Those are steps in places the source had a gradient. That is the stair-step, and it is
  **visible**: rendered at 1:1, the sky of a mountain photograph becomes flat bands of blue at every
  ε tested (§9 verification note).

**So the answer to "does the quantiser harden edges, soften them, or introduce a stair-step" is all
three at once, and the net is hardening.** It deletes 43% of the boundaries in the picture, doubles
the contrast at the ones it keeps, and converts smooth ramps into plateaux with hard risers. Median
edge width falls, not rises, because a ramp that was resolved into three small steps becomes one
large one.

**This is why raising the floor's ε works on this row.** The floor cannot make the quantiser gentler,
but it can hand it fewer near-threshold gradients to break: at ε 0.30 the floor delivers median
width 3.50 in the buffer and the quantiser cuts it to 1.71, which is the canvas target.

### 5.1 The one result that cuts against §6's recommendation `[verified]`

Repeating the three-stage measurement at ε 0.15 and 0.30:

| | ε 0.05 | ε 0.15 | ε 0.30 |
|---|---|---|---|
| Median width, raw → floor → **render** | 1.550 → 1.840 → **1.326** | 1.550 → 2.695 → **1.549** | 1.550 → 3.502 → **1.709** |
| Excess width over a step, surviving the quantiser | 0.326 / 0.840 = **39%** | 0.549 / 1.695 = **32%** | 0.709 / 2.502 = **28%** |
| Hard-width share, raw → floor → render | 50.8 → 42.1 → **60.9** | 50.8 → 23.1 → **49.5** | 50.8 → 9.8 → **42.4** |
| Boundaries erased by the quantiser | 43.5% | 49.9% | 53.0% |
| Contrast amplification, all pairs | ×1.328 | ×1.486 | ×1.614 |
| **Pairs going ΔE < 2 → ΔE ≥ 5 (the stair-step)** | **5.60%** | **6.94%** | **7.65%** |

**Raising ε makes the banding worse, monotonically, even as it makes every other statistic better.**
The mechanism is not subtle: a stronger floor flattens more of the picture, so more pairs enter the
"was flat" population and a larger share of them land either side of a candidate step. The
quantiser's overall amplification rises with ε for the same reason.

That is a real cost of pick 1 and it is the reason pick 4 exists. It is also a limit on how far the
slot-1 lever can be pushed: **ε cannot be raised past the point where the flat areas it creates band
visibly**, and that point is a property of the candidate set, not of the filter.

**A consequence for the app's own documentation.** `GuidedFilter`'s doc comment says its 0.05
default "sits above the sensor noise measured on ordinary photographs and below the contrast of any
edge a painter would treat as an edge". `[verified — `GuidedFilter.cs:30-38`]` The first half is
supported; the second is not. At ε 0.05 the finished picture carries **9.76% of its pixels at
ΔE ≥ 20** against a realist canvas's 2.09%. Whatever 0.05 sits below, it is not the contrast of an
edge a realist painter would keep.

---

## 6. The floor's two knobs at Realism's settings

### 6.1 The ε ladder, rendered `[verified]`

Strength fixed at Realism's declared 1.0, everything else at its registered defaults, means over 17
photographs, measured on the finished render.

| ε | below mark² | bound/1000 | mean ΔE | hard ≥10 | D10 | D20 | spans | hard/firm/soft | med W | **z** |
|---|---|---|---|---|---|---|---|---|---|---|
| 0.01 | 61.60% | 1120.9 | 10.04 | 33.0% | 29.32 | 13.09 | 35.26 | 66.6/30.7/2.7 | 1.24 | 1.952 |
| 0.02 | 57.83% | 1059.2 | 9.95 | 32.3% | 27.29 | 12.34 | 33.86 | 65.4/31.5/3.1 | 1.25 | 1.820 |
| **0.05 (shipped)** | **52.69%** | **967.7** | **9.24** | **28.5%** | **22.33** | **9.76** | **30.75** | **60.9/34.5/4.6** | **1.33** | **1.415** |
| 0.08 | 49.91% | 916.2 | 8.58 | 25.2% | 18.82 | 7.83 | 28.82 | 56.7/37.2/6.0 | 1.41 | 1.117 |
| 0.10 | 48.58% | 890.9 | 8.24 | 23.5% | 17.09 | 6.92 | 27.96 | 54.3/38.7/7.0 | 1.45 | 0.978 |
| 0.15 | 46.11% | 846.6 | 7.61 | 20.5% | 14.21 | 5.40 | 26.66 | 49.5/41.5/9.0 | 1.55 | 0.752 |
| 0.20 | 44.44% | 819.2 | 7.19 | 18.5% | 12.45 | 4.42 | 25.97 | 46.2/43.2/10.6 | 1.62 | 0.634 |
| **0.30** | **42.61%** | **789.9** | **6.69** | **16.0%** | **10.54** | **3.24** | **25.24** | **42.4/44.7/12.9** | **1.71** | **0.548** |
| *canvas target* | — | — | — | — | *10.33* | *2.09* | *20.20* | *38.4/52.1/9.5* | *1.75* | *0.58* |

**ε is monotonic and it runs the whole way to the target.** At 0.30 — the top of the declared range
— D10 lands at 10.54 against 10.33, median width at 1.71 against 1.75, hard-width share at 42.4%
against 38.4%. **There is no interior optimum: the closest setting to a realist canvas is the
largest ε the stage allows.** That is a different shape of answer from Tonalism's, where the ladder
had a ceiling at 0.10, and §6.3 explains why the two are compatible.

### 6.2 Strength `[verified]`

| variant | below mark² | D10 | D20 | hard/firm/soft | med W | z | passes |
|---|---|---|---|---|---|---|---|
| s1 ε 0.05 (shipped) | 52.69% | 22.33 | 9.76 | 60.9/34.5/4.6 | 1.33 | 1.415 | 1 |
| s2 ε 0.05 | 44.88% | 17.81 | 7.73 | 58.0/36.0/6.0 | 1.38 | 1.133 | 2 |
| s3 ε 0.05 | 39.03% | 14.89 | 6.54 | 56.4/36.8/6.8 | 1.41 | 1.011 | 3 |
| s5 ε 0.05 | 31.04% | 11.60 | 5.14 | 55.1/37.4/7.6 | 1.44 | 0.960 | 5 |
| **s1 ε 0.30** | 42.61% | 10.54 | 3.24 | **42.4/44.7/12.9** | **1.71** | **0.548** | **1** |
| s2 ε 0.15 | 35.60% | 9.62 | 3.30 | 44.8/42.5/12.7 | 1.66 | 0.660 | 2 |
| s3 ε 0.10 | 32.20% | 9.70 | 3.65 | 47.9/40.9/11.2 | 1.59 | 0.757 | 3 |
| s5 ε 0.10 | 25.66% | 7.60 | 2.34 | 48.1/39.8/12.1 | 1.60 | 0.851 | 5 |

**The two knobs do different jobs and the split is cleaner here than in Tonalism.**

- **Strength buys density.** 1 → 5 at ε 0.05 takes sub-mark share 52.69% → 31.04%, five guided-filter
  passes for 21.7 points.
- **ε buys edge quality.** 0.05 → 0.30 in the same single pass takes hard-width share 60.9% → 42.4%
  and median width 1.33 → 1.71. **Five passes of strength move the width mix by 5.8 points; one
  scalar change to ε moves it by 18.5.**
- **They are not substitutes, and each is better than the other at its own job.** Five passes at
  ε 0.05 reach 31.04% sub-mark share against one pass at ε 0.30's 42.61% — strength genuinely wins on
  density — while landing at z 0.960 against 0.548, because strength leaves the picture hard: 55.1%
  hard-width share against 42.4%, median width 1.44 against 1.71. **This is the fourth independent
  confirmation that ε is the guided filter's edge parameter**, and the first on a style with no Lab
  remap in the way.
- **The cheapest combination on the Pareto front is s2 ε 0.15** — two passes, z 0.660, sub-mark
  share 35.60%. **s3 ε 0.10** is z 0.757 at 32.20%.

### 6.3 Where a pre-map filter must be judged — a correction to the method, not to the ruling

The Tonalism round's §5.2 ranked filters by their distance to the canvas statistics **measured on
the pre-map buffer**, before the quantiser, and concluded that ε 0.15 and 0.30 "sit *behind an
unfiltered photograph*". Its README's what-not-to-build list carries that forward as a ceiling.

Reproduced here on a realist corpus, **the buffer domain says exactly the same thing, and the
rendered domain says the opposite:** `[verified]`

| | z **in the buffer** | z **rendered** | med W buffer | med W rendered |
|---|---|---|---|---|
| unfiltered photograph | 1.293 | 1.293 | 1.55 | 1.55 |
| floor s1 ε 0.05 | **0.903** (best) | **1.415** (worst but one) | 1.84 | 1.33 |
| floor s1 ε 0.08 | 0.892 | 1.117 | 2.11 | 1.41 |
| floor s1 ε 0.15 | 1.408 (behind the photo) | 0.752 | 2.69 | 1.55 |
| floor s1 ε 0.30 | 2.347 (far behind) | **0.548** (best) | 3.50 | 1.71 |
| Gaussian radius 2 | 0.875 | — | 2.27 | — |
| Gaussian radius 5 | 2.922 | — | 4.00 | — |

**About a third of the filter's buffer-domain edge widening survives the quantiser** — excess width
over a pure step goes 0.84 → 0.33 at ε 0.05 (39%), 1.70 → 0.55 at ε 0.15 (32%), and 2.50 → 0.71 at
ε 0.30 (28%) (§5.1). A buffer-domain comparison therefore **overstates how much softening a pre-map
filter delivers by roughly 3×**, and it reverses the ordering of the whole ladder. The survival
fraction also falls as the filter gets stronger, so the overstatement is worst exactly where the
ladder is being extended.

**This does not overturn Tonalism's ε ≤ 0.10 ceiling**, which its §5.3 supports independently from
the rendered output — that style also runs `ToneAndChromaRemap` with contrast below 1 (0.55 at
`HEAD`, 0.80 in the working tree), which takes contrast down pointwise, so its floor has much less
work left to do. **What it overturns is the generality of the method.** Judge a slot-1 filter after
slot 4, always. Filed as correction 1 in §8.

---

## 7. Slot 5, the merge, and the collision with track 3

Realism is now the app's only row with an empty post-map slot, and at 52.69% sub-mark share it is
the obvious candidate for the stage every other style just gained. **Measured, it is the single
worst thing that could be done to this row's edges.** `[verified]`

| variant | below mark² | bound/1000 | mean ΔE | D10 | D20 | hard/firm/soft | med W | **z** |
|---|---|---|---|---|---|---|---|---|
| shipped (no slot 5) | 52.69% | 967.7 | 9.24 | 22.33 | 9.76 | 60.9/34.5/4.6 | 1.33 | 1.415 |
| + merge, ε 0.05 | **0.00%** | 241.4 | **14.12** | 7.66 | 4.17 | **94.1/5.8/0.1** | **1.00** | **2.080** |
| + merge, ε 0.10 | 0.00% | 261.8 | 12.76 | 8.01 | 4.11 | 92.5/7.4/0.1 | 1.00 | 2.024 |
| + merge, ε 0.20 | 0.00% | 277.4 | 11.76 | 8.27 | 3.93 | 90.6/9.3/0.1 | 1.00 | 1.969 |
| + merge, ε 0.30 | 0.00% | 281.5 | 11.50 | 8.33 | 3.97 | 89.7/10.2/0.1 | 1.00 | 1.950 |
| + merge, s2 ε 0.15 | 0.00% | 282.3 | 10.50 | 7.30 | 3.35 | 86.8/13.0/0.2 | 1.01 | 1.899 |
| *canvas target* | — | — | — | *10.33* | *2.09* | *38.4/52.1/9.5* | *1.75* | *0.58* |

Four things, and the fourth is the one to act on.

1. **The rewritten merge works.** Exactly 0.00% on all 17 photographs in one pass, confirming the
   Tonalism round's finding on a fifth style.
2. **It raises mean boundary ΔE 9.24 → 14.12**, on the row that is already 4.7× over the canvas
   target for high-contrast edges. The Post-Impressionism round predicted this mechanism (the merge
   deletes weak boundaries and keeps strong ones); on Realism it is 53% rather than that round's
   35%, because Realism has far more weak boundaries to delete.
3. **It collapses the width mix to 94.1 / 5.8 / 0.1 and the median width to exactly 1.00** —
   Realism becomes the sixth flat-plane style. **Raising ε does not rescue it**: across the whole
   0.05–0.30 range the median width stays 1.00 and z stays between 1.90 and 2.08, worse than 14 of
   the 17 photographs.
4. **Halving its threshold does not help either.** `SmallRegionMerge` declares no parameters
   (`SmallRegionMerge.cs:27` computes `ceil(MarkPixels²)` from the context), so the only way to give
   it a smaller threshold without reimplementing it is to hand the whole render a smaller mark. At
   MarkScale 0.7 the floor radius is unchanged — `PalettePhotoConverter.FloorRadius` rounds both
   3.5/2 and 5.0/2 to **2** — so only the merge's threshold moves, from 25 px to 13 px:

   | | below (real mark²) | mean ΔE | D10 | D20 | hard/firm/soft | med W |
   |---|---|---|---|---|---|---|
   | no merge, ε 0.05 | 52.69% | 9.24 | 22.33 | 9.76 | 60.9/34.5/4.6 | **1.33** |
   | threshold 13 px, ε 0.05 | **5.83%** | 13.56 | 8.54 | 4.58 | 91.7/8.2/0.2 | **1.00** |
   | threshold 13 px, ε 0.30 | 6.99% | 10.44 | 8.79 | 3.98 | 84.4/15.2/0.3 | **1.02** |
   | threshold 25 px, ε 0.05 | 0.00% | 14.12 | 7.66 | 4.17 | 94.1/5.8/0.1 | **1.00** |
   | *canvas target* | — | — | *10.33* | *2.09* | *38.4/52.1/9.5* | *1.75* |

   Half the threshold buys nine-tenths of the paintability and costs the same edges. `[verified]`
   **There is no threshold at which an area opening stops being a hard-edge operator**, because it
   writes a neighbour's exact index rather than an interpolated one, so every boundary it creates is
   a step and every intermediate step in a ramp is a deletion candidate.

**In fairness to the merge, two of the seven z components drive its score**: D2, which every strong
simplification destroys and which no variant in this report recovers (the best is 55.06 against a
canvas 71.66), and hard-width share, which is the merge's own signature. On D10 and D20 the merge is
*closer* to the canvas target than the shipped row is — 8.01 / 4.11 against 22.33 / 9.76. **The case
against it is a width case, not a contrast case.**

**Rendered and looked at, on five photographs, the merge is worse than its numbers.** On a flock of
sheep it turns individual animals into salmon-pink blobs; on a market interior it produces flat
cyan and white patches in the tiled wall that read as artefacts; on a mountain landscape it turns a
conifer stand into a lumpy field. Ten minutes of looking agreed with the z-distance, which is not
always how that goes.

**The collision with track 3, stated plainly.** Track 3 owns fragmentation and will almost
certainly recommend registering `SmallRegionMerge` on Realism, because on that measurement it is a
one-line fix for the app's worst number. On my measurement it is the largest single move away from
the realist-canvas envelope available in the pipeline. **Both are true.** The resolution is not a
compromise setting — the ladder above shows there isn't one — it is either:

- **accept that Realism is the unpaintable row**, on the grounds that it is the photographic default
  and paintability belongs to the styles that claim it; or
- **give slot 5 an operator that does not step** — the merge's job done by a *selection* rule that
  prefers the second-nearest candidate rather than the neighbour's exact colour, which is still
  category B under the invariant table and is not written; or
- **buy the density in slot 1 instead**, which is what §6.2 measures: s5 ε 0.10 reaches 25.66%
  sub-mark share at z 0.851 — half the fragmentation for a quarter of the edge damage, at five
  guided-filter passes.

Track 3 should have the fragmentation numbers; I have not tried to arbitrate them. What I claim is
that **the edge cost of the obvious fix has now been measured and it is large.**

---

## 8. Where this corrects or extends prior research

**Corrects:**

1. **A pre-map filter must be judged after the quantiser, not at the buffer.** §6.3. The Tonalism
   round's §5.2 z-distance table — and the "do not push ε past 0.10" line its README carries into
   what-not-to-build — is computed on the filtered photograph before mapping. Reproduced here, the
   buffer domain ranks ε 0.05 best and ε 0.30 far behind an unfiltered photograph; the rendered
   domain ranks them in exactly the opposite order. **Roughly a third of the filter's buffer-domain
   edge widening survives to the finished picture.** Tonalism's own ceiling stands on its §5.3
   rendered evidence; the method does not transfer.
2. **`GuidedFilter`'s doc comment overstates its default.** §5. "Below the contrast of any edge a
   painter would treat as an edge" — at ε 0.05 the finished render carries 9.76% of its pixels at
   ΔE ≥ 20 against a realist canvas's 2.09%, and sits further from the canvas envelope than the
   unfiltered photograph. `[verified against `GuidedFilter.cs:30-38`]`
3. **The parent report's edge-hierarchy lever gets no support from a second movement, and this
   round's negative is stronger than the first.** §3. Centre÷outer high-contrast edge ratio is
   1.48 on realist canvases against 1.41 on photographs, t = 1.01. The Tonalism round found the
   device real but not that movement's signature; here it is not distinguishable from what a
   camera does by itself. **Two rounds, two movements, no support. It should come off the queued
   build list unless a third round finds a movement that has it.**

**Extends / confirms:**

- **"Soft edge means low contrast, not wide transition" reproduces on a second movement and a
  second corpus.** §2.3. Median width 1.75 vs 1.55 (t = 1.48, n.s.), soft-share 9.51 vs 11.11
  (t = −0.51). The Tonalism round measured 1.66 vs 1.36. **This is now a settled negative and the
  "what not to build" entry for width-widening stages should be promoted to the parent README.**
- **The range-normalisation method transfers and its answer is style-specific.** §2.4. It dissolved
  four-fifths of Tonalism's edge gap and only a third of Realism's, because realist canvases keep
  87% of a photograph's tonal range where Tonalist canvases keep 59%. **The correct reading of the
  Tonalism result is not "edge gaps are usually artefacts" but "check the tonal range first".**
- **ε is the guided filter's edge parameter, for the fourth time.** §6.2, and the first measurement
  on a row with no Lab remap competing with it.
- **The boundary-statistics method reproduces for a fourth session.** My cross-style table uses the
  Post-Impressionism round's definitions unchanged and the Tonalism round's corpus protocol; the
  four working-tree styles reproduce that round's post-fix ordering.
- **The rewritten `SmallRegionMerge` reaches exactly 0.00% in one pass on a fifth style.** §7.
- **The four-category invariant table needs no change.** Every pick in §9 is category A (pre-map).

**Where I could not settle a question:**

- Whether the academic/naturalist split in §2.5 is real or four pictures. It predicts correctly from
  the literature, which is encouraging, but n = 4 and two of the four drive it.
- Whether the third of buffer-domain softening that survives the quantiser is a constant or depends
  on the candidate-set density. One palette, one measurement.

---

## 9. Picks, ranked by payoff ÷ cost

Line counts are C#-from-scratch estimates in the style of `Imaging/Styles/Stages/`, excluding UI.

### 1. Give Realism a floor `edge` override — the whole of this round in one line

**Slot 1. Three lines in `StyleRegistry.cs`.** Realism inlines `new EdgePreservingFloor()` in its
slot-1 array and has no `WithDefaults` call at all, so it needs the same local-variable shape the
other four styles already use — which is exactly what `BuildAll`'s own doc comment says the method
exists for:

```csharp
var realismFloor = new EdgePreservingFloor();
var realism = new StyleDefinition(
    "Realism", 1.0, new IPreMapStage[] { realismFloor }, /* … */)
    .WithDefaults((realismFloor, "edge", 0.15));
```

Measured over 17 photographs (§6.1): z-distance to the realist-canvas envelope **1.415 → 0.752**,
median edge width **1.33 → 1.55** (target 1.75), hard-width share **60.9% → 49.5%** (target 38.4%),
D20 **9.76 → 5.40** (target 2.09), mean boundary ΔE 9.24 → 7.61, sub-mark share 52.69% → 46.11% —
**at zero extra cost**, since ε is a scalar inside the same single pass.

**Why 0.15 and not 0.30, when 0.30 measures better.** Three reasons, and the third is a measurement
rather than a judgement. 0.30 is the top of the declared range, which leaves the user no headroom in
the direction the evidence points. It is one setting on one palette, on a corpus whose own SD puts
z 0.55 and z 0.75 inside the same band. And **banding gets monotonically worse as ε rises** — the
share of adjacent pairs going from ΔE < 2 to ΔE ≥ 5 runs 5.60% / 6.94% / 7.65% at ε 0.05 / 0.15 /
0.30 (§5.1), and it is visible at 1:1 in any smooth sky. 0.15 captures two-thirds of the edge
improvement at two-thirds of the added banding. **If a second measurement confirms the ladder and
pick 4 lands, go to 0.20** (z 0.634). `[verified for the ladders; `[inferred]` for the choice within
them]`

**Verification.** Rendered D20 must come down into 3–6% and median edge width up into 1.5–1.7 on
real photographs, **not** on `Tests/Golden` — the gradient understates every density figure in this
report by about a factor of two and has produced a false conclusion in four consecutive rounds.
Regenerate `Tests/Golden/Realism.png` and look at it.

### 2. Do not register `SmallRegionMerge` on Realism until slot 5 has an operator that does not step

**Slot 5. Zero lines — a decision not to make a one-line change.**

§7. It buys 52.69% → 0.00% and costs z 1.415 → 2.02, a median edge width of exactly 1.00, and a
width mix of 94.1 / 5.8 / 0.1 against a canvas 38.4 / 52.1 / 9.5. No ε rescues it and no threshold
rescues it. **This is a genuine conflict with track 3 and it should be resolved by the round, not
by whichever report is read second.**

If the decision goes the other way, **pair it with pick 1 at ε 0.30** — that is the least-bad merged
variant measured (z 1.950) — and say in the row's comment that Realism has become a flat-plane
style.

### 3. Raise `strength` to 2 as well, if paintability has to move without slot 5

**Slot 1. A second line in the same `WithDefaults` call.**

s2 ε 0.15 is the cheapest point on the measured Pareto front: **z 0.660** and sub-mark share
**35.60%**, against the shipped 1.415 / 52.69%. Two guided-filter passes instead of one. s3 ε 0.10
(0.757 / 32.20%) and s5 ε 0.10 (0.851 / 25.66%) trade edge quality for density along a smooth
curve; pick a point on it once track 3's fragmentation target is known. `[verified]`

### 4. A dithering or second-nearest-candidate rule in slot 4, to break the stair-step

**Slot 4. ~40 lines for an ordered-dither quantiser; category B under the invariant table.**

§5 measures the defect this would address: **5.60% of adjacent pixel pairs go from ΔE < 2 to
ΔE ≥ 5** — steps the source did not have — and the banding is visible at 1:1 in any smooth sky at
every ε tested. Raising ε makes the bands wider and cleaner but does not remove them, because the
cause is that the candidate set is sparse relative to the gradient, not that the gradient is noisy.
The parent README's build order already holds error-diffusion or blue-noise dithering as item 6,
"contained inside the quantiser", and the Post-Impressionism round measured its gamut gain and
ruled it should be its own style row. **This is a second, independent reason to build it, and it is
the only lever measured in this round that attacks the stair-step at its source.** Ranked fourth
because it is the only pick here that is not a number in a table.

**Verification.** The flat-to-step share must fall below about 2% and the render must not gain
sub-mark regions — a dither at pixel scale would trade banding for fragmentation, which on this row
is the wrong trade.

---

## 10. What not to build

Each of these I went looking for and rejected. The parent, Abstract, Fauvism, Post-Impressionism
and Tonalism lists all still apply; these are additional.

- **A focal edge-threshold floor as Realism's device**, and — on the strength of two rounds now —
  **as any style's device**. §3. Centre÷outer ratio 1.48 on canvases against 1.41 on photographs,
  t = 1.01; six of 21 canvases inverted; and the academic four sit at a median of 0.99, no
  hierarchy at all. The Tonalism round costed the stage at ~120 lines and ranked it fourth of four;
  after a second null result it should come off the queue and be reopened only by a movement that
  measures as having it.
- **`SmallRegionMerge` on Realism at any ε, and at a halved threshold.** §7. Median edge width
  exactly 1.00 across the whole ε 0.05–0.30 range and at a 13 px threshold as well as a 25 px one.
- **A "soft edge" stage that widens transitions.** Realist boundaries are 1.75 px against
  photographs' 1.55 and have *fewer* soft-class edges than photographs do. This is the second
  movement in two rounds to refuse the width story; the entry belongs in the parent README now, not
  in a per-style list.
- **Judging a slot-1 filter on the pre-map buffer.** §6.3. It reversed the entire ε ranking.
- **Amplitude-spectrum slope as an acceptance test.** Carried forward unchanged from the Tonalism
  round; I did not measure it, deliberately, because a statistic the shipped Tonalism render already
  passes cannot gate anything, and the literature relayed in §1 says art and natural scenes share
  the property.
- **Treating "Realism" as a single edge target without deciding what it names.** §2.5. Courbet and
  Gérôme are at opposite ends of this corpus — D20 1.96 and 13.27 — and averaging them produces a
  target neither of them has.
- **Raising the floor's `strength` as the edge fix.** §6.2. Five passes move the width mix 5.8
  points; one ε change moves it 18.5.
- **Validating any of this on `Tests/Golden` or a synthetic gradient.** Fourth round running.
- **A "realist palette" or pigment preset.** Not my track, but the corpus offers no support: median
  C\* 12.09 on the canvases against 16.28 on the photographs, t = −1.45, the same null the Tonalism
  round found for its movement.
- **Automatic focal-point detection.** Unchanged, and strengthened from a new direction: on this
  corpus a centre-bias detector would be describing the photograph, not the painting.

---

## 11. Accuracy warnings

- **All canvas colorimetry is uncalibrated web reproduction of varnished, aged oil paint.** Varnish
  yellowing raises measured chroma and lowers measured lightness. The scale-free figures (the
  D-series as a *shape*, edge-span share, width in pixels at a normalised short edge, the radial
  ratios) are robust to that; the absolute L\* and C\* figures are not.
- **The canvas corpus's own SDs are large and the z-metric inherits them.** D20 has mean 2.09 and
  SD 2.96. A z-distance of 0.55 versus 0.75 is not a meaningful difference; a z of 0.55 versus 1.42
  is. Read the table in §4.2 in bands, not in order.
- **One photograph is a 7σ outlier.** `c10_cotton_xinjiang`, an aerial view of a cotton field, has
  mean g 22.77 and D20 49.5% — an all-over texture with no large forms — and its individual z
  against the canvas envelope is 7.31 against a corpus median of 1.13. It inflates every
  photographic *mean* in §2.2. The medians in the same table are the safer summary, and the
  conclusions do not turn on it: dropping it would widen the canvas-versus-photograph gap, not
  narrow it.
- **The academic/naturalist split in §2.5 is n = 4.**
- **The renders are one palette.** Six paints, `PigmentLibrary.Selectable` indices 0, 2, 6, 9, 11,
  18 — the same set `Tests/StyleTestFixtures` uses. A denser candidate set would reduce the
  stair-step in §5 and might change where on the ε ladder the optimum sits.
- **The visual pass is five photographs judged by one agent** at reduced size plus one 1:1 crop.
  It agreed with the statistics this time, which the Tonalism round warns is not the default.
- **Realism's own row is `HEAD`; the other four styles in the §4.1 table are working-tree.**
- **No primary Realist source on edges was found**, and §1 says so rather than dressing a modern
  blog as doctrine. The Ruskin passages are `[relayed]` from search excerpts, not read.

---

## 12. Verification debt

Ranked by how much clearing each would change a decision above.

1. **Render pick 1 at full size on a dozen subjects and look at it, beside a realist canvas.**
   Cheapest item here, it gates the top pick, and the Tonalism round records the same item
   overturning a recommendation that three pages of statistics supported. My visual pass was five
   photographs at reduced size.
2. **Settle the slot-5 conflict with track 3 inside this round.** §7. Two tracks, two correct
   measurements, opposite recommendations, one line of code. This is a decision, not work, but it
   needs both sets of numbers in one place.
3. **Whether ε 0.30 is genuinely better than 0.15 or the ladder is flat inside the corpus SD.**
   Pick 1 takes 0.15 on judgement, not measurement. One probe run over a second palette and a
   second photographic corpus would settle it.
4. **Whether the third of buffer-domain softening that survives the quantiser (§6.3) is stable.**
   The correction in §8 item 1 rests on it. Measured on one palette, one ε ladder.
5. **Solomon J. Solomon's *The Practice of Oil Painting* (1910) in full.** The archive.org text I
   retrieved returned no edge instruction at all, which is a surprising negative for a Royal
   Academician's manual and may be a retrieval failure rather than a fact about the book. If it does
   teach edge control, it is the closest thing to a Realist-era academic primary source and §1's
   "no doctrine" finding would need softening.
6. **Ruskin, *Modern Painters* IV, chapter IV.** Relayed from search excerpts; the archive.org
   plain text would not return the chapter through this environment. It is not a Realist source, so
   this changes framing rather than any number.
7. **A provenance-checked shared corpus, committed.** Carried forward from the Post-Impressionism
   and Tonalism rounds, where it was also debt 2 and 3 and also uncleared. Five consecutive rounds
   have each independently rediscovered contamination; this one found two more modes (§13).
8. **Whether the academic/naturalist split survives a larger sample.** §2.5, n = 4.
9. **Whether a dithered quantiser removes the stair-step without adding fragmentation.** Pick 4 is
   the only unmeasured item in §9.

---

## 13. Corpus provenance

Every image was downloaded through the Wikimedia Commons API with a declared user agent, cropped 3%
per edge, resampled to an 800-px short edge, and **inspected as a rendered contact sheet twice —
once as downloaded and once after cropping.**

### Paintings — 21 works by 13 painters `[verified]`

Courbet *A Burial at Ornans* (Google Art Project) · Courbet *L'Atelier du peintre* (Orsay) ·
Courbet *Bonjour Monsieur Courbet* (Musée Fabre) · Millet *The Gleaners* (Google Art Project) ·
Millet *The Angelus* (Google Art Project) · Eakins *The Gross Clinic* (Google Art Project) ·
Eakins *Max Schmitt in a Single Scull* · Eakins *Swimming* (1895) ·
Homer *Snap the Whip* (1872, the oil) · Homer *The Gulf Stream* (Metropolitan) ·
Homer *The Fog Warning* (Google Art Project) · Repin *Barge Haulers on the Volga* (Google Art
Project) · Bastien-Lepage *Joan of Arc* · Bastien-Lepage *Hay making* (Google Art Project) ·
Menzel *Das Eisenwalzwerk* (Google Art Project) · Bonheur *The Horse Fair* ·
Bouguereau *The Nut Gatherers* (1882) · Gérôme *Pollice Verso* · Shishkin *Rye* (Google Art
Project) · Zorn *Midsummer Dance* (Google Art Project) · Levitan *Vladimirka*.

**Rejections, which are the useful part:**

- **Homer, *Snap-the-Whip*, Cleveland Museum of Art 1942.1309** — downloaded on the strength of a
  museum accession number and a 7366×5334 TIFF, and seen on the contact sheet to be a **black-and-
  white wood engraving with a white paper margin and a printed caption**, not the oil. Replaced by
  the 1872 painting. **New contamination mode: a museum's own high-resolution file of the *print*
  after a painting, filed under the painting's title.**
- **Bastien-Lepage, *Joan of Arc*, MET DP-14201-049** — a Metropolitan file whose frame occupies
  perhaps 8% of each edge. A 3% crop does not remove a frame that thick, and the frame is gilt, so
  it would have added a bright hard border to every statistic. Replaced by `JoanOfArcLarge.jpeg`,
  which is the canvas alone. **The Tonalism round's 3% crop is not enough on its own; the contact
  sheet is what catches this.**
- Frame lips were visible at full frame on the Courbet *Burial* and the Bonheur; the 3% crop removes
  them and the post-crop sheet confirms none survive.

**Known composition, so it can be weighted:** 17 works of Realism proper against 4
academic/naturalist; 12 with figures at work or in company, 5 landscapes or seascapes, 2 interiors,
2 single figures. Three Courbets, three Homers, three Eakinses, two Millets, two Bastien-Lepages.
French, American, Russian, German, Swedish.

### Photographs — 17 images `[verified]`

Wikimedia Commons featured and quality pictures, chosen to match the paintings' subjects rather
than for photographic interest: figures at work outdoors and indoors, groups, herds, a boat at sea,
an industrial interior, market interiors, rural landscape, grain fields.

*20151030 Syrians and Iraq refugees arrive at Skala Sykamias Lesvos Greece 2* ·
*A girl set fire to cook breakfast by using a coal-filled clay pot* ·
*Bronze casting at Kunstgießerei München 01* ·
*Campamento de ganado de la tribu Mundari, Terekeka, Sudán del Sur, DD 36* · *Beignet maker* ·
*Banaue Philippines Ifugao-Tribesman-01* ·
*20101020 Sheep shepherd at Vistonida lake Glikoneri Rhodope Prefecture Thrace Greece* ·
*Cotton harvest in Xinjiang* · *2014 Fields Swaledale Gunnerside* ·
*2019 — Parc national des Pyrenees, Vallée de Gavarnie* · *Champ d'Orge carrée (Hordeum vulgare)* ·
*Bad Wimpfen — Streuobstwiese mit Raureif* · *2013 Cogden Bridge* · *Capri — 7224* ·
*A bad sales day* · *Crate maker* · *Harvesting seaweed in Jambiani*.

**Rejections:**

- ***Bearded man smoking pipe-3013924*** — a Commons **featured picture of people** that is a
  **monochrome** portrait. It carries no chroma and would have depressed every boundary-ΔE figure.
- ***Blind man carrying a paralysed man*** — a **19th-century black-and-white studio photograph**,
  filed in the same modern category. Rejecting it also avoided a subtler problem: it is a period
  print with its own edge characteristics, which is the "photographic control that is not a
  photograph" failure the Tonalism round flagged.
- ***2013 Cogden Bridge*** was inspected individually because its bracken reads as false-colour
  infrared on a thumbnail. At full size it is genuine autumn bracken, strongly saturated. **Kept
  and flagged**: it is the third-highest individual z in the photographic set.

---

## Appendix — how this was measured

One throwaway console project in the session scratchpad, assembly-named `PaintTranslator.Tests` so
the app's existing `InternalsVisibleTo` grant applies, with a `ProjectReference` to
`PaintTranslator.csproj`. **No repository file outside
`docs/research/painting-style/realism/` was modified, and nothing was staged or committed.**

**No pipeline stage was transcribed.** Every render came from the real `StylePipeline.Render` with a
`StyleDefinition` built from the real `StyleRegistry.ByName("Realism")` and adjusted through the
real `StyleDefinition.WithDefaults`, resolved through the real `StylePipeline.DefaultValues`. The
floor-only measurement in §5 calls the real `EdgePreservingFloor.Apply` on the real buffer with a
real `RenderContext`; the buffer ladder in §6.3 calls the real `GuidedFilter.Apply` and
`GaussianBlur.Apply` at the real `PalettePhotoConverter.FloorRadius(markPixels)`. Region counts and
sub-mark share come from the real `PaintabilityMetrics.CountRegions` and
`FractionInRegionsSmallerThan`. Lab conversion throughout is `PalettePhotoConverter.RgbToLab`. The
merge variants insert a real `SmallRegionMerge` instance into a copy of the real Realism record.

The palette is the six paints `Tests/StyleTestFixtures.SixPaints` uses (`PigmentLibrary.Selectable`
indices 0, 2, 6, 9, 11, 18). Note that `MixtureBuilder.RenderMixture` goes through `ToDisplayColor`,
so every figure here is measured on gamut-mapped 8-bit colour, a mean 3.35 ΔE from unmapped
spectral Lab; `SpectralRenderer`'s doc comment denies this and is wrong.

Boundary statistics follow the Post-Impressionism round's definitions exactly. The source-domain
measures (g, D2–D20, edge spans, edge width, range normalisation, radial bands) follow the Tonalism
round's §2.1 exactly. The z-distance is this report's own and is defined in §4.2. Scripts, corpus
manifests and the full per-image tables are kept in the scratchpad, not committed.
