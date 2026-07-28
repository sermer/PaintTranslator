# Research: Fauvism

Research into Fauvism aimed at one question: **what should the "Fauvism" style actually do?** As
shipped it is `ToneAndChromaRemap` at contrast 1.35 and chroma 2.2, over an edge-preserving floor
left at its default strength 1.0, mark scale 1.3 — the app's loudest chroma setting and its
second-weakest floor.

Four parallel tracks, written by separate agents that did not see each other's work. This README
is the synthesis. The reports are long; read this first and go to them for detail.

| Report | Covers |
|---|---|
| [01-what-defines-fauvism.md](01-what-defines-fauvism.md) | Definitions, the boundary against Post-Impressionism and Expressionism, Matisse's own theory, whether any measured signature separates the movement. |
| [02-brushwork.md](02-brushwork.md) | Stroke geometry, reserved canvas, drawn contour, stroke-based rendering, what the movement's phases imply for which handling to target. |
| [03-colour.md](03-colour.md) | Fauve palettes and their modern equivalents, whether Fauvist colour is high-chroma or high-contrast, the achievable gamut recomputed from the candidate set. |
| [04-flatness-and-space.md](04-flatness-and-space.md) | Flatness mechanisms, value structure measured against a corpus, depth cues, planes versus patches. |

## The headline: the defect is fragmentation, and all four tracks measured it

**Every track measured the committed Fauvism golden independently, from a different direction, and
found the same thing: Fauvism is the least paintable style in the app — worse than Abstract.**
`[verified — computed locally 2026-07-28, four times over]`

| | Fauvism | Abstract | Realism |
|---|---|---|---|
| Regions (4-connected) | **1,035** | 685 | 425 |
| Distinct colours | **331** | 322 | 161 |
| Pixels in regions ≤ 4 px | **6.1%** | 3.7% | 2.4% |
| Pixels in regions ≤ 16 px | **21.1%** | — | 5.4% |
| Pixels below the style's own mark² | **30.87%** | — | 5.42% |

Track 1 reproduced the Abstract investigation's published Abstract column exactly, which validates
the method across sessions. Track 2 adds the shape form of the same result: **median region
elongation is statistically flat across all five styles (2.77–3.23)**, and mean horizontal run is
2.33 px against a nominal 5.2 px mark. That is the empirical form of "no stage makes a mark".
`EdgePreservingFloor`'s own doc comment already predicts this failure and names Fauvism.

The mechanism is identified. Fauvism registers **no floor override at all**, so it runs the
registry's weakest floor while raising mark scale to 1.3 — asking for bigger marks and doing less
to produce them. Contrast 1.35 then moves 46.7% of pixels by more than 5 L\* and stretches the dark
end from L\* 22.6 to 7.5, and expanding L\* is itself a fragmentation multiplier. `[verified]`

**Chroma 2.2 is not the defect.** Three tracks say so explicitly. The loudness is the thing the
style gets *right* relative to the movement's materials; the speckle is what it gets wrong.

## The second convergence: only slots 1 and 5 can produce Fauvism

Tracks 1 and 4 argued this formally and independently, and it is the same conclusion the Abstract
investigation reached about abstraction.

- Track 1: slot 2 is a function of CIELAB alone, so two same-coloured pixels can never diverge.
  A pointwise operator cannot create the relational structure Matisse describes. `[verified]`
- Track 4, empirically: a position-blind operator's regions are level sets by construction. The
  area-weighted isoperimetric quotient of the app's large regions is **0.04–0.11 across the entire
  parameter space**, against 0.617 for a digital disc. Nothing in slots 2–4 moves it. `[verified]`

This kills "tune the remap harder" as a strategy for anything structural, in every style.

## What the movement's own theory says, which is not what the style does

Track 1 decoded and read Matisse's *Notes of a Painter* (1908) in full. It contains a direct rebuke
of the shipped setting: **"If he fears the banal he cannot avoid it by appearing strange, or going
in for bizarre drawing and *eccentric color*."** `[verified]`

What the essay actually argues for is *relational* colour — "the red has succeeded the green as the
dominant color" — and **area as the control variable**: "In order for the first dot to maintain its
value I must enlarge it." Both are spatial. Neither is a chroma multiplier.

It also undercuts the premise that a photo converter is fighting the movement: Matisse writes that
"when he is painting, he should feel that he has copied nature". The style is far less
anti-descriptive than the label implies, so the tension between "Fauvism" and "starts from a
photograph" is weaker than it looks.

## The measured literature gives a negative result

**No published signature separating Fauvism from its neighbours uses chroma, saturation or
contrast, and no study anywhere measures saturation per art movement.** Tracks 1 and 3 searched
independently and agree. `[verified]`

The only published quantitative placement of Fauvism is Sigaki, Perc & Ribeiro 2018 (PNAS, 137,364
WikiArt images) — and it **discards colour entirely**, averaging the three channels and justifying
it with r = 0.989 against luminance. It puts Fauvism in the highest-entropy, lowest-complexity
corner alongside Impressionism and Pointillism. `[verified via ar5iv]`

Track 1's warning about that corner is worth carrying: **H≈1 / C≈0 is also the white-noise corner.**
The metric cannot distinguish diffuse brushwork from speckle — and speckle is precisely this app's
measured defect, so optimising toward Fauvism's published coordinates would reward the bug.

So the materials argument for loud colour is strong (cadmiums, vermilion, viridian, cobalt violet
were the highest-chroma pigments available in 1905, applied near tube strength) and the
colorimetric argument did not exist — until track 4 built one.

## Track 4 supplied the missing colorimetry, and it changes the chroma answer

Track 4 measured 14 Fauve works, 12 Impressionist/Post-Impressionist works and 7 photographs
(Wikimedia, sRGB→CIELAB): `[verified — computed locally, corpus caveat below]`

| | L\*sd | L\*range | C\*mean | Hue entropy | local ΔC\*/ΔL\* |
|---|---|---|---|---|---|
| Fauve (14) | **20.48** | 66.2 | **28.3** | 3.51 | **0.796** |
| Impressionist (12) | 20.65 | 64.1 | 15.7 | 3.61 | 0.492 |
| Photograph (7) | 23.60 | 75.8 | 16.9 | 3.07 | 0.365 |

Three things fall out:

1. **Fauvism's value structure is identical to Impressionism's** (L\*sd 20.48 vs 20.65). The
   "Fauvism flattens by compressing value" hypothesis fails. Both sit *below* photographs, so the
   flattening is real but it is not a contrast operation.
2. **The separator is the chromatic share of mark-scale contrast** — ΔC\*/ΔL\* at ×2.18 over
   photographs. This is the equiluminance mechanism: chroma is the channel that does not compute
   depth. That is why Fauvist paintings read flat while staying legible.
3. **Hue variety does not increase** (3.51 vs 3.61 vs 3.07). No hue-spreading or hue-rotation stage
   is warranted, which independently confirms track 3's rejection of non-descriptive hue
   substitution.

The chroma target implied is **×1.67 of source mean, ×1.77 at p95**. Track 4 computes that a
nominal 1.8 delivers ≈×1.74 through the tanh knee.

**This is the one place the tracks disagree.** Track 3 recommends keeping 2.2, on the grounds that
Fauvism's golden shows both a higher mean *and* a higher SD than Realism, so the Abstract round's
"the knee compresses the spread" objection does not transfer. That reasoning is sound but it is
argued against Realism, not against a target — track 3 explicitly noted no colorimetry of the
movement existed. Track 4 then produced it. **Prefer 1.8, and treat it as the weakest of the three
retune numbers**, because track 4's corpus curation is its own top verification debt (its first
"photograph" control turned out to be a Derain, caught only by inspecting the render).

## Where the tracks converge on numbers

Two of the three retune values were picked independently by two tracks that measured different
things.

| Parameter | Now | Track 1 | Track 4 | Recommended |
|---|---|---|---|---|
| Floor `strength` | 1.0 (unset) | 3.0 | 3.0 | **3.0** |
| `contrast` | 1.35 | 1.0 | 0.95 | **0.95–1.0** |
| `chroma` | 2.2 | keep | 1.8 | **1.8**, see above |

Track 1 reached contrast 1.0 from Matisse's value-identity argument and the L\*-stretch measurement.
Track 4 reached 0.95 from a corpus ratio of ×0.87 against source L\*sd. **Contrast is currently
wrong by sign** — the style raises it where the evidence says lower it.

Track 4's framing is the one to keep in the doc comment: **value compression should come from the
floor, which removes modelling while keeping range, not from the contrast knob, which squashes the
histogram — and squashing the histogram is Tonalism.**

Track 4 measured the floor change alone at **−12.5 points of unpaintable share** (mean of four
photographs, nothing else altered).

## Corrections to prior research

Six, and two of them are load-bearing for work already queued.

**1. The Abstract round's per-hue chroma ceiling table is wrong, in the direction that decides
Fauvism.** Track 3 recomputed it from the candidate set instead of the manifest: `[verified]`

- **Masstone is not the chroma ceiling.** 13 of 18 chromatic selectable paints reach higher C\* in
  a white tint than at full strength. Phthalo Green (Y.S.) goes **18.9 → 56.3 at L\* 75.6**;
  Dioxazine Purple **6.5 → 52.6**. Dark transparent pigments read as near-black at masstone, and
  two-constant K-M gets that right — reading masstone figures off the manifest did not.
- **"K-M mixing always lands below both parents" is false.** 6.49% of 5,301 sampled pair mixtures
  exceed *both* parents' chroma; worst excess 46.1.
- Over the real candidate set from all 19 selectable paints (84,063 mixtures) there is **no empty
  hue sector and none below C\* 35**. Greens reach **C\* 86–89 at L\* 70–82**. Hansa Yellow + 19%
  Phthalo Green (Y.S.) gives C\* 85.4 at L\* 58.6, h 136°. With no green paint at all, yellow +
  ultramarine still gives C\* 59.9 at L\* 52.4.
- **The Fauvist red/green opposition is reachable**, in a band at **L\* 55–65** (red 81.8, green
  86.1). It fails below (dark green tops out at 51) and above (light red is a pink at 53.6).

Item 1 of the Abstract build order — the per-hue ceiling — survives. **The table it was to be built
from does not. Build it from the candidate set, 24 bins.**

**2. "Raising floor strength has never helped" is true on the golden gradient and false on
photographs.** Track 4: strength 1→5 moves Fauvism 21.1% → 18.8% on the synthetic gradient but
**61.2% → 37.6% on a real photo**. The gradient is smooth by construction, so the guided filter has
nothing to remove. **Any conclusion about floor strength drawn only from `Tests/Golden` is unsafe**,
and the Abstract README states one. `[verified]`

**3. Report 02's "Fauvism is the one style whose definition is natively pointwise" is wrong.** True
of the caricature, false of the theory and the measurements. See the slot argument above.

**4. The parent README's "×2 is unreachable in blues and greens" needs qualifying.** Track 1
measured realised mean gain at **×2.07** on the six-paint test palette. The shortfall does not
appear as banding — it appears as **hue rotation**: 40.6% of chromatic pixels move more than 10°,
mean 12.5°. The tanh knee prevented the failure mode it was built for; nobody was watching the one
that actually happens. Track 3 adds that a knob labelled 2.2 realises **0.76× to 1.88×** depending
on hue, and chroma actually *falls* at 180–210°. This is invisible on the committed golden only
because that source has no pixels between 150° and 270°. `[verified]`

**5. The Abstract round's demotion of anisotropic Kuwahara does not carry to brushwork.** Its
objection — that Kuwahara produces no region representation — is a *shape* objection. Brushwork
needs a directional trace, not a region. Poor segmenter, good brush. `[inferred]`

**6. Report 03 priced FDoG contour lines at ~200 lines given a structure tensor.** On an
already-mapped index buffer no line detector is needed at all: boundaries are index mismatches.
That roughly halves the cost and removes the tensor from the dependency chain. `[verified against
the `Refine` signature]`

## The shared infrastructure, again

**Three of the four Fauvism tracks picked small-region merge**, and the Abstract investigation
already found three of *its* four tracks needed the same connected-component labelling on the
mapped index buffer. `PaintabilityMetrics.ForEachRegion` is most of that flood fill.

Six independent recommendations across two investigations rest on one shared component. **Build it
once and charge it once.**

Track 2 supplies the postcondition that makes it testable: after an area opening at `MarkPixels²`,
`FractionInRegionsSmallerThan(MarkPixels²)` must be **exactly zero**. That is a hard assertion, not
a threshold, and it is the most valuable test available anywhere in this work.

Track 2 also supplies the measurement trap: fragmentation after `ContourLines` must be measured
**excluding the line index**, or the lines collapse `CountRegions` and report an unearned win.

## The phase ruling

**Target 1906–08 — broad, flat, outlined. Not 1904–05.** `[verified]`

The 1904–05 divisionist phase (Matisse's *Luxe, calme et volupté*) *is* Neo-Impressionism, and the
parent research already scopes broken colour as the shared feature of Impressionism, Pointillism
and Divisionism. Building it under "Fauvism" would make two styles identical.

Track 2's decisive evidence is a single canvas doing both later devices at once — the NGA on
Derain's *Charing Cross Bridge, London* (1906): buildings "outlined with royal blue and filled in
with mostly flat areas of color", water in "short, horizontal, disconnected strokes and dots…
against the off white of the canvas below". `[relayed — NGA object pages returned 403; this is from
search snippets]`

## Suggested build order

Nothing here is decided. Cheapest and best-supported first. Slot numbers refer to the five-slot
pipeline.

| # | Item | Slot | Cost | Why here |
|---|---|---|---|---|
| 1 | **Retune the registry** — floor strength 1.0 → 3.0, contrast 1.35 → 0.95, chroma 2.2 → 1.8 | — | ~10 lines + doc, 1 golden regenerated | Two tracks converged on the first two values independently. Attacks the measured defect with no new code. Contrast is currently wrong by sign. |
| 2 | **Small-region merge at `MarkPixels²`** | 5 | ~100 lines, ~60 shared | Six recommendations across two investigations need it. Invariant-safe by the `Refine` signature. Hard postcondition available. Turns the mark invariant from a hope into a guarantee. |
| 3 | **`ContourLines`** — boundary detect on indices, dilate to mark-derived width, write one chosen candidate index | 5 | ~130 lines | The highest-value Fauvist mark, and cheaper than any stroke synthesis. Post-map selection, so invariant-safe for free. |
| 4 | **Per-hue chroma ceiling, 24 bins, built from the candidate set** | 2 | ~20–60 lines | Two tracks picked it; fixes a live defect in three styles. Do **not** build it from the manifest. |
| 5 | **Masstone-biased candidate transform** — a purity slider via a share-aware `KeepOnlyMixtures` overload | 3 | ~90 lines | Measured head-to-head at gain 2.2: same mean chroma (35.3 vs 35.4), **higher SD** (20.2 vs 17.0), bigger tail (3.3% vs 2.1% above C\* 75), **3.4× fewer regions and 3.4× less tiny-region area**. |
| 6 | **`FlowFlatten`** — structure tensor plus flow-aligned filter | 1 | ~200 lines | The only pick that would move median elongation. Needs a new directional fixture; the golden gradient's banding confounds it. |

Items 1–3 are roughly 240 lines and address everything all four tracks agree is wrong.

## What not to build

The parent README's list still applies, and the Abstract README's list still applies. These are
additional, and each is rejected by a track that went looking for it.

- **Full stroke-based rendering as the Fauvism default.** Wrong phase, 4–6 uncalibratable
  parameters, stochastic output in a slider UI, 400–600 lines. Report 03's "SBR: payoff very high"
  does not hold for Fauvism specifically.
- **Targeting Fauvism's published entropy/complexity coordinates.** H≈1 / C≈0 is the white-noise
  corner; optimising toward it rewards the app's actual defect.
- **Raising chroma further, or adding a hue-rotation knob.** Hue *variety* does not increase in the
  measured corpus. Three tracks reject non-descriptive hue substitution: it needs semantics, and
  memory colours break it.
- **Complementary shadow assignment**, and complementary pairing as a mechanism generally. Induction
  is from the *illuminant*, real shadows are bluish, and Schloss & Palmer contradict red–green by
  name.
- **Any "Fauve palette" preset** naming viridian, emerald or cobalt violet — all three are withheld
  or absent from the library. Those are the named gaps that matter.
- **Restricting to ≤2-paint mixtures.** "Straight from the tube" is documented for *Vlaminck
  specifically*; Matisse's *Red Studio* has twelve pigments. Generalising it is a folk move.
- **Lowering chroma as the fix**, and **claiming anywhere in the UI or doc comments that Fauvism is
  measurably more saturated** — nobody has measured it against a neighbouring movement.
- **Contrast below ~0.9 as the flattening mechanism.** Flattening belongs to the floor.
- **Aerial-perspective inversion.** 5th of 5 pictorial cues and effective only beyond 30 m
  (Cutting & Vishton 1995, read in full).
- **Perspective violation of any kind.** Occlusion ranks 1st in all three distance regimes and is
  the sole cue in Palaeolithic art.
- **Posterisation sold as planes**, and **isoluminant rendering**.
- **Pointillist dithering under the Fauvism label**, **noise-driven "reserved canvas" masks**, and
  **impasto** — Vlaminck's loading does not reopen the case.
- **Position-dependent quantisers**, and **anti-aliasing plus a repair re-map**.
- **Splitting Fauvism into two style rows today.** Unlike Abstract, the evidence does not support it.

On reserved canvas specifically: it is a *coverage* property, orthogonal to both the colour and the
spatial axes, and no conservation study quantifies its fraction. Track 2 recommends against a
dedicated stage — the right home is the Abstract round's `GroundFill` with the mask test changed to
"low interior gradient **and** high mapped L\*", since Fauve reserve is white gesso and Titanium
White at L\* 98.25 is the lightest candidate.

## Accuracy warnings

Read these before quoting any figure.

- **Track 3's chroma numbers were produced with `ScaleChroma` transcribed, not called.** Its own top
  verification debt. Re-run its §4.2 and §7.1 through the real stage before acting on the per-hue
  ceiling or on any chroma ruling.
- **Track 4's corpus is self-curated from Wikimedia and its first "photograph" control was a
  Derain**, caught by inspecting the render. The corrected table is above, but corpus composition is
  that track's top debt and the chroma target rests on it.
- **Sigaki et al. measure greyscale.** Any colour claim attributed to that paper is not in it.
- **The cadmium yellows in *Le Bonheur de vivre* have measurably degraded.** Any future colorimetry
  of a Fauve canvas measures a 121-year-old object, not a 1906 one.
- **All computational brushstroke work cited operates on uncalibrated web reproductions** with no
  physical scale — pixels of a JPEG, never millimetres of paint. **No published figure could set a
  `MarkPixels` or stroke-length default.** This is a positive argument for stages whose parameters
  fall out of `MarkPixels` over ones that introduce free knobs.
- **Conclusions about floor strength drawn only from `Tests/Golden` are unsafe.** See correction 2.
- **No measured characterisation of Fauvist stroke geometry exists.** Everything material on Fauve
  paintings is pigment science, not geometry.

## Verification debt

Ranked by how much each would change a decision.

1. **Re-run track 3's chroma probes through the real `ScaleChroma`.** Local work, not a source.
   Cheapest item and it gates two build-order entries.
2. **Curate track 4's corpus properly** — provenance-checked Fauve and control sets. The chroma
   target of 1.8 rests on it.
3. **Desikan et al. 2022, *Entropy* 24(9) 1175 (WikiArtVectors)** — MDPI returned 403 and the PDF
   would not decode. The one located source that could overturn the negative result on measured
   colour signatures.
4. **Georgoulaki, JOCCH 2024** (run-length brushstroke analysis) — 403. Would calibrate `FlowFlatten`.
5. **The Met, *Vertigo of Color* (2023)** — 429. Could shift the phase ruling.
6. **NGA object pages** — 403. They carry the headline evidence for `ContourLines`, currently
   `[relayed]` from search snippets.

Items 1 and 2 are local work, cost almost nothing, and gate more than the four paywalled sources
combined. Clear them first.
