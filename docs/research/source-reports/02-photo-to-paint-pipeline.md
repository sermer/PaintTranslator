# Research 02 — RGB Photo → Paint Pipeline: Input Side & Matching Metric

**Agent:** Task 2 (input side + colour-difference metric; mixing physics is Task 1/3)
**Date:** 2026-07-26
**Scope:** sRGB decode, viewing conditions, chromatic adaptation, ΔE metric choice, gamut
mapping, value compression, quantization/segmentation, photo prep, match-quality UX.

**Method note:** web research only. No repository files were modified. Two small throwaway
scripts were run in the session scratchpad to compute the CIELAB numbers marked *(computed)*.

---

## 0. Executive summary — the five things that matter

1. **`Data/GoldenPalette.cs` stores sRGB triples, but Golden publishes measured
   CIELAB for every Heavy Body colour.** Replacing round-tripped sRGB with measured
   L\*a\*b\* is the single highest-value change available and costs almost nothing.
   ([Golden Heavy Body pigment data](https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data))
2. **ΔE76 → ΔE2000 is the right direction but ΔE2000 is the wrong hammer for
   *this* job.** CIEDE2000 is only recommended for differences **< 5 ΔE units**; its
   built-in mathematical discontinuities are bounded at 0.274 below 5 units but "rise
   sharply" above that. Paint-matching residuals in a gamut-limited palette are routinely
   10–40 units. Use **ΔE2000 for reporting** and a **weighted-ΔE / HyAB-style metric for
   searching**. (See §2.)
3. **The dominant error is not hue, it's value range.** Golden Titanium White
   L\*98.25, Bone Black L\*23.82 → a paint gamut spanning **74 L\* units and a ~24:1
   contrast ratio** *(computed)*, versus sRGB's 0–100 / effectively unbounded. Every
   photo shadow below ~L\*24 is unreachable. Naive nearest-neighbour crushes them all
   into one black; sigmoidal L\* rescaling preserves the relationships. (See §3, §4.)
4. **The premise "paint gamut ⊂ sRGB" is false in the chroma dimension.** 22% of
   measured acrylics fall outside AdobeRGB. Golden Cad Yellow Medium is
   L\*84.1 C\*95.5 h82°, while sRGB's yellow corner is L\*97.1 C\*96.9 h103°
   *(computed)* — different hue, 13 L\* lower. It's not a subset, it's an *overlap*.
   You need real gamut mapping in both directions, not clipping. (See §3.)
5. **Do not dither. Segment.** Region-based segmentation (bilateral prefilter → SLIC
   superpixels → RAG merge → min-area cleanup) is what produces paintable output.
   (See §5.)

---

## 1. The emissive / reflective mismatch

### 1.1 sRGB decode — the exact transfer function

From the W3C/IEC sRGB specification, the **inverse** (encoded → linear) function:

```
if  V_srgb <= 0.04045 :  V_lin = V_srgb / 12.92
else                  :  V_lin = ((V_srgb + 0.055) / 1.055) ^ 2.4
```

and the forward direction, for completeness:

```
if  V_lin <= 0.0031308 :  V_srgb = 12.92 * V_lin
else                   :  V_srgb = 1.055 * V_lin^(1/2.4) - 0.055
```

Source: [W3C "A Standard Default Color Space for the Internet — sRGB"](https://www.w3.org/Graphics/Color/sRGB.html)
(standardised as IEC 61966-2-1:1999).

Then linear sRGB → XYZ with the D65-adapted matrix (Lindbloom's higher-precision form,
first row `0.4124564 0.3575761 0.1804375`) —
[Lindbloom RGB/XYZ matrices](http://www.brucelindbloom.com/Eqn_RGB_XYZ_Matrix.html).
Note: `brucelindbloom.com` currently fails TLS handshake from this environment; the
matrix values were confirmed indirectly via search result text, so **verify the full
3×3 against a second source before hard-coding.**

Then XYZ → Lab with the standard `f(t) = t^(1/3)` for `t > 216/24389`, else
`(24389/27·t + 16)/116`.

> **Do not use the "gamma 2.2" shortcut.** The piecewise function's effective exponent
> near black is 1.0 (the linear segment), not 2.2, and the difference is largest exactly
> where paint value structure matters most — the shadows.

### 1.2 Why gamma-space averaging is wrong

This bites the app in three places: downsampling the photo, averaging a superpixel's
colour, and computing region means.

sRGB 128 corresponds to roughly **21% of maximum luminance, not 50%**
([Poynton, via colormanagement.guide](https://colormanagement.guide/en/foundations/gamma-linearity/);
Poynton's Gamma FAQ, [poynton.ca](https://www.poynton.ca/faq/gammafaq/GammaFAQ.pdf)).
My own computation puts sRGB mid-grey 128 at **L\*53.59, Y = 21.59%** *(computed)*.
Averaging encoded values therefore produces a result that is too dark — this is the
classic "dark halo" from blurring in gamma space
([NVIDIA GPU Gems 3, "The Importance of Being Linear"](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-24-importance-being-linear)).

**Rule for the codebase:** any *mean of pixel colours* must be computed in linear light
(or in CIELAB, which is fine because L\* is already a perceptual, additively-averageable
lightness). Never in 8-bit sRGB. `System.Drawing` bilinear/bicubic resizing averages in
gamma space — this is a real, if small, bug for any downscale step.

### 1.3 sRGB's assumed viewing conditions vs. a painting's

| | sRGB reference (IEC 61966-2-1) | Painting under gallery light | Painting under ISO 3664 |
|---|---|---|---|
| White point | D65 (x .3127 y .3290) | ~3000 K warm white | D50 |
| White luminance | 80 cd/m² | ~50–200 lux → ~15–60 cd/m² | 2000 lux → ~500 cd/m² |
| Surround | 20% reflectance | dark/dim room | average |
| Ambient | 64 lux, D50, 1% flare | 50–200 lux | 2000 lux ±500 |

Sources: [W3C sRGB spec](https://www.w3.org/Graphics/Color/sRGB.html) ·
[ICC sRGB registry](https://registry.color.org/rgb-registry/srgb) ·
[ISO 3664:2009](https://www.iso.org/standard/43234.html) and
[JUST-Normlicht summary](https://www.just-normlicht.com/us/iso-3664-2009.html) ·
museum practice: warm 2800–3000 K preferred at museum levels of 50–200 lux, paintings
typically 150–200 lux, CIE 157:2004 capping exposure at 150 klx·h/yr
([Canadian Conservation Institute TB36](https://www.canada.ca/en/conservation-institute/services/conservation-preservation-publications/technical-bulletins/led-lighting-museums.html),
[bannolighting gallery CCT guide](https://bannolighting.com/blog/art-gallery-lighting-color-temperature/)).

Note the spread: **80 cd/m² (screen) vs ~30 cd/m² (gallery) vs ~500 cd/m² (ISO 3664
print booth)** — more than a decade of adapting luminance. That matters because of the
**Hunt effect** (colourfulness rises with luminance) and the **Stevens effect** (lightness
contrast rises with luminance)
([Wikipedia: Hunt effect](https://en.wikipedia.org/wiki/Hunt_effect_(color)) ·
[IES definition](https://ies.org/definitions/hunt-effect/) ·
[Fairchild, *Color Appearance Models*, PDF](https://scis.uohyd.ac.in/~chakcs/cipclass/lecs/ColourAppearance.pdf)).
Neither CIELAB nor ΔE2000 models either effect — they are relative-colorimetric only.

### 1.4 Chromatic adaptation

Von Kries-style CAT: convert XYZ → cone-like space with matrix **M**, scale each channel
by the ratio of destination/source white in that space, convert back.

```
Bradford  M = [ 0.8951  0.2664 -0.1614
               -0.7502  1.7135  0.0367
                0.0389 -0.0685  1.0296 ]

CAT02     M = [ 0.7328  0.4296 -0.1624
               -0.7036  1.6975  0.0061
                0.0030  0.0136  0.9834 ]
```
Source: [colour-science `colour/adaptation/datasets/cat.py`](https://github.com/colour-science/colour/blob/develop/colour/adaptation/datasets/cat.py)
and [colour.adaptation docs](https://www.colour-science.org/api/0.3.6/html/colour.adaptation.html).
D50 PCS white is X 0.9642, Y 1, Z 0.8249 ([ICC, "Why is the media white point D50?"](https://www.color.org/whyd50.xalter)).

**Recommendation: use Bradford, not CAT02.** CAT02 can predict **negative tristimulus
values** for some samples — a hard failure mode when you then feed the result into a
K–M / reflectance model that assumes non-negativity. Brill & Süsstrunk's corrected matrix
still fails for many samples, and the constrained-optimisation fix costs ~1 ΔE of accuracy
([Li & Perales et al., "Mathematical approach for predicting non-negative tristimulus
values using CAT02"](https://www.researchgate.net/publication/227725635_Mathematical_approach_for_predicting_non-negative_tristimulus_values_using_the_CAT02_chromatic_adaptation_transform)).
Bradford is the pragmatic default and what ICC-style workflows use.

### 1.5 Is CIECAM02 / CAM16-UCS worth it here?

**Evidence for:** CAM16-UCS is the best-performing space in the literature. Luo et al.'s
comprehensive test across available visual datasets found **CAM16-UCS gives the overall
best performance across all three groups of datasets**, best at small differences, and
best at predicting COMBVD ellipse data for both local and global uniformity
([Luo, *Color Res. Appl.* 2023, doi:10.1002/col.22844](https://onlinelibrary.wiley.com/doi/10.1002/col.22844?af=R) —
abstract only, paywalled). Jzazbz is second, and is the better choice if HDR ever matters
([Safdar et al., *Optics Express* 25(13):15131](https://opg.optica.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272)).
CAM16-UCS constants are `c1 = 0.007`, `c2 = 0.0228`
([Li et al. 2017, CAM16/CAT16/CAM16-UCS](https://onlinelibrary.wiley.com/doi/abs/10.1002/col.22131);
implementation in [colour.XYZ_to_CAM16UCS](https://colour.readthedocs.io/en/develop/generated/colour.XYZ_to_CAM16UCS.html)).

**Evidence against, for this app:**
- CAM16 requires you to *know* L_A (adapting luminance, conventionally 20% of scene
  white), the surround (average / dim / dark → F, c, N_c), and Y_b. A hobbyist
  photographing a scene and painting in an unknown room cannot supply any of them
  ([CAM16 surround parameters, colour-science source](https://colour.readthedocs.io/en/develop/_modules/colour/appearance/cam16.html)).
  You'd be substituting confident-looking guesses for a known-wrong assumption.
- CAM16-UCS is still under revision, specifically its power correction
  ([Li & Luo, "Revising CAM16-UCS", CIC30](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/30/1/48)).
- The app's dominant error source is a ~25 L\* unit black-point deficit. A model that
  buys you 10–20% better *small*-difference prediction is noise next to that.

**Verdict:** stay in CIELAB under a fixed illuminant for now. Do it *properly* — correct
piecewise decode, explicit D65, Bradford adaptation available as an option — and expose
a single "viewing illuminant" setting (D65 / D50 / 3000 K gallery) that applies a Bradford
CAT. Revisit CAM16-UCS only after gamut mapping and value compression are in. If you do
adopt an appearance model later, CAM16 with `surround = average`, `L_A = 20 cd/m²`,
`Y_b = 20` is the defensible default for a lit room.

---

## 2. Colour-difference metrics — be opinionated

### 2.1 The four candidates

**ΔE76 (what the code uses).** Plain Euclidean distance in L\*a\*b\*:
`ΔE*ab = sqrt(ΔL*² + Δa*² + Δb*²)`. Its documented failure modes are exactly the regions
a photo-to-paint app lives in:

- **Over-weights chroma in saturated regions**, because it ignores the chroma
  and hue weighting that matches perception
  ([PRINTING United Alliance, "Dissecting Delta E"](https://www.printing.org/content/2025/02/18/dissecting-delta-e-and-the-mathematical-difference-between-colors)).
- **Mis-ranks blues.** The CIELAB blue region (h ≈ 250°–300°) is not hue-linear: at
  fixed L\* and h, increasing C\* produces a visible hue shift. ΔE76 has no correction
  for this; ΔE2000 adds the R_T rotation term centred at 275°
  ([ColorAide distance docs](https://facelessuser.github.io/coloraide/distance/)).
  ΔE76 consistently *over*estimates perceived difference for blues — two blues at
  ΔE76 = 3 can look nearly identical where the same ΔE76 in green is obvious
  ([ColorFYI, "What is Delta E?"](https://colorfyi.com/blog/what-is-delta-e/)).
- Empirically: STRESS 28.6 for CIELAB vs 25.9 for CIEDE2000 on the same data
  ([Optimizing parametric factors in CIELAB and CIEDE2000, PMC9227931](https://pmc.ncbi.nlm.nih.gov/articles/PMC9227931/)).
  On COMBVD (3,813 pairs, 6 experiments), CIEDE2000 scores ~29.2
  ([Oklch+ arXiv:2606.05255](https://arxiv.org/html/2606.05255v1)).

**ΔE94.**
`ΔE94 = sqrt((ΔL*/k_L·S_L)² + (ΔC*/k_C·S_C)² + (ΔH*/k_H·S_H)²)` with `S_L = 1`,
`S_C = 1 + K₁·C₁*`, `S_H = 1 + K₂·C₁*`; graphic arts `k_L=1, K₁=0.045, K₂=0.015`,
textiles `k_L=2, K₁=0.048, K₂=0.014`
([Wikipedia: Color difference](https://en.wikipedia.org/wiki/Color_difference)).
**It is a quasimetric — it violates symmetry**, because `S_C`/`S_H` use the *reference*
colour's chroma `C₁*` only ([Colors.jl colour differences](http://juliagraphics.github.io/Colors.jl/stable/colordifferences/)).
Superseded; skip it.

**ΔE2000 — the standard.**
```
ΔE00 = sqrt( (ΔL'/k_L S_L)² + (ΔC'/k_C S_C)² + (ΔH'/k_H S_H)²
             + R_T · (ΔC'/k_C S_C) · (ΔH'/k_H S_H) )

S_L = 1 + 0.015(L̄'-50)² / sqrt(20 + (L̄'-50)²)
S_C = 1 + 0.045 C̄'
S_H = 1 + 0.015 C̄' T
T   = 1 - 0.17cos(h̄'-30°) + 0.24cos(2h̄') + 0.32cos(3h̄'+6°) - 0.20cos(4h̄'-63°)
R_T = -2 · sqrt(C̄'⁷/(C̄'⁷+25⁷)) · sin(60°·exp(-((h̄'-275°)/25°)²))
a'  = a*(1 + G),  G = 0.5(1 - sqrt(C̄*ab⁷/(C̄*ab⁷+25⁷)))
```
([Wikipedia: Color difference](https://en.wikipedia.org/wiki/Color_difference)).

Because `S_L`, `S_C`, `S_H`, `T` and `R_T` all use *arithmetic means* (L̄', C̄', h̄')
rather than the reference sample, **ΔE2000 is symmetric** — unlike ΔE94 and CMC, which
Colors.jl explicitly documents as violating symmetry with the first argument as reference
([Colors.jl](http://juliagraphics.github.io/Colors.jl/stable/colordifferences/)).
*This corrects a premise in the task brief: asymmetry is not a ΔE2000 problem.*

**Authoritative implementation:** Sharma, Wu & Dalal, "The CIEDE2000 Color-Difference
Formula: Implementation Notes, Supplementary Test Data, and Mathematical Observations,"
*Color Res. Appl.* 30(1):21–30 (2005) —
[paper PDF](https://hajim.rochester.edu/ece/sites/gsharma/papers/CIEDE2000CRNAFeb05.pdf),
[code + 34-pair test vectors](https://www.hajim.rochester.edu/ece/~gsharma/ciede2000/).
**Use those test vectors in a unit test.** The site's test file format: columns 1–3 =
reference L\*a\*b\*, 4–6 = sample L\*a\*b\*, column 7 = expected ΔE00.

A verified, compact C reference implementation you can port almost line-for-line to C#
(claims agreement with both Lindbloom and Sharma to 10–12 decimal places; test vector
`(28.9, 47.5, 2.0)` vs `(28.8, 41.6, -1.7)` → **ΔE00 = 2.7749016764**):
[michel-leonard/ciede2000-color-matching](https://github.com/michel-leonard/ciede2000-color-matching)
— [C source](https://raw.githubusercontent.com/michel-leonard/ciede2000-color-matching/main/ciede-2000.c).

Implementation pitfalls to watch (all from the Sharma notes / that source):
- Use `atan2(b, a·(1+G))`, and add 2π when negative — hue must be in [0, 2π).
- Mean hue `h̄'` is ambiguous by 180°; add π to both `h̄'` and `h_d` when
  `|h₂'-h₁'| > π`. Guard the exact-180° case with an epsilon.
- The chroma-dependent `G` factor scales **a\* only**, not b\*.
- `25⁷ = 6103515625`.
- `ΔH'` is `2·sqrt(C₁'C₂')·sin(Δh'/2)` — not a raw angle difference.

**CAM16-UCS / Jzazbz Euclidean.** Best-in-class uniformity (§1.5) but require viewing
conditions you don't have. `colour-science` offers `delta_E` methods CIE 1976/1994/2000,
CMC, DIN99, ITP, CAM02-UCS, CAM16-LCD, HyAB, HyCH
([colour.delta_E](https://colour.readthedocs.io/en/develop/generated/colour.delta_E.html)).

### 2.2 The decisive problem: your differences are too big for ΔE2000

This is the finding that should change the design.

- **CIEDE2000's recommended range of use is 0–5 CIELAB units.** It was developed for
  small colour differences and does not perform well for large ones.
- **The formula is not mathematically continuous.** There are three independent sources
  of discontinuity, the main one being the inherent 180° ambiguity in averaging two hue
  angles. **For pairs under 5 ΔE apart the discontinuity magnitude is bounded by 0.274;
  beyond that it "rises sharply."**
- Consequence: the discontinuities "preclude the use of the formula in Taylor series
  approximations and gradient-based optimization techniques."

Sources: Sharma, Wu & Dalal 2005 (above); Sharma & Bala,
["Mathematical Discontinuities in CIEDE2000 Color Difference Computations," CIC12 (2004)](https://hajim.rochester.edu/ece/sites/gsharma/papers/cic04ciede2000.pdf).

Now compare that to reality in this app. Golden's Phthalo Blue (Green Shade) masstone
measures L\*25.61 a\*6.98 b\*-18.05; sRGB's blue corner is L\*32.30 a\*79.19 b\*-107.86
*(computed)*. A photo pixel of saturated blue sky or a bright cyan will sit **tens of
ΔE units** from anything the palette can mix. Ranking candidate mixtures at ΔE = 25 with
a formula validated below 5 and known to be discontinuous above 5 is using the tool
outside its envelope.

### 2.3 The large-difference metric: HyAB

Abasi, Tehran & Fairchild studied differences **"on the order of 10 CIELAB units or
larger,"** aiming to describe the salience of colour differences between distinct objects
in real scenes — which is precisely the paint-matching regime. Their result, **HyAB**, is
a hybrid: **city-block in lightness, Euclidean in (a\*, b\*)**:

```
ΔE_HyAB = |ΔL*| + sqrt(Δa*² + Δb*²)
```

Rationale: the **psychological separability** of lightness from the chromatic dimensions.
In their experiment (17 observers) HyAB was **more faithful to large observed colour
differences than either Euclidean CIELAB or CIEDE2000**.

Sources: Abasi, Tehran & Fairchild, "Distance metrics for very large color differences,"
*Color Res. Appl.* 45(2):208–223 (2020),
[doi:10.1002/col.22451](https://onlinelibrary.wiley.com/doi/10.1002/col.22451) (paywalled) ·
[colour.difference.delta_E_HyAB](https://colour.readthedocs.io/en/develop/generated/colour.difference.delta_E_HyAB.html)
("intended for large colour differences, on the order of 10 CIE L\*a\*b\* units or greater") ·
practical writeup and caveats: [Väänänen, "HyAB k-means for color quantization," 30fps.net](https://30fps.net/pages/hyab-kmeans/)
(page returns 403 to this fetcher; content confirmed via search excerpts and the
[HN thread](https://news.ycombinator.com/item?id=44514946)).
The same authors also found HyAB more suitable than CIELAB and CIEDE2000 for image edge
detection gradients ([*Color Res. Appl.* 2020, doi:10.1002/col.22494](https://onlinelibrary.wiley.com/doi/10.1002/col.22494)).

**Honest caveat from that writeup:** the author does *not* recommend HyAB as a drop-in
replacement for CIELAB or carefully-weighted sRGB before the rest of the system is tuned.
Treat it as a well-motivated refinement, not a silver bullet.

### 2.4 Practical JND thresholds for the UI

Use real measured thresholds, not folklore. The best-cited controlled study
(Paravina et al., *J. Esthet. Restor. Dent.* 27(S1):S1–S9, 2015 — prospective
multicentre, 50:50% thresholds):

| | Perceptibility (PT) | Acceptability (AT) |
|---|---|---|
| **ΔE\*ab (ΔE76)** | **1.2** | **2.7** |
| **ΔE00 (ΔE2000)** | **0.8** | **1.8** |

[doi:10.1111/jerd.12149](https://onlinelibrary.wiley.com/doi/abs/10.1111/jerd.12149)
(abstract confirmed via search; full text paywalled).

Note the implied **ΔE76 ≈ 1.5 × ΔE2000** ratio at threshold — useful for translating any
existing ΔE76 tuning constants in the codebase.

Wider industry bands, for the verbal scale:

| ΔE2000 | Verbal | Source |
|---|---|---|
| < 1 | Imperceptible | [facadecolorizer paint-app test](https://facadecolorizer.com/us/blog/best-app-match-paint-color-from-photo-2026) |
| 1–2 | Only a trained eye, side by side | ibid. |
| 2–5 | Visible to a casual observer | ibid. |
| > 5 | Obviously different | ibid. |
| 2.0 | "pretty darn good" for print | [John the Math Guy](http://johnthemathguy.blogspot.com/2017/07/is-10-delta-e-just-noticeable-difference.html) |
| 6.0 | "pleasing" for print | ibid. |

**Two warnings.** (a) CIE 142-2001, which defines ΔE00, never uses the phrase "just
noticeable difference" — JND (from MacAdam 1942) and ΔE come from different lineages and
are not interchangeable (John the Math Guy, above). (b) Wikipedia's "ΔE\*ab ≈ 2.3 = JND"
figure is a *median over colours*; some ΔE76 > 2.3 pairs are invisible and some below it
are obvious ([Wikipedia](https://en.wikipedia.org/wiki/Color_difference)). Don't present
any single number as a hard perceptual line.

### 2.5 Using a non-metric distance for nearest-neighbour search

The concern is real but the practical impact here is near zero.

- **ΔE2000 is symmetric** (§2.1), so `d(a,b) = d(b,a)` holds. The problems are
  **non-continuity** and **failure of the triangle inequality** — Euclidean distances in
  CIELAB are not isometric with respect to CIEDE2000
  ([Investigating Euclidean Mappings for CIEDE2000, CIC18](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/18/1/art00058)).
- Consequence for indexing: metric-tree structures (kd-tree, ball tree, VP-tree) prune
  using a triangle-inequality lower bound and are **not valid** under a non-metric
  distance ([BrePartition, arXiv:2006.00227](https://arxiv.org/pdf/2006.00227);
  [scikit-learn neighbors](https://scikit-learn.org/stable/modules/neighbors.html)).
- Consequence for k-means: **Lloyd's algorithm is tied to Euclidean distance and
  mean-based centroid updates.** With an arbitrary distance you need k-medoids, which
  picks actual data points as centres
  ([Lloyd's algorithm](https://en.wikipedia.org/wiki/Lloyd%27s_algorithm);
  [PSU STAT 555 §10.4](https://online.stat.psu.edu/stat555/node/88/)). This matters for
  §5, not for the matcher.

**Resolution — filter-and-refine.** Don't index with ΔE2000; index with a cheap metric
that *is* Euclidean and use ΔE2000/HyAB only to rescore.

1. Build a kd-tree (or just a flat array — see below) over candidate mixtures in
   **L\*a\*b\*** using plain Euclidean ΔE76.
2. Retrieve the top-*k* (k ≈ 32–64) by ΔE76.
3. Rescore those with the real metric; return the best.

The safety of step 2 rests on a bounded ratio between ΔE76 and the final metric. For
HyAB the bound is exact and trivial: by the ℓ1/ℓ2 relationship,
`ΔE76 ≤ ΔE_HyAB ≤ sqrt(2)·ΔE76`, so a `sqrt(2)`-inflated ΔE76 radius is a **provably
safe** candidate filter. **That alone is a strong reason to prefer HyAB over ΔE2000 as
the search metric** — you get correctness *and* an exact acceleration bound. No
equivalent clean bound exists for ΔE2000; its `S_C`/`S_H` denominators reach roughly
`1 + 0.045·C̄'` ≈ 6 at C̄' ≈ 110, so a conservative bound is loose enough to be useless.

**Reality check on effort:** the current brute-force scheme over N paints is
`N + 7·C(N,2) + 4·C(N,3)` candidates. For N = 20 that's ~20 + 1330 + 4560 ≈ 5,900; for
N = 30, ~30 + 3045 + 16,240 ≈ 19,300. Even at 19k candidates × a few dozen flops,
per-pixel search on a 12 MP photo is the bottleneck, not the metric — which is another
argument for §5's approach: **quantize/segment first, then match a few hundred region
colours, not 12 million pixels.** Precompute the candidate table once, then the matcher
runs ~200–2000 times per image and ΔE2000 vs HyAB cost is irrelevant.

---

## 3. Gamut mapping into the paint gamut

### 3.1 Measure the real gamut first — you already have the data

Golden publishes measured **CIELAB** values for every Heavy Body colour alongside
lightfastness, opacity, tinting strength and viscosity. Extracted values
([goldenartistcolors.com Heavy Body pigment data](https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data)),
with C\*ab and h_ab *(computed)*:

| Paint | L\* | a\* | b\* | C\*ab | h_ab |
|---|---|---|---|---|---|
| Titanium White | 98.25 | -0.74 | 1.24 | 1.44 | 121° |
| Zinc White | 95.94 | -0.85 | 0.93 | 1.26 | 132° |
| Carbon Black | 25.37 | 0.10 | -0.14 | 0.17 | — |
| Mars Black | 25.47 | 0.81 | 0.42 | 0.91 | 27° |
| Bone Black | 23.82 | -0.05 | -0.45 | 0.45 | — |
| Phthalo Blue (GS) | 25.61 | 6.98 | -18.05 | 19.35 | 291° |
| Ultramarine Blue | 24.11 | 14.01 | -27.81 | 31.14 | 297° |
| Phthalo Green (YS) | 28.94 | -15.01 | -0.27 | 15.01 | 181° |
| Quinacridone Magenta | 31.07 | 25.99 | 5.11 | 26.49 | 11° |
| Dioxazine Purple | 25.04 | 2.62 | 1.33 | 2.94 | 27° |
| Burnt Umber | 27.33 | 4.10 | 4.19 | 5.86 | 46° |
| Pyrrole Red | 43.54 | 54.93 | 33.22 | 64.19 | 31° |
| Naphthol Red Light | 47.65 | 52.34 | 37.15 | 64.18 | 35° |
| Yellow Ochre | 57.84 | 16.76 | 39.65 | 43.05 | 67° |
| Cad Yellow Medium | 84.13 | 12.86 | 94.59 | 95.46 | 82° |
| Hansa Yellow Opaque | 84.48 | 10.79 | 91.83 | 92.46 | 83° |

Compare sRGB's own corners *(computed, D65, 2° observer)*:

| sRGB | L\* | a\* | b\* | C\*ab | h_ab |
|---|---|---|---|---|---|
| white | 100.00 | 0.00 | 0.00 | 0 | — |
| black | 0.00 | 0.00 | 0.00 | 0 | — |
| red | 53.24 | 80.09 | 67.20 | 104.55 | 40° |
| green | 87.73 | -86.18 | 83.18 | 119.78 | 136° |
| blue | 32.30 | 79.19 | -107.86 | 133.81 | 306° |
| cyan | 91.11 | -48.09 | -14.13 | 50.12 | 196° |
| magenta | 60.32 | 98.23 | -60.82 | 115.54 | 328° |
| yellow | 97.14 | -21.55 | 94.48 | 96.91 | 103° |

**Read the table carefully.** The masstone C\* values for the blues and greens look tiny
(19, 31, 15) *because those masstones are nearly black* — maximum chroma for those hues
is reached in a **tint with white**, not at masstone. Golden's published data covers
masstone only, so **do not compute your gamut hull from this table alone** — sample it
from your own mixer. Berns' spectral dataset covered 68 Golden Heavy Body paints, both
masstone and a 10% tint with white, measured on an X-Rite MS7000 integrating-sphere
spectrophotometer, specular included, 380–730 nm
([Berns, "Artist Acrylic Paint Spectral, Colorimetric, and Image Dataset," *Archiving 2022*](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/archiving/19/1/10) ·
[PDF mirror](https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf)).

### 3.2 Two corrections to the task brief's premises

**(a) Titanium white is L\*98.25, not L\*95.** So the white end costs you ~2 L\* units —
negligible. Titanium dioxide reflects ~97% of incident light
([Jackson's Art, Titanium White pigment story](https://www.jacksonsart.com/colour/pigments-powders/pigment-stories/white/titanium-white)),
and Munsell N9.5/ has LRV 90.29 ([MyPerfectColor](https://www.myperfectcolor.com/paint/289506-munsell-n-9.5-neutral-value-scale)).

**(b) Black is L\*≈24–25, not L\*10–15 — worse than assumed.** Golden Bone Black L\*23.82
→ Y = 4.05%; Carbon Black L\*25.37 → Y = 4.54% *(computed)*. That is a realistic reflectance
for an artists' black film. **So the black end costs you ~24 L\* units.** This is the
single largest source of error in the whole pipeline and it is entirely in the value
dimension.

**Resulting paint dynamic range *(computed)*:**
- L\* span: **74.4 of 100** (23.82 → 98.25)
- Contrast ratio: **23.6 : 1** (Y 4.05% → 95.54%)

For scale: matte print is ~30:1, glossy print ~100:1, a real LCD ~1000:1. **A painting is
a lower-contrast medium than a magazine page.** That is the whole story of §4.

**(c) Paint is not a subset of sRGB.** 22% of the measured acrylic colours fall
**outside AdobeRGB (1998)**, and the dataset needed ProPhotoRGB encoding to represent it
(Berns, above). Concretely, Golden Cad Yellow Medium is L\*84.1 C\*95.5 **h82°** while
sRGB's yellow corner is L\*97.1 C\*96.9 **h103°** — a 21° hue difference and 13 L\* lower.
sRGB yellow is out of paint gamut; Cad Yellow's hue is out of the *bright* part of sRGB.
Design for a two-way overlap, not containment.

### 3.3 Strategies — ranked

**Don't clip.** Per-channel clipping "favours saturation over perceptual accuracy" and
"lightness of the colour is not well preserved"
([ColorAide gamut mapping docs](https://facelessuser.github.io/coloraide/gamut/)).

**Lightness- and hue-preserving chroma compression** is the established general-purpose
approach: shift lightness minimally, compress chroma, hold perceived hue. CIE 156:2004
codifies evaluation of gamut-mapping algorithms and names **HPMINDE** (hue-preserving
minimum-ΔE) and **SGCK** as the reference algorithms
([CIE 156:2004](https://www.cie.co.at/publications/guidelines-evaluation-gamut-mapping-algorithms);
[public sample PDF](https://www.normsplash.com/Samples/CIE/162985436/CIE-156-2004-en.pdf);
[Springer summary](https://link.springer.com/rwe/10.1007/978-3-030-89862-5_3)).
Non-linear methods that "preserve hue completely, preserve lightness for all but the
more saturated colours, and perform most of the mapping by changing chroma" are standard
industrial practice
([US6775028](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/6775028)).

**Beware metric-hue preservation in blue.** Braun & Fairchild: "perceived hue" ≠ "metric
hue angle," and holding CIELAB hue angle constant in the blue region **causes a
perceived** hue shift. Their fix is a hue-linearised CIELAB built from the Hung & Berns
(1995) and Ebner & Fairchild (1998) data
([Braun & Fairchild, "Color gamut mapping in a hue-linearized CIELAB color space," RIT](https://repository.rit.edu/other/358/)).
Cheaper pragmatic alternative: do the chroma reduction in **OkLCh**, which is more
hue-linear than CIELCh by construction.

**A concrete, implementable algorithm — the CSS Color 4 one.** Constant-lightness,
constant-hue chroma reduction by **binary search in OkLCh**, where at each step you
compare the current estimate against its channel-clipped version and stop when the
difference falls below a JND. CSS uses `deltaEOK` with a **JND of 0.02**; short-circuit
to black when `L ≤ 0` and to white when `L ≥ 1`
([CSS Color 4 §13.2](https://www.w3.org/TR/css-color-4/) ·
[Color.js gamut mapping docs](https://colorjs.io/docs/gamut-mapping) ·
[csswg-drafts #7653](https://github.com/w3c/csswg-drafts/issues/7653)).
This maps cleanly onto your problem: replace "is it inside sRGB?" with "is it inside the
paint hull?", and replace "clip channels" with "nearest achievable mixture."

**Ottosson's adaptive-L₀ projection** is the best-argued practical variant. Rather than
projecting to constant L or to a fixed grey, project toward an adaptively chosen L₀:
```
L₀ = (1 + sgn(L_a)·(e₁ - sqrt(e₁² - 2|L_a|))) / 2
```
with α controlling how strongly lightness is preserved. His recommendation: **α = 0.05**,
and the hue-independent `L₀ = 0.5` form is preferable to the hue-dependent `L₀ = L_cusp`
form because the visual difference is negligible and the cost is lower
([Ottosson, "sRGB gamut clipping"](https://bottosson.github.io/posts/gamutclipping/)).
Pure constant-lightness chroma compression over-desaturates very dark and very bright
colours; adaptive L₀ fixes exactly that — which is your titanium-white and carbon-black
problem.

**ICC intents, mapped to this app.** Perceptual "compresses the whole colour space";
relative colorimetric "clips out-of-gamut colours" and rescales to the destination white
([FESPA on rendering intents](https://www.fespa.com/en/news-media/icc-colour-management-the-top-4-rendering-intents/)).
A relative-colorimetric conversion without correction "will plot all source dark tones
into the new black region, leaving the destination's blackest blacks empty — a flat,
plugged-up shadow" ([ONYX white paper](https://www.lfpc.es/wp-content/uploads/2013/05/Rendering_Intents_and_Black_Point_Compensation.pdf)).
**That sentence is a precise description of what the current code does to every shadow
in the photo.** The fix is **black point compensation**: rescale so source black maps to
destination black and source white to destination white, with intermediate levels mapped
smoothly ([ICC White Paper 40](https://archive.color.org/files/WP40-Black_Point_Compensation_2010-07-27.pdf)
— PDF did not parse in this environment, so the exact XYZ scaling formula is
**unverified**; the concept and its applicability to perceptual/relative-colorimetric/
saturation intents is confirmed). Default in image-editing software is relative
colorimetric **with** BPC ([MYIRO](https://www.myiro.com/blog/bpc)).

**Verdict for PaintTranslator:** you want a **perceptual-intent-like** pipeline, because
a painting is a pictorial reproduction, not a spot-colour proof:
1. Sigmoidal L\* rescaling into [L\*_black, L\*_white] of the palette (§4).
2. Per-hue chroma compression toward the achievable hull, hue held in OkLCh.
3. Nearest-achievable-mixture search on the result, with the weighted metric of §4.3.
Offer relative-colorimetric-without-compression as an "accurate colours, crushed
shadows" alternative so the user can choose.

---

## 4. Value / tone compression

### 4.1 The artistic argument

Painters are unambiguous that value structure carries the image and hue is comparatively
negotiable:

- "You can shift every hue in a painting toward purple and it will still read correctly
  if the value relationships hold. Mess up the values, though, and no amount of beautiful
  colour will save it." ([Draw Paint Academy, "What Is Value in Art?"](https://drawpaintacademy.com/what-is-value-in-art/))
- Value is more important than colour to the design and success of a painting; in Monet,
  the values are true and set the structure while colour carries emotional rather than
  structural weight ([Eric Merrell, "Is Value More Important Than Color?"](https://www.ericmerrell.com/news/swpl5ddh4nkzhr3dw2el2epwx4cea9))
- **Notan** is the radical form of the value study: does the fundamental light–dark
  structure work? ([Fine Art Tutorials, "What is Value in Art?"](https://finearttutorials.com/guide/value-in-art/) ·
  [OutdoorPainter, "Understanding Value"](https://www.outdoorpainter.com/painting-basics-understanding-value/))
- Painters have a name for the mechanism they use when the range won't fit — they
  compress the range while preserving *relationships*. That is exactly a tone curve.

### 4.2 The perceptual/published argument

Three independent lines of support:

**(a) Separate visual channels.** Lightness and darkness are transmitted by separate
parallel channels maintained from retina to cortex
([Cui, "Gamut Mapping with Enhanced Chromaticness," CIC9](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/9/1/art00047)).
HyAB's city-block treatment of L\* rests on the same **psychological separability**
of lightness from hue/chroma (§2.3).

**(b) Measured per-attribute thresholds.** Observers are *more sensitive* to lightness
than to chroma. 50:50% CIEDE2000 thresholds, k_L = 1:

| | ΔL' | ΔC' | ΔH' | ΔE' |
|---|---|---|---|---|
| Perceptibility PT00 | **1.04** | 1.58 | (not computable) | 1.01 |
| Acceptability AT00 | 2.82 | 3.04 | (not computable) | 2.66 |

([Tejada-Casado et al., "Exploring the CIEDE2000 thresholds for lightness, chroma, and
hue differences in dentistry," *J. Dent.* 2024](https://www.sciencedirect.com/science/article/pii/S0300571224004962),
[repository copy](https://digibug.ugr.es/handle/10481/109570) — fetch returned 403,
values confirmed via search excerpts, so treat the exact digits as **needing a
second check**.) An earlier study gives acceptability ΔL' 2.92, ΔC' 2.52, ΔH' 1.90
([Pérez et al., *J. Dent.* 2011, PubMed 21986320](https://pubmed.ncbi.nlm.nih.gov/21986320/)).

The perceptibility ratio **ΔC'/ΔL' = 1.58/1.04 ≈ 1.52** is the number to build on:
a lightness error is noticed at roughly 2/3 the magnitude of an equally-noticeable
chroma error.

**(c) Gamut mapping already does this.** Every general-purpose GMA sacrifices chroma to
protect lightness (§3.3), and L\* compression improves preference ratings
([US6775028](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/6775028);
[Gamut Mapping to Preserve Spatial Luminance Variations, CIC8](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/8/1/art00023)).

**One caution against over-claiming.** In *acceptability* judgements the convention runs
the other way: CIE94 textiles and CIEDE2000(2:1:1) use **k_L = 2**, which *halves* the
lightness term — i.e. industrial QC *tolerates* lightness error more. CIEDE2000(2:1:1)
outperformed (1:1:1) for acceptability (AT 1.78 vs 1.87)
([Pérez et al. 2011](https://pubmed.ncbi.nlm.nih.gov/21986320/);
[Springer, "CIE94, History, Use, and Performance"](https://link.springer.com/rwe/10.1007/978-3-642-27851-8_13-1);
[Datacolor, Color Differences & Tolerances](https://www.datacolor.com/wp-content/uploads/2022/03/color_differencestolerances.pdf)).
So the honest framing is: **weight L\* higher for "does this read like the photo"
(perceptibility, image structure), not for "is this batch within tolerance."**
The app is doing the former.

### 4.3 Concrete recommendation

**Step 1 — sigmoidal L\* rescaling (do this before matching).** Braun & Fairchild found
that image-dependent sigmoidal lightness remapping produced **superior matches to linear
lightness scaling for every image tested**, and that "vast improvements were obtained
when linear lightness and chroma rescaling functions are replaced with contrast-preserving
[functions]." Their gamut-mapping pipeline is: sigmoidal lightness remap → knee or
"sigmoid-like" chroma compression.

Sources: [Braun & Fairchild, "Gamut Mapping for Pictorial Images," TAGA 1999](https://repository.rit.edu/other/367/) ·
[Braun & Fairchild, "Image lightness rescaling using sigmoidal contrast enhancement
functions," *J. Electronic Imaging* / SPIE 3648:96 (1999)](https://www.spiedigitallibrary.org/conference-proceedings-of-spie/3648/0000/Image-lightness-rescaling-using-sigmoidal-contrast-enhancement-functions/10.1117/12.334548.short) ·
[Braun & Fairchild, "General-Purpose Gamut-Mapping Algorithms," CIC7](https://library.imaging.org/cic/articles/7/1/art00031).

The function is a **normalised discrete cumulative normal**, parameterised by mean `x₀`
and standard deviation `s`: `x₀` shifts the straight-line portion (above L\*=50 → toward
highlights, below → toward shadows) and `s` sets the slope/contrast. `x₀` imparts
lightness change, `s` imparts contrast change. Working on the L\* scale is preferred
because it is perceptually more uniform and natural-image histograms are more symmetric
on it; sigmoidal curves are near-linear through the midtones so most pixels map
approximately linearly, preserving histogram shape, with saturation at both ends.

**I could not retrieve the exact published equation** — `markfairchild.org` serves an
expired TLS certificate, `digitalarchive.rit.edu` no longer resolves, and SPIE/Wiley are
paywalled. The parameterisation above is confirmed from multiple secondary descriptions.
**Flagged as unverified; get the PDF before citing an equation.** A practical stand-in
that matches the described behaviour:

```
// map photo L* in [0,100] -> paint L* in [Lmin, Lmax], sigmoid, contrast-preserving
double S(double x, double x0, double s)         // normalised cumulative normal
    => 0.5 * (1 + Erf((x - x0) / (s * Math.Sqrt(2))));

double MapL(double Lin, double Lmin, double Lmax, double x0 = 50, double s = 40)
{
    double t  = (S(Lin, x0, s) - S(0, x0, s)) / (S(100, x0, s) - S(0, x0, s));
    return Lmin + t * (Lmax - Lmin);
}
```
Make `x₀` image-dependent (mean or median L\* of the photo) per Braun & Fairchild's
"image-dependent" finding, and expose `s` as a "contrast" slider. Compare against the
linear baseline, which for the Golden Bone-Black/Ti-White range is
`L*_out = 0.7443·L*_in + 23.82` *(computed)*:

| Photo L\* | Linear → | Note |
|---|---|---|
| 0 | 23.82 | deepest shadow available |
| 10 | 31.26 | |
| 25 | 42.43 | |
| 50 | 61.04 | midtones pushed 11 units lighter — the linear method's flaw |
| 75 | 79.64 | |
| 100 | 98.25 | |

Linear compression lightens the whole image and flattens it. The sigmoid keeps the
midtone slope near 1 and spends the compression at the ends, which is what painters do
by eye.

**Step 2 — weighted metric for the search.** Given the ΔC'/ΔL' ≈ 1.52 perceptibility
ratio, a defensible weighted CIELAB metric is:

```
ΔE_weighted² = (w_L · ΔL*)² + Δa*² + Δb*²        with  w_L ≈ 1.5
```
or, in the HyAB form (recommended, since the residuals are large):
```
ΔE_wHyAB = w_L · |ΔL*| + sqrt(Δa*² + Δb*²)      with  w_L ≈ 1.5
```
`w_L = 1.5` is justified by the measured PT00 ratio 1.58/1.04, *not* invented. Note the
ℓ1/ℓ2 candidate-filter bound of §2.5 still holds with `w_L` folded into the L\* axis.

Expose `w_L` as a single "prioritise value over colour" slider, default 1.5, range
1.0–2.5. At the extreme it degenerates to a notan/value-map mode, which is a genuinely
useful feature in its own right.

---

## 5. Quantization / posterization to N paint mixtures

### 5.1 Colour space: LAB, not RGB — but know why

Distances in CIELAB are more aligned with human colour perception than RGB, and
quantization error should be measured where it's perceived
([Efficient Color Quantization Using Superpixels, *Sensors* 22(16):6043 / PMC9416436](https://pmc.ncbi.nlm.nih.gov/articles/PMC9416436/)).
Also: denoising colour images is better done in CIELAB because chroma noise is more
objectionable than luma noise, so chroma can be filtered harder without visible blur
([Zhang & Gunturk, "Multiresolution Bilateral Filtering," PMC2614560](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC2614560/)).

**The caveat nobody mentions:** k-means minimises *squared Euclidean* distance and its
centroid update is the arithmetic mean — so k-means in LAB is minimising **ΔE76²**, i.e.
you get LAB's perceptual advantages but inherit ΔE76's chroma over-weighting (§2.1). If
you want HyAB or ΔE2000 you must move to **k-medoids**, because Lloyd's algorithm is
tied to Euclidean distance and mean-based updates (§2.5). Practical middle ground: run
k-means in LAB with a **rescaled L\* axis** (multiply L\* by `w_L` before clustering,
divide after) — that keeps Lloyd valid (it's still Euclidean in the scaled space) while
encoding the value-priority of §4.3. This is a genuinely cheap win.

### 5.2 Algorithm comparison

| Method | Character | Notes |
|---|---|---|
| **Median cut** | splitting; fast | palette colours can differ substantially from the image's actual colours ([PMC9416436](https://pmc.ncbi.nlm.nih.gov/articles/PMC9416436/)) |
| **Octree** | splitting; fast, streaming | two-pass adaptive octree "gives the best results, typically a little better than median cut" ([Leptonica colour quantization](http://www.leptonica.org/color-quantization.html)) |
| **Wu (1992)** | greedy orthogonal bipartition, **variance**-minimising, + a dynamic-programming variant along the principal axis | designed to beat population-based and axis-restricted cuts; **greatest gains for small palettes** — exactly your case ([Wu, *Graphics Gems II* / "Statistical Colour Quantization for Minimum Distortion"](https://link.springer.com/chapter/10.1007/978-3-642-77586-4_12); [original C](https://www.ece.mcmaster.ca/~xwu/cq.c)) |
| **k-means / FCM** | clustering; slower, best distortion | Lloyd; needs good seeding |
| **Superpixel + any of the above** | two-stage | **2–4× (median cut), up to 15× (k-means), up to 30× (FCM)** speedup on the paper's 5 test images; **340× at medium and 623× at high resolution (1920×1281) for SPFCM**, with negligible quality loss ([PMC9416436](https://pmc.ncbi.nlm.nih.gov/articles/PMC9416436/)) |

**A .NET-ready Wu implementation exists:** [JeremyAnsel.ColorQuant](https://github.com/JeremyAnsel/JeremyAnsel.ColorQuant)
(Xiaolin Wu's quantizer, 32-bit ARGB → 8-bit palettised). It works in RGB, so it's most
useful as a **seeder** for LAB k-means rather than a final answer.

**Recommended:** Wu (or k-means++) to seed → k-means in **L\*-scaled LAB** on
**superpixel means**, not pixels. This is both faster and produces palettes matched to
image structure.

### 5.3 Why dithering is wrong here — with the honest counterargument

**How error diffusion works:** for each pixel, pick the nearest palette colour, then
distribute the residual to unprocessed neighbours. Floyd–Steinberg (1976) uses
**7/16 right, 3/16 below-left, 5/16 below, 1/16 below-right**
([Wikipedia](https://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering);
[Wisconsin CS559 notes](https://research.cs.wisc.edu/graphics/Courses/559-s2004/docs/floyd-steinberg.pdf)).
It produces "organic, noisy-looking but very faithful tonal reproduction"
([ASCII Magic, Complete Guide to Dithering](https://www.ascii-magic.com/blog/complete-guide-to-dithering)).

**The counterargument you should know about, because it's real:** optical mixing of
juxtaposed marks *is* an established painting technique. Seurat's Divisionism /
Chromoluminarism deliberately places small dots of unmixed colour so they blend in the
viewer's eye, and this produces *more* luminous results than subtractive palette mixing,
which "inevitably creates duller colors"
([Wikipedia: Divisionism](https://en.wikipedia.org/wiki/Divisionism);
[Beyond Every Art, "How Pointillism Exploits Optical Mixing vs Pigment Mixing"](https://www.beyondeveryart.com/pointillism-optical-mixing-vs-pigment-mixing/)).
So "you can't dither paint" is false in principle — pointillism *is* dithering, and it
partly solves the chroma-gamut problem of §3, since partitive mixing dodges the
saturation loss of subtractive mixing.

**But it's still wrong for this app's output**, for three concrete reasons:
1. Error diffusion produces per-pixel noise with no contiguous regions. A paint-by-numbers
   or region-guide output needs closed areas large enough to hold a brush and a number;
   posterization creates "distinct, flat colour zones — making posterization more suitable
   for traditional paint-by-numbers" ([ASCII Magic](https://www.ascii-magic.com/blog/complete-guide-to-dithering)).
2. Dither cells must be small relative to viewing distance to fuse. That constraint is
   set by the *viewer*, not the algorithm, and a hand-painter cannot hit sub-mm dot
   placement reliably.
3. Wet acrylic that touches wet acrylic mixes subtractively anyway, destroying the
   partitive effect the dither depended on.

**Offer a "Divisionist / optical mixing" mode later if you want** — a coarse, deliberate,
large-cell ordered dither at the region level, presented as a technique, not as the
default fidelity path. Do not use error diffusion as the general quantizer.

### 5.4 What you actually want: region-based segmentation

**Pipeline (this is the concrete recommendation):**

1. **Edge-preserving prefilter.** Bilateral filter, controlled by spatial σ_s and range
   σ_r: `g(x) = exp(-‖x‖²/2σ_s²)` spatially and `exp(-t²/2σ_r²)` in range. Weights
   across an edge become small, so noise averages out while edges keep their structure
   and location. Both σ near zero → no-op; both too large → edges lost
   ([Brown CS129 bilateral lab](https://cs.brown.edu/courses/cs129/2020_Fall/labs/lab_bilateral/index.html);
   [OpenCV image filtering](https://docs.opencv.org/4.x/d4/d86/group__imgproc__filter.html)).
   Alternative: **mean-shift filtering**, whose output is explicitly a "posterized" image
   with gradients and fine texture flattened
   ([IPOL, "An Implementation of the Mean Shift Algorithm"](https://www.ipol.im/pub/art/2019/255/article_lr.pdf)).
2. **SLIC superpixels.** k-means in a joint colour+position space with a weighted
   distance; the only required parameter is `k` (or step size), plus `compactness` —
   larger compactness → spatially tighter but spectrally more heterogeneous superpixels
   ([Achanta et al., SLIC](https://www.researchgate.net/publication/225069465_SLIC_Superpixels_Compared_to_State-of-the-Art_Superpixel_Methods);
   [GRASS i.superpixels.slic manual](https://grass.osgeo.org/grass-stable/manuals/addons/i.superpixels.slic.html)).
   **SLIC does not enforce connectivity**; orphaned pixels must be reassigned to the
   nearest cluster centre via connected components as a post-step (ibid.).
   For sizing, the superpixel-quantization paper used `N_SP = k + SP_Ratio·k` with
   `SP_Ratio ∈ {2,4,8,16}` — for `k=32` that's 96/160/288/544 superpixels
   ([PMC9416436](https://pmc.ncbi.nlm.nih.gov/articles/PMC9416436/)).
3. **Region Adjacency Graph merge.** Build a RAG over superpixels and merge neighbours
   whose colour difference is below threshold. This is the documented approach in a
   working paint-by-numbers generator
   ([LukaZdr/paint_by_numbers_image_generator](https://github.com/LukaZdr/paint_by_numbers_image_generator)).
   Formal treatments: [Region merging via graph cuts](https://www.ias-iss.org/ojs/IAS/article/view/828) ·
   [Iterated region merging with localized graph cuts](https://www.sciencedirect.com/science/article/abs/pii/S0031320311001282) ·
   [A Formalization of Image Vectorization by Region Merging, *SIAM J. Imaging Sci.*](https://doi.org/10.1137/24M1696469).
4. **Quantize the region means** to N paint mixtures (§5.1–5.2), not the pixels.
5. **Cleanup.** This is where output quality is won or lost. The most mature open
   implementation, [drake7707/paintbynumbersgenerator](https://github.com/drake7707/paintbynumbersgenerator),
   exposes exactly the knobs you need — worth mirroring:
   - `kMeansNrOfClusters`, `kMeansMinDeltaDifference` (convergence, default 1),
     `kMeansClusteringColorSpace`, `kMeansColorRestrictions`
     (**restrict the palette to available paints — directly analogous to your matcher**)
   - `removeFacetsSmallerThanNrOfPoints` — min-area threshold
   - `removeFacetsFromLargeToSmall` — processing order "to prevent boundaries from
     warping"; slower but better
   - `maximumNumberOfFacets` — hard cap on region count
   - `narrowPixelStripCleanupRuns` — iteratively removes thin strips
   - `nrOfTimesToHalveBorderSegments` — Haar-wavelet border point reduction for
     smoothing, then quadratic curves
   Also: eCognition's SLIC merges components below a "minimum element size" expressed as
   a percentage of average superpixel size, **default 25%**
   ([Trimble eCognition, Superpixel Segmentation](https://docs.ecognition.com/eCognition_documentation/Reference%20Book/02%20Algorithms%20and%20Processes/2%20Segmentation%20Algorithms/Superpixel%20Segmentation.htm)).
   Vectorizer.io merges polygons below a minimum area into the largest neighbour
   ([vectorizer.io paint-by-numbers FAQ](https://www.vectorizer.io/faq/paintbynumbers/)).
   Morphological closing to smooth local boundaries and remove tiny regions is standard.
6. **Label placement:** only for shapes large enough to fit the palette index
   (vectorizer.io, above).

**C#/.NET availability:**
- Bilateral filter: `Cv2.BilateralFilter` ([OpenCvSharp](https://shimat.github.io/opencvsharp_docs/html/23f4b144-8b71-47b0-590f-7a57c331f40a.htm)),
  `CvInvoke.BilateralFilter` ([Emgu CV](https://emgu.com/wiki/files/3.3.0/document/html/b56e2983-009a-a7c1-39d0-fda58e740793.htm))
- Mean shift: `Cv2.PyrMeanShiftFiltering` ([OpenCvSharp](https://shimat.github.io/opencvsharp_docs/html/13548c26-7f48-4c3b-b301-d92abf1fcc6a.htm)),
  `CvInvoke.PyrMeanShiftFiltering` ([Emgu CV](https://www.emgu.com/wiki/files/4.8.0/document/html/M_Emgu_CV_CvInvoke_PyrMeanShiftFiltering.htm))
- SLIC: **not in either wrapper's main API.** Use [SLICOSharp](https://github.com/junjiez/SLICOSharp)
  (SLICO in pure C#/.NET) or port it — SLIC is ~200 lines and has no dependencies, which
  is probably preferable to taking an OpenCV dependency in a WinForms app.
- Wu quantizer: [JeremyAnsel.ColorQuant](https://github.com/JeremyAnsel.ColorQuant)

---

## 6. Noise, JPEG artifacts, and camera colour

### 6.1 Denoise first

Bilateral or mean-shift prefilter before quantization (§5.4 step 1). Do it in CIELAB and
filter chroma harder than luma, since chroma noise is more objectionable
([PMC2614560](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC2614560/)). This has a second
benefit specific to your app: it suppresses the per-pixel colour jitter that would
otherwise cause adjacent pixels to snap to different paint mixtures, fragmenting regions.

### 6.2 JPEG chroma subsampling and blocking

4:2:0 halves chroma resolution both horizontally and vertically. **Colour bleeding is
caused primarily by chroma subsampling and quantization**, smearing colour across
boundaries — a red shirt into skin tones, foliage into pavement
([Chen et al., "Deep Wide-Activated Residual Network... 4:2:0 JPEG," *IEEE SPL* 26:79](https://ui.adsabs.harvard.edu/abs/2019ISPL...26...79C/abstract);
[Image Compressor, "A Field Guide to Compression Artifacts"](https://imagecompressor.com/blog/compression-artifacts-explained)).
Artifacts are worst at sharp edges between saturated colours and bright/white regions,
and **chroma suffers more severe distortion than luma** due to both subsampling and
coarser quantization. One reported figure: a relative error of **86% (195 Barten steps)**
from 4:2:0 subsampling alone, even without compression
([US11025927 / related](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/11025927)).

**Practical consequences for the app:**
- **Never sample a single pixel** for the colour-picker / per-pixel readout. At high-
  contrast edges the chroma is literally interpolated from neighbours 2 px away. Sample a
  small median or bilaterally-weighted patch (5×5 or larger) and say so in the UI.
- The 8×8 DCT block grid means block-boundary pixels carry ringing. A bilateral prefilter
  helps; a light 8-px-grid-aware deblock would help more but is probably not worth it.
- Prefer PNG/TIFF/HEIF or RAW-derived input if the user has it. Consider warning when
  the loaded JPEG's sampling factors indicate 4:2:0.

### 6.3 Camera colour — how much can be fixed without a chart

**The distortion is real and deliberate.** In-camera picture styles (Vivid, Landscape,
Portrait, Standard) change rendered colour, contrast and saturation in the JPEG; Vivid
boosts saturation and contrast for "punchy colour," Neutral is flatter
([Wallpics, "Decoding Camera Colour Profiles"](https://www.wallpics.com/blogs/news/decoding-camera-colour-profiles-a-complete-guide-to-vivid-natural-and-neutral-settings);
[iPhotography](https://www.iphotography.com/blog/in-camera-colour-profiles-vivid-natural-and-neutral-explained/)).
Phone pipelines add scene-dependent tone mapping and skin-tone-aware colour boosts
(e.g. [US8311355, "Skin tone aware color boost for cameras"](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/8311355)).
**These are non-linear, spatially-varying, and undocumented. They cannot be inverted.**

**What you can do without a chart — illuminant estimation only.** Classical statistical
methods: **grey world** (assume the mean image irradiance is achromatic), **white patch /
max-RGB**, plus shades-of-grey, grey-edge, weighted grey-edge, bright-and-dark-colours
PCA, grey pixel, greyness index
([Afifi & Brown, "Auto White-Balance Correction for Mixed-Illuminant Scenes," arXiv:2109.08750](https://arxiv.org/pdf/2109.08750);
[MATLAB, "Comparison of Auto White Balance Algorithms"](https://www.mathworks.com/help/images/comparison-of-auto-white-balance-algorithms.html)).
These "rely on statistical hypotheses to approximate the illuminant colour" and their
"accuracy depends heavily on image content and lighting" — grey world fails badly on any
image with a dominant colour (a forest, a sunset, a red barn).

**Magnitude of the problem, measured.** In a 2026 comparison of paint-matching tools:
- **NIX Mini 2** (contact spectro-colorimeter): **ΔE 0.6 average**, the only sub-$100
  tool consistently under 1.0 across three lighting conditions.
- **Adobe Capture** (phone camera, best smartphone-only result): **ΔE 2.1 in midday
  daylight, ΔE 4.2 under incandescent.**
([facadecolorizer, "Best App to Match Paint Color From Photo 2026: 6 Tested"](https://facadecolorizer.com/us/blog/best-app-match-paint-color-from-photo-2026))

Read that as the app's error floor: **~ΔE 2 at best, ~ΔE 4 under tungsten, from an
uncalibrated photo.** That is at or above the acceptability threshold of 1.8 (§2.4). It
also means chasing sub-ΔE-1 precision in the mixing model is pointless while the input
carries ΔE 2–4 of error. **Say this to the user.**

### 6.4 Is a colour-reference chart worth offering? Yes.

**What it buys you.** A ColorChecker Classic gives 24 patches — 18 colours plus 6
neutrals from optical density 0.05 to 1.50, a 4.8-stop range — with known reflectance,
manufactured by Munsell Color Services, industry standard since 1976
([X-Rite, About the ColorChecker Classic](https://xritephoto.com/ph_product_overview.aspx?ID=820&Action=support&SupportID=5005);
[Imatest Colorcheck](https://www.imatest.com/docs/colorcheck/)).
With it in frame you can fit a **3×3 colour-correction matrix plus a tone curve** from
measured patch values to reference values, which corrects white balance, exposure,
saturation boost, and the bulk of the camera's colour rendering **in one step** — and you
can *report the residual ΔE* so the user knows how well it worked. Ground-truth illuminant
for AWB evaluation is itself normally derived from a ColorChecker (MATLAB AWB comparison,
above).

**Gotchas to handle:**
- **Reference data is edition-specific.** X-Rite issued new colorimetric reference data;
  chart editions require the matching reference file for accurate profiling. CGATS-format
  text files are available ([X-Rite, New color specifications for ColorChecker SG and
  Classic](https://www.xrite.com/service-support/new_color_specifications_for_colorchecker_sg_and_classic_charts)).
  Ship the reference data and let the user pick their edition.
- **Don't mix up chart types.** Classic vs ColorChecker SG differ by **average ΔE\*ab ≈ 7
  (ΔE00 ≈ 3.7)** on corresponding patches
  ([Kasson, "CC24 vs CCSG spectra and CIELab values"](https://blog.kasson.com/the-last-word/cc24-vs-ccsg-spectra-and-cielab-values/)) —
  larger than the whole error budget.
- Charts must be lit by the same light as the subject, and specular glare on the chart
  will wreck the fit.

**Recommendation:** ship it as an optional "Calibrate with a colour chart" step with
automatic patch detection, a fitted 3×3 CCM + per-channel tone curve, and a displayed
"calibration residual: ΔE00 = x.x". Even the cheap third-party 24-patch charts (~$20)
would move the app from ~ΔE 2–4 to ~ΔE 1. This is the highest-leverage *accuracy* feature
available on the input side, and it is a good differentiator — none of the phone apps
surveyed offer it.

---

## 7. Reporting match quality to the user

### 7.1 What existing tools do

| Tool | How it reports | Source |
|---|---|---|
| **Mixable** (iOS, K–M engine) | Side-by-side comparison **plus a ΔE accuracy score**; mix cards show ratios + ΔE for both 2-paint and 3-paint mixes | [App Store](https://apps.apple.com/us/app/mixable-paint-mixing-guide/id6769655280) |
| **ArtistAssistApp** | **Percentage** similarity (100% = perfect, 0% = black vs white), plus a **spectral reflectance chart** overlaying target vs mixture curves | [Spectral reflectance tutorial](https://artistassistapp.com/en/tutorials/spectral-reflectance/) |
| **MatchThatPaint / Paint Color HQ** | Side-by-side swatches with hex, **LRV**, and ΔE, "so you can see exactly how close (or far apart) two colours really are"; cross-brand matches show the ΔE score **and what it means** | [matchthatpaint.com](https://matchthatpaint.com/), [paintcolorhq.com](https://www.paintcolorhq.com/tools/color-identifier) |
| **Datacolor ColorReader** | Markets on "precise Delta E accuracy" | [product page](https://www.amazon.com/Datacolor-ColorReader-Matching-Identify-Instantly/dp/B07958V9J3) |
| Industrial QC (leather) | Signed components: **`+` = more saturated, `−` = duller; ΔH `+` = hue angle increase, `−` = decrease** | [JALCA, "Correlation of visual and instrumental..."](https://journals.uc.edu/index.php/JALCA/article/download/3587/2780) |

Note that **ArtistAssistApp deliberately avoids ΔE**: it uses "the weighted geometric
mean of angular similarity (cosine) and Euclidean distance of their spectral
reflectances" and reports a percentage. That's a defensible product decision — a
percentage is legible to an artist in a way that "ΔE00 = 3.4" is not.

### 7.2 Recommendation — do all four, layered

**Layer 1: the swatch (primary).** Two adjacent patches, target and achievable mixture,
sharing an edge with no gap — a hard edge is the most sensitive comparison the visual
system has, and it's honest. This is what every tool above leads with. Include a third
patch of the *actual* target after gamut mapping if mapping is on, so the user sees the
three-way relationship.

**Layer 2: a verbal band (primary number).** Map ΔE00 to words, using the measured
thresholds of §2.4 rather than round numbers:

| ΔE00 | Label | Justification |
|---|---|---|
| < 0.8 | Exact match | Paravina PT00 = 0.8 |
| 0.8 – 1.8 | Very close — you'd need them side by side | PT00 → AT00 |
| 1.8 – 4 | Close — a visible but acceptable difference | above AT00 = 1.8 |
| 4 – 8 | Noticeably different | |
| > 8 | Best your palette can do — clearly different | |

Show the ΔE00 number as secondary text for users who want it.

**Layer 3: direction of error — this is the differentiator.** Report the signed CIELAB
components in plain language, following the industrial `+`/`−` convention:

```
ΔL* = +4.2  ΔC*ab = -18.6  ΔH = -3°
→ "Your mix will be lighter and noticeably duller than the photo,
   with the hue very slightly toward orange."
```

Phrase generation rules (thresholds from §4.2's per-attribute PT00: ΔL' 1.04, ΔC' 1.58):

| Component | Sign | Wording | "slightly" / "noticeably" cutoff |
|---|---|---|---|
| ΔL\* | + / − | lighter / darker | 1.0 / 3.0 |
| ΔC\*ab | + / − | more saturated / duller | 1.6 / 5.0 |
| ΔH | + / − | name the neighbouring hue | 2° / 6° |

This is far more actionable than a number: a painter who is told "duller and lighter"
knows to add a touch more of the chromatic pigment and less white, and can decide whether
they care. It also converts the app's biggest weakness (a limited gamut) into useful
information rather than a silent failure.

**Layer 4: an aggregate for the whole image.** Report mean and 95th-percentile ΔE00 over
the image, plus **the percentage of pixels that were out of paint gamut** and had to be
mapped. Show a small false-colour "unreachable colours" overlay. That tells the user
whether to add a paint to their palette — and which one, if you name the hue region where
the deficit is worst. No surveyed tool does this.

**Layer 5 (optional, cheap, high value).** A side-by-side of the photo and a **simulated
painting render** — the image reconstructed from only the achievable mixtures, under the
selected viewing illuminant. Best possible answer to "how close can I get."

---

## 8. Recommended changes, ranked by (impact / effort)

Rated for the described code: `PalettePhotoConverter.RgbToLab`, `PaintBlendMatcher`,
`Data/GoldenPalette.cs`.

### Tier 1 — do these first (high impact, hours of work)

1. **Replace `GoldenPalette.cs` sRGB triples with Golden's published CIELAB.**
   Impact: very high. Effort: very low. Golden publishes measured L\*a\*b\* per colour
   ([source](https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data)).
   Round-tripping an sRGB swatch through a screenshot loses everything: Cad Yellow Medium
   at L\*84.1 C\*95.5 is **outside sRGB** and cannot survive an sRGB representation at all.
   Keep the sRGB triple only for on-screen display. **Do this before anything else — every
   other improvement is limited by palette data quality.**

2. **Audit `RgbToLab` for the exact piecewise transfer function.** Impact: high if it's
   using a 2.2 power; low if already correct. Effort: minutes. Confirm the 0.04045/12.92
   and 1.055/0.055/2.4 constants (§1.1), the 216/24389 Lab threshold, and D65
   (0.95047, 1.0, 1.08883). Add a unit test: sRGB(255,255,255) → L\*100 a\*0 b\*0;
   sRGB(128,128,128) → **L\*53.59** *(computed)*; sRGB(0,0,255) → L\*32.30 a\*79.19
   b\*-107.86 *(computed)*.

3. **Add sigmoidal L\* rescaling into the palette's real L\* range before matching.**
   Impact: **highest of anything in this document.** Effort: low (one function + one
   slider). The palette spans L\* 23.8–98.3; every shadow below L\*24 currently collapses
   onto one black. This is the ICC "flat, plugged-up shadow" failure, by construction
   (§3.3, §4.3). Compute `Lmin`/`Lmax` from the palette, not constants.

4. **Weight ΔL\* by ~1.5 in the matcher.** Impact: high. Effort: trivial — one multiply.
   Justified by measured PT00 ΔC'/ΔL' = 1.58/1.04 ≈ 1.52 (§4.2) plus the painting-theory
   argument. Expose as a "prioritise value" slider.

5. **Quantize/segment before matching, and match region means instead of pixels.**
   Impact: high (quality *and* 100–600× speed). Effort: medium. Bilateral prefilter →
   SLIC → RAG merge → min-area cleanup → match a few hundred region colours (§5.4). This
   also removes the per-pixel performance pressure that would otherwise make the metric
   choice matter.

### Tier 2 — clear wins, a day or two each

6. **Switch the search metric to weighted HyAB; use ΔE2000 only for reporting.**
   Impact: medium-high. Effort: low-medium.
   `ΔE_wHyAB = 1.5·|ΔL*| + sqrt(Δa*²+Δb*²)`. Rationale: CIEDE2000 is only recommended
   below 5 ΔE and is discontinuous above it (§2.2), while HyAB is validated for ≥10 ΔE
   (§2.3) and admits the exact `sqrt(2)` candidate-filter bound (§2.5). Implement ΔE2000
   properly anyway — port [this C reference](https://raw.githubusercontent.com/michel-leonard/ciede2000-color-matching/main/ciede-2000.c)
   and unit-test against [Sharma's 34 test pairs](https://www.hajim.rochester.edu/ece/~gsharma/ciede2000/) —
   and use it for the numbers shown to the user.

7. **Report match quality: swatch + verbal band + direction of error.**
   Impact: high on perceived quality. Effort: medium. §7.2. The signed-component sentence
   ("slightly lighter, noticeably duller") is the cheapest genuinely novel feature here.

8. **Build a real gamut hull and do hue-preserving chroma compression instead of snapping.**
   Impact: high. Effort: medium-high. Sample your mixer to get an achievable hull, then
   run the CSS Color 4-style binary chroma search in OkLCh (§3.3), or Ottosson's adaptive
   L₀ with α = 0.05. Currently every out-of-gamut colour "snaps to nearest sample," which
   collapses distinct saturated colours onto the same mixture and destroys local contrast.

9. **Precompute the mixture candidate table once per palette; add a filter-and-refine
   nearest-neighbour.** Impact: medium (correctness of #6 + speed). Effort: medium.
   kd-tree on ΔE76 → top-32 → rescore with weighted HyAB, using the `sqrt(2)` safety
   factor. ~19,300 candidates at N=30 makes the table cheap to build and reuse.

10. **Fix any gamma-space averaging.** Impact: medium (visible in shadows). Effort: low.
    Do all downsampling, superpixel means and region means in linear light or in LAB.
    `System.Drawing`'s resamplers average in gamma space (§1.2).

11. **Sample a patch, not a pixel, for the colour picker.** Impact: medium. Effort: low.
    JPEG 4:2:0 means single-pixel chroma near edges is interpolated fiction (§6.2).

### Tier 3 — worth doing, larger scope

12. **Optional ColorChecker calibration step.** Impact: high on *absolute* accuracy —
    plausibly ΔE 2–4 → ΔE ~1 (§6.3–6.4). Effort: high (patch detection, 3×3 CCM fit,
    reference data management, edition selection). Best differentiator versus phone apps.
    Report the calibration residual.

13. **Viewing-illuminant selector with Bradford chromatic adaptation.** Impact: medium
    (matters a lot for anyone painting for a warm-lit room). Effort: medium. Bradford, not
    CAT02 — CAT02 can produce negative tristimulus values that break downstream K–M
    (§1.4). Offer D65 / D50 / 3000 K.

14. **Aggregate image report: mean + p95 ΔE00, out-of-gamut percentage, false-colour
    overlay, "add this paint" suggestion.** Impact: medium-high, genuinely novel.
    Effort: medium.

15. **Simulated-painting preview.** Impact: medium-high on user confidence.
    Effort: medium; mostly reuses the region pipeline.

### Do not do

- **Do not add error-diffusion dithering** as the general quantizer (§5.3). A coarse,
  deliberate "Divisionist mode" is a legitimate separate feature; Floyd–Steinberg as the
  default is not.
- **Do not adopt CIECAM02 / CAM16-UCS yet** (§1.5). You cannot supply L_A, surround or
  Y_b honestly, the space is still being revised, and its benefit is small next to a 24
  L\*-unit black-point deficit.
- **Do not switch the search metric to plain ΔE2000 and stop there.** It is the wrong
  operating range and it forfeits the safe candidate-pruning bound. It is, however, the
  right thing to *display*.
- **Do not use per-channel clipping** anywhere (§3.3).

---

## 9. Unverified / could not confirm

Flagged honestly so nothing here gets treated as settled.

1. **Braun & Fairchild's exact sigmoidal equation.** The parameterisation (normalised
   discrete cumulative normal, mean `x₀`, std dev `s`, `x₀` shifts the linear portion,
   `s` sets slope) is confirmed from multiple secondary descriptions, but I could not
   read the primary text. `markfairchild.org` serves an **expired TLS certificate**
   (blocking PRO06.pdf, PRO08.pdf, PAP07.pdf), `digitalarchive.rit.edu` **no longer
   resolves**, and SPIE/Wiley are paywalled. The C# stand-in in §4.3 is my construction,
   consistent with the description but **not the published function.**
2. **Bruce Lindbloom's site is unreachable from this environment** —
   `error:1000009a:SSL routines:HANDSHAKE_FAILURE_ON_CLIENT_HELLO` for both
   `Eqn_RGB_XYZ_Matrix.html` and `Eqn_DeltaE_CIE2000.html`. The sRGB→XYZ matrix first row
   (`0.4124564 0.3575761 0.1804375`) came from search result text only. **Verify the full
   3×3 against a second source.**
3. **ICC White Paper 40's exact black-point-compensation formula.** The PDF would not
   parse. The concept, its restriction to relative-colorimetric/perceptual/saturation
   intents, and its rationale are confirmed from secondary sources; **the XYZ scaling
   equation is not.**
4. **The 2024 per-attribute CIEDE2000 thresholds** (PT00 ΔL' 1.04 / ΔC' 1.58; AT00
   ΔL' 2.82 / ΔC' 3.04) come from search excerpts of a ScienceDirect page that returned
   403. The 2011 companion values (AT ΔL' 2.92, ΔC' 2.52, ΔH' 1.90) were read directly
   from the PubMed abstract and are solid. **The `w_L ≈ 1.5` recommendation rests on the
   2024 numbers — re-verify before treating 1.5 as precise.** The direction of the effect
   (more sensitive to lightness than chroma) is supported by both papers.
5. **Paravina 2015 thresholds** (ΔE\*ab PT 1.2 / AT 2.7; ΔE00 PT 0.8 / AT 1.8) confirmed
   via search excerpts of the Wiley abstract; full text paywalled (402). Widely cited and
   internally consistent, so confidence is high.
6. **Luo et al. 2023 CAM16-UCS ranking** — abstract-level only; Wiley returned 402.
7. **Golden's measurement conditions are not stated on their pigment-data page** — no
   illuminant, observer, or specular-included/excluded noted. Berns used an X-Rite MS7000,
   integrating sphere, **specular included**, which raises L\* on glossy films. If Golden
   did the same, **their black L\*≈25 may be optimistic for a matte acrylic film.** The
   direction of the error makes the value-compression argument *stronger*, not weaker, but
   measure your own swatches if precision matters.
8. **The `sqrt(2)` candidate-filter bound in §2.5 is my derivation** from the standard
   ℓ1/ℓ2 norm relationship applied to HyAB's structure. The reasoning is elementary and
   I'm confident in it, but it is not cited from a source.
9. **Berns' "22% of acrylic colours out of AdobeRGB"** figure came from search summary
   text; the PDF would not parse. Both the IS&T library copy and the grayskyimaging
   mirror failed text extraction.
10. **30fps.net HyAB k-means article returns 403** to this fetcher. Content confirmed via
    search excerpts and the HN thread, including the author's own caveat against treating
    HyAB as a drop-in replacement.
11. **CIE 156:2004, CIE 142-2001 and CIE 224:2017 are paid standards.** HPMINDE/SGCK and
    the ΔE00 0–5 recommended range are confirmed from secondary literature (Sharma & Bala
    CIC12; multiple papers), not from the standards themselves.
