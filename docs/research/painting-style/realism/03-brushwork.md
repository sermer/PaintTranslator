# Research: Realism — Brushwork and Mark

**Track:** Realism, track 3 of 4 — brushwork and mark.
**Date:** 2026-07-31
**Scope:** what realist brushwork is when it is measured rather than described; what Realism's
fragmentation actually is at the app's own derived default mark and how it moves with image size;
how much of it is genuine detail and how much is quantiser speckle; whether `SmallRegionMerge`
belongs on this row and at what threshold; whether `MarkScale` 1.0 is a real control; and where
the honest paintability ceiling sits.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(edge hierarchy, filter families, the four-category invariant table),
[`../post-impressionism/01-brushwork.md`](../post-impressionism/01-brushwork.md) (the boundary and
region-statistics method, the `SmallRegionMerge` diagnosis, the elongation negative),
[`../tonalism/04-line-and-structure.md`](../tonalism/04-line-and-structure.md) (the value-mass and
notan constructions, the thin-dark-structure detector, the area-opening rule, the merge
confirmation) and [`../abstract/README.md`](../abstract/README.md) (only slots 1 and 5 can produce
spatial structure). Where I correct any of them, §8 says so.

**Verification convention** — house standard: `[verified]` = read from the cited primary source, or
computed in this repo · `[relayed]` = a secondary source reports it and I did not reach the primary
· `[inferred]` = my reasoning, stated nowhere.

**Working-tree note, and it changes the premise of this round's brief.** I measured the
**uncommitted** working tree, not the committed one. In it, `SmallRegionMerge` is the smallest-first
union-find rewrite, and `StyleRegistry.cs` has moved on from what the brief describes: Tonalism now
carries the merge plus contrast 0.80 / key −8.0 / chroma 0.85 / floor edge 0.10, Fauvism runs
`ContourLines` **then** `SmallRegionMerge` (the ordering defect the Tonalism round found is fixed),
Post-Impressionism carries the merge at contrast 1.0 / chroma 1.45, and Abstract runs `GroundFill`
then the merge. `[verified — read from `Imaging/Styles/StyleRegistry.cs` in the working tree]`

**Realism is unchanged and is now the only style in the application with an empty slot 5.**

---

## 0. Headline

**Realism is the last fragmented row in the app, and it is no longer worst by a factor of two — it is
the only one left.** On 15 provenance-checked photographs at the app's own derived default mark,
Realism puts **52.99%** of pixels in regions below its own mark². Every other registered style, in
this working tree, puts **exactly 0.00%**. `[verified, §2]`

**But the number does not mean what four rounds have taken it to mean, and this is the finding that
should change a decision.** Fourteen real Realist canvases — pictures that demonstrably *were*
painted — put through the app's own Realism row and measured with the app's own
`PaintabilityMetrics` score **42.51%** below their own mark². Van Gogh's *Wheatfield with Crows*
scores **66.80%**. `[verified, §4]` **The metric is not measuring whether the picture could be
painted. It is measuring what the converter does to any continuous-tone input.** A Courbet fails it.

Six findings, in descending order of how much each should change a decision.

1. **The paintability floor is a property of the pipeline, not of the subject.** Realist canvases at
   42.51% against photographs at 52.99% — one order, not two, and overlapping ranges (canvases
   21.8%–71.8%, photographs 16.5%–99.2%). `[verified, §4]` The consequence for what the app claims
   is in §5.5: **Realism's contract and the mark invariant are incompatible**, and one of them has
   to be given up explicitly rather than by accident.

2. **"Realism means invisible brushwork" is false, and it measures.** If brushwork were invisible a
   realist canvas would be *smoother* than a photograph at mark scale. It is the opposite: only
   **5.3%** of a realist canvas holds colour within 2 ΔE across a mark-wide window, against
   **17.6%** of a photograph — canvases are **3.3× less** flat at mark scale. `[verified, §1.3]`
   What canvases have instead is *larger* value masses (largest mass 21.6% vs 10.4%). **Realist
   handling is bigger masses with rougher insides**, which is exactly what the movement's own
   sources claim and the opposite of what the app's do-nothing row delivers.

3. **Registering `SmallRegionMerge` reaches exactly 0.000000 on all 15 photographs in one pass and
   is idempotent — and on Realism, alone, it is visually destructive.** Regions 269,973 → 4,078,
   median area 1 → 58.9 px. The cost is **47.5% of pixels changed at a mean ΔE of 18.13**, boundary
   mean ΔE 9.60 → 12.99, hard-boundary share 30.1% → 36.9%, thin-dark-structure retention 38.3%.
   `[verified, §5]` I rendered it and looked: on a forest floor the subject disappears into
   camouflage; on a harbour the boats become white blobs; on a doorway a red sign is swallowed
   whole. **Do not register it bare.** §5.4 gives the pairing that works.

4. **The merge absorbs into the *largest* neighbour, not the nearest in colour, and one comparison
   fixes it.** `SmallRegionMerge.LargestNeighbour` ranks candidates by area alone
   (`SmallRegionMerge.cs:182-216`). `[verified — read from the source]` Ranking by CIELAB distance
   instead, in a prototype that reproduces the shipped stage exactly when told to rank by area,
   still reaches 0.000% and is **better on every axis measured**: regions 4,078 → **3,209**, median
   region area 58.9 → **121.3 px**, boundary ΔE 12.99 → **11.66**, colour displacement of moved
   pixels 18.13 → **15.19**, thin-detail retention 38.3% → **46.4%**. `[verified, §5.3]` **This is a
   new defect, in the stage four of five styles now register, and nobody has recorded it.**

5. **`MarkScale` 1.0 is inert on Realism, and this time it is not the rounding.** Sweeping 0.5 → 3.0
   moves the region count by **3.3%** (273,599 → 282,532), the median region area not at all (1 px
   throughout) and mean boundary ΔE by **0.36** — while the guided filter's window radius genuinely
   moves from 1.40 to 7.33. `[verified, §6]` The Tonalism round attributed its own inert MarkScale to
   `Round(mark/2)` discarding fractional scales; **here the radius changes six-fold and the picture
   still does not.** The floor's real knob is ε: 0.05 → 0.30 takes the sub-mark share 52.99% → 41.82%
   and the hard-boundary share 30.1% → 15.4%.

6. **Most of the fragmentation is real detail, not speckle.** 39.9% of output boundary pairs sit on
   a *floored-input* step below one JND, but only **13.1%** of sub-mark area lies in regions whose
   entire boundary is sub-JND. `[verified, §3]` So **at least 86.9% of the 53% sits on genuine
   source detail below one mark**, which is why no amount of extra denoising closes it and why the
   honest options are a coarser mark, a merge, or an admission.

**Boundary note, deferred.** Whether the row means the 1848 movement or photographic fidelity is
track 2's question. I record only what brushwork says about it: the movement's own definition is
*visible, rough facture set against academic finish*, and the app's row is the one that leaves the
photograph alone. **On brushwork the row is named after the movement and behaves like its
opposite.** §1.1.

The three picks are in §9: **floor `edge` 0.05 → 0.10 and `strength` 1.0 → 3.0, then register
`SmallRegionMerge` (three lines, in that order)**, **make the merge absorb into the colour-nearest
neighbour (~10 lines)**, **a threshold parameter on the merge, left at 1.0 (~40 lines)**.

**Overlap with this round's track 4 (edges), flagged deliberately.** We both touch
`EdgePreservingFloor` and slot 5. Everything I recommend for the floor is chosen for its effect on
*region structure*, and I measure edge quality only as a cost (§5.1's mean boundary ΔE and
hard-boundary columns). Realism's floor is the registry's weakest — strength 1.0, ε 0.05, no
`WithDefaults` call at all — so any edge recommendation has plenty of room and will not collide with
pick 1 unless it wants ε above 0.10 or strength above 3.0. **Where we disagree, prefer track 4 on ε
and me on slot 5**, and note that on this row ε and strength are *not* interchangeable the way the
Tonalism round concluded for its own: ε 0.30 alone reaches 41.82% with mean boundary ΔE 6.32, while
strength 5.0 alone reaches 32.03% at ΔE 8.12 — **ε buys edge quality, strength buys paintability,
and on Realism you want both.**

---

## 1. What realist brushwork is, measured

### 1.1 What the sources claim, and it is not what the app's row does

The movement's descriptors are unusually consistent, and every one of them is about facture being
*visible*.

- **Realism is defined against academic finish.** "Courbet's rough brushwork represents a deliberate
  rejection of the Neoclassical finish that had previously dominated French art… this wasn't the
  smooth, polished finish of academic painting." `[relayed — Concordia open textbook, *Creating the
  Modern*, ch. 1, and the 1st-art-gallery survey, both reached through search summaries]`
- **The handling is the argument.** "For Courbet realism dealt not with the perfection of line and
  form, but entailed spontaneous and rough handling of paint, suggesting direct observation by the
  artist while portraying the irregularities in nature." `[relayed — National Galleries of Scotland
  glossary and the Concordia text]`
- **The palette knife is named as the tool.** Courbet "applied his paint with a palette knife in
  broad, rough patches and strokes, creating windblown clouds and coarse rock texture with equal
  visual and material weight" (*Isolated Rock*, c.1862). `[relayed — Brooklyn Museum object page,
  reached through a search summary; the Art UK essay on the same subject returned **403**]`
- **He swapped tools within one picture** — knife and brush according to the effect, then smoothed
  the surface with a rag or sponge. `[relayed — search summary of conservation-facing material; no
  primary conservation report reached, debt 4]`

Two things follow.

**First, the naive premise the brief asks me to test is refuted before any measurement.** "Realism
means invisible brushwork" describes *academic* painting — the thing Realism was founded against.
The confusion is real and worth naming, because the app's row is called Realism and does the
academic thing: it leaves the photograph alone.

**Second, there is no measured figure to calibrate against, and I searched.** No conservation study
I could reach reports a stroke width, length or area in millimetres for any Realist. The
Post-Impressionism round found exactly one physically scaled figure in the whole literature
(Lamberti et al. 2014, van Gogh: 6.4–25.8 mm², aspect ≥ 2.5:1); **nothing equivalent exists for
this movement.** `[verified — searched, negative]`

And **Sigaki, Perc & Ribeiro 2018 does not name Realism, Naturalism or Academic art anywhere.**
`[verified — fetched [ar5iv/1809.05760](https://ar5iv.labs.arxiv.org/html/1809.05760) and asked for
the full movement list; the 24 named movements are Renaissance, Neoclassicism, Romanticism, Modern
Art, Impressionism, Cubism, Expressionism, Surrealism, Contemporary/Postmodern, Pop Art, Minimalism,
Fauvism, Pointillism, Hard Edge, Op Art, Constructivism, Kinetic, Concretism, Neo-Baroque,
Neo-Romanticism, Pattern and Decoration, Conceptual, Colour Field and Divisionism]` That is the
**fourth consecutive round** to run this check on the one large-scale quantitative placement of art
movements and find its own style absent. Four negatives is no longer a gap; it is a result. **Stop
searching this literature for a movement-level signature.**

### 1.2 The corpus

14 Realist canvases, 3 controls from movements earlier rounds measured, and 15 photographs, all at
960 px on the long edge. Full provenance and the rejection list are in §13. Every image was
displayed and looked at; two were rejected on inspection.

The mark used throughout this section is each picture's **own** `RenderContext.DefaultMarkPixels`,
i.e. short edge ÷ 150 clamped to [2, 128]. That normalises every statistic to "marks across the
picture" rather than to pixels, which is the only way a 3.15 m *Burial at Ornans* and a 55 cm
*Angelus* can appear in the same table.

### 1.3 The measurement, and it refutes the premise

Mean over each group, at each picture's own mark. `[verified — computed 2026-07-31]`

| statistic | **Realist canvases (14)** | **photographs (15)** | controls (3) |
|---|---|---|---|
| largest value mass, share of picture | **21.6%** [6.5–51.7] | **10.4%** [0.1–20.2] | 8.0% [1.7–14.9] |
| area in value masses ≥ mark² | **77.5%** [54.3–88.8] | **70.4%** [11.1–91.0] | 69.0% [62.5–74.7] |
| **flat at mark scale** (whole mark-wide window within 2 ΔE) | **5.3%** [0.0–40.4] | **17.6%** [0.0–48.7] | 0.1% [0.0–0.2] |
| structure-tensor coherence | **0.46** [0.38–0.53] | **0.58** [0.46–0.68] | 0.49 [0.36–0.63] |
| median value-mass elongation | 2.21 [1.93–2.77] | 2.73 [1.87–4.78] | 2.23 [1.94–2.64] |
| notan gap (mean L\* above own median − below) | 30.9 | 38.2 | 29.1 |
| mean L\* | 38.7 | 45.6 | 37.8 |
| sd L\* | 19.6 | 22.9 | 18.0 |
| mean C\*ab | 14.7 [4.4–33.3] | 19.3 [4.7–40.5] | 25.3 [11.7–38.4] |

Four readings.

1. **The invisible-brushwork premise is refuted, and by the statistic built to test it.** A realist
   canvas holds colour across a mark-wide window on **5.3%** of its area; a photograph does so on
   **17.6%**. Twelve of the fourteen canvases are below 10%; the one outlier is Bonheur's
   *Labourage nivernais* at 40.4%, which is the smoothest, most academically finished picture in the
   corpus and reads that way on screen. **A realist canvas is rougher than a photograph at the scale
   of one brushmark, not smoother.** `[verified]`

2. **What canvases do have is bigger masses.** Largest single value mass 21.6% against photographs'
   10.4%, and 77.5% against 70.4% of area in masses of at least one mark². **Realist handling is a
   large-mass, rough-inside construction** — which is precisely the "broad, rough patches" the
   sources describe, and precisely what the app's Realism row cannot produce, because it has nothing
   in slot 1 or slot 5 that makes a mass.

3. **Realism has no directional claim, and two statistics say so.** Canvas coherence 0.46 against
   photographs' 0.58, and median mass elongation 2.21 against 2.73 — canvases are *less* oriented
   than the photographs they would be made from. The one canvas that runs the other way is the
   control: van Gogh's *Wheatfield with Crows* at coherence **0.630**, the highest of any painting in
   the set and above every photograph but two. `[verified]` **That is a useful cross-check that the
   measure is sensitive to directional impasto**, and it is the second style in a row (after
   Tonalism) with no orientation claim to test. Carry the Post-Impressionism round's correction 11
   forward unchanged: no orientation statistic for this row.

4. **Value-mass consolidation does not separate Realism from Tonalism.** The Tonalism round measured
   7 Tonalist canvases at largest mass **23.3%** [13.2–36.1] and in-masses **84.4%** [76.5–91.5]. My
   14 Realist canvases give **21.6%** [6.5–51.7] and **77.5%** [54.3–88.8]. `[verified — the
   construction is identical: nine equal L\* bands, four-connected, at the picture's own mark²]`
   The two corpora are indistinguishable on this axis. **The value-mass measure is a paintability
   measure, not a style measure** — worth recording because two rounds have now used it to justify a
   style decision.

**The caveat, and it is the same one every round carries.** These are uncalibrated web reproductions
of varnished, aged, sometimes relined oil paintings, and JPEG grain and craquelure both inflate
mark-scale roughness. The *direction* of finding 1 is large (3.3×) and consistent (12 of 14 canvases
below 10%), and the control group sits at 0.1%, which is the direction reproduction noise would push
everything. **The sign is safe; the magnitude is not.** Debt 2.

---

## 2. Realism's fragmentation, properly quantified

### 2.1 The number, and it is 53%

15 photographs, `markPixels = 0` so every render uses the app's own
`RenderContext.DefaultMarkPixels` for its own dimensions, the six-paint `StyleTestFixtures.SixPaints()`
fixture reproduced, rendered through the real `StylePipeline.Render` with the real registry row.
`[verified — computed 2026-07-31]`

| Style | mean mark | regions | median area | colours | **below own mark²** | below a common mark² (6.4²) |
|---|---|---|---|---|---|---|
| **Realism** | 4.87 | **269,973** | **1.0** | 1,168 | **52.99%** | **57.05%** |
| Tonalism | 5.84 | 3,200 | 89.3 | 331 | **0.00%** | 3.12% |
| Fauvism | 6.33 | 1,783 | 97.1 | 312 | **0.00%** | 1.36% |
| Post-Impressionism | 7.79 | 1,912 | 153.7 | 361 | **0.00%** | 0.41% |
| Abstract | 12.17 | 155 | 713.2 | 9 | **0.00%** | 0.00% |

**Realism's median region area is one pixel.** It produces 85× more regions than the next worst row
and is the only row above zero. The four rounds of "which style is least paintable" are over: there
is one.

### 2.2 The 51.30% / 40.84% disagreement is not resolution

The brief inherits two figures that differ by ten points and attributes the gap to image size. It
is not size. Rendering the *same* photographs at three sizes, each at its own derived default mark:
`[verified]`

| long edge | mean short edge | mean mark | mean sub-mark share |
|---|---|---|---|
| 480 | 395 | 2.53 | **51.22%** |
| 960 | 759 | 4.87 | **52.99%** |
| 1800 | 1445 | 9.87 | **52.75%** |

**A 3.75× change in linear resolution moves the figure by 1.8 points.** `[verified]` The reason is
structural and worth stating once: the default mark is `min(width, height) / 150`, so the threshold
scales as the square of the image while the detail that lands below it scales the same way. **The
sub-mark share is scale-free by construction.** Per image the picture is more mixed — a portrait
rises 30.8% → 37.1% → 37.3% and a bread still life falls 83.7% → 83.0% → 78.9% — but there is no
systematic direction.

**What does move the number is the short edge at a fixed long edge**, because that is what sets the
mark, and therefore the threshold, for a given amount of subject detail. The Tonalism round's track 4
recorded a mean mark of **4.00** on 14 photographs (four of them panoramas with short edges under
440 px) and got 40.84%; its track 3 used a **800 px short edge** throughout and got 51.30%. My
corpus has a mean mark of 4.87 and gives 52.99%. **The three figures are consistent once the mark is
read off**, and the honest statement is: *Realism sits near 50% at the app's own default, and the
figure rises with the picture's short edge.*

**So: 53%, and stop attributing the spread to resolution.** `[verified]`

### 2.3 Per image, the range is enormous and it is about texture

| photograph | short edge | mark | below own mark² |
|---|---|---|---|
| iris, macro, defocused background | 638 | 4 | **16.51%** |
| harbour, boats and flat water | 960 | 6 | 28.36% |
| portrait, old man | 640 | 4 | 37.05% |
| portrait, Kondh woman | 737 | 5 | 38.28% |
| lake reflection panorama | 388 | 3 | 39.44% |
| market crowd | 960 | 6 | 46.09% |
| cathedral nave | 960 | 6 | 46.17% |
| cityscape at dusk | 721 | 5 | 52.68% |
| portrait with hillside town | 640 | 4 | 54.02% |
| tenement doorway | 960 | 6 | 54.36% |
| snow and bare trees | 641 | 4 | 64.74% |
| mountain and conifer shore | 579 | 4 | 65.30% |
| white horse in grass | 960 | 6 | 69.74% |
| bread and crumb on granite | 960 | 6 | 83.00% |
| **forest floor, grass and stump** | 640 | 4 | **99.15%** |

The spread is 6× and it tracks one thing: **how much of the picture is fine repeating texture at or
below the mark.** Grass, crumb, snow-covered twigs and stone speckle are unpaintable at
short-edge/150 no matter what the pipeline does; a defocused macro background is trivially
paintable. This is the variable a user could act on and the app does not surface.

---

## 3. Genuine detail versus quantiser speckle

The parent README records a local probe showing the converter amplifies input noise — 33.0% of
adjacent 6-bit input bins map to a different mixture, median ΔE 4.39 when they flip. I tested that
mechanism directly against Realism's fragmentation.

**Method.** Run the **real** `EdgePreservingFloor` instance from the real registry row over the
source buffer with the real `ParameterValues` and a `RenderContext` at the render's own mark — this
is exactly the buffer the mapper sees. Render the same photograph through the real pipeline. Then
score every *output* boundary pair (adjacent pixels of different output colour) by the ΔE the
**floored input** carried across it. A pair the eye could not have seen in the input but which
separates two mixtures in the output is manufactured. `[verified]`

Mean over the 15 photographs: `[verified]`

| | value |
|---|---|
| output boundary pairs per 1000 px | 987 |
| mean **input** ΔE across an output boundary | **7.20** |
| mean **output** ΔE across the same boundary | **9.60** |
| amplification | **×1.45** |
| output boundaries on an input step below 1.0 ΔE | **19.4%** |
| output boundaries on an input step below 2.3 ΔE (one JND) | **39.9%** |
| **sub-mark area in regions whose *entire* boundary is sub-JND** | **13.1%** [0.2–25.3] |

Three readings.

1. **The amplification reproduces, at a smaller magnitude on real photographs than on synthetic
   noise.** ×1.45 here against the parent README's ×1.69 on a σ1 noise fixture. `[verified]`
   Confirmation, not correction.

2. **Two-fifths of the output's boundaries are invisible in the input.** 39.9% of the picture's
   colour changes sit on a step the eye would not have seen. That is a real defect and it is what
   makes the render look grainy.

3. **But it is not what makes it unpaintable.** Only **13.1%** of the sub-mark *area* lies in
   regions bounded entirely by sub-JND steps. The other **86.9%** touches at least one genuine
   source step. `[verified]` And the definition is deliberately permissive in the direction that
   makes speckle look big — one genuine boundary is enough to disqualify a region — so **13.1% is an
   upper bound on speckle and 86.9% is a lower bound on genuine detail.**

**The consequence for the build order.** Denoising harder cannot fix this. The image with the worst
score (99.15%, forest floor) has the lowest speckle share in the corpus (**0.2%**) and the highest
input boundary contrast (16.40 ΔE): its fragmentation is grass, and grass is genuinely finer than
one mark. **Realism's fragmentation is a mark-size problem wearing a noise problem's clothes.**

---

## 4. The paintability floor: what a real canvas scores

This is the measurement I did not expect to need and it reframes the other five sections.

A Realist canvas is, by construction, a picture that was painted. Put one through the app's own
Realism row and score it with the app's own `PaintabilityMetrics` at its own derived mark:
`[verified — computed 2026-07-31, real `StylePipeline.Render`, real registry row]`

| work | mark | mark² | regions | below own mark² |
|---|---|---|---|---|
| Millet, *The Angelus* (1857–59) | 5 | 25 | 61,140 | **21.80%** |
| Breton, *Le chant de l'alouette* (1884) | 6 | 36 | 108,569 | 25.95% |
| Eakins, *The Gross Clinic* (1875) | 6 | 36 | 146,463 | 30.41% |
| Courbet, *A Burial at Ornans* (1849–50) | 3 | 9 | 87,404 | 31.93% |
| Bonheur, *Labourage nivernais* (1849) | 3 | 9 | 99,214 | 32.02% |
| Repin, *Barge Haulers on the Volga* (1870–73) | 3 | 9 | 89,597 | 32.59% |
| Eakins, *Max Schmitt in a Single Scull* (1871) | 4 | 16 | 114,032 | 33.93% |
| Leibl, *Drei Frauen in der Kirche* (1882) | 6 | 36 | 255,818 | 39.87% |
| Millet, *The Gleaners* (1857) | 5 | 25 | 147,527 | 46.00% |
| Daumier, *The Third-Class Carriage* (c.1862–64) | 5 | 25 | 148,913 | 48.63% |
| Menzel, *Das Eisenwalzwerk* (1872–75) | 4 | 16 | 154,561 | 52.30% |
| Courbet, *Bonjour Monsieur Courbet* (1854) | 6 | 36 | 269,719 | 57.25% |
| Bastien-Lepage, *Les Foins* (1877) | 6 | 36 | 313,775 | 70.70% |
| Homer, *The Veteran in a New Field* (1865) | 4 | 16 | 273,541 | **71.75%** |
| **Realist mean (14)** | | | **158,591** | **42.51%** |
| *control:* Whistler, *Falling Rocket* (1875) | 6 | 36 | 181,461 | 34.37% |
| *control:* Gauguin, *Vision after the Sermon* (1888) | 5 | 25 | 223,473 | 60.88% |
| *control:* van Gogh, *Wheatfield with Crows* (1890) | 3 | 9 | 205,224 | **66.80%** |
| **photographs (15), same row, same metric** | | | 269,973 | **52.99%** |

**Every canvas fails.** The best is Millet's *Angelus* at 21.80%, which is still 24× the ceiling
`EveryRegisteredStyleIsPaintable` records for Realism. Van Gogh's *Wheatfield with Crows* — a
picture made of nothing but visible marks, each of them larger than one mark² of its own
reproduction — scores 66.80%, worse than 13 of my 15 photographs.

Three consequences, and the third is the one for the app's claim.

1. **`FractionInRegionsSmallerThan` on a converted image does not measure "could a human have
   painted this".** It measures how finely the converter subdivides a continuous-tone field. Feed it
   a picture that *was* painted and it says no. `[verified]` **This is a limit on the metric, not a
   reason to abandon it** — but every prior round has read the number as a statement about the
   picture, and it is a statement about the pipeline.

2. **Canvases and photographs are one order apart, not two.** 42.51% vs 52.99%, ranges 21.8–71.8 and
   16.5–99.2. The gap is real (canvases are more consolidated, §1.3) and it is small. **A converter
   that scores a Courbet at 32% is not going to score a snapshot at 3%.**

3. **The honest ceiling for a pre-map-only Realism row is around 40%**, and that is where it already
   is. Everything below that has to come from slot 5, because slot 5 is the only place that can
   assert a minimum region size. Doing nothing and accepting 53% is not "promising an unpaintable
   picture" in the sense the brief supposes — a Millet would fail the same test — but it *is* three
   times more fragmented than the least fragmented canvas in the corpus, at a median region area of
   **one pixel**, and that part is indefensible. §5.5.

---

## 5. `SmallRegionMerge` on Realism

### 5.1 It works, exactly as the last two rounds promised

Every variant is the real `StyleRegistry` Realism row with real stage instances substituted through
`WithDefaults` and the record's `with` expression. Mean over the same 15 photographs, each at its own
derived mark. `[verified]`

| variant | **below mark²** | regions | median | colours | bnd/1000 | mean ΔE | hard ≥10 | in masses ≥mark² | largest mass | notan | changed | changed ΔE |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **shipped** | **52.99%** | 269,973 | 1.0 | 1,168 | 986.9 | 9.60 | 30.1% | 79.9% | 11.0% | 35.48 | — | — |
| **+ `SmallRegionMerge`** | **0.000%** | **4,078** | **58.9** | 399 | 249.8 | 12.99 | 36.9% | **100.0%** | 16.6% | 36.66 | **47.5%** | **18.13** |
| + merge ×2 | 0.000% | 4,078 | 58.9 | 399 | 249.8 | 12.99 | 36.9% | 100.0% | 16.6% | 36.66 | 47.5% | 18.13 |
| floor strength 2 | 45.20% | 217,594 | 1.0 | 1,067 | 874.8 | 9.17 | 28.1% | 84.3% | 11.8% | 34.52 | 41.0% | 5.66 |
| floor strength 3 | 39.58% | 182,633 | 1.0 | 1,012 | 796.6 | 8.78 | 26.4% | 87.2% | 12.1% | 33.75 | 51.2% | 6.52 |
| floor strength 5 | 32.03% | 138,517 | 1.0 | 942 | 689.8 | 8.12 | 23.6% | 91.4% | 13.4% | 32.64 | 60.1% | 7.79 |
| floor ε 0.10 | 48.36% | 224,933 | 1.0 | 1,028 | 907.5 | 8.48 | 25.4% | 85.2% | 11.7% | 34.26 | 34.9% | 6.18 |
| floor ε 0.20 | 43.92% | 181,611 | 1.0 | 941 | 830.7 | 7.04 | 19.0% | 91.5% | 12.7% | 33.02 | 43.6% | 8.06 |
| floor ε 0.30 | 41.82% | 161,458 | 1.0 | 898 | 793.4 | **6.32** | **15.4%** | 94.8% | 13.1% | 32.59 | 45.5% | 9.05 |
| floor s3 ε 0.10 | 32.05% | 122,671 | 1.0 | 864 | 678.3 | 6.73 | 17.5% | 95.2% | 14.2% | 32.04 | 58.6% | 8.75 |
| **floor s3 ε 0.10 + merge** | **0.000%** | 4,663 | 62.9 | 418 | 290.9 | **8.75** | **24.5%** | 100.0% | **13.7%** | 32.44 | 59.1% | 11.45 |
| floor ε 0.10 + merge | 0.000% | 4,438 | 58.7 | 414 | 267.9 | 11.92 | 33.8% | 100.0% | 13.7% | 36.41 | 48.9% | 15.78 |

Per image, the merge is unanimous: **0.0000% on 15 of 15, exactly zero, in one pass, and a second
pass leaves the buffer byte-identical on 15 of 15.** `[verified]` The Tonalism round's clearing of
this debt reproduces on a fifth style and a sixteenth photograph set.

**The floor is the second lever and neither of its parameters is saturated at the declared default.**
Realism runs `EdgePreservingFloor` at strength **1.0** and ε **0.05** — the stage's own declared
defaults, with no `WithDefaults` call at all. It is the weakest floor in the registry. Strength
1 → 5 buys 21 points; ε 0.05 → 0.30 buys 11 points *and halves the hard-boundary share*.

### 5.2 What it costs, and the cost is unlike the other styles

Registering the merge changes **47.5% of the picture at a mean ΔE of 18.13.** Averaged over the whole
frame that is ΔE 8.6 — against a median candidate nearest-neighbour spacing of 1.70 ΔE (parent
README), five neighbours' worth of movement across half the image, on the row whose registry doc
comment promises "no difference from the single-path converter that predates it".

Two more costs:

- **The merge hardens the picture.** Mean boundary ΔE 9.60 → 12.99, hard share 30.1% → 36.9%.
  Report 03's "nothing in slots 1 or 5 can soften an edge" now holds on a sixth style.
- **Thin structure goes.** Thin-dark-structure retention is **38.3%** across the corpus (15.9% on the
  forest floor, 61.1% on the doorway) `[verified]`. The Tonalism round's rule holds unchanged: **an
  area opening preserves length, not thinness.** Rigging survives, crumb does not.

**And it over-consolidates against the canvases, in one direction only.** Shipped Realism puts
**79.9%** of area in masses of at least one mark², which is *inside* the Realist-canvas envelope of
77.5% [54.3–88.8]. The merge takes it to **100.0%**, past the corpus maximum of 88.8%. But the
largest single mass goes 11.0% → 16.6% against a canvas mean of 21.6%, so the row is still *below*
the canvases on mass size. **The merge does not make one big blob; it abolishes small ones.** That
is the same over-consolidation the Tonalism round found, from the same measurement, and it is again
an argument against raising the threshold — not against having the stage.

### 5.3 The merge absorbs into the largest neighbour, and that is the defect

`SmallRegionMerge.LargestNeighbour` (`SmallRegionMerge.cs:182-216`) picks, among a sub-mark region's
neighbours, the one with the **largest area** that already clears the threshold, falling back to the
largest of any size. Colour never enters. `[verified — read from the source]` The Post-Impressionism
round's proposed fix said "prefer the neighbour with the smallest CIELAB distance among those that
will clear the threshold"; the rewrite in the working tree implemented the union-find half of that
recommendation and not this half.

To measure the difference I wrote a prototype with the identical structure — same smallest-first
`SortedSet`, same union-find, same adjacency maintenance — with the target choice as a switch.
**Told to rank by area it reproduces the real stage exactly**: 4,078 regions, median 58.9, 399
colours, mean ΔE 12.99, hard 36.9%, largest mass 16.6%, changed 47.5%, changed ΔE 18.13 — every
figure identical to the shipped instance. `[verified]` That is the gate that makes the second row
trustworthy.

Mean over the same 15 photographs, on top of the shipped Realism render: `[verified for the first
row, prototype for the other two]`

| absorbing neighbour chosen by | below mark² | regions | median | colours | mean ΔE | hard ≥10 | largest mass | changed | **changed ΔE** | **thin detail kept** |
|---|---|---|---|---|---|---|---|---|---|---|
| **area** — real `SmallRegionMerge` | 0.000% | 4,078 | 58.9 | 399 | 12.99 | 36.9% | 16.6% | 47.5% | **18.13** | **38.3%** |
| area — prototype (gate) | 0.000% | 4,078 | 58.9 | 399 | 12.99 | 36.9% | 16.6% | 47.5% | 18.13 | 38.3% |
| **CIELAB distance** — prototype | **0.000%** | **3,209** | **121.3** | 376 | **11.66** | **33.3%** | **14.9%** | 47.1% | **15.19** | **46.4%** |

**The colour-nearest rule is better on every axis measured and worse on none.** It still reaches
exactly zero; it produces *fewer* regions with a **2.1× larger median area**; boundary ΔE falls
12.99 → 11.66 and the hard-boundary share 36.9% → 33.3%; the largest value mass falls 16.6% → 14.9%,
i.e. it over-consolidates *less*; the colour displacement of the pixels it moves falls **18.13 →
15.19, a 16% reduction**; and thin-dark-structure retention rises **38.3% → 46.4%**, a fifth more of
the picture's fine structure surviving. `[verified]`

The mechanism is visible in §5.4 and it is intuitive: absorbing by area sends every small feature
into whichever neighbour happens to be biggest, which on a photograph is usually a large flat field
of an unrelated colour, and the absorbed region then drags that field's colour across a boundary the
source never had. Absorbing by colour keeps the merge inside the local colour neighbourhood, so
merged regions coalesce with things they already resembled — which is also why the region count
*falls*: like-coloured fragments join each other instead of being scattered into whichever giant
neighbour they touch.

**This is the cheapest improvement anywhere in this round: one comparison swapped in one method.**

### 5.4 What the renders show, and it demotes the obvious pick

I rendered five variants of six photographs and looked at the PNGs. `[verified — inspected
2026-07-31]`

- **The bare merge is destructive on textured subjects, and not marginally.** On the forest-floor
  photograph (the 99.15% image) `+merge` is unrecognisable: the stump dissolves and the grass becomes
  military camouflage in four greens. On the harbour, the moored boats become white blobs and the
  middle-distance quay is gone. On the market crowd, faces disappear. This is not a picture a user
  would accept from a style called Realism.
- **The `LargestNeighbour` defect is visible, not only numeric.** On the tenement doorway a small
  red-and-white sign on the dark door is swallowed into a single white patch, and the cream wall
  turns pink — a large-area neighbour absorbing a smaller, differently coloured one. **A
  selection-only stage that never leaves the candidate set can still change the colour of a field.**
- **Pre-flattening rescues it.** `floor strength 3 + ε 0.10 + merge` is a different picture from
  `+merge` alone on every subject: the stump is legible, the boats separate, the faces survive, the
  doorway keeps its ornament. It reads as a broad-brush oil sketch rather than a posterisation. That
  matches the numbers — mean boundary ΔE 8.75 against 12.99, hard share 24.5% against 36.9%.
- **ε 0.10 alone is nearly invisible.** Side by side with the shipped render it is a marginally
  cleaner photograph. It is cheap and it is not a style.
- **Threshold multipliers above 1.0 make it worse, visibly.** At ×4 and ×9 every subject moves
  further toward camouflage with no metric gain (the share is already zero).

**Ten minutes of looking demoted the obvious pick.** "Register `SmallRegionMerge` on Realism, one
line" is the recommendation every statistic in §5.1 supports and it is wrong on its own.

### 5.5 The consequence for what the app claims

`StyleRegistry.Default`'s doc comment says Realism is "exactly what the converter did before styles
existed… A user who never opens a style picker should see no difference from the single-path
converter that predates it." The v1 scope adds "a second invariant… every output region must be a
mark a human could execute."

**Those two sentences cannot both be true.** The single-path converter puts 53% of an ordinary
photograph in regions below one mark², at a median region area of one pixel. Making that invariant
hold costs 47.5% of the pixels at ΔE 18. `[verified]`

The honest options are three, and the app should pick one out loud rather than inherit the current
one by omission:

- **Keep the contract, drop the invariant for this row**, and say in the doc comment that Realism is
  the unfiltered mapping and is not claimed to be executable. Cheapest; currently true; currently
  unstated.
- **Keep the invariant, break the contract**, register the merge behind a stronger floor, and accept
  that the default style now changes half the picture. §9 pick 1.
- **Keep both by moving the mark** — a user-facing statement that the mark slider is the paintability
  control, with the render telling the user which images need a coarser one. Nothing in the UI does
  this and §2.3 shows the range is 6×.

---

## 6. `MarkScale` 1.0 — inert, and not for the reason last round found

`MarkPixels` reaches four consumers: `EdgePreservingFloor.cs:63` (→
`PalettePhotoConverter.FloorRadius`), `SmallRegionMerge.cs:27`, `ContourLines.cs:28` and
`GroundFill.cs:26`. `[verified — grepped]` **Realism registers only the first**, so `MarkScale` sets
one integer: a guided-filter window radius.

Sweeping it with slot 5 empty, mean over the 15 photographs: `[verified]`

| MarkScale | mean mark | mean floor radius | regions | median | colours | mean ΔE | below own mark² |
|---|---|---|---|---|---|---|---|
| 0.5 | 2.43 | 1.40 | 273,599 | 1.0 | 1,188 | 9.43 | 41.32% |
| 0.8 | 3.89 | 1.93 | 270,359 | 1.0 | 1,176 | 9.56 | 50.03% |
| **1.0 (shipped)** | **4.87** | **2.40** | **269,973** | **1.0** | **1,168** | **9.60** | **52.99%** |
| 1.3 | 6.33 | 3.33 | 271,223 | 1.0 | 1,160 | 9.68 | 56.09% |
| 1.6 | 7.79 | 3.87 | 273,200 | 1.0 | 1,160 | 9.70 | 58.68% |
| 2.0 | 9.73 | 4.87 | 275,745 | 1.0 | 1,156 | 9.75 | 61.16% |
| 2.5 | 12.17 | 6.27 | 279,923 | 1.0 | 1,149 | 9.77 | 63.83% |
| 3.0 | 14.60 | 7.33 | 282,532 | 1.0 | 1,144 | 9.79 | 66.01% |

**A 6× change in the requested mark moves the region count by 3.3%, the median region area not at
all, the colour count by 3.7% and mean boundary ΔE by 0.36.** The sub-mark share climbs 25 points
purely because the bar it is measured against grows as mark². Region counts rise *with* the mark —
the same inverted sign the Tonalism round recorded.

**And the mechanism is not the one that round identified.** It attributed Tonalism's inert MarkScale
to `FloorRadius = Max(Round(m/2), 1)` collapsing 1.0 and 1.2 onto the same integer. Here the radius
genuinely moves, 1.40 → 7.33 mean, and 14 of 15 images render **different pixels** at MarkScale 1.0
versus 1.3. `[verified]` **The guided filter's radius is itself near-inert on region structure at
the strengths this row uses.** The parameter that does work is ε: 0.05 → 0.30 takes the share
52.99% → 41.82%, the mean boundary ΔE 9.60 → 6.32 and the hard share 30.1% → 15.4% — for the same
two passes and no extra cost.

**That is the third confirmation, on a third style, that the guided filter's control is its edge
threshold and not its window** — and the first on the *global* filter rather than a focal one. The
Tonalism round's correction 9 should be generalised accordingly (§8).

**With slot 5 filled, `MarkScale` becomes a real control**, exactly as it did for Tonalism: median
region area 13.4 px at 0.5 → 551.9 px at 3.0, a **41× range**, with the share at 0.00% throughout.
`[verified]`

**Ruling: keep 1.0 and fill slot 5.** `[inferred]` 1.0 is the do-nothing baseline every other
`MarkScale` in the registry is defined against, and moving it before there is a consumer reproduces
the trap three rounds have named. There is no measured argument for a different number: §1.3 finds
realist canvases have larger masses than photographs, but the app's mass measure does not separate
Realism from Tonalism (§1.3 reading 4), so it cannot set the constant either.

---

## 7. The shipped gate reads a four-pixel threshold

`StyleBehaviourTests.EveryRegisteredStyleIsPaintable` records Realism's ceiling at **0.030** — 3.0%.
It renders `StyleTestFixtures.BuildNoisyGradient(256, 256, 3.0)` at `markPixels = 0`, so
`DefaultMarkPixels(256, 256) = 2`, mark = 2 × 1.0 = **2.0** and `markSquared = Round(4.0) = 4`.
`[verified — read from the source]`

So the gate for Realism reads: *fewer than 3% of pixels in regions under **four** pixels, on a
synthetic gradient.* The same style on a real photograph at the app's own default scores **52.99%
under 24 pixels** (mean mark² over my corpus).

Running the gate's own fixture through the real pipeline: `[verified — computed 2026-07-31,
`BuildNoisyGradient(256, 256, 3.0)` reproduced seed-for-seed]`

| style | mark | mark² | measured | ceiling |
|---|---|---|---|---|
| **Realism** | **2.0** | **4** | **2.5757%** | 3.0% |
| Tonalism | 2.4 | 6 | 0.0000% | 0.9% |
| Fauvism | 2.6 | 7 | 0.0000% | 8.5% |
| Post-Impressionism | 3.2 | 10 | 0.0000% | 1.3% |
| Abstract | 5.0 | 25 | 0.0000% | 7.0% |

**Realism is the only style the gate still binds on, and it binds at four pixels with a 16% margin.**
Its threshold is also the smallest in the suite — Tonalism's is 6 px, Post-Impressionism's 10,
Abstract's 25 — because the gate multiplies the fixture's tiny base mark by the style's own
`MarkScale`, and Realism's is 1.0. **The one row that most needs the gate gets the loosest version
of it.** A four-pixel threshold is barely distinguishable from "no isolated pixels", and the four
rows that would fail a real threshold pass this one by construction: they all reach exactly zero,
so their ceilings of 0.9%, 8.5%, 1.3% and 7.0% now measure nothing at all.

On the committed golden, Realism measures **5.42%** at mark 4.0 with 425 regions, median area 3 and
161 colours — reproducing the Fauvism, Post-Impressionism and Tonalism rounds' published figures
**exactly**, across four sessions and four independent probe implementations. `[verified]` That is
the cross-check for everything else in this report. It is also the fourth round in which a synthetic
fixture understated a spatial defect: 3.0% ceiling, 5.42% golden, **52.99%** photographs.

**One thing has changed and it is worth recording.** In this working tree, Realism is the **only**
style above 0.00% on the committed goldens: Tonalism 0.00% (93 regions, median 136), Fauvism 0.00%
(107, 79), Post-Impressionism 0.00% (101, 102), Abstract 0.00% (9, 1741). `[verified]` **The
Tonalism and Fauvism golden PNGs in the working tree are modified and their prior published rows no
longer apply.**

---

## 8. Where this corrects or extends prior research

**Corrects:**

1. **The sub-mark share does not measure whether a picture could be painted.** Fourteen Realist
   canvases through the app's own Realism row score **42.51%**; van Gogh's *Wheatfield with Crows*
   scores 66.80%; Millet's *Angelus*, the best in the corpus, scores 21.80% — 24× the ceiling the
   shipped test records. `[verified, §4]` Four rounds have read this number as a property of the
   output picture. It is a property of the converter's response to continuous tone. The metric is
   still the right gate; the *interpretation* in every prior round's headline is too strong.

2. **The 51.30% / 40.84% disagreement is not resolution.** Rendering the same photographs across a
   3.75× linear range moves the mean by 1.8 points (51.22 / 52.99 / 52.75), because the default mark
   scales with the short edge and the metric is therefore scale-free. The prior spread is the
   corpora's short edges — mean mark 4.00 versus an 800 px short edge — and their subject mix.
   `[verified, §2.2]`

3. **The Tonalism round's diagnosis of an inert `MarkScale` generalises, but its mechanism does
   not.** That round attributed the inertness to `Round(mark/2)` collapsing adjacent scales onto one
   radius. On Realism the radius moves 1.40 → 7.33 across the sweep, 14 of 15 images render
   different pixels, and the region count still moves 3.3%. **The guided filter's radius is
   near-inert on region structure in its own right.** `[verified, §6]`

4. **`SmallRegionMerge` absorbs into the largest neighbour, not the nearest in colour, and it is a
   live defect in the stage three rounds have recommended.** `LargestNeighbour` ranks by area alone
   (`SmallRegionMerge.cs:182-216`). The Post-Impressionism round specified the colour-nearest rule as
   part of its fix; the working-tree rewrite implemented the union-find and not that. `[verified —
   read from the source, §5.3]`

5. **A "selection-only" post-map stage can still change the colour of a field.** `SmallRegionMerge`
   never leaves the candidate set, so the invariant holds — and on the tenement doorway it turns a
   cream wall pink and swallows a red sign into white. The four-category invariant table's "post-map,
   selection-only: safe" is safe *for the invariant* and is not safe for the picture. Worth a clause
   in the table. `[verified, §5.4]`

**Extends:**

6. **The Tonalism round's cleared merge debt reproduces on a fifth style and a sixteenth corpus.**
   Exactly 0.000000 in one pass on 15 of 15 photographs, byte-identical on a second pass on 15 of 15.
   `[verified, §5.1]`

7. **The over-consolidation finding reproduces on Realism, in the same direction and with the same
   ruling.** Shipped Realism sits at 79.9% of area in masses ≥ mark², inside the Realist-canvas
   envelope 77.5% [54.3–88.8]; the merge takes it to 100.0%, past the maximum. Do not raise the
   threshold. `[verified, §5.2]`

8. **"An area opening preserves length, not thinness" holds on a sixth style**, and the subject that
   loses most is different: for Tonalism it was twigs, for Realism it is crumb, grass and stone
   speckle — texture rather than tracery. Thin-dark retention 38.3%. `[verified, §5.2]`

9. **Report 03's "nothing in slots 1 or 5 can soften an edge" holds again.** The merge raises mean
   boundary ΔE 9.60 → 12.99. The only softening lever measured anywhere is the floor, and on Realism
   its ε is twice the lever its strength is: ε 0.05 → 0.30 halves the hard-boundary share for two
   passes, while strength 1 → 5 costs five passes to remove a quarter of it. `[verified, §5.1]`

10. **Sigaki et al. does not name Realism**, reproducing the same negative result for the fourth
    consecutive round on the fourth consecutive movement. `[verified, §1.1]`

**New, and not in any prior report:**

11. **Realist canvases are less flat at mark scale than photographs, by 3.3×** — 5.3% against 17.6%
    of area holding within 2 ΔE across a mark-wide window. `[verified, §1.3]` This is the measurement
    that refutes "realism means invisible brushwork", and the statistic is four lines of code.

12. **Value-mass consolidation does not separate Realism from Tonalism** — largest mass 21.6% vs
    23.3%, in-masses 77.5% vs 84.4%, on the same construction. `[verified, §1.3]` Two rounds have
    used this measure to justify a style decision; it is a paintability measure, not a style one.

13. **At least 86.9% of Realism's fragmentation sits on genuine source detail, not quantiser
    speckle.** `[verified, §3]` The worst image in the corpus (99.15%) has the *lowest* speckle share
    (0.2%). Denoising cannot close this.

**Could not settle:**

- Whether any conservation study reports a stroke width, length or area in physical units for any
  Realist painter. Searched; negative. The Post-Impressionism round found exactly one such figure in
  the literature and its subject is van Gogh.
- Whether the 3.3× mark-scale roughness gap survives calibrated reproductions. Debt 2.

---

## 9. Picks, ranked by payoff ÷ cost

Line counts are C#-from-scratch estimates in the style of `Imaging/Styles/Stages/`, excluding UI.

### 1. Floor `edge` 0.05 → 0.10 and `strength` 1.0 → 3.0 **first**, then register `SmallRegionMerge` — three lines

**Slots 1 and 5.** Add a `WithDefaults` call to the Realism row (it currently has none) and put
`new IPostMapStage[] { new SmallRegionMerge() }` in slot 5. **The order of the argument matters more
than the order of execution:** the floor change is what makes the merge acceptable.

*Evidence.* Sub-mark share **52.99% → 0.000000** on all 15 photographs, one pass, idempotent.
`[verified, §5.1]` Against the bare merge, pre-flattening takes mean boundary ΔE **12.99 → 8.75**,
hard-boundary share **36.9% → 24.5%** and largest value mass **16.6% → 13.7%**, i.e. closer to the
shipped picture's boundary quality on every axis while still reaching zero. And I rendered both on
six photographs: the bare merge destroys a forest floor, a harbour and a market crowd; the pairing
does not. `[verified, §5.4]`

*Why ε 0.10 and strength 3.0.* ε is the cheaper parameter — 0.05 → 0.30 alone halves the hard-boundary
share in the same two passes — but §5.1 shows ε 0.10 + merge still leaves mean ΔE 11.92 against the
pairing's 8.75. Strength 3.0 is Fauvism's and Post-Impressionism's registered value, so it puts
Realism at the registry's middle rather than at either end. **Strength 5.0 was measured (32.03%
alone) and is deliberately not recommended**: it is Abstract's, and a do-nothing row should not carry
the registry's strongest floor.

*The cost, and it should be argued rather than absorbed.* 59.1% of pixels change. Realism's own doc
comment promises the opposite. **Whatever is decided, rewrite that comment** — it is currently false
either way, because a row that leaves 53% of an ordinary photograph in one-pixel regions is not the
converter anyone would defend. §5.5.

*Verification.* `FractionInRegionsSmallerThan(pixels, …, mark²)` must be exactly zero after one
invocation on photographs, and the buffer byte-identical after a second. Regenerate
`Tests/Golden/Realism.png` and **look at it**; expect 425 regions to fall toward Post-Impressionism's
101. Then fix `EveryRegisteredStyleIsPaintable`, which currently reads a **four-pixel** threshold for
this style (§7).

### 2. Make `SmallRegionMerge` absorb into the colour-nearest neighbour — ~10 lines

**Slot 5, shared by all five styles.**

*What.* In `LargestNeighbour`, rank candidates by CIELAB distance to the source region's colour
rather than by area, among those already at or above `minimumArea`, with the same any-size fallback.
The stage already receives the `CandidateSet` and currently ignores it entirely, so the colours are
in hand.

*Evidence.* Measured against a prototype that reproduces the shipped stage exactly when told to rank
by area: sub-mark share stays at **0.000%**, regions **4,078 → 3,209**, median region area
**58.9 → 121.3 px**, mean boundary ΔE **12.99 → 11.66**, hard-boundary share **36.9% → 33.3%**,
largest value mass **16.6% → 14.9%**, colour displacement of moved pixels **18.13 → 15.19 ΔE**, and
thin-dark-structure retention **38.3% → 46.4%**. `[verified, §5.3]` Better on every axis, worse on
none, and it is one comparison.

*Why it is second and not first.* Pick 1 is what makes Realism paintable at all; this makes the
result better. On the renders the improvement is real but partial — the harbour keeps its sky
gradient and the forest floor keeps some of the stump — and it does not on its own make the bare
merge acceptable on Realism.

*Verification.* Sub-mark share must still be exactly zero on every photograph and every golden, and
the stage must still be idempotent. Then regenerate all five goldens and look at them.

*Risk.* It changes four other styles' output. Every one of them currently reaches 0.00%, and the
selection rule cannot change that — the threshold logic is untouched — but all five goldens move and
all five need regenerating and looking at.

### 3. A threshold parameter on `SmallRegionMerge`, default 1.0 — ~40 lines — and leave it at 1.0

**Slot 5.** One `StyleParameter`: a multiplier on `minimumArea`, default 1.0, an exact no-op.

*What this is not.* I expected Realism to be the style that justifies a multiplier **below** 1.0 —
the row where detail matters most, buying most of the paintability for less of the damage. Measured
through the real stage driven at a scaled mark: `[verified]`

| multiplier | below own mark² | regions | median | colours | in masses ≥mark² | mean ΔE | changed | thin detail kept |
|---|---|---|---|---|---|---|---|---|
| ×0.25 | 12.06% | 14,220 | 13.6 | 578 | 97.3% | 11.98 | 37.7% | 48.5% |
| ×0.5 | 5.46% | 6,977 | 31.3 | 474 | 98.8% | 12.40 | 43.4% | 42.7% |
| **×1.0** | **0.00%** | 4,078 | 58.9 | 399 | 100.0% | 12.99 | 47.5% | **38.3%** |
| ×2.0 | 0.00% | 2,209 | 120.7 | 323 | 100.0% | 13.85 | 52.1% | 32.9% |
| ×4.0 | 0.00% | 1,246 | 232.6 | 256 | 100.0% | 14.96 | 56.3% | 27.8% |
| ×9.0 | 0.00% | 613 | 532.9 | 188 | 100.0% | 16.63 | 61.5% | 22.3% |

**The measurement refuses both directions.** Below 1.0 the invariant simply does not hold — ×0.5
leaves 5.46%, which is a different promise, not a cheaper version of the same one. Above 1.0 the
metric cannot improve (it is already zero) and every quality measure degrades; at ×4 and ×9 the
renders are camouflage. `[verified, §5.4]` **Record this as measured and do not build the knob-turn.**

*Why the parameter should exist anyway.* Unchanged from the Tonalism round's argument, now with a
fifth style's data behind it: all three post-map stages declare
`Parameters => Array.Empty<StyleParameter>()`, so slot 5 has no tuning surface and five styles now
register the same stage and get byte-identical behaviour. `[verified — read from the source]` The
first honest use of the knob is a style that wants it **below** 1.0 and is willing to give up the
invariant to get it — and this round says that style is not Realism.

### Not ranked, and deliberately

**A user-facing signal that a photograph is too fine for its mark.** §2.3 shows a 6× spread across
one corpus that tracks subject texture, and §5.4 shows that the failure at the bad end is
subject-destroying rather than merely fragmented. The right control exists (the mark slider) and
nothing tells the user to reach for it. This is a UI item and I have no measurement of what the
signal should be, so it is a note, not a pick.

---

## 10. What not to build

The parent, Abstract, Fauvism, Post-Impressionism and Tonalism lists all still apply. These are
additional, and I went looking for each.

- **Registering `SmallRegionMerge` on Realism on its own, at the shipped floor.** The statistics say
  do it and the renders say no: on a forest floor, a harbour and a market crowd the subject
  disappears. §5.4. **This is the clearest case in five rounds of a pick that only looking could
  demote.**
- **Raising `SmallRegionMerge`'s threshold above one mark² for this style.** The share is already
  zero, so there is nothing to buy, and every quality measure degrades monotonically. §9 pick 3.
- **Lowering it below one mark² to buy detail back.** It leaves 5.46–12.06% and abandons the
  invariant for a partial improvement. If that trade is wanted it should be a stated change to what
  the app promises, not a threshold default.
- **Raising `MarkScale` above 1.0 before slot 5 is filled.** Measured in its purest form yet: a 6×
  change moves the region count 3.3% and the median region area not at all, while the sub-mark share
  climbs 25 points because the bar moves. §6. The same trap the Fauvism, Post-Impressionism and
  Tonalism rounds each named.
- **Spending anything on further denoising to fix the fragmentation.** At least 86.9% of it sits on
  genuine source detail, and the worst image in the corpus has the *lowest* speckle share in it.
  §3. Denoising harder buys points and loses the picture.
- **Reading the sub-mark share as "the app is promising an unpaintable picture" without the canvas
  calibration beside it.** A Millet scores 21.8%, a Bastien-Lepage 70.7%, a van Gogh 66.8%. §4.
  **Always quote the canvas floor beside the app's number**, the way the Tonalism round established
  for the notan gap.
- **Any orientation, coherence or elongation statistic for this row.** Realist canvases are *less*
  coherent (0.46 vs 0.58) and *less* elongated (2.21 vs 2.73) than the photographs they would be made
  from. §1.3. Carried forward from the Post-Impressionism round's correction 11 and now confirmed on
  a second style with a directional control (van Gogh at 0.630) proving the measure is not blind.
- **A directional or flow-aligned pre-filter under the Realism label.** It is
  Post-Impressionism's item and this row has no directional claim to test.
- **`ContourLines` on Realism.** Not measured here because the ruling is inherited three times over
  (Post-Impressionism, Tonalism), and because the movement's own definition is facture rather than
  contour. The one canvas in my corpus with visible drawn line — Daumier's *Third-Class Carriage* —
  is **unfinished**, with the squaring grid and underdrawing showing through, and is the reason it is
  flagged in §13 rather than an argument for the stage.
- **A "Realist palette" preset.** Same rejection, same reason, as the Fauvism round's viridian and the
  Tonalism round's *Sea and Rain* five: the historical pigments are mostly earths and lead white that
  a user cannot select, and the substitute lists are part `ReflectanceDerived`.
- **Chasing "invisible brushwork" as this row's target.** It is academic finish, not Realism, and the
  measurement says realist canvases are the rougher pictures. §1.
- **Treating the shipped `EveryRegisteredStyleIsPaintable` ceiling for Realism as evidence of
  anything.** It reads a **four-pixel** threshold — the loosest in a suite the last four rounds have
  each called too loose. §7.
- **Amplitude-spectrum slope, fractal dimension, or any global texture statistic as this row's
  acceptance test.** Carried forward from the Tonalism round's correction and reinforced by §4: a
  statistic that a real Courbet fails cannot gate a render.

---

## 11. Accuracy warnings

Read these before quoting any figure.

- **My canvas corpus is 14 uncalibrated web reproductions of varnished, aged oil paintings**, and
  finding 1.3 (mark-scale roughness) is exactly the kind of measurement reproduction grain,
  craquelure and JPEG artefacts inflate. The 3.3× direction is large and 12 of 14 canvases agree; the
  magnitude is soft. This is debt 2.
- **Reproduction scale is a confound in §1.3 that the mark normalisation only partly removes.** A
  960 px reproduction of the 3.15 m *Burial at Ornans* resolves 3.3 mm per pixel; the same file size
  for the 55 cm *Angelus* resolves 0.6 mm. The mark-relative statistics (flat-at-mark, in-masses,
  largest mass) are normalised to the picture; the structure-tensor coherence, which uses a fixed
  3×3 Sobel and a radius-2 box, **is not**, and its 0.46 vs 0.58 gap should be treated as suggestive
  rather than measured.
- **Every figure here is on the six-paint `StyleTestFixtures.SixPaints()` fixture.** A larger palette
  produces more candidates and, by the mechanism in §3, more boundaries — so 52.99% is if anything an
  under-statement for a user with 12 paints. Unmeasured.
- **The whole converter runs on gamut-mapped 8-bit colour** — `MixtureBuilder.RenderMixture` goes
  through `ToDisplayColor`, mean 3.35 ΔE from unmapped spectral Lab, and `SpectralRenderer`'s doc
  comment denies it. Every ΔE in this report is in that space, which is the right one for comparing
  outputs and the wrong one for comparing against physical paint.
- **"Speckle" in §3 is defined permissively toward speckle** — one genuine boundary disqualifies a
  region — so 13.1% is an upper bound and 86.9% a lower bound. The JND threshold of 2.3 ΔE is a
  convention, not a measurement in this colour space.
- **§5.3's colour-nearest figures come from a prototype I wrote, not from shipped code.** It
  reproduces the shipped stage exactly when told to rank by area, which is the gate that makes the
  comparison trustworthy, but it has not been reviewed and it has not been run against the goldens.
- **The visual pass is six photographs at 960 px judged by one agent**, and it already overturned the
  round's obvious pick. Nobody has compared a full-resolution conversion against a Realist canvas
  side by side.
- **The Daumier is unfinished.** *The Third-Class Carriage* (Metropolitan, c.1862–64) carries visible
  underdrawing and squaring lines. It is in the corpus because rejecting it would bias the set toward
  finished Salon pictures, but its line and mass statistics are not those of a finished canvas, and
  its 51.7% largest value mass is the corpus maximum by 21 points.
- **The Courbet reproduction problem.** *The Stonebreakers* was destroyed in 1945 and every image of
  it is a photograph of a photograph; it was rejected on inspection (§13). *Bonjour Monsieur Courbet*
  survives at only 1147 × 1100 in the WGA file, which is the lowest-resolution canvas retained.

---

## 12. Verification debt

Ranked by how much clearing each would change a decision above.

1. **Render pick 1 at full size on more than six subjects and look at it, next to a Realist canvas.**
   It gates the top of the build order, it is an hour's work, and in this round ten minutes of
   looking already demoted the recommendation that three pages of statistics supported. Include at
   least one fine-texture subject (grass, crumb, snow) and one portrait.
2. **Calibrated reproductions for the canvas corpus.** Finding 1.3 — the 3.3× mark-scale roughness
   gap that refutes the invisible-brushwork premise — rests on uncalibrated JPEGs of varnished
   paintings. Two or three colour-managed museum downloads at known physical dimensions would settle
   it and the §4 floor together. **This is the fifth consecutive round to record a shared,
   provenance-checked, committed corpus as an uncleared debt.**
3. **Measure pick 2 against all five goldens and all five styles' photographs.** My colour-nearest
   figures are Realism-only. It changes every style that registers the merge, which is now four of
   five, and none of them has been checked.
4. **A primary conservation source for Courbet's paint application.** The Brooklyn Museum object page
   and the Art UK essay were both reached through search summaries; Art UK returned **403** on direct
   fetch. §1.1's palette-knife claims are `[relayed]` and they carry the historical half of finding 2.
5. **Whether the app's Realism row and the 1848 movement are the same thing.** Track 2 owns this. On
   brushwork alone the answer is no — the movement's defining property is visible facture and the row
   delivers the photograph — and if track 2 rules that the row means fidelity rather than the
   movement, §1's corpus is the wrong calibration target and §1.3 should be re-read as a control
   rather than a target.
6. **Whether the merge's colour damage is uniform across palettes.** Everything here is the six-paint
   fixture. A 12-paint palette has a denser candidate cloud, so the colour-nearest neighbour is nearer
   and `LargestNeighbour`'s penalty may shrink — or the extra candidates may fragment further and make
   it worse. One probe run.
7. **Whether the "flat at mark scale" statistic separates any two movements.** It separates canvases
   from photographs cleanly. Whether it separates Realism from Impressionism, or from Tonalism, is
   unmeasured and would tell the project whether it has found a style statistic or only a
   painting/photograph statistic.
8. **Mather 2014**, still unopened across six rounds, still carrying the parent README's lead
   recommendation. Not load-bearing for anything here, recorded so the count is honest.

---

## 13. Corpus provenance

**Every image below was downloaded by me in this session through the Commons MediaWiki API with its
title, dimensions and licence recorded, and every one was displayed on a contact sheet and looked at
before use.** I worked in my own scratchpad subdirectory and used no file I did not fetch myself.
Provenance is in `realism-track3/corpus/provenance.json`; the corpus is reproducible from the titles
below.

### 13.1 Photographs (15 used, 1 rejected)

All Wikimedia Commons, fetched at `iiurlwidth` 480, 960 and 1800 so the size study in §2.2 uses the
same subjects at three resolutions. Subject spread is deliberate: three portraits, two still lifes,
one market crowd, two architecture, one harbour, three landscape, one animal, one macro, one
cityscape.

| key | file | camera | subject |
|---|---|---|---|
| p01_oldman | *Old man face* | — | portrait, close |
| p02_kondhwoman | *Kutia kondh woman 3* | Canon EOS 20D | portrait, close |
| p03_mtnchild | *Mountain child* | — | portrait with landscape |
| p04_loaf | *Milk loaf with wheatgerm* | Canon EOS 6D | still life, fine crumb |
| p06_vegseller | *Vegetable seller 2* | Nikon D300 | market crowd |
| p07_tenement | *Tenement house, 10 Bracka street, Kraków* | Sony ILCE-6000 | architecture |
| p08_riganave | *Riga Cathedral Nave* | — | interior |
| p09_reykjavik | *Boats moored in Reykjavík harbour* | Canon EOS Rebel T2i | harbour |
| p10_glencoe | *Glencoe Lochan reflections 3* | Panasonic DMC-G81 | landscape panorama |
| p11_shasta | *Mount Shasta, Lake Siskiyou, SW view* | Nikon D3300 | landscape |
| p12_blackforest | *Lothar Path – Black Forest National Park 03* | Canon EOS 80D | **forest floor, dense grass** |
| p13_whitehorse | *White horse in field* | — | animal |
| p14_iris | *Siberian Iris… Flower Petal Closeup* | Nikon D50 | macro |
| p15_empirestate | *View of Empire State Building from Rockefeller Center* | Fujifilm GFX 50R | cityscape, dusk |
| p16_winterriver | *Winter river Roshchinka* | Nikon D810 | snow, bare trees |

**Rejected, on inspection:** *Breads of Russia* (Canon EOS 60D, perfect EXIF, Quality Image) — a
**studio product shot on a seamless white sweep**. A blown-out flat background would have handed
every region statistic one enormous mass and flattered the paintability figure. No metadata check
would have caught it.

**Bias to declare.** One panorama (p10) has a 388 px short edge at 960 and therefore a base mark of
3; the rest run 4–6. The corpus is deliberately *not* landscape-weighted, unlike the Tonalism
round's, so the absolute percentages are not directly comparable to it — the *ranking* is.

### 13.2 Realist canvases (14 used, 1 rejected)

| key | work | note |
|---|---|---|
| c02 | Courbet, *A Burial at Ornans* (1849–50) | Google Art Project |
| c03 | Courbet, *Bonjour Monsieur Courbet* (1854) | WGA, 1147 × 1100 — lowest resolution retained |
| c04 | Millet, *The Gleaners* (1857) | Google Art Project |
| c05 | Millet, *The Angelus* (1857–59) | Google Art Project |
| c06 | Repin, *Barge Haulers on the Volga* (1870–73) | Google Art Project |
| c07 | Eakins, *The Gross Clinic* (1875) | Google Art Project |
| c08 | Eakins, *Max Schmitt in a Single Scull* (1871) | Commons |
| c09 | Homer, *The Veteran in a New Field* (1865) | Commons |
| c10 | Menzel, *Das Eisenwalzwerk* (1872–75) | Google Art Project |
| c11 | Bastien-Lepage, *Les Foins* (1877) | Google Art Project |
| c12 | Leibl, *Drei Frauen in der Kirche* (1882) | Commons |
| c13 | Bonheur, *Labourage nivernais* (1849) | Google Art Project |
| c14 | Breton, *Le chant de l'alouette* (1884) | Commons |
| c15 | Daumier, *The Third-Class Carriage* (c.1862–64) | Google Art Project — **unfinished**, see §11 |

**Controls**, chosen so the numbers join up with earlier rounds: Gauguin, *La vision après le sermon*
(1888) — the Post-Impressionism and Tonalism rounds' cloisonnist control; Whistler, *Nocturne in
Black and Gold: The Falling Rocket* (1875) — the Tonalism round's; van Gogh, *Wheatfield with Crows*
(1890) — a directional-impasto control this round added.

**Rejected, on inspection:** Courbet, *The Stonebreakers* (WGA) — the painting was **destroyed in
1945**, so every reproduction is a scan of a photograph of the lost canvas, at 1315 × 800 with a
visibly flattened, low-detail surface. It would have contributed a reproduction chain, not a
brushwork measurement, to the section whose whole point is surface roughness. Two further candidates
were replaced rather than rejected: the first Gauguin file found was 600 × 479 and the first Breton
461 × 600, both too small to measure a mark-scale statistic on.

That is 2 rejections and 2 replacements in 34 fetches, all four caught by looking rather than by any
automated check. **Five consecutive rounds have now each independently rediscovered corpus
contamination.** The recommendation stands and should be escalated: curate a shared,
provenance-checked corpus and commit it.

---

## Appendix — how everything was measured

A throwaway console project in a private session-scratchpad subdirectory
(`realism-track3/probe/`), `AssemblyName` set to `PaintTranslator.Tests` so the app's existing
`InternalsVisibleTo` grant applies, with a `ProjectReference` to `PaintTranslator.csproj`. Nothing
was added to the repository and no file outside
`docs/research/painting-style/realism/` was modified.

**The method rule was followed.** Every render goes through the real
`StylePipeline.Render(source, paints, style, 0, StylePipeline.DefaultValues(style))` with real
`StyleRegistry` rows; variants are produced with `StyleDefinition.WithDefaults` and the record's
`with` expression, so `EdgePreservingFloor` and `SmallRegionMerge` are the shipped instances, never
transcriptions. Sub-mark shares come from the real
`PaintabilityMetrics.FractionInRegionsSmallerThan`. §3's floored buffer comes from calling the real
`EdgePreservingFloor.Apply` with a hand-built `RenderContext` and the row's own `ParameterValues`.
The threshold sweep in §9 pick 3 drives the **real `SmallRegionMerge.Refine`** with a `RenderContext`
whose `MarkPixels` is scaled by √multiplier — the stage reads nothing from the `CandidateSet` and
labels regions by equal `int` value, so running it over the ARGB output buffer is the same operation
it performs on the index buffer. Lab conversion throughout is `PalettePhotoConverter.RgbToLab`.

The one exception is §5.3, which is explicitly a **prototype**: an independent implementation with
the neighbour choice as a switch. Its area-ranking branch reproduces the shipped stage's output
statistics exactly, which is the gate that makes its colour-ranking branch worth reporting.

Region statistics are my own only because `PaintabilityMetrics.ForEachRegion` is private and reports
areas only — the flood fill is four-connected on the RGB triple with alpha masked, matching its
semantics, and my §7 golden figures reproduce three prior rounds' published numbers exactly, which is
the cross-check.

Definitions of the statistics that are mine, matching prior rounds where one exists:

- **boundary pair** — a four-adjacent pixel pair whose RGB differs; **bnd/1000** is pairs ÷ pixels
  × 1000; **boundary ΔE** is plain Euclidean CIELAB between the pair; **hard** is the share of
  boundary pairs at ΔE ≥ 10. Identical to the Post-Impressionism and Tonalism rounds' definitions.
- **value mass** — a four-connected component of the L\* plane quantised to nine equal bands.
  Identical to the Tonalism round's.
- **notan gap** — mean L\* above the image's own median minus mean L\* below it. Identical.
- **thin dark structure** — a pixel more than 10 L\* below the mean of a box of radius 3 × mark,
  whose city-block distance to the edge of that dark set is at most 1.5 × mark; **retained** when the
  converted pixel is still at least 6 L\* below its own local field. Identical.
- **flat at mark scale** — new here. The share of sampled pixels whose entire `(2r+1)²` neighbourhood,
  `r = mark/2`, stays within 2 ΔE of the centre pixel. Sampled on a stride giving ≈20,000 points per
  image.
- **structure-tensor coherence** — new here. Multi-channel Sobel over the Lab planes, tensor
  box-blurred at radius 2, `(λ₁−λ₂)/(λ₁+λ₂)` averaged over pixels with non-zero trace. Computed only
  on continuous-tone sources, never on mapped output, per the Post-Impressionism round's warning.

`markPixels = 0` throughout, so every render and every canvas measurement uses
`RenderContext.DefaultMarkPixels` for its own dimensions multiplied by the style's `MarkScale` — the
app's own default rather than a fixed 4.
