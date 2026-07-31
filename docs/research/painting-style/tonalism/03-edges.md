# Research: Tonalism — Edges

**Track:** Tonalism, track 3 of 4 — the edges half.
**Date:** 2026-07-31
**Scope:** what Tonalism's edge treatment should be, and what the five-slot pipeline should do to
produce it. Covers the movement's own technical vocabulary for the lost edge, a measurement of
edge behaviour on 15 Tonalist canvases against 15 photographs, an audit of the shipped floor at
strength 2.0, the Gaussian-versus-guided-filter question re-opened for this one style, and the
edge-hierarchy lever costed.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(edge hierarchy, the filter families, the four-category invariant table, lever 1's spatially
varying blur), [`../02-styles-and-movements.md`](../02-styles-and-movements.md) §6 (the Tonalism
preset row this report tests), and
[`../post-impressionism/03-edges.md`](../post-impressionism/03-edges.md) (the boundary statistics
this report reproduces, and the guided-filter knob correction it makes). Where I correct any of
them, §8 says so explicitly.

**Verification convention** — matching the rest of `docs/research/`:
`[verified]` = read directly from the cited primary source, or computed in this repo ·
`[relayed]` = reported by a secondary source I did not confirm at the primary ·
`[inferred]` = my reasoning from the above, stated nowhere.

---

## 0. Headline

**Tonalism's softness is tonal, not spatial, and the shipped row already delivers the spatial half
almost exactly. The defect is that it delivers it by flattening the picture into a 26 L\* band,
and inside that band it is harder-edged than the photograph it started from.**

Four results, in descending order of how much they should change a decision.

1. **Most of what reads as "soft edges" in a Tonalist canvas is tonal-range compression, not edge
   softness.** Over 15 canvases the L\* 5–95 spread is **39.5** against 15 photographs' **67.2**,
   and raw high-contrast edge density is **8.8× lower** (pixels with a neighbour ≥ ΔE 20: 1.16% vs
   10.20%). Rescale every image to a common 60 L\* range and that gap **collapses**: edge density
   at ΔE 10 becomes **22.43% vs 23.69% — indistinguishable** — and mean local contrast is actually
   *higher* on the canvases (7.33 vs 6.87). Only the extreme tail survives normalisation
   (ΔE 20: 4.34% vs 8.45%). `[verified]` §2.4
2. **The shipped render is the hardest-edged thing measured relative to its own range.** Tonalism
   compresses to L\* range **26.3** — a third narrower than the canvases — and its
   range-normalised ΔE-20 density is **11.52%**, against **8.45%** for the source photographs and
   **4.34%** for the canvases. It is soft in absolute terms only because it is flat; per unit of
   its own contrast it bands harder than an unprocessed photograph. `[verified]` §4.3
3. **The parent round's headline reason for replacing the Gaussian does not survive contact with a
   Tonalist corpus, and its conclusion survives anyway on different grounds.** Graham & Field's
   art-vs-natural-scene spectral contrast is the load-bearing argument for
   "blur makes a photo statistically *less* painting-like". Measured here, Tonalist canvases are
   **steeper** than photographs (−1.113 ± 0.174 vs −1.031 ± 0.175, d ≈ 0.47, overlapping
   distributions), which is the opposite sign, and the *guided filter* steepens the spectrum
   **more** than a Gaussian does at every comparable setting (ε 0.30 → −2.31; Gaussian radius 5 →
   −1.54). **Edge-preserving is not spectrum-preserving.** The right reason not to add a Gaussian
   to Tonalism is that after the mandatory floor it buys nothing (z-distance to the canvas
   statistics 1.11 with a radius-1 blur, 1.14 without, 1.41 at radius 2) and at any useful radius
   it overshoots the measured edge width by 2×. `[verified]` §5
4. **Varying the guided filter's radius across the frame does nothing, for the second time.** The
   Post-Impressionism round found ≈0% and named the parent lever's wrong knob; reproduced here on
   Tonalism over 15 photographs the four radial bands move **+0.0 / +0.1 / +1.1 / +0.3%**. Varying
   its **edge threshold** instead moves them **+0.0 / −5.2 / −19.4 / −25.2%** and *improves*
   paintability (sub-mark share 32.7% → 23.6%). `[verified]` §6.2

And a fifth, which is the reason edge hierarchy is not this style's defining device: **the
movement's own theorist says uniform softness is equally canonical, and the canvases agree.**
Birge Harrison — in this report's own corpus — devotes a chapter of *Landscape Painting* (1909) to
the lost edge and names two legitimate cases: pictures with "a focus, or centre of interest", and
"certain of Whistler's nocturnes… wherein the eye broods dreamily over the whole scene… the
refraction distributed evenly all over the canvas". `[verified]` Measured, the centre-to-edge
ratio of high-contrast edge density across 15 canvases has median 1.83 but runs **0.15 to 6.86**,
and is **below 1 on five of fifteen** — including Harrison's own painting, at 0.15. §3.

---

## 1. What this movement calls the lost edge, in its own words

The parent report's edge-hierarchy sources (§1.1 of `03-brushwork-and-edges.md`) are modern
painting-instruction blogs. Tonalism does better than that: it has a primary treatise, by a painter
in this report's corpus, that is entirely about the problem.

### 1.1 Birge Harrison, *Landscape Painting* (1909), chapter IV: "Refraction"

Harrison taught at the Art Students League summer school at Woodstock and the book collects those
seminars `[relayed]`. The whole chapter is the lost edge, and he says so: `[verified — read from the full text of the
1909 Scribner edition,
[archive.org/details/landscapepainti01harrgoog](https://archive.org/details/landscapepainti01harrgoog)]`

> "The French word *envelope* and our own **'lost-edge'** were descriptive of the result only and
> not of the cause… the reader will kindly assume **refraction** to stand for that intimate effect
> of one mass of color or value upon its adjoining mass which results in the 'lost-edge,' and a
> general diffusion of tone, thus giving to pictures their atmospheric quality."

Four passages matter for this app, and one of them is the best epigraph a photo-to-paint converter
could have:

**(a) The photograph is named as the antagonist, explicitly.**

> "The scientific fact is that the edges of things are sharp and hard as a rule. This is amply
> proved by the photographic lens, which gives us a clear-cut definition all over the plate which
> the human eye could never hope to compass… **But this scientific fact would still remain an
> artistic lie.**"

**(b) Softness is a property of *vision*, so it is graded by eccentricity from the fixation point.**
Harrison's demonstration is the card pinned to an oak branch: at ten paces an observer holding gaze
on the card cannot count fifty leaves, and the rest is "just a blur". His radius of exact vision is
"twelve inches" at arm's length; "beyond the radius of twelve inches from the centre the image
begins to blur, and this blur increases rapidly".

**(c) There are two legitimate treatments, and the uniform one is named.**

> "Now any interesting picture motive generally has a focus, or centre of interest… it is evident
> that this portion will appear much more definite in outline than the outlying regions of the
> composition; which will become more and more blurred, as they recede, with the softened or lost
> edge everywhere. **But there are other motives — certain of Whistler's nocturnes, for instance —
> wherein the eye broods dreamily over the whole scene, not resting fixed upon any one given point
> of interest; and these should be painted precisely as Whistler painted them, the refraction
> distributed evenly all over the canvas.**"

**(d) The technique is a contrast instruction, not a spatial one.**

> "Prepare for the refraction, as they did, by **lowering values as you approach the edge**, so
> that the final stroke which draws your limb or your tree may be **as fresh and as crisp as
> possible without being hard**."

That last sentence is the whole of §2.3 stated by a practitioner eighty years before anyone
measured it: the mark stays crisp, the *contrast across it* comes down. It is a pointwise
operation on value, not a blur.

Harrison also disposes of the idea that atmosphere comes from broken colour: "A Whistler nocturne…
which is painted without the slightest vibration, or any attempt at broken color, may swoon in the
most exquisite bath of atmosphere, while a vibrant Monet, with a few hard edges, may lack all
atmospheric quality." `[verified]` That is worth holding against the parent README's build order,
which puts divided colour ahead of everything else.

### 1.2 The secondary literature agrees and adds nothing quantitative

- **Definition and dates.** Tonalism is an American style of c. 1880–1915 defined by "evocative
  atmospheric effects and a limited palette of soft, mostly dark colours"; "soft, diffused light,
  muted tones, and hazily outlined objects"; its sources are Barbizon by way of Inness and Hunt,
  and the Aesthetic movement by way of Whistler. The label itself is retrospective, from Wanda
  Corn's 1972 exhibition *The Color of Mood: American Tonalism 1880–1910*. `[relayed — Wikipedia,
  "Tonalism"; Artsy; TheArtStory; search summaries of Corn's catalogue, which I did not obtain]`
- **David Adams Cleveland's twelve characteristics**, as published by the American Tonalist
  Society, include #8 "**the use of soft-edged forms to further the sense of ambiguity and mystery
  of place**" and #5 "a sense of movement or metamorphosis in nature (**the vibration and
  refraction of tones**)". `[verified — read from
  [americantonalistsociety.com/what-is-tonalism](https://www.americantonalistsociety.com/what-is-tonalism)]`
  Note that "refraction" is Harrison's coinage: the movement's modern summary still carries his
  term, which is good evidence the chapter above is central rather than idiosyncratic.
- **Whistler's own statement** is about subtraction, not about edges as such: "And when the evening
  mist clothes the riverside with poetry, as with a veil – and the poor buildings lose themselves
  in the dim sky"; the artist "does not confine himself to purposeless copying, without thought,
  each blade of grass". `[verified — read from the Glasgow *Correspondence of James McNeill
  Whistler* transcription of the Ten O'Clock,
  [whistler.arts.gla.ac.uk/miscellany/tenoclock](https://www.whistler.arts.gla.ac.uk/miscellany/tenoclock/)]`
- **Why the nocturnes measure the way they do.** Whistler thinned oil with a medium he called his
  "sauce" and worked wet-in-wet, scraping, rubbing and scumbling; Hackney and Townsend's technical
  studies at Tate, the NGA and the Hunterian identified turpentine and mastic, with evidence of
  bleached shellac in some nocturnes and not others. `[relayed — Tate, "Understanding Whistler's
  Techniques", and an AIC 2025 abstract; I did not reach the underlying technical reports]` A
  fluid, wiped, wet-in-wet surface is a physical mechanism for a canvas with no hard edges at all,
  which is exactly what *Nocturne: Blue and Gold — Southampton Water* measures as (§2.2).
- **Negative result from the computational-aesthetics literature.** Redies' group reports that
  "there is not a clear pattern of edge density that would help to distinguish natural patterns
  from artworks because there is a large amount of variation within and between image categories",
  while landscape *artworks* do show significantly higher edge-orientation entropy than landscape
  photographs. `[relayed — search summaries of Redies et al., *Vision Research* 2017, "High entropy
  of edge orientations…"; I did not open the paper]` My §2 result is consistent with this and
  sharper: plain edge *density* separates the two corpora weakly, but edge density **at high
  contrast** separates them by 8.8×.

**Nothing in the literature measures Tonalist edges.** Everything numeric below is this report's.

---

## 2. Measured: 15 Tonalist canvases against 15 photographs

### 2.1 Method, stated so it can be reproduced

Every image is cropped **3% off each edge** (removing frame lips, canvas tacking margins and
museum-photograph surrounds; every image was inspected as a contact sheet before and after) and
resampled so its **short edge is exactly 800 px**, so one pixel is the same fraction of picture in
both corpora. Conversion to CIELAB is the app's own `PalettePhotoConverter.RgbToLab`. Definitions:

- **g(x,y)** — local colour change: the larger of the CIELAB ΔE to the right neighbour and to the
  neighbour below. **D2 / D5 / D10 / D20** are the share of pixels with g ≥ 2, 5, 10, 20.
- **Edge span** — an 8-px horizontal or vertical run whose end-to-end ΔE is ≥ 12. **Edge-span
  share** is what fraction of all spans qualify.
- **Edge width** — for a qualifying span, (end-to-end ΔE) ÷ (largest single adjacent step inside
  it), clamped to [1, 8]. A step gives 1; a linear ramp of width *k* gives *k*. Classified
  **hard** < 1.5, **firm** 1.5–3, **soft** 3–6, **lost** ≥ 6.
- **Slope** — least-squares fit of log amplitude against log radial frequency, over bins 3–128 of a
  512² Hann-windowed centre crop of the L\* plane.

This width estimator replaced a Gaussian-scale-ratio one that fired on canvas texture and reported
nonsense; the span form only admits places where a boundary of ΔE ≥ 12 actually exists.

### 2.2 The distributions `[verified — computed 2026-07-31]`

Means over 15 images each, standard deviations in brackets.

| | Tonalist canvases | Photographs | ratio |
|---|---|---|---|
| Mean local change g (ΔE) | 4.52 (1.92) | 7.61 (3.38) | 0.59 |
| **D2** — pixels with g ≥ 2 | **74.22% (18.21)** | **62.06% (18.85)** | **1.20** |
| D5 | 29.41% (19.04) | 42.73% (18.21) | 0.69 |
| D10 | 8.05% (8.21) | 25.68% (13.74) | 0.31 |
| **D20** | **1.16% (2.15)** | **10.20% (7.34)** | **0.11** |
| Edge-span share (ΔE ≥ 12 over 8 px) | 10.80% (7.95) | 32.62% (12.75) | 0.33 |
| Edge width: hard / firm / soft | 42.9 / 49.6 / 7.4% | 59.1 / 32.9 / 8.0% | — |
| Median edge width (px) | 1.66 (0.35) | 1.36 (0.32) | 1.22 |
| Amplitude-spectrum slope | −1.113 (0.174) | −1.031 (0.175) | — |
| L\* 5–95 spread | 39.5 (14.7) | 67.2 (13.7) | 0.59 |
| L\* 5th / 95th percentile | 22.2 / 61.7 | 14.0 / 81.2 | — |
| Median C\*ab | 15.7 (9.8) | 16.0 (6.6) | 0.98 |

**Read the D-row as a shape, not a level.** The two corpora cross over between ΔE 2 and ΔE 5. A
Tonalist canvas has **more** places where the colour changes a little (74.2% vs 62.1%) and
**dramatically fewer** where it changes a lot (1.16% vs 10.20%). That is not "smoother". It is a
*compressed* contrast distribution — lots of small incident, almost no large incident. Any
operator that reduces both ends proportionally moves away from it in one dimension while moving
toward it in the other, which is the whole difficulty in §5.

Per-canvas, so the spread is visible (the two Whistler nocturnes and the Blakelock are the poles):

| Work | g | D2 | D5 | D10 | D20 | slope | edge spans | med width |
|---|---|---|---|---|---|---|---|---|
| Harrison, *Fifth Avenue at Twilight* (c.1910) | 2.01 | 32.2 | 7.2 | 1.5 | 0.13 | −1.30 | 4.95 | 1.97 |
| Eaton, *Edge of the Forest* (1903) | 4.14 | 74.0 | 28.6 | 5.4 | 0.19 | −1.10 | 14.77 | 1.78 |
| Inness, *Early Autumn, Montclair* (1891) | 6.33 | 92.5 | 52.4 | 15.3 | 1.68 | −1.34 | 22.91 | 1.47 |
| Inness, *Sunset in the Woods* (1891) | 2.30 | 45.0 | 5.7 | 0.8 | 0.08 | −1.39 | 3.68 | 2.25 |
| Inness, *The Home of the Heron* (c.1893) | 3.66 | 76.8 | 21.1 | 2.0 | 0.06 | −1.27 | 4.98 | 1.87 |
| Ranger, *The Path through the Woods* | 6.09 | 89.4 | 47.3 | 14.8 | 2.01 | −1.04 | 18.19 | 1.34 |
| Martin, *Landscape* (Cleveland) | 3.82 | 68.9 | 20.0 | 5.7 | 0.84 | −1.04 | 14.51 | 1.62 |
| **Whistler, *Nocturne: Blue and Gold — Southampton Water* (1872)** | 3.38 | 78.0 | 14.9 | 1.1 | **0.06** | −0.83 | **1.01** | 1.61 |
| Whistler, *Nocturne: Blue and Silver — Battersea Reach* | 7.11 | 92.9 | 63.2 | 21.7 | 1.23 | −0.87 | 8.83 | 1.11 |
| Twachtman, *Winter Harmony* (c.1890–1900) | 2.93 | 62.7 | 12.4 | 0.8 | **0.00** | −1.32 | 3.85 | 2.01 |
| **Blakelock, *Moonlight*** | 9.17 | 91.8 | 62.0 | 28.6 | **8.82** | −0.92 | 29.42 | 1.22 |
| Dewing, *The Recitation* (1891) | 2.37 | 52.2 | 4.9 | 0.5 | 0.03 | −1.22 | 2.10 | 2.26 |
| Dewing, *The Green Dress* (c.1910, pastel) | 4.84 | 91.1 | 32.4 | 7.8 | 0.41 | −1.06 | 7.61 | 1.33 |
| Tryon, *November Morning* | 4.81 | 86.4 | 36.5 | 6.7 | 0.26 | −0.97 | 14.92 | 1.68 |
| Whistler, *Nocturne in Black and Gold* (1875) | 4.88 | 79.5 | 32.6 | 8.2 | 1.65 | −1.03 | 10.25 | 1.43 |

Blakelock's *Moonlight* is the outlier at D20 8.82% — a black tree silhouette against a lit sky is
the one Tonalist composition that *needs* a hard edge, and it is the only canvas in the corpus
whose figures reach a photograph's. It is also the corpus's strongest evidence that the small
number of hard edges is a *choice*, not a limitation of the paint.

### 2.3 Lost-and-found, measured: Tonalist edges are low-contrast, not wide

The classical account is that a good painting has few sharp edges and mostly soft or lost ones. On
this corpus **the first half is true and the second half is false as usually stated.**

- **Few sharp edges: confirmed, with a number.** Hard edges (width < 1.5 px within a ΔE ≥ 12 span)
  occupy `10.80% × 42.9% = 4.6%` of a Tonalist canvas's spans, against `32.62% × 59.1% = 19.3%` of
  a photograph's — **4.2× fewer**. `[verified]`
- **Mostly soft or lost: refuted.** Only **7.4%** of the canvases' qualifying edges are soft
  (width 3–6 px) and **0.0%** are lost (≥ 6 px) — statistically identical to the photographs'
  8.0% and 0.03%. The median Tonalist edge width is **1.66 px**, against a photograph's 1.36 px.
  A Tonalist boundary is about **0.3 px wider** than a photograph's at the same picture scale — a
  factor of 1.22, against the 8.8× separation on contrast. `[verified]`
- **What actually differs is the contrast carried across the boundary**, which is Harrison's own
  instruction (§1.1d) rather than the modern blogs' "soften the edge". Where a photographic
  boundary carries ΔE 20 or more 10.2% of the time, a Tonalist one does 1.16% of the time.

**Consequence for the pipeline, and it is the most useful sentence in this report: "soft edge" for
Tonalism means *low ΔE across a narrow transition*, not *a wide transition*.** A low-pass filter
produces the second. The five-slot pipeline's tools for the first are the Lab remap's contrast and
chroma, which Tonalism already runs at the app's lowest settings.

### 2.4 How much of it is just tonal compression? Most of it

Rescale each image's local contrast by 60 ÷ (its own L\* 5–95 spread), so both corpora are compared
at a common tonal range, and re-measure. `[verified]`

| | canvases | photographs |
|---|---|---|
| Raw D10 / D20 | 8.05% / 1.16% | 25.68% / 10.20% |
| **Range-normalised D10 / D20** | **22.43% / 4.34%** | **23.69% / 8.45%** |
| Range-normalised mean g | 7.33 | 6.87 |

The ΔE-10 gap of 3.2× **vanishes** (22.43 vs 23.69) and mean local contrast inverts — the canvases
are marginally busier than the photographs once you correct for range. Only the extreme tail
survives, at 1.9× rather than 8.8×.

**So roughly four-fifths of Tonalism's measured edge softness is the same fact as its narrow value
range**, which the parent report's style table already had as a separate row
(`02-styles-and-movements.md` §6: value window [35, 70], "~35 L\* of total range"). That estimate
is now measured: **39.5 ± 14.7 L\*** across 15 canvases, within 13% of the guess. The same table's
relayed claims that "the brightest areas… are far from pure white" and "the darkest shadows also
stop well short of pure black" are confirmed numerically: mean L\*95 is **61.7** (not ~100) and
mean L\*5 is **22.2** (not ~0), against photographs' 81.2 and 14.0. `[verified]`

---

## 3. Is there an edge hierarchy on the canvases?

Harrison says both patterns are legitimate (§1.1c). Radial quartiles of distance from the image
centre, normalised by the half-diagonal, over each corpus: `[verified]`

| band (0 = centre) | canvases D10 | canvases D20 | photos D10 | photos D20 |
|---|---|---|---|---|
| 0 | 9.92% | 1.72% | 30.19% | 12.62% |
| 1 | 9.05% | 1.59% | 26.95% | 10.77% |
| 2 | 7.59% | 0.98% | 24.59% | 9.56% |
| 3 | 6.20% | 0.51% | 22.83% | 8.95% |
| **centre ÷ outer** | **1.60** | **3.37** | **1.32** | **1.41** |

Both corpora fall off toward the frame — photographs do too, because people centre their subjects.
The finding is the *differential*: **Tonalist canvases shed high-contrast edges toward the
periphery about 2.4× more steeply than photographs do** (3.37 vs 1.41 on D20).

Per canvas, the centre÷outer D10 ratio runs **0.15 to 6.86, median 1.83**, and is below 1 on five
of fifteen: `[verified]`

| ratio > 1.8 (focal) | ratio 1.0–1.8 (weak) | ratio < 1 (inverted) |
|---|---|---|
| Whistler *Black and Gold* **6.86**, Eaton 3.50, Dewing *Green Dress* 3.45, Inness *Heron* 2.63, Martin 2.16, Blakelock 1.92, Ranger 1.85, Inness *Sunset* 1.83 | Whistler *Southampton Water* 1.34, **Whistler *Battersea Reach* 1.08** | Inness *Montclair* 0.74, Dewing *Recitation* 0.71, Tryon 0.56, Twachtman 0.50, **Harrison *Fifth Avenue* 0.15** |

Two of these are worth stating plainly.

- **The two Whistler nocturnes with no focal incident measure as uniform (1.08, 1.34)** — exactly
  the case Harrison names and exactly the treatment he prescribes for it. The third Whistler,
  *Nocturne in Black and Gold*, has a rocket burst in the middle of the canvas and returns the
  corpus's strongest hierarchy at 6.86. The theory and the measurement agree per picture.
- **Harrison's own canvas is the most strongly inverted in the corpus, at 0.15.** *Fifth Avenue at
  Twilight* puts its sharpest structures — bare tree branches — against the frame edges and keeps
  the centre a soft luminous haze. The man who wrote the focal theory did not follow it here.

**Ruling: edge hierarchy is a real, measurable Tonalist tendency and is not this style's defining
device.** A centre-clicked focal blur would be actively wrong for a third of this corpus and
neutral for two more. §7 costs it accordingly.

---

## 4. What the shipped row actually does to edges

The audited row is unchanged from `HEAD`: MarkScale **1.2**; pre-map `EdgePreservingFloor` strength
**2.0**, edge threshold at the stage default **0.05**; `ToneAndChromaRemap` contrast **0.55**, key
**4.0**, chroma **0.45**; `MotherColourTransform` fraction **0.30**; `NearestQuantiser`; **no
post-map stage**. `[verified — `StyleRegistry.cs:42–64`]`

> **Working-tree caveat, and it matters for the table below.** The tree carries uncommitted changes
> to `SmallRegionMerge.cs` (rewritten) and to the **Post-Impressionism** row, which now registers
> `SmallRegionMerge` and runs chroma 1.45. Every figure below is measured against that tree, not
> against `HEAD`. The Tonalism row itself is untouched, so Tonalism's own numbers are `HEAD`
> numbers; Post-Impressionism's are not, which is why it appears here at 0.02% sub-mark where the
> Post-Impressionism round published 32.2%. `[verified — `git diff`]`

### 4.1 The five styles, on 15 photographs, each at its own default mark `[verified]`

| | Realism | **Tonalism** | Fauvism | Post-Imp.\* | Abstract |
|---|---|---|---|---|---|
| MarkScale | 1.0 | **1.2** | 1.3 | 1.6 | 2.5 |
| mark (px, 800-px short edge) | 4.87 | **5.84** | 6.33 | 7.79 | 12.17 |
| Four-connected regions | 321,910 | **183,260** | 5,122 | 2,234 | 191 |
| **Pixels below own mark²** | **51.30%** | **33.83%** | 1.81% | 0.02% | 0.01% |
| Boundary pairs per 1000 px | 948.6 | **649.1** | 218.4 | 196.9 | 55.5 |
| Pixels adjacent to a colour change | 69.5% | **53.5%** | 30.3% | 27.5% | 7.9% |
| Pixels within ½ mark of a boundary | 84.3% | **77.9%** | 75.1% | 73.3% | 34.9% |
| Mean boundary ΔE | 10.00 | **5.73** | 21.90 | 13.60 | 21.71 |
| Boundary pairs below ΔE 2 | 6.1% | **12.8%** | 3.9% | 4.4% | 0.0% |
| Boundary pairs at ΔE ≥ 10 | 32.6% | **14.1%** | 45.7% | 37.4% | 84.3% |
| Distinct colours | 926 | **350** | 393 | 337 | 9 |

\* working tree, with `SmallRegionMerge` registered.

Two readings.

**On contrast, Tonalism is doing its job and is the app's softest style outright** — lowest mean
boundary ΔE (5.73), highest share of sub-ΔE-2 boundaries (12.8%), lowest share of hard boundaries
(14.1%). No other style is close.

**On density, Tonalism is now the second-worst style in the app** — 33.83% of its pixels sit in
regions smaller than one of its own brushmarks squared, behind only Realism, and 649 boundary
pairs per 1000 px. It and Realism are the only two styles with an empty post-map slot, and Realism
at least asks for the smallest mark. **Tonalism asks for a 1.2× brush and does less than any other
style to produce one.**

### 4.2 Cross-check against the Post-Impressionism round `[verified]`

My corpus and working resolution differ from that round's (15 photographs at an 800-px short edge
against their 14 at native size), so absolute levels are higher — a smaller working image makes
the mark ≈ 5 px and sensor noise proportionally larger. The **ratios reproduce almost exactly**,
which validates the method across sessions and corpora:

| Tonalism ÷ Realism | this round | Post-Impressionism round |
|---|---|---|
| Pixels below own mark² | 0.660 | 0.646 |
| Boundary pairs per 1000 px | 0.684 | 0.674 |
| Mean boundary ΔE | 0.573 | 0.564 |
| Pixels adjacent to a colour change | 0.770 | 0.760 |

### 4.3 The relative-hardness result

Section 2.4's normalisation, applied to the rendered output rather than to a source image:
`[verified]`

| | L\*5 | L\*95 | **L\* range** | median C\* | raw D10 | raw D20 | **norm D10** | **norm D20** |
|---|---|---|---|---|---|---|---|---|
| Tonalist canvases | 22.2 | 61.7 | **39.5** | 15.7 | 8.05 | 1.16 | 22.43 | **4.34** |
| Source photographs | 14.0 | 81.2 | **67.2** | 16.0 | 25.68 | 10.20 | 23.69 | **8.45** |
| **Tonalism as shipped** | **44.4** | **70.6** | **26.3** | **7.0** | 8.50 | 1.49 | 24.83 | **11.52** |
| Tonalism, floor ε 0.15 | 44.7 | 69.6 | 25.0 | 7.3 | 3.05 | 0.18 | 14.32 | 5.85 |

Three things fall out.

1. **The shipped row over-compresses.** L\* range 26.3 against the canvases' 39.5 — a third
   narrower — and it does it asymmetrically: the dark end sits at L\* **44.4** where the canvases
   sit at 22.2. That is the `MotherColourTransform` whitening the Post-Impressionism round
   documented (`MostNeutralPaintIndex()` returns Titanium White; at fraction 0.30 the darkest
   achievable colour rises L\* 11.0 → 38.3). Measured against real canvases, **Tonalism's blacks
   are about 22 L\* too light.** `[verified for my figures; `[relayed]` for the mechanism, from
   that round]`
2. **It over-desaturates by half.** Median C\* 7.0 against the canvases' 15.7. Tonalism's canvases
   are *not* low-chroma relative to photographs — median C\* 15.7 vs 16.0, essentially identical.
   The style's chroma 0.45 has no support in this corpus. (Colour is another track's subject; I
   record it because it is the same measurement.)
3. **Inside its own range the shipped render bands harder than the photograph it came from** —
   normalised D20 **11.52%** against the source's 8.45% and the canvases' 4.34%, a **2.7× excess
   over target**. Tonalism reads soft because it is flat, and where a boundary does occur the
   nearest-candidate snap makes it proportionally *larger* than the one in the photograph.

**That is the diagnosis this report exists to deliver.** The style's edge problem is not
insufficient smoothing. It is that a third of its tonal range has been given away for softness that
was already free from the contrast remap, and the quantiser then bands inside what is left.

---

## 5. The Gaussian question, re-opened for this one style

The parent README's strongest result is that a Gaussian pre-blur should be replaced by an
edge-preserving filter, reached by four independent tracks, with track 2's Graham & Field argument
as the load-bearing one: art has a shallower mean amplitude-spectrum slope than natural scenes
(−1.21 ± 0.017 vs −1.40 ± 0.017), and blur **steepens** the spectrum, so blurring a photo makes it
statistically less painting-like. Tonalism is the obvious candidate for an exception. I tested it
three ways and the answer is "no exception, but the stated reason is wrong".

### 5.1 The spectral argument does not carry `[verified]`

Measured with one estimator over both corpora and every filter (512² Hann-windowed centre crop of
the L\* plane, bins 3–128):

| | slope |
|---|---|
| Tonalist canvases | **−1.113 ± 0.174** |
| Photographs | **−1.031 ± 0.175** |
| Gaussian radius 2 / 5 / 10 on the photographs | −1.138 / −1.537 / −2.414 |
| Guided filter, strength 2 / 5 at ε 0.05 | −1.235 / −1.467 |
| Guided filter, strength 2 / 5 at **ε 0.30** | **−2.305 / −2.813** |
| Tonalism as shipped, rendered | −1.119 ± 0.175 |

Four things, and each is a problem for the argument as stated.

1. **The sign is backwards on this corpus.** Tonalist canvases are *steeper* than photographs, not
   shallower. The effect is small (d ≈ 0.47, n = 15 each, distributions heavily overlapping) and I
   would not claim the difference is real — but I can say the corpus gives no support at all for
   "blur moves a photograph away from painting statistics" *for this style*, and mild support for
   the opposite.
2. **"Edge-preserving" is not "spectrum-preserving".** The guided filter at ε 0.30 steepens the
   spectrum to −2.31 (strength 2) and −2.81 (strength 5) — **worse than a radius-10 Gaussian**.
   The mechanism is obvious once measured: flattening large areas to constant removes high
   frequencies whether or not a handful of edges survive. Any recommendation phrased as "use an
   edge-preserving filter *because* blur steepens the spectrum" is not sound; what steepens the
   spectrum is how much smoothing you do, not which smoother you use.
3. **The statistic has almost no discriminating power here.** The two corpora's SDs are identical
   to three decimals (0.174, 0.175) and their ranges overlap almost completely (canvases −0.83 to
   −1.39; photographs −0.76 to −1.34). This is also a **negative result for Mather 2014's
   "artworks occupy a narrower band of spectral slopes than matched photographs"**, which the
   parent README flags as its highest-priority verification debt and which currently carries the
   lead recommendation: on this pairing the bands are exactly as wide as each other.
4. **The shipped render already sits on the canvas mean** — −1.119 against −1.113 — while looking
   nothing like a Tonalist painting (§4.3, and I looked at both). A statistic a bad render already
   satisfies cannot arbitrate between filters.

**My slopes are not on Graham & Field's scale** — I fit L\* rather than linear luminance, on JPEG
web reproductions, over my own frequency window, and I could not open their paper to check their
protocol (§10). Read the *differences between rows*, never the absolute values.

### 5.2 Judged against the canvases instead, at the pre-map buffer `[verified]`

The honest test for a pre-map filter is how close it brings a photograph to the Tonalist canvas
statistics. Distance is the RMS of the seven z-scores {D2, D5, D10, D20, slope, hard-share, median
width}, each in units of the **canvas corpus's own SD**. Means over the 15 photographs. Guided
radius is the shipped `FloorRadius(mark)`.

| filter | z-dist | D2 | D5 | D10 | D20 | slope | hard | med width |
|---|---|---|---|---|---|---|---|---|
| *canvas target* | *0* | *74.22* | *29.41* | *8.05* | *1.16* | *−1.11* | *42.93* | *1.66* |
| **Gaussian radius 2** | **0.78** | 53.78 | 29.65 | 11.51 | 1.74 | −1.14 | 24.53 | 2.13 |
| floor ε 0.05 s2 + Gaussian 1 | 1.11 | 37.69 | 23.37 | 13.26 | 5.17 | −1.24 | 45.64 | 1.68 |
| **floor ε 0.05 s2 (shipped)** | **1.14** | 37.71 | 23.52 | 13.60 | 5.50 | −1.24 | 47.00 | 1.65 |
| floor ε 0.05, radius ×4 | 1.19 | 41.96 | 27.31 | 15.48 | 5.96 | −1.15 | 55.31 | 1.42 |
| floor ε 0.08 s2 | 1.32 | 30.13 | 15.77 | 8.49 | 3.26 | −1.40 | 35.24 | 2.15 |
| floor ε 0.05 s2 + Gaussian 2 | 1.41 | 33.11 | 16.49 | 6.74 | 1.15 | −1.33 | 18.90 | 2.45 |
| floor ε 0.05 s5 | 1.53 | 21.12 | 11.94 | 6.98 | 2.93 | −1.47 | 36.09 | 2.16 |
| Gaussian radius 3 | 1.59 | 47.05 | 19.78 | 5.07 | 0.24 | −1.24 | 8.47 | 2.81 |
| Gaussian radius 1 | 1.76 | 62.03 | 42.41 | 24.96 | 9.51 | −1.04 | 57.46 | 1.39 |
| **unfiltered photograph** | **1.88** | 62.06 | 42.73 | 25.68 | 10.20 | −1.03 | 59.05 | 1.36 |
| Gaussian radius 4 | 2.33 | 40.63 | 13.15 | 2.21 | 0.00 | −1.38 | 2.40 | 3.43 |
| floor ε 0.15 s2 | 2.78 | 20.62 | 6.84 | 2.91 | 0.82 | −1.77 | 15.27 | 3.47 |
| floor ε 0.05 s2 + Gaussian 4 | 2.82 | 26.55 | 8.51 | 1.54 | 0.00 | −1.56 | 1.87 | 3.72 |
| Gaussian radius 8 | 4.61 | 22.17 | 2.83 | 0.01 | 0.00 | −2.11 | 0.01 | 5.12 |
| floor ε 0.30 s2 | 4.94 | 14.75 | 1.86 | 0.24 | 0.01 | −2.30 | 0.69 | 5.20 |
| floor ε 0.30 s5 | 6.47 | 6.37 | 0.10 | 0.00 | 0.00 | −2.81 | 0.00 | 6.24 |

Four readings, and the third is the ruling.

1. **A small Gaussian is the single closest filter to the Tonalist canvas statistics of everything
   tested** — radius 2, z 0.78, comfortably ahead of the shipped floor at 1.14. It reproduces D5
   (29.65 vs 29.41) and the slope (−1.14 vs −1.11) almost exactly. **So Tonalism genuinely is the
   style where a Gaussian is defensible on its own terms.**
2. **Its whole advantage is D2, and the floor destroys D2 first.** The Gaussian's z-score on D2 is
   −1.12 where the floor's is −2.00; on every other measure they are comparable. But the floor is
   mandatory — no registered style may omit it, and without it a noisy photograph puts 44% of its
   pixels into regions of ≤ 4 px `[relayed — `GuidedFilter`'s own doc comment and the parent
   README's local measurement]`. **A Gaussian applied after the floor cannot restore texture the
   floor has already removed**, and the table shows it: floor + Gaussian 1 is 1.11 against the
   floor's 1.14 (inside the noise), and floor + Gaussian 2 is *worse* at 1.41.
3. **Ruling: do not add a Gaussian to Tonalism.** Not because it is un-painting-like — that
   argument fails here — but because after the mandatory floor it buys nothing measurable at
   radius 1 and costs at radius 2 and above, and because at any radius that visibly softens
   (≥ 3) it overshoots the measured median edge width by 1.7–3×, against a canvas target the
   shipped floor already hits to two decimal places (1.65 vs 1.66).
4. **Every strong setting is worse than doing nothing.** The unfiltered photograph is at 1.88;
   Gaussian 4 is 2.33, ε 0.15 is 2.78, ε 0.30 is 4.94. The distance metric is dominated in those
   rows by median width, whose canvas SD is small (0.35 px) precisely because real canvases are
   consistent about it. **There is no amount of smoothing that makes a photograph look like a
   Tonalist canvas**, and the parameter range where smoothing helps at all is narrow.

### 5.3 Judged at the output, where the quantiser has had its say `[verified]`

Rendered through the real pipeline, means over the 15 photographs, in the same source-domain terms:

| | mean g | D2 | D5 | D10 | D20 | edge spans | hard | firm | soft | med width |
|---|---|---|---|---|---|---|---|---|---|---|
| **canvas target** | **4.52** | **74.22** | **29.41** | **8.05** | **1.16** | **10.80** | **42.9** | **49.6** | **7.4** | **1.66** |
| Realism | 7.11 | 56.91 | 41.61 | 24.26 | 11.05 | 30.66 | 69.4 | 27.9 | 2.8 | 1.18 |
| **Tonalism (shipped)** | 2.88 | 39.07 | 19.77 | **8.50** | **1.49** | **11.21** | 68.5 | 29.9 | 1.6 | 1.24 |
| Fauvism | 4.23 | 18.07 | 13.39 | 8.68 | 6.86 | 23.87 | 97.8 | 2.2 | 0.0 | 1.00 |
| Post-Impressionism\* | 2.35 | 16.26 | 11.32 | 6.40 | 3.26 | 15.13 | 95.1 | 4.9 | 0.0 | 1.00 |
| Abstract | 1.05 | 4.77 | 4.75 | 3.99 | 2.28 | 10.47 | 97.6 | 2.5 | 0.0 | 1.00 |
| Tonalism + ε 0.10 | 1.96 | 31.13 | 12.60 | 4.73 | 0.59 | 7.63 | 61.2 | 35.6 | 3.3 | 1.36 |
| Tonalism + ε 0.15 | 1.53 | 27.13 | 9.28 | 3.05 | 0.18 | 5.83 | 55.3 | 38.9 | 5.8 | 1.47 |
| Tonalism + ε 0.20 | 1.33 | 25.07 | 7.69 | 2.30 | 0.07 | 4.94 | 52.0 | 39.4 | 8.5 | 1.55 |
| **Tonalism + Gaussian 4** | 1.57 | 29.91 | 9.24 | 2.50 | 0.08 | 7.45 | **40.1** | **45.7** | **14.2** | **1.81** |
| Tonalism + Gaussian 8 | 1.23 | 24.22 | 6.58 | 1.94 | 0.05 | 4.96 | 45.9 | 34.6 | 19.3 | 1.83 |

**Shipped Tonalism is the only style in the app that lands on the canvas target for high-contrast
edge density** — D10 8.50 against 8.05, D20 1.49 against 1.16, edge-span share 11.21 against 10.80.
Realism is 3× over on D10 and 9.5× over on D20; Fauvism, Post-Impressionism and Abstract are 2–6×
over on D20 while being under on span density, which is the flat-plane signature.

Where it misses is the two things a low-pass cannot fix together: **D2 (39.07 against 74.22 — half
the low-amplitude incident)** and **the width mix (68.5 / 29.9 / 1.6 against 42.9 / 49.6 / 7.4)**.
Adding smoothing fixes the width mix — Gaussian 4 gives the closest width distribution of anything
tested — and pays for it by pushing D10 from a correct 8.50 down to 2.50. **Every softening option
trades the one thing this style has right for the one thing it has wrong.** That is a structural
statement about low-pass filters, not a tuning failure: a low-pass moves edge width up and edge
contrast down together, and the target needs width up with contrast held.

### 5.4 The knob, if you turn one: ε, not strength, not radius `[verified]`

Rendered through the real pipeline, 15 photographs, everything else at Tonalism's registered
defaults. `edge` is the shipped `EdgePreservingFloor` parameter; `gauss N` is a real `OptionalBlur`
appended after the floor, which is where `PalettePhotoConverter` puts it.

| variant | regions | below mark² | bound/1000 | transition | mean ΔE | soft <2 | hard ≥10 | colours | slope |
|---|---|---|---|---|---|---|---|---|---|
| strength 1 | 227,841 | 40.92% | 754.6 | 59.6% | 5.96 | 12.2% | 15.7% | 367 | −1.05 |
| **strength 2 (shipped)** | **183,260** | **33.83%** | **649.1** | **53.5%** | **5.73** | **12.8%** | **14.1%** | **350** | **−1.12** |
| strength 3 | 146,764 | 27.99% | 564.5 | 48.8% | 5.52 | 13.7% | 13.0% | 341 | −1.18 |
| strength 5 | 99,716 | 20.19% | 449.8 | 42.1% | 5.23 | 15.2% | 11.9% | 328 | −1.26 |
| ε 0.02 | 250,505 | 43.55% | 787.1 | 60.2% | 6.27 | 11.3% | 17.6% | 387 | −1.02 |
| ε 0.10 | 109,739 | 23.68% | 505.4 | 47.3% | 4.90 | 15.8% | 9.9% | 297 | −1.29 |
| ε 0.20 | 58,417 | 15.83% | 400.0 | 42.6% | 4.19 | 19.3% | 6.6% | 236 | −1.51 |
| ε 0.30 | 46,694 | 13.62% | 373.2 | 41.2% | 4.04 | 20.6% | 6.0% | 218 | −1.62 |
| ε 0.30, strength 5 | 27,409 | 7.85% | 292.9 | 34.7% | 3.97 | 22.2% | 6.2% | 186 | −1.64 |
| + Gaussian 2 | 130,450 | 29.44% | 565.5 | 51.1% | 4.73 | 14.9% | 8.5% | 302 | −1.19 |
| + Gaussian 5 | 70,831 | 20.45% | 445.0 | 45.8% | 4.14 | 18.4% | 6.0% | 252 | −1.43 |
| + Gaussian 10 | 41,909 | 12.62% | 357.4 | 40.1% | 4.01 | 20.8% | 6.1% | 213 | −1.65 |

- **Strength is the expensive weak knob.** Going 1 → 5 is five guided-filter passes instead of one
  and takes sub-mark share 40.9% → 20.2% and hard-boundary share 15.7% → 11.9%.
- **ε is the cheap strong one.** At one-fifth the cost (strength fixed at 2), ε 0.02 → 0.30 takes
  sub-mark share 43.6% → 13.6% and hard share 17.6% → 6.0%. **This is the third independent
  confirmation that ε, not radius and not iteration count, is the guided filter's softness
  parameter** (see §6.2 for the radius result).
- **At matched paintability, the guided filter's edge preservation works *against* this style.**
  strength 5 and Gaussian 5 land at the same sub-mark share (20.19% vs 20.45%); the Gaussian
  delivers mean boundary ΔE **4.14 against 5.23** and hard-boundary share **6.0% against 11.9%**.
  That is the whole of the "edge-preserving is wrong for the style that wants edges gone"
  intuition, quantified — and §5.2 is why the answer is still "raise ε" rather than "add a blur":
  ε 0.20 matches the Gaussian's contrast figures (4.19, 6.6%) at *better* paintability (15.8% vs
  20.5%), at O(n) cost independent of window size, and with a smaller width overshoot.

---

## 6. Edge hierarchy, and the knob that does not exist

### 6.1 What the parent lever says

`03-brushwork-and-edges.md` lever 1 — its own top pick, "very high payoff, very low cost" — is a
spatially varying *blur radius* driven by a focal point, built as 3–5 filtered copies at geometric
radii lerped per pixel in linear light, ~120 lines. It was written when the pre-map stage was
`GaussianBlur`. The Post-Impressionism round found that the parameter is wrong for a guided filter
and measured ≈0% for radius against −17% for the edge threshold.

### 6.2 Reproduced on Tonalism `[verified — computed 2026-07-31]`

Four filtered copies, a flat sharp core out to 0.35 of the focal span, a ramp to the corner, lerped
per pixel in linear light using the real `LinearPlanes` encode/decode; each copy produced by the
real `GuidedFilter.Apply` or `GaussianBlur.Apply`. Focus fixed at the image centre. Change in mean
boundary ΔE per radial quartile against the uniform render, means over 15 photographs:

| variant | what varies | band 0 (focal) | band 1 | band 2 | band 3 (edge) | sub-mark share | hard ≥10, band 3 |
|---|---|---|---|---|---|---|---|
| uniform | — | — | — | — | — | 32.74% | 11.8% |
| **focalRadius** | guided-filter window radius | **+0.0%** | **+0.1%** | **+1.1%** | **+0.3%** | 33.53% | 12.4% |
| focalGauss | Gaussian radius after the floor | +0.0% | −4.3% | −16.8% | −22.4% | 28.17% | 5.3% |
| **focalEdge** | guided-filter **edge threshold** | **+0.0%** | **−5.2%** | **−19.4%** | **−25.2%** | **23.57%** | **5.0%** |

**The radius variant does nothing on Tonalism, exactly as on Post-Impressionism.** This is now
measured twice, on two styles, on two corpora, by two sessions. The parent lever's parameter is
wrong and should be corrected in the report itself rather than only in the per-style rounds.

The edge-threshold variant does what the lever was for: focal band held to within a rounding error,
periphery down a quarter, outer-band hard-edge share more than halved, and paintability *improved*
by 9 points rather than paid for. The Gaussian variant is close behind on contrast (−22.4% vs
−25.2%) and clearly behind on paintability (28.2% vs 23.6%).

### 6.3 Is it this style's defining device? No — and §3 is why

The device works. Its warrant under this label is split down the middle:

- **For:** Harrison devotes his chapter to it and derives it from foveal vision (§1.1b–c); on the
  corpus, canvases shed high-contrast edges toward the frame 2.4× faster than photographs do (§3).
- **Against:** five of fifteen canvases run the *other way*, including Harrison's own at 0.15; the
  two Whistler nocturnes without a focal incident measure as uniform (1.08, 1.34); and Harrison
  explicitly prescribes uniform treatment for that case. A centre default would be wrong on a third
  of the corpus. `[verified]`
- **Cost.** Four guided-filter copies plus a three-plane lerp. The Post-Impressionism round
  measured the same construction at **4× render time** on a 1920×1200 photograph
  (2,570 ms → 10,251 ms) `[relayed]`; Tonalism's floor runs at strength 2 rather than 3, so its
  base is cheaper and the multiplier applies to a smaller number, but the shape is the same.
- **Cache.** A radial parameter is a *pre-map* operation on the pixel buffer, so it does **not**
  break `PalettePhotoConverter`'s 6-bit-per-channel colour cache — the cache keys the mapping, and
  the mapping still sees only colours. The ~3-bits-of-radial-band extension (~8 MB) the parent
  README budgets is needed only if the *quantiser* becomes position-dependent, which this lever
  does not require. `[verified — `StylePipeline.Render` runs `PreMap` stages on the buffer before
  `ResolveOncePerColour`, and only `IQuantiser.IsPositionDependent` switches to the per-pixel
  path]` **That is a real saving against the parent README's own estimate for this item.**

**Ruling: build it, but as a shared cross-style stage with a user-clicked focus and a default of
"off", not as Tonalism's signature.** Tonalism's signature is uniform low contrast; the hierarchy
is a per-picture option the movement itself treats as optional.

---

## 7. Recommended build items, ranked by payoff ÷ cost

Line counts are C#-from-scratch estimates in the style of the existing `Imaging/Styles/Stages/`
files, excluding UI.

### 1. Give back the tonal range Tonalism is spending on softness it already has

**Slots 2 and 3. Zero new code — three numbers in `StyleRegistry.cs` — plus ~15 lines if
`MotherColourTransform` is to stop whitening.**

The shipped row compresses to L\* range **26.3** with its dark end at L\* **44.4**, against measured
canvases at **39.5** and **22.2**, and to median C\* **7.0** against **15.7** (§4.3). It is paying
for softness twice: once in `ToneAndChromaRemap` contrast 0.55, and again in
`MotherColourTransform` fraction 0.30, which the Post-Impressionism round showed resolves to
Titanium White and therefore lifts the darkest achievable colour from L\* 11.0 to 38.3. The
softness was already delivered — Tonalism has the app's lowest mean boundary ΔE by a factor of two
(§4.1) — so the compression is buying nothing and costing the whole dark end of the picture.

Restoring range makes the relative-hardness figure *worse* on its own (normalised D20 scales with
range), which is why this pairs with item 2 rather than shipping alone. From the measured ladder,
range 39.5 with ε ≈ 0.08–0.10 should land near the canvas envelope: at ε 0.15 the render's raw D20
is 0.18% and at ε 0.05 it is 1.49%, against the canvas target 1.16%, and a 1.5× range restoration
scales both by roughly that factor. `[verified for the ladder; `[inferred]` for the combination]`

**Verification.** Rendered L\* 5–95 spread over a photographic corpus must land in 35–45; median
C\* in 12–20; range-normalised D20 must come *down* from 11.52% toward the canvas 4.34% rather than
up. Regenerate `Tests/Golden/Tonalism.png` and look at it.

### 2. Raise `EdgePreservingFloor`'s edge threshold for Tonalism, and leave strength alone

**Slot 1. One line in `StyleRegistry.cs`: `(tonalismFloor, "edge", 0.10)`.**

Measured over 15 photographs (§5.4): sub-mark share **33.83% → 23.68%**, mean boundary ΔE 5.73 →
4.90, hard-boundary share 14.1% → 9.9%, at **no extra cost** — ε is a scalar in the same two
passes. Reaching the same paintability through `strength` costs five passes instead of two and
leaves the picture harder (20.19% at strength 5, with mean ΔE 5.23 and hard share 11.9%).

Stop at 0.10. ε 0.20 and above pushes the render's D10 to 2.30% against a canvas target of 8.05%
and the median edge width to 1.55 and rising (§5.3) — it is smoothing past the target, and the
z-distance table (§5.2) puts ε 0.15 and ε 0.30 *behind an unfiltered photograph*.

**Verification.** `FractionInRegionsSmallerThan(mark²)` on a real photograph, not on
`Tests/Golden` — the gradient understates this defect by about a factor of two and has produced a
false conclusion in three consecutive rounds. Rendered D10 must stay in 6–11% and D20 in 0.8–2%.

### 3. Register `SmallRegionMerge` on Tonalism

**Slot 5. ~6 lines in `StyleRegistry.cs`.**

Tonalism is the app's second most fragmented style (33.83% of pixels below its own mark²), and one
of only two with an empty post-map slot. The stage exists, is invariant-safe by the `Refine`
signature, is registered on three other styles, and has just been rewritten in the working tree to
converge in one sweep. Item 2 takes 33.8% → 23.7%; a merge should take the remainder, and unlike a
stronger floor it costs nothing in the picture's interior.

One caution specific to this style, from §5.3: `SmallRegionMerge` **raises** mean boundary ΔE,
because it deletes weak boundaries and keeps strong ones — the Post-Impressionism round measured
8.76 → 11.86 over four passes. Tonalism is the style least able to afford that. Register it, then
re-measure D20 against the 1.16% canvas target and back item 2's ε off if the merge has already
bought the paintability.

**Verification.** Sub-mark share on real photographs; and mean boundary ΔE must not rise above
about 7.

### 4. A focal edge-threshold floor, as a shared stage defaulting to off

**Slot 1. ~120 lines, wrapping `EdgePreservingFloor`.**

Four guided-filter copies at geometrically spaced *edge thresholds* (ε, 2.5ε, 6.25ε, 15.6ε), lerped
per pixel in linear light against a radial falloff from a user-clicked focus with a flat sharp
core. Measured on Tonalism: focal band held to +0.0%, outer band −25.2%, sub-mark share 32.7% →
23.6%, outer-band hard share 11.8% → 5.0% (§6.2). Cost ≈ 4× the floor; the colour cache is
**not** affected, contrary to the parent README's budget for this item (§6.3).

Ranked fourth, not first, because §3 shows a third of the Tonalist corpus runs the hierarchy
backwards and the movement's own theorist prescribes uniform treatment for the nocturne case.
It earns its place as a cross-style device, not as this style's identity.

**Verification.** Focal-disc mean boundary ΔE within a few percent of the uniform render's while
the outer band's falls; a zero focal radius must leave the buffer byte-identical to
`EdgePreservingFloor`'s output.

---

## 8. Where this corrects or extends prior research

**Corrects:**

1. **The Graham & Field argument does not support "replace the Gaussian" for Tonalism, and
   "edge-preserving" does not imply "spectrum-preserving".** §5.1. Tonalist canvases measure
   *steeper* than photographs (−1.113 vs −1.031, overlapping distributions), and the guided filter
   at ε 0.30 steepens more than a radius-10 Gaussian. The parent README's conclusion survives on
   other grounds (§5.2); the reason should not be repeated as stated.
2. **Mather 2014's "artworks occupy a narrower band of spectral slopes than matched photographs"
   does not reproduce here.** §5.1. SDs 0.174 and 0.175 on n = 15 each. This is the parent README's
   own highest-priority verification debt and it currently carries the lead recommendation.
3. **Report 03 lever 1 names the wrong parameter — confirmed a second time, on a second style.**
   §6.2. Radius moves the four radial bands +0.0 / +0.1 / +1.1 / +0.3%. The correction should move
   from the Post-Impressionism round's README into `03-brushwork-and-edges.md` itself.
4. **The parent README over-budgets the focal lever.** §6.3. A radially varying *pre-map* filter
   does not break the 6-bit colour cache, because the cache keys the mapping and the mapping still
   sees only colours. The "~3 bits of radial band, ~8 MB" cost applies to a position-dependent
   *quantiser*, not to this lever.
5. **"Tonalism has no spatial component" is too strong.** `02-styles-and-movements.md` §6 calls it
   "the most achievable style… no spatial or semantic component at all". The style's *dominant*
   property is indeed pointwise — §2.4 measures four-fifths of its edge softness as tonal-range
   compression, which vindicates the verdict's substance — but the movement's own treatise devotes
   a chapter to a spatial device (§1.1), and it is measurable on the canvases (§3).

**Extends / confirms:**

- **`02-styles-and-movements.md` §6's value-range estimate is now measured.** The preset table's
  [35, 70] window (35 L\* wide) against a measured **39.5 ± 14.7** over 15 canvases, and the
  relayed "brightest far from white / darkest well short of black" against measured L\*95 **61.7**
  and L\*5 **22.2**. `[verified]`
- **The same section's low-chroma premise does not hold as a comparison against photographs.**
  Median C\* is **15.7** on the canvases and **16.0** on the photographs — identical. Tonalist
  colour is low-chroma relative to *its own* narrow value range, not relative to a photograph.
- **The `MotherColourTransform` whitening the Post-Impressionism round found is measured in the
  output.** The shipped Tonalism render's L\*5 is 44.4 against a canvas 22.2. That round predicted
  it from the code; this one measures the picture.
- **The Post-Impressionism round's boundary-statistics method reproduces across sessions and
  corpora.** §4.2: four Tonalism ÷ Realism ratios agree to within 0.009–0.013.
- **Report 03's four-category invariant table needs no change.** Every item in §7 is category A
  (pre-map) or category B (post-map selection-only).

**Where I could not settle a question:**

- Whether the spectral-slope inversion in §5.1 is real or an artefact of my estimator, my corpus,
  or JPEG reproduction. The effect is small and the distributions overlap; I claim only that the
  corpus gives the argument no support, not that the argument is false in general.
- Whether restoring the tonal range (item 1) and raising ε (item 2) really land inside the canvas
  envelope together. The two ladders are measured separately; the combination is arithmetic.

---

## 9. What not to build

Each of these I went looking for and rejected. The parent, Abstract, Fauvism and
Post-Impressionism lists all still apply; these are additional.

- **A Gaussian pre-blur for Tonalism**, despite this being the style with the best case for one.
  §5.2. Radius 1 after the floor is inside the noise (z 1.11 vs 1.14); radius 2 is worse (1.41);
  radius 4 and 8 are worse than doing nothing (2.82, 4.61 vs 1.88). The Gaussian's only real
  advantage is preserving low-amplitude texture, which the mandatory floor removes first and no
  later blur restores.
- **Spatially varying the guided filter's *radius*.** §6.2. Measured at +0.0 to +1.1% across four
  radial bands. Confirmed on a second style; this is now a settled negative.
- **Raising `EdgePreservingFloor.strength` as Tonalism's fix.** §5.4. Five passes instead of two to
  reach 20.19% sub-mark share, against ε 0.20's 15.83% in two passes — and the strength route
  leaves the picture *harder* (hard-boundary share 11.9% vs 6.6%), which is the wrong direction for
  this style specifically.
- **Pushing ε past 0.10 on Tonalism.** §5.2, §5.3. ε 0.15 and ε 0.30 sit *behind an unfiltered
  photograph* on distance to the canvas statistics, because they overshoot median edge width by
  2–3× against a target whose corpus SD is 0.35 px.
- **A "soft edge" stage that widens transitions.** §2.3. Tonalist boundaries are 1.66 px wide
  against photographs' 1.36 — a difference of 0.3 px. There is no measurable Tonalist practice of
  spreading boundaries out; what differs is the contrast carried across them. Any stage whose
  mechanism is "make the transition gradual" is aimed at a property the canvases do not have.
- **Automatic focal-point detection.** Unchanged from the parent README, and this round strengthens
  it from a different direction: five of fifteen canvases put their sharpest edges at the frame
  rather than the centre (§3), so any detector tuned to centre bias would be confidently wrong on a
  third of the target style.
- **Making the focal lever Tonalism's defining device.** §6.3. Harrison names uniform refraction as
  equally correct and prescribes it for the nocturne; the two nocturnes without focal incident
  measure at ratio 1.08 and 1.34.
- **Amplitude-spectrum slope as an acceptance test for any style.** §5.1. The shipped Tonalism
  render sits on the Tonalist canvas mean (−1.119 vs −1.113) while missing the canvases by 34% on
  tonal range, 55% on chroma and 2.7× on relative boundary contrast. A statistic a bad render
  already passes cannot gate a good one.
- **Validating a Tonalism setting against `Tests/Golden`.** The gradient understates sub-mark share
  by roughly a factor of two here as it did in the three previous rounds, and every §5 conclusion
  reverses direction between the two ends of the parameter ladder.
- **A post-map "atmosphere" or veil composite.** It is post-map arithmetic (category C) and its
  physically honest form is a glaze (category D, a larger invariant). Harrison's own instruction is
  a *pre*-paint contrast preparation — "lowering values as you approach the edge" — which the Lab
  remap already occupies. §1.1d.

---

## 10. Verification debt

Ranked by how much clearing each would change a decision above.

1. **Graham & Field 2007's fitting protocol.** The parent README records the numbers as verified
   (−1.21 ± 0.017 vs −1.40 ± 0.017), but §5.1's comparison needs the *protocol* — luminance
   definition, window, frequency range, per-image vs pooled fit — and the PDF at
   `people.hws.edu/graham/Graham-Spatial_Vision07.pdf` returned undecodable binary through this
   environment, as it did for the parent round. Until that is read, my slopes and theirs are two
   different measurements with the same name, and correction 1 should be read as "this corpus gives
   no support" rather than "the published result is wrong".
2. **Mather 2014.** Same status as in the parent README — never opened, still carrying the lead
   recommendation, and now with a local negative result against one of its claims (§5.1). This is
   the single most valuable item on any verification-debt list in this directory.
3. **The combination in pick 1.** Restoring the tonal range and raising ε are measured on separate
   ladders; the claim that together they land inside the canvas envelope is arithmetic, not a
   render. It is one probe run.
4. **My spectral estimator.** Validated only against internal consistency (blur steepens
   monotonically; the ordering is stable across 15 images). It has not been checked against a
   synthetic image of known slope, which is about twenty lines and would settle item 1's
   comparability question halfway.
5. **Wanda Corn, *The Color of Mood* (1972).** The standard reference for the movement, out of
   print, not online, not obtained. Everything in §1.2 about the movement's definition is at second
   hand through Wikipedia, Artsy and the American Tonalist Society.
6. **Whistler technical studies.** Hackney and Townsend's Tate/NGA/Hunterian work on the "sauce" is
   relayed from Tate's summary page and an AIC 2025 abstract; the underlying reports were not
   reached. This matters only for §1.2's mechanism, not for any number.
7. **Corpus size and selection.** 15 canvases and 15 photographs, chosen by hand without a sampling
   frame. Several of the §2 gaps are large enough to survive that (D20 8.8×, edge spans 3.0×); the
   spectral-slope difference (d ≈ 0.47) is not, and the radial result (§3) has a per-image spread of
   0.15–6.86 that a different fifteen canvases could easily reorder.
8. **Uncalibrated web reproductions.** Every canvas figure is measured on a JPEG derived from a
   museum digitisation of unknown colour management. Scale-free ratios (edge-span share, width in
   pixels at a normalised short edge, the D-series as a *shape*) are robust to that; the absolute
   L\* and C\* figures in §2.2 and §4.3 carry unknown reproduction error, and pick 1's target range
   of 35–45 L\* should be read with that in mind.

---

## 11. Corpus provenance

Three of four tracks in the previous round were compromised by contaminated corpora. Every image
below was downloaded through the Wikimedia Commons API with full metadata recorded, cropped 3% per
edge, resampled to an 800-px short edge, and **inspected as a rendered contact sheet before any
measurement was taken** — twice, once as downloaded and once after cropping.

### Paintings — 15 works by 10 painters `[verified]`

All are Wikimedia Commons files, all public domain by age, all sourced from museum or Google Art
Project digitisations rather than snapshot photographs of hung pictures.

Whistler *Nocturne in Black and Gold: The Falling Rocket* (Detroit Institute of Arts) ·
Whistler *Nocturne: Blue and Silver — Battersea Reach* (Google Art Project) ·
Whistler *Nocturne, Blue and Gold — Southampton Water* (Art Institute of Chicago, 1900.52) ·
Inness *The Home of the Heron* (Art Institute of Chicago, 1911.31) ·
Inness *Early Autumn, Montclair* (1891) ·
Inness *Sunset in the Woods* (1891, National Gallery of Art 166496) ·
Blakelock *Moonlight* (Google Art Project) ·
Tryon *November Morning* (Google Art Project) ·
Dewing *The Recitation* (1891) ·
Dewing *The Green Dress* (c.1910, NGA 180698) ·
Twachtman *Winter Harmony* (c.1890–1900, NGA 50257) ·
Ranger *The Path through the Woods* (Dallas Museum of Art, 1951.12) ·
Martin *Landscape* (Cleveland Museum of Art, 1946.291) ·
Harrison *Fifth Avenue at Twilight* (c.1910) ·
Eaton *Edge of the Forest* (1903).

**Rejections and the reason for each, because they are the useful part:**

- **Eaton, *Pines at Knocke*** — downloaded, then seen on the contact sheet to be a **greyscale**
  reproduction, almost certainly a period photogravure rather than a colour digitisation of the
  canvas. Dropped.
- **Tryon *Moonrise Near the Shore* (640×366), Wyant *Landscape near Arkville* (1024×688), Dabo
  *Evening on the Hudson* (1056×792), J. F. Murphy *Landscape* (1400×628)** — all four downloaded
  and then dropped because their originals are too small to reach an 800-px short edge after
  cropping. Upscaling them would have manufactured soft edges in exactly the corpus whose softness
  is the subject, which is the shape of error this round's brief warns about. Replaced by the five
  large-original works listed above.
- **Frame lips are real and were visible** on the Wyant, Ranger, Blakelock and Dabo files at full
  frame — a dark or gilt border of one to three percent of the canvas. The uniform 3% crop removes
  them; the post-crop contact sheet confirms none survive.
- **Dewing's *The Green Dress* is a pastel on brown paper**, not an oil, and the paper ground shows
  at the margins. Kept, and flagged: its figures sit inside the corpus's spread on every measure.

**Known composition of the corpus, stated so it can be weighted:** 13 landscapes and 2 figure
pieces; 3 Whistler, 3 Inness, 2 Dewing, and one each from Blakelock, Tryon, Twachtman, Ranger,
Martin, Harrison and Eaton. It is
weighted toward the landscape core of the movement, which is what the Tonalism style row will be
applied to least often — a photograph converter's inputs are not mostly nocturnal landscapes. That
is a real limit on transferring §2's targets to portrait or interior sources.

### Photographs — 15 images `[verified]`

Fourteen Wikimedia Commons featured/quality photographs drawn from the landscape, trees and people
categories, plus `Tests/Assets/sample.heic` from this repository. **Thirteen of the fifteen carry
an EXIF camera model** (Olympus E-M5 ×3, E-M1, Canon EOS 5D Mark II, EOS 6D, Nikon D750, D3300 ×2,
Pentax K10D, K-5 II, Sony ILCE-7RM2, Leica M9); the repo sample and *A Tibetan Pilgrim Lighting
Ghee Lamps* have no model recorded. Every one was looked at. **No 3-D renders, no photographs of
artworks, no composites** — the two contamination modes that caught the previous round. Subjects:
eight landscapes, five people in place, one botanical close-up, one architectural interior.

Files: *2013 Cogden Bridge* · *2013 Rainbow over Washfold* · *2014 Yorkshire Dales country road
Swaledale Askrigg* · *2015 Swaledale from Kisdon Hill* · *2016 Inle Lake, Myanmar* ·
*2019 Aquaculture in Chile* · *2019 Parc national des Pyrénées, Vallée de Gavarnie* ·
*A man and his donkey … Aswan, Egypt* · *A Tibetan Pilgrim Lighting Ghee Lamps* ·
*A touareg at the Festival au Désert near Timbuktu* · *Alnus glutinosa 02 by-dpc* ·
*Bad Wimpfen … Streuobstwiese mit Raureif* · *Baltic Sea view from Schmiedeberg hill in Rerik* ·
*Beignet maker* · `Tests/Assets/sample.heic`.

**The corpora are not matched on subject.** The photographs are mostly bright daylight; the
canvases are mostly dusk, dawn and winter. That confound inflates the raw D-series gap in §2.2 and
is precisely what §2.4's range normalisation exists to remove — and after normalisation the gap at
ΔE 10 disappears, which is the finding.

---

## Appendix — how this was measured

Two throwaway console projects in the session scratchpad, assembly-named `PaintTranslator.Tests` so
the app's existing `InternalsVisibleTo` grant applies, each with a `ProjectReference` to
`PaintTranslator.csproj`. Nothing was added to the repository and no repository file outside
`docs/research/painting-style/tonalism/` was modified.

**No pipeline stage was transcribed.** Every render came from the real `StylePipeline.Render` with
a `StyleDefinition` assembled from the real stage instances (`EdgePreservingFloor`,
`ToneAndChromaRemap`, `MotherColourTransform`, `KeepAllCandidates`, `NearestQuantiser`,
`OptionalBlur`) at Tonalism's registered defaults via the real `StyleDefinition.WithDefaults` and
`StylePipeline.DefaultValues`. Filter comparisons called the real `GuidedFilter.Apply` and
`GaussianBlur.Apply` at the real `PalettePhotoConverter.FloorRadius(markPixels)`. The focal
construction used the real `LinearPlanes.Decode`/`Encode` for its linear-light lerp. Region counts
and sub-mark share came from the real `PaintabilityMetrics.CountRegions` and
`FractionInRegionsSmallerThan`. Lab conversion throughout is `PalettePhotoConverter.RgbToLab`. The
palette is the six paints `Tests/StyleTestFixtures.SixPaints` uses (`PigmentLibrary.Selectable`
indices 0, 2, 6, 9, 11, 18).

Boundary statistics follow the Post-Impressionism round's definitions exactly so the two rounds'
tables sit on one scale: a *boundary pair* is a four-adjacent pixel pair whose RGB differs;
*boundary per 1000 px* is boundary pairs ÷ pixels × 1000; a *transition pixel* has at least one
differing four-neighbour; *within ½ mark* is the fraction of pixels within `round(mark/2)` of a
transition pixel; boundary ΔE is plain Euclidean CIELAB. Radial bands are quartiles of distance
from the image centre normalised by the half-diagonal. The source-domain measures (g, D2–D20, edge
spans, edge width, range normalisation) are defined in §2.1 and are this report's own.

The FFT is a hand-rolled iterative radix-2, validated only by internal consistency (§10 item 4).
Scripts and corpus manifests are kept in the scratchpad, not committed.
