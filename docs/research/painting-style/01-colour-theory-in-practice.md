# Colour Theory as Painters Actually Apply It

**Date:** 2026-07-26
**Scope:** How painters reason about colour when they are *not* copying a photograph, and
which of those reasonings can be expressed as a transform inside PaintTranslator's
existing per-pixel matching pipeline.
**Relationship to the existing research:** [acrylic-blending-findings.md](../acrylic-blending-findings.md)
and the four [source reports](../source-reports/) are about *physical accuracy* — getting
the mixture right. This report is about *artistic choice* — deciding what colour to aim
at in the first place. Where the two overlap (value-range compression, gamut mapping,
the HyAB metric) this report cites the prior work rather than restating it.

**Claim marking**, matching the convention used elsewhere in `docs/research/`:

- `[verified]` — checked against a primary source in this session, or standard colour
  science that any textbook states.
- `[relayed]` — a source asserts it and I did not independently confirm it.
- `[inferred]` — my own reasoning or arithmetic from stated premises.

---

## Contents

1. [The framing that matters](#1-the-framing-that-matters)
2. [Value structure](#2-value-structure)
3. [Colour temperature](#3-colour-temperature)
4. [Chroma and saturation control](#4-chroma-and-saturation-control)
5. [Harmony schemes](#5-harmony-schemes)
6. [Limited palettes](#6-limited-palettes)
7. [Simultaneous contrast and relational colour](#7-simultaneous-contrast-and-relational-colour)
8. [Optical mixing and broken colour](#8-optical-mixing-and-broken-colour)
9. [Where painting instruction and colour science disagree](#9-where-painting-instruction-and-colour-science-disagree)
10. [What the current pipeline already does, in these terms](#10-what-the-current-pipeline-already-does-in-these-terms)
11. [Actionable levers](#11-actionable-levers)
12. [Verification status and gaps](#12-verification-status-and-gaps)

---

## 1. The framing that matters

The single most useful idea for this feature is that **a painting is not a reproduction
with errors in it; it is a deliberately different encoding of the same scene.**

Two constraints force this and painters have built a craft around both:

- **The medium's range is far smaller than the scene's.** Golden's measured Titanium
  White is L\*98.25 and Bone Black L\*23.82 ([verified], values re-confirmed in the prior
  research). Converting through the CIELAB inverse, that is Y ≈ 0.9554 against Y ≈ 0.0405
  — a **23.6:1 reflectance ratio** ([inferred], arithmetic from those L\* values). An
  outdoor scene routinely exceeds that by one to two orders of magnitude before you count
  specular highlights. So the painter cannot preserve absolute values and must decide
  *which relationships* to spend the range on.
- **The viewer applies colour constancy to the painting.** David Briggs' framing is that
  tonal painters must "switch off" the normal processing in which each colour is compared
  with an inferred white, and instead compare each colour against the full range of
  colours in the subject ([relayed] — [The Dimensions of Colour §11.6](http://www.huevaluechroma.com/116.php);
  the site would not fetch from this environment, so this is from search-index text and
  a [Colour Society of Australia talk abstract](https://www.academia.edu/117264611/Colour_Constancy_Illusions_and_Painting)).
  A photograph already contains the "wrong" answer in this sense — it records the light
  arriving, and the painter's job is to re-encode it so that the *viewer's* constancy
  machinery reconstructs the intended scene.

**Consequence for this app.** Nearest-colour matching answers "what is the smallest
achievable error against the photo's pixel values." Painters do not minimise that. They
minimise something closer to "the error in the *relationships* a viewer will reconstruct,
weighted by what matters" — value relationships heavily, hue relationships lightly. Every
lever in §11 is a way of expressing part of that reweighting as a transform this pipeline
can actually apply.

---

## 2. Value structure

### 2.1 Value primacy

Every serious instruction tradition puts value ahead of hue.

- Munsell-based teaching states it as an ordering: **decide value first, then hue and
  chroma.** Graydon Parrish's Munsell method is explicit — "when mixing colors using the
  Munsell system, you begin by deciding on the correct value," with hue and chroma
  adjusted afterwards ([relayed] — [Artists Network](https://www.artistsnetwork.com/art-techniques/rational-color-theory/)).
  In the same piece Parrish is quoted as saying values contribute "up to 80 percent of a
  painting's effectiveness" ([relayed]). **That 80% is a rhetorical figure, not a
  measurement** — treat it as a statement of priority, not a weight.
- The Munsell literature itself treats hue as the least important of the three
  dimensions for painting purposes ([relayed], same source and the general Munsell-for-painters
  literature).
- Richard Schmid's *Alla Prima* is summarised as "value builds form more than detail" and
  "colour harmony comes from comparison" ([relayed] — [summary](https://www.fizzread.ai/moment/alla-prima-everything-i-know-about-painting);
  the book itself is not online, so this is second-hand and should be treated as folk
  summary rather than quotation).

**There is real colour science under this.** S-CIELAB — the spatial extension of CIELAB —
filters an image with contrast sensitivity functions in an opponent space before computing
colour difference, and the filters are **band-pass for the luminance channel and low-pass
for the two chromatic channels** ([verified] — [Johnson & Fairchild, *A Top Down Description
of S-CIELAB and CIEDE2000*](https://www.cis.rit.edu/people/faculty/johnson/pub/ciede_scielab.pdf)).
That is a direct statement that the human visual system resolves spatial structure
principally through luminance, and tolerates far more chromatic error at high spatial
frequency. The painters' rule — get the values right and the colour will forgive you — is
the perceptual consequence of that asymmetry ([inferred], but the inference is short).

This also independently justifies the `LightnessWeight = 1.5` already in
`PaintBlendMatcher`, on grounds separate from the perceptibility-threshold argument the
prior report used.

### 2.2 Value massing and notan

Notan is the Japanese term for the balance of light and dark masses, imported into
Western teaching by Arthur Wesley Dow's *Composition* (1899; commonly cited as 1889)
([relayed] — [Art in Context](https://artincontext.org/notan/), [Alvalyn Creative](https://alvalyn.com/value-mapping-the-notan-principle/)).
The operative technique:

- Reduce the composition to **two values** (black/white) — the pure notan.
- Optionally add one or two mid-greys for a **three- or four-value** study ([relayed], same sources).
- Light shapes are *massed together* and dark shapes are *massed together*; the aim is a
  readable abstract pattern before any colour is chosen ([relayed] — [Virtual Art Academy,
  mass notan](https://www.virtualartacademy.com/mass-notan-painting/)).

The nine-step value scale most painters actually work on comes from **Denman Ross**, early
20th century, and predates Munsell's 0–10 scale in painting instruction ([relayed] —
[Vitruvian Studio](https://vitruvianstudio.com/catalog/9-step-neutral-value-scale/),
[Learning to See](https://www.learning-to-see.co.uk/how-to-make-a-munsell-value-scale)).

**What this means computationally.** Massing is *not* the same as posterising. Posterising
quantises value independently per pixel; massing merges *adjacent regions of similar
value into one shape*, which is a segmentation operation. A pure L\* quantisation gets you
part of the way — it will produce contiguous bands wherever the underlying gradient is
smooth — but it will also shatter noisy or textured regions into speckle. The Gaussian
pre-blur already in the pipeline is doing rough duty here; an edge-preserving filter would
do it better (§11, Lever 12) ([inferred]).

### 2.3 Value compression and key

Painters compress the scene's range into the medium's, and they choose *where* to spend
it. The two named moves:

- **Compress the whole range and let hue/chroma carry the variety.** "By painting within
  compressed value ranges, artists keep the fundamental value structure simple, then rely
  on changes in hue and saturation to inject life" ([relayed] — [Draw Paint Academy](https://drawpaintacademy.com/compressed-values/)).
  The same source gives the qualitative mapping: **more compression reads as more fog,
  mist or distance**; compressing toward the light end gives a high-key shimmering
  picture; toward the dark end gives a moody one ([relayed]). It offers no numbers.
- **Preserve relationships, sacrifice absolute range.** This is exactly the sigmoidal L\*
  rescaling the prior research already recommends for gamut reasons (see
  [acrylic-blending-findings.md §3](../acrylic-blending-findings.md), Tier-2 item 14). The
  artistic version adds two more controls on top of it: overall contrast (how steep the
  sigmoid) and key (where its midpoint sits).

**Two colour-appearance effects say this compression must be done with care, not just
scaled:**

- **The Stevens effect** — perceived contrast increases with luminance level ([verified],
  standard colour appearance; see [Fairchild, *Color Appearance Models*](https://scis.uohyd.ac.in/~chakcs/cipclass/lecs/ColourAppearance.pdf)).
  A painting hanging at gallery luminance is far dimmer than a sunlit scene, so a
  *linearly* range-compressed reproduction reads as flatter than the compression alone
  would predict. Painters overcompensate by exaggerating the separation between the main
  value masses. This is the perceptual argument for an **S-curve** rather than a linear
  remap.
- **The Hunt effect** — colourfulness increases with luminance ([verified], same source;
  [Wikipedia](https://en.wikipedia.org/wiki/Hunt_effect_(color))). In CIECAM02 this appears
  as the `F_L^0.25` term multiplying chroma to give colourfulness ([relayed], the exact
  exponent is standard but I did not re-derive it — see [Luo & Li, *CIECAM02 and Its Recent
  Developments*](https://cielab.xyz/pdf/CIECAM02_and_Its_Recent_Developments.pdf)). The
  practical consequence: **compressing luminance without also adjusting chroma leaves the
  image looking wrong**, and HDR tone-mapping literature says the same thing explicitly —
  "compression of luminances should be accompanied by a corresponding adjustment of chroma"
  ([relayed] — [Gamut-Mapping Framework for HDR](https://arxiv.org/pdf/1711.08925)).

That last point is a genuine, non-obvious coupling: **value compression and chroma control
are not independent levers.** If the app compresses L\* it should raise chroma slightly to
compensate, not lower it.

---

## 3. Colour temperature

### 3.1 What painters do with it

Warm/cool is used as a *working axis* — a second dimension after value, before full hue —
and it is used for four distinct jobs:

1. **Light versus shadow.** Warm light implies cooler shadow. Gurney's careful phrasing is
   "warm light, *cooler* shadows" — the shadow is not receiving the warm source, so
   whatever else reaches it is comparatively cooler, "but not necessarily a cool colour"
   ([verified] against [Gurney Journey](http://gurneyjourney.blogspot.com/2020/02/light-temperature.html)
   and the summary of his warm/cool posts).
2. **Modelling form by plane orientation.** Outdoors in shadow, **up-facing planes go cool
   and down-facing planes go warm**, because up-facing planes see more sky and down-facing
   planes see more bounce off the ground ([verified] — [Gurney Journey, colour temperature
   in the shadow out-of-doors](http://gurneyjourney.blogspot.com/2014/05/color-temperature-in-shadow-out-of-doors.html)).
   Gurney notes these shifts "can occur at nearly equal value" and still read.
3. **Weaving.** Warm and cool threads distributed across the whole picture rather than
   segregated ([relayed] — [Gurney Journey](http://gurneyjourney.blogspot.com/2014/11/weaving-warm-and-cool-threads.html)).
4. **As a training wheel for value.** Gurney's two-pigment warm/cool exercise exists
   precisely to keep the student "from straying too far away from making primarily
   value-oriented decisions" ([verified] — [Gurney Journey](http://gurneyjourney.blogspot.com/2018/06/how-to-apply-warm-and-cool-approach.html)).

Gurney himself warns against the rule being fixed: with strong warm bounce or a warm
secondary source, shadows can be warmer than the lights ([verified], same sources).

### 3.2 Does "temperature" map onto anything measurable?

Partly. This is one of the places where the folklore is measurably imprecise.

**Correlated colour temperature is the wrong quantity and it runs backwards.** CCT is
defined by the blackbody locus, so a "warm" reddish light is a *low* CCT (2700 K) and a
"cool" bluish one a *high* CCT (5000 K+) ([verified], standard; e.g.
[LEDSAVE](https://ledsave.co.uk/blog/what-is-correlated-colour-temperature/)). CCT also
only describes near-neutral illuminants; it says nothing useful about a saturated surface
colour. It should not appear anywhere in this app's model of warm/cool.

**There is a measured perceptual warm–cool axis, and it does not line up with the hue
circle the way painters assume.** A 2025 *Journal of Vision* study had 25 observers rate 36
hue angles in cone-opponent space on a 7-point warm–cool scale at three luminance levels
([verified] — [Warm versus cool colors and their relation to color perception, PMC12025320](https://pmc.ncbi.nlm.nih.gov/articles/PMC12025320/)):

| Quantity | Cone-opponent hue angle (at 20 cd/m²) |
|---|---|
| Warm peak | 341.7° ± 25.3° (orangish-red) |
| Cool peak | 146.9° ± 30.5° (greenish-blue) |
| Boundary 1 | 58.4° ± 24.3° |
| Boundary 2 | 249.1° ± 18.7° |

Two findings from that paper matter here:

- The warm and cool peaks correlate only **weakly** with the unique hues (unique red at
  350.2°, unique blue at 139.9°), so warm–cool is a **distinct dimension**, not a relabel
  of the red–blue axis ([verified]).
- **The warm–cool peaks land on the *minima* of the CIELAB saturation contours, and the
  warm–cool boundaries land on the *maxima***  ([verified], same paper). In other words the
  colours people call most emphatically warm or cool are, in CIELAB terms, the ones that
  *can't* be very saturated; the most saturated hues sit at the boundary where warm/cool
  judgements are ambiguous. Any implementation that treats "warm" as "high chroma toward
  red" will be modelling something other than what observers report.

A separate line of work (Oh & Kim, *Color Research & Application*) models warm–cool as a
cosine of CIELAB hue angle with chroma and lightness terms, with the **warmest hue at
CIELAB h ≈ 50°** ([relayed] — [Oh 2022](https://onlinelibrary.wiley.com/doi/full/10.1002/col.22764);
paywalled, figure taken from search summary). h = 50° is orange, which is broadly
consistent with the cone-opponent 341.7° result. A newer chromaticity-based warm–cool model
integrating the neutral white locus exists ([relayed], paywalled —
[Chang 2026](https://onlinelibrary.wiley.com/doi/10.1002/col.70063)).

**Bruce MacEvoy's objective criterion** is the most implementable definition I found. He
argues all saturated warm hues share a reflectance signature: a sharp "cliff" between the
cyan and yellow wavelengths, maximum reflectance from the cliff to the red end, and little
or none from the cliff to the violet end ([verified] against
[handprint, colour temperature](https://www.handprint.com/HP/WCL/color12.html)). Because
this app already carries a 38-band spectrum per candidate, **that criterion is directly
computable from data the pipeline already has** — it does not require a hue-angle proxy.

MacEvoy also demolishes the standard rationalisations: that "blue recedes" because distant
mountains look blue is unfounded, and the chromatic-aberration explanation for warm colours
advancing fails because the eye is adapted to cancel chromatic aberration. What actually
drives "advancing" and mood is **lightness and chroma, not hue** — at matched lightness and
chroma, cool colours advance and arouse as much as warm ones ([verified], same page).

**Briggs is blunter still.** He argues that painters who lack the concepts of saturation
and brilliance fall back on "colour temperature" to describe things it cannot describe —
"highlights are cool (because white is a cool), full-lights are warm but half-lights are
cool (because grey is a cool)" — and that this is "clumsy and inadequate," with different
teachers using the terms incompatibly ([relayed], from search-index text of
[huevaluechroma.com](http://www.huevaluechroma.com/); the site would not fetch here).

**The hue-heat hypothesis** — that warm-hued environments actually *feel* warmer — is
investigated but not settled; the encyclopaedic summary is a stub citing a 2016 paper that
questions the effect outright ([relayed] — [Wikipedia](https://en.wikipedia.org/wiki/Hue-heat_hypothesis)).
Do not build anything on it.

### 3.3 The defensible operational definition for this app

Given the above, the honest position is: **"temperature" is best implemented as a signed
projection onto a chosen axis in the a\*b\* plane, with the axis direction exposed as a
parameter rather than asserted.** A default of h ≈ 50–60° for the warm pole is supported by
two independent modelling papers ([relayed] × 2, converging). Treat any claim that a
specific hue "is" warm or cool in isolation as folklore; treat "this colour is warmer than
that one, along this axis" as measurable ([inferred]).

---

## 4. Chroma and saturation control

### 4.1 Why unmodified photo saturation reads wrong

Four separate mechanisms, which get conflated in painting instruction:

1. **Gamut.** The palette simply cannot reach some of the photo's colours. Note the prior
   research's counter-intuitive finding: this cuts *both* ways — 31% of achievable Golden
   acrylic colours fall outside sRGB, concentrated in phthalos, quinacridones and cadmiums
   ([relayed] via prior research, source paywalled). The paint gamut is not a subset of the
   photo gamut.
2. **Chroma versus saturation.** These are different quantities and the distinction is
   load-bearing. Chroma is colourfulness relative to a same-lightness white; saturation is
   colourfulness relative to the stimulus's own brightness. Under a uniform reduction in
   illumination, **saturation is preserved while chroma falls roughly with lightness**
   ([verified], this is the definition; the practical framing appears in
   [polycount's discussion of real vs cartoon shadows](https://polycount.com/discussion/163884/cartoon-shadows-are-saturated)
   [relayed]). A painter saying "shadows are less saturated" is almost always describing a
   *chroma* fall-off. A photo already contains that fall-off; the painter exaggerates it.
3. **The Hunt effect.** Colourfulness rises with luminance ([verified], §2.3). A painting
   viewed at gallery light will read as less colourful than the sunlit scene it depicts,
   at equal measured chroma.
4. **Surround and adaptation.** A photo is an emissive sRGB encoding assuming a specific
   surround; the painting is a reflective surface in an unknown room. The prior research
   already argues against trying to model this with CAM16 because the required parameters
   are unknowable for a hobbyist ([relayed] via prior research). That argument stands.

**A genuine contradiction worth flagging.** Painting instruction says photographs are
oversaturated. Preferred-colour-reproduction research says the opposite for *viewers*:
observers reliably prefer reproduced object colours **more** saturated than the original,
preferred blue sky is higher purity than real sky, and long-term memory colours have higher
chroma than immediate memory ([relayed] — [Memory Color Based Preferred Color Reproduction,
IS&T](https://library.imaging.org/admin/apis/public/api/ist/website/downloadArticle/cic/16/1/art00058)).
Both can be true: viewers prefer boosted chroma in a *photograph*, and painters reduce
chroma to make a *painting* cohere. But it means "reduce saturation because photos are
oversaturated" is not a claim colour science supports as stated — the real argument is about
**relative** chroma structure, not absolute level ([inferred]).

### 4.2 The chroma curve

The most concrete painters' model I found. Working from Munsell measurements of painted
spheres and cubes, the observation is that across the modelling factors — highlight, light,
halftone, terminator/shadow, reflected light — **hue stays roughly constant while value and
chroma both decrease into shadow, tracing a curve on the Munsell hue page** ([relayed] —
[Learning to See, *The Chroma Curve*](https://www.learning-to-see.co.uk/the-chroma-curve-how-to-paint-light-and-shadow)).
The practical payoff stated there: mix the five points on the curve and interpolate between
them, and you cannot accidentally shift hue.

This is *directly implementable* as a chroma-versus-lightness function and is the strongest
single justification for Lever 4 in §11.

Note the tension with §3.1's plane-orientation rule: the chroma curve says hue is roughly
constant through the light-to-shadow series, while the warm/cool tradition says hue shifts
by plane. They are describing different things — the chroma curve is a single surface under
a single dominant light; the temperature shift is what happens when a second source (sky,
bounce) contributes. Neither is wrong ([inferred]).

### 4.3 Distance

Aerial perspective is the one place where the painting rule has clean physics behind it.
Rayleigh scattering sends short wavelengths preferentially into the line of sight; blue at
400 nm scatters roughly 9.4× as much as red at 700 nm ([relayed] — figure via
[Grokipedia's aerial perspective entry](https://grokipedia.com/page/Aerial_perspective),
which cites EBSCO; the λ⁻⁴ law itself is [verified] and gives (700/400)⁴ = 9.38,
so the number checks out by arithmetic [verified]).

The consequences painters encode: with distance, **contrast compresses, chroma falls, and
hue migrates toward the sky/haze colour** ([relayed], same source and general instruction).
Formally this is a per-pixel linear blend toward an atmospheric colour with a
depth-dependent weight — but **this app has no depth information**, so it can only be
approximated by a user-painted mask or a focal-distance proxy. Flag it as spatial.

### 4.4 Chroma at the focal point

Standard compositional teaching: the lightest light, darkest dark, hardest edge, greatest
detail and **highest chroma should coincide at the centre of interest**, and everything
else should be subordinated ([relayed], consistent across
[John Lovett](https://www.johnlovett.com/focal-point),
[Carol Douglas](https://www.watch-me-paint.com/five-ways-to-create-focal-points/),
[Laura Robb](https://www.laurarobb.com/blog/194887/focal-point)). I found no quantitative
study of this; it is craft consensus, not measured. But it is unambiguous craft consensus,
and it is implementable if the user supplies the focal point.

### 4.5 Chromatic greys and complementary neutralisation

The claim is that greys mixed from complementary pigments read better than greys mixed from
black and white — "a portrait painted with chromatic shadows feels unified, while one
painted with neutral grey shadows often looks flat" ([relayed] — widely repeated; see
[Laura Longoni](https://www.lauralongoniart.com/en/post/what-are-neutrals-a-definition-and-colour-combinations-for-chromatic-neutral-greys-in-acrylic-paints)).
Gurney's gamut-masking method makes it a rule: neutrals come from complementary mixing, not
from black ([verified] — [Gurney Journey, Part 3](https://gurneyjourney.blogspot.com/2011/09/part-3-gamut-masking-method.html)).

**Is there anything to it?** Two things, one solid and one not:

- **Solid.** A complementary mix is not neutral — it lands at low but nonzero chroma, and
  *which* low chroma depends on the parents. The prior research's own measurement is the
  quantitative version: mixes with 120–150° hue separation retain **14%** of parent chroma,
  90–120° retain **22%** ([relayed] via prior research, derived from the Golden spectra).
  So "chromatic grey" is a real, distinguishable class of colours occupying a shell around
  the neutral axis. Black-plus-white lands on the axis itself.
- **Not solid.** The claim that these read as *better* is aesthetic assertion. I found no
  psychophysics on it.

**Important for this app: the distinction is already emergent, not something to add.**
Because `BuildCandidates` samples every pair and triple through the Kubelka–Munk kernel,
the achievable gamut *already contains* the chromatic greys and *already* has very few
truly neutral points unless the palette contains a black. The relevant lever is not "make
chromatic greys" but "**prefer near-neutral candidates that are off-axis over ones on it**"
— i.e. a small bonus for candidates with C\* in a low but nonzero band ([inferred]).

---

## 5. Harmony schemes

### 5.1 The schemes as taught

Analogous (adjacent hues), complementary (opposite), split-complementary, triadic,
tetradic. These descend from Itten and, before him, Chevreul and Goethe. The formalised
computational version is **Matsuda's 80 colour schemes (1995)**, reduced by Cohen-Or et al.
to **eight harmonic templates over the hue wheel** — types i, V, L, I, T, Y, X and N — each
defined as one or two contiguous sectors, free to rotate to any angle ([verified] that this
is the structure — [Cohen-Or, Sorkine, Gal, Leyvand & Xu, *Color Harmonization*, SIGGRAPH
2006](https://dl.acm.org/doi/10.1145/1179352.1141933), [PDF](https://igl.ethz.ch/projects/color-harmonization/harmonization.pdf);
**the exact sector widths in degrees I could not extract** — the PDF would not render as
text in this environment and the widths live in an appendix, so treat any specific figure
as unconfirmed).

Cohen-Or's method: build the image's hue histogram, fit the best template by minimising
total hue-distance-to-template weighted by saturation, then shift each pixel's hue into the
nearest template sector. **This is prior art for the "restrict the candidate gamut to a hue
wedge" lever, and the fitting objective is worth copying.** A useful later critique and
refinement is [*A Geometric Approach to Harmonic Color Palette Design*](https://arxiv.org/pdf/1709.02252)
([relayed], also unreadable as text here), which notes that most of Matsuda's hue patterns
except types L and T collapse to just three — analogous, opposite and triad.

### 5.2 What the evidence actually says

This is the section where the folklore takes the most damage, and the source is good.

**Schloss & Palmer (2011)**, *Aesthetic response to color combinations: preference, harmony,
and similarity*, *Attention, Perception & Psychophysics* 73:551–571 — 1,431 colour pairs from
54 systematically-sampled CIELAB colours, rated by observers on separate preference and
harmony scales ([verified] — free full text at
[PMC3037488](https://pmc.ncbi.nlm.nih.gov/articles/PMC3037488/)):

| Result | Value |
|---|---|
| Best model for **pair harmony** | 67.3% of variance (72.6% with all factors) |
| Predictors of harmony | greater hue similarity, cooler colours, **lower** saturation, similar coolness between components |
| Best model for **pair preference** | 53.5% of variance (62.9% adding component-colour preferences) |
| Predictors of preference | hue similarity, coolness, **lightness contrast** figure-to-ground |
| Harmony ↔ preference correlation | r = +0.79 (62% shared variance) |
| Component colour preference explains… | 21.7% of *preference* variance but only **1.4%** of *harmony* variance |

And the two results that bear directly on the schemes:

- **Complementary pairs were rated reliably *less* harmonious than adjacent hues**
  (F(1,47) = 17.67, p < .001) ([verified]).
- **Preference showed "little evidence" for complementary pairs**; only orange–blue
  exceeded adjacent pairs (F(1,47) = 11.17, p < .008) ([verified]).
- **Both harmony and preference peaked when the two hues were identical and decreased
  monotonically with hue difference** ([verified]).

So: **the analogous scheme is empirically supported; the complementary scheme, as a claim
about harmony, is empirically contradicted.** The one survivor is orange–blue, which is
also the one complementary pair that maps onto natural illumination (warm sun, blue sky) —
plausibly why painters keep finding it works ([inferred]).

Ou & Luo's earlier two-colour harmony model (*Color Research & Application* 2006, 1,431
pairs) is the other standard reference; it likewise models harmony from lightness, chroma
and hue terms rather than from wheel geometry ([relayed] —
[abstract](https://onlinelibrary.wiley.com/doi/abs/10.1002/col.20208), paywalled;
[preprint PDF](https://www.researchgate.net/profile/Ming-Luo-13/publication/229906278_A_color_harmony_model_for_two-color_combinations/links/5c0c957192851c39ebde1ea2/A-color-harmony-model-for-two-color-combinations.pdf)).
Note a known inconsistency between models: neutral colours read as cool in Ou's model and
warm in Koo's ([relayed]).

**Briggs lists "simple rules to establish harmonious colour combinations" as one of four
major misconceptions taught throughout the education system**, alongside primary colours,
the hue circle, and vague colour naming ([relayed], from search-index text of Briggs'
material). **MacEvoy**, discussing NCS complementaries, states flatly that "there is no
evidence that these are more (or less) pleasing or effective than the complementaries
defined in other ways" ([verified] against
[handprint](https://www.handprint.com/HP/WCL/color7.html)).

### 5.3 The devices that do work

Two harmony devices survive scrutiny better than the wheel schemes, because they are about
*restriction* rather than about *geometry*:

- **Mother colour.** Mix a small amount of one chosen colour into every mixture on the
  palette, including the white. Attributed to Edgar Payne ([relayed] — [LiveAbout](https://www.liveabout.com/definition-of-mother-color-2577647),
  [Michael Chesley Johnson](https://mchesleyjohnson.blogspot.com/2008/02/mother-color.html)).
  Mechanically this is a **contraction of the whole gamut toward one point**, which
  compresses hue and chroma variance globally. That is a real, measurable statistical
  effect, not a wheel-geometry claim ([inferred]).
- **Gamut masking.** Gurney's formalisation: draw a shape (usually a triangle) on the
  colour wheel, treat its corners as "subjective primaries," and use only colours inside
  it. Effects claimed: colour unity and a specific mood; and — the interesting one — **a
  neutral grey mixed inside one gamut reads as a distinctly different colour inside
  another** ([verified] — [Gurney Journey, Parts 2](http://gurneyjourney.blogspot.com/2011/09/part-2-gamut-masking-method.html)
  and [3](https://gurneyjourney.blogspot.com/2011/09/part-3-gamut-masking-method.html)).
  That last observation is simultaneous contrast doing the work, not the geometry.

  There is a published implementation: Song, Lau & Süsstrunk, *An Interactive Tool for
  Gamut Masking*, IS&T/SPIE Electronic Imaging 2014, EPFL IVRL. It extracts a **3-D colour
  gamut from a 2-D user-drawn mask** using a **voxel-grid gamut representation** (so any
  mask shape works), and maps the image via a cluster-based representation, demonstrating
  warm and cool renderings of the same photograph ([verified] against
  [EPFL IVRL project page](https://www.epfl.ch/labs/ivrl/research/color/gamut_masking/)).
  **This is the closest published prior art to the feature being considered**, and its
  voxel-gamut structure is a near-relative of `CandidateSet`'s existing 3-D CIELAB grid.

### 5.4 Dominance ratios

The "60-30-10 rule" (60% dominant, 30% secondary, 10% accent) is **interior-design
folklore that migrated into art and UI advice**; the sources tracing it put its origin in
interior design, from which it crossed into fashion, graphic design and web design
([relayed] — [freeCodeCamp](https://www.freecodecamp.org/news/the-60-30-10-rule-in-design/),
[Apartment Therapy](https://www.apartmenttherapy.com/interior-design-rule-60-30-10-explained-37504313)).
Some sources rationalise it via the golden section, which is not evidence. I found **no
study measuring area-proportion of hues in paintings against aesthetic judgement**.

Treat dominance as a real *observation* about how paintings tend to look (one hue family
dominates, one supports, one accents) and as an unsupported *prescription* about exact
proportions ([inferred]). If the app implements it, implement it as a soft bias, and do
not put "60/30/10" in the UI.

---

## 6. Limited palettes

### 6.1 The named palettes

| Palette | Contents | Notes |
|---|---|---|
| **Zorn** | Yellow ochre, cadmium red (orig. vermilion), ivory black, white (orig. flake) | Four single-pigment paints; the "Apelles" tetrachromatic ancestor is attributed to Pliny on Apelles of Kos ([relayed] — [Jackson's](https://www.jacksonsart.com/blog/2021/02/02/colour-mixing-exploring-the-zorn-palette/)) |
| **Earth palette** | Yellow ochre, raw/burnt sienna, raw/burnt umber, black, white | Sorolla's *studio portrait* palette is essentially this plus rose madder, Naples yellow, vermilion and cobalt blue ([relayed] — [Natural Pigments](https://www.naturalpigments.com/artist-materials/joaquin-sorolla-palette)) |
| **Sorolla outdoor** | Cobalt violet, rose madder, all the cadmium reds, cadmium orange, all the cadmium yellows, yellow ochre, chrome green, viridian, Prussian blue, cobalt blue, French ultramarine, lead white | Not limited at all — the point is he had **two different palettes for two different jobs** ([relayed], same source) |
| **Sargent** | Ultramarine, Vandyke brown, viridian among signature colours | Characterised as restrained tonal harmonies against Sorolla's exaggerated temperature contrast ([relayed], secondary sources only — treat as weak) |
| **Split primary** | Warm + cool version of each of yellow, red, blue | See below |

**The Zorn palette's actual limitation is blue.** Cadmium red light is its most saturated
colour; the nearest thing to blue is the slightly-blue cool grey from ivory black plus
white ([relayed] — [Draw Paint Academy](https://drawpaintacademy.com/zorn-palette/),
[Jackson's](https://www.jacksonsart.com/blog/2021/02/02/colour-mixing-exploring-the-zorn-palette/)).
That is precisely why it works for portraits (flesh sits in a narrow warm hue band — Parrish
puts flesh at roughly 7.5R–7.5YR, **chroma 2–6**, much lower chroma than intuition suggests
[relayed]) and fails for landscape.

**MacEvoy demolishes the split-primary rationale** while defending the palette. The
justification — that standard primaries are "impure" and splitting them yields cleaner
mixtures — rests on three false premises: not all colours derive from three light
primaries; mixing limits come from the structure of colour vision, not paint impurity (they
appear even in pure light mixtures); and the brightest mixtures do *not* require only
primaries — adding orange, green or purple paints works better. His conclusion is that the
split primary palette is worth using because it **restricts the chroma of green and purple
mixtures**, producing "characteristically contrasty, brightly illuminated" paintings — an
artistic effect, achieved for a reason unrelated to the theory used to sell it ([verified]
against [handprint, split "primary" palette](https://www.handprint.com/HP/WCL/palette4r.html)).

**That is the general shape of the limited-palette argument, and it is the one this app
should adopt: a limited palette is valuable because of what it *cannot* reach.**

### 6.2 What a limited palette does to a scene's colour statistics

The only large-scale quantitative work I found is Kim, Son & Jeong, *Large-Scale
Quantitative Analysis of Painting Arts*, *Scientific Reports* 4:7370 (2014) — 8,798
paintings across 10 historical periods ([verified] —
[nature.com/articles/srep07370](https://www.nature.com/articles/srep07370), open access,
though it required following a redirect chain to read):

- **Rank-ordered colour-usage distributions** show a **universal long-tailed curve** across
  all ten periods, distinct from the binomial you would get from random colour selection.
- **Photographs show a different tail from paintings.** Applying an "oil painting filter"
  to photographs produced clear changes in the tail. **Hyperrealist paintings showed no
  measurable difference from photographs** — which is a neat internal validation of the
  measure.
- **Box-counting dimension in RGB space** (a measure of palette variety): medieval ≈ **2.4**,
  most other periods **2.6–2.8**, Pollock's drip paintings ≈ **2.35**. The low medieval
  figure is attributed to genuinely limited pigment availability.
- **The roughness exponent α of the brightness field increases monotonically across the ten
  periods**, tracking the introduction of chiaroscuro and sfumato.

**Read that as: the measurable signature of a painting versus a photograph lives in the
*tail* of the colour-usage distribution and in the *spatial roughness of brightness*, not
in mean hue or mean saturation.** That is a direct warning to any implementation that tries
to make images look "painterly" by shifting global colour statistics: the Reinhard-style
mean/σ transfer (§11, Lever 8) is measuring the wrong moments ([inferred] — this is my
reading, the paper does not say it).

**The good news for this app: it already produces the right kind of statistic.** Snapping
every pixel onto a discrete achievable gamut is exactly a long-tailed rank-ordered colour
distribution with a truncated tail. The limited-palette effect is already there, for free,
and the style feature should not duplicate it.

**A measurement the app could run on itself.** `PalettePhotoConverter.SampleAchievableColors`
already returns the full achievable gamut for any palette. Converting that to CIELAB and
reporting hue-angle coverage, chroma range and L\* range would give the user a real,
measured gamut summary for their tube selection — including "your palette cannot reach
blue" for a Zorn-like selection — with no new physics required ([inferred]).

---

## 7. Simultaneous contrast and relational colour

### 7.1 The problem for a per-pixel matcher

The claim painters make is that **a colour has no fixed appearance; it is read against its
neighbours.** Gurney's gamut-masking observation is the cleanest example: the *same*
neutral grey reads as a different colour depending on which gamut surrounds it ([verified],
§5.3).

The current converter matches every pixel independently against the target's own CIELAB
coordinates. Two distinct failures follow ([inferred]):

1. **The error is measured in the wrong space.** Two pixels that differ by ΔE 3 in
   isolation may be perceptually indistinguishable at high spatial frequency (chromatic
   channels are low-pass, §2.1) or glaringly different across a large soft gradient.
2. **The palette's compressed range makes local contrast *systematically* too low.** When
   every value is squeezed into 24:1, all local contrasts shrink together. A painter
   compensates by *increasing* separation at the boundaries that matter — the terminator,
   the focal edge — spending range where it reads. A per-pixel matcher cannot know which
   boundaries those are.

### 7.2 What a spatially-aware objective would need

The established machinery:

- **S-CIELAB**: spatially pre-filter the image in an opponent space with CSF-derived
  filters (band-pass luminance, low-pass chroma) before computing per-pixel ΔE. This gave
  more accurate predictions of perceptual image distortion than point-by-point CIELAB
  ([verified] — [Johnson & Fairchild](https://www.cis.rit.edu/people/faculty/johnson/pub/ciede_scielab.pdf)).
  **This is the cheapest upgrade available**: it is a pre-filter, so the matching stays
  per-pixel. The catch is that the *candidate* side would also need filtering to be
  consistent, which it cannot be — candidates have no spatial extent ([inferred]).
- **CIECAM-based induction models.** Luo et al., *Quantifying colour appearance, Part V:
  simultaneous contrast*, *Color Research & Application* 20:18 (1995) is the reference
  ([relayed], paywalled — [Wiley](https://onlinelibrary.wiley.com/doi/10.1002/col.5080200105)).
- **Induction magnitude.** It grows with the target–surround difference, is stronger for
  more saturated surrounds, and has separate fast and slow mechanisms ([relayed] — survey
  via [What predicts the strength of simultaneous color contrast?, JOV](https://jov.arvojournals.org/article.aspx?articleid=2605429)
  and [PMC6153537](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6153537/)). I did not find a
  single agreed magnitude figure, and the individual variability is large enough that the
  factor-analysis paper above exists specifically to characterise it. **Do not hard-code an
  induction strength.**

**Honest assessment:** a genuinely relational objective would turn per-pixel nearest-colour
lookup into a global optimisation over the whole image with neighbour terms — a different
program, orders of magnitude slower, with no evidence it would look better. The cheap
approximations (local contrast enhancement on L\* before matching; edge-preserving
pre-filtering) capture most of the practical benefit ([inferred]).

---

## 8. Optical mixing and broken colour

### 8.1 What the Divisionists believed and what is actually true

The Neo-Impressionist claim was that placing small dots of unmixed colour side by side and
letting the eye combine them achieves "the maximum luminosity scientifically possible,"
because optical mixing moves toward white whereas pigment mixing moves toward black
([relayed] — [Tate](https://www.tate.org.uk/art/art-terms/n/neo-impressionism), and the
general Divisionist literature).

**This is wrong, and the error is traceable.** Seurat appears to have misread Charles Blanc
and Ogden Rood; neither text makes the claim ([relayed] — widely asserted in the
art-historical literature, e.g. [Wikipedia, Divisionism](https://en.wikipedia.org/wiki/Divisionism);
the specific "Seurat Delusion" essay I tried to fetch returned 404, so I could not check
its argument directly). What Rood described is **partitive (additive-averaging) mixing**,
not additive mixing of lights ([relayed], same). The distinction is decisive:

- **Additive mixing of lights**: luminances *sum*. Red light plus green light is brighter
  than either.
- **Partitive mixing of adjacent reflective marks**: luminances *average*, weighted by
  area. Red paint next to green paint is exactly as bright as the area-weighted average,
  which is **never brighter than the brighter mark** ([verified] — this follows from the
  definition of an area average).

So broken colour cannot brighten anything. What it *does* do is real and worth having:
**it avoids the chroma loss of subtractive mixing.** Two paints physically mixed at 120–150°
hue separation retain roughly 14% of parent chroma ([relayed] via prior research); the same
two placed side by side and averaged optically retain the chroma of the linear-light
average of the two, which is far more ([inferred], but it follows straightforwardly). That
is the actual reason Impressionist greys look alive and mixed greys look dead.

### 8.2 The spatial-frequency constraint

Fusion requires the marks to fall at or below the eye's resolution limit, roughly 1 arcmin
for high-contrast detail. At 1 arcmin, a mark of size *s* fuses at distance
*d* = *s* / tan(1′) = *s* / 0.000291 ([verified], arithmetic):

| Mark size | Fusion distance |
|---|---|
| 0.2 mm | 0.7 m |
| 1 mm | 3.4 m |
| 4 mm (Seurat-scale dot) | **13.7 m** |

*La Grande Jatte* is about 2 × 3 m and is normally viewed from a few metres. **Seurat's
dots do not fuse at normal viewing distance** ([inferred], from the arithmetic above plus
the painting's known dimensions). What the viewer gets is partial fusion plus visible
texture — which is arguably the whole aesthetic effect.

**Implication for this app**: an optical-mixing render mode's dot size is set by the
*viewer's* distance and the *printed* size, neither of which the program knows. It must be
a user parameter, and the honest UI framing is "at what distance is this meant to be seen."

### 8.3 The conflict with everything else in this pipeline

The prior research argues against dithering, and the reasons stand: error diffusion leaves
no contiguous paintable regions; dot size is set by viewing distance not by the algorithm;
and **wet acrylic touching wet acrylic mixes subtractively anyway**, destroying the
partitive effect ([relayed] via prior research). All three are still true.

But note the framing difference. Those are arguments against dithering as an *error-hiding*
technique inside the existing renderer. Broken colour as a **deliberate, coarse, stroke-scale
rendering mode** — where the marks are meant to be visible, the pattern is regular rather
than error-diffused, and the user is told to let each stroke dry — is a different feature
that the same objections do not fully cover ([inferred]). It is the one genuinely
*alternative* rendering the app could offer, as opposed to a bias on the existing one.

---

## 9. Where painting instruction and colour science disagree

| Painting claim | Status | Detail |
|---|---|---|
| Value matters more than hue | **Supported** | S-CIELAB's band-pass luminance / low-pass chroma filters are the mechanism ([verified]) |
| "Values are 80% of a painting" | **Folklore** | Rhetorical figure; no measurement behind it ([relayed] as a quote, not as a fact) |
| Warm light implies cool shadow | **Supported outdoors** | Sun plus blue skylight is a real two-illuminant situation; Gurney himself says it is not a fixed rule ([verified]) |
| "Warm colours advance" | **Not supported** | MacEvoy: at matched lightness and chroma, cool advances equally; the effect is lightness and chroma, not hue ([verified]) |
| Warm/cool is a clean hue-circle division | **Partly wrong** | Warm–cool peaks fall at CIELAB **saturation minima**, boundaries at maxima; warm–cool is a separate dimension from unique hues ([verified]) |
| Colour temperature = CCT | **Wrong** | CCT runs backwards and only describes near-neutral illuminants ([verified]) |
| Complementary schemes are harmonious | **Contradicted** | Complement pairs rated reliably *less* harmonious than adjacent hues, F(1,47)=17.67, p<.001 ([verified]) |
| Analogous schemes are harmonious | **Supported** | Harmony peaks at identical hue and falls monotonically with hue difference ([verified]) |
| 60-30-10 dominance | **Folklore** | Interior-design origin; no painting evidence found ([relayed]) |
| Chromatic greys beat neutral greys | **Half** | The two are genuinely distinguishable classes ([relayed], measured); "better" is untested |
| Shadows are less saturated | **Terminology error** | Physically, *chroma* falls with lightness while *saturation* is preserved under uniform illumination change ([verified] by definition) |
| Photos are oversaturated | **Contested** | Viewers prefer *more* chroma than accurate in photographs ([relayed]); the real painters' claim is about relative structure |
| Optical mixing is more luminous | **Wrong** | Partitive mixing averages luminance; it cannot exceed the brighter component ([verified]) |
| Optical mixing preserves chroma | **Right** | It avoids subtractive chroma collapse ([inferred] from measured mixing data) |
| Split primaries give purer mixtures | **Wrong rationale, right practice** | MacEvoy: the benefit is *restricting* green and purple chroma ([verified]) |

---

## 10. What the current pipeline already does, in these terms

Reading `Imaging/PalettePhotoConverter.cs` and `Imaging/PaintBlendMatcher.cs` against the
above:

- **The converter's distance function is unweighted squared CIELAB.** `NearestCandidateArgb`
  computes `dl*dl + da*da + db*db` with no lightness weight ([verified] by reading the
  source, lines around the `Examine` local function). **`PaintBlendMatcher` uses
  `LightnessWeight = 1.5` with a HyAB-style form; the converter does not.** The two halves
  of the app therefore disagree about what "closest" means, and the half that produces the
  *image* is the one using the metric the prior research recommends against. This is the
  cheapest correctness-adjacent improvement available and it is also Lever 1.
- **The per-pixel cache is keyed on colour only.** `MapPixelsFlat` builds `mapped[CacheKey]`
  over 6-bit-quantised RGB and resolves each distinct colour once. **Any lever that depends
  on pixel position invalidates this cache** — that is the single hard architectural
  constraint on the whole feature.
- **`BuildCandidates` is where gamut restriction belongs.** It already produces a
  deduplicated candidate array with precomputed L\*a\*b\* and a 3-D grid index. Filtering or
  transforming candidates there costs nothing at match time and keeps every downstream
  guarantee (the achievable colours are still real mixtures).
- **The Gaussian pre-blur is the only spatial operation**, and it is value-destroying at the
  edges that matter most.
- **`GamutMapper` and `SpectralRenderer.ToDisplayColor`** already handle out-of-sRGB
  candidates and report `chromaLost`, so a chroma-manipulating lever has somewhere honest to
  report clipping.

---

## 11. Actionable levers

Ordered roughly by confidence × cheapness. "Per-pixel safe" means the transform depends only
on the pixel's own colour, so the existing 6-bit colour cache in `MapPixelsFlat` stays valid.

### The architecture that makes most of this cheap

Nine of the twelve levers below are one of two things:

- **A remap of the target colour in CIELAB before matching** — a pure function
  `(L*, a*, b*) → (L*, a*, b*)`. All of these compose into one function applied inside
  `NearestCandidateArgb` right after `RgbToLab`, and all are per-pixel safe.
- **A filter or transform on the candidate set** in `BuildCandidates`, applied once at build
  time, costing nothing per pixel.

Only the metric weighting (Lever 1), the focal-point levers (9, 10) and the spatial levers
(11, 12, 13) fall outside that. Building the style feature as "a `LabRemap` delegate plus a
`CandidateFilter` delegate" would cover most of the design space with one small change to
the converter's signature ([inferred]).

---

### Lever 1 — Weight lightness in the converter's distance function

- **Artistic effect:** Value structure survives when the palette cannot reach the target.
  When a colour is unreachable, being right in value and wrong in hue reads as a painting;
  the reverse reads as a mistake.
- **Transform:** In `NearestCandidateArgb`, replace `dl*dl + da*da + db*db` with
  `(wL*dl)² + da² + db²`. The shell-walk pruning bound must be scaled to match — `reach`
  is compared against `bestDistance`, and with an anisotropic metric the bound has to use
  the *smallest* effective cell extent after weighting, i.e. `min(CellL*wL, CellA, CellB)`.
  Getting that wrong silently breaks the search.
- **Parameters:** one slider, `wL` ∈ [1.0, 3.0], default 1.5 to match `PaintBlendMatcher`.
- **Per-pixel safe:** yes.
- **Confidence it looks better, not merely different: high.** This is not really a style
  lever — it is aligning the converter with the metric the prior research already justified
  and the matcher already uses. The two components currently disagree.

### Lever 2 — Value curve: contrast and key

- **Artistic effect:** Range compression into the palette's real span, plus the painter's
  choice of high key (shimmering, atmospheric) or low key (moody), plus the S-curve that
  compensates for the Stevens effect.
- **Transform:** Before matching, remap L\* through a parameterised sigmoid onto
  `[L_min, L_max]` of the candidate set (which `CandidateSet` already computes as `MinL` and
  `MinL + CellsPerAxis*CellL`). Couple it to a small chroma gain, because compressing
  luminance without adjusting chroma is a known error (§2.3).
- **Parameters:** black point, white point, contrast (sigmoid slope, 0.5–2.0), key (midpoint
  L\*, 35–65). Presets: "high key", "low key", "flat/atmospheric".
- **Per-pixel safe:** yes.
- **Confidence: high** that the range-fitting part improves things — the prior research
  independently identified plugged shadows as a live defect. **Medium-high** for the key and
  contrast controls, which are genuinely stylistic.

### Lever 3 — Notan / value massing

- **Artistic effect:** Reduce to 2–4 value masses. The classic design study, and a strong
  distinct look.
- **Transform:** Two-pass. Pass 1 computes an L\* histogram over the (already blurred) image
  and picks N−1 thresholds — Otsu multi-level or 1-D k-means on L\* — then snaps each pixel's
  L\* to its mass's representative value before matching. Hue and chroma pass through
  untouched, so the result is still a real mixture at a quantised value.
- **Parameters:** number of values (2–5); whether to snap chroma too (off = "coloured
  notan", on = "poster").
- **Per-pixel safe:** the *second* pass is; the threshold-finding pass is global. That is
  fine — it is one histogram pass over the image and the thresholds become constants that
  the per-colour cache can use.
- **Confidence: medium-high** that it looks deliberate and painterly at N = 4; **low** at
  N = 2, where it stops being a painting and becomes a stencil. Also: this is the lever most
  likely to expose banding on smooth gradients, and the existing blur slider will make that
  worse, not better.

### Lever 4 — Chroma-versus-lightness curve (the chroma curve)

- **Artistic effect:** Chroma falls off into shadow and (optionally) into the highlights,
  matching how painters model a form. §4.2.
- **Transform:** `C*' = C* · f(L*)`, applied in polar a\*b\*, where `f` is a curve peaking in
  the upper-middle values and falling toward both ends. A defensible default shape is
  `f(L) = g · (L/100)^p` with p ≈ 0.5, plus a highlight rolloff above L\* ≈ 85.
- **Parameters:** overall chroma gain g (0.6–1.4), shadow desaturation exponent p (0–1),
  highlight desaturation (0–1).
- **Per-pixel safe:** yes.
- **Confidence: medium-high.** The underlying observation is well-attested craft
  ([relayed]) and is consistent with the Hunt effect ([verified]). The risk is that photos
  *already* contain most of this fall-off, so a naive application double-counts and produces
  mud. Start the default at a mild setting, and consider making the control signed so users
  can go the other way.

### Lever 5 — Temperature split by value

- **Artistic effect:** The single most characteristic painterly move — warm the lights, cool
  the shadows (or the reverse for indoor/tungsten scenes). Covers §3.1 items 1 and 4.
- **Transform:** Add `t · (L* − L_pivot)/50 · (cos h_warm, sin h_warm)` to (a\*, b\*), with
  `h_warm` the warm-pole hue angle. Defaults: `h_warm` = 55° ([relayed] × 2 converging
  sources), `L_pivot` = the image's mean L\*.
- **Parameters:** warm-axis hue angle (0–360°, default 55), split magnitude t (0–8 CIELAB
  units), pivot. A single "warm light / cool light" toggle just flips the sign of t.
- **Per-pixel safe:** yes, provided `L_pivot` comes from a global first pass (or is fixed at
  50).
- **Confidence: medium-high.** The effect is unambiguous and cheap. The caveat from §3.2 is
  real, though: this models temperature as a hue-angle projection, which the JOV data says
  is a simplification. It will still look like what painters do.
- **Better version, if the spectral data is available:** instead of a hue-angle projection,
  rank candidates by MacEvoy's reflectance-cliff criterion computed from the 38-band
  spectrum each candidate already has ([verified] criterion, §3.2). That gives a
  physically-grounded warmth score rather than a proxy. Higher effort, and I have not seen
  it done, so **confidence: unknown but interesting.**

### Lever 6 — Hue-wedge restriction (gamut masking)

- **Artistic effect:** Gurney's gamut mask. Forces a colour scheme; produces strong unity
  and mood; makes neutrals read as coloured by contrast.
- **Transform:** In `BuildCandidates`, after computing L\*a\*b\*, **penalise rather than
  exclude**. Add to the distance a term `k · max(0, angularDistanceOutside(h, wedges))²`
  scaled by the candidate's chroma, so low-chroma candidates are always admissible (this is
  the "allow the neutral core" rule, and it matters — a hard hue cut makes every out-of-wedge
  colour snap to the wedge boundary and produces visible hue banding).
- **Parameters:** one or two wedge centres and half-widths; strictness k; neutral-core
  radius in C\*. Presets from Cohen-Or's template types (analogous / complementary /
  split-complementary / triad), noting §5.2 — the analogous presets have empirical support
  and the complementary ones do not.
- **Per-pixel safe:** yes (it is a modification of the candidate scoring, not of the pixel).
  But it changes the metric, so the same pruning-bound caveat as Lever 1 applies; the
  simplest sound implementation is to bake the penalty into a **per-candidate additive
  constant** computed at build time, which preserves the triangle-inequality reasoning the
  shell walk depends on.
- **Confidence: medium** that it looks better; **high** that it looks distinctive and
  intentional. Published prior art exists (EPFL/SPIE 2014, §5.3) which is a good sign, and
  the voxel-gamut representation there is close to `CandidateSet`'s grid.

### Lever 7 — Mother colour

- **Artistic effect:** Edgar Payne's unification device. Everything shares an undertone.
- **Transform:** At candidate-build time, mix every candidate with fraction m of a chosen
  paint **through the Kubelka–Munk kernel** — i.e. in `BuildCandidates`, renormalise every
  sampled share vector to `(1−m)` and append the mother paint at `m`. Because it goes
  through the real mixing model, every resulting colour remains a genuinely achievable
  mixture and the recipes stay honest.
- **Parameters:** which paint (dropdown from the user's selection); fraction m, 0–20%.
- **Per-pixel safe:** yes — it is purely a gamut transform.
- **Confidence: medium-high, and it is the lowest-risk lever here.** It contracts the gamut
  smoothly rather than cutting it, so there is no banding failure mode. It also degrades
  gracefully: at m = 0 it is the current behaviour. The honest caveat is that it costs
  chroma everywhere, and the prior research's chroma-retention numbers say a third pigment
  in every mix is expensive (35% retention at three pigments, [relayed]). Keep m small.

### Lever 8 — Global colour-statistics transfer to a reference painting

- **Artistic effect:** "Make it look like this painting's palette."
- **Transform:** Reinhard et al., *Color Transfer between Images* (2001) — match the mean and
  standard deviation of each of L\*, a\*, b\* to a reference image's, then match against the
  palette as usual ([verified] — [paper PDF](https://www.cs.tau.ac.il/~turkel/imagepapers/ColorTransfer.pdf)).
  Reinhard works in decorrelated lαβ; CIELAB is a serviceable substitute and is what this
  pipeline already has.
- **Parameters:** reference image or built-in preset; strength 0–1 (interpolate between
  original and transferred statistics).
- **Per-pixel safe:** yes, after a global statistics pass.
- **Confidence: low-medium.** Two reasons for the discount. First, Reinhard transfer is
  notoriously content-sensitive — transferring a sunset's statistics onto a portrait
  produces garbage. Second and more interesting: **the Scientific Reports work says the
  measurable painting-versus-photograph signature is in the *tail* of the colour-usage
  distribution and in the *spatial* roughness of brightness, not in the first two moments**
  ([verified] finding, [inferred] implication). Reinhard matches exactly the moments that
  are *not* the distinguishing ones. Include it if you want the feature; do not expect it to
  be the one that makes images look painted.

### Lever 9 — Focal-point chroma and contrast falloff

- **Artistic effect:** Highest chroma, strongest value contrast and hardest edges gather at
  the centre of interest; the periphery subordinates. §4.4.
- **Transform:** A radial (or elliptical, or user-mask) weight `w(x,y)` ∈ [0,1] driving both
  the chroma gain of Lever 4 and the contrast of Lever 2.
- **Parameters:** focal point position (click on the image), falloff radius, chroma
  suppression at the edge, contrast suppression at the edge.
- **Per-pixel safe: NO.** This is the important one to flag. `MapPixelsFlat` caches by
  colour alone; a position-dependent transform breaks that. The fix is cheap though —
  quantise `w` into K bands (K ≈ 8) and keep K caches, or equivalently extend the cache key
  by 3 bits. Memory goes from 2¹⁸ to 2²¹ ints (8 MB), which is acceptable ([inferred]).
- **Confidence: medium-high that it looks better.** Of all the levers this is the one whose
  underlying principle has the most unanimous craft support, and unlike most of the others
  it adds *information* (where the subject is) that the photo does not carry.

### Lever 10 — Aerial perspective

- **Artistic effect:** Distance reads as reduced contrast, reduced chroma and a hue drift
  toward the sky colour. §4.3.
- **Transform:** Blend the target toward an atmospheric colour with a depth-proportional
  weight, then match.
- **Parameters:** atmospheric colour; strength; and — the blocker — **a depth map.**
- **Per-pixel safe: NO**, and worse than Lever 9: the app has no depth information at all.
  The only honest options are a user-painted gradient/mask, or a vertical-position proxy
  (which is wrong for anything but a horizon-dominated landscape).
- **Confidence: low as an automatic feature; medium if the user paints the mask.** I would
  not build this before Lever 9, since Lever 9 gives you most of the same machinery with a
  much cheaper user input.

### Lever 11 — Local contrast enhancement before matching

- **Artistic effect:** Compensates for the palette's compressed range by spending contrast
  where boundaries matter. It is the computational stand-in for what a painter does at the
  terminator. §7.1.
- **Transform:** Unsharp mask on L\* only (leave a\*, b\* alone), at a radius comparable to
  the blur radius, applied after the blur and before matching.
- **Parameters:** amount, radius. Naturally couples to the existing blur slider — "soften
  detail, sharpen structure" is one control with two ends.
- **Per-pixel safe: NO** (it is a neighbourhood operation), **but it runs as a pre-pass on
  the pixel buffer exactly like `GaussianBlur.Apply` already does**, so it is architecturally
  free — the cache is built after it.
- **Confidence: medium-high.** This is the cheapest partial answer to the simultaneous-contrast
  problem in §7, and it directly attacks the "everything got flatter" failure that value
  compression causes. Halo artefacts are the risk; keep the amount modest.

### Lever 12 — Edge-preserving pre-filter instead of Gaussian blur

- **Artistic effect:** Value *massing* rather than value *smearing*. Flat regions with crisp
  boundaries, which is what a painting is.
- **Transform:** Replace or supplement `GaussianBlur` with a bilateral filter, or go further
  to SLIC superpixels with a minimum-region-size cleanup (the prior research already
  recommends the segmentation route for performance reasons, so this converges with existing
  Tier-2 item 16).
- **Parameters:** spatial radius and range sigma (bilateral), or region count and
  compactness (SLIC).
- **Per-pixel safe: NO**, but same architectural note as Lever 11 — it is a pre-pass.
- **Confidence: medium-high.** The current Gaussian blur is the weakest link in the
  "painterly" chain: it destroys exactly the edges that make a painting readable. Almost any
  edge-preserving substitute is an improvement, and the prior research wants the segmentation
  anyway.

### Lever 13 — Broken-colour / optical-mixing render mode

- **Artistic effect:** Impressionist/Divisionist rendering. Chroma survives where subtractive
  mixing would kill it (§8.1).
- **Transform:** A separate renderer, not a distance tweak. For each region: find a pair of
  *unmixed or lightly-mixed* candidates whose **area-weighted linear-light average** matches
  the target, then lay them out in a regular stroke or dot pattern at a user-specified mark
  size. The averaging must happen in linear RGB or XYZ, not in CIELAB and not through the
  Kubelka–Munk kernel — this is the one place in the app where mixing is *not* subtractive.
- **Parameters:** mark size (mm), viewing distance (which together determine whether it
  fuses — see the table in §8.2), pattern regularity, how many paints per patch.
- **Per-pixel safe: NO**, fundamentally — the whole point is the spatial pattern.
- **Confidence: low that it looks *better*; high that it is the most genuinely different
  output the app could produce.** Three honest warnings: (1) it does *not* brighten anything,
  so do not advertise luminosity ([verified], §8.1); (2) the recipe output becomes "these two
  paints, side by side, at this area ratio" rather than a mixture, which is a different
  contract with the user; (3) the prior research's objection stands — **wet acrylic touching
  wet acrylic mixes subtractively**, so the feature only works if the user is told to let
  each layer dry, and the UI must say so.

### Not recommended

- **A full relational/spatial objective** (global optimisation with neighbour terms). Turns
  an interactive lookup into a slow solve, with no evidence of a better result. §7.2.
- **CIECAM16 appearance modelling** for the style transforms. The prior research already
  rejected it for the physical pipeline because the adapting luminance, surround and
  background are unknowable for a hobbyist's room; the same argument applies here, and the
  style effects are far larger than the appearance-model corrections would be.
- **Anything keyed to CCT.** §3.2.
- **Presenting complementary harmony as the "harmonious" default.** The evidence says the
  opposite. §5.2. Offer it as a *scheme*, label it as a strong stylistic choice, and make an
  analogous preset the default.
- **A "60/30/10" control.** §5.4.

---

## 12. Verification status and gaps

**Verified directly in this session:**

- Schloss & Palmer (2011) results, including the complementary-pairs-are-*less*-harmonious
  finding and all variance-explained figures, read from the open-access full text at
  [PMC3037488](https://pmc.ncbi.nlm.nih.gov/articles/PMC3037488/).
- The 2025 *Journal of Vision* warm–cool study's peaks, boundaries and the
  saturation-minima finding, from [PMC12025320](https://pmc.ncbi.nlm.nih.gov/articles/PMC12025320/).
- Kim/Son/Jeong *Scientific Reports* 4:7370 quantitative findings (universal rank-ordered
  colour distribution, box-counting dimensions, roughness exponent, hyperrealist-paintings-match-photographs).
- MacEvoy's warm/cool reflectance criterion and his split-primary critique, from
  [handprint](https://www.handprint.com/HP/WCL/color12.html) and
  [handprint palette4r](https://www.handprint.com/HP/WCL/palette4r.html).
- Gurney's warm/cool rules and the gamut-masking method, from Gurney Journey directly.
- S-CIELAB's band-pass-luminance / low-pass-chroma structure, from
  [Johnson & Fairchild](https://www.cis.rit.edu/people/faculty/johnson/pub/ciede_scielab.pdf).
- The EPFL/SPIE gamut-masking tool's voxel-gamut approach.
- The current code's unweighted CIELAB distance in `PalettePhotoConverter`, versus
  `LightnessWeight = 1.5` in `PaintBlendMatcher` — read from source.
- Arithmetic: 23.6:1 palette reflectance ratio; Rayleigh (700/400)⁴ = 9.38; the fusion-distance
  table.

**Could not fetch — cite with caution:**

- **huevaluechroma.com (David Briggs, *The Dimensions of Colour*)** returned no content on
  every attempt from this environment, including via the Wayback Machine, which is blocked.
  Everything attributed to Briggs here is from search-index excerpts. **This is the single
  biggest gap** — his site is the most rigorous painter-facing modern colour source I know
  of, and sections 077 (warm and cool hues), 101 (shading and shadow series) and 116 (colour
  constancy illusions) are all directly on-topic. Worth revisiting from a browser.
- **Cohen-Or et al., *Color Harmonization* (SIGGRAPH 2006)** — PDF downloaded but not
  renderable as text here (no `pdftoppm` in this environment). The eight-template structure
  is confirmed from secondary sources; **the sector widths in degrees are not confirmed**.
- **Ou & Luo (2006)** and the Oh (2022) / Chang (2026) warm–cool CCT models are paywalled at
  Wiley. The h ≈ 50° warmest-hue figure is second-hand.
- **Luo et al. (1995), *Quantifying colour appearance Part V: simultaneous contrast*** —
  paywalled.
- **Richard Schmid, *Alla Prima*** and **Harold Speed** are not online; everything here is
  from third-party summaries and should be treated as weak. If value-structure claims become
  load-bearing for a design decision, get the books.
- The "Seurat Delusion" essay returned 404; the Seurat-misread-Rood claim rests on
  secondary art-historical sources only.

**Genuine gaps with no published answer found:**

- No study measuring hue-area *proportions* in paintings against aesthetic judgement — the
  dominance-ratio question is unanswered, not merely unverified.
- No psychophysics on chromatic versus neutral greys.
- No agreed magnitude for chromatic induction that could be hard-coded; individual
  variability is large enough to have its own literature.
- No quantitative evaluation of whether value-posterisation (notan) rendering is preferred
  over continuous rendering.
