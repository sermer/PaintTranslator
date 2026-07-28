# Fauvism, track 4: flatness and space

Research into what Fauvist flatness actually is, and what of it this pipeline can build.
Written 2026-07-28. Claims are marked `[verified]` (primary source read directly, or
measured locally), `[relayed]` (secondary source only) or `[inferred]` (my own reasoning).

**Three findings, in order of how much they should change the product.**

1. **Fauvist flatness is not value compression, and the claim in the brief does not
   survive measurement.** Over 14 Fauve works, 12 Impressionist/Post-Impressionist works
   and 7 photographs, mean L\* standard deviation is Fauve **20.48**, Impressionist
   **20.65**, photograph **23.60**. Fauvism's L\* structure is indistinguishable from
   Impressionism's. What separates it is the *chromatic share of local contrast*: local
   ΔC\*/ΔL\* is **0.80** for Fauve against **0.49** Impressionist and **0.37**
   photographic — a 2.2× increase over photographs, the cleanest separation in the whole
   measurement set. `[verified — measured locally, see Method]`

2. **No operation in slots 2–4 can produce flatness in the sense that matters, as a
   matter of form rather than of tuning.** A position-blind operator's regions are the
   level sets of the image, whose boundaries lie wherever a threshold happened to fall.
   A painted plane's boundary is chosen. Measured: the area-weighted isoperimetric
   quotient of the app's large output regions is **0.06–0.11** across the entire
   parameter space, against ~0.62 for a digital disc and ~0.79 for a square. Nothing —
   floor strength 1 to 5, chroma 1.0 to 2.2, contrast 0.95 to 1.35 — moves it.
   `[verified — measured locally]`

3. **Fauvism as shipped is the only style setting measured that makes a photograph *less*
   paintable than doing nothing.** Mean share of pixels in regions below one mark², over
   four photographs at mark 10: Realism **69.3%**, shipped Fauvism **74.7%**. Raising the
   floor from 1.0 to 3.0 with nothing else changed takes it to **62.1%**.
   `[verified — measured locally]`

This report extends [`../02-styles-and-movements.md`](../02-styles-and-movements.md) and
[`../04-appeal-and-perception.md`](../04-appeal-and-perception.md), and it **contradicts
one finding in [`../abstract/README.md`](../abstract/README.md)** — see "Contradictions"
below.

---

## 1. What Fauvist flatness actually is

**Conclusion: of the five mechanisms named in the brief, only two are available to this
pipeline at all, and the movement itself only strongly exhibits one of the five.**

The brief lists broad unmodulated colour, suppressed value modelling, assertive contour,
perspective violation, and loss of atmospheric perspective. Ranked by contribution and by
whether the app can express them:

| Mechanism | Does Fauvism depend on it? | Can this pipeline do it? |
|---|---|---|
| **Broad areas of unmodulated colour** | Yes — this is the mechanism | Only in slot 1 or 5, and only with region computation that does not exist |
| **Suppressed value modelling within objects** | **Weakly. Not supported by measurement** (§3) | Partly — the guided filter already suppresses *local* modulation |
| **Contour asserting edge over volume** | Yes, but as a *consequence* of flattening, not a cause (§5) | Needs regions first |
| **Deliberate perspective violation** | No — Fauve landscapes keep horizon, overlap and scale | No, and it should not try (§4) |
| **Loss of atmospheric perspective** | Yes, but it is the weakest depth cue there is (§4) | Yes, cheaply — and it buys almost nothing |

The art-historical consensus is that the Fauves "rejected traditional three-dimensional
space and instead used flat areas or patches of colour to create a new pictorial space",
maintaining "an even intensity of colour across the composition, emphasising the flat
surface of the canvas instead of using chiaroscuro to model form" `[relayed — Tate,
"Fauvism" glossary; Humanities LibreTexts 10.3]`. Elderfield's MoMA catalogue is the
standard survey `[relayed — I could not obtain the catalogue text; the MoMA PDF exceeded
the fetch size limit]`. Matisse's own statement of intent in *Notes d'un peintre* (1908) is
about expression and arrangement, and says nothing about value: "the entire arrangement of
the picture is expressive… everything that is not useful in the picture is harmful"
`[relayed — La Grande Revue, December 1908, via secondary quotation]`.

The ranking above is mine, and rests on §3 (measurement) and §4 (perception), not on the
art-historical sources, which do not distinguish the mechanisms quantitatively.

### The perceptual mechanism that does hold up

Flatness from colour is a real neurophysiological effect, and it is specifically an
*equiluminance* effect, not a low-contrast effect. Depth from perspective, shape from
shading, figure-ground segregation, stereopsis and motion are all diminished or abolished
when a boundary is defined by chromaticity alone, because those computations are carried
by the luminance channel `[relayed — Livingstone & Hubel 1988, *Science* 240(4853),
740–749; I fetched the PDF but the environment could not render it, so this is a model's
extraction of the primary, not my own reading]`. Livingstone's later book applies this to
painting directly, naming Matisse's *The Red Studio* among works exploiting equiluminant
contrast, and states the artists' division of labour plainly: contrast produces the
shapes, colour is left for expressive rather than descriptive purposes `[relayed —
Livingstone 2002, *Vision and Art: The Biology of Seeing*, via the NCBI *Neurobiology of
Sensation and Reward* chapter and secondary reviews]`.

The reading for this app: **substituting chroma for luminance genuinely flattens, and
substituting luminance for chroma genuinely does not.** Fauvism's shipped `contrast` of
1.35 raises luminance contrast, which is the anti-flattening direction. See §3.

---

## 2. Flatness is not posterisation — and the difference is formal, not a matter of degree

**Conclusion: the distinction is exact, and it rules out slots 2, 3 and 4 for this feature
in principle rather than in practice.**

Posterisation partitions *colour space*. The regions it produces in the image are the
level sets of the image function: their boundaries are the loci where the image crossed a
quantisation threshold. On a gradient those are long, nested, ramified bands. Their shape
is a property of the gradient, not of anything in the scene.

A painted plane partitions the *image plane*. Its boundary is placed where the painter
decided a thing ends, and its interior is constant because the painter chose one colour
for the thing.

The formal statement: **a position-blind operator's output regions are always level sets
of its input.** If `f` maps colour to colour with no access to position, then two pixels
land in the same output region exactly when their input colours are equivalent under `f`,
so every output region boundary is an input iso-surface crossing. Slots 2 (`ILabRemap`),
3 (`ICandidateTransform`) and 4 (a position-independent `IQuantiser`) are all such
operators. **No parameter value in any of them can produce a plane.** `[inferred — from
the interface signatures; this is the same conclusion the Abstract track reached
empirically, given a proof rather than a survey]`

### Measured: how far the current output is from planes

Level sets are thin and ramified; planes are compact. The area-weighted isoperimetric
quotient **Q = 4πA/P²** separates them. Reference values under four-connected discrete
perimeter: a square scores **0.785**, a digital disc **0.617**, a 1-pixel-wide band of
length *n* scores **≈ πn/(n+1)² → 0**.

Q over regions of at least 100 px, on two converted photographs at mark 10:

| Setting (strength / contrast / chroma) | street Q | village Q |
|---|---|---|
| Realism (1.0 / 1.00 / 1.00) | 0.062 | 0.044 |
| **Fauvism as shipped (1.0 / 1.35 / 2.20)** | **0.082** | **0.059** |
| Floor only (3.0 / 1.35 / 2.20) | 0.105 | 0.104 |
| Proposed (3.0 / 0.95 / 1.80) | 0.094 | 0.106 |
| Proposed, floor 5 (5.0 / 0.95 / 1.80) | 0.103 | 0.106 |
| Floor 5, no remap (5.0 / 1.00 / 1.00) | 0.097 | 0.084 |

`[verified — measured locally]`

**Every setting sits at Q ≈ 0.04–0.11, six to eight times less compact than a square, and
the whole parameter space spans a factor of 2.4 while the gap to a plane is a factor of
8.** The output is made of level-set bands and it stays made of level-set bands. This is
the numeric form of the Abstract track's "no stage in the pipeline computes a region", and
I recommend Q(≥ mark²) as the acceptance test for any future flat-plane stage: **a stage
that produces planes should raise Q above 0.35; anything that leaves it under 0.15 has not
produced planes whatever it looks like at thumbnail size.** `[inferred — the threshold is
mine; 0.35 is roughly halfway between the current output and a digital disc]`

---

## 3. Value structure — the brief's specific test, and it fails

**Conclusion: "Fauvism compresses its L\* range and expands hue variation" is not
supported. Fauvism's L\* range is Impressionism's. What it expands is chroma, and the
statistic that separates the movement is a ratio, not a range.**

I measured 14 Fauve works (13 Derain 1905–07, 3 Matisse 1905, one of which — the Collioure
harbour picture — I initially mis-filed as a photograph and moved after inspecting it),
12 Impressionist and Post-Impressionist works (Monet ×3, Pissarro, Sisley, Renoir,
Cézanne, van Gogh, Gauguin, Seurat, Corot, Courbet), and 7 landscape/street photographs.
All from Wikimedia Commons at ~700 px. Method and caveats in §7.

| | N | L\*mean | **L\*sd** | L\*range (p5–p95) | **C\*mean** | C\*p95 | Hue entropy (bits) | **local ΔC\*/ΔL\*** | **C\*mean / L\*sd** |
|---|---|---|---|---|---|---|---|---|---|
| **Fauve** | 14 | 55.0 | **20.48** | 66.2 | **28.3** | 60.0 | 3.51 | **0.796** | **1.401** |
| Impressionist / Post-Imp. | 12 | 50.3 | 20.65 | 64.1 | 15.7 | 35.0 | 3.61 | 0.492 | 0.833 |
| Photograph | 7 | 51.1 | 23.60 | 75.8 | 16.9 | 33.9 | 3.07 | 0.365 | 0.724 |

`[verified — measured locally]`

Read the columns:

- **L\*sd: Fauve 20.48, Impressionist 20.65.** A 0.8% difference. Whatever Fauvism does,
  it does not compress value relative to the movement it grew out of. Against photographs
  the compression is real but modest — ×0.868 in SD, ×0.873 in p5–p95 range.
- **Hue entropy: Fauve 3.51, Impressionist 3.61.** Fauvism does not use *more* hues than
  Impressionism; it uses them at higher chroma. Against photographs it is +14%, and that
  comes free with a chroma boost, because raising chroma spreads pixels across more hue
  bins. **No hue-rotation or hue-spreading stage is warranted.**
- **C\*mean ×1.67 and C\*p95 ×1.77 over photographs.** This is the large effect.
- **Local ΔC\*/ΔL\* — the chromatic share of mark-scale contrast — Fauve 0.796 against
  0.365 for photographs, ×2.18.** This is the single cleanest separator, and it is
  precisely the equiluminance mechanism of §1: Fauve paintings carry more of their local
  contrast in the chromatic channel, which is the channel that does not compute depth.
- **C\*mean / L\*sd: ×1.94 over photographs.** The same story in global terms.

So the flattening claim is true in a *ratio* sense and false in a *range* sense. Fauvism
does not darken its lights or lighten its darks; it loads the chromatic channel until
luminance stops being the dominant carrier of contrast.

### What that means for `ToneAndChromaRemap`

`ToneAndChromaRemap.Map` computes `L' = clamp(50 + (L − 50)·contrast + key)` and then
scales chroma through a gain that blends a plain multiplier at gain 1.0 into a tanh knee
against `RenderContext.AchievableMaxChroma` at the parameter's maximum of 3.0. So
`contrast` multiplies L\* standard deviation directly, and `key` shifts the mean.

**`contrast` 1.35 is wrong by sign.** The measured target is ×0.87 of the source's L\*sd;
1.35 delivers ×1.35. Measured on four photographs, the shipped setting overshoots L\*sd by
+14% to +41% against target on every one.

But the correction is *not* simply "set contrast to 0.87", because the guided filter
already compresses L\* by removing texture, and the amount depends on `strength`:

| Floor strength (contrast 1.0) | rendered L\*sd ÷ source L\*sd, mean of 4 photos |
|---|---|
| 1.0 | 0.928 |
| 5.0 | 0.816 |

`[verified — measured locally]`

Target is 0.868. **At strength 3–4 the floor alone lands very close to the measured Fauve
value compression; at strength 5 it slightly overshoots.** The honest recommendation is
therefore *`contrast` ≈ 0.95, and let the floor do the work* — and the more important
point is the framing: **value compression in this pipeline should come from the
edge-preserving floor, which removes modelling while keeping large value differences,
not from the contrast knob, which squashes the whole histogram the way Tonalism does.**
Those are two different kinds of flatness and only the first is Fauvist.

For chroma, the measured targets are C\*mean ×1.67 and C\*p95 ×1.77. With the tanh knee,
a nominal gain of **1.8** delivers ≈ ×1.74 on a source at C\*mean ≈ 17 against a ceiling
near 60 — on target. The shipped 2.2 delivers ≈ ×2.0, over target, and buys it in the hues
where the palette has headroom (yellow, orange, red) while doing much less in blue and
nothing in green, per the Abstract track's per-hue ceiling correction. `[inferred — the
effective-gain arithmetic is mine, from the code; the per-hue point is relayed from
`../abstract/README.md` correction 1]`

---

## 4. Depth cues: what survives flattening, and why Fauvism stays legible

**Conclusion: four of the five pictorial depth cues are geometric and therefore
untouchable by anything this pipeline does to colour. That is why colour flattening never
destroys legibility — and also why colour flattening can only ever remove modelling, never
space.**

Cutting & Vishton (1995) assess nine depth sources and rank them by the area under their
ordinal depth-threshold functions within personal, action and vista space. `[verified — I
read the full chapter text]`

Their art-historical ranking, derived by surveying which sources appear when only *n* are
used at all, is: **1 occlusion, 2 height in the visual field, 3 relative size, 4 relative
density, 5 aerial perspective.** The rank-order correlation between that hierarchy and
the ranking derived from their psychophysical threshold functions in action space is
**rs = 1.00, p < 0.01**, and **rs = 0.87** in vista space. `[verified]`

Occlusion sits first in all three spaces and is the only cue used alone: it is the only
depth information in the Lascaux, Niaux and Altamira cave paintings and in Egyptian art of
2500 BC. `[verified]`

Two consequences for this brief.

**Shading is not a depth cue at all.** Cutting & Vishton exclude it explicitly: "Brightness,
light, and shading. Some conflation of these terms has appeared on many lists of sources of
information about layout … but we find their inclusion anomalous (as did Gibson, 1950)."
They argue luminance in such demonstrations acts as a surrogate for relative size, and,
following Cavanagh & Leclerc, that "it is better to consider shadows as information about
object *shape*, not depth per se." `[verified — direct quotation from the chapter; the
citation appears there as Cavanagh & Leclerc 1990, and the paper is Cavanagh, P. &
Leclerc, Y. G., "Shape from shadows", *Journal of Experimental Psychology: Human
Perception and Performance* 15(1), 3–27, 1989 — the year discrepancy is unresolved]`

So suppressing value modelling removes *shape from shading* — the reading of volume within
an object — and removes nothing from the reading of layout. **This is the precise
explanation of why a Fauve painting looks flat and stays legible.** Occlusion, height and
relative size are properties of where things are in the image, and no pointwise colour
function and no edge-preserving filter can touch them. `[inferred — combining the above
with the pipeline's structure]`

**Aerial perspective is the weakest pictorial cue and it is the only one that is a
colour gradient.** It ranks fifth of five in art, is the only source whose effectiveness
*increases* with the logarithm of distance, and in practice "is likely to become effective
only with great distance" — beyond roughly 30 m, in vista space. Cutting & Vishton also
note it "allows only ordinal comparisons; we know of no data on the topic." `[verified]`

The brief asked whether aerial perspective is cheaply invertible. It is — a two-handle
chroma-and-value gradient, as the parent README already proposes — but the return is the
lowest of any depth cue in the list, and it applies only to landscapes with a genuine
distant plane. **Not a Fauvism recommendation.** It is a landscape feature that happens to
have a Fauvist reading.

Summary table for the brief's question:

| Cue | Carried into the output by the photo? | Can a pipeline stage suppress it? | Worth suppressing? |
|---|---|---|---|
| Occlusion | Yes, for free, exactly | No — geometric | No; it is what keeps the picture legible |
| Height in visual field | Yes, for free, exactly | No — geometric | No |
| Relative size | Yes, for free, exactly | No — geometric | No |
| Relative density (texture gradient) | Yes | Partly — the guided filter erases fine texture, which *does* attenuate this | Incidentally, already happening |
| Aerial perspective | Yes | Yes, slot 1, two-handle gradient | Lowest payoff of the five |
| Shading (not a layout cue) | Yes | Yes — floor strength already does it | **Yes. This is the one.** |

---

## 5. Contour: it belongs to track 2, with one correction

**Conclusion: outlining is not a source of flatness. It is what makes flatness legible,
which is a different claim, and it is downstream of region computation either way.**

The tempting argument is that if flat planes are held together by drawn contours, then
outlining is a flatness feature. The perception literature says the opposite. A dark
contour line is a *luminance* edge, and luminance is the channel that carries form and
boundary assignment (§1). Adding contour therefore *adds* form information; it does not
remove it. What it does is let you remove the interior modelling without the object
dissolving — which is exactly the Gauguin/cloisonnist device the Fauves inherited.

There is one flatness-relevant fact worth passing on. Cutting & Vishton note that in Roman
art, Chinese landscape and much Romanesque work, "artists often used a dimming and
fuzziness of contour of shapes in the distance which can be said to mimic aerial
perspective." `[verified]` So a *uniform, hard* contour across the whole picture removes an
aerial-perspective surrogate, and that is a genuine, if small, flattening contribution.

Practical reading: **contour belongs to track 2 to specify and build.** My only constraint
on it is sequencing — a contour needs a region boundary to trace, so it lands after
whatever computes regions, and it cannot be built as an independent stage first.

---

## 6. Physical execution

**Conclusion: flattening pays off twice, and the shipped Fauvism collects neither
dividend. Floor strength is the dominant executability lever; the remap is nearly
irrelevant to it.**

`PaintabilityMetrics.FractionInRegionsSmallerThan` on four photographs, resized to 768 px
wide, mark 10, six-paint fixture palette. Figures are the share of pixels in
four-connected same-colour regions smaller than one mark²:

| Setting (strength / contrast / chroma) | village | coast | street | hills | **mean** |
|---|---|---|---|---|---|
| A Realism (1.0 / 1.00 / 1.00) | 50.6 | 65.7 | 73.3 | 87.8 | **69.3** |
| **B Fauvism as shipped (1.0 / 1.35 / 2.20)** | 61.2 | 63.5 | 81.0 | 92.9 | **74.7** |
| C Floor only (3.0 / 1.35 / 2.20) | 46.0 | 53.9 | 59.3 | 89.4 | **62.1** |
| D Contrast only (1.0 / 0.95 / 2.20) | 64.3 | 65.9 | 79.7 | 90.8 | **75.2** |
| E Chroma only (1.0 / 1.35 / 1.80) | 57.7 | 64.1 | 79.9 | 92.2 | **73.5** |
| F Proposed (3.0 / 0.95 / 1.80) | 46.2 | 54.8 | 56.6 | 84.6 | **60.6** |
| G Proposed, floor 4 (4.0 / 0.95 / 1.80) | 40.9 | 49.3 | 50.1 | 79.4 | **54.9** |
| H Proposed, floor 5 (5.0 / 0.95 / 1.80) | 36.7 | 42.7 | 45.8 | 68.6 | **48.4** |
| I Floor 5, no remap (5.0 / 1.00 / 1.00) | 29.1 | 38.9 | 37.6 | 49.6 | **38.8** |

`[verified — measured locally]`

Readings:

- **Shipped Fauvism is worse than Realism** (74.7 vs 69.3), on three of four photographs.
  It is the only setting tested with that property.
- **Floor strength dominates.** B → C, floor 1 → 3 with the remap untouched: −12.5 points.
  B → floor 5 would be larger still.
- **The remap parameters are nearly irrelevant to paintability.** Contrast alone (D) is
  +0.5 points; chroma 2.2 → 1.8 alone (E) is −1.2 points. The stylistic case for changing
  them (§3) is strong; the executability case is not.
- **The chroma boost has an irreducible cost.** I → H, adding the Fauvist remap on top of
  floor 5: +9.6 points. That is the price of the style, and it is worth paying — but at
  chroma 2.2 you pay more for a chroma the palette cannot reach in half the hue circle.

One caveat that matters and that I got wrong first. **On the committed golden gradient,
raising floor strength barely helps** — 21.1% → 18.8% going from strength 1 to 5. On real
photographs it is the biggest lever there is. The gradient is smooth by construction, so
the guided filter has almost nothing to remove; photographs have texture, which is what the
floor exists for. **Any conclusion about floor strength drawn only from `Tests/Golden` is
unsafe.** See Contradictions.

### Does flattening improve executability, and by how much?

Yes, and the number is: going from the shipped Fauvism to floor 4 with the corrected remap
takes the unpaintable share from 74.7% to 54.9%, i.e. it moves roughly **a fifth of the
canvas** out of sub-brushmark speckle. That is real but it is not sufficient — at 54.9%,
over half the picture still cannot be executed at the declared mark. **Parameter tuning
cannot deliver a paintable Fauvism. Region merging can**, which is pick 3.

---

## 7. Three concrete changes, in priority order

### Pick 1 — Raise Fauvism's floor strength from 1.0 to 3.0

**Slot 1. One line** (`(fauvismFloor, "strength", 3.0)` in `StyleRegistry`, plus hoisting
the floor into a named local the way Tonalism's is, so ~3 lines including the local).

**Evidence.** −12.5 points of unpaintable share, mean of four photographs, with nothing
else changed (row B → row C above). Fauvism currently runs the weakest floor in the app
while declaring the second-largest mark scale (1.3), which is the exact combination
`EdgePreservingFloor`'s own doc comment warns about. Post-Impressionism at 3.0 and Abstract
at 5.0 both already demonstrate the value is safe.

**Why 3.0 and not 5.0.** 5.0 is better on paintability (48.4 vs 60.6 with the corrected
remap) but over-compresses L\*: at strength 5 the floor alone delivers ×0.816 of source
L\*sd against a measured Fauve target of ×0.868, before the contrast knob does anything.
3.0 lands on target and leaves headroom for the user's slider. If the product prefers
paintability over fidelity, take 4.0 and raise `contrast` to 1.0 to compensate.

**Verification.** Extend `StyleBehaviourTests.EveryRegisteredStyleIsPaintable` with a
per-style bound rather than a shared one, and assert Fauvism's fragmentation is no worse
than Realism's on the same source — a property it currently violates. Regenerate
`Tests/Golden/Fauvism.png` and look at it.

### Pick 2 — Correct the remap: `contrast` 1.35 → 0.95, `chroma` 2.2 → 1.8

**Slot 2. Two lines** in `StyleRegistry.WithDefaults`, plus a doc-comment rewrite, since
the existing comment argues for the numbers being changed.

**Evidence.** §3. Measured Fauve L\*sd is ×0.868 of photographic, not ×1.35; the shipped
contrast overshoots on every photograph tested by 14–41%. Measured Fauve C\*mean is ×1.67
and C\*p95 ×1.77, which nominal gain 1.8 delivers (≈ ×1.74 effective through the tanh
knee); 2.2 delivers ≈ ×2.0 and spends the excess in the three hue sectors where the palette
has headroom. Against measured Fauve targets, the proposed settings are the better fit on
9 of 12 comparisons (L\*sd, C\*mean and C\*mean/L\*sd across four photographs).

**Note this is nearly free on paintability** (rows D and E) — it is a fidelity change, and
should be argued as one. The one thing it definitely fixes is the direction of the value
move, which is currently backwards relative to both the movement and the perceptual
mechanism.

**Verification.** A test asserting that Fauvism's rendered L\* standard deviation is
*below* the source's and its C\*mean is 1.5–1.9× the source's, on the noisy-gradient
fixture. That pins the intent — flatter in value, louder in chroma — rather than the
numbers, so a future retune cannot silently reverse the sign again.

### Pick 3 — Small-region merge in slot 5, at `MarkPixels²`

**Slot 5. ~100 lines**, of which ~60 is the connected-component labelling three separate
recommendations in `../abstract/README.md` already need, and which
`PaintabilityMetrics.ForEachRegion` is most of.

**Evidence.** After picks 1 and 2, 54.9% of pixels still sit in sub-mark regions. No
setting of the existing five stages gets below ~39% (row I, and that one has no style left
in it). The isoperimetric measurement in §2 says the same thing from the shape side: Q
stays at 0.04–0.11 across the whole parameter space. **This is the only pick that attacks
flatness rather than loudness**, and it is invariant-safe for free, because
`IPostMapStage.Refine` takes and returns indices and cannot name a colour outside the
candidate set.

Merging every region below mark² into its largest neighbour is the minimal version. It
raises median region area by construction, and it makes `MarkPixels` a guarantee instead of
the hope the Abstract track identified.

**Verification.** Two assertions: `FractionInRegionsSmallerThan(…, mark²)` is exactly 0
after the stage runs, and the isoperimetric quotient Q over regions ≥ mark² rises. Q is the
one that tells you whether you got planes or just bigger blobs; expect a modest rise from
merging alone (it removes thin fragments but does not straighten boundaries), and treat
Q > 0.35 as the bar for a later contour-tracing stage.

---

## What not to build

- **A separate hue-spreading or hue-rotation stage for Fauvism.** Measured hue entropy is
  Fauve 3.51 against Impressionist 3.61 — Fauvism uses *fewer* effective hues than the
  movement before it, at higher chroma. The +14% over photographs comes free with the
  chroma gain. `[verified — measured locally]`
- **A contrast knob below ~0.9 as the flattening mechanism.** That compresses the whole
  histogram, which is Tonalism's move and produces greyness, not flatness. Measured Fauve
  L\*range is 66.2 against Impressionist 64.1 — Fauvism keeps a full value range. The
  compression that is Fauvist is *local* (modelling), and the guided filter already does
  that kind. `[verified — measured locally]`
- **Aerial-perspective inversion as a Fauvism feature.** Fifth of five pictorial cues,
  effective only beyond ~30 m, ordinal only, and Cutting & Vishton report no data on its
  metric use. Build it as a landscape feature if at all. `[verified]`
- **Any attempt to violate perspective, flatten geometry, or suppress occlusion.**
  Occlusion is the top-ranked cue in all three spaces and the sole cue in the oldest
  surviving art. Fauve paintings do not violate it. And the pipeline cannot express it
  anyway without a scene model. `[verified]`
- **Posterisation or candidate-count reduction sold as "flat planes".** §2 — a
  position-blind operator produces level sets by construction. Candidate thinning is still
  worth doing (the Abstract track's pick 2, and it makes conversion faster), but it should
  be justified as *fewer colours*, not as *planes*, or the next reviewer will measure Q and
  find nothing moved. `[inferred + verified by measurement]`
- **Isoluminant rendering as a flatness maximiser.** The equiluminance literature is about
  boundaries defined by chroma alone; driving a whole picture there produces the unstable,
  shimmering percept Livingstone describes, which is a specific and mostly unpleasant
  effect, not Fauvism. Fauve paintings retain ΔL\* — their local ΔC\*/ΔL\* is 0.80, not
  ∞. `[inferred, from the measurement plus the relayed equiluminance literature]`
- **Neural monocular depth to drive any of the above.** Already rejected by the parent
  README; §4 adds that the cue it would serve ranks last of five.

---

## Contradictions with earlier reports

**1. `../abstract/README.md` says "raising `strength` further has never helped: the floor
is not the problem." That is true on the golden gradient and false on photographs.** On
`Tests/Golden`'s synthetic source, strength 1 → 5 moves Fauvism's unpaintable share only
21.1% → 18.8%. On four real photographs it is the dominant lever, worth 12–24 points. The
gradient is smooth by construction, so the guided filter has nothing to remove. The Abstract
track's *conclusion* for Abstract may still stand — its defect was diagnosed as chroma
spraying transitions, which I confirm — but the general claim about floor strength should
not be carried forward. `[verified — measured locally]`

**2. The parent README's "Fauvist ×2 is simply unreachable in blues and greens" is right,
and the measurement says ×2 is also wrong as a target.** Real Fauve works sit at C\*mean
×1.67 over photographs, not ×2. The palette limit and the historical evidence point the
same way for once. `[verified — measured locally]`

**3. Neither parent report is contradicted on value.** `../01-colour-theory-in-practice.md`
and `../02-styles-and-movements.md` do not put a number on Fauvist value range; this report
supplies one and it is "the same as Impressionism".

---

## Method

Everything marked "measured locally" was produced on 2026-07-28 in a throwaway copy of the
worktree under the session scratchpad. **No file in the repository was modified.** The
probe test was written into the copy, not into `Tests/`.

- **Corpus.** 33 images from Wikimedia Commons at ~700 px: 14 Fauve (Derain 1905–07 ×11,
  Matisse 1905 ×3), 12 Impressionist/Post-Impressionist, 7 photographs. sRGB → CIELAB
  (D65), whole-image statistics. "Local ΔC\*/ΔL\*" is the mean absolute CIELAB C\* and L\*
  difference to the pixel *r* away horizontally and vertically, with *r* = short side ÷ 60,
  i.e. roughly one brushmark; the ratio of the two.
- **Pipeline probe.** `StylePipeline.Render` with `StyleTestFixtures.SixPaints()`, four
  photographs resized to 768 px wide, `markPixels` 10, styles constructed directly rather
  than read from `StyleRegistry`, so the registry was never touched.
- **Region metrics.** `PaintabilityMetrics` for counts and fragmentation; a separate
  four-connected flood fill for the isoperimetric quotient, using the same alpha-masked
  colour key.

### Caveats on the corpus, in order of how much they could move a conclusion

1. **Reproduction fidelity.** These are web scans with unknown colour management. The
   parent research already flags this against Rothko. The Fauve-vs-Impressionist comparison
   is the safest, since both groups come from the same source population; the
   painting-vs-photograph comparison is the weakest, since modern digital photographs and
   scanned paintings differ systematically in tone curve. **The chroma result (×1.67) is
   large enough to survive plausible reproduction error; the value result (×0.868) is
   not, and should be treated as "not an expansion" rather than as a precise figure.**
2. **The Fauve set is Derain-heavy** (11 of 14) and 1905–07 only. Matisse's later flat
   period is not represented, and would likely push L\*sd down and C\* up.
3. **N is small** (14 / 12 / 7) and I report means without dispersion or significance
   tests. The ΔC\*/ΔL\* separation is large relative to the within-group spread
   (Fauve range 0.43–1.16, photographs 0.19–0.56) but they overlap.
4. **The photograph set is opportunistic** — seven Commons search results, not a sampled
   corpus, and one search result turned out to be a Derain painting, which I caught by
   inspection and moved. Others may be mis-typed in ways I did not catch.
5. `Matisse — The Green Stripe` is an outlier on every metric (L\*sd 21.5 but local
   ΔC\*/ΔL\* 0.43, hue entropy 1.45) because it is a tightly cropped head against a
   three-colour ground. Removing it raises the Fauve ΔC\*/ΔL\* mean.

---

## Verification debt

Ranked by how much clearing each would change a decision.

1. **A properly sampled painting corpus with known colour management.** Everything in §3
   rests on 33 web reproductions I chose. This is the single largest risk to picks 1 and 2,
   and the cheapest fix is a larger, source-controlled set (WikiArt with provenance, or a
   museum's own colour-managed downloads).
2. **Livingstone & Hubel 1988 and Livingstone 2002.** Both are `[relayed]`. They carry the
   perceptual mechanism in §1 and the "contour adds form" argument in §5. The 1988 PDF
   downloaded but the environment could not render it; the book is not online. This is the
   most important unverified *reasoning* in the report.
3. **Elderfield 1976, *The "Wild Beasts"*.** The standard survey. The MoMA catalogue PDF
   exceeded the fetch size limit. It is the source most likely to correct or refine the
   mechanism ranking in §1, which is currently my inference over glossary-level secondary
   sources.
4. **Whether the ΔC\*/ΔL\* separation holds against Post-Impressionism specifically.** My
   control group mixes Impressionist and Post-Impressionist work; Gauguin (0.805) and
   Seurat (0.633) score close to the Fauve mean while Corot (0.375) and Courbet (0.258) do
   not. A cleaner Post-Impressionist control might show the Fauve/Post-Impressionist gap is
   small, which would weaken the case that this statistic is *Fauvist* rather than
   *twentieth-century*.
5. **Cavanagh & Leclerc year.** Cited as 1990 by Cutting & Vishton; indexed as 1989, *JEPHPP*
   15(1), 3–27. Trivial, but the paper carries the "shadows are shape, not depth" claim that
   §4 leans on, and I have not read it.
6. **Graham & Field 2008, *Perception* 37, 1341–52** — located but not read. It measures
   intensity statistics by content class and could corroborate or contradict the L\* result
   in §3 directly.
7. **Q > 0.35 as the plane threshold** is my invention and has no empirical backing. It is
   a proposed acceptance test, not a measured fact. Calibrating it against a hand-segmented
   painting would make pick 3 checkable rather than merely measurable.
