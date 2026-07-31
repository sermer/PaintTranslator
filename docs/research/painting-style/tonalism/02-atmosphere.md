# Atmosphere in Tonalism

**Date:** 2026-07-31
**Track:** 2 of 4, Tonalism.
**Question:** is atmosphere a buildable operation in this pipeline, and if so which one?

**Relationship to prior research.** [post-impressionism/04-backgrounds.md §6](../post-impressionism/04-backgrounds.md)
rejected any aerial-perspective stage on the grounds that the source photograph already
carries +51 L\* / −18 C\* on real landscapes and carries it *backwards* on three of seven.
This report re-tests that on a landscape-heavy corpus and on Tonalist canvases. **Half of
it holds and half of it is wrong**, and the half that is wrong is the half everybody
builds — see §2. The parent [README](../README.md) rejects neural monocular depth and
points at a two-handle gradient instead; that ruling stands and is strengthened (§2.4).
[03-brushwork-and-edges.md §3.9](../03-brushwork-and-edges.md) proposed a Kubelka-Munk
glaze pass as build item 7; §4 here is the first measurement of what it would actually
buy, and the answer is larger than that report guessed but for a different reason.

**Claim marking** (enforced across `docs/research/`):

- `[verified]` — I read a primary or reputable source directly in this session, or it is
  arithmetic I performed on data in this repository, or a measurement I made by calling
  the real code.
- `[relayed]` — a secondary source or a search summary asserts it and I could not confirm
  it against the primary.
- `[inferred]` — my own reasoning from stated premises.

**Method note.** Every pipeline figure was produced by *calling* the shipped stages through
`StylePipeline.Render`, from a throwaway console project whose assembly name is
`PaintTranslator.Tests` (so the app's `InternalsVisibleTo` grant applies), referencing
`PaintTranslator.csproj`. Nothing was transcribed. The probe lived in the scratchpad and is
not in the repository. Two pieces of arithmetic are genuinely new rather than borrowed —
the finite-thickness Kubelka-Munk layer (§4.1) and a re-enumeration of `MixtureBuilder`'s
sampling grid (§4.4) — and **each is gated against the shipped code before any result is
taken from it**: the layer reproduces `KubelkaMunk.Mix` to ΔE 0.00000 at large thickness on
all seven paints, and the re-enumeration reproduces `MixtureBuilder.Build()` exactly
(4,888 distinct colours, zero missing, zero extra). The palette throughout is the same
seven `Selectable` paints the Post-Impressionism round used — Titanium White, Hansa Yellow
Opaque, C.P. Cadmium Orange, Pyrrole Red, Cobalt Blue, Phthalo Green (Y.S.), Bone Black —
giving **4,888 candidates**, so figures are comparable across rounds. Corpus and provenance
in §9.

---

## The answer, first

**Yes. Atmosphere is buildable, it is one operation and not six, and the shipped Tonalism
row currently does the opposite of it.** Six results, in descending confidence:

1. **Atmosphere decomposes into exactly two measurable axes, not seven, and only one of
   them survives contact with real images.** Local contrast and edge softness are the same
   measurement — Pearson **r = 0.973** (range 0.956–0.989) across 21,340 blocks of nine
   photographs. Chroma-with-distance and contrast-with-distance *are* separable
   (r = −0.036) but chroma-with-distance points the **wrong way on 6 of 8** landscapes and
   **4 of 6** Tonalist canvases. What is left is **lightness with distance**, present in
   the textbook direction on **8 of 8** landscape photographs (far − near ΔL\* **+23.0 to
   +64.7**, median **+45.2**) and **5 of 6** canvases (+4.7 to +60.0). `[verified]` §1, §2.
2. **The shipped Tonalism row is an atmosphere *destroyer*.** It attenuates the far−near
   lightness separation the photograph already carried by a factor of **0.41** (median
   45.2 → 19.7), the chroma separation by **0.50**, and it annihilates the contrast
   separation outright — the sign flips or collapses below ±0.5 on 5 of 8 photographs.
   Nothing in the row is spatial; the compression is uniform, so *depth is compressed
   exactly as hard as everything else*. `[verified]` §3.
3. **The row also over-compresses relative to the paintings it is named after, by about
   2×.** Seven Tonalist canvases give a median L\* 5–95 range of **51.5** and a median mean
   C\*ab of **23.3**. The shipped row delivers **32.0** and **5.3** on the same nine
   photographs. Contrast **0.55 → ≈0.80** with the mother colour at **0.00** lands the
   range at 47–57 and restores the far−near separation to **+30.6 to +37.2**, against the
   canvases' median +40.9. This is a two-number change in `StyleRegistry.cs`. `[verified]` §3.3.
4. **The atmosphere operation is a slot-1 two-handle ramp, it works, and it is free.**
   Written as a real `IPreMapStage` and run through the real pipeline, an atmospheric veil
   ramped by row recovers a third to a half of the lost depth separation (schnebelhorn
   18.1 → 25.6; rothesay 10.9 → 21.4; swaledale 10.6 → 20.2), pushes far chroma toward
   zero, and — unexpectedly — **cuts the unpaintable share on all nine photographs**
   (swaledale 38.9% → 21.4%, millvalley 25.2% → 15.5%, yangshuo 24.1% → 11.1%). Slot 1
   runs before the colour cache is built, so it costs the cache nothing and cannot break
   the invariant. `[verified]` §5.
5. **Glazing is buildable, and the reason to build it is not the one the earlier research
   gives.** A white scumble is the *only* paint in the palette that moves colour in the
   aerial direction (+4.09 L\*, −5.24 C\*, on 99.1% / 86.8% of bases at an 8 ΔE move); every
   other paint fails on one axis or both. But its real value is a **gamut** argument
   specific to Tonalism: the style's own remap pushes **87% of target colours into L\*
   40–80**, which is where the opaque candidate set is sparsest (621 / 466 / 289 / 93
   candidates in the L\* 50–60 / 60–70 / 70–80 / 80–90 bands out of 4,888). Adding a
   five-level white scumble ladder cuts pixel-weighted quantisation error from **2.595 to
   1.209 ΔE**, and at matched candidate count it beats spending the same budget on finer
   opaque sampling by **25%** (1.633 vs 2.045 ΔE at ~13k candidates). `[verified]` §4.
6. **`GroundFill` is not an atmosphere device and a ground is not an atmosphere.** In
   Tonalism it is far less destructive than the Post-Impressionism round found (ΔE 2.1–19.4
   against their 23.4–58.7) — but only because Tonalism's key of +4.0 happens to park the
   image near the constant L\* 58 the stage hard-codes. It changes the paintability metric
   by **0.00 on eight of nine photographs**, and on two of nine it selects the dark
   *foreground* and lifts it 19 ΔE. `[verified]` §6.

**In one line:** fix two numbers in the registry, then add a two-handle veil to slot 1. The
scumble ladder is the good third item and it is the one that needs a new invariant.

---

## Contents

1. [What "atmosphere" decomposes into](#1-what-atmosphere-decomposes-into)
2. [Aerial perspective: does the Post-Impressionism rejection hold?](#2-aerial-perspective-does-the-post-impressionism-rejection-hold)
3. [What the shipped row actually does](#3-what-the-shipped-row-actually-does)
4. [Glazing and scumbling](#4-glazing-and-scumbling)
5. [The atmosphere operation, built and measured](#5-the-atmosphere-operation-built-and-measured)
6. [The toned ground](#6-the-toned-ground)
7. [Recommended build items, by payoff ÷ cost](#7-recommended-build-items-by-payoff--cost)
8. [What not to build](#8-what-not-to-build)
9. [Verification debt](#9-verification-debt)
10. [Corpus provenance](#10-corpus-provenance)

---

## 1. What "atmosphere" decomposes into

The brief names seven candidate decompositions. Measured over 16×16 blocks of the nine
photographs (21,340 blocks), the correlation structure collapses them to three.
`[verified — computed locally 2026-07-31]`

| Photograph | blocks | r(L\*,C\*) | r(L\*,contrast) | r(C\*,contrast) | **r(contrast,edge)** | r(row,L\*) | r(row,C\*) |
|---|---|---|---|---|---|---|---|
| mist-stacpollaidh | 1,040 | 0.863 | −0.184 | −0.093 | **0.976** | −0.789 | −0.457 |
| fog-millvalley | 1,680 | −0.217 | −0.410 | 0.194 | **0.979** | −0.747 | 0.119 |
| haze-kathmandu | 2,700 | 0.280 | −0.543 | −0.016 | **0.976** | −0.807 | −0.174 |
| mist-schnebelhorn | 2,340 | 0.929 | −0.653 | −0.550 | **0.989** | −0.975 | −0.933 |
| fog-yellowstone | 2,700 | 0.597 | −0.306 | −0.030 | **0.974** | −0.426 | 0.028 |
| dusk-rothesay | 2,700 | 0.649 | −0.423 | −0.113 | **0.979** | −0.618 | −0.363 |
| clear-swaledale | 2,400 | −0.180 | −0.573 | 0.464 | **0.956** | −0.788 | 0.550 |
| clear-yangshuo | 1,380 | 0.216 | −0.570 | 0.131 | **0.974** | −0.767 | 0.184 |
| ctrl-portrait | 5,400 | 0.028 | 0.081 | −0.312 | **0.956** | 0.066 | −0.609 |
| **mean** | | **0.352** | **−0.398** | **−0.036** | **0.973** | **−0.650** | **−0.184** |

"Local contrast" is the SD of L\* inside 8×8 blocks; "edge" is mean |∇L\*| per pixel.

**Reading the table:**

- **"Contrast falls with distance" and "edge softness rises with distance" are one
  operation, not two.** r = 0.973 mean, and the lowest single value in the corpus is 0.956.
  A pipeline that implements both implements one thing twice. `[verified]`
- **Chroma and contrast are genuinely independent** (r = −0.036 mean, and the sign is
  inconsistent across photographs). So "chroma falls with distance" and "contrast falls
  with distance" *are* two separate levers — the separability question in the brief has a
  clean answer, and it is the opposite of the contrast/edge answer. `[verified]`
- **Chroma is confounded with lightness** (r = +0.352 mean), which matters because the
  textbook rule asks for chroma and lightness to move in *opposite* directions with
  distance while the images have them moving in the *same* direction. §2.2 removes the
  confound and the rule still fails.
- **A vertical ramp explains lightness moderately (r = −0.650, so r² ≈ 0.42) and chroma
  barely (−0.184).** That is the quantitative case for a two-handle gradient as a
  *lightness*-driven device and against it as a chroma-driven one. `[verified]`

The remaining candidates in the brief resolve as follows `[inferred]` from §2–§4:

| Candidate | Verdict |
|---|---|
| value compression toward a mid key | Real, already shipped, and **overdone** (§3.3). Not the same axis as depth, and currently spending the depth axis to pay for it. |
| chroma falling with distance | **Backwards on 6 of 8 landscapes** in absolute C\* (§2.2). True only in *relative* chroma C\*/L\*, which the pipeline does not operate on. |
| hue shifting cool with distance | The far patch is in the warm arc (h\_ab 20–100°) on **5 of 8** landscapes and 3 of 6 canvases. Fails hardest on exactly Tonalism's subject matter — low sun and twilight (§2.3). |
| contrast falling with distance | Real, 6 of 8 landscapes, 4 of 6 canvases. Same measurement as edge softness. |
| edge softness rising with distance | Same operation. r = 0.973. |
| a luminous veil over the whole field | Buildable two ways, and they are **not** the same operation: as a slot-1 ramp (§5) or as a K-M scumble in the candidate set (§4). `MotherColourTransform` is neither (§4.3). |
| unification of light source | Not measurable from a photograph without semantics. The app's existing answer is the mother colour, which §4.3 shows is a value operation, not a light-source one. Not pursued further here. |

---

## 2. Aerial perspective: does the Post-Impressionism rejection hold?

The Post-Impressionism round split each photograph into a top and bottom quarter and
concluded that a two-handle gradient "will damage portraits and night scenes" because three
of seven photographs carried the vertical gradient backwards. That method conflates *sky*
with *far land*. This report keeps it as a control and adds a second method: a **far patch
and a near patch drawn by eye** on each photograph after looking at it, so the depth
ordering is a human judgement rather than an inferred depth map. Boxes are recorded in the
probe and reproduced in §10.

### 2.1 Lightness: the rejection does not hold

`[verified — computed locally 2026-07-31]`

| Photograph | far L\* | near L\* | **far − near ΔL\*** | far − near ΔC\* | Δ contrast | Δ edge | ΔE |
|---|---|---|---|---|---|---|---|
| mist-stacpollaidh | 58.48 | 5.07 | **+53.41** | +18.43 | +1.29 | +0.58 | 56.56 |
| fog-millvalley | 74.74 | 28.07 | **+46.67** | −8.72 | −9.35 | −17.52 | 49.85 |
| haze-kathmandu | 61.36 | 9.29 | **+52.07** | +2.41 | −3.31 | −5.28 | 52.31 |
| mist-schnebelhorn | 56.44 | 12.67 | **+43.77** | +6.42 | −2.03 | −3.43 | 44.69 |
| fog-yellowstone | 70.64 | 5.99 | **+64.65** | +15.17 | −1.12 | −2.06 | 66.45 |
| dusk-rothesay | 42.43 | 14.25 | **+28.18** | +9.20 | +5.31 | +6.74 | 29.88 |
| clear-swaledale | 54.18 | 31.16 | **+23.01** | −16.45 | −3.13 | −6.59 | 31.91 |
| clear-yangshuo | 62.47 | 25.47 | **+37.01** | +3.33 | −5.55 | −12.40 | 38.13 |
| ctrl-portrait | 29.67 | 25.54 | +4.13 | +28.69 | −3.05 | −4.36 | 50.20 |

**The lightness component of aerial perspective is present, in the textbook direction, on
8 of 8 landscape photographs — including the two "clear" controls with no visible haze.**
Median +45.2. The mechanical top/bottom band control agrees (+28.0 to +59.9 on all eight,
−4.4 on the portrait). The Post-Impressionism round's own two landscapes reproduce here:
swaledale +51.2 by band, yangshuo +40.9 by band. `[verified]`

The Tonalist canvases carry the same thing (§2.5): +4.7 to +60.0, five of six positive.

So **the "double-counting" argument is right about the sign and wrong about the
conclusion.** Yes, the photograph already carries it. But the *converter* then throws two
thirds of it away (§3.2), which means a stage that puts it back is not double-counting; it
is repairing. `[inferred]` from §2.1 and §3.2 together.

### 2.2 Chroma: the rejection holds, and harder than stated

Three normalisations, all on the same patches. `[verified — computed locally 2026-07-31]`

| Photograph | raw ΔC\* | ΔC\* residual after regressing C\* on L\* | far C\*/L\* | near C\*/L\* |
|---|---|---|---|---|
| mist-stacpollaidh | **+18.43** | **+3.65** | 0.428 | 1.306 |
| fog-millvalley | −8.72 | −6.28 | 0.100 | 0.577 |
| haze-kathmandu | **+2.41** | **+0.30** | 0.130 | 0.597 |
| mist-schnebelhorn | **+6.42** | **+0.46** | 0.220 | 0.474 |
| fog-yellowstone | **+15.17** | **+5.37** | 0.252 | 0.441 |
| dusk-rothesay | **+9.20** | **+4.20** | 0.390 | 0.515 |
| clear-swaledale | −16.45 | −13.90 | 0.149 | 0.786 |
| clear-yangshuo | **+3.33** | **+1.01** | 0.309 | 0.628 |

The regression is fitted per image over its own 16×16 blocks, so the residual column is
"chroma relative to what this photograph's own lightness would predict".

**In absolute C\*ab the far patch is *more* chromatic than the near one on 6 of 8
landscapes, and removing the lightness confound does not change that count.** The rule is
recovered only in *relative* chroma C\*/L\*, where the far patch is lower on 8 of 8 — and
that is arithmetic, not atmosphere: the far patch is two to twelve times lighter. `[verified]`

`ToneAndChromaRemap` operates on absolute C\*ab (`ToneAndChromaRemap.cs:63–76`). **A
depth-driven chroma reduction built on that stage would therefore fight the photograph on
6 of 8 landscapes.** `[verified]` against the source. This is the half of the
Post-Impressionism rejection that stands, and it now stands on a landscape-only corpus,
which is the strongest place it could have been tested.

### 2.3 Hue: fails on Tonalism's own subject matter

Far-patch hue h\_ab: 79.6°, 251.1°, 62.8°, 203.7°, 86.6°, 21.9°, 178.4°, 58.8°. **Five of
eight sit in the warm arc (20–100°).** The two that are genuinely cool — millvalley 251°
and schnebelhorn 204° — are the two flat-light, overcast scenes. The warm ones are the
backlit and low-sun scenes: stac pollaidh 79.6°, yellowstone 86.6°, rothesay 21.9°.
`[verified]`

That is the awkward result for this style specifically. **Tonalism is the twilight
movement**, and twilight is precisely when the distant part of a landscape is warmer than
the near part, not cooler. A cool-with-distance rule would be wrong most often on the
pictures the style exists for. `[inferred]` The canvases agree: far hue is in the warm arc
on 3 of 6 (killarney 99.5°, blakelock 92.2°, peaceful 62.4°), and only Whistler's *Nocturne:
Blue and Silver* is genuinely blue at 269.5°.

### 2.4 The two-handle gradient, tested as a depth proxy

The parent README's alternative to neural depth is a two-handle vertical gradient. Testing
it directly: does a row-only ramp order the hand-drawn far and near patches correctly?

| Photograph | far row | near row | ordered correctly? |
|---|---|---|---|
| mist-stacpollaidh | 0.49 | 0.88 | yes |
| fog-millvalley | 0.28 | 0.80 | yes |
| haze-kathmandu | 0.38 | 0.86 | yes |
| mist-schnebelhorn | 0.20 | 0.86 | yes |
| fog-yellowstone | 0.65 | 0.89 | yes |
| dusk-rothesay | 0.54 | 0.85 | yes |
| clear-swaledale | 0.28 | 0.91 | yes |
| clear-yangshuo | 0.15 | 0.89 | yes |

**8 of 8 landscapes.** `[verified]` The portrait also reports "yes", and that is an artefact
of my patch choice rather than a result — the backdrop patch happens to be above the face —
so read the portrait row as "the ramp did no harm", not "the ramp was right"; §5.2 shows it
moves the portrait by only 3.66 ΔE at full strength.

**The rejection of neural monocular depth stands and needs no revisiting.** A row-only ramp
with two user handles orders near and far correctly on every landscape in this corpus, and
the far/near lightness separation it is being asked to modulate is the one component that
is reliably there (§2.1). `[inferred]`

### 2.5 What the canvases do

Same method, seven Tonalist canvases, frames cropped 4% per edge.
`[verified — computed locally 2026-07-31]`

| Canvas | far L\* | near L\* | ΔL\* | far C\* | near C\* | ΔC\* | far h | near h | Δ contrast |
|---|---|---|---|---|---|---|---|---|---|
| Whistler, *Nocturne: Blue and Silver — Chelsea* | 31.22 | 66.86 | **−35.64** | 23.41 | 17.66 | +5.75 | 269.5 | 253.8 | −1.66 |
| Inness, *Landscape* (1889) | 26.75 | 22.08 | +4.67 | 14.06 | 16.40 | −2.34 | 107.7 | 92.9 | −0.07 |
| Twachtman, *Arques-la-Bataille* | 61.63 | 36.42 | **+25.21** | 8.92 | 19.71 | **−10.79** | 139.2 | 154.2 | +0.72 |
| Wyant, *Misty Morning Near the Lakes of Killarney* | 87.97 | 27.96 | **+60.00** | 28.79 | 26.26 | +2.54 | 99.5 | 50.0 | −3.45 |
| Blakelock, *Moonlight* | 72.24 | 21.08 | **+51.17** | 38.50 | 16.29 | +22.21 | 92.2 | 41.1 | +3.46 |
| Wyant, *Peaceful Valley* | 66.73 | 20.52 | **+46.20** | 31.64 | 12.38 | +19.26 | 62.4 | 78.0 | −2.18 |

Median |ΔL\*| **40.92**, against the photographs' median 45.22. **The canvases do not
compress the depth separation relative to a photograph** — they keep it while compressing
everything else (§3.3). Only Twachtman desaturates distance in the textbook direction, and
Whistler *inverts* the lightness relation because his far bank is a silhouette against a
lighter sky and water — the "notan" arrangement, which is the same reason
mist-stacpollaidh's far mountain reads dark.

**Ruling on §2:** the Post-Impressionism round's rejection of an aerial-perspective stage
should be **narrowed, not overturned**. Reject the chroma component and the hue component
for all styles. The lightness component is real on 8 of 8 landscapes, real on 5 of 6
canvases, is thrown away by the pipeline, and a two-handle ramp addresses it correctly.

---

## 3. What the shipped row actually does

`StyleRegistry.cs:41–64`: MarkScale 1.2; `EdgePreservingFloor` strength 2.0;
`ToneAndChromaRemap` contrast 0.55, key 4.0, chroma 0.45; `MotherColourTransform` fraction
0.30; `NearestQuantiser`; empty post-map slot. Every stage is pointwise or edge-local.
`[verified]` against the source, working tree of 2026-07-31.

### 3.1 The whole-image effect

`[verified — computed locally 2026-07-31]`

| Photograph | src L\* | out L\* | src SD L\* | out SD L\* | src C\* | out C\* | src range 5–95 | out range 5–95 | mean ΔE | unpaintable |
|---|---|---|---|---|---|---|---|---|---|---|
| mist-stacpollaidh | 51.78 | 57.62 | 29.32 | 13.33 | 21.31 | 9.57 | 79.11 | 34.96 | 20.88 | 2.5% |
| fog-millvalley | 54.01 | 57.11 | 25.83 | 12.76 | 12.32 | 5.34 | 75.35 | 34.40 | 14.53 | 25.2% |
| haze-kathmandu | 41.30 | 51.14 | 21.73 | 8.95 | 6.08 | 2.95 | 60.75 | 21.46 | 12.72 | 3.6% |
| mist-schnebelhorn | 36.84 | 48.50 | 19.37 | 8.55 | 9.48 | 4.39 | 59.96 | 23.17 | 14.72 | 3.0% |
| fog-yellowstone | 51.90 | 56.21 | 22.86 | 9.97 | 7.93 | 4.31 | 77.37 | 31.96 | 11.86 | 2.5% |
| dusk-rothesay | 49.59 | 55.23 | 25.02 | 11.22 | 13.43 | 5.63 | 74.14 | 30.56 | 15.39 | 18.0% |
| clear-swaledale | 50.54 | 55.28 | 24.94 | 12.08 | 17.97 | 8.00 | 77.03 | 34.71 | 17.92 | **38.9%** |
| clear-yangshuo | 37.94 | 48.28 | 21.53 | 10.62 | 11.50 | 5.20 | 71.02 | 33.69 | 16.21 | 24.1% |
| ctrl-portrait | 33.26 | 48.21 | 20.62 | 8.36 | 21.54 | 9.21 | 69.91 | 28.32 | 23.35 | 13.5% |

Median value range 74.14 → **31.96** (×0.43); median mean chroma 12.32 → **5.34** (×0.43).
On paper that is the Tonalist recipe. §3.3 shows it is roughly twice too much of it.

An aside outside this report's topic, recorded because it is large: **Tonalism's median
unpaintable share is 13.5% and reaches 38.9%**, and Tonalism is one of the two styles with
no `SmallRegionMerge` in slot 5. `[verified]`

### 3.2 What it does to depth

The same patches as §2.1, measured on the render rather than the source.
`[verified — computed locally 2026-07-31]`

| Photograph | src ΔL\* | **out ΔL\*** | ratio | src ΔC\* | out ΔC\* | src Δcontrast | out Δcontrast |
|---|---|---|---|---|---|---|---|
| mist-stacpollaidh | 53.41 | 20.88 | 0.391 | +18.43 | +10.32 | +1.29 | +1.47 |
| fog-millvalley | 46.67 | 22.78 | 0.488 | −8.72 | −3.25 | −9.35 | −1.87 |
| haze-kathmandu | 52.07 | 20.74 | 0.398 | +2.41 | +1.60 | −3.31 | −0.09 |
| mist-schnebelhorn | 43.77 | 18.10 | 0.414 | +6.42 | +3.90 | −2.03 | **+0.24** |
| fog-yellowstone | 64.65 | 25.32 | 0.392 | +15.17 | +6.89 | −1.12 | **+0.39** |
| dusk-rothesay | 28.18 | 10.89 | 0.386 | +9.20 | +3.43 | +5.31 | +2.51 |
| clear-swaledale | 23.01 | 10.64 | 0.462 | −16.45 | −6.40 | −3.13 | −0.15 |
| clear-yangshuo | 37.01 | 18.64 | 0.504 | +3.33 | +1.95 | −5.55 | **+0.48** |
| **median** | **45.22** | **19.69** | **0.406** | | ×0.50 | | |

Three things fall out. `[verified]`

1. **The depth separation is attenuated by ×0.41, and the attenuation is uniform.** The
   ratio sits in 0.386–0.504 on every photograph regardless of subject. That is the
   signature of a pointwise transform: contrast 0.55 compresses distance and local detail
   identically, because it cannot tell them apart.
2. **The *relative* depth structure survives.** |ΔL\*| / (L\* 5–95 range) is 0.65 median in
   the source and 0.63 median in the render (canvases: 0.68). So the row is not
   destroying the composition — it is shrinking the container. That is the useful framing
   for §3.3: the fix is the range, not a depth stage.
3. **The contrast/edge component of atmosphere is annihilated.** The far−near contrast
   difference flips sign on schnebelhorn, yellowstone and yangshuo, and collapses to
   −0.09 and −0.15 on kathmandu and swaledale. `EdgePreservingFloor` at strength 2 smooths
   the near and the far equally, so the differential a viewer reads as recession is gone.
   **A uniform edge-preserving filter is an anti-atmosphere operation.**

### 3.3 The numbers are about 2× too strong against the canvases

Sweeping contrast, chroma and the mother-colour fraction through the real pipeline, medians
over the eight landscape photographs. `[verified — computed locally 2026-07-31]`

| contrast | chroma | mother | median range 5–95 | median mean C\* | median far−near ΔL\* | median unpaintable |
|---|---|---|---|---|---|---|
| 0.40 | 0.45 | 0.30 | 27.17 | 5.34 | 16.89 | 11.6% |
| **0.55** | **0.45** | **0.30** | **32.82** | **5.27** | **19.69** | **10.8%** |
| 0.55 | 0.45 | 0.00 | 37.80 | 5.23 | 24.08 | 8.1% |
| 0.70 | 0.45 | 0.30 | 37.59 | 5.00 | 21.50 | 9.3% |
| 0.70 | 0.45 | 0.00 | **47.10** | 5.05 | **30.57** | 9.0% |
| 0.70 | 0.80 | 0.00 | 46.02 | 9.28 | 30.39 | 11.9% |
| 0.85 | 0.45 | 0.30 | 40.55 | 4.76 | 22.93 | 8.3% |
| 0.85 | 0.45 | 0.00 | **57.24** | 4.95 | **37.16** | 10.4% |
| 0.85 | 0.80 | 0.00 | 56.98 | 9.22 | 36.87 | 13.7% |

Reference bands, from §2.5 and §3.1: **seven canvases** give median range **51.46**, median
mean C\* **23.26**, median far−near |ΔL\*| **40.92**. **Nine photographs** give median range
74.14, median mean C\* 12.32.

Three readings. `[verified]` for the arithmetic, `[inferred]` for the recommendation.

- **Contrast ≈ 0.75–0.85 with the mother colour at 0.00 reproduces the canvases' value
  range and their depth separation simultaneously.** Contrast 0.85 / mother 0.00 gives
  57.24 and +37.16 against 51.46 and +40.92. The shipped 0.55 / 0.30 gives 32.82 and
  +19.69 — about 64% and 48% of the target.
- **The mother colour at 0.30 costs 5 to 17 L\* of range and buys almost no chroma.** At
  contrast 0.85 it takes the range from 57.24 to 40.55 (−29%) while moving mean chroma
  4.95 → 4.76 (−4%). This independently reproduces, from the image side, the
  Post-Impressionism round's correction 1: `MostNeutralPaintIndex()` returns Titanium
  White, so the stage is a lightness operation. On the candidate set it moves mean L\*
  41.19 → 57.09 and min L\* 11.00 → 38.32 at f = 0.30 while mean C\* falls only
  35.69 → 33.21. `[verified]`
- **Raising contrast does not cost paintability.** Unpaintable share sits in 8.1–13.7%
  across the whole sweep and is *lowest* at 0.55/0.00 and 0.85/0.30. The usual objection to
  opening the value range does not apply here.

**Chroma 0.45 is the one number I am not confident about.** The output mean C\* of 5.0–5.3
is below every canvas in the corpus (6.81–29.91, median 23.26) and half the source
photographs' 12.32. But those canvas figures are uncalibrated reproductions of varnished
oils, and varnish yellowing raises measured chroma; §9 records that debt. What is safe to
say is that **chroma 0.45 and contrast 0.55 are not independent errors — value and chroma
are coupled (the parent README's Hunt-effect warning), and both are currently set toward
the same extreme.**

### 3.4 The floor override is nearly a no-op

Rendering Tonalism with each of its three tuned stages returned to that stage's own
declared default, mean ΔE from the full row: `[verified]`

| Photograph | Tonalism vs Realism | remap at defaults | mother at 0.00 | floor at strength 1 |
|---|---|---|---|---|
| mist-stacpollaidh | 19.56 | 13.05 | 5.03 | **0.33** |
| fog-millvalley | 13.33 | 8.09 | 2.24 | **0.73** |
| haze-kathmandu | 11.11 | 4.07 | 3.73 | **0.34** |
| mist-schnebelhorn | 13.57 | 5.18 | 3.64 | **0.24** |
| fog-yellowstone | 11.10 | 7.06 | 4.23 | **0.35** |
| dusk-rothesay | 14.42 | 9.44 | 3.43 | **0.55** |
| clear-swaledale | 16.24 | 11.09 | 2.46 | **1.24** |
| clear-yangshuo | 14.95 | 6.72 | 3.27 | **1.18** |
| ctrl-portrait | 20.68 | 11.67 | 4.88 | **0.48** |

**Tonalism's `EdgePreservingFloor` strength override from 1.0 to 2.0 is worth 0.24–1.24 ΔE**
— below or at the edge of visibility on every photograph. The row's identity is the remap
(4.07–13.05) and secondarily the mother colour (2.24–5.03). Anyone tuning Tonalism should
spend their attention there. `[verified]`

---

## 4. Glazing and scumbling

The parent README's four-category table classes post-map K-M layering as "a different,
larger, physically honest invariant". This section works out what that invariant would cost
and what it would buy.

### 4.1 The kernel only implements the opaque limit, and the gap is 30 lines

`KubelkaMunk.Invert` (`KubelkaMunk.cs:220–229`) computes `1 + K/S − √((K/S)² + 2K/S)`, which
is the infinite-thickness solution. There is no thickness parameter anywhere in
`Pigments/`. `[verified]` against the source. `docs/research/acrylic-blending-findings.md`
already flags this: "a transparent glaze over a dry layer is layer compositing over a
substrate, needing the finite-thickness K–M form rather than the opaque inversion."

The finite-thickness (Kubelka 1948 hyperbolic) form over a substrate of internal
reflectance `Rg` is, per band:

```
a = (S + K) / S ;  b = √(a² − 1)
R = [1 − Rg·(a − b·coth(b·S·X))] / [a − Rg + b·coth(b·S·X)]
```

It is a strict generalisation: as `X → ∞`, `coth → 1` and `R → a − b`, which is exactly what
`Invert` computes. **Verified against the shipped kernel** — over all seven palette paints,
a layer of thickness 1e6 over a black substrate and over a white substrate both reproduce
`KubelkaMunk.Mix`'s masstone to **ΔE 0.00000**. `[verified — computed locally 2026-07-31]`
The pure-scatterer degenerate case (`b → 0`) needs its own branch, as `KubelkaMunk`'s
`MinimumScattering` floor does for the opposite degeneracy.

**Cost: roughly 30 lines in `Pigments/`, plus a Saunderson decision.** I applied Saunderson
once at the top of the stack and used the substrate's *internal* reflectance underneath
(via `KubelkaMunk.InverseSaunderson`, which already exists for exactly this kind of
round-trip). That ignores the refractive-index mismatch at the layer/substrate interface;
§9 records it.

### 4.2 A white scumble is the only paint that moves colour the atmospheric way

Thickness bisected per paint so the mean move is exactly 8.0 ΔE over 448 base mixtures
(seven masstones plus every pair at the shares `MixtureBuilder` samples), then the
direction of that move: `[verified — computed locally 2026-07-31]`

| Glaze paint | mean ΔL\* | mean ΔC\* | % ΔL\*>0 | % ΔC\*<0 | **% both** |
|---|---|---|---|---|---|
| **Titanium White** | **+4.09** | **−5.24** | 99.1 | 86.8 | **86.8** |
| Hansa Yellow Opaque | +1.05 | −0.05 | 76.8 | 44.0 | 39.3 |
| C.P. Cadmium Orange | −0.45 | −1.50 | 52.7 | 48.9 | 25.2 |
| Pyrrole Red | −2.86 | +0.11 | 24.1 | 43.3 | 7.8 |
| Cobalt Blue | −3.44 | −3.62 | 18.3 | 75.9 | 9.4 |
| Phthalo Green (Y.S.) | −2.11 | −2.62 | 1.6 | 62.9 | 0.7 |
| Bone Black | −5.08 | −4.79 | 2.2 | 90.2 | 2.2 |

Aerial perspective wants lighter *and* less chromatic. **Only a white layer delivers both,
and it delivers them on 86.8% of base colours.** `[verified]` This is the craft
literature's distinction arriving from the physics: `03-brushwork-and-edges.md §1.4` relays
the mnemonic *"glazes go darker, scumbling goes lighter"* and describes scumbling as
producing "a veiled, atmospheric, granular colour that is neither layer". **The atmospheric
operation is a scumble, not a glaze**, and the table above is the measurement of why.
`[inferred]` from the table and `[relayed]` for the mnemonic.

Cobalt Blue is the interesting near-miss: −3.62 mean ΔC\* on 75.9% of bases, but it darkens
(ΔL\* −3.44, only 18.3% lightened). A blue glaze is a *nocturne* device, not a haze device.

### 4.3 A scumble is not the mother colour, and the difference is 5×

Both are "unify everything through one paint", so the natural question is whether the
existing `MotherColourTransform` already covers the ground. It does not.
`[verified — computed locally 2026-07-31]`

| Operation | mean ΔL\* | mean ΔC\* | ΔC\* per unit ΔL\* |
|---|---|---|---|
| `MotherColourTransform` f = 0.30 (blends Titanium White) | +15.90 | −2.48 | **0.16** |
| White scumble at x = 0.30 | +11.59 | −13.92 | **1.20** |
| White scumble at x = 0.15 | +7.08 | −8.89 | **1.26** |

Mother-colour figures are candidate-set means (4,888 → 4,952 candidates, mean L\*
41.19 → 57.09, mean C\* 35.69 → 33.21); scumble figures are over the 448-mixture base
sample. **Per unit of lightness lift, a white scumble removes 7.5× the chroma the mother
colour does.** `[verified]` Mixing white *into* a paint and laying white *over* it are
physically different operations and the K-M kernel says so: the mixture keeps the
underlying pigment's absorption at full weight relative to a raised scattering, while the
layer attenuates the substrate's returned light multiplicatively at every band.

**Consequence: `MotherColourTransform` is not the luminous veil, and should not be
described as one.** The Post-Impressionism round already established it is a whitener; this
adds that it is a *weak* whitener in exactly the axis a veil is supposed to act on.

### 4.4 The gamut argument, which is the real reason to build it

The interesting measurement is not "does a scumble reach new colours" in the abstract, but
"does it reach the colours *this style* asks for". Tonalism's remap (contrast 0.55, key
+4.0, chroma 0.45) applied to every distinct 6-bit colour present in the nine photographs
gives **33,821 target colours**, weighted by pixel count. Against those:
`[verified — computed locally 2026-07-31]`

| Candidate set | candidates | mean ΔE (pixel-weighted) | p95 | max |
|---|---|---|---|---|
| opaque only (shipped) | 4,888 | **2.595** | 6.232 | 11.366 |
| + white scumble at x = 0.15 | 9,531 | 1.858 | 5.254 | 12.187 |
| + scumble at x = 0.075, 0.30 | 13,998 | **1.633** | 4.567 | 11.798 |
| + scumble at x = 0.05, 0.15, 0.40 | 18,477 | 1.426 | 4.146 | 9.955 |
| + scumble at x = 0.03, 0.075, 0.15, 0.30, 0.60 | 27,171 | **1.209** | 3.482 | 10.455 |

**The control matters more than the table.** Spending the same candidate budget on finer
opaque sampling instead:

| Opaque sampling | candidates | mean ΔE |
|---|---|---|
| pairs 63, triple divisions 16 (shipped) | 4,888 | 2.595 |
| pairs 255, divisions 24 | 12,123 | 2.045 |
| pairs 511, divisions 32 | 20,070 | 1.798 |

At ~13k candidates the scumble ladder gives **1.633** against finer sampling's **2.045**;
at ~19k, **1.426** against **1.798**. **The scumble is 20–25% better per candidate than
more of the same, which is the signature of a genuine gamut extension rather than denser
sampling of one.** `[verified]` It also confirms `MixtureBuilder`'s own doc comment that the
mixing lines saturate past 63 samples — quadrupling the opaque budget buys only 2.595 → 1.798.

**Where the gain is, and why it is Tonalism-specific:**

| L\* band | opaque candidates | + 5-level ladder | target colours | mean ΔE opaque | mean ΔE ladder |
|---|---|---|---|---|---|
| 20–30 | 1,016 | 3,107 | 438 | 1.645 | 0.754 |
| 30–40 | 865 | 4,873 | 3,813 | 2.374 | 0.766 |
| 40–50 | 848 | 5,774 | **7,405** | 2.774 | 1.244 |
| 50–60 | **621** | 6,060 | **8,681** | 2.370 | 0.900 |
| 60–70 | **466** | 3,596 | **9,285** | **3.045** | 1.828 |
| 70–80 | **289** | 2,049 | **4,090** | 2.786 | 1.655 |
| 80–90 | **93** | 591 | 109 | 4.005 | 3.285 |
| 90–100 | 42 | 224 | 0 | — | — |

**87% of Tonalism's targets land in L\* 40–80, and the opaque candidate set has only 2,224
of its 4,888 colours there — thinning to 289 above L\* 70.** The style's key of +4.0 and
contrast of 0.55 aim the whole image at the sparsest half of the paint gamut. A white
scumble multiplies candidate density there by 6–10×. `[verified]`

This independently reproduces, from the opposite direction, the Post-Impressionism round's
finding that "very light, very slightly tinted colours are the sparsest region of the
sampled achievable gamut… quantise at roughly ΔE 6.5". **Their observation is a general
property of the gamut; this is the style for which it is load-bearing.**

### 4.5 Where a scumble can and cannot live in the pipeline

`[verified]` against `PipelineStages.cs` and `MixtureBuilder.cs`.

| Placement | Works? |
|---|---|
| **Slot 5, `IPostMapStage`** | **No.** `Refine` receives `int[] indices` and a `CandidateSet` and has no access to spectra. A K-M layer needs `K` and `S`, which no post-map stage can name. The invariant is structural here and it structurally forbids this. |
| **Slot 3, `ICandidateTransform`** | **Not as the interface stands.** `MixtureBuilder` exposes exactly two mutations — `BlendInto` (opaque mixing) and `KeepOnly` (a colour predicate) — and neither can express a layer. Adding a third, e.g. `ScumbleLadder(paintIndex, double[] thicknesses)`, is where the widened invariant belongs. ~40 lines plus §4.1's kernel. |
| **Slot 4, `IQuantiser`** | Possible and wrong. Making the scumble level position-dependent sets `IsPositionDependent` and forfeits the colour cache, which the Abstract round measured at roughly 80×. |
| **Slot 1, `IPreMapStage`** | Works today and needs nothing new — but it is a *simulation* of a scumble in pixel space, not a scumble. It cannot tell the user what to paint. §5. |

**The clean design is slot 3 plus slot 1 together**: put the scumbled colours in the
candidate set so they are genuinely reachable and genuinely executable, and let the slot-1
ramp aim the far parts of the picture at them. The quantiser then *selects* the veil rather
than synthesising it, and per-pixel cost is unchanged. `[inferred]`

**What the widened invariant actually says.** Today: every output pixel is a colour the
selected paints can be mixed to. After: every output pixel is a colour the selected paints
can be mixed to, *or that mixture under a named scumble of a named paint at a named
thickness*. That is still executable by hand — arguably more so, since a scumble is a
single pass with a dry brush over a dried layer. But **the plan and the tooltip must carry
the scumble instruction, or the user is handed a colour they cannot mix.** That UI change
is not optional and should be costed with the feature.

### 4.6 A correction to the spectral pipeline's own documentation

`SpectralRenderer`'s class doc comment states: "Every comparison — recipe search, match
quality, the invariant tests — runs on the unmapped Lab… Gamut mapping is a display concern
and appears nowhere else." **That is false for the converter.**
`MixtureBuilder.RenderMixture` (`MixtureBuilder.cs:357–369`) calls
`SpectralRenderer.ToDisplayColor(...).ToArgb()` and `Build()` then derives every candidate's
CIELAB from that 8-bit sRGB value; `PalettePhotoConverter.BuildCandidates` is the same path.
**The entire converter runs on gamut-mapped, 8-bit-quantised colour.** `[verified]` against
both files.

Measured cost of the discrepancy: over 448 mixtures of the 7-paint palette, the unmapped
spectral CIELAB of a mixture sits a mean **3.35 ΔE** (p90 12.32, max 24.80) from the display
colour the candidate set stores for it. `[verified]` I lost an hour to this — my first
glaze measurements compared spectral Lab against stored candidate Lab and reported the sRGB
clipping as a glaze effect. **Anyone comparing new colour arithmetic against `CandidateSet`
must go through `ToDisplayColor` first.** The doc comment should be corrected; the
behaviour is probably right (the app shows paint on a screen) but it is not what the
comment says.

---

## 5. The atmosphere operation, built and measured

### 5.1 What was built

An `AtmosphericRamp` implementing the real `IPreMapStage`, run through the real
`StylePipeline.Render` in Tonalism's slot 1 ahead of `EdgePreservingFloor`. It blends every
pixel toward a veil colour in linear light, with the weight ramped between two row handles.
Six parameters: strength, near handle, far handle, veil R/G/B. About 60 lines.

Slot 1 is explicitly the positional slot and runs *before* `ResolveOncePerColour` builds the
6-bit cache (`StylePipeline.cs:127–153`), so this costs the cache nothing beyond a few newly
occupied keys and cannot violate the colour invariant — everything it produces is mapped
onto the achievable gamut afterwards. `[verified]` against the source.

### 5.2 What it does

Veil colour fixed at sRGB (210, 214, 220); handles set from the hand-drawn patch rows.
`[verified — computed locally 2026-07-31]`

| Photograph | strength | far−near ΔL\* | far−near ΔC\* | far−near Δcontrast | ΔE from plain | unpaintable |
|---|---|---|---|---|---|---|
| mist-stacpollaidh | 0.00 → 0.50 | 20.88 → **26.57** | +10.32 → +2.50 | +1.47 → +0.16 | 8.48 | 2.5% → **1.5%** |
| fog-millvalley | 0.00 → 0.50 | 22.78 → **24.35** | −3.25 → −2.69 | −1.87 → −2.29 | 4.88 | 25.2% → **15.5%** |
| haze-kathmandu | 0.00 → 0.50 | 20.74 → **25.75** | +1.60 → +1.35 | −0.09 → −0.19 | 7.26 | 3.6% → **2.2%** |
| mist-schnebelhorn | 0.00 → 0.50 | 18.10 → **25.64** | +3.90 → +1.63 | +0.24 → −0.14 | 7.96 | 3.0% → **2.1%** |
| fog-yellowstone | 0.00 → 0.50 | 25.32 → **26.65** | +6.89 → +2.75 | +0.39 → −0.13 | 8.29 | 2.5% → **1.9%** |
| dusk-rothesay | 0.00 → 0.50 | 10.89 → **21.37** | +3.43 → +1.75 | +2.51 → −0.12 | 7.96 | 18.0% → **7.8%** |
| clear-swaledale | 0.00 → 0.50 | 10.64 → **20.24** | −6.40 → −6.84 | −0.15 → −1.73 | 7.35 | 38.9% → **21.4%** |
| clear-yangshuo | 0.00 → 0.50 | 18.64 → **24.35** | +1.95 → −1.89 | +0.48 → −0.89 | 9.33 | 24.1% → **11.1%** |
| ctrl-portrait | 0.00 → 0.50 | 3.03 → 5.38 | +10.89 → +4.82 | −0.49 → −0.45 | **3.66** | 13.5% → 12.9% |

Four results. `[verified]`

1. **It restores depth separation.** The largest gains are on the photographs the shipped
   row hurt most: rothesay 10.89 → 21.37 (source 28.18), swaledale 10.64 → 20.24 (source
   23.01), schnebelhorn 18.10 → 25.64 (source 43.77). Roughly half the loss from §3.2 comes
   back at strength 0.50.
2. **It drives far chroma toward the near value.** +10.32 → +2.50, +6.89 → +2.75,
   +3.90 → +1.63. Note this achieves the "chroma falls with distance" appearance *without*
   a chroma stage and without fighting the photograph, because the veil is a colour, not a
   chroma multiplier. This is the resolution of §2.2's problem. `[inferred]`
3. **It improves paintability on all nine.** Swaledale 38.9% → 21.4%, millvalley
   25.2% → 15.5%, yangshuo 24.1% → 11.1%, rothesay 18.0% → 7.8%. The veil flattens the
   far field, which is exactly what an area opening is trying to do downstream. **An
   atmosphere stage is also a fragmentation stage**, and for Tonalism — which has no
   `SmallRegionMerge` — that is a second reason to want it.
4. **It does the least on the picture that wants it least.** The portrait moves 3.66 ΔE at
   full strength against 4.88–9.33 on the landscapes. That is not a safety argument (the
   handles are user-set, so a user could put a veil across a face), but it does mean the
   default is not catastrophic on non-landscapes.

### 5.3 Cost of leaving the veil colour as a constant

I did not sweep the veil colour, and it matters: a warm veil is a sunset, a cool one is
haze, and §2.3 says Tonalist subjects go both ways. The right default is probably derived
from the image's own light — the chroma-weighted mean hue of its lightest decile — but that
is a guess and §9 records it as untested.

---

## 6. The toned ground

**A ground is not an atmosphere, and the distinction is the one the Abstract round already
drew.** [abstract/03-grounds-and-background.md](../abstract/03-grounds-and-background.md)
separates the *ground* (a layer, no position, unifies multiplicatively) from the *field* (a
region). Atmosphere is neither: it is a *gradient*, and it is the only one of the three that
has a spatial derivative. `[inferred]` A ground applied everywhere at one strength cannot
express recession, because recession is the change in the veil, not the veil.

That said, `GroundFill` exists, Tonalism's slot 5 is empty, and the question deserves an
answer rather than a definition. Registering the shipped `GroundFill` in Tonalism's slot 5:
`[verified — computed locally 2026-07-31]`

| Photograph | coverage | mean filled row | field was | became | moved | unpaintable before → after | whole-image ΔE |
|---|---|---|---|---|---|---|---|
| mist-stacpollaidh | 11.44% | 0.20 | L\* 68.6 C\* 10.3 | L\* 58.4 C\* 5.6 | 11.29 | 2.5% → 2.5% | 1.29 |
| fog-millvalley | 7.51% | 0.12 | L\* 71.8 C\* 5.8 | L\* 57.9 C\* 2.3 | 14.39 | 25.2% → 25.2% | 1.08 |
| haze-kathmandu | 15.33% | 0.24 | L\* 60.2 C\* 2.0 | L\* 58.2 C\* 1.7 | **2.09** | 3.6% → 3.5% | 0.32 |
| mist-schnebelhorn | 6.34% | 0.19 | L\* 58.4 C\* 7.1 | L\* 56.3 C\* 3.9 | **3.87** | 3.0% → 3.0% | 0.25 |
| fog-yellowstone | 11.08% | **0.91** | L\* 38.9 C\* 1.5 | L\* 58.2 C\* 1.7 | **19.35** | 2.5% → 2.5% | 2.14 |
| dusk-rothesay | 9.63% | 0.21 | L\* 63.6 C\* 5.1 | L\* 57.9 C\* 2.3 | 6.48 | 18.0% → 18.0% | 0.62 |
| clear-swaledale | 3.64% | 0.14 | L\* 74.6 C\* 2.6 | L\* 57.9 C\* 2.3 | 16.72 | 38.9% → 38.9% | 0.61 |
| clear-yangshuo | 1.91% | 0.04 | L\* 70.9 C\* 5.1 | L\* 58.2 C\* 1.7 | 13.28 | 24.1% → 24.1% | 0.25 |
| ctrl-portrait | 10.23% | **0.88** | L\* 38.9 C\* 1.5 | L\* 58.2 C\* 1.7 | 19.35 | 13.5% → 13.5% | 1.98 |

Three findings, one of which is a partial correction to prior work. `[verified]`

1. **`GroundFill` is much gentler in Tonalism than in Post-Impressionism — for an accidental
   reason.** ΔE 2.09–19.35 here against 23.4–58.7 there. The cause is that Tonalism's
   `key = 4.0` and `contrast = 0.55` park the whole image near L\* 51–58 (§3.1), so the
   stage's hard-coded 58.0 (`GroundFill.cs:94`) is roughly where the field already was.
   **This is a coincidence of two numbers in different files and it evaporates the moment a
   user moves the key slider** — and §3.3 recommends moving contrast, which widens the range
   and breaks the accident.
2. **It still does nothing measurable.** The paintability metric is unchanged on eight of
   nine photographs and moves 0.1 points on the ninth. The Post-Impressionism round's
   finding reproduces exactly.
3. **On 2 of 9 it fills the foreground, not the sky.** Mean filled row 0.91 on yellowstone
   and 0.88 on the portrait, both lifting a dark near region by 19.35 ΔE — the precise
   inverse of aerial perspective. "Largest border-connected region" finds the sky most of
   the time and the ground the rest of the time, and it has no way to tell which it found.

**Ruling: do not register `GroundFill` in Tonalism**, repaired or not. A repaired
`GroundFill` (the Post-Impressionism round's pick 2 — derive the lightness, add a coverage
floor) would be a defensible *field* device for a style that wants one. Tonalism wants a
*gradient*, and §5 is that.

---

## 7. Recommended build items, by payoff ÷ cost

### Pick 1 — retune contrast and the mother colour. **~4 lines.**

`StyleRegistry.cs:59–64`: `contrast` **0.55 → 0.80**, `MotherColourTransform` fraction
**0.30 → 0.00** (which means Tonalism switches to `KeepAllCandidates` and drops a stage).

**Payoff:** median value range 32.8 → ≈52, landing on the canvases' 51.46; median far−near
depth separation 19.7 → ≈34, against the canvases' 40.9; unpaintable share unchanged or
slightly better. §3.3.
**Cost:** two numbers, one regenerated golden PNG, and a look at the result with human eyes.
**Risk:** the mother colour is also load-bearing for other tracks' concerns (harmony,
palette unity) — this recommendation is made on the atmosphere axis only, and the controller
should reconcile it. If the mother colour must stay, contrast 0.85 with f = 0.30 recovers
the range to 40.6, which is most of the way.

Nothing else in this report comes close on payoff ÷ cost.

### Pick 2 — `AtmosphericRamp` in slot 1. **~60 lines, plus two handles in the UI.**

Written and measured (§5). Blends toward a veil colour in linear light with the weight
ramped between two user-set rows.

**Payoff:** restores roughly half the depth separation the row destroys, drives far chroma
toward the near value without touching the chroma multiplier, and cuts unpaintable share on
9 of 9 photographs (swaledale 38.9% → 21.4%).
**Cost:** the stage, plus two draggable handles — the parent README already concedes a user
click, and this is that concession spent on a horizon rather than a focal point.
**Invariant:** untouched, structurally. Slot 1 is pre-map. Cache: free.
**Do not** default the strength above zero until someone has looked at the output; §9.

### Pick 3 — the white scumble ladder. **~30 lines of kernel + ~40 in `MixtureBuilder` + a plan/UI change.**

Finite-thickness K-M (§4.1), a `ScumbleLadder` mutation on `MixtureBuilder`, and a scumble
field on whatever the plan and tooltip emit.

**Payoff:** pixel-weighted quantisation error against Tonalism's own targets 2.595 → 1.633
ΔE at 2.9× candidates, or 1.209 at 5.6×; 20–25% better per candidate than the same budget
spent on finer opaque sampling; 6–10× more candidates in the L\* 50–80 band where 87% of
Tonalism's targets land. §4.4.
**Cost:** build time scales with the ladder depth; the candidate set grows 2–6×; and **the
invariant widens** — every output is a mixture *or a mixture under a named scumble*. The
plan must say so or the user cannot execute it.
**Ranked third because of that last clause**, not because the measurement is weak. It is the
strongest measurement in this report; it is also the only pick that changes what the app
promises.

### Pick 4 — correct `SpectralRenderer`'s doc comment. **~4 lines of comment.**

"Gamut mapping is a display concern and appears nowhere else" is false: the whole converter
runs on `ToDisplayColor` output, 3.35 ΔE mean from the unmapped spectral Lab. §4.6. Free,
and it will save the next person the hour it cost me.

---

## 8. What not to build

Each of these is something I went looking for and rejected on evidence. The parent,
Abstract, Fauvism and Post-Impressionism lists all still apply.

- **A chroma-falls-with-distance stage, in any style.** The far patch is *more* chromatic in
  absolute C\*ab on 6 of 8 landscape photographs and 4 of 6 Tonalist canvases, and removing
  the lightness confound by regression does not change the count. §2.2. The rule survives
  only in relative chroma C\*/L\*, which no stage in this pipeline operates on.
- **A hue-shifts-cool-with-distance stage.** The far patch is in the warm arc (h\_ab
  20–100°) on 5 of 8 landscapes, and it fails hardest on backlit and low-sun scenes — which
  is Tonalism's entire subject matter. §2.3.
- **A separate "edge softness with distance" stage alongside a contrast one.** r(local
  contrast, edge energy) = 0.973 mean, minimum 0.956 across 21,340 blocks. They are one
  measurement. §1.
- **A K-M glaze as an `IPostMapStage`.** `Refine` takes indices and a `CandidateSet`; a
  layer needs K and S. It cannot be expressed, and that is the invariant working correctly.
  §4.5.
- **A glaze in a *chromatic* paint as the atmosphere device.** Only white moves colour in
  the aerial direction; cobalt darkens (ΔL\* −3.44), black darkens harder, and every warm
  paint fails the chroma test. §4.2. A cobalt glaze is a nocturne device and should be
  labelled one.
- **Treating `MotherColourTransform` as the luminous veil.** Per unit of lightness lift it
  removes 7.5× less chroma than a white scumble, and it has no area and no gradient. §4.3.
- **`GroundFill` in Tonalism, repaired or not.** No measurable paintability effect on 9 of
  9; fills the *foreground* on 2 of 9; and its apparent gentleness here is an accident of
  `key = 4.0` meeting a hard-coded L\* 58 that pick 1 would break. §6.
- **Any further value compression.** The shipped row already sits at 62% of the canvases'
  value range and 23% of their mean chroma. §3.3. The pressure should be in the other
  direction.
- **Raising `EdgePreservingFloor` strength as a Tonalism control.** The existing 1.0 → 2.0
  override is worth 0.24–1.24 ΔE — at or below visibility on every photograph. §3.4.
- **Neural monocular depth.** Already rejected by the parent README; a row-only ramp orders
  the hand-drawn far and near patches correctly on 8 of 8 landscapes. §2.4. Nothing here
  reopens it.
- **Deriving the veil weight from an automatic horizon detector.** Not tested, and the
  parent README's rejection of automatic focal-point detection as load-bearing applies by
  the same argument: two handles cost the user one drag and cannot be wrong in a way the
  user cannot see.

---

## 9. Verification debt

Ranked by how much clearing it would change a decision.

1. **Nobody has looked at any of this.** Every figure in this report is a statistic. I
   rendered nine photographs through five stage configurations and measured them; I did not
   open one. The parent README's rejection of automated quality scoring applies with full
   force to picks 1 and 2 — a value range of 52 may look right or may look like a
   under-exposed Realism. **Render the pick-1 numbers and the pick-2 ramp and look before
   changing the registry.** Cheapest item and it gates the top two picks.
2. **The canvas statistics are measurements of uncalibrated web reproductions of varnished,
   aged oil paintings.** Varnish yellowing raises measured chroma and lowers measured
   lightness; the Abstract round's Rothko caution (lithol red degradation, "any Lab value
   sampled from a photograph of a Rothko is wrong by an unknown and non-uniform amount") and
   the Fauvism round's cadmium-yellow shift both apply. The Inness reproduction is a
   Sotheby's catalogue image; two are Google Art Project captures; one is a MET donation.
   **The median mean C\* of 23.26 in §3.3 is the least trustworthy number in this report**
   and the chroma recommendation deliberately stops short of using it.
3. **The glaze thickness `x` has no absolute calibration.** The coefficients' units come
   from `Tools/IngestSpectra` and nothing in the repository states what one unit of
   thickness is. I normalised by a self-derived hiding thickness (the `x` at which a layer
   over black and over white agree to ΔE < 1: 12.2 for Bone Black, 322 for Hansa Yellow
   Opaque), which is internally consistent but not physical. Before shipping pick 3, the
   ladder levels need to be chosen against something a painter can measure — coats, or
   medium-to-paint ratio.
4. **The finite-thickness form applies Saunderson once, at the top of the stack, and uses
   the substrate's internal reflectance underneath.** That ignores the refractive-index
   mismatch at the layer/substrate interface. It is the standard first-order treatment and
   I did not check it against a published two-layer derivation. The opaque-limit gate
   (ΔE 0.00000) tests the *bulk* solution, not the interface convention.
5. **The near/far patches are my own eyeball judgements from the 960-px renders.** A
   different annotator would move every number in §2 and §3.2. The boxes are recorded in
   §10 so the judgement is auditable rather than hidden; nobody should treat ±3 L\* in those
   tables as meaningful.
6. **Nine photographs and seven canvases.** The Post-Impressionism round already flagged
   that seven is small; this is nine, deliberately landscape-weighted because atmosphere
   lives there, which means the corpus is *biased toward* the conclusion that a landscape
   device is worth having. The one non-landscape control behaves as expected but one control
   is one control.
7. **The contrast = 1.00 row of the §3.3 sweep did not complete** before the probe hit its
   time limit. The trend across 0.40–0.85 is linear enough that interpolation is safe, but
   the row is not measured.
8. **The veil colour in §5 is a constant I chose** — sRGB (210, 214, 220), a neutral light
   grey. I did not sweep it, and §2.3 says Tonalist far-distance hue goes both warm and
   cool. The natural default (chroma-weighted mean hue of the lightest decile) is untested.
9. **`x_hide` for Titanium White is 102.45 and for Bone Black 12.19** — an 8× spread that I
   have not sanity-checked against real hiding-power data. If the coefficients' scattering
   scale is off for one pigment family, §4.2's ranking could move.
10. **The "narrow value range" claim about Tonalism is `[relayed]`** from
    `02-styles-and-movements.md`, which relays it from tonalism.com and a painting-
    instruction blog ("a value range of maybe three steps"). **My canvas measurements
    contradict it**: L\* 5–95 range 32.6–77.0, median 51.5, which is not narrow. Either the
    reproductions are wrong (item 2) or the claim is folklore. I did not resolve this and it
    matters for pick 1. The underlying Whistler *Sea and Rain* four-pigment analysis is
    also still `[relayed]` and unlocated, as report 02 records.
11. **I did not test whether the slot-1 ramp and the slot-3 scumble ladder compose.** §4.5
    argues they should — the ramp aims veiled pixels at scumbled candidates — but the ladder
    does not exist yet, so the claim is `[inferred]`.
12. **The corpus fetch hit HTTP 429 from Wikimedia** partway through and two intended
    replacement images were never retrieved. See §10.

---

## 10. Corpus provenance

**Every image below was opened and looked at before use.** One was rejected on inspection
(see the note under the photograph table) — the Fauvism and Post-Impressionism rounds both
recorded corpora compromised by images that passed a metadata check and failed a visual one.

### 10.1 Photographs

All from Wikimedia Commons, fetched 2026-07-31 via
`Special:FilePath`/`imageinfo` at `iiurlwidth=960`; the stac pollaidh original is only
640 px wide so it was served at 640. Camera metadata was read from Commons' own
`commonmetadata` and is reproduced as a *corroborating* signal, not as the check.

| slug | Commons file | original | licence | author | camera |
|---|---|---|---|---|---|
| mist-stacpollaidh | `Stac Pollaidh above a layer of evening mist - geograph.org.uk - 599951.jpg` | 640×426 | CC BY-SA 2.0 | Ian Capper (geograph) | — |
| fog-millvalley | `Early morning fog over Mill Valley (Panorama).jpg` | 4767×2243 | CC BY-SA 3.0 | Frank Schulenburg | Leica X1 |
| haze-kathmandu | `Kathmandu Valley panoramic view from Shivapuri hills under haze.jpg` | 5536×4160 | CC0 | Krishna k. sahh | Canon EOS 6D Mark II |
| mist-schnebelhorn | `Misty Forest Schnebelhorn.jpg` | 4686×3099 | CC BY-SA 4.0 | Jason Conzett | Sony ILCE-7M2 |
| fog-yellowstone | `Fog on the Yellowstone River in Hayden Valley.jpg` | 4032×3024 | CC BY-SA 4.0 | TigerScientist | iPhone 12 mini |
| dusk-rothesay | `Rothesay Harbour at twilight - geograph.org.uk - 8310068.jpg` | 1600×1200 | CC BY-SA 2.0 | Mr H (geograph) | — |
| clear-swaledale | `2015 Swaledale from Kisdon Hill.jpg` | 4608×3072 | CC BY-SA 3.0 | Kreuzschnabel | Olympus E-M1 |
| clear-yangshuo | `1 pano cuiping yangshuo 2016.jpg` | 17806×6969 | CC BY-SA 4.0 | Chensiyuan | Nikon D810 |
| ctrl-portrait | `Russia, Moscow Oblast. Young woman with umbrella, studio portrait.jpg` | 2028×3050 | CC BY-SA 4.0 | Dmitry Makeev | Nikon D70s |

`clear-swaledale` and `clear-yangshuo` are deliberately the same two files the
Post-Impressionism round used, so §2.1's band figures can be checked against theirs
(+51.23 / −18.05 and +40.86 / −1.64; both reproduce exactly).

**Rejected on visual inspection:** `Blue Ridge Haze - Flickr - basheertome.jpg`. It passes
every metadata test — Fujifilm X100, CC BY 2.0, a real photograph of a real place — and on
opening it is a **heavily defocused, split-toned (teal/orange) Lightroom edit** with no
resolvable structure at all. It would have contributed a spurious "far is very low
contrast" data point to §1 and §2. Two intended replacements
(`Clouds and mountains as seen from Woraksan (3).jpg`, `Breath of the Mountains.jpg`) were
lost to an HTTP 429 from Wikimedia and the corpus was left at nine.

**Hand-drawn far/near boxes**, as fractions of (width, height), (x0, y0, x1, y1). These are
my judgement after looking at each picture; they are the load-bearing input to §2.1, §2.2,
§3.2 and §5.2 and are recorded so the judgement can be audited.

| slug | far | near | what they are |
|---|---|---|---|
| mist-stacpollaidh | 0.25, 0.38, 0.65, 0.60 | 0.58, 0.76, 0.98, 0.99 | backlit peak above the mist bank / foreground hillside |
| fog-millvalley | 0.16, 0.22, 0.64, 0.33 | 0.70, 0.62, 0.99, 0.99 | ridge under the cloud band / flowering bush and bank |
| haze-kathmandu | 0.26, 0.32, 0.85, 0.43 | 0.16, 0.74, 0.74, 0.99 | ridge line behind valley haze / foreground trees |
| mist-schnebelhorn | 0.05, 0.05, 0.45, 0.34 | 0.10, 0.74, 0.90, 0.99 | misted slope upper left / front rank of firs |
| fog-yellowstone | 0.16, 0.61, 0.55, 0.68 | 0.00, 0.78, 0.34, 0.99 | fogged shoreline across the river / sage bank |
| dusk-rothesay | 0.10, 0.50, 0.55, 0.59 | 0.58, 0.72, 0.99, 0.99 | terrace across the harbour / fishing boat and quay |
| clear-swaledale | 0.16, 0.24, 0.85, 0.32 | 0.10, 0.82, 0.90, 0.99 | moor top beyond the dale / foreground pasture |
| clear-yangshuo | 0.30, 0.10, 0.75, 0.21 | 0.05, 0.78, 0.60, 0.99 | karst peaks at the horizon / foreground scrub |
| ctrl-portrait | 0.86, 0.00, 0.99, 0.26 | 0.42, 0.10, 0.72, 0.31 | studio backdrop / the sitter's face |

### 10.2 Tonalist canvases

All public domain, from Wikimedia Commons, fetched 2026-07-31 at `iiurlwidth=900`. **Every
one was opened and looked at; four carry a visible frame edge or nameplate in the
reproduction, so all seven were cropped 4% per edge before measurement.** The far/near
boxes are stated in cropped-frame coordinates, so they carry up to a 4% positional shift
relative to the boxes I judged on the uncropped images.

| slug | painting | reproduction source | frame in shot? |
|---|---|---|---|
| whistler-nocturne-blue-silver | Whistler, *Nocturne: Blue and Silver — Chelsea* | Google Cultural Institute, max zoom | no |
| whistler-falling-rocket | Whistler, *Nocturne in Black and Gold: The Falling Rocket* (1875) | "Unknown source", 707×1000 | no |
| inness-1889 | George Inness, *Landscape* (1889) | Sotheby's New York, 11 April 2013, lot 45 | no |
| twachtman-arques | Twachtman, *Arques-la-Bataille* | Metropolitan Museum of Art donation | **yes, gilt** |
| blakelock | Blakelock, *Moonlight* | Google Cultural Institute, max zoom | **yes, light** |
| wyant-killarney | Wyant, *Misty Morning Near the Lakes of Killarney* | Harvard Art Museums | **yes + nameplate** |
| wyant-peaceful | Wyant, *Peaceful Valley* | National Gallery of Art | thin |

*The Falling Rocket* is included in the whole-canvas statistics (§3.3's reference band) and
excluded from the far/near table, because it has no legible near/far structure — it is a
near-uniform dark field with sparks.

**These are reproductions, not measurements of paint.** Every figure derived from them
carries verification debt item 2. They are used here only to bound the *shipped row's*
numbers, never to seed a preset.

### 10.3 Probe

A console project with assembly name `PaintTranslator.Tests`, referencing
`PaintTranslator.csproj`, in the session scratchpad. Not in the repository, not staged.
Files: `Corpus.cs`, `Stats.cs`, `Aerial.cs`, `Shipped.cs`, `Bands.cs`, `Glaze.cs`,
`Ground.cs`, `Slot1.cs`, `Paintings.cs`, `Sweep.cs`, `Ladder.cs`. Two gates guard the two
pieces of new arithmetic and both are reported in the output rather than assumed:
`G0` (finite-thickness K-M vs `KubelkaMunk.Mix`, ΔE 0.00000 × 7 paints) and `L0`
(re-enumeration vs `MixtureBuilder.Build()`, 4,888 = 4,888, zero missing, zero extra).
