# The Palette of Realism

**Date:** 2026-07-31
**Track:** 1 of 4 on Realism — "is *doing nothing* the right colour behaviour for a realist
conversion, or does it only look like the safe choice?"
**Shipped state under examination:** `Imaging/Styles/StyleRegistry.cs:33-40` — mark scale **1.0**;
pre-map `EdgePreservingFloor` at the stage's own declared defaults (**strength 1.0, edge 0.05** —
there is no `WithDefaults` call at all); `IdentityRemap`; `KeepAllCandidates`;
`NearestQuantiser`; **empty post-map slot**. It is `StyleRegistry.Default`.

**Relationship to prior research.** Extends [../01-colour-theory-in-practice.md](../01-colour-theory-in-practice.md)
and [../02-styles-and-movements.md](../02-styles-and-movements.md), and is the first per-style
round to examine the row every other round has used as its *control*. It **confirms** the
Tonalism round's finding that mean chroma does not separate a nineteenth-century movement from
photographs (§3), **contradicts** that round's attribution of Tonalism's separation to value key
(§3.3), and **contradicts the recommendation all four prior rounds made for every style with an
empty slot 5** (§6).

**Claim marking:** `[verified]` = read the primary source directly, or computed it locally this
session; `[relayed]` = a source says so and I did not confirm it; `[inferred]` = my own reasoning
from marked inputs.

**Method note.** Every pipeline number below comes from *calling* the shipped code —
`StyleRegistry.ByName("Realism")` itself for the shipped row, plus `StylePipeline.Render`,
`StylePipeline.DefaultValues`, `EdgePreservingFloor.Apply`, `ToneAndChromaRemap`,
`NearestQuantiser.NearestIndex`, `MixtureBuilder.Build`, `PalettePhotoConverter.RgbToLab`,
`ImageDecoder.DecodeFile` and `PaintabilityMetrics`. Nothing is transcribed. **No conclusion is
drawn from `Tests/Golden` or from any synthetic image.** Full method in §9.

---

## Conclusions, first

**1. The statistic that separates realist canvases from photographs is the dark end, and it runs
the opposite way to intuition.** Over a provenance-checked 53-work realist corpus against 23
EXIF-verified photographs, **L\*p1 is 7.10 on canvases and 2.785 on photographs**
(Welch t = **+3.93**, df 70) — canvases *do not reach* the blacks a photograph reaches. The
second separator is **chroma variance, not chroma mean**: C\*ab sd **7.90 vs 11.66**
(t = **−3.94**, df 29) while C\*ab *mean* is 13.75 vs 16.57 and does not survive a Welch *t*
(t = −1.80). These two are the only statistics of eighteen that survive a Bonferroni correction
across the comparison. `[verified — computed locally 2026-07-31]`

**2. The shipped Realism row is inside that envelope on three statistics of seven, and it is the
first row any of these five rounds has measured that is inside its envelope on anything.**
Rendered through the real pipeline over 12 photographs: L\*sd ratio **0.927** against a corpus
target of 0.903; local |ΔL\*| ratio **0.806** against 0.819; notan ratio **0.935** against 0.894.
It misses on chroma variance (delivers 1.041 against a target of 0.677), overshoots the dark-end
lift (5.32× against 2.55×), and misses the value-key target by 6.0 L\* — on the weakest-supported
of the seven. **This is a genuine negative result: for colour, doing nothing is close to right.**
`[verified]`

**3. And the honest version of conclusion 2 is weaker still, which is itself the finding.** The
realist corpus is so internally variable — L\*mean SD **13.01** across 53 works, wider than the
photographs' 12.4 — that an absolute envelope test cannot discriminate. Root-mean-square z-distance
to the corpus across eight statistics: **converted 0.83, unconverted photograph 0.84.** Passing
through the whole converter moves a photograph 0.01 of a standard deviation closer to a realist
canvas. **Realism has no tight colour envelope to hit**, which is the strongest argument available
that `IdentityRemap` is correct. `[verified]`

**4. The mandatory floor lands the dark end exactly on the canvas figure, and then the
nearest-candidate match overshoots it.** Measured by calling `EdgePreservingFloor.Apply` on its
own: source L\*p1 **3.40** → after the floor alone **7.50** → after the full render **12.58**,
against a corpus target of **7.10**. The floor is doing, for free and by accident, the one thing
the corpus asks for; the quantiser then doubles it. Swapping the six-paint fixture for all 19
selectable paints (28× the candidates, 17× the candidates below L\* 10) leaves the rendered dark
end unmoved (13.03 → 13.25 on a common subset), so **the overshoot is not a dark-candidate-count
problem and more paint will not fix it.** `[verified]`

**5. `SmallRegionMerge` — the item all four prior rounds picked first or second for every style
with an empty slot 5 — must not be registered on Realism.** It reaches exactly **0.000000**
sub-mark share as advertised, and it costs **+4.26 ΔE of fidelity** (6.45 → 10.72 mean ΔE from the
source) and **destroys human faces**. Rendered and looked at at 1:1: the eyes, mouth and headdress
of a portrait dissolve into a mottled patchwork of flat blobs. No floor setting rescues it —
at strength 3 / edge 0.15 the merge is cheaper (+0.54 ΔE) only because the floor has already
blurred the face away. **There is no setting of slots 1 or 5 that makes this row paintable and
keeps a face.** `[verified — measured and looked at; §6]`

**6. Realism's published paintability figure is 14× too small, and unlike the other rows this
one should probably not be fixed.** `StyleBehaviourTests.EveryRegisteredStyleIsPaintable` records
a 3.0% ceiling for Realism on a 256² synthetic gradient (`StyleBehaviourTests.cs:472`). On
photographs the shipped row leaves **41.5–42.7%** at 768 px and **45.5%** at 1600 px — the figure
gets *worse* with resolution, so it is not a small-image artefact. `[verified]` This is the fifth
consecutive round to find a `Tests/Golden`-derived spatial figure false on photographs.

**7. Quantisation costs pixel-weighted ΔE 5.58 against realist target colours on the six-paint
fixture and 2.48 on all 19 — and the realist target region is the *best*-served region of the
achievable gamut, not the worst.** 53.5% of realist pixel mass sits below L\* 40, against 52.8%
of the six-paint candidate set; the alignment is near-exact and no transform produced it. Worst
served are L\* 80–100 (weighted ΔE 8.20–8.90, 7.7% of realist pixels) and L\* 0–10 (6.88, 9.6%).
**This is the mirror image of the Tonalism round's result**, where the style's own remap pushed
87% of targets into L\* 40–80, the sparsest band. Realism's identity remap is why. `[verified]`

**8. There is no realism-specific paint-selection preset worth shipping, and the measurement that
looked like one is an artefact of pixel weighting.** A warm four-paint Zorn-like set (Titanium
White, Diarylide Yellow, C.P. Cadmium Red Light, Bone Black) has the best *style-specific* signal
in the study — realist ÷ photographic weighted ΔE **0.686**, and realist ÷ Impressionist **0.787**,
against 0.80–1.05 for every other palette tested. Rendered and looked at, **it turns every sky in the
set to grey**. The corpus has almost no blue in it (warm-pixel share 0.870, b\*mean +10.84); a
user's photographs do. **The defensible recommendation is "more paints is better", which is not a
style recommendation.** `[verified — measured and looked at; §7]`

**9. `ToneAndChromaRemap` cannot deliver what the corpus asks for even in principle.** The corpus
asks for chroma *variance* compressed to 0.677 while chroma *mean* stays at 0.830. The stage's
chroma parameter is a plain multiplier below gain 1.0, so it scales mean and spread together:
gain 0.85 gives mean 0.862 (on target) and sd 0.941 (nowhere near); gain 0.75 gives 0.747 and
0.823. **No setting hits both.** The operation the corpus describes is
`C* → C̄* + (C* − C̄*)·k`, which no stage in the pipeline implements. `[verified]`

**10. Every remap setting tested makes the picture worse.** Across sixteen single-parameter remap variants,
mean ΔE from the source runs 10.65–13.99 against the shipped row's **6.45**, and the two settings
that best hit corpus ratios (contrast 0.92, key −5) produce a visibly grey, dulled sky on outdoor
subjects. **Identity is correct.** `[verified — measured and looked at]`

**Four picks in §8, three of them "do not do the thing the previous four rounds recommended".
Eleven rejections in §10.**

---

## Contents

1. [What "Realism" names, and where its edges are](#1-what-realism-names-and-where-its-edges-are)
2. [The corpus](#2-the-corpus)
3. [What Realism measurably is](#3-what-realism-measurably-is)
4. [Does the shipped row land inside that envelope?](#4-does-the-shipped-row-land-inside-that-envelope)
5. [What the nearest-candidate match costs](#5-what-the-nearest-candidate-match-costs)
6. [The paintability trap](#6-the-paintability-trap)
7. [The limited-palette question](#7-the-limited-palette-question)
8. [Picks](#8-picks), and §8.5 — what they look like
9. [Method](#9-method)
10. [What not to build](#10-what-not-to-build)
11. [Corrections to prior research](#11-corrections-to-prior-research)
12. [Accuracy warnings](#12-accuracy-warnings)
13. [Verification debt](#13-verification-debt)
14. [Corpus provenance](#14-corpus-provenance)

---

## 1. What "Realism" names, and where its edges are

**Ruling: one row, and its centre is the Courbet–Millet–Homer–Eakins–Bastien-Lepage figure
painting, not landscape.** The academic naturalists (Bouguereau, Gérôme) measure inside the same
distribution and can stay. The Pre-Raphaelite social realists do not and are reported separately.

Realism is the mid-nineteenth-century programme of painting contemporary life without idealisation,
announced by Courbet around 1848–55 and continued as *naturalism* by Bastien-Lepage, Breton and
Lhermitte, as American realism by Homer and Eakins, and as the Peredvizhniki programme by Repin
and Perov. `[relayed — general survey literature; I read no monograph]` For an app whose default
row carries this name, the important fact is that the movement is defined by **subject and
attitude**, not by a technique — which is why one should expect it to be hard to separate on
colour statistics, and it is.

The internal spread confirms it:

| sub-group | n | L\*mean | L\*sd | C\*mean | hue conc. | b\*mean | local ΔL\* |
|---|---|---|---|---|---|---|---|
| Eakins | 4 | **28.96** | 16.58 | **10.12** | 0.74 | +6.46 | 6.51 |
| urban French (Daumier, Degas, Caillebotte) | 4 | 35.78 | 19.49 | 11.87 | 0.87 | +9.96 | 6.92 |
| Millet | 4 | 36.28 | 22.42 | 12.85 | 0.92 | +11.24 | 5.42 |
| Courbet | 6 | 39.20 | 22.64 | 15.22 | 0.87 | +13.36 | 8.98 |
| naturalism (Bastien-Lepage, Breton, Lhermitte, Fildes, Herkomer) | 8 | 39.24 | 19.08 | 14.27 | 0.91 | +12.47 | 8.01 |
| Homer | 9 | 40.05 | 19.29 | 13.26 | 0.83 | +9.71 | 6.20 |
| academic (Bouguereau, Gérôme) | 6 | 42.07 | 20.57 | 15.19 | 0.77 | +10.63 | 6.93 |
| German/Nordic (Menzel, Leibl, Krøyer) | 4 | 45.88 | 18.54 | 14.16 | 0.74 | +7.32 | 6.22 |
| Russian (Repin, Perov) | 6 | **50.01** | 20.60 | 15.83 | 0.93 | +14.29 | 7.63 |
| *(Pre-Raphaelite control)* | 3 | *37.91* | *25.46* | *19.60* | *0.81* | *+14.10* | *11.66* |

`[verified — computed locally]`

**The movement spans 21 L\* of mean lightness between its own sub-groups.** That is nearly the
entire gap between any two of the movements this directory has measured. The academic naturalists
sit in the middle of it and there is no statistic on which they separate from the rest, so the
"academic vs realist" boundary — which is an art-historical and political distinction — has no
colour signature here and should not be given a row.

**The Pre-Raphaelites are the one group that measurably does not belong.** Three works (Brown's
*Work* and *The Last of England*, Hunt's *The Awakening Conscience*) give C\*mean 19.60 against
the realist 13.75 and local ΔE 16.84 against 9.15 — every statistic pushed outward. That is the
known PRB programme of bright pigments on a wet white ground, and it is nearer this app's Fauvism
row than its Realism row. `[verified]`

---

## 2. The corpus

**53 realist paintings, 23 modern photographs, 9 Impressionist paintings, 6 Tonalist paintings
and 3 Pre-Raphaelite paintings**, all from Wikimedia Commons, resolved by exact `File:` title
through the API and downloaded as 800/960 px thumbnails with `commonmetadata` and `extmetadata`
captured alongside. Full provenance in §14.

**Photographic subject matter was matched to the movement deliberately**, which no prior round in
this directory has done: the control is 14 photographs of people at work or portraits (ploughing,
foundry casting, cotton milling, crate making, fishing, market trade, cooking over fire) plus 9
rural landscapes and marines. The Tonalism round's photographic control was landscape-only, which
was right for that movement and would have been wrong here.

Curation, in the order it caught things:

- **Automated.** Paintings had to resolve through the English Wikipedia article's own lead image
  or through a Commons title carrying the artist's name; photographs had to carry EXIF **Make,
  Model and `DateTimeOriginal`**. 22 of 23 kept photographs pass all three; *A Tibetan Pilgrim
  Lighting Ghee Lamps* carries none and is kept and flagged. `[verified]`
- **Visual, on seven contact sheets — and this is where the real errors were.** Five of the seven
  rejections were found by looking:
  - Courbet's *Les demoiselles des bords de la Seine* is photographed **inside its gold frame**,
    which fills roughly a third of the image.
  - Monet's *Reflections of Clouds on the Water-Lily Pond* is a **gallery installation
    photograph** — museum wall and wooden floor occupy most of the frame.
  - Three photographs are **black-and-white**: *Bearded man smoking pipe*, *Fishmonger smiling,
    Maracaibo* and *Cycling Amsterdam 03*.
- **Numeric confirmation after the visual pass, which caught one the visual pass had only
  suspected.** *Cycling Amsterdam 03* measures C\*mean **0.000** — pure greyscale — and I had
  filed it as "heavily desaturated, probably keep" on the contact sheet. **Re-checking a
  visual suspicion numerically is a step no prior round records, and here it removed an image
  that would have pulled the photographic chroma control downward.** `[verified]`
- **Aspect-ratio exclusion.** Two photographs (*Aquaculture in Chile* 3.74:1, *Ahuriri River*
  3.80:1) are stitched panoramas and were excluded on the Tonalism round's stated grounds: a
  multi-frame composite's lightness statistics are not a single exposure's.
- **Automated white-mount trim** (rows and columns in which every pixel exceeds 242 in all three
  channels) was applied to every image. **No image in this corpus had one**, unlike the Tonalism
  round's two.

**Caveats, in order of how much they could move a conclusion.**

1. **Varnish and age.** These are 110–180-year-old varnished oils photographed under unknown
   conditions. The +10.84 b\*mean in §3 is partly age. The control that limits the damage is the
   same one the Tonalism round used and it works better here: the Impressionist group is the same
   medium, the same century and the same source population, and sits at b\*mean **+3.90** and
   L\*mean **52.84** — *lighter* than the photographs. A systematic scan darkening cannot produce
   both a realist 40.06 and an Impressionist 52.84. `[inferred, from verified numbers]`
2. **The dark-end result is the one most exposed to the confound in the other direction.** A scan
   with lifted shadows would raise L\*p1 exactly as measured. Against that: the Impressionist
   group's L\*p1 is 21.16 and the Tonalist group's 16.25, both *far* above the realist 7.10, so
   the realist figure is not simply "what a painting scan does". And 8 of 53 realist works reach
   L\*p1 below 3.5. But I cannot rule the confound out and it is verification debt 1.
3. **n is unbalanced and the *t* statistics carry no correction for multiple comparisons.**
   Eighteen statistics were tested against three controls. Only **L\*p1 (t = +3.93)** and
   **C\*sd (t = −3.94)** survive a Bonferroni correction over eighteen against the photographic
   control. Treat everything at 2.0 ≤ |t| < 3.2 as suggestive.
4. **Contemporary representational painting is absent.** I could not source it cleanly from
   Commons — living realist painters' work is in copyright and what is uploaded is unverifiable.
   The corpus is therefore 1848–1900 and the report should not be read as speaking for
   contemporary realism. Verification debt 5.

---

## 3. What Realism measurably is

Whole-image statistics at ~700 px longest edge, sRGB → CIELAB through the app's own
`PalettePhotoConverter.RgbToLab`. "hue concentration" is the chroma-weighted circular resultant
length of the pixel hues. "local ΔL\*" is the mean absolute difference to the pixel *r* away
horizontally and vertically, *r* = short side ÷ 60, following the Fauvism and Tonalism rounds.
"notan gap" is the mean L\* of pixels at or above the image's own median minus the mean L\* below
it — the light/dark mass separation the Tonalism round's track 4 introduced.

| group | n | L\*mean | L\*sd | **L\*p1** | L\*p5 | L\*p95 | C\*mean | **C\*sd** | %px C\*<20 | hue conc. | b\*mean | ΔL\* | ΔC\* | notan | warm |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Realist** | 53 | 40.06 | 20.12 | **7.10** | 11.20 | 73.63 | 13.75 | **7.90** | 78% | 0.85 | +10.84 | 7.12 | 3.35 | 32.38 | 87% |
| Impressionist | 9 | 52.84 | 15.82 | 21.16 | 27.22 | 76.45 | 12.16 | 8.12 | 84% | 0.48 | +3.90 | 7.72 | 4.32 | 25.92 | 62% |
| Tonalist | 6 | 39.13 | 13.58 | 16.25 | 19.75 | 61.19 | 17.89 | 8.46 | 67% | 0.92 | +13.99 | 4.95 | 3.12 | 22.81 | 82% |
| Pre-Raphaelite | 3 | 37.91 | 25.46 | 2.07 | 4.71 | 84.88 | 19.60 | 14.55 | 59% | 0.81 | +14.10 | 11.66 | 7.63 | 42.16 | 88% |
| **Photograph** | 23 | 45.23 | 22.29 | **2.79** | 9.96 | 79.64 | 16.57 | **11.66** | 67% | 0.73 | +7.60 | 8.69 | 3.99 | 36.24 | 71% |

`[verified — computed locally 2026-07-31]`

Welch two-sample *t*, realist against each control:

| statistic | vs photograph | vs Impressionist | vs Tonalist | reading |
|---|---|---|---|---|
| **L\*p1** | **t = +3.93, df 70** | t = −2.49, df 9 | t = −2.07, df 6 | Canvases do not reach photographic blacks |
| **C\*sd** | **t = −3.94, df 29** | t = −0.25, df 11 | t = −0.26, df 5 | Chroma *variance* is what is low |
| warm share | t = +2.55, df 32 | **t = +3.92, df 11** | t = +0.47 | |
| %px C\*<20 | t = +2.12, df 32 | t = −1.35 | t = +0.80 | |
| hue concentration | t = +2.09, df 31 | **t = +4.55, df 9** | t = −2.13, df 19 | |
| local ΔL\* | t = −2.05, df 31 | t = −0.53 | t = +2.10, df 6 | |
| L\*p95 | t = −1.90, df 47 | t = −0.49 | t = +1.92 | |
| **C\*mean** | **t = −1.80, df 29** | t = +1.08, df 11 | t = −1.04, df 5 | **Nothing** |
| L\*mean | t = −1.62, df 43 | t = −2.03, df 9 | **t = +0.18, df 6** | **Nothing, against Tonalism** |
| L\*sd | t = −1.65, df 37 | t = +1.90 | t = +2.35, df 6 | |
| b\*mean | t = +1.32, df 27 | **t = +5.45, df 20** | t = −0.67 | |
| notan gap | t = −1.40, df 34 | t = +1.51 | t = +1.86 | |

`[verified]`

### 3.1 The dark end is the finding, and it is not the one anybody would predict

A photograph reaches L\*p1 **2.79**; a realist canvas reaches **7.10**. Across 53 works the SD is
7.22 against the photographs' 2.22 — canvases vary in how dark they get, photographs almost all
bottom out at black. The mechanism is not mysterious: oil paint on a canvas under gallery light has
a reflectance floor that a sensor's shadow noise does not, and the darkest pigment available in
1855 was ivory black. **What matters for this app is that the achievable gamut has the same
floor.** The darkest candidate the six-paint fixture can mix is L\* **6.464**; over all 19
selectable paints it is **6.43**. The corpus figure is 7.10.

**The paint's own limitation reproduces the canvas statistic, for free, on every palette.**
`[verified]`

### 3.2 "Sombre palette" is a claim about chroma variance, and about warmth, not about chroma level

Realist C\*mean 13.75 against the photographs' 16.57 does not survive a *t* (−1.80), and against
Impressionism it runs the *other* way (13.75 vs 12.16). What does separate is **C\*sd 7.90 vs
11.66, t = −3.94** — the strongest chroma result in the study. A realist canvas is not less
colourful on average; it is more *uniform* in colourfulness. Nothing in this pipeline can produce
that (§4.3).

The other real difference is warmth, and it is the largest single *t* in the report — against
Impressionism, not against photographs: **b\*mean +10.84 vs +3.90, t = +5.45, df 20**, with
warm-pixel share 87% vs 62% and hue concentration 0.85 vs 0.48. That is the earth palette against
the banished-earth palette, and it is the one place where the historical materials record (§7) and
the measurement agree exactly. **It is also the number most exposed to varnish**, and I cannot
separate the two.

### 3.3 Realist and Tonalist canvases have the same value key — which corrects last round's reading

The Tonalism round concluded that "everything that makes Tonalism Tonalism is value, not chroma",
with value *key* (L\*mean) as the largest of its two significant statistics: Tonalist 39.37 against
photographs 50.01 (t = −3.15) and Impressionism 61.38 (t = −6.96).

Measured against a *realist* control, that separation vanishes: **Tonalist L\*mean 39.13, realist
40.06, t = +0.18** — the smallest *t* in this entire report. What separates the two nineteenth-
century oil groups is **value range** (L\*sd 13.58 vs 20.12, t = +2.35), **the light end**
(L\*p99 66.83 vs 83.58, t = +2.94) and **local lightness contrast** (4.95 vs 7.12, t = +2.10).
`[verified]`

The finding stands and the framing needs correcting: **low key separates Tonalism from
Impressionism and from photographs; it does not separate it from the other nineteenth-century
movement in the same room.** Anyone tuning Tonalism's `key` parameter downward should know that
the parameter's measured target also describes the app's *default* row, and that Tonalism's
distinguishing move is the compression of range, not the lowering of key. Correction 1 in §11.

---

## 4. Does the shipped row land inside that envelope?

Twelve EXIF-verified photographs at 768 px longest edge, six-paint fixture palette
(`Tests/StyleTestFixtures.SixPaints()`, 3,007 candidates), rendered through
`StylePipeline.Render` with `StyleRegistry.ByName("Realism")` and its own `DefaultValues` — the
shipped row, not a reconstruction of it. Ratios are output ÷ source, per image, averaged.

### 4.1 The headline table

| statistic | corpus target (realist ÷ photograph) | shipped delivers | verdict |
|---|---|---|---|
| local \|ΔL\*\| ratio | 0.819 | **0.806** | inside, −1.6% |
| L\*sd ratio | 0.903 | **0.927** | inside, +2.7% |
| notan-gap ratio | 0.894 | **0.935** | near, +4.6% |
| C\*mean ratio | 0.830 | 0.989 | misses, +19% |
| **C\*sd ratio** | **0.677** | **1.041** | **misses, +54%** |
| **L\*p1 ratio** | **2.55** | **5.32** | **right sign, 2.1× too far** |
| ΔL\*mean | −5.17 | +0.87 | misses by 6.0 L\* |
| mean ΔE from the source | — | **6.45** | the number every alternative loses on |

`[verified — computed locally 2026-07-31]`

**Three of seven inside or near.** No row in the Abstract, Fauvism, Post-Impressionism or Tonalism
rounds was inside its envelope on any statistic. Realism is inside on three without a single
parameter override.

### 4.2 And the absolute test says nothing, which is the more important result

Absolute statistics, shipped render against the realist corpus distribution:

| statistic | corpus mean ± SD | shipped render | z | unconverted photograph | z |
|---|---|---|---|---|---|
| L\*mean | 40.06 ± 13.01 | 49.16 | +0.70 | 48.31 | +0.63 |
| L\*sd | 20.12 ± 4.75 | 20.43 | **+0.07** | 22.04 | +0.40 |
| C\*mean | 13.75 ± 4.16 | 16.25 | +0.60 | 16.78 | +0.73 |
| C\*sd | 7.90 ± 2.48 | 11.67 | +1.52 | 11.21 | +1.34 |
| L\*p1 | 7.10 ± 7.22 | 12.58 | +0.76 | 3.40 | −0.51 |
| hue concentration | 0.846 ± 0.170 | 0.623 | −1.31 | 0.639 | −1.22 |
| notan gap | 32.38 ± 9.20 | 33.00 | **+0.07** | 35.28 | +0.32 |
| local ΔL\* | 7.12 ± 2.24 | 7.40 | **+0.13** | 9.18 | +0.92 |
| **RMS z** | | | **0.83** | | **0.84** |

`[verified. The unconverted-photograph L\*sd, C\*sd, notan and local ΔL\* are derived from the
shipped ratios rather than measured directly; the other four are measured.]`

**Passing a photograph through the entire converter moves it 0.01 of a standard deviation closer
to a realist canvas.** Not because the converter does nothing — it moves L\*p1 from z −0.51 to
z +0.76, a full 1.3 SD — but because the corpus is so wide that every plausible image is inside it.
A realist canvas can be Daumier's *Third-Class Carriage* at L\*mean 14.6 or Krøyer's Skagen beach
at 72.2. **There is no envelope to aim at**, and a style row that aimed at one would be aiming at
an average nobody painted. `[verified]`

### 4.3 The one thing the corpus asks for that the pipeline cannot express

The corpus wants chroma **variance** at 0.677 of the photograph's with chroma **mean** at 0.830.
`ToneAndChromaRemap`'s chroma parameter below gain 1.0 is `gain × chroma`
(`ToneAndChromaRemap.cs:136-139`, `[verified against the source]`), a pure scaling that moves mean
and spread together:

| chroma gain | delivered C\*mean ratio | delivered C\*sd ratio |
|---|---|---|
| target | **0.830** | **0.677** |
| 0.75 | 0.747 | 0.823 |
| 0.85 | 0.862 | 0.941 |
| 0.95 | 0.952 | 1.037 |
| 1.0 (shipped) | 0.989 | 1.041 |

`[verified — 12 photographs, each row a real render]`

No gain reaches the target pair; the closest, 0.75, undershoots the mean by 10% and still leaves
the spread 22% high. The operation the corpus describes is a contraction toward the image's own
mean chroma, `C* → C̄* + (C* − C̄*)·k`, which no stage implements. Whether it is worth building is
§8 pick 4 — and the answer is probably not, because it moves the one chroma statistic that
separates the movement by a margin the Tonalism round already showed the quantiser can absorb.

### 4.4 The value knobs, swept

All rows below carry `SmallRegionMerge` so that fragmentation does not confound the colour reading;
§6 explains why that stage must not actually ship here.

| variant | mean ΔE from source | L\*sd ratio | ΔL\*mean | C\*sd ratio | local ΔL\* ratio |
|---|---|---|---|---|---|
| *(corpus target)* | — | *0.903* | *−5.17* | *0.677* | *0.819* |
| **shipped (identity, no merge)** | **6.45** | **0.927** | **+0.87** | 1.041 | **0.806** |
| identity + merge | 10.72 | 0.960 | +2.05 | 1.088 | 0.668 |
| contrast 0.85 | 10.87 | 0.839 | +1.14 | 1.075 | 0.596 |
| contrast 0.92 | 10.68 | 0.889 | +1.42 | 1.083 | 0.627 |
| contrast 1.2 | 12.03 | 1.093 | +3.29 | 1.110 | 0.744 |
| key −4 | 11.34 | 0.929 | −1.55 | 1.083 | 0.636 |
| key −8 | 13.03 | 0.897 | −4.87 | 1.076 | 0.621 |
| contrast 0.92 / key −5 / chroma 0.87 | 12.04 | 0.861 | −3.23 | 0.937 | 0.595 |

`[verified]`

Two things fall out and both argue for identity:

- **Contrast is a net loss even where it helps.** Moving contrast 1.0 → 0.92 improves the L\*sd
  ratio by 0.038 toward its target and costs 0.179 on the local-ΔL\* ratio, which was already on
  target. It buys the global statistic by overspending the local one.
- **The value-key target is the weakest of the seven and the most expensive to chase.** ΔL\*mean
  −5.17 has t = −1.62; reaching it needs key ≈ −8.5, which costs 6.6 ΔE of fidelity and drops hue
  concentration further from its target. §8.5 shows what it looks like.

---

## 5. What the nearest-candidate match costs

Targets are every distinct 6-bit-quantised colour in each corpus — the same quantisation the
converter's own colour cache uses (`ColorQuantization.Key`), so the sample is exactly the set of
distinct colours the pipeline resolves. Errors are `NearestQuantiser.NearestIndex` against a real
`MixtureBuilder.Build()` candidate set, in plain CIELAB — the metric the converter uses.

### 5.1 Overall

| palette | candidates | realist targets, pixel-weighted ΔE | median | p95 | photographic targets |
|---|---|---|---|---|---|
| six-paint fixture | 3,007 | **5.58** | 4.80 | 12.30 | 5.53 |
| all 19 selectable | 84,063 | **2.48** | 1.82 | 6.85 | 2.65 |

`[verified]`

**Quantisation error is a gamut-coverage problem, not a sampling-density problem.** Median
nearest-neighbour spacing inside the six-paint candidate set runs 0.98–3.06 ΔE depending on the
L\* band; the error is 2–5× that. Sampling the same six paints more finely cannot close it. Adding
paints can, and does — 6 → 19 paints cuts the error by 56%.

### 5.2 Where in Lab it is worst, and how the realist target region sits

Six-paint fixture. "px share" is the share of realist corpus pixels in that band.

| L\* band | candidates | median NN spacing | realist px share | realist weighted ΔE | photo px share | photo weighted ΔE |
|---|---|---|---|---|---|---|
| 0–10 | 79 (2.6%) | 0.98 | **9.6%** | **6.88** | 10.6% | 7.97 |
| 10–20 | 432 (14.4%) | 0.97 | **15.9%** | 3.81 | 11.0% | 4.06 |
| 20–30 | 527 (17.5%) | 1.34 | **15.2%** | **2.70** | 11.8% | 3.16 |
| 30–40 | 549 (18.3%) | 1.83 | 12.8% | 4.20 | 11.6% | 3.89 |
| 40–50 | 496 (16.5%) | 2.60 | 11.8% | 6.28 | 10.9% | 5.54 |
| 50–60 | 440 (14.6%) | 2.88 | 10.9% | 7.24 | 12.1% | 6.66 |
| 60–70 | 275 (9.2%) | 3.03 | 8.7% | 7.33 | 13.1% | 6.19 |
| 70–80 | 122 (4.1%) | 3.06 | 7.4% | 7.08 | 8.1% | 6.69 |
| 80–90 | 48 (1.6%) | 1.50 | 6.4% | **8.90** | 6.9% | 6.22 |
| 90–100 | 39 (1.3%) | 0.53 | 1.3% | **8.20** | 4.0% | 5.71 |

`[verified]`

**The realist target region is the best-served region of the achievable gamut.** 53.5% of realist
pixel mass sits below L\* 40, against **52.8%** of the candidate set — a near-exact match reached
without any transform. The three densest, cheapest bands (L\* 10–40, median spacing 0.97–1.83,
weighted ΔE 2.70–4.20) carry **43.9%** of realist pixels against 34.4% of photographic ones.

**This is the exact mirror of the Tonalism round's result.** There, the style's own remap pushed
87% of targets into L\* 40–80, where only 44% of candidates live and spacing is 2.6–3.1 — and the
round's strongest recommendation was a scumble ladder to fix it. Realism needs none of that,
because `IdentityRemap` leaves the targets where the paint already is. **Any remap that lifts the
key would move realist targets out of the app's best-served band into its worst.** `[verified]`

The two genuinely badly-served regions for this style are the **highlights** (L\* 80–100:
weighted ΔE 8.20–8.90 over 7.7% of pixels, only 87 of 3,007 candidates) and the **deep shadows**
(L\* 0–10: 6.88 over 9.6%). Both are the ends of the value scale, both are where a limited paint
gamut runs out, and neither is reachable by a Lab remap.

### 5.3 The dark end, decomposed

Calling `EdgePreservingFloor.Apply` on its own, at Realism's declared defaults, over the same 12
photographs:

| stage | L\*p1 | L\*p5 | L\*sd |
|---|---|---|---|
| source photograph | 3.40 | 12.47 | 22.01 |
| **after the mandatory floor alone** | **7.50** | 16.16 | 21.01 |
| after the full render | 12.58 | — | 20.43 |
| *(realist corpus target)* | *7.10* | *11.20* | *20.12* |

`[verified]`

**The floor alone lands the dark end on 7.50 against a corpus figure of 7.10.** Nothing in the
app was designed to do that; it is the guided filter's window averaging away the darkest 1% of a
photograph. The nearest-candidate match then lifts it a further 5.1 L\*, and that lift is *not*
explained by dark-candidate scarcity: rendering the same photographs with all 19 selectable paints
(1,376 candidates below L\* 10 against the fixture's 79) leaves L\*p1 at 13.25 against 13.03 on a
common three-image subset. `[verified]` The residual is the 3-D geometry of nearest-neighbour
matching — a dark chromatic target's nearest candidate is often a lighter one — plus the 8-bit
gamut-mapped rendering the converter runs on (see §12).

---

## 6. The paintability trap

**This is the section that disagrees with every prior round in this directory.**

Realism leaves **41.5% of pixels in regions below its own mark²** on 12 photographs at 768 px and
**45.5%** at 1600 px, against the **3.0%** ceiling `StyleBehaviourTests` records on a 256²
synthetic gradient (`StyleBehaviourTests.cs:472`). The source photographs themselves measure
94.7–96.3%, so the conversion does most of the work; it just does not finish. `[verified]`

Every prior round's answer to an empty slot 5 has been `SmallRegionMerge`, and the repaired
union-find version in the working tree delivers exactly what it promises:

| variant | sub-mark share | mean ΔE from source | regions |
|---|---|---|---|
| *(source photograph)* | *94.71%* | *0* | *342,790* |
| **shipped Realism** | **42.70%** | **6.45** | 108,698 |
| floor edge 0.10 | 37.08% | 7.07 | 89,622 |
| floor strength 2 | 34.16% | 7.21 | 85,318 |
| floor edge 0.15 | 33.99% | 7.47 | 79,292 |
| floor edge 0.30 | 29.84% | 8.07 | 66,193 |
| floor strength 3 | 28.85% | 7.74 | 70,708 |
| floor strength 5 | 22.21% | 8.46 | 53,154 |
| floor strength 3 + edge 0.15 | 19.37% | 9.11 | 42,433 |
| floor strength 5 + edge 0.30 | 13.93% | 10.15 | 30,274 |
| **shipped + `SmallRegionMerge`** | **0.0000%** | **10.72** | 4,213 |
| floor s3/e0.15 + merge | 0.0000% | 9.65 | 3,954 |
| floor s5/e0.30 + merge | 0.0000% | 10.23 | 3,387 |

`[verified — 12 photographs, each row a real render]`

**It works, and it must not ship on this row.** §8.5 has the picture. At 1:1 on a portrait, the
merge replaces the sitter's eyes, mouth, moustache and headdress with flat blobs of the largest
neighbouring colour; the face becomes unreadable. The shipped row renders the same face
convincingly.

**The mechanism, and the design rule that follows.** An area opening reassigns every sub-mark
region to its largest neighbour. The damage therefore scales with **how much area it has to
reassign**, and Realism hands it more than any other row in the app:

| row | floor | remap | sub-mark share it hands the merge | merge's ΔE cost |
|---|---|---|---|---|
| Realism | strength 1.0, edge 0.05 | identity | **42.7%** | **+4.26** |
| Realism + floor e0.10 | strength 1.0, edge 0.10 | identity | 37.1% | +2.94 |
| Realism + floor s3/e0.15 | strength 3.0, edge 0.15 | identity | 19.4% | +0.54 |

`[verified]` **Realism runs the app's weakest declared floor and its only identity remap, so it
generates the most sub-mark area and pays the most for the merge.** It is the only row with no
`WithDefaults` call at all, so the floor sits at the stage's own declared strength 1.0 — which is
the case `EdgePreservingFloor`'s own doc comment describes and mis-attributes to Fauvism
(correction 6). The other four rows flatten
first — Tonalism at strength 2, Post-Impressionism at 3, Abstract at 5, each with a contrast or
palette transform ahead of the quantiser — which is why the merge is nearly free for them and
ruinous here.

**And flattening first does not rescue it**, because the flattening is what removes the face: at
strength 3 / edge 0.15 the guided filter has already erased the eyes before the merge runs (§8.5,
third panel). **There is no combination of slots 1 and 5 that makes this row paintable and keeps a
human face.** The subject matter of Realism lives below one mark.

---

## 7. The limited-palette question

### 7.1 The materials record says the palette was limited, and says nothing this app can act on

- **Courbet.** The Nelson-Atkins technical entry for *Jo, the Irish Woman* records "an opaque,
  white ground layer" under "a medium-toned reddish-brown imprimatura" which "remains visible
  throughout the final composition", and describes Courbet constructing form "by applying thin
  layers of paint in a limited range of colors". No pigments were identified analytically.
  `[verified — fetched
  [Schafer, technical entry, *French Paintings and Pastels, 1600–1945*, Nelson-Atkins, 2021,
  doi:10.37764/78973.5.506.2088](https://nelson-atkins.org/fpc/nineteenth-century-realism-barbizon/506/)]`
- **Homer.** Nineteen Winsor & Newton moist watercolours from a box owned by Homer were identified
  by emission spectrography, Debye-Scherrer X-ray diffraction and microscopy: brown earth/burnt
  umber, green earth with Prussian blue, Prussian blue, Indian yellow, Hooker's green, bone/ivory
  black, vermilion, burnt sienna/Mars orange, cadmium yellow, organic brown, red lakes, chrome
  orange and sepia. `[verified — fetched
  [Newman, Weston & Farrell, *JAIC* 19(2), 1980, 103–105](https://cool.culturalheritage.org/jaic/articles/jaic19-02-006_indx.html)]`
  It is a watercolour box, not an oil palette, and it dates from 1900–1910.
- **The general nineteenth-century palette** is reported as "Vermilion, Ivory black, Cobalt blue,
  Raw Sienna, Burnt ochre, Red ochre, Yellow ochres, Naples yellow, Silver white". `[relayed —
  painterspalettes.net, uncorroborated]`
- One secondary source measures Courbet's *The Painter's Studio* as having "most of its colours
  below 40% saturation, the highest at only 60%", against *The Sculptor* (1845) at up to 80%.
  `[relayed — [eclecticlight.co](https://eclecticlight.co/2015/10/01/pigments-technique-%E2%86%92-style-3-up-to-1850/);
  the measure is HSV-style saturation from a 16-colour extraction and is not comparable to C\*ab.
  My own measurement of the same painting is C\*mean 10.75.]`

**Every one of those palettes is dominated by earths, and a user of this app can select none of
them.** `PigmentLibrary.Selectable` holds 19 `TwoConstantMeasured` paints: one white, three
yellows, two oranges, four reds/magentas, one purple, five blues, two greens, one black — **and
not one earth**. Yellow Ochre, Raw Sienna, Burnt Sienna, Raw Umber, Burnt Umber, Burnt Umber Light
and Terre Verte Hue all exist in `PigmentLibrary.All` and every one of them is
`ReflectanceDerived`, so every one is withheld from the picker.
`[verified — `Pigments/pigments.manifest.txt`]`

**So a "Realist palette" preset naming pigments is rejected for exactly the reason the Fauvism
round rejected its viridian preset and the Tonalism round rejected report 02's *Sea and Rain*
five.** Third round, same rejection, same cause.

### 7.2 The question that is actually open: which *selectable* paints serve realist colour best

This is different from a preset, and it is the app's real lever. Every candidate set below is
built by the real `MixtureBuilder`; errors are pixel-weighted over the corpus target colours.

| palette | paints | candidates | min L\* | realist weighted ΔE | realist p95 | photo | Impressionist | **realist ÷ photo** |
|---|---|---|---|---|---|---|---|---|
| all 19 selectable | 19 | 84,063 | 6.43 | **2.48** | 6.85 | 2.65 | 2.27 | 0.936 |
| nine | 9 | 10,684 | 6.43 | 3.69 | 9.29 | 4.16 | 3.42 | 0.889 |
| split primary 8 + cobalt | 8 | 7,507 | 6.46 | 4.07 | 9.94 | 4.41 | 3.75 | 0.924 |
| split primary 7 | 7 | 4,896 | 6.46 | 4.23 | 10.09 | 4.73 | 4.06 | 0.894 |
| with Phthalo Green 7 | 7 | 4,853 | 6.43 | 4.80 | 11.91 | 4.91 | 3.90 | 0.978 |
| **earth-substitute 5** | 5 | 1,650 | 6.46 | **4.93** | 14.09 | 6.01 | 5.25 | **0.821** |
| **Zorn-like 4, warm yellow** | 4 | 797 | 11.23 | **5.55** | **19.01** | 8.09 | 7.05 | **0.686** |
| six-paint fixture | 6 | 3,007 | 6.46 | 5.58 | 12.30 | 5.53 | 4.81 | 1.010 |
| W/Y/R/B/Bk + cerulean 6 | 6 | 3,013 | 6.46 | 5.62 | 13.96 | 5.33 | 4.53 | 1.053 |
| primaries + white + black 5 | 5 | 1,649 | 6.46 | 6.03 | 14.08 | 6.12 | 5.44 | 0.986 |
| cool 6 | 6 | 2,732 | 6.46 | 6.27 | 15.24 | 6.27 | 5.32 | 1.000 |
| Zorn-like 4 (cool yellow) | 4 | 800 | 11.23 | 6.39 | 17.95 | 8.01 | 6.76 | 0.798 |
| no black 5 | 5 | 1,658 | 6.43 | 6.68 | 15.97 | 7.04 | 6.47 | 0.949 |

`[verified — computed locally. "earth-substitute 5" is Titanium White, Diarylide Yellow, Pyrrole
Red, Ultramarine Blue, Bone Black. "Zorn-like 4, warm yellow" is Titanium White, Diarylide Yellow,
C.P. Cadmium Red Light, Bone Black.]`

Three readings, and the third demolishes the first two:

- **Composition beats count at the low end.** *earth-substitute 5* beats the shipped six-paint
  fixture (4.93 vs 5.58) with one fewer paint and half the candidates, because it trades
  Quinacridone Magenta — a hue realist canvases barely use — for the warmer Diarylide Yellow.
- **There is a real style-specific signal, and it survives the obvious control.** The warm
  four-paint set is better for realist colour than for photographic colour by a ratio of **0.686**,
  and better than for *Impressionist* colour by **0.787**, while every other palette sits at
  0.80–1.05. The Impressionist control matters: both corpora are aged varnished oil scans from the
  same source population, so the advantage is not simply "aged oil". It is the earth-warm bias of
  §3.2.
- **And it is worthless as a recommendation, because I rendered it and looked.** §8.5: the warm
  four-paint set turns **every sky in the set grey**. Its pixel-weighted average is dominated by
  the low-chroma ground colours that fill a realist canvas; the sky is a large, high-chroma,
  maximally salient region and it collapses. **The corpus has almost no blue in it. A user's
  photographs do.** `[verified]`

The all-19 render is the most convincing picture in the study by a clear margin, and the ranking
by number of paints is the ranking by appearance. **The recommendation is "select more paints",
which is not a style recommendation, and is already what the app tells a user by letting them.**

### 7.3 What does not transfer, and should be said in the doc comment

Every technical description in §7.1 is of a **toned ground worked from dark to light with thin
layers** — Courbet's reddish-brown imprimatura showing through the final surface. That is the
parent README's fourth invariant category, "post-map, K-M layering", and this app cannot offer it.
Realism wants it less than Tonalism does (which wants glazing) but it is the same gap. `[inferred]`

---

## 8. Picks

### Pick 1 — Change nothing in slots 2, 3 and 5. Write down why.

- **Where:** `StyleRegistry.cs:33-40`. **Zero lines of behaviour.** The change is the doc comment.
- **Evidence:** §4 (three of seven corpus ratios inside without a parameter), §4.2 (the absolute
  envelope cannot discriminate at all), §4.4 (every remap setting tested costs 4.2–7.5 ΔE of
  fidelity and moves at most one ratio toward target while moving another away), §5.2 (the
  identity remap is *why* realist targets land in the best-served band of the gamut), §6 (the
  merge destroys faces).
- **What the doc comment should say**, replacing "exactly what the converter did before styles
  existed": that this row is measured against the movement and lands inside its value envelope
  without a transform; that the mandatory floor already delivers the dark-end lift the corpus
  asks for; that the identity remap is load-bearing because it keeps target colours in the
  densest region of the achievable gamut; and **that `SmallRegionMerge` is deliberately absent
  and must stay absent**, with the reason, because otherwise the next round will add it.
- **Confidence:** **high** on the colour conclusion; **high** on the merge exclusion (looked at);
  **medium** on whether "do nothing" is the right *product* answer, which is §8.5's question and
  not a measurement.

### Pick 2 — A per-style paintability ceiling that Realism is allowed to fail, or a different metric for it

- **Where:** `StyleBehaviourTests.EveryRegisteredStyleIsPaintable`, and whatever the registry-wide
  merge postcondition test the Tonalism round proposed turns into.
- **Why:** the Tonalism round's build-order item 5 is "write the merge postcondition as a
  registry-wide test". **If that is written as written, Realism either has to register the merge —
  which pick 1 says it must not — or the test has to exempt it.** Better: state the rule the
  measurement supports, which is that **a row whose claim is fidelity is not subject to the
  mark-size invariant in the same way as a row whose claim is a brushmark.** The 3.0% ceiling in
  the current test is a synthetic-fixture number and asserting it on a photograph would fail at
  41.5%.
- **Cost:** a decision plus ~10 lines. **Confidence: high** that the conflict is real and will
  land the moment someone implements the Tonalism round's item 5.

### Pick 3 — Move the floor's `edge` from the stage default 0.05 to 0.10 **only if** paintability is judged to matter here

- **Measured:** sub-mark 42.70% → **37.08%** for **+0.62 ΔE** (6.45 → 7.07), with the L\*sd ratio
  moving 0.927 → 0.904 (closer to the 0.903 target) and the local-ΔL\* ratio 0.806 → 0.719
  (further from 0.819). It is the cheapest paintability point on the whole ladder in §6 — every
  other rung costs more ΔE per point of sub-mark share.
- **But it is a 13% reduction in a 42% problem for a visible cost**, and the Tonalism round already
  took ε 0.10 for its own row on different grounds. I would **not** ship it: it buys too little of
  what it is for. Listed because it is the best of the alternatives and someone will ask.
- **Confidence:** **high** on the numbers; **low** that it is worth doing.

### Pick 4 — A chroma-variance contraction, as a general stage, and probably not

- **Where:** slot 2, either a parameter on `ToneAndChromaRemap` or a new `ILabRemap`.
  `C* → C̄*ᵢₘₐ𝓰ₑ + (C* − C̄*ᵢₘₐ𝓰ₑ)·k`, needing the image's own mean chroma, which means the same
  `IImageAwareCandidateTransform`-style access the Tonalism round's hue-rotation pick needed.
  **~60 lines.**
- **Evidence:** §3.2 and §4.3. C\*sd is the second-strongest separator in the study (t = −3.94)
  and the only one the pipeline cannot express. Realist canvases are uniform in colourfulness
  rather than low in it.
- **Why it is last and why I would not build it.** The Tonalism round measured a structurally
  identical stage — a per-pixel hue rotation — and found that **at low delivered chroma the
  quantiser absorbs the ask entirely** (0.670 → 0.668 for a near-total rotation). A chroma
  contraction toward the mean moves every pixel a *smaller* distance than that rotation did.
  I did not build it, so this is inference from a verified adjacent result, and the honest label
  is: **plausible, unmeasured, and with a specific reason to expect it to deliver nothing.**
- **Confidence:** **high** that the target statistic is right; **low** that a stage would move it.

### 8.5 What the picks look like — and the two things looking changed

Twelve photographs rendered through every variant; four subjects and one 1:1 crop inspected.

- **The shipped row looks genuinely good.** Faithful colour, solid silhouettes, a light painterly
  patching in flat areas that reads as brushwork rather than as noise. On a portrait at 1:1 the
  face is fully readable — wrinkles, eyes, the pattern on the headdress. This is not what four
  rounds of "the shipped row is broken" led me to expect. `[verified — I looked]`
- **`SmallRegionMerge` destroys faces, and the statistics said the opposite.** It takes sub-mark
  share to exactly 0.000000, which is the assertion the Fauvism round called "the single most
  valuable test available anywhere in this work". At 1:1 the same operation replaces the sitter's
  eyes with a flat pink patch, the headdress with a white blob and the mouth with nothing. On a
  street scene it erases the seated vendor's head. On a landscape it is fine. **The metric that
  says it works and the picture that says it does not are measuring the same operation.**
  `[verified — I looked, at 1:1]`
- **The strong-floor route is not an escape.** At strength 3 / edge 0.15 the guided filter has
  already smeared the eyes into the cheeks before the merge runs. Strength 5 / edge 0.30 is worse.
- **The Lab-remap combination reads as a faded photograph.** contrast 0.92 / key −5 / chroma 0.87
  hits more corpus ratios than anything else tested and, on the outdoor subjects, turns a blue sky
  grey-lavender and dulls the greens. It looks like an under-exposed scan, not like a Courbet.
- **The warm four-paint palette greys every sky** (§7.2). Its numbers are the best style-specific
  signal in the report and its appearance is the worst of the palettes tried. **This is the second
  consecutive round in which the best-scoring pick was demoted by ten minutes of looking**, and
  the failure mode is the same both times: a pixel-weighted average over a corpus whose colour
  distribution is not the user's.
- **The palette ladder is monotone in appearance.** all 19 > nine > split-primary-8 > the
  six-paint fixture > earth-substitute 5 > Zorn-like 4. Nothing about that ordering is
  realism-specific.

---

## 9. Method

Everything marked "computed locally" was produced on 2026-07-31 from a throwaway console project
in the session scratchpad, referencing `PaintTranslator.csproj` and named `PaintTranslator.Tests`
so the app's `InternalsVisibleTo` applies. **No file in the repository was modified** other than
this report; the probe lives outside the tree.

- **Stages are called, never transcribed.** The shipped Realism row is obtained from
  `StyleRegistry.ByName("Realism")` and rendered with `StylePipeline.DefaultValues(style)` — no
  reconstruction of its parameters exists in the probe. Variants are new `StyleDefinition`s built
  from the same shipped stage classes. `EdgePreservingFloor.Apply`, `ToneAndChromaRemap.Map`,
  `NearestQuantiser.NearestIndex`, `SmallRegionMerge.Refine`, `MixtureBuilder.Build`,
  `AbstractPaletteTransform`, `PalettePhotoConverter.RgbToLab`, `ColorQuantization.Key`,
  `PaintabilityMetrics.FractionInRegionsSmallerThan` and `CountRegions` are the shipped
  implementations.
- **Palette:** the six-paint fixture from `Tests/StyleTestFixtures.SixPaints()` (transcribed as a
  paint-index list only, since it is a fixture and not a pipeline stage; the indices were checked
  line by line), 3,007 candidates. §5.1 and §7.2 also use all 19 of `PigmentLibrary.Selectable`
  and eleven other subsets.
- **Render sources:** the first 12 photographs of the corpus in filename order, loaded at 768 px
  longest edge, converted at `RenderContext.DefaultMarkPixels(w, h) × MarkScale`. §6's resolution
  check re-runs six of them at 1600 px. **No figure in this report is drawn from `Tests/Golden`,
  from `BuildGradientBitmap` or from `BuildNoisyGradient`.**
- **Corpus statistics** are whole-image at ~700 px longest edge after the white-mount trim, through
  the app's own `RgbToLab`. Welch's *t* with Satterthwaite degrees of freedom; no correction for
  multiple comparisons is applied in the tables and the Bonferroni threshold is quoted in §2.
- **Image decoding** goes through the app's `ImageDecoder.DecodeFile`, with a Magick.NET fallback
  for the seven corpus files GDI+ rejects (CMYK and arithmetic-coded museum scans). Without the
  fallback those seven — including *The Gross Clinic*, the Millet *Angelus* and the Krøyer — would
  have dropped out silently, and they are among the largest museum files in the set.
- **Working-tree state.** `Imaging/Styles/StyleRegistry.cs`, `Imaging/Styles/MixtureBuilder.cs` and
  `Imaging/Styles/Stages/SmallRegionMerge.cs` carry uncommitted changes: the Tonalism round's
  picks 1 and 2 applied to Tonalism, the repaired `MostNeutralPaintIndex`, and the union-find
  merge. **Realism's row is untouched by all of them**, so every "shipped Realism" figure is a
  figure for the shipped row. Every `SmallRegionMerge` figure is against the **repaired** stage.

---

## 10. What not to build

The parent, Abstract, Fauvism, Post-Impressionism and Tonalism lists all still apply. These are
additional, each rejected after going looking for it.

- **`SmallRegionMerge` on Realism, at any floor setting.** §6, §8.5. Reaches exactly 0.000000 and
  destroys human faces; costs +4.26 ΔE of fidelity; no floor setting rescues it because the
  flattening that would make the merge cheap is what removes the face. **This is the first time in
  five rounds that the item every round has recommended is wrong for a row, and the reason is
  specific: an area opening deletes exactly what this movement is about.** `[verified]`
- **Any `ToneAndChromaRemap` setting on this row.** §4.4. All 22 variants tested cost 4.2–7.5 ΔE
  of fidelity; the best-scoring combination reads as a faded photograph; and the two knobs that
  help a global statistic (contrast, key) each damage a local one that was already on target.
  `[verified]`
- **Lowering the key on Realism.** ΔL\*mean −5.17 is the weakest of the seven targets (t = −1.62),
  it does not separate Realism from Tonalism at all (t = +0.18 on L\*mean), and reaching it moves
  target colours out of the app's densest gamut band (L\* 10–40, 43.9% of realist pixels, weighted
  ΔE 2.70–4.20) into its sparsest. `[verified]`
- **A chroma multiplier of any value.** §3.2: C\*mean does not separate the movement from
  photographs (t = −1.80) and runs backwards against Impressionism. §4.3: the stage cannot express
  what C\*sd asks for anyway. `[verified]`
- **A "Realist palette" preset naming pigments.** §7.1. Courbet's imprimatura, Homer's nineteen
  watercolours and the general nineteenth-century list are all earths, and every earth in
  `PigmentLibrary.All` is `ReflectanceDerived` and withheld from the picker. **Third round, same
  rejection.** `[verified against the manifest]`
- **A short "realist" default palette of selectable paints**, including the warm four-paint set
  that has the best style-specific number in the report. §7.2, §8.5: it greys every sky. Its
  advantage is an artefact of pixel-weighting a corpus that contains almost no blue.
  `[verified — measured and looked at]`
- **Raising `MarkScale` above 1.0 to make the row paintable.** It attacks the metric, not the
  cause: sub-mark share is 41.5% at 768 px and **45.5%** at 1600 px, so the problem is not that
  the mark is too small relative to the image. `[verified]`
- **Treating the 3.0% `EveryRegisteredStyleIsPaintable` ceiling for Realism as meaningful**, or
  writing the registry-wide merge postcondition without exempting this row. §6, pick 2.
- **Splitting an "academic naturalism" row from Realism.** §1: six academic works sit inside the
  realist distribution on every statistic, between the naturalists and the Germans/Nordics. There
  is nothing to separate. `[verified]`
- **Adding the Pre-Raphaelites to this row.** §1: C\*mean 19.60 against 13.75, local ΔE 16.84
  against 9.15. They belong nearer Fauvism. `[verified]`
- **A "make the darks darker" stage, in any style.** The one strong dark-end result in this study
  runs the other way: canvases stop at L\*p1 7.10 where photographs reach 2.79, and the achievable
  gamut's own floor is 6.43–6.46. The paint already enforces the canvas behaviour. `[verified]`
- **Sampling the candidate set more finely to reduce quantisation error.** §5.1: the error (5.58)
  is 2–5× the median candidate nearest-neighbour spacing (0.98–3.06), so it is a coverage problem.
  Six paints sampled twice as finely will not approach nineteen paints sampled as now (2.48).
  `[verified]`

---

## 11. Corrections to prior research

**1. Value key does not separate Tonalism from Realism, and the Tonalism round's framing needs
that qualifier.** That round reported Tonalist L\*mean 39.37 against photographs 50.01 (t = −3.15)
and Impressionism 61.38 (t = −6.96), and made value key the largest of its two significant
statistics. Measured against a 53-work realist control on a corpus processed identically:
**Tonalist 39.13, realist 40.06, t = +0.18** — the smallest *t* in this report. What separates the
two nineteenth-century oil groups is value *range* (L\*sd 13.58 vs 20.12, t = +2.35) and the light
end (L\*p99 66.83 vs 83.58, t = +2.94). The Tonalism round's conclusion survives against the
controls it used; its *reason* ("the movement is low-key") describes the app's default row equally
well. `[verified]`

**2. Tonalist canvases are not lower in chroma than realist ones — they are higher.** C\*mean
17.89 vs 13.75. This is a third independent confirmation of that round's own correction 6, from a
control it did not have. `[verified]`

**3. "Register `SmallRegionMerge` on every row with an empty slot 5" is wrong for Realism, and the
rule that replaces it is about how much sub-mark area the row generates.** Four rounds have
recommended it and it has been right four times. On Realism it costs +4.26 ΔE and destroys faces,
because Realism runs the app's weakest floor and its only identity remap and therefore hands the
merge 42.7% of the image, against 19.4% when the floor is raised. **The merge's cost scales with
what it is given.** `[verified]`

**4. Realism's published paintability figure is 14× too small, on the same mechanism the Tonalism
round found for its own row.** `EveryRegisteredStyleIsPaintable` records a 3.0% ceiling on a 256²
synthetic gradient; photographs give 41.5% at 768 px and 45.5% at 1600 px. The Tonalism round
recorded Realism at 51.30% from an independent corpus at a larger mark; mine is lower because a
768 px longest edge gives mark² = 9 against that round's 25. **Both are an order of magnitude from
3.0% and the direction is the same.** This is the fifth consecutive round to find a
synthetic-fixture spatial figure false on photographs. `[verified]`

**5. The parent README's "the palette and the edge treatment carry the load" is confirmed from an
unexpected direction.** For this row the palette carries essentially all of it: 6 → 19 selectable
paints cuts quantisation error 5.58 → 2.48 ΔE and is the only change in the study that visibly
improves the picture, while every transform tested makes it worse. `[verified]`

**6. `EdgePreservingFloor`'s doc comment names the wrong style, and the right one is the subject of
this report.** It says "a style that registers a large `MarkScale` without a floor strength to
match — **Fauvism runs this stage at its own weakest declared default**; Abstract already registers
this stage's strongest — can still leave more of its output fragmented"
(`EdgePreservingFloor.cs:19-21`). Fauvism registers **strength 3.0** (`StyleRegistry.cs:87`,
unchanged from HEAD). **Realism is the only row in the app that runs the floor at its declared
default of 1.0**, because it is the only row with no `WithDefaults` call at all. The comment's
argument is correct and it is describing Realism. `[verified against the source]`

**7. `StyleRegistry`'s Fauvism comment contradicts the code three lines below it.** "Only contrast
and chroma are overridden below; the floor's `strength` and the remap's `key` already sit at the
stage's own declared defaults (1.0 and 0.0), so naming them again here would be a no-op override"
— immediately followed by `(fauvismFloor, "strength", 3.0)`. Present in HEAD and in the working
tree. `[verified against the source]`

**Confirmed, not corrected:** mean chroma does not separate a nineteenth-century movement from
photographs (Tonalism round, correction 6 — reproduced here for a second movement, t = −1.80); the
repaired union-find `SmallRegionMerge` reaches exactly 0.000000 in one pass on photographs
(reproduced on 12 more); and `MostNeutralPaintIndex`'s repair is in the working tree and scores
`chroma + 0.10·|L*−50|` as the Tonalism round's pick 2 specified (`MixtureBuilder.cs:191-197`,
`[verified against the source]`).

---

## 12. Accuracy warnings

Read these before quoting any figure.

- **The dark-end result is the report's load-bearing number and it has an unresolved confound.**
  L\*p1 7.10 vs 2.79 could in principle be a scan-processing artefact (lifted shadows in museum
  photography). Against that: Impressionist scans measure 21.16 and Tonalist 16.25 from the same
  source population, so it is not a blanket effect, and 8 of 53 realist works reach below 3.5. It
  is not settled. Verification debt 1.
- **All canvas colorimetry is uncalibrated web reproductions of varnished, aged oil paintings.**
  Varnish yellowing raises measured b\* and lowers measured L\*. The +10.84 b\*mean and the
  87% warm-pixel share in §3.2 are the least trustworthy numbers in the report, and they are what
  the palette result in §7.2 rests on.
- **Only two of eighteen statistics survive a multiple-comparison correction** against the
  photographic control. Everything else in the §3 table at |t| between 2 and 3 is suggestive.
- **The render figures are 12 photographs at 768 px through one six-paint palette.** The corpus
  ratios they are compared against come from 53 canvases and 23 photographs, and the 12-photograph
  subset's own mean L\* is 48.31 against the 23-photograph group's 45.23, so the ratios in §4.1
  should be read to two significant figures, not three.
- **The visual pass is four subjects plus one 1:1 crop, judged by one agent.** It overturned two
  recommendations, which is the argument for it, not evidence that it is sufficient. Nobody has
  viewed a full-resolution conversion or put one beside a Courbet.
- **`MixtureBuilder.RenderMixture` goes through `SpectralRenderer.ToDisplayColor`**, so every
  candidate colour in this report is gamut-mapped 8-bit, a mean 3.35 ΔE from unmapped spectral Lab
  `[relayed — Tonalism round correction 11]`. That affects the absolute ΔE figures in §5 and the
  dark-end residual in §5.3; it does not affect any comparison between two configurations measured
  the same way.
- **The 12 render photographs overlap the Tonalism round's corpus in three files.** That is
  deliberate for comparability and it means the two rounds' absolute percentages are not fully
  independent.
- **No colorimetry of Realism exists in the literature.** I searched for it specifically and found
  none — the fifth consecutive round to report that absence for its own movement. Sigaki et al.
  2018 (PNAS 115, E8585) analyses ~140,000 paintings by permutation entropy and statistical
  complexity but its abstract does not enumerate styles, and I could not confirm whether Realism
  appears. `[verified that the abstract does not enumerate; the supplementary information was not
  obtained]`

---

## 13. Verification debt

Ranked by how much clearing each would change a decision.

1. **Whether the L\*p1 gap survives colour-managed reproductions.** The report's strongest
   statistic and its main practical consequence (the floor already delivers the corpus dark end;
   no stage should chase darker) both rest on it. A dozen museum downloads with embedded profiles,
   or the same works from two sources, would settle it.
2. **Render pick 1 against a Courbet or a Homer, side by side, at full resolution.** §8.5 is four
   subjects at 300 px plus one crop, and it already overturned two picks. This is the cheapest
   item on the list and it gates the only real decision in the round.
3. **Whether the merge's face damage is a mark-size effect that a user's slider can escape.**
   I measured at mark 3 (768 px) and mark 6 (1600 px) and the sub-mark share rose. I did not test
   whether a *smaller* user mark makes the merge survivable, which is the one configuration in
   which registering it might be defensible.
4. **Curate a shared, provenance-checked corpus and commit it.** Carried forward unchanged from
   the Post-Impressionism and Tonalism rounds, where it was debt 2 and debt 3 and uncleared both
   times. Five consecutive rounds have each independently rediscovered contamination; this round
   found two modes the others did not record (a greyscale image that passed a visual pass and was
   caught only by re-checking numerically, and a gallery *installation* photograph rather than a
   framed one).
5. **Contemporary representational painting is entirely absent from the corpus.** The brief asked
   for it and Commons cannot supply it cleanly. If the row is meant to serve users converting
   modern photographs in a modern realist idiom, nothing here speaks to that.
6. **Whether a chroma-variance contraction delivers anything** (pick 4). Inferred to deliver
   nothing, from a verified adjacent result on a different stage. One probe would settle it.
7. **Sigaki et al. 2018's supplementary style list**, to find out whether Realism is separable by
   published image statistics at all. Three rounds have now failed to establish whether their own
   movement appears in it.
8. **The eclecticlight saturation figures for Courbet** are `[relayed]` from a secondary blog using
   an HSV-style extraction; they are quoted only as colour, not used.
9. **The general nineteenth-century palette list** in §7.1 is `[relayed]` from one painter-facing
   site. The Courbet and Homer entries are `[verified]` from museum and JAIC sources and support
   the same conclusion, so the risk to §7's ruling is low.

Items 1–3 are local work, cost little, and gate more than everything else on the list combined.

---

## 14. Corpus provenance

**Source:** Wikimedia Commons, 2026-07-31. Painting titles were resolved through the English
Wikipedia article's own lead image (`prop=pageimages&piprop=original|name`) where the work has an
article, and through Commons `list=search` otherwise; every title was then resolved through
`commons.wikimedia.org/w/api.php` (`prop=imageinfo&iiprop=url|extmetadata|commonmetadata`) and
downloaded as an 800 px thumbnail (served at 960 px). Nothing was taken from a search ranking
unexamined.

**Realist paintings — 53 kept, 1 rejected.**

| group | n | works |
|---|---|---|
| Courbet | 6 | *The Stonebreakers*; *A Burial at Ornans*; *L'Atelier du peintre*; *Bonjour Monsieur Courbet*; *The Wheat Sifters* (Gustave Courbet 014); *Les Baigneuses* |
| Millet | 4 | *The Gleaners*; *L'Angélus*; *The Sower*; *Man with a Hoe* |
| Homer | 9 | *Snap the Whip*; *The Gulf Stream*; *Breezing Up*; *The Fog Warning*; *Northeaster*; *Eight Bells*; *Prisoners from the Front*; *The Veteran in a New Field*; *The Herring Net* |
| Eakins | 4 | *The Gross Clinic*; *The Agnew Clinic*; *Max Schmitt in a Single Scull*; *Swimming* |
| naturalism | 8 | Bastien-Lepage *Jeanne d'Arc*, *October*; Breton *Le chant de l'alouette*; Lhermitte *La paye des moissonneurs*; *Les foins*; Fildes *Applicants for Admission to a Casual Ward*, *The Doctor*; Herkomer *Hard Times* |
| urban French | 4 | Daumier *The Third-Class Carriage*; Degas *In a Café (L'Absinthe)*; Caillebotte *The Floor Planers*, *Paris Street; Rainy Day* |
| academic | 6 | Bouguereau *The Birth of Venus*, *Nymphs and Satyr*, *The Broken Pitcher*; Gérôme *Pollice Verso*, *Le charmeur de serpents*, *Suites d'un bal masqué* |
| Russian | 6 | Repin *Volga Boatmen*, *Reply of the Zaporozhian Cossacks*, *Ivan the Terrible and His Son*, *Religious Procession in Kursk*, *They Did Not Expect Him*; Perov *Troika* |
| German/Nordic | 4 | Menzel *Eisenwalzwerk*, *Das Balkonzimmer*; Leibl *Three Women in Church*; Krøyer *Summer Evening on Skagen's Beach* |
| American other | 2 | Anshutz *The Ironworkers' Noontime*; Tanner *The Banjo Lesson* |

**Rejected: 1.** Courbet *Les demoiselles des bords de la Seine* — photographed inside its gold
frame.

**Photographs — 23 kept, 4 rejected.** All Commons featured pictures; EXIF from the API's
`commonmetadata`.

| kept | Make / Model / DateTimeOriginal |
|---|---|
| Schaupflügen, Fahrenwalde | Canon PowerShot G5, 2004-09-12 |
| 2013 Rainbow over Washfold | Olympus E-M5, 2013-10-27 |
| 2014 Track on Fremington Edge | Olympus E-M1, 2014-09-27 |
| 2015 Swaledale from Kisdon Hill | Olympus E-M1, 2015-09-07 |
| Inle Lake, Myanmar | Pentax K-5 II, 2016-08-05 |
| **A Tibetan Pilgrim Lighting Ghee Lamps** | *(no EXIF — the one exception, kept and flagged)* |
| A bad sales day | Nikon D300, 2013-03-02 |
| A girl set fire to cook breakfast | Nikon D3200, 2014-07-26 |
| A man and his donkey, Aswan | Canon EOS 6D, 2019-01-26 |
| Banaue, Ifugao tribesman | Sony DSLR-A700, 2008-11-08 |
| Beignet maker | Nikon D3300, 2017-11-25 |
| Birka, June 2013 | Nikon D600, 2013-06-02 |
| Blond and green rice fields | Canon EOS 5D Mark IV, 2017-10-13 |
| Bronze casting, Kunstgießerei München | Panasonic DMC-FZ1000, 2023-11-06 |
| Cap San Diego | Nikon D200, 2011-08-12 |
| Lençóis Maranhenses dune | Canon EOS 550D, 2011-06-14 |
| Comercio, Tánger | Canon EOS 5DS R, 2015-12-11 |
| Copper bleach | Nikon D750, 2017-01-19 |
| Cotton miller, Kültür | Canon EOS 5D Mark III, 2020-09-27 |
| Craftmen at work, Luang Prabang | Canon EOS 5D Mark IV, 2018-06-15 |
| Crate maker | Nikon D750, 2018-09-14 |
| Elektriker bei der Arbeit | Olympus E-M1 Mark II, 2017-08-01 |
| Girl of the Welayta people | Nikon D600, 2014-07-27 |

**Rejected: 4.** *Bearded man smoking pipe* and *Fishmonger smiling, Maracaibo* — black and white.
*Cycling Amsterdam 03* — greyscale, C\*mean 0.000, caught by re-checking a visual suspicion
numerically. *Aquaculture in Chile* (3.74:1) and *Ahuriri River* (3.80:1) — stitched panoramas.

**Controls.** Impressionist, 9 kept: Sisley *Bridge at Moret-sur-Loing*; Pissarro *Boulevard
Montmartre, Spring* and *…at Night*; Monet *Cliff Walk at Pourville*, *Haystacks, end of Summer*,
*Impression, Sunrise*; Renoir *Moulin de la Galette*, *Luncheon of the Boating Party*; Cassatt
*The Boating Party*. **Rejected: 1** — Monet *Reflections of Clouds on the Water-Lily Pond*, a
gallery installation photograph. Tonalist, 6: Whistler *Nocturne in Black and Gold* and *Old
Battersea Bridge*; Inness *The Home of the Heron* and *Early Morning, Tarpon Springs*; Tryon
*November Morning*; Blakelock *Moonlight*. Pre-Raphaelite, 3: Brown *Work* and *The Last of
England*; Hunt *The Awakening Conscience*.

**All 101 downloaded images were viewed on seven contact sheets before any statistic was computed**,
and five of the seven rejections are what that pass found. The rejected files are retained in the
session scratchpad.
