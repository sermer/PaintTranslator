# Research: Post-Impressionism

Research into Post-Impressionism aimed at one question: **what should the "Post-Impressionism"
style actually do?** As shipped it is `EdgePreservingFloor` at strength 3.0 and
`ToneAndChromaRemap` at contrast 1.1 / chroma 1.3, mark scale **1.6**, `KeepAllCandidates`,
`NearestQuantiser`, and **no post-map stage at all** — the largest mark in the app paired with
nothing that makes a mark.

Four parallel tracks, written by separate agents that did not see each other's work. This README
is the synthesis. The reports are long; read this first and go to them for detail.

| Report | Covers |
|---|---|
| [01-brushwork.md](01-brushwork.md) | Stroke geometry, the first physically calibrated mark size in the app, directionality and whether `FlowFlatten` is warranted, the `SmallRegionMerge` bug. |
| [02-colour.md](02-colour.md) | A 57-work corpus measured per painter, what the chroma knob actually delivers, the per-hue ceiling's dead accessor, the optical-mixing gate. |
| [03-edges.md](03-edges.md) | Edge density measured 14 ways, `ContourLines`' three defects, key-line colour from eight canvases, the edge-hierarchy knob correction. |
| [04-backgrounds.md](04-backgrounds.md) | Ground versus field versus negative space for this movement, `GroundFill`'s hard-coded lightness, why the flat decorative plane is unbuildable. |

## The headline: this row asks for the biggest brush and does nothing to produce it

**All four tracks measured the same defect independently, and it is the same defect the Fauvism
round found — except that Fauvism's fix shipped and this one did not.** `[verified — computed
locally 2026-07-30, four times over four different corpora]`

| Track | Corpus | Sub-mark share, as shipped |
|---|---|---|
| 01 brushwork | 12 photographs | **35.3%** (Fauvism 6.8%, Abstract 1.7%) |
| 02 colour | golden | **38.30%** (Fauvism 3.65%, Abstract 2.45%) |
| 03 edges | 14 photographs | **32.2%** |
| 04 backgrounds | 7 photographs | **43.5%** median (range 10.5–64.2%) |

Post-Impressionism is the only styled row with an empty slot 5. Fauvism and Abstract both
received `SmallRegionMerge` last round and both moved by an order of magnitude — Fauvism 1,035 →
186 golden regions, Abstract 685 → 8. This row was never touched.

Track 3 adds the form of the result that best explains what a viewer sees: **82.5% of the canvas
sits within half a brushmark of a colour change**, and boundary contrast (ΔE 8.76) is
statistically identical to Realism's (8.77) at a brush 1.6× wider. Track 1's golden measurement
reproduces the Fauvism round's published Post-Impressionism figures exactly, which validates the
method across sessions for the third round running.

**The fix is one registry line**, and all four tracks picked it first or second.

## The second convergence: `SmallRegionMerge` does not do what it claims, and two tracks found why

The Fauvism round specified `FractionInRegionsSmallerThan(MarkPixels²) == 0` after one pass as
"the most valuable test available anywhere in this work". **It is false as implemented**, and all
four tracks measured it failing. `[verified]`

| Track | Residual after one pass |
|---|---|
| 01 | 0.67–48.17% |
| 03 | 19.7% mean; ladder 32.2 → 19.7 → 13.5 → 6.9 → 2.3 → 0.3% at 0/1/2/4/8/16 passes, **3 of 14 sources still failing at 16** |
| 04 | 4.3–46.5% |

Two tracks diagnosed it from different directions, and the diagnoses are compatible — both are
the same failure to update state during the sweep:

- **Track 1:** 72% of sub-mark regions have no neighbour already ≥ mark², so `LargestNeighbour`
  (`SmallRegionMerge.cs:77`) falls through to merging one small region into another, and region
  sizes are never updated afterward. Stranded area (23.94%) matches the one-pass residual
  (23.07%) within a point on every image.
- **Track 3:** the label map is computed once, before any merge, so every subsequent decision in
  the sweep reads pre-merge geometry.

Both propose the same fix: **a single smallest-first union-find sweep that accumulates area**,
~50–60 lines. It converges in one pass, and it carries to Fauvism and Abstract, which ship the
broken version today.

**It survived review because it holds on the synthetic golden** — 1.11% → 0.00% at two passes.
This is the third consecutive round in which a conclusion drawn from `Tests/Golden` proved false
on photographs. See "The corpus problem" below.

## Where the tracks converge on what to build

| Item | Tracks | Slot | Cost |
|---|---|---|---|
| **Register `SmallRegionMerge`** on this style | **all four** | 5 | ~1–10 lines |
| **Fix the area opening** to converge in one sweep | 01, 03 (04 confirms the failure) | 5 | ~50–60 lines |
| **Do not adopt `ContourLines` here** | 01, 03, 04 | — | — |
| **Keep one style row; do not split** | **all four** | — | — |

Combined, track 1 measures floor 5 plus a correct merge reaching **~8%** — Fauvism's territory —
and reports the renders show broad lozenge patches instead of dissolved speckle.

### Why `ContourLines` is rejected here, on three independent grounds

- **Measured worse** (track 1): 35.33% → 42.84%, regions 124k → 231k.
- **Duplicates Fauvism** (tracks 1 and 3). Fauvism's own phase ruling targets 1906–08 "areas of
  flat colour, similar to Gauguin", so flat-planes-plus-drawn-contour is already occupied. Track 3
  rendered the same photograph both ways and inspected them: same picture, different key.
- **Structurally identical, not merely similar** (track 3): **all three post-map stages declare
  zero parameters**, so a second style registering the stage gets byte-identical behaviour rather
  than a version of it.

Track 4 rejects it separately, measuring 48.3% line coverage on one photograph.

## The boundary ruling: one row, and the tracks agree on the reason

**Keep one style row.** All four tracks reached this independently, from four different arguments:

- **Track 2, measured.** A provenance-checked 57-work corpus gives mean C\*ab **30.3 (van Gogh),
  22.5 (Gauguin), 15.3 (Cézanne), 16.6 (Seurat/Signac)** against **14.7 Impressionist** and
  **16.3 photograph**. Cézanne and the Neo-Impressionists sit *inside* the Impressionist
  distribution on every statistic. Two of four sub-rows would render as Realism-with-a-floor.
- **Track 1, by elimination.** Three of the five handlings are already taken: Seurat is the
  planned broken-colour feature, Gauguin's cloisonnism *is* the shipped Fauvism, Lautrec's
  exposed support is `GroundFill`.
- **Track 3:** three of four branches are not buildable.
- **Track 4:** no image-statistics study separates the four painters.

Unlike Abstract, there is no measured bimodality to appeal to — Sigaki et al. does not name
Post-Impressionism at all.

**What the row should target is the one place the tracks disagree.** Track 1 says the
Cézanne–Van Gogh constructive/directional patch; track 2 says the van Gogh–Gauguin axis.
**Prefer van Gogh primary, Cézanne for structure, and treat Gauguin as Fauvism's** — track 2's
inclusion of Gauguin rests on chroma alone (22.5 vs Cézanne's 15.3), while tracks 1 and 3 give
structural arguments that Gauguin's handling is already shipped under another name. Van Gogh is
the only painter both framings keep.

## The retune, and the one real disagreement

| Parameter | Now | Track 1 | Track 2 | Track 3 | Recommended |
|---|---|---|---|---|---|
| Floor `strength` | 3.0 | **5.0** | **4.0** | **do not raise** | unresolved — see below |
| `contrast` | 1.1 | inert | **1.0** | — | **1.0**, low confidence |
| `chroma` | 1.3 | — | **1.45** | — | **1.45**, low confidence |

**Floor strength is a genuine three-way disagreement and should not be changed until it is
settled.** Track 1 measures 3.0 → 5.0 at −6.88 points alone and 23.07% → 16.79% with the merge,
while noting the caveat that 5.0 is Abstract's setting; track 2 wants 4.0; track 3 rejects
raising it above 3 outright. Track 4 adds that a 1→5 sweep moved unpaintable share by up to 40
points while moving the largest border-connected region by ≤0.6 points, so the floor does not do
what a background treatment would need. Since the merge is the agreed fix and lands first, **ship
the merge, re-measure, then revisit the floor** — its value is entangled with a stage that is
about to change.

**Contrast is not a contradiction, despite appearances.** Track 2 says 1.1 is wrong by sign
(corpus target L\*sd ratio 0.729, shipped delivers 0.921); track 1 measures contrast as inert
here (0.95 vs 1.3 differ by 0.27 points). They measured different quantities — colour statistics
against a corpus, and fragmentation. Both hold. The reading is that the retune is cheap and
defensible on colorimetry, and **will not improve paintability**, so it must not be sold as a fix
for the headline defect. Note this means **the Fauvism round's "contrast is wrong by sign"
finding does not transfer to this style.**

Both retune numbers rest on track 2's self-curated corpus, which is that track's own top
verification debt. Treat them as the weakest recommendations in this document.

## Corrections to prior research

Eleven. Four were verified against the shipped source while writing this synthesis and are
flagged as such; the rest are as reported by the tracks.

**1. `SmallRegionMerge`'s postcondition is false.** The Fauvism round's "single most valuable
assertion" does not hold on photographs. See the second convergence above. `[verified — four
tracks, two independent mechanisms]`

**2. The per-hue chroma ceiling — a top build item in *both* prior rounds — is already 90% built,
and the last 10% is measurably worthless.** `RenderContext.AchievableMaxChromaByHue` (36 bins,
`RenderContext.cs:74`) and `AchievableMaxChromaForHue` (`RenderContext.cs:110`) already exist and
are populated from the candidate set. **Nothing calls the accessor.** `[verified against the
source while writing this synthesis — the only references are its own definition and
`RenderContext`'s internals]` Wiring it moves realised chroma ×1.209 → ×1.198 and hue drift
17.3° → 17.2° on real photographs; even at Fauvism's 1.8, ×1.558 → ×1.521. The knee weight is
`(gain−1)/2`, so at 1.3 the ceiling governs 15% of the transform. **Both prior rounds reasoned
from the *ask* side and never measured the *delivered* side.** Remove it from the build order.

**3. `GroundFill` hard-codes the ground at L\* 58** (`GroundFill.cs:94` — `FindNearest(58.0, …)`,
`[verified against the source]`). It never implemented the lerp the Abstract round specified. On
seven photographs it moved the field it repainted by **ΔE 23.4–58.7** — an L\* 98.2 sky and an
L\* 11.2 background both became the same mid grey — and changed the paintability metric by
**exactly 0.00 on all seven**.

**4. `MotherColourTransform` is a whitening operation, not a unifying one.**
`MostNeutralPaintIndex()` returns Titanium White for any palette containing white, so at
Tonalism's 0.30 the darkest achievable colour rises L\* 11.0 → **38.3** for a −7% mean chroma
change. **Live in Tonalism and Abstract today.**

**5. `ContourLines` draws a constant band for every mark size in the app's ordinary range.**
`Math.Round(context.MarkPixels * 0.10)` collapses to 1 for `MarkPixels` 2–12
(`ContourLines.cs:28`, `[verified against the source]`), so relative width swings from 2.00 marks
down to 0.33 — backwards. Its canvas share is not a parameter at all: mean 23.8%, up to **55.8%**
on one photograph, against 3.8–17.1% measured on real cloisonnist canvases.

**6. "No published figure could set a `MarkPixels` default" is wrong.** Lamberti et al. 2014
(EURASIP JIVP 2014:53) states its constraint in dpi *of the painting*, and the counter-PDF
endpoint serves it unauthenticated where the Fauvism round hit a Springer redirect. It converts
to van Gogh strokes of **6.4–25.8 mm², aspect ≥ 2.5:1** (≈4.5–9.1 × 1.8–3.6 mm), putting the base
mark on stroke *width* and `MarkScale 1.6` on stroke *length*. **The first physical justification
for any parameter in this app** — and it rests on one unconfirmed reading, see the debt list.

**7. The parent round's edge-hierarchy lever names the wrong knob.** Report 03 lever 1 varies
blur *radius*; the floor is now a guided filter whose softness parameter is ε. Measured mid-field
boundary-contrast change: radius ≈0%, Gaussian −4%, **edge threshold −17%** with focal-band
contrast held within 2%. Same architecture, same line count, 4× render time as prototyped.

**8. The Fauvism round's "value compression should come from the floor, not the contrast knob"
is wrong as stated.** Floor 1→5 moves L\*sd 8% and fragmentation 22 points; contrast 1.1→0.85
moves L\*sd 17% and fragmentation 0.2 points. **They are orthogonal, not substitutes.**

**9. The Fauvism round's "change `GroundFill`'s mask test to low interior gradient and high
mapped L\*" is impossible as written** — `GroundFill` has no gradient test.

**10. The key line is not black.** Across eight canvases, thin dark structures have C\* 2.0–18.9
and hues 52°–269°; only Bernard's *Le Pardon* is neutral, and van Gogh's redrawn contours are
Prussian blue, MA-XRF-identified. Line lightness is field L\* − 20 (range 10–28), while
`ContourLines`' absolute target lands at L\* 37 against a corpus median ≈26. This closes the
parent round's "no track covered line weight, colour and placement" gap.

**11. Two verification methods the Fauvism round proposed do not work.** Median region elongation
cannot verify a directional stage (van Gogh's calibrated aspect floor is 2.5:1 and the app
already sits at 2.5–4.6 from banding), and structure-tensor coherence on mapped output is
contaminated on *any* source rather than only the golden — quantisation boundaries are maximally
anisotropic, and on the golden gradient conversion *raises* coherence 0.474 → 0.667,
manufacturing the signal under test.

**Confirmed, not corrected:** Fauvism's paintability win is genuine and not a contour artefact —
9.61% with the contour index dropped from both numerator and denominator (track 1, checking the
trap the Fauvism round itself flagged).

## The optical-mixing gate the parent round queued has now been measured, and it passes

Step 1 of the parent README's build order — "measure the dithering gain before building anything
on it" — was queued across two rounds and never run. Track 2 ran it. `[verified]`

Over 4,631 photographic colours, mean ΔE **6.06** (best single mixture) → **2.15** (best 1:1
juxtaposition averaged as radiance in linear light); 93.5% improved, 62.4% by more than 2 ΔE.

**But the gain is lightness, not chroma** — mean |ΔL\*| −1.59, mean C\* −0.39. That confirms the
parent round's *direction* and refutes its stated *reason*. The specific illustration
"blue+yellow dithered reads grey; mixed reads green" is **false in this library**: it reads light
yellow, because ultramarine masstone is L\* 7.8. Juxtaposition is lighter than K-M in all 15
masstone pairs (mean +14.8 L\*), loses 34–50 C\* against white, and gains 12–36 between chromatic
pairs.

**Track 2's ruling: build divided colour as its own style row, not this one.** Seurat measures as
Impressionist, and mark scale 1.6 is the worst place in the app to put it.

## Suggested build order

Nothing here is decided. Cheapest and best-supported first. Slot numbers refer to the five-slot
pipeline.

| # | Item | Slot | Cost | Why here |
|---|---|---|---|---|
| 1 | **Register `SmallRegionMerge` on Post-Impressionism** | 5 | ~1–10 lines | All four tracks picked it. Attacks the measured defect with no new code: 43.5→26.2, 38.30→25.84, 32.2→19.7, −12.26 across four corpora. |
| 2 | **Fix the area opening** — one smallest-first union-find sweep accumulating area | 5 | ~50–60 lines | Two independent diagnoses, one fix. Turns the mark invariant from a hope into a guarantee, and repairs Fauvism and Abstract at the same time. Makes the Fauvism round's hard assertion true. |
| 3 | **Repair `GroundFill`** — derive ground lightness from the field, add a ~10% coverage floor | 5 | ~25 lines | It is currently a no-op on the metric and a ΔE 23–59 error on the picture. Fixes a live defect in Abstract. |
| 4 | **Fix `ContourLines`' three defects and give it parameters** — registered on Fauvism, **not** here | 5 | ~25 lines | Constant band width, uncontrolled canvas share, wrong line lightness. All three post-map stages declare zero parameters, which is the deeper issue. |
| 5 | **Retune** — contrast 1.1 → 1.0, chroma 1.3 → 1.45 | 2 | ~2 lines + doc | Defensible on colorimetry. **Must not be sold as a paintability fix** — track 1 measured contrast inert here. Rests on track 2's corpus. |
| 6 | **Focal edge-threshold floor** — report 03's lever 1 with ε as the parameter, not radius | 1 | ~120 lines | The only device that differentiates this style from Fauvism without duplicating a stage, and the only variant tested that *softens* rather than hardens. 4× render time as prototyped. |
| 7 | **`FlowFlatten`** — structure tensor plus flow-aligned flattening, orientation source parameterised | 1 | ~200 lines | Warranted — post-floor anisotropy 0.66–0.84, 69–94% coherent, so there is a field to align to. Useless before item 2 lands, and its verification method is an open question (correction 11). |

**Floor strength is deliberately absent.** See the retune section: three tracks gave three
answers, and its value is entangled with item 2.

Items 1–3 are roughly 90 lines and fix live defects in three styles.

## What not to build

The parent README's list still applies, as do the Abstract and Fauvism lists. These are
additional, each rejected by a track that went looking for it.

- **`ContourLines` on this style.** Measured worse, duplicates Fauvism, and byte-identical by
  construction. Three tracks.
- **Splitting the Post-Impressionism row.** All four tracks.
- **Wiring the per-hue chroma ceiling.** Already built, and worth ×0.01 of realised chroma.
  Remove from both prior rounds' build orders.
- **The flat decorative background.** Flattening any mask larger than an already-uniform region
  costs mean ΔE 9.1–38.7 against median candidate spacing 1.70. The paint is fine — a Gauguin
  vermilion plane is ΔE 1.28 from an achievable mixture — the photograph is not.
- **A doubled or searching Cézanne contour.** The geometry is expressible; the optical character
  is not. A flat opaque double band is a railway track. Track 3's only inference-based rejection.
- **A mother-colour fraction for this style**, and extending `BlendInto` to take a mixture (the
  ground it would name quantises at ΔE 6.5).
- **Any hue operation.** Drift is 17.2° with *no* remap at all — a palette-gap defect, not a
  remap defect.
- **Mean saturation as the target.** The only preference study on van Gogh landscapes has SD of
  saturation β = 0.404, p = .003, with mean absent — the same shape as the Abstract round's
  finding.
- **Contrast above 1.0 anywhere**, and **`MarkScale` above 1.6** (2.5 measured worse).
- **Spatially varying the guided filter's *radius***, and a spatially varying Gaussian as the
  hierarchy mechanism. The parameter is ε.
- **Any aerial-perspective stage.** The source already carries +51 L\* / −18 C\* on real
  landscapes, and carries it *backwards* on 3 of 7.
- **Validating a contour stage by dark-area fraction.** It does not separate cloisonnist canvases
  from Cézanne or van Gogh — 9.9% vs 11.0%.
- **Any "black outline" default**, and any **"Post-Impressionist palette" preset**.
- **Pointillist dithering under this label**, **full stroke-based rendering**, a
  **reserved-canvas stage**, and **impasto** — all carried forward from prior rounds and
  re-rejected here.

## The corpus problem, which is now the round's systemic weakness

**Every track hit it, and three caught a contaminated fixture only by looking at the images.**
This is the third consecutive round in which a synthetic fixture produced a false conclusion.

- Track 3's first pass used Windows stock wallpapers, which turned out to be **synthetic 3-D
  renders, not photographs** — flattering to the pipeline in exactly the way `Tests/Golden` is.
  Its §5.2 percentages come from that smaller set and are flagged as an optimistic bound.
- Track 2's automated checks caught a scanned 1899 map that passed the camera-EXIF test on a
  scanning back, but **five paintings had to be removed by looking at them** (three framed museum
  photographs, one white-margin watercolour).
- Track 1's 12-photograph corpus has recorded provenance for only 5; **the other 7 came from a
  scratchpad directory shared with another agent this round**, so its absolute percentages are
  not independently reproducible. Its per-image rankings are.
- Track 4 used seven Wikimedia photographs with provenance recorded.

**`EveryRegisteredStyleIsPaintable` does not measure what its name says** — mark² = 10 px on a
256² synthetic gradient, 27× looser than the real default. That is why a broken area opening
passed review.

The standing warning should now be read at full strength: **any conclusion about a spatial stage
drawn from a synthetic fixture is unsafe, including the fixture the paintability test itself
uses.**

## Accuracy warnings

Read these before quoting any figure.

- **Both retune numbers (contrast 1.0, chroma 1.45) rest on track 2's self-curated 57-work
  corpus** — web reproductions, no colour management, subject matter confounded with group,
  deliberately van Gogh-weighted.
- **Track 1's absolute percentages are not reproducible as they stand.** See the corpus problem.
- **Track 3's §5.2 figures are an optimistic bound**, drawn from the synthetic-render set.
- **Track 3's doubled-contour rejection is argued from the `Refine` signature, not measured.** It
  is the only recommendation in this round resting purely on inference.
- **The "up to six overlapping contour lines" figure for Cézanne is `[relayed]`** from a search
  summary; the AIC digital publication returned 403 on both essays.
- **Nobody has rendered a dithered output**, so the appearance of divided colour is unmeasured
  even though its gamut gate now passes.

## Verification debt

Ranked by how much clearing each would change a decision.

1. **Build the corrected area opening and measure the postcondition.** About an hour of local
   work. Items 1 and 2 of the build order — the round's entire headline — rest on one sweep
   reaching zero, and two tracks diagnosed the cause differently enough that the fix should be
   confirmed rather than assumed.
2. **Curate a shared, provenance-checked photograph corpus and commit it.** Three of four tracks
   were compromised by fixture contamination this round, and each rediscovered the problem
   independently. This is the cheapest thing that would raise the quality of every future round.
3. **Confirm "86.1 dpi" in Lamberti et al. 2014 is dots per inch of the *painting*, not the
   file.** Every physical claim in correction 6 rests on it — including the first physical
   justification for `MarkScale`. Settled by checking published pixel dimensions for one of three
   named paintings.
4. **Re-measure floor strength after the merge lands.** Three tracks gave three answers, and the
   disagreement may dissolve once the stage it interacts with is fixed.
5. **Build and look at the doubled Cézanne contour** — the one inference-only rejection.
6. **Li et al., TPAMI 2012** — the only source on whether van Gogh's orientation field differs
   from a structure-tensor flow field, which is `FlowFlatten`'s premise. Stanford mirror
   `ECONNREFUSED`, PMC reCAPTCHA, IEEE and ACM paywalled.
7. **Werner 2026, *Color Research & Application* 51(3)**, on black in Impressionism and
   Post-Impressionism. Wiley 403, ORA file listing 404.
8. **AIC's Cézanne digital publication** — 403 on both essays; carries the contour figure above.

Items 1–4 are local work, cost little, and gate more than the three paywalled sources combined.
Clear them first.
