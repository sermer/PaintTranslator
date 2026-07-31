# Research: Post-Impressionism — Brushwork

**Track:** Post-Impressionism, track 2 of 4 — the mark-making half.
**Date:** 2026-07-30
**Scope:** which of the five Post-Impressionist handlings the app's single style row should target;
what stroke geometry the literature actually licenses; whether a directional stage is warranted;
where the shipped style sits against the mark invariant; and which slot each recommendation lands in.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(filter families, stroke-based rendering, the four-category invariant table),
[`../fauvism/02-brushwork.md`](../fauvism/02-brushwork.md) (the local measurement method, the
contour-measurement trap, the negative result on calibrated stroke figures) and
[`../abstract/README.md`](../abstract/README.md) (only slots 1 and 5 can produce spatial structure;
never put a positional operation in slot 4). Where I contradict any of them I say so in §7.

**Verification convention:** `[verified]` = read from the cited primary source, or computed in this
repo · `[relayed]` = a secondary source I could not confirm at the primary · `[inferred]` = my
reasoning, stated nowhere.

---

## 0. Headline

**Post-Impressionism is now the least paintable style in the application except for Realism, which
is the style that deliberately does nothing.** Measured on 12 real photographs at the app's own
default mark, it puts **35.3% of pixels in regions smaller than one brushmark squared**, against
Fauvism's 6.8% and Abstract's 1.7%. It is the style with the second-largest `MarkScale` (1.6) and
**no stage anywhere in its pipeline that makes a mark**. `[verified — computed locally, §4]`

Four findings behind that, in descending strength:

1. **The Fauvism and Abstract rounds landed; Post-Impressionism did not.** Both of those styles
   gained post-map stages and were retuned. Post-Impressionism's registry row is byte-for-byte what
   it was, and its committed golden is unchanged — my measurement of it reproduces the Fauvism
   round's published figures exactly (486 regions, median area 5, 205 colours, 16.86% below mark²,
   mean h-run 3.30). Meanwhile Fauvism went 1,035 regions → **186** and Abstract 685 → **8**.
   `[verified, §4]` The published cross-style table in
   [`../fauvism/README.md`](../fauvism/README.md) is stale, and stale in the direction that hides
   this.
2. **The stage that would fix it exists and is registered on two other styles.** Adding
   `SmallRegionMerge` to Post-Impressionism moves the 12-photograph mean from **35.33% → 23.07%**
   with no new code at all. `[verified, §5]`
3. **`SmallRegionMerge` does not satisfy the postcondition the Fauvism round proposed as its most
   valuable assertion.** "After an area opening at `MarkPixels²`,
   `FractionInRegionsSmallerThan(MarkPixels²)` must be **exactly zero**" is false as implemented —
   one pass leaves 0.67% to 48.17% depending on the image. The mechanism is identified and
   quantified: **24–95% of sub-mark regions (mean 72%) have no neighbour that is already at or
   above the threshold**, so `LargestNeighbour` falls through to merging one sub-mark region into
   another, and the recorded region sizes are never updated. Stranded area (mean 23.94%) matches
   the one-pass residual (mean 23.07%) to within a point on every image. `[verified, §5.2]`
4. **A directional stage is warranted but it is third, and the verification the Fauvism round
   proposed for it does not work.** Median region elongation cannot test a directional filter:
   van Gogh's own strokes are constrained to aspect ratio ≥ 2.5:1 by the one physically calibrated
   study I could read, and the app's regions already sit at 2.5–3.5 from source banding alone.
   `[verified, §6]`

And one result the prior round said did not exist: **there is a physically scaled brushstroke figure
in the literature.** Lamberti et al. 2014 states its area constraint in dots-per-inch of the
painting and reports the calibrated values, which converts to **van Gogh strokes of 6.4–25.8 mm²
with an aspect ratio floor of 2.5:1** — roughly 4.5–9.1 mm long by 1.8–3.6 mm wide. That happens to
put `RenderContext.MarksAcrossShortEdge = 150` and `MarkScale = 1.6` in the right place, for the
first time on physical grounds rather than taste. `[verified from the PDF; one interpretive
assumption flagged in §3.2 and carried as debt 1]`

The three picks are in §8: **register and fix the area opening (slot 5, ~60 lines)**, **floor
strength 3 → 5 (slot 1, one line)**, **`FlowFlatten` (slot 1, ~200 lines)** — in that order.

---

## 1. The boundary problem, and the ruling

### 1.1 The label is an admitted evasion

Roger Fry organised *Manet and the Post-Impressionists* at the Grafton Galleries, opening 8 November
1910, and named the group in the way a man names something he does not want to characterise:
"For purposes of convenience, it was necessary to give these artists a name, and I chose, as being
the vaguest and most non-committal, the name of Post-Impressionism." `[relayed — quoted in
[Wikipedia, Post-Impressionism](https://en.wikipedia.org/wiki/Post-Impressionism); I did not reach
Fry's own text]` The same article states plainly that "although they often exhibited together,
Post-Impressionist artists were not in agreement concerning a cohesive movement", and dates the
period 1886–1905, from the last Impressionist exhibition to the birth of Fauvism. `[relayed]`

This is a stronger disclaimer than Fauvism carried. Fauvism at least had a phase break (1906) that
survey sources agree on; Post-Impressionism has five painters who share a decade and a city and
essentially nothing about how paint gets onto canvas.

### 1.2 Five handlings, and what each one already maps to in this app

| Painter | Handling | Sources | Where it already lives |
|---|---|---|---|
| **Cézanne** | The "constructive stroke", from the late 1870s: a system of parallel patches. In *Poplars* (1879–80) most foliage strokes are diagonal at **~45°, irrespective of the underlying form**. | `[relayed]` — [Eclectic Light, "Cézanne and constructive strokes"](https://eclecticlight.co/2015/11/17/trees-in-the-landscape-6-paul-cezanne-and-constructive-strokes/), also cited by report 03 §1.3 | Nothing |
| **Van Gogh** | Directional impasto; strokes follow the form and construct the perceived geometry. Measured as "strongly rhythmic — regularly shaped brushstrokes tightly arranged". | `[relayed]` — Li et al., *IEEE TPAMI* 34(6), 2012 (unreachable, debt 3) | Nothing |
| **Seurat** | Points of near-uniform size; ~220,000 on *La Grande Jatte*, uniform sizing from 1885–86. | `[relayed]` — search-index summary of the AIC/Wikipedia material; I did not open a conservation source | Already scoped as the shared broken-colour feature for Impressionism / Pointillism / Divisionism (parent README planned-work table) |
| **Gauguin / Bernard / Anquetin** | Cloisonnism, first named 1888: large flat colour planes with heavy dark outlines, after Japanese prints and stained glass. | `[relayed]` — [TheArtStory, Cloisonnism and Synthetism](https://www.theartstory.org/movement/cloisonnism-and-synthetism/) | **Already shipped as Fauvism** — `SmallRegionMerge` + `ContourLines` is exactly flat planes with drawn outlines |
| **Toulouse-Lautrec** | *Peinture à l'essence*: oil thinned with turpentine on cardboard, "long, striated brush strokes", frequently leaving the support exposed "to function positively as 'color'". | `[relayed]` — search-index summary of Art UK / Davis / Met material; the Musée d'Orsay object pages were not opened | The exposed support is a coverage property; the Fauvism round already routed reserve to `GroundFill` |

**Three of the five are already taken.** Seurat is the planned broken-colour feature; Gauguin is
what the shipped Fauvism style is; Lautrec's reserve is `GroundFill`'s job. Building any of them
under the Post-Impressionism label would produce two style rows that render the same picture — the
exact argument the Fauvism round used to rule out its own 1904–05 divisionist phase.

### 1.3 The ruling: target the constructive/directional patch, and do not split

**Post-Impressionism should own the Cézanne–Van Gogh axis: a visible, repeated, roughly
brush-width mark laid across the whole surface that does not coincide with a region boundary.**
`[inferred]`

Two reasons, and the second is the product one:

- **It is the only handling left, and it is the one thing no style in the app does.** Realism is
  faithful, Tonalism is low-key, Fauvism is flat planes plus contour, Abstract is a restricted
  palette. Nothing makes a mark. The Abstract round's test for whether a single row is worth having
  — does it aim at a populated part of the distribution — is passed here for the opposite reason it
  failed there: the target is *unoccupied by the other four rows*, not unoccupied by the art.
- **The alternative readings are already rendered by other rows.** A user who selects
  Post-Impressionism and gets outlined flat planes has selected Fauvism with a different chroma
  number.

**Do not split into "Cézanne" and "Van Gogh" rows.** `[inferred]` The Abstract round's split
argument rested on measured bimodality — Redies' abstract subset at edge-orientation entropy
3.945 ± **0.722**, an SD 3.4× that of Western oils, and Graham & Field putting geometric and
gestural abstraction on opposite sides of representational art. **No equivalent measurement exists
here.** Sigaki, Perc & Ribeiro 2018 — the one large-scale quantitative placement of art movements —
**does not name Post-Impressionism at all** in its results, and discards colour by averaging the
three channels (Pearson r = 0.989 against greyscale). `[verified via
[ar5iv/1809.05760](https://ar5iv.labs.arxiv.org/html/1809.05760)]` I searched for any measured
signature separating Post-Impressionist handling from its neighbours and found none, which
reproduces the Fauvism round's negative result one movement over.

More decisively: **the difference between Cézanne and Van Gogh is one parameter, not one style.**
Report 03 §1.3 already framed it correctly — Van Gogh is a data-driven orientation field, Cézanne
is a constant one. Both run through the same filter with the same radius. That is an
`orientationSource` parameter on a single stage, and the pipeline already expresses per-style
parameter defaults through `StyleDefinition.WithDefaults`. A second row would duplicate four slots
to vary one number.

---

## 2. Where the shipped style actually is

`StyleRegistry.cs:92–113` `[verified]`: mark scale **1.6**, `EdgePreservingFloor` strength **3.0**,
`ToneAndChromaRemap` contrast **1.1** / chroma **1.3**, `KeepAllCandidates`, `NearestQuantiser`,
`Array.Empty<IPostMapStage>()`.

Its doc comment says flatness "is meant to come from the floor's strength, not from the remap".
**The measurement says the floor cannot deliver that on its own, and the style has nothing else.**
Post-Impressionism and Tonalism are now the only two styles with an empty slot 5; Realism's is
empty by design.

`MarkPixels` now reaches four consumers, up from the one the abstract round found. `grep` over the
repo: `EdgePreservingFloor.cs:63` (→ `PalettePhotoConverter.FloorRadius(m) = m/2`, a guided-filter
window), `SmallRegionMerge.cs:26`, `ContourLines.cs:28` and `GroundFill.cs:26` — and
**Post-Impressionism registers none of the latter three**. `[verified]` For this style specifically,
the abstract round's "mark size is a hope, not a guarantee" is still literally true: raising
`MarkScale` to 1.6 only widens a denoising window.

That is measurably counterproductive. Raising it further makes things strictly worse: at mark scale
2.5 the 12-photograph sub-mark share rises to **38.66%**, at 1.0 it falls to **31.47%**.
`[verified, §5]` **Asking for a bigger mark without a stage that makes one is a fragmentation
multiplier** — the identical trap the Fauvism round diagnosed, still live in the style next door.

---

## 3. Stroke geometry — what the literature licenses

### 3.1 Almost all of it is shape words

Pulling the descriptors together: "parallel", "diagonal at approximately 45°", "constructive",
"rhythmic", "tightly arranged", "long, striated", "flat planes", "heavy outlines", "dots of almost
uniform size". Not one of those is a measurement, and this reproduces the Fauvism round's finding
about its own movement. `[verified — searched, negative]`

### 3.2 One exception, and it is physically scaled — a correction to the prior round

The Fauvism round's accuracy warning states: "All computational brushstroke work cited operates on
uncalibrated web reproductions with no physical scale — pixels of a JPEG, never millimetres of
paint. **No published figure could set a `MarkPixels` or stroke-length default.**"

**That is wrong for one paper, and it is the one whose subject is a Post-Impressionist.**

Lamberti, Sanna & Paravati, *"Computer-assisted analysis of painting brushstrokes: digital image
processing for unsupervised extraction of visible features from van Gogh's works"*, **EURASIP
Journal on Image and Video Processing 2014:53**, doi:10.1186/1687-5281-2014-53. The Fauvism round
recorded that SpringerOpen "redirected to an authorisation endpoint and did not yield the text".
**The counter-PDF endpoint serves it without authentication** —
`https://jivp-eurasipjournals.springeropen.com/counter/pdf/10.1186/1687-5281-2014-53.pdf`, 17
pages, downloaded and read in full. `[verified]`

The paper's segmentation admits a candidate brushstroke only if

> δ² × r_m < σ_r < δ² × r_M   (eq. 1)

where "σ_r represents the area of the region and **δ indicates the number of dot per inch (dpi) of
the image under consideration. In this work, image patches considered have been normalized to a
common resolution of 86.1 dpi.**" The calibrated values, chosen to minimise disagreement with ten
human observers, are **r_m = 0.01, r_M = 0.04**, plus shape constraints **r_e = 0.35** and
**r_w = 0.40** — the latter meaning width < 0.40 × length, i.e. **an aspect-ratio floor of
2.5:1**. `[all verified, read from the PDF]`

Converting, on the reading that δ is dots per inch **of the painting surface**:

| | Value |
|---|---|
| Pixel pitch at 86.1 dpi | 25.4 / 86.1 = **0.295 mm** |
| Admissible stroke area | 86.1² × 0.01 … 0.04 = 74.1 … 296.6 px² = **6.4 … 25.8 mm²** |
| Aspect ratio | **≥ 2.5 : 1** |
| Ellipse at the aspect bound | length **4.5 … 9.1 mm**, width **1.8 … 3.6 mm** |
| Ellipse at 1:1 | diameter **2.9 … 5.7 mm** |

`[verified arithmetic on verified inputs; the dpi reading is [inferred] — see below]`

**Why I read δ as dpi of the painting.** Table 4 of the paper runs the *same* r_m and r_M against
patches at 86.1, 129.1 and 172.2 dpi (×1, ×1.5, ×2) and reports near-identical accuracy. Eq. 1
scales the admissible pixel area by δ², so an invariant physical stroke is the only thing that
makes those three runs agree. If δ were a file-metadata number it would carry no information and
the robustness result would be meaningless. This is the single load-bearing assumption in the
section and it is **verification debt 1**.

**What the figure is and is not.** It is the *admissibility window* of an extraction algorithm,
calibrated so that its length, width and orientation judgements agree with human observers (average
orientation error ~11°, <9% on length, <5% on width; human inter-observer correlation 0.97 length /
0.89 width / 0.92 orientation). `[verified]` It is **not** a measured distribution of van Gogh's
strokes — the paper never publishes one. But it is a window that ten people agreed brackets real
van Gogh strokes on real Van Gogh Museum reproductions, which is a great deal more than "no
published figure exists".

### 3.3 What that does to this app's mark arithmetic

`RenderContext.MarksAcrossShortEdge = 150`, so if the painting's short edge is *S* millimetres, one
mark is *S*/150 mm and Post-Impressionism's is *S*/93.75 mm. `[verified from the code]`

| Painting short edge | Base mark | Post-Impressionism mark (×1.6) |
|---|---|---|
| 40 cm | 2.7 mm | 4.3 mm |
| 50 cm (van Gogh's common size) | 3.3 mm | 5.3 mm |
| 60 cm | 4.0 mm | 6.4 mm |

Against a measured stroke of **1.8–3.6 mm wide and 4.5–9.1 mm long**: the app's *base* mark lands
on van Gogh's stroke **width**, and Post-Impressionism's ×1.6 lands on his stroke **length**.
`[inferred, from the verified figures above]`

**Keep `MarkScale` at 1.6.** It is the only parameter in this application with a physical-units
justification, and it happens to be right. What is wrong is that nothing consumes it.

### 3.4 Directionality, coverage, and what nobody measured

- **Direction.** Cézanne ~45° irrespective of form; van Gogh along the form. No angular
  distribution is published for either. `[verified — searched, negative]`
- **Coverage.** Lautrec leaves the support exposed as positive colour; Cézanne uses "intermittent
  strokes which permit the use of the white priming of the fabric as part of the design" (relayed
  through the Fauvism round's citation of the AIC Cézanne technical publication). **No conservation
  study anywhere quantifies the reserved fraction** — the Fauvism round searched and found nothing,
  and so did I. `[verified — searched, negative]`
- **Whether strokes follow or oppose form.** This is the one Post-Impressionist property that is
  unambiguously documented on both sides — Cézanne *opposes*, van Gogh *follows* — and it is
  exactly the parameter §1.3 says should be a knob rather than two style rows.

---

## 4. What the shipped style produces — measured

### 4.1 The committed goldens

`Tests/Golden/*.png`, read to 32bpp ARGB, four-connected on the RGB triple with alpha masked, mark
= `4.0 × MarkScale` matching `GoldenStyleTests.MarkPixels = 4`. Region counts and sub-mark shares
come from the **real** `PaintabilityMetrics.CountRegions` and `FractionInRegionsSmallerThan`; only
the shape and orientation statistics are my own code. `[verified — computed 2026-07-30]`

| Style | mark | Regions | Median area | Colours | % below mark² | Mean h-run | Median elongation |
|---|---|---|---|---|---|---|---|
| Realism | 4.0 | 425 | 3 | 161 | 5.42% | 3.82 | 2.77 |
| Tonalism | 4.8 | 344 | 6 | 151 | 7.85% | 4.35 | 2.77 |
| Fauvism | 5.2 | **186** | **41** | 127 | **2.85%** | 4.67 | 2.12 |
| **Post-Impressionism** | **6.4** | **486** | **5** | **205** | **16.86%** | **3.30** | 2.84 |
| Abstract | 10.0 | **8** | **1456** | **8** | **0.00%** | 22.17 | 2.32 |

Three readings:

1. **My Post-Impressionism row reproduces the Fauvism round's published figures exactly** (486 / 5 /
   205 / 16.86% / 3.30). Two independent sessions, independent probe code, identical numbers. The
   method is sound and the style has not moved.
2. **Fauvism went 1,035 → 186 regions and 30.87% → 2.85%; Abstract went 685 → 8 and 54.38% →
   0.00%.** Those rounds shipped. Post-Impressionism is now the worst of the five on raw region
   count and second-worst on sub-mark share behind nobody — Realism is 5.42% at a much smaller
   mark.
3. The golden gradient understates everything by a factor of two to three. Read §4.2 before
   quoting any of it.

### 4.2 Real photographs — the number that matters

12 photographs (960 px long edge, JPEG, Wikimedia Commons), the same six-paint fixture,
`markPixels = 4`, which is also `RenderContext.DefaultMarkPixels` for a 960×640 image, so these are
the app's own default mark. Rendered through the **real `StylePipeline.Render` with the real
registry styles** — no transcription. `[verified — computed 2026-07-30]`

Measured against a **common** mark² of 6.4² for every style, so no style is flattered by its own
`MarkScale` setting the bar:

| Style | % below common mark² | Regions | % below, dominant colour dropped | Dominant colour's share |
|---|---|---|---|---|
| Abstract | **1.66%** | 2,966 | 2.12% | 32.6% |
| Fauvism | **6.82%** | 14,861 | **9.61%** | 27.5% |
| Tonalism | 30.08% | 102,524 | 33.55% | 14.9% |
| **Post-Impressionism** | **35.33%** | **124,052** | **38.12%** | 8.9% |
| Realism | 48.47% | 181,593 | 52.35% | 9.3% |

The right-hand columns exist because of the Fauvism round's measurement trap — a stage that writes
one index across the frame fuses into a single region and deflates every fragmentation metric. I
dropped the dominant colour from **both numerator and denominator** rather than sentinelling it
(sentinelling inflates the figure just as badly, in the other direction). **Fauvism's win survives
the check**: 9.61% with a contour covering 27.5% of the frame. That is a confirmation of the prior
round, not a correction.

Per photograph, Post-Impressionism as shipped:

| Photograph | Regions | Colours | % below mark² | + `SmallRegionMerge` |
|---|---|---|---|---|
| daisy | 33,680 | 689 | 7.65% | 3.39% |
| donkey | 203,794 | 926 | 61.07% | 43.30% |
| elephant | 142,162 | 883 | 41.45% | 24.23% |
| opera | 146,743 | 1,552 | 54.61% | 38.68% |
| wheat field | 8,379 | 220 | 5.80% | 0.67% |
| portrait | 127,150 | 1,052 | 31.06% | 12.94% |
| autumn landscape | 200,153 | 1,580 | 55.48% | 48.17% |
| moorland track | 234,958 | 1,128 | 51.00% | 44.81% |
| rooster | 213,824 | 1,448 | 35.49% | 22.13% |
| Swaledale | 107,557 | 655 | 37.90% | 22.50% |
| Touareg | 30,794 | 695 | 10.96% | 6.10% |
| Yangshuo | 39,439 | 658 | 31.48% | 9.96% |
| **mean** | **124,053** | **957** | **35.33%** | **23.07%** |

### 4.3 The test that is supposed to catch this does not

`StyleBehaviourTests.EveryRegisteredStyleIsPaintable` records Post-Impressionism's ceiling at
**0.013** — 1.3%. `[verified]` It measures on `BuildNoisyGradient(256, 256, 3.0)` with
`markPixels = 0`, so `DefaultMarkPixels(256,256) = 2`, mark = 2 × 1.6 = 3.2 and **mark² = 10 px**.
`[verified from the code]`

So the gate reads "fewer than 1.3% of pixels in regions under 10 px, on a synthetic gradient". The
same style on a real photograph at the app's own default mark scores **35.3% under 92 px**. The
test is 27× looser than its name implies, and the looseness comes from measuring a smaller mark on
a smoother source. **This is the Fauvism round's correction 2 — "any conclusion about floor
strength drawn only from `Tests/Golden` is unsafe" — recurring as a live hole in the test suite,
not only in the research.** `[verified]`

---

## 5. What moves the number

### 5.1 The sweep

Every variant is the real `StyleRegistry` Post-Impressionism row with real stage instances
substituted through `WithDefaults` and the record's `with` expression. Mean over the same 12
photographs, each measured at its own mark. `[verified]`

| Variant | Regions | Colours | % below own mark² | Δ vs shipped |
|---|---|---|---|---|
| **shipped** (floor 3, contrast 1.1, chroma 1.3, ×1.6) | 124,053 | 957 | **35.33%** | — |
| floor strength 1 | 196,256 | 1,091 | 49.69% | +14.36 |
| **floor strength 5** | 90,687 | 887 | **28.45%** | **−6.88** |
| contrast 0.95 | 121,700 | 891 | 35.12% | −0.21 |
| contrast 1.3 | 125,585 | 1,012 | 35.39% | +0.06 |
| chroma 1.0 | 111,784 | 765 | 32.44% | −2.89 |
| chroma 1.6 | 129,890 | 1,053 | 37.02% | +1.69 |
| **+ `SmallRegionMerge`** | 68,437 | 837 | **23.07%** | **−12.26** |
| + `SmallRegionMerge` + `ContourLines` | 192,869 | 480 | 31.93% | −3.40 |
| + `ContourLines` only | 231,121 | 592 | **42.84%** | **+7.51** |
| mark scale 1.0 | 129,579 | 986 | 31.47% | −3.86 |
| mark scale 2.5 | 122,299 | 920 | 38.66% | +3.33 |

Five things fall out:

1. **`SmallRegionMerge` is the single largest lever and costs no new code.** −12.26 points.
2. **The floor is the second largest**, and it is *not* saturated at 3: 3 → 5 buys another −6.88,
   and 3 → 1 costs +14.36. This confirms the Fauvism round's correction 2 on a different style —
   the "raising strength has never helped" claim is an artefact of the synthetic gradient.
3. **Contrast is inert.** 0.95 vs 1.3 differ by 0.27 points. Whatever contrast 1.1 is doing to this
   style, it is not fragmentation. The Fauvism round's "contrast is wrong by sign" finding does not
   transfer.
4. **Chroma 1.3 costs about 3 points**, and 1.6 would cost 5. That is a real but second-order
   effect and it is a colour decision — handing it to this round's colour track rather than ruling
   on it here.
5. **`ContourLines` alone makes Post-Impressionism materially worse** — +7.51 points, region count
   nearly doubled, colour count cut by 38%. See §9.

### 5.2 `SmallRegionMerge` does not clear the bar, and here is why

The Fauvism round wrote: "after an area opening at `MarkPixels²`,
`FractionInRegionsSmallerThan(MarkPixels²)` must be **exactly zero**. That is a hard assertion, not
a threshold, and it is the most valuable test available anywhere in this work."

**It is false against the shipped implementation.** Running the real `SmallRegionMerge.Refine`
repeatedly on the real mapped output: `[verified]`

| Photograph | before | pass 1 | pass 2 | pass 3 | pass 4 |
|---|---|---|---|---|---|
| wheat field | 5.80% | 0.67% | 0.17% | 0.03% | 0.00% |
| Yangshuo | 31.48% | 9.96% | 3.68% | 1.51% | 0.61% |
| Swaledale | 37.90% | 22.50% | 13.64% | 7.50% | 3.56% |
| rooster | 35.49% | 22.13% | 14.11% | 8.66% | 5.17% |
| donkey | 61.07% | 43.30% | 33.57% | 27.08% | 22.09% |
| moorland track | 51.00% | **44.81%** | 39.88% | 35.18% | **30.74%** |
| autumn landscape | 55.48% | **48.17%** | 44.06% | 40.53% | **37.26%** |
| **mean (12)** | **35.33%** | **23.07%** | 16.94% | 12.90% | — |

Four passes still leave 37% on one image, and convergence is slow enough that it is not a
fixpoint-after-a-few-passes situation either.

**Mechanism, quantified.** For each sub-mark region I counted whether it has *any* four-connected
neighbour already at or above `MarkPixels²`: `[verified]`

| Photograph | Sub-mark regions | Share of image | Regions with no ≥mark² neighbour | Their area share | Residual after pass 1 |
|---|---|---|---|---|---|
| wheat field | 7,371 | 5.80% | 24.3% | 0.72% | 0.67% |
| Yangshuo | 38,005 | 31.48% | 52.4% | 10.70% | 9.96% |
| portrait | 123,966 | 31.06% | 61.2% | 14.17% | 12.94% |
| Swaledale | 106,057 | 37.90% | 76.4% | 23.33% | 22.50% |
| rooster | 211,142 | 35.49% | 79.4% | 23.38% | 22.13% |
| donkey | 202,359 | 61.07% | 85.9% | 44.93% | 43.30% |
| moorland track | 234,335 | 51.00% | 94.8% | 45.48% | 44.81% |
| autumn landscape | 199,488 | 55.48% | **95.4%** | **48.62%** | **48.17%** |
| **mean** | — | 35.33% | **72.0%** | **23.94%** | **23.07%** |

**The stranded area *is* the residual**, to within a point on every image. The cause is visible in
`SmallRegionMerge.cs:69–88` and `:116–182`: regions are labelled once, then walked in raster order;
`LargestNeighbour` prefers a neighbour of at least `minimumArea` (`best`) but falls through to the
largest neighbour of any size (`fallback`) when none exists — and **`labels` and `regions` are
never updated after a merge**, so absorbing a sub-mark region into another sub-mark region does not
grow anything. On a densely textured passage, where most of a region's neighbours are themselves
sub-mark, almost every merge takes the fallback path and the area never crosses the threshold.
`[verified against the source]`

**The fix is a standard area opening**: label once, sort regions ascending by area, merge each
sub-threshold region into a chosen neighbour through a **union-find that accumulates area**, and
re-test. Then the postcondition genuinely holds whenever the image itself is at least `MarkPixels²`,
and one pass suffices. That is a rewrite of the merge loop, not a new algorithm; ~50 lines.
`[inferred]`

### 5.3 The reachable target

Combining, mean over the same 12 photographs: `[verified]`

| Variant | Regions | Colours | % below mark² | Mean h-run |
|---|---|---|---|---|
| shipped | 124,053 | 957 | 35.33% | 4.45 |
| `SmallRegionMerge` | 68,437 | 837 | 23.07% | 6.31 |
| `SmallRegionMerge` ×2 | 44,776 | 711 | 16.94% | 7.61 |
| `SmallRegionMerge` ×3 | 31,270 | 597 | 12.90% | 8.77 |
| floor 5 + `SmallRegionMerge` | 46,844 | 746 | 16.79% | 7.60 |
| **floor 5 + `SmallRegionMerge` ×3** | **19,153** | 522 | **8.23%** | 10.19 |
| floor 5 + ×3 + chroma 1.0 | 14,549 | 392 | 6.71% | 10.96 |

Stacking the stage three times is a diagnostic, not a proposal — it is what a correct one-pass area
opening would approximate. **The point is that the fixpoint sits below 10%, which is Fauvism's
territory (9.61%), so the target is reachable with the floor at 5 and a correct merge.**

I looked at the renders rather than only the numbers. At floor 5 with the merge, a Yorkshire
landscape comes out as broad lozenge-shaped patches of green with visible internal facets — it
reads as a broad-brush painting, where the shipped version reads as a slightly soft photograph with
a stone wall dissolved into unpaintable speckle. `[verified — inspected the PNGs]`

---

## 6. Directionality: warranted, third, and not testable the proposed way

### 6.1 There is an orientation field to align to, on photographs

I computed the smoothed multi-channel structure tensor (Sobel, tensor blurred to ~σ2, anisotropy
`A = (λ₁−λ₂)/(λ₁+λ₂)`) on the source, on the output of the **real `EdgePreservingFloor` stage
called directly**, and on the fully converted image. `[verified]`

| Photograph | source | floor str 1 | floor str 3 | floor str 5 | full Post-Impressionism |
|---|---|---|---|---|---|
| daisy | 0.665 | 0.804 | 0.844 | 0.859 | 0.577 |
| elephant | 0.556 | 0.629 | 0.742 | 0.800 | 0.583 |
| wheat field | 0.558 | 0.749 | 0.815 | 0.833 | 0.714 |
| portrait | 0.476 | 0.582 | 0.775 | 0.822 | 0.693 |
| moorland track | 0.538 | 0.622 | 0.670 | 0.708 | 0.516 |
| rooster | 0.532 | 0.714 | 0.786 | 0.827 | 0.627 |
| Swaledale | 0.588 | 0.668 | 0.789 | 0.832 | 0.655 |

Two readings, and only the first is safe:

- **After the floor there is a strong, well-defined orientation field**: mean anisotropy 0.66–0.84
  at strength 3, with 69–94% of pixels above A = 0.5. A flow-aligned filter would have something
  coherent to align to on real photographs. `[verified]`
- **The rise through the floor is not evidence of directional structure.** Removing isotropic
  sensor noise raises anisotropy mechanically. The honest claim is only that the field exists and
  is stable, not that the floor creates direction.

### 6.2 Coherence measured on mapped output is contaminated, and so is elongation

The Fauvism round warned that "the golden gradient's banding confounds" elongation. **The problem
is worse and more general than that.** `[verified]`

- On the golden gradient the *source* has anisotropy 0.474 (44.2% coherent) and the *converted*
  Post-Impressionism output has **0.667 (77.5%)**. Conversion manufactures the signal a directional
  stage would be trying to add. The fixture is not merely uninformative; it is actively misleading.
- On real photographs the direction of the effect is inconsistent — conversion raises coherence on
  seven images and lowers it on three. A quantisation boundary is a hard step, and a hard step is
  maximally anisotropic. **Any orientation statistic computed on a palette-mapped image is measuring
  Voronoi boundaries, not brushwork.**
- **Median region elongation cannot serve as the verification for a directional stage at all.**
  The Fauvism round proposed "median region elongation on the golden renders must rise measurably".
  But the app's five styles already sit at 2.12–2.84 on the golden, and Post-Impressionism alone
  ranges 2.52–4.59 across the 12 photographs, against Lamberti et al.'s calibrated aspect-ratio
  floor for a valid van Gogh stroke of **2.5:1**. The
  target is already met by accident. Elongation does not distinguish a stroke from a banding sliver.

**The only clean measurement point is after slot 1 and before the mapping** — precisely the "floor
only" rows above. Any `FlowFlatten` acceptance test has to be written there, and it needs a fixture
with real oriented content that the repo does not have.

### 6.3 So is `FlowFlatten` warranted?

**Yes, but third, and it must not be built before the merge.** `[inferred]`

For:
- It is the only proposal that would make Post-Impressionism the *constructive-stroke* style rather
  than merely a paintable one, which §1.3 says is what the row should be.
- The orientation-source parameter (constant ~45° for Cézanne, structure-tensor flow for van Gogh)
  gives the single row both painters for one number, which is the split argument resolved.
- The tensor is shared infrastructure that report 03 costed once and three reports have now wanted.

Against, and this is why it is third:
- The binding constraint is fragmentation, not direction. A directional pre-filter feeding a map
  that then produces 124,000 regions writes its trace onto a surface that immediately shatters. The
  merge has to come first or the effect will not be visible in either the metric or the picture.
- It has no calibration and cannot get one from the literature: no angular distribution is published
  for Cézanne or van Gogh, and the only reachable stroke study reports *magnitudes*, not
  orientation statistics.
- Its radius is the one thing that *is* pinned — tie it to `MarkPixels`, which §3.3 now justifies in
  millimetres.

---

## 7. Where this extends or contradicts prior research

**Contradicts:**

1. **The `SmallRegionMerge` postcondition is false as implemented.** §5.2. This is the most
   load-bearing correction here, because the Fauvism round called that assertion "the single most
   valuable test available anywhere in this work" and it would fail the moment it was written.
   The mechanism is a missing union-find, not a wrong idea — the postcondition becomes true after
   the ~50-line fix.
2. **"No published figure could set a `MarkPixels` or stroke-length default" is wrong.** §3.2.
   Lamberti et al. 2014 states its constraint in dpi of the painting, and the counter-PDF endpoint
   serves the full text without authentication — the prior round hit the `link.springer.com`
   redirect and stopped there. Subject to debt 1.
3. **Median region elongation is not a valid verification statistic for a directional stage.** §6.2.
   The Fauvism round proposed it for `FlowFlatten`; it cannot separate a stroke from a banding
   sliver, and van Gogh's own calibrated aspect floor is inside the range the app already produces.
4. **The Fauvism round's cross-style table is stale and understates the problem.** Fauvism is no
   longer the least paintable style; Post-Impressionism is, excepting Realism. §4.1.
5. **The `EveryRegisteredStyleIsPaintable` gate does not measure what its name says.** §4.3. Not a
   research claim of any prior round, but it is the reason none of them noticed.

**Extends:**

6. The Fauvism round's correction 2 — floor-strength conclusions from `Tests/Golden` are unsafe —
   reproduces on a second style: 3 → 5 is worth 6.9 points on photographs and roughly nothing on
   the golden.
7. The Fauvism round's contour-measurement trap is real, and I checked whether it invalidated that
   round's own headline. **It does not** — Fauvism is at 9.61% with the contour dropped from both
   numerator and denominator. I also found that the obvious way to apply the correction
   (sentinelling the excluded pixels so they cannot join regions) inflates every style by 4–27
   points and is worse than not correcting at all.
8. The abstract round's "`MarkPixels` reaches exactly one consumer" is now false in general (it
   reaches four) but **still true for Post-Impressionism specifically**, which registers none of
   the three new consumers. §2.

**Could not settle:**

- Whether van Gogh's orientation field is quantitatively distinguishable from a structure-tensor
  flow field. Li et al. 2012 is the only paper that would say and I could not reach it (debt 3).
- Whether the reserved-support fraction has ever been measured for any Post-Impressionist. Two
  rounds have now searched and found nothing.

---

## 8. Three picks

Line counts are C#-from-scratch estimates in the style of `Imaging/Styles/Stages/`, excluding UI.

### 1. Register `SmallRegionMerge` on Post-Impressionism, and make its area opening actually close

**Slot 5. ~10 lines registry + ~50 lines rewrite of the merge loop.**

*What.* Add `new SmallRegionMerge()` to Post-Impressionism's `PostMap`. Then rewrite
`SmallRegionMerge.Refine` as a real area opening: label once, process regions in ascending area
order, merge through a union-find that accumulates area so an absorbed region grows its target, and
prefer the neighbour with the smallest CIELAB distance among those that will clear the threshold.

*Evidence.*
- Post-Impressionism is at 35.33% below its own mark² on 12 real photographs, against Fauvism's
  9.61% and Abstract's 2.12%. `[verified, §4.2]`
- The stage as it stands buys −12.26 points with zero new code. `[verified, §5.1]`
- The residual after one pass is exactly the stranded area — 72% of sub-mark regions have no
  neighbour large enough to absorb them. `[verified, §5.2]`
- Three of four Abstract tracks, three of four Fauvism tracks, and now this one have converged on
  region merging. Fixing it once fixes it for all three styles.

*Verification.* After the rewrite, `FractionInRegionsSmallerThan(pixels, …, MarkPixels²)` must be
**exactly zero** on every one of the 12 photographs and on all five goldens. That assertion is now
correct, and it was not before; it should be added as a test over the whole registry, not one
style. Also assert the stage is idempotent — a second pass must leave the buffer byte-identical.

*Risk.* Merging erases genuinely thin features. The measured cost is real: colour count falls
957 → 837 in one pass. Cap it and keep the same threshold-as-multiple-of-mark² parameter Fauvism's
version would expose.

### 2. Raise `EdgePreservingFloor` strength from 3.0 to 5.0

**Slot 1. One line plus a regenerated golden.**

*Evidence.*
- −6.88 points alone; 23.07% → 16.79% in combination with the merge. `[verified, §5.1, §5.3]`
- Raises post-floor structure-tensor anisotropy from 0.66–0.84 to 0.69–0.86, which is the field a
  future directional stage would consume. `[verified, §6.1]`
- The doc comment already claims the style's flatness "is meant to come from the floor's strength".
  At 3.0 it does not, measurably.

*Against, and it should be argued before shipping.* 5.0 is Abstract's setting. At floor 5,
Post-Impressionism and Abstract would differ only by mark scale and the palette transform. If that
is unacceptable as a product matter, take 4.0 (untested — measure it) or take the same points from
the merge fix alone, which reaches 12.90% at floor 3 with a three-pass stack and should reach
similar with a correct one-pass opening.

*Verification.* Regenerate `Tests/Golden/Post-Impressionism.png` and look at it. Then tighten
`EveryRegisteredStyleIsPaintable`, and **fix the fixture while you are there** — it measures a
mark² of 10 px on a synthetic gradient and is 27× looser than the real default.

### 3. `FlowFlatten` — structure tensor plus flow-aligned flattening, with the orientation source as a parameter

**Slot 1. ~70 lines tensor + ~130 lines filter ≈ 200 lines.**

*What.* Smoothed multi-channel structure tensor on the pre-map buffer; extract orientation and
anisotropy; run a one-dimensional bilateral along the flow (cheaper) or an anisotropic Kuwahara
(better) at a radius tied to `context.MarkPixels`. **One parameter decides the movement:**
orientation source ∈ {flow, constant angle}. Flow gives van Gogh; a constant ~45° gives Cézanne's
constructive stroke. Default to flow.

*Evidence.*
- Cézanne's constructive stroke and van Gogh's directional impasto are the two handlings left once
  Seurat, Gauguin and Lautrec are assigned elsewhere. `[relayed, §1.2]`
- After the floor there is a coherent orientation field to align to on every photograph measured —
  anisotropy 0.66–0.84, 69–94% of pixels above 0.5. `[verified, §6.1]`
- The radius is pinned by `MarkPixels`, which §3.3 now justifies in millimetres against a measured
  van Gogh stroke. No free parameter is introduced except the orientation source, which is a
  two-value enum.
- The tensor is shared infrastructure wanted by report 03 lever 5, the Fauvism round's pick 3, and
  this one.

*Verification, and this part is new.* Measure orientation-histogram entropy and mean anisotropy
**on the buffer after slot 1 and before the mapping** — never on the mapped output, where
quantisation boundaries dominate the tensor (§6.2). Median region elongation is not usable. The
repo needs a fixture with real oriented content; the golden gradient's own anisotropy *rises* from
0.474 to 0.667 through conversion, so it manufactures the signal under test.

*Risk.* Highest cost, weakest calibration, and the effect is invisible until pick 1 lands. Build it
third or not at all.

---

## 9. What not to build

The parent, Abstract and Fauvism "what not to build" lists all still apply. These are additional,
and each was investigated here.

- **`ContourLines` on Post-Impressionism.** Measured: alone it takes the sub-mark share from 35.33%
  to **42.84%**, nearly doubles the region count (124k → 231k) and cuts distinct colours by 38%.
  With the merge it is still worse than the merge alone (31.93% vs 23.07%). `[verified, §5.1]` The
  historical argument is independent and points the same way: the drawn outline over flat planes is
  **cloisonnism** — Bernard and Anquetin, 1888 — and cloisonnism is what the app already ships as
  Fauvism. Adding it here would make two rows render the same picture.
- **Splitting into "Cézanne" and "Van Gogh" style rows.** The difference is one parameter, and
  unlike the Abstract case there is no measured bimodality to appeal to — Sigaki et al. does not
  name Post-Impressionism, and no signature separating any Post-Impressionist handling from its
  neighbours exists. §1.3.
- **Pointillist or divisionist dithering under the Post-Impressionism label.** Seurat is already
  scoped as the shared broken-colour feature for Impressionism, Pointillism and Divisionism.
  Identical reasoning to the Fauvism round's rejection of its own 1904–05 phase.
- **Full stroke-based rendering (Hertzmann / Litwinowicz) as the Post-Impressionism default.** All
  of the Fauvism round's objections stand — 4–6 uncalibratable parameters, stochastic output in a
  slider UI, 400–600 lines. One new one: SBR is a *placement* algorithm and this style's measured
  defect is *fragmentation*. Pre-map placement is re-quantised afterwards, so strokes the algorithm
  intended as one colour snap apart and add fragments to the style that already has the most.
- **Raising `MarkScale` above 1.6.** Measured worse: 2.5 gives 38.66% against 1.6's 35.33% and 1.0's
  31.47%. A larger mark with no mark-making stage is strictly a fragmentation multiplier — the
  Fauvism trap, recurring. `[verified, §5.1]` And 1.6 is the value the physical arithmetic in §3.3
  supports, so the fix is to build a consumer, not to move the number.
- **Median region elongation as anything's acceptance criterion.** §6.2. It cannot separate a stroke
  from a banding sliver and the app already meets van Gogh's calibrated aspect floor by accident.
- **A "reserved canvas" or exposed-support stage for Toulouse-Lautrec.** Same ruling as the Fauvism
  round: coverage is orthogonal to both pipeline axes and the right home is `GroundFill`. Nobody has
  ever measured the reserved fraction for any painter in this movement.
- **Impasto, in any form, for van Gogh.** He is the strongest case anyone will ever make for it and
  it still fails for the reason the parent research gives: shaded colours are not achievable
  colours. Van Gogh does not reopen it any more than Vlaminck did.
- **A position-dependent quantiser in slot 4** for any of this. Every operation here fits slot 1 or
  slot 5.
- **Tuning contrast.** Measured inert for this style: 0.95 and 1.3 differ by 0.27 points. The
  Fauvism round's "contrast is wrong by sign" is a Fauvism finding, not a general one.

---

## 10. Verification debt

Ranked by how much clearing each would change a decision above.

1. **Confirm that "86.1 dpi" in Lamberti et al. 2014 is dots per inch of the painting, not of the
   image file.** Everything physical in §3.2 and §3.3 rests on it, including the defence of
   `MarkScale = 1.6` and the aspect-ratio figure that kills elongation as a test statistic. Table 4
   (same parameters valid at 86.1 / 129.1 / 172.2 dpi) is strong circumstantial support but is not
   a statement. Settled cheaply by finding the pixel dimensions the Van Gogh Museum publishes for
   any one named painting in the dataset — *The garden of Saint-Paul's Hospital* (Nov 1889),
   *Landscape at twilight* (June 1890), or *Olive grove* (June 1889), all named in the paper.
2. **Build the corrected area opening and re-measure.** Local work. §5.3 shows the fixpoint is below
   10% via a three-pass stack, but that is a proxy. Pick 1's entire value rests on one correct pass
   reaching zero, and that is an hour's work to verify rather than an assumption to carry.
3. **Li, Yao, Hendriks & Wang, *IEEE TPAMI* 34(6):1159–1176, 2012.** The Stanford mirror
   (`infolab.stanford.edu/~wangz/project/imsearch/ART/PAMI11/li.pdf`) returns
   `ECONNREFUSED 171.64.75.45:443`; `pmc.ncbi.nlm.nih.gov` serves a reCAPTCHA; IEEE and ACM are
   paywalled. It is the only source that would say whether van Gogh's orientation field is
   quantitatively distinct from a structure-tensor flow field, which is pick 3's whole premise.
   The authors released their extracted brushstroke data as an Academic Torrents collection, which
   may be a route around the paywall.
4. **The Art Institute of Chicago's Cézanne digital publication** —
   `artic.edu/digital-publications/47/…/a-harmony-parallel-to-nature-…` returned **403**. It is the
   most likely source of a measured constructive-stroke dimension, which would either corroborate
   or contradict the van Gogh figures as a basis for `MarkPixels`.
5. **My photograph corpus.** 12 Wikimedia Commons images, 960 px long edge, JPEG, skewed toward
   landscape (7 of 12). **Only 5 of the 12 were downloaded by me with the URL recorded**; the other
   7 appeared in a shared scratchpad directory used concurrently by another agent in this round and
   I did not record their provenance. The cross-style *ranking* is robust — it holds on every
   individual image — but the absolute percentages are corpus-dependent, and the corpus is not
   reproducible as it stands. This is the same failure the Fauvism round's track 4 recorded, and I
   repeated it.
6. **A directional fixture for the repo.** Not a source; a missing artefact. `Tests/Golden` cannot
   support any conclusion about direction (§6.2), so pick 3 is unverifiable until an oriented
   synthetic source exists.
7. **Any conservation study quantifying reserved support for a Post-Impressionist.** Searched;
   negative. Recorded so the third round does not search again.

---

## Appendix — how everything was measured

A throwaway console project in the session scratchpad, `AssemblyName` set to `PaintTranslator.Tests`
so `InternalsVisibleTo` grants access to `StylePipeline`, `StyleRegistry` and the stages. Not
committed.

**The method rule was followed.** Every render goes through the real
`StylePipeline.Render(source, paints, style, markPixels, StylePipeline.DefaultValues(style))` with
real `StyleRegistry` rows; variants are produced with `StyleDefinition.WithDefaults` and the
record's `with` expression, so `EdgePreservingFloor`, `ToneAndChromaRemap`, `SmallRegionMerge` and
`ContourLines` are the shipped instances, never transcriptions. Region counts and sub-mark shares
come from the real `PaintabilityMetrics.CountRegions` and
`PaintabilityMetrics.FractionInRegionsSmallerThan`. `§6.1`'s floor-only rows call
`EdgePreservingFloor.Apply` directly with a hand-built `ParameterValues` and `RenderContext`.

Only three measurements are my own code, because `PaintabilityMetrics.ForEachRegion` is private and
reports only areas: median region area, region second-moment elongation (√(λ₁/λ₂), regions ≥ 8 px),
and the exclude-a-colour variant of the sub-mark share. The structure tensor is mine throughout —
the repo has none.

Palette: `PigmentLibrary.Selectable` indices 0, 2, 6, 9, 11, 18 — the same six-paint fixture
`StyleTestFixtures.SixPaints()` uses, reproduced rather than referenced because the probe is not in
the test project. `markPixels = 4` throughout, which matches `GoldenStyleTests.MarkPixels` and is
also `RenderContext.DefaultMarkPixels` for a 960×640 image, so the photograph figures are at the
app's own default.
