# Research: Tonalism — Line, Mass and the Shipped Row

**Track:** Tonalism, track 4 of 4 — line and structure, plus the shipped-row audit.
**Date:** 2026-07-31
**Scope:** whether Tonalism refuses line and whether that is measurable; what gives a Tonalist
picture its structure if not contour, and whether this pipeline can produce it; whether
`ContourLines` has any place on this style; what the empty post-map slot 5 is actually costing on
real photographs; whether `SmallRegionMerge` should be registered here; whether `MarkScale` 1.2 is
right; and whether "Tonalism" is one style row or more than one.

**Builds on, does not repeat:** [`../03-brushwork-and-edges.md`](../03-brushwork-and-edges.md)
(edge hierarchy, filter families, the four-category invariant table),
[`../02-styles-and-movements.md`](../02-styles-and-movements.md) §6 (the Tonalism preset row that
produced the shipped numbers), [`../post-impressionism/03-edges.md`](../post-impressionism/03-edges.md)
(the thin-dark-structure detector I reuse, and `ContourLines`' three defects),
[`../post-impressionism/01-brushwork.md`](../post-impressionism/01-brushwork.md) (the
`SmallRegionMerge` diagnosis this round's rewrite implements) and
[`../abstract/README.md`](../abstract/README.md) (only slots 1 and 5 can produce spatial
structure). Where I correct any of them, §9 says so.

**Verification convention** — house standard: `[verified]` = read from the cited primary source, or
computed in this repo · `[relayed]` = a secondary source reports it and I did not reach the primary
· `[inferred]` = my reasoning, stated nowhere.

**Working-tree note, and it matters for every number below.** I measured against the **uncommitted
rewrite** of `Imaging/Styles/Stages/SmallRegionMerge.cs` — the smallest-first union-find sweep that
accumulates area — and the uncommitted `StyleRegistry.cs` in which Post-Impressionism now carries
`new SmallRegionMerge()`. Any figure I give for Post-Impressionism or for "+ merge" is the **new**
stage, not the one the Post-Impressionism round measured. §5.2 states plainly what the rewrite
does, because confirming it was that round's top verification debt.

---

## 0. Headline

**Tonalism refuses line, that is measurable, and the app should not draw one. But line is not this
row's problem. The row's problem is that it is now the least paintable styled row in the
application, and that it physically cannot reach a dark.**

**And the answer to "if there is no drawn line, what gives the picture its structure" is value
mass — which is the one thing this row is measurably worst at.** The light/dark separation (mean
L\* above the image's own median minus mean L\* below it) is **34.93** in the source photographs,
34.35 under Realism, 31.38 under Post-Impressionism, 28.26 under Fauvism, 23.68 under Abstract —
and **15.59 under Tonalism**. `[verified, §3.2]` The style whose own sources put value structure
first halves it.

Six findings, in descending order of how much each should change a decision.

1. **The 0.77% figure is a fixture artefact, and the real number is 25.77%.**
   `StyleBehaviourTests.EveryRegisteredStyleIsPaintable` records Tonalism at **0.7675%** with a
   0.9% ceiling — the tightest in the app. It measures a **6 px** threshold on a 256² synthetic
   gradient. On **14 provenance-checked photographs** at the app's own derived default mark, the
   real threshold is 24 px and Tonalism puts **25.77%** of pixels in regions below its own mark².
   `[verified, §4]` That is a **33× gap**, and it is the fourth consecutive round in which a
   synthetic fixture produced a false reading.

2. **Registering `SmallRegionMerge` takes Tonalism to exactly 0.000000 on every one of the 14
   photographs, in one pass, and the pass is idempotent.** Regions fall 97,389 → 1,488, median
   region area 1 → 67.5 px. One registry line. `[verified, §5]` The rewritten stage in the working
   tree does what the last two rounds asked for: **the Fauvism round's "hard postcondition" is now
   true on photographs.** That closes the Post-Impressionism round's verification debt 1.

3. **`ContourLines` has no place on Tonalism, and the measurement is unusually clean.** On seven
   provenance-checked Tonalist canvases the thin-dark-structure ("line") share is **0.98%–5.94%,
   mean 2.61%**; Gauguin's *Vision after the Sermon*, measured with the same detector as a
   cloisonnist control, is **8.97%**. `ContourLines` on Tonalism paints **3.1%–39.2% of the canvas
   (mean 17.5%)** as line — **6.7× the Tonalist corpus mean, and twice the cloisonnist control**.
   All three known defects reproduce: the band radius is **1 on all 14 images**, canvas share is
   unbounded, and the line index resolves to an absolute L\* 42.2 against field lightnesses from
   45.4 to 67.5. `[verified, §2, §6]`

4. **`MarkScale` 1.2 is not backwards. It is inert.** Tonalism registers exactly one `MarkPixels`
   consumer — `EdgePreservingFloor`, whose window is `Math.Round(mark / 2)` — and that rounding
   discards the 1.2 for base marks 2, 3, 4 and 7. Sweeping `MarkScale` 0.8 → 3.0 with slot 5 empty
   moves the region count by **5.6%** (95,310 → 103,637) and mean boundary ΔE by **0.01**, while
   the sub-mark share climbs 22.0% → 33.2% **entirely because the bar it is measured against
   grows**. `[verified, §7]` With the merge registered the same sweep becomes a real control:
   median region area 27.9 → 338.9 px. **The mark is a measurement unit in this row until slot 5
   is filled.**

5. **The row cannot make a nocturne, and that is the finding with the longest reach.**
   `MotherColourTransform` at 0.30 raises the darkest achievable candidate from **L\* 6.46 to
   L\* 40.30** on the six-paint fixture. Realised, Tonalism's 1st-percentile L\* is **43.8** and
   never falls below 42.6 on any of the 14 photographs. Six of the seven Tonalist canvases have a
   1st-percentile L\* below 27, and Whistler's *Falling Rocket* is at **0.2** with a mean of 14.4.
   **Realism, which does nothing, lands closer to a Whistler nocturne than Tonalism does** — on the
   moonlit-street photograph Realism realises mean L\* 31.4 / p1 10.0 against Tonalism's 46.1 /
   42.6. `[verified, §3.3, §8]` Track 1 owns the fix; this is the structural consequence.

6. **Some line-like structure does survive, and the rule is area, not width.** A 2 px shroud
   running 800 px comes through `SmallRegionMerge` intact because its area is 1,600 against a mark²
   of 52; a 2 px branch tip 20 px long does not. `[verified, §3.2b]` Masts, horizons, trunks and
   walls survive; twigs, sparks and distant fences do not. The one silhouette that fails badly —
   a bare tree against a sunrise, 12.9% of its thin dark structure retained — fails for a *colour*
   reason before any spatial stage runs, and removing the mother colour visibly repairs it.

**Boundary ruling: one row.** The term was coined in **1972** by Wanda Corn, and her exhibition put
**49 paintings and 46 photographs by 31 painters and photographers** in one room, with Inness and
Whistler both inside the founding definition. There is no split to make that the founding source
did not already refuse. `[relayed, §1]`

The three picks are in §10: **register `SmallRegionMerge` (slot 5, one line)**, **raise the floor
from 2.0 to 4.0 (one line)**, **a value-mass consolidation parameter on the merge (slot 5, ~40
lines)** — in that order.

---

## 1. The boundary problem, and the ruling

### 1.1 The label is younger than the Post-Impressionism one, and it was born inclusive

Every prior round has had to rule on whether its style is one row or several, and each time the
argument turned on when and by whom the label was invented. Tonalism is the extreme case.

- The movement ran roughly **1880–1915** (TheArtStory dates it 1870–1915; Wikipedia "from the 1880s
  into the early 20th century"; Cleveland's history is subtitled *1880–1920*). `[relayed]`
- **The word "Tonalism" was not applied to it until 1972**, when Wanda Corn organised *The Color of
  Mood: American Tonalism 1880–1910* at the M.H. de Young Memorial Museum. `[relayed — multiple
  secondary sources agree; I did not reach the catalogue]` Fry named Post-Impressionism five years
  after its close; Corn named Tonalism **fifty-seven** years after its close.
- Corn's foundational group is given as **Inness, Whistler, Tryon, Dewing and Wyant**, and the
  exhibition checklist is reported as **49 paintings and 46 photographs**, with biographies of
  **31 painters and photographers**. `[relayed]`

That last figure decides the whole section. **The exhibition that created the term was
approximately half photographs.** Pictorialism is not a rival grouping that might justify a second
row; it is inside the founding definition, at near-parity.

The contemporary term was different and vaguer still: critics in the late 1890s used "tonal", with
"Quietism" and "Intimism" as synonyms. `[relayed — Wikipedia, *Tonalism*]`

### 1.2 The three candidate splits, and why none survives

| Candidate split | What it would be | Verdict |
|---|---|---|
| **Whistler's nocturne vs Inness's late landscape** | Low-key night vs mid-key dusk | **One parameter, and the app already has it.** `ToneAndChromaRemap.key` is a signed L\* shift; the difference between *Nocturne: Blue and Gold* (mean L\* 53.3) and *The Home of the Heron* (28.6) is a key shift, not a different pipeline. Same shape as the Post-Impressionism round's Cézanne/van Gogh ruling. `[inferred, from the measured means in §3.3]` |
| **Pictorialist photography as its own row** | Soft focus, suppressed detail, tonal narrowness | **Not a style row at all.** It is a *medium* claim — photography arguing it is art — and its formal properties are the same three the painting row already asks for. The Photo-Secession's own common denominators are given as "emphasis on tonalism, on a blurring or softening of outlines, and on the suppression of details" `[relayed — Met, *Pictorialism in America*, reached via search summary; the Met page returned 429]`. Every one of those is a slot-1 or slot-5 operation this row already owns. And Corn put them in the same exhibition. |
| **American Tonalism vs Whistler-the-Londoner** | Nationality | **Refused by the source that coined the term.** Whistler is one of Corn's five. `[relayed]` |

**Ruling: keep one row.** `[inferred]` This is a *stronger* one-row ruling than Post-Impressionism's,
which rested on three of five handlings being already taken by other styles. Here the founding
scholarship simply never drew the lines a split would need.

### 1.3 There is no measured signature to appeal to, and I checked

Sigaki, Perc & Ribeiro 2018 — the one large-scale quantitative placement of art movements, 137,364
WikiArt images — **does not name Tonalism anywhere.** `[verified via
[ar5iv/1809.05760](https://ar5iv.labs.arxiv.org/html/1809.05760); the styles it names are
Renaissance, Neoclassicism, Romanticism, Impressionism, Fauvism, Pointillism, Expressionism,
Cubism, Surrealism, Pop Art, Minimalism, Hard Edge, Colour Field, Conceptual, Op Art,
Constructivism, Kinetic, Concretism, Pattern and Decoration, Neo-Baroque, Neo-Romanticism,
Divisionism and Abstract Painting]` That is the same negative result the Post-Impressionism round
recorded for its own movement, on the same paper, from a different direction. Three rounds have now
searched this literature for a movement-level signature and found one only for Fauvism, and that
one is a greyscale texture measure.

---

## 2. Does Tonalism refuse line? Yes — and it measures

### 2.1 What the sources claim

The claim is unusually consistent across sources, which is itself unusual for this project.

- **The lost edge is named as a period technique.** David Adams Cleveland's characteristics list
  gives "the use of soft-edged forms to further the sense of ambiguity and mystery of place (known
  as **lost-edge technique** in the nineteenth century)", alongside "an emphasis on the **broad,
  graphic, ultimately abstract reading of major forms**". `[verified — read from
  [artsy.net/article/david-adams-cleveland-what-is-tonalism-12-essential-characteristics] via a
  secondary rendering; the Artsy page itself returned **403** on direct fetch]`
- **Form is given by value patches, not contour.** Ephraim Rubenstein, on tonal drawing: "the eye
  retreats from the edges of things and sees, instead, patches of light and shade", and — the
  sentence that decides §6 — **"To substitute a line for the edge of a value relationship is to
  substitute something that is not there for something that is."** `[verified — read from
  [artistsnetwork.com, "Drawing Basics: The Emergence of Tonal Drawing"]]`
- **Whistler's own account is about mass replacing detail.** "As light fades and the shadows
  deepen, all petty and exacting details vanish, everything trivial disappears, and I see things as
  they are in great strong masses: **the buttons are lost, but the sitter remains.**" `[relayed —
  quoted by TheArtStory's Tonalism overview; I did not reach a primary edition]`
- **And the Ten O'Clock lecture describes the same operation applied to a city:** "when the evening
  mist clothes the riverside with poetry, as with a veil… and the poor buildings lose themselves in
  the dim sky, and the tall chimneys become campanili, and the warehouses are palaces in the
  night". `[verified — read from the University of Glasgow's *Whistler's Writings* site quoting the
  lecture; the Gutenberg text of *The Gentle Art of Making Enemies* does not contain the Ten
  O'Clock and I checked]`
- **Peak-Tonalist hallmarks**, as report 02 already relayed: "a narrow range of muted colors,
  diffused light and **softened, indistinct forms**". `[relayed]`

Two of these are worth separating from the rest, because they are *not* the same claim. "Soft edge"
is a statement about the **gradient across a boundary**; "no drawn line" is a statement about
whether a **separate dark mark** is laid on top of the boundary. Tonalism asserts both. The app can
act on only the second (§9, and report 03's finding that nothing in slots 1 or 5 can soften an
edge).

### 2.2 Measured on seven canvases

I ran the Post-Impressionism round's thin-dark-structure detector, reimplemented but with the same
construction, over seven provenance-checked Tonalist paintings plus one cloisonnist control. A
pixel is **dark** when its L\* is more than 10 below the mean of a window of 3 × mark around it
(mark = the app's own `DefaultMarkPixels × 1.2` for that reproduction's size); a city-block
distance transform inside the dark set gives each dark pixel a local half-width, and a **line**
pixel is a dark pixel whose half-width is at most 1.5 marks. `[verified — computed 2026-07-31;
provenance in §13]`

| Work | line share | dark share | line L\* − field L\* | mean L\* | sd L\* | p1 L\* | p99 L\* | mean C\* |
|---|---|---|---|---|---|---|---|---|
| Whistler, *Nocturne in Black and Gold: The Falling Rocket* (1875) | **0.0309** | 0.0309 | −13.5 | **14.4** | 11.66 | **0.2** | 51.0 | 7.44 |
| Whistler, *Nocturne: Blue and Gold – Old Battersea Bridge* (c.1872–75) | **0.0098** | 0.0098 | −12.3 | 53.3 | 13.25 | 26.9 | 71.3 | 8.20 |
| Inness, *Old Homestead* (1877) | 0.0594 | 0.0594 | −14.7 | 44.1 | 19.21 | 9.4 | 77.4 | 21.39 |
| Inness, *The Home of the Heron* (1893) | 0.0175 | 0.0175 | −13.5 | 28.6 | 8.79 | 11.6 | 52.3 | 31.50 |
| Blakelock, *Evening* (1880–90) | 0.0166 | 0.0166 | −15.4 | 32.3 | 20.10 | 2.9 | 66.9 | 22.02 |
| Ryder, *Moonlight on the Sea* (1884) | 0.0220 | 0.0220 | −12.8 | 25.6 | 18.82 | 2.8 | 64.7 | 15.20 |
| Twachtman, *Winter Harmony* (c.1890–1900) | 0.0263 | 0.0263 | −13.1 | **78.8** | 8.51 | 57.1 | 94.9 | 4.63 |
| **Tonalist mean (7)** | **0.0261** | 0.0261 | **−13.6** | **39.6** | 14.33 | 15.8 | 68.4 | 15.77 |
| *control:* Gauguin, *Vision after the Sermon* (1888) | **0.0897** | 0.0901 | −16.1 | 44.0 | 23.35 | 5.3 | 86.2 | 21.37 |

Four readings.

1. **Tonalist canvases carry roughly a quarter of a cloisonnist canvas's thin dark structure.**
   Mean 2.61% against the control's 8.97%. The Post-Impressionism round measured six cloisonnist
   works at **3.8%–17.1%, mean 10.3%** with a slightly different window, and measured *this same
   Gauguin* at 11.2% against my 8.97% — different reproduction, different window, same order.
   **The two measurements cross-validate**, which matters because the whole ruling in §6 rests on
   the comparison. `[verified]`
2. **The "line" that does exist is never a drawn contour** in these pictures. Every hit is a mast,
   a trunk, a branch or a spark. The detector is blind to intent and the canvases still separate.
3. **"Restricted palette" is the claim that measures cleanly.** Mean C\* 4.63–31.50, mean 15.77,
   against the Gauguin control's 21.37 and against the mean C\* the app's own **Realism** realises
   on photographs, **18.98** (§8). The two Whistlers are at 7.44 and 8.20.
4. **"Narrow value range" does not measure cleanly, and report 02's "maybe three steps" should not
   be quoted as a histogram claim.** These canvases run p99 − p1 of **37.8 to 68.0 L\*, mean
   52.5**, and sd L\* 8.51–20.10, mean **14.33**. The app's Tonalism realises sd L\* **9.62** and a
   p1–p99 span of **32.4** — it is *more* compressed than the paintings, not less. What is narrow
   in a nocturne is the **key** and the **chroma**, not the total excursion: *Falling Rocket* spans
   L\* 0.2–51.0, which is 51 units, all of them below 51. `[verified]`

**Caveat, and it is the same one every round carries.** These are uncalibrated web reproductions
with unknown capture and display transforms. The *ratios* (line share, dark share) and the *signs*
(line darker than field, by 12–15 L\*) are robust to that; the absolute L\* coordinates are not.
Six candidate works were rejected on inspection (§13) — including a Whistler that turned out to be
an etching with a paper margin, which would have inverted this section's finding.

---

## 3. If not line, what gives the picture its structure — and can the pipeline make it?

### 3.1 The three candidate mechanisms, and where each lands in the pipeline

| Mechanism | What it is | Slot | Buildable? |
|---|---|---|---|
| **Value mass** | A few large connected regions of held lightness; the "broad, graphic reading of major forms" | 5 (region merging), 1 (flattening) | **Yes, and slot 5 is empty** |
| **Silhouette** | A dark shape read against a lighter field, with its interior undescribed | 5 | Yes, and it is the same operation as value mass |
| **Soft edge** | A gradual transition rather than a step | — | **No.** Report 03 §5.2 established that *every* operation in slots 1 and 5 preserves or hardens an edge, and the one softening operator in the codebase (`OptionalBlur`) is registered by no style. Confirmed here: adding the merge raises Tonalism's mean boundary ΔE 6.42 → 7.20 `[verified, §5]` |

So two of the three are the same build item, and the third is not available. **This is a
convenient result for Tonalism specifically**, because value mass is the mechanism the sources put
first and it is the one the pipeline can express.

### 3.2 The measurement

Quantising L\* into nine equal bands (a Munsell-like ladder) and labelling four-connected
components gives a direct reading of "how much of this picture sits in a value mass a brush could
lay down".

Mean over the 14 photographs, all five registered styles plus Tonalism-with-the-merge, against the
source itself. **Notan gap** is mean L\* above the image's own median minus mean L\* below it — the
light/dark separation a value composition is built on. `[verified — computed 2026-07-31]`

| | value masses | largest mass | **in masses ≥ mark²** | dark share | largest dark mass | **notan gap** |
|---|---|---|---|---|---|---|
| source photograph | 81,960 | 18.0% | 72.1% | 50.0% | 42.1% | **34.93** |
| Realism | 50,594 | 22.1% | 83.8% | 47.9% | 40.7% | 34.35 |
| **Tonalism** | 20,295 | 31.2% | 93.2% | 44.3% | 39.1% | **15.59** |
| **Tonalism + merge** | **245** | **37.1%** | **100.0%** | 43.2% | 39.9% | 15.79 |
| Fauvism | 777 | 34.6% | 99.7% | 43.3% | 36.0% | 28.26 |
| Post-Impressionism | 262 | 33.4% | 100.0% | 45.8% | 39.8% | 31.38 |
| Abstract | 57 | 42.0% | 100.0% | 39.9% | 30.8% | 23.68 |
| *for reference:* Tonalist canvases (§2.2) | — | **23.3%** (13.2–36.1) | **84.4%** (76.5–91.5) | — | 44.3% | — |

Three readings, and the third is the one that should change a decision.

1. **On the value-mass axis the app's Tonalism already overshoots the paintings, and the merge
   overshoots them further.** Largest mass 31.2% shipped and 37.1% merged, against a canvas mean of
   23.3%; in-masses share 93.2% and 100.0% against 84.4%. **This is the evidence against raising a
   minimum-mass threshold above one mark²** — see §10 pick 3, which I softened because of it.
2. **But the mass figure is partly an artefact of the compression.** Nine equal L\* bands over a
   picture whose sd L\* is 9.62 puts nearly everything in two or three bands, so "large masses" is
   partly "few bands occupied". The canvases reach 84.4% at sd L\* 14.33. **Per unit of value range
   the paintings are the more consolidated pictures**, and the app is reaching a similar number by
   flattening the histogram rather than by organising the shapes.
3. **Tonalism has the weakest value separation in the application, by a factor of two.** Notan gap
   34.93 in the source, 34.35 under Realism, **15.59 under Tonalism** — against Fauvism's 28.26,
   Post-Impressionism's 31.38 and Abstract's 23.68. `[verified]` **The style whose sources put
   value structure first destroys more of it than any other row in the app.** Contrast 0.55 and the
   mother colour together halve the light–dark separation that mass composition is made of. This is
   the same conclusion §3.3 reaches from the dark end, arrived at from the light/dark split.

### 3.2b Does any line-like structure survive? Yes — and the rule is area, not width

Taking the source's thin dark structures (§2.2's detector, mean **10.94%** of pixels across the 14
photographs) and asking what share is still at least 6 L\* below its own local field after
conversion: `[verified]`

| | thin dark structure retained | local contrast retained |
|---|---|---|
| Realism | **83.5%** | 0.717 |
| **Tonalism** | **31.5%** | 0.248 |
| Fauvism | 24.7% | 0.125 |
| Post-Impressionism | 23.3% | 0.123 |
| Abstract | 16.9% | 0.096 |
| **Tonalism + merge** | **12.9%** | 0.082 |

Per subject, which is where the answer actually lives:

| photograph | thin dark share | Realism | **Tonalism** | **Tonalism + merge** |
|---|---|---|---|---|
| baobab, bare branches against overcast sky | 18.4% | 97.1% | **75.0%** | 25.2% |
| mast and rigging against blue sky | 13.9% | 99.4% | **78.5%** | 31.7% |
| twigs and frost, backlit | 11.2% | 95.0% | 34.8% | 10.5% |
| moorland road, walls and rushes | 12.1% | 94.6% | 35.8% | 14.4% |
| **tree silhouette against sunrise** | 11.1% | 91.3% | **12.9%** | 3.4% |
| trees in dense mist | 2.3% | 41.1% | 4.4% | 3.3% |

And I looked at the renders. `[verified — inspected the PNGs]`

- **A mast survives an area opening; a twig does not, and width is not the criterion.** In the
  rigging render with the merge, the mast, the yards and every shroud come through as continuous
  dark structures — a 2 px-wide shroud running 800 px has an area of 1,600 px against a mark² of
  52. In the baobab render with the merge, the same 2 px branch tips, which are 20 px long, are
  gone and the crown is a lumpy blob. **An area opening preserves length, not thinness.** A
  horizon, a mast, a trunk and a wall survive; a twig, a spark and a distant fence do not.
- **The silhouette case fails for a colour reason, not a spatial one.** The tree-against-sunrise
  photograph loses **87%** of its thin dark structure *before* any post-map stage runs. The
  branches are near-black in the source and Tonalism's darkest reachable colour is L\* 40.3
  (§3.3), so they land within a few ΔE of the sky and stop being dark relative to their field. In
  the render the tree reads as a pale blue-grey tracery on a pink sky with no weight at all —
  a silhouette with the silhouette removed.
- **Removing the mother colour visibly repairs it.** The same photograph at fraction 0 with the
  merge reads as a Tonalist winter sunset: the tree is a dark mass with internal texture, the snow
  field has a lower value than the sky, and the sun has a genuine gradation. Of the four renders I
  made of that photograph it is unambiguously the best, and the shipped one is the worst.
  `[verified — inspected]` That is independent corroboration of track 1's item from the structural
  side.

### 3.3 The dark end, and why a nocturne is out of reach

The mother colour is the binding constraint, and it binds before any spatial stage runs. Measured
on the six-paint fixture by calling the real `MixtureBuilder` with the real
`MotherColourTransform` fraction: `[verified]`

| fraction | candidates | min L\* | max L\* | mean L\* | mean C\* |
|---|---|---|---|---|---|
| 0.00 | 3,007 | **6.46** | 98.17 | 40.06 | 36.13 |
| 0.15 (Abstract) | 3,037 | 30.90 | 98.17 | 49.37 | 36.22 |
| **0.30 (Tonalism)** | 3,037 | **40.30** | 98.17 | 55.50 | 34.58 |
| 0.45 | 3,032 | 47.82 | 98.17 | 60.78 | 32.38 |
| 0.60 | 3,021 | 54.81 | 98.17 | 65.99 | 29.50 |

`MostNeutralPaintIndex()` returns index 0 — Titanium White — confirming the Post-Impressionism
round's correction 4 on a second palette. The darkest colour the style can reach rises by
**33.8 L\***; the lightest does not move at all. The candidate count barely changes, so this is not
a gamut *contraction*, it is a gamut *translation upward*.

Realised on the photographs, Tonalism's p1 L\* is **43.8** and its per-image minimum across all 14
is **42.6**. Against §2.2's canvases: `[verified]`

| | mean L\* | p1 L\* |
|---|---|---|
| Whistler, *Falling Rocket* | 14.4 | 0.2 |
| Ryder, *Moonlight on the Sea* | 25.6 | 2.8 |
| Inness, *Home of the Heron* | 28.6 | 11.6 |
| Blakelock, *Evening* | 32.3 | 2.9 |
| Inness, *Old Homestead* | 44.1 | 9.4 |
| Whistler, *Battersea Bridge* | 53.3 | 26.9 |
| Twachtman, *Winter Harmony* | 78.8 | 57.1 |
| **app, Tonalism (14 photographs)** | **56.0** | **43.8** |
| **app, Realism (14 photographs)** | **51.8** | **15.8** |

**The shipped Tonalism row matches exactly one canvas in the corpus — Twachtman's high-key snow
scene — and is lighter than the other six.** And on the one photograph in the corpus that *is* a
nocturne subject (a moonlit Berlin street), Realism renders mean L\* 31.4 / p1 10.0 while Tonalism
renders 46.1 / 42.6. The row that does nothing is the closer nocturne. `[verified]`

Two registered numbers compound with the mother colour to produce that: `key` **+4.0** is a
*lightening* shift, and `contrast` 0.55 pivots about L\* 50, which pulls darks up. Setting key to 0
alone moves realised mean L\* 56.0 → 53.2 and p1 to 43.6; removing the mother colour moves p1 to
**34.0** and sd L\* to 11.11. `[verified, §5]` The mother colour is the dominant term.

---

## 4. Where the shipped row actually is

`StyleRegistry.cs:42–64` `[verified]`: `MarkScale` **1.2**, `EdgePreservingFloor` strength **2.0**,
`ToneAndChromaRemap` contrast **0.55** / key **4.0** / chroma **0.45**, `MotherColourTransform`
fraction **0.30**, `NearestQuantiser`, `Array.Empty<IPostMapStage>()`.

### 4.1 The committed golden — which reproduces the prior rounds exactly

`Tests/Golden/*.png`, read to 32bpp ARGB, four-connected on the RGB triple with alpha masked, mark
= `4.0 × MarkScale`, sub-mark share from the real `PaintabilityMetrics`. `[verified]`

| Style | mark | regions | median | colours | below own mark² | mean L\* | sd L\* | mean C\* |
|---|---|---|---|---|---|---|---|---|
| Realism | 4.0 | 425 | 3 | 161 | 5.42% | 59.98 | 18.10 | 16.96 |
| **Tonalism** | **4.8** | **344** | **6** | **151** | **7.85%** | 59.63 | 9.77 | 8.18 |
| Fauvism | 5.2 | 183 | 40 | 118 | 2.75% | 56.29 | 17.86 | 27.86 |
| Post-Impressionism | 6.4 | 101 | 102 | 100 | **0.00%** | 60.25 | 18.64 | 24.12 |
| Abstract | 10.0 | 8 | 1456 | 8 | **0.00%** | 58.14 | 16.57 | 15.42 |

My Tonalism row reproduces the Fauvism and Post-Impressionism rounds' published golden figures
**exactly** (344 regions, median 6, 151 colours, 7.85%). Three sessions, three independent probe
implementations, identical numbers. The method is sound and the style has not moved. **The Fauvism
and Post-Impressionism rows have moved, and both of those PNGs are modified in the working tree** —
Post-Impressionism because it gained the merge, Fauvism because the merge itself was rewritten. Do
not compare those two rows against the prior rounds' published golden tables.

### 4.2 Real photographs — the number that matters

14 photographs (960 px long edge, JPEG, Wikimedia Commons, every one inspected and
provenance-recorded — §13), the six-paint `StyleTestFixtures.SixPaints()` fixture reproduced,
`markPixels = 0` so each image uses **the app's own derived default** (base marks 2–6). Rendered
through the real `StylePipeline.Render` with the real `StyleRegistry` rows. `[verified — computed
2026-07-31]`

| Style | mean mark | regions | median area | colours | **below own mark²** | below common mark² | dominant share | below, dominant dropped |
|---|---|---|---|---|---|---|---|---|
| Realism | 4.00 | 166,528 | 1 | 832 | **40.84%** | 46.47% | 12.5% | 45.92% |
| **Tonalism** | **4.80** | **97,389** | **1** | **323** | **25.77%** | 28.22% | 18.4% | 29.96% |
| Fauvism | 5.20 | 3,022 | 28.8 | 287 | **1.45%** | 3.50% | 22.0% | 1.85% |
| Post-Impressionism | 6.40 | 1,510 | 115.6 | 262 | **0.00%** | 0.00% | 13.1% | 0.00% |
| Abstract | 10.00 | 106 | 375.0 | 9 | **0.00%** | 0.00% | 31.5% | 0.00% |

The right-hand columns exist because of the Fauvism round's contour-measurement trap; I dropped the
dominant colour from both numerator and denominator rather than sentinelling it. Tonalism's figure
*rises* to 29.96% under that correction, so the trap is not flattering it.

Three readings:

1. **Tonalism is now the second-least-paintable style in the app and the worst styled row**, behind
   only Realism, which is defined as doing nothing. That is a rank change caused by another
   round landing while this one measured: Post-Impressionism went from 35.3% to 0.00%.
2. **The synthetic golden understates it by 3.3×** (7.85% → 25.77%), and the shipped test
   understates it by **33×** (0.77% → 25.77%). §4.3.
3. **Tonalism's median region area is 1 pixel**, identical to Realism's, at a mark 1.2× larger.
   The style with the app's gentlest colour transform produces the app's second-most speckled
   picture.

### 4.3 The test that should catch this reads a 6-pixel threshold

`StyleBehaviourTests.EveryRegisteredStyleIsPaintable` renders
`StyleTestFixtures.BuildNoisyGradient(256, 256, 3.0)` at `markPixels = 0`, so
`DefaultMarkPixels(256, 256) = 2`, mark = 2 × 1.2 = **2.4** and `markSquared = Round(5.76) = 6`.
Tonalism's recorded fraction is 0.7675% and its ceiling 0.9%. `[verified — read from the source]`

So the gate reads: *fewer than 0.9% of pixels in regions under six pixels, on a smooth synthetic
gradient.* The same style on a real photograph at the app's own default mark scores **25.77% under
24 pixels**.

The test's own doc comment is unusually candid about its limits — it documents a "MarkScale blind
spot" and states that it "only bounds fragmentation from above per style". What it does not
disclose is the **threshold** gap: 6 px against the 13–52 px this corpus's real defaults produce.
The 0.9% ceiling is described as the tightest relative margin in the suite (+17.3%), which reads as
rigour and is in fact a tight bound on a number 33× too small. `[verified]`

This is the Post-Impressionism round's finding recurring one style over, and the fourth consecutive
round to hit it. **The fixture problem is now the single most expensive thing in this research
programme.**

---

## 5. What moves the number

### 5.1 The sweep

Every variant is the real `StyleRegistry` Tonalism row with real stage instances substituted
through `WithDefaults` and the record's `with` expression. Mean over the same 14 photographs, each
at its own derived default mark, each measured against **its own** mark². `[verified]`

| Variant | regions | median | colours | **below own mark²** | mean L\* | sd L\* | p1 L\* | p99 L\* | mean C\* | bnd/1000px | transition | mean ΔE | hard ≥10 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **shipped** | 97,389 | 1.0 | 323 | **25.77%** | 55.98 | 9.62 | 43.8 | 76.2 | 8.83 | 559.2 | 46.1% | 6.42 | 17.7% |
| **+ `SmallRegionMerge`** | **1,488** | **67.5** | 125 | **0.00%** | 55.63 | 9.71 | 43.8 | 74.9 | 8.63 | 171.8 | 23.4% | 7.20 | 19.4% |
| + `SmallRegionMerge` ×2 | 1,488 | 67.5 | 125 | **0.00%** | 55.63 | 9.71 | 43.8 | 74.9 | 8.63 | 171.8 | 23.4% | 7.20 | 19.4% |
| + `ContourLines` | 35,238 | 1.0 | 228 | 12.34% | 53.44 | 10.14 | 42.2 | 73.3 | 9.58 | 342.8 | 34.9% | 7.63 | 18.2% |
| + merge + `ContourLines` | 1,894 | 38.7 | 120 | 0.53% | 54.51 | 9.94 | 42.2 | 73.7 | 8.88 | 171.9 | 23.5% | 8.92 | 22.5% |
| floor strength 1 | 119,126 | 1.0 | 340 | 32.06% | 56.02 | 9.71 | 43.8 | 76.3 | 8.87 | 658.9 | 52.1% | 6.54 | 18.4% |
| floor strength 3 | 82,014 | 1.0 | 315 | 21.36% | 55.96 | 9.53 | 43.9 | 76.0 | 8.81 | 492.0 | 42.4% | 6.25 | 16.8% |
| **floor strength 5** | 63,668 | 1.0 | 301 | **16.12%** | 55.99 | 9.38 | 43.9 | 75.7 | 8.80 | 410.5 | 37.7% | 5.99 | 15.9% |
| floor 5 + merge | **1,358** | **74.7** | 131 | **0.00%** | 55.84 | 9.47 | 43.9 | 74.3 | 8.67 | 167.0 | 23.0% | **6.29** | **15.7%** |
| mother fraction 0 | 88,800 | 1.0 | **156** | 24.32% | 55.17 | **11.11** | **34.0** | 76.0 | 8.77 | 534.4 | 45.3% | 7.91 | 26.0% |
| mother 0 + merge | 1,618 | 65.8 | **86** | **0.00%** | 54.90 | **11.28** | **34.1** | 75.0 | 8.69 | 180.3 | 24.5% | 8.39 | 25.0% |
| contrast 0.4 | 96,961 | 1.0 | 292 | 26.32% | 54.42 | 7.35 | 44.3 | 68.9 | 8.98 | 569.4 | 47.4% | 5.60 | 14.4% |
| contrast 0.75 | 98,165 | 1.0 | 333 | 25.56% | 58.14 | 12.21 | 43.5 | 83.6 | 8.65 | 561.3 | 46.3% | 7.34 | 20.5% |
| chroma 0.25 | 86,295 | 1.0 | 158 | 22.58% | 56.25 | 9.58 | 43.7 | 75.3 | **5.20** | 490.1 | 40.5% | 5.85 | 16.3% |
| chroma 0.7 | 114,673 | 1.0 | 577 | 30.36% | 56.00 | 9.25 | 44.0 | 75.7 | 13.52 | 649.6 | 52.6% | 6.79 | 17.8% |
| key 0 | 90,876 | 1.0 | 324 | 24.07% | 53.15 | 8.05 | 43.6 | 70.7 | 8.87 | 534.9 | 44.9% | 6.17 | 15.6% |

Six things fall out.

1. **`SmallRegionMerge` is not the largest lever — it is the only one that finishes.** Every other
   variant leaves 16%–32%; the merge leaves **zero**. That is a qualitative difference, not a
   quantitative one.
2. **The floor is the second lever and it is not saturated at 2.** 1 → 32.06%, 2 → 25.77%,
   3 → 21.36%, 5 → 16.12%. Tonalism registers **2.0**, the second-weakest floor in the registry
   after Fauvism's declared default. Raising it to 5 buys 9.7 points *and* softens the picture
   (mean ΔE 6.42 → 5.99, hard-edge share 17.7% → 15.9%) — the only lever measured here that
   improves both. It is also the only route to a Tonalist edge quality, since nothing downstream
   can soften anything (§3.1).
3. **Chroma is a fragmentation lever in this style**, unlike in Post-Impressionism where contrast
   was inert: 0.25 → 22.58%, 0.45 (shipped) → 25.77%, 0.7 → 30.36%, and the colour count runs
   158 → 323 → 577. Lowering chroma is nearly free paintability. That is a colour decision and I
   hand it to this round's colour track rather than ruling on it.
4. **Contrast is inert for paintability** (0.4 → 26.32%, 0.75 → 25.56%, a 0.76-point spread) but
   is the only clean control over the value *span* — p1–p99 of 24.6 / 32.4 / 40.1 across the
   three settings. Post-Impressionism's round found the same split of duties in the opposite
   direction, and both hold.
5. **Removing the mother colour is close to free on the metric (25.77% → 24.32%) and transforms the
   picture** — p1 L\* 43.8 → 34.0, sd L\* 9.62 → 11.11, mean boundary ΔE 6.42 → 7.91, distinct
   colours 323 → **156**. The last number is the surprising one: **whitening the gamut *increases*
   the colour count**, because a compressed, light candidate cloud is denser in the region the
   photograph lands in, so adjacent source colours split across more mixtures. `[verified]`
6. **`ContourLines` alone halves the sub-mark share (25.77% → 12.34%) and does it by painting over
   the evidence.** §6.

### 5.2 The rewritten `SmallRegionMerge` converges in one pass — the prior round's debt, cleared

The Post-Impressionism round's verification debt 1 was: *build the corrected area opening and check
one sweep reaches zero on photographs.* The working tree contains that rewrite. It does.

Rendering each style twice — once with slot 5 emptied, once as registered — and measuring the real
`PaintabilityMetrics.FractionInRegionsSmallerThan` at each style's own mark²: `[verified —
computed 2026-07-31, all 14 photographs]`

| Style | slot 5 emptied | as registered (one pass) | sources at exactly 0 | idempotent |
|---|---|---|---|---|
| **Tonalism + merge** (proposed) | 6.26%–50.40% | **0.000000 on all 14** | **14 / 14** | **14 / 14** |
| Post-Impressionism (shipped in tree) | 6.89%–55.90% | **0.000000 on all 14** | **14 / 14** | **14 / 14** |
| Abstract | 0.71%–29.87% | **0.000000 on all 14** | 14 / 14 | n/a |
| Fauvism | 6.80%–51.19% | 0.60%–2.34% | **0 / 14** | n/a |

The idempotence column is `n/a` for Fauvism and Abstract because my "twice" variant replaces the
whole `PostMap` list, which for those two also drops `ContourLines` / `GroundFill`; the byte-identity
comparison is only meaningful where the merge is the entire chain.

**Two results, and the second is a live defect.**

- **The postcondition the Fauvism round called "the single most valuable test available anywhere in
  this work" is now true.** One invocation, exactly zero, on every photograph, for every style that
  ends its post-map chain with the merge. And a second invocation leaves the buffer
  **byte-identical** — I checked all 14 for Tonalism and Post-Impressionism. It should be written
  as a test over the whole registry now, not deferred again.
- **Fauvism is the exception, and the reason is `ContourLines`.** Fauvism's chain is
  `SmallRegionMerge` then `ContourLines`; replacing that chain with two merges gives exactly zero
  on all 14. **The contour re-introduces sub-mark fragments after the merge has removed them** —
  mean residual 1.45%, up to 2.34%. `[verified]` That is a new finding and it is not
  Tonalism-specific: **the merge must run last, or the contour must be made mark-aware.**
  Fauvism's entire remaining fragmentation is its own contour stage.

### 5.3 What the renders look like

I rendered six photographs five ways each and looked at the PNGs rather than only the numbers.
`[verified — inspected 2026-07-31]`

- **The merge is a clear win where the subject is mass, and a clear loss where it is tracery.** The
  misty-mountain photograph with the merge reads as broad soft planes of green-grey with the single
  larch still legible — a Tonalist picture. The baobab with the merge loses its branch structure
  and the crown becomes a mottled blob. Both are the area opening working correctly; the second is
  a subject whose defining feature is genuinely unpaintable at a 6 px mark. **Tonalism needs the
  mark slider more than any other row**, and the metric will not tell a user that.
- **Shipped Tonalism reads as a faded print, not a painting.** Every render at fraction 0.30 has
  the same character: a correct drawing under a milky veil, with the darks lifted and the whole
  picture sitting in a narrow band around L\* 56. On the baobab that reads plausibly as a
  Pictorialist photograph, which is arguably on target; on the sunrise and the moonlit street it
  reads as a printing fault.
- **`ContourLines` is visually catastrophic here and not marginally so.** On the baobab it fills
  the entire crown with one flat violet and scatters violet crumbs across the grass; on the sunrise
  it replaces the tree with a solid violet mass and draws a cartoon outline round the sun. Neither
  is a tuning problem.
- **The mother colour is what stands between this row and a Tonalist picture, and it is visible
  rather than only numeric.** At fraction 0 with the merge, the sunrise photograph reads as a
  winter dusk with a weighted tree and a graded sky, and the moorland landscape reads as a
  broad-brush oil. At fraction 0.30 the same two are veiled. I did not render floor 5, so the
  case for pick 2 in §10 rests on the numbers alone (mean ΔE 6.42 → 5.99, hard share 17.7% →
  15.9%) and should be looked at before shipping.

---

## 6. `ContourLines` on Tonalism — a clear negative

**Do not register it. Not at any setting, not after a merge, not with the three defects fixed.**
Four independent grounds, in descending weight.

### 6.1 The measured canvas share is 6.7× the corpus

`ContourLines` writes `candidates.FindNearest(35, 5, −15)` into every pixel within `radius` of a
boundary whose ΔE ≥ 12. Driving the real stage over the real Tonalism candidate set: `[verified]`

| | Tonalist canvases (§2.2) | Gauguin control | Tonalism + `ContourLines` | Tonalism + merge + `ContourLines` |
|---|---|---|---|---|
| line share of the picture | **0.98%–5.94%, mean 2.61%** | 8.97% | **3.08%–39.17%, mean 17.55%** | 1.91%–17.39%, mean 7.37% |

The stage paints **6.7× more line than a Tonalist canvas has**, and its mean is **twice the
cloisonnist control**. Even with the merge run first — which is what keeps Fauvism's share in
range — it is still 2.8× the corpus mean and its worst case (17.4%) exceeds the corpus maximum
three times over.

Note the causal direction, which the Post-Impressionism round identified and which bites hard here:
**canvas share is boundary density × band width, and neither is bounded.** Tonalism as shipped
carries **559.2 boundary pairs per 1000 px** with 46.1% of pixels adjacent to a colour change
`[verified, §5.1]`, so the more fragmented the row the more of it becomes line. `ContourLines` is
at its heaviest exactly where the underlying render is worst — which is why running the merge first
pulls the mean from 17.55% to 7.37% without fixing anything.

### 6.2 All three known defects reproduce, and two are worse here than elsewhere

| Defect | Prior finding | On Tonalism |
|---|---|---|
| **Band width does not scale with the mark** | `Math.Round(mark × 0.10)` collapses to 1 for `MarkPixels` 2–12 | **radius = 1 on all 14 photographs**, at marks from 2.4 to 7.2 — a 3× mark range with a constant 4 px band. Relative width therefore runs 1.67 marks down to 0.56 marks *within one corpus*. `[verified]` |
| **Canvas share is not a parameter** | mean 23.8%, up to 55.8% | mean **17.55%**, up to **39.17%**. Lower than the prior round's figure only because Tonalism's ΔE-12 gate fires less often on a low-contrast style — which is the stage doing the right thing for the wrong reason, since nothing bounds it. |
| **Line lightness is absolute** | target lands at L\* 37.0 on the six-paint fixture | On Tonalism's **mother-coloured** candidate set the same call resolves to **L\* 42.18, a\* 7.90, b\* −11.21** — lighter still, because the mother colour raised the floor. Field lightness across the corpus runs **45.4 to 67.5**, so line-minus-field swings from **−3.2** (moonlit street) to **−25.3** (baobab against sky). The canvases sit at a consistent **−12.3 to −15.4**. `[verified]` |

The third row is the interesting one: **the corpus target is a relative rule and the stage
implements an absolute one**, exactly as the Post-Impressionism round found — but on Tonalism the
mother colour moves the absolute value too, so two shipped stages interact to produce a line whose
lightness relative to its field is uncontrolled in both directions.

### 6.3 The historical ground is unusually strong here

Every source in §2.1 says the same thing, and one of them says it as a principle rather than a
description: **"To substitute a line for the edge of a value relationship is to substitute
something that is not there for something that is."** `[verified]` A Tonalist row that draws
contours is not a variant of Tonalism; it is the thing Tonalism was defined against.

### 6.4 And it would duplicate Fauvism byte-for-byte

All three post-map stages still declare `Parameters => Array.Empty<StyleParameter>()`. `[verified —
read from the source]` A second style registering `ContourLines` gets **exactly** Fauvism's line,
not a version of it. The Post-Impressionism round rejected it on this ground; nothing has changed.

**One thing worth salvaging, and one measurement caveat.** `ContourLines` alone does move
Tonalism's sub-mark share 25.77% → 12.34%, and it is tempting to read that as half a fix. It is
not: it works by overwriting small regions with one index, so the fragments are hidden rather than
merged. The merge reaches 0.00% and paints nothing. `[verified]`

**The caveat, which cuts the same way.** Every sub-mark figure I quote for a contour variant —
12.34%, 0.53%, and Fauvism's 0.60%–2.34% in §5.2 — is measured **without excluding the line index**,
because the line fuses into one enormous region and *deflates* the metric. The Fauvism round's trap
applies and these figures are therefore **optimistic**; the true contour-excluded numbers are
higher. I did not recompute them line-excluded because the ruling does not turn on them: the
verdict rests on canvas share (§6.1) and the defects (§6.2), neither of which the trap touches.
Record it as a known understatement rather than a corrected number.

---

## 7. `MarkScale` 1.2 — inert, not backwards

The brief asks whether a small mark is right for a style whose forms are large soft masses, or
whether that is backwards. **Neither. The number does not currently do anything.**

### 7.1 The mechanism

`MarkPixels` reaches four consumers in the app: `EdgePreservingFloor.cs:63` (→
`PalettePhotoConverter.FloorRadius(m) = Max(Round(m / 2), 1)`), `SmallRegionMerge.cs:27`,
`ContourLines.cs:28` and `GroundFill.cs:26`. `[verified — grepped]` **Tonalism registers only the
first.** So `MarkScale` 1.2 sets one integer: a guided-filter window radius.

And that integer usually does not change. Driving the real
`PalettePhotoConverter.FloorRadius(markPixels)` at `MarkScale` 1.0 and 1.2 across the default
range: `[verified — computed by calling the shipped method]`

| base mark | short edge (px) | radius at ×1.0 | radius at ×1.2 | differs? |
|---|---|---|---|---|
| 2 | 225–375 | 1 | 1 | **no** |
| 3 | 375–525 | 2 | 2 | **no** |
| **4** | **525–675** | **2** | **2** | **no** |
| 5 | 675–825 | 2 | 3 | yes |
| 6 | 825–975 | 3 | 4 | yes |
| 7 | 975–1125 | 4 | 4 | **no** |
| 8 | 1125–1275 | 4 | 5 | yes |
| 9 | 1275–1425 | 4 | 5 | yes |
| 12 | 1725–1875 | 6 | 7 | yes |

Base marks 2, 3 and 4 cover images with short edges from 225 to 675 px, and base 4 is exactly what
a 960×640 photograph gets. **On the app's own default for an ordinary photograph, `MarkScale` 1.2
and 1.0 render byte-identically through the only stage that reads the mark.** (The `no` at base 7
is `Math.Round`'s banker's rounding: 3.5 → 4 and 4.2 → 4.)

### 7.2 The sweep confirms it

`MarkScale` 0.8 → 3.0, slot 5 empty, mean over the 14 photographs: `[verified]`

| MarkScale | mean mark | regions | median | colours | below own mark² | largest region | mean ΔE |
|---|---|---|---|---|---|---|---|
| 0.8 | 3.20 | 95,310 | 1.0 | 328 | 21.99% | 11.2% | 6.41 |
| 1.0 | 4.00 | 96,341 | 1.0 | 324 | 23.97% | 12.9% | 6.42 |
| **1.2 (shipped)** | **4.80** | **97,389** | **1.0** | **323** | **25.77%** | 12.9% | **6.42** |
| 1.6 | 6.40 | 98,792 | 1.0 | 319 | 28.15% | 13.1% | 6.41 |
| 2.0 | 8.00 | 100,657 | 1.0 | 315 | 29.90% | 12.0% | 6.42 |
| 2.5 | 10.00 | 102,513 | 1.0 | 313 | 31.80% | 12.1% | 6.41 |
| 3.0 | 12.00 | 103,637 | 1.0 | 311 | 33.21% | 12.1% | 6.40 |

**A 3.75× change in the requested mark moves the region count by 8.7%, the colour count by 5%, and
mean boundary ΔE by 0.02.** The sub-mark share rises monotonically only because the threshold it is
measured against grows as mark². The picture is essentially the same picture at every setting.
Region counts rise slightly *with* mark because the wider filter window occasionally splits a flat
passage across a candidate boundary — the opposite of the intent.

### 7.3 With slot 5 filled, the same sweep becomes a real control

| MarkScale | mean mark | regions | median area | colours | below own mark² | largest region | mean ΔE |
|---|---|---|---|---|---|---|---|
| 0.8 | 3.20 | 3,251 | 27.9 | 156 | 0.00% | 16.4% | 7.02 |
| 1.0 | 4.00 | 2,106 | 43.5 | 138 | 0.00% | 17.2% | 7.07 |
| **1.2** | **4.80** | **1,488** | **67.5** | 125 | 0.00% | 17.5% | 7.20 |
| 1.6 | 6.40 | 879 | 122.4 | 107 | 0.00% | 17.7% | 7.31 |
| 2.5 | 10.00 | 407 | 338.9 | 81 | 0.00% | 19.5% | 7.65 |

Median region area moves **12×** across the same sweep. `[verified]`

### 7.4 The ruling

**Keep 1.2 and fill slot 5, rather than moving the number.** `[inferred]`

The argument for a *larger* mark is real — the sources ask for "the broad, graphic, ultimately
abstract reading of major forms" and 1.2 is the smallest scale of the four styled rows (Realism's
1.0 is the do-nothing baseline), on the style with the
softest forms — but it is the second decision, not the first. Tonalism at `MarkScale` 1.2 with the
merge already reaches median region area 67.5 px against Realism's 1 px, and the value-mass
measurement (§3.2) is the right basis for choosing the final number. Raising `MarkScale` before
slot 5 is filled reproduces the trap the Fauvism and Post-Impressionism rounds each named: **asking
for a bigger brush and building nothing that holds one.**

---

## 8. Tonalism and Realism next door — measurably different

Tonalism sits directly after Realism in `StyleRegistry.All`, and both had an empty slot 5 until
this round. The registry's own doc comment says every Tonalism override "exists because the style
should look different from Realism the moment it is selected". It does. `[verified]`

Per-pixel comparison of the two real renders over the 14 photographs: `[verified]`

| | Realism | Tonalism |
|---|---|---|
| pixels that differ | — | **100.0%** (12 of 14 images at exactly 1.0000; two at 0.9994) |
| mean ΔE between the two | — | **18.63** (median 19.57, p95 30.09) |
| mean L\* | 51.8 | 56.0 |
| **sd L\*** | **20.63** | **9.62** (×0.47) |
| **mean C\*** | **18.98** | **8.83** (×0.47) |
| p1 – p99 L\* | 15.8 – 89.9 | 43.8 – 76.2 |
| distinct colours | 832 | 323 |
| regions | 166,528 | 97,389 |

Against median candidate nearest-neighbour spacing of **1.70 ΔE** (parent README), a mean
separation of 18.63 is an order of magnitude clear. **The two rows are not confusable.**

But read the last two rows again. **Tonalism is Realism with the value range and the chroma both
halved and the fragmentation only slightly reduced.** Its distinguishing move is entirely
chromatic; its structural behaviour is Realism's. That is precisely the shape the Fauvism and
Abstract rounds each diagnosed on their own styles, and the fix is the same one: fill slot 5.

---

## 9. Where this corrects or extends prior research

**Corrects:**

1. **Tonalism's published paintability figure is 33× too small for the app's real default.** The
   0.77% in `StyleBehaviourTests` and quoted in three rounds' cross-style tables is a 6 px
   threshold on a 256² synthetic gradient. Photographs at the app's own default give **25.77%**.
   §4.2, §4.3.
2. **"Tonalism is the most achievable style in this report — no caveats needed"** (report 02 §6,
   and the parent README's build-order item 2) **is wrong on the structural axis.** Every *colour*
   property of Tonalism is indeed pointwise and the shipped row delivers them. But the style's own
   sources put "the broad, graphic, ultimately abstract reading of major forms" first, and that is
   spatial. The style was built as though it had "zero spatial or semantic component" and it is now
   the app's worst styled row on the spatial metric. §3, §4.
3. **Report 02's "value range of maybe three steps" should not be quoted as a histogram claim.**
   Measured over seven Tonalist canvases, p99 − p1 runs **37.8 to 68.0 L\*** (mean 52.5) and sd L\*
   **8.51 to 20.10** (mean 14.33) — *wider* than the app's Tonalism realises (32.4 and 9.62). The
   narrow thing is the key and the chroma, not the excursion. §2.2. Report 02 already flagged the
   claim as coming from a painting-instruction blog; this is the measurement that closes it.
4. **The Post-Impressionism round's `MotherColourTransform` figure was measured on a different
   palette and the effect is larger than recorded.** They reported darkest L\* 11.0 → 38.3 at
   fraction 0.30; on the six-paint `StyleTestFixtures` palette I measure **6.46 → 40.30**, a rise
   of 33.8 rather than 27.3. The finding is confirmed and strengthened. §3.3.
5. **`ContourLines` re-introduces sub-mark regions after the merge has removed them, and that is
   the whole of Fauvism's remaining fragmentation.** With the rewritten merge, Fauvism's chain
   leaves 0.60%–2.34% while two merges leave exactly zero on the same images. §5.2. Nobody has
   recorded this, because until this working tree the merge never reached zero for anything to be
   re-broken.

**Extends:**

6. **The Post-Impressionism round's verification debt 1 is cleared and the answer is yes.** The
   smallest-first union-find sweep reaches exactly zero in one pass on 14 photographs, for three
   styles, and is idempotent. §5.2. The hard assertion should now be written as a registry-wide
   test.
7. **Report 03's "nothing in slots 1 or 5 can soften an edge" holds on a fifth style**, and it is
   the constraint that most limits a Tonalism row: adding the merge raises mean boundary ΔE
   6.42 → 7.20. The only measured operation that softens is **raising the floor** (6.42 → 5.99 at
   strength 5), which makes floor strength a *style* parameter for Tonalism rather than only a
   denoising one. §3.1, §5.1.
8. **The Post-Impressionism round's cloisonnist line-share corpus cross-validates.** My detector
   measures *Vision after the Sermon* at 8.97% against their 11.2%, on a different reproduction
   with a different window. Two independent implementations, same order. §2.2.
9. **Sigaki et al. does not name Tonalism**, reproducing the negative result that round found for
   Post-Impressionism. §1.3.

**New, and not in any prior report:**

10. **An area opening preserves length, not thinness.** A 2 px shroud running 800 px survives
    `SmallRegionMerge` at mark² 52 because its area is 1,600; a 2 px branch tip 20 px long does
    not. `[verified — measured and inspected, §3.2b]` This is the rule that decides which
    "line-like structure" survives in any style with the merge registered: masts, horizons, trunks,
    walls and cables come through; twigs, sparks, distant fences and rigging *ends* do not.
    Width is irrelevant. Worth stating because three rounds have discussed the merge's cost to thin
    features without anyone naming the criterion.
11. **The notan gap is a cheap, discriminating statistic that nobody has used**, and it separates
    the five styles where region counts do not: source 34.93, Realism 34.35, Fauvism 28.26,
    Post-Impressionism 31.38, Abstract 23.68, **Tonalism 15.59**. `[verified, §3.2]` It is four
    lines of code and it catches the one failure mode the mass measures cannot — a picture whose
    shapes are large and whose values are all the same.
12. **Tonalism is the only style in the app whose floor strength is a *style* parameter rather than
    a denoising one**, because it is the only measured operation anywhere in slots 1 or 5 that
    softens an edge, and soft edges are this movement's named technique. §5.1, §10 pick 2.

**Could not settle:**

- Whether any conservation or technical study reports a *measured* value range for a Tonalist
  canvas. Searched; nothing found. The Whistler "sauce" literature is about the medium (turpentine,
  copal, linseed, with mastic and bleached shellac identified in some nocturnes but not all)
  `[relayed]`, not about tone.
- Whether Corn's 1972 catalogue states a formal criterion for inclusion, or only a mood. The
  49-paintings / 46-photographs / 31-artists figures are `[relayed]` from secondary accounts.

---

## 10. Build items, ranked by payoff ÷ cost

Line counts are C#-from-scratch estimates in the style of `Imaging/Styles/Stages/`, excluding UI.

### 1. Register `SmallRegionMerge` on Tonalism — **one line**

**Slot 5.** Add `new IPostMapStage[] { new SmallRegionMerge() }` to the Tonalism row.

*Evidence.* Sub-mark share **25.77% → 0.000000** on all 14 photographs; regions 97,389 → 1,488;
median region area 1 → 67.5 px. Idempotent, one pass, verified on every image. `[verified, §5]`
The stage exists, is registered on three other styles, and is invariant-safe by the `Refine`
signature. This is the single highest payoff-per-line item anywhere in this round.

*It also turns `MarkScale` into a real parameter*, which is the prerequisite for every other
structural decision about this row (§7.3).

*Verification.* `FractionInRegionsSmallerThan(pixels, …, mark²)` must be **exactly zero** after one
invocation, on photographs and not only on the golden — and it now is, so **write the assertion as
a test over the whole registry**, with the Fauvism exception (§5.2) either fixed or documented.
Regenerate `Tests/Golden/Tonalism.png` and look at it. Expect the golden's 344 regions to fall
toward Post-Impressionism's 101.

*Risk, and it is real.* Merging erases short thin features, and Tonalism's subjects are exactly the
ones that have them. Thin-dark-structure retention falls **31.5% → 12.9%** across the corpus, and
on the baobab the branch tracery visibly goes (§3.2b, §5.3). Long thin features — masts, horizons,
trunks, cables — survive, because an area opening cares about area rather than width. **Ship it
anyway**: a 20 px branch tip at a 6 px mark is not paintable, and the honest answer for that
subject is the mark slider, not a leaky invariant. But say so in the doc comment.

### 2. Raise `EdgePreservingFloor` strength from 2.0 to 4.0 — **one line**

**Slot 1.**

*Evidence.* Strength 2 → 5 takes the sub-mark share 25.77% → 16.12% *and* softens the picture
(mean boundary ΔE 6.42 → 5.99, hard-edge share 17.7% → 15.9%) — **the only lever measured in this
round that improves paintability and edge quality together.** `[verified, §5.1]` And it is the
only device available at all for the movement's defining "lost edge", because nothing in slots 1 or
5 downstream can soften anything (§3.1).

*Why 4.0 and not 5.0.* 5.0 is Abstract's registered value, and Tonalism should not share the
registry's strongest floor with the style whose whole premise is maximal flattening. 4.0 is
untested — **measure it** — and the interpolated expectation is ≈18%, which the merge then takes to
zero regardless. The floor's job here is edge *quality*, not the metric.

*Verification.* Mean boundary ΔE must fall and hard-edge share with it; regenerate the golden. If
the picture reads as smeared rather than atmospheric, take 3.0.

### 3. Give `SmallRegionMerge` a threshold parameter — **~40 lines** — and leave it at 1.0

**Slot 5.** One `StyleParameter`: a multiplier on `minimumArea`, default 1.0, an exact no-op.

*What this is not.* I expected to recommend raising it for Tonalism, on the strength of "the broad,
graphic, ultimately abstract reading of major forms", and **the measurement refused**. Real
Tonalist canvases put **76.5%–91.5% (mean 84.4%)** of their area in value masses of at least one
mark², with the largest single mass at **13.2%–36.1% (mean 23.3%)**. Tonalism with the merge at
multiplier 1.0 already reaches **100.0%** and **37.1%** — *past* the corpus on both. `[verified,
§3.2]` Raising the threshold would take a row that already over-consolidates and consolidate it
further. **Record this as measured and do not build the knob-turn.**

*Why the parameter should exist anyway.* It fixes the structural defect three rounds have now
named: **all three post-map stages declare `Parameters => Array.Empty<StyleParameter>()`**, so slot
5 has no tuning surface at all and two styles registering the same stage get byte-identical
behaviour rather than a version of it. `[verified — read from the source]` The pipeline's central
design claim is that a stage generalises because a style can retune it; in slot 5 that mechanism
exists and nothing uses it. Adding one parameter to the stage every style now wants is the cheapest
place to break the deadlock, and the *right* first use of it is the opposite of what I expected:
a style with fine subject matter might want a multiplier **below** 1.0.

*Verification.* At multiplier 1.0 the buffer must be byte-identical to today's, on all five
goldens. Sweep against the canvas value-mass figures in §3.2, not against taste, and expect the
sweep to say "stay at 1.0".

### Not ranked, and deliberately

**Fixing the mother colour** is the largest single improvement available to this row (§3.3) and
**track 1 owns it**. I record only the structural consequence: at fraction 0.30 the style cannot
render a nocturne, a silhouette against a dark field, or any of the six canvases in §2.2 that sit
below mean L\* 45. Whatever track 1 concludes, note that removing the mother colour is *also*
slightly paintability-positive here (25.77% → 24.32%) and cuts the colour count by half.

---

## 11. What not to build

The parent, Abstract, Fauvism and Post-Impressionism lists all still apply. These are additional,
and I went looking for each.

- **`ContourLines` on Tonalism, at any setting.** §6. Measured at 6.7× the corpus line share and
  twice the cloisonnist control; all three defects reproduce; the historical ground is the
  strongest in the app; and it would be byte-identical to Fauvism's. **This is the clear negative
  the brief asked for.**
- **A "soft edge" or feathered-contour stage of any kind.** The `Refine` signature admits no
  partial coverage, and post-map arithmetic breaks the invariant. Report 03's category-D glaze is
  the only honest route and it is a much larger decision. The buildable proxy is floor strength
  (pick 2).
- **Raising `MarkScale` above 1.2 before slot 5 is filled.** Measured: 1.2 → 3.0 with slot 5 empty
  moves the region count 6% and the picture not at all, while the sub-mark share climbs 7 points
  purely because the bar moves. §7.2. The same trap the Fauvism and Post-Impressionism rounds each
  named, now measured in its purest form — here the mark does not even reach the floor's radius on
  a typical photograph.
- **Lowering `MarkScale` toward Realism's 1.0 "because Tonalism is not a broad-brush style".** The
  sources say the opposite ("broad, graphic reading of major forms") and the measurement says the
  number is currently inert either way. Fix the consumer, not the constant.
- **A separate "Nocturne" or "Pictorialism" style row.** §1.2. The founding exhibition of the term
  put paintings and photographs in one room at near-parity, and the Whistler/Inness difference is
  one signed L\* shift the app already exposes as `key`. Revisit only if the mother colour is fixed
  *and* the row still cannot reach a nocturne.
- **Splitting on "American Tonalism" versus Whistler.** Refused by the scholar who coined the term.
- **A drawn horizon, mast or branch "structure" stage** — the tempting reading of "some line-like
  structure survives". Every thin dark structure in the §2.2 canvases is *subject matter*, not
  drawing, and reaching it would need to know what the pixels depict. The parent README's rejection
  of semantic segmentation covers it unchanged.
- **Validating any Tonalism stage by dark-area fraction.** The Post-Impressionism round found this
  does not separate cloisonnist canvases from their controls; here the Tonalist mean (2.61%) and
  the Gauguin control (8.97%) *do* separate, but only because the detector is measuring thin
  structures. Dark share and line share are the same number in this construction (§2.2), so the
  measure cannot distinguish "few lines" from "few small dark things".
- **Median region elongation, structure-tensor coherence on mapped output, or any orientation
  statistic** for this style. Carried forward from the Post-Impressionism round's correction 11 and
  re-confirmed: Tonalism has no directional claim to test.
- **Raising `SmallRegionMerge`'s area threshold above one mark² for this style.** §3.2, and I went
  in expecting to recommend it. Tonalism with the merge already puts **100.0%** of the picture in
  value masses of at least mark² and its largest mass is **37.1%**, against a Tonalist-canvas mean
  of 84.4% and 23.3%. The row over-consolidates before the knob is turned.
- **Treating the shipped `EveryRegisteredStyleIsPaintable` ceilings as evidence of anything.** All
  five are measured at a threshold between 6 px and 25 px on a synthetic gradient. §4.3.
- **Reading "large value masses" off this app's output as evidence that it is composing well.** The
  measure is inflated by lightness compression: nine equal L\* bands over a picture with sd L\*
  9.62 puts everything in two or three bands. Tonalism reaches a higher mass share than the
  paintings while having **half their notan gap** (15.59 against a source figure of 34.93 and
  Post-Impressionism's 31.38). Always report the notan gap beside it. §3.2.

---

## 12. Verification debt

Ranked by how much clearing each would change a decision above.

1. **My canvas corpus is seven works and I curated it myself.** Six candidates were rejected on
   inspection (§13) and the survivors are uncalibrated web reproductions. The *ratios* (line share,
   dark share, value-mass share) and the *signs* are robust; the absolute L\* and C\* coordinates
   carry unknown reproduction error, and §3.3's comparison of canvas mean L\* against the app's
   realised mean L\* is the place that error would bite hardest. **A calibrated or
   museum-colour-managed reproduction of any one nocturne would settle it.**
2. **Measure `EdgePreservingFloor` strength 4.0.** Pick 2 recommends a value I did not measure; I
   measured 1, 2, 3 and 5 and interpolated. An hour's work.
3. **Wanda Corn, *The Color of Mood: American Tonalism 1880–1910* (1972).** The 49-paintings /
   46-photographs / 31-artists figures and the foundational five are `[relayed]` from secondary
   accounts. The catalogue is the primary source for the whole of §1 and I did not reach it. It is
   out of print and not online; a library copy would settle the boundary ruling properly.
4. **The Artsy page carrying David Adams Cleveland's twelve characteristics returned 403.** I read
   its content through a secondary rendering. The "lost-edge technique" and "broad, graphic,
   ultimately abstract reading of major forms" phrases are load-bearing for §2.1 and §10 pick 3.
5. **The Met's *Pictorialism in America* page returned 429.** The Photo-Secession "common
   denominators" quotation in §1.2 comes from a search summary of it.
6. **A primary edition of the Ten O'Clock lecture.** I read the mist passage from the University of
   Glasgow's *Whistler's Writings* site, which quotes it; the Gutenberg *Gentle Art of Making
   Enemies* does not contain the lecture. Low stakes — the passage is not load-bearing for any
   recommendation.
7. **Whether "the buttons are lost, but the sitter remains" is Whistler's own wording.** Relayed by
   TheArtStory without attribution to an edition. It is the most quotable sentence in this report
   and it is the least verified.
8. **Whether the `ContourLines` / `SmallRegionMerge` ordering defect (§5.2) also affects Abstract.**
   Abstract's chain is `GroundFill` then `SmallRegionMerge`, so the merge runs last and reaches
   zero — but I did not test a `GroundFill` after a merge. Cheap to check.
9. **Re-measure Fauvism's residual with the line index excluded from both numerator and
   denominator.** §6's caveat: my 0.60%–2.34% figure is measured with the contour counted as one
   giant region, which deflates it. The *conclusion* (the contour re-fragments what the merge
   fixed) is safe in sign because the trap only makes the number look better; the magnitude is not.

---

## 13. Corpus provenance

**Every image below was downloaded by me, in this session, with its Commons URL recorded, and every
one was displayed and looked at before use.** Three of four tracks last round were compromised by
contaminated corpora; the scratchpad this round is shared between tracks and already contained
another track's `corpus/` directory when I started, so I worked inside a private `t4/`
subdirectory and used no file I did not fetch myself.

### 13.1 Photographs (14)

All from Wikimedia Commons, fetched via the MediaWiki API at `iiurlwidth=960`, JPEG, all with
camera make/model EXIF present. Seven general subjects, seven chosen for Tonalist subject matter
(mist, dusk, nocturne, snow) so the corpus is not silently biased toward high-contrast daylight.

| key | file | camera | subject |
|---|---|---|---|
| g1_swaledale | *2014 Yorkshire Dales country road Swaledale Askrigg* | Olympus E-M5 | landscape |
| g2_yangshuo | *1 pano cuiping yangshuo 2016* | Nikon D810 | landscape, backlit |
| g3_toledo | *1 toledo spain evening sunset 2014 DXR edit* | Sony ILCE-7R | cityscape, dusk |
| g4_rose | *Bloem van een Rosa canina 01-06-2026 (actm.)* | Canon EOS M | close-up |
| g5_baobab | *Baobab (Adansonia digitata), Tarangire, 2024-05-24, DD 78* | Canon EOS 5DS R | **bare branches against sky** |
| g6_viaduct | *2015 Ribblehead Viaduct 1* | Olympus E-M1 | architecture in landscape |
| g7_mast | *Grand mât Hermione Rochefort sur Mer* | Sony DSLR-A550 | **mast and rigging against sky** |
| t1_lakeheron | *Fog around Lake Heron, Taylor Range, Canterbury, NZ* | Canon EOS 6D | fog |
| t2_mist04 | *Bergtocht van Vens naar Bettex… dichte mist 04* | Canon EOS M | **trees in dense mist** |
| t3_mist09 | *Bergtocht van Vens naar Bettex… dichte mist 09* | Canon EOS M | trees in dense mist |
| t4_strelitzer | *150502 Strelitzer Straße bei Nacht* | Canon EOS 6D, 30 s | **nocturne, moonlit street** |
| t5_rocinha | *1 rocinha night 2014 panorama* | Nikon D800, 10 s | nocturne, city |
| t6_sonnenauf | *Dülmen, Hausdülmen, Sonnenaufgang – 2015 – 4952* | Canon EOS 70D | **tree silhouette against sunrise** |
| t7_eis | *Dülmen, Hausdülmen, eisbedeckter Strauch – 2021 – 5033-7* | Canon EOS 5D Mark IV | backlit twigs, snow |

Licences: CC BY-SA 3.0 / 4.0, CC BY 3.0 de, and one CC0. Full URLs, EXIF and licence strings are in
the session scratchpad at `t4/corpus/provenance.json`; the corpus is reproducible from the file
titles above via the Commons API.

**Bias to declare.** Four of the fourteen are panoramas with short edges under 440 px, which gives
them base marks of 2–3 rather than 4–6. Seven were chosen *because* they are Tonalist subjects, so
this corpus is more favourable to Tonalism than a random photographic sample would be — and the
25.77% figure is still the second-worst in the app.

### 13.2 Paintings (7 + 1 control)

| key | work | source |
|---|---|---|
| c2 | Whistler, *Nocturne in Black and Gold: The Falling Rocket* (1875) | Commons |
| c12 | Whistler, *Nocturne: Blue and Gold – Old Battersea Bridge* (c.1872–75) | Commons |
| c3 | Inness, *Old Homestead* (1877) | Commons |
| c9 | Inness, *The Home of the Heron* (1893) | Art Institute of Chicago via Commons |
| c6 | Blakelock, *Evening* (1880–90) | Haggin Museum via Commons |
| c7 | Ryder, *Moonlight on the Sea* (1884) | Worcester Art Museum via Commons |
| c11 | Twachtman, *Winter Harmony* (c.1890–1900) | National Gallery of Art via Commons |
| x1 | *control:* Gauguin, *Vision after the Sermon* (1888) | National Galleries of Scotland via Commons |

**Six candidates were fetched and rejected on inspection**, and the list is worth recording because
five of the six would have passed an automated check:

| rejected | why |
|---|---|
| Whistler, *Nocturne* (Google Art Project) | **An etching, not a painting**, with a wide paper margin — and a *line* work, which would have inverted §2.2's finding outright |
| Tryon, *Early Morning – September* (MFA) | A **black-and-white photograph** of the painting |
| J. Francis Murphy, *Deep Woods* (Dallas) | **Framed museum photograph** — the gold frame occupies more of the file than the picture |
| Homer Dodge Martin, *Near Newport* (Cleveland) | Watercolour on paper with a wide **paper margin** |
| Whistler, *Nocturne in Blue and Green* | **Duplicate** of c12 at lower quality |
| Blakelock, *Clear Creek Canyon* (NGA) | A **pencil drawing** with a paper margin |

That is a 6-of-14 rejection rate on a hand-picked list, all six caught by looking. The
Post-Impressionism round's recommendation stands and should be escalated: **curate a shared,
provenance-checked corpus and commit it.**

---

## Appendix — how everything was measured

A throwaway console project in a private session-scratchpad subdirectory, `AssemblyName` set to
`PaintTranslator.Tests` so the app's existing `InternalsVisibleTo` grant applies, with a
`ProjectReference` to `PaintTranslator.csproj`. Nothing was added to the repository and no file
outside `docs/research/painting-style/tonalism/` was modified.

**The method rule was followed.** Every render goes through the real
`StylePipeline.Render(source, paints, style, 0, StylePipeline.DefaultValues(style))` with real
`StyleRegistry` rows; variants are produced with `StyleDefinition.WithDefaults` and the record's
`with` expression, so `EdgePreservingFloor`, `ToneAndChromaRemap`, `MotherColourTransform`,
`SmallRegionMerge` and `ContourLines` are the shipped instances, never transcriptions. The
mother-colour gamut table calls the real `MixtureBuilder.MostNeutralPaintIndex()` and
`BlendInto`/`Build`. The contour line index comes from the real `CandidateSet.FindNearest(35, 5,
−15)` on Tonalism's own candidate set. Sub-mark shares in §5.2 come from the real
`PaintabilityMetrics.FractionInRegionsSmallerThan`; Lab conversion throughout is
`PalettePhotoConverter.RgbToLab`.

Region statistics are my own only because `PaintabilityMetrics.ForEachRegion` is private and
reports areas only — the flood fill is four-connected on the RGB triple with alpha masked, matching
its semantics, and my §4.1 golden figures reproduce two prior rounds' published numbers exactly,
which is the cross-check.

Definitions of the statistics that are mine:

- **boundary pair** — a four-adjacent pixel pair whose RGB differs; **bnd/1000px** is pairs ÷
  pixels × 1000; a **transition pixel** has at least one differing four-neighbour; **boundary ΔE**
  is plain Euclidean CIELAB between the pair; **hard** is the share of boundary pairs at ΔE ≥ 10.
  These match the Post-Impressionism round's definitions so the numbers are comparable.
- **thin dark structure** — a pixel whose L\* is more than 10 below the mean of a box of radius
  3 × mark, and whose city-block distance to the edge of that dark set is at most 1.5 × mark.
- **value mass** — a four-connected component of the L\* plane quantised to nine equal bands.
- **notan gap** — mean L\* above the image's median minus mean L\* below it.

`markPixels = 0` throughout, so every photograph renders at `RenderContext.DefaultMarkPixels` for
its own dimensions multiplied by the style's `MarkScale` — the app's own default rather than a
fixed 4, which is a deliberate departure from the prior rounds and is why my cross-style
percentages are not directly comparable to their published ones. The *ranking* is.
