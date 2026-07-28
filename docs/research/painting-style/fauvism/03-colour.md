# The Colours of Fauvism

**Date:** 2026-07-28
**Track:** 3 of 4 on Fauvism — "what were the Fauves' colours, is Fauvist colour actually
high-chroma, and what can this library reach?"
**Shipped state under examination:** `Imaging/Styles/StyleRegistry.cs:66-88` — Fauvism is
`EdgePreservingFloor` at default strength, `ToneAndChromaRemap` at **contrast 1.35, chroma
2.2**, `KeepAllCandidates`, `NearestQuantiser`, no post-map stage, mark scale 1.3.

**Relationship to prior research.** Extends
[../01-colour-theory-in-practice.md](../01-colour-theory-in-practice.md) (13 levers) and
[../abstract/04-colour-and-palette.md](../abstract/04-colour-and-palette.md). **It
contradicts the abstract track on the single point that matters most for Fauvism** — the
per-hue chroma ceiling — and the contradiction is measured, not argued. See §3.

**Claim marking:** `[verified]` = read a primary source directly, or measured it locally
this session; `[relayed]` = a secondary source asserts it and I could not confirm;
`[inferred]` = my own reasoning.

---

## Conclusions, first

**1. The green wall does not exist. The abstract track's chroma ceiling is wrong, and
wrong in the direction that matters for Fauvism.** That track concluded from
`pigments.manifest.txt` that the best selectable green is C\* 31.9 at L\* 18.9 and that
hue sectors 120–150° and 180–210° hold nothing at all — and inferred from that "a Fauve
green is not available." Both halves fail. Masstone chroma is *not* the ceiling: **13 of
the 18 chromatic selectable paints reach higher C\* in a white tint than at full
strength**, because dark transparent pigments read as near-black at masstone. Phthalo
Green (Y.S.) goes from C\* 18.9 masstone to **C\* 56.3 at L\* 75.6** let down with white;
Dioxazine Purple from **6.5 to 52.6**. And mixing is not monotone downward either — 6.5%
of sampled pair mixtures exceed *both* parents' chroma. Measured over the actual candidate
set from all 19 selectable paints (84,063 mixtures), **there is no empty hue sector and no
sector below C\* 35**; the greens reach **C\* 86–89 at L\* 70–82**. `[verified — computed
locally 2026-07-28]`

**2. "Fauvist paintings are more saturated" has still never been measured, and the app's
own numbers say the shipped style is not delivering saturation anyway.** The largest
computational studies of art style are all spatial — Sigaki et al.'s PNAS analysis of
~140,000 paintings, which does place Fauvism, **converts every image to greyscale first**
and so says nothing about colour `[verified]`. What I could measure is the app. On the
committed golden renders, Fauvism's realised mean C\* is **35.1 against Realism's 17.0** —
but **90.8% of the Fauvism render sits below C\* 60**, i.e. below every chromatic masstone
in its own palette except quinacridone magenta. And it costs: **1,035 regions and 21.1% of
pixels in regions ≤16 px, against Realism's 425 and 5.4%.** Chroma 2.2 buys a modest chroma
lift by quadrupling the unpaintable fraction. `[verified — computed locally]`

**3. The knob says 2.2; the picture gets between 0.76× and 1.88×, depending on hue.** On a
full-hue-circle source, the realised source→output chroma ratio at gain 2.2 with the golden
six-paint palette ranges from **0.76×** at 180–210° (chroma actually *falls*) to **1.88×**
at 30–60°. The scalar `AchievableMaxChroma` over-asks by 22–31 C\* units in five of twelve
sectors. That defect is invisible on the committed golden because its source contains **no
pixels at all between 150° and 270°** — the sectors where the failure lives. `[verified —
computed locally]`

**4. Fauvism's real move is hue displacement, and a photo converter cannot do the
interesting version of it.** Global hue rotation is one line and is legible — but the
rotation studies say originals are preferred, and the memory-colour literature says skin,
sky and foliage are exactly what breaks under a hue edit. Per-region non-descriptive hue
substitution — the green face, the pink road — needs object semantics the app does not have
and should not acquire. §5.

**5. The single strongest lever is a masstone-biased candidate set, not a chroma number.**
Measured head to head on the golden source at identical settings: restricting candidates to
tube colours plus their white/black tints delivers **the same mean chroma (35.3 vs 35.4), a
wider chroma spread (SD 20.2 vs 17.0), a bigger high-chroma tail (3.3% vs 2.1% above C\*
75) and 3.4× fewer regions (2,135 vs 7,194) with 3.4× less tiny-region area (21.0% vs
71.8%)**. It is better on every axis Fauvism cares about *and* on the paintability
invariant. `[verified — computed locally]`

**Three recommendations, in priority order** (detail in §8):

- **A. Masstone-biased candidate transform** — slot 3, ~90 lines including a share-aware
  predicate on `MixtureBuilder`. The evidence above.
- **B. Per-hue chroma ceiling** — slot 2, ~20 lines. Fixes a measured 0.76×–1.88× spread in
  a knob labelled 2.2. This is the abstract track's item 1, and my measurement both
  *confirms it is needed* and *corrects the table it was to be built from*.
- **C. Leave chroma at 2.2** and add a small-region merge in slot 5. The number is not the
  defect; the fragmentation is.

**Do not build:** per-region non-descriptive hue assignment, complementary shadow
assignment, a Fauve palette preset that names viridian or cobalt violet, or a lower chroma
number as the fix. §9.

---

## 1. The Fauves' actual palettes, and what this library holds

**Conclusion: three of the four pigments that define the Fauve look are missing from the
picker, and the one that hurts is green.**

### 1.1 What the conservation record says

The best-documented Fauve-period materials evidence:

| Work | Method | Pigments identified | Source |
|---|---|---|---|
| Matisse, *The Red Studio* (1911) | MA-XRF, FORS, SEM-EDS, Raman, SERS, µ-FTIR | **Whites:** lead white, zinc white, calcium carbonate. **Blacks:** bone black (hydroxyapatite), plant-derived carbon black. **Reds:** Venetian red (hematite earth + gypsum), red ochre, **vermilion (HgS)**, madder lake (alizarin + purpurin), eosin red lake. **Yellows:** **cadmium yellow (CdS)**, yellow ochre, aureolin/cobalt yellow (K₃[Co(NO₂)₆]), orpiment. **Greens:** **viridian (Cr₂O₃·2H₂O)**, chromium(III) oxide green. **Blues:** cobalt blue, ultramarine, Prussian blue. **Violets:** **cobalt violet light** (Co₃(AsO₄)₂·8H₂O), possibly manganese violet. | `[verified — fetched npj Heritage Science, "Exploring the private universe of Henri Matisse in The Red Studio", via the `?error=cookies_not_supported` redirect]` |
| Matisse, *Le Bonheur de vivre* (1905–06), Barnes Foundation | handheld XRF, multispectral imaging, Cd L(III) XANES at SSRL, SEM-EDS, SR-FTIR | Degradation confined to **cadmium yellow (CdS)**; altered paint contains CdCO₃, CdSO₄, CdC₂O₄ | `[relayed — Mass et al., *Applied Physics A* 111:59 (2013); Mass et al., *Analyst* 2013 (RSC). Read via abstracts and the ADS record; neither full text fetched.]` |
| Matisse, 1906 and early 1910s works | technical imaging + analysis | natural **madder** and **cochineal** lakes in the pink backgrounds, consistent across works | `[relayed — search summary of a Heritage Science survey; primary not fetched]` |

Two facts from *The Red Studio* study are operationally important and easy to miss:

- **Matisse mixed.** The study lists cobalt blue + lead white for the wall underlayer,
  natural ochre + lead white for the furniture, lead white + madder lake for the pink
  floor, **cadmium yellow + chromium oxide green for the deep vase green**, zinc white +
  earths for skin. Straight-from-the-tube application is noted specifically for *Young
  Sailor II* (sweater and cap), not as a general rule. `[verified]` The popular account
  that the Fauves painted only from the tube is an overstatement of a real tendency.
- **The Red Studio was blue, pink and ochre before it was red**, with viridian panelling
  covering roughly two-thirds of the surface, all overpainted with Venetian red.
  `[verified]` The famous flat red field is a *decision applied over* a conventionally
  coloured painting — which is the closest historical analogue to what a converter's
  candidate-restriction stage does.

Supporting, weaker: Matisse's palette is described across secondary sources as carbon
black, lead and zinc white, ultramarine, **viridian**, yellow ochre, burnt umber, madder
lake, cadmium yellow lemon, vermilion, **cobalt violet**, cobalt blue, cadmium orange and
strontium yellow `[relayed]`. I found **no technical study of Derain or Vlaminck** at all —
a deliberate search returned museum overview pages and general Raman-microscopy method
papers, nothing work-specific. `[verified — the absence, not the content]`

### 1.2 Mapping onto `Pigments/pigments.manifest.txt`

Only the 19 `TwoConstantMeasured` paints reach `PigmentLibrary.Selectable`. Every
`ReflectanceDerived` paint is withheld from the user. `[verified — read
`Pigments/PigmentLibrary.cs`]`

| Fauve pigment | Status in this app | Consequence |
|---|---|---|
| Cadmium yellow (CdS) | Cad Yellow Light/Medium/Dark are all **`ReflectanceDerived` — withheld**. Substitute: **Hansa Yellow Opaque (PY74)**, L\* 85.5 / C\* 106.4, or Bismuth Vanadate (PY184), L\* 91.7 / C\* 96.6 | Fine. The substitutes are *more* chromatic than the originals and sit at the right lightness. |
| Cadmium orange | **C.P. Cadmium Orange (PO20)** selectable, L\* 63.4 / C\* 99.9 | Exact. |
| Cadmium red | **C.P. Cadmium Red Light (PR108)** selectable, L\* 49.6 / C\* 89.2 | Exact. |
| Vermilion (HgS) | **Absent.** Nearest: Cad Red Light or Pyrrole Red (PR254, L\* 39.2 / C\* 84.7) | Acceptable — vermilion is an opaque orange-red and Cad Red Light occupies the same place. |
| **Viridian (Cr₂O₃·2H₂O)** | "Viridian Green Hue" is **`ReflectanceDerived` — withheld**. So are Chromium Oxide, Cobalt Green, Terre Verte, Perm Green Light, Cobalt Teal, Cobalt Titanate Green — **every green in the library except the two phthalos** | **This is the significant loss.** The user's only greens are Phthalo Green (B.S.) and (Y.S.), both far stronger tinters and far darker at masstone than viridian. They behave differently in mixture: a phthalo dominates a mix where viridian sits in it. |
| Emerald / Veronese green | **Absent entirely**, at any provenance tier | No substitute. |
| **Cobalt violet** | **Absent.** The only selectable violet is Dioxazine Purple (PV23), masstone L\* 13.5 / C\* 6.5 — effectively black | Serious. Cobalt violet is a light, chalky, moderate-chroma violet; dioxazine is a near-black staining pigment. Its **white tint** reaches C\* 52.6 at L\* 37.3, which is the only route to a violet in this library. |
| Cobalt blue, ultramarine, cerulean | **All three selectable**, exact (PB28, PB29, PB36) | The blues are the best-served part of the Fauve palette. |
| Prussian blue | "Prussian Blue Hue" **`ReflectanceDerived` — withheld**. Substitute: Phthalo Blue (R.S.) or (G.S.) | Reasonable — phthalo is the standard modern stand-in. |
| Madder / alizarin / carmine / cochineal lakes | Alizarin Crimson Hue **withheld**. Substitutes: Quinacridone Red (PV19), Quinacridone Magenta (PR122) | Good, and more lightfast. |
| Venetian red, red ochre, yellow ochre, burnt umber | **All `ReflectanceDerived` — withheld** | Matters more than it looks. Matisse's most radical canvas is built on Venetian red and ochre. A user cannot pick an earth at all; they must mix one. |
| Lead white, zinc white | Substitute: **Titanium White (PW6)** | A real behavioural difference: titanium is far more opaque and more strongly tinting, so tints made with it lighten faster and grey more. Not modellable here. |

**The named gaps that matter, in order: viridian and emerald green, then cobalt violet,
then the earths.** `[verified against the manifest]`

---

## 2. Is Fauvist colour high-chroma, or is it high-contrast and non-descriptive?

**Conclusion: on materials evidence, genuinely high-chroma. On measured colorimetry,
nobody has checked. And the defining innovation is a hue operation, not a chroma one — the
two claims are usually conflated and should not be.**

### 2.1 What the measurement literature actually covers

I searched specifically for colorimetry of Fauvist paintings and for any computational
comparison of colour distributions across movements. **There is none.** The pattern is the
same one the abstract track hit: every large-scale computational study of painting measures
*spatial* structure.

- **Sigaki, Perc & Ribeiro (2018), *PNAS* 115:E8585–E8594** — ~140,000 paintings, 1031–2016,
  >2,000 artists, ~100 styles, permutation entropy and statistical complexity from local
  spatial ordering patterns. **Fauvism is placed**: it clusters with Impressionism and
  Pointillism in the region of *highest entropy and lowest complexity*, which the authors
  attribute to "smudged and diffuse brushstrokes, and also by blending colors to avoid the
  creation of sharp edges." Crucially, the method **averages the three colour channels to
  greyscale before analysis**, so it is silent on colour. Style prediction from H and C
  alone over 20 styles reaches **~18% accuracy**, which the authors themselves call "quite
  modest for practical applications." `[verified — fetched PMC6140488]`
- Kim, Son & Jeong (2014), *Sci Rep* 4:7370 — box-counting dimension in RGB space, by
  period not by movement; no Fauvism figure. `[relayed via report 01]`
- Graham & Field, Redies — amplitude-spectrum slope and edge-orientation entropy; spatial.
  `[verified via report 02]`
- Hasler & Süsstrunk (2003) give a validated *colourfulness* metric (a linear combination
  of the mean and standard deviation of the pixel cloud in the CIELAB chromatic plane) —
  the right instrument for this question. **I found nobody who has run it across art
  movements.** `[verified — the metric; the absence of the study]`

**So the claim "Fauvist paintings are more saturated than Post-Impressionist ones" is not
refuted; it has never been tested.** Treat any statement of it, including in this app's
UI, as unverified. `[inferred]`

Two further reasons to be careful about any future measurement:

- **The paintings have changed colour.** The cadmium yellows in *Le Bonheur de vivre* have
  lightened, darkened and flaked, and the degradation is confirmed by XANES and SR-FTIR
  `[relayed]`. Eosin lake and madder are fugitive. Any colorimetry of a 1905 Fauve canvas
  measures a 121-year-old object, not the exhibited painting.
- Reproductions are the only accessible source and are colorimetrically unreliable, which
  is the same warning the abstract track raised for Rothko.

### 2.2 The materials argument, which is stronger than the measurement argument

The Fauve palette **is** the highest-chroma pigment set available in 1905: cadmium
yellow/orange/red, vermilion, viridian, emerald, cobalt violet, cobalt and ultramarine
blue. These are precisely the pigments a painter buys when they want intensity, and
Matisse is documented as adopting modern synthetics as they appeared `[relayed]`. Applied
at or near full strength — which *The Red Studio* study confirms for at least some
passages `[verified]` — that produces genuinely high chroma. `[inferred, from verified
materials]`

**But note what that argument does and does not license.** It licenses "a Fauve canvas
carries colours near the top of the available gamut." It does **not** license "multiply the
photograph's chroma by 2.2." Those are different operations: the first restricts the
achievable set upward toward the tube, the second scales whatever the photograph happened to
contain. §4 shows the app is doing the second and getting neither.

### 2.3 The part that is definitely not chroma

Matisse's own account undercuts the "wild colour" reading. *Notes of a Painter* (1908):
"Expression, for me, does not reside in passions glowing in a human face or manifested by
violent movement. **The entire arrangement of my picture is expressive**: the place occupied
by the figures, the empty spaces around them, the proportions, everything has its share";
and "From the relationship I have found in all the tones there must result a **living
harmony of colors**, a harmony analogous to that of a musical composition." `[relayed —
widely reprinted; I read transcriptions, not the 1908 *La Grande Revue* original]`

That is a statement about *relations and quantities*, not about intensity. The
Fauve-defining works support it: *The Green Stripe* (1905) works by dividing a face into a
cool and a warm half either side of a green band, replacing tonal modelling with a
chromatic division — an operation on **hue and temperature at roughly constant value**,
with the green stripe standing in for a shadow line `[relayed — museum and secondary
analyses; no technical study of this painting located]`.

**Reading for the app: Fauvism's signature is (a) large flat areas of near-tube colour, (b)
hue assigned by design rather than by observation, (c) tonal modelling replaced by chromatic
division. A scalar chroma multiplier addresses none of the three.** `[inferred]`

---

## 3. The chroma ceiling, corrected — the most load-bearing measurement in this report

**Conclusion: the abstract track's per-hue ceiling table is a masstone table, and masstone
is not the ceiling. Recomputed from the actual achievable candidate set, this library has
no empty hue sector, and green reaches C\* 86–89.**

### 3.1 Tints beat masstones for most selectable paints

Measured this session: each selectable paint alone, then sampled along its Titanium White
line at 64 steps, taking the maximum C\* anywhere on that line. `[verified — computed
locally]`

| Paint | Masstone L\* | Masstone C\* | Best C\* on the white line | at L\* | at white share |
|---|---|---|---|---|---|
| Dioxazine Purple | 13.6 | **6.5** | **52.6** | 37.3 | 0.625 |
| Phthalo Green (Y.S.) | 18.5 | **18.9** | **56.3** | 75.6 | 0.703 |
| Phthalo Green (B.S.) | 13.6 | 13.3 | **43.4** | 68.3 | 0.797 |
| Phthalo Blue (R.S.) | 9.6 | 27.4 | **46.1** | 53.2 | 0.656 |
| Ultramarine Blue | 7.8 | 58.5 | **76.4** | 24.6 | 0.047 |
| Quinacridone Magenta | 25.8 | 49.2 | **59.3** | 45.3 | 0.281 |
| Phthalo Blue (G.S.) | 10.5 | 40.8 | 47.3 | 17.4 | 0.031 |
| Cerulean Blue Chromium | 33.7 | 30.6 | 39.7 | 50.0 | 0.094 |
| Pyrrole Orange | 54.5 | 89.4 | 92.9 | 55.6 | 0.031 |
| Bismuth Vanadate Yellow | 91.7 | 91.3 | 92.0 | 92.5 | 0.078 |
| Bone Black | 11.2 | 1.5 | 4.2 | 47.3 | 0.297 |
| Hansa Yellow Opaque, Diarylide Yellow, C.P. Cad Orange, C.P. Cad Red Light, Pyrrole Red, Quinacridone Red, Cobalt Blue | — | — | *masstone is the maximum* | — | 0.000 |

**Thirteen of eighteen chromatic paints peak in tint, not at masstone.** The pattern is
exactly what a colourist would predict: opaque, high-scattering pigments (cadmiums, hansa
yellows, cobalt blue, quinacridone red) top out at full strength; dark, transparent,
high-absorption pigments (dioxazines, phthalos, ultramarine) are so dark at masstone that
the eye — and CIELAB — reads them as near-black, and letting them down with white is what
reveals their hue. Two-constant Kubelka-Munk reproduces this correctly because it tracks
scattering separately, which is precisely the property `CLAUDE.md` says single-constant
theory would lose. `[verified — measured; mechanism `[inferred]` but standard]`

**The abstract track's claim that "Kubelka-Munk mixing always lands below both parents in
chroma, so mixing can only reduce chroma; masstones are the chroma ceiling" is therefore
false.** It was marked `[inferred]` there. Measured over all 171 selectable pairs at 31
ratios (5,301 mixtures): **344 mixtures (6.49%) exceed max(parent C\*) by more than 0.5**,
worst excess **46.11** (Titanium White + Dioxazine Purple at 38% purple: mix C\* 52.6
against parents 0.6 and 6.5). 61.5% exceed min(parent C\*). `[verified — computed locally]`

### 3.2 The real per-hue ceiling, from the candidate set

Built from all 19 selectable paints through `MixtureBuilder` — every paint alone, every
pair across its mixing line at 63 samples, every triple across its simplex at 16 divisions
— **84,063 distinct candidates, `AchievableMaxChroma` 92.86**. Maximum C\* per 15° CIELAB
hue sector: `[verified — computed locally 2026-07-28]`

| Sector | n | max C\* | at L\* | colour | | Sector | n | max C\* | at L\* | colour |
|---|---|---|---|---|---|---|---|---|---|---|
| 0–15 | 2,736 | 62.28 | 43.3 | `#C0294F` | | 180–195 | 1,688 | 43.78 | 67.2 | `#00B8A3` |
| 15–30 | 5,502 | 75.69 | 41.3 | `#C40728` | | 195–210 | 1,218 | 38.97 | 68.3 | `#00B9BC` |
| 30–45 | 13,103 | 91.79 | 53.0 | `#EC3C00` | | 210–225 | 1,041 | 36.48 | 67.8 | `#1AB6C6` |
| 45–60 | 8,486 | **92.86** | 55.6 | `#F44500` | | 225–240 | 1,092 | **35.07** | 59.0 | `#0E9AC0` |
| 60–75 | 4,421 | 85.37 | 67.3 | `#FF8100` | | 240–255 | 1,580 | 43.38 | 62.4 | `#00A1E0` |
| 75–90 | 3,277 | 86.26 | 85.7 | `#FFD200` | | 255–270 | 2,370 | 49.34 | 53.9 | `#0086D6` |
| 90–105 | 3,133 | 92.01 | 92.5 | `#FFED00` | | 270–285 | 2,768 | 59.20 | 45.8 | `#246ACD` |
| 105–120 | 3,069 | 89.44 | 82.4 | `#ACDE09` | | 285–300 | 4,510 | 74.78 | 29.1 | `#2335AC` |
| 120–135 | 3,249 | 88.28 | 79.2 | `#92D819` | | 300–315 | 5,497 | 77.35 | 16.8 | `#1B0D87` |
| 135–150 | 3,856 | 86.79 | 70.4 | `#42C52E` | | 315–330 | 2,064 | 48.50 | 40.4 | `#8D4389` |
| 150–165 | 3,292 | 71.04 | 73.3 | `#18CE72` | | 330–345 | 1,843 | 57.86 | 49.2 | `#C04790` |
| 165–180 | 2,278 | 56.32 | 75.6 | `#00D3A2` | | 345–360 | 1,990 | 59.31 | 45.3 | `#B83984` |

**Read against the abstract track's table:**

| Sector | Abstract track (masstone) | This report (candidate set) | Ratio |
|---|---|---|---|
| 120–150° | **"nothing"** | **86.07–88.28** | ∞ |
| 150–180° | 31.9 at L\* 18.9 | **71.04 at L\* 73.3** | 2.2× |
| 180–210° | **"empty"** | 43.78 at L\* 67.2 | ∞ |
| 210–240° | 15.5 | 36.48 | 2.4× |
| 330–360° | **"empty across all 80 paints"** | 59.31 at L\* 45.3 | ∞ |
| 0–30° | 61.3 | 75.69 | 1.2× |

The narrowest place in the gamut is not green — it is **cyan at 225–240°, C\* 35.07** —
and even that is 2.3× the abstract track's figure. The ceiling ranges 35 to 93, a factor of
**2.6**, not the factor of 3.3 the masstone table suggested, and it is nowhere zero.

**Two caveats, both of which make the table conservative rather than optimistic:**

- **25.0% of pair mixtures (641 of 2,565 sampled) lose chroma to the sRGB gamut** on the way
  to a display colour `[verified — `SpectralRenderer.ToDisplayColor`'s `chromaLost` output].
  Gamut mapping only *reduces* chroma, so the true paint chroma is at or above every figure
  above. This is consistent with the prior research's relayed figure that 31% of achievable
  Golden acrylic colours fall outside sRGB.
- The candidate set is the *achievable* set. Every entry above is a real mixture of real
  paints, so nothing here weakens the converter's invariant.

### 3.3 Where the greens come from

Traced directly. Hansa Yellow Opaque + Phthalo Green (Y.S.): `[verified — computed
locally]`

| green share | L\* | C\* | h |
|---|---|---|---|
| 0.000 | 85.6 | 86.1 | 89.9 |
| 0.062 | 66.5 | 82.4 | 125.5 |
| 0.125 | 61.7 | 84.5 | 132.2 |
| **0.188** | **58.6** | **85.4** | **136.4** |
| 0.250 | 55.7 | 74.4 | 140.4 |
| 0.500 | 47.4 | 54.0 | 149.1 |
| 1.000 | 18.5 | 18.9 | 176.6 |

At 19% phthalo green the mixture is a **mid-value green at C\* 85** — 4.5× the green
parent's masstone chroma and essentially equal to the yellow parent's, at a hue 47° away
from it. That is a Fauve green, and it costs one small squeeze of phthalo into a large pile
of yellow.

**And it does not even require a green paint.** Hansa Yellow + Ultramarine, the golden
palette's only route to green: `[verified]`

| blue share | L\* | C\* | h |
|---|---|---|---|
| 0.125 | 52.4 | 59.9 | 120.7 |
| 0.250 | 43.7 | 50.2 | 132.1 |
| 0.500 | 32.7 | 39.1 | 149.6 |

C\* 59.9 at L\* 52.4 — a fully usable leaf green from two paints neither of which is green.
The abstract track's "a Fauve or abstract green is not available" is wrong by a factor of
between 2 and 4.5 depending on palette. `[verified]`

### 3.4 Which Fauvist oppositions survive

The brief's central practical question. Best achievable chroma on each side of a red/green
opposition, at matched lightness, over the 19-paint candidate set (red taken as h ∈
[340°, 40°], green as h ∈ [120°, 180°], both within ±5 L\*): `[verified — computed locally]`

| Matched L\* | Best red C\* | Best green C\* | Verdict |
|---|---|---|---|
| 30 | **72.1** `#A80510` | 51.3 `#00601A` | Red-dominant. A dark green cannot answer a dark red. |
| 45 | **87.8** `#CD0702` | 71.9 `#10880F` | Workable. 16 C\* asymmetry, favouring red. |
| **60** | 81.8 `#F74E31` | **86.1** `#2DAC04` | **The best opposition available. Both above C\* 80, green marginally stronger.** |
| 75 | 53.6 `#FF8B70` | **88.3** `#92D819` | Green-dominant. A light red is a pink and cannot answer. |

**The red–green opposition survives, and it lives at L\* 55–65.** Above that the red fails;
below it the green fails. That is a genuinely actionable constraint: a Fauvist red/green
passage in this app must be built at mid-to-light value, not in shadow.

By the same construction the **orange–blue** opposition is the strongest in the library
(orange 92.9 at L\* 55.6, blue 74.8 at L\* 29.1) but is badly value-asymmetric — the blues
are all dark. **Yellow–violet is the weakest**: yellow reaches 92–106 but the only violets
are the dioxazine tint (52.6 at L\* 37.3) and the 315–330° candidates at 48.5. A Fauve
yellow–violet chord is not available at parity.

---

## 4. What chroma 2.2 actually does — measured on the shipped output

**Conclusion: it delivers roughly a 2× mean chroma lift on a warm source and nothing at all
in green-cyan, and it does so by more than doubling the region count and quadrupling the
unpaintable fraction.**

### 4.1 The committed goldens

All five goldens render the same 128×128 noisy gradient through the six-paint golden palette
at mark 4. `[verified — computed locally from `Tests/Golden/*.png`]`

| | distinct colours | mean L\* | mean C\* | SD C\* | p90 C\* | max C\* | % above C\* 60 | % above C\* 75 | regions | % pixels in regions ≤16 px |
|---|---|---|---|---|---|---|---|---|---|---|
| **Realism** | 161 | 60.0 | 17.0 | 9.9 | 29.4 | 54.3 | 0.0 | 0.0 | 425 | **5.4** |
| **Fauvism** | **331** | 63.4 | **35.1** | 17.0 | 59.5 | 88.1 | **9.2** | 2.1 | **1,035** | **21.1** |
| **Abstract** | 322 | 66.0 | 24.4 | 13.1 | 42.0 | 64.9 | 1.6 | 0.0 | 685 | 14.6 |

Three readings:

- **Mean chroma doubles (17.0 → 35.1) and the spread rises too (9.9 → 17.0).** The abstract
  track's worry that the tanh knee compresses the chroma distribution — the statistic with
  the only positive empirical support — **is not borne out at gain 2.2 on this source**. The
  knee weight is `(2.2 − 1)/(3 − 1) = 0.6`, so 40% of the transform is still a plain linear
  multiplier, and for photographic input chroma the linear term dominates. Correction to
  ../abstract/04, §3.2 item 2. `[verified]`
- **90.8% of the render still sits below C\* 60**, and 97.9% below C\* 75. The palette's
  chromatic masstones are at C\* 49, 58, 86 and 89. **The Fauvism render is almost entirely
  below its own palette's tube colours.** Whatever "Fauvist" means, it is not this.
  `[verified]`
- **The cost is the mark invariant.** 1,035 regions against Realism's 425, and 21.1% of
  pixels in regions at or below mark² = 16 px, against 5.4%. Fauvism is the most fragmented
  of the five committed goldens by a wide margin. Given the design's second invariant —
  "every output region must be a mark a human could execute" — **the shipped Fauvism is the
  style that violates it hardest.** `[verified]`

### 4.2 The realised gain is hue-dependent by a factor of 2.5

The golden source contains **no pixels between 150° and 270°**, so it cannot show the
failure the `ToneAndChromaRemap` doc comment warns about. I re-ran ask-versus-deliver on a
360×360 full-hue-circle source (HSV S = 0.55, V ramped 0.25→0.85), six-paint golden palette,
Fauvism's own settings, comparing the scalar `AchievableMaxChroma` (89.3) against a 12-bin
per-hue ceiling built from the same candidate set: `[verified — computed locally]`

| source hue | n | source C\* | ask (scalar ceiling) | ask (per-hue ceiling) | **delivered** | per-hue ceiling | **realised gain** |
|---|---|---|---|---|---|---|---|
| 0–30 | 8,892 | 35.2 | 67.4 | 63.1 | 56.5 | 68.1 | 1.60× |
| 30–60 | 7,451 | 31.3 | 61.4 | 61.4 | 58.7 | 89.3 | **1.88×** |
| 60–90 | 7,117 | 30.8 | 60.6 | 60.2 | 56.0 | 86.1 | 1.82× |
| 90–120 | 12,620 | 40.1 | 74.5 | 73.2 | 63.4 | 83.2 | 1.58× |
| 120–150 | 21,526 | 47.2 | 84.2 | 74.4 | 58.0 | 59.9 | 1.23× |
| 150–180 | 10,849 | 33.6 | 64.9 | 51.1 | 42.6 | 38.5 | 1.27× |
| **180–210** | 6,130 | 24.6 | 50.1 | 32.8 | **18.8** | 18.9 | **0.76×** |
| 210–240 | 4,346 | 21.0 | 43.6 | 28.8 | 19.7 | 17.6 | 0.94× |
| 240–270 | 4,999 | 23.3 | 47.8 | 34.5 | 34.8 | 24.7 | 1.49× |
| 270–300 | 11,636 | 38.4 | 71.8 | 68.5 | 61.4 | 74.0 | 1.60× |
| 300–330 | 21,842 | 49.5 | 87.2 | 83.2 | 71.3 | 76.4 | 1.44× |
| 330–360 | 12,192 | 40.9 | 75.7 | 67.3 | 54.4 | 59.3 | 1.33× |

**A knob labelled 2.2 produces between 0.76× and 1.88×.** In five sectors (120–240°,
330–360°) the scalar ceiling over-asks by 21–31 C\* units against what the palette can
deliver — the exact "many distinct colours land on the same few boundary candidates"
condition `RenderContext.AchievableMaxChroma`'s doc comment describes. At 180–210° the
output is *less* chromatic than the input. `[verified]`

Repeating with a green-bearing palette (White, Hansa Yellow, Pyrrole Orange, Cad Red Light,
Cobalt Blue, Phthalo Green Y.S.) closes most of the green shortfall — 120–150° delivers 73.7
instead of 58.0, 180–210° delivers 36.6 instead of 18.8 — and opens a new one at 330–360°,
where the shortfall grows to 27.3 because that palette has no magenta and targets there
**drift out of the sector entirely** (delivered C\* 49.1 against a 330–360° candidate ceiling
of 21.9). `[verified]` **Hue drift is a real, measurable failure of the current
arrangement**, and it is palette-dependent, so no fixed table can pre-empt it — the ceiling
must be built from the *user's* candidate set, which is exactly what the abstract track's
item 1 proposes.

---

## 5. Colour freed from description — can a converter do hue displacement at all?

**Conclusion: global rotation, yes and cheaply, but the evidence says it is not an
improvement. Per-region non-descriptive substitution — the actual Fauvist move — is not
available to this app and should not be attempted.**

### 5.1 What the rotation studies say

The parent and abstract reports already establish the three studies; the Fauvism-specific
reading is what matters here.

- **Nakauchi & Tamura (2022), *Sci Rep* 12:14367** — 1,200 paintings, 31,353 participants,
  4AFC among 0°/90°/180°/270° CIELAB hue rotations. Originals selected above every rotation,
  p < 0.001. Genre mattered, η²p = 0.42, ordering `abstract < poster = symbolic < flowers =
  still life`. `[verified via ../abstract/04]`
- **Nascimento et al. (2017), *Vision Research* 130:76–84** — free continuous rotation,
  preference maxima deviated only ~7° from the original gamut orientation. `[relayed via
  ../abstract/04; that report could not fetch it either — see §10]`
- **Nascimento et al. (2022), *Sci Rep* 12:4294** — the preference survives scrambling and
  patchworking, so it is carried by colour statistics rather than composition. `[verified
  via ../abstract/04]`

**The Fauvism reading:** the cost of rotation is smallest where colour is least tied to
depicted objects. Fauvism is representational — landscapes, portraits, harbours — so it sits
at the *expensive* end of Nakauchi's ordering, not the cheap end where abstraction sits.
A hue rotation applied to a Fauvist conversion is therefore worse supported than the same
rotation applied to an abstract one. `[inferred, from verified inputs]`

### 5.2 The memory-colour objection, which is decisive for photographs

A photograph of a real scene contains **skin, sky and foliage**, and these three are the
most studied and least tolerant colours in reproduction. Memory colours are recalled *more
saturated* than natural ones and with **high hue constancy** — grass is remembered greener,
brick redder — so a hue displacement moves them off a remembered anchor rather than off an
arbitrary one. Blue sky additionally sits in the region of CIELAB with a known hue
inconstancy problem, which is why sky reproduction work is routinely moved into CIECAM02.
`[relayed — Tian et al. (2022), *Color Research & Application* 47, image-naturalness model
from prototypical memory colours; plus the memory-colour literature summarised in search
results. I did not fetch the primary.]`

**The asymmetry that follows is the useful part.** A *global* rotation moves every memory
colour together, which the visual system partly absorbs as an illuminant shift — this is why
the rotation studies find the images remain legible. A *per-region* substitution moves one
memory colour and leaves its neighbours, which reads as an error rather than as a style,
because there is no illuminant that could produce it. `[inferred]`

### 5.3 What principled non-descriptive hue assignment would need, and why it is out of reach

Fauvism's actual operation is *object-conditional*: this face is green, that road is pink.
Reproducing it requires knowing what a region depicts. The options, and why each fails here:

| Option | Slot | Verdict |
|---|---|---|
| **Global hue rotation θ** | 2 (`ILabRemap`), 1 line | Works, cheap, gamut-safe, per-pixel safe. But §5.1 says originals are preferred, and Fauvism is at the expensive end of that ordering. Defensible only as an explicitly labelled arbitrary control, default 0°. |
| **Per-region hue substitution** | needs a segmenter in slot 1, then a slot-2 map keyed on region | The honest Fauvist operation. Requires semantic labels (face, road, sky) the app has no source for. Region *geometry* is obtainable — the abstract track's connected-component work gives it — but geometry without semantics assigns hues at random, which §5.2 says reads as breakage. **Not buildable.** |
| **Complementary shadow assignment** | 2, keyed on L\* | Sounds like the Fauve move and is not. See §6.2. |
| **Hue snapping to achievable anchors** | 3 (`ICandidateTransform`) | Real, gamut-safe, and subsumed by the masstone restriction in §7 — restricting to tube colours *is* a hue quantisation, with anchors that are achievable by construction. Prefer that. |
| **Temperature split by value** (report 01, lever 5) | 2 | The closest legitimate approximation of *The Green Stripe*'s warm/cool division: add `t·(L*−pivot)/50` along a warm-axis direction in a\*b\*. Hue moves, but continuously and monotonically with value, so it reads as light rather than as error. **This is the hue lever Fauvism should get.** `[inferred]` |

**Bottom line for the brief's question:** yes, a converter can do hue displacement without
noise — provided the displacement is a *smooth function of something the pixel already
carries* (its own hue, or its lightness). It cannot do the object-conditional version, and
attempts to fake object-conditionality from geometry alone will produce noise. `[inferred]`

---

## 6. Simultaneous contrast and complementary pairing

**Conclusion: the perception literature contradicts the complementary scheme specifically
and by name, including the red–green pair Fauvism is built on. But it contradicts it as a
claim about *harmony*, which is not what the Fauves were claiming.**

### 6.1 What Schloss & Palmer found, restated against Fauvism

Schloss & Palmer (2011), *Attention, Perception & Psychophysics* 73:551–571, 1,431 colour
pairs from 54 systematically sampled CIELAB colours: harmony and preference both peak at
*identical* hue and fall monotonically with hue difference; complementary pairs rated
reliably *less* harmonious, F(1,47) = 17.67, p < .001; only orange–blue exceeded adjacent
pairs on preference, F(1,47) = 11.17, p < .008. `[verified via ../01]` The Palmer Lab's own
summary of the same data puts it in the strongest available form: **"There is no evidence
for Chevreul's harmonies of contrast, because there are no reliable increases in the
functions at opposite hues (e.g., red and green)."** `[relayed — Palmer Lab summary page;
the page I fetched directly did not carry that sentence, so it comes from a search-index
excerpt and needs re-checking. Flagged in §10.]`

The parent README already draws the conclusion that complementary harmony should not be the
app's default. **The Fauvism-specific extension is that this does not damage Fauvism as much
as it looks.** Schloss & Palmer measured *harmony* and *preference* for isolated colour
pairs. The Fauves were not pursuing harmony in that sense — Matisse's "living harmony of
colors" is a property he attributes to the relations across a whole picture, and the
movement's contemporary reception was that the pictures were *unpleasant*. A style preset
whose historical warrant is "this shocked the 1905 Salon" is not undermined by evidence that
viewers find complementary pairs less harmonious. `[inferred]`

**What the finding does rule out is using complementarity as a mechanism**: do not build a
stage that pairs a region with its hue complement on the theory that this produces a better
picture. There is no evidence it does, and there is direct evidence against.

### 6.2 The Neo-Impressionist inheritance, and where it goes wrong

The Fauves' complementary practice descends from Signac's *D'Eugène Delacroix au
néo-impressionnisme* (1899), which Matisse absorbed directly — he worked alongside Signac at
Saint-Tropez in 1904 and painted *Luxe, calme et volupté* (1904–05) in divided touches
before Collioure. `[relayed — consistent across Musée de Grenoble, Centre Pompidou and
Universalis summaries; I did not read Signac's text]` The commentary is unanimous that what
the Fauves took from the book was **the primacy of colour, not the divided stroke** —
several sources note readers found in it "an apology for colour itself" rather than for
divisionism. `[relayed]`

That matters because it means **the Fauves inherited Chevreul's *simultaneous contrast*
without inheriting the optical-mixing apparatus that was supposed to justify it** — and
report 01 §8 has already established that the optical-mixing justification was itself wrong
(partitive mixing averages luminance; it cannot brighten). `[verified via ../01]`

**On coloured shadows specifically**, the folklore and the physics diverge:

- The *measured* colour of a cast shadow outdoors is bluish, from Rayleigh-scattered
  skylight filling the shadow. `[relayed; the λ⁻⁴ mechanism is `[verified]` via ../01 §4.3]`
- The *perceived* complementary tint in a shadow is chromatic induction from the surround,
  approximately complementary to the **illuminant** — not to the local surface colour.
  `[relayed — the standard account; I read only summaries]`

So "the shadow is the complement of the light" is roughly right and "the shadow is the
complement of the object" is wrong. A stage that assigned each region's shadow the
complement of that region's own hue would be modelling the wrong thing. **Report 01's
lever 5 — a single warm/cool axis split by lightness, with the axis direction exposed as a
parameter — is the correct form of this idea and already covers it.** `[inferred]`

### 6.3 Simultaneous contrast is free and gets stronger as the palette narrows

The abstract track's point carries over verbatim and is *more* applicable to Fauvism, because
induction magnitude grows with target–surround difference and with surround saturation
`[relayed via ../01 §7.2]`. A restricted, high-chroma, large-flat-region output is the
configuration that maximises induction. **Build the configuration and let the eye do the
work.** Do not pre-compensate — report 01 found no agreed induction magnitude and large
individual variability.

---

## 7. Does Fauvism need a masstone-only mode more than Abstract did?

**Conclusion: yes, and the case is much stronger than the abstract track could make it,
because I measured it. At identical settings it delivers the same mean chroma, a wider
spread, a bigger high-chroma tail, and 3.4× fewer regions.**

### 7.1 The measurement

Golden source (128×128 noisy gradient), golden six-paint palette, `ToneAndChromaRemap` at
Fauvism's contrast 1.35, nearest-candidate in plain squared CIELAB. **The
`EdgePreservingFloor` is omitted from all four rows**, so these are comparable to each other
but not to §4.1's committed goldens — the floor is doing a lot of the region work in the
shipped output. `[verified — computed locally]`

| candidate set | chroma gain | distinct | mean C\* | SD C\* | p90 C\* | % > C\* 75 | regions | % in regions ≤16 px |
|---|---|---|---|---|---|---|---|---|
| full mixture set (3,007) | 2.2 | 485 | 35.4 | 17.0 | 59.5 | 2.1 | 7,194 | 71.8 |
| full mixture set (3,007) | 1.0 | 326 | 17.2 | 10.5 | 29.9 | 0.0 | 3,508 | 34.4 |
| **masstone-only (141)** | **2.2** | **65** | **35.3** | **20.2** | 58.5 | **3.3** | **2,135** | **21.0** |
| masstone-only (141) | 1.0 | 48 | 13.9 | 15.2 | 37.9 | 0.0 | 1,273 | 11.4 |

"Masstone-only" here means: each paint alone, plus its 16-step mixing line with the
palette's white and with its black — 141 candidates from 3,007.

**Every comparison favours it.** Same mean chroma (35.3 vs 35.4). Higher SD (20.2 vs 17.0)
— the statistic with the only positive empirical support in the aesthetics literature
`[verified via ../abstract/04 §3.1]`. Bigger high-chroma tail (3.3% vs 2.1% above C\* 75).
**7.5× fewer distinct colours, 3.4× fewer regions, 3.4× less tiny-region area.** And the
recipe becomes executable: "paint A, straight from the tube" or "paint A plus n parts
white", which is exactly what a hobbyist can follow and exactly what the conservation record
says Matisse did for at least some passages `[verified]`.

The one caveat: at gain 1.0 masstone-only *lowers* mean chroma (13.9 vs 17.2), because a
141-candidate set has few mid-chroma near-neutrals for a muted target to land on, so muted
pixels resolve either to a near-neutral tint or jump to a saturated one. **That polarisation
is the point** — it is what raises the SD — but it means the mode wants a high chroma gain
paired with it, not a low one.

### 7.2 What it costs to build

`MixtureBuilder.KeepOnly` takes `Func<double, double, double, bool>` over L\*a\*b\* only. A
masstone predicate is about *shares*, not colour: keep a candidate iff at most one of its
paints is chromatic, i.e. every other paint in the mixture is the palette's white or its
most-neutral member. That needs a second, share-aware overload —
`KeepOnlyMixtures(Func<int[], double[], bool>)` — evaluated inside `RenderMixture` before
the colour is even computed, which is cheaper than the existing post-hoc filter and cannot
be approximated safely in Lab (a chromatic-plus-chromatic mixture can land arbitrarily close
to a tint line, as §3.1's tint data shows).

Note `KeepOnly`'s documented safety valve: if the predicate empties the set, `Build` returns
the unfiltered set. A masstone predicate can never empty it — the single-paint entries always
survive — so the valve stays unarmed. `[verified — read `MixtureBuilder.cs`]`

**A softer variant worth considering**: rather than a hard filter, keep every candidate but
*bias* selection toward masstones by subtracting a chroma bonus in the quantiser. That would
sit in slot 4, which is where positional operations must not go — but a chroma bonus is not
positional, so it is legal. It avoids the polarisation caveat above at the cost of not
producing an executable recipe. I did not measure it. `[inferred]`

---

## 8. Three recommendations

### A. Masstone-biased candidate transform — **build this first**

- **Slot 3** (`ICandidateTransform`), plus a share-aware overload on `MixtureBuilder`.
- **Cost:** ~90 lines. ~40 for the stage and its parameter, ~30 for
  `KeepOnlyMixtures(Func<int[], double[], bool>)` and its wiring into `RenderMixture`, ~20
  for the "is this paint the palette's white/neutral" determination (reuse
  `MostNeutralPaintIndex`, and add a lightest-paint equivalent).
- **Parameter:** one slider, "purity" ∈ [0, 1], interpolating from the full set to
  masstones-plus-tints by admitting mixtures with at most *k* chromatic paints, k = 3 → 2 →
  1. That makes 0.0 an exact no-op and keeps the stage's identity guarantee.
- **Evidence:** §7.1 measured head-to-head — same mean chroma, higher SD, bigger high-chroma
  tail, 3.4× fewer regions. §1.1's conservation record for tube application. §3.1's
  demonstration that tints, not masstones, hold the chroma in this library, which is why the
  predicate must admit the white line rather than single paints alone.
- **Invariants:** gamut safe by construction (slot 3 acts before any colour exists); mark
  invariant strictly helped, measured.
- **Verification:** pin the four rows of §7.1's table as a test — distinct-colour count,
  mean C\*, SD C\* and `PaintabilityMetrics.CountRegions` — with tolerances. All four are
  numeric properties, which is what `CLAUDE.md` asks for over "does not throw". Add a test
  that every surviving candidate's share vector has at most one chromatic paint. Regenerate
  `Tests/Golden/Fauvism.png` and look at it.
- **Confidence: high** that it looks more Fauve; **high** that it improves paintability;
  **high** that it is cheap.

### B. Per-hue chroma ceiling, built from the candidate set

- **Slot 2**. Replace `RenderContext.AchievableMaxChroma` (a scalar) with a small per-hue
  lookup — **24 bins of 15°**, matching the resolution at which §3.2's table is stable —
  computed in `StylePipeline.MaximumChroma` from the same single pass that already scans
  every candidate.
- **Cost:** ~20 lines, plus changing one `double` on `RenderContext` to a
  `IReadOnlyList<double>` with an accessor, plus updating `RenderContextTests` and
  `ToneAndChromaRemapTests`.
- **Evidence:** §4.2 — a knob labelled 2.2 realises 0.76× to 1.88×; the scalar ceiling
  over-asks by 21–31 C\* in five of twelve sectors; and with a green-bearing palette the
  failure moves to magenta, so no fixed table works and it must come from the user's own
  candidate set. This is the abstract track's item 1, **but do not build it from
  `pigments.manifest.txt`** — §3.2 shows the masstone table understates the candidate-set
  ceiling by up to 2.4× and reports three sectors as empty that are not.
- **Two implementation notes.** Bin by the *source* hue, since `ScaleChroma` preserves hue
  by construction. Interpolate between adjacent bins rather than stepping, or the ceiling
  itself becomes a source of banding at bin boundaries. Empty bins (possible with a
  two-paint palette) should fall back to the neighbouring non-empty bins' minimum, not to
  the global maximum.
- **Verification:** re-run §4.2's ask-versus-deliver table as a test on a synthetic
  full-hue-circle source and assert the per-sector shortfall drops below, say, 15 C\* in
  every sector. That is a direct measurement of the defect being fixed, not a proxy.
- **Confidence: high** that it is a correctness fix; **medium** that a user notices it on a
  typical warm photograph; **high** that they notice it on a landscape.

### C. Keep chroma at 2.2; add small-region merge instead

- **The number is not the defect.** §4.1 shows chroma 2.2 delivering a 2.07× mean chroma
  lift with a *widening*, not narrowing, chroma distribution — so the abstract track's
  argument for lowering Abstract's 1.5 does not transfer. What it also shows is 1,035 regions
  and 21.1% of pixels in unpaintable regions, against Realism's 425 and 5.4%. **Fauvism is
  the style that breaks the mark invariant hardest, and it breaks it through fragmentation,
  not through chroma.**
- If a number must move, the honest recommendation is to **leave 2.2 and let recommendation
  A do the work**, because masstone restriction at gain 2.2 already produces the same mean
  chroma with 3.4× fewer regions. Only if A is not built would I lower 2.2 — and then to
  about **1.8**, which is where the linear term's contribution falls enough to bring the
  region count near Post-Impressionism's while keeping the style visibly distinct. That
  figure is interpolated from §7.1's gain-1.0 and gain-2.2 rows and is **`[inferred]`, not
  measured** — measure it before shipping it.
- **Slot 5**, `IPostMapStage`: flood-fill the index buffer, rewrite every region below
  `MarkPixels²` to its largest neighbour. ~100 lines on top of
  `PaintabilityMetrics.ForEachRegion`, which is already most of the flood fill. Index-in,
  index-out, so the gamut invariant is structurally untouchable. This is the abstract
  track's item 4 and three of its four tracks wanted the same labelling infrastructure —
  **it is shared, so its cost should be charged once, not to Fauvism.**
- **Verification:** assert `FractionInRegionsSmallerThan(..., MarkPixels²)` is below a
  threshold for every registered style, which turns the mark invariant from a measurement
  into a gate.
- **Confidence: high** that fragmentation is the real defect; **medium** on the 1.8 figure.

---

## 9. What not to build

Each of these sounds right for Fauvism and does not survive the evidence or the physics.
The parent and abstract "what not to build" lists still apply; these are additional or
Fauvism-specific.

- **Per-region non-descriptive hue substitution — the green face, the pink road.** This is
  the movement's actual innovation and it is the one thing this app cannot do. It requires
  object semantics; geometry alone assigns hues at random, and §5.2's memory-colour evidence
  says a randomly displaced skin or foliage hue reads as breakage rather than as style. Do
  not approximate it with region-mean hue jitter. `[inferred, from `[relayed]` memory-colour
  work]`
- **Complementary shadow assignment.** The perceived complementary tint in a shadow is
  induction from the **illuminant**, and the measured shadow colour is bluish from skylight
  — neither is the complement of the object's own hue. `[relayed]` Report 01's lever 5
  (warm/cool split by lightness on a parameterised axis) is the correct form and already
  exists as a design.
- **Complementary hue pairing as a mechanism.** Schloss & Palmer's 1,431 pairs contradict it
  by name, including red–green. `[verified via ../01; the Palmer Lab's blunt phrasing is
  `[relayed]` and needs re-checking]` Fauvism does not *need* harmony, but it also gains
  nothing from a stage that manufactures complements.
- **A "Fauve palette" preset naming viridian, emerald green or cobalt violet.** None is
  selectable; two are absent from the library at every provenance tier. A preset that names
  pigments the picker cannot supply is a broken promise. If a preset ships, build it from
  what §1.2 says is actually reachable: Titanium White, Hansa Yellow Opaque, C.P. Cadmium
  Orange, C.P. Cadmium Red Light, Phthalo Green (Y.S.) **used as a tint, not a masstone**,
  Cobalt Blue, Ultramarine. `[verified]`
- **Lowering chroma 2.2 as the fix.** §4.1 — at 2.2 the chroma distribution both shifts up
  and widens, so the abstract track's "the knee compresses the spread" argument does not
  apply at this gain. The measured defect is region count. Fixing the wrong thing here would
  make Fauvism indistinguishable from Post-Impressionism while leaving 21% of the image
  unpaintable.
- **Building the per-hue ceiling from `pigments.manifest.txt`.** §3.2 — the masstone table
  understates the achievable ceiling by up to 2.4× and declares three sectors empty that
  hold C\* 43–88. Build it from the candidate set, which is already in memory and already
  scanned once.
- **Claiming Fauvism is measurably more saturated in any UI text or doc comment.** §2.1 —
  nobody has measured it. The materials argument is good; the measurement does not exist.
- **A Fauve *value* structure lever.** I looked for evidence that Fauvism has a
  characteristic value distribution and found none — Sigaki's placement of Fauvism is
  greyscale-derived but reports entropy and complexity, not value statistics, and it lumps
  Fauvism with Impressionism and Pointillism. `[verified]` There is nothing to target.
- **Impasto, quality scoring, golden ratio, rule-of-thirds, automatic focal detection,
  neural style transfer** — all already rejected upstream; nothing in the Fauvism material
  rescues any of them.

---

## 10. Verification debt

Ranked by how much clearing each would change a decision.

1. **`ScaleChroma`'s behaviour at gain 2.2 was reimplemented, not called.** Every
   ask-versus-deliver number in §4.2 and every row of §7.1 came from a local copy of the
   formula in a throwaway probe, because `ToneAndChromaRemap.ScaleChroma` is private. I
   transcribed it from source and it matches, but the numbers are one transcription away
   from the shipped code. **Re-run them through the real stage before acting on §8B or §8C.**
   Highest impact, cheapest to clear.
2. **The Palmer Lab's "no evidence for Chevreul's harmonies of contrast… red and green"
   sentence is `[relayed]`.** It came from a search-index excerpt; the page I fetched
   directly (`palmerlab.berkeley.edu/Harmony.html`) did not contain it. The underlying
   Schloss & Palmer 2011 result *is* `[verified]` via report 01, so the conclusion stands
   either way — but do not quote that sentence.
3. **No technical study of Derain or Vlaminck was located.** Everything in §1.1 is Matisse.
   A deliberate search returned only museum overviews and general method papers. If the
   Fauve palette claim is to be load-bearing, at least one non-Matisse conservation study
   is needed. **This is a genuine gap, not a fetch failure.**
4. **Mass et al. on *Le Bonheur de vivre*** (*Applied Physics A* 111:59, 2013; *Analyst*
   2013) — abstracts and the ADS record only; neither full text fetched. The cadmium-yellow
   identification is safe (multiple corroborating sources), the degradation chemistry is
   `[relayed]`.
5. **No study measures chroma or colourfulness across art movements.** Same absence the
   abstract track reported for abstract art. Hasler & Süsstrunk's metric exists and is
   perceptually validated; nobody has run it on WikiArt by style. **This is the single
   highest-value missing measurement for the whole style feature**, and it is a project
   somebody could do in an afternoon with a WikiArt dump — not something this environment
   can do (no image-fetching path, no PIL).
6. **Matisse's *Notes of a Painter* quotations are `[relayed]`** from transcriptions, not
   from the 1908 *La Grande Revue* original or a scholarly edition. They are widely and
   consistently reprinted, so the risk is low, but they are load-bearing for §2.3.
7. **The memory-colour evidence in §5.2 is `[relayed]`** — Tian et al. 2022 and the
   surrounding literature via search summaries only. It carries the §9 rejection of
   per-region hue substitution. That rejection also stands on the semantics argument alone,
   so the debt is not critical.
8. **Signac's influence on Matisse and Derain is `[relayed]`** from three consistent French
   museum/encyclopaedia summaries. I did not read *D'Eugène Delacroix au néo-impressionnisme*.
9. **The `sRGB` clipping census (25.0% of pair mixtures) uses `chromaLost > 0.001`** as the
   clipping test. I did not check what units `chromaLost` is reported in, so the threshold
   may be over- or under-sensitive. The direction of the conclusion — that §3.2's ceilings
   are lower bounds — is unaffected.
10. **The masstone-only comparison in §7.1 omits `EdgePreservingFloor`.** The four rows are
    comparable to each other; none is comparable to the committed goldens in §4.1. Re-run
    through `StylePipeline.Render` with the floor before quoting §7.1's absolute region
    counts anywhere.

### What was verified locally this session

All computed against this worktree on 2026-07-28, via throwaway xUnit probes since deleted:

- The 19-selectable per-hue masstone table at both 30° and 15° resolution (§3, confirming
  the abstract track's figures for the masstones themselves).
- **Masstone vs white-line maximum for all 18 chromatic selectable paints** (§3.1) — the
  correction.
- Pair-mixture chroma against parent chroma over all 171 selectable pairs × 31 ratios
  (§3.1) — 6.49% exceed both parents.
- The **84,063-candidate per-15° ceiling table** for all 19 selectable paints (§3.2), plus
  per-30° tables for the golden six-paint palette (3,007 candidates), a six-paint Fauve
  palette (2,968) and an eight-paint wide palette (7,388).
- Hansa Yellow + Phthalo Green (Y.S.) and Hansa Yellow + Ultramarine mixing lines (§3.3).
- Red/green opposition maxima at matched L\* (§3.4).
- sRGB clipping census on 2,565 pair mixtures (§3.2).
- Colour and region statistics of all five committed goldens (§4.1), via
  `PaintabilityMetrics`.
- Ask-versus-deliver by hue on a synthetic full-hue-circle source, scalar versus 12-bin
  per-hue ceiling, for two palettes (§4.2).
- The four-row masstone-only comparison (§7.1).
