# Colour and Palette in Abstract Art

**Date:** 2026-07-28
**Track:** 4 of 4 on abstract art — "what colours does abstract art actually use, and how
should a converter choose them?"
**Relationship to prior research:** [../01-colour-theory-in-practice.md](../01-colour-theory-in-practice.md)
covers colour theory generally (13 levers); this report does not repeat it.
[../02-styles-and-movements.md](../02-styles-and-movements.md) already ruled Colour Field
out as a conversion style and supplied the historical pigment tables for representational
movements; this report extends that to the abstract movements it did not cover.
The chroma ceiling in [../README.md](../README.md) — "Two warnings that constrain the whole
feature" — is the binding constraint here and is *tightened* below.

**Claim marking**, per the directory convention: `[verified]` = read a primary or reputable
source directly, or measured it locally in this session; `[relayed]` = a secondary source or
search summary asserts it and I could not confirm; `[inferred]` = my own reasoning.

---

## Conclusions, first

**The current Abstract style has its premise backwards.** It is built on a chroma multiplier
(1.5) plus a contrast boost (1.5) plus a mother colour (0.15). Every strand of evidence I
found says the chromatic component is the *weakest* signal of abstraction, and that the
sign of the chroma effect is at best unproven and at worst negative:

1. **Abstract art is quantitatively distinguished by its *spatial* statistics, not its
   colour.** Edge-orientation entropy collapses (3.945 ± 0.722 vs 4.380 ± 0.214 for
   traditional art) `[verified via report 02]`; the amplitude-spectrum slope shallows
   (−1.13 vs −1.26) `[verified via report 02]`; Minimalism, Hard Edge and Colour Field sit
   at the low-entropy/high-complexity extreme of the PNAS style map `[verified via report
   02]`. No comparable colour statistic separates abstract from representational work.
2. **Where saturation *has* been measured against judgements of abstract paintings, it
   predicted beauty *negatively*** — HSV saturation ρ = −0.217, p < 0.01, over 150 abstract
   artworks `[verified]`. The only positive chromatic predictor found anywhere in this
   search was the **standard deviation** of saturation (β = 0.404, p = .003), not its mean
   `[verified]`. A global multiplier raises the mean; the tanh knee it passes through then
   *compresses* the spread, which is the statistic with support `[inferred]`.
3. **The palette cannot deliver the boost anyway, and the ceiling is worse than the README
   states.** Measured locally from `Pigments/pigments.manifest.txt` this session: the
   library's best green masstone, Permanent Green Light at C\* 56.0, is
   `ReflectanceDerived` and therefore **excluded from `PigmentLibrary.Selectable`** — a user
   cannot pick it. Of the **19 selectable paints**, the best green masstone is Phthalo Green
   (Y.S.) at **C\* 31.9, L\* 18.9**, and two 30° hue sectors (120–150° and 180–210°) contain
   **no selectable masstone at all** `[verified — computed locally, table in §3.3]`.

**The implementable definition of abstraction is restriction, not amplification.** Every
abstract movement whose materials I could check restricts *something* — De Stijl restricts
hue to three primaries plus three achromatics; Albers restricts *mixing* to zero; Vasarely
restricts to a closed 60-cell alphabet; Malevich restricts to two blacks and a white.
Restriction maps directly onto slot 3 (`ICandidateTransform`), costs nothing per pixel,
cannot violate the gamut invariant, and — uniquely among the options — *helps* the mark
invariant instead of fighting it.

**Top three recommendations, in build order:**

- **A. Restrict the candidate set to N colours** (slot 3, `MixtureBuilder.KeepOnly`).
  Weighted k-means in Lab over the image's own colours, snapped to the nearest *achievable*
  candidate. N ≈ 6–12. This is the single highest-value abstract stage and it is roughly 80
  lines.
- **B. Add a masstone-only mode** to that stage (a checkbox, not a new stage): keep only
  unmixed paints plus their white/black tints. This is the *only* way to reach high chroma
  in this app, because Kubelka-Munk mixing almost always lands below both parents. It is
  Albers' actual practice, and it makes the recipe output trivially executable.
- **C. Replace the flat chroma gain with a per-hue-reachable one.** Do not ask for chroma
  the selected paints do not have in that hue; the ceiling is hue-dependent by a factor of
  more than 3 (C\* 106 in yellow, C\* 32 in green) `[verified]`.

**Do not build:** arbitrary hue rotation as a default, Kandinsky's colour-shape mapping, a
Rothko/Colour-Field preset, or a chroma multiplier as the defining move of the style. §6.

---

## 1. Palette by movement

### 1.1 De Stijl and Mondrian — the restriction is the doctrine

**Doctrine.** Neoplasticism's stated palette is the three primaries — red, yellow, blue —
plus the three "non-colours" white, black and grey `[relayed —
[Britannica](https://www.britannica.com/topic/De-Stijl-art),
[TheArtStory](https://www.theartstory.org/movement/de-stijl/)]`. This is a **six-colour
palette by manifesto**, which is the cleanest thing in this entire report to implement.

**Practice, from conservation science.** The doctrine is a description of the *appearance*,
not of the tubes.

| Painting(s) | Method | Pigments found | Source |
|---|---|---|---|
| *Broadway Boogie Woogie* (1942–43) | MA-XRF + MCR-ALS | **White/grey:** titanium white with barium sulfate, zinc white, lithopone, bone black. **Yellow:** cadmium yellow CdS, zinc-coprecipitated CdS[(Cd,Zn)S], cadmopone CdS(Zn)·yBaSO₄. **Red:** cadmium red CdS·CdSe, cadmium-barium red cadmopone, an organic red in lower layers. **Blue:** synthetic ultramarine (possibly Prussian or phthalocyanine, unconfirmed). **Ground:** lead white. | `[verified — fetched [npj Heritage Science 4:22](https://www.nature.com/articles/s40494-016-0091-4)]` |
| Nine neoplastic works, 1921–1935 | XRF/imaging survey | **White:** lead white early, zinc white later, including two distinct zinc whites with aluminium-phosphate and aluminium-sulphate inclusions. **Yellow:** cadmium yellow, often mixed with the Al/P zinc white. **Blue:** ultramarine, **cobalt blue and cerulean blue**, frequently mixed with white. **Black lines:** bone black, sometimes mixed with the Al/S zinc white. | `[verified — fetched [npj Heritage Science, 2023](https://www.nature.com/articles/s40494-023-01127-8)]` |
| *Victory Boogie Woogie* (1944) | MOLAB + RCE, 2006–08 | titanium white/barium sulfate, zinc white, bone black, cadmium and cadmium-zinc yellow, cadmium and cadmium-barium red, ultramarine. Also adhesive tape and a paint-scraping technique. | `[relayed — search summary of the Gemeentemuseum/RCE study]` |

Two findings from that 2023 survey matter operationally:

- **Mondrian's blues are not one blue.** Ultramarine, cobalt and cerulean all appear, and
  they differ enormously in this library — Ultramarine masstone L\* 7.4 / C\* 66.4, Cobalt
  L\* 27.5 / C\* 70.7, Cerulean Chromium L\* 33.8 / C\* 42.3 `[verified — computed locally]`.
  "The De Stijl blue" is not a well-defined target.
- **The flat planes are not flat construction.** The survey reports **up to ten distinct
  paint layers in a single rectangle** `[verified]`. Mondrian's whites are built, scraped
  and rebuilt. A converter cannot and should not reproduce that; what it can reproduce is
  the *result*, which is a small number of flat colours.

**What De Stijl costs in this library.** Its five working colours are all reachable as
**unmixed masstones**, with no mixing at all `[verified — computed locally]`:

| Role | Nearest selectable paint | L\* | C\* | h |
|---|---|---|---|---|
| White | Titanium White (PW6) | 98.21 | 0.75 | — |
| Yellow | Hansa Yellow Opaque (PY74) | 85.49 | 106.40 | 89.0° |
| Red | C.P. Cadmium Red Light (PR108) | 49.59 | 89.16 | 42.0° |
| Blue | Cobalt Blue (PB28) | 27.46 | 70.74 | 297.3° |
| Black | Bone Black (PBk9) | 11.42 | 1.38 | — |

Note the L\* spread: 98 / 85 / 50 / 27 / 11. **The primaries carry a full value structure on
their own.** Any notan or value-massing lever gets most of its effect for free from a
restriction to saturated masstones, because saturated paints are inherently far apart in
lightness `[inferred, from the measured values]`. This is the strongest single argument for
restriction-plus-masstone over chroma multiplication.

**Where the doctrine is a restriction, not an expansion:** entirely. Nothing in De Stijl asks
for a colour the palette cannot make. It asks for *fewer* colours.

### 1.2 Kandinsky — theory and practice do not match, and the theory does not survive testing

**The theory.** *Concerning the Spiritual in Art* (1911) asserts fixed correspondences
between primary colours and elementary forms: yellow–triangle, red–square, blue–circle. He
ran a questionnaire at the Weimar Bauhaus mural workshop in 1923 and reported majority
agreement `[relayed]`. Jacobsen & Wolsdorff (2007) reported 97% agreement among colour-design
students at Hildesheim `[relayed]` — but those were students *taught the theory*, which makes
it a measure of instruction, not of perception `[inferred]`.

**The theory fails a bias-free test.** Makin & Wuerger (2013), *Frontiers in Psychology*
4:616, ran three Implicit Association Tests on the three pairings, N = 36. Results: IAT 1
t(35) = −1.301, p = 0.202; IAT 2 t(35) < 1, n.s.; IAT 3 t(35) = 2.020, p = 0.051. The largest
D score was **0.12**, against a typical IAT effect of 0.2–0.5. Their conclusion is that there
is no implicit basis for the associations. Jacobsen (2002) separately found that free-choice
participants both chose *different* pairings (red–triangle, from traffic signage) and
actively **disliked** Kandinsky's `[verified — fetched
[Frontiers](https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2013.00616/full)]`.

**The practice contradicts the theory too.** Guggenheim conservation science on Kandinsky's
paintings found he "created his own palette out of combinations of as many as **ten different
pigments per hue**" `[relayed — quoted by
[FAD Magazine](https://fadmagazine.com/2011/08/22/kandinsky%E2%80%99s-painting-with-white-border-at-guggenheim-museum/)
from the Guggenheim/Harvard Straus Center study; the
[Guggenheim project page](https://www.guggenheim.org/conservation/vasily-kandinsky-research-project)
returned only cookie text]`. A multi-analytical study of four 1911–14 reverse-glass paintings
at the Lenbachhaus identified lead white, zinc white, strontium yellow, Prussian blue,
viridian, cadmium yellow, ultramarine, cinnabar, carbon black, plus rare synthetic organics
PR60 and PB52 `[relayed — [npj Heritage Science
7:26](https://www.nature.com/articles/s40494-019-0268-8), abstract only]`.

**Read that plainly:** the artist most associated with "pure" spiritual primaries mixed ten
pigments to a hue. His palette is *broader* than a representational painter's, not narrower,
and his stated colour theory is a poor guide to either his practice or anyone's perception.
**There is nothing here to implement.** §6.

### 1.3 Malevich

*Black Square* (1915) is a two-value painting. Tretyakov analysis found Malevich used **two
distinct kinds of black paint**, and X-ray revealed two earlier compositions plus an
inscription beneath `[relayed — [artnet](https://news.artnet.com/art-world/kizimir-malevich-black-square-363368),
Tretyakov/Vakar 2015]`. Factum Foundation's 2018 Lucida 3D scan captured the surface at
254 dpi `[relayed — [Factum Foundation](https://factumfoundation.org/our-projects/digitisation/malevichs-black-square/)]`.

The operationally interesting fact is the **two blacks**: even a painting whose entire
premise is one colour is materially two, and the difference is in surface and undertone
rather than in Lab position. That is a texture effect, outside this app's contract
`[inferred]`. Treat Suprematism as the N = 2 limit case of restriction, and note report 01's
warning that N = 2 stops being a painting and becomes a stencil.

### 1.4 Rothko — a layering style, not a palette style

Rothko's signature is **method**, not pigment choice: turpentine-thinned washes into
unprimed canvas, edges softened by rubbing solvent back with a rag, glazes producing an
oscillation between matte and gloss, built over extended periods `[relayed — Tate/Harvard
conservation summaries]`. The known palette core is **Lithol Red (an unstable commercial
printing pigment) mixed with ultramarine**, plus cadmium reds and earths in the Harvard
murals `[relayed — [Tate Papers 10: The History and Manufacture of Lithol
Red](https://www.tate.org.uk/research/tate-papers/10/history-and-manufacture-of-lithol-red-pigment-used-by-mark-rothko-in-seagram-and-harvard-murals-1950s-and-1960s)]`.
The Lithol Red / ultramarine combination is *less* lightfast than either component alone
`[relayed]`.

The Harvard restoration is worth knowing as a cautionary structure: because the paintings had
faded, the museum reconstructed the original colours from 1964 Ektachrome slides, a vintage
colour reference card, and direct measurement of an unhung sixth panel, then compensated
**with a projector, pixel by pixel** rather than with paint `[relayed —
[The Conversation](https://theconversation.com/how-we-restored-harvards-rothko-murals-without-touching-them-35245)]`.

**Verdict, agreeing with report 02:** not a conversion style. Rothko's colour effect is
carried by translucency and layer count, which this app deliberately does not model — the
gamut invariant is defined over *mixtures*, not over *glazes*. Report 02's "ship it as
extreme posterisation and claim nothing more" stands. There is a 2025 *Journal of Cultural
Heritage* paper that k-means-clusters Rothko canvases and reports ΔE between adjacent zones,
which would be the right source for a defensible N — but it is paywalled (§7).

### 1.5 Abstract Expressionism — expansion, and therefore unreachable

Pollock is the one abstract painter with a hard count. Non-invasive spectroscopy of *Alchemy*
(1947) identified **fifteen different paints** `[verified — fetched
[npj Heritage Science 4:33](https://www.nature.com/articles/s40494-016-0089-y)]`:

- Whites (3): lead white ground; zinc oxide + anatase; rutile + anhydrite
- Yellows/orange (3): Cd-Zn sulfide; the same diluted; CdS·CdSe orange
- Reds (3): toluidine red (PR3); CdS·CdSe + organic dye; hematite deep red
- Greens (2): viridian; phthalocyanine green
- Blues (2): phthalocyanine blue; ultramarine
- Violet (1): manganese phosphate — Black (1): carbon black in an alkyd medium

He mixed artists' oils with alkyd house paints. MoMA and Stanford confirmed in 2025 that the
blue in *Number 1A, 1948* is **manganese blue** (Mn, Ba, S) `[relayed —
[Stanford Chemistry](https://chemistry.stanford.edu/news/mystery-solved-jackson-pollock-used-manganese-blue-famous-1948-painting-moma-and-solomon-lab),
[PNAS 2025](https://www.pnas.org/doi/abs/10.1073/pnas.2513166122)]`.

Yet Kim, Son & Jeong's box-counting dimension in RGB space — a palette-variety measure —
puts **Pollock's drip paintings at ≈ 2.35, below every historical period they measured
(2.6–2.8) and below even medieval work (2.4)** `[relayed via report 01, from
[Sci Rep 4:7370](https://www.nature.com/articles/srep07370)]`. Fifteen tubes, but a *narrower*
distribution of realised colours than a Renaissance panel. The reconciliation is that most
of the canvas is a small number of colours laid at high spatial frequency, and the
juxtaposition does not create new colour so much as it creates texture `[inferred]`.

**Implication:** Abstract Expressionism's colour is not the target; its mark is. Report 02
already routes that to stroke synthesis, which this app does not have.

### 1.6 Hard-edge and Op Art — the most implementable group

This is where the evidence is most directly usable, because these artists documented their
systems.

**Josef Albers.** *Homage to the Square*: paint squeezed **directly from the tube** and spread
with a **palette knife** on hardboard, "either unmixed or in their industrially produced
state" — and on the verso of every panel he recorded **manufacturer, line and tube number**
`[relayed — [Public Delivery](https://publicdelivery.org/josef-albers-homage-to-the-square/),
corroborated by the Smart Museum's authentication note that visible brushstrokes indicate a
fake]`. Each painting is **three or four** nested squares, hence three or four colours.

This is the single most directly transferable practice in the whole report:

- **No mixing at all.** In Kubelka-Munk terms that means every colour sits at a masstone,
  which is the chroma maximum. Nothing else in this app reaches those colours.
- **A recipe that is trivially executable.** "Paint A, straight from the tube" satisfies the
  mark invariant with no interpretation.
- **Colour as subject.** *Interaction of Color* (1963) is a book of demonstrations — "one
  colour appears as two", "two colours look alike", the fluting effect — in which the
  *relationship* is the content `[relayed]`. Modern work has replicated the lightness version
  quantitatively using Albers' own double-cross pattern (5 targets × 41 test/background
  combinations, 20 observers, calibrated display) `[relayed]`, so the phenomenon is real even
  where the pedagogy is loose.

**Bridget Riley.** Works out colour interactions first in small gouache studies, then
full-size paper designs, before painting `[relayed — [Art UK](https://artuk.org/learn/learning-resources/bridget-riley-and-op-art)]`.
Introduced colour in 1967 with the stripe paintings; adopted a self-described "Egyptian
palette" after 1979 and later an Indian one, and switched from acrylic/PVA to oil for higher
apparent intensity `[relayed — [MyArtBroker](https://www.myartbroker.com/artist-bridget-riley/articles/the-evolution-of-bridget-rileys-colour-palettes)]`.
Tate's Riley holdings are PVA on canvas (*Deny 2*, *Late Morning*) and hardboard (*Hesitate*)
`[relayed]`. **A named, small, fixed palette re-used across a body of work** is the
transferable idea — it is exactly a candidate-set restriction with a saved preset.

**Victor Vasarely.** The "plastic alphabet": a lexicon of simple geometric shapes embedded in
squares of pure colour, the *plastic units*, with **six basic colours — red, yellow, blue,
green, violet and grey — each in 10 shades**, i.e. a closed 60-cell palette `[relayed —
search summary; the [Vasarely Foundation page](https://www.fondationvasarely.org/en/planetary-folklore-period/)
I fetched does not state the numbers, so this specific 6 × 10 figure is unconfirmed]`.

**Where these are restrictions rather than expansions:** all of them. Albers restricts
mixing; Riley restricts the tube list; Vasarely restricts to a finite enumerated palette;
Mondrian restricts hue. The one movement that *expands* — Abstract Expressionism — is the
one whose defining property this app cannot render anyway.

---

## 2. How many colours

### 2.1 The measurements

| Source | Quantity | Value |
|---|---|---|
| Nascimento group, psychophysics of "relevant colours" `[verified]` | Colours observers spontaneously name as describing a painting's palette | **21 (SD 5)** for 20 Prado paintings; **22 (SD 11)** for 20 from the Khan set; their algorithm returns **19 (SD 6)**. 6 observers. Segmentation from those palettes correlates 0.941 / 0.955 with the original. **≈70% of selected colours had C\* < 35**; hues clustered near 30°, 40°, 70°, 90°, 110°, 130°. |
| Pollock, *Alchemy* `[verified]` | Distinct paints in one canvas | **15** |
| Mondrian, neoplastic `[verified]` | Distinct *appearances* | 6 by doctrine (3 primaries + white/black/grey); materially more, up to 10 layers per rectangle |
| Albers, *Homage* `[relayed]` | Colours per painting | **3–4**, unmixed |
| Vasarely `[relayed]` | Closed system size | 6 hues × 10 shades = **60** |
| Chang et al., *Palette-based Photo Recoloring*, SIGGRAPH 2015 `[relayed]` | Palette size that works for editing photos | default **k = 5**; **k ∈ [3, 7]** works for typical operations |
| Kim/Son/Jeong `[relayed]` | Box-counting dimension in RGB (palette variety) | Pollock **2.35**; medieval 2.4; most periods 2.6–2.8 |

**The convergence is striking.** Independent lines — a psychophysical naming task on real
paintings, an image-editing paper choosing a usable palette size, and the actual practice of
hard-edge painters — land between **3 and 22**, with the interactive-editing sweet spot at
5 and the perceptual description task at ~21.

The 21-colour figure is the right one for "how many colours does a viewer *perceive* a
painting to have"; the 3–7 figure is the right one for "how many colours can be manipulated
as a palette"; the 3–4 figure is what an abstract painting can get away with. The gap
between them is the design space.

### 2.2 What that means for this app

A crucial distinction the app must not blur `[inferred]`:

- **Paints selected** — currently up to a handful, chosen by the user, from 19 selectable.
- **Candidates generated** — the `MixtureBuilder` output. With 6 paints this is ~3,000
  distinct achievable colours (per the README's measured probe).
- **Colours actually used in one output image** — currently unbounded up to the candidate
  count, and in practice large.

The abstract stage should constrain the **third**, not the first. Constraining the paint list
is the user's job and it is already possible; constraining realised output colours is what no
existing stage does and what every abstract movement does.

**Recommended default: N = 8.** Rationale `[inferred]`: above Chang's editing range (which
is chosen for *manipulability*, not for looking painted), below the 19–22 perceptual naming
figure (so the reduction is visible rather than subliminal), and comfortably above the N = 4
at which report 01 warns that notan "stops being a painting and becomes a stencil". Expose
N ∈ [2, 24].

---

## 3. Saturation and contrast — is the folk belief true?

### 3.1 What the evidence says

**Direct evidence on abstract paintings says saturation is not what makes them good.**
Mallon, Redies & Hayn-Leichsenring (2014), *Frontiers in Human Neuroscience*, rated **150
abstract artworks** and correlated beauty ratings with a wide statistic set (self-similarity
by PHOG, complexity, anisotropy, a Birkhoff-like measure, aspect ratio, and colour measures
in HSV, RGB and Lab). Significant correlations over all participants `[verified — fetched
[PMC3968763](https://pmc.ncbi.nlm.nih.gov/articles/PMC3968763/)]`:

| Measure | ρ | p |
|---|---|---|
| HSV saturation | **−0.217** | < 0.01 |
| HSV value | −0.277 | < 0.01 |
| Lab lightness | −0.206 | < 0.05 |
| Lab b\* (yellow–blue) | −0.224 | < 0.01 |

Multivariate R² = **0.134** for average beauty. So: within abstract art, higher saturation
went with *lower* rated beauty, weakly; and the whole statistical apparatus explains ~13% of
variance — consistent with report 04's "what not to build" finding about painting-quality
scores.

**The one positive chromatic predictor is variance, not level.** A 2023 study predicting
infant looking and adult aesthetic preference from MacLeod-Boynton chromatic statistics found
**standard deviation of saturation** to be a positive predictor for both groups (adults
β = 0.404, p = .003; infants β = 0.278, p = .048). Mean saturation was not the predictor
`[verified — fetched [PMC10399602](https://pmc.ncbi.nlm.nih.gov/articles/PMC10399602/); note
the stimuli were van Gogh **landscapes**, so this does not transfer to abstract work
directly]`.

**The abstract/representational difference that *is* measured is spatial.** Report 02's
figures, re-stated because they carry the argument: abstract art's amplitude-spectrum slope
−1.13 ± 0.0614 (n = 12) vs landscape −1.26 ± 0.0387, differing at p < 0.03, while *basic
intensity statistics — mean, variance, skew, kurtosis — did not differ significantly by
content* `[verified via report 02]`. Redies' edge-orientation entropy for 572 abstract works
is 3.945 ± 0.722 against 4.380 ± 0.214 for traditional art `[verified via report 02]`.

**Verdict: "abstract art is more saturated" is folk belief.** I found no measurement
supporting it, one measurement weakly opposing it within abstract art, and a body of work
showing the real separation is in edge and frequency statistics. `[inferred]` from
`[verified]` inputs.

### 3.2 Why the current ×1.5 is nonetheless not *harmless*

It is worse than a no-op, for two reasons `[inferred]`:

1. **It is hue-selective in a way nobody chose.** Because the tanh knee is scaled by a single
   scalar `AchievableMaxChroma`, and the actual ceiling varies by more than 3× across hue
   (§3.3), a global gain of 1.5 is a modest lift in yellow-orange and an unreachable demand
   in green, cyan and violet. The nearest-candidate search resolves the unreachable demands
   onto whichever few boundary candidates exist there — which is exactly the banding and
   hue-drift failure the `ToneAndChromaRemap` doc comment warns about, arriving through the
   ceiling being a *scalar* rather than through the gain being too high.
2. **It compresses the statistic with the only positive evidence.** The knee is concave;
   applying it flattens the upper tail of the chroma distribution, reducing the spread of
   saturation across the image while raising its mean. §3.1 says the spread is the part with
   support.

### 3.3 The per-hue chroma ceiling — measured locally

Maximum masstone C\*ab per 30° CIELAB hue sector, computed this session from
`Pigments/pigments.manifest.txt` `[verified — local computation]`:

| Hue sector | All 80 paints | Best paint (all) | **19 selectable only** | Best selectable |
|---|---|---|---|---|
| 0–30° (crimson/red) | 61.8 | Cad Red Medium, L\* 43.2 | **61.3** | Quinacridone Red, L\* 34.1 |
| 30–60° (orange/red) | 99.9 | Pyrrole Orange, L\* 54.4 | **99.9** | Pyrrole Orange |
| 60–90° (yellow) | 106.4 | Hansa Yellow Opaque, L\* 85.5 | **106.4** | Hansa Yellow Opaque |
| 90–120° (green-yellow) | 96.6 | Bismuth Vanadate Yellow, L\* 91.7 | **96.6** | Bismuth Vanadate Yellow |
| 120–150° (yellow-green) | 43.8 | Cobalt Titanate Green | **0.0 — none** | — |
| 150–180° (green) | 56.0 | Perm Green Light, L\* 45.3 | **31.9** | Phthalo Green (Y.S.), L\* 18.9 |
| 180–210° (blue-green) | 42.3 | Cobalt Teal | **0.0 — none** | — |
| 210–240° (cyan) | 15.5 | Phthalo Green (B.S.) | **15.5** | Phthalo Green (B.S.), L\* 13.6 |
| 240–270° (cyan-blue) | 42.3 | Cerulean Blue Chromium | **42.3** | Cerulean Blue Chromium |
| 270–300° (blue) | 70.7 | Cobalt Blue, L\* 27.5 | **70.7** | Cobalt Blue |
| 300–330° (violet-blue) | 66.4 | Ultramarine Blue, L\* 7.4 | **66.4** | Ultramarine Blue |
| 330–360° (magenta/pink) | 0.0 — none | — | **0.0 — none** | — |

**Read this carefully.** "No masstone in a sector" does *not* mean the achievable gamut is
empty there — mixtures of the neighbouring paints land in the gap. It means the gamut there
is filled only by mixtures, and Kubelka-Munk mixing lands *below both parents* in chroma, so
those sectors are structurally low-chroma `[inferred, but it follows directly from the mixing
data in the prior research]`. The masstone maximum is therefore a good upper bound on the
per-hue chroma ceiling.

**Consequences for any abstract style:**

- A chroma boost is **freely available in 30–120°** (orange through yellow-green) and
  **nowhere else** at comparable magnitude.
- **Green is the worst case, and worse than the README states** — the README quotes
  Permanent Green Light at 56.0, which is `ReflectanceDerived` and is filtered out of
  `PigmentLibrary.Selectable` by design. A user's actual best green masstone is C\* 31.9 at
  L\* 18.9, which is a very dark green. A Fauve or abstract green is not available.
- **Magenta/pink is absent entirely** as a masstone; the closest is Quinacridone Magenta at
  h 10.7°.
- The **cyan hole at 210–240° (C\* 15.5)** is the single narrowest place in the gamut.

**The one honest way to raise chroma in this app is to stop mixing.** §5, lever B.

### 3.4 Contrast

The `contrast` parameter is on firmer ground than `chroma`, because value compression/
expansion is well-attested and Sigaki's entropy/complexity axis — on which Hard Edge,
Minimalism and Colour Field are extreme — is explicitly a **texture and edge** axis, i.e. a
"how much to flatten" axis `[verified via report 02]`. Flattening is done by the pre-map
floor and by the quantiser, not by the L\* gain, so the contrast slider's real job is to open
the value range so that a small N of colours still separates. **Keep it; raise it slightly
when N falls** `[inferred]`.

---

## 4. Colour relationships in abstraction

### 4.1 Simultaneous contrast as subject

Albers' whole programme is that a colour has no fixed identity — "one colour appears as two."
Report 01 §7 already handles the computational question and its answer stands: a genuinely
relational objective is a different, far slower program with no evidence of a better result.

The abstract-specific point is different and better `[inferred]`: **simultaneous contrast is
free, and it gets stronger as the palette gets smaller.** Induction magnitude grows with the
target–surround difference and with surround saturation `[relayed via report 01]`. A
restricted, high-chroma, large-flat-region output is precisely the configuration that
maximises induction — so the Albers effect arrives as a *consequence* of restriction, in the
viewer's visual system, without the app modelling anything. The correct implementation of
Albers is to build the configuration and let the eye do the work. Do **not** attempt to
pre-compensate for induction; report 01 found no agreed magnitude to hard-code and large
individual variability.

### 4.2 Arbitrary versus derived colour — the strongest evidence in this report

Two large, independent studies asked exactly the question "does it matter whether the colours
refer to the source?" by rotating the hue gamut of paintings and measuring preference.

**Nakauchi & Tamura (2022), *Scientific Reports* 12:14367 — 1,200 paintings across five
genres (abstract, flowers, posters, symbolic, still life), 31,353 participants, 4AFC among
0°/90°/180°/270° CIELAB hue rotations** `[verified — fetched
[PMC9418166](https://pmc.ncbi.nlm.nih.gov/articles/PMC9418166/)]`:

- Originals (0°) selected significantly above the other rotations, p < 0.001, **including for
  abstract paintings with no recognisable objects.**
- **Genre mattered, η²p = 0.42, with the ordering `abstract < poster = symbolic < flowers =
  still life`.** Abstract paintings had the *lowest* selection rate for the original — i.e.
  hue rotation costs least in abstraction.
- Eight colour statistics predicted preference, multiple regression **r = 0.61**. Strongest:
  **skewness of a\* (β 0.049)**, **a\*–b\* correlation (β 0.046)**, L\*–b\* correlation
  (0.026), variance of b\* (0.023).
- Model fit was **best for abstract paintings (RMSE 0.08)** and worst for flowers (0.14) —
  i.e. abstract preference is more purely chromatic and less object-driven.
- Paintings differ systematically from natural scenes: positive a\* skew in paintings but not
  in nature (**Cohen's d = 0.565**); positive a\*–b\* correlation ("clockwise tilt") in
  paintings vs negative in nature (**d = 0.904**). The matching-to-nature hypothesis was not
  supported (r = 0.398, n.s.).

**Nascimento et al. (2022), *Scientific Reports* 12:4294 — 40 paintings (20 museum works, 20
from internet galleries of which 10 figurative and 10 abstract), Japanese and Portuguese
observers, ~45 per group with a ~44-per-group replication** `[verified — fetched
[PMC8917196](https://pmc.ncbi.nlm.nih.gov/articles/PMC8917196/)]`:

| Condition | Japanese | Portuguese |
|---|---|---|
| C0 intact | ~58% | ~56% |
| C1 scrambled into squares | ~50% | ~48% |
| C2 patchwork of pieces from different paintings | ~54% | ~48% |
| C3 randomised | ~25% (chance) | ~25% (chance) |

The original survives scrambling and patchworking, which means the preference is carried by
**colour statistics, not by composition**.

**Nascimento et al. (2017), *Vision Research* 130:76–84 — 10 paintings, 50 naïve observers,
free continuous rotation:** the preference maxima deviated **on average only 7°** from the
original gamut orientation, with FWHM just above the threshold for perceiving a chromatic
change; saturation varied by < 3.8 ΔE\*ab across rotations. Pooled over abstract paintings the
maximum was at exactly 0°. Also reported: painting colours are generally more **red-biased**
than natural scenes `[relayed — the PDF would not render (§7); figures from the
[Manchester record](https://research.manchester.ac.uk/en/publications/the-colors-of-paintings-and-viewers-preferences)
and search summaries]`.

**The synthesis for this app** `[inferred]`:

- Arbitrary hue re-assignment is **not** an improvement. The artist's chosen orientation is
  preferred, by a consistent margin, across cultures, across genres, and even after the
  image is cut up.
- But the margin is **small** (≈56% against 25% chance) and it is **smallest for abstract
  work**. So a hue-rotation control is defensible as a *style* option in the abstract style
  specifically — it is the one genre where breaking the link to the source colour is
  cheapest — as long as its default is 0° and it is not sold as an improvement.
- The statistics that actually predict preference — **a\* skewness and the a\*–b\*
  correlation** — are computable in one pass and are far better targets than mean chroma. A
  stage that preserved or gently pushed *those* would have more evidence behind it than
  anything currently in `ToneAndChromaRemap` `[inferred]`. Flagged as future work; I have not
  seen it done.

### 4.3 Harmony

Report 01's finding stands unmodified and I found nothing abstraction-specific that overturns
it: Schloss & Palmer's 1,431-pair data has harmony peaking at *identical* hue and falling
monotonically with hue difference, with complements reliably *less* harmonious,
F(1,47) = 17.67, p < .001 `[verified via report 01]`.

The abstraction-specific addendum is that the "relevant colours" psychophysics found
observers' colour selections **clustered at discrete hue angles ~30°, 40°, 70°, 90°, 110°,
130°, with ~70% below C\* 35** `[verified]`. That is an empirically observed anchor set, and
it is *not* a wheel scheme — it is a warm-biased, low-chroma cluster consistent with the
red-bias finding above. If a hue-snapping stage needs default anchors, those measured angles
are a better starting point than an evenly spaced ring `[inferred]`.

---

## 5. How to choose colours when turning a photograph abstract

Each lever: the pipeline slot, the cost, the effect on recognisability, and its standing
against the gamut and mark invariants.

### A. Restrict the candidate set to N colours — **build this first**

- **Slot 3**, `ICandidateTransform`, via `MixtureBuilder.KeepOnly(Func<double,double,double,bool>)`
  as a membership predicate against N chosen Lab points.
- **How to choose the N.** Three-step `[inferred]`:
  1. Pre-pass over the (already floor-filtered) image: k-means in CIELAB, k = N, **weighted
     by pixel count** so large regions get their own colour. Area weighting matters — an
     unweighted k-means spends clusters on specular highlights.
  2. Snap each centroid to its nearest *achievable* candidate. This is the step that keeps
     the gamut invariant structural rather than aspirational; the surviving set is by
     construction real mixtures.
  3. Optionally force-include the extremes: the palette's lightest and darkest candidates
     (lever F) and, if the user asks for it, a primary triad (lever G).
- **Cost.** One k-means over quantised colours (the 6-bit cache key already gives you a
  histogram for free), plus a predicate that is a nearest-of-N test. Roughly 80 lines. Zero
  per-pixel cost after build.
- **Recognisability.** High at N ≥ 8. This is posterisation in colour rather than in value,
  and human recognition survives it well — value structure is preserved because the k-means
  runs in Lab and L\* dominates the variance in most photographs `[inferred]`.
- **Invariants.** Gamut: safe by construction (slot 3 acts before anything becomes a colour).
  Mark: **strictly helped** — fewer colours means larger contiguous regions, directly
  attacking the tiny-region failure the README measured. Note `KeepOnly`'s documented safety
  valve: if the predicate rejects everything, `Build` returns the unfiltered set, so N must
  produce a non-empty membership test.
- **Confidence: high** that it looks deliberate and abstract; **high** that it is the correct
  reading of the movement evidence (§1); **high** that it is cheap.

### B. Masstone-only candidates ("unmixed") — the only real route to high chroma

- **Slot 3**, as an option on the same stage. Keep only the single-paint candidates and their
  binary mixtures with the palette's white and black (tints and shades), discarding
  chromatic-plus-chromatic mixtures.
- **Cost.** A predicate on the mixture's share vector rather than on its Lab colour — which
  the current `KeepOnly` signature does *not* expose (it takes L\*, a\*, b\*). Either extend
  `MixtureBuilder` with a share-aware predicate, or approximate it with a Lab predicate that
  keeps only candidates within ΔE of a masstone/tint line. The former is cleaner.
- **Effect.** This is Albers' practice exactly, and it is the *only* operation in the app
  that raises the reachable chroma ceiling instead of chasing it. It also produces the most
  executable recipe possible: "paint A straight from the tube".
- **Recognisability.** Low-to-moderate — it is a strong abstraction. Pair it with a larger N.
- **Invariants.** Both safe; the mark invariant is helped.
- **Confidence: high** that it produces the intended look and **high** that it is
  physically correct. This is the honest answer to the chroma-ceiling problem.

### C. Hue quantisation / snapping to anchors

- **Slot 2** (`ILabRemap`) if you snap the *target* hue before matching, or **slot 3** if you
  restrict the candidates to hue wedges. **Prefer slot 3** — report 01 already found that a
  hard hue cut applied to targets makes every out-of-wedge colour snap to a wedge boundary
  and band visibly, and it recommends a chroma-scaled penalty with a neutral core.
- **Anchors.** Either N evenly spaced, or the measured cluster angles from §4.3
  (30/40/70/90/110/130°), or a user-chosen triad.
- **Cost.** Small. Per-pixel safe.
- **Recognisability.** Moderate loss; hue snapping is much more visible than colour-count
  reduction because it moves colours *sideways* rather than merging them.
- **Caveat.** Snapping interacts badly with §3.3: snapping to a hue anchor that lies in the
  cyan or magenta hole forces a chroma collapse. **Derive anchors from the achievable gamut,
  not from an abstract hue wheel** `[inferred]`.
- **Confidence: medium.** Lever A subsumes most of its benefit at lower risk.

### D. Deliberate hue rotation / arbitrary re-assignment

- **Slot 2**, one line: rotate (a\*, b\*) by θ.
- **Cost.** Trivial.
- **Evidence.** §4.2: originals preferred across 31,353 participants and 1,200 paintings, but
  the margin is small and **smallest for abstract work**; free-rotation maxima sit 7° from the
  original.
- **Recognisability.** Colour contributes only **5–10%** to recognition memory for natural
  scenes (Wichmann, Sharpe & Gegenfurtner 2002, *JEP:LMC* 28(3):509–20) `[relayed]`, and
  S-CIELAB's chromatic channels are low-pass while luminance is band-pass `[verified via
  report 01]`. So rotation costs surprisingly little recognisability — the image still reads.
  It just is not preferred.
- **Recommendation:** ship it in the Abstract style only, default **0°**, and label it as an
  arbitrary choice rather than an enhancement. Continuous rotation is much safer than random
  per-colour re-assignment, which destroys the a\*–b\* correlation structure that §4.2 found
  predicts preference `[inferred]`.
- **Confidence: medium** that users will want it; **high** that it should not be on by
  default.

### E. Chroma boost

- **Slot 2**, already exists.
- **The fix, not the removal** `[inferred]`: replace the scalar `AchievableMaxChroma` with a
  **per-hue** ceiling — a small lookup (say 36 bins of 10°) built once at candidate-build
  time by scanning the candidate set's own max C\* per hue bin, which is ~15 lines given
  `CandidateSet` already holds Lab per candidate. Then `ScaleChroma` asks for a reachable
  target in every hue instead of an unreachable one in half of them.
- **What it does per hue** at gain 1.5, given §3.3: a real lift in 30–120°; nothing usable in
  120–240°; moderate in 270–330°. Without the per-hue ceiling the style is effectively "make
  the yellows and oranges louder", which is not what abstraction means.
- **Recognisability.** Minimal effect.
- **Confidence: high** that the per-hue ceiling is a correctness fix; **low** that any chroma
  gain is what makes the output read as abstract (§3.1).

### F. Value structure preserved while colour is freed

- Applies **across** levers A/C/D. The claim to test is: does keeping L\* while scrambling hue
  still read as the photo?
- **Evidence says largely yes.** S-CIELAB's structure — band-pass luminance, low-pass chroma
  — means spatial form is carried by luminance `[verified via report 01]`; colour adds only
  5–10% to scene recognition `[relayed]`; and the §4.2 rotation studies show hue-rotated
  paintings remain fully legible images (observers judged them on preference, not on whether
  they could see what they were).
- **Implementation.** Any lever that operates in polar (a\*, b\*) while leaving L\* alone
  automatically has this property. Lever A's k-means does *not* automatically — it can merge
  two colours of different lightness. **Add an L\*-weighted distance to the k-means** (weight
  1.5, matching `PaintBlendMatcher`) so clusters split on value before they split on hue
  `[inferred]`. This is the single most important detail in lever A.
- **Confidence: high.**

### G. Forced inclusion of black and white as structural elements

- **Slot 3**, one extra rule inside lever A: always retain the candidate nearest L\* 100 and
  the candidate nearest L\* 0, regardless of what k-means chose.
- **Why.** De Stijl's "non-colours" are half its palette; Malevich is black and white;
  Albers' *Homages* usually contain a near-neutral. And structurally: forcing the two L\*
  extremes into an N-colour set guarantees the output spans the palette's full value range,
  which is what stops a small N from reading as muddy `[inferred]`.
- **Cost.** Negligible. Two of the N slots.
- **Caveat.** With a palette containing no black or white, "nearest to L\* 0/100" is whatever
  the darkest and lightest candidates happen to be — which is correct behaviour, and matches
  how `MotherColourTransform` already picks the most neutral paint from whatever is loaded
  rather than naming one.
- **Confidence: high.**

### H. Forced primary triad

- **Slot 3**, an option: force-include the candidates nearest three chosen hue angles at
  maximum available chroma. With hue angles at the De Stijl targets and the §1.1 table, this
  reproduces neoplasticism directly.
- **Recognisability.** Low. This is the most aggressive option and it should be a preset
  ("De Stijl") rather than a default.
- **Confidence: medium-high** that it looks like what it claims to look like — because it is
  literally the documented palette (§1.1) — and **high** that it is achievable, since all five
  colours are unmixed masstones in the library.

### I. Region merge at mark scale (post-map)

- **Slot 5**, `IPostMapStage` — index-in, index-out, so the gamut invariant is structurally
  untouchable.
- **What.** After mapping, absorb any connected region below the mark-size threshold into the
  modal index of its neighbours. This is a *selection-only* operation and therefore explicitly
  safe under the four-category table.
- **Why it belongs in the abstract style.** Restriction to N colours dramatically reduces
  region count but does not eliminate speckle at boundaries; the mark invariant is the second
  invariant the design added, and this is its direct enforcement.
- **Cost.** Connected-component labelling plus a modal pass; moderate, ~150 lines, but reused
  by every style.
- **Confidence: high** that it is needed eventually; medium that it is needed *before* lever
  A ships, since lever A already suppresses most of the problem.

### Summary table

| Lever | Slot | Cost | Recognisability | Chroma reachable? | Confidence |
|---|---|---|---|---|---|
| **A** N-colour restriction | 3 | ~80 lines | high at N ≥ 8 | n/a | **high** |
| **B** Masstone-only | 3 | needs share-aware predicate | moderate | **yes — the only lever that raises it** | **high** |
| **C** Hue snapping | 3 (not 2) | small | moderate loss | no — can force a collapse | medium |
| **D** Hue rotation | 2 | trivial | small loss | neutral | medium, default 0° |
| **E** Per-hue chroma ceiling | 2 | ~15 lines | none | fixes the ask, not the ceiling | high as a fix |
| **F** L\*-weighted clustering | inside A | trivial | **preserves it** | n/a | **high** |
| **G** Forced black + white | 3 | trivial | improves it | n/a | high |
| **H** Forced primary triad | 3 | small | low | yes, at masstone | medium-high |
| **I** Mark-scale region merge | 5 | ~150 lines | none | n/a | high, later |

---

## 6. What not to build

Each of these sounds compelling for an abstract style and does not survive the evidence.

- **A chroma multiplier as the *definition* of the Abstract style.** §3. There is no
  measurement showing abstract art is more saturated; the one direct measurement on abstract
  paintings has saturation correlating *negatively* with beauty (ρ = −0.217) `[verified]`; and
  the palette cannot deliver a boost outside 30–120° `[verified, measured locally]`. Keep the
  control, demote it, and fix its ceiling to be per-hue.
- **Kandinsky's colour–shape correspondences** (yellow-triangle / red-square / blue-circle) as
  a mapping rule. The IAT shows no implicit association, max D 0.12 `[verified]`; free-choice
  participants prefer *different* pairings and dislike Kandinsky's `[verified]`; and Kandinsky
  himself mixed up to ten pigments per hue, so the theory does not even describe his own
  practice `[relayed]`.
- **Random or per-colour arbitrary hue re-assignment.** Continuous rotation is defensible
  (§5D); randomised re-assignment is not. It destroys the a\*–b\* correlation and a\* skewness
  that a 31,353-participant study identified as the actual predictors of colour-composition
  preference `[verified]`.
- **A Rothko or Colour Field preset.** Agrees with report 02's independent verdict. Rothko's
  effect is layered translucency over unprimed canvas with matte/gloss oscillation
  `[relayed]`; this app's invariant is defined over opaque mixtures. Shipping it as
  posterisation-with-a-famous-name is the failure mode report 02 warns about.
- **Simultaneous-contrast pre-compensation.** §4.1. The effect is real and Albers built a
  career on it, but induction magnitude has no agreed value and large individual variability
  `[relayed via report 01]`. Restriction produces the configuration; the eye does the rest for
  free.
- **Complementary harmony as the abstract default.** Already on the README's list; nothing in
  the abstract literature rescues it. If the app offers hue anchors, the measured cluster set
  (30/40/70/90/110/130°, mostly below C\* 35) `[verified]` has more support than any wheel
  scheme.
- **Extracting the palette from the *photo* alone.** k-means on the source image gives colours
  the paints may not be able to make; snapping to achievable candidates *after* clustering is
  what keeps the invariant structural rather than a promise (§5A step 2). An unsnapped palette
  would put the invariant in the hands of the nearest-neighbour search, where a whole cluster
  can collapse onto one boundary candidate.
- **"Mondrian-ification" — fitting the image to rectangles with primary fills.** Tempting,
  satisfies the mark invariant beautifully, and is not a photo conversion: the output no
  longer depends on the photograph in any recoverable way. It is a composition generator
  wearing a converter's name. Same objection report 02 raised for Colour Field.
- **An abstraction "quality" score.** Report 04's finding generalises: over 150 abstract
  artworks the full statistic set explains R² = 0.134 of beauty ratings `[verified]`.

---

## 7. Verification debt

**Verified directly this session (fetched and read):**

- Mondrian *Broadway Boogie Woogie* MA-XRF pigment list, and the nine-painting neoplastic
  zinc-white survey including "up to ten distinct paint layers in single rectangles" — both
  *npj Heritage Science*, reached only via the `idp.nature.com` → `?error=cookies_not_supported`
  redirect chain.
- Pollock *Alchemy* — fifteen paints, itemised, *npj Heritage Science* 4:33.
- Makin & Wuerger 2013 IAT results on Kandinsky's colour–shape theory, *Frontiers in
  Psychology* 4:616.
- Mallon, Redies & Hayn-Leichsenring 2014, 150 abstract paintings, all four ρ values and
  R² = 0.134, PMC3968763.
- Nakauchi & Tamura 2022, 1,200 paintings / 31,353 participants, genre ordering, regression
  coefficients and the natural-scene comparison, PMC9418166.
- Nascimento et al. 2022, 40 paintings, four conditions, per-condition selection rates,
  PMC8917196.
- "Psychophysical Determination of the Relevant Colours That Describe the Colour Palette of
  Paintings" — 21 (SD 5) / 22 (SD 11) / algorithmic 19 (SD 6), hue clusters and the C\* < 35
  figure, PMC8321366.
- Chromatic-statistics preference paper, SD-of-saturation β values, PMC10399602.
- **Local measurements**: the per-hue masstone chroma table (§3.3), the De Stijl palette table
  (§1.1), the 19-selectable count, and the fact that Permanent Green Light is
  `ReflectanceDerived` and thus excluded by `PigmentLibrary.Selectable` — all computed from
  `Pigments/pigments.manifest.txt` and read from `Pigments/PigmentLibrary.cs`.

**Could not fetch — cited as `[relayed]`:**

- **Nascimento et al. 2017, *Vision Research* 130:76–84.** ScienceDirect returned **403**; the
  Manchester institutional PDF downloaded but would not render as text (no `pdftoppm` in this
  environment, and the `Read` tool cannot page a PDF without it). The "7° mean deviation",
  "10 paintings, 50 observers" and "< 3.8 ΔE\*ab saturation variation" figures are from the
  Manchester record page and search summaries. **Needs a browser.** This matters because it is
  the finest-grained of the three rotation studies.
- **Papia & Kondi, "Quantifying subtle color transitions in Mark Rothko's abstract paintings
  through K-means clustering and Delta E analysis", *Journal of Cultural Heritage* 2025.**
  ScienceDirect **403**; the SSRN preprint also **403**. This is the one source that would
  give a defensible measured N for a Rothko-like reduction and a ΔE for the transitions.
  Highest-value single item to clear.
- **Guggenheim Vasily Kandinsky Research Project.** The page returned only cookie-consent
  text. The "as many as ten different pigments per hue" quotation is relayed through FAD
  Magazine. Worth confirming, since it is the load-bearing fact in §1.2's practice argument.
- **Kandinsky reverse-glass paintings, *npj Heritage Science* 7:26** — abstract/search summary
  only; the pigment list is unconfirmed at the primary source.
- **Vasarely's "six colours × ten shades"**. The Vasarely Foundation's own Planetary Folklore
  page, which I fetched, does **not** state any numbers. The 6 × 10 figure is from a search
  summary and should be treated as unconfirmed folklore until a catalogue raisonné or the
  Fondation's technical material confirms it.
- **Albers' verso records (manufacturer / line / tube number) and the palette-knife,
  unmixed-from-tube method** — corroborated across several secondary sources but not against
  a conservation report or the Josef and Anni Albers Foundation.
- **Malevich's "two kinds of black paint"** — artnet reporting of the 2015 Tretyakov
  examination; no primary conservation publication located.
- **Rothko's Lithol Red + ultramarine and the layering description** — Tate Papers 10 is open
  access and would confirm the pigment history; I read only its search summary.
- **Wichmann, Sharpe & Gegenfurtner 2002** 5–10% colour recognition benefit — search summary
  only; paywalled at APA.
- **Chang et al. 2015** k = 5 / k ∈ [3,7] — search summary of the paper's own text; the
  Princeton PDF was not fetched.
- **Victory Boogie Woogie** pigment list — search summary of the Gemeentemuseum/RCE study; the
  primary is a 2015 *Journal of Cultural Heritage*-adjacent publication I did not reach.

**Genuine gaps — no answer found:**

- **No measured CIELAB or sRGB values for Mondrian's actual painted planes.** Conservation
  studies identify *pigments*, not colorimetry. Searches for spectrophotometric measurement at
  Kunstmuseum Den Haag returned nothing. Anyone wanting "the De Stijl red" as a number will
  have to measure a reproduction, which is not a measurement of the painting. The §1.1 table
  is therefore *this library's nearest paints to the named pigments*, not Mondrian's colours.
- **No study measuring chroma or colourfulness across art genres or movements.** This is the
  central absence for §3. Every large computational study I found (Graham & Field; Redies;
  Sigaki/Perc/Ribeiro; Kim/Son/Jeong) measures **spatial** statistics — spectral slope, edge
  entropy, complexity, fractal dimension — and where colour appears it is box-counting variety
  rather than saturation level. The claim "abstract art is more saturated" is therefore not so
  much refuted as **never tested at scale**. The Mallon/Redies within-abstract correlation is
  the closest thing to evidence and it points the other way.
- **No count of distinct colours per abstract painting at scale.** The 21-colour figure comes
  from a 6-observer study over 40 paintings of mixed type. Nobody has run it per movement.
