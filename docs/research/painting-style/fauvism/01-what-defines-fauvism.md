# Fauvism, track 1: what defines it

**Date:** 2026-07-28. **Status:** research only, no application code changed.
**Question:** does the shipped Fauvism style earn its name, and if not, what should it do?

Confidence markers follow the house convention: `[verified]` — I read the primary source or
computed the number myself, method stated; `[relayed]` — a secondary source reports it and I
could not reach the primary; `[inferred]` — my reasoning, in no source.

---

## 0. Headline

**The shipped Fauvism style does the one thing the popular caricature of the movement names,
does nothing the movement's own theory names, and actively degrades the only property the
movement has ever been measured on.**

Three findings drive that, in descending order of how much they should change the product:

1. **The only published quantitative placement of Fauvism is a *texture* measure computed on
   greyscale, with colour discarded entirely** (Sigaki, Perc & Ribeiro 2018, PNAS). It puts
   Fauvism at the highest-entropy, lowest-complexity corner alongside Impressionism and
   Pointillism, described as "smudged and diffuse brushstrokes… blending colors in order to
   avoid the creation of sharp edges." `[verified]` No published signature separating Fauvism
   from its neighbours uses chroma, saturation or contrast. This is the same negative result
   the Abstract investigation found, arrived at from a different corpus.

2. **Matisse's "Notes of a Painter" (1908) — published in the movement's final year, by its
   leader, explicitly as a reply to its critics — never once endorses high saturation, and
   contains a direct warning against it:** "If he fears the banal he cannot avoid it by
   appearing strange, or going in for bizarre drawing and eccentric color." `[verified — I read
   the full text]` What the essay does argue for is *relational* colour: every colour chosen in
   response to every other colour already on the canvas, with area as the control variable.
   A pointwise CIELAB function is structurally incapable of that.

3. **Measured locally on the five committed golden renders, Fauvism is the worst of the five
   styles on every paintability statistic** — 331 distinct colours (highest, above Abstract's
   322), 1035 connected regions (highest, 2.4× Realism), 6.1% of pixels in regions of ≤4 px
   (highest, 2.5× Realism). `[verified — computed locally, method in §9]` It is also the only
   style whose defining art-historical property, on report 02's own reading, is *value
   preservation* — and it registers contrast 1.35, which moves 46.7% of pixels by more than
   5 L\* and stretches the dark end of the range from L\* 22.6 to L\* 7.5. `[verified]`

**My best recommendation is to change the existing parameters, not to add a stage.** The
cheapest item on my list is roughly ten lines of `StyleRegistry`, it is the best supported, and
it fixes a measured defect. The two stage-level items below it are both worth doing and both
cost more.

**Where I contradict the parent research** is flagged inline and collected in §10.

---

## 1. Where the Fauvism boundary actually is, and whether it is defensible

**Conclusion: the boundary is social and chronological, not formal, and it does not survive
being asked to do formal work.**

The facts that are solid:

- The style began around 1904 and continued past 1910, but "the movement as such lasted only a
  few years, 1905–1908, and had three exhibitions" — the 1905 Salon d'Automne, the 1906 Salon
  des Indépendants and the 1906 Salon d'Automne. `[relayed — Wikipedia, *Fauvism*]`
- The name is a hostile joke. Louis Vauxcelles, seeing a Renaissance-manner sculpture in the
  same room as the canvases, wrote "Donatello chez les fauves" in *Gil Blas*, 17 October 1905.
  `[relayed — Wikipedia, *Fauvism*]`
- Tate's definition: work "from around 1905 to 1910, which is characterised by strong colours
  and fierce brushwork", using "bold, non-naturalistic colours (often applied directly from the
  tube), and wild loose dabs of paint" with simplified forms. `[verified — fetched tate.org.uk]`
- Tate also states the genealogy plainly: Fauvism is "an extreme extension of the
  post-impressionism of Van Gogh combined with the neo-impressionism of Seurat", and "is also a
  form of expressionism in its use of brilliant colors and spontaneous brushwork."
  `[verified — fetched tate.org.uk]`

That last sentence is the problem. The institution that defines the term defines it as a
*combination of the two things it is supposed to be distinguished from*, plus a form of the
third. There is no formal predicate in it.

The distinctions that are actually offered in the literature are non-formal:

| Proposed boundary | What it really is |
|---|---|
| Fauvism vs Post-Impressionism | Chronology. Post-Impressionism runs "roughly between 1886 and 1905, from the last Impressionist exhibition to the birth of Fauvism" — Fauvism's start date *is* Post-Impressionism's end date. `[relayed — Wikipedia, *Post-Impressionism*]` |
| Fauvism vs Neo-Impressionism | Intent, not appearance. "The Fauves… lacked such scientific intent." `[relayed — Wikipedia, *Fauvism*]` Matisse confirms this himself (§4). Intent is not measurable from an image. |
| Fauvism vs German Expressionism | Nationality and motive. France vs Germany; aesthetic liberation vs industrial-era anxiety. `[relayed — multiple secondary sources; no primary formal criterion located]` |

Machine classifiers agree that these classes overlap. A 2024 deep-ensemble study reaches 68.55%
on 21 WikiArt style classes and notes that "Impressionism and Post-Impressionism share many
common characteristics, as well as Post-Impressionism and Expressionism", concluding that
"the styles that are confused the most are styles that share common characteristics and often
mislead and confuse art experts too." `[verified — fetched arXiv:2405.11675v1]` I could not
obtain a per-class figure for Fauvism from any classifier paper; that is a verification debt
(§11), but note that the failure to find one across several attempts is itself weak evidence
that Fauvism is not treated as a cleanly separable class.

**Practical reading for this app:** there is no formal test you could apply to an output image
that would say "this is Fauvism and not Post-Impressionism." Any Fauvism preset is therefore a
*claim about intent*, and the honest design goal is not "pass a discriminator" but "produce the
handful of properties Fauve pictures reliably have that the neighbouring styles' presets do
not." Those properties are enumerated in §5 and §6.

---

## 2. Is there a measured signature? A negative result

**Conclusion: no measured signature separating Fauvism from its neighbours uses colour. The
one that places Fauvism at all discards colour by construction, and it places Fauvism on an
axis the shipped style moves in the wrong direction.**

This is the exact check the Abstract investigation's track 1 ran, and it comes out the same
way. Here is what I found and what each source can and cannot support.

### 2.1 Sigaki, Perc & Ribeiro 2018 — the one place Fauvism is named

*History of art paintings through the lens of entropy and complexity*, PNAS 115(37)
E8585–E8594. Read via the ar5iv rendering of arXiv:1809.05760. `[verified — the PNAS page and
the raw arXiv PDF both failed; ar5iv's HTML rendering succeeded and I read it]`

- Corpus: "137,364 visual artwork images (mainly paintings), obtained from the online visual
  arts encyclopedia WikiArt.org", spanning "more than a hundred styles". `[verified]`
- The passage that names Fauvism: "styles displaying the smallest values of C and the highest
  values of H (such as **Impressionism, Pointillism, and Fauvism**) are characterized by the use
  of smudged and diffuse brushstrokes." `[verified]` The opposite corner is "Minimalism, Hard
  Edge Painting, and Color Field Painting, which are all marked by the use of simple design
  elements that are well-delimited by abrupt transitions of colors." `[verified]`
- **Colour is discarded.** The authors convert images to greyscale by taking "the average value
  of the three color shades of each pixel", and justify the shortcut by reporting a Pearson
  correlation of 0.989 between H computed that way and H computed with the standard luminance
  transform. `[verified]` Nothing chromatic enters the measure at all.

So the single quantitative statement in the literature about where Fauvism sits is a statement
about **local spatial ordering of intensities**, and it groups Fauvism with the two movements
this project's own research has already identified as needing a *dithering quantiser*, not a
chroma multiplier.

**A caveat that matters more than the finding.** The paper states that H "close to one"
indicates pixels appearing at random, and that C "is zero for both extremes of order and
disorder." `[verified]` White noise therefore sits at H ≈ 1, C ≈ 0 — the *same corner* Fauvism
occupies relative to other styles. The measure cannot distinguish diffuse brushwork from
speckle. Since the app's actual measured defect on Fauvism is speckle (§3), **"move Fauvism
further into the Sigaki corner" is a metric this pipeline can satisfy by getting worse.** Do
not adopt it as a target. `[inferred, from the paper's own definitions]`

### 2.2 Everything else I checked, and what it does not say

| Source | Reached? | Does it separate Fauvism on colour? |
|---|---|---|
| Graham & Field 2007, 2008 (already in report 02) | via report 02 `[verified there]` | No. They report "few low-level statistical differences among classes" of art, and their content breakdown is Landscape / Portrait–still-life / Abstract, not movements. |
| Redies et al. 2017, Front. Neurosci. 11:593 (report 02 §1.5) | via report 02 `[verified there]` | No. Fractal dimension, self-similarity and edge-orientation entropy — all spatial. No movement-level Fauvism row. |
| Kim, Son & Jeong 2014, Sci. Rep. 4:7370 | via report 02 `[relayed there]` | No — the dataset stops mid-19th century, so Fauvism is outside it entirely. |
| Lee, Kim, Sun, Jeong & Park 2018, *PLOS ONE* 13(9) e0204430 | `[verified — read the PMC rendering]` | **Colour is used** — "seamlessness" S is built from CIELAB distances between adjacent pixels — but there is no per-movement breakdown and Fauvism is never named. The paper's own conclusion cuts against per-style targeting: "the distribution of S is narrow around the mean, but it becomes increasingly broader as we approach the modern times", and it notes individual painters span wide stylistic ranges within themselves. |
| Pessoa et al. 2025, arXiv:2503.09844, two-by-two ordinal patterns, ~140,000 paintings | abstract only `[verified]` | No. "Pixel intensities" again — greyscale. The abstract's own hedge is the relevant part: "styles generally exhibit considerable variability in the prevalence of ordinal patterns." |
| Costa et al. 2023 (report 02 §1.6), the best explainable colour classifier | via report 02 `[verified there]` | No Fauvism class at all. Its three classes are Baroque / Impressionism / Post-Impressionism, at 78.01% ± 6.63 on 90 images. |
| Desikan et al. 2022, *Entropy* 24(9) 1175, WikiArtVectors | **not reached** — MDPI returned 403, the Semantic Scholar PDF would not decode | Unknown. This is my top verification debt. |

I searched specifically for a study reporting mean saturation or colourfulness per art movement
and found none. `[verified — searched; nothing located]` The claim that Fauvist paintings are
measurably more saturated than Post-Impressionist or Expressionist ones appears never to have
been tested at scale.

**This exactly parallels the Abstract finding.** That investigation concluded that "abstract art
is more saturated" is a folk belief resting on nothing. I reach the same verdict for "Fauvist
art is more saturated": it is almost certainly *true* of the canonical examples, and it has
never been measured against a control, and the only measured axis on which Fauvism has ever
been located is not chromatic at all.

---

## 3. What the shipped style actually does, measured

**Conclusion: Fauvism is the most fragmented and most colour-scattered of the five registered
styles, and the contrast parameter is a substantial part of why.**

Measured on the five committed golden renders under `Tests/Golden/`, all five produced from the
same source (`StyleTestFixtures.BuildNoisyGradient(128, 128, 2.0)` — a seeded bilinear field
between four photographic colours with σ = 2 Gaussian noise) and the same six-paint palette, at
`MarkPixels = 4`. Method in §9. `[verified — computed locally 2026-07-28]`

| | Realism | Tonalism | **Fauvism** | Post-Imp. | Abstract |
|---|---|---|---|---|---|
| Distinct colours | 161 | 151 | **331** | 205 | 322 |
| Colours to cover 90% of pixels | 88 | 70 | **177** | 108 | 159 |
| Connected regions | 425 | 344 | **1035** | 486 | 685 |
| Median region area (px) | 3 | 6 | 4 | 5 | 6 |
| Pixels in regions ≤ 4 px | 2.4% | 1.6% | **6.1%** | 2.7% | 3.7% |
| Largest single region (px) | 1611 | 1431 | 1280 | 1139 | 2009 |
| Mean C\*ab | 17.0 | 8.2 | 35.1 | 22.3 | 24.4 |
| L\* mean / SD | 60.0 / 18.1 | 59.6 / 9.8 | 63.4 / **23.3** | 61.6 / 20.9 | 66.0 / 22.8 |

My Abstract column reproduces the Abstract investigation's published figures exactly (322
colours, 685 regions, 2009 px largest, 30.2% top-5 share, 159 colours to 90%), which validates
the decoder and the method against an independently produced measurement.

**Fauvism is worse than Abstract on every fragmentation statistic**, and Abstract was the style
that investigation flagged as defective for exactly this reason. The codebase already knows:
`StyleBehaviourTests` records Fauvism at a 7.8033% sub-mark fraction on the σ = 3 source — the
highest of the five, against Realism's 2.5757% — and its doc comment states that Fauvism "runs
this stage at its own weakest declared default" with "no lever left to turn without changing the
style's registered behaviour." `[verified — read the source]` My measurement is independent
confirmation from a different source image and a mark-size-independent threshold.

### 3.1 The chroma boost lands, but crookedly

Comparing Fauvism against Realism pixel-for-pixel (same source, same palette), over the 14,538
of 16,384 pixels that are chromatic in both (C\* > 5): `[verified — computed locally]`

- Mean chroma gain **×2.07**, median ×1.99. On this palette the chroma 2.2 setting substantially
  arrives.
- Mean absolute hue shift **12.5°**, median 7.7°. **40.6% of chromatic pixels rotate by more
  than 10°**; 6.5% by more than 30°.

Broken down by the pixel's hue sector in the Realism render:

| Realism hue sector | Share of image | Realised chroma gain | Mean signed hue shift | C\* before → after |
|---|---|---|---|---|
| 0–30° | 52.9% | ×2.20 | **+10.6°** | 16.7 → 35.5 |
| 30–60° | 5.6% | ×1.79 | +0.1° | 40.9 → 72.4 |
| 90–120° | 12.4% | **×1.60** | **−7.8°** | 20.1 → 32.1 |
| 270–300° | 4.8% | ×2.16 | +9.7° | 23.2 → 49.3 |
| 300–330° | 7.9% | ×2.08 | +2.1° | 16.0 → 33.2 |
| 330–360° | 5.1% | ×2.08 | −1.3° | 14.8 → 29.9 |

The 90–120° row is the diagnostic. Those pixels start at the *lowest* chroma of any sector, so
`ToneAndChromaRemap`'s tanh knee should bite them *least* and they should receive the *largest*
multiplier. They receive the smallest, and they rotate 7.8° away from their own hue. That gap is
not the knee; it is the nearest-candidate search failing to find anything at the requested
chroma in that hue and settling for a neighbouring one. Working the arithmetic: at gain 2.2 the
knee weight is (2.2 − 1)/(3 − 1) = 0.6, and against a ceiling near 106 (Hansa Yellow Opaque's
masstone is in this palette) an input of C\* 20.1 is asked for C\* ≈ 42.6 and realises 32.1 — a
25% shortfall paid for in hue drift. `[verified — computed locally, arithmetic mine]`

**This is the live cost of the scalar `AchievableMaxChroma`**, and it confirms the Abstract
investigation's correction 1 from a direction that investigation did not take: they showed the
ceiling is per-hue in the *paint library*; I measured what the scalar version does to *rendered
output*. Recomputed over the 19 selectable paints, my own pass over `pigments.manifest.txt`
reproduces their table: `[verified — computed locally]`

| Hue sector | Best selectable masstone |
|---|---|
| 0–30° | Quinacridone Red, C\* 61.3 at L\* 34.0 |
| 30–60° | Pyrrole Orange, C\* 99.9 at L\* 54.4 |
| 60–90° | Hansa Yellow Opaque, C\* 106.4 at L\* 85.5 |
| 90–120° | Bismuth Vanadate Yellow, C\* 96.6 at L\* 91.7 |
| 120–150° | **empty** |
| 150–180° | Phthalo Green (Y.S.), C\* 31.9 at L\* 18.9 |
| 180–210° | **empty** |
| 210–240° | Phthalo Green (B.S.), C\* 15.5 at L\* 13.6 |
| 240–270° | Cerulean Blue, Chromium, C\* 42.3 at L\* 33.8 |
| 270–300° | Cobalt Blue, C\* 70.7 at L\* 27.5 |
| 300–330° | Ultramarine Blue, C\* 66.4 at L\* 7.4 |
| 330–360° | **empty** |

A single number cannot serve a range from 106.4 to zero.

### 3.2 Contrast 1.35 breaks the one constraint report 02 identified

Report 02's Fauvism formula is `L*_out = L*_in` with the annotation "**Identity — this is the
defining constraint**". The shipped style registers contrast 1.35. Measured effect:
`[verified — computed locally]`

- L\* SD 18.1 → 23.3 (+29%).
- L\* range 22.6–98.2 → 7.5–98.2. The dark end moves 15 L\* units.
- **46.7% of pixels move by more than 5 L\*.**

Two costs. First, it contradicts the project's own stated reading of the style. Second — and
this is the part nobody has written down — **expanding L\* is a fragmentation multiplier.**
Stretching the tonal range spreads the image's colours across more of the candidate set's
Voronoi cells, so more adjacent pixels land on different mixtures. That is the mechanism behind
Fauvism's 331 distinct colours and 1035 regions, on top of the chroma boost doing the same
thing in the a\*b\* plane and the weakest floor in the registry doing nothing to stop either.
`[inferred, from the measured L\* spread and the measured colour count together]`

---

## 4. Matisse's own theory versus the label

**Conclusion: "Notes of a Painter" is not a manifesto for loud colour. It is a manifesto for
relational colour and for restraint, and one of its sentences reads as a direct rebuke of the
shipped style's approach.**

I obtained and read the full English text (Flam translation lineage, 5pp., as circulated for
teaching). `[verified — I decoded the PDF locally and read the whole essay; see §9]` The
following are verbatim.

**On what he is after:**
> "What I am after, above all, is expression… Expression, for me, does not reside in passions
> glowing in a human face or manifested by violent movement. The entire arrangement of my
> picture is expressive: the place occupied by the figures, the empty spaces around them, the
> proportions, everything has its share."

**On colour's job:**
> "The chief function of color should be to serve expression as well as possible."

**On method — this is the passage that decides the architecture question:**
> "If upon a white canvas I set down some sensations of blue, of green, of red, each new stroke
> diminishes the importance of the preceding ones… But these different tones mutually weaken one
> another. It is necessary that the various marks I use be balanced so that they do not destroy
> each other… A new combination of colors will succeed the first and render the totality of my
> representation. I am forced to transpose until finally my picture may seem completely changed
> when, after successive modifications, the red has succeeded the green as the dominant color."

**On area as the control variable:**
> "If I put a black dot on a sheet of white paper, the dot will be visible no matter how far away
> I hold it… But beside this dot I place another one, and then a third, and already there is
> confusion. In order for the first dot to maintain its value **I must enlarge it** as I put
> other marks on the paper."

**On colour theory:**
> "My choice of colors does not rest on any scientific theory; it is based on observation, on
> sensitivity, on felt experiences. Inspired by certain pages of Delacroix, an artist like Signac
> is preoccupied with complementary colors… But I simply try to put down colors which render my
> sensation."

**On nature:**
> "I cannot copy nature in a servile way; I am forced to interpret nature and submit it to the
> spirit of the picture." And, two pages later: "An artist must recognize, when he is reasoning,
> that his picture is an artifice; but when he is painting, he should feel that he has copied
> nature. And even when he departs from nature, he must do it with the conviction that it is only
> to interpret her more fully."

**The rebuke:**
> "The simplest means are those which best enable an artist to express himself. If he fears the
> banal he cannot avoid it by appearing strange, or going in for bizarre drawing and **eccentric
> color**."

**On the goal:**
> "What I dream of is an art of balance, of purity and serenity, devoid of troubling or
> depressing subject-matter… something like a good armchair which provides relaxation from
> physical fatigue."

**On the label itself.** Matisse's only reference to "Fauves" is second-hand and sardonic — he
notes that Péladan "reproaches a certain number of painters, amongst whom I think I should place
myself, for calling themselves Fauves, and yet dressing like everyone else" and replies that
"tomorrow I would call myself Sar and dress like a necromancer." He never claims the term.

### What follows for this app

1. **The essay contains no endorsement of saturation.** The words saturation, intensity, brilliance
   and vividness do not appear. The nearest thing to a chroma statement is the condemnation of
   "eccentric color". The popular caricature — Fauvism = crank the saturation — is not in the
   founding document. `[verified]`
2. **"Balance, purity and serenity" is the opposite register from the caricature.** The armchair
   sentence is the single most-quoted line in the essay and it describes a calm picture.
3. **Colour is assigned relationally and globally.** Matisse's process is: place a colour, observe
   what it does to the others, revise all of them, possibly until the dominant hue has changed
   from green to red. That is a *whole-picture optimisation*, not a per-pixel function.
4. **Force is controlled by area, not by chroma.** The dot passage says explicitly that to keep a
   mark's value you enlarge it. In pipeline terms that is `MarkPixels` and region size, not
   `chroma`.
5. **He is not anti-descriptive.** "When he is painting, he should feel that he has copied nature"
   is a defence of description as a working attitude. So the tension the brief identifies — a
   photo-to-paint converter is by construction descriptive — is *less* acute than the label
   implies. Matisse is not asking for arbitrary colour; he is asking for colour subordinated to
   the picture's internal balance. **A converter that starts from a photograph is not disqualified
   from Fauvism by its own theorist.** `[inferred, from the two nature passages read together]`

**Where this leaves "colour liberated from description" operationally.** The liberation Matisse
describes is not "ignore the photo". It is: (a) let the *quantity* of a colour, not its intensity,
carry its force; (b) let the number of distinct colours be small enough that they can be balanced
against each other at all; (c) allow the dominant hue of the picture to be chosen rather than
averaged. All three are region-level operations. None is a pointwise function of pixel colour.
`[inferred]`

---

## 5. Is Fauvism one style or several?

**Conclusion: at least two, and they need opposite pipeline treatments. The shipped single row
implements neither.**

I ran the same test the Abstract investigation ran, without assuming the answer.

**The social structure was tripartite from the start.** The Fauves comprised three groups that
formed independently: the Gustave Moreau studio group (Matisse, Marquet, Camoin, Puy, Manguin);
the Chatou pair (Derain, Vlaminck); and the Le Havre trio (Friesz, Dufy, Braque).
`[relayed — multiple secondary sources agree on this grouping; I did not reach a primary]`

**The handling splits cleanly in two, and the split runs through individual artists, not just
between them.** Within Matisse's own Fauve years:

- *Luxe, calme et volupté* (1904–05) is divisionist — "staccato brushstrokes with the white of
  the canvas showing through". `[relayed]`
- *Le bonheur de vivre* (1905–06) and the flat portraits of 1905 abandon that entirely for broad
  arabesque planes of unmodulated colour.

So inside eighteen months, by one artist, the mark structure inverts while the chroma stays high.
Vlaminck's practice — "squeezing paint directly onto the canvas from the tube" `[relayed —
Wikipedia, *Fauvism*]` — belongs to the first branch; Matisse's flat planes to the second.

**The measured evidence sits on one branch only.** Sigaki places Fauvism at the diffuse,
high-entropy corner with Impressionism and Pointillism `[verified]`. Flat unmodulated planes with
hard boundaries belong at the *opposite* corner, with Hard Edge and Colour Field. The WikiArt
Fauvism corpus must therefore be dominated by the broken/loose branch — which means the one
published measurement of "Fauvism" does not describe the Matisse that most people picture when
they hear the word.

**Recommendation on this point: keep one row, and commit it to the flat-plane branch.** Not
because that branch is more authentic — it is not — but because it is the branch this
architecture can reach. Nearest-colour matching onto a small palette with no dithering is by
construction a posteriser; flat planes of unmixed colour are what it is natively good at. The
broken-colour branch requires the dithering quantiser the parent research has already parked
behind a gamut measurement. Splitting the row before that quantiser exists would produce two
presets that differ only in numbers. `[inferred]`

This is a weaker recommendation than the Abstract investigation's, deliberately. There, splitting
was free because both branches were expressible. Here, one branch is not.

---

## 6. Which of the five slots could produce Fauvism, formally

**Conclusion: the same answer the Abstract investigation reached for abstraction — only slots 1
and 5. Slots 2 and 3 can produce *loudness*, which is not the same thing.**

Set up the argument the way track 1 of that investigation did.

**Definitions.** Let the input be a pixel field `p: (x,y) → Lab`. The pipeline composes:

- Slot 1, `IPreMapStage.Apply` — arbitrary `(pixels, x, y) → pixels`. Full spatial access.
- Slot 2, `ILabRemap.Map` — a function `f: Lab → Lab`. No position argument exists in the
  signature. `[verified — read `PipelineStages.cs`]`
- Slot 3, `ICandidateTransform.Transform` — edits the achievable set `K ⊂ Lab` before any pixel is
  seen. Independent of the image entirely.
- Slot 4, `IQuantiser.Map` — `(Lab, K, x, y) → index`, but position is meaningful only if
  `IsPositionDependent` is set, which forces `ResolvePerPixel` and costs roughly 80× the
  nearest-neighbour searches on a 12 MP image. `[verified — read `StylePipeline.cs`]`
- Slot 5, `IPostMapStage.Refine` — arbitrary `(indices, x, y) → indices`. Full spatial access,
  selection-only by signature.

**Claim.** Fauvism's defining move, on Matisse's own account, is that a colour's assignment
depends on the rest of the picture, not on the pixel. Formally: there exist two pixels `u`, `v`
with `p(u) = p(v)` that a Fauve treatment sends to different colours — the leaf that stays green
in one passage and turns red in another, because the balance of the whole demanded it.

**Slot 2 cannot do this, by definition.** `f` is a function of `Lab` alone, so `p(u) = p(v)`
implies `f(p(u)) = f(p(v))`. Any style whose definition requires two same-coloured pixels to
diverge is outside slot 2's expressible set. This is not a limitation of the current
implementation; it is the type signature.

**Slot 3 cannot do this either, and for a stronger reason.** It never sees a pixel. It can shrink,
bias or thin `K`, which changes *what colours are available* — but the assignment from colour to
candidate remains a function, so the same argument applies to the composition of slot 2 and slot 3.

**Slot 4 could, but must not.** It receives `x, y`. Using them sets `IsPositionDependent` and
forfeits the per-colour cache. The Abstract investigation's design rule stands: never put a
positional operation in slot 4.

**Slots 1 and 5 can.** Slot 1 can segment the image, compute region statistics and rewrite pixel
values per region — two identical input pixels in different regions receive different values, and
the gamut invariant is untouched because everything is mapped afterwards. Slot 5 can relabel
whole regions of the index buffer, and by the `Refine` signature it cannot name a colour outside
`K`, so it cannot break the invariant either.

**Corollary, and this is the point.** The three properties §4 extracted from Matisse — force
carried by area, few enough colours to be balanced, a chosen dominant hue — are all region-level.
**All three land in slots 1 and 5. None of them is reachable from slot 2, which is where the
entire shipped Fauvism lives.** `[inferred, from the interface signatures]`

What slots 2 and 3 *can* do is worth stating fairly, because it is not nothing: they can raise the
overall chroma and change what mixtures are available. That produces a louder picture. It does not
produce a differently-*organised* one. Fauvism's own theory is about organisation.

---

## 7. Three changes, in priority order

Cheapest and best-supported first. Line counts include this codebase's doc-comment convention,
which roughly doubles the figure a bare-implementation estimate would give.

### 7.1 Retune the registered numbers — no new stage

**Slot:** none. `Imaging/Styles/StyleRegistry.cs` only.
**Cost:** ~10 lines of code changed; realistically ~60 lines of doc comment rewritten in
`StyleRegistry` and `StyleBehaviourTests`; one golden PNG regenerated; one test baseline moved.

The change:

| Parameter | Now | Proposed | Why |
|---|---|---|---|
| `ToneAndChromaRemap.contrast` | 1.35 | **1.0** | Report 02 names value identity as Fauvism's defining constraint; Greenberg's account of the Matisse/Hofmann line is the "rejection of value contrast as the essential building block, in favor of color" `[relayed]`; and locally, contrast 1.35 moves 46.7% of pixels by >5 L\* and is a direct driver of the 331-colour, 1035-region fragmentation. |
| `EdgePreservingFloor.strength` | 1.0 (stage default, no override) | **3.0** | Fauvism is the only style with a raised `MarkScale` (1.3) and the registry's weakest floor. The code's own comment says there is "no lever left"; that is only true because no override is registered. Post-Impressionism at strength 3.0 with `MarkScale` 1.6 measures 2.7% sub-4px against Fauvism's 6.1%. |
| `ToneAndChromaRemap.chroma` | 2.2 | leave at 2.2 for now | It is the one part of the style with a plausible claim to the name, and §7.2 changes what it means. Reassess after the per-hue ceiling lands. |

**Evidence:** strong for the floor (my measurement plus the existing `StyleBehaviourTests`
baseline, two independent sources and metrics agreeing); moderate for the contrast (report 02's
inference, one relayed art-critical claim, and a mechanical argument I measured).

**Verification:** regenerate `Tests/Golden/Fauvism.png` under the documented Regenerate
procedure, look at it, and re-run the §9 script. Expect distinct colours ≈ 331 → ~200, regions
≈ 1035 → ~500, pixels in ≤4 px regions 6.1% → ~2.5%, and mean C\* roughly unchanged near 34.
`StyleBehaviourTests.EveryRegisteredStyleIsPaintable` will need Fauvism's ceiling moved down from
0.085; `FauvismRaisesMeanChromaAboveRealism` must still clear its 1.25× bar — it currently has a
wide margin (2.07× on the golden source), so this is safe.

**This is my top recommendation and it adds no code.**

### 7.2 Replace the scalar `AchievableMaxChroma` with a per-hue ceiling

**Slot:** feeds slot 2. `Imaging/RenderContext.cs` and `Imaging/StylePipeline.cs`, read by
`ToneAndChromaRemap`.
**Cost:** ~50–60 lines with doc comments (a ~36-bin lookup built from the candidate set in place
of `StylePipeline.MaximumChroma`, an accessor on `RenderContext`, three lines in the remap), plus
~40 lines of test.

This is the Abstract investigation's build-order item 1, and I independently reached it from
rendered output rather than from the paint manifest. Their argument was that a scalar ceiling
makes "×1.5" mean 106 in yellow and 32 in green. My measurement (§3.1) is the consequence in
pixels: 40.6% of chromatic pixels rotate more than 10° of hue, and the sector that should receive
the largest multiplier receives the smallest.

**Evidence:** strongest of the three. Two independent derivations, a measured effect, and it fixes
a live defect in Fauvism, Post-Impressionism and Abstract simultaneously.

**Verification:** re-run the paired script in §9. The target is that mean absolute hue shift falls
materially from 12.5°, and that realised chroma gain becomes monotone in requested gain across
sectors — specifically, that 90–120° stops being the lowest-gain sector while starting from the
lowest chroma. Add a unit test that for a single-paint palette the ceiling at that paint's own hue
equals its masstone chroma and falls away in distant sectors.

### 7.3 A per-hue chroma-floor candidate transform

**Slot:** 3. A new `ICandidateTransform`, reusing §7.2's hue table.
**Cost:** ~70 lines for the stage with doc comments, ~3 lines of registry wiring. No change to
`MixtureBuilder` is needed — `KeepOnly` already receives `(L*, a*, b*)`, which is everything a
chroma floor requires. `[verified — read `MixtureBuilder.KeepOnly`]`

The stage keeps only candidates whose chroma is at least some fraction *t* of the per-hue hull at
their own hue (plus, unconditionally, the near-neutrals, so shadows and whites still have
somewhere to land). At *t* = 0 it is an exact no-op, satisfying the pipeline's default convention.

**Why this rather than more chroma gain.** Raising `chroma` moves the *target* to a place the
paints may not reach and lets nearest-neighbour clean up the mess — which is what produces the
measured hue drift. Thinning the candidate set moves the *available answers* instead. Every
surviving candidate is a colour the paints genuinely mix to, so the operation is gamut-safe by
construction, costs nothing per pixel, and makes conversion faster. And it attacks the
fragmentation directly: fewer candidates means fewer Voronoi cells means larger regions. It is the
same reasoning that made candidate-set thinning the Abstract investigation's first pick, aimed at
chroma instead of colour count. `[inferred]`

Note what it does *not* do: it does not raise the reachable ceiling, because the ceiling is already
a masstone and masstones survive any thinning. What it raises is the *typical* chroma of the answer
by removing the muted interior of the gamut, which is exactly where a photograph mostly lives.

**Evidence:** weakest of the three — the mechanism is sound and the physics argument is solid
(K-M mixing always lands below both parents, so the interior is dull by construction), but no
source measures that Fauve paintings sit near the hull. Treat *t* as a tunable with a conservative
default (~0.4) rather than a claim.

**Verification:** on the golden, distinct-colour count should fall *and* mean C\* should rise at
the same time. That conjunction is the signature of "chroma obtained by candidate selection rather
than by moving the target", and it is a property no amount of `chroma` gain can produce — gain
raises both counts together. Watch for banding: check that the largest region does not grow past
roughly a quarter of the image at the default *t*.

### 7.4 Deliberately not in the top three

**Restricting mixtures to ≤2 paints** (the "straight from the tube" reading). It is a real lever,
it needs `KeepOnly` extended with mixture arity, and the Abstract investigation already flags it as
their item 6. I did not rank it because its warrant is weaker than it looks: "squeezing paint
directly onto the canvas from the tube" is documented for **Vlaminck specifically** `[relayed]`,
and the technical study of Matisse's *The Red Studio* identifies twelve pigments `[relayed, via
report 02]`. Generalising Vlaminck's habit to the movement is a folk move. §7.3 obtains most of the
same visual effect with a better-founded rationale.

**Region-level colour reassignment in slot 5.** This is what Matisse actually describes, and it is
the right long-term answer. It needs the shared connected-component labelling three Abstract tracks
already want, plus ~120 lines on top. It is out of scope for a first pass at Fauvism and should ride
on that shared infrastructure when it exists.

---

## 8. What not to build

Each of these sounds right for Fauvism and does not survive the evidence.

- **Raising the chroma multiplier further, or adding a hue-rotation "arbitrary colour" knob.**
  Matisse's own text warns against exactly this — "he cannot avoid [the banal] by appearing strange,
  or going in for bizarre drawing and eccentric color" `[verified]` — and no measured signature
  anywhere supports chroma as a Fauvism discriminator `[verified, by absence]`. The measured
  consequence in this app is hue drift, not saturation (§3.1).

- **Targeting the Sigaki entropy–complexity corner.** The corner Fauvism occupies is the corner
  white noise occupies: H ≈ 1, C ≈ 0. `[verified — from the paper's own definitions]` The metric
  cannot tell diffuse brushwork from speckle, and the app's measured defect *is* speckle. Optimising
  toward it would reward the bug.

- **Non-local or symbolic colour** (a green stripe down a face, red trees). It needs semantic
  segmentation and a model file. The parent research already rejects it for Expressionism; the same
  reasoning applies here and nothing I found weakens it.

- **A "Fauve palette" preset as the answer on its own.** Report 02's Matisse pigment list is
  `[relayed]`, and Costa et al.'s 78% on a three-way task bounds how far colour-only features can
  read as a style anyway. A palette preset is a fine addition and it is not a fix.

- **Treating "Fauvism preserves value" as established.** I am recommending contrast → 1.0, but the
  art-historical leg of that argument is `[relayed]` (the Greenberg/Hofmann "rejection of value
  contrast" line) and report 02's version is `[inferred]`. **No corpus measurement of Fauve value
  distributions exists that I could find.** `[verified — searched; nothing located]` The
  recommendation stands on the fragmentation measurement, which is solid; do not restate the value
  claim as if it were measured.

- **Splitting Fauvism into two style rows now.** The evidence says it is two things (§5), but one
  of the two branches needs the dithering quantiser this project has not built. Two rows today
  would differ only in numbers, which is exactly the criticism the current row already attracts.

- **Any attempt to make the output "not descriptive".** Matisse explicitly says the painter "should
  feel that he has copied nature" `[verified]`. The tension in the brief is real for the *label* and
  much weaker for the *theory*. Do not design against a caricature the movement's own leader
  rejected.

---

## 9. Method notes for the local measurements

All measurements were made read-only against committed files; no application code, test or asset
was modified. Scripts live in the session scratchpad, not the repository.

- **Golden renders.** `Tests/Golden/{style}.png`, decoded with a purpose-written PNG reader
  (zlib inflate plus the five PNG scanline filters) rather than a library, since no imaging package
  was available. Validated by reproducing the Abstract investigation's published Abstract-column
  figures exactly.
- **Colour space.** sRGB → linear → XYZ (D65) → CIELAB, standard IEC 61966-2-1 and CIE
  definitions. This is a re-implementation, not the app's `PalettePhotoConverter.RgbToLab`; the
  agreement of my Abstract figures with theirs is the cross-check.
- **Regions.** 4-connected flood fill over exact RGB equality, matching the semantics of
  `PaintabilityMetrics.ForEachRegion`.
- **Source image.** `StyleTestFixtures.BuildNoisyGradient(128, 128, 2.0)` — a seeded bilinear
  field between four photographic colours plus σ = 2 Gaussian noise, 128×128, six paints, mark 4 px.
  This is a synthetic stress case, not a photograph; the *cross-style comparison* is valid because
  all five styles render the same source, but the absolute fragmentation percentages should not be
  quoted as photograph behaviour.
- **Manifest.** `Pigments/pigments.manifest.txt`, 80 rows, 19 `TwoConstantMeasured`. Hue from
  `atan2(b*, a*)`, binned at 30°.
- **Matisse text.** The PDF at `kevinrmuller.net` would not render through the fetch tool; I
  decoded its Flate-compressed content streams locally and read the complete five-page essay. Every
  Matisse quotation in §4 is from that text.

---

## 10. Where this contradicts or corrects the parent research

1. **Report 02 says Fauvism is "the one style whose definition is *natively pointwise*."** That is
   the claim this report most directly contradicts. It is true of the *caricature* and false of the
   movement's own theory: Matisse's stated method is relational and area-driven, and the only
   published measurement of Fauvism is spatial. `[verified]` The formal argument in §6 shows a
   pointwise remap cannot express the defining move even in principle.

2. **Report 02's "Fauvism = value identity, chroma ×1.8–2.2" is right about the identity and the
   shipped style does not implement it.** Contrast 1.35 is registered. Report 02 marked the identity
   `[inferred]`; I have not been able to upgrade that marker, and I say so in §8.

3. **The parent README's warning that "a Fauvist ×2 is simply unreachable in blues and greens"
   needs qualifying, not repealing.** On the six-paint test palette the realised mean gain is
   ×2.07, so ×2 is not universally unreachable — it depends entirely on the palette. What *is*
   universal is that the shortfall shows up as **hue rotation** rather than as visible banding:
   40.6% of chromatic pixels move more than 10°. `[verified]` Banding is the failure mode the tanh
   knee was built to prevent, and it appears to have prevented it; hue drift is the failure mode
   nobody was watching.

4. **The Abstract investigation's correction 1 is confirmed and extended.** They showed the ceiling
   is per-hue in the library; I measured the scalar version's cost in rendered output. Their
   recommendation is unchanged and now has a second, independent line of support.

5. **New, and not in any prior report: Fauvism is the least paintable of the five registered
   styles**, worse than Abstract, which was the style the previous investigation was convened to
   fix. `[verified]`

---

## 11. Verification debt

Ranked by how much reaching the source would change a recommendation.

1. **Desikan et al. 2022, *Entropy* 24(9) 1175, "WikiArtVectors".** MDPI returned 403; the Semantic
   Scholar PDF would not decode. It is the one located source that builds per-style *colour*
   distributions over WikiArt and could therefore either confirm or destroy my §2 negative result.
   If it reports Fauvism as chromatically distinctive, recommendation 7.1's contrast change stands
   but the framing of §0 and §8 needs softening. **Clear this first.**

2. **A per-class accuracy or confusion figure for Fauvism from any style classifier.** I reached
   overall figures (68.55% on 21 WikiArt classes) but never a Fauvism row. `[verified]` If Fauvism
   is separable at high accuracy, the "no formal boundary" claim in §1 weakens; if it is confused
   mainly with Post-Impressionism and Expressionism, §1 strengthens. Saleh & Elgammal 2015
   (arXiv:1505.00855) is the likeliest source and its PDF would not decode.

3. **Sigaki et al.'s SI Appendix Fig. S6**, which reportedly holds all 92 styles' H and C
   coordinates. I have the qualitative statement verbatim but no number for Fauvism. Would let §2's
   claim be stated as a coordinate rather than a corner.

4. **Any corpus measurement of Fauve value distributions.** None found. This is the missing leg of
   recommendation 7.1's contrast change, which currently rests on report 02's inference plus my
   fragmentation argument.

5. **Falomir et al. 2018, *QArt-Learn*, Expert Systems with Applications 97:83–94.** Paywalled;
   still the outstanding debt from report 02. Carries per-style mean HSL figures, which is the
   closest thing to a per-movement saturation number anyone has published.

6. **A primary source for the three-group structure of the Fauves** (Moreau studio / Chatou /
   Le Havre). Multiple secondary sources agree, none cites a primary. Low stakes — it supports §5
   but §5 does not depend on it alone.

7. **The Greenberg source for "rejection of value contrast."** Reached only through a secondary
   paraphrase. Named in §8 as a claim not to over-state.
