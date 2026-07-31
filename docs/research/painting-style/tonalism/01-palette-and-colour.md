# The Subdued Palette of Tonalism

**Date:** 2026-07-31
**Track:** 1 of 4 on Tonalism — "what should the Tonalism style row actually do to colour?"
**Shipped state under examination:** `Imaging/Styles/StyleRegistry.cs:42-64` — mark scale **1.2**;
pre-map `EdgePreservingFloor` strength **2.0**; Lab remap `ToneAndChromaRemap` contrast **0.55**,
key **+4.0**, chroma **0.45**; candidate transform `MotherColourTransform` fraction **0.30**;
`NearestQuantiser`; **empty post-map slot**.

**Relationship to prior research.** Extends [../01-colour-theory-in-practice.md](../01-colour-theory-in-practice.md),
[../02-styles-and-movements.md](../02-styles-and-movements.md) §6 and its style table (which
called Tonalism "the most achievable of all the styles" and proposed the parameters the shipped
row descends from), and the three per-style rounds. It **confirms and enlarges the
Post-Impressionism round's correction 4** on `MotherColourTransform` (§7), **contradicts the
parent README's premise that Tonalism has "zero spatial component"** (§10), and **corrects
report 02's Tonalism row in three places**:

> **Correction to [../02-styles-and-movements.md](../02-styles-and-movements.md) §6 and its
> style table, 2026-07-31.** (a) The proposed output value range **[35, 70]** with "strong
> compression toward the middle" is wrong at the dark end. Measured over 34 works, the Tonalist
> L\*p5–p95 band is **[16.1, 64.5]** and the mean is 39.4 — the range is *low*, not *central*,
> and compressing toward the middle is what the shipped row does and why its output has no
> darks (§7, §8). (b) The proposed chroma **×0.35** is roughly half the measured figure of
> **0.82**, and mean chroma turns out not to separate Tonalism from Impressionism at all (§3).
> (c) Its one *correct* and unbuilt recommendation — "lerp every pixel's (a\*, b\*) 40–60%
> toward a chosen dominant hue axis" — is the only lever that moves the one statistic that does
> separate the movement, and it is measured here for the first time (§5.1). The shipped row
> substituted `MotherColourTransform` for it, which is a different operation with a different
> effect. The *Sea and Rain* four-pigment preset the table proposes is rejected in §6.

**Claim marking:** `[verified]` = read the primary source directly, or computed it locally this
session; `[relayed]` = a source says so and I did not confirm it; `[inferred]` = my own
reasoning from marked inputs.

**Method note, because this is the fourth round in which it has mattered.** Every pipeline
number below comes from *calling* the shipped `StylePipeline.Render`, `ToneAndChromaRemap.Map`,
`NearestQuantiser.Map`, `MotherColourTransform.Transform`, `AbstractPaletteTransform`,
`MixtureBuilder.Build`, `MixtureBuilder.MostNeutralPaintIndex`, `KubelkaMunk.Mix` and
`PaintabilityMetrics`. Nothing is transcribed. No conclusion is drawn from `Tests/Golden`.
Every render figure is a mean over **eight EXIF-verified photographs**. Full method in §11.

---

## Conclusions, first

**1. "Subdued palette" is a claim about value, and it is false as a claim about chroma.**
Over a provenance-checked 34-work Tonalist corpus against 14 EXIF-verified photographs, mean
C\*ab is **16.79 vs 20.42** — a difference that does not survive a Welch *t* (t = −1.18,
df = 20). Against Impressionism it is **16.79 vs 17.31**, t = −0.21: **zero**. What *does*
separate Tonalism, in descending order of evidence, is **value spread** (L\*sd 16.13 vs 23.60,
t = −4.05, df = 31), **value key** (L\*mean 39.37 vs 50.01, t = −3.15, df = 40; vs
Impressionism 61.38, **t = −6.96, df = 34**), **local lightness contrast** (mean |ΔL\*| at
short-side/60 spacing 5.57 vs 9.30, t = −2.38) and **hue concentration** (chroma-weighted
resultant length 0.90 vs 0.68, t = +2.62, df = 16). `[verified — computed locally 2026-07-31]`

**2. The key is low, and the shipped row raises it.** The measured target is roughly
**−10 L\*** against a photograph (−14 for Whistler's nocturnes alone). The shipped row delivers
**+8.08 L\*** on eight photographs. That is a sign error on the *largest* of the two
significant value statistics, and it is the same class of error the Fauvism and
Post-Impressionism rounds found on `contrast` in their own rows. `[verified]`

**3. `MotherColourTransform` at fraction 0.30 deletes the entire dark half of the achievable
gamut, and buys a 4.3% chroma reduction for it.** Confirmed against the source and by
measurement, and it is worse than the Post-Impressionism round reported. On the six-paint
fixture palette the candidate set goes from **3,007 candidates with minimum L\* 6.46, of which
760 are below L\* 25 and 1,587 below L\* 40**, to **3,037 candidates with minimum L\* 40.30 and
*not one* below L\* 40**. Mean candidate chroma falls only 36.13 → 34.58. Rendered, the darkest
output pixel over eight photographs rises from L\* 27.78 to **L\* 42.61**. A nocturne is not
representable. `[verified]`

**4. There are two independent floors under the dark end and both are in the shipped row.**
Besides the candidate floor above, the affine part of `ToneAndChromaRemap` maps L\* = 0 to
`50(1 − contrast) + key` = 22.5 + 4.0 = **26.5** at Tonalism's settings. Removing the mother
colour alone leaves the rendered minimum at 27.78, which is that number. **Both floors have to
move**, and the fix for one does not fix the other. `[verified]`

**5. The mother colour is a whitener because `MostNeutralPaintIndex` ranks by masstone chroma
alone.** Titanium White measures C\* **0.6**, Bone Black **1.5**, and the tie-break toward
L\* 50 fires only on an *exact* chroma tie, which no real pair produces. So any palette
containing white gets white. **The fix is not a different single paint** — this library holds no
neutral near L\* 50; it holds white at 98.2 and black at 11.2. A mid-grey mother is a *mixture*,
and `MixtureBuilder.BlendInto` takes one paint index. Measured through the real builder with a
premixed 40:60 white/black grey (arithmetically identical to blending both, since K-M combines
K and S linearly by share): at fraction 0.30 mean candidate chroma falls **33.01 → 23.05
(−30%)** while mean candidate L\* moves only **41.50 → 44.51**. That is **33× more chroma
contraction per unit of lightness rise** than the shipped white. `[verified]` **The right move
for Tonalism is nevertheless to delete the stage from this row, not to repair it** — once
contrast, key and chroma are set from evidence, even the good mother colour makes every
statistic worse (§7.4). Repair it anyway for Abstract, which is the other caller.

**6. The one statistic that separates Tonalism from Impressionism is hue concentration, and
the pipeline has no stage for it — but only the per-image variant works.** A wrapper
`ILabRemap` that rotates each pixel's hue a share of the way toward a single target, leaving
L\* and C\* alone, raises delivered hue concentration from 0.670 to **0.7995** at strength 0.85
when the target is **derived from the source image's own chroma-weighted mean hue**, at no cost
in lightness or chroma and with *lower* fragmentation. Aimed at a **fixed** hue (90°, the
corpus mean) the same stage delivers **0.668** — no improvement at all. `[verified]` The
corpus target is 0.90, and on top of the retune in conclusion 9 the same stage at strength 0.55
delivers **0.899**.

**7. Tonalism has an empty post-map slot, and with the Post-Impressionism round's pick 2 applied
in the working tree it is one of only two rows that do.** 28.78% of
pixels sit in regions below the style's own mark² on eight photographs — consistent with the
Post-Impressionism round's 30.07% for this row on six different ones `[relayed via
../post-impressionism/02-colour.md; I did not re-measure the other four rows]`.
Adding the `SmallRegionMerge` already in the tree takes that to **0.75%** and region count from
75,820 to **1,490**, at a cost of 0.02 in the L\*sd ratio. `[verified — against the *repaired*
`SmallRegionMerge` in the working tree, not the committed one. Against the committed version
the Post-Impressionism round measured the postcondition failing on photographs, so **every merge
figure in this report is worth what it says only if that repair lands**; see §11.]`

**8. Tonalism's "limited palette" was a limited set of *premixed tones*, not a short pigment
list — and the app already has the stage for that.** The Glasgow catalogue raisonné lists
**twelve** pigments in one Whistler nocturne. What was limited was the number of colours he let
onto the canvas: each tone premixed in a saucer before painting began. That is a candidate-set
reduction, which `AbstractPaletteTransform` (`colourCount`, 3–12) already implements —
including pinning the lightest and darkest candidates, which is exactly what a nocturne needs.
`[relayed for the pigments; verified for the stage]`

**9. A five-number change to the shipped row hits three of the four corpus targets, and adding
the rotation hits all four.** Measured over eight photographs: dropping the mother colour,
moving contrast 0.55 → 0.75, key +4.0 → −8, chroma 0.45 → 0.85, and filling the post-map slot
gives L\*sd ratio **0.666** (target 0.683), C\* ratio **0.818** (target 0.822), ΔL\*mean
**−7.33** (target −10.6), minimum output L\* **11.60** (shipped 42.61), and **1.23%** of pixels
below mark² (shipped 28.78%). Adding the per-image hue rotation at 0.55 takes hue concentration
to **0.899** against a target of 0.90. `[verified]`

**10. And the historically correct stage beats all of it on the numbers and loses on the
picture.** `AbstractPaletteTransform` at `colourCount` 8 with `motherFraction` 0, on the same
retuned tone settings, delivers L\*sd **0.698**, C\* **0.840**, hue concentration **0.804**,
**20 distinct colours** and **0.33%** below mark² — better than the retune on every measured
axis, and it is one line to adopt. Rendered and looked at, it is **posterised**: 20 colours
across a photograph reads as a screen print, and it is worst on the foggiest, most Tonalist
subject in the set. It also reproduces the whitening defect exactly at `motherFraction` 0.30
(minimum output L\* **42.41**), and Abstract already runs it at `colourCount` 8. `[verified —
measured and looked at; §9.5]`

**11. Looking at the renders changed the ranking, and it took ten minutes.** §9.5. Three prior
rounds recorded "nobody has rendered one and looked" as verification debt and shipped
recommendations anyway. The two things a look caught that no statistic did: pick 3 posterises,
and pick 4's rotation produces a colour *cast* rather than a harmony on a subject whose warm
and cool halves are balanced.

**Four picks ranked by payoff ÷ cost in §9, a look at what they produce in §9.5, eight
rejections in §10.**

---

## Contents

1. [What "Tonalism" names, and where its edges are](#1-what-tonalism-names-and-where-its-edges-are)
2. [The corpus](#2-the-corpus)
3. [What Tonalism measurably is](#3-what-tonalism-measurably-is)
4. [Low chroma and value key are not the same lever](#4-low-chroma-and-value-key-are-not-the-same-lever)
5. [Is the dominant hue tint real, or a critical cliché?](#5-is-the-dominant-hue-tint-real-or-a-critical-cliché)
6. [The limited-palette question](#6-the-limited-palette-question)
7. [The mother colour is a whitener — confirmed, and costed](#7-the-mother-colour-is-a-whitener--confirmed-and-costed)
8. [key +4.0 and contrast 0.55, audited](#8-key-40-and-contrast-055-audited)
9. [Picks, ranked by payoff ÷ cost](#9-picks-ranked-by-payoff--cost), and §9.5 — what they look like
10. [What not to build](#10-what-not-to-build)
11. [Method](#11-method)
12. [Verification debt](#12-verification-debt)
13. [Corpus provenance](#13-corpus-provenance)

---

## 1. What "Tonalism" names, and where its edges are

**Ruling: keep one row, and target the Whistler–Inness–Blakelock centre. Twachtman is
Impressionism and should not be allowed to pull the numbers.**

Tonalism is a retrospective label for American landscape painting of roughly **1870–1915**,
descending from Barbizon by way of George Inness and William Morris Hunt, and from the Aesthetic
Movement by way of Whistler. `[relayed — [TheArtStory](https://www.theartstory.org/movement/tonalism/)
and the general survey literature, consistent across sources; I read no monograph]` Its
contemporaries were not flattering about the palette: by the late 1800s critics were calling it
the **"brown gravy" school**. `[relayed — same source]` That epithet turns out to be a
measurement (§5).

The label has the same problem Post-Impressionism had, and the corpus shows it in the same
place — the internal spread:

| sub-group | n | L\*mean | C\*mean | hue concentration |
|---|---|---|---|---|
| Whistler, nocturnes | 9 | **36.15** | **9.96** | 0.86 |
| Blakelock | 5 | **30.14** | 23.19 | 0.94 |
| Inness | 7 | 34.64 | 22.05 | 0.93 |
| Wyant + Ranger | 4 | 38.87 | 20.53 | 0.98 |
| Tryon | 5 | 45.50 | 13.43 | 0.92 |
| **Twachtman** | 4 | **59.23** | 15.46 | **0.77** |
| *(Whistler, the two Symphonies)* | 2 | *61.32* | *24.86* | *0.90* |
| *(Impressionist control)* | 10 | *61.38* | *17.31* | *0.71* |

`[verified — computed locally]`

**Twachtman sits on top of the Impressionist control on both L\*mean (59.2 vs 61.4) and hue
concentration (0.77 vs 0.71).** He is in the corpus because every survey names him a Tonalist;
he measures as an Impressionist, exactly as Cézanne measured as an Impressionist for the
Post-Impressionism round. He is kept in the aggregate below because excluding him would be
choosing the answer, but every figure in this report is 1–2 units more Tonalist without him.
`[verified]`

The two Whistler *Symphonies in White* are reported separately for the same reason and are
**excluded from every Tonalist aggregate in this report**: they are high-key figure paintings
at L\*mean 61.3, and one of the two reproductions is visibly yellowed (b\*mean +23.5 against
the nocturnes' −1.0). Whistler's "harmonies" and "symphonies" belong to the same aesthetic
programme as the nocturnes but not to the same value key, and a style row cannot be both.
`[verified]`

**The pictorialist photographers are a separate case and are reported separately.** They are
the most extreme members of the family on every value statistic — L\*sd **10.49**, C\*mean
**7.57**, local |ΔL\*| **3.42**, ΔC\*/ΔL\* **0.23** — but their colour is a *printing-process
choice*, not an observation. Steichen's *The Pond—Moonlight* exists in three platinum prints
from one negative, differentiated by gum-bichromate and cyanotype layers, and my three
reproductions of it measure C\*mean **0.1, 5.1 and 7.1** at hue **158°, 288° and 97°**.
`[verified for the measurements; [relayed](https://en.wikipedia.org/wiki/The_Pond%E2%80%94Moonlight)
for the three-print account]` **That is the single best argument in this report for exposing
the tint hue as a parameter rather than hard-coding one** (§5, pick 2).

---

## 2. The corpus

**34 Tonalist paintings, 7 Pictorialist photographs, 10 Impressionist paintings and 14 modern
photographs**, all from Wikimedia Commons, resolved by exact file title through the API and
downloaded as 760 px thumbnails. Full provenance table in §13.

Curation, in the order it caught things:

- **Automated:** paintings had to resolve to a Commons file in the expected artist's category
  or carry the artist in the filename; photographs had to carry EXIF **Make and Model and
  `DateTimeOriginal` ≥ 1990**. 13 of 14 photographs pass all three (`Brighton West Pier` has
  the capture date and no Make/Model; it is kept and flagged). `[verified]`
- **Visual, on a 71-image contact sheet — and this is where the real errors were.** Four works
  were rejected after looking at them: **three museum photographs including the frame and
  gallery wall** (Wyant *A Gray Day* MFA, Tryon *Evening Landscape* Princeton, Twachtman
  *Landscape with River* Harvard) and one — Blakelock's *Brook by Moonlight* — **photographed
  with a colour-calibration target strip inside the frame**. That last one is a failure mode no
  previous round has recorded, and on a corpus whose whole subject is low chroma it would have
  been the single most damaging image in the set. `[verified — see the rejected images in the
  scratchpad; the calibration strip is unmistakable]`
- **Automated white-mount trim**, applied after the visual pass: rows and columns in which every
  pixel exceeds 242 in all three channels are cut. Exactly two images had one — Blakelock
  *Moonlight* (Indianapolis, 7.3% of area) and Tryon *Moonrise* (Indianapolis, 13.7%). Both
  had L\*p95 = 100.0 before the trim. Trimming moved the Tonalist aggregate by 0.45 L\* and
  0.06 C\*, so the corpus is robust to it, but the two individual works were not. `[verified]`

**Caveats, in order of how much they could move a conclusion.**

1. **Varnish.** Tonalist canvases were deliberately glazed and often varnished with tinted
   media, and they are 110–160 years old. The +12.1 b\*mean in §5 is *partly* age. The control
   that limits the damage: the Impressionist group is the same medium, the same century and
   the same source population, and sits at b\*mean **+5.59**. Ageing cannot explain a 6.5-unit
   gap between two groups of aged oil scans, but it can inflate the size of it.
2. **The painting-versus-photograph comparisons carry a tone-curve confound** — the same one
   the Post-Impressionism round flagged. The strongest defence available here is that the
   confound would have to act in *opposite directions* on two groups drawn from the same
   population to explain the results: Impressionist scans are **lighter** than the photographs
   (61.4 vs 50.0) and Tonalist scans are **darker** (39.4). A systematic scan darkening cannot
   produce both. `[inferred, from verified numbers]`
3. **Subject is confounded with group.** The photographs are Commons featured landscapes,
   including four in fog or at dusk, which if anything biases the control *toward* Tonalism and
   makes the separations conservative.
4. **n is small and the *t* statistics are reported without correction for multiple
   comparisons.** Six statistics were tested against two controls. Treat t = −4.05 (L\*sd) and
   t = −6.96 (L\*mean vs Impressionism) as solid, t ≈ 2.1–2.6 (hue concentration) as
   suggestive, and the chroma nulls as what they are: an absence of evidence over 34 works, not
   proof of no effect.

---

## 3. What Tonalism measurably is

Whole-image statistics at ~700 px longest edge, sRGB → CIELAB through the app's own
`PalettePhotoConverter.RgbToLab`. "hue concentration" is the chroma-weighted circular resultant
length *R* of the pixel hues: 1.0 means every chromatic pixel shares one hue, 0 means they are
spread evenly round the circle. "local ΔC\*/ΔL\*" follows the Fauvism round's definition —
mean absolute difference to the pixel *r* away horizontally and vertically, *r* = short side ÷ 60.

| group | n | L\*mean | **L\*sd** | L\*p5 | L\*p95 | **C\*mean** | C\*p95 | %px C\*<20 | **hue conc.** | b\*mean | ΔC\* | ΔL\* | ΔC/ΔL |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Tonalist** | 34 | **39.37** | **16.13** | 16.08 | 64.47 | **16.79** | 32.11 | 69% | **0.90** | **+12.10** | 3.30 | 5.57 | 0.60 |
| Whistler nocturnes | 9 | 36.15 | 12.65 | 17.70 | 55.08 | 9.96 | 16.44 | **92%** | 0.86 | −1.01 | 1.73 | 3.66 | 0.48 |
| Pictorialist | 7 | 37.38 | **10.49** | 26.29 | 57.64 | **7.57** | 12.91 | 85% | 0.91 | +5.49 | 0.81 | 3.42 | **0.23** |
| Impressionist | 10 | 61.38 | 13.22 | 38.78 | 81.29 | 17.31 | 30.91 | 67% | 0.71 | +5.59 | 4.63 | 7.45 | 0.67 |
| **Photograph** | 14 | 50.01 | 23.60 | 11.12 | 84.00 | 20.42 | 42.58 | 59% | 0.68 | +10.23 | 4.96 | 9.30 | 0.55 |

`[verified — computed locally 2026-07-31]`

Welch two-sample *t*, Tonalist against each control:

| statistic | vs photograph | vs Impressionist | reading |
|---|---|---|---|
| L\*sd | **t = −4.05, df 31** | t = +1.79, df 17 | Narrower than a photograph; **not** narrower than Impressionism |
| L\*mean | **t = −3.15, df 40** | **t = −6.96, df 34** | Low key, and this is the strongest result in the report |
| hue concentration | **t = +2.62, df 16** | t = +2.10, df 11 | Suggestive both ways |
| local ΔL\* | **t = −2.38, df 15** | (not run) | Lower local lightness contrast |
| **C\*mean** | t = −1.18, df 20 | **t = −0.21, df 15** | **Nothing** |
| ΔC\*/ΔL\* | t = +0.59, df 29 | (not run) | Nothing |

`[verified]`

**The headline is the last two rows.** The Fauvism round's ΔC\*/ΔL\* separator, which the
Post-Impressionism round found separates twentieth-century high-chroma painting from
photographs, does not separate Tonalism from anything. And mean chroma — the statistic the
shipped row spends its slot-2 and slot-3 budget on — is **statistically indistinguishable from
Impressionism**, a movement this app does not even have a row for.

Note also what *is* narrow. Tonalist L\*sd 16.13 is 0.68× the photographs' but **1.22× the
Impressionists'**. "A narrow value range around a dominant key" is therefore only half right:
Tonalism is narrower than a photograph, but the thing that makes it look narrow is that the
range sits **low**, where a viewer reads differences less readily, and that the *local*
lightness contrast is low (5.57 against 9.30) while the global range is not especially so. The
Pictorialists are the limiting case — L\*sd 10.49 and local ΔL\* 3.42 — and they are also the
group nobody would confuse with a photograph. `[inferred, from verified numbers]`

---

## 4. Low chroma and value key are not the same lever

The brief asks whether the usual description — "a narrow value range around a dominant key" —
survives measurement, and whether the key is high or low.

**Low, and by about 10 L\*.** Tonalist L\*mean 39.37, photographs 50.01, Impressionists 61.38.
The Tonalist L\*mean SD across works is **13.65**, against 7.92 for the photographs and **5.97**
for the Impressionists — so the movement is not merely darker, it is *more variable in key*
than either control, ranging from Blakelock's *Afterglow* at L\*mean 10.67 to Twachtman's
*White Bridge* at 64.18. `[verified]`

That variability is the design argument for **exposing key as a slider with a low default**
rather than picking one number, and it is also why `ToneAndChromaRemap` was right to separate
`key` from `contrast` in the first place.

**Chroma and value are coupled in this library in a way that makes the "low chroma" reading
partly an artefact.** CIELAB C\* falls with L\* for a fixed pigment, because a dark paint
reflects less of everything. Blakelock's canvases are the darkest in the corpus (L\*mean 30.14)
and among the *most* chromatic (C\*mean 23.19); Whistler's nocturnes are lighter (36.15) and far
less chromatic (9.96). So chroma is not tracking key within the movement — the two are genuinely
independent axes here, and the shipped row moves them in the same direction. `[verified]`

The practical statement for the style row: **lowering the key is the operation; lowering chroma
is at most a secondary consequence, and lowering it hard is a different style (the
Pictorialists', not the painters').** `[inferred]`

---

## 5. Is the dominant hue tint real, or a critical cliché?

**Real, measurable, and the only thing in this study that separates Tonalism from
Impressionism on an axis the pipeline could act on. But the *hue* is per-picture, not
per-movement.**

Chroma-weighted hue resultant length, per work:

| group | mean *R* | SD | interpretation |
|---|---|---|---|
| Tonalist | **0.90** | 0.15 | one hue dominates each picture |
| Pictorialist | 0.91 | — | one toner |
| Impressionist | 0.71 | 0.25 | |
| Photograph | 0.68 | 0.28 | |

`[verified]`

Twenty-eight of the 34 Tonalist works measure *R* ≥ 0.86. Wyant and Ranger average **0.98**;
Inness **0.93**; Blakelock **0.94**. The exceptions are Twachtman's *October* (0.23) and
Whistler's *Nocturne in Green and Gold* (0.53).

**But the dominant hue itself is not shared.** Aggregating the per-work mean hues as vectors
gives a movement-level resultant of only **R = 0.66 at 91°** — i.e. a warm-yellow centre with
real dispersion. Per sub-group: Blakelock 75°, Inness 81°, Tryon 92°, Wyant/Ranger 87°,
Twachtman 120°, and **Whistler 117° with an across-works resultant of only 0.25** — his nine
nocturnes run from 22° through 105°, 113°, 168°, 190°, 244°, 254° to 326°. `[verified]`

That is exactly what the titles say. *Blue and Silver*, *Blue and Green*, *Grey and Gold*,
*Green and Gold*, *Black and Gold*: **Whistler named the harmony of each picture individually.**
Writing in *The World*, 22 May 1878, he put it as "the artist is born to pick, and choose, and
group with science, these elements, that the result may be beautiful — as the musician gathers
his notes, and forms his chords, until he bring forth from chaos glorious harmony."
`[relayed — "The Red Rag", reprinted in *The Gentle Art of Making Enemies*, 2nd ed. 1892,
pp. 142–43; I read search-index excerpts of the reprint, not the original]`

**The warm centre is partly the "brown gravy", and partly varnish.** Inness, Blakelock, Wyant
and Ranger average b\*mean **+20.3, +20.8, +19.6** and warm-hue shares of 87–91%. Whistler's
nocturnes average b\*mean **−1.01** and a warm share of **30%**. So the golden bias is real for
the American Barbizon wing and absent from Whistler; a single hard-coded warm tint would be
right for two-thirds of the movement and wrong for the third that gave it its name. `[verified]`

### 5.1 What a hue-convergence stage delivers, measured

I built the obvious stage as a wrapper `ILabRemap` — the shipped `ToneAndChromaRemap` runs
underneath, and its output hue is then rotated a share *s* of the way toward a target,
preserving L\* and C\* exactly — and rendered eight photographs through `StylePipeline.Render`
with it. All rows below have the mother colour off, so the only change is the rotation.

| variant | delivered hue conc. | C\* ratio | L\*sd ratio | distinct colours | % below mark² |
|---|---|---|---|---|---|
| no rotation (control) | 0.670 | 0.509 | 0.505 | 208 | 26.99 |
| s = 0.30, target **90° fixed** | 0.696 | 0.504 | 0.501 | 167 | 26.43 |
| s = 0.60, target **90° fixed** | 0.691 | 0.507 | 0.505 | 145 | 26.01 |
| s = 0.85, target **90° fixed** | **0.668** | 0.503 | 0.509 | 122 | 25.45 |
| s = 0.60, target **250° fixed** | 0.778 | **0.443** | 0.496 | 194 | 28.62 |
| s = 0.60, target **image's own hue** | 0.764 | 0.509 | 0.507 | 155 | 25.50 |
| **s = 0.85, target image's own hue** | **0.800** | **0.509** | 0.510 | 125 | **25.18** |

`[verified — computed locally]`

Three readings, and the first is the one that would have been missed by reasoning about the ask:

- **Rotating everything to a fixed 90° does nothing to the delivered hue concentration** —
  0.670 → 0.668 at s = 0.85, despite the ask being near-total. The reason is the same shape as
  the Post-Impressionism round's per-hue-ceiling result: at Tonalism's delivered mean chroma of
  9.7, the a\*b\* displacement the rotation asks for is small against the spacing of
  near-neutral candidates, and `NearestQuantiser` absorbs it. **The ask moves; the picture does
  not.** `[verified]`
- **Rotating toward the image's own dominant hue does work**, 0.670 → 0.800, because that target
  is by construction a hue the palette already reaches near the image's colours. It also
  *reduces* fragmentation (26.99% → 25.18%) and cuts distinct colours by 40%.
- **A fixed cool target costs chroma**: 250° pulls toward a sector the six-paint fixture cannot
  reach, and the delivered C\* ratio falls 0.509 → 0.443 as targets slide toward the neutral
  axis. Any hue parameter must therefore be evaluated against the *user's* candidate set, which
  is the same lesson the Fauvism round drew about the chroma ceiling. `[verified]`

The corpus target is 0.90 and s = 0.85 reaches 0.80, so on the shipped tone settings this closes
about **59%** of the gap between a converted photograph and a Tonalist painting on the one
statistic that separates the movement from Impressionism. `[verified]`

**On the retuned tone settings of §9 pick 1 it closes all of it.** With contrast 0.75, key −8,
chroma 0.85, no mother colour and `SmallRegionMerge` in slot 5, adding the rotation at only
s = 0.55 moves delivered hue concentration from **0.775 to 0.899** against a corpus target of
0.90 — with the L\*sd ratio unchanged at 0.688 and the C\* ratio 0.818 → 0.807. The stage works
better at a *lower* strength once chroma is no longer being crushed, which is consistent with
the mechanism above: the rotation is only realisable when the target sits far enough from the
neutral axis for the candidate spacing to resolve it. `[verified]`

---

## 6. The limited-palette question

**Tonalism's "limited palette" is not a short pigment list, and treating it as one would repeat
the mistake the Fauvism round already rejected.**

The materials record says the opposite of the folklore. From the Glasgow catalogue raisonné's
technical entries:

| Work | Ground | Pigments identified |
|---|---|---|
| *Nocturne: Blue and Silver – Chelsea* (1871), YMSM 103 | "Dark grey imprimatura applied over white priming" | lead white, bone black, vermilion, synthetic ultramarine, Prussian blue, madder, dull red ochre, cobalt blue, red lake, yellow ochre, transparent red ochre, possibly gamboge — **twelve** — described as "a limited range of cool, luminous colour" |
| *Nocturne: Blue and Silver – Cremorne Lights* (1872), YMSM 115 | "off-white" priming under a "thin brown *imprimatura*" | "synthetic ultramarine, Prussian blue, madder and another red lake, but mostly lead white"; shore lights "lead white and cadmium yellow with some added vermilion"; dark underpainting possibly "black and raw umber" |
| *Nocturne in Black and Gold: The Falling Rocket* (1875), YMSM 170 | "a reddish brown base" | not enumerated; "vivid drops of orange and green fireworks" applied individually with fine brushes |

`[verified — fetched
[YMSM 103](https://whistlerpaintings.gla.ac.uk/catalogue/display/?mid=y103&xml=tec),
[YMSM 115](https://whistlerpaintings.gla.ac.uk/catalogue/display/?mid=y115&xml=tec) and
[YMSM 170](https://whistlerpaintings.gla.ac.uk/catalogue/display/?mid=y170&xml=tec) directly]`

Supporting and weaker: Whistler's working palette is described as about **twelve** colours —
"yellow ochre, raw sienna, vermilion, Venetian red, Indian red, burnt sienna, raw umber, cobalt
blue, mineral blue…, black and white" — and, crucially, **"each tone in the painting was
premixed and placed in saucers"**, at a consistency "akin to a thin glaze, rather than the
thicker consistency of tube colour", thinned with "a painting medium made of copal, mastic and
turpentine". `[relayed —
[Artists Network](https://www.artistsnetwork.com/art-history/master-of-limited-color-whistlers-painting-process/);
I did not read the underlying source]` Whistler's *Sea and Rain* (1865) is reported to use only
four pigments — cobalt blue, iron-oxide yellow, vermilion, bone black — which shows the range,
not the rule. `[relayed — English Wikipedia, uncorroborated]`

**So the limitation is downstream of the tubes.** Twelve pigments and a reddish-brown or grey
imprimatura, resolved into a small number of premixed tones laid on thinly enough that the
ground shows through. What is *limited* is the count of distinct colours that reach the canvas.

### What that implies for an app whose premise is a chosen palette of real acrylics

Three things, in decreasing order of how well the evidence supports them.

1. **Model it as a candidate-set reduction, not a pigment list.** `AbstractPaletteTransform`
   already does exactly this: image-aware *k*-means over the source in CIELAB (with L\* weighted
   1.5×), reduced to `colourCount` ∈ [3, 12] candidates, **with the lightest and darkest
   candidates pinned in** (`AbstractPaletteTransform.cs:81-85`). The pinning matters more here
   than it does for Abstract — it is the one mechanism in the codebase that guarantees a
   nocturne keeps its dark end. `[verified — read the source]`
2. **Do not ship a "Tonalist palette" preset naming pigments.** Of the pigments above, lead
   white, Prussian blue, madder, the ochres, raw sienna, burnt sienna, raw umber, Venetian red,
   Indian red and vermilion are either absent from this library or `ReflectanceDerived` and
   withheld from the picker. Whistler's palette is *mostly earths*, and the Fauvism round
   already established that a user cannot select an earth at all. **This includes report 02's
   proposed *Sea and Rain* preset** — "Cobalt Blue, Yellow Ochre (~iron-oxide yellow), C.P.
   Cadmium Red Light (~vermilion), Bone Black, Titanium White" — of which Yellow Ochre is
   `ReflectanceDerived` and withheld, and which in any case rests on one uncorroborated
   Wikipedia sentence about a single 1865 painting. A preset naming colours the picker cannot
   supply is a broken promise, and the Fauvism round rejected it for the same reason.
   `[verified via ../fauvism/03-colour.md §1.2 and the manifest]`
3. **The glazing is the part that does not transfer, and it should be said plainly in the doc
   comment.** Every technical entry above describes thin transparent layers over a toned ground.
   The converter's invariant is that every output pixel is a colour the paints can be *mixed*
   to; a glaze is a colour they can be *layered* to, which is the fourth row of the parent
   README's invariant table ("post-map, K-M layering — a different, larger, physically honest
   invariant"). Tonalism is the style that most wants it and the app is furthest from being
   able to offer it. `[inferred]`

---

## 7. The mother colour is a whitener — confirmed, and costed

**The Post-Impressionism round's correction 4 is confirmed against the source and by
measurement, and the damage in this style is larger than that round reported.**

### 7.1 The mechanism

`MotherColourTransform.Transform` is one line: `builder.BlendInto(builder.MostNeutralPaintIndex(),
values["fraction"])`. `MostNeutralPaintIndex` renders each paint's masstone, converts to CIELAB
and keeps the lowest C\*, breaking ties toward L\* 50 — but only on an **exact** chroma tie,
which `MixtureBuilder`'s own doc comment records never happens in the real library. Measured
masstones across all 19 selectable paints: `[verified — computed locally]`

| paint | masstone L\* | masstone C\* |
|---|---|---|
| **Titanium White** | **98.2** | **0.6** ← always wins |
| Bone Black | 11.2 | 1.5 |
| Dioxazine Purple | 13.6 | 6.5 |
| every other selectable paint | — | 13.3 – 91.3 |

So **any palette containing Titanium White gets Titanium White as its "mother colour"**, and a
palette without white gets Bone Black. There is no selectable paint that is both neutral and
mid-lightness; the two neutrals sit at the extreme ends of the L\* axis. `[verified]`

### 7.2 What fraction 0.30 does to the candidate set

Six-paint fixture palette (Titanium White, Hansa Yellow Opaque, C.P. Cadmium Red Light,
Quinacridone Magenta, Ultramarine Blue, Bone Black), through the real `MixtureBuilder`:

| mother | fraction | candidates | **min L\*** | mean L\* | **mean C\*** | max C\* | n below L\* 25 | n below L\* 40 |
|---|---|---|---|---|---|---|---|---|
| *(none)* | — | 3,007 | **6.46** | 40.06 | 36.13 | 89.32 | **760** | **1,587** |
| Titanium White | 0.10 | 3,026 | 26.70 | 46.89 | 36.53 | 82.51 | 0 | 1,211 |
| Titanium White | 0.15 | 3,037 | 30.90 | 49.37 | 36.22 | 83.22 | 0 | 954 |
| **Titanium White (shipped)** | **0.30** | 3,037 | **40.30** | 55.50 | **34.58** | 83.29 | **0** | **0** |
| Titanium White | 0.60 | 3,021 | 54.81 | 65.99 | 29.50 | 73.92 | 0 | 0 |
| Bone Black | 0.30 | 2,930 | 7.17 | 32.85 | **19.92** | 45.96 | 993 | 2,024 |
| Bone Black | 0.15 | 2,977 | 6.58 | 35.62 | 25.44 | 59.29 | 888 | 1,862 |

`[verified — computed locally through `MotherColourTransform` and `MixtureBuilder.Build`]`

**At the shipped fraction the achievable set contains no colour below L\* 40.30**, against a
palette minimum of 6.46. The chroma it buys is **36.13 → 34.58, −4.3%**. Bone Black at the same
fraction achieves **−45%** chroma and keeps 993 candidates below L\* 25.

The Post-Impressionism round reported 11.0 → 38.3; my figures are 6.46 → 40.30 on this palette.
The direction and magnitude agree; the small difference is palette- and measurement-dependent
and does not matter. **What that round did not report is the count: not one of 3,037 candidates
is below L\* 40.** `[verified]`

### 7.3 What "mother colour" should mean here instead

The technique is real. It is attributed to **Edgar Payne** and the practitioner literature is
unanimous on both the method — add a little of one colour to every mixture on the palette — and
the warning: "a danger with using a mother colour too strongly is that the colours are too
similar (in tone and hue), not giving the painting enough contrast, and making for a boring or
dull painting". `[relayed — consistent across
[LiveAbout](https://www.liveabout.com/definition-of-mother-color-2577647),
[Creative Ventures](https://www.creativeventuresfineart.com/what-is-the-mother-color-technique/)
and other painter-facing sources; I did not trace Payne's own text]`

**The right mother colour for this library is a chromatic grey, and a chromatic grey is a
mixture.** Measured through the real `MixtureBuilder` by handing it a premixed white/black
`PigmentCoefficients` — legitimate because `KubelkaMunk.Mix` combines K and S linearly by
normalised share, so blending fraction *f* of a 40:60 grey is arithmetically identical to
blending 0.4*f* white and 0.6*f* black:

| mother | fraction | candidates | min L\* | mean L\* | **mean C\*** | Δ mean C\* | Δ mean L\* | **ΔC per ΔL** |
|---|---|---|---|---|---|---|---|---|
| grey 40:60 | 0 (control) | 4,708 | 6.46 | 41.50 | 33.01 | — | — | — |
| **grey 40:60** | **0.30** | 4,675 | 28.88 | 44.51 | **23.05** | **−9.96** | **+3.01** | **−3.31** |
| grey 40:60 | 0.45 | 4,611 | 34.03 | 46.13 | 19.21 | −13.80 | +4.63 | −2.98 |
| grey 50:50 | 0.30 | 4,356 | 31.24 | 46.65 | 24.59 | −9.61 | +4.92 | −1.95 |
| grey 25:75 | 0.30 | 4,572 | 24.47 | 40.36 | 21.42 | −11.23 | +0.45 | −24.96 |
| *(Titanium White, shipped)* | *0.30* | *3,037* | *40.30* | *55.50* | *34.58* | *−1.55* | *+15.44* | ***−0.10*** |

`[verified — computed locally. The grey sits in the paint list so `BlendInto` can name it, which
also lets the builder enumerate it as a tube; the fraction-0 control row shows what that adds by
itself, and every comparison above is within one paint list.]`

**A 40:60 white/black mother delivers 33× more chroma contraction per unit of lightness rise
than the shipped one, and a 25:75 mother delivers 250×.** That is the whole finding: the
transform is not wrong in concept, it is being handed the wrong paint, and the right paint does
not exist as a tube.

**Cost of the fix.** Three options, cheapest first.

| Fix | Change | Cost | What it buys |
|---|---|---|---|
| **A. Repair the ranking** | `MostNeutralPaintIndex` scores `chroma + w·|L*−50|` instead of chroma with an unreachable tie-break | **~6 lines** plus a test; `IsMoreNeutral` already exists as the seam a test can drive | Picks Bone Black over Titanium White in any palette holding both. Gets the dark end back and −45% chroma at 0.30. Does **not** give a mid-grey — it swaps a whitener for a blackener, mean candidate L\* 40.06 → 32.85 |
| **B. Mixture mother** | `MixtureBuilder.BlendInto(int[] indices, double[] shares, double fraction)`; `ApplyBlend` already handles the "blend paint already present" case for one index and must generalise to several. `MotherColourTransform` picks the lightest and most-neutral paints and blends them at a ratio exposed as a second parameter | **~45 lines** plus tests | The table above. Chroma contraction at near-constant key, which is what the technique is for |
| **C. Per-image derived mother** | As B, but the white/black ratio is chosen so the blended set's mean L\* matches the source's | ~20 lines on top of B; needs `IImageAwareCandidateTransform`, which `AbstractPaletteTransform` already implements | Removes the last free parameter. Unmeasured; I did not build it |

**And note what the mother colour does right, because it is the only thing that survives.** On
eight photographs the shipped white mother raises delivered hue concentration from **0.670 to
0.745** — more than any slot-2 setting except the per-image rotation in §5.1. Contracting the
gamut toward one point *does* unify hue. It is just doing it by moving everything toward white.
`[verified]`

### 7.4 But Tonalism should not use a mother colour at all

Having established that the grey mother is 33× better than the shipped one, the measurement that
settles the row goes the other way. On the retuned tone settings of §9 pick 1, adding the grey
40:60 mother at 0.30 moves every statistic **away** from the corpus target:

| | L\*sd ratio | ΔL\*mean | min L\* out | C\* ratio | hue conc. | % below mark² |
|---|---|---|---|---|---|---|
| corpus target | 0.683 | −10.6 | (p5 = 16.1) | 0.822 | 0.90 | — |
| retune, **no mother**, + rotation | **0.688** | **−6.86** | **11.87** | **0.807** | **0.899** | 1.22 |
| retune, **grey 40:60 at 0.30**, + rotation | 0.436 | −3.40 | 31.16 | 0.704 | 0.892 | 1.00 |
| retune, grey 40:60 at 0.30, no rotation | 0.434 | −3.37 | 30.63 | 0.720 | 0.753 | 1.17 |

`[verified — computed locally. The grey rows use the seven-paint list described in §11.]`

**Once contrast, key and chroma are set from evidence, the mother colour is doing their job a
second time and overshooting.** It contributes nothing to hue concentration that the rotation
does not deliver better (0.892 with it against 0.899 without), and it costs 0.25 of the L\*sd
ratio and 19 L\* at the dark end.

**Revised recommendation: remove `MotherColourTransform` from Tonalism (pick 1); repair
`MostNeutralPaintIndex` anyway (pick 2), because Abstract still calls it** — at
`motherFraction` 0.15 through `AbstractPaletteTransform.Transform`, and measurably: the same
stage at `motherFraction` 0.30 puts the rendered minimum L\* at **42.41**. Design B above is
the right shape for whoever keeps a mother colour; it is no longer urgent for this row.
`[inferred, from verified measurements]`

---

## 8. key +4.0 and contrast 0.55, audited

Eight photographs, six-paint fixture palette, rendered through the shipped `StylePipeline.Render`
at `RenderContext.DefaultMarkPixels` × Tonalism's 1.2. "L\*sd ratio" and "C\* ratio" are output ÷
source, per image, averaged.

| variant | **L\*sd ratio** | **ΔL\*mean** | **min L\* out** | **C\* ratio** | hue conc. | distinct | regions | % below mark² |
|---|---|---|---|---|---|---|---|---|
| identity remap, no mother | 0.880 | +1.43 | 10.34 | 0.965 | 0.777 | 775 | 105,875 | 40.43 |
| **shipped** | **0.419** | **+8.08** | **42.61** | **0.485** | 0.745 | 342 | 75,820 | 28.78 |
| shipped, mother 0 | 0.505 | +5.65 | 27.78 | 0.509 | 0.670 | 208 | 66,530 | 26.99 |
| shipped, mother 0.15 | 0.499 | +6.06 | 34.07 | 0.484 | 0.718 | 297 | 76,834 | 31.10 |
| shipped, mother 0.60 | 0.203 | +14.55 | 56.71 | 0.477 | 0.727 | 373 | 55,098 | 22.74 |
| key **0** | 0.357 | +5.20 | 42.60 | 0.481 | 0.732 | 351 | 68,799 | 26.19 |
| key **−6** | 0.268 | +2.04 | 42.52 | 0.481 | 0.696 | 326 | 58,002 | 23.42 |
| key **−12** | 0.178 | −0.53 | 42.41 | 0.478 | 0.678 | 266 | 48,863 | 21.30 |
| contrast **0.75** | 0.522 | +9.87 | 42.61 | 0.478 | 0.708 | 358 | 75,229 | 27.71 |
| contrast **0.95** | 0.625 | +11.65 | 42.49 | 0.474 | 0.704 | 360 | 74,910 | 27.65 |
| chroma **0.30** | 0.419 | +8.02 | 42.76 | 0.350 | 0.692 | 224 | 68,542 | 25.92 |
| chroma **0.65** | 0.417 | +8.09 | 42.35 | 0.664 | 0.783 | 535 | 85,998 | 32.17 |
| mother 0, key −6 | 0.508 | −4.54 | **18.17** | 0.498 | 0.707 | 241 | 64,166 | 26.86 |
| **mother 0, key −6, + `SmallRegionMerge`** | 0.496 | −5.49 | 18.71 | 0.485 | 0.704 | 132 | **1,705** | **0.83** |
| shipped + `SmallRegionMerge` | 0.411 | +7.23 | 42.78 | 0.465 | 0.752 | 151 | 1,490 | 0.75 |
| Bone Black mother 0.30 | 0.411 | +3.52 | 29.11 | 0.449 | 0.756 | 295 | 71,390 | 29.17 |
| Bone Black mother 0.15 | 0.482 | +5.19 | 27.21 | 0.490 | 0.702 | 261 | 70,112 | 28.65 |
| Ultramarine mother 0.15 | 0.435 | +3.99 | 26.12 | 0.547 | 0.671 | 202 | 60,877 | 26.24 |
| **retune: 0.75 / −8 / 0.85, no mother, + merge** | **0.666** | **−7.33** | **11.60** | **0.818** | 0.775 | 271 | **2,468** | **1.23** |
| retune + rotation s = 0.55 (per image) | 0.688 | −6.86 | 11.87 | 0.807 | **0.899** | 206 | 2,291 | 1.22 |
| retune + `AbstractPaletteTransform` n = 8 | 0.698 | −5.74 | 16.62 | 0.840 | 0.804 | **20** | 660 | **0.33** |
| retune + `AbstractPaletteTransform` n = 8 + rotation | 0.684 | −6.21 | 18.33 | 0.826 | 0.879 | 18 | 661 | 0.33 |
| *(corpus target)* | *0.683* | *−10.6* | *(p5 16.1)* | *0.822* | *0.90* | — | — | — |

`[verified — computed locally 2026-07-31]`

### key +4.0 is wrong by sign

Target, from §3: Tonalist L\*mean is **10.6 below** the photographs' and **14 below** for
Whistler's nocturnes alone. Shipped delivers **+8.08**. The gap between target and delivery is
therefore about **19 L\***, on the axis with this report's strongest statistical support.

Three separate mechanisms contribute, and they should not be confused:

- `key` = +4.0 contributes +4.0 directly.
- `contrast` = 0.55 pivots at L\* 50 while the photographs average 47.57, so compression adds
  a further **+1.09**.
- The white mother adds the rest: removing it drops ΔL\*mean from +8.08 to **+5.65**.

**Setting `key` to about −8 corrects the mean** (measured: −6 gives −4.54 with the mother off,
and the pivot term is a further +1.1). **It does not by itself correct the dark end**, because
of the two floors in §4. `[verified]`

### contrast 0.55 is the right sign and about a third too strong

Target L\*sd ratio: Tonalist 16.13 ÷ photograph 23.60 = **0.683**; p5–p95 range ratio 0.664; for
Whistler's nocturnes alone 0.536. Shipped delivers **0.419** — over-compressed by about a third
against the movement figure. The sweep says contrast **0.75** delivers 0.522 and **0.95**
delivers 0.625 (both with the mother on, which itself costs ~0.09 of the ratio). With the mother
off, contrast 0.55 already delivers 0.505. **Contrast in the 0.75–0.85 band with the mother
repaired lands near 0.68.** `[verified]`

Note the interaction the previous rounds' framing misses: **the mother colour is a contrast
control too.** Fraction 0.60 alone drives the L\*sd ratio to 0.203. The Post-Impressionism
round's framing — "the floor is the fragmentation control and the contrast knob is the
value-range control" — is right as far as it goes, but in *this* row a third stage is silently
doing the value-range job, which is why the contrast number looks defensible in isolation and
over-delivers in place.

### chroma 0.45 has no measured support and is not the worst thing in the row

Target C\* ratio: **0.822** for the movement, 0.488 for Whistler's nocturnes alone, 0.371 for
the Pictorialists. Shipped delivers **0.485**. So the shipped number is well calibrated **to
Whistler's nocturnes** and about 40% too aggressive for Tonalism as a whole — and §3 says the
statistic it targets does not separate the movement from Impressionism in the first place.

I would move it to **0.80–0.85** and let the key and hue-convergence changes carry the style,
**with one reservation I could not resolve**: the corpus C\*mean is measured on 110–160-year-old
varnished canvases, and if the varnish is adding chroma as well as yellow then 0.822 is an
overestimate. The Whistler nocturnes are the least varnish-affected group in the corpus
(b\*mean −1.01) and they say 0.488. **Both numbers cannot be right and I do not know which
is.** This is verification debt item 2. `[verified for the measurements; the resolution is
`[inferred]` and weak]`

---

## 9. Picks, ranked by payoff ÷ cost

### Pick 1 — Retune the row and delete its mother colour: **six lines, three of four targets**

- **Where:** `StyleRegistry.cs:51-64`. Replace `tonalismMotherColour` with
  `new KeepAllCandidates()`, drop its override, put `new SmallRegionMerge()` in the post-map
  array, and change contrast 0.55 → **0.75**, key +4.0 → **−8**, chroma 0.45 → **0.85**.
  **~6 lines changed, ~12 lines of comment, one golden regenerated.**
- **Measured, over eight photographs** (§8, last block):

  | | L\*sd ratio | ΔL\*mean | min L\* out | C\* ratio | % below mark² |
  |---|---|---|---|---|---|
  | corpus target | 0.683 | −10.6 | (p5 16.1) | 0.822 | — |
  | **this pick** | **0.666** | **−7.33** | **11.60** | **0.818** | **1.23** |
  | shipped | 0.419 | +8.08 | 42.61 | 0.485 | 28.78 |

- **On the empty post-map slot.** With the Post-Impressionism round's pick 2 applied in the
  working tree, Tonalism and Realism are the only two rows left without one.
  Adding `SmallRegionMerge` alone, without the retune, already takes the shipped row from 28.78%
  to **0.75%** below mark² and regions from 75,820 to 1,490, at a cost of 0.008 in the L\*sd
  ratio and 0.020 in the C\* ratio. **This measurement is against the repaired stage in the
  working tree**; against the committed version the Post-Impressionism round measured the
  postcondition failing on photographs, so the figure is worth what it says only if that repair
  lands. If the brushwork track picks the same item, charge it once.
- **Why all six at once and not one at a time.** They are not independent. The mother colour is
  silently a value-range control (fraction 0.60 alone drives the L\*sd ratio to 0.203) and a
  hard floor on the dark end, so with it in place the `key` parameter cannot reach the dark end
  at all — every key row in §8 sits at minimum L\* 42.4–42.6 regardless of key. And the
  fragmentation the retune would otherwise expose (the identity remap runs at 40.43%) is what
  `SmallRegionMerge` is for.
- **On `key −8` specifically:** it delivers −7.33, not −8, because contrast pivots at L\* 50
  while a photograph averages 47.57, adding back about +1.1. To hit the measured −10.6, key
  wants to be about **−11**. I recommend −8 rather than −11 for the same reason the
  Post-Impressionism round recommended contrast 1.0 rather than 0.85: the exact magnitude rests
  on a scan-versus-photograph tone comparison I do not trust to two figures, while the
  *direction* is unanimous across 34 works and two controls.
- **What the doc comment should say.** The current one explains only that the overrides exist so
  the style differs from Realism. Replace with the measured framing: **the key parameter is this
  style's primary control and its sign is load-bearing; the contrast parameter sets the value
  range; a candidate-set contraction is silently a third value control and must not be used as
  one.**
- **Verification:** pin as numeric properties on a real photograph — not on `Tests/Golden` —
  that the rendered L\*sd ratio is between 0.60 and 0.75, the C\* ratio between 0.75 and 0.90,
  the mean L\* strictly *below* the source's, and the fraction below mark² under 5%. All four
  are properties, not "does not throw". Then regenerate `Tests/Golden/Tonalism.png` and
  **look at it**.
- **Confidence:** **high** on the key sign and the mother removal; **high** that it is cheap;
  **medium** on chroma 0.85 (§8's varnish reservation, debt 2); **medium-high** on appearance —
  §9.5 looked at it on four photographs and it reads as a painting where the shipped row reads
  as a faded photocopy.

### Pick 2 — Repair `MostNeutralPaintIndex`, for the callers that remain

- **Where:** `MixtureBuilder.MostNeutralPaintIndex` (`MixtureBuilder.cs:135-162`) and
  `IsMoreNeutral` (`:184-188`). Score `chroma + w·|L* − 50|` with *w* around 0.05 rather than
  chroma alone with a tie-break that the doc comment already records as unreachable. **~6 lines
  changed, ~10 lines of comment, one test** — `IsMoreNeutral` exists precisely as the seam a
  test can drive with contrived numbers.
- **Why it still matters after pick 1.** `AbstractPaletteTransform.Transform` calls it, so
  Abstract carries the same defect at `motherFraction` 0.15, and any future style that wants a
  mother colour inherits it. Measured: `AbstractPaletteTransform` at `motherFraction` 0.30 puts
  the rendered minimum L\* at **42.41** and ΔL\*mean at **+4.48**, against 16.62 and −5.74 at
  `motherFraction` 0. The bug swamps everything else the stage does. `[verified]`
- **Known limitation:** it swaps a whitener for a blackener, because this library holds no
  neutral near L\* 50 — Titanium White at 98.2 and Bone Black at 11.2 are the only two. The
  correct fix is design B in §7.3 (a *mixture* mother, ~45 lines). Pick 2 is the cheap 80%.
- **Confidence:** **high** that the current behaviour is a defect; **high** on the cost;
  **medium** that the ranking weight *w* = 0.05 is the right number.

### Pick 3 — Give Tonalism `AbstractPaletteTransform` in slot 3 (try `colourCount` 12, not 8)

- **Where:** `StyleRegistry.cs:56` plus two `WithDefaults` entries. **1 line of structure**;
  the stage already exists and is already wired for `IImageAwareCandidateTransform`.
- **Evidence:** §6 makes the historical argument — Whistler's limitation was a small set of
  premixed tones, not a short pigment list — and this stage is that operation, including
  pinning the lightest and darkest candidates (`AbstractPaletteTransform.cs:81-85`), which is
  the only mechanism in the codebase that guarantees a nocturne keeps its dark end. Measured on
  pick 1's tone settings with `motherFraction` **0**: L\*sd **0.698**, C\* **0.840**, hue
  concentration **0.804**, minimum L\* 16.62, **20 distinct colours**, **0.33%** below mark².
  Better than pick 1 alone on every axis. `[verified]`
- **Two reasons it is third and not first.** It is gated on pick 2 — at the shipped
  `motherFraction` it reproduces the whitening defect exactly. And **Abstract already runs this
  stage at `colourCount` 8**, so adopting it would give two rows the same distinctive stage at
  the same setting, separated only by mark scale (1.2 vs 2.5), floor (2 vs 5) and the tone
  numbers. That is a product decision, not a measurement, and it should be made deliberately.
- **And it looks posterised at n = 8** (§9.5) — a screen print rather than a canvas, worst on
  the foggiest subject in the set. `colourCount` 12 gives 26 colours at L\*sd 0.685 and should
  be tried first; below 12 I would not ship it.
- **Confidence:** **high** on the numbers; **high** on the historical argument; **low, and
  lowered by looking**, on appearance at n = 8; **medium** on whether it should be Tonalism's
  stage rather than Abstract's alone.

### Pick 4 — A hue-convergence parameter on the Lab remap, targeting the image's own dominant hue

- **Where:** slot 2. Either two new parameters on `ToneAndChromaRemap` (`tintHue`,
  `tintStrength`) or a separate `ILabRemap`. The per-image default needs the source's dominant
  hue, which means either `IImageAwareCandidateTransform`-style access to the pixel buffer or a
  new field on `RenderContext`. **~50 lines** for the rotation plus **~25** for the
  dominant-hue pass.
- **Evidence:** §5.1. On pick 1's settings the rotation at strength 0.55 moves delivered hue
  concentration **0.775 → 0.899** against a corpus target of **0.90**, with the L\*sd ratio
  unchanged (0.666 → 0.688), the C\* ratio essentially unchanged (0.818 → 0.807) and
  fragmentation unchanged (1.23% → 1.22%). It is the only measured lever on the one statistic
  that separates Tonalism from Impressionism (t = +2.10) as well as from photographs
  (t = +2.62). `[verified]`
- **The target must be derived from the image, and the strength must be a slider.** A fixed 90°
  target at strength 0.85 delivers **0.668 against a control of 0.670** — nothing. Whistler's
  nine nocturnes span the hue circle with an across-work resultant of 0.25 and he named the
  harmony of each picture individually; Steichen printed one negative three ways and the three
  reproductions measure 158°, 288° and 97°. A single default hue is indefensible on the
  evidence; a default derived from the image is measured to work.
- **Why it is last despite closing the last target:** it is the only pick that is new code
  rather than a number or an existing stage, it is ~75 lines against 6, and picks 1 and 3
  already reach 0.775 and 0.804 on their own.
- **Looked at (§9.5):** convincing on warm subjects, and on a cool seascape it produces a
  magenta-brown **cast** rather than a harmony, because the per-image dominant hue of a picture
  split between warm rock and cool water belongs to neither half. Start the strength lower than
  0.55 and expect the hue to need to be user-overridable, not merely user-visible.
- **Confidence:** **high** that the target statistic is the right one; **high** that the
  per-image variant moves it; **medium** on appearance, and subject-dependent.

### 9.5 What the four picks look like — the one thing statistics cannot tell you

Four photographs rendered five ways (source, shipped, pick 1, pick 1 + pick 4, pick 3) and
**looked at**. This changed two of the confidence ratings above and should be repeated by
whoever implements any of it.

- **The shipped row looks like a faded photocopy, not a painting.** Every image comes out pale,
  flat and milky-lavender; the Corcovado sunset loses its sun and the foggy pond loses every
  dark. The whitening in §7 is not subtle in the picture — it reads as a grey scrim laid over
  the photograph, which is exactly what "blend 30% Titanium White into every mixture" is.
  `[verified — I looked]`
- **Pick 1 looks like a painting.** Silhouettes are solid, the colour is back, the value
  structure reads. On a warm sunset it is arguably a little brighter and more saturated than
  "Tonalism" suggests, which is the chroma-0.85-versus-0.49 question of §8 showing up visually.
- **Pick 4's rotation is not uniformly good.** On the two warm images it unifies convincingly.
  On a cool blue seascape it drags the sea toward magenta-brown and reads as a **colour cast**,
  not a harmony — the per-image dominant hue of a picture that is half warm rock and half cool
  water is a compromise that belongs to neither. **Lower the default strength, and expect this
  to need a per-image escape hatch.** Downgrading pick 4's appearance confidence from "unknown"
  to **"works on warm subjects, needs care on cool ones"**.
- **Pick 3 at `colourCount` 8 looks posterised.** 20 distinct colours across a photograph reads
  as a woodcut or a screen print, not as a Tonalist canvas — best on the simplest subject
  (Corcovado) and worst on the foggy pond, which is the most Tonalist subject in the set. Its
  numbers are the best in the report and its appearance is the worst. **This is why the
  measurement gate in the parent README's build order exists**, and pick 3 fails it at n = 8.
  Try 12 before trying 8. `[verified — I looked]`
- **A caution about all four.** Every statistic in this report is global — mean, SD, ratio over
  the whole frame. The visible defect the renders share is *local*: hard boundaries between
  large flat regions where a Tonalist canvas has none. The corpus already says so (local |ΔL\*|
  5.57 against 9.30, t = −2.38) and no pick here addresses it. **The edge treatment, not the
  palette, is what is left.** `[inferred, from verified numbers and one look]`

---

## 10. What not to build

The parent, Abstract, Fauvism and Post-Impressionism "what not to build" lists all still apply.
These are additional, and each was rejected after going looking for it.

- **A hard-coded warm tint, on the strength of the "brown gravy" reading.** §5: the golden bias
  is real for Inness, Blakelock, Wyant and Ranger (b\*mean +19.6 to +20.8, warm-hue share
  87–91%) and **absent from Whistler's nocturnes** (b\*mean −1.01, warm share 30%). Across the
  movement the hue resultant is only 0.66. It is also partly varnish — 110–160-year-old glazed
  canvases against a modern digital control. Expose the hue; do not choose it. `[verified]`
- **A fixed-hue tint of *any* value, even user-set, without a per-image default.** §5.1: aimed
  at 90°, strength 0.85, the delivered hue concentration is **0.668 against a control of
  0.670** — the ask is near-total and the picture does not move, because at Tonalism's delivered
  chroma the rotation is smaller than the spacing of near-neutral candidates. Aimed at the
  image's own hue the same stage reaches 0.800. **This is the same failure mode the
  Post-Impressionism round found on the per-hue chroma ceiling, in a different stage.**
  `[verified]`
- **Lowering chroma further, or treating mean chroma as the style's target statistic.** §3:
  Tonalist C\*mean 16.79 against Impressionist 17.31 (t = −0.21) and photographic 20.42
  (t = −1.18). Over 34 works there is no chroma separation to chase. The shipped 0.45 is already
  40% below the movement figure. `[verified]`
- **A "Tonalist palette" preset naming pigments — including report 02's *Sea and Rain* five.**
  §6: Whistler's twelve are mostly earths and historical pigments — lead white, Prussian blue,
  madder, yellow/red ochre, raw sienna, burnt sienna, raw umber, Venetian red, Indian red,
  vermilion — of which the user can select none, and report 02's proposed substitute list is
  two-fifths unavailable (Yellow Ochre is `ReflectanceDerived` and withheld). Same rejection,
  same reason, as the Fauvism round's viridian preset. `[verified against the manifest and
  ../fauvism/03-colour.md §1.2]`
- **Glaze simulation to reproduce Tonalist depth.** It is the technique every technical entry in
  §6 describes, and it is the parent README's "post-map, K-M layering" category: a different and
  larger invariant. Tonalism wants it more than any other row, which is precisely why it should
  not be smuggled in as a Tonalism feature. Defer it as its own decision. `[inferred]`
- **Using the Pictorialists to calibrate the numbers.** They are the extreme of every statistic
  here (L\*sd 10.49, C\*mean 7.57, ΔC\*/ΔL\* 0.23) and it is tempting to aim at them. Their
  chroma is a **toner**, not a palette: one negative, three prints, C\*mean 0.1 / 5.1 / 7.1.
  Calibrating a paint-mixing app against a monochrome print process would produce a style that
  no selection of acrylics is doing any work in. `[verified]`
- **Keeping a mother colour in Tonalism once the tone numbers are set from evidence — even the
  repaired one.** §7.4: on pick 1's settings a grey 40:60 mother at 0.30 moves the L\*sd ratio
  from 0.688 to 0.436, ΔL\*mean from −6.86 to −3.40, the C\* ratio from 0.807 to 0.704 and the
  minimum output L\* from 11.87 to 31.16 — every one of them away from the corpus target. It
  contributes nothing to hue concentration that the rotation does not deliver better (0.892
  against 0.899). The stage was doing contrast's and key's job badly; once they do it
  themselves, it is subtraction. `[verified]`
- **Treating Tonalism as the "purely pointwise, zero spatial component" style.** The parent
  README's build order calls it "track 2's most-achievable style… every property is a pointwise
  transform plus the existing blur. Zero spatial or semantic component." One of the four
  statistics that separate Tonalism is spatial — local |ΔL\*| 5.57 vs 9.30, t = −2.38 — no pick
  in §9 addresses it, the largest defect in the shipped rendered output is fragmentation
  (28.78%), which is spatial, and §9.5 says the remaining visible fault after all four picks is
  hard region boundaries, which is spatial too. **The premise that made Tonalism the first style
  built is wrong**, though the conclusion that it is the most achievable may still be right.
  `[verified]`

---

## 11. Method

Everything marked "computed locally" was produced on 2026-07-31 from a throwaway console project
in the session scratchpad, referencing `PaintTranslator.csproj` and named `PaintTranslator.Tests`
so the app's `InternalsVisibleTo` applies. **No file in the repository was modified** other than
this report; the probe lives outside the tree.

- **Stages are called, never transcribed.** `StylePipeline.Render`, `ToneAndChromaRemap.Map`,
  `NearestQuantiser.Map`, `MotherColourTransform.Transform`, `AbstractPaletteTransform`,
  `MixtureBuilder.Build` / `.BlendInto` / `.MostNeutralPaintIndex`, `EdgePreservingFloor`,
  `SmallRegionMerge`, `KubelkaMunk.Mix`, `SpectralRenderer.ToDisplayColor`,
  `PalettePhotoConverter.RgbToLab`, `PaintabilityMetrics.CountRegions` and
  `FractionInRegionsSmallerThan` are the shipped implementations.
- **The proposed hue-convergence stage is a wrapper `ILabRemap`** that calls the real
  `ToneAndChromaRemap.Map` and then rotates the returned a\*b\* about the origin, preserving
  L\* and C\* exactly. The shipped stage runs unmodified; only what happens after it changes.
  That is exactly the change under test.
- **The "grey mother" is a real mixture, not a synthetic colour.** `KubelkaMunk.Mix` combines
  absorption and scattering linearly by normalised share (`KubelkaMunk.cs:109-119`), so a
  `PigmentCoefficients` whose K and S are the share-weighted sum of white's and black's is
  arithmetically identical to those two paints at those shares. Blending fraction *f* of it is
  therefore blending 0.4*f* white and 0.6*f* black, and every resulting candidate is a genuine
  mixture of genuine paints. The grey has to live in the paint list for `BlendInto` to name it,
  which also lets `Build` enumerate it as a tube; the fraction-0 control row in §7.3 quantifies
  what that adds, and no comparison crosses paint lists.
- **Styles are constructed in the probe**, not mutated in `StyleRegistry`. Tonalism's shipped
  numbers are transcribed into a probe `Variant` and were checked line-by-line against
  `StyleRegistry.cs:51-64`.
- **Palette:** the six-paint fixture from `Tests/StyleTestFixtures.SixPaints()` — Titanium
  White, Hansa Yellow Opaque, C.P. Cadmium Red Light, Quinacridone Magenta, Ultramarine Blue,
  Bone Black; 3,007 candidates. The paint-index table in §7.1 is over all 19 of
  `PigmentLibrary.Selectable`.
- **Sources:** eight of the corpus photographs, loaded at 768 px longest edge and converted at
  `RenderContext.DefaultMarkPixels(w, h) × 1.2`. **No figure in this report is drawn from
  `Tests/Golden`**, which is a synthetic gradient and has produced a false conclusion in three
  consecutive rounds.
- **Corpus statistics** are whole-image at ~700 px longest edge after the mount trim described
  in §2, through the app's own `RgbToLab`. Welch's *t* with Satterthwaite degrees of freedom;
  no correction for multiple comparisons.
- **Working-tree state.** `Imaging/Styles/Stages/SmallRegionMerge.cs` and
  `Imaging/Styles/StyleRegistry.cs` carry uncommitted changes at the time of writing —
  respectively the Post-Impressionism round's repair of the merge postcondition and its picks 1
  and 2 applied to the Post-Impressionism row. **Tonalism's row is untouched by them**, so
  §7–§8's "shipped" figures are figures for the shipped row; but every `SmallRegionMerge` figure
  anywhere in this report is against the **repaired** stage.

---

## 12. Verification debt

Ranked by how much clearing each would change a decision.

1. **The visual pass in §9.5 is four photographs at 300 px, judged by one agent, and it already
   overturned pick 3.** That is a warning about how little of this report's evidence is about
   appearance, not a claim that the debt is cleared. Nobody has viewed a full-resolution
   conversion, nobody has compared one against a Tonalist painting side by side, and the
   posterisation verdict on pick 3 rests on four images. **Repeat it at full size, on more
   subjects, before shipping any of the four.**
2. **Whether the corpus C\* target of 0.822 survives varnish.** §8. The two available answers
   differ by 1.7× — 0.822 from the whole movement, 0.488 from Whistler's nocturnes, which are
   the least yellowed group in the set. Pick 1's chroma number rests entirely on choosing
   between them. A handful of colour-managed museum downloads of the same works, or of
   conserved-and-revarnished versus untouched canvases, would settle it. This is the same
   sourcing debt the Fauvism and Post-Impressionism rounds both recorded as their top item and
   nobody has cleared.
3. **The corpus itself.** 65 self-curated web reproductions with unknown colour management,
   n = 34/7/10/14, no correction for multiple comparisons, and a Tonalist group whose
   composition (9 Whistler, 7 Inness, 5 Blakelock, 5 Tryon, 4 Twachtman, 4 Wyant/Ranger) I chose.
   Every ratio in §8 is calibrated against it.
4. **Whether lowering the key interacts badly with the paint gamut's dark end.** Below about
   L\* 25 the six-paint candidate set thins (760 of 3,007) and its chroma collapses with it. A
   nocturne rendered at key −8 might band. I measured minimum L\* and distinct-colour counts but
   not banding, and the fragmentation metric does not detect it.
5. **Whether `AbstractPaletteTransform` at `colourCount` 8 should be Tonalism's slot 3 at all.**
   The colorimetry is measured (§8, pick 3) and it is the best of anything I tried. What is not
   settled is a product question: Abstract already runs the same stage at the same
   `colourCount`, and I have no measurement of how distinguishable the two rows would then be.
   §9.5 says n = 8 posterises; I did not render n = 12 and look at it, which is the version I
   would actually recommend.
6. **Whistler's "Red Rag" quotation is `[relayed]`** from search-index excerpts of the 1892
   reprint in *The Gentle Art of Making Enemies*, not from the 1878 *World* article or the
   reprint itself. It is load-bearing only for framing in §5.
7. **The Whistler palette description — twelve colours, premixed saucers, copal/mastic/turpentine
   medium — is `[relayed]`** from a single painter-facing article. The *pigment* lists in §6 are
   `[verified]` from the Glasgow catalogue's technical entries, which is the stronger source and
   supports the same conclusion, so the risk to §6's ruling is low.
8. **`Sea and Rain`'s four pigments** are `[relayed]` from English Wikipedia with no
   corroboration. It is quoted only as a range, not as a rule.
9. **No colorimetry of Tonalism exists in the literature.** I searched specifically for it and
   found none, which is the fourth consecutive round to report that absence for its own movement.
   The Hasler & Süsstrunk colourfulness metric the Fauvism round identified still has never been
   run across movements.
10. **The Pictorialist group is n = 7 and three of those seven are the same photograph.** It is
    used only for a directional argument in §3 and a rejection in §10.

### What was verified locally this session

- The 65-work corpus table, its per-sub-group breakdown and its dispersion (§1, §3), plus Welch
  *t* against two controls on six statistics.
- Four curation rejections found by inspecting a 71-image contact sheet, and two white-mount
  trims found automatically afterwards (§2).
- Masstone L\* and C\* for all 19 selectable paints, and the resulting behaviour of
  `MostNeutralPaintIndex` (§7.1).
- Candidate-set statistics — count, minimum L\*, mean L\*, mean C\*, maximum C\*, counts below
  L\* 25 and L\* 40, hue resultant — for 33 mother-colour configurations across two palettes:
  Titanium White and Bone Black at six fractions each, every single paint at 0.30, and four
  premixed white/black greys at five fractions each (§7.2, §7.3).
- **Thirty-eight rendered variants** over eight photographs through `StylePipeline.Render`, each
  with L\*sd ratio, ΔL\*mean, minimum output L\*, C\* ratio, C\*sd, hue resultant, distinct
  colours, region count and fraction below mark² (§5.1, §7.4, §8).
- The hue-convergence wrapper at four strengths against a fixed 90° target, a fixed 250° target
  and the per-image derived target, on both the shipped and the retuned tone settings (§5.1).
- `AbstractPaletteTransform` at `colourCount` 6, 8 and 12 with `motherFraction` 0 and 0.30
  (§8, §9 pick 3).
- Four photographs rendered five ways at 300 px and inspected by eye (§9.5). The sheet is in the
  session scratchpad as `visual.png`.

---

## 13. Corpus provenance

**Source:** Wikimedia Commons, 2026-07-31, resolved by exact `File:` title through
`commons.wikimedia.org/w/api.php` (`action=query&prop=imageinfo`), downloaded as
`iiurlwidth=760` thumbnails with `extmetadata` captured alongside. Titles were found through
`generator=categorymembers` over the artists' Commons categories and through `generator=search`,
then hand-picked; nothing was taken from a search-result ranking unexamined.

**Paintings — 51 kept.** Every file sits in the named artist's Commons category or carries the
artist in its filename.

| group | n | works |
|---|---|---|
| Whistler, nocturnes | 9 | *The Falling Rocket*; *Blue and Green*; *Blue and Silver* (Google Art Project); *Blue and Silver – Chelsea*; *Green and Gold* (MET DT257408); *Grey and Silver – Chelsea Embankment, Winter*; *Blue and Silver – Bognor*; *Grey and Gold – Westminster Bridge* (Burrell); *Trafalgar Square, Chelsea, Snow* |
| Whistler, symphonies *(excluded from aggregates)* | 2 | *Symphony in White No. 2*; *Symphony in White No. 3* |
| Inness | 7 | *Early Morning, Tarpon Springs*; *The Home of the Heron*; *The Coming Storm*; *Autumn Gold*; *Sunset on the Passaic*; *A Silver Morning* (AIC); *Evening at Medfield* (MET) |
| Blakelock | 5 | *Moonlight* (Brooklyn); *Moonlight* (Google Art Project); *Indian Encampment at Twilight*; *Moonlight* (Indianapolis, **mount-trimmed 7.3%**); *Afterglow* (MFA) |
| Tryon | 5 | *November Morning*; *Morning in September* (Dallas); *Twilight* (Dallas); *Moonrise* (Indianapolis, **mount-trimmed 13.7%**); *Autumn Day* |
| Twachtman | 4 | *Hemlock Pool*; *Arques-la-Bataille* (MET); *The White Bridge* (AIC); *October* |
| Wyant + Ranger | 4 | Wyant *Peaceful Valley* (NGA); Wyant *Moonlight and Frost* (Brooklyn); Ranger *Connecticut Woods* (SAAM); Ranger *Bradbury's Mill Pond no. 2* (SAAM) |
| Pictorialist | 7 | Steichen *Moonlight – The Pond* ×3 (two Google Art Project versions, one `ThePondMoonlight`); Käsebier *Blessed Art Thou among Women* (MET); Stieglitz *Spring Showers, New York* (Cleveland); Stieglitz *Spring Showers, the Coach* (MET); Clarence White *Edge of the Woods, Evening* |
| Impressionist control | 10 | Monet *Water Lilies 1906*; Monet *Rouen Cathedral, Facade (Sunset)*; Monet *Haystacks, end of Summer*; Monet *San Giorgio Maggiore*; Monet *Cliff Walk at Pourville*; Monet w1595; Pissarro *Boulevard Montmartre*; Pissarro 021; Sisley 042; Sisley *The Bridge at Moret 1888* |

**Rejected after visual inspection — 4.** Wyant *A Gray Day* (MFA) and Tryon *Evening Landscape*
(Princeton) and Twachtman *Landscape with River* (Harvard) are museum photographs **including the
frame and the gallery wall**. Blakelock *Brook by Moonlight* is photographed **with a colour
calibration target inside the frame**. The rejected files are retained in the scratchpad.

**Photographs — 14 kept**, all Commons featured pictures of landscapes, fog, forests or sunsets.
EXIF from the API's `commonmetadata`:

| file | Make | Model | DateTimeOriginal |
|---|---|---|---|
| Canada Geese and morning fog | Canon | EOS DIGITAL REBEL XTi | 2009-08-27 |
| A foggy winter morning | NIKON | D7100 | 2017-12-12 |
| Clifton Beach 4 | Canon | EOS 400D | 2009-10-03 |
| Corcovado sunset silhouette | Canon | PowerShot SX220 HS | 2011-10-24 |
| **Brighton West Pier, England** | *(absent)* | *(absent)* | 2007-10-06 |
| Beech and ferns in Gullmarsskogen | SONY | DSC-RX100 | 2017-06-09 |
| Bothe-Napa Valley State Park | OLYMPUS | E-PL6 | 2018-02-10 |
| Cliff Lake, Lassen Volcanic NP | Canon | EOS R3 | 2022-08-20 |
| 2013 Rainbow over Washfold | OLYMPUS | E-M5 | 2013-10-27 |
| 2015 Swaledale from Kisdon Hill | OLYMPUS | E-M1 | 2015-09-07 |
| 2014 Track on Fremington Edge | OLYMPUS | E-M1 | 2014-09-27 |
| 2019 Aquaculture in Chile | NIKON | D750 | 2019-03-16 |
| Birchwood Slavnoe 2012 G1 | Canon | EOS 550D | 2012-03-09 |
| Bilberry bush and moss, Gullmarsskogen | Panasonic | DMC-FZ1000 | 2017-08-14 |

**Brighton West Pier is the one exception to the Make-and-Model rule** and is kept because it
carries a 2007 capture date and is visibly a photograph. Panoramas and stitched 360° images were
excluded deliberately: they are multi-frame composites and their lightness statistics are not
those of a single exposure. **Every one of the 71 downloaded images was viewed on a contact
sheet before any statistic was computed**, and the four rejections above are what that pass
found — no metadata test would have caught any of them.

The eight photographs used for the render measurements in §5.1 and §8 are the first eight in
filename order: *Canada Geese and morning fog*, *A foggy winter morning*, *Clifton Beach 4*,
*Corcovado sunset silhouette*, *Brighton West Pier*, *Beech and ferns in Gullmarsskogen*,
*Bothe-Napa Valley State Park* and *Cliff Lake, Lassen Volcanic NP*. Their mean source L\* is
47.57 and mean source C\* is 20.4 — close enough to the 14-photograph group's 50.01 and 20.42
that the ratios in §8 can be read against §3's targets directly.
