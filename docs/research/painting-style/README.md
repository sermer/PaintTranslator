# Research: Painting Style

Research into how painters actually make colour and mark decisions, aimed at one question:
**what should the converter do instead of always picking the strictly nearest colour?**

Four parallel tracks, written by separate agents that did not see each other's work (with
one exception noted under "Accuracy warnings"). This README is the synthesis. The reports
are long; read this first and go to them for detail.

| Report | Covers |
|---|---|
| [01-colour-theory-in-practice.md](01-colour-theory-in-practice.md) | Value structure, colour temperature, chroma control, harmony schemes, limited palettes, simultaneous contrast, optical mixing. 13 levers. |
| [02-styles-and-movements.md](02-styles-and-movements.md) | Impressionism through flat/graphic work, quantified where the literature allows. Historical pigment lists. Ends in a style-presets table. |
| [03-brushwork-and-edges.md](03-brushwork-and-edges.md) | Edge hierarchy, mark size, broken colour, grounds — and the classical algorithms that approximate each. 11 levers ranked by payoff ÷ cost. |
| [04-appeal-and-perception.md](04-appeal-and-perception.md) | Why literal copies look dead, value design, saliency and focal points, empirical aesthetics. Ends in a "what not to build" list. |

**Per-style follow-up research**, done once a style is being nailed down rather than merely
registered:

| Index | Covers |
|---|---|
| [abstract/README.md](abstract/README.md) | Abstract. Four more tracks, 2026-07-28. Concludes the shipped style operates on the wrong axis — abstraction is spatial, not chromatic — and **corrects the green chroma figure below**. |
| [fauvism/README.md](fauvism/README.md) | Fauvism. Four more tracks, 2026-07-28. All four measured fragmentation, not chroma, as the defect — Fauvism is the least paintable of the five styles. **Overturns the abstract round's per-hue chroma ceiling** (masstone is not the ceiling: a white tint raises C\* for 13 of 18 chromatic paints), and finds contrast 1.35 wrong by sign. |

## The one finding all four tracks reached independently

**Replace the Gaussian pre-blur with an edge-preserving filter.** This is the strongest
result in the set, because four tracks arrived at it from four unrelated directions:

- Track 1, from chroma and edge reasoning — the current blur destroys exactly the edges
  that make a painting readable.
- Track 2, from Graham & Field 2007: art has a *shallower* mean amplitude-spectrum slope
  than natural scenes (−1.21 ± 0.017 vs −1.40 ± 0.017). Blur **steepens** the spectrum, so
  blurring a photo makes it statistically *less* painting-like. `[verified]`
- Track 3, from surveying the filter families — bilateral, guided and domain transform are
  denoisers with an edge term; iterated bilateral, L0 and anisotropic Kuwahara genuinely
  flatten.
- Track 4, from Mather 2014: artworks occupy a narrower band of spectral slopes than
  closely matched photographs, and *edge-preserving* smoothing reproduces that compression
  while band-pass filtering does not.

The practical reading: **`blurTrackBar` is a simplification control, not a painterliness
control.** It is not wrong to have, but it is not the style knob, and turning it up moves
the image away from painting statistics rather than toward them.

### Measured locally, 2026-07-27 — the case is stronger than aesthetic `[verified]`

A throwaway probe against `Convert` (6-paint palette, 3007 candidates) turned the
recommendation from a stylistic preference into a correctness argument:

- A **noiseless** 512² gradient converts cleanly at `blurRadius 0` — 0.1% of pixels in
  regions ≤4px, median region 38px. The converter does *not* manufacture speckle on its
  own.
- The **same source with σ3 noise** produces 92,326 connected regions, median region area
  **1 px**, and **44.3% of pixels in regions ≤4px**. Unpaintable at any brush size.
- Amplification is measurable below visibility: σ=1 noise (input mean ΔE 0.91) leaves as
  ΔE 1.54, ×1.69, with 19.8% of pixels changing mixture. Mechanism: 33.0% of *adjacent*
  6-bit input bins map to a different mixture, 28.7% to a visibly different one, median
  ΔE 4.39 when it flips. That bin sample is uniform over the colour cube and includes
  out-of-gamut colours, so treat it as an upper bound; the gradient result is the in-gamut
  confirmation.
- **Near-duplicate candidates are not the cause.** Median candidate nearest-neighbour
  spacing is 1.70 ΔE against a median flip of 4.39 — flips land on genuinely different
  colours. Perceptual dedup of the candidate set would not help.
- Gaussian blur is the existing mitigation and needs **radius ≈5** to control σ3 noise
  (0.4% tiny regions). Radius 2 leaves 9.9% and is not enough. Radius 5 softens every edge
  in the picture to buy it.

So noise suppression before mapping is load-bearing for whether the output could be painted
at all, and a plain Gaussian is an expensive way to pay for it. The edge-preserving filter
is the cheaper one.

Anisotropic Kuwahara is track 3's pick, because it is the only surveyed filter that
flattens *along* local feature direction, leaving a directional trace like a brush. Its
radius parameter is effectively brush size. It needs a structure tensor (~60 lines), which
is shared infrastructure that also unlocks stroke orientation and flow-based DoG later.

## Suggested build order

Nothing here is decided. This is the order the evidence supports, cheapest and
best-supported first.

1. **Measure the dithering gain (~30 lines, no feature).** Track 3's finding: juxtaposed
   marks average as *radiance* — a straight line in linear light — while Kubelka-Munk
   mixing follows a darker, duller curve. Blue+yellow dithered reads grey; mixed reads
   green. So dithering is a genuine gamut *extension*, in the lightness/mid-chroma
   direction where track 2 independently found the paint gamut most constrained. Measure
   it against `SampleAchievableColors` before building anything on it.
2. **Tonalism/Whistler preset.** Track 2's most-achievable style: narrow value range, low
   chroma, dominant-hue tint, uniform soft edges — every property is a pointwise transform
   plus the existing blur. Zero spatial or semantic component. Good first proof that the
   architecture works.
3. **Mother colour** (track 1). Mix a fraction *m* of one paint into every candidate
   through the K-M kernel at build time. Contracts the gamut smoothly, no banding failure
   mode, costs nothing at match time, and every output stays genuinely mixable.
4. **Edge-preserving pre-filter**, replacing or supplementing the Gaussian. The consensus
   change above.
5. **Spatially varying blur radius from a user-clicked focal point** (track 3). Implements
   edge hierarchy and mark-size hierarchy together. Avoid a per-pixel variable kernel:
   build 3–5 blurred copies at geometric radii with the existing separable blur and lerp
   per pixel in linear light. ~120 lines, invariant untouched.
6. **Error-diffusion or blue-noise dithering onto the paint gamut**, at controllable dot
   scale — *if* step 1 justifies it. Track 2 calls this the single highest
   payoff-per-effort extension, because it is contained inside the quantiser and unlocks
   Impressionism and Pointillism at once.

## Architecture

Track 1 found that 9 of its 13 levers are one of two shapes, and tracks 2 and 3 mostly
agree: a `(L*,a*,b*) → (L*,a*,b*)` remap applied after `RgbToLab`, or a filter on the
candidate set in `BuildCandidates`. **One `LabRemap` delegate plus one `CandidateFilter`
delegate would cover most of the design space.** Worth designing around before writing any
individual style.

### The invariant is under-specified in the code

`PalettePhotoConverter`'s doc comment says blur must come before mapping. The operative
rule is narrower: **can the operation synthesise a colour outside the candidate set?**
That gives four categories (track 3):

| Category | Examples | Safe? |
|---|---|---|
| Pre-map | blur, any filter before mapping | Always |
| Post-map, selection-only | modal filters, dithering, hard-edged stroke fills, nearest-neighbour resample | Yes |
| Post-map, arithmetic | anti-aliased strokes, any filtered downsample | Breaks it — but re-running `MapPixelsFlat` repairs it cheaply, since it is cached per distinct quantised colour |
| Post-map, K-M layering | glazing, scumbling | A different, larger, physically honest invariant |

The current blanket phrasing forbids several operations that are actually fine. Worth
rewriting that comment regardless of what gets built.

## What not to build

Track 4's list, which is the most valuable single section produced. Each of these sounds
compelling and does not survive the evidence:

- **An automated "painting quality" score.** Hand-crafted image statistics explain only
  6–15% of variance in painting beauty ratings, and in a 1,629-image study they cannot
  separate the Museum of Bad Art from traditional Western oils (fractal dimension 1.47 vs
  1.56, SDs 0.13–0.15). The Pollock fractal-authentication collapse is the cautionary
  case: a Photoshop scribble made in minutes passed the published criteria.
- **Golden ratio and dynamic symmetry overlays.** The golden-rectangle preference is a
  contested 130-year-old result about bare shapes. For dynamic symmetry, track 4 found no
  empirical literature at all after a deliberate search.
- **Rule-of-thirds scoring.** ρ ≈ 0.17 against beauty ratings, and the sensitivity appears
  only in trained observers — a learned convention, not a perceptual law.
- **Automatic focal-point detection as load-bearing.** Image-independent *centre bias*
  outperforms image salience at explaining fixations on paintings. Let the user click; use
  saliency only to pre-fill that click.
- **Neural monocular depth for aerial perspective.** ONNX Runtime plus a large model file
  in a WinForms app, for an effect a two-handle gradient approximates.
- **Complementary colour harmony as the "harmonious" default.** Schloss & Palmer 2011
  (open access, 1,431 pairs): harmony peaks at *identical* hue and falls monotonically with
  hue difference; complements rated reliably *less* harmonious, F(1,47) = 17.67, p < .001.
  Only orange-blue survives, plausibly because it maps onto sun-plus-skylight.
- **Impasto** (track 3, defer indefinitely). It undermines the app's central claim, since
  shaded colours are not achievable colours.
- **Colour Field as a style preset** (track 2). Not a photo-conversion style in any
  meaningful sense.

## Two warnings that constrain the whole feature

**A naive chroma multiplier will backfire.** Track 2 computed C\*ab across all 80 paints in
`pigments.manifest.txt`: median masstone chroma 33.6, and every one of the 13 paints above
C\* 84 is yellow, orange or red. The best blue is Cobalt at 70.7; the best green is
Permanent Green Light at 56.0. A Fauvist ×2 is simply unreachable in blues and greens —
boosting chroma and letting nearest-Lab clip will band and hue-drift rather than saturate.

> **Corrected 2026-07-28 — the green figure above is wrong for any user-facing purpose.**
> Permanent Green Light is `ReflectanceDerived`, so it never reaches
> `PigmentLibrary.Selectable` and a user cannot choose it. Over the **19 selectable** paints
> the best green masstone is Phthalo Green (Y.S.) at **C\* 31.9, L\* 18.9**; hue sectors
> 120–150° and 180–210° hold **no selectable masstone at all**, and 330–360° is empty across
> all 80. Cobalt at 70.7 *is* selectable, so the blue figure stands. The practical
> consequence is that a **scalar** chroma ceiling makes "×1.5" mean 106 in yellow and 32 in
> green — see [abstract/README.md](abstract/README.md), correction 1.

> **Corrected again 2026-07-28 — the correction above is itself wrong, and so is the original.**
> Both read masstone figures off the manifest, and **masstone is not the chroma ceiling**. 13 of
> 18 chromatic selectable paints reach higher C\* in a white tint than at full strength: Phthalo
> Green (Y.S.) goes **18.9 → 56.3 at L\* 75.6**, Dioxazine Purple **6.5 → 52.6**. Dark
> transparent pigments read as near-black at masstone and two-constant K-M gets that right.
> Computed over the real candidate set (84,063 mixtures from the 19 selectable paints) there is
> **no empty hue sector and none below C\* 35**, and greens reach **C\* 86–89 at L\* 70–82**. The
> Fauvist red/green opposition *is* reachable, in a band at L\* 55–65. Related: "K-M mixing always
> lands below both parents" is also false — 6.49% of sampled pair mixtures exceed both.
> **A per-hue chroma ceiling is still the right build item; build it from the candidate set, never
> from the manifest.** See [fauvism/README.md](fauvism/README.md), correction 1. That report's own
> caveat applies: the probes transcribed `ScaleChroma` rather than calling it, so re-verify before
> building.

**Value and chroma are coupled** (track 1, Hunt effect; corroborated by the HDR
tone-mapping literature). Compressing L\* without raising chroma looks wrong. A value-curve
control cannot ship alone.

**And a bounding result on ambition** (track 2): the best published colour-only style
classifier reaches ~78% on a 3-way task over 90 images. Graham & Field state directly that
there are "few low-level statistical differences among classes" of art. A chroma
multiplier plus a tone curve will not, by itself, read as a style. **The palette and the
edge treatment carry the load** — which is good news for a palette-driven app.

## Accuracy warnings

Read these before quoting any figure from the reports.

- **Report 04 cites "report 02" for two figures that are not in it** — a ~24:1 Golden
  Bone-Black-to-Titanium-White contrast ratio, and a ΔE 2–4 uncalibrated-photo error
  floor. Report 02 did not exist when 04 was written. Both were checked against the
  finished report 02 and are absent. **Treat them as unsourced.**
- **Report 04's Graham & Field debt is cleared by report 02.** 04 relayed −1.23 / −1.40 as
  unconfirmed; 02 verified −1.21 ± 0.017 (mean of individual slopes) and −1.23 (fit to the
  mean spectrum, R² 0.97). Both numbers are right and describe different quantities.
- **The app does not have a dithered mode.** The agent brief said it did; that was an error
  inherited from a stale timing table in an archived handoff doc. `Convert` has a single
  unconditional path and no `dither` appears in any `.cs` file.
- **huevaluechroma.com (David Briggs) would not fetch** from the agent environment,
  including via the Wayback Machine. It is the most rigorous painter-facing modern colour
  source. Anything attributed to Briggs in report 01 is from search-index excerpts and
  needs re-checking in a browser.
- **Report 02 deliberately does not cite** a supposed 2019 *Journal of Cultural Heritage*
  study of Rembrandt luminosity histograms that surfaced in a search summary and could not
  be corroborated. Do not reintroduce it.

Each report ends with its own verification-debt list. The one worth clearing first is
Mather 2014, since it currently carries the lead recommendation and was reached only
through an abstract — SAGE, Brill, MDPI and Nature all returned 403s.

## Scope decided, 2026-07-27

The design is `docs/superpowers/specs/2026-07-27-style-aware-conversion-design.md`. Note
that path is **gitignored** (`.gitignore:6` is `/docs/*`, excepting only `/docs/research`),
so the roadmap below is duplicated here deliberately — this file is the tracked copy.

**v1:** five styles — Realism, Tonalism, Post-Impressionism, Fauvism, Abstract — driven by
a slotted pipeline (pre-map spatial → Lab remap → candidate transform → quantiser →
post-map selection). A style is a data row naming which stage fills each slot; stages never
learn which style invoked them. Mark size is a user slider defaulting to
`min(width, height) / 150`, and a second invariant is added: every output region must be a
mark a human could execute.

### Planned, in rough order

| Item | Unlocks | Blocked on |
|---|---|---|
| Broken colour at **mark scale** — not pixel dithering, which no painter can execute | Impressionism, Pointillism, Divisionism | The ~30-line gamut measurement in step 1 above |
| Key-line rendering | Cloisonnism, Ukiyo-e | Research on line weight, colour and placement — no track covered it |
| Focal point, spatially varying treatment | Edge and mark hierarchy together | Extending the colour cache key by ~3 bits of radial band (~8 MB) |
| Aerial perspective | Landscape depth | Two-handle gradient. **Not** neural monocular depth |
| Anisotropic Kuwahara + structure tensor | Directional flattening, then stroke orientation and flow-based DoG | ~60 lines of shared infrastructure |
| User-defined styles from disk | User extensibility | Nothing — the architecture allows it |
| Debounced live preview | Usability with a dynamic slider panel | A downsampled preview path; a full convert is too slow per tick |
| Converter/matcher metric divergence | Image and tooltip agreeing | A decision, not work. See the inherited-problems note below |

**Rejected rather than deferred** — see "What not to build" above for the evidence: impasto,
automated quality scoring, golden ratio and dynamic symmetry, rule-of-thirds scoring,
automatic focal-point detection as load-bearing, complementary harmony as the default.
