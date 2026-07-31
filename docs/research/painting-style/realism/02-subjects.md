# Subject matter in Realism

**Date:** 2026-07-31
**Track:** 2 of 4, Realism.
**Question:** does what a photograph *depicts* have any consequence for this converter, and if so
is any of it buildable without semantics?

**Relationship to prior research.** The parent [README](../README.md) rejects **automatic
focal-point detection as load-bearing** and **neural monocular depth**, and its pattern for a
subject-dependent effect is "reject the detector, expose a user control" — realised once, in
[tonalism/02-atmosphere.md §5](../tonalism/02-atmosphere.md), as a two-handle ramp. The Tonalism
round's track 4 concluded that "every thin dark structure in the corpus is subject matter, not
drawing; reaching it needs to know what the pixels depict". This report tests that boundary from
the other side: it measures whether subject-dependence exists at all in the *conversion*, and
finds that it does, that it is large, and that **it is not a semantic property** — a single
statistic the pipeline already has access to predicts it about twice as well as the subject label
does (§4.1).

**Claim marking** (enforced across `docs/research/`):

- `[verified]` — I read a primary or reputable source directly in this session, or it is
  arithmetic I performed on data in this repository, or a measurement I made by calling the real
  code.
- `[relayed]` — a secondary source or a search summary asserts it and I could not confirm it
  against the primary.
- `[inferred]` — my own reasoning from stated premises.

**Method note.** Every pipeline figure was produced by *calling* the shipped code from a throwaway
console project whose assembly name is `PaintTranslator.Tests` (so the app's `InternalsVisibleTo`
grant applies), referencing `PaintTranslator.csproj`. Renders go through the real
`StylePipeline.Render` with the real `StyleRegistry` rows; pre-map targets come from calling the
real `EdgePreservingFloor.Apply`; nearest-candidate answers come from the real
`NearestQuantiser`; region statistics come from the real `PaintabilityMetrics`. **Two pieces of
new arithmetic exist and each is gated against the shipped code before any figure derived from it
is used:**

- `Enumerator` re-enumerates `MixtureBuilder`'s sampling grid while keeping each mixture's
  composition. **Gate G0: 4,888 = 4,888 distinct colours, zero missing, zero extra**, on the
  seven-paint palette. `[verified]`
- `ScaledMerge` invokes the shipped `SmallRegionMerge` through a `RenderContext` whose mark has
  been scaled, which is the only way to ask what a threshold parameter on that stage would buy
  without reimplementing it. **Gate G1: at scale 1.00 the output is byte-identical to registering
  the shipped stage, on 36 of 36 photographs.** `[verified]`

The palette throughout is the same seven `Selectable` paints the Post-Impressionism and Tonalism
rounds used — Titanium White, Hansa Yellow Opaque, C.P. Cadmium Orange, Pyrrole Red, Cobalt Blue,
Phthalo Green (Y.S.), Bone Black — giving **4,888 candidates**, so figures are comparable across
rounds. The full 19-paint `PigmentLibrary.Selectable` set gives **84,063 candidates**, reproducing
the Fauvism round's count exactly. Corpus and provenance in §10.

`MixtureBuilder.RenderMixture` goes through `ToDisplayColor`, so every candidate colour here is
gamut-mapped 8-bit; all comparisons in this report are made in that same space, never against
unmapped spectral Lab.

---

## The answer, first

**Subjects convert measurably differently, the difference is large, and none of it is buildable as
a subject-aware feature — because the differences are shadows of image statistics the pipeline can
already compute.** Six results, in descending confidence:

1. **The app fails differently on faces than on foliage, and it fails in opposite directions on
   the two axes it is judged by.** Over 36 photographs in six subject strata: portraits are the
   *least* fragmented stratum by a wide margin (**36.7%** of pixels below one mark², against
   57.7% for landscape and **65.1%** for interiors) and simultaneously among the *worst* on colour
   (mean quantisation error **4.85 ΔE** against landscape's **3.21**, and the highest p95 of any
   stratum at 12.12). Still life is worst on colour (5.11) and interiors worst on fragmentation.
   `[verified]` §2.
2. **The one genuinely subject-dependent gamut fact is the dark end, and it is enormous.** The
   seven-paint candidate set's darkest colour is **L\* 11.00**; with all 19 selectable paints it is
   **L\* 6.43**. The share of pixels darker than the darkest achievable paint runs **3.4% for
   landscape, 7.5% interior, 10.0% urban, 16.5% portrait, 23.5% still life, 23.9% night**. Across
   the corpus this is the best single predictor of an image's quantisation error, r = **+0.616**.
   `[verified]` §2.4.
3. **Skin is not gamut-limited, it is sampling-limited — and one paint carries it.** Over 63,962
   hand-boxed skin pixels the residual against the seven-paint set is **5.357 ΔE**, well above the
   whole-corpus 4.14, but its *direction* is null: mean signed ΔL\* −0.34, ΔC\* −0.27, Δh +2.45°.
   Skin sits inside the gamut and the mixture grid cannot land on it. Leave-one-out over the 19
   selectable paints ranks **C.P. Cadmium Orange** as the one chromatic paint skin depends on
   (+1.198 ΔE when dropped, 3.7× the next), against **Bone Black** for landscape (+0.718). A
   six-paint palette chosen greedily for skin reaches **3.578 ΔE** — better than the seven-paint
   palette — and costs **10.670 ΔE on landscape**, 2.7× the landscape-tuned six.
   `[verified]` §3.
4. **The subject-dependence is not semantic.** A single statistic — the share of pixels the
   guided filter left locally flat — explains **69.7%** of the variance in sub-mark share across
   the 36 photographs. **The subject label explains 35.4%.** A per-subject style row would be a
   worse predictor of the defect it exists to fix than a number the pipeline can compute in one
   pass, and there is nothing left over for semantics to add. `[verified]` §4.1.
5. **`SmallRegionMerge` in Realism's empty slot 5 reaches exactly 0.00% on all 36 photographs and
   should not be built.** Ten minutes of looking overturned three tables of statistics: at the
   default mark the merge turns Half Dome into flat blue-grey blobs and a face into camouflage,
   for a mean **9.74 ΔE** from the shipped row and up to **34.13**. Hard-boundary share more than
   doubles, 9.29% → 20.51%. The mechanism is that Realism's floor is the weakest in the app, so
   the merge has ~231,000 tiny regions to consolidate and each is absorbed by its largest
   neighbour, cascading. `[verified]` §5.
6. **Realism cannot be made paintable by any setting of the stages it has, and both the doc
   comment and the test that would have caught that are wrong.** `EdgePreservingFloor`'s class
   comment names "44.3% of pixels in regions of four pixels or fewer" as the catastrophic case and
   says the stage "keeps every registered style far short of" it;
   `EveryRegisteredStyleIsPaintable` asserts Realism stays under **3.0%** at that same 4 px
   threshold. Measured at that exact threshold with the stage at Realism's declared defaults on
   real photographs: mean **41.67%**, **17 of 36 above 44.3%**, worst **71.15%** — 14× the test's
   ceiling. Sweeping the floor to its strongest sane setting (strength 3.0, edge 0.10) still
   leaves 34.27% below mark². `[verified]` §5.3, §8.

**In one line:** subjects differ, the difference is real and worth knowing, and every buildable
response to it is either already available to the user (paint selection) or already available to
the pipeline (a per-image statistic) — so the correct output of this track is a negative plus one
paint-selection fact.

---

## Contents

1. [What the realists painted, and why it does not reach the pipeline](#1-what-the-realists-painted-and-why-it-does-not-reach-the-pipeline)
2. [Do different subjects convert differently?](#2-do-different-subjects-convert-differently)
3. [Skin and flesh against the paint gamut](#3-skin-and-flesh-against-the-paint-gamut)
4. [Is any subject-dependence buildable without semantics?](#4-is-any-subject-dependence-buildable-without-semantics)
5. [Should Realism be the default row at all?](#5-should-realism-be-the-default-row-at-all)
6. [Picks](#6-picks)
7. [What not to build](#7-what-not-to-build)
8. [Corrections to prior research](#8-corrections-to-prior-research)
9. [Accuracy warnings](#9-accuracy-warnings)
10. [Corpus provenance](#10-corpus-provenance)
11. [Verification debt](#11-verification-debt)

---

## 1. What the realists painted, and why it does not reach the pipeline

The movement's subject programme is not in dispute and is not worth a section. French Realism from
about 1848 rejected Romanticism's idealisation and exotic subjects in favour of contemporary
ordinary life observed directly; Courbet painted unidealised peasants and workers at the scale
academic painting reserved for history and religion (*A Burial at Ornans*), and Millet
generalised and ennobled peasant labour (*The Gleaners*). `[relayed]` — Met Museum's
*Nineteenth-Century French Realism* essay and Concordia's *Creating the Modern*, read this session.

**It has no consequence for a pixel pipeline, for one structural reason: the app does not choose
the subject.** A converter is handed a photograph a user already took or already picked. Every
lever in the five slots — a pre-map filter, a Lab remap, a candidate transform, a quantiser, a
post-map selection — acts on colours and neighbourhoods. None can prefer a labourer to an
aristocrat, an unidealised face to a flattered one, or a field to an arcadia, because none of
those distinctions has a colorimetric or spatial signature. The one thing a Realist subject
programme *would* imply for a style row — "do not idealise" — is already what Realism's row does by
having an `IdentityRemap`: it is the only row in the registry that does not push the colour
anywhere. `[verified — StyleRegistry.cs:33-40]` `[inferred]` for the conclusion.

What the subject programme *is* good for is telling this track where to point the measurement.
Realism is the movement of the figure, the interior and the street rather than the sublime
landscape, and §2 finds that those are exactly the strata the converter is worst at — figures on
colour, interiors on fragmentation. That is the whole of the connection, and it is a research
prompt, not a feature.

---

## 2. Do different subjects convert differently?

### 2.1 The measurement

36 photographs, six subject strata, each rendered through the real `StylePipeline.Render` with the
real Realism row at its own default mark (`RenderContext.DefaultMarkPixels` × MarkScale 1.0), on
the seven-paint palette. `[verified — computed locally 2026-07-31]`

- `qe*` is the CIELAB distance between the **post-floor** pixel colour and the candidate the real
  `NearestQuantiser` picks for it. It isolates how well the paint set serves that colour from
  everything the cache and slot 5 do afterwards.
- `submark` is `PaintabilityMetrics.FractionInRegionsSmallerThan` at `ceil(mark²)`, which is the
  same threshold `SmallRegionMerge` uses.
- `smoothShare` is the share of pixels whose mean CIELAB distance to their four neighbours *after
  the floor* is below 0.5 — the part of the picture the source itself made flat. `bandDE` is the
  same measurement taken on the output over exactly those pixels: contrast the converter
  manufactured where the photograph had none.

| slug | subject | mark | src L\* | src SD | src C\* | qe mean | qe p95 | sub-mark % | regions | bound ΔE | hard % | smooth % | band ΔE |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| portrait-1 | portrait | 4 | 36.75 | 24.40 | 27.68 | 5.410 | 12.61 | 46.32 | 161,378 | 7.36 | 5.64 | 22.20 | 0.347 |
| portrait-2 | portrait | 6 | 29.08 | 31.47 | 8.29 | 5.068 | 9.83 | **15.01** | 84,854 | 8.03 | 5.24 | 66.99 | 0.284 |
| portrait-3 | portrait | 6 | 42.41 | 24.13 | 32.10 | **8.532** | **31.46** | 33.86 | 126,885 | 6.34 | 3.58 | 26.49 | 0.521 |
| portrait-4 | portrait | 6 | 54.94 | 19.41 | 35.46 | 5.238 | 9.81 | 36.30 | 150,277 | 7.51 | 3.33 | 22.97 | 0.949 |
| portrait-5 | portrait | 4 | 34.44 | 20.91 | 11.12 | 3.338 | 7.86 | 43.05 | 154,190 | 6.37 | 2.53 | 29.82 | 0.508 |
| portrait-6 | portrait | 6 | 52.86 | 19.49 | 14.79 | 3.701 | 8.21 | 25.91 | 99,476 | 6.20 | 1.25 | 45.99 | 0.454 |
| portrait-7 | portrait | 4 | 42.47 | 25.42 | 13.83 | 3.555 | 8.24 | 52.93 | 175,267 | 6.59 | 3.96 | 11.15 | 0.454 |
| portrait-8 | portrait | 5 | 36.47 | 18.92 | 32.21 | 3.955 | 8.94 | 39.98 | 144,609 | 6.81 | 4.20 | 34.54 | 0.520 |
| landscape-1 | landscape | 4 | 57.56 | 21.08 | 24.11 | 3.501 | 6.96 | 69.57 | 316,893 | 11.54 | 15.20 | 13.83 | 0.426 |
| landscape-2 | landscape | 4 | 61.88 | 17.22 | 18.94 | 3.246 | 5.79 | 40.97 | 113,218 | 6.76 | 0.90 | 27.10 | 0.267 |
| landscape-3 | landscape | 4 | 60.33 | 26.67 | 17.99 | **2.322** | 4.39 | 41.89 | 151,209 | 5.82 | 2.51 | 37.01 | 0.237 |
| landscape-4 | landscape | 4 | 53.32 | 26.15 | 24.71 | 3.942 | 7.84 | 54.90 | 173,008 | 6.60 | 2.21 | 12.88 | 0.600 |
| landscape-5 | landscape | 4 | 48.23 | 21.09 | 16.27 | 2.575 | 5.03 | 66.64 | 278,333 | 8.12 | 6.40 | 12.50 | 0.439 |
| landscape-6 | landscape | 4 | 50.93 | 21.82 | 26.13 | 3.653 | 6.77 | 72.39 | 376,067 | 17.74 | 32.97 | 23.76 | 0.118 |
| interior-1 | interior | 5 | 44.15 | 20.53 | 9.51 | 2.903 | 6.28 | 66.15 | 272,819 | 7.42 | 4.34 | 13.20 | 0.610 |
| interior-2 | interior | 6 | 37.20 | 17.06 | 7.93 | 2.785 | 6.23 | **83.18** | 401,333 | 8.66 | 8.36 | 5.78 | 0.873 |
| interior-3 | interior | 6 | 46.40 | 20.11 | 14.11 | 3.155 | 6.58 | 47.33 | 229,119 | 8.20 | 7.91 | 30.10 | 0.512 |
| interior-4 | interior | 3 | 41.02 | 16.83 | 17.48 | 3.092 | 7.02 | 47.84 | 130,678 | 6.64 | 4.31 | 7.22 | 0.773 |
| interior-5 | interior | 6 | 41.96 | 14.23 | 20.74 | 3.736 | 7.31 | 70.07 | 310,191 | 9.06 | 8.28 | 6.22 | 0.833 |
| interior-6 | interior | 5 | 48.08 | 19.37 | 11.92 | 3.735 | 7.09 | 75.96 | 347,686 | 10.05 | 10.78 | 5.65 | 1.090 |
| stilllife-1 | still life | 6 | 50.67 | 30.11 | 16.59 | 4.775 | 9.55 | 32.51 | 201,930 | 9.15 | 10.36 | 44.64 | 0.348 |
| stilllife-2 | still life | 5 | 23.89 | 32.88 | 16.63 | **8.814** | 10.95 | 26.22 | 106,222 | 11.60 | 21.89 | 61.63 | 0.141 |
| stilllife-3 | still life | 4 | 60.06 | 24.00 | **62.78** | 5.115 | 10.18 | 49.19 | 174,204 | 7.42 | 5.06 | 7.18 | 0.286 |
| stilllife-4 | still life | 4 | 41.75 | 27.88 | 27.48 | 4.488 | 10.07 | 83.60 | 363,694 | 11.89 | 17.71 | 3.31 | 0.222 |
| stilllife-5 | still life | 4 | 50.66 | 26.52 | 23.35 | 4.131 | 9.53 | 73.21 | 304,600 | 10.45 | 12.69 | 4.79 | 0.322 |
| stilllife-6 | still life | 5 | 40.29 | 23.49 | 13.03 | 3.361 | 9.19 | 73.50 | 310,792 | 8.95 | 11.28 | 7.43 | 0.598 |
| urban-1 | urban | 5 | 41.75 | 21.93 | 14.54 | 3.628 | 8.06 | 66.37 | 264,173 | 9.90 | 11.82 | 11.14 | 0.521 |
| urban-2 | urban | 5 | 44.11 | 24.32 | 6.77 | 2.764 | 6.54 | 60.95 | 251,748 | 8.26 | 8.31 | 18.20 | 0.492 |
| urban-3 | urban | 6 | 70.71 | 23.33 | 5.88 | 4.298 | 8.80 | 61.81 | 370,708 | 13.48 | 17.57 | 28.56 | 0.307 |
| urban-4 | urban | 5 | 52.12 | 26.56 | 7.93 | 3.771 | 7.58 | 53.18 | 231,753 | 10.73 | 12.81 | 17.75 | 0.392 |
| urban-5 | urban | 5 | 52.13 | 25.03 | 7.40 | 3.466 | 7.83 | 57.56 | 238,461 | 8.54 | 8.41 | 22.75 | 0.627 |
| night-1 | night | 4 | 23.30 | 19.32 | 19.45 | 5.928 | 10.32 | 55.04 | 191,922 | 9.94 | 12.74 | 29.00 | 0.257 |
| night-2 | night | 6 | 35.31 | 19.27 | 18.02 | 3.655 | 9.27 | 80.65 | 387,822 | 9.04 | 10.46 | 8.31 | 0.321 |
| night-3 | night | 3 | 27.08 | 14.49 | 23.37 | 4.114 | 9.68 | 39.48 | 118,300 | 9.49 | 10.14 | 38.47 | 0.504 |
| night-4 | night | 5 | 23.10 | 17.84 | 22.29 | 5.268 | 11.21 | 74.58 | 323,769 | 9.22 | 11.57 | 14.97 | 0.281 |
| night-5 | night | 4 | 44.39 | 27.29 | 13.82 | 3.855 | 9.51 | 72.47 | 296,135 | 11.89 | 17.80 | 6.99 | 0.545 |

### 2.2 Per-stratum aggregates

`[verified]`

| subject | n | src L\* | src C\* | **qe mean** | qe p95 | qe > 3 ΔE | **sub-mark %** | mean region px | bound ΔE | hard % | smooth % | band ΔE |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **portrait** | 8 | 41.18 | 21.93 | **4.85** | **12.12** | 58.7% | **36.67** | **6.71** | **6.90** | **3.72** | **32.52** | 0.50 |
| landscape | 6 | 55.38 | 21.36 | **3.21** | 6.13 | 46.4% | 57.73 | 2.93 | 9.43 | 10.03 | 21.18 | 0.35 |
| interior | 6 | 43.13 | 13.62 | 3.23 | 6.75 | 44.3% | **65.09** | 2.80 | 8.34 | 7.33 | **11.36** | **0.78** |
| still life | 6 | 44.55 | 26.64 | **5.11** | 9.91 | **65.9%** | 56.37 | 3.70 | 9.91 | **13.16** | 21.50 | 0.32 |
| urban | 5 | 52.16 | **8.50** | 3.59 | 7.76 | 52.3% | 59.97 | 2.74 | 10.18 | 11.78 | 19.68 | 0.47 |
| night | 5 | 30.64 | 19.39 | 4.56 | 9.99 | 58.4% | 64.44 | 2.50 | 9.92 | 12.54 | 19.55 | 0.38 |
| **all** | **36** | 44.49 | 19.02 | **4.14** | 8.96 | 54.5% | **55.29** | 3.79 | 8.94 | 9.29 | 21.68 | 0.47 |

**The dissociation is the finding.** Portraits are the best-behaved subject spatially and among the
worst chromatically; landscapes are the reverse; interiors are worst spatially and best
chromatically. **No single ranking of subjects exists** — which is already an argument against a
per-subject row, since a row would have to choose which axis to serve.

The spatial ordering has an obvious mechanism and it is measurable without knowing what the
picture is of: portraits are 32.5% locally flat after the floor against interiors' 11.4%, because a
portrait is a face and a defocused background while a church nave is stone, chairs and carved
detail from edge to edge. §4.1 turns that observation into the argument that closes this track.

**The manufactured-contrast result is a clean negative.** In the parts of the picture the source
made flat, the converter's own local contrast is **0.32–0.78 ΔE** and the share of those pixels
adjacent to a step above 5 ΔE is **0.65–2.13%**. **The converter does not band smooth gradients.**
Every fragmentation figure above is the converter faithfully following texture the photograph
already had. `[verified]`

### 2.3 Where in CIELAB the failures land

Mean quantisation ΔE by L\* decile, with each stratum's pixel share underneath.
`[verified — every second pixel, 36 photographs]`

| subject | 0–10 | 10–20 | 20–30 | 30–40 | 40–50 | 50–60 | 60–70 | 70–80 | 80–90 | 90–100 |
|---|---|---|---|---|---|---|---|---|---|---|
| interior | **6.41** | 2.42 | 1.80 | 2.44 | 3.52 | 3.92 | 4.02 | 4.46 | 5.45 | 5.03 |
| *% px* | 2.1 | 8.5 | 16.0 | 19.0 | 18.3 | 15.4 | 12.1 | 6.0 | 2.0 | 0.6 |
| landscape | **6.39** | 2.99 | 2.04 | 2.48 | 2.83 | 3.85 | 3.60 | 3.43 | 3.65 | 3.88 |
| *% px* | 0.5 | 4.1 | 9.1 | 16.0 | 16.0 | 15.8 | 10.6 | 8.3 | 8.4 | 11.1 |
| night | **8.47** | 4.03 | 2.64 | 3.11 | 4.03 | 4.37 | 4.80 | 6.29 | 7.17 | 6.64 |
| *% px* | **14.1** | 22.9 | 19.9 | 14.9 | 10.2 | 6.2 | 4.6 | 2.9 | 2.0 | 2.3 |
| portrait | **6.81** | 4.47 | 4.83 | 3.74 | 4.43 | 4.78 | 4.54 | 5.46 | 6.27 | 5.99 |
| *% px* | **15.8** | 10.6 | 10.1 | 14.0 | 10.5 | 10.1 | 14.2 | 8.7 | 3.1 | 2.8 |
| still life | **8.57** | 2.96 | 2.77 | 3.09 | 3.58 | 3.92 | 4.95 | 5.77 | 5.52 | 5.70 |
| *% px* | **18.5** | 11.4 | 8.0 | 7.6 | 8.0 | 8.2 | 9.9 | 12.6 | 11.9 | 3.8 |
| urban | **6.31** | 1.91 | 1.66 | 2.77 | 3.60 | 3.57 | 3.79 | 4.73 | 4.79 | 4.25 |
| *% px* | 5.1 | 8.0 | 9.4 | 10.3 | 11.7 | 13.8 | 13.3 | 9.7 | 7.0 | 11.8 |

**Every stratum's worst decile is the darkest one, by 2–3×, and the strata differ almost entirely
in how much of the picture lands there.** That is §2.4.

The two other places error concentrates, both narrow:

- **Chroma above C\* 70.** Portrait 21.99 ΔE over 2.8% of pixels; landscape 12.26 over 0.1%;
  night 12.96 over 0.5%. Real out-of-gamut clipping, and rare.
- **Hue 300° (magenta-violet) in portraits: 16.91 ΔE over 4.1% of pixels.** This is a property of
  the seven-paint round palette, which holds no magenta — its reddest paint is Pyrrole Red at
  h ≈ 36°. The saturated purple headscarf in portrait-3 and the crimson veil in portrait-4 have
  nowhere to go. Over the 19 selectable paints the Fauvism round's finding holds and no hue sector
  is empty. **It is a palette-choice fact, not a pipeline fact.** `[verified]`

### 2.4 The dark end: the one gamut fact that is genuinely subject-dependent

`[verified]`

| candidate set | candidates | L\* range | max C\* |
|---|---|---|---|
| seven-paint round palette | 4,888 | **11.00** – 98.17 | 88.2 |
| all 19 selectable | 84,063 | **6.43** – 98.17 | 92.9 |

Share of pixels darker than the darkest achievable colour:

| subject | seven paints (< L\* 11.00) | 19 paints (< L\* 6.43) |
|---|---|---|
| landscape | **3.36%** | 2.11% |
| interior | 7.58% | 4.36% |
| urban | 9.98% | 6.75% |
| portrait | 16.5% | 10.84% |
| still life | 23.5% | 16.81% |
| night | **23.9%** | 13.37% |

**A seven-times spread between the best and worst subject, on a property no stage can address.**
Across the 36 photographs this share correlates with an image's mean quantisation error at
**r = +0.616** — better than any other single statistic measured here (the next best is
`smoothShare` at +0.401, and mean source lightness at −0.362). `[verified]`

Real acrylic has the same limit — a masstone of Bone Black is not a void — so this is honest
physics rather than a defect, and the converter is already doing the right thing by clipping to
it. But it means **"the app is worse at night scenes and studio still lifes" is true, is not
fixable by any style row, and should be stated in the UI rather than engineered around.**
`[inferred]`

### 2.5 What this says about the movement

Realism as a movement is figurative, domestic and urban. On the corpus above those are precisely
the strata where the app is worst on colour (portrait 4.85, still life 5.11) and worst on
paintability (interior 65.1%), while the one subject the movement rejected as a sentimental
default — the sublime landscape — is the app's best stratum on colour (3.21) and holds only 3.4% of
its pixels below the black floor. **The app is at its best on the subject the movement was
reacting against.** That is a genuine irony and it is not actionable; it is recorded because it
tells the owner which strata deserve the next hour, not because anything in the registry should
change on account of it. `[inferred]`

---

## 3. Skin and flesh against the paint gamut

### 3.1 What skin measures as, in photographs

14 hand-drawn cheek and forehead patches across 8 portraits — **63,962 pixels, 27,787 distinct
8-bit colours**. Every box was drawn on a 10%-grid overlay, then checked twice on a montage of the
crops themselves; boxes that caught an eye, hair, lips, jewellery or backdrop were moved, and three
were dropped rather than moved. The boxes are recorded in §10.3 so the judgement is auditable.
`[verified]`

| statistic | p5 | p50 | p95 | mean |
|---|---|---|---|---|
| L\* | 30.9 | 55.1 | 82.6 | **55.78** |
| C\*ab | 17.2 | 29.8 | 41.8 | **29.19** |
| h_ab | 19.9° | 44.8° | 58.4° | — |

Mean a\* +20.65, b\* +20.00.

Published controlled colorimetry puts skin at hue 45–78°, C\* 9–30 and L\* roughly 60–73 across
global skin tones. `[relayed]` — Wang, Xiao & Wuerger (IS&T, *Measuring Human Skin Colour*) and a
2026 *Color Research & Application* k-means classification of global CIELAB skin data, both read as
search summaries this session, neither opened. **My distribution is wider on every axis and
displaced warm**, which is what an uncalibrated photograph corpus with mixed daylight, shade and
tungsten should do; the p5 hue of 19.9° is warm-lit and shadowed skin, not a skin tone. Treat the
external figures as a sanity check on the *centre*, which agrees, and not on the spread.

### 3.2 How well the gamut serves it

`[verified]`

| palette | candidates | skin ΔE mean | p95 | max | share > 3 ΔE |
|---|---|---|---|---|---|
| seven-paint round palette | 4,888 | **5.357** | 10.63 | 14.13 | 73.0% |
| six-paint `StyleTestFixtures` palette | 3,007 | 5.647 | 11.88 | 16.82 | 71.0% |
| all 19 selectable | 84,063 | **2.267** | 5.16 | 10.35 | 26.6% |

Skin is served **worse than the corpus average** (5.357 against 4.14 whole-image) by a small
palette, and **better than average** (2.267 against 3.084) by the full selectable set.

**But the residual has no direction, which is the load-bearing result.**

| palette | mean signed ΔL\* | mean signed ΔC\* | mean signed Δh |
|---|---|---|---|
| seven-paint | −0.34 | −0.27 | +2.45° |
| 19 selectable | −0.16 | +0.03 | −0.70° |

If skin were outside the gamut the nearest candidate would be systematically duller, darker or
hue-shifted. It is none of those. **Skin sits inside the achievable gamut and the mixture sampling
grid simply cannot land on it.** `[verified]` The gap between 5.357 and 2.267 is candidate density,
not reachability — which is the same phenomenon the Tonalism round found for very light tints,
arriving here in the mid-light warm band instead.

Where it concentrates, seven-paint palette:

| L\* decile | 20–30 | 30–40 | **40–50** | **50–60** | 60–70 | 70–80 | 80–90 |
|---|---|---|---|---|---|---|---|
| mean ΔE | 2.12 | 3.18 | **7.60** | **6.75** | 4.75 | 4.12 | 4.44 |
| % of skin px | 3.8 | 13.8 | 21.1 | 21.5 | 17.9 | 13.4 | 7.4 |

**42.6% of skin sits in L\* 40–60, and that is where the seven-paint set is at its worst on skin
by 2–3×.** With 19 paints the same band falls to 1.84 and 2.64.

### 3.3 Which selectable paints skin actually depends on

Rebuild the full 19-paint candidate set with one paint removed and measure the skin error, against
the landscape stratum's colour distribution as a control. `[verified — 19 rebuilds, 51 s]`

| paint dropped | candidates | skin ΔE | **Δ skin** | landscape ΔE | **Δ landscape** |
|---|---|---|---|---|---|
| *(none)* | 84,063 | 2.2666 | — | 2.5109 | — |
| Titanium White | 67,678 | 15.1159 | **+12.849** | 20.6616 | **+18.151** |
| **C.P. Cadmium Orange** | 71,512 | 3.4646 | **+1.198** | 2.5749 | +0.064 |
| Phthalo Green (Y.S.) | 74,451 | 2.5953 | +0.329 | 2.5837 | +0.073 |
| C.P. Cadmium Red Light | 71,790 | 2.5870 | +0.320 | 2.5336 | +0.023 |
| Bismuth Vanadate Yellow | 70,009 | 2.5450 | +0.278 | 2.6554 | +0.145 |
| Pyrrole Orange | 71,977 | 2.4398 | +0.173 | 2.5457 | +0.035 |
| Cerulean Blue, Chromium | 71,374 | 2.4064 | +0.140 | 2.6569 | +0.146 |
| **Bone Black** | 73,091 | 2.3777 | +0.111 | 3.2287 | **+0.718** |
| Cobalt Blue | 71,409 | 2.3712 | +0.105 | 2.5896 | +0.079 |
| Quinacridone Magenta | 72,467 | 2.3612 | +0.095 | 2.5334 | +0.023 |
| Ultramarine Blue | 73,463 | 2.3048 | +0.038 | 2.6254 | +0.114 |
| **Diarylide Yellow** | 70,755 | 2.2753 | +0.009 | 2.7573 | **+0.246** |
| Quinacridone Red | 72,392 | 2.2987 | +0.032 | 2.5275 | +0.017 |
| Pyrrole Red | 71,516 | 2.2946 | +0.028 | 2.5238 | +0.013 |
| Hansa Yellow Opaque | 71,117 | 2.2824 | +0.016 | 2.6321 | +0.121 |
| Dioxazine Purple | 76,080 | 2.2779 | +0.011 | 2.5227 | +0.012 |
| Phthalo Blue (G.S.) | 76,216 | 2.2676 | +0.001 | 2.5331 | +0.022 |
| Phthalo Green (B.S.) | 75,813 | 2.2677 | +0.001 | 2.5381 | +0.027 |
| Phthalo Blue (R.S.) | 75,813 | 2.2705 | +0.004 | 2.5703 | +0.059 |

**After white, skin depends on exactly one paint: C.P. Cadmium Orange, at 3.7× the next.**
Landscape depends on Bone Black at 6.5× *its* next chromatic paint, and on Cadmium Orange not at
all (+0.064). **The two subjects want different paints and the ranking is unambiguous.**

**No earth colour is selectable.** Burnt Sienna, Burnt Umber, Raw Sienna and Yellow Ochre — the
pigments a painter would actually reach for on a flesh palette — are all `ReflectanceDerived` and
withheld by `PigmentLibrary.Selectable`. `[verified — pigments.manifest.txt]` Cadmium Orange is
carrying the whole job those pigments normally share, which is why dropping it costs so much. If
the provenance tier is ever promoted, **flesh is the first thing to re-measure.**

### 3.4 Palettes chosen for a subject, and what they cost on another

Greedy forward selection from the 19 selectable paints, minimising pixel-weighted ΔE against each
target distribution. `[verified]`

| step | best palette for **skin** | ΔE | best palette for **landscape** | ΔE |
|---|---|---|---|---|
| 1 | Quinacridone Red | 44.13 | Phthalo Green (Y.S.) | 49.50 |
| 2 | + Titanium White | 31.22 | + Titanium White | 29.50 |
| 3 | + Bismuth Vanadate Yellow | 18.59 | + Bone Black | 19.61 |
| 4 | + Bone Black | 9.41 | + Hansa Yellow Opaque | 10.49 |
| 5 | + C.P. Cadmium Red Light | 5.31 | + Cobalt Blue | 5.17 |
| 6 | + C.P. Cadmium Orange | **3.578** | + C.P. Cadmium Orange | **3.952** |
| 7 | + Cerulean Blue, Chromium | 3.125 | + Cerulean Blue, Chromium | 3.633 |
| 8 | + Phthalo Green (Y.S.) | 2.861 | + Diarylide Yellow | 3.296 |

The two six-paint palettes share only white, black and Cadmium Orange. Cross-applied:

| palette | cands | **skin** | portrait | landscape | interior | still life | urban | night | **all** |
|---|---|---|---|---|---|---|---|---|---|
| round-7 | 4,888 | 5.357 | 5.120 | 3.890 | 3.799 | 5.571 | 4.048 | 5.374 | **4.680** |
| `StyleTestFixtures` 6 | 3,007 | 5.647 | 6.011 | 4.741 | 5.986 | 6.560 | 5.075 | 6.467 | 5.850 |
| **skin-6** | 3,035 | **3.578** | 7.312 | **10.670** | 4.627 | 7.816 | 4.980 | 8.627 | 7.215 |
| **landscape-6** | 2,980 | 7.730 | 7.845 | **3.952** | 4.817 | 6.892 | 4.381 | 6.660 | 5.991 |
| greedy-all-7 | 4,646 | 7.512 | 5.680 | 4.142 | 3.731 | 5.694 | 3.780 | 4.886 | 4.772 |
| greedy-portrait-7 | 4,629 | 4.683 | **5.099** | 5.447 | 3.730 | 6.440 | 3.990 | 5.059 | 4.982 |
| all 19 | 84,063 | 2.267 | 3.403 | 2.511 | 2.408 | 3.797 | 2.727 | 3.422 | 3.084 |

Three readings, and the third is the one that decides §4. `[verified]`

- **A subject-tuned six-paint palette is spectacular on its subject and catastrophic off it.**
  skin-6 costs **2.7×** the landscape-6 palette on landscape.
- **For a whole image the effect nearly vanishes.** The best greedy seven for *portraits as whole
  pictures* reaches 5.099 against the round-7 palette's 5.120 — a difference of **0.02 ΔE**. The
  round-7 palette is already close to the best general seven (4.680 against greedy-all-7's 4.772;
  greedy is myopic, so the round-7 palette may simply be better).
- **Skin is the only region where palette choice buys anything worth having**, because a face is a
  narrow region of colour space embedded in a picture that is mostly not a face.

### 3.5 Rendered, and looked at

Three crops at 1:1, source against renders. `[verified — rendered and inspected 2026-07-31]`

- **Round-7 on a face** is visibly waxy and orange-shifted; the shadow modelling in the cheek and
  under the eye collapses into two or three mottled patches. This is the 5.357 ΔE made visible and
  it is worse to look at than the number suggests, because the error is spatially correlated —
  a whole cheek moves together.
- **Skin-6 on the same face** is markedly better: hue correct, modelling continuous, the picture
  reads as a portrait rather than as a colour-separation.
- **Skin-6 on Half Dome** turns the sky purple-grey and the granite mauve. Unusable.
- **The 19-paint render is nearly indistinguishable from the source** on all three crops, which is
  what "sampling-limited, not gamut-limited" looks like.

**The looking agreed with the statistics here**, unlike §5, where it did not.

---

## 4. Is any subject-dependence buildable without semantics?

### 4.1 The variance argument, which closes the question

Across the 36 photographs: `[verified]`

| predictor of sub-mark share | r | r² |
|---|---|---|
| **`smoothShare`** — share of pixels locally flat after the floor | **−0.835** | **0.697** |
| mean region area (pixels ÷ regions) | −0.834 | 0.696 |
| source SD of L\* | −0.258 | 0.067 |
| mean source chroma | −0.116 | 0.013 |
| mark size | −0.117 | 0.014 |
| **subject label** (six-level, one-way η²) | — | **0.354** |

**One number the pre-map stage could emit as a by-product predicts the defect twice as well as
knowing what the photograph is of.** For quantisation error the same holds with a different
statistic: the subject label gives η² = 0.325, and the share of pixels below the candidate set's
darkest colour gives r = +0.616, r² = 0.379. `[verified]`

That is the whole answer to question 4. Subject-dependence in this converter is not a semantic
phenomenon that a detector would recover; it is a *summary* of local flatness and dark-pixel share,
both of which are one pass over the buffer. **A per-subject style row would be strictly worse than
a per-image number, and there is no residual for semantics to explain.** `[inferred]` from the
variance figures.

This also strengthens the parent README's rejection of automatic focal-point detection from a new
direction: the parent found image-independent centre bias beats image salience at explaining
fixations on paintings, i.e. that a detector loses to a constant. Here a detector would lose to a
one-line statistic that is *already computable inside slot 1*. `[inferred]`

### 4.2 The four options, costed

| option | what it would do | cost | verdict |
|---|---|---|---|
| **Per-subject style rows** ("Realism (portrait)", "Realism (landscape)") | pick different stage parameters by subject | 5 registry rows + a picker the user must get right | **No.** Explains 35% of the variance a free statistic explains 70% of, and the two axes rank subjects oppositely (§2.2), so a row would have to choose which one to serve |
| **A user-set "subject" handle** | user tells the app it is a portrait | ~1 combo box + branching defaults | **No.** Same information content as the row, moved to the user, and still worse than the statistic |
| **A paint-selection hint** ("for portraits, include Cadmium Orange") | text in the picker; no code path | ~0 | **Yes, as documentation only.** §3.3 is a real fact, and it costs a sentence |
| **An adaptive floor** — derive the floor's `edge` from the image's own flatness | close the loop the r² above says is there | ~30 lines in slot 1, plus a decision about whether a style may be image-adaptive | **Maybe, and it belongs to track 4.** §5.3 shows the floor is too weak a lever on Realism for this to fix the default, but it is the only place the finding could ever cash out |

**The pick is the third row, and it is a sentence, not a feature.** `[inferred]`

---

## 5. Should Realism be the default row at all?

Realism is `StyleRegistry.Default` and is now **the only row in the registry with an empty slot 5**
— Tonalism, Fauvism, Post-Impressionism and Abstract all register `SmallRegionMerge` in the
current working tree. `[verified — StyleRegistry.cs:33-40 against 50-136]` The Tonalism round put
it worst in the app at **51.30%** of pixels below its own mark². This corpus reproduces that from a
different session, resolution and subject mix: **55.29% mean, 15.01–83.60%.**

### 5.1 What registering `SmallRegionMerge` would do

Realism with `SmallRegionMerge` added to slot 5, and the same stage invoked through a scaled mark
to stand in for the threshold parameter the Tonalism round proposed as its build item 7. **Gate G1
passed on 36 of 36 photographs**: `ScaledMerge` at scale 1.00 is byte-identical to registering the
shipped stage. `[verified]`

| variant | sub-mark % | regions | bound ΔE | hard bound % | ΔE from shipped Realism |
|---|---|---|---|---|---|
| shipped (empty slot 5) | **55.29** | 231,492 | 8.94 | 9.29 | 0.00 |
| merge at 0.25 × mark | 40.40 | 121,085 | 10.19 | 12.25 | 2.56 |
| merge at 0.35 × mark | 21.28 | 31,061 | 12.08 | 16.01 | 6.34 |
| merge at 0.50 × mark | 12.60 | 13,393 | 12.47 | 16.99 | 7.76 |
| merge at 0.71 × mark | 5.75 | 6,546 | 13.13 | 18.58 | 8.88 |
| **merge at 1.00 × mark (the shipped stage)** | **0.00** | 3,834 | 13.95 | 20.51 | **9.74** |

Per stratum, sub-mark % / ΔE from shipped:

| subject | none | 0.25 | 0.35 | 0.50 | 0.71 | 1.00 |
|---|---|---|---|---|---|---|
| portrait | 36.7 / 0.0 | 28.4 / 1.0 | 17.6 / 2.3 | 11.1 / 3.1 | 5.2 / 3.9 | 0.0 / **4.5** |
| landscape | 57.7 / 0.0 | 57.7 / 0.0 | 23.9 / 8.1 | 12.0 / 10.1 | 4.5 / 11.3 | 0.0 / **11.9** |
| interior | 65.1 / 0.0 | 36.4 / 4.5 | 22.4 / 6.1 | 14.6 / 7.1 | 7.3 / 8.0 | 0.0 / 8.9 |
| still life | 56.4 / 0.0 | 46.3 / 1.9 | 23.5 / 7.5 | 13.4 / 9.7 | 5.8 / 11.4 | 0.0 / **12.6** |
| urban | 60.0 / 0.0 | 29.4 / 6.1 | 17.5 / 8.0 | 11.3 / 9.1 | 5.7 / 10.0 | 0.0 / 11.0 |
| night | 64.4 / 0.0 | 47.5 / 3.1 | 23.8 / 7.9 | 13.7 / 9.7 | 6.1 / 10.9 | 0.0 / 11.8 |

Two mechanical notes. The 0.25 row is an exact no-op on mark-4 images, because
`SmallRegionMerge` returns early when `minimumArea <= 1` — at mark 4 a quarter-mark squared is 1.
`[verified — SmallRegionMerge.cs:27-31]` And **the merge is the app's third confirmation that the
union-find rewrite reaches exactly 0.000000 in one pass**, now on 36 photographs across six
subjects.

### 5.2 Then I looked at it, and the statistics were wrong

`[verified — rendered at full size and inspected 2026-07-31]`

**Shipped Realism looks excellent.** At full size it is a slightly posterised photograph and
nothing else; on a face, a landscape and a street it is hard to tell from the source without a
1:1 crop. Whatever 55.29% means, it does not mean the picture looks broken.

**Merge at 1.00 is a disaster on three of three.** Half Dome dissolves into flat blue and grey
slabs with sprayed speckle at the joins; the old man's face becomes camouflage, with the cheek
broken into unrelated dark patches; the fruit stall loses every piece of fruit. **Merge at 0.35 is
already visibly wrong** on the landscape and mottles the face.

**The mechanism is Realism's floor, and it is why this works for other rows and not for this one.**
`SmallRegionMerge` assigns each sub-threshold region to its *largest* neighbour and accumulates
areas as it goes. On Tonalism or Post-Impressionism, the strong floor and the remap have already
flattened the picture, so the merge mops up speckle between large masses. Realism runs
`EdgePreservingFloor` at the stage's own weakest declared defaults — strength 1.0, edge 0.05 — so
it hands the merge **231,492 regions averaging 3.8 px each** and no large masses to attach
them to. The merge then invents the masses, and their boundaries are wherever the cascade happened
to stop. `[inferred]` from the mechanism plus the region counts; the renders are the evidence.

**Sub-mark share is therefore not a sufficient acceptance criterion.** A change that takes it from
55.29% to exactly 0.00% made every picture worse. That is a new caution for the whole directory and
it belongs beside the Tonalism round's warning about synthetic fixtures.

### 5.3 The floor is also not enough

Sweeping Realism's own slot-1 floor, which is the only other knob the row has:
`[verified]`

| floor | sub-mark % | regions | bound ΔE | hard bound % | ΔE from shipped | qe mean |
|---|---|---|---|---|---|---|
| **strength 1.0, edge 0.05 (shipped)** | **55.29** | 231,492 | 8.94 | 9.29 | 0.00 | 4.14 |
| strength 1.0, edge 0.10 | 49.84 | 191,976 | 7.92 | 7.00 | 2.08 | 4.11 |
| strength 1.0, edge 0.20 | 45.33 | 158,081 | 6.72 | 4.05 | 3.39 | 4.12 |
| strength 2.0, edge 0.05 | 45.99 | 180,769 | 8.50 | 8.59 | 2.31 | 4.07 |
| strength 2.0, edge 0.10 | 39.82 | 138,417 | 7.05 | 5.21 | 3.93 | 4.07 |
| strength 3.0, edge 0.10 | **34.27** | 111,516 | 6.46 | 3.87 | 5.00 | 4.04 |
| strength 2.0, edge 0.20 | 35.57 | 109,085 | **5.70** | **1.72** | 5.21 | 4.06 |

**The strongest setting worth looking at still leaves a third of the picture unpaintable**, and
rendered, strength 2.0 / edge 0.20 has the plastic, smeared look of an "oil paint" filter — on the
street scene it erases the cars' detail and the shop signs while leaving the composition
unchanged. Note the Tonalism round's ceiling on ε applies here too and for a different reason.

`edge` remains the better of the two knobs per unit of ΔE, reproducing the Tonalism round's
finding on a second style: edge 0.05 → 0.20 at strength 1 buys 10 points of sub-mark share and cuts
hard-boundary share from 9.29% to 4.05% for 3.39 ΔE, while strength 1 → 2 at edge 0.05 buys 9
points for 2.31 ΔE but leaves the picture harder (8.59% vs 4.05%). `[verified]`

### 5.4 The ruling

**Realism should stay the default, and the app should stop claiming it is paintable.**

The case for keeping it: it is the only row that does not move the colour, it is what the converter
did before styles existed, it is the row a user can check the app's central claim against, and it
is the best-looking render in the app at full size. The case against — that the first thing every
user sees is the least paintable output the app produces — is real but is an argument about a
*metric*, and §5.2 shows the metric is not tracking what a viewer sees on this row.

**What should change is the claim, not the row.** Three concrete options, cheapest first:

1. **Say so.** A row that renders a faithful, un-stylised conversion is not a brush-mark plan and
   should not be measured as one. `EveryRegisteredStyleIsPaintable` asserts Realism stays under
   **3.0%** at a **4 px** threshold on a 256² synthetic gradient; photographs give **41.67%** at
   that same threshold, 14× the ceiling. That assertion is measuring the fixture, not the row (the
   Tonalism round's correction 2 applies here too, and harder).
2. **Floor `edge` 0.05 → 0.10** on Realism: 55.29% → 49.84%, hard-boundary share 9.29% → 7.00%,
   for 2.08 ΔE, at no extra cost — ε is a scalar in the same two passes. This is the same lever
   and the same value the Tonalism round took, and it is the largest improvement available that
   does not change what Realism is. **It has not been looked at**; see debt 1.
3. **Do not register `SmallRegionMerge`, at any threshold.** §5.2.

---

## 6. Picks

### Pick 1 — write down that Cadmium Orange is the flesh paint. **One sentence.**

After Titanium White, **C.P. Cadmium Orange is the only selectable paint the skin distribution
depends on** (+1.198 ΔE when removed from the 19-paint set, 3.7× the next), and no earth colour is
selectable to share the job. Landscape depends on Bone Black instead (+0.718) and on Cadmium
Orange not at all (+0.064).

**Payoff:** a user converting a portrait with a palette that omits it pays 2.2–2.8 ΔE on every
face in the picture, visibly (§3.5).
**Cost:** a line in the paint picker or the docs. No code path, no branch, no detector.
**Why it is pick 1:** it is the only actionable subject-dependent fact this track found, and it
costs nothing.

### Pick 2 — floor `edge` 0.05 → 0.10 on Realism. **One line in `StyleRegistry`.**

Sub-mark share 55.29% → 49.84%, mean boundary ΔE 8.94 → 7.92, hard-boundary share 9.29% → 7.00%,
mean quantisation error unchanged (4.14 → 4.11), for 2.08 ΔE of movement. ε is a scalar inside the
same two guided-filter passes, so this is free at run time. §5.3.

**Risk:** nobody has looked at this specific setting; I rendered 0.20 and it was too much. Render
0.10 before taking it. This is the same lever, the same direction and the same value the Tonalism
round settled on for its own row, which is weak corroboration rather than independent evidence.

### Pick 3 — stop asserting that Realism is paintable, and correct the floor's doc comment.

`EdgePreservingFloor`'s class comment claims the stage "keeps every registered style far short of
that catastrophic case", naming 44.3% of pixels in regions of ≤4 px. Measured at that exact
threshold on 36 photographs with the stage at Realism's declared defaults: **mean 41.67%, 17 of 36
above 44.3%, worst 71.15%.** §8, correction 1. Free, and it stops the next person believing a
guarantee the code does not provide.

### Pick 4 — record the black floor as a UI fact, not an engineering problem.

The paint set's darkest achievable colour is L\* 11.00 at seven paints and L\* 6.43 at nineteen.
Night scenes and dark-ground still lifes put **23–24% of their pixels below it**, landscapes 3.4%.
This is the largest genuinely subject-dependent quantity in the report and no stage can address it,
because real paint has the same limit. A one-line note beside the converted image ("N% of this
photograph is darker than your paints can mix") would be honest and is ~10 lines.

---

## 7. What not to build

Each of these is something I went looking for and rejected on evidence. The parent, Abstract,
Fauvism, Post-Impressionism and Tonalism lists all still apply.

- **Per-subject style rows, in any form** — "Realism (portrait)", a subject combo box, or a
  detector. The subject label explains 35.4% of the variance in the defect it would be built to
  fix; a statistic slot 1 already computes explains 69.7%. And the two axes rank subjects
  oppositely, so a row would have to pick which one to serve. §4.1, §2.2.
- **A subject classifier of any kind**, neural or hand-crafted. Same argument, and it inherits the
  parent README's rejection of automatic focal-point detection without needing a separate one:
  there is no residual variance for it to explain.
- **A per-subject paint-selection *feature*.** A palette chosen for skin costs **2.7×** on
  landscape (10.670 against 3.952) and makes the *portrait as a whole picture* worse than the
  generic palette (7.312 against 5.120), because a portrait is mostly not a face. The fact is
  worth a sentence (pick 1); the mechanism is not worth a code path. §3.4.
- **`SmallRegionMerge` in Realism's slot 5, at full mark or at any scaled threshold.** Reaches
  exactly 0.00% and destroys the picture; 9.74 ΔE mean, 34.13 worst, hard-boundary share 9.29% →
  20.51%. §5.1, §5.2.
- **A "de-banding" or gradient-smoothing stage.** In the parts of the picture the source made
  flat, the converter's own local contrast is 0.32–0.78 ΔE and only 0.65–2.13% of those pixels sit
  next to a step above 5 ΔE. **The converter does not band smooth gradients**; there is nothing to
  fix. §2.2.
- **A chroma ceiling or chroma-clipping remedy aimed at skin.** The residual on skin has no
  direction — signed ΔC\* −0.27, ΔL\* −0.34, Δh +2.45°. Skin is inside the gamut. §3.2.
- **Denser sampling of the mixture grid as the skin fix.** `MixtureBuilder`'s own doc comment
  records the saturation (pairs 63 → 255 buys 0.91 → 0.83), and the Tonalism round measured
  quadrupling the opaque budget at 2.595 → 1.798 ΔE. The skin gap is 5.357 → 2.267 and it comes
  from *more paints*, not more samples of fewer.
- **A "flesh palette" preset naming pigments.** Same rejection and same reason as the Fauvism
  round's viridian preset and the Tonalism round's *Sea and Rain* five: the pigments a flesh
  palette would name — burnt sienna, yellow ochre, raw umber — are all `ReflectanceDerived` and
  the user cannot select any of them. Name the *one* paint that is selectable (pick 1) and stop.
- **Anything that reads the subject to place a focal point.** Beyond the parent's rejection: on
  this corpus the most-detailed and least-detailed strata are interiors and portraits, and both
  contain images that run the other way (portrait-7 at 52.9% sub-mark, interior-3 at 47.3%).
  A subject prior would be confidently wrong on a quarter of each stratum.
- **Treating `EveryRegisteredStyleIsPaintable`'s Realism ceiling as evidence.** It measures a 4 px
  threshold on a 256² synthetic gradient and passes at 3.0%; photographs give 41.67% at the same
  threshold. The Tonalism round's correction 2 applies here with a larger factor.
- **Amplitude-spectrum or any whole-image summary as a subject discriminator.** Not tested here,
  and not worth testing: the Tonalism round already showed a bad render passing the one summary
  statistic that had been proposed as a gate.

---

## 8. Corrections to prior research

**1. `EdgePreservingFloor`'s doc comment overstates the guarantee, and the shortfall is on the
default row.** The class comment names 44.3% of pixels in regions of four pixels or fewer as the
catastrophic case the stage exists to prevent, and says "Including the stage keeps every
registered style far short of that catastrophic case" — hedging only that it "does not by itself
guarantee any particular style clears a given fragmentation bar". The unhedged clause is the one
that fails. Measured at that exact ≤4 px threshold, on the output of the real pipeline, with the
stage at Realism's own declared defaults: **mean 41.67% across 36 photographs, 17 of 36 above
44.3%, maximum 71.15%.** By stratum: portrait 23.11, still life 43.15, urban 45.18, interior
47.72, landscape 47.85, night 51.40. Realism is not far short of the catastrophic case; on half
the corpus it is at or past it. The comment is right about the other four rows, which all have
slot 5 filled. `[verified — EdgePreservingFloor.cs:10-27 against the measurement]`

**1b. And the test that would have caught it measures the same threshold on a synthetic
gradient.** `EveryRegisteredStyleIsPaintable` renders a 256² σ3 noisy gradient, where
`DefaultMarkPixels` is **2**, so for Realism (MarkScale 1.0) `markSquared` is **4** — numerically
the same ≤4 px statistic as the doc comment's 44.3% — and records a ceiling of **3.0%**. On real
photographs the same statistic is **41.67% mean and 71.15% worst**, a factor of **14 to 24**.
`[verified — StyleBehaviourTests.cs:468-502]` This is the fourth consecutive round to find a
`Tests/Golden`-or-synthetic-fixture conclusion failing on photographs, and the first to find the
*same threshold* recorded twice, in a test and in a doc comment, both from the fixture.

**2. The Tonalism round's build item "register `SmallRegionMerge` in the empty slot 5" must not be
generalised to Realism.** That round measured it on Tonalism, where it is right. On Realism it
reaches exactly 0.00% and produces the worst-looking output in this report — 9.74 ΔE mean from the
shipped row, 34.13 worst, hard-boundary share more than doubled, and three of three renders
visibly destroyed. The difference is Realism's floor: it is the app's weakest, so the merge is
handed 231,492 regions averaging 3.8 px each and no large masses to attach them to. The item
is correct for rows that flatten first and wrong for the row that does not. `[verified]`

**3. Sub-mark share is not a sufficient acceptance criterion, and this is the first case where it
points the wrong way.** Every round since Fauvism has treated the fraction of pixels below mark² as
the paintability defect to be driven down. Here a change that drove it from 55.29% to exactly
0.000000 made every picture worse to look at. **Report a rendered comparison beside any
fragmentation improvement**, the way the Tonalism round now requires the notan gap beside a
value-mass claim. `[verified]`

**Confirmed, not corrected:**

- **Realism's sub-mark share reproduces across three sessions, three corpora and three
  resolutions**: 48.47% (Post-Impressionism round, 12 photographs at native size), 51.30%
  (Tonalism round, 15 photographs at an 800-px short edge), **55.29%** (here, 36 photographs at a
  960-px long edge across six subjects). The spread is explained by working resolution, which
  makes the mark smaller and sensor noise proportionally larger.
- **The rewritten `SmallRegionMerge` reaches exactly 0.000000 in one pass** on 36 further
  photographs spanning six subject strata, and `ScaledMerge` at scale 1.00 is byte-identical to it
  on all 36 — a second, independent confirmation of the Tonalism round's clearance of the
  Post-Impressionism round's verification debt 1.
- **The seven-paint palette gives 4,888 candidates and the 19 selectable paints give 84,063**,
  reproducing the Tonalism and Fauvism rounds' counts exactly.
- **`edge` beats `strength` per unit of ΔE on a second style.** The Tonalism round's track 3
  measured this on Tonalism; it reproduces on Realism (§5.3).
- **The Fauvism round's correction that the 19-paint candidate set has no empty hue sector** is
  consistent with what I see: the 300° failure in portraits (16.91 ΔE) is a property of the
  *seven-paint* palette, which holds no magenta, and disappears with the full selectable set.

---

## 9. Accuracy warnings

Read these before quoting any figure.

- **The interior stratum is five church naves and one dining room.** It is the least
  representative stratum in the corpus and it carries the worst sub-mark figure (65.09%). A
  domestic-interior stratum would very likely score better, because naves are stone, chairs and
  carved detail edge to edge. Every interior figure here should be read as "high-detail interior",
  not "interior". Two of the naves (interior-2, interior-5) are visibly tone-mapped, which raises
  local contrast and therefore fragmentation.
- **Six subjects × five or six images is a small sample.** The per-stratum means in §2.2 rest on
  5–8 photographs each and the within-stratum spread is often larger than the between-stratum
  difference — portrait sub-mark runs 15.01–52.93 around a mean of 36.67. **The rank order of the
  strata is the finding; the magnitudes are soft.**
- **The skin patches are one agent's eyeball judgements.** They were checked three times against
  crop montages and the boxes are recorded in §10.3, but a different annotator would move every
  number in §3. The distribution is 14 patches from 8 people, which is not a skin-tone survey.
- **The external skin colorimetry in §3.1 is `[relayed]` from search summaries.** Neither the
  IS&T paper nor the 2026 *Color Research & Application* study was opened. They are used only as a
  sanity check on the centre of the distribution.
- **The corpus is uncalibrated web JPEGs at 960 px.** The absolute quantisation errors depend on
  that: a larger working image makes the default mark larger and sensor noise proportionally
  smaller, and the Tonalism round's cross-session check showed absolute levels moving while
  ratios held. Compare ratios across rounds, not absolutes.
- **`smoothShare`'s 0.5 ΔE threshold is a number I chose**, not one derived from anything. The
  r² = 0.697 in §4.1 has not been checked for sensitivity to it.
- **Greedy forward selection is myopic.** The six- and seven-paint palettes in §3.4 are not proven
  optimal; `greedy-all-7` scoring worse than the round-7 palette on "all" (4.772 vs 4.680) is
  direct evidence of that. The *cross-application* result — that a skin palette costs 2.7× on
  landscape — does not depend on optimality and stands.
- **`smoothShare` and mean region area are near-collinear** (both r ≈ −0.83 with sub-mark share).
  §4.1's claim is that *some* image statistic beats the subject label, not that this particular one
  is the right one to build on.
- **Nothing in this report was rendered above 960 px**, and the visual judgements in §3.5 and §5.2
  are one agent's, on three to six images.
- **Everything here is measured against the working tree of 2026-07-31, not `HEAD`.** The tree
  carries uncommitted changes to `SmallRegionMerge.cs` (the union-find rewrite), `StyleRegistry.cs`
  (the retuned Tonalism row, which now registers the merge), `MixtureBuilder.cs`,
  `StyleBehaviourTests.cs` and four golden PNGs. **The Realism row itself is untouched by any of
  that**, so every Realism figure is a `HEAD` figure; but the claim in §5 that Realism is the *only*
  row with an empty slot 5, and the `EveryRegisteredStyleIsPaintable` ceiling of 3.0% quoted in
  correction 1b, are both working-tree facts.

---

## 10. Corpus provenance

**Every image was opened and looked at before use, twice** — once as a whole-corpus contact sheet
and once as a per-stratum sheet — and the skin crops a third time. This caught contamination that
no metadata check would have found, for the fifth round running.

### 10.1 Rejected on inspection

| candidate | why rejected |
|---|---|
| `The Making of a Great Illustrated Newspaper ILN0-1909-1225-0034.jpg` | a 1909 printed newspaper page, not a photograph |
| `AA (2/4/6/8 de 9).jpg` ×4 | perfect EXIF (Canon EOS-1D X Mark III), real photographs of real people — and **near-monochrome split-toned edits**. Would have poisoned the skin distribution and the portrait chroma figures. The same contamination mode the Tonalism round recorded |
| `Woman with colored makeup and flower petals on face (1) 01/02.jpg` | faces painted yellow and pink. Not skin |
| `Barclays / Market Street` (urban-08) | black-and-white edit |
| `Market Street through City Hall Arch, Philadelphia` | a sepia stereograph card, two images side by side |
| `Floris van Schooten - A Still-life with Bread, Cheese…` | a painting, returned by a "still life" search |
| `Le Cardon rose - Juan Sanchez Cotan` | a painting, same cause |
| `Interior view of the sitting room, Charles F. Lummis house` and one HABS negative | black-and-white archival photographs |
| `Osborne House ceramic plaques` ×2, `Ham House chairs` ×3 | close-ups of furniture and carving, not interiors |
| night-11 (magenta/blue light installation) | a coloured-light artwork; an outlier that would have dominated the night stratum |
| `Water reflection of a smiling woman planting rice` | a distant figure; almost no skin |

Eleven further candidates were lost to Wikimedia HTTP 429s and never retrieved. **The 429s were
user-agent-driven**, not rate-driven: the same URLs returned 200 immediately with a browser-like
`User-Agent` and a `Referer` header. Worth knowing for the next round, which will otherwise lose
an hour to it.

### 10.2 The corpus

36 files, all Wikimedia Commons, fetched 2026-07-31 via the `imageinfo` API at `iiurlwidth=900`
(served at 960 in most cases). All are colour photographs with a camera or a named photographer.

| slug | Commons file | licence | author | camera |
|---|---|---|---|---|
| portrait-1 | Indian woman portrait.jpg | CC BY-SA 4.0 | Alberto Buscató Vázquez | — |
| portrait-2 | Woman wearing għonnella.jpg | CC BY-SA 4.0 | Renata Apan | Nikon D850 |
| portrait-3 | Peasant woman with jewelry.jpg | CC BY-SA 4.0 | Alberto Buscató Vázquez | — |
| portrait-4 | Widow woman and farmer.jpg | CC BY-SA 4.0 | Pappu Ram Meena | Samsung SM-A750F |
| portrait-5 | Old man face.jpg | CC BY-SA 4.0 | Basile Morin | — |
| portrait-6 | Woman with hand-rolled cigarette.jpg | CC BY-SA 4.0 | Basile Morin | Canon EOS 5D IV |
| portrait-7 | Smiling young woman while dancing.jpg | CC BY-SA 4.0 | Basile Morin | — |
| portrait-8 | Kutia kondh woman 3.jpg | CC BY-SA 4.0 | PICQ | Canon EOS 20D |
| landscape-1 | Zagedan Lakes, Mountain cirque, Caucasus Mountains.jpg | CC BY 4.0 | Vyacheslav Argenberg | Sony SLT-A55V |
| landscape-2 | Breathtaking beauty of Dzukou Valley… (edit).jpg | CC BY-SA 4.0 | Samudra Bikash Hazarika | Nikon D3200 |
| landscape-3 | Abudelauri valley under clouds, early summer, Georgia.jpg | CC BY 4.0 | Vyacheslav Argenberg | Sony SLT-A55V |
| landscape-4 | Summer landscape Telemark (2690578882) - cropped.jpg | CC BY-SA 2.0 | Randi Hausken | Sony DSLR-A100 |
| landscape-5 | Mount Lorette panorama.jpg | CC BY-SA 2.5 ca | The Cosmonaut | Nikon D3300 |
| landscape-6 | Yosemite National Park…, Mirror Lake -- 2022.jpg | CC BY-SA 4.0 | Dietmar Rabich | Canon EOS 5D IV |
| interior-1 | Eaton Street House Interior, Key West Florida 1975 - Dining Room.jpg | CC BY 2.0 | Florida Keys Public Libraries | — |
| interior-2 | Saint Merri Church Interior 2, Paris, France - Diliff.jpg | CC BY-SA 3.0 | Diliff | — |
| interior-3 | Riga Cathedral Nave, Riga, Latvia - Diliff.jpg | CC BY-SA 3.0 | Diliff | — |
| interior-4 | Nave of Basilica Saint-Sernin - 2012-08-24.jpg | CC BY 3.0 | PierreSelim | Canon EOS 7D |
| interior-5 | Basilica of Saint Clotilde Interior, Paris, France - Diliff.jpg | CC BY-SA 3.0 | Diliff | — |
| interior-6 | Saint-Sulpice, Nave, Paris 20140515 1.jpg | CC BY-SA 3.0 | DXR / Daniel Vorndran | Nikon D800 |
| stilllife-1 | Aperitif plate with cheese, vegetables… .jpg | CC BY-SA 4.0 | HaJunkiyada | iPhone 13 Pro Max |
| stilllife-2 | Zitrone -- 2025 -- 7294.jpg | CC BY-SA 4.0 | Dietmar Rabich | Canon EOS 5D IV |
| stilllife-3 | Funchal, Mercado dos Lavradores -- 2025 -- 0997.jpg | CC BY-SA 4.0 | Dietmar Rabich | Canon EOS 5D IV |
| stilllife-4 | Funchal, Mercado dos Lavradores -- 2025 -- 0969.jpg | CC BY-SA 4.0 | Dietmar Rabich | Canon EOS 5D IV |
| stilllife-5 | Münster, Wochenmarkt -- 2017 -- 2332.jpg | CC BY-SA 4.0 | Dietmar Rabich | Canon EOS 70D |
| stilllife-6 | Münster, Wochenmarkt -- 2015 -- 7417.jpg | CC BY-SA 4.0 | Dietmar Rabich | Canon EOS 70D |
| urban-1 | Street scene in Aberystwyth. Wales.jpg | CC0 | Terry Kearney | Canon EOS-1D IV |
| urban-2 | Flea market Waterlooplein, Amsterdam.jpg | CC0 | Fons Heijnsbroek | Panasonic DMC-LX100 |
| urban-3 | Market Street, Bradford.jpg | CC BY 2.0 | Tim Green | iPhone 13 Pro |
| urban-4 | High Street Wells busy with people on market day.jpg | CC BY-SA 2.0 | Derek Voller | Panasonic DMC-FZ48 |
| urban-5 | Kaluga, street vendors in former city market area.jpg | CC BY-SA 2.0 | Serge Zykov | Sony DSC-RX100 |
| night-1 | Széchenyi Chain Bridge in Budapest at night.jpg | CC0 | Wilfredor | Nikon D300 |
| night-2 | Petit Champlain at night, Quebec city.jpg | CC0 | Wilfredor | Nikon D7200 |
| night-3 | 1 rocinha night 2014 panorama.jpg | CC BY-SA 3.0 | Chensiyuan | Nikon D800 |
| night-4 | Tikkurila Old railway station in night illumination.jpg | CC BY 4.0 | Ximonic (Simo Räsänen) | Canon EOS 5D IV |
| night-5 | Long exposure of Hong Kong street (125873797).jpg | CC BY 3.0 | Jan Philipp Kohrs | Canon EOS 60D |

The still-life stratum is market produce and one studio lemon rather than arranged tabletop still
life; the urban stratum is deliberately market-and-street rather than skyline, because that is the
Realist subject. Neither choice was made to reach a conclusion, and both are recorded so the
stratification can be judged.

### 10.3 Skin patches

Fractions of (width, height) as (x0, y0, x1, y1). Judged by eye on a 10%-grid overlay, then
corrected twice against a montage of the crops. Three boxes were dropped rather than moved.

| slug | boxes |
|---|---|
| portrait-1 | (0.42, 0.34, 0.50, 0.44), (0.44, 0.26, 0.52, 0.31) |
| portrait-2 | (0.465, 0.195, 0.487, 0.212), (0.510, 0.160, 0.545, 0.195) |
| portrait-3 | (0.31, 0.53, 0.37, 0.60) |
| portrait-4 | (0.38, 0.48, 0.44, 0.54), (0.54, 0.40, 0.62, 0.50), (0.42, 0.25, 0.58, 0.31) |
| portrait-5 | (0.40, 0.52, 0.50, 0.66), (0.45, 0.26, 0.60, 0.34) |
| portrait-6 | (0.32, 0.50, 0.39, 0.57), (0.44, 0.22, 0.56, 0.27) |
| portrait-7 | (0.47, 0.28, 0.53, 0.34) |
| portrait-8 | (0.40, 0.48, 0.46, 0.55) |

### 10.4 Probe

A console project with assembly name `PaintTranslator.Tests`, referencing
`PaintTranslator.csproj`, in the session scratchpad. Not in the repository, not staged. Files:
`Shared.cs`, `Program.cs`, `Skin.cs`, `Enumerator.cs`, `ScaledMerge.cs`. Two gates guard the two
pieces of new arithmetic and both are reported in the output rather than assumed: **G0** (mixture
re-enumeration vs `MixtureBuilder.Build()`, 4,888 = 4,888, zero missing, zero extra) and **G1**
(`ScaledMerge` at 1.00 vs the shipped `SmallRegionMerge`, byte-identical on 36 of 36 photographs).

---

## 11. Verification debt

Ranked by how much clearing each would change a decision.

1. **Render pick 2 (floor `edge` 0.10 on Realism) and look at it, on more than three subjects.**
   It is the only registry change this track recommends and I have rendered 0.05 and 0.20 but not
   0.10. Cheapest item on the list and it gates the only code change proposed. §5.3 already shows
   0.20 is too much.
2. **Whether the interior stratum's 65.09% survives a domestic-interior corpus.** Five of six
   interiors are church naves, and two are tone-mapped. If it does not survive, the "interiors are
   the worst stratum" headline weakens, though the portrait/landscape dissociation does not depend
   on it.
3. **Whether `smoothShare` still explains 70% of sub-mark variance at other thresholds and other
   image sizes.** §4.1 is the argument that closes question 4 and it rests on one threshold I chose
   (0.5 ΔE) on one working resolution. One probe run.
4. **Whether promoting the `ReflectanceDerived` tier changes the skin answer.** The earth colours
   a flesh palette would normally use are all in that tier. If they ever reach `Selectable`, both
   the leave-one-out ranking and the greedy skin palette need re-running, and pick 1 may change
   which paint it names.
5. **The skin distribution against a real skin-tone survey.** 14 patches from 8 people, all
   hand-boxed, all from Commons portraiture that skews toward South and South-East Asia and older
   subjects. The Monk Skin Tone scale or an open colorimetric dataset would settle whether the
   L\* 40–60 concentration that drives §3.2 is a property of skin or of this corpus.
6. **Whether a *user-selected* small palette behaves like the round-7 palette.** Every conversion
   figure here uses one seven-paint set for comparability with prior rounds. A real user picks
   whatever they own, and §3.4 shows the choice moves skin by 4 ΔE. The whole-image conclusions
   should be re-checked on two or three plausible real palettes.
7. **Whether Realism's fragmentation matters at print size.** Every judgement in §5.2 is at 960 px
   on screen. A 55% sub-mark share at a mark of 4 px may be invisible at 4,000 px and a mark of
   26 px, in which case the whole question 5 debate is about a working-resolution artefact. This
   is the largest unexamined assumption in the report.
8. **The two skin-colorimetry sources in §3.1**, neither opened.
9. **Whether the 300° hue failure in portraits is entirely the seven-paint palette.** I inferred it
   from the palette's composition and the 19-paint result; I did not measure the same statistic
   under the 19-paint set per stratum.

Items 1–3 are local work, cost about an hour between them, and gate everything this track
recommends.
