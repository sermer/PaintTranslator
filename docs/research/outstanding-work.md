# Outstanding Work

**Updated:** 2026-07-26
**Companion to:** [acrylic-blending-findings.md](acrylic-blending-findings.md) — the
research this is drawn from. Raw source reports are in
[source-reports/](source-reports/).

Everything here is *not yet built*. Items are grouped by what unblocks what, because
several of them are gated on a single performance problem.

---

## Where things stand

**Done and staged (uncommitted):**

- Mixing weights are used as concentrations, not squared — recipes now mean what they say
- `Strength` → `RelativeScattering`, with the two-constant algebra documented
- Wavelength range corrected to 380–750nm with explicit constants
- HyAB search metric with lightness weighted 1.5
- Recipes restricted to a geometric parts ladder; percentages no longer printed
- CIEDE2000 with match quality and error direction in the tooltip
- `MeasuredPalette` — 19 Golden paints with measured two-constant K and S, via Unicolour
- `MeasuredPaintMixer`, and a `PaintBlendMatcher` constructor over measured paints

**Test suite:** 134 passing.

**The app itself still uses the reconstructed-spectrum palette.** The measured path
exists, is tested, and is not yet reachable from the UI. Unblocking that is item 1 below.

---

## Blocker — everything user-visible waits on this

### 1. Candidate-set construction is 7,789 ms (was 14 ms)

`PaintBlendMatcher` sweeps roughly 7,000 mixtures (19 singles + 171 pairs × 15 ratios
+ 969 triples × 4), and each `new Unicolour(pigments, weights)` costs about a
millisecond. The tooltip builds this lazily on first hover, so switching the UI to the
measured palette would mean an eight-second freeze.

Root cause: Unicolour's `SpectralCoefficients` keeps `Coefficients`, `Wavelengths`,
`Start` and its indexer **`internal`**, so the measured K and S arrays cannot be read
out and mixed in a tight loop. Every candidate has to go through the full public
`Unicolour` construction.

Options, best first:

1. **Vendor the coefficients.** `Unicolour.Datasets/ArtistPaint.cs` is MIT — copy the
   K and S arrays into a project data file with attribution, then mix in
   `SubtractivePaintMixer` directly. Removes the per-candidate allocation entirely and
   also unblocks item 2, which needs a shared band layout.
2. **Precompute the candidate set** into a generated data file at build time.
3. **Build on a background thread**, showing reconstructed results until ready.
4. **Cut candidates** — drop triples, or thin the ratio ladder. Cheapest, and loses the
   most.

Option 1 is the one that also solves the next item, so it is the recommended path.

---

## Recommendations 2–4, as originally scoped

### 2. Ingest the remaining 59 Golden paints

`GoldenSpectra.zip` (verified, redistributable, Golden granted permission) holds 78
paints. 19 of them already have superior two-constant data; the other 59 have measured
reflectance and measured Lab, which is still far better than an sRGB triple.

Verified file layout: sheet1 `A1:BQ80`, **header on row 2**, data rows 3–80.
`A` Prod #, `B` Name, `C/D/E` L\*a\*b\*, `F..AK` % reflectance **400–700nm / 31 bands**,
`AL..BQ` K/S for the same bands. Columns `G` and `AM` are spacers. Measured **D65/10°**.
Confirmed the K/S column is single-constant from reflectance (Alizarin at 400nm:
R=5.44% → (1−R)²/2R = 8.2184 = cell AL).

To make these mixable with the measured 19, they need the same band layout and the same
K–M model. Derivation, in order:

1. Resample 400–700/31 → 380–750/38, holding the endpoint values. Defensible because
   the CIE observer weights are tiny below 400nm and `ObserverZ` is already 0 above
   ~650nm.
2. Remove surface reflection with **inverse Saunderson** (SPEX form), because the 19's
   coefficients were fitted with it applied and raw reflectance has not been:
   `R_internal = R_measured / ((1 − k1)(1 − k2) + k2 · R_measured)` with k1=0.03, k2=0.65.
   The denominator is `0.3395 + 0.65·R`, always positive, so there is no singularity.
   Clamp just below 1 as a guard — it can exceed 1 as R approaches 1, though Golden's
   maximum of ~0.9 only reaches ~0.973.
3. With `s = 1`, single-constant K/S *is* k: `k = (1 − R_i)² / (2 · R_i)`.

The `s ≡ 1` assumption asserts "this paint scatters like titanium white." Wrong for
transparent pigments, correct in scale (Berns normalises S(white)=1), and a strict
improvement on the luminance stand-in. Document it in code and surface it as provenance
in the UI so a user can see which paints are high-fidelity.

**Two caveats on the file.** It is "10 mil drawdowns over **white**" — opaque paints
approximate mass tone but transparent ones are substrate-contaminated, which is why
K/S saturates with reflectance flooring at 3.7–4.6%. And there is **no white paint row
at all**; white must come from the Unicolour set.

### 3. Masstone/tint inconsistency in `GoldenPalette.cs`

`"Ultramarine Blue"` `(50,47,75)` is a mass-tone value, while `"Light Ultramarine Blue"`
`(100,160,230)` and `"Light Phthalo Blue"` `(143,217,242)` are tints. The mixer treats
every entry as a mass tone, so the tint entries mix as though they were dense pigment.
This is a correctness bug, not precision loss.

Partly moot once item 2 lands, since all 19 measured entries are mass tones — but the
legacy palette keeps the bug until it is either corrected or retired.

Other errors in that file, from the sRGB round trip:

- Bone Black `(35,34,36)` → L\*≈13.7 against Golden's published **23.82**
- Titanium White `(255,247,255)` implies a\*>0 and b\*<0; published is a\*−0.74, b\*+1.24 —
  both signs inverted
- Cadmium Yellow Medium (b\* 94.59) has no sRGB representation at all

### 4. Sigmoidal L\* rescaling before matching

The palette spans roughly L\* 24–98 — about 74 units, 24:1 contrast, lower than a
magazine page. Every photo shadow below L\* 24 currently collapses onto a single black,
which is the classic ICC "flat, plugged-up shadow" failure.

Rescale the photo's lightness into the palette's real range before matching, preserving
value *relationships* rather than absolute values. This is independent of all the
pigment work and could be done at any time.

---

## Deferred by request: "do after"

- **Best-subset NNLS + Gauss–Newton** replacing the ratio-ladder sweep. Exhaustive
  ≤3-paint subsets is 2,324 combinations at 24 paints and 36,050 at 60 — trivially
  interactive and *provably optimal*, unlike a fixed ratio grid. Reject orthogonal
  matching pursuit: paint libraries have high mutual coherence, so greedy selection
  makes early mistakes it cannot correct. See Allen's two-stage algorithm (JOSA 56:1256,
  1966; 64:991, 1974) and Centore's convex-polytope formulation.
- **Region segmentation before matching.** Match 200–2,000 region colours rather than
  12M pixels: 100–600× speedup, and it makes metric cost irrelevant. Edge-preserving
  prefilter → SLIC superpixels or mean-shift → minimum-region-size cleanup.
- **Glazing as a separate feature** from wet-in-wet mixing, using the finite-thickness
  K–M form over a substrate. Different math; do not model one with the other.

---

## Longer term

- **Masstone + 1:10 tint pairs** for full two-constant K and S on every paint. Golden's
  own lab characterisation pair, and the mathematical minimum for two unknowns.
  Curtis et al. give a closed form from a white-card + black-card swatch pair, which
  would also let users characterise their own tubes.
- **Optional ColorChecker calibration.** Uncalibrated phone photos carry a measured
  error floor of ΔE 2.1 in daylight and 4.2 under tungsten, at or above the ΔE 1.8
  acceptability threshold. A chart step plausibly reaches ΔE ≈ 1, and no surveyed
  competitor offers it.
- **Metamerism warnings.** A spectral match holds up under changing gallery light; a
  tristimulus match can fall apart. Warn when a chosen recipe is strongly metameric.
- **Provenance in the UI.** Show which paints have measured data, which are
  reflectance-only, and which are convenience mixes containing 2–3 pigments (mixing two
  of those stacks 4–6 pigments and lands in the 23–28% chroma-retention band).

---

## Known defects, unresolved

### Mixing can create chroma

Measured over 20,000 random pairs on the reconstructed path: **930 produce a mixture
more chromatic than either parent**, worst excess C\* +34.5. Subtractive mixing cannot
do this physically; it is an artifact of the 7-basis sRGB reconstruction. Not asserted
as a test because it currently fails. Should disappear as measured spectra replace
reconstruction — worth re-measuring afterwards to confirm.

### `MinReflectance` is 1e-15 and governs black mixing

Through an accidental cancellation (`Y·K/S = 0.5` regardless of the floor), this
constant controls how blacks mix; 50/50 black+white gives L\* 68.0. The research
recommends raising it to ~1e-4.

**Deliberately not done**, because there is no validation target. "Too light" is
relative to real paint and no measured reference was available. Two candidate
assertions were tried and rejected as non-discriminating — "darker than the L\*
midpoint" and "darker than a linear RGB average" both already pass. Revisit once
measured spectra provide a reference.

### Berns and Golden disagree on Bone Black

Berns' measured mass tone reads L\* **11.4**; Golden's own chart publishes L\* **23.82**.
Two independent measurements of nominally the same paint. Do not assume the chart and
the spectra agree — prefer one source per field and record which was used.

---

## Benchmarking warning

Match quality against **random sRGB targets is a misleading metric** and should not be
used to judge these changes.

Measured over 2,000 random targets with the same 19 paints: the reconstructed path
scores mean ΔE00 **4.58** and the measured path **5.33**. The reconstructed model looks
better because it builds its spectra *from* sRGB and is therefore fitted to hit
arbitrary screen colours, while real paints have a smaller, differently shaped gamut.
Being better at reproducing arbitrary RGB is not evidence of physical correctness.

Every test that does discriminate physical correctness favours the measured path:
titanium white reproducing Golden's published L\* 98.25, phthalo blue rotating from
violet-blue mass tone (a\* +22.3) to brilliant cyan tint (a\* −17.1), and
ultramarine + yellow giving a\* −31.1.

**A meaningful benchmark needs measured real mixtures as targets.** Until one exists,
judge changes on the discriminating cases, not on aggregate ΔE against random colours.

---

## Verification debts

Carried forward from the research; check before relying on any of these.

**Not independently verified:**

- Berns' 31%-outside-sRGB and 22%-outside-AdobeRGB figures, and the three-primaries
  ΔE00 1.8 / worst 9.88 result — source papers are paywalled. Load-bearing for the
  "stop storing sRGB" argument.
- The derived tinting-strength index, which assumes Golden's tint-strength column is
  the tint's L\*. Strong evidence, unconfirmed.
- Chroma-retention and hue-separation percentages (51/35/28/23%, 68/41/22/14%).
- Phone-photo ΔE error floors.
- HyAB's √2 pruning bound and the Abasi/Tehran/Fairchild experimental result.
- Competitor feature claims (Impasto and others).

**Known unavailable:** Mixbox commercial pricing (unpublished), artistpigments.org
(HTTP 403), Berns 2022 Excel (withdrawn), LBNL/Liquitex spectral files (AES-encrypted).

**No published answer exists:** measured ΔE for the acrylic wet-to-dry shift, and human
volumetric mixing accuracy.

---

## Practical notes

- `GoldenSpectra.zip` needs a **browser User-Agent**; plain `curl` returns 403.
- Golden's web pigment chart states **no** illuminant, observer or geometry, and does
  not say whether values are mass tone or tint. The spectra file does state D65/10°.
  Do not assume they match.
- Manufacturer web swatches are notoriously inaccurate — prefer the measured datasets
  over scraped colour-chart images.
- Licences to respect: Unicolour and spectral.js are MIT. **Centore's Kubelka-Munk
  Toolbox is GPLv3 — do not port.** Tan's Pigmento repos have no LICENSE file, so all
  rights reserved. ImageSharp is the Six Labors Split License, not Apache-2.0.
