# Grounds and Background in Abstract Painting

**Date:** 2026-07-28
**Track:** 3 of 4, abstract art.
**Question:** what is the background in an abstract painting, and what colour is it?
**Relationship to prior research:** [01-colour-theory-in-practice.md](../01-colour-theory-in-practice.md)
covers harmony, mother colour and limited palettes; [03-brushwork-and-edges.md §1.6](../03-brushwork-and-edges.md)
covers imprimatura and the "toned ground with a coverage mask" lever. This report does not
restate either. It answers the harder question they left open — whether an abstract picture
has a background at all, what colour it is when it does, and how a per-pixel converter that
structurally cannot produce one should be changed.

**Claim marking** (enforced across `docs/research/`):

- `[verified]` — I read a primary or reputable source directly in this session, or it is
  arithmetic I performed on data in this repository.
- `[relayed]` — a secondary source or a search summary asserts it and I could not confirm it
  against the primary.
- `[inferred]` — my own reasoning from stated premises.

---

## The answer, first

**"Background" is the wrong word and it hides three different things.** Separating them is
the whole result:

| | What it is | Positional? | Can this app express it? |
|---|---|---|---|
| **The ground** | A physical layer under the entire picture — toned canvas, imprimatura, a stain. Present everywhere, *including under the figures*, and visible only in the gaps. | No. It is a layer, not a region. | Partly. `MotherColourTransform` already implements its optical half. |
| **The field** | A large area of paint that reads as continuous, against which smaller incidents sit — Newman's red, Rothko's outer rectangle, Kandinsky's cream. | Yes. It is a region. | No. Nothing in the pipeline produces one. |
| **Negative space** | Area deliberately left unworked or under-worked. | Yes. | Not as absence — but it does not need to be. See §4. |

**A converter that maps every photo pixel to a mixture already produces the ground's optical
effect and never produces a field.** That is the gap, and the field is what a viewer reads as
"the background".

Four conclusions, in order of confidence:

1. **The ground is not a region, so "detect the background" is the wrong framing for it.** A
   toned ground covers 100% of the canvas and unifies by *multiplying every colour above it
   by a common reflectance*. That is what `MixtureBuilder.BlendInto` already does in mixture
   space. The existing mother colour is a ground missing only its area. `[inferred]`,
   mechanism grounded in §2.
2. **The field is a region, is what actually reads as background, and belongs in slot 5.**
   `IPostMapStage.Refine` takes and returns candidate *indices*, so a flat fill cannot leave
   the achievable gamut — the brief's reasoning is correct, and it is stronger than stated:
   the invariant is enforced by the signature, not by a rule. `[verified]` by reading
   `Imaging/Styles/PipelineStages.cs:136–152`.
3. **The colour cache costs nothing.** The brief flags a ground as inherently positional and
   therefore cache-breaking. That is wrong for the two slots a ground should use. The 6-bit
   cache lives in `StylePipeline.ResolveOncePerColour`, which has already finished by the
   time slot 5 runs, and slot 1 is explicitly the positional slot and runs before the cache
   is built. Only implementing a ground in slot 4 — by setting `IQuantiser.IsPositionDependent`
   — costs anything, and it costs roughly **80×** (§5.2). `[verified]` by reading
   `Imaging/StylePipeline.cs:126–271`.
4. **Every classical ground is comfortably mixable; some famous abstract grounds are not.**
   Newman's cadmium-red field is a single tube in this library (C\*ab 89.2). Klein's IKB is
   short by about 5.6 C\* units against the best achievable blue and is unreachable. §2.4.

**The single recommendation:** build a `GroundFill` post-map stage that writes one candidate
index into a binary mask of large, low-detail, mapped-index regions, with the ground colour
derived from the image's own median lightness and chroma-weighted mean hue, desaturated. It
is under 150 lines, breaks no invariant, costs no cache, and is the only thing on this list
that gives an abstract conversion a background a viewer can see.

---

## Contents

1. [Is "background" even the right concept?](#1-is-background-even-the-right-concept)
2. [Grounds as physical practice](#2-grounds-as-physical-practice)
3. [Ground colour in named abstract movements](#3-ground-colour-in-named-abstract-movements)
4. [Negative space as an active element](#4-negative-space-as-an-active-element)
5. [How to give a converted photograph a ground](#5-how-to-give-a-converted-photograph-a-ground)
6. [What not to build](#6-what-not-to-build)
7. [Verification debt](#7-verification-debt)

---

## 1. Is "background" even the right concept?

### 1.1 Modernism's own answer: sometimes, and it was contested at the time

Greenberg's *Modernist Painting* (1960/61) is the canonical statement that painting's
essential and unique property is flatness — "flatness, two-dimensionality, was the only
condition painting shared with no other art" — and that what modernism abandons in principle
is not recognisable objects but "the representation of the kind of space that recognizable
objects can inhabit". `[relayed]` — the primary text would not fetch from this environment
(see §7); this is from [Britannica](https://www.britannica.com/topic/Modernist-Painting) and
[TheArtStory](https://www.theartstory.org/definition/medium-specificity-and-flatness/).

**Greenberg himself conceded the limit, and the concession is the useful part.** He wrote
that "the first mark made on a canvas destroys its literal and utter flatness, and the result
of the marks made on it by an artist like Mondrian is still a kind of illusion that suggests
a kind of third dimension" — modernist flatness "can never be an absolute flatness".
`[relayed]`, from search-index text of the essay; the two hosted copies both failed (§7).

So even on the strictest formalist account, figure and ground do not vanish; they are
compressed to an optical residue. That matters here because it licenses a *weak* ground —
one that differs from the figures by lightness and chroma rather than by depth cues.

**All-over composition is the case where the concept genuinely dissolves.** Greenberg
introduced the term in *The Crisis of the Easel Picture* (1948) for the "'decentralized,'
'polyphonic' picture that relies on a surface knit together of identical or closely similar
elements", one that "dispenses with beginning, middle, end" and is woven into "a tight mesh
whose scheme of unity is recapitulated at every meshing point". `[relayed]` — from a
transcription blog, [clementgreenberg.tumblr.com](https://clementgreenberg.tumblr.com/post/329882573/the-crisis-of-the-easel-picture-1948);
I did not see the printed text. He also noted this brings the easel picture close to
wallpaper, a "fatal ambiguity". `[relayed]`

For Pollock's drip paintings, "background" is a category error: there is no region the marks
sit on that is not itself marked. `[relayed]`, standard art-historical reading, e.g.
[Artsy on all-over composition](https://www.artsy.net/gene/allover-composition).

**Mondrian is the second category error, for a different reason.** Neo-plasticism abolishes
the figure-ground dichotomy by using a grid that resists hierarchy; the white, grey and black
planes are *planes*, of the same status as the coloured ones, and the viewer cannot decide
which set is behind. `[relayed]` — [TheArtStory, Neo-Plasticism](https://www.theartstory.org/movement/neo-plasticism/).
Calling Mondrian's white a background inverts his stated programme.

**The strongest counter-argument to Greenberg is Steinberg's.** Leo Steinberg's "flatbed
picture plane" (lecture at MoMA 1968; *Other Criteria*, 1972) reframes the modern picture
surface as a horizontal *receptor* — a surface onto which things are deposited — rather than
a vertical window analogous to a field of vision. It supplements Greenberg's purely optical
flatness with tactility and the hand. `[relayed]` — the MIT-hosted PDF would not parse (§7);
this is from search summaries of [Steinberg's Wikipedia entry](https://en.wikipedia.org/wiki/Leo_Steinberg)
and secondary commentary.

Steinberg's framing is the more useful one for this app `[inferred]`: a receptor surface has
a *state before anything is deposited on it*, and that state is the ground. The window model
has no such thing.

### 1.2 Perception's answer: figure-ground assignment is weak, and colour drives it

Rubin's classical cues — surroundedness, size, orientation, contrast, symmetry, convexity,
parallelism — say the figure tends to be the smaller, enclosed, convex, symmetric, lower
region and the ground the larger surrounding one. `[relayed]` —
[Scholarpedia, figure-ground perception](http://www.scholarpedia.org/article/Figure-ground_perception),
[Vecera et al., *Lower Region*](https://awhvogellab.com/files/pdfs/vecera_2002_lower-region-new-cue.pdf).

Fowlkes, Martin & Malik (*Journal of Vision* 7(8):2, 2007) measured how well those cues
actually work on natural images with human figure-ground labels. Reported single-cue
classification accuracies: **convexity 60.1%, size 64.4%, lower region 67.8%.** `[relayed]` —
figures from a search summary; both the JOV page and the two PDF mirrors failed (§7).

**That is the most important number in this report for what *not* to build.** Against a 50%
baseline, the best single geometric cue is ~18 points above chance. Automatic figure-ground
extraction from a photograph is not a solved problem you can lean on, which corroborates
report 04's independent finding that image-independent centre bias beats image salience at
predicting fixations on paintings.

**Colour, however, is a strong cue, and the direction is actionable.** A study of Vasarely-
inspired configurations found that when saturation and luminance contrast are controlled,
**warm colours are far more likely to be assigned figure status than cool ones** — red on a
dark surround gave ~0.9 probability of being judged "nearer" against ~0.6 for blue; colour
effect F(1,79) = 33.92, p < .001 for "nearer" and F(1,79) = 29.37, p < .001 for "bigger";
background luminosity F(1,79) = 4.35, p < .05 and F(1,79) = 13.05, p < .001 respectively.
Perceived "nearerness" and "bigness" correlated at Pearson r = 0.98–0.99, p < .001.
`[verified]` — [Frontiers/PMC7365985](https://pmc.ncbi.nlm.nih.gov/articles/PMC7365985/).

The decisive detail: **red on dark green (Weber contrast 16.9) produced higher figure
probabilities than blue on dark blue (Weber contrast 64.38)** — hue beat nearly four times
the luminance contrast. `[verified]`, same source.

**Operational consequence** `[inferred]`: if the app inserts a field, the field should be the
*cooler and less chromatic* party and the mapped figures the warmer, more chromatic one, or
the picture will read inside-out. Note this does not forbid a warm *earth* ground — burnt
umber and raw umber are warm in hue but sit at C\*ab 4.7–22.4 masstone (§2.4), far below the
chroma at which the warm-advances effect was measured.

### 1.3 Where a ground genuinely exists, and where the term is a category error

| Has a real ground or field | Does not |
|---|---|
| Rothko — a stained chromatic ground under the whole canvas, documented in conservation literature (§3.2) | Pollock's all-over drip works — no unmarked region |
| Kandinsky's Bauhaus compositions — a cream/pale-blue field with shapes on it (§3.4) | Mondrian's neo-plastic grids — every plane is a plane (§3.1) |
| Colour Field and hard-edge on raw canvas — the fabric itself is the ground (§3.5) | Dense gestural abstract expressionism generally |
| Miró, Motherwell, Twombly, and most contemporary studio abstraction `[relayed]`, craft consensus | Anything the artist describes as "all-over" |

**The implication for a style preset is that a ground must be a control, not a constant.**
Some abstract painting has one and some deliberately does not, and the app cannot infer which
the user wants from a photograph.

---

## 2. Grounds as physical practice

Report 03 §1.6 already covers what an imprimatura is, that it supplies the mid-tone, and that
painters take care not to cover it completely. This section adds what it does not have: the
specific colours, the mechanism stated precisely enough to connect it to the code, and
whether any of it is mixable from this library.

### 2.1 The colours painters actually use, and their one shared property

Two independent practitioner sources, one a manufacturer and one a teaching studio:

**Gamblin's four Toned Ground colours** `[verified]` — [gamblincolors.com/toned-ground](https://gamblincolors.com/toned-ground/):

| Colour | Description | Stated use |
|---|---|---|
| Warm Birch | earthy yellow | warmth, summer light |
| Neutral Grey | "an excellent unbiased choice" | winter scenes, contrasting warm light |
| Warm Grey | violet-leaning grey | "versatile" without over-influencing temperature |
| Raw Linen | brown-grey | imitates traditional oil painting surfaces |

**Will Kemp's five** `[verified]` — [willkempartschool.com](https://willkempartschool.com/how-to-choose-a-colour-for-a-tonal-ground-my-top-5-choices/):
thinned yellow ochre (light, warm); thinned burnt umber (dark, for a glow); raw umber + white
(cooler mid-tone, for portraits); Golden Neutral Grey N6; ultramarine + white (muted blue
mid-tone, plein-air seascapes). His stated principle is that grounds should stay close to
**mid-tonal value** — neither too dark nor too light.

The traditional imprimatura is described as "almost always moderate in intensity and value",
relying on earth pigments for cost and fast drying. `[relayed]` —
[Wikipedia, Imprimatura](https://en.wikipedia.org/wiki/Imprimatura).

**The shared property across all nine recommendations is mid value, low chroma, and a slight
warm or violet bias.** `[inferred]` from the lists above. Munsell value 6 — Golden's N6 — is
L\* ≈ 61.7; value 5 is L\* ≈ 51.6. `[verified]`, arithmetic from the Munsell value-to-Y table
(V5 → Y 19.77%, V6 → Y 30.05%) through the CIELAB lightness function.

This gives a defensible numeric target where the art-historical literature gives none:
**a ground sits near L\* 50–65 at C\*ab below about 25.**

### 2.2 The two mechanisms of optical unification, and which one the codebase already has

Painting instruction says a visible ground "unifies". Two physically distinct things are
being conflated, and only one of them is in the code.

**Mechanism (i) — the layer, acting multiplicatively.** Anything applied over a ground at less
than full opacity is a glaze. Its reflectance is the layer's own transmittance applied over
the ground's reflectance, so *every* colour in the picture acquires a common spectral factor.
That is a contraction of the whole gamut toward the ground's colour. `[inferred]`, standard
optics; report 03 §3.9 states the glaze equation.

**Mechanism (ii) — the area, acting relationally.** The ground shows through in gaps, at
edges, and in shadow passages, so the same colour recurs across the whole picture. Every local
colour judgement the viewer makes then includes it, and simultaneous contrast references
everything to a common anchor. Gamblin's phrasing: "a toned surface allows your initial marks
to appear unified, not fragmented", and "leftover traces of Toned Ground pull a painting
together and amplify color harmony from edge-to-edge". `[verified]`, same source. Wikipedia's
imprimatura entry says the same and specifies that it shows through "particularly in the
middle to dark shadow areas". `[relayed]`

**`MotherColourTransform` is mechanism (i) and nothing else.** `MixtureBuilder.BlendInto`
folds a fixed fraction of one paint into every sampled mixture and renormalises the shares, so
the Kubelka-Munk kernel sees a real mixture that includes the ground paint. Every candidate
colour is pulled toward one point; the gamut contracts smoothly with no banding failure mode.
`[verified]` by reading `Imaging/Styles/MixtureBuilder.cs:86–100, 381–423`.

That is the correct implementation of the *layer*. It has no area, no gaps, and no positional
presence, so it delivers none of mechanism (ii). **Mother colour is a ground with its
visibility removed.** `[inferred]`

**Two further gaps worth naming, both in `MotherColourTransform`:**

- It blends toward `MixtureBuilder.MostNeutralPaintIndex()` — the palette's *least chromatic*
  paint, tie-broken toward L\* 50. `[verified]`, `MixtureBuilder.cs:135–162`. The doc comment
  justifies this well for a harmoniser that must not tint. But **real grounds are chromatic** —
  seven of the nine recommendations in §2.1 are warm or violet, not neutral. The existing
  stage cannot express a warm earth ground at all.
- Edgar Payne's mother colour, as report 01 §5.3 relays it, is a *chosen* colour, not
  automatically the greyest one. The stage's choice is a defensible engineering simplification,
  not the technique as described.

### 2.3 How much of the canvas is left as ground

**I found no measurement for Western oil or acrylic painting.** The craft literature says
"do not cover it completely" and stops. `[verified]` that the sources give no figure —
Wikipedia, Gamblin and Kemp all state the principle qualitatively.

The one quantitative study of unworked area in any painting tradition is Chinese landscape:
**56.8% of ancient Chinese landscape paintings are mostly empty space, against 9.4% of modern
ones**, peaking in the Yuan dynasty (1271–1368) and reaching a minimum in the 1960s.
`[relayed]` — [Leonardo 55(1):43–47, 2022](https://direct.mit.edu/leon/article/55/1/43/102698/A-Computational-Study-of-Empty-Space-Ratios-in);
the article is paywalled and returned 403, so the *method* (how "empty" was thresholded) is
unverified and the numbers should not be treated as a coverage-fraction prior.

Report 03's existing recommendation of a 0–30% coverage fraction is therefore a guess, and an
honest one. Nothing found here contradicts or supports it. `[inferred]`

### 2.4 Can these grounds actually be mixed? — checked against this library

This is the section that can be checked rather than cited, and the result is asymmetric.

**The classic earth grounds are not selectable as tubes.** `Pigments/pigments.manifest.txt`
holds 80 paints, of which **19 are `TwoConstantMeasured`** and reach `PigmentLibrary.Selectable`;
**61 are `ReflectanceDerived`** and are withheld from the user. `[verified]`, counted from the
manifest. Every earth colour is in the withheld set:

| Paint (withheld) | L\* | a\* | b\* | C\*ab |
|---|---|---|---|---|
| Burnt Sienna | 34.23 | 17.71 | 13.73 | 22.4 |
| Burnt Umber | 27.52 | 3.14 | 3.50 | 4.7 |
| Raw Umber | 29.44 | 1.54 | 4.44 | 4.7 |
| Raw Sienna | 51.86 | 14.00 | 32.38 | 35.3 |
| Yellow Ochre | 58.32 | 14.88 | 40.79 | 43.4 |

`[verified]` — masstone values read from the manifest; C\*ab is my arithmetic.

**This is good news, not bad.** All five are mid-to-dark and low-to-moderate chroma. Tinted
with white toward L\* 55–70 — which is what a ground is — they land at C\* roughly 15–30, and
that region of CIELAB is dense with achievable mixtures for any palette containing white, a
red or orange, and a black. A user with Titanium White, C.P. Cadmium Orange and Bone Black can
mix a warm mid-grey ground directly. `[inferred]`, from the gamut structure `BuildCandidates`
samples.

**A neutral mid-grey ground is trivial**: Titanium White (L\* 98.21) plus Bone Black
(L\* 11.42) spans the neutral axis. `[verified]` from the manifest.

**A raw-linen ground is achievable.** The colour-naming convention for ecru, `#C2B280`,
converts to **L\* 72.8, a\* −1.7, b\* +27.6, C\*ab 27.7**. `[inferred]` — the sRGB→CIELAB
conversion is my own arithmetic and is sound; **the hex itself is `[relayed]` from colour-naming
sites and is not a measurement of artists' cotton duck** (§7). White plus a yellow plus a trace
of black reaches it easily.

**Now the failures.** Report 02's chroma ceilings — median masstone C\*ab 33.6, best blue 70.7,
best green 56.0 — decide which famous abstract grounds are reachable:

- **Barnett Newman's cadmium-red field is reachable as a single tube.** C.P. Cadmium Red Light
  masstone is L\* 49.59, a\* 66.30, b\* 59.61 → **C\*ab 89.2**. `[verified]` from the manifest,
  chroma is my arithmetic. A saturated red Colour Field ground is not a problem.
- **Yves Klein's IKB is not reachable.** The commonly cited sRGB `#002FA7` converts to
  **L\* 26.2, a\* 36.9, b\* −66.8, C\*ab 76.3** `[inferred]`, my arithmetic. The library's most
  chromatic blue is Cobalt Blue masstone at L\* 27.46, a\* 32.44, b\* −62.86 → **C\*ab 70.7**
  `[verified]` — which independently reproduces report 02's figure. **IKB is short by ~5.6
  C\* units at essentially the same lightness, and any *mixture* is less chromatic than a
  masstone, so the real shortfall is larger.** Caveat: `#002FA7` is itself a screen
  approximation of a paint that is famously not reproducible on a screen.

**The rule that falls out** `[inferred]`: *saturated grounds are achievable in the
yellow-orange-red arc and not in blue, green or violet.* This is the same asymmetry report 02
found for Fauvist chroma boosts, arriving from a different direction. A "Colour Field ground"
control must either restrict itself to the warm arc or clamp honestly, and clamping a blue
ground will hue-drift toward cyan rather than desaturate.

---

## 3. Ground colour in named abstract movements

### 3.1 Mondrian — a warm off-white ground under planes that are not backgrounds

The best technical source is a 2024 study of nine neoplastic paintings 1921–1935, three at the
Fondation Beyeler and one at Kunstmuseum Den Haag. `[verified]` —
[npj Heritage Science, s40494-023-01127-8](https://www.nature.com/articles/s40494-023-01127-8),
open access, reached through a redirect chain.

- *Tableau I* (1921–1925): **"a very thin chalk ground"**. `[verified]`
- *Lozenge composition with yellow lines* (1933): **"a lead white ground containing small
  amounts of yellow ochre and black pigment"**. `[verified]`
- *Composition with yellow and blue* (1932): **five white layers** on the white planes.
  `[verified]`
- Zinc white with aluminium phosphate inclusions acts as a chronological marker paint, added
  to yellow, blue and black paint in specific layers; the planes became "lighter and brighter
  over the years". `[verified]`
- **The paper contains no colorimetry and no L\*a\*b\* values.** `[verified]` — I asked
  explicitly.

Two readings matter here. First, **Mondrian's actual ground is a slightly warm, slightly
greyed off-white**, not a pure white — lead white knocked back with yellow ochre and black is
exactly the "warm neutral" of §2.1. Second, **the white you see is not that ground**; it is up
to five layers of deliberately applied white paint, i.e. a *plane*, of the same status as the
red and blue ones. `[inferred]` from the layer counts. Treating Mondrian as "coloured shapes
on a white background" describes the reproduction, not the painting.

### 3.2 Rothko — the best-documented ground in abstract painting, and it is chromatic

Tate's conservation study of *Black on Maroon* 1958 reconstructs the layer structure
`[verified]` — [Tate Papers 23](https://www.tate.org.uk/research/tate-papers/23/conserving-mark-rothkos-black-on-maroon-1958-the-construction-of-a-representative-sample-and-the-removal-of-graffiti-ink):

- Support: heavyweight cotton duck "awning-type" canvas. **No priming layer.**
- Ground: a size of **rabbit-skin glue, synthetic ultramarine and lithol red**, heated thin —
  Rothko's assistant Dan Rice described it as "like water this stuff" — and brushed on with
  large decorators' brushes, saturating the canvas as a **translucent stain**.
- Everything above is thin washes and glazes over that stain.

**This is a chromatic ground covering 100% of the canvas, applied first, with the whole picture
built on top of it.** It is the two-layer decomposition of §5.4, executed by hand, and it is the
strongest single piece of evidence in this report that a ground-plus-figure model is faithful
to practice rather than a computational convenience. `[inferred]` from the verified structure.

**A caution that kills an obvious shortcut.** The lithol red has degraded photochemically —
the maroon is now "much more intense" than the original, driven by interactions between lithol
red and the other materials, and the Ba-lithol-red-plus-ultramarine combination specifically
exacerbates the fading. `[verified]`, same source and
[Tate Papers 10](https://www.tate.org.uk/research/tate-papers/10/history-and-manufacture-of-lithol-red-pigment-used-by-mark-rothko-in-seagram-and-harvard-murals-1950s-and-1960s).
**Any Lab value sampled from a photograph of a Rothko is wrong by an unknown and
non-uniform amount.** Do not seed a preset from museum images.

### 3.3 Malevich — the figure-ground difference is a few ΔE, and this app cannot render it

*Suprematist Composition: White on White* (1918, MoMA): a white square, off-centre and tilted,
**"barely differentiated from a slightly warmer white ground"**. `[relayed]` — MoMA's own
curatorial text via search summary; [the MoMA page returned 403](https://www.moma.org/collection/works/80385).
[Wikipedia](https://en.wikipedia.org/wiki/White_on_White) agrees the ground is the warmer of
the two. `[relayed]`

**Implication for this app, and it is a hard limit** `[inferred]`: the entire content of the
painting is a hue and chroma difference of a few units at very high lightness. The 6-bit colour
cache bins at 4 code values per channel, and the README records a **median candidate
nearest-neighbour spacing of 1.70 ΔE** for a 6-paint palette. A deliberate 2–3 ΔE figure/ground
separation would survive neither the quantiser nor a hand-mixed execution. A "Malevich mode" is
not buildable and should not be attempted.

### 3.4 Kandinsky — the closest thing to a conventional background

*Composition VIII* (1923, Guggenheim): geometric shapes, lines and colours set against **"a
background of cream that melds at certain points into areas of pale blue"**. `[relayed]` —
secondary art sites only, e.g. [thehistoryofart.org](https://www.thehistoryofart.org/wassily-kandinsky/composition-viii/);
I found no Guggenheim technical or conservation source (§7).

A light, low-chroma, slightly modulated field with discrete incidents on it is the one movement
surveyed whose structure maps cleanly onto "figures on a background", and it is also the
structure a photo converted with a strong ground fill would most resemble. `[inferred]`

### 3.5 Colour Field and hard-edge — the ground is the fabric, and figure-ground inverts

Frankenthaler poured thinned paint onto raw unprimed canvas so that "the paint and support
become one entity"; Greenberg brought Louis and Noland to her studio in 1953–54 and both
adopted the technique. `[relayed]` — [TheArtStory, Color Field](https://www.theartstory.org/movement/color-field-painting/),
[MyArtBroker](https://www.myartbroker.com/artist-helen-frankenthaler/articles/helen-frankenthalers-innovative-soak-stain-technique).
Noland "tended to juxtapose color bands of equal width… leaving portions of unprimed canvas
blank in contrast to the color". `[relayed]` — [TheArtStory, Noland](https://www.theartstory.org/artist/noland-kenneth/).
Hard-edge painting is characterised as "broad areas of bright, unmodulated colour… stained into
unprimed canvas". `[relayed]` — [Britannica](https://www.britannica.com/art/hard-edge-painting).

Newman's *Vir Heroicus Sublimis* (1950–51, 242.2 × 541.7 cm) is "a vast red field, broken by
five thin vertical stripes", the zips varying in width, colour and edge firmness. `[relayed]` —
[MoMA](https://www.moma.org/collection/works/79250) via search summary and
[Wikipedia](https://en.wikipedia.org/wiki/Vir_Heroicus_Sublimis).

**Two structural facts here matter more than the colours.** First, in soak-stain work the
ground is the raw fabric — an unpainted, light, warm, low-chroma neutral, and the ratio of
stained to bare area is a compositional variable. Second, **in Newman the field is the most
chromatic thing in the picture and the incidents are quieter** — figure-ground has inverted
relative to §1.2's warm-advances rule. `[inferred]`

That inversion is worth exposing as a control rather than resolving: a *quiet ground with loud
figures* and a *loud ground with quiet incidents* are both real abstract structures and they
are one sign flip apart.

### 3.6 Is there a defensible statistical claim about ground lightness and chroma?

**No, and the honest answer is worth more than a fabricated one.**

What exists:

- Manovich's cultural-analytics comparison plots **128 Mondrians (1905–1917)** and **151
  Rothkos (1944–1957)** by mean brightness against mean saturation, and finds each artist
  occupies a compact "footprint"; Rothko begins in the same **high-brightness / low-saturation**
  region Mondrian arrives at by 1917, then moves outside it. `[relayed]` —
  [Cultural Analytics Lab](https://lab.culturalanalytics.info/2016/04/mondrian-vs-rothko.html),
  [Style Space PDF](https://manovich.net/content/04-projects/074-style-space/70_article_2011.pdf).
  This is a whole-image measure. It says nothing about grounds specifically.
- Kim, Son & Jeong's 8,798-painting study (already cited by report 01) measures colour-usage
  rank distributions, box-counting dimension in RGB and brightness roughness — again
  whole-image. `[relayed]` via report 01.
- **No study I found segments grounds from figures across abstract paintings and reports their
  colour.** `[verified]` that my searches did not surface one; not proof it does not exist.

And a bounding result on trying to derive a ground colour from an aesthetic objective: a study
of **150 abstract paintings rated by 50 observers** found the whole battery of statistical
properties gave **R² = 0.134**, with the largest single correlations only ρ = −0.277 (HSV
value), ρ = −0.224 (CIELAB b\*) and ρ = −0.217 (saturation), all |ρ| < 0.28. Complexity,
anisotropy, a Birkhoff-like measure and aspect ratio showed no meaningful correlation.
`[verified]` — [PMC3968763](https://pmc.ncbi.nlm.nih.gov/articles/PMC3968763/). Note the
fetched summary is internally inconsistent about the sign of the lightness effect (it reports
ρ = −0.206 for CIELAB lightness while also saying observers preferred "bright images"); treat
the direction as unresolved and only the magnitude as informative.

**Conclusion:** the defensible claim is about *practice*, not measurement — grounds converge on
L\* 50–65 and C\*ab < 25 with a slight warm bias (§2.1) — and it should be implemented as a
default, not as an inferred optimum.

---

## 4. Negative space as an active element

**The craft claim** is that unworked area is what lets worked area read, and that a painting
whose every square inch is stated has nowhere for the eye to rest. It is universal in
instruction and I found no psychophysics on it. `[relayed]`

**The one empirical result** is the Chinese landscape empty-space study of §2.3. `[relayed]`,
method unverified.

**The distinction that actually matters for this app is between *unworked* and *flat*, and it
is not the one the brief assumes.** `[inferred]`

- *Unworked* means no mark was made: bare canvas, or ground only.
- *Flat* means one mark covers a large area.

The app can express flat. It cannot express unworked — `StylePipeline.Render` writes
`candidates.Argb[indices[at]]` for **every** pixel, preserving only the source alpha, and there
is no reserved "no paint" index. `[verified]`, `Imaging/StylePipeline.cs:141–149`.

**But it does not need to, and this resolves the brief's fourth candidate direction cleanly.**
A toned ground *is paint*. In Rothko's procedure the ground goes on first, over the entire
canvas, and the "empty" passages of the finished picture are the passages where only the ground
layer is present `[verified]` from §3.2. **The correct representation of "leave this unpainted"
in a paint-by-mixture plan is "this is the ground colour", and that is not a fiction — it is
what the hand actually does.** Even Noland's bare canvas is a colour a human executes by
choosing not to cover a surface whose colour they selected when they bought it.

So: do not add a null index, do not add an alpha channel, do not add a "bare" token. Add a
ground colour and let it stand for absence.

---

## 5. How to give a converted photograph a ground

Six directions, each with its slot, cost, and invariant status. The recommendation is B + C.

### 5.1 The invariant reasoning, confirmed and sharpened

**The brief's claim — "a flat ground fill is selection-only and therefore safe" — is correct,
and the mechanism is stronger than the wording suggests.** `IPostMapStage.Refine` receives
`int[] indices` and a `CandidateSet` and returns nothing but rewritten indices. It is given no
way to *name* a colour. As the interface doc comment already puts it, post-mapping arithmetic
"is not forbidden by a rule anybody has to remember; it simply cannot be expressed through this
signature." `[verified]`, `Imaging/Styles/PipelineStages.cs:126–152`.

Two conditions on that safety `[inferred]`:

1. **The ground colour must be an index into `CandidateSet`, not an RGB value.** If the user
   picks a colour, snap it to the nearest candidate first — a selection, therefore safe — and
   show them the mixture they will actually get. A ground colour outside the palette's gamut
   must fail visibly, not silently become something else.
2. **The mask must be binary.** Any feathering, anti-aliasing or alpha blend of the ground
   against the mapped image is post-map arithmetic and breaks the invariant. Report 03 already
   says this; it is the single most likely accidental violation when someone tries to soften a
   hard ground edge.

**The mark invariant is satisfied trivially.** A flat field is the largest and easiest mark in
painting. A ground fill can only *increase* median region area, which is the quantity the
README's paintability probe measures. It is the one style lever that makes the output strictly
more paintable rather than less.

### 5.2 The colour cache — correcting the brief

The brief asks me to flag that a ground is inherently positional and to estimate the cache
cost. **The cost is zero in the two slots a ground should use, and the reasoning is worth
recording because it is a general fact about this architecture, not about grounds.**

`StylePipeline.Render` runs: pre-map stages (slot 1, positional, on the raw pixel buffer) →
colour resolution (slots 2 and 4, cached per 6-bit key in `ResolveOncePerColour`) → post-map
stages (slot 5, positional, on the index buffer). `[verified]`, `Imaging/StylePipeline.cs:121–139`.

- **Slot 1 is explicitly the positional slot** — its doc comment says so — and it runs *before*
  the cache is built. A ground painted into the pixel buffer here costs the cache nothing; it
  adds at most one new occupied key.
- **Slot 5 runs after the cache has been consumed and discarded.** It already receives width,
  height and the full buffer. Positional work here is free.
- **Slot 4 is the only expensive placement.** Setting `IQuantiser.IsPositionDependent = true`
  switches the whole render to `ResolvePerPixel`. The cached path resolves at most
  `ColorQuantization.CacheSize` = 2¹⁸ = **262,144** keys and in practice only the occupied
  subset — order 50k–200k for a photograph. The per-pixel path resolves W×H. For a 12 MP image
  against ~150,000 occupied keys that is **≈ 80× more nearest-neighbour searches**.
  `[verified]` for the constants and the two code paths; the 80× is `[inferred]` arithmetic on
  an assumed occupancy.

**Design rule: never implement a ground in slot 4.** There is no reason to — a ground is not a
choice about *which* candidate a colour becomes, it is a decision to override that choice
entirely, which is exactly what slot 5 is for.

### 5.3 The candidate directions

---

**A. Mother colour — already implemented. How far does it get us?**

*Slot 3 (`ICandidateTransform`). Already shipped in Tonalism at 0.30 and Abstract at 0.15.*

**What it delivers:** mechanism (i) in full (§2.2) — every achievable colour acquires a common
note, the gamut contracts smoothly toward one point, hue and chroma variance fall globally,
there is no banding failure mode, and it costs nothing at match time because it happens at
build time. Every output remains a genuine mixture because `BlendInto` renormalises shares
inside the K-M kernel rather than blending colours afterwards. `[verified]`

**Where it falls short, precisely:**

1. **No area.** It changes what every colour *is*; it does not put a colour anywhere. A viewer
   sees a more harmonious picture, not a picture with a background.
2. **No gaps.** Mechanism (ii) requires the ground to recur *between* things. Blending it into
   everything is the opposite operation — it hides the ground inside the figures.
3. **Neutral only.** `MostNeutralPaintIndex()` forbids a warm earth ground (§2.2).
4. **It reduces contrast everywhere, including where the picture needs it.** A real ground
   unifies by recurrence, which costs nothing in the passages where it is covered.

**Recommended extension, cheap:** a sibling stage that blends toward a *chosen* paint — either
a palette index or the palette member nearest a user-set hue angle — reusing `BlendInto`
unchanged. ~40 lines, no new maths, no invariant risk. This gives a warm imprimatura in
mixture space and is worth having independently of a field.

**Verdict:** keep, extend, but stop calling it a ground. It is half of one.

---

**B. Flat ground fill over a low-detail mask — the recommendation.**

*Slot 5 (`IPostMapStage`). ~120–150 lines. Invariant: kept structurally. Cache: free.*

**What it does.** Identifies the regions a painter would state in one pass and states them in
one pass, in a single colour, producing an actual field.

**Mask construction — two options, and I recommend the second.**

*Option 1, saliency.* Achanta et al.'s frequency-tuned saliency (CVPR 2009) is
`s(x) = ‖I_mean − I_blur(x)‖²` in CIELAB, where `I_mean` is the image's mean Lab and `I_blur`
a small-kernel Gaussian. `[relayed]` — method from search summaries of
[the EPFL project page](https://www.epfl.ch/labs/ivrl/research/saliency/frequency-tuned-salient-region-detection/);
I did not fetch the paper. The app already has `RgbToLab`, `GaussianBlur` and `LinearPlanes`,
so this is roughly 40 lines. **Known failure mode, already flagged in report 03: it flags
anything colour-distinct from the global average, so a bright sky scores as salient.**

*Option 2, region structure — better, and it needs no new colour maths.* Run connected
components over the **already-mapped index buffer**, then mark as ground every region whose area
exceeds `k · markPixels²` and whose interior gradient is below a threshold. This is a direct
operational definition of "an area a painter would cover in one pass", it reuses the machinery
`PaintabilityMetrics` already needs for region statistics, and it cannot be fooled by a bright
uniform sky — it will correctly identify the sky as a field, which is what a painter does too.
`[inferred]`

Use option 2 as the primary and option 1 only to break ties or to *protect* a
user-clicked focal region from being swallowed.

**Why not automatic figure-ground segmentation.** §1.2: the best single geometric cue runs
67.8% on natural images. Report 04 independently found centre bias beats salience on paintings.
Region size and flatness are honest, local, and do not pretend to know what the subject is.

**Parameters:** coverage target or minimum region area (tie to `RenderContext` mark size);
ground colour (see C); a toggle for whether the ground may swallow regions touching the image
border only, which is the cheap version of Rubin's surroundedness cue.

**Invariant:** kept by the signature. Mask must be binary.

---

**C. Choosing the ground colour.**

*A global pre-pass, so it touches neither the per-pixel path nor the cache.*

Options and what the evidence says about each:

| Candidate rule | Verdict |
|---|---|
| **Complement of the dominant hue** | **Reject as default.** Schloss & Palmer (report 01 §5.2): harmony peaks at *identical* hue and falls monotonically with hue difference; complements rated reliably less harmonious, F(1,47) = 17.67, p < .001. `[verified]` via report 01. |
| **Image mean colour** | Weak. The mean of a photograph is a muddy mid-grey with no relation to how the picture is organised, and it is exactly the reference `I_mean` the saliency measure subtracts — using it as the ground makes every pixel maximally salient by construction. `[inferred]` |
| **Image mode / most frequent mapped candidate** | Reasonable and nearly free — it is one histogram over the index buffer. Risk: on a portrait it returns skin. |
| **Fixed per-style constant** | Reject. A fixed "Abstract uses warm grey" will fight palettes that cannot mix a warm grey and images with no warm content. `[inferred]` |
| **Derived: desaturated, mid-value, image's own dominant hue** | **Recommended.** |

**The recommended rule**, which is the mother-colour logic given an area and matches the craft
convergence of §2.1 `[inferred]`:

```
L*_ground = lerp(median L* of the image, 58, groundKey)      // toward Munsell ~V6
C*_ground = min(median C* of the image × 0.35, 25)
h_ground  = chroma-weighted circular mean hue of the image
groundIndex = nearest candidate to (L*_ground, C*_ground, h_ground)
```

Three sliders — key, chroma factor, hue offset — plus a "pick from candidates" override. Every
term is defensible: L\* 50–65 and C\* < 25 from §2.1; same-hue rather than complementary from
Schloss & Palmer; keeping the ground *less* chromatic than the figures from the Vasarely
figure-ground result in §1.2.

**One deliberate exception to expose:** a *hue offset* control that lets the user push toward
Newman's inversion (loud ground, quiet incidents) by raising `C*_ground` above the image
median. §3.5.

---

**D. Ground-plus-figure two-layer decomposition.**

*Not a pipeline change. An export/UI change.*

Physically this is exactly Rothko's procedure and every toned-ground painter's: cover the whole
canvas with one colour, then paint on top of it. `[verified]` via §3.2. It makes execution
*easier*, not harder — one large flat pass with a decorator's brush, then the figures.

In the app, the composite image the pipeline already produces is the finished painting. The
decomposition only needs to exist in the *plan*: mark one candidate index as "the ground —
cover the whole canvas with this first", and the remaining regions become marks laid over it.
No change to `StylePipeline`. The tooltip already resolves a mixture per colour, so the ground's
recipe is already available; what is missing is the instruction.

**One honest caveat, and it is in the app's favour** `[inferred]`: acrylic over a ground is not
perfectly opaque in one coat, so the executed figure colours will be pulled slightly toward the
ground. That is the glaze equation, and it is precisely mechanism (i) from §2.2 — the physical
unification the app is trying to simulate. The app models opaque masstone and therefore
*under-predicts* how unified the executed painting will look. Worth documenting rather than
correcting; correcting it means K-M layering, which report 03 calls "a different, larger,
physically honest invariant".

---

**E. Actually leaving areas unpainted.**

**Reject.** §4: the app cannot express "no paint here" — every opaque pixel gets a candidate
index — and adding a null index would break the structural guarantee that every index names a
mixture. More importantly, it is unnecessary: the ground *is* paint, and "ground only here" is
the faithful representation of an unworked passage.

---

**F. Candidate-set restriction relative to the ground.**

*Slot 3, via `MixtureBuilder.KeepOnly`, which exists and no registered style uses.*
`[verified]`, `MixtureBuilder.cs:116–119`, `StyleRegistry.cs` uses `KeepAllCandidates` everywhere
except Tonalism/Abstract's mother colour.

Not a ground mechanism, but it is where a rule like "no figure may be less chromatic than the
ground" or "no figure may be within ΔE 5 of the ground" would live — enforcing that the field
stays legible as a field. Note the built-in safety: if a predicate rejects everything, `Build`
discards it and returns the unfiltered set rather than crashing. `[verified]`

---

**G. Ground in slot 1 instead of slot 5.**

Write the ground colour into the *pixel buffer* pre-map. Also cache-free, and it removes the
need to snap the ground to a candidate yourself — the mapper does it. Downside: the mapped
result is the nearest candidate to what you asked for, not what you asked for, and you lose the
ability to reason about the ground as a known index in later stages. **Prefer slot 5**;
mention slot 1 only because it is the cheaper prototype.

### 5.4 Suggested order

1. **Extend `MotherColourTransform` to a chosen paint** (~40 lines, slot 3). Gives a warm
   imprimatura. Independent of everything else.
2. **`GroundFill` in slot 5 with the region-structure mask and rule C** (~120–150 lines).
   The actual feature.
3. **Ground-first instruction in the plan/export** (UI only). Makes the output executable in
   the order a painter would use.
4. *Only if 2 proves out:* a `KeepOnly` predicate enforcing figure/ground separation (slot 3).

---

## 6. What not to build

Each of these sounds compelling and does not survive the evidence above.

- **Complementary ground colour as the default.** Schloss & Palmer: complements rated
  reliably *less* harmonious than adjacent hues, F(1,47) = 17.67, p < .001; harmony peaks at
  identical hue. `[verified]` via report 01. Offer it as an option; never default to it.
- **Automatic figure-ground segmentation as a load-bearing stage.** Best single local cue is
  67.8% on natural images, convexity only 60.1%. `[relayed]` Region size and flatness are
  honest; "find the subject" is not.
- **A saturated blue, green or violet Colour Field ground.** IKB needs C\* 76.3; the library's
  best blue masstone is 70.7 and mixtures are lower still. §2.4. It will hue-drift, not
  desaturate.
- **A Malevich-style near-invisible figure/ground separation.** Median candidate spacing is
  1.70 ΔE and the cache bins at 4 code values per channel. §3.3.
- **Feathered or alpha-blended ground edges.** Post-map arithmetic; breaks the invariant. The
  fix if a hard edge looks wrong is a *different mask*, not a soft one.
- **A fixed per-style ground colour constant.** Fights palettes that cannot mix it and images
  with no related content. Derive it from image and palette together.
- **Sampling ground colours from photographs of famous paintings.** Rothko's lithol red has
  measurably shifted and the reproductions are uncalibrated. `[verified]` §3.2.
- **Tuning the ground colour against a statistical aesthetic objective.** All statistical
  properties together explain R² = 0.134 of beauty ratings on 150 abstract paintings.
  `[verified]` §3.6. This is report 04's "no automated quality score" reaching the same wall
  from the abstract-art side.
- **Adding a null / "bare canvas" candidate index.** §4, §5.3E.
- **Implementing the ground in slot 4.** ~80× cost for no benefit. §5.2.
- **Treating Mondrian's white or Pollock's canvas as a background in a preset.** Both are
  category errors on the artists' own terms, and a preset built on them will do something the
  user cannot explain. §1.3.

---

## 7. Verification debt

**Returned 403 or otherwise refused to fetch:**

- [MoMA collection page for Malevich, *White on White*](https://www.moma.org/collection/works/80385) — 403.
  The warm-ground claim in §3.3 is from search-index text of that page plus Wikipedia. **The
  primary curatorial wording is unconfirmed, including which of the two whites is warmer.**
- [Leonardo 55(1):43–47, Chinese landscape empty-space ratios](https://direct.mit.edu/leon/article/55/1/43/102698/A-Computational-Study-of-Empty-Space-Ratios-in) — 403.
  The 56.8% / 9.4% figures are from search summaries. **The method — how "empty" is thresholded
  — is entirely unverified**, which matters because the whole number depends on it.
- [ScienceDirect, *Quantifying subtle color transitions in Mark Rothko's abstract paintings through K-means clustering and Delta E analysis*](https://www.sciencedirect.com/science/article/pii/S1296207425000160) —
  403, and the [SSRN preprint](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=4997631)
  also 403. This is the single most relevant paper found for putting Lab numbers on abstract
  fields and **none of its results are in this report.** Highest-value item to clear.
- [JOV, Fowlkes, Martin & Malik 2007](https://jov.arvojournals.org/article.aspx?articleid=2122053) — 403.
  The 60.1% / 64.4% / 67.8% cue accuracies are from a search summary; **both PDF mirrors
  (Berkeley EECS and eScholarship) downloaded but would not parse as text.** Numbers are load
  bearing for §6's rejection of automatic segmentation — worth confirming in a browser.

**Downloaded but unparseable (PDF binary, no text layer extracted):**

- Greenberg, *Modernist Painting* — [YorkU PDF](https://www.yorku.ca/yamlau/readings/greenberg_modernistPainting.pdf).
  The [sharecom.ca mirror](http://www.sharecom.ca/greenberg/modernism.html) returned
  `ECONNREFUSED` and [giacomobelloni.com](http://www.giacomobelloni.com/page4/styled-45/index.html)
  served an empty body. **Every Greenberg quotation in §1.1 is `[relayed]` from search-index
  text, including "the first mark made on a canvas destroys its literal and utter flatness".**
  Verify before quoting in code comments or UI.
- Steinberg, *Other Criteria* — [MIT PDF](https://web.mit.edu/allanmc/www/othercriteria.pdf).
  §1.1's account of the flatbed picture plane is from search summaries only.
- DeCarlo & Santella, *Stylization and Abstraction of Photographs* (SIGGRAPH 2002) —
  [PUC-Rio PDF](https://web.tecgraf.puc-rio.br/~scuri/inf1378/pub/decarlo.pdf) would not parse.
  It is the closest published prior art to option B ("bold edges and large regions of constant
  color", hierarchical segmentation, eye-tracking to decide what to keep) and **I could not
  read how they choose the hierarchy level or the region colour.** Worth reading before
  implementing the mask.
- Achanta et al., *Frequency-tuned Salient Region Detection* (CVPR 2009). The formula in
  §5.3B is from a search summary. The kernel size ("5×5") and whether `I_blur` uses the full
  image or a fixed small kernel are unconfirmed.

**Asserted from weak sources:**

- **Raw canvas colour.** The ecru hex `#C2B280` is from colour-naming sites
  ([Wikipedia, Ecru](https://en.wikipedia.org/wiki/Ecru), colourlovers), **not a measurement of
  artists' cotton duck or linen.** The Lab conversion of it is sound; the input is not. If a
  "raw linen ground" preset ships, measure a real canvas with a colorimeter first.
- **Kandinsky's cream/pale-blue ground** (§3.4) is from general art-appreciation sites. I found
  no Guggenheim conservation or technical source. Treat as illustrative, not as data.
- **Newman's field colour.** Described qualitatively as "a vast red field"; I found no pigment
  identification and no colorimetry. The C\*ab 89.2 figure in §2.4 is the *library's* Cadmium
  Red Light, offered as an existence proof that a saturated red ground is mixable — **not as a
  measurement of the painting.**
- **Golden Neutral Grey N6.** I assumed the "N6" designates Munsell value 6 and converted to
  L\* 61.7. Gamblin's and Golden's own greys are different products and I did not confirm
  either manufacturer's value definition.

**Internal inconsistency noted and not resolved:**

- [PMC3968763](https://pmc.ncbi.nlm.nih.gov/articles/PMC3968763/) (beauty in abstract
  paintings) — the fetched summary reports ρ = −0.277 for HSV value and ρ = −0.206 for CIELAB
  lightness while also stating observers preferred "bright images… of high color value". The
  **sign of the lightness effect is unresolved**; only the magnitude (|ρ| < 0.28) and the
  overall R² = 0.134 are used in this report.

**Not attempted:**

- No measurement of what fraction of a *Western* painting's ground is typically left visible.
  The craft sources give a principle and no number, and report 03's 0–30% remains a guess.
- No dataset segmenting grounds from figures across abstract paintings. If one exists I did not
  find it, and §3.6's conclusion — that no defensible statistical claim about ground lightness
  and chroma is available — rests on that absence.
- The ~80× per-pixel-versus-per-colour cost estimate in §5.2 assumes ~150,000 occupied cache
  keys for a 12 MP photograph. **That occupancy was not measured.** It is a ~30-line probe
  against `ResolveOncePerColour` and would be worth running before anyone is tempted by slot 4.
