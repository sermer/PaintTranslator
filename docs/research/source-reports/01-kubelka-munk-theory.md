# Research 01 — Kubelka–Munk, spectral reconstruction, and the `SubtractivePaintMixer` port

Web research only. No repository files were modified. Every claim below is either cited to a URL or
labelled as **[measured]** — meaning I re-implemented the shipped C# algorithm verbatim in Python
(parsing the actual spectra arrays out of `Imaging/SubtractivePaintMixer.cs`) and ran the numbers.

---

## 0. Executive summary

Six findings that matter more than the rest:

1. **The mixer is already a two-constant Kubelka–Munk model, not single-constant.** The
   `concentration = weight² × Strength` line is algebraically identical to Duncan's two-constant
   mixing law with the assumption `S_i = Y_i` (a wavelength-independent scattering coefficient per
   paint, set equal to the paint's CIE-Y). See §1.6 for the two-line proof. This is a *better* start
   than the code comments claim, and it means the fix is to improve the estimate of `S_i` rather than
   to restructure the model.

2. **The `weight²` term is a gradient-easing fudge with no physical basis.** It comes from
   spectral.js, whose author added it for visual smoothness in colour interpolation; a GitHub issue
   asking why it exists went unanswered. It is *harmless at 50/50* and *wrong everywhere else*: a
   user asking for "1 part A : 3 parts B" gets the physics of 1 : 9. For a recipe app whose output
   *is* the ratio, this is the single most damaging line of code. Delete the squaring. (§2)

3. **CIE-Y is not tinting strength — it is roughly the inverse of it**, and the code's variable name
   `Strength` plus its doc comment ("so light, weakly-tinting paints are not overwhelmed by dark
   ones") have the physics backwards relative to the industry meaning of the term. Y is, however, a
   crude but *defensible* proxy for **scattering** power `S`, which is the role it actually plays in
   the equation. Phthalo blue has ~40× the tinting strength of ultramarine at nearly identical Y —
   luminance cannot distinguish them. (§3)

4. **The 7-basis-spectra reconstruction is the weakest physical link, and no amount of algorithm
   work fixes it.** Berns showed that three primaries derived by PCA from 58 measured Golden Heavy
   Body acrylics reproduce those paints with mean ΔE00 = 1.8 and worst case 9.88 under a 2200 K
   illuminant, and concluded "three primaries are insufficient to approximate the 58 pigments." The
   real answer is to stop reconstructing and use measured spectra for the ~20–40 tubes the app
   actually recommends. (§4)

5. **`MinReflectance = 1e-15` silently controls how pure black behaves in mixtures**, through a
   cancellation that happens to be scale-invariant but is entirely accidental. 50/50 black+white
   currently yields L\* = 67.8 (sRGB 165,165,165), far lighter than a real 50/50 black:white
   drawdown. (§6, test T5)

6. **A documentation bug:** the class doc and `BandCount` comment say "380–730nm in 10nm steps" for
   38 bands. 380 + 37×10 = **750 nm**. Confirmed by the CMF tables (ȳ peaks at index 17 = 550 nm,
   z̄ truncates to zero at index 27 = 650 nm) and by spectral.js/Mixbox, which both state 380–750 nm.
   36 bands would be 380–730. Anyone later merging measured 380–730 data (Berns' dataset is
   380–730) will trip on this.

---

## 1. Single-constant vs two-constant Kubelka–Munk

### 1.1 The underlying model

Kubelka–Munk is a two-flux (two-stream) approximation. Inside a plane-parallel, homogeneous,
isotropically scattering, non-fluorescent layer, with `i` the downward diffuse flux and `j` the
upward diffuse flux and `x` depth:

```
-di/dx = -(K + S)·i + S·j
 dj/dx = -(K + S)·j + S·i
```

`K` = absorption coefficient, `S` = scattering (back-scattering) coefficient, both per unit
thickness, both functions of wavelength. Assumptions, per
[Wikipedia](https://en.wikipedia.org/wiki/Kubelka%E2%80%93Munk_theory) and
[HandWiki](https://handwiki.org/wiki/Physics:Kubelka-Munk_theory): perfectly diffuse illumination
and internal flux, plane-parallel semi-infinite (or finite) layers, linear and independent K and S,
a continuum treatment of a discontinuous particulate medium, and no surface reflection.

### 1.2 Opaque (infinite-thickness) solution — the "remission function"

For a layer thick enough that the substrate contributes nothing:

```
R∞ = 1 + (K/S) - sqrt((K/S)² + 2·(K/S))
```

and its inverse, the Kubelka–Munk remission function:

```
F(R∞) = (1 - R∞)² / (2·R∞) = K/S
```

These are exactly the two functions in the C# code (`ToSpectrum` band loop and `FromMixedKs`), and
exactly `KS()` / `KM()` in
[spectral.js](https://github.com/rvanwijnen/spectral.js/blob/3.0.0/README.md). Note the crucial
property: **R∞ depends only on the ratio K/S, not on their magnitudes.** That is the whole reason a
"single-constant" simplification is even conceivable.

### 1.3 Mixture law (Duncan 1940) — the actual two-constant theory

For `N` colorants at concentrations `c_i`, absorption and scattering are separately additive
([Duncan 1940](https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf),
eq. 1; also [Wikipedia](https://en.wikipedia.org/wiki/Kubelka%E2%80%93Munk_theory)):

```
K_mix(λ) = Σ_i c_i · K_i(λ)
S_mix(λ) = Σ_i c_i · S_i(λ)          with c_i ≥ 0, Σ_i c_i = 1

  ⇒  (K/S)_mix(λ) = Σ_i c_i·K_i(λ) / Σ_i c_i·S_i(λ)
```

`c_i` is a **mass or volume fraction of colorant**, not a perceptual slider. Centore's tint-ladder
worked example uses "concentration by volume of white, `Cw`, and blue, `Cb`"
([Perceptual Reflectance Weighting for Estimating Kubelka-Munk Coefficients, §5](http://www.munsellcolorscienceforpainters.com/ColourSciencePapers/PerceptualReflectanceWeightingForKubelkaMunk.pdf)).

### 1.4 Single-constant simplification

Single-constant K-M assumes all colorants in the system share one scattering spectrum — physically,
that a single strongly-scattering white (titanium dioxide) dominates `S`, and the chromatic
colorants are non-scattering (transparent) absorbers on top of it. Then `S_mix ≈ S_white` cancels:

```
(K/S)_mix(λ) = Σ_i c_i · (K/S)_i(λ)
```

i.e. **K/S values mix linearly in concentration with the weights summing to 1, and there is no
"strength" factor at all.** A strong colorant is strong because its `(K/S)` curve has large
magnitude; a weak one has a small one. This is worth internalising because it kills the intuition
that a strength multiplier is needed: in true single-constant K-M, tinting strength is *already
encoded in the numeric size of the K/S array*.

For textiles, the single-constant form additionally works because dyes are molecularly dissolved
and genuinely non-scattering, and it has been the basis of computer recipe prediction since the
1960s
([Manchester thesis abstract](https://research.manchester.ac.uk/en/studentTheses/novel-linearisation-of-the-single-constant-kubelka-munk-equation-)).

### 1.5 Where single-constant breaks for artist paints

- **Dark colours and masstones.** Berns & Mohammadi's abstract states that the conservation-standard
  single-constant approach (one tint of each pigment with white) "can lead to errors in pigment
  selection for dark colors and colors not containing a white pigment," and that the two-constant
  form is used "where the model assumptions more closely match the optics of inpainting materials"
  ([Studies in Conservation 52(4):299–314, 2007](https://www.tandfonline.com/doi/abs/10.1179/sic.2007.52.4.299)).
- **Mixtures with no white in them.** Ultramarine + cadmium yellow has no titanium dioxide; the
  assumption "one dominant scatterer" is simply false. Both pigments scatter, and by very different
  amounts.
- **Quantified elsewhere:** for pre-coloured fibre blends, two-constant beat single-constant by
  mean ΔE00 **0.10 vs 1.47** and RMSE **0.0032 vs 0.0449**
  ([Frontiers, Optimal Learning Samples for Two-Constant Kubelka-Munk Theory](https://www.frontiersin.org/journals/neuroscience/articles/10.3389/fnins.2022.945454/full)).
  Textiles are not paint, but this is the right order of magnitude for "how much does the second
  constant buy you."
- **Berns' own review** of the single-constant simplification for paint systems is
  [Color Research & Application 32(3), 2007, doi:10.1002/col.20309](https://onlinelibrary.wiley.com/doi/10.1002/col.20309)
  (paywalled — I could not read the body, only the title/abstract framing).
- **Dark mixtures specifically are a known weak spot of K-M itself**, not just of the
  simplification: at very low reflectance, light is absorbed before it is diffused, and measured
  reflectance is small enough that noise dominates
  ([Kubelka-Munk Prediction for Dark Mixtures](https://www.researchgate.net/publication/323656636_Kubelka-Munk_Prediction_for_Dark_Mixtures)).
- **Concentration linearity itself degrades at high loading.** K/S is empirically non-linear (closer
  to exponential) in colorant concentration above low concentrations
  ([Sci Reports, Estimation of dye concentration…](https://www.nature.com/articles/s41598-023-29264-x);
  [Springer, A Novel Correction Method of Kubelka–Munk Model](https://link.springer.com/article/10.1007/s12221-024-00559-8)).

### 1.6 What two-constant costs, and the key realisation about the existing code

**What it requires:** per paint, `K(λ)` and `S(λ)` on a common scale, conventionally normalised so
`S_white = 1` at every wavelength. The industry-minimal calibration is the **masstone–tint method**:
two opaque drawdowns per paint — the masstone straight from the tube, and one tint mixed with a
known mass/volume fraction of the white — giving two equations and two unknowns per wavelength.

Berns' 2022 dataset does exactly this: 68 Golden Heavy Body acrylics, drawdowns of masstone plus a
**10 % mixture with titanium white**, X-Rite MS7000 integrating sphere, specular included,
380–730 nm at 10 nm, two-constant opaque K-M, "unit absorption and scattering coefficients for each
paint relative to the scattering of white defined as unity"
([Berns, Artist Acrylic Paint Spectral, Colorimetric, and Image Dataset, Archiving 2022, PDF](https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf)).
He flags the limitation honestly: with two samples and two unknowns the fit is determinate, so
accuracy cannot be assessed, and ideally you want multiple tints plus, for yellows, mixtures with
black. Mohammadi & Berns' 2004 technical report concluded the minimum sample counts are **one** for
single-constant and **two** for two-constant
([RG](https://www.researchgate.net/publication/264844752_Verification_of_the_Kubelka-Munk_Turbid_Media_Theory_for_Artist_Acrylic_Paint_Summer_2004)).
For fibre blends the optimum pair is masstone + a 40 % tint; for single-constant the best single
sample is an 80 % tint
([Frontiers](https://www.frontiersin.org/journals/neuroscience/articles/10.3389/fnins.2022.945454/full)).

If you have more than two tints, the standard estimator is the Walowit–McCarthy–Berns (1987)
least-squares system; Centore's contribution is that WMB implicitly weights all residuals equally,
which is badly wrong because K/S sensitivity varies by orders of magnitude with reflectance level.
His concrete number: **going from R = 0.02 to 0.03 changes K/S by 7; going from R = 0.85 to 0.86
changes it by 0.002**
([Centore 2013, §7](http://www.munsellcolorscienceforpainters.com/ColourSciencePapers/PerceptualReflectanceWeightingForKubelkaMunk.pdf)).
He also has a companion paper on keeping the OLS solution physically realisable (concentrations and
coefficients in [0,1]) via a nearest-point-on-polytope / GJK formulation
([Enforcing Kubelka–Munk constraints for opaque paints, Coloration Technology, 2020](https://onlinelibrary.wiley.com/doi/abs/10.1111/cote.12497),
free PDF index at
[munsellcolorscienceforpainters.com](http://www.munsellcolorscienceforpainters.com/ColourSciencePapers/ColourSciencePapers.html)).

**Now the realisation.** The C# mixer computes

```
c_i = w_i² · Y_i                                (Y_i = CIE-Y of paint i's reconstructed spectrum)
(K/S)_mix = Σ_i c_i·(K/S)_i / Σ_i c_i
```

Substitute `d_i = w_i²` (whatever the concentration proxy is) and expand:

```
(K/S)_mix = Σ_i d_i·Y_i·(K/S)_i / Σ_i d_i·Y_i
          = Σ_i d_i·[ Y_i·(K/S)_i ] / Σ_i d_i·[ Y_i ]
          = Σ_i d_i·K_i / Σ_i d_i·S_i         with   S_i ≡ Y_i,  K_i ≡ Y_i·(K/S)_i
```

That is **verbatim Duncan's two-constant law** (§1.3) under the assumption that each paint's
scattering coefficient is spectrally flat and numerically equal to its luminance. So:

- The code's structure is right and more capable than a pure single-constant model.
- `Strength` is occupying the slot of a **relative scattering coefficient `S_i`**, not a tinting
  strength.
- Remarkably, this is close to a published model. Abed & Berns propose a hybrid of the opaque
  single- and two-constant forms that "introduces an impurity index, a spectrally nonselective
  scattering coefficient for each chromatic component," validated on 28 matte acrylic dispersion
  paints
  ([Linear modeling of modern artist paints…, Color Res Appl 42(3):308–318, 2017](https://www.researchgate.net/publication/309540310_Linear_modeling_of_modern_artist_paints_using_a_modification_of_the_opaque_form_of_Kubelka-Munk_turbid_media_theory)).
  "A spectrally nonselective scattering coefficient per chromatic component" is exactly what
  `Strength` is. The published version *fits* it; spectral.js *guesses* it as Y.

### 1.7 Saunderson correction — and whether it matters here

K-M solves for **internal** (bulk) reflectance. A real measurement includes Fresnel reflection at
the air/binder interface on the way in and total internal reflection on the way out. Saunderson
(1942) corrects for both:

```
R_meas = k1 + (1 - k1)(1 - k2)·R_int / (1 - k2·R_int)
```

Inverting (the toolbox ships only the forward form, so here is the algebra):

```
R_int = (R_meas - k1) / [ (1 - k1)(1 - k2) + k2·(R_meas - k1) ]
```

`k1` = external surface (specular) reflection fraction, from Fresnel with the film's refractive
index; `k2` = internal reflection fraction at the interface. Typical values:

| source | k1 | k2 |
|---|---|---|
| Berns 2022, Golden acrylics, from minimum masstone reflectance | 0.035 | 0.6 (theoretical, n = 1.5, normal illumination, per Orchard 1969) |
| Textile practice | 0.08 | 0.5 |
| Toolbox note | Fresnel-derived; "usually not known" | 0.6 theoretical, "as low as 0.4 might be used" |

Sources:
[Berns 2022 PDF](https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf);
[colour-science/MunsellAndKubelkaMunkToolbox SaundersonCorrection.m](https://github.com/colour-science/MunsellAndKubelkaMunkToolbox/blob/master/KubelkaMunk/SaundersonCorrection.m);
[Frontiers](https://www.frontiersin.org/journals/neuroscience/articles/10.3389/fnins.2022.945454/full).
Mixbox applies it as its eq. 6, with `k1, k2` taken from Okumura's measurements
([Mixbox paper](https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf)).

**Does it matter for this app?** Yes for accuracy, but with a sharp caveat.

**[measured]** With k1 = 0.04, k2 = 0.6, the change in K/S from applying the correction:

| R_meas | R_int | K/S(R_meas) | K/S(R_int) | ratio |
|---|---|---|---|---|
| 0.90 | 0.9556 | 0.006 | 0.001 | 0.19× |
| 0.50 | 0.6970 | 0.250 | 0.066 | 0.26× |
| 0.20 | 0.3333 | 1.600 | 0.667 | 0.42× |
| 0.10 | 0.1429 | 4.050 | 2.571 | 0.63× |
| 0.06 | 0.0505 | 7.363 | 8.925 | 1.21× |
| 0.05 | 0.0256 | 9.025 | 18.51 | **2.05×** |

So it is a strongly non-linear reweighting: it *reduces* K/S for light bands and *doubles* it for
dark bands near R ≈ 0.05. Because the mix is a weighted average of K/S across paints, changing the
relative K/S of light vs dark bands changes the mixed hue, not just its lightness. It is not a
no-op even though the round trip (correct in, correct out) is preserved.

**The caveat:** the inverse is undefined for `R_meas ≤ k1`. The app's reconstructed spectra bottom
out at `MinReflectance = 1e-15`, and even the well-behaved base spectra dip to R ≈ 0.003 (blue at
430 nm) — far below any physical k1. This is precisely why Berns had to set `k1 = 0.035` "based on
the minimum reflectance of the masstone samples" and why he manually corrected negative optical
values for cobalt and cerulean. **Do not bolt Saunderson onto RGB-reconstructed spectra.** It
belongs with measured spectra, where the interface reflection you are removing actually exists in
the data. Priority: low until §4's measured-spectra work lands, then medium.

---

## 2. The `weight²` concentration term

### 2.1 Provenance — what it is and where it came from

spectral.js v3's README states the concentration formula explicitly:

```
C = f² · T² · L
```

with `L` = luminance (XYZ Y), `T` = tinting strength (a user-settable parameter, default 1), `f` =
the user's mixing factor. The README's justification is that the exponents "emphasize [tinting
strength's] effect on stronger or weaker pigments"
([README, 3.0.0](https://github.com/rvanwijnen/spectral.js/blob/3.0.0/README.md)).

The source confirms it (`spectral.js` v3.0.0, in `mix()`):

```js
let concentration = factor ** 2 * color.tintingStrength ** 2 * color.luminance;
totalConcentration += concentration;
ksMix += color.KS[i] * concentration;
...
R[i] = KM(ksMix / totalConcentration);
```

and `get luminance() { return this._luminance ??= Math.max(Number.EPSILON, this.XYZ[1]); }`,
`get tintingStrength() { return this._tintingStrength ??= 1; }`. The C# port is faithful except that
it omits `T` (which defaults to 1 anyway, so the two are numerically identical) and it clamps to
gamut instead of gamut-mapping (see §7).

**Rationale, in the author's own words.** From the Krita integration thread, van Wijnen's stated
reason for the luminance term is that "a lighter color needs more 'parts' for the mix than a darker
color"
([Krita Artists, Paint like color mixing (Kubelka-Munk)](https://krita-artists.org/t/paint-like-color-mixing-kubelka-munk/78156)).
ColorAide's port documents it the same way: "the mixing of the colors can turn out a bit dark. The
author of Spectral.js noticed this and found that weighting the mix such that more luminous colors
had more weight produced more natural lighting when mixing"
([ColorAide interpolation docs](https://facelessuser.github.io/coloraide/interpolation/)).

**Nobody has ever justified the squaring.** GitHub issue #13 (API-Beast, 29 May 2024) asks exactly
this — noting the "quadratic weighting applied to the mixing factor" and that the Y-tristimulus
choice "seems rather arbitrary, why the Y tristimulus in particular?" The issue is closed with **no
maintainer reply**
([issue #13](https://github.com/rvanwijnen/spectral.js/issues/13)). Issue #21 similarly questions
the redundancy of the RGB vs CMYW base spectra and is open and unanswered
([issue #21](https://github.com/rvanwijnen/spectral.js/issues/21)). There is no paper, no blog post,
and no derivation. Treat it as an undocumented aesthetic choice.

### 2.2 What squaring actually does, numerically

For two paints of equal Y, the effective concentration share of paint B is `w² / ((1-w)² + w²)`:

**[measured]**

| user weight w | effective share | linear would be |
|---|---|---|
| 0.10 | 0.012 | 0.10 |
| 0.25 | 0.100 | 0.25 |
| 0.50 | **0.500** | 0.50 |
| 0.75 | 0.900 | 0.75 |
| 0.90 | 0.988 | 0.90 |

Derivative at w = 0.5 is exactly **2.0** vs 1.0 for linear. So it is a symmetric S-curve — a
smoothstep-like easing with flat ends and a doubled slope in the middle. It is **exactly** the shape
you would choose to make a two-colour gradient *look* evenly spaced when the underlying K-M mixing
crowds all the visual change into the first few percent. That is a gradient-rendering fix, not
physics.

**Consequences that matter for a recipe app:**

- **Nothing changes at 50/50.** **[measured]** ultramarine+cad yellow, phthalo+hansa, black+white,
  red+green all give byte-identical results with `w²` and with linear `w`. Any test suite built only
  on equal mixes will not detect this bug.
- **Off-centre ratios are distorted by up to 3×.** "1 part : 3 parts" (w = 0.25) is executed as
  1 : 9. A recipe app whose *deliverable is the ratio* is therefore lying to the user.
- **Small additions of white become nearly invisible**, which is the opposite of real paint
  behaviour. **[measured]** phthalo-blue-like (0,60,120) + titanium white, as shipped vs linear
  weights:

| white fraction | as shipped `w²·Y`, L\* | linear `w·Y`, L\* | ΔL\* |
|---|---|---|---|
| 0.02 | 25.6 | 30.0 | 4.4 |
| 0.05 | 26.2 | 35.2 | 9.0 |
| 0.10 | 28.4 | 41.5 | **13.1** |
| 0.20 | 36.4 | 50.3 | 13.9 |
| 0.50 | 66.6 | 66.6 | 0 |
| 0.90 | 94.4 | 85.4 | −9.0 |

  A 13 L\* error at 10 % white is roughly ΔE ≈ 13 — visually enormous. Note that the industry uses a
  **10 % tint** as *the* characteristic sample for a pigment precisely because that region carries
  the most information; the squaring flattens exactly the region that matters most.

### 2.3 The physically correct answer for "2 parts A, 1 part B"

```
c_A = 2/3,  c_B = 1/3            (volume or mass fractions, Σc = 1)
K_mix(λ) = c_A·K_A(λ) + c_B·K_B(λ)
S_mix(λ) = c_A·S_A(λ) + c_B·S_B(λ)
(K/S)_mix = K_mix / S_mix
R∞ = 1 + (K/S)_mix - sqrt((K/S)_mix² + 2·(K/S)_mix)
```

With only `K/S` known and a per-paint scattering scalar `s_i`, the equivalent single-array form the
current code can implement directly is:

```
(K/S)_mix(λ) = Σ_i c_i·s_i·(K/S)_i(λ) / Σ_i c_i·s_i          with c_i linear in parts
```

i.e. **keep the structure, drop the square, keep (and improve) the scattering weight.**

Two caveats worth stating in the UI, not the model:

- "Parts" for artists usually means volume, and volume fraction ≠ pigment mass fraction because
  pigment load differs per tube (handprint: synthetic organics typically 20–30 % pigment by volume,
  cadmiums 40–50 %, some over 50 % —
  [handprint, material attributes of paints](https://www.handprint.com/HP/WCL/pigmt3.html)). If K
  and S are derived from paint-as-sold rather than dry pigment, volume fractions of paint are the
  right units and this is consistent.
- Mixbox does exactly the linear thing: `ĉ = (1−t)·c₁ + t·c₂`, no squaring, no luminance term, with
  `Σc = 1` and residuals interpolated linearly alongside
  ([Mixbox eqs. 10–11](https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf)).
  That is the most directly comparable published system and it has no analogue of the squaring.

---

## 3. Luminance as a proxy for tinting strength

### 3.1 What tinting strength actually is

Colour/tinting strength is the power of a colorant to impart colour to a white base, measured
**relative to a reference standard in the same base**:

```
% Tinting Strength = K/S(batch, λ_max) / K/S(standard, λ_max) × 100
```

Standard test methods: ASTM D4838 (relative tinting strength of chromatic paints), ASTM D2745
(white pigments), ASTM D387 (mechanical muller, withdrawn 2023), ISO 787
([SpecialChem tinting strength guide](https://www.specialchem.com/coatings/guide/tinting-strength) —
403 to automated fetch, content via search summary;
[ASTM D4838](https://www.astm.org/Standards/D4838.htm)).

**Physical determinants**, per
[handprint](https://www.handprint.com/HP/WCL/pigmt3.html) and
[PCI Magazine / Missouri S&T coatings notes](https://www.pcimag.com/articles/110186-how-light-and-pigment-interactions-affect-hiding-tint-strength-and-more):

- **Particle size (dominant).** Smaller particles → more surface area per unit mass → more intense
  colour. Rule of thumb: surface area scales as 1/size.
- **Refractive index contrast with the binder** (this drives `S`, hence hiding power). handprint's
  numbers, RI ratio to gum/acrylic (n ≈ 1.47): titanium white (rutile) RI 2.7, ratio 1.84; cadmium
  yellow RI 2.4, ratio 1.63; **ultramarine RI 1.5, ratio 1.02**; **phthalocyanines RI 1.4, ratio
  0.95**. A pigment looks cloudy at ratio ≈ 1.33 and nearly opaque above 1.5. So phthalo and
  ultramarine both barely scatter in acrylic — both are effectively transparent absorbers.
- **Optimum scattering diameter ≈ λ/2 (~0.2–0.3 µm for visible light);** below that hiding power
  falls off sharply even as tinting strength keeps rising. This is the tinting-strength / hiding-power
  trade-off, and it is Mie theory, not K-M.
- **Pigment load in the tube** and quality of dispersion/grind.

**Quantitative anchors for tests:**

- **Phthalocyanine blue has ~40× the tinting strength of ultramarine blue, and 2× that of Prussian
  blue** ([handprint](https://www.handprint.com/HP/WCL/pigmt3.html)).
- Golden's own recipe for Permanent Green Light is **1 part Phthalo Blue GS : 10 parts
  Benzimidazolone Yellow Medium**, and they rank strength as: highest = Phthalo Blue and Phthalo
  Green; next = Naphthol Red Light and Quinacridone Magenta; "Benzimidazolone Yellow Medium has very
  little tinting strength"
  ([Golden Color Mixing Guide](https://goldenartistcolors.com/resources/color-mixing-guide)).

### 3.2 Is luminance directionally right?

**As "tinting strength": no — it is backwards, and the code's naming and comment are misleading.**
Real tinting strength is high for dark, high-absorption pigments. Using Y as a *multiplier on
concentration* gives light paints *more* weight — that is exactly what van Wijnen said he wanted
("a lighter color needs more parts") and it is the opposite of tinting strength. Naming it `Strength`
with the comment "so light, weakly-tinting paints are not overwhelmed by dark ones" describes a
lightness correction, not a strength.

**As a relative scattering coefficient `S_i`: yes, crudely.** Per §1.6, that is mathematically the
role it plays, and scattering does correlate with luminance: titanium white has both the highest S
and the highest Y; transparent glazing pigments have both low S and low Y. **[measured]** the Y
values the code actually computes:

| paint (approx sRGB) | Y (= `Strength`) | min R | max K/S | mean K/S |
|---|---|---|---|---|
| titanium white (252,252,250) | 0.9722 | 0.958 | 9.5e-4 | 0.001 |
| hansa yellow (252,222,0) | 0.7294 | 0.023 | 20.7 | 4.70 |
| cad yellow (255,196,0) | 0.6074 | 0.025 | 18.8 | 4.56 |
| cad red (196,30,35) | 0.1279 | 0.015 | 31.8 | 11.6 |
| phthalo green (0,110,90) | 0.1189 | 0.0030 | 166 | 59.4 |
| phthalo blue (0,60,120) | 0.0459 | 0.0029 | 171 | 69.0 |
| ultramarine (31,42,141) | 0.0387 | 0.018 | 27.4 | 16.0 |
| black (0,0,0) | 1e-15 (floored) | 1e-15 | **5.0e14** | 5.0e14 |

Two things jump out:

1. **Y cannot distinguish phthalo blue from ultramarine** (0.0459 vs 0.0387 — an 18 % difference)
   even though their tinting strengths differ by ~40× and their scattering coefficients differ by a
   lot too. Interestingly, the *mean K/S* does discriminate them well (69.0 vs 16.0, a 4.3× ratio) —
   because a saturated dark blue reconstructs to a spectrum with much deeper troughs than a
   violet-leaning one. Mean or integrated K/S is a far better cheap strength proxy than Y.
2. **Cadmium red gets `Strength` = 0.128, essentially the same as phthalo green (0.119)**, though
   cadmium red is a highly opaque, strongly scattering inorganic (RI 2.4-ish class) and phthalo green
   is a transparent organic. As a scattering estimate this is simply wrong, and it's wrong in the
   direction that matters: cadmium's real S is several times phthalo's.

### 3.3 Defensible cheap approximations, ranked

Given only an sRGB swatch (i.e. no measured data), best to worst:

1. **Don't estimate it — look it up.** Tinting strength / opacity is printed on the tube and
   published per-pigment. Ship a small table: `{ pigment → relativeScattering, relativeStrength }`.
   Golden's guide and handprint give qualitative rankings you can quantise to 3–5 levels. This is
   the highest accuracy-per-effort option by a wide margin, and the honest one for an app that
   already knows *which named paint* the user owns.
2. **Integrated / mean K/S over the visible range** as an inverse-strength scalar:
   `strengthProxy_i = Σ_λ ȳ(λ)·(K/S)_i(λ)` or the unweighted mean. **[measured]** this separates
   phthalo blue (69.0) from ultramarine (16.0) and phthalo green (59.4) from cadmium red (11.6),
   which Y does not. Use it as `S_i ∝ 1 / meanKS_i^γ` (γ ≈ 0.5–1, needs calibration), or better,
   keep it as an explicit "how much of this do you need" number in the recipe UI.
3. **Opacity/transparency flag → two-level `S_i`.** Split paints into opaque (cadmiums, cobalts,
   oxides, titanium/zinc whites: `S_i ≈ 0.5–1.0`) and transparent (phthalos, quinacridones,
   dioxazine, ultramarine in acrylic: `S_i ≈ 0.02–0.2`). handprint's RI-ratio table gives the
   assignment. This directly encodes what Y is trying and failing to capture, and matches how
   Curtis et al. distinguish their pigments (opaque = similar colour on white and black; transparent
   = coloured on white, near-black on black —
   [Computer-Generated Watercolor §5.1](https://grail.cs.washington.edu/projects/watercolor/paper_small.pdf)).
4. **Fitted spectrally-nonselective `S_i`, per Abed & Berns.** If you ever get two drawdowns per
   paint (or find their coefficients), fit one scalar S per paint. This is the published state of the
   art for "single number per paint, better than single-constant"
   ([Color Res Appl 42(3):308–318, 2017](https://www.researchgate.net/publication/309540310_Linear_modeling_of_modern_artist_paints_using_a_modification_of_the_opaque_form_of_Kubelka-Munk_turbid_media_theory)).
5. **Keep Y.** Only defensible as a stopgap, and only if renamed to `RelativeScattering` with a
   comment saying it is an unvalidated proxy.

**What I would *not* do:** invent a new formula and call it physics. There is no published
closed-form "tinting strength from sRGB." Any such formula is a heuristic; label it as one.

---

## 4. Spectral reconstruction from RGB — the alternatives

### 4.1 The current approach, characterised

`ToSpectrum` is spectral.js's `lRGB_to_R`: a **non-linear** decomposition (min/max chain) of linear
RGB into 7 non-negative coefficients over 7 fixed basis spectra (W, C, M, Y, R, G, B), then a linear
combination. Provenance: van Wijnen's README credits **Scott Allen Burns' LHTSS method** as the
source of the base spectra
([README](https://github.com/rvanwijnen/spectral.js/blob/3.0.0/README.md)) — i.e. the 7 curves were
themselves generated by Burns' optimiser for the 7 corner/edge colours, then reused as a basis.

**[measured] properties of the shipped implementation:**

- **Round trip is exact.** Over 4000 random 8-bit sRGB triplets, max per-channel error = 0, and
  zero reconstructed spectra out of [0,1]. Good — this matches the "0 % round-trip error" claim in
  the Krita thread.
- **Idempotent.** `mix(A, A, 0.5)` returns A exactly, for black, white, mid-grey and saturates.
- **Mixes go out of sRGB gamut more often than the library's own figure suggests.** van Wijnen
  reports 0.025 % out-of-gamut with max 0.05 % deviation for gradient interpolation. **[measured]**
  for *random pairs* of sRGB colours at random weights: **1.67 % of 2-paint mixes** exceed gamut,
  worst linear-RGB excursion 0.024 (≈ 10 sRGB code values after companding); 0.60 % for 3-paint,
  0.55 % for 4-paint. spectral.js handles this with OKLCh chroma-reduction gamut mapping
  (`gamutMap`, binary search on chroma with ΔE_OK jnd = 0.03); the C# port hard-clamps. That is a
  real behavioural divergence from the source library, and clamping a 0.024 excursion shifts hue.
- Base spectra dip to R ≈ 0.0029 (blue, ~430 nm), and the `MinReflectance = 1e-15` floor only
  engages for exact black or fully-saturated primaries.

**The physical problem:** these 7 curves are *smooth metamers optimised for smoothness*, not paint
spectra. Real paint spectra have features they cannot express — the double-peaked shoulder of
ultramarine, cobalt blue's long-wavelength reflectance rise (Berns picked cobalt blue for his 2019
database precisely because of that unique feature), the sharp yellow absorption edge.

**The decisive evidence:** Berns ran PCA on 58 measured Golden Heavy Body acrylics. Three
eigenvectors captured 97.97 % of spectral variance, six captured 99.72 %. Rotating the first three
into cyan/magenta/yellow-like curves and re-predicting the dataset gave, under a 2200 K blackbody,
**mean CIEDE2000 = 1.8 with range 0.03–9.88**, and his conclusion is blunt: "For this dataset, three
primaries are insufficient to approximate the 58 pigments"
([Berns 2022](https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf)).
Also relevant to this app: **31 % of the 831 real paint-mixture colours in his dataset fall outside
sRGB** (22 % outside AdobeRGB). An sRGB-in/sRGB-out pipeline structurally cannot represent a third
of what acrylics do.

### 4.2 The alternatives

| method | form | round-trip error | bounded [0,1]? | smooth in RGB? | resembles measured spectra? | cost | source |
|---|---|---|---|---|---|---|---|
| **Smits 1999** | linear combo of 7 precomputed spectra (RGB + CMY + W), piecewise constant bins, smoothest-metamer LP | considerable | soft constraint only | yes | "attempts physical plausibility"; ad-hoc RGB+CMY combination | trivial | [Smits, JGT 4(4):11–22](https://www.tandfonline.com/doi/abs/10.1080/10867651.1999.10487511), [PDF](https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.40.9608&rep=rep1&type=pdf) |
| **Burns LSS** | closed-form linear solve, min Σ(ΔR)² | zero | **no** (negatives) | yes | RMM 1.11 | one 36×3 matrix multiply | [arXiv:1710.05732](https://arxiv.org/pdf/1710.05732) |
| **Burns ILSS** | LSS + iterative clipping to [0,1] | zero | yes, with slope kinks | yes | RMM 1.04 | small linear solves | same |
| **Burns LLSS** | min Σ(Δ log R)², Newton on 39 eqns | zero | positive but can exceed 1 | yes | RMM 0.92 | ~6.8 Newton iters, 39×39 | same |
| **Burns ILLSS** | LLSS + iterative cap at 1 | zero | yes | mostly, kinks | RMM 0.86 | most expensive of the set | same |
| **Burns LHTSS** | reparameterise R = (1+tanh z)/2, min Σ(Δz)², Newton on 39 eqns | zero | **yes, strictly (0,1), smoothly approached** | yes | **RMM 0.84 — best of the group** vs 1296 Munsell chips | ~5.66 Newton iters, ~= LLSS, ~60 % of ILLSS | same |
| **Meng et al. 2015** | 2D xy table of smooth metamers | small but nonzero | no (rescaled after, "introduces considerable errors even on sRGB") | yes | smooth only | table lookup, 60.7 KB | [CGF 34(4):31–40](https://onlinelibrary.wiley.com/doi/abs/10.1111/cgf.13626) (discussed in Jakob §2) |
| **Otsu et al. 2018** | 8-cluster kD-tree in xy + per-cluster PCA of 1269 measured Munsell spectra | **zero by construction** | **no** — PCA can leave [0,1]; colour-science exposes a `clip` option that "may cause noticeable colour differences for very saturated colours" | **no** — spectra can change discontinuously across cluster boundaries | **best of the reconstruction methods**, by design | one matrix multiply after tree traversal; ~4.9 KB tables | [PDF](https://cs.uwaterloo.ca/~thachisu/rgb2spec.pdf), [CGF 37(6):370–381](https://onlinelibrary.wiley.com/doi/10.1111/cgf.13332) |
| **Jakob & Hanika 2019** | `f(λ) = S(c₀λ² + c₁λ + c₂)`, `S(x) = ½ + x/(2√(1+x²))`; 3 coefficients | **zero on the full sRGB gamut** | **yes, intrinsically** (sigmoid) | **yes, explicitly optimised for it** (continuation from a stable seed colour) | smooth; can form sharp/rectangular peaks when forced; not data-driven | 6 flops per wavelength; 3 × 64³ tables ≈ 9 MiB, touched once at load | [PDF](https://rgl.s3.eu-central-1.amazonaws.com/media/papers/Jakob2019Spectral_3.pdf), [rgb2spec](https://github.com/mitsuba-renderer/rgb2spec) |
| **Mallett & Yuksel 2019** | `S = r·Sᵣ + g·S_g + b·S_b` with **Sᵣ+S_g+S_b = 1** (partition of unity) and 0 ≤ S ≤ 1 | **zero (to numerical/MC precision)** | **yes** (guaranteed by partition of unity) | **yes** (linear in RGB, so trivially) | "at some cost in terms of smoothness"; not data-driven | **3 multiply-adds, 3 stored spectra** | [PDF](http://www.cemyuksel.com/research/papers/spectral_primary_decomposition.pdf), [EGSR 2019](https://diglib.eg.org/handle/10.2312/sr20191216) |

Burns' "RMM" is his reflectance-match measure against 1296 measured Munsell chips (lower is better)
— the only apples-to-apples "does this look like a real paint" number in the table.

### 4.3 Which of these actually helps *this* app

Ranking by physical plausibility of the resulting *paint* spectrum, at reasonable cost:

1. **Measured spectra for the real tubes (§4.4). Not a reconstruction method — the actual answer.**
2. **Otsu 2018** is the only reconstruction method that is data-driven, and therefore the only one
   whose output resembles a measured curve rather than a smooth metamer. But two of its properties
   are hostile to K-M: it can produce reflectances outside [0,1] (K/S is undefined at R ≤ 0 and
   explodes as R → 0), and it is discontinuous across cluster boundaries (two visually identical
   input colours can get different spectra, so nearby recipes would jump). Both are fixable with
   clamping + a smoothing/blend across boundaries, but it is real work.
3. **Jakob & Hanika 2019** is the best-behaved for a mixing engine: strictly bounded in (0,1) by
   construction, smooth in both λ and RGB, zero round-trip error on sRGB, 3 doubles per colour, and
   6 flops to evaluate. If the app keeps reconstructing, this is the upgrade. The 9 MiB table is the
   only cost and it's load-time only. Note: it is *not* claimed to match measured spectra — it is
   claimed to be smooth and physical. So it fixes the numerical hazards but not the "is this really
   what ultramarine looks like" problem.
4. **Mallett & Yuksel 2019** is a drop-in replacement for the current 7-basis decomposition with a
   *smaller* footprint (3 spectra vs 7) and stronger guarantees (partition of unity ⇒ energy
   conservation; white → flat 1, black → flat 0). It is arguably strictly better than what's there
   now for the same code shape. But it's still three smooth primaries, so Berns' "three primaries are
   insufficient" verdict applies.
5. **Burns LHTSS** is worth knowing about because it is the *most* paint-like of the optimisation
   methods (RMM 0.84) and because the current base spectra came from it. Running LHTSS per pixel is
   too slow (Newton on 39 unknowns), but it is the right tool for **offline generation of a better
   basis or LUT**.
6. **Status quo (7 fixed bases).** Not worse than Smits and it round-trips exactly, so it isn't
   broken — it's just not physically grounded.

### 4.4 The thing to actually do: measured reflectance spectra

The app knows which named acrylics it recommends. That is a closed set of maybe 12–40 tubes.
Measured spectra for exactly that class of paint exist:

- **Berns 2022, 68 Golden Heavy Body acrylics** — masstones + 10 % titanium-white tints, X-Rite
  MS7000 integrating sphere, specular included, **380–730 nm at 10 nm**, plus the derived
  two-constant K and S coefficients and Saunderson constants, plus 831 computed tint/tone spectra.
  Paper: [Berns_Archiving_2022.pdf](https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf).
  **Availability problem:** the resources page now says "the spectral reflectance database mentioned
  in the article is no longer available"; only the image/curves ZIP remains
  ([Gray Sky Imaging resources](https://www.grayskyimaging.com/resources/),
  [ZIP](https://grayskyimaging.com/wp-content/uploads/2022/06/Acrylic_paint_target_and_cleaning_and_varnishing_curves.zip)).
  **Flagged as unverified — I could not download the spectral Excel file.** Berns' email is in the
  2019 paper (berns@cis.rit.edu, now Gray Sky Imaging); the 2016 19-paint version was distributed
  "by request."
- **Berns 2019, 19 Golden Heavy Body acrylics**, 380–750 nm at 10 nm, Macbeth MS7000, 4 measurements
  averaged, Leneta Form 3B opacity charts with a 0.006″ drawdown bar, weights to 0.005 g; 23 hues +
  a grey scale, 770 unique spectra
  ([ArtistSpectralDatabase.pdf](https://www.rit.edu/science/sites/rit.edu.science/files/2019-03/ArtistSpectralDatabase.pdf)).
- **Okumura 2005**, RIT MSc, "Developing a Spectral and Colorimetric Database of Artist Paint
  Materials" — the source Mixbox used for its `k1, k2` Saunderson constants
  ([RG](https://www.researchgate.net/publication/36183327_Developing_a_Spectral_and_Colorimetric_Database_of_Artist_Paint)).
- **Mixbox's own four pigments** are Golden PB15:4, PY73, PR122, PW6, with K and S taken from
  Berns' database, sampled at 10 nm
  ([Mixbox §implementation](https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf)).
  Mixbox itself is available at [scrtwpns.com/mixbox](https://scrtwpns.com/mixbox/) and
  [github.com/scrtwpns/pigment-mixing](https://github.com/scrtwpns/pigment-mixing) — check its
  licence before reuse; the paper's code release is CC-BY-NC-style in some distributions.
- **CHSOS "Pigments Checker" free reflectance database** for modern & contemporary art pigments
  ([chsopensource.org](https://chsopensource.org/chsos-application-note-4/)).

With measured spectra you also get to drop `ToSpectrum` entirely on the *paint* side and keep it only
for the *target* colour from the photo — which is the correct division of labour: reconstruct the
thing you only know as RGB, measure the thing you can measure.

---

## 5. Opacity, hiding power, layering

### 5.1 Finite-thickness K-M over a substrate

For a layer of thickness `X` with coefficients `K, S` over a background of reflectance `R_g`, define

```
a = (S + K)/S = 1 + K/S
b = sqrt(a² - 1)
```

Then the hyperbolic-cotangent form:

```
        1 - R_g·(a - b·coth(b·S·X))
R  =  ───────────────────────────────
        a - R_g + b·coth(b·S·X)
```

Equivalently, with `c = a·sinh(b·S·X) + b·cosh(b·S·X)`, the layer's own reflectance and
transmittance (over a black background) are

```
R_layer = sinh(b·S·X) / c
T_layer = b / c
```

Limits worth encoding as assertions: `X → ∞` ⇒ `R → R∞ = a − b = 1 + K/S − sqrt((K/S)² + 2K/S)`;
`K → 0` ⇒ `R = S·X·R_g...`, reducing to `R = r₀X/(r₀X + 1)` for an ideal white; `S → 0` (pure glaze,
no scattering) ⇒ `R = R_g·exp(−2·K·X)` — Beer–Lambert through the layer twice.

Sources:
[Curtis et al., Computer-Generated Watercolor §5.2](https://grail.cs.washington.edu/projects/watercolor/paper_small.pdf);
[ScienceDirect Kubelka-Munk overview](https://www.sciencedirect.com/topics/engineering/kubelka-munk-theory);
[HandWiki](https://handwiki.org/wiki/Physics:Kubelka-Munk_theory);
review of four coefficient-determination methods for translucent paints (black-white, infinite,
masstone-tint) in
[Zhao & Berns, Color Res Appl 34(6):417–431, 2009](https://onlinelibrary.wiley.com/doi/10.1002/col.20525).

**Numerical warning:** `coth`, `sinh`, `cosh` of `b·S·X` overflow for thick/absorbing layers, and
`(1−R)²/(2R)` loses precision both as R→1 (cancellation) and R→0 (division by near-zero). There is a
whole paper on this:
[Numerical Pathology in Selected Kubelka-Munk Formulas, CIC 29](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/29/1/art00022).
Practical mitigations: branch on `b·S·X` magnitude and use `coth(z) → 1` for `z > ~20`; clamp R to
something like `[1e-4, 1-1e-6]` rather than `1e-15`.

### 5.2 Layer compositing (glazing / scumbling)

Kubelka's compositing equations for two abutting layers with reflectances/transmittances
`(R₁,T₁)` on top of `(R₂,T₂)`:

```
R = R₁ + T₁²·R₂ / (1 - R₁·R₂)
T = T₁·T₂     / (1 - R₁·R₂)
```

Apply repeatedly for each additional glaze. The `1/(1 − R₁R₂)` term is the geometric series of
inter-reflections between the layers, which is what gives glazes their depth. Same source (Curtis
§5.2). The generalised form for dissimilar layers is the Benford relation,
`R_xy = R_x + T_x²R_y/(1 − R_{−x}R_y)`
([HandWiki](https://handwiki.org/wiki/Physics:Kubelka-Munk_theory)).

**This is genuinely different math from mixing**, and both belong in the app if it wants to advise
on glazing:

| | wet-in-wet physical mixing | glazing / scumbling |
|---|---|---|
| operation | one homogeneous layer, coefficients averaged by concentration | ordered stack, reflectances composited |
| math | `K_mix = Σc_iK_i`, `S_mix = Σc_iS_i`, then opaque R∞ | per-layer `(R,T)` from finite-thickness K-M, then `R = R₁ + T₁²R₂/(1−R₁R₂)` |
| commutative? | yes | **no** — order matters, which is the point of glazing |
| needs | K/S per paint | K *and* S per paint, plus a thickness parameter per layer |
| Mixbox status | supported | explicitly **not** supported: "our approach only considers homogeneous mixing of opaque paints… a possible extension left for future work is to add support for the Kubelka-Munk layer-compositing model, which would enable more faithful simulation of translucent layers and handle effects like watercolor glazing" ([Mixbox §limitations](https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf)) |

**A cheap way to get K and S without a spectrophotometer**, from Curtis et al. §5.1: let the user (or
a swatch photo) supply the paint's appearance over white (`R_w`) and over black (`R_b`) at unit
thickness. Then

```
a = ½·( R_w + (R_b - R_w + 1)/R_b )
b = sqrt(a² - 1)
S = (1/(b·X))·arccoth[ (b² - (a - R_w)(a - 1)) / (b·(1 - R_w)) ]
K = S·(a - 1)
```

requiring `0 < R_b < R_w < 1` per channel. Curtis et al. note this occasionally yields K or S > 1
("clearly not possible for any physical pigment") without visible harm in their simulation. Their
diagnostic classification is useful UI language too: opaque paints look similar on white and black
(high S in their own wavelengths); transparent paints look coloured on white and near-black on black
(low S everywhere, high K in complementary wavelengths); interference paints look white on white and
coloured on black. **This is a much better data-entry story for an app than "please buy a
spectrophotometer": two photographed swatches per tube, over a white and a black card.**

---

## 6. Failure modes and sanity tests

Below, "current model" numbers are **[measured]** from the shipped C# reimplementation, using
plausible sRGB stand-ins for each paint (`UB = (31,42,141)`, `CY = (255,196,0)`, `PB = (0,60,120)`,
`HY = (252,222,0)`, `TW = (252,252,250)`, `BK = (0,0,0)`, `CR = (196,30,35)`, `PG = (0,110,90)`).
L\*/C\*/h are CIELAB under D65.

| # | Test | What a correct model must predict | What a naive RGB/subtractive model does | Current model **[measured]** | Verdict |
|---|---|---|---|---|---|
| **T1** | ultramarine + cad yellow, 1:1 | a **muted / olive** green; both pigments carry red bias so it's a near-complementary mix and chroma is suppressed ([Will Kemp](https://willkempartschool.com/the-hidden-secret-of-colour-mixing/), [WetCanvas](https://www.wetcanvas.com/forums/topic/cadmium-yellowultramarine-blue-complementary/)) | RGB lerp → grey/brown; naive multiply → very dark grey | L\*=55.4, C\*=47.2, h=119.4° → sRGB (115,142,57) | **PASS** — hue is green, chroma moderate |
| **T2** | phthalo blue + hansa yellow, 1:1 | a **notably more intense** green than T1 (no red bias in either pigment) | indistinguishable from T1 | L\*=60.2, C\*=**68.1**, h=139.0° → sRGB (59,165,60) | **PASS** — C\* is 1.44× T1. Assert `C*(T2) > 1.3 × C*(T1)` |
| **T3** | tint ladder: phthalo blue + white | value rises **and hue shifts** toward turquoise; chroma **rises to a peak at low white fraction then falls** — Mixbox Fig. 4 shows phthalo blue shifting "from purple to turquoise" as white is added, and notes real paints *gain* saturation with white unlike RGB | RGB lerp toward white: chroma monotonically falls, hue constant | h goes 282.2° → 213.1°; C\* goes 39.9 → peak **41.6 at 20 % white** → 9.6 at 90 % | **PASS on shape** — hue shift and chroma bump both reproduced. But see T4 |
| **T4** | tint ladder **sensitivity at low white** | 10 % white must be a large jump (industry uses a 10 % tint as *the* characteristic sample) | — | **FAIL**: 10 % white moves L\* by only 2.9 (25.5→28.4). Linear weights give 16.0 (25.5→41.5). ΔL\* between the two = **13.1** | **FAIL** — caused entirely by `weight²`. Assert L\* at 10 % white is at least ~12 above masstone |
| **T5** | black + white, 1:1 | a **dark-to-mid** grey. Black pigments have very high tinting strength; a 50/50 volume mix is well below middle grey | RGB lerp → L\*≈54 (sRGB 128); naive multiply → black | L\*=**67.8**, sRGB (165,165,165) | **FAIL, too light.** And the value is an artifact: black's `Y` floors to `MinReflectance` while its `K/S` = `1/(2·MinReflectance)`, so the product `Y·(K/S)` = 0.5 independent of the floor. Pure black's entire mixing behaviour is an accidental cancellation. Assert `L*(50/50 black+white) < 55` |
| **T6** | complementaries, e.g. cad red + phthalo green 1:1 | a **chromatic grey / near-neutral dark**, not pure black; Golden: "Phthalo Green and Naphthol Red Light create a simple black" ([Golden](https://goldenartistcolors.com/resources/color-mixing-guide)) | RGB lerp → mid grey with wrong lightness; naive multiply → near-black | L\*=24.1, C\*=**4.2**, h=9.5° | **PASS** — low but non-zero chroma, dark but not black. Assert `2 < C* < 12` and `15 < L* < 35` |
| **T7** | 3 pigments vs 2 | chroma must **drop sharply**; artists' rule is that beyond ~3 pigments a mix goes muddy fast | RGB lerp toward the centroid — drops but for the wrong reason | C\*: 2-paint (UB+CY) 47.2 → 3-paint (UB+CY+CR) 35.9 → 4-paint (+PG) **21.8** | **PASS** — monotone decrease, 54 % chroma loss by 4 pigments. Assert monotone non-increasing chroma as pigments are added |
| **T8** | round-trip identity | `mix([A], [1])` and `mix([A,A],[w,1−w])` must return A exactly | — | 0 error on 4000 random sRGB; idempotent on all 6 test colours | **PASS** — lock this in as a regression test |
| **T9** | gamut | mixes of in-gamut paints should mostly stay in gamut; excursions must be *mapped*, not clamped | — | **1.67 %** of random 2-paint mixes out of gamut, worst linear excursion 0.024; the port clamps where spectral.js does OKLCh chroma reduction | **WEAK** — clamping shifts hue on ~1 in 60 mixes |
| **T10** | Golden's published recipe | Permanent Green Light = **1 part Phthalo Blue GS : 10 parts Benzimidazolone Yellow Medium** ([Golden](https://goldenartistcolors.com/resources/color-mixing-guide)) | — | not tested (needs the real paint sRGBs) | **Highest-value integration test available.** If the model, given those two paints' colours and a 1:10 ratio, does not land near Permanent Green Light, the concentration model is wrong. Same for Turquoise = 1:1 Phthalo Blue GS : Phthalo Green BS |
| **T11** | strength ordering | phthalo blue must dominate ultramarine by ~40× at equal parts ([handprint](https://www.handprint.com/HP/WCL/pigmt3.html)) | — | Y-based `Strength`: 0.0459 vs 0.0387 (1.18×) → **cannot express this**. Mean K/S: 69.0 vs 16.0 (4.3×) → partially expresses it | **FAIL** — no representation of tinting strength at all. This is the test that justifies §3.3 |

Additional invariants worth asserting cheaply:

- Reflectance after inversion is in (0,1] for every band, for every mix.
- Mixing is order-independent (`mix([A,B],[u,v]) == mix([B,A],[v,u])`) — currently true; keep it.
- Weight scale invariance: `mix(p,[2,1]) == mix(p,[200,100])` — currently true *because* of the
  normalisation; note this is *not* true of `w²` in an absolute sense, only after normalising, so it
  survives. Keep the test anyway.
- Adding a paint at weight 0 must not change the result.
- `Σ` monotonicity: adding more of a dark paint must never *raise* L\*.

---

## 7. Recommended changes, ranked by (impact / effort)

Effort in rough half-days for someone who knows the codebase.

### Tier 1 — high impact, hours

**R1. Delete the `weight²`. Use linear normalised parts.** (impact: very high / effort: 10 minutes
plus test updates)

```csharp
// Concentrations are the paint's share of the mixture by volume; Kubelka-Munk
// mixes absorption and scattering linearly in concentration (Duncan 1940).
double concentration = weights[i] * paints[i].RelativeScattering;
```

This is the difference between the app reporting a truthful ratio and a distorted one. **[measured]**
it changes nothing at 50/50 and up to ΔL\* ≈ 13 at 10:90 — so your existing 50/50 tests will not
catch the change either way. Add T4 first.

**R2. Rename `Strength` → `RelativeScattering` and fix the doc comment.** (impact: high on future
correctness / effort: 20 minutes) Per §1.6 the term occupies the `S_i` slot in Duncan's two-constant
law, not a tinting-strength slot. The current name and comment will actively mislead the next person
who tries to improve it. Also update the class doc: the model is **two-constant K-M with a spectrally
flat per-paint scattering coefficient**, not single-constant.

**R3. Fix the wavelength range in the docs: 38 bands × 10 nm from 380 nm is 380–750 nm, not
380–730.** (impact: prevents a future data-alignment bug / effort: 5 minutes) Verified against the
CMF arrays and against spectral.js/Mixbox. Berns' data is 380–730 (36 bands) — someone will hit this.

**R4. Raise `MinReflectance` from `1e-15` to ~`1e-4` and clamp reflectance on output too.**
(impact: high / effort: 1 hour incl. re-baselining tests) `1e-15` gives K/S = 5e14, and pure black's
entire mixing behaviour currently rides on a cancellation between that and the floored luminance
(§6 T5). Centore's sensitivity numbers show K/S is already numerically hostile below R ≈ 0.02;
there is no reason to go 13 orders past that.
[Numerical Pathology in Selected Kubelka-Munk Formulas](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/29/1/art00022)
is the citation. Expect the black+white ladder to change — verify against T5's target.

**R5. Add the §6 test table as unit tests.** (impact: high / effort: half a day) T8 (round trip,
idempotence) and T1/T2/T6/T7 pass today and are pure regression value. T4, T5, T11 fail today and
document the work. T10 is the highest-value one to add as soon as real paint sRGBs are available.

**R6. Port spectral.js's gamut mapping instead of clamping, or at minimum log/flag out-of-gamut
mixes.** (impact: medium / effort: half a day) **[measured]** 1.67 % of 2-paint mixes exceed sRGB,
worst excursion 0.024 linear. spectral.js's `gamutMap` does a binary search on OKLCh chroma with a
ΔE_OK jnd of 0.03; the algorithm is ~30 lines. For a *recipe* app there's an argument the right
behaviour is to surface "this mix is outside what your monitor can show" rather than silently clamp.

### Tier 2 — high impact, days

**R7. Replace Y with a real per-paint scattering/strength number from a lookup table.** (impact: very
high / effort: 1–3 days, mostly data entry) This is the single biggest accuracy win available without
spectrophotometry. Add to the paint database, per tube:
`relativeScattering` (opaque ≈ 0.5–1.0, semi ≈ 0.2–0.5, transparent ≈ 0.02–0.2) and optionally
`relativeTintingStrength` for the UI. Assignments from handprint's RI-ratio table
([pigmt3.html](https://www.handprint.com/HP/WCL/pigmt3.html)) and Golden's strength ranking
([Color Mixing Guide](https://goldenartistcolors.com/resources/color-mixing-guide)). Keep Y as the
fallback for paints not in the table, but rename and comment it as a guess. Validate against T10 and
T11.

**R8. Use measured reflectance spectra for the paints; keep reconstruction only for the photo pixel.**
(impact: very high / effort: 2–5 days + data acquisition risk) §4.4 has the candidate sources. This
also unlocks true two-constant mixing (you get K and S separately, not just K/S) and makes Saunderson
meaningful. **Risk flagged:** Berns' 2022 spectral Excel is no longer on the site; acquiring it may
require emailing him, and Mixbox's licence needs checking before lifting its four pigments'
coefficients.

**R9. Two-swatch K/S entry (Curtis et al. §5.1).** (impact: high / effort: 2–3 days) If measured
spectra don't materialise, let the user photograph each tube over a white and a black card. The
closed-form inversion in §5.2 gives K and S per channel; extend to per-band by applying it to the
reconstructed spectra of the two swatch colours. This is the pragmatic middle road and it directly
solves the opaque-vs-transparent discrimination that Y cannot (§3.2).

### Tier 3 — worthwhile, larger

**R10. Upgrade the reconstruction to Jakob & Hanika 2019 or Mallett & Yuksel 2019.** (impact: medium
/ effort: 2–4 days) Mallett & Yuksel is the cheaper change — three basis spectra with a
partition-of-unity guarantee replacing seven without one, same code shape, strictly bounded output.
Jakob & Hanika is more robust (strictly bounded by a sigmoid, smooth in RGB, zero sRGB error) but
brings a 9 MiB table and a fitting step. Either removes the class of numerical hazards R4 papers
over. **Neither makes the spectra look like paint** — that's R8's job. Do R8 first if you can.

**R11. Add finite-thickness K-M and layer compositing for glazing.** (impact: medium, new feature /
effort: 3–5 days) §5 has all the equations. Note this needs K and S separately (so it depends on R8
or R9) plus a thickness parameter, and needs the `coth` overflow guards. Mixbox explicitly lists this
as future work, so there's no off-the-shelf reference implementation for pigments — Curtis et al. is
the closest.

**R12. Saunderson correction.** (impact: medium / effort: 1–2 days) Only after R8. `k1 = 0.035`,
`k2 = 0.6` for varnished acrylic per Berns. **Do not apply it to RGB-reconstructed spectra** — the
inverse is undefined for `R ≤ k1` and reconstructed spectra go far below any physical k1 (§1.7).

**R13. Consider Otsu 2018 as an offline LUT generator.** (impact: low-medium / effort: 3+ days) The
only data-driven reconstruction, so the only one whose spectra resemble measurements — but
discontinuous across cluster boundaries and not bounded to [0,1], both of which are worse for K-M
than for rendering. Probably only worth it if R8 fails and you still need plausible *paint* spectra.

---

## 8. Things I could not verify

- **Berns' 2022 spectral Excel file (58 Golden acrylics, K and S coefficients).** The paper describes
  it in detail and says it is downloadable from grayskyimaging.com, but the resources page now states
  "the spectral reflectance database mentioned in the article is no longer available." I could only
  retrieve the paper and the image/curves ZIP.
- **Berns, "Single-constant simplification of Kubelka-Munk turbid-media theory for paint systems — A
  review," Color Res Appl 32(3), 2007** ([doi:10.1002/col.20309](https://onlinelibrary.wiley.com/doi/10.1002/col.20309))
  — paywalled (HTTP 402). This is the most on-point single reference for question 1 and I am relying
  on its title/abstract framing plus the Berns & Mohammadi companion paper.
- **Berns & Mohammadi, Studies in Conservation 52(4):299–314, 2007** — paywalled (HTTP 403). Quoted
  abstract wording comes from search-engine indexing of the abstract, not from the article.
- **Abed & Berns 2017, "Linear modeling of modern artist paints…"** — the abstract (impurity index,
  spectrally nonselective scattering coefficient, 28 matte acrylics) comes from search indexing;
  ResearchGate returned 403 and I could not read the equations. My claim that it validates the
  "one scalar S per paint" approach rests on that abstract only.
- **Yang, Zhu & Pan, "On the Kubelka-Munk Single-Constant/Two-Constant Theories," Textile Research
  Journal, 2010** — the SciSpace PDF returned 403.
- **Centore's "Perceptual Reflectance Weighting" PDF** — I extracted text successfully and quote it,
  but the numbered equations (his eq. 13, 29, 30) did not survive text extraction, so I am citing his
  prose and his worked sensitivity numbers rather than his formulas.
- **Whether the specific base spectra in the C# file were generated by Burns' LHTSS.** van Wijnen's
  README credits "a variation of his LHTSS method"; I did not re-derive them to confirm.
- **The "1 in 60 mixes out of gamut" figure vs van Wijnen's 0.025 %.** These measure different
  things (random colour pairs vs gradient interpolation between chosen endpoints) and I did not
  reproduce his sampling, so they are not directly contradictory.
- **All "real paint" targets in §6 are qualitative** (from Golden, handprint, Will Kemp,
  WetCanvas). I did not find a public table of measured L\*a\*b\* values for specific 1:1 acrylic
  mixes. T10 (Golden's published recipes) is the only test in the table with a manufacturer-stated
  ground truth, and it needs the real paints' sRGB values to run.
- **The paint sRGB values used in all §6 measurements are my own plausible stand-ins**, not measured
  values. The *relative* comparisons (T1 vs T2 chroma, chroma monotonicity in T7) are robust to that;
  the absolute L\*/C\* numbers are not.
