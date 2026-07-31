# Backgrounds in Post-Impressionism

**Date:** 2026-07-30
**Track:** 4 of 4, Post-Impressionism.
**Question:** what should Post-Impressionism do about backgrounds, and what should the
pipeline do to produce it?

**Relationship to prior research.** [abstract/03-grounds-and-background.md](../abstract/03-grounds-and-background.md)
established the taxonomy this report starts from — *ground* (a layer, no position,
unifies multiplicatively, already built as `MotherColourTransform`), *field* (a region
read as background, positional, identified there as the gap), *negative space* (unworked
area, argued to be unnecessary because the ground is paint). That taxonomy is correct and
is not re-derived here. [fauvism/README.md](../fauvism/README.md) ruled that reserved
canvas is a *coverage* property with no conservation study quantifying its fraction, best
handled by changing `GroundFill`'s mask test rather than by a dedicated stage. This report
tests both of those recommendations against the shipped code on real photographs, and
**both fail the test** — see §7.

**Claim marking** (enforced across `docs/research/`):

- `[verified]` — I read a primary or reputable source directly in this session, or it is
  arithmetic I performed on data in this repository, or a measurement I made by calling
  the real code.
- `[relayed]` — a secondary source or a search summary asserts it and I could not confirm
  it against the primary.
- `[inferred]` — my own reasoning from stated premises.

---

## The answer, first

**Post-Impressionism should do almost nothing special about backgrounds, and the two
existing stages that look like the answer both make the picture worse.** The five results,
in descending confidence:

1. **The movement's own answer is "treat the background exactly like the subject", for
   three of its four founders.** Cézanne builds distance out of the same constructive
   patches as the foreground; Van Gogh's skies are the most heavily worked passages on the
   canvas; Seurat's fields carry the same stipple everywhere. Only Gauguin flattens, and
   he flattens by *raising* chroma and drawing a `cloison` around it. **A style row that
   differentiates the background is faithful to one of four painters.** §1.
2. **`GroundFill` is actively harmful and should not be added to Post-Impressionism.** It
   hard-codes the ground at L\* 58.0 (`GroundFill.cs:94`), so on seven real photographs it
   moved the field it repainted by **ΔE 23.4 – 58.7** — a white sky at L\* 98.2 became a
   mid grey, a black background at L\* 11.2 became the same mid grey, a cobalt blue-hour
   sky at C\* 70.7 became C\* 21.1 — and it changed the app's own paintability metric by
   **exactly 0.00 points on all seven**. `[verified — computed locally 2026-07-30]` §5.
3. **A mother colour is wrong for this style twice over.** The movement worked on light
   commercial primings, not toned grounds (§2), so there is little to blend; and
   `MixtureBuilder.MostNeutralPaintIndex()` returns **Titanium White** for every palette
   containing white, so the stage is a *whitening* operation. At Tonalism's 0.30 it raises
   the darkest achievable colour on a 7-paint palette from **L\* 11.0 to L\* 38.3** while
   moving mean chroma by only −7%. `[verified]` §3.
4. **The flat decorative background is not buildable, and the measurement is decisive.**
   Flattening any mask larger than one already-uniform region to a single candidate costs
   a mean **ΔE 9.1 – 38.7** across seven photographs, against a median candidate spacing of
   1.70 ΔE. A photograph's background is not one colour and forcing it to be one destroys
   it. The *cloison* — the drawn boundary — is the half of Gauguin's device that survives
   contact with a photograph, and even that overshoots badly today (§4.3). §4.
5. **The field exists only where the photograph already has one, and no pipeline setting
   creates it.** Sweeping floor strength 1→5 and mark scale 1.6→3.2 moved the largest
   border-connected region by at most 0.6 percentage points on any of seven photographs
   (elephant 2.4→3.0%, opera 22.3→22.1%, donkey 1.3→2.9%), while unpaintable share moved
   by up to 40 points. **Fragmentation is a pipeline property; the field is a property of
   the picture.** `[verified]` §5.4.

**What Post-Impressionism should actually get, in one line:** the `SmallRegionMerge` it
does not currently register. That is a two-line registry change and it cuts the median
unpaintable share on real photographs from **43.5% to 26.2%**, which is the precondition
for a viewer being able to see a background at all.

---

## Contents

1. [The boundary problem](#1-the-boundary-problem)
2. [Ground colour: what they actually painted on](#2-ground-colour-what-they-actually-painted-on)
3. [Should Post-Impressionism carry a mother colour?](#3-should-post-impressionism-carry-a-mother-colour)
4. [The flat decorative background](#4-the-flat-decorative-background)
5. [Should the background be treated differently at all?](#5-should-the-background-be-treated-differently-at-all)
6. [Aerial perspective and recession](#6-aerial-perspective-and-recession)
7. [Corrections to prior research](#7-corrections-to-prior-research)
8. [Three picks](#8-three-picks)
9. [What not to build](#9-what-not-to-build)
10. [Verification debt](#10-verification-debt)

**Method note.** Every pipeline measurement below was made by *calling* the real stages
through `StylePipeline.Render` from a throwaway console project named
`PaintTranslator.Tests` (so the app's `InternalsVisibleTo` grant applies), referencing
`PaintTranslator.csproj`. No stage was transcribed. The probe lived in the scratchpad and
is not in the repository. Sources are seven Wikimedia Commons photographs listed in §10.
The palette is seven `Selectable` paints — Titanium White, Hansa Yellow Opaque, C.P.
Cadmium Orange, Pyrrole Red, Cobalt Blue, Phthalo Green (Y.S.), Bone Black — giving
**4,888 candidates**. Gamut figures marked "19 selectable" use all of
`PigmentLibrary.Selectable`, giving **84,063 candidates**, and are computed from the
candidate set, never from `pigments.manifest.txt`.

---

## 1. The boundary problem

### 1.1 The umbrella is real and it is retrospective

Roger Fry coined "Post-Impressionism" in 1910 for the exhibition *Manet and the
Post-Impressionists* at the Grafton Galleries, London. Tate's definition is that it
"describes the changes in impressionism from about 1886, the date of last Impressionist
group show in Paris", and names four artists pursuing "distinct artistic directions":
Cézanne ("re-do Poussin from nature"), Seurat (divisionism), Gauguin (intense colour,
rejection of naturalism, imaginative subjects), Van Gogh (personal, emotionally expressive
colour and brushwork). `[verified]` — [Tate, Post-Impressionism](https://www.tate.org.uk/art/art-terms/p/post-impressionism).
The National Galleries of Scotland call it "never a specific movement with clearly defined
goals or members… a catch-all term". `[relayed]` — search summary of
[nationalgalleries.org](https://www.nationalgalleries.org/art-and-artists/glossary-terms/post-impressionism);
the page itself returned 403.

So the four incompatible background answers the brief names are not a strawman; they are
the movement's own structure.

### 1.2 The four answers, and what each one *is* as an operation

| Painter | What is behind the subject | The operation | Expressible in this pipeline? |
|---|---|---|---|
| **Cézanne** | The same constructive patches as the foreground, plus exposed white priming in distant passages | *No* background operation. A uniform mark everywhere, and reserve where distance is wanted | Partly — reserve is a coverage property with no stage |
| **Van Gogh** | An active sky, worked as hard as or harder than the ground | *No* background operation. Directional, high-relief marks everywhere | No — needs stroke orientation, not a background stage |
| **Seurat** | A uniformly stippled field, plus a painted border | *No* background operation. One texture across the whole surface | No — needs mark-scale broken colour |
| **Gauguin / Bernard / the Nabis** | A flat, saturated, non-descriptive plane bounded by a drawn contour | Two operations: flatten-and-saturate a region, then outline it | Flatten: no (§4). Outline: yes, badly (§4.3) |

**Three of the four are the same instruction: do nothing special.** `[inferred]` from the
rows above. That is the single most useful thing this section produces, because it removes
the pressure to build a background feature at all.

The evidence for each row:

- **Cézanne.** McCrone Associates examined ten Cézannes spanning 1877–1906. The primings
  "consist mainly of lead white, although it is the sole pigment in only two cases"; three
  have 5% chalk, two of those also 1–2% iron oxide yellow, five contain black from 1–2% up
  to 10%, and one 25%. `[verified]` — [mccrone.com](https://www.mccrone.com/mm/investigate-materials-techniques-cezanne/).
  On the background question the same source is explicit: in the 1883 *View of the Bay of
  Marseilles*, "great areas of white priming visible to further lighten and to increase
  the feeling of distance", and elsewhere "white priming is used as part of the design".
  `[verified]` The technique that unifies foreground and background is *passage* — "small,
  intersecting planes of patchlike brushwork" that "break down the contours" — with
  modulation replacing chiaroscuro. `[relayed]` — [Artsy, Passage](https://www.artsy.net/gene/passage)
  via search summary; the Art Institute of Chicago's digital publication on Cézanne
  returned 403 (§10).
- **Van Gogh.** The Van Gogh Museum records that *Daubigny's Garden* has a **pink base**
  that contrasted with the green of the garden, that "the ground layer is visible between
  the strokes of paint", and that the red pigment has faded so the pink now reads grey.
  `[relayed]` — search summary of [vangoghmuseum.nl/en/collection/s0104v1962](https://www.vangoghmuseum.nl/en/collection/s0104v1962);
  the page itself rendered as an empty SPA shell and I could not read the wording (§10).
  A 2021 open-access technical study of three landscapes found two-layer commercial
  grounds — calcium sulfate or calcium carbonate below, "primarily lead white" above,
  applied with a roller — and describes *Mountains at Saint-Rémy* (1889) as having "a thin
  and warm-toned lead white-based oil ground". `[verified]` —
  [npj Heritage Science s40494-021-00489-1](https://www.nature.com/articles/s40494-021-00489-1).
  On sky handling I found only secondary sources; treat "the sky is worked as hard as the
  ground" as `[relayed]`, though it is the standard reading and visible in any reproduction.
- **Seurat.** *A Sunday on La Grande Jatte* sits on a **lead white priming** (in one
  sample "lead white and some black"), with a painted border added c. 1888–89 on tacking
  margins folded out and primed with lead white underpaint, using vermilion, red lake,
  cadmium yellow, chrome yellow, cobalt blue and lead white. `[verified]` —
  [ColourLex](https://colourlex.com/project/georges-seurat-a-sunday-on-la-grande-jatte/),
  corroborated by search summaries of the Art Institute's *Seurat and the Making of La
  Grande Jatte*. Seurat's answer to "what is behind the subject" is *the same dots*, plus
  a literal painted frame — which is a border, not a background.
- **Gauguin and the Nabis.** Cloisonnism was named by Édouard Dujardin in 1888 for
  Bernard's and Anquetin's use of "clearly defined contours separating colored fields",
  after medieval cloisonné. *Vision after the Sermon* (1888) shows "emphasized two
  dimensional flat patterns, contouring and omission of shaded areas", after Japanese
  prints; "an undulating blue line encases everything". `[relayed]` —
  [TheArtStory, Cloisonnism and Synthetism](https://www.theartstory.org/movement/cloisonnism-and-synthetism/)
  and search summaries; the Smarthistory and National Galleries of Scotland pages both
  returned 403 (§10). Maurice Denis's 1890 formula in *Art et Critique* is the programmatic
  statement: "Remember that a picture, before being a battle horse, a female nude or some
  sort of anecdote, is essentially a flat surface covered with colours assembled in a
  certain order." `[relayed]` — [Wikiquote, Maurice Denis](https://en.wikiquote.org/wiki/Maurice_Denis)
  via search summary; I did not read the 1890 periodical.

### 1.3 Should the style row be split?

**No — and the reason is that the evidence for splitting does not exist, not that the
difference between the four painters is small.** `[inferred]`

The Abstract round split its style on measurement: Redies' abstract subset has
edge-orientation entropy SD 3.4× that of Western oils, and Graham & Field put geometric
and gestural abstraction on opposite sides of representational art. Nothing equivalent
exists here. **I searched for a quantitative study separating Cézanne, Gauguin, Van Gogh
and Seurat by amplitude-spectrum slope, edge-orientation entropy or any other image
statistic and found none.** `[verified]` that my searches did not surface one; this is not
proof one does not exist. The located whole-corpus studies (Graham & Field; Sigaki, Perc &
Ribeiro; the PLOS One 1/f² survey) treat art at genre or movement granularity, not artist.

So a split would have to be justified by the *device*, not by a measured signature. On
device the honest split is one-against-three: Gauguin flattens, the other three do not.
And §4 shows the Gauguin branch is the one this app cannot build. **A "Cloisonnism" row is
the split worth having eventually, and it is blocked on a segmenter, not on this report.**

**Ruling:** one row, targeting the shared behaviour — a uniform, unified surface with the
background handled like everything else. That is what the shipped row already implies by
having no candidate transform and no post-map stage, so this is a ruling in favour of the
status quo on the background axis specifically.

---

## 2. Ground colour: what they actually painted on

### 2.1 The period answer is "light, commercially primed, faintly tinted"

Every technical source located gives the same picture, and it is *not* the mid-value toned
ground the craft literature recommends today (Abstract report 03 §2.1: L\* 50–65,
C\*ab < 25):

| Painter | Ground | Source |
|---|---|---|
| Cézanne | Commercial single-layer priming, mainly lead white; 5% chalk in three of ten; 1–2% iron oxide yellow in two; black 1–25% in five, "grayed in tone" | `[verified]` McCrone |
| Van Gogh (Antwerp/Paris) | Commercial light grounds: lead white, chalk, gypsum, barium sulphate, pipe clay, "small amounts of tinting pigments such as carbon black" | `[relayed]` search summary of the Tasset et L'Hôte ground study on academia.edu (§10) |
| Van Gogh (Arles/Saint-Rémy) | Two-layer commercial grounds, upper layer "primarily lead white"; one described as "thin and warm-toned"; *Daubigny's Garden* on a **pink** base, visible between strokes | `[verified]` npj Heritage Science; `[relayed]` Van Gogh Museum |
| Seurat | Lead white priming, in one sample "lead white and some black" | `[verified]` ColourLex |
| Gauguin | Rough, thick, loosely woven absorbent canvas "lightly glued with animal glue" | `[relayed]` search summary of *Paul Gauguin, "Tahitian Pastorals": Study of Painting Materials* (§10) |

**The shared property is near-white with a trace of something** — a few percent of black,
iron oxide yellow, or a fugitive red. Munsell value 9-ish, not value 6. `[inferred]` from
the table.

That matters because it inverts the Abstract round's ground target. Abstract report 03
derived L\* 50–65 at C\*ab < 25 from Gamblin's and Will Kemp's modern teaching
recommendations. Those are *modern studio practice*. The 1880s Post-Impressionist ground
is a manufactured lead-white priming, and the one deliberate deviation found — Van Gogh's
pink — is a light tint, not a mid tone.

### 2.2 The reachability result, and it is the opposite way round from the Abstract round's

Computed over the real candidate sets, plain CIELAB distance, nearest candidate:
`[verified — computed locally 2026-07-30]`

| Target | 19 selectable (84,063) | 7-paint (4,888) |
|---|---|---|
| **warm lead-white ground L\* 93, C\* 5, h 80°** | **ΔE 6.48** → L\* 90.1, a\* −0.5, b\* −0.7 (neutral) | ΔE 6.48 |
| **Van Gogh pink ground L\* 82, C\* 14, h 20°** | **ΔE 4.92** | ΔE 4.92 |
| greyed lead white L\* 88, C\* 2 | ΔE 1.29 | ΔE 1.29 |
| neutral grey N6 L\* 61.7, C\* 0 | ΔE 1.71 | ΔE 3.59 |
| warm/violet grey L\* 58, C\* 8 | ΔE 1.99 | ΔE 2.89 |
| raw umber + white L\* 60, C\* 12 | ΔE 3.53 | ΔE 4.46 |
| yellow ochre tint L\* 68, C\* 32 | ΔE 5.40 | ΔE 5.40 |
| ecru `#C2B280` | ΔE 0.87 | ΔE 7.11 |
| burnt sienna tint L\* 55, C\* 28 | ΔE 2.45 | ΔE 6.56 |
| Gauguin flat vermilion L\* 50, C\* 70 | **ΔE 1.28** | ΔE 2.27 |
| Gauguin flat pink L\* 70, C\* 45 | ΔE 2.27 | ΔE 6.02 |
| Gauguin flat green L\* 45, C\* 40 | ΔE 1.48 | ΔE 6.42 |

**The modern mid-value grounds are all comfortably reachable. The period's own near-white
tinted grounds are the worst-served targets in the table.** `[verified]`

The cause is sampling density, not the gamut boundary. Above L\* 90 the 84,063-candidate
set holds only **190 candidates**, and above L\* 95 only **31** — while the maximum chroma
available at L\* ≥ 90 is 92.0, because Bismuth Vanadate Yellow's masstone is itself
L\* 91.7, C\* 91.3. So the light end is not chroma-limited; it is *empty between* white and
the light yellows. A mixing line sampled at 63 interior points steps in shares of 1/64,
and one sixty-fourth of Hansa Yellow in Titanium White is already well past b\* 5.
`[verified]` for the counts and ceilings; `[inferred]` for the mechanism.

**Consequence beyond this report:** very light, very slightly tinted colours are the
sparsest region of the sampled achievable gamut. Highlights, near-white skies and any
"white plus a trace" ground quantise at roughly **ΔE 6.5**, about 4× the README's median
candidate spacing of 1.70 ΔE. That is worth knowing wherever pale colours matter.

### 2.3 Gauguin's flat planes *are* reachable

The one positive result in the table: a saturated vermilion plane at L\* 50, C\* 70 is
ΔE 1.28 from an achievable mixture on the full palette, and ΔE 2.27 on a seven-paint one.
Flat saturated pink and flat saturated green are equally close. `[verified]` This
independently reproduces the Fauvism round's correction that the warm arc is not
chroma-limited and extends it to green, and it means **the obstacle to a Gauguin
background is not the paint. It is the photograph.** §4.

---

## 3. Should Post-Impressionism carry a mother colour?

**No.** Three independent reasons, one art-historical and two measured.

### 3.1 `MostNeutralPaintIndex()` returns Titanium White

`MixtureBuilder.MostNeutralPaintIndex()` (`MixtureBuilder.cs:135–162`) renders each paint's
masstone through the real kernel and picks the least chromatic, tie-broken toward L\* 50.
Called for real: `[verified — computed locally 2026-07-30]`

| Palette | Returns |
|---|---|
| 19 selectable | **[0] Titanium White**, L\* 98.17, C\* 0.62 |
| 6-paint test fixture | **[0] Titanium White** |
| 7-paint Post-Impressionist | **[0] Titanium White** |
| 18 selectable with white removed | [17] Bone Black, L\* 11.23, C\* 1.53 |

Titanium White's masstone chroma is 0.62; Bone Black's is 1.53. White wins on chroma
before the lightness tie-break is ever consulted, because the tie-break only fires on an
*exact* chroma tie — and the doc comment for `IsMoreNeutral` records that no two selectable
paints share one.

So the stage's own doc comment — "its least chromatic member is the one that greys
everything it touches toward a common note rather than tinting it toward some other hue" —
describes a paint the stage does not choose. **For any palette containing white,
`MotherColourTransform` blends white.** `[verified]`

### 3.2 What that actually does to the gamut

`MotherColourTransform.Transform` called at each fraction, then `Build()`:
`[verified — computed locally 2026-07-30]`

| 7-paint palette | n | mean L\* | **min L\*** | mean C\* | max C\* |
|---|---|---|---|---|---|
| f = 0.00 | 4,888 | 41.19 | **11.00** | 35.69 | 88.16 |
| f = 0.15 (Abstract) | 4,948 | 50.88 | **30.11** | 34.63 | 85.06 |
| f = 0.30 (Tonalism) | 4,952 | 57.09 | **38.32** | 33.21 | 85.08 |

| 19 selectable | n | mean L\* | **min L\*** | mean C\* | max C\* |
|---|---|---|---|---|---|
| f = 0.00 | 84,063 | 32.71 | **6.43** | 33.92 | 92.86 |
| f = 0.15 | 95,312 | 41.45 | **19.26** | 33.73 | 88.65 |
| f = 0.30 | 97,443 | 48.09 | **24.72** | 33.23 | 85.08 |

**Mean chroma falls by 7% across the whole range. The darkest achievable colour rises by
27 L\* points.** The stage is a value operation wearing a chroma operation's name. On real
photographs the same thing shows up as a mean lightness lift of +2.0 to +8.6 L\* at
f = 0.15 and +4.2 to +13.5 at f = 0.30, with mean chroma falling 0.4 to 6.1. `[verified]`

Whatever the merits for Tonalism — where losing the dark end is arguably the point — this
is not what the technique is, and it is definitely not a Post-Impressionist ground.

### 3.3 A *chosen* warm paint is the wrong shape of fix

The Abstract round's build item 7 was "mother colour from a chosen paint, ~40 lines". I
tested it by calling `MixtureBuilder.BlendInto` directly with each palette index at
fraction 0.20, and by rendering the seven photographs with C.P. Cadmium Orange as the blend
at 0.15. `[verified — computed locally 2026-07-30]`

- On the 7-paint palette, blending Cadmium Orange at 0.20 moves mean L\* 41.19 → 40.89 and
  mean C\* 35.69 → 34.76. Gentle in aggregate.
- On real photographs at 0.15 it is not gentle at all: mean ΔE from the unblended render
  ranges **4.04 to 19.98**, and on blue-dominant images it strips chroma —
  **ΔC\* −16.89** on the touareg (blue robes, blue night) and **−15.52** on the opera at
  blue hour, against **+7.38** on the daisy.

That is the correct physical behaviour of a warm ground over a cool subject, and it is far
too strong to be a ground. The reason is structural: **`BlendInto` takes one paint index**
(`MixtureBuilder.cs:86`), and no `Selectable` paint is a low-chroma warm — the closest is
Cadmium Orange at C\* 88.0. Every earth colour in the library is `ReflectanceDerived` and
withheld, exactly as the Abstract round found. A period-correct ground is *lead white plus
1–2% iron oxide yellow*: a mixture, which the API cannot name.

Extending `BlendInto` to take a mixture is about 35 lines and physically sound. **I am not
recommending it**, because §2.2 shows the ground it would produce quantises at ΔE 6.5 —
four times the median candidate spacing — so the feature would deliver a ground the user
did not ask for.

### 3.4 Ruling

**Post-Impressionism's `MotherColourTransform` fraction should stay at zero, i.e. the style
should keep `KeepAllCandidates`.** `[inferred]` The movement's grounds are near-white; the
stage cannot express near-white-plus-a-trace; and what it *would* do is lighten the palette
and destroy the dark end. Tonalism at 0.30 and Abstract at 0.15 are separate questions this
report does not settle, but §3.1 and §3.2 apply to both and someone should look at them.

---

## 4. The flat decorative background

This is the single most recognisable Post-Impressionist background device and the honest
answer is that the app cannot produce it. The negative result is worth more than a
half-working feature.

### 4.1 The measurement

On each photograph I rendered Post-Impressionism as shipped plus the real
`SmallRegionMerge`, labelled connected regions on the output, and scored three candidate
field masks by coverage and by the mean ΔE that flattening the mask to a single achievable
candidate would cost. `[verified — computed locally 2026-07-30]`

| Photograph | largest border region (what `GroundFill` uses) | all border regions ≥ 4·mark² | all regions ≥ 4·mark² |
|---|---|---|---|
| elephant | 2.8%, ΔE 0.00 | 20.0%, **ΔE 27.95** | 58.8%, ΔE 30.37 |
| swaledale | 6.8%, ΔE 0.00 | 23.8%, **ΔE 26.25** | 60.8%, ΔE 32.67 |
| yangshuo | 1.2%, ΔE 0.00 | 9.4%, **ΔE 21.69** | 60.2%, ΔE 24.83 |
| touareg | 8.6%, ΔE 0.00 | 72.3%, **ΔE 15.26** | 88.1%, ΔE 19.81 |
| daisy | 3.6%, ΔE 0.00 | 28.9%, **ΔE 9.08** | 88.1%, ΔE 37.06 |
| opera | 22.2%, ΔE 0.00 | 25.8%, **ΔE 17.34** | 46.4%, ΔE 38.72 |
| donkey | 1.9%, ΔE 0.00 | 8.6%, **ΔE 25.92** | 41.1%, ΔE 26.36 |

A fixed geometric band does no better: flattening the top 30% of each image to one
candidate costs mean **ΔE 8.44 – 33.14** (median 25.02), against a within-band Lab spread
of 16.6 – 34.6. `[verified]`

**Read the ΔE 0.00 column carefully.** The largest border-connected region is already one
candidate index, so filling it with its own colour is free — and that is precisely why it
is not a decorative plane. The moment the mask covers anything a viewer would call "the
background", the flattening cost jumps to 9–39 ΔE. The README records median candidate
nearest-neighbour spacing at 1.70 ΔE; the cheapest honest flat plane is **5× that** and the
typical one is **15×**.

### 4.2 Why, stated as a principle

A Gauguin background is a colour decision made *against* the motif — the red field in
*Vision after the Sermon* is not the colour of anything in Brittany. A photo converter's
entire contract is that the output is the nearest achievable colour to what the camera saw.
**Flat decorative colour requires abandoning the source, and this app is a source-fidelity
machine.** `[inferred]` The only way to get it honestly is to ask the user for the colour
and the region, which is a paint program, not a converter.

Note the asymmetry with §2.3: the *paint* for a Gauguin plane is available at ΔE 1.28. It
is the *region* and the *choice* that are missing, and neither is a colour problem.

### 4.3 The cloison is buildable — and today it overshoots

`ContourLines` already exists (built in the Fauvism round) and is the other half of
Gauguin's device. I ran it on Post-Impressionism after `SmallRegionMerge`, real stages:
`[verified — computed locally 2026-07-30]`

| Photograph | line coverage | naive unpaintable |
|---|---|---|
| elephant | 23.9% | 11.8% (vs 30.3% without lines) |
| swaledale | 23.9% | 10.0% (vs 26.2%) |
| yangshuo | 11.9% | 14.0% (vs 18.6%) |
| touareg | 11.3% | 3.3% (vs 7.8%) |
| daisy | 4.1% | 1.8% (vs 4.3%) |
| opera | 39.3% | 10.8% (vs 41.7%) |
| donkey | **48.3%** | 9.4% (vs 46.5%) |

The line is 3 px wide at these mark sizes and its own candidate is L\* 32.6, a\* 3.0,
b\* −12.4. On the donkey it becomes **half the picture**. And the apparent paintability
improvement is exactly the trap the Fauvism round named: the lines connect into one giant
region and flatter `CountRegions`, so the naive number must not be read as a win.

**`ContourLines` is not ready for Post-Impressionism.** Its boundary test is a fixed
ΔE 12 between neighbouring indices, and a Post-Impressionism render of a real photograph
holds 503–1,790 distinct colours, so almost every pixel is near a qualifying boundary. It
needs a much flatter input — a segmenter, or an area opening that actually opens — before
the outline reads as a `cloison` rather than as soot.

---

## 5. Should the background be treated differently at all?

### 5.1 `GroundFill`, measured

The stage identifies the largest border-touching connected region with area ≥ 4·mark² and
replaces it with `candidates.FindNearest(58.0, fieldA × s, fieldB × s)` where
`s = min(C×0.35, 25)/C` (`GroundFill.cs:26, 91–98`). Called for real on the seven
photographs, after `SmallRegionMerge`: `[verified — computed locally 2026-07-30]`

| Photograph | coverage | field was | became | **moved** | mean filled row |
|---|---|---|---|---|---|
| elephant | 2.8% | L\* 85.4 C\* 17.5 | L\* 57.9 C\* 5.9 | **ΔE 29.9** | 0.32 |
| swaledale | 6.8% | L\* 90.1 C\* 0.9 | L\* 57.5 C\* 3.1 | **ΔE 32.8** | 0.16 |
| yangshuo | 1.2% | L\* 98.2 C\* 0.6 | L\* 57.3 C\* 2.8 | **ΔE 41.0** | 0.04 |
| touareg | 8.6% | L\* 43.4 C\* 29.7 | L\* 59.9 C\* 13.7 | **ΔE 23.4** | 0.19 |
| daisy | 3.6% | L\* 11.2 C\* 1.5 | L\* 57.5 C\* 3.1 | **ΔE 46.3** | 0.91 |
| opera | 22.2% | L\* 27.6 C\* 70.7 | L\* 58.1 C\* 21.1 | **ΔE 58.7** | 0.13 |
| donkey | 1.9% | L\* 85.3 C\* 35.6 | L\* 58.1 C\* 14.3 | **ΔE 35.2** | 0.59 |

Three separate defects, all measured:

1. **The lightness is a constant.** Every field lands at L\* 57–60 regardless of what it
   was. A white sky at L\* 98.2 goes to L\* 57.3; a black background at L\* 11.2 goes to
   L\* 57.5. The Abstract round *recommended* `L*_ground = lerp(median L* of the image, 58,
   groundKey)` with a user key; what shipped is the endpoint of that lerp with the key
   welded to 1. `[verified]` against `GroundFill.cs:94` and
   [abstract/03-grounds-and-background.md §5.3C](../abstract/03-grounds-and-background.md).
2. **It does nothing for paintability.** Running Post-Impressionism with `GroundFill` and
   without gives **identical** `FractionInRegionsSmallerThan(mark²)` on all seven
   photographs, to the printed precision — 46.3/41.7/43.5/13.6/10.5/56.3/64.2%, unchanged.
   `[verified]` It repaints one region that was already one region.
3. **In Abstract it runs before `SmallRegionMerge`** (`StyleRegistry.cs:131`), so it picks
   its field out of the *unmerged*, more fragmented buffer, which can only make the chosen
   region smaller. `[verified]` against the registry.

The one thing it does right: **the region it selects is in the upper third of the image on
5 of 7 photographs** (mean filled row 0.04–0.32), i.e. the largest border-connected region
is usually the sky. Border-connectedness is a cheap and effective proxy for "background"
where a background exists. `[verified]`

### 5.2 What criterion would Post-Impressionism use?

From §1, the movement's criterion is **distance, not background identity** — Cézanne loosens and
lightens distance, Van Gogh does not differentiate at all. Distance is not computable from
the mapped index buffer. Border-connectedness *is*, and it correlates with sky, which
correlates with distance in landscapes and with nothing at all in the other four
photographs.

So: **the only background criterion computable from the index buffer alone is "large and
border-connected", it finds the sky about 5 times in 7, and Post-Impressionism has no
warrant to do anything to the sky once it has found it.** `[inferred]` from §1 and §5.1.

### 5.3 The prior research's user-click concession still stands

The parent README rejects automatic focal-point detection as load-bearing (image-independent
centre bias beats image salience on paintings) while accepting a user click. Nothing here
changes that. What this report adds is that even *given* a perfect background mask,
Post-Impressionism does not want to do anything with it except in the Gauguin branch, which
§4 rules out. **The user click is not the bottleneck. The absence of a wanted operation
is.** `[inferred]`

### 5.4 The field is a property of the photograph, not of the settings

Sweeping floor strength and mark scale, real stages, with `SmallRegionMerge`:
`[verified — computed locally 2026-07-30]`

| Photograph | largest border region at (strength, scale) = (1, 1.6) / (3, 1.6) / (5, 1.6) / (3, 3.2) / (5, 3.2) |
|---|---|
| elephant | 2.4% / 2.8% / 2.9% / 2.4% / 3.0% |
| swaledale | 6.8% / 6.8% / 7.1% / 7.6% / 7.8% |
| yangshuo | 1.2% / 1.2% / 1.2% / 1.5% / 1.9% |
| touareg | 8.0% / 8.6% / 8.6% / 8.7% / 8.7% |
| daisy | 3.9% / 3.6% / 3.3% / 2.7% / 2.5% |
| opera | 22.3% / 22.2% / 22.1% / 22.2% / 22.2% |
| donkey | 1.3% / 1.9% / 2.3% / 2.5% / 2.9% |

Over the same sweep, unpaintable share moves by up to 40 points (opera 61.4% → 30.3%;
yangshuo 51.7% → 6.0%). **Everything the pipeline controls moves fragmentation. Nothing it
controls moves the field.** Two of the seven photographs have a field worth the name
(opera 22%, touareg 9%); five do not, and no setting creates one.

---

## 6. Aerial perspective and recession

**Reject as a feature, and the reason is new.** The prior rejections stand — neural
monocular depth is out (ONNX plus a large model for an effect a two-handle gradient
approximates), and Cutting & Vishton 1995 puts aerial perspective 5th of 5 pictorial cues,
effective only beyond 30 m. This report adds two measurements.

**First: the photograph already has it.** Top 25% versus bottom 25% band of each source,
before any conversion: `[verified — computed locally 2026-07-30]`

| Photograph | ΔL\* (top − bottom) | ΔC\* | ΔL\*sd |
|---|---|---|---|
| swaledale (landscape) | **+51.23** | **−18.05** | −12.94 |
| yangshuo (landscape) | **+40.86** | −1.64 | +8.93 |
| elephant | +24.23 | −3.22 | +1.60 |
| donkey | +3.08 | −12.16 | −7.71 |
| daisy | +5.93 | +2.41 | +2.70 |
| opera | −1.17 | **+57.45** | −5.03 |
| touareg | **−7.76** | +12.79 | −19.27 |

On the two genuine landscapes the source already carries aerial perspective in the textbook
direction — lighter, less chromatic, less contrasty with distance. **Adding it would be
double-counting.** On three of seven the vertical gradient points the wrong way, and the
app cannot tell which case it is in. A two-handle vertical gradient is therefore a
landscape-only tool that will damage portraits and night scenes.

**Second: the period's own mechanism is not a gradient.** Cézanne creates distance by
leaving priming exposed — "great areas of white priming visible to further lighten and to
increase the feeling of distance" `[verified]`, McCrone. That is a *coverage* operation,
which is the same axis the Fauvism round identified for reserved canvas: orthogonal to both
colour and space, with no conservation study quantifying its fraction. This app writes a
candidate index to every opaque pixel (`StylePipeline.cs:160–168`), so it cannot leave
anything exposed, and the Abstract round already argued convincingly that it should not try.

**Ruling:** no aerial-perspective stage for Post-Impressionism. If a landscape depth
control is ever built, it belongs behind an explicit user gesture (a horizon handle), in
slot 1, and it should exaggerate what the photograph already has rather than synthesise it.

---

## 7. Corrections to prior research

Five, ranked by how much they change a decision. All are `[verified]` by calling the
shipped code.

**1. `MostNeutralPaintIndex()` returns Titanium White for every realistic palette, so
`MotherColourTransform` whitens rather than greys.** The Abstract round's phrasing —
"`MostNeutralPaintIndex()` forbids the warm earth ground" — is true and understates the
problem by a lot. The stage cannot express *any* mid-value ground; it can only lighten. At
Tonalism's 0.30 it raises the palette's darkest achievable colour by 27 L\* points while
moving mean chroma 7%. §3.1, §3.2. **This affects Tonalism and Abstract today**, not just
the styles this report is about.

**2. `GroundFill` does not implement the rule the Abstract round recommended.** The
recommendation was a lerp between the image's own median L\* and 58 under a user key; the
code writes 58 unconditionally, and the field it repaints moves by ΔE 23.4–58.7. §5.1.

**3. The Fauvism round's "reuse `GroundFill` with the mask test changed to low interior
gradient **and** high mapped L\*" cannot be done as described.** `GroundFill` has no
interior-gradient test to change — its mask is "largest border-touching region ≥ 4·mark²",
full stop — and its fill colour is pinned at L\* 58, which is the opposite of preserving a
high-L\* reserve. Reserved canvas via `GroundFill` needs the *colour rule* rewritten, not
the mask test. §5.1.

**4. `SmallRegionMerge` is not an area opening and does not satisfy the Fauvism round's
hard postcondition.** Track 2 there proposed that after an area opening at `MarkPixels²`,
`FractionInRegionsSmallerThan(MarkPixels²)` must be **exactly zero**, and called it "the
most valuable test available anywhere in this work". The shipped stage leaves **4.3% –
46.5%** on seven real photographs. It merges each undersized region into its largest
neighbour in a single pass over stale labels, so a merge can leave the target still
undersized and can create new small regions it never revisits. The postcondition is right;
the stage does not meet it. §5, §8.

**5. The synthetic-fixture warning generalises beyond `Tests/Golden`.** The Fauvism round
established that conclusions drawn from the golden gradient are unsafe. The *noisy* fixture
that `StyleBehaviourTests.EveryRegisteredStyleIsPaintable` uses is unsafe too: it gives
Post-Impressionism **1.23%** unpaintable, against **10.5% – 64.2%** (median 43.5%) on real
photographs, and the recorded ceiling for the style is 1.3%. Part of that gap is mark size
— the 256×256 fixture yields mark² = 10 against 24–93 on a 960-px photograph — but a
roughly 4× difference in the threshold does not explain a ~35× difference in the result.
**No paintability conclusion should be drawn from a synthetic source of any kind.**
`[verified]` against `Tests/StyleBehaviourTests.cs:468–505`.

**Where I disagree with the Abstract round on substance, not implementation:** its report
03 concluded that the field is "the actual gap" and that `GroundFill` is "the only thing on
this list that gives an abstract conversion a background a viewer can see". For
Post-Impressionism that is wrong on both halves — the movement does not want a field
(§1), and the stage as built cannot deliver one (§5.1, §5.4). I have not tested whether it
is wrong for Abstract; the §5.4 result that no setting moves the largest border region
suggests it may be, and someone should check before building on it.

---

## 8. Three picks

Ranked. Slot numbers refer to the five-slot pipeline.

### Pick 1 — register `SmallRegionMerge` in Post-Impressionism's slot 5

**Slot 5. ~2 lines in `StyleRegistry.cs`, plus one regenerated golden and one updated test
ceiling.**

Post-Impressionism is the only style other than Realism and Tonalism with an empty slot 5,
and it has the second-largest mark scale in the registry. Measured on seven real
photographs, adding the existing stage: `[verified]`

| Photograph | unpaintable, shipped | + `SmallRegionMerge` |
|---|---|---|
| elephant | 46.3% | 30.3% |
| swaledale | 41.7% | 26.2% |
| yangshuo | 43.5% | 18.6% |
| touareg | 13.6% | 7.8% |
| daisy | 10.5% | 4.3% |
| opera | 56.3% | 41.7% |
| donkey | 64.2% | 46.5% |
| **median** | **43.5%** | **26.2%** |

Region counts fall 40–60% (elephant 154,096 → 82,025; yangshuo 64,270 → 28,070). This is
background work because it is the precondition for one: at 154,096 regions there is no
figure and no ground, only speckle. It costs nothing to try — the stage is written, tested
and already shipping in Fauvism and Abstract.

Two caveats to record with it. It does not reach zero (correction 4), and it barely moves
the largest border-connected region (§5.4) — it makes the picture paintable, not
compositional.

### Pick 2 — repair `GroundFill`

**Slot 5. ~25 lines in `GroundFill.cs`, plus one new `StyleParameter`.**

Two changes, both small:

1. **Derive the ground lightness instead of pinning it.** Replace the literal `58.0` at
   `GroundFill.cs:94` with `lerp(fieldL, 58.0, key)` where `key` is a new parameter
   defaulting to something well under 1. At `key = 0` the stage becomes "desaturate the
   field in place", which is the operation the Vasarely figure-ground result actually
   supports (the field should be the cooler, less chromatic party — not a different
   lightness). At `key = 1` it reproduces today's behaviour, so the change is opt-out.
2. **Add a coverage floor and no-op below it.** The stage currently fires whenever the
   largest border region clears 4·mark², which on 5 of 7 photographs meant repainting
   1.2–6.8% of the image and calling it a background. A floor at, say, 10% of the image
   would leave those five untouched and fire only on the touareg and the opera, which are
   the two photographs that genuinely have a field.

Both are invariant-safe by the `Refine` signature. Together they turn a stage that moves
its target by ΔE 23–59 into one that either does nothing or does something defensible.

### Pick 3 — register the repaired `GroundFill` in Post-Impressionism, behind the coverage floor

**Slot 5. ~2 lines in `StyleRegistry.cs`, blocked on pick 2.**

Ordered *after* `SmallRegionMerge`, unlike Abstract, which runs it first. With the coverage
floor in place this is a safe registration: on five of the seven photographs it is a no-op,
and on the two with a real field it flattens and quietens a sky that the picture already
reads as background. That is the weakest defensible version of a Post-Impressionist
background treatment, and the strongest the evidence in §1 supports.

**If pick 2 is not done, do not do pick 3.** Registering today's `GroundFill` in
Post-Impressionism would put a hard-edged mid-grey patch into five of seven photographs for
no measurable benefit.

---

## 9. What not to build

The parent, Abstract and Fauvism "what not to build" lists all still apply. These are
additional, and each is something I went looking for and rejected on evidence.

- **A flat decorative background plane.** Flattening any mask larger than one already-flat
  region costs mean ΔE 9.1–38.7; a fixed top-30% band costs 8.4–33.1. Median candidate
  spacing is 1.70 ΔE. §4.1. The paint is available (a Gauguin vermilion plane is ΔE 1.28
  from an achievable mixture) — the photograph is not.
- **A `MotherColourTransform` fraction for Post-Impressionism.** The movement's grounds are
  near-white commercial primings, and the stage blends Titanium White. §2, §3.
- **Extending `BlendInto` to take a mixture so a period-correct ground can be named.**
  Physically sound, ~35 lines, and pointless: the near-white tinted grounds it would target
  quantise at ΔE 6.5 because only 190 of 84,063 candidates sit above L\* 90. §2.2.
- **`ContourLines` in Post-Impressionism as it stands.** 48.3% line coverage on one of
  seven photographs, 23.9% on two more, and its apparent paintability win is the region-
  merging artefact the Fauvism round warned about. §4.3. Revisit only after the output
  actually flattens.
- **Any aerial-perspective stage, including the two-handle gradient, as a default.** The
  source already carries the effect on real landscapes (+51 L\*, −18 C\* top-to-bottom) and
  carries it *backwards* on three of seven photographs. §6.
- **Splitting Post-Impressionism into per-painter rows on measured grounds.** I found no
  study separating Cézanne, Gauguin, Van Gogh and Seurat by any image statistic. §1.3. A
  Cloisonnism row is a product decision blocked on a segmenter, not a research finding.
- **A vertical-band or any other geometric background mask.** Costs ΔE 8.4–33.1 to flatten
  and has no relationship to what a viewer reads as background on 3 of 7 photographs. §4.1.
- **Treating `GroundFill`'s current output as a ground.** It is an L\* 58 constant. §5.1.
- **Trusting `StyleBehaviourTests`' 1.3% Post-Impressionism ceiling as evidence about real
  images.** Real photographs give 10.5–64.2%. Correction 5.

---

## 10. Verification debt

Ranked by how much clearing it would change a decision.

1. **Whether `SmallRegionMerge` can be made a true area opening.** Correction 4 says the
   shipped stage leaves 4.3–46.5% where the Fauvism round's postcondition demands zero.
   Pick 1's whole value is fragmentation reduction, and a correct area opening would take
   it further. This is local work — read the stage, decide whether the single-pass
   stale-label merge is the cause, and if so iterate to a fixed point. **Cheapest item and
   it gates the top pick.**
2. **Whether §5.4 also holds for Abstract.** I measured that no floor strength or mark
   scale moves the largest border-connected region on Post-Impressionism settings. Abstract
   runs strength 5, mark scale 2.5 and an 8-colour palette, which is a materially different
   regime, and its whole `GroundFill` recommendation rests on the field being findable.
   ~20 minutes of probe time.
3. **Corpus colorimetry of Post-Impressionist backgrounds.** This report contains none, on
   purpose: the Abstract round established that reproductions of degraded paint are
   colorimetrically wrong by an unknown amount, and the Fauvism round added that Fauve
   cadmium yellows have measurably shifted. Van Gogh's pink ground in *Daubigny's Garden*
   "now looks grey" `[relayed]` — the same failure. **If anyone wants Lab targets for a
   Gauguin field, they need a museum measurement, not a JPEG.**
4. **The Art Institute of Chicago's Cézanne digital publication** —
   [artic.edu/digital-publications/47/…/362](https://www.artic.edu/digital-publications/47/cezanne-paintings-and-watercolors-at-the-art-institute-of-chicago/362/a-harmony-parallel-to-nature-color-form-and-space-in-cezannes-watercolors-and-oil-paintings)
   and [artic.edu/articles/991](https://www.artic.edu/articles/991/cezanne-s-still-lifes-under-the-microscope),
   both **403**. These are the best located source on Cézanne's treatment of background
   space and would firm up §1.2's central row, which currently rests on the McCrone report
   plus an Artsy glossary entry.
5. **Smarthistory on *Vision after the Sermon*** — [smarthistory.org](https://smarthistory.org/paul-gauguin-vision-after-the-sermon-or-jacob-wrestling-with-the-angel/),
   **403**. And [nationalgalleries.org/art-and-artists/5643](https://www.nationalgalleries.org/art-and-artists/5643),
   the owning museum's own page, also **403**. Every claim about that painting in §1.2 is
   `[relayed]` from search snippets. Nobody should quote "an undulating blue line encases
   everything" in a doc comment on this basis.
6. **The Van Gogh Museum page for *Daubigny's Garden*** —
   [vangoghmuseum.nl/en/collection/s0104v1962](https://www.vangoghmuseum.nl/en/collection/s0104v1962)
   rendered as an empty SPA shell (`{{pageTransitionStateText}}`), so the pink-ground claim
   is from a search summary of that page rather than the page. Same for the Tasset et
   L'Hôte ground study ([academia.edu](https://www.academia.edu/28954780/Investigation_of_the_grounds_of_Tasset_et_L_H%C3%B4te_commercially_primed_canvas_used_by_Vincent_Van_Gogh_in_the_period_1888_to_1890))
   and the Gauguin *Tahitian Pastorals* materials study (researchgate/academia).
7. **Maurice Denis, *Définition du néo-traditionnisme*, *Art et Critique*, 1890.** The
   famous sentence is `[relayed]` from Wikiquote. It is the clearest statement of the flat-
   plane programme and would be worth quoting properly if a Cloisonnism row is ever built.
8. **My photograph set is seven images and I chose them.** They span landscape, portrait,
   architecture, animal, flower and night, which is deliberate, but seven is small and the
   Fauvism round's cautionary case is a self-curated corpus whose control turned out to be
   a Derain. Every number in §4, §5 and §8 would be firmer on thirty. Sources, all
   Wikimedia Commons via `Special:FilePath?width=800` (served at 960 px wide), fetched
   2026-07-30: `Elephant_Diversity.jpg`; `2015 Swaledale from Kisdon Hill.jpg`;
   `1 pano cuiping yangshuo 2016.jpg`;
   `A touareg at the Festival au Desert near Timbuktu, Mali 2012.jpg`;
   `African Cape Daisy (Osteospermum barberiae).jpg`;
   `20180109 Vienna State Opera at blue hour 850 9387.jpg`;
   `A man and his donkey on the way back from the field in Aswan, Egypt (edited).jpg`.
9. **No measurement of what fraction of a Post-Impressionist canvas is left as exposed
   priming.** Cézanne's "great areas" is qualitative. The Abstract round found the same
   absence for Western painting generally and the Fauvism round for Fauve reserve. Three
   rounds have now failed to find a number; it is probably not published.

**Not attempted:** any test of whether a viewer prefers the with-field or without-field
render. The parent README's rejection of automated quality scoring applies, and I had no
way to run human judgements.
