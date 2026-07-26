# Acrylic Paint Blending: Research Findings

**Date:** 2026-07-26
**Scope:** How acrylic paints blend, how to translate RGB photos to paint equivalents,
and how pigment mixing should be modelled in PaintTranslator.
**Method:** Four parallel research tracks (Kubelka–Munk theory, photo pipeline,
acrylic physical reality, prior art). Load-bearing claims re-verified directly —
see [Verification status](#verification-status).

---

## Bottom line

The mixing engine is in better shape than its own comments claim, and the largest
errors are in the **data**, not the math.

Three things to change first:

1. **Delete the `weight²` term.** It silently turns a 1:3 recipe into 1:9. For a
   tool whose entire output is a mixing ratio, this is a correctness bug.
2. **Stop storing paints as sRGB triples.** 31% of achievable Golden acrylic colours
   fall outside sRGB, concentrated in exactly the paints that matter — phthalos,
   quinacridones, cadmiums, dioxazine. Store spectra or K/S; derive sRGB for display only.
3. **Adopt `Wacton.Unicolour`** (MIT) for the physics and measured data instead of
   extending the hand-port. It already has correct two-constant Kubelka–Munk with
   Saunderson correction, and Berns' measured K and S for ~19 Golden Heavy Body
   acrylics *including Titanium White*.

The recipe solver is the one place worth building yourself. No library does
"which two or three of *my* tubes, at what ratio."

---

## 1. How pigment mixing actually works

### Subtractive mixing is spectral, not per-channel

Paint colour comes from wavelength-selective absorption and scattering. Two paints
mixed together reflect only what *both* fail to absorb. This is why ultramarine plus
cadmium yellow makes green: ultramarine's reflectance still has a green shoulder even
though its sRGB green channel is nearly zero, and that shared reflectance is all that
survives. Averaging RGB channels cannot reproduce this — it gives grey.

### Kubelka–Munk, single- and two-constant

**Single-constant** tracks only the ratio K/S, and mixes it linearly by concentration:

```
K/S = (1-R)² / 2R                     (per wavelength)
(K/S)_mix = Σ cᵢ (K/S)ᵢ ,  Σcᵢ = 1
R = 1 + K/S − √((K/S)² + 2·K/S)       (opaque film inversion)
```

It assumes every colorant scatters alike — acceptable for dyes on an opaque base,
wrong for paints where titanium white scatters enormously and a quinacridone barely
scatters at all.

**Two-constant** tracks K and S separately, which is what the paint and textile
industries use:

```
K_mix = Σ cᵢ Kᵢ ,  S_mix = Σ cᵢ Sᵢ
R = 1 + (K/S) − √((K/S)² + 2(K/S)) ,  K/S = K_mix / S_mix
```

Calibration convention: **S of titanium white is normalised to 1.0 at every
wavelength**, and all other pigments' K and S are relative to it. Unicolour's data
follows this — its `TitaniumWhite.s` array is 38 values of exactly `1.0`.

Getting K and S for a paint needs **two measurements, because there are two unknowns**:
masstone plus a tint at known ratio. Golden's own lab pair is **masstone + 1:10 tint
with titanium white**. Curtis et al. give a closed form from a swatch drawn over
white card and black card — a realistic data-entry path if you ever let users
characterise their own tubes.

### Saunderson correction

Real measurements include light reflected off the film surface before it ever reaches
pigment. The correction (SPEX-mode form, as Unicolour implements it):

```
R_corrected = (1−k₁)(1−k₂)·R / (1 − k₂·R)
```

Unicolour uses `k₁ = 0.03, k₂ = 0.65`. It changes K/S by 0.19× in light bands up to
2.05× at R = 0.05. **It is undefined for R ≤ k₁**, so it must only be applied to
measured spectra, never to reconstructed ones.

### Glazing is different math from mixing

Wet-in-wet mixing combines K and S. A transparent glaze over a dry layer is layer
compositing over a substrate, needing the finite-thickness K–M form rather than the
opaque inversion. These are separate features; don't model one with the other.

---

## 2. What the current code actually does

Reading `Imaging/SubtractivePaintMixer.cs` against the theory produced one genuinely
surprising result.

### It is already two-constant Kubelka–Munk

The comments say single-constant. The algebra says otherwise. Given
`cᵢ = wᵢ²·Yᵢ` and `Σcᵢ(K/S)ᵢ / Σcᵢ`, substitute and it expands to:

```
Σ dᵢ·Kᵢ / Σ dᵢ·Sᵢ     with    Sᵢ ≡ Yᵢ ,  Kᵢ ≡ Yᵢ·(K/S)ᵢ
```

That is Duncan's 1940 two-constant law with a spectrally flat scattering coefficient
per paint. Abed & Berns published almost this structure — "a spectrally nonselective
scattering coefficient for each chromatic component" — validated on 28 matte acrylics.

**Consequence: the fix is a better `Sᵢ` estimate, not a restructure.**

### `weight²` is a cosmetic fudge, and it breaks recipes

spectral.js documents its concentration as `C = f²·T²·L`. The squaring has no physical
basis; the author's stated reason is that straight K–M mixing looks "a bit dark." A
GitHub issue asking why received no maintainer reply.

Numerically it is a smoothstep with slope exactly 2.0 at w = 0.5. So:

- 50/50 mixes are **unaffected** — which is why the model looks fine in the blend strips.
- **1:3 is executed as 1:9.**
- Adding 10% white to phthalo blue moves L\* by 2.9 when it should move 16.0 (ΔL\* = 13).

A gradient renderer can afford this. A tool that prints "3 parts A to 1 part B" cannot.

### `Strength` is backwards from what tinting strength means

The field holds CIE-Y luminance and is documented as tinting strength. Phthalo blue
has roughly 2.4× ultramarine's tinting strength by tube volume, but their Y values are
0.046 and 0.039 — a factor of 1.18. Luminance cannot express it. Mean K/S can: 69.0
versus 16.0.

The redeeming detail is that in the two-constant algebra above, `Yᵢ` occupies the
**scattering** slot, and luminance is a crude but defensible scattering proxy. So
rename it `RelativeScattering` rather than deleting it, and source real S values when
available.

### Smaller defects

| Issue | Detail |
|---|---|
| **Wavelength range mis-documented** | 38 bands × 10 nm from 380 nm is **380–750 nm**, not 380–730 as stated in the class docs. Verified by arithmetic and independently by two tracks. Harmless now (tables are copied verbatim) but a live bug the moment measured 380–730/36-band data is merged. |
| **`MinReflectance = 1e-15`** | Governs black mixing through an accidental cancellation where `Y·K/S = 0.5` regardless of the floor. 50/50 black + white yields L\* 67.8 — too light. Raise to ~1e-4. |
| **Gamut handling** | 1.67% of mixes clamp to sRGB instead of gamut-mapping, diverging from spectral.js's OKLCh mapping. |
| **Dropped parameter** | The port hard-codes spectral.js's `tintingStrength` (`T`) to 1. |

### The spectral.js author's own position

From issue #22: *"Spectral.js is not built for realism. It is specifically built for
sRGB input and output."* He directs anyone wanting realism to spectrophotometer
measurements plus two-constant K–M. Issue #24 confirms the luminance weighting is a
deliberate perceptual choice, which means **a returned weight is not a mass fraction**.
Issue #23 is a user asking this project's exact question, closed unanswered.

The library is well built for what it targets. This project targets something else.

### Sanity tests: current model

Already passing — worth locking in as regression tests:

- Ultramarine + cadmium yellow → green
- Phthalo blue + hansa yellow → intense green
- Complementaries → chromatic greys, not black
- Chroma decreases monotonically with pigment count
- sRGB round trip exact over 4,000 colours

Failing: three tests, plus weak gamut-mapping behaviour. Tint-direction accuracy
(adding white) is the notable failure, and it traces to `weight²`.

---

## 3. Translating an RGB photo to paint

### The mismatch

A photo is an emissive sRGB encoding assuming D65 and a specific surround. Paint is a
reflective surface under whatever light the room has. The pipeline should be:

```
sRGB (piecewise decode) → linear RGB → XYZ → [chromatic adaptation] → CIELAB → match
```

Use **Bradford**, not CAT02, for adaptation: CAT02 can emit negative tristimulus
values, which would break the downstream K–M model.

**Skip CAM16-UCS.** It performs best in the literature, but you cannot honestly supply
adapting luminance, surround, or background for a hobbyist's unknown room, and the
benefit is small next to a 24 L\*-unit black-point deficit.

### The paint gamut is not a subset of sRGB

This inverts the intuitive framing. Berns measured 831 Golden tints, tones and
masstones: **31% fall outside sRGB, 22% outside AdobeRGB.** sRGB's yellow corner is
L\*97.1 C\*96.9 h103°; Golden Cadmium Yellow Medium is L\*84.1 C\*95.5 h82°. Neither
contains the other.

So storing paints as sRGB triples is lossy *before mixing starts*, and it is worst for
the chromatic paints users care about most.

### Value range is the binding constraint

Golden's published measurements:

| Paint | L\* | a\* | b\* |
|---|---|---|---|
| Titanium White | 98.25 | −0.74 | 1.24 |
| Bone Black | 23.82 | −0.05 | −0.45 |
| Cadmium Yellow Medium | 84.13 | 12.86 | 94.59 |

White costs about 2 L\* units against paper white. **Black costs 24.** The palette
spans 74 L\* units, roughly 24:1 contrast — lower than a magazine page.

Every photo shadow below L\*24 currently collapses onto a single black. That is the
classic ICC "flat, plugged-up shadow" failure. The fix is **sigmoidal L\* rescaling**
into the palette's real range before matching, which is also what painters do: preserve
value *relationships*, sacrifice absolute range.

### Colour-difference metric: split the roles

`PaintBlendMatcher` uses squared CIELAB distance (ΔE76), which mis-ranks saturated
blues. But ΔE2000 is not the right replacement for *searching*:

- CIEDE2000's **recommended range of use is 0–5 ΔE units**. Sharma & Bala showed its
  built-in discontinuities are bounded at 0.274 below 5 units and rise sharply above.
- Gamut-limited paint residuals routinely run **10–40 units**.

Recommended split:

- **Search with HyAB:** `|ΔL*| + √(Δa*² + Δb*²)` (Abasi, Tehran & Fairchild 2020).
  Validated for large differences, beat both CIELAB and CIEDE2000 in their experiment,
  and admits an exact √2 pruning bound against a Euclidean k-d tree — which ΔE2000
  does not.
- **Refine and report with ΔE2000**, where residuals are already small and it is valid.

Report thresholds to the user as: ΔE2000 ≈ 1 just noticeable, 2–3 noticeable side by
side, >5 clearly different.

### Weight lightness more heavily

Weight ΔL\* by about **1.5**, justified by measured perceptibility thresholds
(ΔL′ 1.04 vs ΔC′ 1.58, ratio 1.52).

One caveat worth preserving: the literature points both ways. Industrial acceptability
convention uses kL = 2, weighting lightness *less*. That convention answers "is this
batch in tolerance," not "does this read like the photo." Weighting up is right here —
but do not cite kL = 2 as support for it.

### Segment before matching, and don't dither

Match ~200–2,000 region colours rather than 12M pixels: a 100–600× speedup that also
makes metric cost irrelevant. Use edge-preserving prefiltering, then SLIC superpixels
or mean-shift, then minimum-region-size cleanup.

Don't dither. The usual explanation — "you can't dither a brushstroke" — is wrong;
pointillism is dithering, and partitive mixing genuinely avoids subtractive saturation
loss. The real reasons are that error diffusion leaves no contiguous paintable regions,
dot size is set by the viewer's distance rather than the algorithm, and wet acrylic
touching wet acrylic mixes subtractively anyway.

### Input error sets a precision ceiling

Uncalibrated phone photos carry a measured error floor of **ΔE 2.1 in daylight, 4.2
under tungsten** — at or above the ΔE 1.8 acceptability threshold. Chasing sub-ΔE-1
precision in the mixing model is pointless while the input is that noisy.

An optional ColorChecker calibration step plausibly reaches ΔE ≈ 1, and no surveyed
phone app offers it.

---

## 4. Acrylic paint reality

### Masstone versus undertone

A paint straight from the tube (masstone) and the same paint spread thin or mixed with
white (undertone) can look almost unrelated. Phthalo blue is near-black at masstone and
brilliant cyan in tint. Quinacridone crimson is dark red at masstone, clear pink in tint.
The cause is concentration and the balance between absorption and scattering at
different film thicknesses.

**Store both: masstone + 1:10 tint with titanium white.** That is Golden's own
characterisation pair and the minimum for two-constant K–M.

**This is already a live bug.** `Data/GoldenPalette.cs` is internally inconsistent:
"Ultramarine Blue" `(50,47,75)` is a masstone value, while "Light Ultramarine Blue"
`(100,160,230)` and "Light Phthalo Blue" `(143,217,242)` are tint-like.
`SubtractivePaintMixer` treats every entry as masstone, so those tint entries mix as
though they were dense pigment.

### Tinting strength, quantified

Relative strength **by tube-paint volume**, which is how users actually mix:

| Comparison | Ratio |
|---|---|
| Phthalo Blue GS vs Cerulean Blue Chromium | ≈ 7× |
| Phthalo Blue GS vs Ultramarine Blue | ≈ 2.4× |
| Phthalo Blue GS vs Cobalt Blue | ≈ 3.7× |
| Phthalo Blue GS vs Phthalo Green BS | ≈ 2× |
| Carbon Black vs Cerulean Blue Chromium | ≈ 21× |
| Cadmium Yellow Light vs Phthalo Blue GS | ≈ 0.011× |

**Do not use MacEvoy's well-known "phthalo is 40× ultramarine."** That figure is
pigment-versus-pigment **by mass**. Tube paints differ in pigment loading and density,
so the volumetric figure is ~2.4×. Both numbers are correct about different things;
only the volumetric one belongs in a mixing app.

### Why mixtures go muddy

Each pigment absorbs a broad band, so combining several leaves little reflected
anywhere. Chroma retention in equal-parts mixes:

| Pigments | Chroma retained |
|---|---|
| 2 | 51% |
| 3 | 35% |
| 4 | 28% |
| 5 | 23% |

By hue separation between the two parents:

| Separation | Chroma retained |
|---|---|
| < 60° | 68% |
| 60–90° | 41% |
| 90–120° | 22% |
| 120–150° | 14% |

This puts hard numbers behind the traditional rule: **two pigments plus white, three
maximum.**

### Recipe precision has a hard floor

ΔE00 tracks *relative* ratio error, and ~10% relative error is only ΔE00 1–3. So
37%/63% versus 40%/60% is **ΔE00 ≈ 0.4 — below the just-noticeable difference.**

Snap recipes to a geometric ladder — 1:1, 1:1.5, 1:2, 1:3, 1:5, 1:8, 1:12, 1:20 — and
**never print percentages**. Precision the user cannot execute is false precision.

One open question: K–M concentration is a pigment mass or volume fraction, while users
measure tube-paint volume. Pigment loading and density vary widely (cadmiums and
titanium white are dense; phthalos and organics are light). State which basis the app
reports in, and prefer volume since that is what a palette knife measures.

### Wet-to-dry shift: less of a problem than expected

Acrylics dry darker because the milky emulsion clears as the binder's refractive index
goes from ~1.33 to ~1.49. The mechanism is well documented; **no measured ΔE for the
shift appears to be published.**

The corollary removes the worry: Golden's published Lab values are **already dry-film
measurements**. The app needs no correction — only a UI advisory that wet paint on the
palette will not match the target until it dries.

### Opacity ratings are not measurements

Golden concedes its opacity rating is judgement, not measurement — it ranks Phthalo
Blue "on par with Cobalt Blue, Pyrrole Red, and Cadmium Orange" in a 10 ml drawdown.
The chart's **Gloss Average** column *is* a real measurement and a better transparency
proxy.

### Single-pigment versus convenience mixes

Single-pigment paints mix predictably. Multi-pigment convenience colours (most "Hues",
sap green, flesh tint) already contain 2–3 pigments, so mixing two of them stacks 4–6
pigments and lands in the 23–28% chroma-retention band above. Flag convenience colours
in the UI and prefer single-pigment paints in recipes.

---

## 5. Data sources

### Verified and usable

**GoldenSpectra.zip** — `https://www.realtimerendering.com/downloads/GoldenSpectra.zip`
Golden granted permission to redistribute; curated by Glassner & Haines.

- Downloaded and inspected: one xlsx, `A1:BQ80`, exactly **78 paint names**
- Columns: `% Reflectance`, `K/S`, `Prod #`, `Name`, `L*`, `a*`, `b*`
- Measurement conditions stated in the sheet: **D65, 10° observer**
- **Caveat 1:** the file is "10 mil **Drawdowns over White**" — not masstone over
  black. Opaque paints approximate masstone; transparent paints are substrate-
  contaminated, which is why K/S saturates with reflectance flooring at 3.7–4.6%.
  Using it properly needs the finite-thickness K–M form, not the opaque inversion.
- **Caveat 2:** **no Titanium White and no Zinc White row.** Only Titan Buff,
  Titanate Yellow, Cobalt Titanate Green. White must be sourced elsewhere.

**Golden Heavy Body Pigment Detail Chart** —
`https://goldenartistcolors.com/products/golden-artist-acrylics/heavy-body/pigment-data`
Plain HTML, ~154 rows, trivially scrapable. Publishes per colour: item number, series,
**pigment CI name**, opacity/transparency, lightfastness, Munsell notation, gloss
average, viscosity range, **CIE L\*a\*b\***, and a **numeric tint strength**.

Note: the web chart states **no** illuminant, observer, or geometry, and does not say
whether values are masstone or tint. Do not assume it matches the spectra file's D65/10°.

**Wacton.Unicolour** — MIT, © 2022–2026 William Acton. See §6.
Its `ArtistPaint` dataset **includes PW6 Titanium White** at 380 nm / 10 nm / 38 bands,
filling the exact gap in GoldenSpectra.zip.

### Restricted or unavailable

| Source | Status |
|---|---|
| LBNL/Liquitex database | Masstone + 1:4 + 1:9 tints, K/S in mm⁻¹, but spectral files are AES-encrypted with the key restricted to project members |
| Berns 2022 Excel | Withdrawn; available by request only |
| RIT 2016 19-paint set | Includes PW6 and PBk9; "available by request" |
| CHSOS Pigments Checker | Genuinely open, but oriented to pigment identification |
| artistpigments.org | HTTP 403 — needs a human with a browser |

### Trust warning

Manufacturer web swatches are notoriously inaccurate. Prefer the measured datasets
above over scraped colour-chart images, and treat any palette derived from marketing
imagery as provisional.

---

## 6. Build versus borrow

### Adopt: Wacton.Unicolour (MIT)

Pure C#, .NET Standard 2.0, zero dependencies, actively maintained. It already contains
what this project hand-rolled, done properly:

- **`Pigment.cs`** — two-constant K–M with Saunderson correction, plus single-constant.
  Verified by reading the source: weights are normalised to concentrations
  (`x / totalWeight`) with **no squaring and no luminance term**. Correct
  `K_mix/S_mix` formulation.
- **`Unicolour.Experimental/PigmentGenerator.cs`** — Scott Burns' LHTSS reflectance
  reconstruction: the principled version of spectral.js's 7-base-spectra decomposition.
- **`Unicolour.Datasets/ArtistPaint.cs`** — Berns' measured two-constant K and S for
  ~19 Golden Heavy Body acrylics with colour indices (PW6, PB15, PR122, PY74, PBk9…),
  `K1 = 0.03, K2 = 0.65`, and titanium white's S normalised to 1.0.
- Also ships CIEDE2000, gamut mapping, and a spectral.js port for comparison.

Adopting this replaces `SubtractivePaintMixer` with a maintained, measured-data-backed
equivalent, and gives you the white paint the Golden spectra file lacks.

### Reject: Mixbox

Authors are **Šárka Sochorová and Ondřej Jamriška** (SIGGRAPH Asia 2021,
DOI 10.1145/3478513.3480549). Licensed **CC BY-NC 4.0**; commercial licensing is by
email quote with **no published price**. A C# binding exists (NuGet `Mixbox 2.0.0`).

Setting licence aside, it is the wrong tool: it is locked to four fixed surrogate
primaries and **cannot express "mix these three of my tubes."** That is precisely this
app's job.

### Licence notes — check before porting anything

| Project | Licence |
|---|---|
| spectral.js | MIT, © 2025 Ronald van Wijnen |
| Wacton.Unicolour | MIT, © 2022–2026 William Acton |
| Mixbox | CC BY-NC 4.0 (commercial by quote) |
| Centore Kubelka-Munk Toolbox | **GPLv3 — do not port** |
| Lindemeier PaintMixer | LGPL-3.0 |
| Tan Pigmento / layer decomposition | **No LICENSE file → all rights reserved** |
| ImageSharp | **Six Labors Split License**, not Apache-2.0 |
| Accord.NET | LGPL-2.1, archived |

---

## 7. Recipe solving

Replace brute-force fixed-ratio enumeration with the industrial standard. The relevant
prior work is Allen's two-stage algorithm (JOSA 56:1256, 1966; 64:991, 1974), Centore's
constrained-least-squares / convex-polytope formulation, and Lindemeier's sparsity term.

### Recommended algorithm

```
for each candidate subset S of paints, |S| ≤ 3:          # exhaustive
    c ← weighted NNLS: minimise ‖ W·(KS_target − Σ cᵢ·KS_i) ‖²
                        subject to  cᵢ ≥ 0,  Σcᵢ = 1     # simplex-constrained
    c ← damped Gauss-Newton refinement on ΔE2000(mix(c), target)
    score ← HyAB(mix(c), target)                          # valid at large residuals
keep best; snap c to the ratio ladder; re-evaluate; report ΔE2000
```

Exhaustive subset enumeration is **provably optimal** and trivially interactive:
**2,324 subsets at 24 paints, 36,050 at 60.** That removes the current "skip triples
above 30 paints" cliff entirely.

**Reject orthogonal matching pursuit.** Paint libraries have high mutual coherence, so
greedy selection makes early mistakes it cannot correct.

Lindemeier's Hoyer sparsity term (`a_sum = 0.5, a_sp = 0.1`) is the alternative if you
prefer a single continuous optimisation over subset enumeration.

**Prefer spectral matching over tristimulus matching.** A spectral match is robust
under changing gallery light; a tristimulus match is metameric and can fall apart when
the room light changes. Warn the user when a chosen recipe is strongly metameric.

### Most relevant paper

Lindemeier, Gülzow & Deussen, *Painterly Rendering using Limited Paint Color Palettes*,
VMV 2018 — computes mixture recipes from real base paints using Kubelka–Munk, for a
24-pot painting robot. Free PDF. Closest published match to this project's core problem.

---

## 8. Competitive landscape

**"We use Kubelka–Munk" is table stakes.** Impasto (iOS, free) already advertises
spectral K–M at 36 wavelengths, photo input, the user's own tube list, and output like
"3× Burnt Umber + 1× Yellow Ochre + 1× Titanium White — 99% match." Mixable, Real Color
Mixer, and Golden's own MXR are in the same space. ArtistAssistApp is AGPL prior art
doing nearly this job.

Every open-source paint-by-numbers generator, by contrast, stops at k-means posterise
plus nearest-palette-colour. **None computes a mixture.**

Available differentiators:

- Offline photo pipeline with proper gamut and value-range mapping
- Measured two-constant data rather than sRGB-reconstructed spectra
- Exact best-subset recipes with a stated ΔE2000 and honest "this is as close as your
  palette gets"
- Ratios snapped to what a human can actually execute
- Metamerism warnings
- Optional ColorChecker calibration — no surveyed competitor offers it

---

## 9. Prioritised changes

### Tier 1 — hours of work, high impact

| # | Change | File |
|---|---|---|
| 1 | Delete the `weight²` term; use weights as normalised concentrations | `SubtractivePaintMixer.cs` |
| 2 | Fix the 380–730 → **380–750** doc error; add explicit start/interval constants | `SubtractivePaintMixer.cs` |
| 3 | Rename `Strength` → `RelativeScattering`; correct its doc comment | `SubtractivePaintMixer.cs` |
| 4 | Raise `MinReflectance` from 1e-15 to ~1e-4 | `SubtractivePaintMixer.cs` |
| 5 | Switch search metric from squared ΔE76 to **HyAB**; weight ΔL\* by 1.5 | `PaintBlendMatcher.cs` |
| 6 | Report ΔE2000 plus a verbal band and error *direction* to the user | UI |
| 7 | Snap recipes to the geometric ratio ladder; stop printing percentages | `PaintBlendMatcher.cs` |
| 8 | Lock the five passing sanity tests as regression tests | `Tests/` |
| 9 | Golden-test the port against spectral.js v3 via Node before refactoring | `Tests/` |

### Tier 2 — the real fixes

| # | Change |
|---|---|
| 10 | Ingest GoldenSpectra.zip; store reflectance/K-S per paint, derive sRGB for display only |
| 11 | Adopt `Wacton.Unicolour` for K–M and its measured `ArtistPaint` data (supplies Titanium White) |
| 12 | Scrape the Golden pigment chart for CI names, tint strength, gloss; flag convenience colours |
| 13 | Resolve the masstone/tint inconsistency in `GoldenPalette.cs` |
| 14 | Sigmoidal L\* rescaling into the palette's real range before matching |
| 15 | Replace ratio-grid sampling with best-subset NNLS + Gauss-Newton; delete the 30-paint cliff |
| 16 | Segment into regions before matching |

### Tier 3 — later

| # | Change |
|---|---|
| 17 | Obtain masstone + 1:10 tint pairs for full two-constant K and S |
| 18 | Finite-thickness K–M for glazing as a separate feature from wet mixing |
| 19 | Optional ColorChecker calibration |
| 20 | Metamerism warnings |

---

## 10. Conflicts between tracks, resolved

| Conflict | Resolution |
|---|---|
| ΔE2000 as the search objective vs HyAB | **Split the roles.** HyAB for search (valid at 10–40 ΔE residuals), ΔE2000 for Gauss-Newton refinement and user reporting, where residuals are small and it is valid. |
| Phthalo blue "40×" vs "2.4×" ultramarine | Both correct about different things. 40× is pigment-vs-pigment **by mass**; 2.4× is tube paint **by volume**. Use **2.4×** — users mix tubes, not pigment powder. |
| Report ratios by weight vs by volume | **Volume.** A palette knife measures volume. Note the basis explicitly, since K–M concentration is properly a pigment fraction and tube loadings differ. |
| Single- vs two-constant K–M | Moot — the code is already two-constant. Improve the S estimate. |

---

## Verification status

**Verified directly in this session, not taken from agent reports:**

- Golden pigment-data page exists and publishes the columns listed, including numeric
  tint strength. Confirmed values: Titanium White L\*98.25 a\*−0.74 b\*1.24; Bone Black
  L\*23.82 a\*−0.05 b\*−0.45; Cadmium Yellow Medium L\*84.13 a\*12.86 b\*94.59.
- GoldenSpectra.zip downloaded (49,236 bytes) and unpacked. 78 paints, `A1:BQ80`,
  D65/10°, "10 mil Drawdowns over White", no Titanium or Zinc White row.
  **Requires a browser User-Agent** — plain `curl` returns 403.
- Unicolour is MIT (© 2022–2026 William Acton). `Pigment.cs` read in full: two-constant
  K–M, Saunderson SPEX form, linear concentration normalisation, no squaring.
  `ArtistPaint.cs` contains PW6 Titanium White, 380 nm / 10 nm / 38 bands, S ≡ 1.0.
- 38 bands × 10 nm from 380 nm = 380–750 nm, by arithmetic.
- `GoldenPalette.cs` errors: Bone Black `(35,34,36)` → L\*≈13.7 vs published 23.82;
  Titanium White `(255,247,255)` has both a\* and b\* signs inverted versus published.

**Reported by agents, not independently checked** — verify before relying on:

- Berns' 31%-outside-sRGB and 3-primaries ΔE00 figures (source papers are paywalled)
- The derived tinting-strength index, which assumes Golden's tint-strength column is
  the tint's L\* (strong evidence, unconfirmed)
- Chroma-retention and hue-separation percentages (derived from the Golden spectra by
  the agent, not re-derived here)
- Phone-photo ΔE error floors
- HyAB's √2 pruning bound and the Abasi/Tehran/Fairchild experimental result
- Competitor feature claims (Impasto et al.)

**Known unavailable:** Mixbox commercial pricing (unpublished), artistpigments.org
(HTTP 403), Berns 2022 Excel (withdrawn), LBNL spectral files (encrypted).

**Gaps with no published answer:** measured ΔE for the acrylic wet→dry shift, and human
volumetric mixing accuracy.
