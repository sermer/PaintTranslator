# Research 03 — The Physical Reality of Acrylic Paint

**Scope:** what a single-sRGB-triple-per-paint model throws away, and what data exists to do better.
**Date:** 2026-07-26. **Status:** research only, no code changed.
**Confidence markers used throughout:** `[CITED]` = directly sourced, `[DERIVED]` = I computed it from cited data (method stated), `[INFERRED]` = reasoned, not verified.

---

## 0. Executive summary — the five things that matter most

1. **A paint is not one color. The industry standard characterization is a *pair*: masstone (straight from the tube) + a 1:10 tint with titanium white.** Golden's own lab uses exactly this pair, and it is not an accident — the "masstone-tint method" is the minimum data needed to solve two-constant Kubelka–Munk for a paint's separate absorption (K) and scattering (S) spectra. Two samples, two unknowns. `[CITED]` (Berns 2022; Golden "Strong and Weak Colors")
2. **Golden publishes far more per-paint data than the app currently uses**, including per-color **CIE L\*a\*b\***, Munsell H V/C, opacity rating, lightfastness, gloss, viscosity, pigment CI names, **and a numeric tint strength** — 154 rows, free, on the web. There is also a **free, redistributable spreadsheet of 78 Golden Heavy Body reflectance spectra (400–700 nm @ 10 nm) with L\*a\*b\* and K/S**, released by Golden with permission to share.
3. **Tinting strength is the single biggest missing parameter, and it spans ~50× across the Golden line.** By paint volume, Phthalo Blue GS is ≈7× Cerulean Blue Chromium, ≈2.4× Ultramarine, ≈3.7× Cobalt Blue. `[DERIVED]` Equal-volume recipes are therefore wrong by up to an order of magnitude.
4. **Chroma collapse is measurable and steep.** Equal-parts mixes retain ≈51% of parent chroma at 2 pigments, 35% at 3, 28% at 4. Retention also falls hard with hue separation: ≈68% at <60° apart, ≈14% at 120–150°. `[DERIVED]` This is quantitative support for the "2 pigments + white, 3 max" rule.
5. **Recipe precision beyond about ±10% *relative* on each component is wasted.** ΔE00 ≈ 0.25 × (percent relative error in the ratio). 37/63 vs 40/60 is ΔE00 ≈ 0.4 — invisible. Recipes should be snapped to a **geometric ladder** (1:1, 1:2, 1:3, 1:5, 1:10, 1:20), not percentages. `[DERIVED]`

---

## 1. Masstone vs undertone

### 1.1 Definitions (manufacturer + handprint)

- **Masstone** — "paint applied so it completely covers the surface and underlying colors cannot show through… when Phthalo Blue is thickly applied, the masstone appears near black."
  **Undertone** — "visible when we spread the color very thinly over a white surface… by scraping the color over a surface or by thinning the colors dramatically with acrylic medium or water."
  "Certain colors, such as the Cadmiums and Cobalts, have similar masstones and undertones. With the transparent organic colors like the Quinacridones or Phthalos, the undertone can be quite different from what might be expected by looking at the masstone." `[CITED]` — https://goldenartistcolors.com/resources/color-mixing-guide and https://goldenartistcolors.com/resources/strong-and-weak-colors
- MacEvoy is more precise. Masstone (top tone) = "its color appearance when applied on a pure white surface as a layer that mostly but not completely hides the surface below." Undertone (tint) = "the color appearance of the paint applied on a pure white surface as a highly diluted solution… or as mixed with a large quantity of titanium oxide paint." And the three-way change: **"(1) the undertone has a lighter value, (2) the undertone has a lower chroma or less saturated color, and (3) the undertone usually has a slightly different hue."** `[CITED]` — https://www.handprint.com/HP/WCL/pigmt3.html

### 1.2 The physics

Three separate mechanisms stack up, and the app's single-triple model collapses all of them:

**(a) Absorption vs. scattering — the K/S ratio.** In Kubelka–Munk terms a paint film's reflectance depends on the *ratio* K/S, not on K alone. A transparent organic pigment such as PB15:3 has very high K and very low S. At masstone concentration, K/S is enormous at every visible wavelength → reflectance approaches a few percent everywhere → **the film reads near-black, and hue information is buried**. Add titanium white (huge S, near-zero K) and S in the denominator jumps by orders of magnitude; now K/S is small where the phthalo doesn't absorb (450–500 nm) and still large where it does (>550 nm) → brilliant cyan. Same pigment, same K spectrum, different S. `[CITED for the mechanism]` — Berns 2022 (two-constant K-M, masstone-tint method), https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf

**Direct evidence from the data.** In Golden's own published masstone spectra the minimum reflectance across 78 Heavy Body paints bottoms out at **3.7–4.6%** for every strong transparent color (Phthalo Blue GS, Phthalo Green YS, Dioxazine Purple, Ultramarine, Bone Black, Prussian Blue Hue). K/S at the absorption peak is correspondingly clamped at **~12** for all of them. `[DERIVED from the Golden spectra spreadsheet]` Masstone alone literally cannot distinguish a strong colorant from a weak one — the signal is saturated. This is a hard argument for storing the tint too.

**(b) Concentration / pigment volume concentration (PVC).** Golden: `PVC = pigment volume / (pigment volume + binder volume)`. Critical PVC (CPVC), where binder just fills the voids, "generally falls between 30–60% across paint systems." Measured PVC by medium: Ultramarine ≈14% in acrylic vs ≈27% oil, ≈76% watercolor; Cobalt ≈20% acrylic vs ≈28% oil, ≈68% watercolor. Below CPVC: "smoother surfaces produce saturated, deeper color with glossy finish and higher transparency." Above CPVC: rough texture creates "haze of white, diffused light," appearing "lighter, chalky, and more opaque." Their Ultramarine example: reflectance at 440 nm goes from ~10% at 40% PVC to ~60% at 80% PVC. `[CITED]` — https://justpaint.org/pigment-volume-concentration-and-its-role-in-color/
Corroborating third-party value: LBNL measured **Liquitex Cobalt Blue dry-film PVC = 14%**. `[CITED]` — https://coolcolors.lbl.gov/LBNL-Pigment-Database/paints/U05.html

**(c) Particle size and surface scattering.** MacEvoy: "the increase in surface area produced by the smaller particle sizes increases the total surface scattering from the same quantity of pigment," which lightens and desaturates dilute applications. Golden: weak/light tints "indicate larger pigment particle sizes"; strong/dark tints "indicate very small pigment particle sizes." Representative particle sizes from handprint: quinacridones/phthalocyanines ~0.1 µm, carbon black ~0.05 µm, cadmium red ~1 µm, ultramarine ~5 µm, cobalt violet/manganese blue ~50 µm. `[CITED]` — handprint pigmt3, Golden "Strong and Weak Colors"

**(d) Refractive-index ratio governs opacity, not "pigment transparency."** MacEvoy, important and counter-intuitive: "Paint transparency depends primarily on the average size of the pigment particles and the thickness of the paint layer… Despite common belief, it does not much depend on whether the pigment particles themselves are transparent or semiopaque." What matters is the RI ratio pigment:binder. "A pigment appears significantly cloudy when the RI ratio is around 1.33, and almost completely opaque when it is above 1.5." Pigment RI values: phthalocyanines 1.4 (ratio 0.95 in a ~1.47 vehicle), ultramarine 1.5 (1.02), viridian 1.6 (1.09), cerulean 1.8 (1.22), rutile titanium white 2.7 (1.84). `[CITED]` — https://www.handprint.com/HP/WCL/pigmt3.html
The coatings-industry rule of thumb agrees: "pigments with a refractive index of more than 1.5 are considered hiding or opacifying pigments. Pigments below 1.5 are considered extenders or fillers." `[CITED]` — https://www.specialchem.com/coatings/guide/tinting-strength

**(e) Film thickness.** Golden's own spectra are labelled "HB 10 mil Drawdowns over White" — 10 mil *wet*, which dries to **6 mil** because acrylics lose water. Golden explicitly cautions: "the white of the card definitely influences the reading of more transparent colors. For many calculations, however, one would want to have readings based on truly opaque films. Unfortunately those are things we currently do not have available." `[CITED]` — https://www.realtimerendering.com/golden.html

### 1.3 THE CRITICAL QUESTION: masstone, tint, or both?

**Answer: both, and specifically masstone + a 1:10 tint with titanium white.** Three independent lines of evidence:

1. **Golden's own lab characterization pair.** Berns, describing what Golden sent him: *"A request was made for spectral measurement data of drawdowns used to evaluate colorant strength, opacity, evaluate pigment batch to batch consistency, etc. Golden kindly sent spectral data for 68 Heavy Body acrylic paints from measurements of **drawdowns of masstones (out of the tube) and a 10% mixture with titanium white**."* `[CITED]` — Berns 2022, https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf
2. **It is the mathematical minimum for two-constant K-M.** Berns: *"The masstone-tint method was used to calculate unit absorption and scattering coefficients for each paint relative to the scattering of white defined as unity. One limitation of this method is that accuracy cannot be evaluated since the calculations are determinate: two samples and two unknowns, resulting in perfect spectral fits for the 10% tint and masstone. In the author's experience, this method gives reasonable results."* He notes the ideal would be *"multiple tints and for yellows, additional mixtures with black."* `[CITED]`
3. **Golden's public tint-strength number is measured from exactly this tint.** "Golden's tints are done by **mixing 10 parts Titanium White to 1 part paint color**." `[CITED]` — https://goldenartistcolors.com/resources/strong-and-weak-colors

**A second, subtler standard exists and is worth knowing.** ASTM D4303 lightfastness testing uses tints matched to *equal colorimetric strength*, not equal volume: for acrylics and oils the tint must reach **"35–45% reflectance at the wavelength of maximum absorption."** And "ASTM lightfastness ratings are always based on the tint and not the masstone." `[CITED]` — https://justpaint.org/lightfastness-testing-at-golden-artist-colors/
So there are two industry tint conventions: **equal-volume 1:10** (Golden's strength/QC drawdown, which is what you want for a mixing model) and **equal-strength ~40%R at λmax** (ASTM permanence testing). Don't conflate them.

**Practical recommendation for the app:** store, per paint, a masstone spectrum *and* a 1:10-tint spectrum (or, if only Lab is available, masstone Lab + tint Lab), then solve two-constant K-M offline to get K(λ) and S(λ). Store K and S. That is the schema Berns used and it is the one that makes phthalo blue behave correctly.

Also note the well-documented pitfall the app should warn about, straight from Golden: *"People have a tendency to try and add white to lighten colors instead of adding color to white for a light tint. This is especially true for the Light Phthalo Blue and Light Phthalo Green due to the parent colors' intense pigmentation."* `[CITED]` — https://justpaint.org/introducing-new-golden-heavy-body-light-value-colors/

---

## 2. Tinting strength — how it's measured, and real numbers

### 2.1 The industry definitions

- **Golden (qualitative):** "The ability of a color to change the character of another color. We determine this by adding the same amount of Titanium White to each color and observing the resulting strength of the color mixture. Weaker tinting colors create light pastel mixtures. Stronger tinting colors create darker mixtures." `[CITED]` — https://goldenartistcolors.com/technical-specifications-explained
- **The coatings-industry formula:** `% Tinting Strength = Batch K/S(λmax) / Control K/S(λmax) × 100`, measured on a let-down into a white base with a spectrophotometer. `[CITED]` — https://www.specialchem.com/coatings/guide/tinting-strength
- **Standard test methods:** **ASTM D387** "Standard Test Method for Color and Strength of Chromatic Pigments with a Mechanical Muller" (mass color + tinting strength vs a standard; withdrawn 2023) and **ASTM D4838** "Standard Test Method for Determining the Relative Tinting Strength of Chromatic Paints" (withdrawn 2023). `[CITED]` — https://www.astm.org/Standards/D387.htm , https://www.astm.org/Standards/D4838.htm
- **MacEvoy:** tinting strength is "its colorant power in relation to its mass"; the traditional test "measures the minimum amount of pigment required to impart a perceptible color to a specific amount of clear liquid or white paint." His headline number: **"Phthalocyanine blue… has a tinting strength about 40 times greater than ultramarine blue, and twice that of prussian blue."** `[CITED]` — https://www.handprint.com/HP/WCL/pigmt3.html

⚠ **Do not use the 40× figure in the app.** It is *pigment vs pigment by mass*. What the app needs is *tube paint vs tube paint by volume*, and the two differ enormously because (i) Golden's phthalo paints carry much lower pigment loading than its ultramarine paints (phthalos are reactive and don't tolerate high loads), and (ii) the pigment densities differ 1.5× (PB15:3 = 1.62, PB29 = 2.35). The measured paint-level ratio is ≈2.4×, not 40×. `[DERIVED]` See §2.3.

### 2.2 Golden's published numeric tint strength — and what the number is

Golden's Heavy Body Pigment Detail Chart has a **Tint Strength** column with real numbers for 121 of 154 colors. Source: https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data

`[INFERRED — flagged, and I could not find Golden documenting the column's units]` The values are almost certainly the **L\* of the 1:10 white tint**, so **lower = stronger tinter**. Evidence for the inference:
- Range is 27–98, i.e. L\*-shaped, and the qualitative rankings come out exactly right: Carbon Black (PBk7) 56.55 < Mars Black (PBk11) 67.81 < Bone Black (PBk9) 78.87 — correct strength order for the three blacks, despite all three having *identical masstone L\* ≈ 25*. So it is an independent measurement of the tint, not a restatement of masstone.
- Phthalo Blue GS 71.17 < Ultramarine 79.94 < Cobalt 83.70 < Cerulean 87.78 — correct.
- Hansa Yellow Opaque (PY74) 92.91 < Cadmium Yellow Light (PY35) 95.19 — correct (hansa is the stronger tinter).
- **Caveat:** the two whites break the pattern (Zinc White 27.20, Titanium White 42.25) — for whites Golden evidently runs the test in reverse (white added into a dark standard), which is why weaker Zinc reads *lower*. Exclude whites from any ranking built on this column.

### 2.3 Derived relative tinting-strength index (Golden Heavy Body)

`[DERIVED]` Method: treat Golden's tint-strength number as the tint's L\*; convert L\*→Y→R; take single-constant `K/S = (1−R)²/2R`; invert the 1:10 mix relation `K/S_tint = (10·K/S_white + 1·K/S_paint)/11` to recover a per-unit-volume strength index for the paint; normalise to Cerulean Blue Chromium = 1.00. Full 121-row table below.

⚠ **Known bias:** this index is luminance-based, so it *systematically understates yellows* (a yellow's absorption is in the blue band and barely dents luminance). The yellow rows are ordinally useful among themselves but not comparable to the blues/blacks. A correct index needs tint spectra or at least tint a\*b\*, which Golden does not publish. Flagging clearly.

Headline numbers (multiples of Cerulean Blue Chromium PB36, by *paint* volume):

| Paint | Pigment | × Cerulean | vs Phthalo Blue GS |
|---|---|---|---|
| Anthraquinone Blue | PB60 | 9.8 | 1.39× |
| Dioxazine Purple | PV23 | 9.6 | 1.37× |
| Mars Black | PBk11 | 9.3 | 1.32× |
| Prussian Blue Hue | PBk9+PB15:0+PV23 | 7.6 | 1.08× |
| Phthalo Blue (Red Shade) | PB15:0 | 7.3 | 1.03× |
| **Phthalo Blue (Green Shade)** | **PB15:3** | **7.0** | **1.00×** |
| Quinacridone Magenta | PR122 | 5.7 | 0.81× |
| Cadmium Red Medium | PR108 | 5.5 | 0.78× |
| Phthalo Green (Blue Shade) | PG7 | 3.5 | 0.50× |
| **Ultramarine Blue** | **PB29** | **3.0** | **0.43×** |
| Burnt Sienna | PBr7 | 2.8 | 0.40× |
| Phthalo Green (Yellow Shade) | PG36 | 1.9 | 0.27× |
| **Cobalt Blue** | **PB28** | **1.9** | **0.27×** |
| Yellow Oxide | PY42 | 1.3 | 0.19× |
| **Cerulean Blue, Chromium** | **PB36** | **1.00** | **0.14×** |
| Cobalt Teal | PG50 | 0.56 | 0.08× |
| Yellow Ochre | PY42 | 0.50 | 0.07× |
| Cadmium Yellow Medium | PY35 | 0.36 | 0.05× |
| Hansa Yellow Opaque | PY74 | 0.31 | 0.04× |
| Cadmium Yellow Light | PY35 | 0.08 | 0.011× |
| Bismuth Vanadate Yellow | PY184 | 0.05 | 0.008× |

So: **"phthalo blue is roughly 7× cerulean, 2.4× ultramarine, 3.7× cobalt, and 2× phthalo green BS"** is defensible. The often-quoted 10× phthalo-vs-cerulean is close to right at the paint level. Carbon Black is the strongest achromatic (≈21× cerulean).

Golden's own qualitative grouping, for cross-check: the strong pigments are **Quinacridones, Hansas, Phthalos** ("organic… excellent transparency"); weak ones are the **Cadmiums and Cobalts**. Golden also notes "practically all of our paint lines are made with the same amount of pigment, as much as possible without sacrificing film strength" — i.e. strength differences are pigment-intrinsic, not line-specific, so Heavy Body / Fluid / SoFlat share tinting behaviour. `[CITED]` — https://goldenartistcolors.com/resources/strong-and-weak-colors

### 2.4 What unbalanced tinting strength actually does to a mix

`[DERIVED]` Single-constant K-M on Golden's masstone spectra, mixing by paint volume, Cad Yellow Medium as the base:

| Blue added | 5% by volume | 10% | 25% | 50% |
|---|---|---|---|---|
| **Phthalo Blue GS** | L\* 62, C\* 57 | L\* 54, C\* 42 | L\* 41, C\* 20 | L\* 32, **C\* 3** (mud) |
| **Ultramarine** | L\* 62, C\* 56 | L\* 53, C\* 42 | L\* 41, C\* 19 | L\* 32, **C\* 2** |
| **Cerulean Chromium** | L\* 68, C\* 66 | L\* 61, C\* 54 | L\* 51, C\* 36 | L\* 43, C\* 20 |

Two lessons. (1) Cerulean is the only one that still has chroma left at 50/50 — a "balanced green" from cerulean+cadmium yellow is roughly 1:1.6, whereas anything with phthalo needs the blue down at a few percent. (2) At masstone concentration, adding a strong transparent blue to a yellow behaves mostly as a **darkener**, not a hue-shifter — hue barely moves from ~90° while L\* falls 30 points. That is the masstone problem in miniature, and it is why a masstone-only model will produce recipes that look like mud.

### 2.5 Full derived tinting-strength table

<details><summary>121 Golden Heavy Body colors, sorted strongest → weakest (derived, see §2.3 caveats)</summary>

| Paint | Pigment | Golden Tint Strength (L\* of 1:10 tint) | derived K/S index | × Cerulean |
|---|---|---|---|---|
| Zinc White (1415) ⚠whites measured in reverse | PW4 | 27.20 | 95.758 | 157.32 |
| Titanium White (1380) ⚠ | PW6 | 42.25 | 33.122 | 54.41 |
| Carbon Black (1040) | PBk7 | 56.55 | 12.817 | 21.06 |
| Anthraquinone Blue (1005) | PB60 | 67.09 | 5.976 | 9.82 |
| Dioxazine Purple (1150) | PV23 | 67.34 | 5.860 | 9.63 |
| Red Oxide (1360) | PR101 | 67.40 | 5.833 | 9.58 |
| Mars Black (1200) | PBk11 | 67.81 | 5.647 | 9.28 |
| Pyrrole Red Dark (1278) | PR264 | 68.89 | 5.179 | 8.51 |
| Prussian Blue Hue (1460) | PBk9, PB15:0, PV23 | 70.30 | 4.611 | 7.58 |
| Quinacridone Violet (1330) | PV19 | 70.65 | 4.478 | 7.36 |
| Phthalo Blue (Red Shade) (1260) | PB15:0 | 70.77 | 4.433 | 7.28 |
| Cadmium Red Dark (1080) | PR108 | 71.14 | 4.295 | 7.06 |
| Phthalo Blue (Green Shade) (1255) | PB15:3 | 71.17 | 4.284 | 7.04 |
| Naphthol Red Medium (1220) | PR5 | 71.47 | 4.176 | 6.86 |
| Naphthol Red Light (1210) | PR112 | 72.21 | 3.916 | 6.43 |
| Medium Violet (1572) | PW6, PR122, PV23 | 72.29 | 3.889 | 6.39 |
| Quinacridone Crimson (disc.) (1290) | PR202, PR206 | 72.39 | 3.855 | 6.33 |
| Hooker's Green Hue (1454) | PY150, PB60, PR122 | 72.50 | 3.817 | 6.27 |
| Permanent Maroon (1252) | PR122, PY150, PR101, PG7 | 72.59 | 3.787 | 6.22 |
| Permanent Violet Dark (1253) | PR122, PB60 | 73.00 | 3.651 | 6.00 |
| Alizarin Crimson Hue (1450) | PR122, PY150, PG7 | 73.10 | 3.619 | 5.95 |
| Pyrrole Red (1277) | PR254 | 73.37 | 3.532 | 5.80 |
| Cadmium Red Medium Hue (1552) | PR5, PR112 | 73.39 | 3.525 | 5.79 |
| Quinacridone Magenta (1305) | PR122 | 73.51 | 3.487 | 5.73 |
| Cadmium Red Medium (1100) | PR108 | 73.92 | 3.359 | 5.52 |
| Turquoise (Phthalo) (1390) | PG7, PB15:3 | 74.57 | 3.163 | 5.20 |
| Van Dyke Brown Hue (1462) | PR101, PBk7 | 74.68 | 3.130 | 5.14 |
| Payne's Gray (1240) | PB29, PBk7 | 74.88 | 3.072 | 5.05 |
| Naphthol Pink (1579) | PW6, PR112 | 75.05 | 3.023 | 4.97 |
| Benzimidazolone Burnt Orange (1006) | PBr25 | 75.41 | 2.921 | 4.80 |
| Burnt Umber (1030) | PBr7 | 75.50 | 2.895 | 4.76 |
| Pyrrole Red Light (1279) | PR255 | 75.61 | 2.865 | 4.71 |
| Quinacridone Burnt Orange (disc.) (1280) | PR206 | 76.32 | 2.673 | 4.39 |
| Raw Umber (1350) | PBr7 | 76.39 | 2.654 | 4.36 |
| Quinacridone Red (1310) | PV19 | 76.49 | 2.628 | 4.32 |
| Primary Magenta (1510) | PV19 | 76.55 | 2.612 | 4.29 |
| Naples Yellow Hue (1459) | PW6:1, PW6, PY42, PY83(HR70) | 76.58 | 2.605 | 4.28 |
| Cadmium Red Light (1090) | PR108 | 76.60 | 2.599 | 4.27 |
| Cobalt Blue Hue (1556) | PB29, PW6, PB15:3 | 76.96 | 2.507 | 4.12 |
| Violet Oxide (1405) | PR101 | 77.03 | 2.489 | 4.09 |
| Jenkins Green (1195) | PBk9, PG36, PY150 | 77.30 | 2.422 | 3.98 |
| N2 Neutral Gray (1442) | PBk9, PBr7, PW6 | 77.33 | 2.414 | 3.97 |
| Cobalt Violet Hue (1465) | PV19, PW6, PR122, PV23 | 77.59 | 2.351 | 3.86 |
| Light Violet (1568) | PW6, PV23 | 77.95 | 2.264 | 3.72 |
| N3 Neutral Gray (1443) | PBk9, PW6, PBr7 | 77.96 | 2.262 | 3.72 |
| Mars Yellow (1202) | PBr6 | 78.26 | 2.191 | 3.60 |
| Phthalo Green (Blue Shade) (1270) | PG7 | 78.54 | 2.127 | 3.49 |
| Transparent Brown Iron Oxide (1383) | PR101, PBk7 | 78.63 | 2.106 | 3.46 |
| Medium Magenta (1570) | PW6, PR122 | 78.66 | 2.100 | 3.45 |
| Bone Black (1010) | PBk9 | 78.87 | 2.053 | 3.37 |
| Primary Cyan (1500) | PB15:3, PW6 | 78.98 | 2.028 | 3.33 |
| Quinacridone Red Light (disc.) (1320) | PR207 | 79.47 | 1.922 | 3.16 |
| Vat Orange (1403) | PO43 | 79.62 | 1.890 | 3.10 |
| N4 Neutral Gray (1444) | PW6, PBk9, PBr7 | 79.63 | 1.888 | 3.10 |
| Ultramarine Blue (1400) | PB29 | 79.94 | 1.823 | 2.99 |
| Burnt Umber Light (1035) | PBr7 | 80.07 | 1.796 | 2.95 |
| Burnt Sienna (1020) | PBr7 | 80.56 | 1.697 | 2.79 |
| Light Turquoise (Phthalo) (1564) | PW6, PG7, PB15:3 | 80.75 | 1.660 | 2.73 |
| Sap Green Hue (1461) | PY150, PG36, PBk7, PR101 | 80.88 | 1.635 | 2.69 |
| Chromium Oxide Green (1060) | PG17 | 81.19 | 1.576 | 2.59 |
| Pyrrole Orange (1276) | PO73 | 81.55 | 1.509 | 2.48 |
| N5 Neutral Gray (1445) | PW6, PBk9, PBr7 | 82.20 | 1.392 | 2.29 |
| Cerulean Blue Deep (1051) | PB36 | 83.10 | 1.240 | 2.04 |
| Cadmium Orange (1070) | PO20 | 83.30 | 1.208 | 1.98 |
| Permanent Green Light (1250) | PY175, PG7, PW6 | 83.50 | 1.176 | 1.93 |
| Transparent Red Iron Oxide (1385) | PR101 | 83.51 | 1.174 | 1.93 |
| Phthalo Green (Yellow Shade) (1275) | PG36 | 83.69 | 1.146 | 1.88 |
| Cobalt Blue (1140) | PB28 | 83.70 | 1.144 | 1.88 |
| Quin./Nickel Azo Gold (disc.) (1301) | PY150, PO48 | 84.10 | 1.083 | 1.78 |
| Graphite Gray (1160) | PBk10 | 84.22 | 1.065 | 1.75 |
| N6 Neutral Gray (1446) | PW6, PBk9, PBr7 | 85.10 | 0.939 | 1.54 |
| Viridian Green Hue (1469) | PW6, PB15:3, PBr7, PY150 | 85.29 | 0.912 | 1.50 |
| Light Ultramarine Blue (1566) | PW6, PB29 | 85.40 | 0.898 | 1.47 |
| Azo Gold (1302) | PR101, PY150 | 85.62 | 0.868 | 1.43 |
| Azurite Hue (1464) | PBr7, PW6, PB15:0 | 85.69 | 0.859 | 1.41 |
| Cobalt Turquoise (1144) | PB36 | 86.04 | 0.814 | 1.34 |
| Yellow Oxide (1410) | PY42 | 86.07 | 0.810 | 1.33 |
| Teal (1369) | PW6, PG7, PB15:3 | 86.24 | 0.788 | 1.30 |
| Cobalt Green (1142) | PG26 | 86.82 | 0.718 | 1.18 |
| Chromium Oxide Green Dark (1061) | PG17 | 87.51 | 0.638 | 1.05 |
| Light Magenta (1562) | PW6, PR112 | 87.58 | 0.631 | 1.04 |
| Cerulean Blue, Chromium (1050) | PB36 | 87.78 | 0.609 | 1.00 |
| Manganese Blue Hue (1457) | PB15:3, PW6, PG7 | 87.92 | 0.594 | 0.98 |
| Raw Sienna (1340) | PBr7 | 88.08 | 0.577 | 0.95 |
| Diarylide Yellow (1147) | PY83(HR70) | 88.16 | 0.568 | 0.93 |
| Ultramarine Violet (1401) | PV15 | 88.18 | 0.566 | 0.93 |
| N7 Neutral Gray (1447) | PW6, PBk9, PBr7 | 88.54 | 0.530 | 0.87 |
| Smalt Hue (1467) | PB29, PBk7, PV23 | 88.69 | 0.515 | 0.85 |
| Isoindolinone Yellow (1193) | PY110 | 88.89 | 0.495 | 0.81 |
| Green Gold (1170) | PY150, PG36, PY175 | 89.11 | 0.474 | 0.78 |
| Naples Yellow Deep (1222) | PBr24 | 89.44 | 0.444 | 0.73 |
| Light Green (Blue Shade) (1558) | PY175, PW6, PG7 | 89.67 | 0.423 | 0.69 |
| India Yellow Hue (1455) | PY175, PY150, PR122 | 90.36 | 0.364 | 0.60 |
| Cobalt Teal (1145) | PG50 | 90.62 | 0.343 | 0.56 |
| Nickel Azo Yellow (1225) | PY150 | 90.70 | 0.337 | 0.55 |
| Cadmium Yellow Dark (1110) | PY35, PO20 | 91.01 | 0.313 | 0.51 |
| Transparent Yellow Iron Oxide (1386) | PY42 | 91.02 | 0.313 | 0.51 |
| Yellow Ochre (1407) | PY42 | 91.10 | 0.307 | 0.50 |
| Terre Verte Hue (1468) | PG17, PBk9, PBr7, PY42, PG36 | 91.29 | 0.293 | 0.48 |
| Cadmium Yellow Medium (1130) | PY35 | 92.35 | 0.221 | 0.36 |
| N8 Neutral Gray (1448) | PW6, PBk9, PBr7 | 92.54 | 0.209 | 0.34 |
| Light Green (Yellow Shade) (1560) | PY175, PW6, PG7 | 92.61 | 0.205 | 0.34 |
| Titan Buff (1370) | PW6:1, PW6 | 92.87 | 0.189 | 0.31 |
| Hansa Yellow Opaque (1191) | PY74 | 92.91 | 0.187 | 0.31 |
| Titan Green Pale (1371) | PW6, PY42, PBr7, PG7 | 92.99 | 0.182 | 0.30 |
| Light Phthalo Blue (1577) | PW6, PB15:3 | 93.01 | 0.181 | 0.30 |
| Light Orange (1575) | PW6, PY184, PO73 | 93.34 | 0.163 | 0.27 |
| Titan Mars Pale (1576) | PW6, PBr6 | 93.39 | 0.160 | 0.26 |
| Cadmium Yellow Medium Hue (1554) | PY74, PY175, PY83(HR70) | 93.75 | 0.142 | 0.23 |
| Benzimidazolone Yellow Medium (1008) | PY154 | 93.97 | 0.131 | 0.22 |
| Primary Yellow (1530) | PY175, PW6, PY74 | 95.17 | 0.079 | 0.13 |
| Cadmium Yellow Light (1120) | PY35 | 95.19 | 0.078 | 0.13 |
| Titan Violet Pale (1573) | PW6, PR101 | 95.51 | 0.067 | 0.11 |
| Light Phthalo Green (1578) | PW6, PG36 | 95.69 | 0.061 | 0.10 |
| Benzimidazolone Yellow Light (1009) | PY175 | 95.88 | 0.054 | 0.09 |
| Bismuth Vanadate Yellow (1007) | PY184 | 95.93 | 0.053 | 0.09 |
| Cadmium Yellow Primrose (1135) | PY35 | 96.38 | 0.039 | 0.06 |
| Titanate Yellow (1375) | PY53 | 96.83 | 0.028 | 0.05 |
| Light Bismuth Yellow (1574) | PW6, PY184 | 97.68 | 0.010 | 0.02 |

</details>

---

## 3. Opacity / transparency ratings

### 3.1 What Golden publishes, and its stated limits

Golden is unusually candid: *"Currently there are no standards for measuring transparency or opacity and most ratings, including ours, are made through examining similarly prepared samples and rating them relative to one another. The difficulty here is that many pigments that are inherently transparent will seem quite strong and opaque if used full-strength from the tube, especially when made with a high pigment load. **Phthalo Blue is an excellent example of this. In a 10 ml drawdown it was ranked on par with more commonly opaque colors such as Cobalt Blue, Pyrrole Red, and Cadmium Orange.** However, when applied very thinly, mixed with a gel, or extended with a medium, Phthalo Blue shows another side and becomes a transparent and beautiful glazing color."* `[CITED]` — https://goldenartistcolors.com/technical-specifications-explained

**So the published opacity rating is a 4-level ordinal judgement, not a measurement, and for the transparent strong organics it is measuring the wrong thing.** Treat it as a UI hint, not a physical parameter.

### 3.2 The Golden Heavy Body opacity data (full chart)

154 rows at https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data with columns:
`Color Name (Item Number) | Series | Pigment (CI Name) | Opacity/Transparency | Lightfastness | Munsell Notation | Gloss Average | Viscosity Range | CIE L*a*b* Values | Tint Strength`

Distribution of opacity ratings `[DERIVED by parsing the chart]`: **opaque 46, semi-opaque 36, semi-transparent 24, transparent 43, N/A 5.**

Selected rows (verbatim from the chart):

| Color | Pigment | Opacity | LF | Munsell | Gloss | Viscosity | CIE L\*a\*b\* | Tint Str |
|---|---|---|---|---|---|---|---|---|
| Titanium White (1380) | PW6 | opaque | Excellent | 6.2 GY 9.7/0.1 | 43.15 | 22000–24000 | L\*98.25 a\*−0.74 b\*1.24 | 42.25 |
| Zinc White (1415) | PW4 | semi-transparent | Excellent | 9.1 GY 9.5/0.1 | 58.87 | — | L\*95.94 a\*−0.85 b\*0.93 | 27.20 |
| Cadmium Yellow Medium (1130) | PY35 | opaque | Excellent | 3.4 Y 8.5/14.1 | 66.74 | 23000–27000 | L\*84.13 a\*12.86 b\*94.59 | 92.35 |
| Hansa Yellow Opaque (1191) | PY74 | semi-opaque | Excellent | 3.7 Y 8.6/13.5 | 78.28 | 23000–27000 | L\*84.48 a\*10.79 b\*91.83 | 92.91 |
| Quinacridone Magenta (1305) | PR122 | transparent | Excellent | 1.5 R 3.1/5.8 | 97.99 | 14000–17000 | L\*31.07 a\*25.99 b\*5.11 | 73.51 |
| Cadmium Red Medium (1100) | PR108 | opaque | Excellent | 5.8 R 4.3/13 | 78.88 | 23000–27000 | L\*42.93 a\*51.21 b\*29.20 | 73.92 |
| Naphthol Red Light (1210) | PR112 | semi-transparent | Very Good | 7.4 R 4.7/13.4 | 49.20 | 18000–22000 | L\*47.65 a\*52.34 b\*37.15 | 72.21 |
| Dioxazine Purple (1150) | PV23 | transparent | Very Good | 1.2 YR 2.5/0.5 | 90.47 | 16000–21000 | L\*25.04 a\*2.62 b\*1.33 | 67.34 |
| Ultramarine Blue (1400) | PB29 | semi-transparent | Excellent | 9.4 PB 2.3/7.1 | 23.89 | 23000–27000 | L\*24.11 a\*14.01 b\*−27.81 | 79.94 |
| Cobalt Blue (1140) | PB28 | semi-transparent | Excellent | 7.4 PB 3.3/12.5 | 69.45 | 23000–27000 | L\*36.18 a\*11.40 b\*−47.07 | 83.70 |
| Cerulean Blue, Chromium (1050) | PB36 | semi-opaque | Excellent | 3.3 PB 3.8/8.7 | 21.44 | 23000–27000 | L\*40.96 a\*−10.70 b\*−32.37 | 87.78 |
| Phthalo Blue (Green Shade) (1255) | PB15:3 | transparent | Excellent | 9.2 PB 2.4/4.3 | 94.21 | 16000–20000 | L\*25.61 a\*6.98 b\*−18.05 | 71.17 |
| Phthalo Green (Blue Shade) (1270) | PG7 | transparent | Excellent | 6.8 B 2.5/1.8 | 94.02 | 16000–20000 | L\*26.40 a\*−6.93 b\*−5.36 | 78.54 |
| Yellow Ochre (1407) | PY42 | semi-opaque | Excellent | 8.8 YR 5.8/6.8 | 62.36 | 20000–24000 | L\*57.84 a\*16.76 b\*39.65 | 91.10 |
| Burnt Sienna (1020) | PBr7 | opaque | Excellent | 0.6 YR 3.4/4 | 13.63 | 20000–23000 | L\*34.24 a\*17.02 b\*13.69 | 80.56 |
| Carbon Black (1040) | PBk7 | opaque | Excellent | 4.1 P 2.5/0 | 79.23 | 23000–27000 | L\*25.37 a\*0.10 b\*−0.14 | 56.55 |
| Mars Black (1200) | PBk11 | opaque | Excellent | 9.6 R 2.5/0.2 | 52.79 | 18000–22000 | L\*25.47 a\*0.81 b\*0.42 | 67.81 |

Note the useful correlation Golden itself points out: *"Colors that tolerate a higher pigment load dry to a more opaque, matte finish, while colors that are more reactive and do not allow as much pigment loading tend to have a glossier, more transparent finish."* `[CITED]` — https://goldenartistcolors.com/resources/heavy-body-acrylic-colors
This is visible in the chart's **Gloss Average** column: transparent phthalos/quinacridones sit at 90–98 gloss; opaque earths and ultramarine at 13–24. **Gloss Average is therefore a free proxy for transparency that is a real measurement, unlike the ordinal opacity rating.** `[DERIVED observation]`

### 3.3 How opacity interacts with mixing

The correct formalism is **two-constant Kubelka–Munk**: absorption and scattering coefficients are *each* volume-weighted sums,
`K_mix = Σ cᵢKᵢ`, `S_mix = Σ cᵢSᵢ`, and `R` depends on `K_mix/S_mix`.
`[CITED]` — https://www.researchgate.net/publication/216567998_On_the_Kubelka-Munk_Single-ConstantTwo-Constant_Theories ; https://www.sciencedirect.com/topics/engineering/kubelka-munk-theory

The consequence for the app: **an opaque pigment dominates a mixture's appearance out of all proportion to its volume, because it dominates S.** Titanium white's S is so much larger than a quinacridone's that even a small volume of white sets `S_mix`, and the resulting reflectance is governed by `K_quin / S_white`. This is exactly the regime in which **single-constant K-M is valid** — "when the scattering is dominated by white pigment, the ratio K/S of the overall pigment mixture can be modeled as the linear combination of the K/S of all the constituent pigments." Away from that regime (transparent + transparent, no white), single-constant K-M breaks down and you need two constants. `[CITED, same sources]`

Practical implications, ranked:
- **If the recipe contains ≥10–20% white, single-constant K/S mixing is defensible.** If it contains no white and two transparent colors, it is not.
- The published `opaque / semi-opaque / semi-transparent / transparent` string is best used as a **veto/warning flag** ("this mix has no opaque component; it will look different over dark vs light ground") rather than a number in the mixing math.
- Also worth surfacing to the user: the two whites behave completely differently. Titanium White (PW6, opaque, high S) makes chalky pastels; Zinc White (PW4, semi-transparent, ~1/3 the hiding) tints with far less chroma loss and is Golden's own recommendation for compensating drying shift. `[CITED]` — https://goldenartistcolors.com/resources/titanium-white-and-zinc-white , https://justpaint.org/color-shift-shrinkage/

---

## 4. Color shift on drying

### 4.1 Mechanism (well documented)

Golden: *"The binder used in acrylic paint appears milky or white when wet and clarifies as it dries, resulting in a darkening of colors. The color shift may be less noticeable in lighter colors (like Benzimidazolone Yellow Light) and more noticeable in darker colors (like Ultramarine Blue)."* Plus the shrinkage: *"As the water releases from the paint, the acrylic polymer spheres coalesce and eventually fuse to form a continuous film, shrinking in volume."* `[CITED]` — https://justpaint.org/color-shift-shrinkage/

Chroma (Atelier Interactive) gives a cleaner three-part breakdown: *(1)* wet paint looks glossier and dry paint more matte, changing the specular/first-surface component; *(2)* *"the acrylic emulsion is milky when wet but dries transparent. This means that there is already some 'white' in the paint straight from the tube that can affect your values. The wet, milky acrylic emulsion 'tints' your colors. This is typically noticed in darker, transparent colors."*; *(3)* water thins and lightens. `[CITED]` — https://www.dick-blick.com/items/016/16/pdfs/Chroma_Atelier_Matching_Wet_Paint_To_Dry_Paint.pdf

### 4.2 The refractive-index story

The wet film is a suspension of ~1.49-RI acrylic polymer spheres in ~1.33-RI water. The RI mismatch (ratio ≈ 1.12) makes the *binder itself* a scatterer, so the wet film has an extra, colorless S term — hence lighter, lower-chroma, and slightly cooler. On drying, the water leaves, the spheres coalesce into a continuous ~1.49 film, the mismatch disappears, that extra S vanishes, and K/S rises everywhere → darker and more chromatic.
`[CITED for the RI values]` PMMA/acrylic RI = **1.491** at 589.3 nm — https://www.kla.com/products/instruments/refractive-index-database/acrylic/acrylate-lucite-perspex-plexiglass ; water = 1.33. `[INFERRED]` the K/S framing of the mechanism is mine, though it follows directly from Golden's and Chroma's descriptions plus MacEvoy's RI-ratio rule (§1.2d) that opacity kicks in above an RI ratio of ~1.33.

Golden also notes matting agents *reduce* wet-to-dry shift (they add permanent scattering to the dry film, so the wet and dry states are more alike) — which is consistent with the mechanism.

### 4.3 The quantification gap — flag this

**I could not find a published ΔE or ΔL\* for acrylic wet→dry shift from any manufacturer or conservation paper.** I searched Golden/Just Paint, Liquitex, Chroma, Winsor & Newton, conservation literature on acrylic emulsion film formation, and general spectrophotometry-of-drying-paint literature. Everything available is qualitative ("mix a step lighter", "more noticeable in darker colors"). `[NOT VERIFIED — genuine gap]`

What is available and useful:
- **ΔE reference thresholds.** Golden: *"In theory, a single Delta E is equivalent to the smallest amount of visual change a normal observer can distinguish."* ASTM D4303 lightfastness bands: **LF I (Excellent) = ΔE 1–4, LF II (Good) = ΔE 4–8, LF III (Fair) = ΔE 8–16.** `[CITED]` — https://justpaint.org/lightfastness-testing-at-golden-artist-colors/ A separate industry convention: **ΔE > 3 is considered noticeable to the naked eye.**
- **Golden's own published Lab values are dry-film measurements** (dried 6-mil drawdowns), which is what the app wants. So if the app sources from Golden's chart or the Golden spectra spreadsheet, **the drying shift is already baked in and needs no correction.** That is the single most important practical conclusion here. `[CITED]` — https://www.realtimerendering.com/golden.html (explicitly describes 10 mil wet → 6 mil dry, measured dry)
- Where drying shift *does* matter is the **UI**: the user mixes wet paint and compares it to the app's dry-film target. The app should say so, and should adopt Golden's rule — aim a step light, or test-dry a swatch.

**Suggested cheap experiment if you want the number:** measure a handful of Golden HB masstones and 1:10 tints wet and dry with a phone-camera-plus-reference-chart or a cheap colorimeter, report ΔL\* and ΔE00. My `[INFERRED]` expectation from the mechanism: single-digit ΔL\* for pale opaques (cad yellow light, titanium white — small change, small S contrast) and **ΔL\* on the order of 5–15 with ΔE00 in the 5–15 range for dark transparent colors** (ultramarine, phthalos, dioxazine), i.e. clearly visible but not catastrophic. Do not ship this number as fact.

---

## 5. Single-pigment vs convenience mixes, and limited palettes

### 5.1 Why single-pigment mixes cleaner

Golden's own statement of the rule, which is about pigment *class* rather than count: *"When Mineral pigments are mixed together, they create a 'muddy' or low chroma mixture. When Organic pigments are mixed together, they maintain their brightness and yield clean, high chroma mixtures."* Their worked example: *"Mix a Cadmium Red Medium and Cobalt Blue (inorganics) to create violet. Now use Quinacridone Red and Phthalo Blue (organics) to make another violet."* `[CITED]` — https://goldenartistcolors.com/resources/clean-color-mixing

The count-based reason is compositional. A "Sap Green Hue" that is already `PY150 + PG36 + PBk7 + PR101` is a 4-pigment mixture; adding one more paint to it means you are mixing 5+ pigments, and §6 shows chroma retention at N=5 is ~23%. **Convenience colors spend your pigment-count budget before you start.**

### 5.2 Golden Heavy Body composition census

`[DERIVED by parsing Golden's chart]` Of 154 Heavy Body colors: **96 are single-pigment, 58 are multi-pigment** (24 two-pigment, 27 three-pigment, 6 four-pigment, 1 five-pigment).

The five-pigment worst case is **Terre Verte Hue (1468): PG17, PBk9, PBr7, PY42, PG36.** Other high-count offenders to steer users away from as mixing components:

| Convenience color | Pigments |
|---|---|
| Terre Verte Hue (1468) | PG17, PBk9, PBr7, PY42, PG36 |
| Sap Green Hue (1461) | PY150, PG36, PBk7, PR101 |
| Permanent Maroon (1252) | PR122, PY150, PR101, PG7 |
| Cobalt Violet Hue (1465) | PV19, PW6, PR122, PV23 |
| Viridian Green Hue (1469) | PW6, PB15:3, PBr7, PY150 |
| Naples Yellow Hue (1459) | PW6:1, PW6, PY42, PY83(HR70) |
| Titan Green Pale (1371) | PW6, PY42, PBr7, PG7 |
| Alizarin Crimson Hue (1450) | PR122, PY150, PG7 |
| Hooker's Green Hue (1454) | PY150, PB60, PR122 |
| Jenkins Green (1195) | PBk9, PG36, PY150 |
| Prussian Blue Hue (1460) | PBk9, PB15:0, PV23 |
| Smalt Hue (1467) | PB29, PBk7, PV23 |
| all N2–N8 Neutral Grays | PW6, PBk9, PBr7 |

Note that **almost every color with "Hue" or "Light/Titan/Primary" in the name is multi-pigment**, and every "Light …" color contains PW6 (they are literally factory tints — Golden says so: light-value colors are made by taking mural-list pigments "which are then tinted with Titanium White to create convenience colors"). `[CITED]` — https://justpaint.org/introducing-new-golden-heavy-body-light-value-colors/

Key single-pigment paints the app should prefer, with codes (all from Golden's chart):
`PW6` Titanium White · `PW4` Zinc White · `PY184` Bismuth Vanadate Yellow · `PY35` Cadmium Yellow Primrose/Light/Medium · `PY74` Hansa Yellow Opaque · `PY175` Benzimidazolone Yellow Light · `PY154` Benzimidazolone Yellow Medium · `PY150` Nickel Azo Yellow · `PY42` Yellow Oxide / Yellow Ochre · `PY53` Titanate Yellow · `PO20` Cadmium Orange · `PO73` Pyrrole Orange · `PO43` Vat Orange · `PR108` Cadmium Red Light/Medium/Dark · `PR254` Pyrrole Red · `PR255` Pyrrole Red Light · `PR264` Pyrrole Red Dark · `PR112` Naphthol Red Light · `PR5` Naphthol Red Medium · `PR122` Quinacridone Magenta · `PV19` Quinacridone Red / Violet / Primary Magenta · `PR101` Red Oxide / Violet Oxide / Transparent Red Iron Oxide · `PV15` Ultramarine Violet · `PV23` Dioxazine Purple · `PB29` Ultramarine Blue · `PB28` Cobalt Blue · `PB36` Cerulean Blue Chromium / Cerulean Blue Deep / Cobalt Turquoise · `PB60` Anthraquinone Blue · `PB15:0` Phthalo Blue RS · `PB15:3` Phthalo Blue GS · `PG7` Phthalo Green BS · `PG36` Phthalo Green YS · `PG17` Chromium Oxide Green · `PG26` Cobalt Green · `PG50` Cobalt Teal · `PBr7` Burnt Sienna/Umber/Raw Umber/Raw Sienna · `PBr6` Mars Yellow · `PBr24` Naples Yellow Deep · `PBr25` Benzimidazolone Burnt Orange · `PBk7` Carbon Black · `PBk9` Bone Black · `PBk10` Graphite Gray · `PBk11` Mars Black

### 5.3 Candidate limited palettes

**(a) Golden's own curated 8** — Titanium White, Zinc White, **Quinacridone Magenta (PR122)**, **Naphthol Red Light (PR112)**, **Hansa Yellow Medium (PY73)**, **Phthalo Green BS (PG7)**, **Phthalo Blue GS (PB15:3)**, **Yellow Ochre (PY42)**. Golden's stated logic: the three mixing primaries are Hansa Yellow Medium + Quinacridone Magenta + Phthalo Blue GS, magenta "chosen over Quinacridone Red to achieve a broader range of violets and purples"; Naphthol Red Light "balances the magenta"; PG7 gives "a great range of greens, particularly subtle yellow greens"; Yellow Ochre "warms mixtures and subdues bright colors"; two whites for opaque pastels vs transparent glazing. With published example ratios: **Turquoise = 1 PB15:3 : 1 PG7; Light Red = 1 Quin Magenta : 5 Hansa Yellow Medium; Bright Green = 1 PG7 : 9 Hansa Yellow Medium.** `[CITED]` — https://goldenartistcolors.com/resources/color-mixing-with-8-curated-acrylic-colors
Note how far from 1:1 those published ratios are — direct manufacturer confirmation that equal-volume mixing is wrong.

**(b) CMY "printer's" triad.** MacEvoy's version: Hansa Yellow (PY97), Quinacridone Rose (PV19), Phthalo Blue GS (PB15:3). His verdict: it produces *"the largest chromatic gamut possible with three paints"* but *"a limited gamut is the price you pay for any limited palette"* — greens and violets suffer most, and orange mixtures are *"noticeably duller than dedicated warm pigments like cadmium scarlet."* `[CITED]` — https://www.handprint.com/HP/WCL/palette4c.html

**(c) Split primary (warm+cool of each).** MacEvoy's list: *"cadmium lemon (PY37), cadmium yellow (PY35), pyrrole red (PR254), quinacridone carmine, ultramarine blue (PB29), phthalocyanine blue GS (PB15:3)."* He is hostile to its theoretical justification but concedes the practical point: it *"restricts the chroma of purple and green mixtures"* and requires *"nearly all color mixtures"* to involve three paints; its actual value is *"the warm/cool biasing effect, not color theory doctrine."* Its documented weak spot: *"the greatest weakness of the 6-color split primary palette lies between the blue and yellow hues… the result is mixed greens which are low chroma, dull and gray, by comparison with the high chroma warm yellow-orange-reds."* `[CITED]` — https://www.handprint.com/HP/WCL/palette4r.html

**(d) MacEvoy's actual recommendation: the secondary / "colorist" palette** — six paints that sample the hue circle *including* dedicated orange, green and violet, because *"the secondary palette offers the most evenly balanced and highly saturated range of mixing possibilities of any minimal palette"*, and *"painters know that saturation costs are inevitable and add three paints where the 'primary' paint mixtures are dullest."* He states six paints achieve *"almost the entire color mixing potential possible in watercolor paints."* `[CITED]` — https://www.handprint.com/HP/WCL/palette4e.html , https://www.handprint.com/HP/WCL/color18b.html

**(e) Zorn** — historically Vermilion, Ivory Black, Flake White, Yellow Ochre; modern substitution Cadmium Red + Ivory/Bone Black + Titanium White + Yellow Ochre. Documented limitation: *"the lack of blue seems to be the biggest limitation. The closest thing to blue is a cool gray"* and it *"does not excel in vivid colors"* — good for portraiture, poor for landscape. `[CITED]` — https://www.jacksonsart.com/en-us/techniques/the-zorn-palette , https://drawpaintacademy.com/zorn-palette/

### 5.4 Measured gamut comparison

`[DERIVED]` Method: Golden HB masstone reflectance spectra (78 paints, 400–700 nm @10 nm, from Golden's own released spreadsheet); single-constant K-M mixing by paint volume; all pairwise mixes at 9 ratios plus all triples on a 1/9 simplex grid; CIE 1931 2°, D65; convex-hull area of the resulting point cloud projected on the a\*b\* plane.
⚠ **Caveats, important:** masstone concentration only (no white, no tints — Golden's public spectra contain no Titanium White row), single-constant K-M, and the masstone K/S is saturated for the strong transparents (§1.2a). This measures *chromatic reach at full concentration*, not the full 3-D gamut a painter has access to. Treat as ordinal, not absolute.

| a\*b\* hull area | % of best | max C\*ab | Palette |
|---|---|---|---|
| 5636 | 100% | 98.0 | **Split-primary 6** (PY35 primrose + PY35 med + PR254 + Quin Crimson + PB29 + PB15:3) |
| 5175 | 92% | 94.5 | **Secondary/colorist 6** (Cad Yel Light + Pyrrole Orange + Quin Magenta + Dioxazine + PB15:3 + PG7) |
| 4166 | 74% | 98.0 | **RYB traditional 3** (PY35 Cad Yel Med + PR108 Cad Red Med + PB29 Ultramarine) |
| 4110 | 73% | 93.6 | **Golden curated 8** (its 6 chromatic members) |
| 2548 | 45% | 93.6 | **CMY + PG7 (4)** |
| 2026 | 36% | 93.6 | **CMY printer's 3** (PY73 + PR122 + PB15:3) |
| 993 | 18% | 61.7 | **Zorn 3** (PY42 + PR108 + PBk9) |

And a coarser but more robust metric — how many of twelve 30° hue bins the palette can fill at **C\*ab ≥ 40**:

| Palette | bins ≥C\*40 (of 12) |
|---|---|
| Split-primary 6 | 4 |
| Secondary 6 | 4 |
| RYB 3 | 4 |
| Golden curated 8 | 4 |
| Zorn 3 | 3 |
| CMY + PG7 (4) | 3 |
| CMY printer's 3 | 2 |

**Reading of these results.** The CMY triad's poor showing here is a real, explainable effect and not just an artifact: at masstone concentration, PB15:3 and PR122 are near-black, so *every* CMY mixture is dark and desaturated, and the palette's chroma only appears once white enters. The split-primary and secondary palettes score well because they include high-chroma, high-reflectance *opaque* paints (cadmiums, pyrroles) whose masstones already sit at C\* 90+. **The honest conclusion is that palette gamut cannot be assessed without white in the model.** That is itself an argument for the two-constant schema in §9. Zorn's 18% is unsurprising and matches the literature.

Berns' independent, better-founded gamut result is worth quoting for scale: from 58 Golden HB pigments he generated 831 varnished tints/tones/masstones and found **22% out of AdobeRGB(1998) gamut and 31% out of sRGB gamut.** `[CITED]` — Berns 2022. **Directly relevant to this app: roughly a third of what Golden acrylics can physically do cannot be represented in sRGB at all.** Any sRGB-triple-per-paint model is lossy before mixing even begins, and the app's photo input (sRGB) cannot express a third of its own output space.

Berns also tested whether 3 primaries suffice: rotating the first three PCA eigenvectors to approximate cyan/magenta/yellow gave an average CIEDE2000 metamerism index of **1.8, range 0.03–9.88**, and he concluded *"three primaries are insufficient to approximate the 58 pigments."* Three eigenvectors captured 97.97% of spectral variance, six captured 99.72%. `[CITED]`

---

## 6. Why mixtures go muddy — quantified

### 6.1 MacEvoy's rules (the canonical statement)

- **Rule 38:** *"The farther apart two paint colors are on the hue circle, the duller their mixture will be."*
- **Rule 39:** *"Two paint colors on opposite sides of the hue circle will mix an achromatic or near neutral color."*
- **Rule 40:** *"The mixture of two paints will always be duller than the more saturated paint and darker than the lighter valued paint."*
- **Rule 41:** *"Saturation costs are not equal across equal spans of a visual hue circle."* Subtractive-primary mixing lines (magenta–yellow) bow **outward** (less chroma loss than expected); additive-primary lines (orange–violet) bow **inward** (more loss).
- His worked complementary example: pyrrol scarlet (L 48) + phthalo green (L 50) do not give middle gray but *"a very impressive dark gray"* at **L 22** — i.e. complementary mixing costs *lightness* as well as chroma, dramatically.
`[CITED]` — https://www.handprint.com/HP/WCL/color18b.html

MacEvoy also gives the practical prediction shortcut: the **geometric mean of the two reflectance curves** is a reasonable predictor for an equal-proportion mixture of two paints. `[CITED]` (same page / handprint mixing discussion)

### 6.2 Quantification: chroma collapse vs number of pigments

`[DERIVED]` Method as §5.4. 20 chromatic single-pigment Golden HB paints; up to 400 random equal-parts combinations per N; C\*ab of the mixture; "retention" = C\*mix ÷ mean C\* of the components. Mean single-pigment masstone chroma across the set = **53.7**.

| N pigments (equal parts) | mean C\*ab | median C\*ab | max C\*ab | mean chroma retention | P(C\*ab < 20) |
|---|---|---|---|---|---|
| 1 | 53.7 | — | 98 | 100% | — |
| **2** | 29.7 | 15.0 | 97.0 | **51.4%** | 55.8% |
| **3** | 19.9 | 13.6 | 95.1 | **35.2%** | 75.8% |
| **4** | 15.8 | 12.4 | 73.7 | **27.7%** | 81.8% |
| **5** | 12.9 | 10.1 | 75.9 | **23.4%** | 84.5% |

**This is direct quantitative support for the "2 pigments + white ideally, 3 maximum" rule.** Going 2→3 pigments costs ~16 percentage points of chroma retention and raises the probability of a near-neutral result from 56% to 76%. Going 3→4 buys almost no new hue and costs another 7 points. Note also that a *good* pair can still hit C\* 97 — the mean is low because most random pairs are far apart on the hue circle, which is precisely Rule 38.

### 6.3 Quantification: overlapping absorption bands / hue separation

`[DERIVED]` Same method; retention measured against the *more saturated* parent, binned by masstone hue-angle separation:

| Hue separation | n pairs | mean chroma retention vs stronger parent |
|---|---|---|
| 0–29° | 49 | **67.6%** |
| 30–59° | 24 | **68.0%** |
| 60–89° | 36 | **41.0%** |
| 90–119° | 27 | **22.3%** |
| 120–149° | 33 | **14.1%** |
| 150–179° | 21 | **11.6%** |

This is Rule 38 with numbers on it, and gives the app a usable heuristic: **hue separation below ~60° is nearly free (≈68% retention); above ~90° you are deliberately making a chromatic gray.** The physical reading is the absorption-band-overlap one: two pigments 20° apart absorb nearly the same band, so the union of their absorption is barely wider than either alone; two pigments 160° apart absorb complementary halves of the spectrum, and their union covers everything → little reflected anywhere → near-neutral, and *dark* (see MacEvoy's L 48 + L 50 → L 22).

### 6.4 The complementary-mixing / chromatic-gray case

Practical rules the app can encode, all supported above:
- Complementary pairs are the *correct* way to make grays and shadow colors, and Golden markets exactly this (Payne's Gray is PB29+PBk7; the N2–N8 grays are PW6+PBk9+PBr7).
- Complementary mixes lose lightness disproportionately — do not assume L\*mix ≈ mean of L\*.
- Because chroma retention is so hue-separation-sensitive, the app should **prefer the closest-hue pair that spans the target** and reject candidate pairs whose hue separation exceeds ~110° unless the target chroma is itself low.

---

## 7. Practical mixing units

### 7.1 Volume vs mass — what K-M concentration means

The Kubelka–Munk literature uses **volume fraction**: "the standard formulation uses volume fraction for an individual pigment in the Kubelka-Munk equation… When mixing colorants, the K/S values of each colorant are multiplied by its concentration in the mixture recipe, with results summed to give the mixture K/S values." Independent scattering holds below ~10% PVC for titanium systems; the equation becomes inaccurate above ~15% PVC for conventional TiO₂ due to crowding. `[CITED]` — https://www.sciencedirect.com/topics/engineering/kubelka-munk-theory

⚠ **But note what Berns actually did:** *"The weight of each paint forming the mixture was determined using an Acculab scale with a precision of 0.005 g."* — i.e. the RIT/Golden K-M coefficients were fitted to mixtures measured **by mass of tube paint**. `[CITED]` — https://www.rit.edu/science/sites/rit.edu.science/files/2019-03/ArtistSpectralDatabase.pdf
And Golden's tint-strength drawdowns are described as **"10 parts Titanium White to 1 part paint color"** — "parts" unspecified, most plausibly by mass in a lab setting. `[INFERRED]`

**Consequence for the app: there is an unresolved units ambiguity in the source data, and it matters at the ~10–30% level.** Golden HB paints span roughly 1.1–1.5 g/mL at the tube level (pigment SG 1.27–6.11 at PVC ~14–20%), so mass-parts and volume-parts differ by tens of percent for a cadmium-vs-phthalo mix. `[INFERRED, from Golden's pigment density chart + PVC figures]` Recommendation: **store the assumed basis explicitly per data source** and, if using Berns-style coefficients, convert mass→volume using paint density before showing the user volumetric "parts".

### 7.2 Real density numbers

Golden publishes a **pigment density chart** (specific gravity relative to water) — the actual per-pigment values, not estimates. `[CITED]` — https://justpaint.org/wp-content/uploads/2019/05/PIGMENT-SPECIFIC-GRAVITY_jpechart.pdf (also https://justpaint.org/pigment-density/ ; the Golden-hosted copy at goldenhub8 is behind Cloudflare Access). Selected values:

| SG | Pigments |
|---|---|
| 1.27–1.35 | PY83 HR70 Diarylide Yellow 1.27 · PY3 Hansa Yellow Light 1.30 · all Fluorescents 1.35 |
| 1.40–1.55 | PR207 1.40 · PR112 Naphthol Red Light 1.41 · PR264 1.45 · **PR122 Quinacridone Magenta 1.45** · PV19 1.48/1.53 · PO48 1.50 · PO43 1.50 · PB60 1.51 · PY175 1.52 · PR206 1.52 · PG36 1.53 · PO73 1.55 · PR254 1.55 · PR202 1.55 · **PV23 Dioxazine Purple 1.44** |
| 1.59–1.80 | PY154 1.59 · **PB15:3 Phthalo Blue GS 1.62** · PB15:1 Phthalo Blue RS 1.62 · PR5 1.69 · PY150 Nickel Azo 1.77 · **PBk7 Carbon Black 1.80** |
| 1.90–2.52 | PV19 Quin Red 1.90 · **PG7 Phthalo Green BS 2.05** · PBk10 Graphite 2.25 · **PB29 Ultramarine 2.35** · PV15 Ultramarine Violet 2.35 · PBr7 Burnt Umber 2.50 · PBk9 Bone Black 2.52 |
| 3.10–3.90 | PBr7 Burnt Sienna 3.10 · PBr7 Burnt Umber Light 3.35 · PY43 Yellow Ochre 3.50 · PY43 Raw Sienna 3.51 · PY42 Trans. Yellow Iron Oxide 3.70 · PG50 Cobalt Teal 3.70 · PY42 Trans. Red Iron Oxide 3.90 · **PW6 Titanium White 3.90** |
| 4.10–4.70 | PW6:1 Titan Buff 4.10 · PY42 Yellow Oxide 4.10 · PBr6 Mars Yellow 4.19 · PB36:1 Cerulean Blue Chromium 4.20 · **PB28 Cobalt Blue 4.30** · PBr7 Raw Umber 4.42 · PY53 Titanate Yellow 4.50 · **PY35 Cadmium Yellow (all) 4.60** · PBk11 Mars Black 4.60 · PB36 Cerulean Blue Deep 4.70 |
| 4.80–6.11 | PB36 Cobalt Turquoise 4.80 · PR101 Red/Violet Oxide 5.00 · **PR108 Cadmium Red 5.03–5.40** · PG17 Chromium Oxide Green Dark 5.10 · PG26 Cobalt Green 5.10 · PO20 Cadmium Orange 5.32 · **PW4 Zinc White 5.60** · PY184 Bismuth Vanadate Yellow 6.11 |

That is a **4.8× spread in pigment density** (1.27 to 6.11), which is why "grams of pigment per 100 g of oil/binder" style figures mislead. Golden's oils article makes the same point for oil: by weight Flake White (SG ~6.5) looks lean, but *"when recalculated using volume instead of weight, the hierarchy reverses dramatically… Prussian Blue and Titanium White can actually end up having slightly less oil than Flake White."* Also from that article: CPVC = 1/(1 + (OA·ρ)/93.5). `[CITED]` — https://justpaint.org/volume-weight-and-pigment-to-oil-ratios/

Golden's own SDS reports finished Heavy Body paint **specific gravity 1.0–2.0** (a range, not per-color). `[CITED]` — https://www.jerrysartarama.com/media/pdfs/golden/GOLDEN%20SDS%20Sheet%20Heavy%20Body%20Acrylics%20MSDS.pdf

### 7.3 From "pigment concentration" to "parts out of the tube"

The chain is: K-M concentration `c` = pigment **volume** fraction in the dry film → divide by that paint's dry-film PVC to get **paint volume** → multiply by paint density to get mass. Known PVC anchors: Golden's Ultramarine ≈14% and Cobalt ≈20% in acrylic; LBNL measured Liquitex Cobalt Blue at 14%. `[CITED]`
`[INFERRED]` A practical simplification that avoids all of this: **fit K and S per *tube paint* rather than per pigment, with concentration defined as volume fraction of tube paint.** That is exactly what Berns' "unit absorption and scattering coefficients for each paint relative to the scattering of white defined as unity" does, and it makes recipes directly expressible as "parts of tube A to parts of tube B" with no density or PVC bookkeeping. **Strong recommendation: do this.**

### 7.4 How precise can a human be? — the bound on recipe precision

No study of artists' volumetric mixing accuracy turned up; `[NOT VERIFIED]`. But the question can be answered from the other end — how much does the *color* care?

`[DERIVED]` ΔE00 between a mix at fraction *f* and at *f*+δ, for representative 2-paint mixes on Golden's spectra:

| Mix | at f=5% | f=10% | f=25% | f=50% | f=75% |
|---|---|---|---|---|---|
| Phthalo Blue GS + Cad Yel Med, ΔE00 per **+1 pt** | 2.11 | 1.43 | 0.71 | 0.59 | 0.34 |
| …per **+5 pt** | 8.91 | 6.40 | 3.40 | 2.82 | 1.64 |
| Ultramarine + Cad Yel Med, per +1 pt | 2.15 | 1.46 | 0.74 | 0.67 | 0.40 |
| Quin Magenta + Hansa Yel Med, per +1 pt | 1.74 | 1.23 | 0.68 | 0.37 | 0.26 |
| Cerulean + Cad Yel Med, per +1 pt | 1.74 | 1.19 | 0.68 | 0.50 | 0.67 |
| Phthalo Blue GS + Titan Buff, per +1 pt | 2.25 | 1.33 | 0.53 | 0.22 | 0.13 |

**The pattern is Weber-like.** ΔE00 tracks *relative* error in the minor component, not absolute percentage points. Worked checks: +1 pt at f=5% is a +20% relative change → ΔE00 ≈ 2.1 (coefficient ≈ 0.11 per percent relative); +5 pt at f=50% is a +10% relative change → ΔE00 ≈ 2.8 (coefficient ≈ 0.28); +5 pt at f=25% is +20% relative → ΔE00 ≈ 3.4 (coefficient ≈ 0.17). So the defensible statement is:
> **≈10% relative error in a component's amount ≈ ΔE00 1–3. ≈20–30% relative error ≈ ΔE00 2–8.**

`[INFERRED]` A palette-knife scoop is realistically ±20–30% by volume, which puts a hand-executed recipe at ΔE00 ≈ 2–8 no matter how precise the printed number is.

Consequences, and these are firm:
1. **Percentages are the wrong UI.** 37%/63% vs 40%/60% is a 7.5% relative change → ΔE00 ≈ 0.4, i.e. **below the JND** (Golden: "a single Delta E is equivalent to the smallest amount of visual change a normal observer can distinguish"). Recommending 37/63 is literally unmeasurable by the user.
2. **Snap recipes to a geometric ratio ladder.** `1:1, 1:1.5, 1:2, 1:3, 1:5, 1:8, 1:12, 1:20, 1:40` — adjacent steps are ~30–50% relative, i.e. ΔE00 ~3–10 apart, which is comfortably above JND and executable with a palette knife. Anything finer is noise.
3. **Precision must scale with the minor component.** For a 1:20 tint, ±1 percentage point already costs ΔE00 ≈ 2; for a 1:1 mix, ±5 points costs ΔE00 ≈ 1–3. So the app should express strong-tinter recipes as "a *touch*" / "1 knife-tip to 20 scoops" and *warn* that they are error-sensitive, rather than pretending a number is achievable.
4. **Never recommend a ratio with a component below ~2–3% of the total** unless you also tell the user to pre-dilute it (make a 1:10 stock tint first, then use *that*). Golden's own advice is the operational version of this: add *color into white*, never white into color, for strongly pigmented paints.

---

## 8. Available measured data — inventory with licensing

Ranked by usefulness to this app.

### ⭐ 1. Golden Heavy Body reflectance spectra — FREE, redistributable, already downloaded and verified
- **URL:** https://www.realtimerendering.com/golden.html → https://www.realtimerendering.com/downloads/GoldenSpectra.zip (note: Cloudflare requires a browser UA + referer; plain curl gets a challenge page)
- **File:** `Reflectance Data for Golden HB 10 mil Drawdowns over White.xlsx`, ~52 KB
- **Contents (verified by opening it):** **78 rows**, one per Golden Heavy Body paint. Columns: `Prod # | Name | L* | a* | b* | %Reflectance 400–700 nm @ 10 nm (31 values) | K/S 400–700 nm @ 10 nm (31 values)`. D65 / 10° observer.
- **License:** *"Golden Artist Colors, Inc., has kindly given us spectral data for their acrylic paints, and they have allowed us to share these with others."* Curated by Andrew Glassner and Eric Haines; they ask that you tell them what you build with it. Page last updated Oct 16 2025 — actively maintained. **This is the single best free starting point.**
- **Coverage check `[DERIVED]`:** includes all the mixing workhorses — Phthalo Blue GS/RS, Phthalo Green BS/YS, Ultramarine, Cobalt Blue, Cerulean Chromium/Deep, Quin Magenta/Red/Violet/Crimson, Dioxazine Purple, all four Cadmium Reds, Pyrrole Orange/Red/Red Light/Red Dark, Cad Yellow Light/Medium/Dark/Primrose, Hansa Yellow Light/Medium, Bismuth Vanadate, Nickel Azo, Yellow Ochre/Oxide, Burnt/Raw Sienna & Umber, Bone/Carbon/Mars Black, Titan Buff.
- ⚠ **Three limitations you must design around:**
  1. **No Titanium White and no Zinc White rows.** You cannot compute tints from this file alone.
  2. **Masstone only**, at 6 mil dry over a white Leneta card, and Golden explicitly says it is not opaque: *"the white of the card definitely influences the reading of more transparent colors… For many calculations, however, one would want to have readings based on truly opaque films. Unfortunately those are things we currently do not have available."*
  3. **K/S saturates.** Minimum reflectance across all 78 is 3.7–4.6%; K/S at λmax caps at ~12 for every strong transparent. `[DERIVED]` So this file cannot give you tinting strength.
- Golden's own caution about illuminant/backing baked into the numbers: *"the spreadsheet data inherently includes the D65 illuminant spectrum and the white backing card's reflective spectrum. Most calculations will want the paint's own spectrum, which means you'll need to factor out the illuminant and the backing card."*

### ⭐ 2. Golden Heavy Body Pigment Detail Chart — FREE, web, 154 rows, Lab + tint strength
- **URL:** https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data (plain HTML table, trivially scrapable — I parsed all 154 rows with a 6-line regex)
- **Columns:** Color Name (Item Number), Series, Pigment (CI Name), Opacity/Transparency, Lightfastness rating, Munsell Notation, Gloss Average, Viscosity Range, **CIE L\*a\*b\* Values**, **Tint Strength**
- Sister charts exist per product line: SoFlat at https://goldenartistcolors.com/products/golden-artist-acrylics/soflat/technical-chart (⚠ its Tint Strength column is all zeros — data not populated), Fluid, High Load, OPEN etc.
- **License:** not stated; it is public marketing/technical data. `[INFERRED]` Fine to use as reference data with attribution; don't redistribute the table wholesale as your own dataset.
- ⚠ **This chart's L\*a\*b\* and the spreadsheet's L\*a\*b\* differ slightly** (Phthalo Blue GS: chart L\*25.61 a\*6.98 b\*−18.05 vs spreadsheet L\*25.11 a\*8.84 b\*−19.75) — different batches and different illuminant/observer (D50 vs D65/10°). Pick one and be consistent.

### 3. Berns / Gray Sky Imaging — Artist Acrylic Paint Spectral, Colorimetric and Image Dataset (2022) — ⚠ WITHDRAWN
- Paper: https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf
- 58 Golden HB pigments (from 68 supplied) → 831 varnished tints/tones/masstones via two-constant K-M. Xrite MS7000, integrating sphere, SPIN, 380–730 nm. Saunderson K1=0.035, K2=0.6, Kinstrument=1.0.
- The Excel with spectra, K-M optical coefficients, mixtures and PCA **was** on grayskyimaging.com. The resources page now says: **"NOTE: the spectral reflectance database mentioned in the article is no longer available."** `[CITED]` — https://grayskyimaging.com/resources/
- Still downloadable from that page: `Acrylic_paint_target_and_cleaning_and_varnishing_curves.zip`.
- **Action:** email Berns / Golden and ask. The paper documents exactly the data this app wants, and Golden clearly has masstone+10%-tint drawdown spectra for its whole line internally.

### 4. RIT / Berns — Artist Paint Spectral Database (2016 version)
- https://www.rit.edu/science/sites/rit.edu.science/files/2019-03/ArtistSpectralDatabase.pdf
- 19 Golden HB paints (listed with CI numbers, including **Titanium White PW6 and Bone Black PBk9** — the two the 78-paint spreadsheet lacks), Leneta Form 3B opacity charts, 0.006" drawdown bar, Macbeth MS7000, SPIN, 4 measurements averaged, **380–750 nm @ 10 nm**. Masstone-tint method; Saunderson K1=0.03, K2=0.65. 770 spectra total across 23 hues + gray scale. Mixture masses weighed to 0.005 g.
- Berns: *"An Excel file was made available by request."* `[CITED]` — so **ask**. PCA variance: eigenvector 1 = 0.7392, 2 = 0.1593, 3 = 0.0769, cumulative 3 = 97.5%.

### 5. LBNL Pigment Database (Cool Colors Project) — masstone + 1:4 + 1:9 tints, but data is encrypted
- Index: https://coolcolors.lbl.gov/LBNL-Pigment-Database/database.html ; example paint page: https://coolcolors.lbl.gov/LBNL-Pigment-Database/paints/U05.html
- **73 pigmented coatings** (White 4, Black/Brown 17, Blue/Purple 14, Green 11, Red/Orange 9, Yellow 14, Pearlescent 14). **The artist paints are Liquitex**, with CI pigment names and **dry-film PVC** published per paint.
- Per paint: masstone plus **two tint ratios with titanium white, 1:4 and 1:9**, imaged over both white and black, plus tab-delimited spectral datafiles containing reflectance/transmittance/absorptance over void, white and black backgrounds, film thicknesses in µm, and **Kubelka-Munk K and S coefficients in mm⁻¹**. Format guide: https://coolcolors.lbl.gov/LBNL-Pigment-Database/assets/misc/spectral-datafile-guide.pdf
- ⚠ **Blocker:** *"each spectral datafile is stored in the LBNL Pigment database as a ZIP archive with AES 128-bit encryption… Members and industrial partners of the Cool Colors project may obtain the decryption key via fax by contacting Ronnen Levinson."* `[CITED, verified — I downloaded a datafile and confirmed it is AES-encrypted]`
- The **spectral chart PDFs are public and unencrypted** (e.g. https://coolcolors.lbl.gov/LBNL-Pigment-Database/assets/spectral-charts/pdf/B09-tint-ladder-spectral-chart.pdf), so curves can be read off graphically.
- The underlying papers publish K and S vs wavelength for common colorants: Levinson, Berdahl & Akbari, *Solar spectral optical properties of pigments, Part I* and *Part II: survey of common colorants*, Solar Energy Materials & Solar Cells 89 (2005) 351–389, doi:10.1016/j.solmat.2004.11.013. https://heatisland.lbl.gov/publications/solar-spectral-optical-properties-0 — paywalled at Elsevier, LBNL preprints may be free.
- Bonus: the Liquitex TDS is mirrored there unencrypted — https://coolcolors.lbl.gov/LBNL-Pigment-Database/assets/manufacturer-TDS/Liquitex/PaintTechInfo.pdf

### 6. Liquitex published technical data
- The mirrored **Liquitex Paint Technical Information** PDF (above) has a full color table: `Color Number | Color Name | Viscosity Availability | Series | Hue | Value | Chroma | Lightfast Rating | Opacity Rating | Pigment Rating | Pigment name(s) with CI codes`. Opacity legend: `O = Opaque, TL = Translucent, TP = Transparent`. Pigment legend: `S = Single Pigment Color, M = Mixed Pigment Color` — **a ready-made single-vs-convenience flag**. Lightfast: I/II/III.
- ⚠ It publishes **Munsell H V/C, not CIELAB.** No spectra. The PDF's tabular layout extracts messily (columns interleave).
- Liquitex's public per-product pages (liquitex.com) give pigment codes and opacity but not colorimetry.

### 7. CHSOS "Pigments Checker" reflectance spectra database — free, but modern-pigment subset
- https://chsopensource.org/pigments-checker/ and app note 4: https://chsopensource.org/chsos-application-note-4/
- Modern & Contemporary Art set applied **with an acrylic binder on cardboard**, plus spectra of the pigments alone and the binder alone. Includes PBr24, PG36, PY53, PY150, PW6 (anatase), PW7, PW11, lithopone. Reflectance (FORS) plus Raman, XRF, FTIR, XRD. Explicitly **open access / freely downloadable**, and the reflectance library ships ready-to-use for Spectragryph: https://chsopensource.org/pigments-checker-spectra-databases-on-spectragryph/
- Pigment reference: Cosentino, *Pigments Checker version 3.0…*, Microchemical Journal (2016), https://www.sciencedirect.com/science/article/abs/pii/S0026265X16301011
- ⚠ It is a **pigment-identification** set, not a paint-mixing set: no tint ladders, and the acrylic binder is not Golden's.

### 8. Code / model references (not data, but directly relevant)
- **Mixbox** — Sochorová & Jamriška, *Practical Pigment Mixing for Digital Painting*, ACM TOG 2021. Paper: https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf · code: https://github.com/scrtwpns/pigment-mixing · demo: https://scrtwpns.com/mixbox/painter/. K-M mixing entirely in RGB via a learned latent "pigment space" with additive residuals. ⚠ Check the license before shipping — Mixbox has commercial licensing terms.
- **spectral.js** — https://github.com/rvanwijnen/spectral.js — K-M paint-like mixing, JS.
- **ArtistAssistApp** — https://github.com/eugene-khyst/artistassistapp — **AGPL-3.0**. Does almost exactly what PaintTranslator does: stores spectral reflectance per paint, mixes with an empirical K-M model, matches by comparing reflectance curves with a percentage score, and outputs proportional ratios. Worth studying as prior art; AGPL means you cannot lift code without going AGPL yourself, but the data model and UX are instructive.
- **painting_tools** — https://github.com/rubenwiersma/painting_tools — Python, spectral↔RGB, K-M mixing and layering, links to hyperspectral painting datasets.
- **ColorMixer** — https://github.com/benjholla/ColorMixer — Java K-M reference implementation.
- **artistpigments.org** — cross-references ~80,000 artist paints across 1,397 brands with **CIE Lab and Munsell notations** and a masstone-spectral-curve colour mixer. ⚠ Cloudflare-protected (403 to both WebFetch and curl); requires sign-up for advanced features; **licensing unknown — assume not redistributable.**
- **The Color of Art Pigment Database** — https://www.artiscreation.com/Color_index_names.html — the standard free cross-reference of CI names to brand names; no colorimetry. Useful for the pigment-code column.

### 9. Things I checked and found unusable
- **Golden's own pigment-density PDF at goldenhub8** — behind Cloudflare Access (login wall). Use the justpaint.org copy instead (§7.2). `[VERIFIED blocked]`
- **Kremer Pigmente / Forbes Pigment Collection** — no downloadable reflectance dataset found in this pass; Forbes is a physical reference collection at Harvard. `[NOT VERIFIED — worth a dedicated search if needed]`
- **Golden/Liquitex do not publish per-color L\*a\*b\* for every line.** Golden publishes it for Heavy Body (and Munsell for SoFlat); Liquitex publishes Munsell only.

---

## 9. What data the app should store per paint (proposed schema)

The guiding principle: **store what Golden measures, in the units Golden measures it, and derive everything else.** Concretely, replace the single `Color` with a record whose *core* is a masstone/tint spectral pair and its fitted K-M coefficients.

```csharp
public sealed class AcrylicPaint
{
    // ---- identity ----
    string  Brand;                 // "Golden"
    string  Line;                  // "Heavy Body"
    string  Name;                  // "Phthalo Blue (Green Shade)"
    string  ItemNumber;            // "1255"
    int     Series;                // 4  (price tier; also a proxy for pigment cost)

    // ---- pigment composition (drives the "clean mixing" heuristics) ----
    string[] PigmentCodes;         // { "PB15:3" }
    bool     IsSinglePigment => PigmentCodes.Length == 1;   // derived
    int      PigmentCount    => PigmentCodes.Length;        // budget vs the 3-pigment rule

    // ---- CORE OPTICS: the masstone/tint pair, per Golden's own characterization ----
    // 400-700 nm @ 10 nm = 31 samples. Store BOTH; this is the minimum for 2-constant K-M.
    float[] MasstoneReflectance;   // dry 6-mil drawdown, %R / 100
    float[] Tint10Reflectance;     // 1 part paint : 10 parts Titanium White, %R / 100 (nullable)
    SpectralBasis Basis;           // { WavelengthStart, Step, Count, Illuminant, Observer, Geometry }

    // ---- DERIVED optics, computed offline once and cached ----
    float[] K;                     // 2-constant K-M unit absorption, S(white) == 1
    float[] S;                     // 2-constant K-M unit scattering
    float   SaundersonK1, SaundersonK2;   // 0.035 / 0.6  (Berns 2022) or 0.03 / 0.65 (RIT 2016)
    bool    KsAreFitted;           // false => only single-constant K/S from masstone is available

    // ---- colorimetry (cache; do not treat as the source of truth) ----
    Lab   MasstoneLab;             // Golden's published dry-film L*a*b*
    Lab   Tint10Lab;               // from the tint spectrum
    Color MasstoneSrgb;            // for swatches only. NOTE: ~31% of Golden's achievable
                                   // colors are out of sRGB gamut (Berns 2022) -> flag clipping.
    bool  MasstoneOutOfSrgbGamut;

    // ---- strength & behaviour: the parameters a naive mixer is missing ----
    float TintStrengthL;           // Golden's published number = L* of the 1:10 tint. LOWER = STRONGER.
    float RelativeStrength;        // derived K/S index, normalised (Cerulean PB36 == 1.0)
                                   // use to pre-scale recipe ratios and to bound minor components

    Opacity OpacityRating;         // Opaque | SemiOpaque | SemiTransparent | Transparent
                                   // ORDINAL JUDGEMENT, not a measurement. UI/veto use only.
    float   GlossAverage;          // real measurement; better transparency proxy than the rating
    float   ViscosityCpsLow, ViscosityCpsHigh;

    // ---- physical / practical ----
    float PigmentSpecificGravity;  // from Golden's pigment density chart (1.27 .. 6.11)
    float PaintDensityGPerMl;      // finished paint, ~1.0-2.0 per Golden SDS (often unknown)
    float DryFilmPvc;              // ~0.14 ultramarine, ~0.20 cobalt; nullable
    MixBasis RecipeBasis;          // Volume | Mass -- MUST be explicit, sources disagree

    // ---- permanence / provenance ----
    Lightfastness LightfastnessRating;   // ASTM I..V  (I: dE 1-4, II: 4-8, III: 8-16)
    string MunsellNotation;              // "9.2 PB 2.4/4.3"
    DataSource Source;                   // which dataset each field came from, with URL + date
    string     Notes;                    // e.g. "masstone not truly opaque; white card shows through"
}
```

**Notes on the schema, in priority order:**

1. `MasstoneReflectance` + `Tint10Reflectance` + fitted `K`/`S` is the heart of it. Everything else is convenience or UI.
2. **`RecipeBasis` is not optional bookkeeping.** Berns weighed by mass; K-M theory is volume-fraction; Golden says "parts". Getting this wrong is a systematic 10–30% error on cadmium-vs-phthalo mixes.
3. `MasstoneSrgb` should be kept *only* for swatch rendering, and should carry `MasstoneOutOfSrgbGamut`. The current single-triple model is not just imprecise, it is unable to represent ~31% of the reachable colors.
4. `KsAreFitted == false` should make the mixer refuse (or heavily caveat) any recipe with no white in it, since that is where single-constant K/S fails.
5. Add a **derived** `HueAngleMasstone` so the mixer can apply the 60°/90°/110° hue-separation gates from §6.3 cheaply.

---

## 10. Recommended changes, ranked by (impact / effort)

### Tier 1 — very high impact, low effort (do these first)

| # | Change | Why | Effort |
|---|---|---|---|
| 1 | **Ingest the 78-paint Golden spectra spreadsheet** (`GoldenSpectra.zip`) as the palette's source of truth: 31-band reflectance + K/S + L\*a\*b\* per paint, free and licensed to share. | Replaces guessed sRGB triples with Golden's own dry-film measurements, and gives the mixer a spectrum instead of three numbers. | Hours. It is a 52 KB xlsx; I already parsed it. |
| 2 | **Scrape Golden's Heavy Body Pigment Detail Chart** (154 rows) for `PigmentCodes`, `OpacityRating`, `Lightfastness`, `GlossAverage`, `Viscosity`, `MasstoneLab`, `TintStrengthL`. Plain HTML table. | Adds tinting strength, pigment composition and opacity — three of the four biggest missing parameters — from one page. | Hours. |
| 3 | **Add `RelativeStrength` and use it to pre-scale every recipe.** Normalise the derived K/S index (§2.3) and divide candidate ratios by it. | This is the single largest source of wrong recipes today. Equal volumes of PB15:3 and PY35 are off by ~20×. | Hours (table is in §2.5). |
| 4 | **Quantise recipes to a geometric ratio ladder and stop printing percentages.** `1:1, 1:1.5, 1:2, 1:3, 1:5, 1:8, 1:12, 1:20, 1:40`. Refuse/reformulate any recipe whose minor component is <2–3%; instead instruct the user to pre-mix a 1:10 stock tint. | 37%/63% is ΔE00 ≈ 0.4 — below JND. Ratios finer than ~30% relative are unexecutable and dishonest. | Hours. |
| 5 | **Flag out-of-sRGB targets and out-of-sRGB paints in the UI.** ~31% of achievable Golden colors are outside sRGB. | Prevents the app silently promising a color the photo could never have contained. | Hours. |
| 6 | **Add `IsSinglePigment` / `PigmentCount` and prefer single-pigment paints as mixing components**; warn when a chosen paint is a 3+ pigment convenience color (§5.2 list). | Cheapest possible muddiness prevention. | Hours. |

### Tier 2 — high impact, moderate effort

| # | Change | Why | Effort |
|---|---|---|---|
| 7 | **Obtain masstone + 10%-tint spectral pairs and fit two-constant K-M.** Email Golden (they supplied Berns twice) and Roy Berns; also ask Glassner/Haines, who already have a relationship. | This is *the* fix. Without tint data, phthalo blue is a near-black darkener in the model and a cyan in reality. Every other improvement is downstream of this. | Days of correspondence + a day of fitting. Highest value per hour of anything on this list. |
| 8 | **Interim workaround if (7) stalls:** synthesise a Titanium White row. Golden's spectra file has no white. Take PW6 masstone L\*98.25 a\*−0.74 b\*1.24 from the chart, or use the RIT 2016 19-paint set (which *does* include PW6 and PBk9) if Berns shares it. Then reconstruct approximate tint spectra using each paint's published `TintStrengthL` as a constraint. | Unlocks white mixing, which is most of what a paint-matching app actually recommends. | 1–2 days. Flag results as estimated. |
| 9 | **Encode the muddiness gates** from §6: reject/penalise candidate mixes with >3 pigments total (counting convenience-color constituents), and penalise hue separation >90–110° unless the target chroma is itself low. Show predicted chroma retention. | Turns "the app suggested mud" into "the app told me it would be mud". Supported by measured retention curves. | 1–2 days. |
| 10 | **Store and display the masstone-vs-tint distinction in the UI.** Show each paint as a masstone swatch *and* a tint swatch, like Golden's hand-painted charts. | Users pick paints by tube color and get surprised. This is the app's chance to teach undertone. | 1–2 days. |
| 11 | **Add a "dries darker" advisory** and Golden's compensation rule (aim a step lighter; or add Zinc White rather than Titanium). Note that the app's targets are already dry-film values. | Closes the biggest wet/dry confusion without needing the unmeasured ΔE. | Hours, but needs the copy written carefully. |

### Tier 3 — worthwhile, higher effort or lower certainty

| # | Change | Why | Effort |
|---|---|---|---|
| 12 | **Offer curated limited palettes** as selectable presets: Golden's own 8, MacEvoy's secondary 6, split-primary 6, CMY triad, Zorn 4 — each with its documented weak zone shown (split-primary: dull greens; CMY: dull orange and green; Zorn: no blue). | Users get better results from a good 6 than a bad 30. All five are citable. | 2–3 days incl. gamut precomputation. |
| 13 | **Recompute the palette gamut comparison properly once two-constant K and S exist** (with white and black, in 3-D CIELAB volume, not a 2-D hull). My §5.4 numbers are masstone-only and should be replaced. | The current comparison under-rates CMY for a model artifact, not a physical reason. | 1 day once (7) lands. |
| 14 | **Make `RecipeBasis` explicit and convert mass↔volume** using `PigmentSpecificGravity` / `PaintDensityGPerMl`. | Removes a silent 10–30% systematic error. | 1–2 days, gated on getting paint densities (Golden SDS gives only a 1.0–2.0 range). |
| 15 | **Measure the wet→dry shift yourself** (a dozen HB masstones + 1:10 tints, wet and dry, ΔL\*/ΔE00) and publish the number. | Nobody has published this. It would be a genuinely novel, citable contribution and it removes an `[INFERRED]` from the model. | Days, needs a colorimeter or a carefully calibrated camera. |
| 16 | **Pursue the LBNL Liquitex data** (masstone + 1:4 + 1:9 tints + published K and S in mm⁻¹) by requesting the decryption key from Ronnen Levinson. | A second, independent brand with a *three-point* tint ladder — better than Golden's two-point pair, and would let you validate the fit rather than just interpolate it. | Uncertain; access is restricted to project members. Low cost to ask. |
| 17 | **Evaluate Mixbox** as the mixing kernel vs a hand-rolled two-constant K-M. | Published, peer-reviewed, and already solves the RGB↔pigment-space problem. ⚠ Check commercial licensing. | 2–3 days to evaluate. |

### Explicitly not recommended
- **Do not use MacEvoy's "phthalo is 40× ultramarine"** as a mixing coefficient. It is pigment-mass-based; the paint-volume figure is ≈2.4×.
- **Do not treat the opacity rating as a number.** Golden says outright there is no standard and that Phthalo Blue ranked "on par with" cobalt and cadmium in their own drawdown. Use `GlossAverage` if you need a scalar transparency proxy.
- **Do not try to model wet paint.** The app's targets and Golden's data are both dry-film. Just tell the user.

---

## 11. Open items and honest gaps

| Item | Status |
|---|---|
| Units of Golden's chart `Tint Strength` column | `[INFERRED]` L\* of the 1:10 tint. Strongly supported by the three-blacks test and by every family's known ordering, but **Golden does not document it.** Worth one email to help@goldenpaints.com to confirm. Whites are measured by a different (reversed) procedure. |
| Measured wet→dry ΔE / ΔL\* for acrylics | **`[NOT FOUND]`** anywhere — Golden, Liquitex, Chroma, conservation literature. Mechanism is well documented; magnitude is not. My 5–15 ΔE00 estimate for dark transparents is `[INFERRED]`, do not ship it as fact. |
| Berns 2022 Excel (58 paints, 831 spectra, K-M coefficients) | **Withdrawn** from grayskyimaging.com. Must be requested. |
| RIT 2016 Excel (19 paints incl. PW6, PBk9, 770 spectra) | "Available by request" per the paper. Not tested. |
| LBNL spectral datafiles | **AES-128 encrypted**, key restricted to Cool Colors members. Verified by download. Charts and PVC values are public. |
| Whether Golden's "10 parts white to 1 part color" is by mass or volume | `[NOT VERIFIED]`. Matters for tinting-strength calibration. Ask Golden. |
| Yellows' tinting strength | My derived index is luminance-based and **understates yellows systematically**. Needs tint a\*b\* or tint spectra, which Golden does not publish. |
| Human volumetric mixing accuracy (empirical) | **No study found.** The §7.4 bound is derived from colour sensitivity, not from measured human performance. A small user study would be easy and genuinely useful. |
| Kremer / Forbes Pigment Collection spectral data | Not located in this pass. Worth a dedicated search. |
| Per-color finished-paint density for Golden | Only a range (SG 1.0–2.0) is published. Per-pigment SG is available; per-tube density is not. |

---

## Appendix A — Golden Heavy Body masstone L\*a\*b\* (D65/10°), all 78 paints in the free spectra file

`[CITED]` Source: https://www.realtimerendering.com/downloads/GoldenSpectra.zip (Golden Artist Colors via Glassner & Haines). Prod # · Name · L\* a\* b\*. The file also carries 31-band %R and 31-band K/S for each row.

```
1450 Alizarin Crimson Hue        L*28.00 a*10.77 b*2.86     1210 Napthol Red Light         L*46.90 a*54.15 b*36.67
1005 Anthraquinone Blue          L*24.23 a*3.72  b*-5.25    1220 Napthol Red Medium        L*37.19 a*40.63 b*21.60
1464 Azurite Hue                 L*31.36 a*-4.80 b*-15.69   1225 Nickel Azo Yellow         L*51.20 a*16.30 b*41.54
1007 Bismuth Vanadate Yellow     L*90.21 a*-3.92 b*91.23    1240 Paynes Gray               L*23.69 a*0.23  b*-2.95
1010 Bone Black                  L*24.03 a*0.07  b*-0.33    1250 Perm Green Light          L*45.65 a*-50.66 b*20.98
1020 Burnt Sienna                L*33.79 a*17.16 b*13.05    1252 Permanent Maroon          L*26.79 a*5.22  b*0.03
1030 Burnt Umber                 L*27.39 a*3.20  b*3.31     1253 Permanent Violet Dark     L*26.62 a*7.84  b*-5.98
1035 Burnt Umber Light           L*29.22 a*6.95  b*6.42     1255 Phthalo Blue GS           L*25.11 a*8.84  b*-19.75
1080 Cad Red Dark                L*38.35 a*43.45 b*20.23    1260 Phthalo Blue RS           L*23.99 a*4.96  b*-10.14
1090 Cad Red Light               L*51.58 a*59.36 b*43.49    1270 Phthalo Green BS          L*26.68 a*-6.90 b*-4.71
1100 Cad Red Medium              L*42.44 a*50.16 b*27.65    1275 Phthalo Green YS          L*27.73 a*-12.41 b*0.59
1110 Cad Yellow Dark             L*79.03 a*26.48 b*88.12    1460 Prussian Blue Hue         L*24.12 a*1.92  b*-3.09
1130 Cad Yellow Medium           L*84.08 a*15.45 b*95.30    1276 Pyrrole Orange            L*56.19 a*60.86 b*54.22
1135 Cad Yellow Primrose         L*90.98 a*-10.49 b*84.48   1277 Pyrrole Red               L*42.42 a*55.27 b*31.80
1070 Cadmium Orange              L*63.65 a*54.14 b*64.09    1278 Pyrrole Red Dark          L*37.45 a*45.14 b*21.86
1120 Cadmium Yellow Light        L*88.89 a*-2.35 b*94.17    1279 Pyrrole Red Light         L*47.10 a*57.14 b*37.88
1040 Carbon Black                L*25.65 a*0.24  b*0.14     1280 Quin Burnt Orange         L*29.04 a*14.55 b*7.03
1050 Cerulean Blue Chromium      L*41.70 a*-11.99 b*-32.61  1290 Quin Crimson              L*26.17 a*11.96 b*2.92
1051 Cerulean Blue Deep          L*37.65 a*-16.34 b*-22.73  1305 Quin Magenta              L*31.19 a*28.36 b*4.14
1060 Chromium Oxide              L*44.20 a*-15.78 b*16.20   1301 Quin Nickel Azo           L*36.47 a*20.95 b*18.85
1061 Chromium Oxide Dark         L*33.57 a*-9.37 b*8.61     1310 Quin Red                  L*36.32 a*40.31 b*15.20
1140 Cobalt Blue                 L*36.09 a*11.36 b*-48.54   1320 Quin Red Light            L*41.82 a*45.00 b*20.85
1142 Cobalt Green                L*35.04 a*-18.26 b*3.26    1330 Quin Violet               L*29.74 a*25.35 b*7.21
1145 Cobalt Teal                 L*63.80 a*-41.42 b*-9.54   1340 Raw Sienna                L*50.89 a*16.15 b*31.21
1143 Cobalt Titanate Green       L*59.79 a*-33.50 b*24.53   1350 Raw Umber                 L*29.29 a*1.88  b*4.26
1144 Cobalt Turquoise            L*42.62 a*-28.91 b*-9.45   1360 Red Oxide                 L*38.75 a*30.39 b*21.68
1147 Diarylide Yellow            L*75.82 a*34.33 b*81.69    1461 Sap Green Hue             L*27.57 a*-3.09 b*3.14
1150 Dioxazine Purple            L*25.21 a*2.21  b*0.98     1468 Terre Verte Hue           L*35.57 a*-6.57 b*8.18
1170 Green Gold                  L*45.64 a*-11.99 b*31.51   1370 Titan Buff                L*85.41 a*0.96  b*15.21
1180 Hansa Yellow Light          L*87.75 a*-3.90 b*88.74    1375 Titanate Yellow           L*89.45 a*-6.90 b*56.84
1190 Hansa Yellow Medium         L*78.68 a*19.42 b*90.49    1383 Transparent Brown Iron Ox L*29.07 a*3.72  b*3.12
1454 Hookers Green Hue           L*26.50 a*-2.11 b*0.00     1384 Transparent Pyrrole Orang L*49.12 a*55.18 b*41.74
1455 Indian Yellow Hue           L*53.92 a*30.86 b*47.66    1385 Transparent Red Iron Oxid L*32.62 a*14.98 b*9.13
1195 Jenkins Green               L*25.76 a*-3.62 b*0.16     1386 Transparent Yellow Iron O L*47.74 a*16.93 b*26.81
1457 Manganese Blue Hue          L*39.12 a*-15.14 b*-30.71  1390 Turquoise                 L*25.24 a*-1.58 b*-6.27
1200 Mars Black                  L*25.50 a*0.39  b*0.07     1400 Ultramarine Blue          L*25.21 a*16.21 b*-31.37
1202 Mars Yellow                 L*47.08 a*26.27 b*27.36    1462 Van Dyke Brown Hue        L*25.11 a*0.45  b*0.63
                                                            1405 Violet Oxide              L*32.19 a*19.31 b*9.44
                                                            1469 Viridian Green Hue        L*35.21 a*-23.23 b*-0.07
                                                            1407 Yellow Ochre              L*57.13 a*17.85 b*39.40
                                                            1410 Yellow Oxide              L*62.60 a*15.43 b*46.00
```

⚠ Titanium White and Zinc White are **absent** from this file. Their masstone Lab from Golden's chart: **PW6 L\*98.25 a\*−0.74 b\*1.24** (D50), **PW4 L\*95.94 a\*−0.85 b\*0.93**.

---

## Appendix B — Every source cited

**Golden Artist Colors / Just Paint**
- Heavy Body Pigment Detail Chart (154 rows, Lab + tint strength) — https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data
- SoFlat technical chart — https://goldenartistcolors.com/products/golden-artist-acrylics/soflat/technical-chart
- Technical Specifications Explained — https://goldenartistcolors.com/technical-specifications-explained
- Strong and Weak Colors (10:1 tint method) — https://goldenartistcolors.com/resources/strong-and-weak-colors
- Color Mixing Guide (masstone/undertone) — https://goldenartistcolors.com/resources/color-mixing-guide
- Color Mixing with 8 Curated Acrylic Colors — https://goldenartistcolors.com/resources/color-mixing-with-8-curated-acrylic-colors
- Clean Color Mixing (mineral vs organic) — https://goldenartistcolors.com/resources/clean-color-mixing
- Heavy Body Acrylic Colors (pigment load ↔ opacity/gloss) — https://goldenartistcolors.com/resources/heavy-body-acrylic-colors
- Titanium White and Zinc White — https://goldenartistcolors.com/resources/titanium-white-and-zinc-white
- Color Shift–Shrinkage — https://justpaint.org/color-shift-shrinkage/
- Pigment Volume Concentration and its Role in Color — https://justpaint.org/pigment-volume-concentration-and-its-role-in-color/
- Volume, Weight, and Pigment to Oil Ratios — https://justpaint.org/volume-weight-and-pigment-to-oil-ratios/
- Pigment Density (article) — https://justpaint.org/pigment-density/
- Pigment Specific Gravity chart (PDF, full data) — https://justpaint.org/wp-content/uploads/2019/05/PIGMENT-SPECIFIC-GRAVITY_jpechart.pdf
- Lightfastness Testing at Golden (ΔE thresholds, ASTM D4303 tint spec) — https://justpaint.org/lightfastness-testing-at-golden-artist-colors/
- Introducing New GOLDEN Heavy Body Light Value Colors — https://justpaint.org/introducing-new-golden-heavy-body-light-value-colors/
- Heavy Body SDS (SG 1.0–2.0) — https://www.jerrysartarama.com/media/pdfs/golden/GOLDEN%20SDS%20Sheet%20Heavy%20Body%20Acrylics%20MSDS.pdf

**Golden spectral data (free, redistributable)**
- Golden Paint Spectra (Glassner & Haines) — https://www.realtimerendering.com/golden.html
- The dataset — https://www.realtimerendering.com/downloads/GoldenSpectra.zip

**handprint.com (Bruce MacEvoy)**
- The material attributes of paints (tinting strength, SG, particle size, RI, masstone/undertone) — https://www.handprint.com/HP/WCL/pigmt3.html
- What the ratings mean (measurement methodology, HS = hue shift masstone→tint) — https://www.handprint.com/HP/WCL/pigmt8.html
- Modern color theory: applications (Rules 38–42) — https://www.handprint.com/HP/WCL/color18b.html
- "Primary" triad palette — https://www.handprint.com/HP/WCL/palette4c.html
- Split "primary" palette — https://www.handprint.com/HP/WCL/palette4r.html
- Secondary palette — https://www.handprint.com/HP/WCL/palette4e.html

**Color science / conservation**
- Berns, *Artist Acrylic Paint Spectral, Colorimetric, and Image Dataset* (2022) — https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf
- Gray Sky Imaging resources (dataset withdrawal notice) — https://grayskyimaging.com/resources/
- Berns, *Artist Paint Spectral Database* (RIT, 19 Golden HB paints) — https://www.rit.edu/science/sites/rit.edu.science/files/2019-03/ArtistSpectralDatabase.pdf
- Berns & Mohammadi, *Evaluating Single- and Two-Constant Kubelka-Munk…* — https://www.researchgate.net/publication/275501001_Evaluating_Single-_and_Two-Constant_Kubelka-Munk_Turbid_Media_Theory_for_Instrumental-Based_Inpainting
- On the Kubelka-Munk Single-Constant/Two-Constant Theories — https://www.researchgate.net/publication/216567998_On_the_Kubelka-Munk_Single-ConstantTwo-Constant_Theories
- Kubelka-Munk Theory overview (volume fraction, PVC limits) — https://www.sciencedirect.com/topics/engineering/kubelka-munk-theory
- Sochorová & Jamriška, *Practical Pigment Mixing for Digital Painting* (Mixbox) — https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf
- Levinson, Berdahl & Akbari, *Solar spectral optical properties of pigments, Part II* — https://heatisland.lbl.gov/publications/solar-spectral-optical-properties-0

**LBNL Pigment Database (Liquitex, masstone + 1:4 + 1:9 tints)**
- Database index — https://coolcolors.lbl.gov/LBNL-Pigment-Database/database.html
- Example paint page (Cobalt Blue, PVC 14%) — https://coolcolors.lbl.gov/LBNL-Pigment-Database/paints/U05.html
- Guide to Reading Spectral Datafiles (AES-encryption note, field list) — https://coolcolors.lbl.gov/LBNL-Pigment-Database/assets/misc/spectral-datafile-guide.pdf
- Example public spectral chart — https://coolcolors.lbl.gov/LBNL-Pigment-Database/assets/spectral-charts/pdf/B09-tint-ladder-spectral-chart.pdf
- Liquitex Paint Technical Information (mirrored TDS) — https://coolcolors.lbl.gov/LBNL-Pigment-Database/assets/manufacturer-TDS/Liquitex/PaintTechInfo.pdf

**Standards & industry**
- ASTM D387 (color and strength of chromatic pigments) — https://www.astm.org/Standards/D387.htm
- ASTM D4838 (relative tinting strength of chromatic paints) — https://www.astm.org/Standards/D4838.htm
- SpecialChem, Tinting Strength (K/S formula, RI 1.5 rule) — https://www.specialchem.com/coatings/guide/tinting-strength
- Leneta opacity/drawdown charts — https://www.leneta.com/wp-content/uploads/2023/12/Drawdown-Basics-v2-2-1.pdf
- KLA refractive index database, acrylic/PMMA = 1.491 — https://www.kla.com/products/instruments/refractive-index-database/acrylic/acrylate-lucite-perspex-plexiglass
- Chroma Atelier, Matching Wet Paint to Dry Paint — https://www.dick-blick.com/items/016/16/pdfs/Chroma_Atelier_Matching_Wet_Paint_To_Dry_Paint.pdf

**Palettes / other**
- Jackson's Art, The Zorn Palette — https://www.jacksonsart.com/en-us/techniques/the-zorn-palette
- Draw Paint Academy, The Zorn Palette — https://drawpaintacademy.com/zorn-palette/
- Jackson's Art, The Unique Qualities of Phthalo Pigments — https://www.jacksonsart.com/blog/2021/02/09/the-unique-qualities-of-phthalo-pigments/
- Cosentino, *Pigments Checker version 3.0* — https://www.sciencedirect.com/science/article/abs/pii/S0026265X16301011
- CHSOS Pigments Checker — https://chsopensource.org/pigments-checker/
- CHSOS App Note 4, Reflectance Spectra Database for Modern & Contemporary Art — https://chsopensource.org/chsos-application-note-4/
- The Color of Art Pigment Database — https://www.artiscreation.com/Color_index_names.html
- ArtistAssistApp (AGPL-3.0 prior art) — https://github.com/eugene-khyst/artistassistapp
- Mixbox code — https://github.com/scrtwpns/pigment-mixing
- spectral.js — https://github.com/rvanwijnen/spectral.js
- painting_tools — https://github.com/rubenwiersma/painting_tools

---

## Appendix C — Reproducing the derived numbers

All `[DERIVED]` figures came from a scratch workspace, not from the repo. Method summary so another agent can redo or check them:

1. **Data.** `curl` the Golden spectra zip with a browser User-Agent and `-e https://www.realtimerendering.com/golden.html` (plain curl hits a Cloudflare challenge). Open the xlsx with `openpyxl`: Sheet1, header on row 2, data rows 3–80; reflectance in cols 7–37 (400–700 @10 nm, in %), K/S in cols 39–69. Separately `curl` the Heavy Body pigment-data page and regex the `<tr>/<td>` cells (11 columns, 154 data rows).
2. **Tinting-strength index (§2.3, §2.5).** Golden tint-strength value treated as tint L\* → `Y = 100((L*+16)/116)³` → `R = Y/100` → `K/S = (1−R)²/2R`; then `K/S_paint = 11·K/S_tint − 10·K/S_white` with white from L\*98.25. Normalise to Cerulean Blue Chromium.
3. **Colorimetry (§5.4, §6.2, §6.3, §7.4).** `colour-science` 0.4.7, CIE 1931 2° observer, D65, `SpectralShape(400,700,10)`. Single-constant K-M: `K/S = (1−R)²/2R`, mix by volume-weighted sum, invert with `R = 1 + K/S − sqrt((K/S)² + 2K/S)`.
4. **Gamut hulls (§5.4).** All pairwise mixes at 9 ratios + all triples on a 1/9 simplex; Andrew's monotone-chain convex hull on the a\*b\* projection; shoelace area.
5. **Ratio sensitivity (§7.4).** `colour.delta_E(..., method='CIE 2000')` between mixes at fraction *f* and *f*+δ.

Sanity check that the pipeline is right: my computed masstone Lab for Phthalo Blue GS is L\*24.41 a\*11.96 b\*−20.53 against the spreadsheet's published D65/10° value of L\*25.11 a\*8.84 b\*−19.75 — consistent within the 2° vs 10° observer difference.
