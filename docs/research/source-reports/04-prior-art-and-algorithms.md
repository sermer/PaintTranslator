# Research 04 — Prior Art, Libraries, and Recipe-Solving Algorithms

**Date:** 2026-07-26
**Scope:** implementations and algorithms only (K–M theory rigor, photo pipeline, and paint
physical data are covered by agents 1–3).

**Headline finding:** there is an MIT-licensed, zero-dependency, .NET Standard 2.0 C# library —
**Wacton.Unicolour** — that already ships (a) two-constant Kubelka–Munk with Saunderson
correction, (b) Scott Burns' LHTSS reflectance reconstruction from sRGB, (c) a spectral.js port,
and (d) **Roy Berns' measured two-constant K and S data for 19 Golden Heavy Body acrylics**.
That is most of the mixing engine this project hand-rolled, plus the physical data it currently
lacks. See §3.5 and the build-vs-borrow recommendation at the end.

---

## 1. Mixbox (Secret Weapons)

### 1.1 Correct citation — the author list in the task brief is wrong

**Šárka Sochorová and Ondřej Jamriška.** *Practical Pigment Mixing for Digital Painting.*
ACM Transactions on Graphics 40(6), Article 234 (Proc. SIGGRAPH Asia 2021), 11 pages.
DOI **10.1145/3478513.3480549**.

Two authors only. Both were at Czech Technical University in Prague, FEE **and** Secret Weapons.
Alexander Wilkie is **not** an author. Daniel Sýkora is **not** an author — he is thanked in the
acknowledgments ("Daniel Sýkora and the anonymous reviewers for their insightful comments").
Correct any citation string that lists four authors.

- Paper PDF (free, publisher-hosted): <https://scrtwpns.com/mixbox.pdf> (4 MB)
- Paper PDF (free, CTU mirror): <https://dcgi.fel.cvut.cz/wp-content/wpallimport-dist/publications/pdf/publications-2021-sochorova-tog-pigments-paper.pdf>
- Project page: <https://dcgi.fel.cvut.cz/en/publications/2021/sochorova-tog-pigments/>
- ACM DL: <https://dl.acm.org/doi/10.1145/3478513.3480549>
- Product site: <https://scrtwpns.com/mixbox/> · Docs: <https://scrtwpns.com/mixbox/docs/>
- Library repo: <https://github.com/scrtwpns/mixbox>
- Research reference implementation repo referenced in the paper: `https://github.com/scrtwpns/pigment-mixing`
  (now redirects/mirrored; a copy is at <https://github.com/0xchaosbi/pigment-mixing>)

### 1.2 The method, accurately

Everything below is read straight out of the paper text.

**Primaries.** Exactly **four**: Phthalo Blue, Quinacridone Magenta, Hansa Yellow, Titanium
White — "as suggested by Briggs [2007]" (huevaluechroma.com). Concretely they use four **Golden
Artist** acrylics **PB15:4, PY73, PR122, PW6**, taking absorption/scattering coefficients from
**Berns' Artist Paint Spectral Database** (CIC24, 2016). Coefficients sampled 380–750 nm at
10 nm. (The paper says "36 wavelengths" and "288 variables" = 4 pigments × 2 coefficients × 36;
380–750 at 10 nm is actually 38 samples, so the paper is internally slightly inconsistent — see
the band-count note in §2.3.)

**Forward model `mix_P(c)`** (paper eqs. 1–7): two-constant K–M mixing,
`K(c,λ) = Σ cᵢKᵢ(λ)`, `S(c,λ) = Σ cᵢSᵢ(λ)` (Duncan 1940), then opaque K–M
`R = 1 + K/S − √((K/S)² + 2K/S)`, then **Saunderson (1942)** surface correction
`R' = (1−k₁)(1−k₂)R / (1 − k₂R)`, then CIE 1931 2° × D65 integration and the sRGB matrix.

**Inverse `unmix_P(RGB)`** (eq. 9) — this is the important part for us:

```
unmix(RGB) = argmin_c ‖ mix_P(c) − RGB ‖²
             s.t.  cᵢ ≥ 0 ,  Σ cᵢ = 1
```

solved with **automatic differentiation + L-BFGS-B**, initialised at `c₀ = (0.25,0.25,0.25,0.25)`.
It takes **up to 100 ms per colour** — which is exactly why they precompute it.

**Latent space.** 7-dimensional: `z = [c₁ c₂ c₃ c₄ | r_R r_G r_B]` = 4 concentrations plus a
3-channel **additive residual** `r = RGB − mix(unmix(RGB))`. Encoder `F(RGB) = z`,
decoder `G(z) = mix(c) + r`. Mixing = plain linear combination of latents:
`G( Σ wᵢ F(RGBᵢ) / Σ wᵢ )`. The residual is what makes the round trip exact and makes
non-pigment-reachable sRGB colours behave, i.e. it is a *fudge that carries the un-mixable part of
the colour as light rather than pigment*. `LATENT_SIZE = 7` is confirmed in the shipped
`python/mixbox.py`.

**Why ultramarine + yellow → green works.** Nothing magic: the latent decomposition converts each
RGB into concentrations of *real measured pigments*, mixes those concentrations, and re-runs the
full two-constant K–M + spectral integration. The green comes from the physics (scattering *and*
absorption per wavelength), not from a hue-space trick. The paper explicitly shows RYB
(Gossett & Chen 2004), purely subtractive, and subtractive-additive (Simonot & Hébert 2014) all
predicting the wrong hue and/or too-dark results (Figs. 5, 6).

**Surrogate pigments (§3.1) — an idea worth stealing conceptually.** Mixtures of the real four
primaries leave the sRGB gamut, which breaks invertibility. Rather than gamut-compress after
mixing, they *optimise the pigments themselves*: solve for surrogate `Q` minimising
`α·E_gamut(Q) + E_fidelity(Q,P)` where `E_gamut` penalises the mixture gamut boundary protruding
outside `[0,1]³` (signed distance squared) and `E_fidelity` penalises Oklab deviation from
`P`-mixtures, with `α` annealed from 1e5 by halving, autodiff gradients, L-BFGS-B, 288 variables.
Measured bias: CIEDE2000 histogram peaks near ΔE 1 (just-noticeable) and falls off fast.

**LUTs.** `unmix` precomputed over all 8-bit RGB into a 256³ table storing 3 of the 4
concentrations (4th is `1 − Σ`), 8-bit quantised = 48 MB; `mix` likewise 48 MB; 96 MB total,
compressed losslessly as two 4096×4096 PNGs ≈ 7 MB on disk. **Note the shipped v2.0 library is
much leaner than the paper:** `mixbox_lut.png` is 176 KB and the decoder is a **20-term cubic
polynomial** in the four concentrations plus the residual (`mixbox_eval_polynomial`,
`mixbox_latent_to_rgb`) — so the `mix` LUT was replaced by a fitted polynomial. Runtime cost:
2×–3× ordinary RGB lerp, median 2.3×, both under 16 ms in their painting benchmark.

**Stated limitations (§5), verbatim in substance:**
- Hard cap of **four** primaries. More than four breaks the 3D-LUT trick *and* creates ambiguity
  (a 5th green pigment means some greens are reachable two ways → not invertible).
- Homogeneous mixing of **opaque** paints only; alpha-compositing treats both layers as thick wet
  paint; no K–M layer compositing, so no watercolour glazing.
- Pigment character is baked in: acrylic gloss makes it over-saturate if applied to a
  watercolour-like medium.
- Only ~4 pigment slots means you cannot have both a warm and a cool blue.

### 1.3 Licence and pricing — verified precisely

- Repo `LICENSE` header, verbatim: *"Mixbox is licensed for non-commercial use under the
  CC BY-NC 4.0 license below. If you want to obtain commercial license, please contact:
  mixbox@scrtwpns.com"*, followed by the full **Creative Commons
  Attribution-NonCommercial 4.0 International** legal text.
  <https://github.com/scrtwpns/mixbox/blob/master/LICENSE>
- The NuGet package `Mixbox 2.0.0` declares the same: CC BY-NC 4.0, commercial by contact.
- **Pricing is not published anywhere.** The site only says *"You won't need to buy the commercial
  license until you're ready to launch your product."* **UNVERIFIED:** any dollar figure, royalty
  model, or per-title structure. It is quote-by-email only.
- Copyright: Secret Weapons, 2022.
- I found **no patent notice** on the repo, the docs, or the product page. A USPTO document
  ("RGB-based parametric color mixing system for digital painting", US 10,924,633) surfaced in
  search but I **could not verify** its assignee or any relation to Secret Weapons — Google Patents
  returned 404 for that number. Treat as an open question if commercial use is ever on the table.

### 1.4 Is there a C#/.NET binding? Yes — and it is a genuinely easy drop-in

- `csharp/Mixbox.cs` — **one file, 176 KB**, LUT embedded (base64), no native DLL needed.
- `csharp/Mixbox.csproj` (366 bytes) + `csharp/examples/HelloMixbox.cs`.
- NuGet: **`Mixbox` 2.0.0**, published 2022-09-20 by `scrtwpns`, 224 KB, **.NET Standard 2.0**
  (so fine for .NET 5 WinForms), ~4.3 K total downloads.
  <https://www.nuget.org/packages/Mixbox/2.0.0>
- API surface: `Mixbox.Lerp(int argb1, int argb2, float t)`, `Mixbox.RGBToLatent(int rgb)`,
  `Mixbox.LatentToRGB(MixboxLatent z)`, with `MixboxLatent` supporting arithmetic so you can
  weight-average N colours. Bindings also exist for C/C++, Java, JS, Python, Rust, Unity, Godot,
  GLSL/HLSL/Metal.

### 1.5 Mixbox vs spectral.js for *this* project

| | Mixbox 2.0 | spectral.js v3 (current hand-port) |
|---|---|---|
| Physics | Two-constant K–M on **measured Golden acrylic** K & S + Saunderson | Single-constant K–M on **synthetic** base spectra, no K/S separation |
| Pigments | 4 fixed surrogate primaries (LUT-baked; **cannot be swapped**) | none — any sRGB colour is a "pigment" |
| Perceptual quality | Best available; validated against real paint in the paper | Plausible; author says it is *not* built for realism (§2.4) |
| Licence | **CC BY-NC 4.0** — commercial needs a paid quote | MIT |
| .NET integration | 1 file / NuGet, netstandard2.0 | already hand-ported |
| Cost | £/$ unknown, by email | free |

**Verdict for the app's actual job.** Mixbox is a *mixing* engine, not a *recipe solver*. Its
`unmix` is hard-wired to its own four surrogate primaries — it can never tell you "2 parts
Liquitex Cadmium Yellow Medium + 1 part Ultramarine". You would still have to write the recipe
solver yourself and use Mixbox only as the forward `mix` oracle, and for that you would be
forcing every one of the user's 24 tubes through a 4-primary bottleneck, losing exactly the
pigment identity that makes the answer useful. Combined with CC BY-NC, **Mixbox is the wrong
dependency for this app.** Its *ideas* (the residual term, the constrained-simplex inverse, the
LUT-the-inverse trick) are all worth copying and are not encumbered by the code licence.

---

## 2. spectral.js — version history, known issues, licence

Repo: <https://github.com/rvanwijnen/spectral.js> (default branch is literally named `3.0.0`;
1.2 K stars; homepage <https://spectraljs.com/>).

### 2.1 Version history (from the GitHub releases API, exact)

| Tag | Date | Notes (verbatim) |
|---|---|---|
| 1.0.0 | 2023-04-06 | "First stable and feature ready release" |
| 1.0.1 | 2023-04-11 | "Fixed rounding errors to reduce banding. New method for including shader. Function name change from webgl_color to glsl_color. Added separate shader files for glsl and glsl3." |
| 2.0.0 | 2023-04-14 | **"Switched to 7 channel (white, cyan, magenta, yellow, red, green and blue) mixing."** |
| 2.0.1 | 2023-04-16 | "Fixed the color string parser" |
| 2.0.2 | 2023-04-21 | **"Fixed a precision error (most noticeable with extremes like black to white)"**, renamed `spectral_weights` → `spectral_upsampling`, replaced if-branching, added minified build |
| **3.0.0** | 2025-04-25 | **"Multicolor mixing, Gradient, Tinting strength, Gamutmapping. Complete re-write, there are breaking changes due to the new Color class."** |

So: v1 = 3 base curves (Burns' R/G/B primaries); **v2 = the 7-curve W/C/M/Y/R/G/B decomposition**;
v3 = same 7 curves plus N-way mixing, an explicit `tintingStrength` per colour, OkLCh gamut
mapping, and a lazy `Color` class. The interesting historical detail is that **the black↔white
precision bug the brief hints at was fixed in v2.0.2** and is not present in v3.

### 2.2 What v3 actually computes (read from source)

```js
const KS  = (r)  => (1 - r) ** 2 / (2 * r);
const KM  = (KS) => 1 + KS - (KS ** 2 + 2 * KS) ** 0.5;

// per band i:
concentration = factor ** 2 * color.tintingStrength ** 2 * color.luminance;
ksMix        += color.KS[i] * concentration;
R[i]          = KM(ksMix / totalConcentration);
```

`tintingStrength` defaults to `1` (`this._tintingStrength ??= 1`), and `luminance` is
`max(EPSILON, XYZ[1])`. The README states the concentration formula as **`C = f² · T² · L`**.
So the C# port's `weight² × luminance` is a faithful port with `T = 1`, and **the port is missing
the per-paint tinting-strength knob**, which is the single most useful hand-tuning lever the
library offers for real pigments (phthalos and dioxazine bully a mixture; cadmiums and earths do
not).

Spectral upsampling (`lRGB_to_R`) is a greedy W/C/M/Y/R/G/B decomposition of linear RGB:
`w = min(lRGB)`, subtract; `c = min(g,b)`, `m = min(r,b)`, `y = min(r,g)`;
`r = max(0, min(R−B, R−G))` etc.; then `R[i] = Σ weight·BASE_SPECTRA[i]`, floored at
`Number.EPSILON`.

Other constants: gamut-mapping JND `0.03`, binary-search epsilon `0.0001`, sRGB companding
`GAMMA = 2.4`, thresholds `0.04045 / 0.0031308`.

### 2.3 Band count — the brief and the port's own comments are wrong

`const SIZE = 38`. `BASE_SPECTRA.W` has **38** entries; each CMF row has **38** entries. I
verified the wavelength grid numerically from the bundled D65-premultiplied CMFs:
`x̄` argmax at index 22, `ȳ` argmax at index 17, `z̄` argmax at index 7, and `z̄` becomes exactly
`0.0` at index 27. With `380 + 10·i` that gives peaks at 600 / 550 / 450 nm and `z̄ = 0` from
650 nm — all exactly the CIE 1931 2° table. **The grid is 380–750 nm at 10 nm, 38 bands.**

`Imaging/SubtractivePaintMixer.cs` documents itself as *"38-band reflectance spectrum
(wavelengths 380-730nm)"* on lines 38–39 and 52–54, and `PROJECT.md` repeats it. That is
arithmetically impossible (38 bands at 10 nm cannot span 350 nm). The **numbers** in the port are
copied verbatim from spectral.js so nothing is broken today — but the label is wrong, and it is a
live landmine the moment anyone tries to align this spectrum with real measured paint data
(Berns' Golden data is also 380–750/38, so it happens to line up; Burns' LHTSS and the
`artistpigments.org` measurements are **380–730/36** and would be off by two bands).
**Fix the comments; add a `StartWavelength`/`Interval` constant instead of a bare band count.**

For context, the maintainer answered "380 to 730 nm in 10 nm increments" in issue #12 — that is
the **v1** grid (Burns' 36-band LHTSS basis), not v3's.

### 2.4 Known issues and limitations documented by the author

Issue tracker: <https://github.com/rvanwijnen/spectral.js/issues>

- **#24 (open, 2026-05-28) "What is the purpose of weighting concentration by luminance."**
  Author's answer: it is "intended to make the 0-1 mixing scale feel more perceptually balanced …
  A 50/50 mix should appear closer to a visual midpoint between two colors, rather than being
  dominated by the darker color." **This is an aesthetic correction, not physics.** For a
  *recipe* app it is actively harmful: it means a returned mix weight is not a mass or volume
  fraction, so telling the user "0.5" does not mean "half a squeeze".
- **#22 (closed) "generated vs experimental spectral data."** Author, verbatim in substance:
  *"Spectral.js works by using 7 carefully chosen base spectra that span the sRGB gamut, so it can
  turn any RGB color into a plausible reflectance curve for mixing"* and — the money quote —
  **"Spectral.js is not built for realism. It is specifically built for sRGB input and output."**
  He explicitly directs anyone wanting realism to *"do your own measurements with a
  spectrophotometer"* and use **two-constant Kubelka-Munk functions** for mixing them. He notes
  you can feed a `Color` a raw 38-length reflectance array and "let go of the notion of sRGB
  input".
- **#23 (closed, 2025-09) "using spectral.js for mixing physical pigments"** — a user asked
  exactly this project's question (given CMY liquid pigments, find the closest match to a digital
  colour). Closed with no recorded solution. **spectral.js does not solve the inverse problem.**
- **#21 / #17** — why 7 curves rather than RGB or CMYW; **#16** — no TypeScript types;
  **#14 / #20** — porting requests; **#12** — CMF/illuminant provenance (CIE tables ×
  D65, normalised by Σȳ, cross-checked against ColorPy).
- README's own limitation: tinting strength "requires manual, perceptual adjustment — there is no
  way to programmatically determine if a Color is too dominant as this is pure perceptual."
- Independent confirmation of the core weakness, from ColorAide's docs (see §3.4): the
  single-constant approach "can turn out a bit dark", which is *why* the luminance weighting
  exists; two-constant is "more accurate for paint".

### 2.5 Licence and attribution — verified

`LICENSE` on branch `3.0.0` is the standard **MIT License**, `Copyright (c) 2025 Ronald van
Wijnen`. (Note ColorAide's vendored copy carries `Copyright (c) 2023 Ronald van Wijnen` — the
year moved with the v3 rewrite.)

**Attribution string to use.** MIT requires the copyright line and the permission notice be
retained in "copies or substantial portions". A hand-port of the algorithm plus the verbatim base
spectra and CMF tables is a substantial portion. Suggested header for
`Imaging/SubtractivePaintMixer.cs`:

> Derived from spectral.js v3.0.0 (<https://github.com/rvanwijnen/spectral.js>).
> MIT License — Copyright (c) 2025 Ronald van Wijnen.
> [full MIT permission + warranty paragraphs]
> spectral.js in turn credits Scott Allen Burns (reflectance-curve reconstruction / LHTSS),
> Color.js (conversion matrices), and Mixbox by Secret Weapons (conceptual inspiration).

Two upstream credit errors worth not propagating: spectral.js's README credits *"Richard S.
Kubelka"* — the 1931 author is **Paul Kubelka** (Paul Kubelka & Franz Munk, *Ein Beitrag zur
Optik der Farbanstriche*, Z. Tech. Physik 12, 593–601, 1931) — and dates the theory to "the
1930s" correctly but the name is wrong.

---

## 3. Other implementations surveyed

### 3.1 colour-science (Python) — <https://github.com/colour-science/colour>

BSD-3-Clause, 2.6 K stars, very actively maintained. It is the reference for colorimetry, spectral
upsampling (`colour.recovery`: Smits 1999, Meng 2015, Otsu 2018, Jakob & Hanika 2019, Mallett &
Yuksel 2019), and CMFs/illuminants. **It does not ship a Kubelka–Munk pigment-mixing module.**
Its K–M reference is external: it credits and *hosts a mirror of* Paul Centore's toolbox (below).
Useful to us as a cross-check oracle in a scratch Python script, not as a dependency.

### 3.2 Munsell and Kubelka-Munk Toolbox (Paul Centore) — Octave/MATLAB

- Mirror: <https://github.com/colour-science/MunsellAndKubelkaMunkToolbox> (archived 2018,
  GitHub detects **no licence**)
- Home: <https://www.munsellcolorscienceforpainters.com/MunsellAndKubelkaMunkToolbox/MunsellAndKubelkaMunkToolbox.html>
- **Licence: GNU GPL v3 or later**, stated in every file header
  (`"This file is part of MunsellAndKubelkaMunkToolbox … free software under the GNU General
  Public License … either version 3 of the License, or (at your option) any later version."`).
  **Viral — do not port code from it into a closed-source app.** Reading the papers and
  re-implementing from the published maths is fine.

`KubelkaMunk/` contains exactly the routines this project needs conceptually:
`ReflectanceOfMixtureFromKandS.m`, `KoverSfromMasstoneR.m`, `MasstoneRfromKandS.m`,
`SaundersonCorrection.m` + inverse, `KandSfromMixturesWalowit1987.m`,
`KandSfromMixturesCentore2013.m`, `CompareReflectanceSpectra.m`. The headers cite
**Allen 1980, ch. 7 of *Optical Radiation Measurements Vol. 2: Color Measurement*** eq. (19) as
the mixture-reflectance basis.

Centore's papers are free PDFs and are the single best written source for the constrained
problem — see §4.

### 3.3 Krita and MyPaint

Krita's **spectral / "paint-like" blending** came from **spectral.js**, contributed by Ronald van
Wijnen himself with help from Krita dev Dmitry Kazakov.

- MR: <https://invent.kde.org/graphics/krita/-/merge_requests/1783> ("Add spectral blending mode",
  opened 2023-04-08; motivated by smudge-brush performance)
- Design thread: <https://krita-artists.org/t/paint-like-color-mixing-kubelka-munk/78156>

Numbers reported in that thread (v1-era spectral.js): **three** premade reflectance curves,
380–730 nm at 10 nm; concentration function weighted by lightness for perceptual balance;
sRGB→spectral→XYZ→sRGB round-trip error 0 %; mixed colours out of sRGB gamut in only 0.025 % of
cases with max deviation 0.05 %, so they **clip rather than gamut-map**. That clip-vs-map result
is a useful datapoint: for in-gamut palette work, gamut mapping is not worth the complexity.

MyPaint: the same thread is where the discussion started ("references a discussion thread … about
spectral color mixing with MyPaint"), but I found **no shipped K–M blending in MyPaint** —
**UNVERIFIED / likely absent.**

### 3.4 ColorAide (Python, MIT) — the best-documented independent reimplementation

`coloraide.interpolate.spectral` — <https://github.com/facelessuser/coloraide>
(`coloraide-extras` deprecated its copy; it now lives in core ColorAide).
The file carries spectral.js's MIT notice verbatim plus a modification log:

> - Recalculated D65-illuminated CMFs and R, G, B curves with higher precision and generated them
>   with our exact RGB matrix, white point and CMF calculations.
> - **Added sane handling for colors beyond the sRGB gamut. Limit the curves to be below 1 and
>   calculate residual XYZ difference between what the concentration can produce and actual value.
>   Add the interpolated residual back in after blending the reflectance curves.**

That second bullet is **Mixbox's residual idea applied to spectral.js, under MIT** — and it is
directly portable. Their `spectral_mix`:

```py
r1, res1 = single_constant_xyz_to_reflectance(xyz1)   # returns curve + XYZ residual
r2, res2 = single_constant_xyz_to_reflectance(xyz2)
c1, c2 = ((1-t)**2 * l1, t**2 * l2)  ; normalise
r = [km_to_r(km_to_ks(r1[i])*c1 + km_to_ks(r2[i])*c2) for i in range(SIZE)]
xyz = reflectance_to_xyz(r) + lerp(res1, res2, t)
```

ColorAide's docs also state plainly why spectral.js has the luminance hack: the single-constant
approach "can turn out a bit dark", and two-constant is "more accurate for paint" but needs real
absorption/scattering measurements. **Worth borrowing:** the residual term, and their
`xyz_to_concentration` which is a slightly cleaner (and correctly `[0,1]`-clamped) version of
spectral.js's W/C/M/Y/R/G/B decomposition.

### 3.5 ⭐ Wacton.Unicolour (C#, MIT) — the find of this research

<https://github.com/waacton/Unicolour> · <https://unicolour.wacton.xyz/> ·
NuGet `Wacton.Unicolour` (currently 6.4.0), `Wacton.Unicolour.Datasets`, `Wacton.Unicolour.Experimental`

- **Licence: MIT**, `Copyright (c) 2022-2026 William Acton`. Verified from the repo `LICENSE`.
- **.NET Standard 2.0, zero dependencies, cross-platform** → drops straight into .NET 5 WinForms.
- Actively maintained (last push 2026-07-08).
- 40 colour spaces, CIEDE2000 and friends, gamut mapping, chromaticity, ICC CMYK, colour
  temperature.

What matters here:

**`Unicolour/Pigment.cs` — real Kubelka–Munk, both flavours.**

```csharp
// single-constant, from a reflectance curve
new Pigment(startWavelength: 380, wavelengthInterval: 10, r: [...]);
// two-constant, from measured K and S (+ optional Saunderson k1,k2)
new Pigment(startWavelength: 380, wavelengthInterval: 10, k: [...], s: [...], k1, k2, name);
var green = new Unicolour(pigments: [phthaloBlue, hansaYellow], weights: [0.5, 0.5]);
```

Internals (read from source): weights are clamped `≥0` then normalised to concentrations summing
to 1 — **no luminance fudge, no squaring**, so a weight is a real concentration.
`SingleConstantR` averages `K/S = (1−r)²/(2r)`; `TwoConstantR` averages K and S separately then
`R = 1 + K/S − √((K/S)² + 2K/S)` and applies Saunderson
`R' = (1−k₁)(1−k₂)R / (1 − k₂R)` (SPEX-mode form, no leading `k₁ +` term). It refuses to mix
pigments with mismatched wavelength grids, mismatched single/two-constant type, or mismatched
`k1/k2` (returns `null`) — conservative and correct.

**`Unicolour.Datasets/ArtistPaint.cs` — measured Golden acrylic K and S in C#.**
38 KB of C# arrays: **19 pigments**, `startWavelength: 380, wavelengthInterval: 10`, 38 values
each (→ 380–750 nm), with `K1 = 0.03, K2 = 0.65`, configured `RgbConfiguration.StandardRgb` +
`XyzConfiguration.D50`. This is **Berns' Artist Paint Spectral Database** (the same source Mixbox
used). Each entry is commented with its colour index and Golden product URL:

`BoneBlack` PBk9 · `HansaYellowOpaque` PY74 · `DiarylideYellow` PY83 · `CadmiumOrange` PO20 ·
`PyrroleOrange` PO73 · `CadmiumRedLight` PR108 · `PyrroleRed` PR254 · `QuinacridoneRed` PV19 ·
`QuinacridoneMagenta` PR122 · `DioxazinePurple` PV23 · `UltramarineBlue` PB29 · `CobaltBlue` PB28 ·
`CeruleanBlueChromium` PB36 · `PhthaloBlueRedShade` PB15 · `PhthaloBlueGreenShade` PB15 ·
`PhthaloGreenBlueShade` PG7 · `PhthaloGreenYellowShade` PG36 · `BismuthVanadateYellow` PY184 ·
`TitaniumWhite` PW6

**`Unicolour.Experimental/PigmentGenerator.cs` — Burns LHTSS, properly.**
`PigmentGenerator.From(Unicolour)` → a single-constant `Pigment`. It implements **LHTSS** (Least
Hyperbolic Tangent Slope Squared) from Burns, *Numerical methods for smoothest reflectance
reconstruction*, Color Res. Appl. (doi:10.1002/col.22437) — `ρ(z) = (tanh z + 1)/2`, a
Newton solve on the KKT system `[D − diag(sech²z·tanh z · Aλ), diag(sech²z/2)·A; Aᵗ·diag(sech²z/2), 0]`,
max 20 iterations, tolerance `1e-8`, over **36 bands 380–730 nm at 10 nm** with a tridiagonal
smoothness matrix `D`. Special-cases pure black (all-`1e-8`) and pure white (all-`1.0`).
This is the *principled* replacement for spectral.js's 7-base-spectra hack. Round-trip tolerance
in their own tests is `0.05` in sRGB — i.e. **LHTSS reconstruction is not exact**, worth knowing.

Also `Unicolour.Experimental/SpectralJs.cs` (3 KB) — a spectral.js port, used for their pigment
colour wheel; and `SystemOfLinearEquations.cs` / `MatrixUtils.cs`, small dependency-free linear
algebra you can reuse for the recipe solver.

### 3.6 Colourful (C#, MIT) — <https://github.com/tompazourek/Colourful>

MIT, 297 stars, maintained (2025-05). RGB/linear RGB/XYZ/xyY/Lab/Luv/LCh(ab)/LCh(uv)/Hunter
Lab/LMS, many ΔE formulas, CCT, chromatic adaptation, no dependencies. **No spectral or K–M
anything.** Strictly a colorimetry library; Unicolour is a superset for our purposes.

### 3.7 SixLabors.ImageSharp — licence caution

GitHub reports `NOASSERTION`; the actual `LICENSE` is the **Six Labors Split License, Version 1.0
(June 2022)** — *not* an OSI licence, and *not* Apache-2.0 (that was ImageSharp ≤ 2.x). It
distinguishes direct vs transitive package dependencies and carves out commercial use. If
ImageSharp is only used for decoding it is probably fine, but **read the split licence before any
commercial distribution**, and note it is a different obligation from MIT. Its colour-space
support is incidental and much weaker than Unicolour's.

### 3.8 Accord.NET

`LGPL-2.1`, **archived**, last push 2020-11-18. It *does* have
`Accord.Statistics.Models.Regression.Fitting.NonNegativeLeastSquares` (Lawson–Hanson active set).
But: dead project, LGPL obligations, and NNLS is ~80 lines you should just write. **Do not take
this dependency.**

### 3.9 Math.NET Numerics — MIT, maintained (2025-03), 3.8 K stars

<https://github.com/mathnet/mathnet-numerics>. Good for dense linear algebra
(`Matrix<double>.Solve`, QR/SVD, Cholesky) if you would rather not hand-roll the normal-equation
solve inside NNLS. **No built-in NNLS.**

### 3.10 Lindemeier — the closest published system to what this app is trying to do

**T. Lindemeier, J. M. Gülzow, O. Deussen. *Painterly Rendering using Limited Paint Color
Palettes.* VMV 2018, pp. 135–145. DOI 10.2312/vmv.20181263.**
Free PDF: <https://diglib.eg.org/server/api/core/bitstreams/98c9fd61-2fec-4aff-a3b6-abf6335e8dd0/content>
(also <https://graphics.uni-konstanz.de/publikationen/Lindemeier2018PainterlyLimitedPalette/Lindemeier2018PainteryLimitedPalette.pdf>,
which 404'd for me).

Abstract, verbatim: *"…automatically extracts color palettes from images and computes mixture
recipes for these from a set of real base paint colors based on the Kubelka-Munk theory."* That
**is** this project's problem statement, applied to a painting robot with 24 paint pots.

Code:
- <https://github.com/lindemeier/PaintMixer> — **LGPL-3.0**, archived, C++ + Ceres.
  Ships **14 measured acrylic base pigments**: Primary Magenta, Carmine Red, Cadmium Red Medium,
  Raw Umber, Cadmium Orange Hue, Cadmium Yellow Hue, Primary Yellow, Leaf Green, Phthalo Green,
  Cobalt Blue Tone Deep, Ultramarine Blue, Lilac, Lamp Black, Titanium White.
- <https://github.com/lindemeier/painty> — successor, **MIT**, 30 stars, C++/OpenCV, includes the
  K–M paint renderer and palette extraction.

**Their recipe solver, verbatim from the paper (eqs. 4–7).** Target: find weights `w` and
thickness `d` so that the K–M composite of the mixture over background `R₀` matches `R₁`:

```
argmin_{w,d}  ‖ R_KM(K_w, S_w, d, R₀) − R₁ ‖  +  a_sum·E_sum  +  a_sp·E_sp
s.t.  0 ≤ w ≤ 1 ,  d > 0                    with R₀ = 1 (white ground)

E_sum = | ‖w‖₁ − 1 |                                    # soft simplex constraint
E_sp  = 1 − ( √n − ‖w‖₁/‖w‖₂ ) / ( √n − 1 )             # Hoyer sparsity, penalises dense w
a_sum = 0.5 ,  a_sp = 0.1
```

`E_sp` is the **sparsity term that makes the solver prefer 2–3 paints over 12** — exactly what
this project needs — and it is differentiable, unlike an L0 count. The C++ (`src/PaintMixer.cxx`)
confirms the implementation: three Ceres residual blocks (`CostFunction_MixPaint` 6 residuals,
`CostFunction_E_sum` 1 residual scaled by 0.5, `CostFunction_E_sparse` 1 residual scaled by 0.1),
`DynamicAutoDiffCostFunction`, per-parameter bounds `[0,1]`, init `wᵢ = 1/k`,
`max_num_iterations = 1000`, `function_tolerance = 1e-9`, then a final normalise-to-sum-1.

They cite **Aharoni-Mack, Shambik & Lischinski, *Pigment-Based Recoloring of Watercolor
Paintings*, NPAR 2017** and **Tan et al. (Pigmento)** as the two prior methods that find mixture
weights for a target RGB, and describe their contribution as adding the sum + sparsity terms.

Table 2 of the paper is a **published, real, physically mixed recipe set** (Fig. 1/6 palette,
4 target colours over their 14-pigment base) — useful as a smoke-test corpus, though it has no
measured Lab values attached. Also relevant: their palette extraction uses Aharoni-Mack's
CIELAB *ab*-plane convex hull + Douglas–Peucker simplification, modified to force the darkest and
brightest image colours into the palette before extracting `k − 2` more. And their base-pigment
K/S estimation is a nonlinear least squares over camera before/after photos (10 s in Ceres, RMS
CIELAB ≈ 2.20) — a cheap alternative to a spectrophotometer.

### 3.11 Smaller repos worth a glance

From <https://github.com/topics/kubelka-munk>: `justinh-rahb/filament-mixer` (Python, K–M for 3D
printer filament — same inverse problem, different medium), `RNVizion/rnv-color-mixer` (PyQt6
desktop paint mixer), `peppemagic/paint-mixer-pro`, `Raghavan-04/PaletteCraft` (web app
extracting palettes *and* generating paint-mixing recommendations), `fligt/revigo-spectra`
(measured paint reflectance notebooks), `STVND/davis-pigment-mixing` (GLSL K–M),
`food211/Mixbox-Palette`. All small/unmaintained; none has a rigorous solver. Nothing here beats
Unicolour + a hand-written solver.

I found **no repository** matching the names "coloralgebra", "pigmentmixing" (as a standalone
library), or "ArtColorMix". **UNVERIFIED / probably do not exist** under those names — the brief
may be misremembering `scrtwpns/pigment-mixing` (the Mixbox research repo).

---

## 4. Recipe solving — replacing brute-force enumeration

This is a solved industrial problem, and the current `PaintBlendMatcher` is solving it the way
you would in 1960 without a computer.

### 4.1 The classic references

- **Eugene Allen. *Basic Equations Used in Computer Color Matching.* JOSA 56(9), 1256–1259
  (1966).** DOI 10.1364/JOSA.56.001256. <https://opg.optica.org/josa/abstract.cfm?uri=josa-56-9-1256>
  Abstract, verbatim: *"If we are given the spectrophotometric curves of a color and three
  colorants to be used in matching it, the computation of the concentrations of the three
  colorants required for a tristimulus match is a complicated nonlinear problem. However, with the
  help of an approximating assumption, a linear solution may be obtained by a matrix inversion
  technique. Although this is an approximate solution, it is better the less metameric the match.
  With this rough solution as a starting point, iteration may be used to approach an exact match
  to any desired degree of accuracy. The inverted matrix used for the iterative computation is
  identical to that used for the rough solution."*
- **Eugene Allen. *Basic equations used in computer color matching, II. Tristimulus match,
  two-constant theory.* JOSA 64(7), 991–993 (1974).**
  <https://opg.optica.org/josa/abstract.cfm?uri=josa-64-7-991> — two stages: rough match, then
  Taylor-expand X, Y, Z about trial concentrations, solve for `Δc`, repeat. Same formal matrix for
  both stages but recomputed and re-inverted once before the iterative stage.
- **Eugene Allen. *Colorant formulation and shading.* In Grum & Bartleson (eds.), *Optical
  Radiation Measurements Vol. 2: Color Measurement*, Academic Press, 1980, p. 290.** — the
  textbook version; eq. (19) is the mixture-reflectance equation Centore's toolbox implements.
- Also: **linear-programming approach to colour-recipe formulation**, JOSA 64, 1541 (1974);
  **Sluban, *Comparison of colorimetric and spectrophotometric algorithms for computer match
  prediction*, Color Res. Appl. 18(2), 1993**; **Berns, *Billmeyer and Saltzman's Principles of
  Color Technology*, 4th ed., Wiley 2019**, ch. 10 — the modern canonical treatment.
- **Paul Centore, *Enforcing Kubelka-Munk Constraints for Opaque Paints*, Coloration Technology
  (2020; preprint dated 2016-01-24).** DOI 10.1111/cote.12497.
  **Free PDF:** <http://www.munsellcolorscienceforpainters.com/ColourSciencePapers/EnforcingKubelkaMunkConstraintsForOpaquePaints.pdf>
  This is the clearest free write-up of the *constrained* problem and I have read it in full.

### 4.2 The constrained least-squares formulation (Centore, adapted to single-constant)

Centore's framing, verbatim in substance: *"Standard estimation procedures cast the Kubelka–Munk
relationships as an overdetermined linear system, and apply ordinary least squares (OLS). OLS,
however, sometimes produces coefficients or concentrations that are less than 0 or greater than 1.
These physically impossible solutions occur because OLS projects a target vector onto a vector
subspace, while in fact the set of physically realizable paint combinations is a convex polytope,
which is a subset of that subspace."*

His **two-constant** concentration estimation (his eqs. 18–20). Because
`(K/S)_mix = Σcᵢ Kᵢ / Σcᵢ Sᵢ`, rearranging gives, per wavelength λ, a **homogeneous** row:

```
Σᵢ cᵢ · [ f(R(λ)) · Sᵢ(λ) − Kᵢ(λ) ]  =  0 ,      f(R) = (1−R)² / (2R)
```

weight each row by `w(λ)` (he uses the photopic luminous efficiency `ȳ(λ)`; he notes ideally
`w(λ)` should reflect perceived colour difference) and stack over all wavelengths:

```
M c = 0 ,   Mλ,i = w(λ)·[ f(R_target(λ))·Sᵢ(λ) − Kᵢ(λ) ]
subject to  0 ≤ cᵢ ≤ 1 ,  Σ cᵢ = 1
```

The trivial `c = 0` is excluded by the simplex constraint. Geometrically: with
`0 ≤ cᵢ ≤ 1, Σcᵢ = 1`, the set `{Mc}` is the **convex hull of the columns of M**, a convex
polytope `P` in `ℝ^bands`; you want the point `b_P ∈ P` closest to the target (here the origin),
and its barycentric coefficients *are* the concentrations. Convexity makes `b_P` unique.

He recommends the **Gilbert–Johnson–Keerthi (GJK)** algorithm because it takes generators directly
(no half-space conversion, unlike QP) and returns the answer already expressed as a convex
combination of generators — "the coefficients in that expression can be taken as entries in x".
He explicitly warns off generic QP: *"the Kubelka-Munk polytope will be expressed as the convex
hull of a finite set of generators, while QP problems express a polytope as the intersection of a
set of half-spaces. Converting between the two kinds of expression can be computationally
demanding"*. Alternative pointer: Lawson & Hanson, *Solving Least Squares Problems*, chapters
20–23.

**Our single-constant case is simpler.** With single-constant K–M we only have `K/S` per paint,
and the mixing rule is `(K/S)_mix = Σ cᵢ (K/S)ᵢ` — already linear. So the spectral fit is a plain
**non-negative least squares on the simplex**:

```
minimise  ‖ W ( A c − t ) ‖²
where  A[λ,i] = (K/S)ᵢ(λ)      (bands × paints)
       t[λ]   = (K/S)_target(λ) = (1 − R_t(λ))² / (2 R_t(λ))
       W      = diag(w(λ))     (ȳ(λ), or a ΔE-derived weighting)
s.t.   c ≥ 0 ,  Σ c = 1
```

### 4.3 NNLS and the simplex constraint — concrete C#

**Lawson–Hanson active-set NNLS** (no library needed, ~70 lines, `Bands × Paints` is tiny — say
38 × 24 — so speed is a non-issue):

```
NNLS(A, b):                        # min ‖Ac − b‖², c ≥ 0
  P = {} ; Z = {1..n} ; c = 0
  loop:
    w = Aᵗ(b − Ac)
    if Z empty or max_{j∈Z} w_j ≤ tol: return c
    j = argmax_{j∈Z} w_j ; move j from Z to P
    inner:
      s_P = argmin ‖A_P s − b‖²        # unconstrained LS on the active set (QR or normal eqs)
      s_Z = 0
      if min_{j∈P} s_j > 0: c = s ; break inner
      α = min over {j∈P : s_j ≤ 0} of c_j / (c_j − s_j)
      c = c + α(s − c)
      move every j∈P with c_j ≈ 0 into Z
```

**Adding `Σc = 1`.** Three options, in order of preference:

1. **Penalty row (simplest, robust, what I recommend).** Append one row to the system:
   `A' = [ W·A ; ρ·1ᵗ ]`, `b' = [ W·t ; ρ ]` with `ρ` large (e.g. `ρ = 100 · max|W·A|`), then run
   plain NNLS on `(A', b')`. Because our mixing is *invariant to overall scale* only in the
   two-constant case — with single-constant `K/S` averaging it is **not** scale-invariant — the
   constraint genuinely matters and this soft form works well. This is Lawson & Hanson's
   "heavily weighted constraint row".
2. **Normalise-after-NNLS.** Run unconstrained-sum NNLS, then divide by `Σc`. Cheap and what
   PaintMixer does as a final step; slightly biased but usually within noise.
3. **Full simplex-constrained LS** (NNLS with an equality) — Lawson & Hanson ch. 20–23, or GJK per
   Centore. Only worth it if (1) misbehaves.

### 4.4 The outer Newton loop (Allen's second stage)

Fitting `K/S` is a *spectral* fit; what the user perceives is a *tristimulus* fit, and
`K/S → R → XYZ → Lab` is nonlinear. Allen's structure, modernised:

```
SolveRecipe(target_Lab, paints[subset]):
  # ---- stage 1: rough spectral match (linear) ----
  R_t = reconstruct_reflectance(target_sRGB)        # LHTSS, or the 7-base-spectra upsample
  t   = KoverS(R_t)
  c   = NNLS_simplex(W·A, W·t)

  # ---- stage 2: Newton / Gauss-Newton on the perceptual error ----
  for iter in 1..maxIter (8 is plenty):
    Lab   = LabOf( MixKM(paints, c) )               # forward model, exact
    e     = target_Lab − Lab                        # 3-vector
    if ‖e‖ < 0.05: break                            # well below JND, stop
    J     = ∂Lab/∂c                                 # 3 × n, finite differences: h = 1e-4
    Δc    = solve_constrained_lsq(J, e, c)          # keep c+Δc on the simplex
    c     = project_to_simplex(c + damping·Δc)      # damping 1.0, halve on non-improvement
  return c, deltaE2000(target_Lab, LabOf(MixKM(paints,c)))
```

Notes that matter in practice:
- The Jacobian by **central finite differences** is fine: `n ≤ 3` in the final recipe, so 6 extra
  forward evaluations per iteration. No autodiff needed. (Mixbox and PaintMixer both use autodiff
  because they solve millions of these; we solve a handful per click.)
- `project_to_simplex` = clamp negatives to 0, renormalise; or the exact Euclidean simplex
  projection (Duchi et al. sort-based, ~10 lines) if clamping oscillates.
- **Damped Gauss-Newton with backtracking** is more robust than raw Newton here because the K–M
  inversion has a `√` that steepens near `R → 0`.
- Report `ΔE2000`, not squared ΔE76. The current matcher uses squared CIELAB (ΔE76) which
  systematically misranks saturated blues and near-neutrals — flagged as a separate fix.

### 4.5 Choosing *which* 2–3 paints — the cardinality constraint

The user wants 2–3 tubes, not 12. Three approaches:

**(a) Exhaustive best-subset (recommended).** With `n` paints, `C(n,1) + C(n,2) + C(n,3)` subsets:

| n | subsets ≤ 3 |
|---|---|
| 12 | 298 |
| 24 | 2 324 |
| 36 | 7 770 |
| 48 | 18 424 |
| 60 | 36 050 |

Each subset costs one NNLS on a `38 × ≤3` system plus ~8 Gauss-Newton iterations. That is
microseconds. **Even 60 paints is under ~40 K tiny solves — comfortably interactive, and it is
*exact*: it returns the provably best ≤3-paint recipe.** This is strictly better than what the
code does now, which enumerates *fixed* ratio grids (7 ratios for pairs, 4 weightings for triples)
and therefore cannot hit the optimum, and which gives up on triples above 30 paints.

Cheap pruning to make it fast even at n = 100:
- Skip any subset whose **convex hull in K/S space cannot bracket the target**: if
  `min_i (K/S)ᵢ(λ) > t(λ)` for any λ, or `max_i (K/S)ᵢ(λ) < t(λ)` for any λ, the simplex cannot
  reach it. One vectorised pass, kills most subsets instantly.
- Rank singles by ΔE first; restrict pairs/triples to the top `m` (m ≈ 12–16) nearest paints
  **plus** always-include white and black. Reduces 36 050 to ~700 with negligible quality loss.
- Cache each paint's `K/S` array and precompute `AᵗA` blocks.

**(b) Orthogonal Matching Pursuit / greedy forward selection.** Standard in hyperspectral
unmixing, where it is called sparse unmixing (SOMP, SMP, RSFoBa, SUnSAL/C-SUnSAL by Bioucas-Dias):

```
OMP_simplex(A, t, k=3):
  S = {} ; r = t
  repeat k times:
    j = argmax_{j∉S}  |⟨ Aⱼ , r ⟩| / ‖Aⱼ‖
    S = S ∪ {j}
    c_S = NNLS_simplex(A_S, t)
    r   = t − A_S c_S
  return S, c_S
```

O(k·n) instead of O(n³). **But** the unmixing literature is blunt about the failure mode:
*"mutual coherence of endmember signatures in a spectral library is usually very high … the more
similar the endmember signatures are, the more difficult the sparse unmixing is."* A paint set
contains many near-duplicates (three phthalo blues, four yellows), so coherence is high and greedy
selection makes early mistakes it cannot undo. Backward-correcting variants (RSFoBa) exist
precisely because of this. **Given that (a) is already fast enough, OMP is not worth its
approximation error.** Keep it in the back pocket only if the palette grows past ~150 paints.

**(c) Differentiable sparsity penalty (Lindemeier).** Solve over *all* paints with the Hoyer
sparsity term from §3.10, then threshold. Elegant, single solve, but it gives you "mostly 3
paints" not "exactly ≤3", needs `a_sp` tuning, and needs a nonlinear solver. Better as a
*secondary* objective: among subsets that tie on ΔE, prefer the one with fewer paints and
less extreme ratios (a 1:1 mix is easier for a human to execute than 0.93:0.07).

**Practical UX corollary:** also snap the final weights to human-executable ratios (1:1, 2:1, 3:1,
1:1:1, 4:1, 5:1…) and re-evaluate ΔE for each snapped candidate, reporting the best. A recipe of
"0.6431 : 0.2109 : 0.1460" is useless at the palette; "3 : 1 : 1" is not. Golden's own guidance
says *"mixing by weight is the most accurate way to recreate a mixture"* — so state units.

### 4.6 Metameric vs colorimetric matching — and why we should care a bit

- A **colorimetric (tristimulus) match** equalises X, Y, Z under one illuminant. Two different
  reflectance curves that agree on XYZ are **metamers**: they match under D65 and diverge under
  gallery halogen or LED. Metamers typically cross at ≥ 3 wavelengths.
- A **spectral match** equalises the reflectance curve itself; it then matches under *every*
  illuminant and is immune to observer variation. Allen's own abstract notes his linear
  approximation "is better the less metameric the match".
- Quantifying it: Berns (*Quantification of illuminant metamerism for four coloration systems via
  metameric mismatch gamuts*, Color Res. Appl. 13(6), 1988) generated metamers under D65/1964 and
  measured the mismatch gamut under test illuminants. Berns' 2022 acrylic dataset paper does the
  same trick to evaluate PCA-CMY primaries: match under D50 (ΔE = 0 by construction), then
  re-evaluate under a **2200 K blackbody** — average **CIEDE2000 1.8**, range **0.03–9.88**, and
  concludes *"three primaries are insufficient to approximate the 58 pigments."*
- **Recommendation for this app:** solve in K/S (spectral) space as the primary objective — which
  the pipeline in §4.4 already does — and use the tristimulus Newton stage only as a *refinement*,
  not as the objective. Then optionally **report a metamerism index**: recompute the mixture's
  ΔE2000 against the target under a second illuminant (A, or a 2700 K LED) and warn if it exceeds
  ~2. That is a genuinely differentiating feature for a painter whose work will hang under
  tungsten. Practical caveat: our "target spectrum" is *reconstructed* from sRGB, not measured, so
  the spectral match is only as good as the reconstruction — which is another argument for LHTSS
  over the 7-base-spectra hack.

### 4.7 Accelerating the nearest-neighbour / precomputation layer

The current design precomputes every enumerated recipe once and then does a **linear scan** over
that cache per query. Options, in increasing order of payoff:

1. **k-d tree in CIELAB.** `Supercluster.KDTree` (MIT, C#, <https://github.com/ericreg/Supercluster.KDTree>,
   NuGet `Supercluster.KDTree` / `.Standard` / `.Net`) — the author reports ~7.5× faster than the
   CodeandCats tree for bulk k-NN, but it is **immutable after build**, which is fine for a
   precomputed recipe cache. Caveat: a k-d tree on Lab is a *Euclidean* (ΔE76) structure. If you
   want ΔE2000 ranking, use the tree to fetch the k ≈ 32 nearest by ΔE76, then re-rank those by
   ΔE2000 — ΔE2000 is not a metric so it cannot index directly, but ΔE76 bounds it closely enough
   for a candidate set.
   Honestly: for ≤ 50 K cached recipes a linear scan of 3 floats is ~0.2 ms. **The k-d tree is
   premature unless the cache grows past ~10⁶ entries.**
2. **3D LUT over the sRGB cube — this is the real win, and it is the Mixbox trick.** Precompute
   the *solver output* (paint indices + weights) on a 32³ or 33³ grid of sRGB (32 768 cells).
   At 40 K subset-solves per cell that is too slow to do eagerly, so: build it **lazily with
   memoisation**, keyed on quantised sRGB, persisted to disk per palette. A whole-photo conversion
   then costs one dictionary lookup per pixel. Because neighbouring cells have near-identical
   answers, also **seed each solve from the neighbouring cell's solution** — that typically cuts
   the Gauss-Newton stage to 1–2 iterations. This is exactly why Mixbox precomputes `unmix` into a
   256³ table: their per-colour solve is 100 ms, ours will be ~1 ms, but the same reasoning
   applies once you are doing it per pixel.
3. **Do not interpolate between LUT cells for recipes.** Interpolating *weights* between cells is
   fine only if the *paint subset* is identical; across a subset boundary it is nonsense.
   Interpolate the resulting colour if you need smoothness, or just accept the quantisation (this
   is a paint-by-numbers app; posterisation is a feature).

---

## 5. Paint-by-numbers and paint-matching prior art

### 5.1 Direct competitors that already do what this app does

This is the most commercially significant finding: **several shipping mobile apps already solve
the exact problem, and at least two claim Kubelka–Munk.**

- **Impasto — Paint Recipes** (iOS, free) —
  <https://apps.apple.com/us/app/impasto-paint-recipes/id6760235665>
  Claims *"Kubelka-Munk spectral color science at 36 wavelengths"*; photo input (tap a colour in
  your reference photo); user-customisable palette ("match the tubes you actually own"); output is
  *"pigment names, ratios, and an accuracy percentage"* — their own example: *"3× Burnt Umber +
  1× Yellow Ochre + 1× Titanium White — 99 % match"*. Oil/acrylic/watercolour/gouache.
  **36 wavelengths ⇒ 380–730 nm at 10 nm ⇒ Burns/Berns-aligned, not spectral.js-aligned.**
- **Mixable — Paint Mixing Guide** (iOS) —
  <https://apps.apple.com/us/app/mixable-paint-mixing-guide/id6769655280>
  "picks any target color and calculates the best possible match using only the paints in your
  palette, with exact ratios"; states the engine uses Kubelka–Munk.
- **Real Color Mixer** (iOS/Android) — "sophisticated light-spectrum model … subtractive"; has a
  **"Find Mixture"** reverse-solver.
- **Real Paint mixing tools PRO** (iOS), **Able Software Artist Color Mixer** (web).
- **GOLDEN / Williamsburg Virtual Paint Mixer (MXR)** —
  <https://goldenartistcolors.com/mixer/acrylic>, <https://goldenartistcolors.com/mixer-overview>
  Manufacturer-official. Sample a colour from an uploaded image, or enter RGB/CMYK, and get a
  recipe from a chosen palette; shows a **"CIE dist & ALERT"** metric (distance between the CIE
  value of target and mixture) — the same UX this project wants.
  **Crucially it is not a physics model:** *"the data that runs the program are generated from
  physical colors that were mixed, scanned, and then used to build the software, as opposed to
  colors based solely on digital computations"* — for the Williamsburg oil MXR, *"about 2,000
  color mixes, 11,000 spectrophotometer readings and over a year to develop."* Documented caveats:
  it *"will not always provide the same mix twice"* because it works on averages; and some
  mixtures are less accurate because of *"subtleties of pigment particle size or transparency that
  are too complex for a lightweight online tool"*. It also says *"mixing by weight is the most
  accurate way to recreate a mixture"*.

**Implication:** the differentiators available to this project are (i) desktop/offline with a real
photo pipeline rather than single-colour tapping, (ii) two-constant K–M on measured data instead
of a synthetic single-constant approximation, (iii) exact best-subset optimisation with a stated
ΔE2000 and a metamerism warning, and (iv) human-executable ratio snapping. Not "we do K–M" — that
is table stakes now.

### 5.2 Paint-by-numbers generators (open source) — none do mixing

`drake7707/paintbynumbersgenerator` (**MIT**, 425 stars, TS, SVG output, live demo at
<https://drake7707.github.io/paintbynumbersgenerator/>) is the best of the bunch; also
`cj-holmes/paintr` (R, supports supplying a custom target palette and maps to nearest),
`ethan-grinberg/paint-by-number`, `LukaZdr/paint_by_numbers_image_generator` (k-means),
`WorkNeberg/my-paint-by-numbers`, `GulnazSerikbay/PBN`, `brufino/paint_by_numbers`,
`DmitroKDS/IBNG`.

**Every one of them stops at "k-means / superpixel posterise, then map each region to the nearest
colour in a palette."** None computes a mixing recipe. The reusable ideas are on the *region*
side, not the colour side: SLIC/superpixel segmentation, region merging with a minimum-area
threshold, SVG contouring, and number-label placement. `drake7707`'s MIT licence makes it the one
to read for the vectorisation stage.

### 5.3 Hardware colour matchers — nearest-swatch, not mixing

Nix Mini 2/3, Nix Spectro 1/2, Color Muse / Color Muse 2 / 3, Datacolor ColorReader and
ColorReader Spectro, plus the Sherwin-Williams ColorSnap and Behr apps. All of these **match a
measured colour to the nearest ready-mixed product in a fan deck** (Nix advertises 300 000+ brand
paints). They do not solve mixtures. Their relevance to us is (a) as the credible way to *acquire*
palette data if manufacturer swatches prove untrustworthy — a Nix Mini 3 is cheap — and (b) as
evidence that "match to a fixed catalogue" is a solved commodity while "compute a mixture" is not.

### 5.4 Academic palette / layer decomposition — what to borrow

- **Chang, Fried, Liu, DiVerdi, Finkelstein. *Palette-based Photo Recoloring.* ACM TOG 34(4)
  (SIGGRAPH 2015).** <https://gfx.cs.princeton.edu/pubs/chang_2015_ppr/index.php>
  Extracts a small palette and propagates edits via RBF weights in Lab. Borrow: the palette
  *extraction* (k-means in Lab with careful initialisation) and the idea that a handful of colours
  plus per-pixel weights reconstructs the image well. Not pigment-aware.
- **Tan, Lien, Gingold. *Decomposing Images into Layers via RGB-space Geometry.* ACM TOG 36(1)
  (2016/2017).** <https://cragl.cs.gmu.edu/singleimage/> ·
  code <https://github.com/CraGL/Decompose-Single-Image-Into-Layers> (108 stars, **no LICENSE file
  → all rights reserved**; read, do not copy).
  Core insight, and it is the directly transferable one: **the set of colours reachable by mixing
  a palette is the convex hull of the palette in the relevant space**, and decomposition is
  finding barycentric-style coordinates inside that hull. That is precisely the polytope Centore
  describes, arrived at from graphics rather than colour science.
- **Tan, DiVerdi, Lu, Gingold. *Pigmento: Pigment-Based Image Analysis and Editing.* IEEE TVCG
  25(9), 2019** (arXiv:1707.08323, <https://arxiv.org/abs/1707.08323>;
  project <https://cragl.cs.gmu.edu/pigmento/>; code
  <https://github.com/JianchaoTan/Pigmento-PaintingAnalysis> — **no LICENSE file, 404 → all rights
  reserved**). Models each pixel as a mixture of a small number of pigments with **multispectral
  absorption and scattering coefficients** and recovers that structure from an RGB image.
  **The single most relevant paper to this project after Lindemeier's.** Their per-pixel weight
  solve is the same constrained problem; their `KM_rendering.py` is the forward model. Borrow the
  formulation and the two-stage "recover palette, then recover per-pixel weights" architecture.
- **Tan, Echevarria, Gingold. *Efficient palette-based decomposition and recoloring of images via
  RGBXY-space geometry.* ACM TOG 37(6) (SIGGRAPH Asia 2018).**
  <https://cragl.cs.gmu.edu/fastlayers/> · code <https://github.com/CraGL/fastLayerDecomposition>
  Decomposes a 6 MP image into layers in **20 ms after preprocessing**, with the core algorithm in
  ~48 lines of Python. **This is the answer to "how do I do this per-pixel on a whole photo
  fast":** precompute the geometry once (a Delaunay/convex-hull structure over RGBXY), then every
  pixel is a barycentric lookup. Same spirit as the 3D-LUT recommendation in §4.7 but with a
  principled interpolation structure.
- **Aharoni-Mack, Shambik, Lischinski. *Pigment-Based Recoloring of Watercolor Paintings.*
  NPAR 2017.** Where Lindemeier's palette extraction comes from: project image colours to the
  CIELAB *ab* plane, take the convex hull, iteratively simplify with **Douglas–Peucker** until you
  have `k` colours. Cheap, deterministic, and much better than naive k-means at capturing the
  *extremes* of an image's gamut — which is what you want if the palette must be mixable.
  Lindemeier's tweak: force the darkest and brightest colours in, then extract `k − 2`.
- **Shugrina, Kar, Fidler, Singh. *Nonlinear Color Triads…*, ACM TOG 39(4), 2020** — reduced-order
  parametric fit to a colour distribution; mentioned by Mixbox as related but "tailored to
  manipulating existing artwork".

---

## 6. Datasets of real paint colours

Ranked by trustworthiness. **The general warning holds and should be stated in the app:
manufacturer web swatches are sRGB-clipped, screen-rendered, and often out of gamut — Berns found
31 % of real Golden acrylic tints/tones/masstones fall outside sRGB.** A palette built from
scraped hex codes will be systematically desaturated and hue-shifted in exactly the saturated
regions where mixing decisions matter most.

### 6.1 Tier 1 — measured spectral data, machine-readable

- ⭐ **`Wacton.Unicolour.Datasets` → `ArtistPaint`** — 19 Golden Heavy Body acrylics as
  two-constant K and S arrays in C#, 380–750 nm/10 nm/38 bands, `K1 = 0.03, K2 = 0.65`, each
  labelled with its colour index (PW6, PBk9, PY74, PY83, PY184, PO20, PO73, PR108, PR254, PV19,
  PR122, PV23, PB29, PB28, PB36, PB15 ×2, PG7, PG36).
  **Licence: MIT (Unicolour's).** Source data: Berns' Artist Paint Spectral Database.
  This is the highest-value dataset for this project by a wide margin — it is already in C#, it is
  two-constant, and it covers a sensible mixing palette.
- **Roy S. Berns. *Artist Paint Spectral Database.* Proc. CIC24 (2016).**
  Free paper: <https://www.rit.edu/science/sites/rit.edu.science/files/2019-03/ArtistSpectralDatabase.pdf>
  19 Golden Heavy Body acrylics on Leneta Form 3B opacity charts, 0.006″ drawdown bar, masstone +
  a white tint, weighed to 0.005 g, **Macbeth MS7000 integrating sphere, SPIN, 4 measurements
  averaged, 380–750 nm at 10 nm**. 23 hues + a grey scale, 770 unique computed spectra, plus
  eigenvectors and optical data. Originally "Excel file available by request". **This is the source
  Mixbox used.**
- **Roy S. Berns. *Artist Acrylic Paint Spectral, Colorimetric, and Image Dataset.* Archiving
  2022, DOI 10.2352/issn.2168-3204.2022.19.1.10.**
  Free PDF: <https://grayskyimaging.com/wp-content/uploads/2022/06/Berns_Archiving_2022.pdf>
  **58** Golden Heavy Body acrylics (from 68 supplied by Golden; mixtures/fluorescents/carbon &
  mars black excluded) → **831 varnished tints, tones and masstones** via **two-constant opaque
  K–M**. X-Rite MS7000, integrating sphere, specular included, 360–750 truncated to **380–730 nm**.
  Saunderson `K1 = 0.035` (from masstone minimum reflectance), `K2 = 0.6` (theoretical, n = 1.5,
  normal illumination), `K_instrument = 1.0`. Masstone–tint method for unit K and S relative to
  white's scattering = 1. Honest about its limits: the method is *determinate* (2 samples, 2
  unknowns → perfect fit at 10 % tint and masstone, so accuracy cannot be assessed), reflectances
  needed smoothing, and cobalt/cerulean produced negative optical values that were **corrected
  manually**. Distribution: *"An Excel spreadsheet … available for downloading at
  grayskyimaging.com."* **UNVERIFIED:** I could not reach a direct download URL or a licence
  statement on grayskyimaging.com; the site's Resources page did not resolve for me. Treat the
  licence as "ask Berns" until confirmed.
- **Golden Paint Spectra (Glassner & Haines)** — <https://www.realtimerendering.com/golden.html>
  and blog post <https://www.realtimerendering.com/blog/free-golden-paint-spectra-spreadsheet/>.
  Andrew Glassner asked Golden Artist Colors for spectra; **Golden agreed to release them for
  free** and permitted redistribution ("Golden Artist Colors, Inc. has given spectral data for
  their acrylic paints and allowed it to be shared with others"). 68 Heavy Body paints, masstone
  and 10 % titanium-white tint. **UNVERIFIED — both URLs are behind Cloudflare and returned 403
  to me;** I could not read the exact permission wording or grab the spreadsheet. Worth opening in
  a browser: this is potentially the cleanest legally-clear bulk source.
- **Yoshio Okumura. *Developing a spectral and colorimetric database of artist paint materials.*
  MSc thesis, RIT, 2005.** <https://repository.rit.edu/theses/4892/> — free PDF (9 MB).
  Acrylics; the source Mixbox cites for the Saunderson reflectance constants.
- **Paul Centore. *A Colour Survey of Artist's Pastels.* J. Int. Colour Assoc. 15, 42–59 (2016).**
  <http://www.munsellcolorscienceforpainters.com/ColourSciencePapers/AColourSurveyOfArtistsPastels.pdf>
  **Measured spectral data for 3 154 pastels across 8 brands.** Wrong medium for us, but by far the
  largest independently-measured artist-material spectral survey I found, and a model for how to
  publish one.

### 6.2 Tier 2 — measured Lab/hex, large coverage, licence unclear

- **artistpigments.org** — <https://artistpigments.org/> · methodology page
  <https://artistpigments.org/methodology> · <https://artistpigments.org/our_database>
  **80 577 artist paints, 1 397 brands, 36 media**, searchable, with **CIE Lab and Munsell
  notation**, a "Digital" view with hex codes and out-of-gamut warnings. Measured with **X-Rite
  i1Basic Pro 3 (from 2024), 380–730 nm**, previously **Nix Spectro 2** (author reports the two
  agree closely), plus CHNSpec DS-620 for neons.
  **This is real instrument data, not swatch scrapes — the best broad-coverage source for Golden /
  Liquitex / W&N / Amsterdam / Arteza Lab values.**
  **UNVERIFIED / blocker:** both the homepage and `/methodology` returned **HTTP 403** to my
  fetches (bot protection). I could not confirm terms of use, whether there is an API or bulk
  export, or who runs it. **Someone must open this in a browser and read the terms before any
  data is ingested.** Assume "all rights reserved, ask permission" until proven otherwise.
- **handprint.com (Bruce MacEvoy)** — <https://www.handprint.com/HP/WCL/mixtable.html>,
  <https://www.handprint.com/HP/WCL/pigmt8.html>
  GretagMacbeth spectrophotometer, 2 cm swatches on Arches CP, measured **wet (30–60 s) and dry
  (4 h+)**, two swatches averaged. Wet→dry shifts tabulated for **75 pigments/convenience
  mixtures**, sortable by total ΔE / lightness / chroma / hue. Also a large **mixing-complements**
  study: 21 cool single-pigment paints × 50 warm complements, mixed and assessed.
  Watercolour, and HTML tables (no clean export), and MacEvoy's own caveat that *"the accuracy of
  this table may change as manufacturers change pigment suppliers."* Copyright MacEvoy — scraping
  is not licensed. Value here is as **validation** and as evidence for how large wet→dry shift is
  (a real physical effect this app currently ignores entirely).

### 6.3 Tier 3 — hex-only, treat with suspicion

- Gist scraped from matchmypaintcolor.com: <https://gist.github.com/brendancol/6b23b0cc8f32908da0facfb3dd4cbb5e>
  (`paint-colors.json`) — architectural paints, provenance unknown.
- `devchauhann/colors-database` (13 638 palettes), `meodai/color-names`,
  `uncommoncode/color_palettes_json`, `palettejson/palettejson-schema` (a portable palette
  container format worth adopting for the app's own palette files).
- I could **not** find a credible, licensed, machine-readable JSON/CSV of Golden / Liquitex /
  Winsor & Newton / Amsterdam / Arteza **artist acrylic** sRGB or Lab values on GitHub.
  **UNVERIFIED / likely does not exist.** The realistic options are: (1) use Unicolour's 19-paint
  Berns set, (2) get permission from artistpigments.org, (3) buy a Nix Mini 3 and measure the
  actual tubes the user owns — which is also the only way to handle brand and batch variation.

### 6.4 The swatch-accuracy warning, with numbers

Berns 2022 measured a real Golden acrylic gamut and found **22 % of the 831 tints/tones/masstones
outside AdobeRGB (1998)** and **31 % outside sRGB IEC61966-2.1**. Mixbox hit the same wall from
the other direction: mixtures of four *real* pigments leave sRGB badly enough that they had to
invent surrogate pigments. Consequence for this project: **a palette defined as sRGB triples is
lossy by construction for the most chromatic paints** — phthalos, quinacridones, cadmiums,
dioxazine. `Data/GoldenPalette.cs` storing sRGB is the root cause of a whole class of
"why won't it mix that green" complaints, and no amount of solver improvement fixes it. Store
spectra (or K and S) and derive sRGB for display only.

---

## 7. How to validate a pigment-mixing implementation

There is **no off-the-shelf conformance suite**. Neither Mixbox nor spectral.js ships tests
(I listed both repo trees; Mixbox has none, spectral.js has none). Here is what exists and how to
turn it into C# unit tests, in rough order of value.

### 7.1 Analytic invariants (cheap, catch most porting bugs)

These need no external data and would have caught the band-count documentation error:

1. **Round trip.** For a dense sample of sRGB (all 9 primaries/secondaries + a few hundred
   pseudo-random), `LabOf(Mix([c], [1.0])) ≈ c`. Unicolour's own `PigmentGeneratorTests.Roundtrip`
   asserts within **0.05 in sRGB** for LHTSS. For the 7-base-spectra path the tolerance should be
   tighter — measure it and lock it in.
2. **Identity and idempotence.** `Mix(a, a, t) == a` for all `t`. `Mix(a, b, 0) == a`,
   `Mix(a, b, 1) == b`. Note: spectral.js's `f²·T²·L` weighting **does** satisfy the endpoints
   (since the other concentration goes to zero) — verify the port does too.
3. **Monotonic lightness.** Along `Mix(dark, white, t)`, `L*` must be non-decreasing in `t`.
4. **Weight permutation invariance.** `Mix([A,B],[w,1−w]) == Mix([B,A],[1−w,w])`.
5. **Scale invariance of weights.** `Mix([A,B],[2,1]) == Mix([A,B],[0.667,0.333])` — true for
   Unicolour (normalises), and true for spectral.js (`ksMix/totalConcentration`) *only because* of
   the normalisation; the `f²` makes it **not** invariant to which of `[2,1]` vs `[0.667,0.333]`
   you pass unless you normalise first. **Worth an explicit test** — this is a real trap in the
   port.
6. **Gamut sanity.** Krita's measurement is a useful target: out-of-sRGB mixtures should be rare
   (they saw 0.025 % of cases, max deviation 0.05 %). Assert an upper bound.
7. **No NaN / no negative reflectance** for any input, including pure black and pure white.
   Unicolour special-cases both because `0` and `double.Epsilon` produce NaN downstream — the
   spectral.js `Number.EPSILON` floor is the equivalent guard; test it.

### 7.2 Cross-implementation golden tests (highest value per hour)

Generate reference vectors from an independent implementation and freeze them as C# test data:

- **spectral.js v3 itself** — run it in Node over a fixed list of ~200 colour pairs at
  `t ∈ {0, 0.1, …, 1}` and dump hex results. Any divergence in the C# port is then a port bug, not
  a design question. **Do this first; it is an afternoon's work and it is definitive.**
- **ColorAide** (MIT, Python) `Color.mix(..., method='spectral')` — an *independent*
  reimplementation of the same maths, so agreement across both is strong evidence.
- **Unicolour** (MIT, C#) — for the two-constant path, mix `ArtistPaint` pigments at known
  concentrations and compare against your own two-constant implementation. Same language, so this
  can be a direct in-solution test project.
- **Mixbox** — usable as an oracle **for evaluation only** under CC BY-NC (non-commercial). Its
  results are the closest thing to ground truth for "what does real paint do" without a
  spectrophotometer. Keep it out of shipped code and out of the repo if commercial use is possible.

### 7.3 Physically-grounded reference data

- ⭐ **Berns 2022 dataset (§6.1)** is the best validation corpus available: 831 spectra of
  **known-concentration** tints (with titanium white) and tones (with bone black) of 58 Golden
  acrylics, generated from measured masstone + 10 % tint. Because the concentrations are known,
  you get direct `(pigments, concentrations) → spectrum → Lab` ground truth. **Test:** feed the
  two-constant K and S, mix at the dataset's concentrations, assert ΔE2000 below ~1 against the
  dataset's Lab. Caveat to state in the test: the tints/tones are themselves *computed* via
  two-constant K–M, so this validates your K–M arithmetic and integration, **not** K–M's fidelity
  to physical reality. For that you need the raw masstone/10 %-tint measurements (which the
  spreadsheet includes for all 68 paints).
- **Berns 2016 (19 paints)** — same structure, and the 19-paint subset is exactly what Unicolour
  ships, so this is the pairing to use if you adopt Unicolour.
- **Lindemeier VMV 2018, Table 2** — a published, physically-executed recipe: 4 target palette
  colours mixed from their 14 base acrylics, with parts given (e.g. Primary Magenta 0.20 /
  Carmine Red 0.28 / Cadmium Red 0.32 / Cadmium Orange 0.13 for one column; Primary Yellow 0.70 +
  Cadmium Yellow 0.21 for another; Titanium White 1.00 for a third). No Lab values published, and
  their paints are Schmincke-ish rather than Golden, so this is a *plausibility* check on the
  solver's structure (does it also pick a magenta+red+orange combination for that target?) rather
  than a numerical assertion.
- **GOLDEN "Historical Color Matches" Table 1** —
  <https://justpaint.org/table-1-color-matches-using-golden-heavy-body-acrylics/>
  Manufacturer-published recipes in parts, e.g. Hooker's Green = Anthraquinone Blue : Nickel Azo
  Yellow **1:3**; Naples Yellow = Titan Buff : Yellow Oxide : Diarylide Yellow **20:2:1**;
  Sepia = Raw Umber : Burnt Sienna : Carbon Black **20:3:0.7**; Van Dyke Brown = Burnt Umber :
  Quinacridone Burnt Orange : Carbon Black **20:0.5:0.3**; Malachite = Titanium White : Phthalo
  Green (BS) : Cobalt Titanate Green **8:3:4**; Indian Yellow = Nickel Azo Yellow : Transparent
  Pyrrole Orange : Regular Gel **15:1:50**. **No Lab, RGB, or ΔE values are published**, and
  Golden explicitly says exact matching is impossible. Good as a "does the solver produce sane
  *kinds* of recipes" sanity corpus; several of these paints are not in the Berns 19 anyway.
- **Alexander Messick. *Mixing Paint: An analysis of color value transformations in multiple
  coordinate spaces using multivariate linear regression.* arXiv:2406.15364 (2024), CC BY 4.0.**
  <https://arxiv.org/abs/2406.15364> — **120 physically mixed pairs from 16 paints**, analysed
  across colour spaces. CC BY licensing means the data is reusable if it is in the paper/source
  tarball. **UNVERIFIED:** whether the raw measurements are published (the abstract page does not
  say; check the arXiv TeX source). If they are, this is a rare independent physical-mixture
  validation set.
- **handprint wet→dry shift table (§6.2)** — 75 pigments with measured wet and dry Lab. Not for
  unit tests (licence), but it tells you the magnitude of a systematic error this app currently
  ignores: acrylics dry darker and the shift is pigment-dependent. If recipes are consistently a
  little off in one direction, this is a likely cause, and a per-paint wet→dry correction is a
  legitimate future feature.
- **Berns' metamerism protocol** as a *test*: after solving a recipe, recompute ΔE2000 under a
  second illuminant (Berns used a 2200 K blackbody and got mean 1.8 / max 9.88 for a 3-primary
  approximation). Assert the solver's mixtures stay under some threshold; regressions in the
  spectral fit will show up here long before they show up in the D65 ΔE.

### 7.4 What to assert, numerically

Suggested thresholds to lock in (adjust once measured):

| Test | Metric | Threshold |
|---|---|---|
| Single-paint round trip | ΔE2000(input, `Mix([p],[1])`) | < 0.5 |
| Port fidelity vs spectral.js v3 | max per-channel sRGB byte diff | ≤ 1 |
| Two-constant vs Berns dataset at known concentrations | mean / max ΔE2000 | < 0.5 / < 1.5 |
| Solver optimality | best-subset result vs exhaustive fine-grid search | ΔE2000 difference < 0.1 |
| Illuminant robustness | ΔE2000 under illuminant A after matching under D65 | < 2 warn, < 5 fail |
| Perf | full recipe solve, 36-paint palette, ≤ 3 paints | < 50 ms |

---

## 8. Recommended changes, ranked by (impact / effort)

Impact and effort both 1–5; ratio drives the ranking.

| # | Change | Impact | Effort | I/E | Notes |
|---|---|---|---|---|---|
| 1 | **Fix the band-count comments** in `SubtractivePaintMixer.cs` (lines 38–39, 52–54) and `PROJECT.md`: it is **380–750 nm**, 38 bands. Replace the bare `BandCount` with `StartWavelengthNm = 380` + `WavelengthIntervalNm = 10`. | 3 | 1 | **3.0** | Pure documentation bug today; a real bug the moment measured data is introduced. Do it now, before agent 3's paint data lands. |
| 2 | **Replace fixed-ratio enumeration with best-subset + NNLS + Gauss-Newton** (§4.2–4.5). Exhaustive over subsets of size ≤ 3 with hull-bracketing pruning. Delete the "skip triples above 30 paints" hack. | 5 | 2 | **2.5** | The single biggest quality win. Returns the *provably optimal* ≤3-paint recipe instead of the best point on an arbitrary 7-/4-point ratio grid. Also removes a scalability cliff. ~2 300 subsets at 24 paints, ~36 K at 60. |
| 3 | **Switch the objective from squared ΔE76 to ΔE2000** (retrieve k ≈ 32 candidates by ΔE76, re-rank by ΔE2000). | 4 | 1 | **4.0** | ΔE76 misranks saturated blues and near-neutrals — exactly where paint mixing is hard. Cheapest real accuracy gain available. Highest ratio on the list; only ranked below #2 because #2 changes what is being ranked. |
| 4 | **Snap final weights to human-executable ratios** (1:1, 2:1, 3:1, 1:1:1, 4:1, 5:1, 20:3:1…) and report ΔE for the snapped recipe, with the mix stated **by weight** (per Golden's guidance). | 4 | 1 | **4.0** | "0.6431 : 0.2109 : 0.1460" is unusable at a palette. This is a UX fix that costs almost nothing and transforms perceived quality. |
| 5 | **Expose per-paint tinting strength** `T` (spectral.js's missing knob) with sane defaults by pigment family (phthalo/dioxazine high, cadmium/earth low). | 3 | 1 | **3.0** | The port dropped `T` (hard-coded to 1). It is the only hand-tuning lever available until real K/S data arrives, and it directly addresses "why does adding a little phthalo not turn it blue". |
| 6 | **Take a dependency on `Wacton.Unicolour` (MIT, netstandard2.0, zero deps) and its `Datasets` package**, and add a **two-constant K–M** path using `ArtistPaint`'s 19 Golden pigments. Keep the spectral.js path as the fallback for arbitrary user-supplied sRGB colours. | 5 | 3 | **1.7** | Replaces the synthetic single-constant model with measured physics for the paints it covers. Also gets Saunderson correction, ΔE2000, gamut mapping, and LHTSS reconstruction for free. See build-vs-borrow below. |
| 7 | **Add the residual term** (Mixbox's idea, ColorAide's MIT implementation): carry `XYZ_target − XYZ(reconstructed)` and add it back after mixing. | 3 | 2 | **1.5** | Makes the round trip exact and makes out-of-gamut and near-black/near-white inputs behave. ~30 lines. |
| 8 | **Replace the 7-base-spectra upsample with Burns' LHTSS** (36 bands, 380–730) so the reconstructed target spectrum lines up with real measured paint data and is smooth/physically plausible. | 4 | 3 | **1.3** | Unicolour's `PigmentGenerator` is a working MIT C# implementation to read (Newton on the KKT system, 20 iterations, tol 1e-8). Note LHTSS round-trip is only ~0.05 in sRGB, so pair it with #7. Blocked on #6 for the band-grid decision. |
| 9 | **Lazy memoised 3D LUT over quantised sRGB** for whole-photo conversion, seeded from neighbouring cells, persisted per palette. | 4 | 3 | **1.3** | This is what makes per-pixel recipes feasible; it is the Mixbox precomputation trick and the `fastLayerDecomposition` insight. Do after #2 so you are caching good answers. |
| 10 | **Store the palette as spectra (or K and S), not sRGB triples**; derive sRGB for display only. | 5 | 4 | **1.25** | 31 % of real Golden acrylic tints are outside sRGB (Berns 2022). An sRGB palette is lossy exactly where mixing matters. Big refactor of `Data/GoldenPalette.cs` and everything downstream; coordinate with agent 3. |
| 11 | **Golden-test the port against spectral.js v3 via Node** (~200 pairs × 11 t-values) and freeze the vectors as unit tests. | 3 | 2 | **1.5** | Definitively separates "port bug" from "design limitation". Should arguably be done *before* #2 so refactors are safe. |
| 12 | **Metamerism warning:** report ΔE2000 under a second illuminant (A or 2700 K) alongside the D65 match. | 3 | 2 | **1.5** | Genuine differentiator vs the shipping competitor apps (§5.1); painters hang work under tungsten. Needs #6 or #8 for a trustworthy target spectrum. |
| 13 | **Better palette extraction from the photo:** Aharoni-Mack CIELAB *ab*-convex-hull + Douglas–Peucker, with Lindemeier's darkest/brightest forcing. | 3 | 3 | **1.0** | Captures the image's gamut extremes far better than k-means, which matters because extremes are the hardest to mix. |
| 14 | Adopt a **portable palette file format** (PaletteJSON) so users can supply "the tubes I actually own" — which every competitor app does. | 3 | 3 | **1.0** | Feature parity item. |
| 15 | **Do not** adopt Mixbox, Accord.NET, Centore's toolbox, or Tan's decomposition code. | — | — | — | CC BY-NC / dead+LGPL / GPLv3 / no licence respectively. See §9. |

Sequencing suggestion: **1 → 11 → 3 → 4 → 2 → 5 → 6 → 7 → 8 → 9 → 10**, with 12–14 as features
once the engine is trustworthy.

---

## 9. Build vs borrow — recommendation for the mixing engine

**Borrow Unicolour for the physics and the data. Build the recipe solver yourself. Do not adopt
Mixbox.**

**Borrow: `Wacton.Unicolour` + `Wacton.Unicolour.Datasets` (MIT).**
This is the unusual case where the licence, the language, the target framework, the dependency
footprint, and the feature set all line up perfectly. MIT, pure C#, **.NET Standard 2.0** (works in
.NET 5 WinForms), **zero dependencies**, actively maintained (last push 3 weeks ago), and it
already contains: two-constant Kubelka–Munk with Saunderson correction; single-constant K–M;
Burns LHTSS reflectance reconstruction; a spectral.js port; CIEDE2000; gamut mapping; 40 colour
spaces; **and Berns' measured K and S for 19 Golden Heavy Body acrylics**. The hand-port currently
reimplements a strictly weaker subset of that (single-constant only, synthetic spectra, no
Saunderson, ΔE76) and carries the maintenance burden. There is no strategic value in owning this
code.

Keep `SubtractivePaintMixer.cs` alive as the fallback for the case Unicolour deliberately does not
handle: mixing arbitrary user-picked sRGB colours that have no measured pigment behind them
(Unicolour's `Pigment.GetReflectance` returns `null` if you try to mix a generated single-constant
pigment with a measured two-constant one). Architecturally: two engines behind one interface —
`TwoConstantMixer` (measured paints, the good path) and `SpectralApproximationMixer` (arbitrary
colours, the fallback) — with the UI indicating which is in use and how confident it is.

**Build: the recipe solver.** Nothing off the shelf does it. Mixbox's `unmix` is locked to its four
surrogate primaries. spectral.js explicitly does not do inverse (issue #23, closed unanswered).
colour-science has no K–M. Centore's toolbox is GPLv3 MATLAB. Lindemeier's PaintMixer is LGPL-3.0
archived C++ needing Ceres. Tan's code is unlicensed Python. But the *algorithm* is fully published
and small: Allen (1966/1974) gives the two-stage structure, Centore (2016) gives the constrained
geometry, Lindemeier (2018) gives the sparsity term, and §4 above has the pseudocode. Weighted
NNLS plus damped Gauss-Newton plus exhaustive ≤3-subset enumeration is maybe 400 lines of C# with
no dependencies (or Math.NET, MIT, if you would rather not hand-roll the inner least-squares).
This is also the part that is *specific to this product* — it is where the differentiation lives,
and it is the part worth owning.

**Reject: Mixbox.** Best-in-class mixing quality, easy C# integration, and the wrong tool anyway:
it cannot express "mix these three tubes from your Liquitex set", only "here is what these two RGB
values would look like mixed through four fixed pigments". Add **CC BY-NC 4.0** with
unpublished, quote-only commercial pricing and an unresolved question about a possibly-related
patent, and it becomes a liability the moment this app is anything other than a personal project.
Use it during development as an **evaluation oracle** if you like — it is the closest free proxy
for ground truth — but keep it out of the shipped binary and out of the repository.

---

## 10. Open questions and things I could not verify

Flagged explicitly, because several are licence claims:

1. **Mixbox commercial pricing** — not published anywhere. Email-quote only. Any figure would be
   invention.
2. **US 10,924,633** ("RGB-based parametric color mixing system for digital painting") — surfaced
   in search; Google Patents 404'd for me. **Assignee, inventors, status, and any relation to
   Secret Weapons or to Mixbox's method are unverified.** Worth 10 minutes on Espacenet before any
   commercial decision.
3. **artistpigments.org terms of use / API / bulk export** — HTTP **403** on both `/` and
   `/methodology`. The instrument details (i1Basic Pro 3, 380–730 nm, Nix Spectro 2) came from
   search snippets, not from the page itself. **Must be read in a browser.** Assume all rights
   reserved.
4. **Berns 2022 dataset download URL and licence** at grayskyimaging.com — the paper says the
   Excel and image are "available for downloading at grayskyimaging.com" but I could not locate a
   direct link or licence statement.
5. **realtimerendering.com Golden Paint Spectra** — Cloudflare 403 on both the data page and the
   blog post. The permission wording ("Golden … allowed it to be shared with others") is from
   search snippets. This is the most likely source of a legally clean bulk spectral dataset, so
   confirming it is high value.
6. **`Pigmento-PaintingAnalysis` licence** — `LICENSE` returns 404; GitHub reports `NOASSERTION`.
   Treat as **all rights reserved**.
7. **`CraGL/Decompose-Single-Image-Into-Layers`** — GitHub reports no licence. Same treatment.
8. **MyPaint** — I found no evidence of shipped K–M/spectral blending, only the discussion thread
   that seeded Krita's. **Likely absent, not confirmed.**
9. **"coloralgebra", "pigmentmixing" (standalone), "ArtColorMix"** — no such repositories found.
   Possibly a garbled reference to `scrtwpns/pigment-mixing`.
10. **Messick arXiv:2406.15364 raw data** — CC BY 4.0 paper, but whether the 120 measured mixtures
    are published as data (vs only summarised) is unconfirmed; check the arXiv source tarball.
11. **spectral.js grid label** — the maintainer said "380 to 730 nm" in issue #12, but the shipped
    v3 arrays are unambiguously 38 samples on the 380–750 nm grid (verified against CIE 1931
    landmarks). I am confident in 380–750 for v3 and believe 380–730/36 refers to v1, but the
    author has not stated this explicitly anywhere I could find.
12. **Berns' Saunderson constants differ between his own papers** — `K1 = 0.035, K2 = 0.6` (2022
    paper) vs `K1 = 0.03, K2 = 0.65` (as encoded in Unicolour's `ArtistPaint` for the 2016
    19-paint set). Not a contradiction (different datasets, different instruments) but worth being
    deliberate about which pair goes with which data. Hand this to agent 3.
