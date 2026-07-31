# The Colour of Post-Impressionism

**Date:** 2026-07-30
**Track:** colour, on Post-Impressionism — "what should this style's colour treatment be, and
what should the pipeline do to produce it?"
**Shipped state under examination:** `Imaging/Styles/StyleRegistry.cs:92-113` — mark scale
**1.6**, `EdgePreservingFloor` strength **3.0**, `ToneAndChromaRemap` contrast **1.1** /
chroma **1.3**, `KeepAllCandidates`, `NearestQuantiser`, **no post-map stage**.

**Relationship to prior research.** Extends [../01-colour-theory-in-practice.md](../01-colour-theory-in-practice.md),
[../abstract/04-colour-and-palette.md](../abstract/04-colour-and-palette.md) and
[../fauvism/03-colour.md](../fauvism/03-colour.md). It **overturns the per-hue chroma
ceiling** that both earlier rounds queued as a top build item (§4), **corrects the Fauvism
round's floor-versus-contrast framing** (§5), and **makes the optical-mixing measurement**
the parent README has carried as build-order item 1 since 2026-07-27 (§6).

**Claim marking:** `[verified]` = read the primary source directly, or computed it locally
this session; `[relayed]` = a secondary source or search snippet asserts it and I could not
open the primary; `[inferred]` = my own reasoning.

**Method note, because the last colour track's numbers failed on this.** Every chroma figure
below comes from *calling* `ToneAndChromaRemap.Map` and `NearestQuantiser.Map` on a real
`CandidateSet` built by `MixtureBuilder`. `ScaleChroma` is never transcribed. The per-hue
ceiling arrays are obtained by reflecting onto the shipped private
`StylePipeline.MaximumChroma` and `StylePipeline.MaximumChromaByHue`, so they are the exact
arrays `Render` builds. Full method in §10.

---

## Conclusions, first

**1. "Post-Impressionism" does not name one colour treatment, and the app's single row must
choose the van Gogh–Gauguin half of it.** Measured over a provenance-checked 57-work corpus:
mean C\*ab is **30.3 for van Gogh, 22.5 for Gauguin, 15.3 for Cézanne, 16.6 for Seurat and
Signac** — against **14.7 for Impressionism** and **16.3 for photographs**. Cézanne and the
Neo-Impressionists sit *inside* the Impressionist distribution on every colour statistic I
measured. The umbrella's within-group chroma SD is **9.27** against **3.57** for
Impressionism. Half of Post-Impressionism is colorimetrically Impressionist; only van Gogh
and Gauguin separate. `[verified — computed locally 2026-07-30]`

**2. Chroma 1.3 is slightly low but it is the right kind of number, and the knob
under-delivers by more than the setting is wrong by.** On six real photographs the shipped
knob of 1.3 realises **×1.209** of source mean chroma. The corpus target — Post-Impressionist
C\*mean 22.40 against photographic 16.29 — is **×1.375**, and the pipeline reaches an output
C\*mean of 22.4 at a knob of about **1.42**. So the correction is one notch, not a sign
error. `[verified]`

**3. Contrast 1.1 is wrong by sign, exactly as the Fauvism round found for Fauvism.** The
measured target is a rendered L\*sd of **×0.729** of source; the shipped settings deliver
**×0.921**. `[verified]`

**4. The per-hue chroma ceiling — queued as a top item by both the Abstract and the Fauvism
rounds — is already 90% built, and the last 10% is measurably not worth building.**
`RenderContext.AchievableMaxChromaByHue` (36 bins) and `AchievableMaxChromaForHue` already
exist and are populated from the candidate set by `StylePipeline.MaximumChromaByHue`.
**Nothing calls the accessor.** Wiring it changes Post-Impressionism's realised mean chroma
from ×1.209 to **×1.198** and its mean hue drift from 17.3° to **17.2°**. Even at Fauvism's
1.8 the deltas are ×1.558 → ×1.521 and 14.6° → 14.3°. The reason is structural: the tanh
knee's weight is `(gain − 1)/2`, so at gain 1.3 the ceiling governs **15%** of the transform.
`[verified]`

**5. Hue drift is a palette-gap defect, not a chroma-knob defect.** On real photographs the
mean hue displacement is **17.2° with no remap at all** and **17.3°** at the shipped
settings. Raising chroma *reduces* it (14.6° at gain 1.8). On a full-hue-circle source with a
green-bearing palette, the identity remap already drifts **30.2°** in magenta, where that
palette has no paint. `[verified]`

**6. Broken colour is a real gamut extension, and the size of it is now measured.** Over
4,631 distinct colours from six landscape photographs and the six-paint fixture palette
(3,007 candidates): mean ΔE to the nearest single achievable mixture is **6.06**; allowing
two candidates juxtaposed 1:1 and averaged as radiance takes it to **2.15**, a **2.8×**
reduction. 93.5% of target colours improve, 62.4% by more than 2 ΔE. **The gain is in
lightness, not chroma** — mean |ΔL\*| error falls 1.59 while mean C\* is unchanged (−0.39).
`[verified]` The parent README's directional prediction is confirmed; its chroma claim is
not.

**7. The shipped Post-Impressionism is the most fragmented style row in the registry.** On
the same six photographs, pixels in regions below the style's own mark²: Abstract 2.45%,
Fauvism 3.65%, Tonalism 30.07%, **Post-Impressionism 38.30%**, Realism 43.18%. Fauvism and
Abstract both acquired `SmallRegionMerge`; Post-Impressionism has no post-map stage at all.
Adding the existing stage takes it to **25.84%** at zero cost in chroma. `[verified]`

**Three picks and three rejections in §8 and §9.**

---

## Contents

1. [The boundary problem, ruled on evidence](#1-the-boundary-problem-ruled-on-evidence)
2. [The corpus, and what it measures](#2-the-corpus-and-what-it-measures)
3. [Is Post-Impressionist colour distinguishable?](#3-is-post-impressionist-colour-distinguishable)
4. [Chroma: what 1.3 actually realises, and the ceiling that does not matter](#4-chroma-what-13-actually-realises-and-the-ceiling-that-does-not-matter)
5. [Contrast and value](#5-contrast-and-value)
6. [Optical mixing and broken colour — the queued measurement](#6-optical-mixing-and-broken-colour--the-queued-measurement)
7. [What the painters themselves said, and what it licenses](#7-what-the-painters-themselves-said-and-what-it-licenses)
8. [Three picks](#8-three-picks)
9. [What not to build](#9-what-not-to-build)
10. [Method](#10-method)
11. [Verification debt](#11-verification-debt)

---

## 1. The boundary problem, ruled on evidence

**Ruling: keep one style row; target the van Gogh–Gauguin axis; do not split.**

The label is retrospective and was improvised. Roger Fry organised *Manet and the
Post-Impressionists* at the Grafton Galleries, 8 November 1910 – 11 January 1911, and the
term was coined during the planning when Desmond MacCarthy observed they had no shorthand
for the period; Fry supplied one. `[relayed — consistent across
[TheCollector](https://www.thecollector.com/manet-and-the-post-impressionists-roger-frys-1910-exhibition/),
the [Fortnightly Review](https://fortnightlyreview.co.uk/2017/10/fry-sickert-post-impressionism/)
and [Tate's Camden Town Group research](https://www.tate.org.uk/art/research-publications/camden-town-group/critics-r1105712);
I did not read Fry's catalogue]` A label invented to fill a gap in a press release is not a
colour theory, and the measurements in §2 show it is not one statistically either.

What the corpus says about the four candidate targets:

| Sub-group | n | C\*mean | ÷ Impressionist | ÷ photograph | local ΔC\*/ΔL\* |
|---|---|---|---|---|---|
| **van Gogh** | 8 | **30.29** | **2.07×** | **1.86×** | 0.804 |
| **Gauguin** | 5 | **22.50** | **1.53×** | **1.38×** | 0.755 |
| Cézanne | 4 | 15.35 | 1.05× | 0.94× | 0.558 |
| Seurat + Signac | 5 | 16.60 | 1.13× | 1.02× | 0.656 |
| Toulouse-Lautrec | 1 | 15.91 | 1.09× | 0.98× | 0.629 |
| *(Impressionist control)* | 13 | 14.66 | — | 0.90× | 0.554 |

`[verified — computed locally]`

**Cézanne's colour is Impressionist colour.** So is Seurat's and Signac's. Their local
chromatic contrast ratio (0.56, 0.66) brackets the Impressionist 0.554. Their mean chroma is
within 13% of it. Nothing in a chroma-and-contrast pipeline can render "Cézanne" as distinct
from "Impressionism", and the app has no Impressionism row, so a Cézanne row would render as
Realism with a stronger floor. `[inferred, from verified numbers]`

That is not a claim that Cézanne and Seurat are stylistically the same as Monet. It is a
claim about the axis this pipeline operates on. **Cézanne's difference is the constructive
plane and Seurat's is the divided touch — both spatial**, which is the same conclusion the
Abstract round reached about abstraction and the Fauvism round reached about flatness, now
reached a third time from colorimetry. `[inferred]`

**Do not split the row.** Two of the four sub-styles would be colorimetrically empty, and the
one that is genuinely separable on a non-colour axis — Seurat's divisionism — is already
scoped in the parent README as "broken colour at mark scale", shared with Impressionism and
Pointillism. Building it under "Post-Impressionism" would repeat the mistake the Fauvism
round avoided when it ruled out the 1904–05 divisionist phase.

---

## 2. The corpus, and what it measures

The Fauvism round's colorimetry is the only measured placement of any of these movements, and
its own top verification debt is that its corpus was opportunistic — its first "photograph"
control was a Derain, caught by inspecting the render. Its debt item 4 asks precisely my
question: *does the ΔC\*/ΔL\* separation hold against Post-Impressionism specifically?* This
section answers it.

**Corpus: 57 images, provenance-checked twice.** 23 Post-Impressionist, 13 Impressionist,
7 Fauve, 14 photographs. Sources: English Wikipedia article lead images resolved through the
API, plus named Commons files. Every painting had to be confirmed as its expected artist by
the Commons `extmetadata` Artist credit **or** by the filename; every photograph had to carry
camera EXIF Make *and* Model *and* a `DateTimeOriginal` in 1990 or later.

**The automated checks caught two classes of error the Fauvism round's method would not
have.**

- **A scanned 1899 comic map passed the "has a camera" test** on a Better Light Super6k
  scanning back and would have entered the photograph control group. The capture-date rule
  removed it. `[verified]`
- **Five paintings had to be removed after visual inspection**, which no metadata test
  catches: three museum photographs including the gilt frame and gallery wall (two Derain,
  one Signac), and one Signac watercolour photographed with a large white paper margin
  (L\*mean 79.6, C\*mean 9.4 — it would have dragged the Neo-Impressionist group toward the
  photograph control). **Inspecting the render remains mandatory.** `[verified]`

Statistics are whole-image over a ~700px longest edge, sRGB → CIELAB through the app's own
`PalettePhotoConverter.RgbToLab`. "Local ΔC\*/ΔL\*" follows the Fauvism round's definition
exactly: mean absolute C\* and L\* difference to the pixel *r* away horizontally and
vertically, *r* = short side ÷ 60, then the ratio.

| Group | n | L\*mean | **L\*sd** | L\*range (p5–p95) | **C\*mean** | C\*p95 | Hue entropy | **ΔC\*/ΔL\*** |
|---|---|---|---|---|---|---|---|---|
| **Post-Impressionist** | 23 | 47.92 | **17.02** | 54.18 | **22.40** | 45.99 | 3.454 | **0.711** |
| Impressionist | 13 | 52.44 | 18.87 | 59.83 | 14.66 | 32.19 | 3.670 | 0.554 |
| Fauve | 7 | 55.81 | 18.43 | 60.31 | 27.49 | 55.46 | 4.050 | 0.875 |
| Photograph | 14 | 49.57 | 23.36 | 71.14 | 16.29 | 34.81 | 2.707 | 0.492 |

`[verified — computed locally 2026-07-30]`

Dispersion, because the means hide most of the story:

| Group | C\*mean SD | C\*mean range | ΔC\*/ΔL\* SD | ΔC\*/ΔL\* range |
|---|---|---|---|---|
| Post-Impressionist | **9.27** | 9.9 – 50.3 | 0.163 | 0.46 – 1.09 |
| Impressionist | 3.57 | 7.8 – 23.6 | 0.142 | 0.30 – 0.88 |
| Fauve | 6.01 | 19.2 – 37.6 | 0.239 | 0.58 – 1.24 |
| Photograph | 6.74 | 6.6 – 32.2 | 0.121 | 0.25 – 0.74 |

**Caveats, in order of how much they could move a conclusion.**

1. **Reproduction fidelity.** These are web scans with unknown colour management, compared
   against modern digital photographs with a modern tone curve. The painting-versus-painting
   comparisons (Post-Imp vs Impressionist vs Fauve) come from the same source population and
   are the safe ones. **The painting-versus-photograph ratios carry a systematic confound and
   should be treated as calibration, not measurement.**
2. **Subject matter is confounded with group.** The 14 photographs are all Commons featured
   landscapes; the paintings include interiors, portraits and street scenes. Hue entropy in
   particular is likely inflated for the paintings by subject alone, and I would not quote the
   hue-entropy row as evidence of anything.
3. **n is small and I report no significance tests.** Every range above overlaps every other.
4. **The Post-Impressionist group is deliberately unbalanced** — 8 van Gogh against 4 Cézanne
   — because that is roughly the balance of what the label denotes in general use. A
   Cézanne-heavy sample would move C\*mean down by several units. This is the strongest
   argument for §1's ruling rather than a defect in it.

---

## 3. Is Post-Impressionist colour distinguishable?

**From Impressionism: yes, on chroma, and this is the first measurement of it I am aware
of.** C\*mean **22.40 vs 14.66, ×1.53**; C\*p95 45.99 vs 32.19, ×1.43; local ΔC\*/ΔL\* 0.711
vs 0.554, ×1.28. Value structure is essentially identical (L\*sd 17.02 vs 18.87, ×0.90).
`[verified]`

**From Fauvism: barely.** C\*mean 22.40 vs 27.49 is only ×1.23, and van Gogh alone (30.29)
is *above* the Fauve mean. Local ΔC\*/ΔL\* 0.711 vs 0.875 — and van Gogh's 0.804 sits inside
the Fauve range of 0.58–1.24. **The Fauvism round's ΔC\*/ΔL\* separator does not separate
Fauvism from Post-Impressionism; it separates twentieth-century high-chroma painting from
photographs.** That clears their verification debt item 4, in the direction they feared.
`[verified]`

**From a photograph: yes, on value range, and that is the more robust of the two.** Every
painting group has a *lower* L\*sd than the photographs (17.0 / 18.4 / 18.9 vs 23.4) and a
*lower* p5–p95 range (54 / 60 / 60 vs 71). The direction is unanimous across 43 paintings and
three movements, which is more than can be said for the chroma comparison. `[verified]`

**The published literature still contains no colorimetry per movement.** The Fauvism and
Abstract rounds both searched and found none; I searched again specifically for
Post-Impressionism and found none. Every large computational study of painting measures
spatial structure — Sigaki/Perc/Ribeiro convert to greyscale outright; Graham & Field and
Redies measure amplitude spectra and edge entropy; Kim/Son/Jeong measure box-counting
dimension of the colour cloud, not its chroma. `[verified — the absence]`

**One directly relevant new source surfaced.** J. S. Werner, *Black in Impressionism and
Post-Impressionism: Art, Color Vision, and Psychophysics*, *Color Research & Application*
51(3), published 8 April 2026, University of Oxford. It separates Seurat's *Le Chahut*
digitally into chromatic and achromatic components and reports that **the achromatic image
carries the spatial detail — edges and borders — while the chromatic image carries the large
surfaces**. `[relayed — Wiley returned 403 and the ORA file listing 404; abstract and finding
via the ORA record and search snippets. A CC BY-NC-ND PDF is said to exist.]`

If that holds, it is the Post-Impressionist statement of the same asymmetry S-CIELAB encodes
(band-pass luminance, low-pass chroma, `[verified via ../01 §2.1]`) and it is an argument for
the shape this pipeline already has: keep value edges crisp in slot 1, spread chroma broadly
in slot 2. `[inferred]`

**The one aesthetic-preference study ever run on Post-Impressionist paintings tells against a
mean-chroma knob.** 40 van Gogh landscape oils, cropped square, rated by adults and shown to
infants. Adult pleasantness was predicted by **standard deviation of saturation (β = 0.404,
p = .003)** and **proportion of green pixels (β = 0.383, p = .005)**, with straight-edge
density and 1-D fractal dimension entering negatively; F(5,34) = 7.25, p < .001,
adj. R² = .445. **Mean saturation is not in the model.** Infant looking was predicted by
SD of saturation too (β = 0.278, p = .048). `[verified — fetched
[PMC10399602](https://pmc.ncbi.nlm.nih.gov/articles/PMC10399602/)]`

The Abstract round cited this paper and correctly noted its stimuli were van Gogh landscapes,
which made it a poor fit for abstraction. **For this track it is exactly on target**, and it
says the same thing the Abstract round found in Mallon et al.: the *spread* of saturation
carries the evidence, the *level* does not.

Measured against the shipped pipeline, the chroma knob does raise the spread as well as the
level — output C\*sd is 10.99 at gain 1.0, 13.52 at 1.3, 16.61 at 1.8 on the six photographs.
`[verified]` So a chroma gain is not *opposed* by this evidence. But it is a weak instrument
for the statistic that has support, and a candidate-set restriction is a stronger one — the
Fauvism round measured masstone-biased candidates delivering the same mean chroma at a higher
SD. `[relayed via ../fauvism/03-colour.md §7.1, whose own numbers carry that report's
transcription caveat]`

---

## 4. Chroma: what 1.3 actually realises, and the ceiling that does not matter

### 4.1 Ask versus deliver, by hue, at gain 1.3

Full-hue-circle source (360×360, HSV S = 0.55, V ramped 0.25→0.85 — the committed golden has
no pixels between 150° and 270°, so it cannot show this), six-paint fixture palette, 3,007
candidates, scalar ceiling 89.32, Post-Impressionism's own contrast 1.1 and chroma 1.3, run
through the real `ToneAndChromaRemap.Map` and `NearestQuantiser.Map`:

| source hue | n | source C\* | ask | **delivered** | **realised gain** | per-hue ceiling | hue drift |
|---|---|---|---|---|---|---|---|
| 0–30 | 8,885 | 35.1 | 45.0 | 45.1 | 1.28× | 61.6 | 1.9° |
| 30–60 | 7,434 | 31.3 | 40.2 | 40.5 | 1.30× | 84.7 | 2.7° |
| 60–90 | 7,125 | 30.8 | 39.5 | 40.5 | 1.32× | 82.8 | 3.9° |
| 90–120 | 12,621 | 40.1 | 51.2 | 49.7 | 1.24× | 74.1 | 3.5° |
| 120–150 | 21,516 | 47.2 | 60.0 | 52.0 | 1.10× | 51.4 | 9.8° |
| 150–180 | 10,857 | 33.6 | 43.1 | 31.5 | **0.94×** | 31.4 | 18.0° |
| **180–210** | 6,129 | 24.6 | 31.7 | 15.7 | **0.64×** | 17.1 | 12.5° |
| 210–240 | 4,356 | 21.0 | 27.2 | 17.3 | **0.82×** | 15.5 | 15.0° |
| 240–270 | 5,005 | 23.3 | 30.1 | 25.8 | 1.11× | 21.8 | 11.8° |
| 270–300 | 11,650 | 38.5 | 49.1 | 47.6 | 1.24× | 63.2 | 3.0° |
| 300–330 | 21,821 | 49.5 | 62.8 | 54.4 | 1.10× | 57.3 | 7.0° |
| 330–360 | 12,201 | 40.9 | 52.2 | 47.4 | 1.16× | 55.1 | 3.9° |
| **ALL** | 129,600 | 38.9 | — | **44.0** | **1.13×** | — | **7.3°** |

`[verified — computed locally]`

A knob labelled 1.3 produces between **0.64× and 1.32×**. That reproduces the Fauvism round's
qualitative finding at a gain 0.9 lower than the one they tested, and confirms it against the
real stage rather than a transcription. On real photographs, whose colours are not spread
uniformly round the circle, the overall figure is **×1.209**.

### 4.2 The per-hue ceiling is already built, and it does not help

`RenderContext` already carries `AchievableMaxChromaByHue` — 36 ten-degree bins, populated
from the candidate set by `StylePipeline.MaximumChromaByHue`, with an empty-sector fallback to
the nearest populated neighbour — and exposes `AchievableMaxChromaForHue(a, b)`.
**`ToneAndChromaRemap.Map` calls `context.AchievableMaxChroma`, the scalar. Nothing in the
codebase calls the per-hue accessor.** `[verified — read the source; `grep` for
`AchievableMaxChromaForHue` returns its own definition and no call site]`

So the Abstract round's build item 1 and the Fauvism round's recommendation B are, in effect,
one line away from done. I wired that one line through a wrapper `ILabRemap` that hands the
real `ToneAndChromaRemap` a context whose scalar ceiling is the per-hue value for the pixel's
own hue — the shipped `ScaleChroma` runs unmodified — and measured it.

| | delivered C\*, scalar | delivered C\*, per-hue | mean hue drift, scalar | per-hue |
|---|---|---|---|---|
| Hue circle, gain **1.3** | 44.0 (×1.13) | 43.4 (×1.12) | 7.3° | 7.1° |
| Hue circle, gain **1.8** | 52.1 (×1.34) | 49.9 (×1.28) | 9.1° | 8.2° |
| Hue circle, gain **2.2** | 55.6 (×1.43) | 51.8 (×1.33) | 10.2° | 8.4° |
| Hue circle, gain **3.0** | 57.4 (×1.48) | 46.9 (×1.21) | 10.9° | 6.2° |
| **Six photographs, gain 1.3** | ×1.209 | **×1.198** | 17.3° | **17.2°** |
| Six photographs, gain 1.8 | ×1.558 | ×1.521 | 14.6° | 14.3° |

`[verified — computed locally]`

**The effect is proportional to the knee weight, which is `(gain − 1) / (3 − 1)`.** At
Post-Impressionism's 1.3 that is 0.15: 85% of the transform is a plain linear multiplier the
ceiling never touches. At Fauvism's current 1.8 it is 0.40 and the change is still under
4 C\* units and 0.4° of drift. The item only pays at the top of the slider, which no style
uses.

**This contradicts both prior rounds.** The Abstract round priced it at ~15 lines and called
it "the cheapest item on the list, fixes a live defect in three styles". The Fauvism round
called it a correctness fix with "high confidence". Neither measured what wiring it does to a
rendered image; both reasoned from the *ask* side of the transform, where the ceiling is
obviously wrong, without checking the *deliver* side, where the nearest-candidate search has
already absorbed most of the error. `[inferred, from verified measurements]`

**And with a green-bearing palette it can make hue drift worse.** Substituting Phthalo Green
(Y.S.) for Quinacridone Magenta closes the green shortfall — 120–150° delivers ×1.25 instead
of ×1.10 — and opens a magenta hole where the per-hue ceiling is 12.0. In that sector the
per-hue variant drifts **33.1°** against the scalar's 33.5° at gain 1.3, and **34.1° against
33.5°** at gain 1.8. Asking for less chroma in an empty sector moves the target toward the
neutral axis, where the nearest candidate's hue is less determined. `[verified]`

### 4.3 Hue drift belongs to the palette

The same landscape palette at **gain 1.0 — an exact identity for the chroma path** — already
drifts 30.2° at 330–360° and 19.4° at 300–330°, with an overall mean of 10.3°. On the six real
photographs the shipped Post-Impressionism drifts 17.3° and a floor-only Realism-like
configuration drifts 17.2°. `[verified]`

**Raising chroma reduces drift**: 20.0° at gain 1.0, 17.3° at 1.3, 15.8° at 1.5, 14.6° at 1.8.
More chromatic targets land on more chromatic candidates, whose hue angle is better
determined. `[verified]` That is a small positive argument for a chroma gain that has nothing
to do with style.

The honest reading: **the app's hue error is set by which paints the user picked, and no
remap parameter is a meaningful lever on it.** The useful product here is not a stage — it is
a palette report telling the user "your selection cannot reach magenta", which
`SampleAchievableColors` already has the data for and report 01 §6.2 already proposed.
`[inferred]`

---

## 5. Contrast and value

**Contrast 1.1 is wrong by sign, and the correction is larger than Fauvism's was.**

Target: Post-Impressionist L\*sd ÷ photographic L\*sd = 17.02 / 23.36 = **0.729**; p5–p95
range ratio 0.762. Measured on the six photographs through `StylePipeline.Render`:

| Variant (floor / contrast / chroma) | rendered L\*sd ÷ source | rendered C\* ÷ source | colours | regions | % below mark² |
|---|---|---|---|---|---|
| **shipped 3 / 1.1 / 1.3** | **0.921** | 1.209 | 751 | 83,428 | 38.30 |
| 3 / 1.0 / 1.3 | 0.862 | 1.221 | 712 | 82,697 | 38.13 |
| 3 / 0.95 / 1.3 | 0.831 | 1.228 | 695 | 82,600 | 38.16 |
| 3 / 0.90 / 1.3 | 0.799 | 1.235 | 681 | 82,068 | 38.16 |
| 4 / 0.85 / 1.3 | **0.754** | 1.242 | 630 | 69,442 | 33.70 |
| 4 / 0.85 / 1.45 | 0.744 | 1.362 | 669 | 72,165 | 34.79 |
| 4 / 0.85 / 1.45 + merge | 0.749 | 1.360 | 554 | 38,239 | **21.23** |

`[verified — computed locally]`

### The floor and the contrast knob are not substitutes

The Fauvism round's framing — "value compression should come from the floor, which removes
modelling while keeping range, not from the contrast knob, which squashes the histogram" — is
half right and needs correcting. Holding contrast at 1.0 and sweeping the floor:

| Floor strength | L\*sd ratio | % below mark² |
|---|---|---|
| 1 | 0.910 | 51.57 |
| 2 | 0.883 | 43.61 |
| 3 | 0.862 | 38.13 |
| 4 | 0.847 | 33.64 |
| 5 | 0.836 | 29.95 |

`[verified — computed locally]`

**The floor moves L\*sd by 8% across its entire range and fragmentation by 22 points. The
contrast knob moves L\*sd by 17% across a quarter of its range and fragmentation by 0.2
points.** They are orthogonal instruments: the floor controls how paintable the output is,
the contrast knob controls the value range. The Fauvism round's target of ×0.868 happened to
be reachable by the floor alone, which is why the framing looked right there. A target of
×0.729 is not reachable by the floor at any strength. `[verified]`

The framing that survives, and belongs in the doc comment: **the floor is the fragmentation
control and the contrast knob is the value-range control; do not use either to do the other's
job.** `[inferred]`

### How far to go

I would move contrast to **0.9–1.0**, not to 0.85, despite 0.85 hitting the measured target.
The reproduction confound in §2 bites hardest on exactly this statistic — a scanned canvas and
a digital photograph differ systematically in tone curve — and the Fauvism round reached the
same caution independently for its own ×0.868. Setting 1.0 removes the sign error and costs
nothing; setting 0.85 buys accuracy against a number I do not trust to two figures.
`[inferred]`

**Post-Impressionism must not become Tonalism.** Tonalism runs contrast 0.55 and realises an
L\*sd ratio of **0.420** with a C\* ratio of **0.458** `[verified]`. At contrast 0.9–1.0 with
chroma 1.45 the two styles are separated by more than 2× on both axes.

---

## 6. Optical mixing and broken colour — the queued measurement

The parent README has carried this as build-order item 1 since 2026-07-27: *"Measure the
dithering gain (~30 lines, no feature). Juxtaposed marks average as radiance — a straight line
in linear light — while Kubelka-Munk mixing follows a darker, duller curve… Measure it against
`SampleAchievableColors` before building anything on it."* Neither the Abstract nor the
Fauvism round made it. Seurat and Signac are in my umbrella, so here it is.

### 6.1 Masstone pairs: physical mixture versus juxtaposition

Each pair mixed 50/50 through the shipped `KubelkaMunk.Mix` and rendered by
`SpectralRenderer.ToDisplayColor`, against the same two masstones juxtaposed 1:1 and averaged
in linear sRGB light. "outside" is the ΔE from the juxtaposed colour to the nearest of the
3,007 achievable candidates.

| pair (six-paint fixture) | K-M L\* | K-M C\* | K-M h | juxt. L\* | juxt. C\* | juxt. h | ΔL\* | ΔC\* | outside |
|---|---|---|---|---|---|---|---|---|---|
| White + Hansa Yellow | 90.7 | 60.1 | 94° | 92.3 | 26.0 | 90° | +1.5 | **−34.1** | 6.8 |
| White + Cad Red Light | 63.3 | 56.6 | 29° | 80.0 | 20.2 | 26° | +16.7 | **−36.4** | 4.7 |
| White + Quin Magenta | 53.9 | 56.3 | 343° | 76.1 | 6.1 | 10° | +22.2 | **−50.2** | 5.0 |
| White + Ultramarine | 57.3 | 50.5 | 284° | 75.0 | 3.8 | 291° | +17.7 | **−46.7** | 1.8 |
| White + Bone Black | 57.6 | 3.9 | 261° | 75.0 | 0.7 | 144° | +17.4 | −3.2 | 3.1 |
| Yellow + Cad Red Light | 53.8 | 83.0 | 46° | 71.4 | 78.5 | 74° | +17.7 | −4.5 | 1.4 |
| Yellow + Quin Magenta | 37.3 | 52.7 | 35° | 66.5 | 64.7 | 83° | +29.2 | +11.9 | 13.4 |
| **Yellow + Ultramarine** | **32.7** | **39.1** | **150°** | **65.1** | **51.9** | **87°** | **+32.4** | **+12.8** | 11.6 |
| Yellow + Bone Black | 35.4 | 33.5 | 128° | 65.1 | 65.7 | 90° | +29.7 | +32.2 | 11.5 |
| Cad Red + Quin Magenta | 37.8 | 68.9 | 33° | 40.3 | 70.4 | 33° | +2.5 | +1.5 | 1.8 |
| Cad Red + Ultramarine | 23.6 | 21.4 | 41° | 36.9 | 57.8 | 18° | +13.4 | **+36.4** | 1.5 |
| Cad Red + Bone Black | 28.0 | 32.3 | 46° | 37.2 | 65.4 | 39° | +9.2 | +33.1 | 3.8 |
| Quin Magenta + Ultramarine | 12.6 | 20.8 | 309° | 19.1 | 45.6 | 340° | +6.5 | +24.8 | 9.5 |
| Quin Magenta + Bone Black | 15.0 | 11.2 | 353° | 20.0 | 34.7 | 10° | +5.0 | +23.5 | 2.7 |
| Ultramarine + Bone Black | 8.5 | 10.6 | 285° | 9.7 | 34.3 | 304° | +1.2 | +23.7 | 0.9 |
| **mean** | | | | | | | **+14.8** | **+1.7** | 5.3 |

`[verified — computed locally]`

Three corrections to the parent claim.

- **The lightness result is unambiguous and large: juxtaposition is lighter than mixture in
  every one of 15 pairs, mean +14.8 L\*.** That is the "darker curve" claim, confirmed and
  quantified.
- **The chroma result is not a gain. Mean ΔC\* is +1.7, essentially zero, and the sign
  depends entirely on whether white is one of the marks.** With white, juxtaposition *loses*
  34–50 C\*; between two chromatic paints it gains 12–36. The mechanism is the one the
  Fauvism round found for the chroma ceiling: two-constant K-M tinting a dark transparent
  pigment with white *reveals* its hue, while averaging its near-black masstone with white in
  linear light merely greys it. **"Broken colour preserves chroma that mixing destroys" is
  true only for chromatic-plus-chromatic pairs.** `[verified]`
- **"Blue + yellow dithered reads grey, mixed reads green" is wrong in this library, and
  interestingly so.** Hansa Yellow + Ultramarine mixes to a green (h 150°, C\* 39.1, L\* 32.7)
  and juxtaposes to a **light yellow** (h 87°, C\* 51.9, L\* 65.1) — not a grey. Ultramarine's
  masstone is L\* 7.8; the radiance average of a light paint and a near-black one is a
  darkened version of the light paint. Partitive averaging between paints of very unequal
  lightness is a *value* operation on the lighter hue, not a hue operation. `[verified]`

### 6.2 Coverage: how much gamut does juxtaposition actually add?

4,631 distinct colours from six landscape photographs. "Single" searches all 3,007 candidates;
"juxtaposed" searches 282,376 1:1 pairs drawn from a **thinned** 752-candidate subset, so the
comparison is biased *against* juxtaposition.

| | nearest single mixture | best 1:1 juxtaposition |
|---|---|---|
| mean ΔE | **6.06** | **2.15** |
| p90 ΔE | 11.77 | 5.91 |
| p99 ΔE | 14.90 | 11.72 |
| targets improved | — | **93.5%** |
| improved by > 2 ΔE | — | **62.4%** |
| mean change in \|ΔL\*\| error | — | **−1.59** |
| mean change in delivered C\* | — | −0.39 |

`[verified — computed locally]`

**A 2.8× reduction in mean colour error, and the direction is lightness.** The parent
README's prediction that the gain lies "in the lightness/mid-chroma direction where the paint
gamut is most constrained" is confirmed; its expectation of a chroma extension is not — mean
delivered chroma is unchanged.

Two controls worth stating. The residual 6.06 is a genuine gamut boundary, not a sampling
artefact: `MixtureBuilder`'s own doc comment records mean sampling error of **0.91** at 63
samples per pair and 16 simplex divisions, with 255 samples reaching only 0.83
`[verified — read the source]`. And nothing here breaks the converter's invariant: every mark
is still a real mixture of real paints. Only the *perceived* average is a colour no single
mixture makes, and the painter never has to mix it.

### 6.3 What that licenses, and what it does not

It licenses building broken colour. It does **not** licence putting it in Post-Impressionism.

- The fusion arithmetic in report 01 §8.2 stands: a 4 mm Seurat-scale dot fuses at 13.7 m,
  so at any plausible viewing distance the viewer gets partial fusion plus visible texture
  `[verified via ../01]`. The 2.15 ΔE figure is the fully-fused limit and is an upper bound
  on what a viewer sees.
- The corpus says Seurat and Signac are colorimetrically Impressionist (§1). Divided colour
  is their *spatial* signature, and the parent README already scopes it as the shared feature
  of Impressionism, Pointillism and Divisionism.
- Post-Impressionism's mark scale is 1.6, the second coarsest in the registry. Dividing at
  that scale would produce the most visible texture of any style row.

**Build it as its own row when Impressionism is built. Measure it first at mark scale on a
rendered image, because §6.2 measures colour reach, not appearance.** `[inferred]`

---

## 7. What the painters themselves said, and what it licenses

Van Gogh is the one painter in this umbrella who wrote down a colour programme, and it is a
complementary-contrast programme. On *The Night Café*, to Theo, Arles, 8 September 1888:
"I've tried to express the terrible human passions with the red and the green", describing a
room of "blood-red and dull yellow, a green billiard table in the centre, 4 lemon yellow lamps
with an orange and green glow". `[relayed — letter 676; vangoghletters.org returned 403 and
the Van Gogh Museum highlights page rendered as an empty SPA shell, so this is from search
snippets of both. Flagged in §11.]`

**This does not licence a complementary stage, for three reasons already established.**

1. Schloss & Palmer's 1,431 pairs contradict complementary harmony by name, including
   red–green, F(1,47) = 17.67, p < .001 `[verified via ../01 §5.2]`. Van Gogh was not after
   harmony — he says so — but a stage that manufactures complements has no evidence behind it
   either way.
2. Assigning a complement requires knowing what a region *is*. The Fauvism round rejected
   per-region non-descriptive hue substitution on exactly this ground, and the memory-colour
   objection applies with more force here because a photograph's skin, sky and foliage are
   what a landscape converter mostly contains. `[relayed via ../fauvism/03-colour.md §5.2]`
3. Van Gogh's red/green is a *composition* decision — this room, these lamps — not a
   transform. The corpus number that survives from it is the one in §2: he painted at
   C\*mean 30.3, twice the Impressionist figure.

Gauguin's advice as relayed by Sérusier — "How do you see this tree? Green? Then use green,
the most beautiful green on your palette" — is the same shape of statement, and its
implementable content is **restriction toward tube colour**, not hue substitution. `[relayed —
widely reprinted from Sérusier's account of the 1888 Pont-Aven lesson; I did not read
Sérusier's *ABC de la peinture*]` That is the Fauvism round's masstone-biased candidate
transform, and it is a slot-3 item shared with Fauvism and Abstract rather than a
Post-Impressionism-specific one.

---

## 8. Three picks

### Pick 1 — Retune the registry row: contrast 1.1 → 1.0, chroma 1.3 → 1.45, floor 3.0 → 4.0

- **Slots 1 and 2.** `StyleRegistry.cs:110-113` plus the doc comment above it. **~4 lines
  changed, ~8 lines of comment, one golden regenerated.**
- **Evidence.** Contrast: measured target L\*sd ratio 0.729, shipped delivers 0.921; contrast
  1.0 delivers 0.862 (§5). Chroma: measured target C\*mean 22.40, shipped delivers 20.79 at a
  realised ×1.209; interpolating the 1.3 and 1.5 rows puts 22.40 at a knob of 1.42, and 1.45
  delivers 23.4 (§4.1, §5). Floor: 3 → 4 moves pixels below mark² from 38.13% to 33.64% and
  costs 0.015 of L\*sd ratio and nothing in chroma (§5).
- **Why not contrast 0.85**, which hits the target exactly: §2 caveat 1. The L\*sd target is
  the statistic most exposed to the reproduction confound. Removing the sign error is
  defensible on the direction alone, which is unanimous across 43 paintings; the exact
  magnitude is not.
- **What this does to the doc comment.** The current one says "the flatness here is meant to
  come from the floor's strength, not from the remap" while setting contrast to 1.1, which
  expands the histogram. Replace with §5's measured framing: the floor is the fragmentation
  control, the contrast knob is the value-range control.
- **Verification.** Pin the three ratios on the golden as a test: rendered L\*sd ÷ source
  below 0.90, rendered C\*mean ÷ source between 1.30 and 1.45, and both bounded away from
  Tonalism's 0.42 / 0.46. Numeric properties, not "does not throw".
- **Confidence: high** on the contrast direction, **medium-high** on 1.45 (it rests on the
  corpus), **high** that it is cheap.

### Pick 2 — Give Post-Impressionism the `SmallRegionMerge` it is the only styled row without

- **Slot 5.** The stage exists and is already used by Fauvism and Abstract. **1 line** in
  `StyleRegistry` plus a `ParameterValues` entry that `DefaultValues` creates automatically.
- **Evidence.** On six photographs: 38.30% of pixels in regions below mark², against
  Fauvism's 3.65% and Abstract's 2.45%. Adding the stage takes it to **25.84%** and region
  count from 83,428 to 47,879, with **no chroma cost** (C\*mean 20.80 vs 20.79). Combined
  with pick 1 it reaches **21.23%** (§5). `[verified]`
- **Why it is in a colour report.** It is not a colour change and I would not claim it. It
  came out of the colour probes because every render had to be measured for fragmentation
  anyway, and it is by a wide margin the largest defect those renders showed. If the
  brushwork track picks it too, charge it once.
- **Confidence: high** on all three of needed, cheap and invariant-safe (`Refine` takes and
  returns indices).

### Pick 3 — Build divided colour as its own row, not inside this one; the gating measurement now passes

- **Slot 4**, a position-dependent `IQuantiser`: choose the best pair of candidates for the
  target colour from a thinned pair index, then select between them by an ordered threshold at
  mark scale. **~150 lines**, of which ~60 is the pair index. Costs the per-colour cache —
  `IsPositionDependent` is already the declared escape hatch and `ResolvePerPixel` already
  exists.
- **Evidence.** §6.2: mean ΔE 6.06 → 2.15 over a real photograph's colours, 93.5% improved,
  62.4% by more than 2 ΔE, with the gain in lightness. That clears the parent README's
  build-order item 1, which has been the gate on this since 2026-07-27.
- **But not here.** §6.3: Seurat and Signac measure as Impressionist; Post-Impressionism's
  mark scale is 1.6; and the 2.15 figure is the fully-fused limit, which no viewer gets.
- **Do the appearance measurement before the feature.** Render a dithered output at mark
  scale and measure `PaintabilityMetrics` and the perceived average against a downsampled
  reference. §6.2 measures colour reach only.
- **Confidence: high** that the gamut extension is real and worth having eventually;
  **high** that it does not belong in this style row; **low** that it looks good before
  somebody renders one.

---

## 9. What not to build

The parent, Abstract and Fauvism "what not to build" lists all still apply. These are
additional, and each was rejected after going looking for it.

- **The per-hue chroma ceiling, at least as a Post-Impressionism fix — and the dead accessor
  that implements it should be deleted or wired-and-tested, not left as it is.** §4.2:
  measured through the real stage, wiring it moves realised chroma from ×1.209 to ×1.198 and
  hue drift from 17.3° to 17.2°. At Fauvism's 1.8 it moves them by under 4 C\* and 0.4°. With
  a green-bearing palette it makes magenta drift slightly *worse*. Two prior rounds ranked
  this first or second; both reasoned from the ask side without measuring the delivered side.
  `[verified]` If it is built, build it for a style that runs the slider near 3.0 — and none
  does.
- **Splitting Post-Impressionism into per-artist rows.** §1: Cézanne (C\* 15.3) and
  Seurat/Signac (16.6) are inside the Impressionist distribution (14.7), and the app has no
  Impressionism row for them to differ from. Two of the four rows would render as Realism with
  a floor. `[verified]`
- **A complementary-contrast stage, on van Gogh's authority.** §7. His own account is a
  composition decision about one room; Schloss & Palmer contradict red–green by name; and
  assignment needs semantics the app does not have. `[verified via ../01; the letter itself
  is `[relayed]`]`
- **Any hue operation at all — rotation, snapping, or per-region substitution.** §4.3: the
  measured hue error is 17.2° with *no* remap, and it is set by palette gaps. A hue stage
  would be tuning a parameter that contributes nothing against a defect it cannot fix. The
  rotation studies already say originals are preferred `[verified via ../abstract/04 §4.2]`.
- **Mean saturation as the target statistic.** §3: the only preference study ever run on
  Post-Impressionist paintings — 40 van Gogh landscapes — has SD of saturation in the model
  (β = 0.404, p = .003) and mean saturation absent. `[verified]` Keep the gain, but the
  stronger instrument for the statistic with support is a candidate-set restriction, which is
  a shared slot-3 item.
- **Raising contrast as a "boldness" control.** §5: contrast moves L\*sd by 17% and
  fragmentation by 0.2 points. It is a value-range control and nothing else. Every painting
  group in the corpus has a *lower* L\*sd than photographs; there is no measured case for a
  contrast above 1.0 in any of the five styles.
- **A "Post-Impressionist palette" preset.** The pigment argument that carries Fauvism does
  not transfer: van Gogh, Gauguin, Cézanne and Seurat used different palettes across three
  decades, and van Gogh's chrome yellows have measurably degraded, so a colorimetry of his
  canvases measures a 138-year-old object. Naming tube colours the picker may not supply is
  the broken promise the Fauvism round already rejected.
- **Claiming anywhere in the UI that Post-Impressionism is measurably more saturated than
  Impressionism.** §2 and §3: I measured it and it is (×1.53), but on 36 web reproductions
  with unknown colour management, no significance test, and a deliberately van Gogh-weighted
  sample. The claim is *supported by this report* and is not established.
- **Pointillist dithering inside this style row.** §6.3.

---

## 10. Method

Everything marked "computed locally" was produced on 2026-07-30 from a throwaway console
project in the session scratchpad, referencing `PaintTranslator.csproj` and named
`PaintTranslator.Tests` so the app's `InternalsVisibleTo` applies. **No file in the repository
was modified**; the probe lives outside the tree.

- **Stages are called, never transcribed.** `ToneAndChromaRemap.Map`, `NearestQuantiser.Map`,
  `EdgePreservingFloor`, `SmallRegionMerge`, `MixtureBuilder.Build`, `KubelkaMunk.Mix`,
  `SpectralRenderer.ToDisplayColor`, `PalettePhotoConverter.RgbToLab`,
  `PaintabilityMetrics.CountRegions` and `FractionInRegionsSmallerThan`, and
  `StylePipeline.Render` are the shipped implementations. `StylePipeline.MaximumChroma` and
  `MaximumChromaByHue` are private and were reached by reflection rather than copied.
- **The per-hue variant** is a wrapper `ILabRemap` that constructs a `RenderContext` whose
  scalar `AchievableMaxChroma` is `context.AchievableMaxChromaForHue(a, b)` and delegates to a
  real `ToneAndChromaRemap`. The shipped `ScaleChroma` runs unmodified; only the ceiling it is
  handed changes. That is exactly the one-line change under test.
- **Styles are constructed directly** in the probe rather than mutated in `StyleRegistry`,
  except for the five-style comparison in conclusion 7, which reads `StyleRegistry.All` as
  shipped.
- **Palettes.** "six-paint" is `Tests/StyleTestFixtures.SixPaints()` — White, Hansa Yellow
  Opaque, Cad Red Light, Quinacridone Magenta, Ultramarine, Bone Black; 3,007 candidates.
  "landscape" substitutes Phthalo Green (Y.S.) for the magenta; 2,952 candidates.
- **Sources.** A 360×360 full-hue-circle synthetic (HSV S = 0.55, V 0.25→0.85), because the
  committed golden has no pixels between 150° and 270°; and six of the corpus photographs at
  768 px, converted at `RenderContext.DefaultMarkPixels`.
- **Corpus.** 57 images at ~700 px from Wikimedia Commons. Provenance: Commons `extmetadata`
  Artist or filename for paintings; camera EXIF Make + Model + `DateTimeOriginal` ≥ 1990 for
  photographs. Five works removed after visual inspection (three framed museum photographs,
  one white-margin watercolour, and a scanned 1899 map that passed the EXIF test on a scanning
  back).
- **Nothing in this report is drawn from `Tests/Golden` alone.** The Fauvism round's
  correction 2 established that conclusions from the synthetic gradient are unsafe; every
  render figure here is a mean over six photographs.

---

## 11. Verification debt

Ranked by how much clearing each would change a decision.

1. **The corpus.** 57 self-curated web reproductions, no colour management, n = 23/13/7/14,
   no significance tests, subject matter confounded with group, and deliberately van
   Gogh-weighted. **Both retune numbers in pick 1 rest on it.** The cheapest fix is a larger
   provenance-controlled set from museum colour-managed downloads. This is the top debt in
   this report and it is the same top debt the Fauvism round recorded — nobody has fixed it,
   and I have only improved the checking, not the sourcing.
2. **Whether divided colour looks like anything at mark scale.** §6.2 measures colour reach on
   a fully-fused assumption. Nobody has rendered a dithered output in this app. Pick 3 is
   gated on it and I did not do it.
3. **Werner 2026, *Color Research & Application* 51(3).** Wiley 403; the ORA record says a
   CC BY-NC-ND PDF exists but the file listing 404'd. It is the only recent source that
   addresses Impressionist and Post-Impressionist colour through vision science, and its
   chromatic/achromatic spatial-frequency separation of Seurat's *Le Chahut* bears directly on
   how this pipeline should split slot 1 from slot 2. Currently `[relayed]` from an abstract.
4. **Van Gogh letter 676.** vangoghletters.org returned 403; the Van Gogh Museum highlights
   page rendered as an empty SPA shell. The red/green quotation in §7 is from search snippets
   of both. It is load-bearing for a *rejection*, not for a build item, so the risk is low.
5. **The L\*sd target's dependence on tone curve.** ×0.729 is the single number pick 1's
   contrast change is calibrated against, and painting-scan versus digital-photograph tone
   curves are exactly the systematic error it is exposed to. Measuring a handful of paintings
   from a colour-managed museum download against the same photographs would settle it.
6. **Desikan et al. 2022, *Entropy* 24(9) 1175 (WikiArtVectors).** Inherited from the Fauvism
   round's debt list, still unfetched (MDPI 403 there; I did not retry). Still the one located
   source that could overturn the negative result on measured colour signatures per movement.
7. **Sérusier's account of Gauguin's "the most beautiful green"** — §7, `[relayed]` from
   general reprinting. Load-bearing only for a framing sentence.
8. **The 4,631-target coverage figure uses a thinned 752-candidate pair set.** Thinning biases
   against juxtaposition, so 2.15 is an upper bound on the achievable error — the conclusion's
   direction is safe, the magnitude could improve. I did not check how much.

### What was verified locally this session

- Ask-versus-deliver by hue at gains 1.0, 1.3, 1.8, 2.2 and 3.0, scalar ceiling versus per-hue
  ceiling, for two palettes, through the real `ToneAndChromaRemap` (§4.1, §4.2).
- The dead-code status of `RenderContext.AchievableMaxChromaForHue` (§4.2).
- Fifteen masstone pairs, Kubelka-Munk 50/50 against linear-light 1:1 juxtaposition, for two
  palettes, with distance to the nearest achievable candidate (§6.1).
- Coverage of 4,631 photographic colours by single candidates versus 282,376 juxtapositions
  (§6.2).
- Fifteen Post-Impressionism variants and all five registered styles rendered over six
  photographs through `StylePipeline.Render`, with L\*sd, C\*mean, C\*sd, distinct colours,
  region count, fraction below mark² and mean hue drift (§4.2, §5, conclusion 7).
- The 57-work corpus table and its per-group dispersion (§2), plus the five curation
  rejections found by inspecting the images.
