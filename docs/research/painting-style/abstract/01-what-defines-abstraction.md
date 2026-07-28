# Research: What Defines Abstraction, and What Would Make a Converted Photo Read as Abstract

**Track:** Abstract art, track 1 of 4. **Date:** 2026-07-28. **Status:** research only, no code changed.
**Prior work this builds on and must not contradict:** [`../README.md`](../README.md),
[`../02-styles-and-movements.md`](../02-styles-and-movements.md),
[`../04-appeal-and-perception.md`](../04-appeal-and-perception.md).

**Claim markers** follow the directory convention:
`[verified]` — I read the primary source or ran the measurement myself.
`[relayed]` — a secondary source or search summary asserts it and I could not open the primary.
`[inferred]` — my reasoning, not in any source.

---

## 0. Conclusions

**Abstraction is a shape operation. The current `Abstract` style is a colour operation. That is
the whole finding, and it is measurable in this repository today.**

Every published statistical signature that separates abstract from representational art is
spatial. None of them is chromatic or tonal:

- Abstract works have a **shallower** amplitude-spectrum slope than representational ones —
  **−1.13 ± 0.061** (n=12) against **−1.26 ± 0.039** for landscape and **−1.25 ± 0.030** for
  portrait/still-life, significant at p < 0.03. In the same analysis, **the basic intensity
  statistics — mean, variance, skew, kurtosis — did *not* differ significantly by content.**
  `[verified — Graham & Field 2008, Table 2, read in report 02 §1.2 from the primary PDF]`
- Abstract art's **first-order edge-orientation entropy collapses to 3.945 ± 0.722** against
  **4.380 ± 0.214** for traditional Western oils (n=572 vs 1,629). Few, long, aligned edges.
  `[verified — Redies et al. 2017, quoted in report 02 §1.5 and 04 §7.3]`
- Minimalism, Hard-Edge and Colour Field occupy the lowest-entropy / highest-complexity corner
  of the 92-style order/disorder map, characterised in the authors' own words by "simple design
  elements that are well-delimited by abrupt transitions of colors."
  `[verified — Sigaki, Perc & Ribeiro 2018, quoted in report 02 §1.3]`

And the perceptual literature says the operations the pipeline currently performs **cannot
destroy representation**, because representation survives far more damage than the converter
does:

- Humans identify the semantic category of a real-world scene from a **32×32 colour thumbnail
  at 80.8% accuracy** (78.8% greyscale), and natural scenes remain **65.1%** correct at **8×8**.
  `[relayed — Torralba et al., see §2.1]` Resolution loss does not remove the scene.
- **Line drawings are identified about as quickly as full-colour photographs** — colour,
  texture and shading are near-irrelevant to object recognition.
  `[relayed — Biederman & Ju 1988, see §2.2]` So *no* operation confined to colour can make a
  photograph read as non-representational.
- NPR abstraction makes recognition **easier**: participants were **faster at naming abstracted
  faces than photographs**, and better at remembering abstracted scenes.
  `[relayed — Winnemöller, Olsen & Gooch 2006, see §2.3]`

### The measured defect in the shipped `Abstract` style `[verified — measured locally, 2026-07-28]`

I ran a connected-component and colour-count pass over the five committed golden renders in
`Tests/Golden/` (128×128, six-paint palette, `MarkPixels = 4` for all five, each style's own
`MarkScale` applied on top). Four-connected, alpha masked off — the same convention as
`PaintabilityMetrics`.

| Style | `MarkScale` | Distinct colours | Regions | Median region px | Largest region px | Top-5 region share | Area-weighted mean region | Colours covering 90% of pixels |
|---|---|---|---|---|---|---|---|---|
| Realism | 1.0 | 161 | 425 | 3 | 1611 | 25.4% | 375.5 | 88 |
| Tonalism | 1.2 | **151** | **344** | 6 | 1431 | 25.2% | 387.5 | **70** |
| Fauvism | 1.3 | 331 | 1035 | 4 | 1280 | 22.2% | 251.5 | 177 |
| Post-Impressionism | 1.6 | 205 | 486 | 5 | 1139 | 24.5% | 314.7 | 108 |
| **Abstract** | **2.5** | **322** | **685** | 6 | **2009** | **30.2%** | **454.9** | **159** |

Read the Abstract row carefully, because it is more interesting than "least flat":

- Abstract has the **largest single region (2009 px)**, the **highest top-5 region share (30.2%)**
  and the **highest area-weighted mean region size (454.9)** of all five styles. Its guided-filter
  floor at strength 5 and a 2.5× mark genuinely does produce big plateaus.
- It simultaneously has **twice Realism's distinct-colour count (322 vs 161)**, **1.6× Realism's
  region count (685 vs 425)**, and needs **159 colours to cover 90% of its pixels against
  Realism's 88**.

So the output is **bimodal**: large flat plateaus separated by wide transition bands sprayed with
many small distinct mixtures. The floor is doing its job; the `ToneAndChromaRemap` at contrast 1.5
and chroma 1.5 is undoing it. Expanding contrast and chroma stretches mid-tones across *more*
candidate cells, so every plateau boundary becomes a multi-step ramp instead of an edge.
`[inferred, but the mechanism is the same one report 02 §0.7 warns about — expansion followed by
nearest-Lab matching bands rather than saturating]`

**The corrective is not more smoothing. It is fewer available colours and straighter, fewer
boundaries.** Abstraction is a *colour-count* and *shape* problem, and both live in slots the
pipeline already has: slot 3 (candidate transform) and slot 5 (post-map selection). Both are
invariant-safe by construction — slot 3 only removes candidates, slot 5 only rewrites indices.

**Caveats on the table.** One synthetic 128×128 hue-rich gradient, one six-paint palette, one mark
size. It is directional evidence about the shipped defaults, not a corpus measurement. The
`StyleBehaviourTests` sub-mark fractions (Fauvism 7.80%, Abstract 6.39%) measure a different
quantity — fraction of pixels below *that style's own* mark² — and do not contradict this.

### Top three recommendations

1. **A colour-count reduction stage in slot 3** (`ICandidateTransform`). Subset the candidate set
   to N mixtures by farthest-point or k-means selection in CIELAB before any pixel is mapped.
   Invariant-safe by construction. Directly attacks the measured defect. ~60–80 lines. §4.1.
2. **A small-region merge stage in slot 5** (`IPostMapStage`). Flood-fill the index buffer and
   rewrite every region below mark² to its largest 4-connected neighbour's index. Invariant-safe
   by the interface signature. Enforces the mark invariant instead of hoping the pre-map floor
   achieves it. ~100 lines, and `PaintabilityMetrics.ForEachRegion` is already the flood fill. §4.2.
3. **An axis-aligned recursive partition mode** ("Neoplastic"), deriving a BSP/kd rectangle
   decomposition from the photo and filling each cell with its re-mapped mean. This is the only
   proposal in this report that will make a viewer say "abstract" unprompted, and it has a direct
   art-historical warrant in van Doesburg's *Composition VIII (The Cow)* sequence. §4.5.

**And one thing to stop doing:** raising `chroma` and `contrast` to signal abstraction. It is the
mechanism producing 322 colours where the style wants 8–20.

---

## 1. Definitions and the spectrum

### 1.1 The boundary, and who drew it

Tate's own terms, which are the cleanest standard usage `[verified — fetched
https://www.tate.org.uk/art/art-terms/a/abstract-art]`:

- **Abstract art** — work that "does not attempt to represent an accurate depiction of a visual
  reality but instead uses shapes, colours, forms and gestural marks to achieve its effect."
- **Semi-abstraction / derived abstraction** — "based on an object, figure or landscape, where
  forms have been simplified or schematised." Cubist and Fauvist artists pioneered it.
- **Pure abstraction** — "forms, such as geometric shapes or gestural marks, which have **no
  source at all in an external visual reality**." Pioneered by Malevich and Mondrian c. 1910–1920.
- Tate notes directly that some artists prefer **concrete art** or **non-objective art**, but that
  "in practice the word abstract is used across the board and the distinction between the two is
  not always obvious."

**That last sentence is the operative one for this app.** The distinction that matters technically
is not the art-historical label but a single question: **does the image have a source?** A photo
converter can only ever produce derived abstraction. It cannot produce non-objective art, because
non-objective art is defined by the absence of the thing the app takes as input. Any "Abstract"
style this app ships is by definition on the abstraction-from-nature side of Tate's line.
`[inferred]`

The two founding paths are genuinely distinct and were characterised that way at the time:
Kandinsky "approached abstraction tentatively and visually, by gradually and progressively
concealing forms drawn from nature", whereas Malevich "plunged precipitously into abstraction, by
creating symbolic elements that had no representational origins" and sought to free art from "the
burden of the object". `[relayed — Galenson & Jensen, "Two Paths to Abstract Art: Kandinsky and
Malevich", NBER WP 12403, https://www.nber.org/papers/w12403; I read the search summary and the
abstract, not the full paper]`

**Kandinsky's path is the app's path. Malevich's is unavailable to it.** `[inferred]`

### 1.2 The movements, and whether a photo converter can reach them

| Movement | Defining property | Reachable from a photo? |
|---|---|---|
| **Abstraction-from-nature** (Kandinsky pre-1914, Mondrian's tree series, van Doesburg, Ellsworth Kelly) | Progressive simplification of an observed subject; the subject remains the generator | **Yes.** It is *defined* as a transform of an observation, which is exactly what the converter is. |
| **Neoplasticism / De Stijl** | Orthogonal rectangles, no diagonal, primaries plus black/white/grey | **Yes, partially.** The partition can be derived from image structure; the palette restriction is a palette preset the app already supports. Best geometric fit. |
| **Hard-edge painting** | Flat unmodulated planes, abrupt boundaries, "consciously impersonal approach to paint application", term coined by Jules Langsner in 1959 for the *Four Abstract Classicists* show (McLaughlin, Hammersley, Feitelson, Benjamin) `[relayed — https://www.tate.org.uk/art/art-terms/h/hard-edge-painting]` | **Yes — the best fit of any abstract movement.** It is a quantiser plus a boundary-simplification problem, and both are invariant-safe slots. |
| **Suprematism** | Invented geometric elements on white, explicitly rejecting representational origin | **No** as a derivation. Only reachable by extracting shapes and recomposing them, which severs the photo and forfeits the app's premise. |
| **Abstract Expressionism (gestural)** | The recorded trace of the artist's arm; the process is the content | **No.** Needs a stroke renderer and a source of gesture that a photograph does not contain. Report 02 §12 already ranks stroke synthesis as "a renderer, not a filter". |
| **Colour Field** | Large fields of "more or less flat single colour", stained soft edges, scale, absence of depicted content `[relayed — https://www.tate.org.uk/art/art-terms/c/colour-field-painting]` | **No.** See §1.3. |
| **Op Art** | "Geometric forms to create optical effects", depending on how "viewers' eyes and minds process visual information" (Riley, Soto, Vasarely) `[verified — fetched https://www.tate.org.uk/art/art-terms/o/op-art]` | **No.** Generative, not derived. The pattern is the work; the photo would contribute nothing. |
| **Lyrical Abstraction** | Fluid, spontaneous, gestural, "unpredictability in composition" `[relayed]` | **No.** Same failure as AbEx, plus deliberate unpredictability is not a transform of an input. |
| **Biomorphic abstraction** | Abstract forms that "refer to, or evoke, living forms" — curvilinear rather than rectilinear, organic rather than geometric (Arp, Miró, Moore) `[verified — fetched https://www.tate.org.uk/art/art-terms/b/biomorphic]` | **Partially.** Region contours from a photograph are already curvilinear; a smoothed-contour mode is the natural biomorphic counterpart to the rectilinear Neoplastic mode. |

The **geometric / biomorphic** split is the most useful axis for the app, because it maps directly
onto a single implementation choice: **how the boundary of a merged region is re-drawn** — polyline
simplification and axis snapping give geometric, contour smoothing gives biomorphic. One stage, one
parameter. `[inferred]`

### 1.3 Colour Field: I agree with the prior rejection, and I would strengthen it

Report 02 §11 calls Colour Field "not a photo-conversion style at all" and the synthesis README
lists it under "what not to build". **Agree.** Three additional reasons, one of which is
architectural rather than aesthetic:

1. **Its defining property is the absence of the input.** Hard-edge, Neoplasticism and derived
   abstraction all take a *structure* and simplify it. Colour Field takes *nothing* and presents
   scale and surface. A photograph supplies content, which is the one thing the style requires you
   not to have. `[inferred]`
2. **Its edges break the invariant.** Rothko's and Louis's boundaries are stained, feathered and
   soft — the field bleeds into the ground over centimetres. In this pipeline a soft boundary is
   post-map arithmetic (anti-aliased or filtered), which is the category the four-category table
   marks as invariant-breaking. Re-running the mapping repairs the invariant but destroys the
   softness, which was the whole style. `[inferred, using the table in ../README.md]`
3. **It sits at the *opposite* corner of the order/disorder map from what an under-specified
   "2–4 huge flat regions" preset would produce.** Colour Field is low-entropy/**high**-complexity
   — structured, deliberate, abrupt-bounded. An extreme posterisation of a photo is low-entropy and
   *low*-complexity: it is a blob map, not a composition. Shipping it under the name would be a
   false claim about which corner it reaches. `[inferred from Sigaki et al. 2018, verified in report 02 §1.3]`

If the app ever wants the *look*, the honest name is "extreme posterisation", exactly as report 02
recommends.

---

## 2. The perceptual question: what must be destroyed, and what may be kept

**This is the most valuable section, and its result is negative in a useful way: everything the
current pipeline manipulates is on the "may be kept" side.**

### 2.1 Resolution is not it

Humans identify the semantic category of a real-world scene from a **32×32 colour image at 80.8%**
accuracy, **78.8%** in greyscale; for natural scenes performance is still around **65.1% at 8×8**;
and "more than 80% of the segmented objects are correctly recognized at very low resolution."
`[relayed — Torralba, "How Many Pixels Make an Image?", Visual Neuroscience special issue,
https://people.csail.mit.edu/torralba/publications/howmanypixels.pdf, and Torralba, Fergus &
Freeman, "80 Million Tiny Images", IEEE PAMI 2008, https://cs.nyu.edu/~fergus/papers/tiny.pdf.
**Both PDFs failed to extract to text through the fetch tool; the digits come from search
summaries — see §6.**]`

The blanket statement in the 80-million-images work is that "the human visual system is remarkably
tolerant to degradations in image resolution: in a scene recognition task, the performance of
subjects is similar whether 32×32 color images or multi-mega pixel images are used." `[relayed]`

**Consequence.** The `EdgePreservingFloor` at strength 5 with a 2.5× mark, and the blur slider at
any setting, are operating far above the resolution at which scene identity dies. A 4 MP photo
smoothed to a 10 px mark still carries roughly 200× the pixels of a thumbnail that reads at 80%.
**No amount of the app's smoothing will make a photograph stop being a photograph of something.**
`[inferred]`

### 2.2 Colour, texture and shading are not it either

Biederman & Ju (1988) compared identification latency for 126 subjects on objects presented as
**line drawings** versus **colour photographs**, and found the line drawing "was identified about
as quickly as the photograph", concluding that primal access to an object representation can be
modelled from an edge-based description and that internal surface features — colour, texture,
shading — are of minimal importance. `[relayed — Biederman & Ju, *Cognitive Psychology* 20(1):38–64,
1988, https://www.sciencedirect.com/science/article/abs/pii/0010028588900242; ScienceDirect is
paywalled, ERIC record https://eric.ed.gov/?id=EJ378302. I read the abstract via search summaries
only.]`

**Consequence, and it is the strongest single result for this brief.** `ILabRemap` is a pure
colour → colour function. `ICandidateTransform` changes which colours exist. Neither can touch the
edge structure that carries recognition. **Slot 2 and slot 3 cannot, even in principle, produce
abstraction.** They can only produce a differently-coloured picture of the same thing. This is the
formal reason the current `Abstract` style — whose only differences from Post-Impressionism are a
remap at bigger numbers and a mother colour — cannot work as designed. `[inferred]`

The known qualification: Sanocki, Bowyer, Heath & Sarkar showed that line drawings produced by
*edge detectors* are considerably less recognisable than artist-drawn ones, so "edges are
sufficient" holds for well-chosen edges, not for any gradient map. `[relayed — I did not open this
paper; it surfaced in the same search and is worth checking before building any key-line feature.]`

### 2.3 Abstraction, as NPR does it, makes recognition *better*

Winnemöller, Olsen & Gooch's real-time video abstraction — bilateral-ish flattening plus
difference-of-Gaussian edges plus luminance quantisation — was validated with a user study finding
**participants were faster at naming abstracted faces of known persons than photographs**, and
**better at remembering abstracted images of arbitrary scenes** in a memory task. `[relayed —
Winnemöller, Olsen & Gooch, SIGGRAPH 2006, ACM TOG 25(3):1221–1226,
https://dl.acm.org/doi/10.1145/1179352.1142018; the Colby PDF mirror failed TLS verification and
ResearchGate returned 403 — see §6. I did not obtain the participant count or the reaction times.]`

This lines up with the processing-fluency account already recorded in report 04 §7.1, and with
DeCarlo & Santella's framing of abstraction as "clarifying meaningful structure". **What the NPR
literature calls "abstraction" is legibility engineering, not de-representation.** Naming it the
same thing as art-historical abstraction is a false friend, and the current `Abstract` style has
fallen into it. `[inferred]`

### 2.4 So what *does* make a viewer classify an image as abstract?

The literature does not give a clean threshold, and I want to be honest that I could not find one.
What it gives instead:

- **Viewers detect intention in abstract work, reliably, without training.** Hawley-Dolan & Winner
  paired 30 abstract-expressionist paintings (Hofmann, Twombly, Kline, Francis) with closely matched
  paintings by preschool children and by gorillas, chimpanzees, monkeys and elephants — matched on at
  least two of colour, line quality, brushstroke, medium and composition. Participants preferred the
  professional works and judged them better **even when the labels were reversed**. `[relayed —
  Hawley-Dolan & Winner, "Seeing the Mind Behind the Art", *Psychological Science* 22(4):435–441,
  2011, https://journals.sagepub.com/doi/abs/10.1177/0956797611400915; SAGE not opened. Follow-up:
  "Your kid could not have done that", https://pubmed.ncbi.nlm.nih.gov/25659538/]`
  **Reading for this app:** abstraction is not read as *absence of structure*. Structure that looks
  chosen reads as art; structure that looks accidental does not. Quantisation confetti will read as
  accidental. `[inferred]`
- **Naive observers recover canonical art periods from appearance alone.** Wallraven et al. had
  non-experts sort printouts of artworks from different periods into free categories, and MDS on
  the similarity data produced clusters "corresponding sometimes surprisingly well to canonical art
  periods." `[relayed — Wallraven, Fleming, Cunningham, Rigau, Feixas & Sbert, "Categorizing art:
  Comparing humans and computers", *Computers & Graphics* 33(4):484–495, 2009,
  https://www.sciencedirect.com/science/article/abs/pii/S0097849309000612 — paywalled]`
  So style *is* perceptually available from low-level appearance, which is the optimistic half of
  the bounding result in report 02 §0.1.
- **Eye movements over abstract work differ, but the reported effect is dispersion, not a
  category boundary.** Abstract paintings draw "dispersed attention, focusing on geometric shapes
  and color contrasts", while figurative work concentrates on narrative elements; Wallraven et al.
  found a strong effect of art period on both number and duration of fixations. `[relayed — search
  summaries over the eye-tracking-and-art literature; I did not open a primary source for the
  dispersion claim and would not build on it]`

**My reading of the whole set** `[inferred]`: an image reads as abstract when a viewer's attempt to
resolve it into named objects fails *and* the residue still looks deliberate. The first half is a
shape condition — you have to break the object contours, not the colours inside them. The second
half is a regularity condition — the surviving shapes have to be few, large, and related to each
other. Both conditions are shape conditions. Neither is a colour condition. That is the whole
design implication.

---

## 3. Degrees of abstraction as a controllable parameter

**Yes, defensibly — and the discipline built the demonstration itself, three times over.**

Abstraction-from-nature is historically *documented as a sequence of steps from one source image*,
which is exactly the shape of a knob:

- **Theo van Doesburg, *Composition VIII (The Cow)*, c. 1917–18, MoMA.** A graphite drawing "is
  immediately recognizable as a cow or bull grazing head down in a field"; a second "squares off
  those contours" so "the cow's hip and thigh muscles are simplified into a block, and the rear leg
  is straightened into an elongated peg"; a painted study continues the squaring; the final
  painting "consists of just fourteen rectangles… four green, three blue, four black, and two red",
  aligned with the canvas edges and non-overlapping. `[relayed — MoMA collection pages
  https://www.moma.org/collection/works/79189 and https://www.moma.org/collection/works/85589,
  read via search summary]`
- **Mondrian's tree series** (*Red Tree* → *Grey Tree* → *Flowering Apple Tree* → *Composition*),
  1908–1913, the canonical teaching example of the same progression. `[relayed]`
- **Picasso, *Le Taureau* (1945), eleven lithographic states** from a modelled bull to a contour.
  `[relayed]`

Van Doesburg's sequence is the most useful because its steps are *nameable operations*: recognisable
outline → contour simplification → rectilinearisation → rectangle count reduction → axis alignment.
That is a pipeline, not a metaphor. `[inferred]`

The psychology literature has also treated abstraction as a rated continuum rather than a binary.
Chatterjee et al.'s **Assessment of Art Attributes (AAA)** includes *abstraction* as a
conceptual-representational attribute measured on a Likert scale with anchor training slides;
other work classifies paintings on a five-category representational-to-abstract dimension.
`[relayed — from search summaries over the AAA instrument and follow-on studies; I did not open
Chatterjee et al. 2010]`

### 3.1 What the ends of the knob should be

Not "chroma ×1 to ×2". `[inferred]`

| Axis | Knob = 0 (Realism) | Knob = 1 (fully abstract) | Slot |
|---|---|---|---|
| **Colour count** | Full candidate set (thousands) | 5–8 mixtures | 3, candidate transform |
| **Minimum region area** | 1 px (unconstrained) | ≈ (min(w,h)/8)² | 5, post-map selection |
| **Boundary geometry** | Pixel-accurate | Polyline-simplified, then axis-snapped | 5, post-map selection |
| **Boundary count** | Unbounded | ~10–20 regions total | 5, via merge threshold |

Every one of those four is monotone, every one has a defensible endpoint, and **none of them is a
colour transform.** A single 0–1 slider driving a schedule over the four is the honest "how
abstract" control. The `MarkScale` field and the `EdgePreservingFloor.strength` parameter are
*not* that control and should stop being sold as it.

### 3.2 One product warning the literature hands over

**Making the output more abstract will, on average, make laypeople like it less.** Level of
abstraction affected the aesthetic judgements and emotional-valence ratings of laypersons —
"highest for representational paintings and lowest for abstract paintings" — while having **no
effect on experts' opinions**. `[relayed — "Experiencing Art: The Influence of Expertise and
Painting Abstraction Level", https://www.researchgate.net/publication/51664924 ; read via search
summary, primary not opened]`

Ship the knob. Do not default it high, and do not describe high settings as "better".

---

## 4. How to turn a photograph into something abstract, ranked by payoff ÷ cost

Every item names its slot. The invariant status of each follows mechanically from the interface
signatures in `Imaging/Styles/PipelineStages.cs`, which is a nicer property than a rule anyone has
to remember.

### 4.1 Candidate-set thinning — slot 3, `ICandidateTransform`. **Payoff very high, cost low.**

**Operation.** Before the gamut sampler renders anything, reduce the mixture list to N entries by
farthest-point sampling (or k-means) in CIELAB. Expose N as the stage's parameter, default 8–16
for an abstract style.

**Why it is the top pick.** It attacks the exact measured defect: 322 distinct colours in a render
that should have ten. It is the operation the whole low-entropy corner of the style map is made of
— nishiki-e used 7–10 blocks, Katz screenprints run 10–38 flat colours (report 02 §8, `[relayed]`),
Neoplasticism runs five. And it is *free at match time*: the nearest-neighbour grid index gets
smaller, so conversion gets faster.

**Invariant status.** Safe by construction: removing candidates cannot synthesise a colour outside
the candidate set. This is what slot 3 is for.

**Failure mode to watch.** Naive k-means over the candidate set will over-sample wherever the paint
gamut is dense (neutrals) and under-sample the chroma extremes. Farthest-point sampling seeded with
the palette's own extreme mixtures avoids that and is simpler. `[inferred]`

**Cost.** ~60–80 lines plus a parameter. No new dependency.

### 4.2 Small-region merge — slot 5, `IPostMapStage`. **Payoff very high, cost low.**

**Operation.** Flood-fill the index buffer four-connected; for each region below `MarkPixels²`,
rewrite every pixel to the index of its largest 4-connected neighbouring region. Iterate to a fixed
point (two or three passes suffice in practice `[inferred]`).

**Why.** It converts the mark invariant from a hope into a guarantee. Today it is enforced
indirectly by a pre-map smoothing floor whose strength has to be guessed against an unknown input;
`EdgePreservingFloor`'s own doc comment admits it "does not by itself guarantee any particular style
clears a given fragmentation bar". A post-map merge measures the thing the bar is about and fixes
it. It also raises *order* without lowering *complexity* further, which is the direction Van Geert
& Wagemans' order/complexity split says matters (report 04 §7.2), and it removes the "quantisation
confetti" that reads as accidental rather than chosen (§2.4).

**Invariant status.** Safe by the signature: `Refine` takes and returns indices, so it cannot name
a colour outside the candidate set. No arithmetic, no re-mapping needed.

**Cost.** ~100 lines. `PaintabilityMetrics.ForEachRegion` is already the flood fill, already
stack-based rather than recursive, already four-connected for the right reason. Extract it.

**Note.** 4.1 and 4.2 compose multiplicatively — fewer colours means larger regions before the merge
even runs, and the merge then cleans the residue. Build them together.

### 4.3 Anisotropic Kuwahara filtering — slot 1, `IPreMapStage`. **Payoff high, cost medium.**

Already the four-track consensus in `../README.md`, and it is *specifically* the right filter for
abstraction rather than merely for painterliness: it "generates a painting-like flattening effect
along the local feature directions while preserving shape boundaries", producing results with "the
clearness of cartoon illustrations but also directional information as found in oil paintings."
`[relayed — Kyprianidis, Kang & Döllner, "Image and Video Abstraction by Anisotropic Kuwahara
Filtering", *Computer Graphics Forum* 28(7), 2009,
https://www.kyprianidis.com/p/pg2009/ ; multi-scale variant https://www.kyprianidis.com/p/tpcg2010/ ]`

The abstraction-specific argument: aligning the flattening to local feature direction is what drives
edge orientations toward a small number of dominant angles, and **low edge-orientation entropy is
the measured signature of abstract art** (3.945 vs 4.380, §0). No other surveyed filter targets that
statistic. `[inferred, from the verified Redies figures]`

Costs the structure tensor (~60 lines) which is shared infrastructure the roadmap already wants.

### 4.4 Contour simplification — slot 5. **Payoff high, cost medium-high.**

**Operation.** After 4.2, trace region boundaries, run Douglas–Peucker with a tolerance tied to
mark size, and re-rasterise each region by scanline fill of the simplified polygon. Two variants
from one parameter: **geometric** (also snap segment angles to a small set, e.g. 0°/45°/90°) and
**biomorphic** (Chaikin or B-spline smoothing instead of snapping).

**Why.** This is the operation that produces "simple design elements well-delimited by abrupt
transitions of colors" — the literal description of the low-entropy corner. It is also the single
step in the van Doesburg sequence that most changes whether a shape still reads as a cow.

**Invariant status.** Safe: re-rasterising assigns existing indices to pixels. No arithmetic.

**Cost.** Contour tracing plus polygon fill in C# is the largest item on this list that is still a
feature rather than a project — call it 300–400 lines. Rank it below 4.1/4.2 purely on cost.

### 4.5 Recursive rectangular partition ("Neoplastic" mode) — slot 1 or a dedicated style. **Payoff very high on the *reads-as-abstract* criterion, cost low-medium.**

**Operation.** Recursively split the image with an axis-aligned cut, choosing the axis and position
that most reduces within-cell CIELAB variance (a BSP / kd-tree over the image, terminating on a
cell-count or variance budget). Fill each leaf with its mean colour. Run the result through the
normal mapping.

**Why it is here despite being the most "gimmick-shaped" idea in the list.** Every other proposal
makes the picture *simpler*; this is the only one that makes it *non-representational*, because it
destroys object contours outright while preserving the photograph's large-scale value and colour
composition. It is van Doesburg's terminal step, implemented. And it lands squarely in Tate's
"pure abstraction" territory while remaining honestly *derived* — the rectangle layout is a
function of the photograph, not decoration invented on top of it.

**Invariant status.** If implemented as a slot-1 pre-map stage, safe unconditionally — the mean is
computed on source pixels and everything is mapped afterwards.

**Cost.** ~150–200 lines. No dependency, no filter kernel, no structure tensor.

**Honest limits.** The palette restriction that makes it read as *Mondrian* specifically (red,
yellow, blue, black, white, grey) is a palette preset, not a pipeline feature, and the app already
does palette presets. Do not hard-code it into the stage.

### 4.6 Ranking summary

| # | Operation | Slot | Payoff | Cost | Invariant |
|---|---|---|---|---|---|
| 1 | Candidate-set thinning to N colours | 3 | Very high | Low | Safe by construction |
| 2 | Small-region merge to mark² | 5 | Very high | Low | Safe by signature |
| 3 | Recursive rectangular partition | 1 | Very high (reads-as-abstract) | Low-medium | Safe (pre-map) |
| 4 | Anisotropic Kuwahara | 1 | High | Medium | Safe (pre-map) |
| 5 | Contour simplification, geometric/biomorphic | 5 | High | Medium-high | Safe by signature |
| 6 | Single 0–1 abstraction knob over 1/2/5 | — | High (UX) | Low, once 1/2/5 exist | Inherits |

### 4.7 Research projects, not features — say so in the UI if they are ever attempted

- **Shape-grammar Suprematism.** Extract salient region shapes, discard their positions, recompose
  on a white ground. Once you discard position you have severed the photograph, and the app's claim
  ("this is your picture, in your paints") goes with it. `[inferred]`
- **Gestural stroke fields for Abstract Expressionism.** Needs a stroke renderer *and* a synthetic
  source of gesture. Report 02 §12 already classes stroke synthesis as large work; AbEx needs more
  than that, because the strokes are not derived from the image at all.
- **Semantic segmentation to abstract *objects* rather than *colour regions*.** This is what would
  actually let the converter break a contour deliberately rather than statistically. It needs a
  model, which is out of scope by the project's own stated constraint.
- **Cubist multi-viewpoint rendering.** There is genuine prior art (Collomosse & Hall, "Cubist Style
  Rendering from Photographs", IEEE TVCG 2003) `[relayed — not read]`, but it needs feature
  detection and salience-driven re-composition, and its output is a montage rather than a set of
  paintable regions.

---

## 5. What not to build

### 5.1 Neural style transfer — no, and the reason is not just the dependency

**Verdict: reject, and do not revisit.** Report 04 §11.6 flagged it out of scope; here is the real
verdict against it, four independent reasons any one of which is sufficient.

1. **It cannot run here.** Gatys-style optimisation with VGG-19 needs **over an hour of CPU time
   for a 500×500 image at 300 iterations**. `[relayed — CS229 project report,
   https://cs229.stanford.edu/proj2019spr/poster/10.pdf ; I read this figure in a search summary,
   not in the poster]` The app is .NET 5 WinForms, no GPU, no neural runtime, and shipping a large
   model file is already explicitly rejected in the project's constraints. Feed-forward variants
   are faster but still need the runtime and the weights.
2. **The invariant destroys the contribution.** NST emits colours from a continuous space. Every
   one would have to be re-quantised onto the achievable mixture set afterwards — post-map
   arithmetic followed by a re-map, which by the four-category table is legal but *discards
   precisely the sub-ΔE colour modulation NST spent the compute producing*. What survives
   quantisation is roughly the palette and the large-scale layout, both of which the app already
   controls directly and more cheaply.
3. **It transfers the wrong thing for this problem.** The Gram matrix is a second-order feature
   statistic that averages over spatial position, so it is "fully blind to the global arrangement of
   objects inside the reference image"; it captures texture and "the geometrical content or spatial
   semantics are ignored"; it "destroys the semantics of the style image, only preserving the basic
   texture components", and style "spills over" into mismatching regions — building texture in the
   sky. `[relayed — synthesised from several sources: "Pitfalls of the Gram Loss for Neural Texture
   Synthesis in Light of Deep Feature Histograms"
   https://www.researchgate.net/publication/342169118 ; Luan et al., "Deep Photo Style Transfer",
   CVPR 2017, https://www.cs.cornell.edu/~fujun/files/style-cvpr17/style-cvpr17.pdf ; "Multimodal
   Style Transfer via Graph Cuts", https://arxiv.org/pdf/1904.04443 . I read search summaries and
   abstracts, not the full papers.]` **Abstraction is a global-arrangement property (§0, §2).
   The Gram matrix is defined to be blind to exactly that.** NST is the wrong tool for abstraction
   specifically, independent of every practical objection.
4. **Its output is not a set of marks.** The mark invariant requires every output region to be
   something a brush can lay down. Texture-statistic output is per-pixel modulation by
   construction — the same disqualification that rules out dithering, arriving from a different
   direction.

### 5.2 Chroma and contrast multipliers as the abstraction lever

This is what ships today and it is **backwards**. Measured above: `Abstract` at contrast 1.5 /
chroma 1.5 produces 322 distinct colours against Realism's 161. Expansion before nearest-Lab
matching spreads mid-tones across more candidate cells, so plateau boundaries become multi-step
ramps. The prior research already warned that a naive chroma multiplier bands and hue-drifts rather
than saturating (median masstone C\* 33.6, best blue 70.7, best green 56.0) `[verified in report 02
§0.7 from the manifest]`; this report adds that it also *fragments*. `[verified — measured locally]`

### 5.3 More blur / stronger smoothing as the abstraction lever

Abstract works have a **shallower** amplitude-spectrum slope than representational ones
(−1.13 vs −1.25/−1.26) `[verified — Graham & Field 2008]`, and blur **steepens** the slope. So the
existing project finding — that blur makes a photo statistically *less* painting-like — is *more*
true for abstraction than for painting in general. Smoothing moves the image away from the abstract
end of the measured axis, not toward it.

Note the tension worth recording honestly: four Mondrian compositions measured a slope of
−1.4 ± 0.06, *steeper* than the abstract group mean and steeper than the representational groups.
`[verified — Graham & Field 2008, via report 02 §1.2]` So the spectral-slope signature is not
uniform across abstraction, which is another reason not to treat abstract art as one category
(§5.6).

### 5.4 Colour Field, Op Art, Lyrical Abstraction and gestural AbEx as conversion styles

See §1.2 and §1.3. Colour Field: agree with the standing rejection, plus the three additional
arguments. Op Art: generative, would ignore the photograph, and its high-frequency geometry is a
mark-invariant problem at any output size a photo converter chooses. Lyrical Abstraction and
gestural AbEx: the mark *is* the content and there is no mark data in a photograph.

### 5.5 Fractal / Pollock-style generation, and any fractal "abstractness" measure

Already covered by report 04 §7.3 and the synthesis README, and it applies here with extra force
because fractal dimension is the statistic most often reached for when someone wants to quantify
abstraction. Jones-Smith produced "Untitled 5" in Photoshop in minutes and it passed the published
fractal authenticity criteria; and in the 1,629-image comparison, abstract art's fractal dimension
(1.45 ± 0.22) is statistically indistinguishable from "Bad Art" (1.47 ± 0.15). `[verified — Redies
et al. 2017, via reports 02 §1.5 and 04 §7.3]`

### 5.6 Any automated "abstractness" or "abstract-art quality" score

Two results, and the second is the stronger one:

- Global image properties **do not relate to preference ratings for abstract art**, replicating an
  earlier null. In the same study, only a **semantic** network built from viewers' verbal
  descriptions predicted preference ratings and art-style affiliation; the GIP-based network did
  not. `[verified — Hayn-Leichsenring, Kenett, Schulz & Chatterjee, "Abstract art paintings, global
  image properties, and verbal descriptions", *Acta Psychologica* 2020, DOI
  10.1016/j.actpsy.2019.102936, abstract read verbatim at
  https://pubmed.ncbi.nlm.nih.gov/31743852/]`
- The authors' own closing sentence: **"it is not useful in empirical aesthetics to treat abstract
  art paintings as a single category."** `[verified — same]`

That second finding is a design instruction, not just a caution. **A single style row named
"Abstract" is the wrong shape.** Ship two or three named, visibly different modes — e.g. *Hard-edge*
(4.1 + 4.2 + 4.4-geometric), *Neoplastic* (4.5), *Organic* (4.1 + 4.2 + 4.4-biomorphic) — each of
which makes a claim the output can actually support. `[inferred]`

### 5.7 Tuning an abstract preset for broad appeal

You cannot, because there is no shared target to tune toward. Vessel & Rubin found robust and
consistent preferences *within* each observer for both image types, but **mean pairwise
between-observer correlation of 0.46 for real-world images against 0.20 for abstract images** —
they attribute the gap to shared semantic interpretation, which abstract images do not supply.
`[relayed — Vessel & Rubin, "Beauty and the beholder: Highly individual taste for abstract, but not
real-world images", *Journal of Vision* 10(2):18, 2010, DOI 10.1167/10.2.18; JOV returned 403, see
§6. Numbers from search summary and the PubMed record https://pubmed.ncbi.nlm.nih.gov/20462319/]`

Expose parameters. Do not chase a default that "looks good to everyone" — for abstract output that
population barely exists. This also means golden-image review for an abstract style is weaker
evidence than it is for Tonalism: one reviewer's "yes, that reads as abstract" generalises less.
`[inferred]`

### 5.8 Randomised or generative composition

"Scatter Kandinsky-ish shapes over the palette" is the most tempting cheap win and it severs the
product. The app's claim is that the output is *your photograph* in *your paints*. An abstract mode
whose composition is invented rather than derived is a random-art generator with a palette
constraint, and the user could get it without uploading anything. Every recommendation in §4 is
deliberately a *function of the input image*. `[inferred]`

### 5.9 Pixel dithering, again

Already rejected twice. Recording it here only because "abstract" is the style under which someone
will be most tempted to reintroduce it as texture. It fails the mark invariant regardless of what
it does to the colour invariant.

---

## 6. Verification debt

Ranked by how load-bearing the claim is.

1. **Torralba's tiny-image recognition numbers (80.8% colour / 78.8% greyscale at 32×32, 65.1% at
   8×8, >80% of segmented objects).** Both `howmanypixels.pdf` and `tiny.pdf` downloaded but would
   not extract to text through the fetch tool, and this environment has no PDF renderer. The digits
   are from search summaries. **This claim carries §2.1, which is half the argument that colour
   operations cannot produce abstraction. Get the PDF.**
2. **Biederman & Ju 1988.** ScienceDirect paywalled; ERIC record only. The "line drawings identified
   about as quickly as photographs" statement and the n=126 come from search summaries. **This is
   the single most load-bearing claim in the report — it is the formal reason slots 2 and 3 cannot
   produce abstraction. Get the paper.**
3. **Winnemöller, Olsen & Gooch 2006 user study.** The Colby PDF mirror failed TLS certificate
   verification; ResearchGate and the ACM DL entry were not retrievable. No participant count, no
   reaction times, no statistics obtained — only the qualitative direction. Also means I could not
   record the algorithm's actual parameter values (bilateral iterations, DoG constants, quantisation
   bin count), which would be directly useful for §4.3.
4. **Vessel & Rubin 2010 (r = 0.46 vs 0.20).** JOV returned 403. Numbers from a search summary and
   the PubMed abstract listing. Load-bearing for §5.7.
5. **Hawley-Dolan & Winner 2011.** SAGE not opened. The design (30 pairs, matched on ≥2 of five
   properties, label-reversal condition) and the direction of the result are from search summaries;
   I have no effect sizes or accuracy figures.
6. **Wallraven et al. 2009, "Categorizing art".** ScienceDirect paywalled, Academia.edu not fetched.
   The MDS-recovers-art-periods claim is relayed; no numbers.
7. **Galenson & Jensen, "Two Paths to Abstract Art" (NBER WP 12403).** Read only the search summary
   and the abstract framing. The Kandinsky/Malevich characterisation is a strong quotation and I
   would like the full paper before it is quoted again.
8. **Sanocki et al., "Are edges sufficient for object recognition?"** Not opened at all. It is the
   principal qualification on §2.2 and matters if key-line rendering is ever built.
9. **The eye-tracking "dispersed attention on abstract paintings" claim.** Search summaries only, no
   primary source identified. Not used to support any recommendation; do not promote it.
10. **Chatterjee et al.'s Assessment of Art Attributes (AAA).** Relayed. If a rated "abstractness"
    scale is ever wanted for user testing, this is the instrument to check first.
11. **Collomosse & Hall, "Cubist Style Rendering from Photographs" (IEEE TVCG 2003).** Named from
    memory of the NPR literature and not verified in this session. Confirm it exists as cited before
    anyone follows it.
12. **My local measurement** (§0) is reproducible but narrow: one 128×128 synthetic source, one
    six-paint palette, one mark size, and the golden renders fix `MarkPixels = 4` for all five
    styles. Re-run it on a real photograph at working resolution before treating the colour counts
    as characteristic. The script is a throwaway; it is not committed.
