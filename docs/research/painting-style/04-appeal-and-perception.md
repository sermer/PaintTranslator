# Research 04 — Appeal and Perception: Why a Literal Photo Copy Looks Dead

**Track:** Style-aware conversion, appeal/composition/perception arm
**Date:** 2026-07-26
**Scope:** why faithful photo reproduction reads as lifeless; value design and visual
hierarchy; aerial perspective; compositional "rules" and their actual evidence;
empirical aesthetics; colour-appearance effects; computational aesthetic scoring.
**Out of scope (covered elsewhere):** Kubelka–Munk mixing physics
([01](../source-reports/01-kubelka-munk-theory.md)), sRGB decode / ΔE metrics / gamut
mapping / sigmoidal value compression / segmentation
([02](../source-reports/02-photo-to-paint-pipeline.md)), pigment behaviour
([03](../source-reports/03-acrylic-paint-reality.md)), NPR prior art and licensing
([04](../source-reports/04-prior-art-and-algorithms.md)).

**Method note:** web research only. No repository files were modified, no `.cs` file was
read or changed. Several PDFs would not extract to text through the fetch tool; where I
could only obtain a search-engine summary or an abstract, the claim is marked `[relayed]`
and the failure is recorded in §9.

**Claim marking.** Every substantive claim carries one of:

- `[verified]` — I fetched the source (paper, abstract, or article body) and read the
  statement or the number in it.
- `[relayed]` — the claim comes from a search-result summary, a secondary description, or
  a paywalled abstract I could not open. Treat the wording and any digits as
  approximately right and re-check before building on them.
- `[inferred]` — my reasoning from the above, applied to this codebase. Not a finding.

---

## 0. Executive summary

1. **The single most defensible claim in this whole area is that painters do not
   reproduce, they *compress and redistribute*.** Two independent literatures converge on
   it: HDR tone mapping (a scene spans 10⁵:1, a reflective surface about 10²:1, matte
   acrylic less) and image statistics (art images show a *narrower range* of Fourier
   spectral slopes than matched photographs, reproducible by applying edge-preserving
   smoothing to the photographs). §1.1, §1.2, §7.
2. **Detail redistribution has direct experimental support and is the strongest thing
   this app could act on.** DiPaola, Riebe & Enns showed viewer gaze is attracted to and
   held longer by regions of *relatively* finer detail, and that appreciation rose when
   the rendering biased gaze strongly. Santella & DeCarlo showed viewers look where
   detail is locally preserved in an abstracted image. This is not art folklore; it was
   measured with an eye tracker. §1.2, §4.
3. **Value design (notan) has a real perceptual mechanism but essentially no quantitative
   art-instruction research behind the specific numbers.** The mechanism — coarse-to-fine
   processing, low spatial frequencies reaching cortex first, scene gist available under
   150 ms — is solid. "Two, three, or four values" and "it must work as a thumbnail" are
   heuristics with no measured optimum. Build the thumbnail check, do not build a
   value-count scorer. §3.
4. **Aerial perspective is the one compositional lever that is physically grounded and
   quantifiable.** Contrast is a demonstrated monocular depth cue in its own right
   (O'Shea, Blackburn & Ono 1994), and Koschmieder's law gives the exponential form. The
   problem is depth, not the effect. A cheap classical proxy exists: the dark-channel
   transmission map. §5.
5. **The compositional "rules" fail an evidence audit.** Rule of thirds: weak
   (ρ ≈ 0.17 with beauty ratings, and expert-only sensitivity). Golden ratio: a
   century of contradictory results on bare rectangles and no credible evidence it was
   used in painting composition. Dynamic symmetry: no empirical literature located at
   all. The strongest predictor of where people actually look is **centre bias**, which
   is image-independent. §6.
6. **Statistical aesthetics is real but weak, and it cannot tell good art from bad.**
   Spatial and chromatic image statistics explain only **6–15% of the variance** in
   beauty ratings of oil paintings, and the Museum of Bad Art overlaps traditional
   Western art heavily on fractal dimension, self-similarity, and edge-orientation
   entropy. §7.
7. **Do not build an aesthetic scorer.** State-of-the-art learned aesthetic models reach
   Spearman ≈ 0.61 against *crowd mean* photo-contest scores — a benchmark that is itself
   a poor proxy for whether a painting will be good. §8, §11.

---

## 1. Why faithful photo reproduction reads as lifeless

### 1.1 The dynamic-range gap — painters are forced to make choices, and the choices are the style

**The gap, in numbers.**

- Real-world scene dynamic range: a sunlit-to-deep-shade ratio in a single outdoor scene
  routinely exceeds 1000:1 and ratios of 100,000:1 are common in nature `[relayed]`
  ([HDRsoft, "HDR Images FAQ"](https://www.hdrsoft.com/resources/dri.html);
  [scantips, "Dynamic Range"](https://www.scantips.com/basics14.html)).
  The often-quoted figure for the range of real scenes is ~14 orders of magnitude across
  all conditions, versus 2–3 orders for displays `[relayed]`
  ([Mantiuk, "Tone Mapping" chapter, Cambridge](https://www.cl.cam.ac.uk/~rkm38/pdfs/tone_mapping.pdf)).
- A reflective print is typically limited to **under 100:1** `[relayed]` (same sources);
  blacks reflect a couple of percent, whites ~90–95%.
- **Acrylic on a panel is worse than a print.** Report 02 computed the Golden Heavy Body
  extremes as Titanium White L\*98.25 / Bone Black L\*23.82, a **74 L\* unit span and a
  ~24:1 luminance ratio**
  ([02 §0.3, §3.1](../source-reports/02-photo-to-paint-pipeline.md)). Matte acrylic in a
  normally-lit room, with surface flare, will do worse than that in practice `[inferred]`.

So the painter is working with about **1.4 orders of magnitude** against a scene of 3–5.
Every painter who works from life is running a tone-mapping operator in their head, and
they have been doing it for five centuries — the HDR literature explicitly frames itself
as inheriting that problem `[relayed]`
([Meylan, Daly & Süsstrunk, "The Reproduction of Specular Highlights on High Dynamic
Range Displays" and the related "Painting in High Dynamic Range," *J. Vis. Commun. Image
Represent.* 18(5), 2007](https://www.sciencedirect.com/science/article/abs/pii/S1047320307000120)
— paywalled; the framing sentence "the discipline of reproducing scenes with a high range
of luminances has a 5-century history that includes painting, photography, electronic
imaging and image processing" is `[relayed]` from the article summary).

**Why this makes literal reproduction look dead.** Nearest-neighbour matching in CIELAB
with no tone mapping is a *clipping* operator, not a compressing one. Everything below
the palette's darkest achievable L\* collapses onto a single black, and everything above
the lightest onto a single white. The consequences are specific and all read as "cheap":

- **Shadow families merge.** A photograph's shadow region typically contains 3–5 distinct
  value steps within 15 L\* units; a palette floor at L\*24 destroys all of them at once.
  Painters spend a disproportionate share of their available range on exactly this region
  because that is where form reads `[inferred, but consistent with the sigmoidal-remap
  finding in 02 §4.3]`.
- **The midtone slope goes wrong.** Linear rescaling into the paint range lightens the
  whole image and flattens it (02 §4.3 tabulates this: photo L\*50 → paint L\*61).
  Sigmoidal remapping keeps midtone slope near 1 and spends the compression at both ends,
  which is what Braun & Fairchild found produced superior matches for *every* image tested
  `[relayed via 02 §4.3]`.
- **The compression is not a neutral technical step — it is where the style lives.** The
  HDR literature splits operators into global/spatially-invariant tone reproduction
  *curves* and spatially-variant tone reproduction *operators* `[relayed]`
  ([Mantiuk, Cambridge](https://www.cl.cam.ac.uk/~rkm38/pdfs/tone_mapping.pdf)). A painter
  is doing the second: local contrast is preserved at the focal point and sacrificed
  elsewhere. **PalettePhotoConverter, being per-pixel and spatially independent, can only
  ever do the first — and only if a curve is added, which today it is not** `[inferred]`.

**The actionable form of this finding.** A global sigmoidal L\* remap (02 §4.3) is table
stakes. The *stylistic* upgrade is a **spatially-varying** remap: more of the available
range allocated to the region the viewer is meant to look at, less elsewhere. That is
where §4 and §10 go.

### 1.2 Uniform detail density — a photo resolves everything equally, a painting does not

This is the best-evidenced item in this whole report.

**Direct experimental evidence.**

- DiPaola, Riebe & Enns rendered portraits with a parameterised Rembrandt-style NPR
  technique and tracked viewers' gaze. **Gaze was attracted to, and held longer by,
  regions of relatively finer detail and by textural highlighting; artistic appreciation
  increased when the portrait strongly biased gaze** `[relayed]`
  ([DiPaola, Riebe & Enns, "Following the Masters: Portrait Viewing and Appreciation is
  Guided by Selective Detail," *Perception* 42, 2013](https://journals.sagepub.com/doi/10.1068/p7463)
  — SAGE returned 403; PDFs at
  [ivizlab.sfu.ca](http://ivizlab.sfu.ca/papers/perceptionPaperDiPaolaC.pdf) and
  [UBC VisionLab](https://visionlab-psych.sites.olt.ubc.ca/files/2014/12/149_DiPaola_etal_PERCinpress.pdf)
  would not extract to text). The related earlier framing is
  [DiPaola & Riebe, "Rembrandt's Textural Agency"](https://summit.sfu.ca/item/52) and the
  press summary [ScienceDaily, 2010](https://www.sciencedaily.com/releases/2010/05/100528092019.htm).
  The term the authors use for the mechanism is **textural agency** — selective variation
  in image detail used to guide the observer's eye.
- Santella & DeCarlo validated the converse direction: **viewers are attracted to areas
  where detail is locally preserved in a meaningfully abstracted image** `[relayed]`
  ([Santella & DeCarlo, "Visual interest and NPR: an evaluation and manifesto," NPAR
  2004](https://dl.acm.org/doi/10.1145/987657.987669)).
- The original system that generated those images used **eye-tracking data from a 5-second
  free viewing of the photograph to decide where to preserve detail**, then rendered bold
  edges and large regions of constant colour everywhere else `[verified]`
  ([DeCarlo & Santella, "Stylization and Abstraction of Photographs," SIGGRAPH
  2002](https://lvelho.impa.br/ip02/papers/decarlo/); ACM DOI
  [10.1145/566570.566650](https://dl.acm.org/doi/10.1145/566570.566650)). Their framing
  is "clarifying meaningful structure" — good *information design*, not a filter.

**Statistical evidence that painters actually do this.**

- Mather compared the Fourier spectral slope of **31 artworks against closely matched
  photographs** and found **the artworks occupy a relatively narrow range of spectral
  slopes compared to the photographs.** Two explanations were tested. Simple band-pass
  filtering (the "window of visibility" account) could *not* reproduce the compression.
  **Applying "artistic" filters that smooth textural detail while preserving edges *did*
  reproduce it** `[relayed]`
  ([Mather, "Artistic Adjustment of Image Spectral Slope," *Art & Perception* 2:11–22,
  2014](https://georgemather.com/PDF/Mather_ArtP_2014.pdf) — PDF would not extract;
  abstract obtained via [Lincoln Repository](https://eprints.lincoln.ac.uk/id/eprint/13365/)).
  **This is a directly usable result: an edge-preserving smoother moves a photograph's
  spatial statistics toward those of a painting, and a Gaussian blur does not.**
- Graham & Field found paintings and natural scenes share broad spatial-frequency
  regularities but have **significantly different mean amplitude-spectrum slopes**, and
  that paintings' intensity distributions show **lower skewness and sparseness** than
  natural scenes — but that once a **compressive nonlinearity** was applied to the images,
  paintings became *more* sparse than natural scenes, which the authors read as evidence
  that **artists achieve some degree of nonlinear compression** `[relayed]`
  ([Graham & Field, "Statistical regularities of art images and natural scenes: spectra,
  sparseness and nonlinearities," *Spatial Vision* 21:149–164,
  2007](https://pubmed.ncbi.nlm.nih.gov/18073056/); PDF at
  [people.hws.edu](https://people.hws.edu/graham/Graham-Spatial_Vision07.pdf) would not
  extract). Secondary sources give the mean amplitude-spectrum slopes as **−1.23 for art
  images and −1.40 for natural scenes** `[relayed — digits unconfirmed at primary source,
  see §9]`.
- Art, cartoons, comics and mangas all show power-spectrum slopes clustering near **−2.0**
  (monochrome graphic art ≈ −2.0, cartoons −2.09, comics −1.93, mangas −1.85), the same
  neighbourhood as natural-scene photographs — but **art is distinguished from photographs
  by *lower slope anisotropy*, i.e. its Fourier spectra are more isotropic** `[verified]`
  ([Koch, Denzler & Redies, "1/f² Characteristics and Isotropy in the Fourier Power
  Spectra of Visual Art, Cartoons, Comics, Mangas, and Different Categories of
  Photographs," *PLOS ONE* 5(8):e12268,
  2010](https://journals.plos.org/plosone/article?id=10.1371%2Fjournal.pone.0012268);
  ~1,500 images, ~150 per category).

**The mechanism this points at, applied to PalettePhotoConverter.** The existing optional
Gaussian pre-blur is the *wrong* smoother for this purpose. It reduces detail uniformly
and softens edges — exactly the two things Mather's result says painters do *not* do. An
edge-preserving smoother (bilateral, guided, or anisotropic diffusion) with a
spatially-varying strength — weak at the focal region, strong at the periphery — is the
shape of the correct operator `[inferred, but each half of the inference is sourced]`.

### 1.3 The camera's colour response vs the painter's selection

Report 02 §6.3 covers the mechanics: in-camera picture styles, phone tone mapping and
skin-tone-aware colour boosts are non-linear, spatially varying and undocumented, and
**cannot be inverted**; without a reference chart the realistic error floor is **ΔE ≈ 2
in daylight, ΔE ≈ 4 under tungsten**. I will not repeat that. What belongs here is the
*aesthetic* consequence.

- **Painters decouple chroma from luminance; cameras do not.** In a direct comparison of
  108 still-life fruit paintings (Fogg Art Museum) against 41 colour-calibrated fruit
  photographs (McGill database), **the photographs showed a correlation of r = 0.68
  (p < 1e-6) between achromatic and chromatic contrast, while the paintings showed
  essentially none (r = 0.007, p = 0.48)** `[verified]`
  ([Nascimento et al. / "The color of fruits in photographs and still life paintings,"
  PMC11077907](https://pmc.ncbi.nlm.nih.gov/articles/PMC11077907/)). The same study found
  **relatively higher chromatic contrast for warm colours in paintings than in
  photographs**, and confirmed the effect was specific to warm fruit colours by checking
  that blue pixels showed no comparable elevation.
  This is the cleanest quantitative statement I found of "the painter chooses the colour;
  the camera records it." In a photograph, the brightest thing tends also to be the most
  saturated thing. In a painting it need not be.
- **Shadow colour is a positive choice, and it is physically real.** Outdoor shadows are
  lit by skylight rather than sun, and skylight CCT runs far above the ~5500 K of direct
  sun — measurements under shade give markedly higher CCT, and artificial-sky work uses
  >11,000 K to reproduce a deep blue `[relayed]`
  ([ResearchGate, CCT of skylight and daylight for different atmospheric
  conditions](https://www.researchgate.net/figure/Correlated-color-temperature-of-skylight-and-daylight-for-different-atmospheric_fig15_249969495)).
  A camera's auto white balance and its shadow-lifting curve both fight this: AWB pulls
  the global illuminant toward neutral, which *desaturates the warm-lit areas relative to
  the cool shadows or vice versa*, and shadow lift raises value while chroma noise gets
  crushed by chroma denoising. The result is shadows that are simultaneously too neutral
  and too noisy `[inferred]`.
- **Sensor noise reads as texture the palette will amplify.** Report 02 §6.1 already notes
  that per-pixel colour jitter causes adjacent pixels to snap to different paint mixtures
  and fragments regions. From the appeal side the point is stronger: photographic grain is
  *uniformly distributed high-frequency energy*, which is exactly the thing Mather's
  edge-preserving filters remove to make photograph statistics look like painting
  statistics. Noise is not neutral here; it actively pushes the image away from painting
  statistics `[inferred]`.
- **The saturation the camera gives you is not the saturation the paint can give you, and
  they fail in opposite directions.** Report 02 §0.4 established the paint gamut is an
  *overlap* with sRGB, not a subset — 22% of measured acrylics fall outside AdobeRGB in
  chroma, while the sRGB yellow corner is 13 L\* above Golden Cad Yellow Medium. So a
  literal match simultaneously *under*-saturates some hues and cannot reach the value of
  others. Matching exactly is not even available; the question is only what to do instead.

### 1.4 Edge uniformity and lack of hierarchy

The art-instruction literature is unanimous and specific here, and it is worth recording
even though it is not research:

- Edges are classified hard / soft / lost / found; a **lost edge** is one where the value
  and colour of an object match its background closely enough that the border disappears
  and the viewer's brain completes the shape `[relayed]`
  ([Draw Paint Academy, "Edges in Art"](https://drawpaintacademy.com/edges/);
  [Sight-Size, "Lost and Found"](https://www.sightsize.com/lost-and-found/)).
- **The sharpest edge in a painting becomes a focal point by default**; the working rule
  given is one or two sharp-edge areas with everything else progressively softer, and
  edges at the back of the picture plane softer than those at the front `[relayed]`
  ([Montcarta, "Edge Control: Creating Visual Hierarchy"](https://montcarta.com/blogs/art-guide-inspiration/edge-control-visual-hierarchy-hand-painted-art);
  [OutdoorPainter, "Edge Control"](https://www.outdoorpainter.com/edge-control-for-a-more-nuanced-painting-approach/);
  [TAAO / Skip Whitcomb](https://tucsonartacademyonline.com/blog/2025/11/5/controlling-your-edges)).

Rate this as **consistent instruction folklore with a strong independent research
anchor**: the DiPaola eye-tracking result (§1.2) reports that "the transition from sharp
to blurry edges, known as lost and found edges, directed viewers' eyes around the
portrait" `[relayed]`, and O'Shea et al. (§5) independently show low contrast reads as
farther away. The *direction* of the folklore is supported; none of the specific
prescriptions ("one or two sharp areas") is measured.

A photograph has one focal plane and everything in it is either in focus or out of focus
by an amount determined by optics, not by meaning. That is a hierarchy — just the wrong
one. A very deep depth-of-field photograph has no hierarchy at all `[inferred]`.

### 1.5 Failure modes — why paint-by-numbers, posterisation and naive filters look cheap

Failure modes are the most informative part of this section because they are the states
the current pipeline can actually fall into.

**Banding / false contour.** Quantisation into flat regions produces staircased bands,
and **the human visual system's edge enhancement amplifies them** — the Mach-band
mechanism (local response normalisation that equalises early channel gains) exaggerates
contrast at those boundaries, so a step that is numerically 1–2 units can be conspicuous
`[relayed]`
([Wikipedia, Mach bands](https://en.wikipedia.org/wiki/Mach_bands);
[Kingdom, "Mach bands explained by response normalization," PMC4219435](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4219435/);
[Wang et al., "BBAND Index," arXiv:2002.11891](https://arxiv.org/pdf/2002.11891)).
The diagnostic given in the banding literature is useful: **if a band appears where the
gradient is mathematically smooth it is perceptual (Mach); if the pixel values show
discrete steps it is quantisation** `[relayed]`.

This matters directly: PalettePhotoConverter snaps every pixel to the nearest achievable
mixture. In a smooth sky or a smoothly-lit cheek, adjacent pixels will alternate between
two mixtures along an iso-distance boundary, producing a hard contour where the photo had
none. That contour is then *amplified* by the visual system. This is the cheapest-looking
failure the app has `[inferred]`.

Report 02 §5.3 already argues against dithering as the fix and for region-based
segmentation. From the appeal side I agree, with one refinement: dithering trades a false
contour for uniform high-frequency noise, which §1.2 says is the *other* thing that makes
an image read as photographic rather than painted. Segmentation moves the boundary to
where a painter would have put a shape boundary anyway.

**Paint-by-numbers.** The failure is not the flat regions — many strong paintings are
flat. It is that the region boundaries are placed by a **colour-distance criterion**
rather than a **meaning criterion**, so a face and the wall behind it get the same
treatment as an eye and an iris, and every boundary is equally hard. A painter's flat
shapes are cut where an object or a value family ends `[inferred; the "meaningful
structure" framing is DeCarlo & Santella's, verified]`.

**Photo filters.** The commonly cited reason they look fake is that **algorithmic filters
apply uniform treatment across the image while human artists vary brushwork by context and
importance**, and that block-wise stroke prediction produces boundary inconsistency
artifacts `[relayed — this is weak sourcing, mostly blogs and one arXiv paper]`
([Hu et al., "Stroke-based Neural Painting and Stylization with Dynamically Predicted
Painting Region," arXiv:2309.03504](https://arxiv.org/pdf/2309.03504);
[Fstoppers, "5 Reasons Your Photos Look Fake"](https://fstoppers.com/post-production/5-reasons-your-photos-look-fake-and-how-fix-them-720150)).
I would not build anything on the blog sources; the arXiv observation about uniform
block-wise treatment is the same point as §1.2 arriving from a different direction.

**A useful contrast: Hertzmann's 1998 painterly renderer got this right 27 years ago.**
It paints in **coarse-to-fine layers**, and adds finer strokes **only where the current
canvas differs from a blurred reference beyond a threshold T** `[verified]`
([Hertzmann, "Painterly Rendering with Curved Brush Strokes of Multiple Sizes," SIGGRAPH
1998](https://mrl.cs.nyu.edu/publications/painterly98/hertzmann-siggraph98.pdf)). Detail
concentrates where the coarse approximation fails — which is a *content-driven* detail
map obtained for free, with no eye tracker and no depth estimate. This is the single most
portable idea in the NPR literature for a per-pixel app. See §10.

---

## 2. What a painting is doing that a per-pixel matcher structurally cannot

Worth stating plainly before the composition sections, because it bounds what is worth
building.

`PalettePhotoConverter.Convert` is a pointwise map ℝ³ → palette. Every property discussed
below — value grouping, detail hierarchy, edge hierarchy, aerial perspective, focal point
— is a property of a *neighbourhood* or of the *whole image*. None of them can be
expressed as a per-pixel colour substitution. The Gaussian pre-blur is currently the only
spatial operator in the pipeline, and it is isotropic and global.

The implication is not "rewrite the converter." It is that **every style idea in this
report has to be implemented as a pre-pass that rewrites the source image before matching,
or as a post-pass over regions** `[inferred]`. That is architecturally cheap: a pre-pass
produces a modified `Bitmap` and the existing converter is untouched. The blur slider is
already precedent for this shape.

---

## 3. Value design: notan, the N-value plan, the thumbnail test

### 3.1 What the tradition actually claims

- **Notan** entered Western art education through Ernest Fenollosa at the Boston MFA and
  was popularised by Arthur Wesley Dow's *Composition: Understanding Line, Notan and
  Color* (1899), where it is one of three core elements alongside line and colour
  `[relayed]`
  ([Art in Context, "Notan"](https://artincontext.org/notan/);
  [Will Kemp Art School](https://willkempartschool.com/how-to-use-notan-design-to-create-compelling-compositions-in-your-paintings/)).
- The practice is 2-value and 3-value thumbnail studies made *before* the painting;
  the 3-value version adds a mid-value between the two extremes `[relayed]` (same sources;
  [Artists Network, "The VALUE of Notan"](https://www.artistsnetwork.com/art-mediums/pastel/the-value-of-notan/)).
- Report 02 §4.1 already collected the "values carry the image, hue is negotiable"
  quotations. I found nothing beyond them: **no controlled study, no measured optimum
  number of values, no dataset of notan studies with outcome ratings.** The claim "a
  painting should work as a thumbnail" appears to have **zero** direct empirical
  literature `[verified by absence — searched; see §9]`.

### 3.2 The perceptual mechanism, which *is* solid

The tradition has no numbers but it does have a mechanism, and the mechanism is well
established:

- Visual processing is **coarse-to-fine**: low spatial frequencies, carried by fast
  magnocellular pathways, deliver global shape and structure to cortex **before** high
  spatial frequencies arrive via parvocellular pathways `[relayed]`
  ([Kauffmann, Ramanoël & Peyrin, "The neural bases of spatial frequency processing during
  scene perception," *Front. Integr. Neurosci.* 8:37, 2014, PMC4019851](https://pmc.ncbi.nlm.nih.gov/articles/PMC4019851/);
  frameworks from Schyns & Oliva 1994 and Bar 2003).
- With very brief presentation (~30 ms) **categorisation of a hybrid image is dominated by
  the low-spatial-frequency component** `[relayed]` (same source).
- Complex natural scenes can be categorised in **under 150 ms** `[relayed]`
  ([Thorpe, Fize & Marlot 1996, as relayed in
  PMC4019851](https://pmc.ncbi.nlm.nih.gov/articles/PMC4019851/) and
  [PMC3184650](https://pmc.ncbi.nlm.nih.gov/articles/PMC3184650/)).

**Read together:** the viewer's first, fastest, involuntary pass over the image *is* a
low-pass, low-value-resolution pass. If the big light and dark masses are ambiguous, the
first pass returns nothing and the viewer has to work. That is a real justification for the
thumbnail test. It is *not* a justification for "exactly three values."

### 3.3 What is honestly buildable

A **notan preview** — downsample hard, posterise to 2/3/4 levels of L\*, show it beside
the output — is cheap, honest, and puts the judgement where it belongs, with the user. A
**value-plan score** that says "this composition has 5 value clusters, reduce to 3" would
be asserting a number nobody has measured. §10 and §11.

---

## 4. Focal point and visual hierarchy — which levers have measurable support

The painters' list of levers is: highest contrast, hardest edges, greatest chroma, most
detail, and convergence of lines. Audited against the saliency and eye-tracking
literature:

| Lever | Evidence status | Source |
|---|---|---|
| **Most detail** | **Strongest.** Directly measured with an eye tracker on paintings and on NPR renderings; also drives *appreciation*, not just gaze. | DiPaola et al. 2013; Santella & DeCarlo 2004 `[relayed]` |
| **Hardest edges** | Good. Same DiPaola study reports lost-and-found edges directing gaze; O'Shea et al. give the depth-cue half independently. | `[relayed]` |
| **Highest luminance contrast** | Good — it is a primary channel in every saliency model since Itti–Koch. | `[relayed]` |
| **Greatest chroma** | Good — colour is a primary channel in Itti–Koch, and Achanta's whole saliency measure is Lab distance from the image mean. | `[verified for Achanta]` |
| **Convergence of lines / leading lines** | **Weak.** Orientation is an Itti–Koch channel, but I found no study isolating "lines converging on a point attract fixation." One composition eye-tracking study reports composition does lead the eye but that fixation *order* varies. | `[relayed]` |

**Saliency models: what they are and how well they work.**

- The Itti–Koch architecture extracts intensity, colour and orientation into separate
  feature maps at multiple scales, combines them into a single saliency map, and predicts
  fixated locations **significantly better than chance** `[relayed]`
  ([Itti, Koch & Niebur, "A Model of Saliency-Based Visual Attention for Rapid Scene
  Analysis," IEEE PAMI 20(11):1254–1259, 1998](https://www.researchgate.net/publication/3192913_A_Model_of_Saliency-based_Visual_Attention_for_Rapid_Scene_Analysis);
  [Itti & Koch, *Vision Research* 40:1489–1506, 2000](http://wexler.free.fr/library/files/itti%20(2000)%20a%20saliency-based%20search%20mechanism%20for%20overt%20and%20covert%20shifts%20of%20visual%20attention.pdf)).
- **On paintings specifically**, eight models were benchmarked against an eye-tracking
  dataset of paintings from romanticism to fauvism. Best model **SAM-ResNet: CC 0.700,
  NSS 1.834, AUC-J 0.862.** Handcrafted models averaged **CC 0.422**; deep models averaged
  **CC 0.583**. The same SAM-ResNet gets **CC 0.78 on MIT300 natural scenes**, so there is
  a real **performance drop on paintings**, worst on Impressionist and Pointillist work.
  Fine-tuning on paintings recovered **+9.7% CC** `[verified]`
  ([Kong et al. / "Can we accurately predict where we look at paintings?" *PLOS ONE*,
  PMC7546463](https://pmc.ncbi.nlm.nih.gov/articles/PMC7546463/)).
- **The essential caveat: centre bias.** Fixations cluster near the image centre
  independently of content or task, and this is one of the strongest effects in scene
  viewing; **image-independent centre bias outperforms image salience** in accounting for
  attention during scene viewing `[relayed]`
  ([Hayes & Henderson, "Center bias outperforms image salience but not semantics in
  accounting for attention during scene viewing," *Atten. Percept. Psychophys.* 2020,
  PMC11149060](https://pmc.ncbi.nlm.nih.gov/articles/PMC11149060/);
  [Nuthmann et al., *Front. Hum. Neurosci.* 11:491, 2017, PMC5671469](https://pmc.ncbi.nlm.nih.gov/articles/PMC5671469/);
  Tatler 2007). Many published saliency models bake a centre prior in, which inflates
  their scores.

**What this means for the app.** A saliency map is a plausible input to a detail/contrast
allocation pass, but (a) a *cheap* saliency map is much worse than CC 0.70, (b) any
saliency map is heavily confounded with "near the middle," and (c) the app has a far more
reliable source of the same information: **the user.** A click-to-set-focal-point control
beats every automatic estimator listed above and costs almost nothing `[inferred]`.

---

## 5. Aerial / atmospheric perspective — the one physically grounded compositional lever

**The effect.** With distance: contrast between object and background falls, contrast of
markings *within* the object falls, chroma falls, and hue shifts toward the sky
`[relayed]`
([Wikipedia, Aerial perspective](https://en.wikipedia.org/wiki/Aerial_perspective);
[Britannica](https://www.britannica.com/art/aerial-perspective)).

**The physics.** Koschmieder's law: apparent contrast attenuates exponentially with
distance, `C(d) = C₀ · exp(−β·d)`, where β is the atmospheric extinction coefficient;
visual range is the distance at which a black object is just visible against the horizon,
conventionally at 2% contrast `[relayed]`
([Lee & Shang, "Visibility: How Applicable is the Century-Old Koschmieder Model?"
*J. Atmos. Sci.* 73(11), 2016](https://journals.ametsoc.org/view/journals/atsc/73/11/jas-d-16-0102.1.xml);
[UMass copy](https://cpb-us-w2.wpmucdn.com/blogs.umb.edu/dist/d/1690/files/2014/12/Lee_2016_Visibility-1quaaje.pdf)).
The model's assumptions — uniform background, black target, 2% contrast threshold, constant
airlight-to-extinction ratio — are explicitly first-approximation only.

**The perception.** Contrast works as a depth cue *on its own*, with no other depth
information present: subjects reported the lower-contrast area as farther, and the effect
persisted **even when size cues opposed it**. The authors' conclusion is that contrast acts
as a pictorial depth cue simulating aerial perspective `[relayed]`
([O'Shea, Blackburn & Ono, "Contrast as a depth cue," *Vision Research* 34(12):1595–1604,
1994](https://pubmed.ncbi.nlm.nih.gov/7941367/)).

**So the effect is real and the maths is simple. The hard part is depth.**

**Depth from a single photograph — realistic options, hardest to easiest:**

1. **Learned monocular depth (Depth Anything V2, MiDaS/DPT).** These give
   *affine-invariant relative* depth, not metric depth; metric models generalise worse
   because their training data requires camera parameters `[relayed]`
   ([Depth Anything V2 overview](https://www.digitalocean.com/community/tutorials/depth-anything-v2-a-powerful-monocular-depth-estimation-model);
   [MapAnything, arXiv:2509.14839](https://arxiv.org/pdf/2509.14839)). **Cost for this
   project: high.** A WinForms/.NET app would need ONNX Runtime plus a 25–350 MB model
   file, adding a large dependency and a first-run download for a feature that only helps
   landscapes. Relative depth is however exactly what aerial perspective needs — the
   absolute scale is absorbed into the β parameter `[inferred]`.
2. **Dark-channel transmission map.** He, Sun & Tang's dark channel prior estimates
   **haze thickness directly** from a single image, and the transmission map it produces
   *is* an aerial-perspective map: `t(x) = exp(−β·d(x))` `[relayed]`
   ([He, Sun & Tang, "Single Image Haze Removal Using Dark Channel Prior," CVPR 2009 /
   IEEE PAMI 33:2341–2353, 2011](https://projectsweb.cs.washington.edu/research/insects/CVPR2009/award/hazeremv_drkchnl.pdf)).
   **Cost: low — a min-filter over colour channels, an atmospheric-light estimate from the
   brightest dark-channel pixels, and a guided-filter refinement. Perhaps 150 lines of
   C#.** It fails on white/bright objects (snow, a white wall, a white shirt) where the
   dark-channel assumption breaks. But note the framing advantage: **the app does not want
   to remove haze, it wants to add it.** Running the estimator only to *modulate* the
   effect it already detects is a much more forgiving use than dehazing, because the
   errors are in the direction of "already correct" `[inferred]`.
3. **Value/vertical-position heuristics.** In a landscape, distance correlates with height
   in frame above the horizon line and with existing local contrast. Crude, but free, and a
   user-drawn horizon line would make it respectable `[inferred]`.
4. **Defocus estimation.** A depth-of-field blur map is a depth *ordering* cue in
   photographs with shallow DoF and gives nothing in a deep-DoF landscape — which is
   precisely the case where aerial perspective matters. Poor fit `[inferred]`.

**Honest verdict.** Aerial perspective is the best-justified geometric idea in this report
and also the one most likely to be over-built. The defensible increments are: a
**user-painted or slider-controlled depth ramp** (top-of-frame → far), and, if that proves
useful, a **dark-channel transmission map** as an automatic initial guess the user can
override. Do not start with a neural depth model.

---

## 6. Compositional structures — evidence audit

This is the section where separating research from folklore matters most.

### 6.1 Rule of thirds — weak, and possibly learned rather than perceptual

- Amirshahi, Hayn-Leichsenring, Denzler & Redies evaluated whether photographs and
  paintings actually place main subjects on the thirds lines. I could not open the paper
  (Brill returned 403; ResearchGate requires login), so the specific measures are unverified
  `[relayed]`
  ([Amirshahi et al., "Evaluating the Rule of Thirds in Photographs and Paintings,"
  *Art & Perception* 2(1-2):163–182, 2014](https://brill.com/view/journals/artp/2/1-2/article-p163_11.xml?language=en)).
- Reported correlation between rule-of-thirds adherence and beauty ratings: **ρ ≈ 0.17**
  — very weak `[relayed]`
  ([Frontiers, "Swipes and Saves: A Taxonomy of Factors Influencing Aesthetic Assessments
  and Perceived Beauty of Mobile Phone Photographs," *Front. Psychol.* 13:786977,
  2022](https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2022.786977/full)).
- Eye-tracking: **experts with a photography background rated thirds-compliant images as
  more interesting; novices were not sensitive to it** `[relayed]` (same source). If the
  effect requires training to perceive, it is a convention, not a perceptual law.

**Verdict: real but tiny, and plausibly learned. Not worth a feature.**

### 6.2 Golden ratio — a century of contradictory results, and no link to painting at all

Two separate claims must be kept apart:

- **(a) Do people prefer golden-section rectangles?** Contested. Fechner's original result
  (76% preferring the three rectangles closest to φ) has been attacked and defended for
  130 years; Green's 1995 review argued much of the criticism rests on erroneous beliefs
  about Fechner's procedures and concluded there are "real psychological effects associated
  with the golden section," while other work found φ emerging as the *mean* and *median*
  response but not the *mode*, and found large individual and cultural differences
  `[relayed]`
  ([Green, "All That Glitters: A Review of Psychological Research on the Aesthetics of the
  Golden Section," *Perception* 24:937–968, 1995](https://journals.sagepub.com/doi/10.1068/p240937);
  [De Bartolo et al., *PsyCh Journal*, 2022, PMC9787369](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC9787369/)).
- **(b) Did painters compose on φ, and does composing on φ help?** Markowsky's 1992 review
  found the standard claims — the Parthenon, Leonardo — unsupported: there is no evidence
  the ratio was known at the Parthenon's construction, the building is not rectangular so
  the measurer can select dimensions to taste, and the Leonardo claims rest on
  insignificant unfinished works with no measurements of the famous ones `[relayed]`
  ([Markowsky, "Misconceptions about the Golden Ratio," *College Mathematics Journal*
  23(1):2–19, 1992](https://www.goldennumber.net/golden-ratio-misconceptions-by-george-markowsky-reviewed/)
  — note this summary is hosted by a pro-φ site and is written as a rebuttal, so it is a
  hostile-witness source and the underlying points survive it;
  [University of Edinburgh EUSci, "Myth-busting the Golden Ratio"](https://eusci.org.uk/2020/07/29/myth-busting-the-golden-ratio/)).

**Verdict: (a) is a live but unstable finding about bare rectangles. (b) is folklore. The
leap from (a) to "put your focal point at 0.618 of the frame" is unsupported by anything I
found.**

### 6.3 Dynamic symmetry — no evidence located

Jay Hambidge's dynamic symmetry (root rectangles, diagonals, reciprocals) is taught widely
in atelier and illustration circles. **I searched and found no empirical study of it at
all — no eye tracking, no preference data, no corpus analysis** `[verified by absence, see
§9]`. It should be treated as an untested compositional grammar.

### 6.4 Leading lines — supported in direction, unmeasured in magnitude

One eye-tracking study of landscape art compared expected scanpaths built from the
artist's known intention against measured scanpaths and concluded **composition is
successful in leading the eye, although the order of fixations varies** `[relayed]`
([Locher / "The Role That Composition Plays in Determining How a Viewer Looks at Landscape
Art," *J. Eye Mov. Res.* 13(2), 2020](https://doi.org/10.16910/jemr.13.2.13) — the DOI
resolves to MDPI, which returned 403; claim taken from search summary). This is a
one-study, small-N result and should not carry weight.

### 6.5 Ramachandran & Hirstein's "eight laws" — treat as speculation

Widely cited in design writing as if it were established neuroscience. The authors
themselves say the laws were **"initially proposed in a playful spirit"** and that the
essay's purpose was to stimulate dialogue; the paper is a *proposal for an experimental
programme*, not a report of one `[relayed]`
([Ramachandran & Hirstein, "The Science of Art: A Neurological Theory of Aesthetic
Experience," *J. Consciousness Studies* 6(6-7):15–51,
1999](https://www.dgp.toronto.edu/~hertzman/courses/csc2521/fall_2007/ramachandran-science-art.pdf)).
Peak shift, isolation, contrast extraction and grouping are individually interesting but
none was validated in that paper.

---

## 7. Empirical aesthetics and image statistics — what they can and cannot tell you

### 7.1 Processing fluency

The dominant modern account: **aesthetic pleasure is a function of the perceiver's
processing dynamics — the more fluently an object is processed, the more positive the
aesthetic response.** Figural goodness, figure–ground contrast, symmetry, prototypicality,
repetition and priming are all reinterpreted as fluency manipulations `[relayed]`
([Reber, Schwarz & Winkielman, "Processing Fluency and Aesthetic Pleasure: Is Beauty in the
Perceiver's Processing Experience?" *Personality and Social Psychology Review*
8:364–382, 2004](https://pages.ucsd.edu/~pwinkiel/reber-schwarz-winkielman-beauty-PSPR-2004.pdf)).

This is the theoretical bridge from §3.2 to §1.2: **strong value grouping and a clear
detail hierarchy both increase fluency** — they make the image cheaper to parse. It is
also the theory that predicts a banded, contour-ridden output will be *disliked*, because
false contours are structure the visual system must resolve and then discard `[inferred]`.

### 7.2 Order vs complexity

Berlyne's inverted-U (moderate complexity preferred) is neither dead nor confirmed. A
review of 115 years of music-preference work found **50 of 57 studies (87.7%) compatible
with an overarching inverted-U model** `[relayed]`, and Van Geert & Wagemans' contribution
is the conceptual split between **order** (structure and organisation of information) and
**complexity** (quantity and variety of information), arguing the two must be manipulated
independently `[relayed]`
([Van Geert & Wagemans, "Order, Complexity, and Aesthetic Appreciation,"
*Psychology of Aesthetics, Creativity, and the Arts* 14:135–154,
2020](https://lirias.kuleuven.be/retrieve/d52cbec2-cf2f-4e4f-a87e-d58dcdecaf10);
[OCTA toolbox, *Behav. Res. Methods*, 2022](https://link.springer.com/article/10.3758/s13428-022-01900-w)).

For this app: converting to a limited palette *decreases complexity*. Whether it increases
or decreases *order* depends entirely on whether the resulting regions are coherent shapes
or quantisation confetti. That is the whole argument for segmentation, restated in the
aesthetics vocabulary `[inferred]`.

### 7.3 Statistical regularities — the numbers, and their ceiling

- **Fractal-dimension / self-similarity / entropy comparison across 1,629 traditional
  Western paintings, 288 "Bad Art" images, and 572 abstract works** `[verified]`
  ([Redies et al., "Statistical Image Properties in Large Subsets of Traditional Art, Bad
  Art, and Abstract Art," *Front. Neurosci.* 11:593,
  2017](https://www.frontiersin.org/journals/neuroscience/articles/10.3389/fnins.2017.00593/full)):

  | Property | Traditional | Bad Art | Abstract |
  |---|---|---|---|
  | Fractal dimension | 1.56 ± 0.13 | 1.47 ± 0.15 | 1.45 ± 0.22 |
  | Self-similarity | 0.72 ± 0.09 | 0.65 ± 0.13 | 0.65 ± 0.13 |
  | 1st-order edge-orientation entropy | 4.380 ± 0.214 | 4.371 ± 0.234 | 3.945 ± 0.722 |
  | 2nd-order edge-orientation entropy | 4.474 ± 0.100 | 4.408 ± 0.177 | 4.093 ± 0.672 |

  **The authors' own conclusion is that there is "considerable overlap between all six art
  categories" and the properties cannot definitively separate good from bad art.** Look at
  the means against the standard deviations: traditional vs Bad Art differ by ~0.6 SD on
  fractal dimension and are indistinguishable on 1st-order entropy.

- **The ceiling on statistics-based aesthetic prediction.** Spatial and chromatic image
  statistics predicted **only 6–15% of the variance** in beauty ratings across a large set
  of Western oil paintings spanning 11 art periods (JenAesthetics). Crucially, expressing
  the statistics *relative to real-world scene statistics* explained about the same
  variance as raw values, and **the importance of a statistic to perceived beauty was not
  related to how closely the art reproduced the real-world value** — which undercuts the
  "art is beautiful because it matches natural-scene statistics" hypothesis `[relayed]`
  ([Nature *Scientific Reports* 14, "The perceived beauty of art is not strongly calibrated
  to the statistical regularities of real-world scenes,"
  2024](https://www.nature.com/articles/s41598-024-69689-6) /
  [PMC11339329](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC11339329/) — Nature redirected
  to an auth wall; figures taken from the search summary and the PMC listing).

- **Redies' "universal model"** proposes that art induces a resonant state in a visual
  system adapted to natural-scene statistics, predicting that aesthetic perception depends
  on **form rather than content**, on **higher-order statistics**, and is **non-intuitive
  to introspection** `[relayed]`
  ([Redies, "A universal model of esthetic perception based on the sensory coding of
  natural stimuli," *Spatial Vision* 21:97–117, 2007](https://pubmed.ncbi.nlm.nih.gov/18073053/)).
  Note this sits in direct tension with the 2024 *Sci Rep* result above. Report the
  disagreement; do not pick a side.

- **A ready-made implementation exists.** The Aesthetics Toolbox (Redies, Bartho, Koßmann,
  Spehar, Hübner, Wagemans, Hayn-Leichsenring) consolidates the measures used by four
  research groups into a **Python package with a web interface, CC BY 4.0**, computing
  lightness and colour statistics, Fourier spectral properties, fractality,
  self-similarity, symmetry, entropy measures and CNN-based variances — explicitly
  motivated by the fact that "results are difficult to compare between research groups"
  because everyone writes their own scripts `[verified]`
  ([arXiv:2408.10616](https://arxiv.org/abs/2408.10616);
  [Behav. Res. Methods, 2025](https://link.springer.com/article/10.3758/s13428-025-02632-3);
  [GitHub RBartho/Aesthetics-Toolbox](https://github.com/RBartho/Aesthetics-Toolbox)).
  Useful as a **reference implementation to port a measure from**, not as a scorer to run.

- **Pollock and fractals — the cautionary tale.** Taylor's claim that Pollock's drip
  paintings are fractal, and his use of fractal criteria to authenticate them, was rebutted
  by Jones-Smith, Mathur & Krauss: Pollock's works lack the range of scales box-counting
  needs (smallest marks only ~1000× smaller than the canvas), and **Jones-Smith produced
  "Untitled 5" in Photoshop in minutes, a field of stars that passes Taylor's published
  authenticity criteria** `[relayed]`
  ([ScienceDaily summary](https://www.sciencedaily.com/releases/2006/12/061204123447.htm);
  [Science News, "Fractal or Fake?"](https://www.sciencenews.org/article/fractal-or-fake);
  [Micolich et al. reply, arXiv:0803.0530](https://arxiv.org/pdf/0803.0530);
  [Jones-Smith et al. comment, arXiv:0712.1652](https://arxiv.org/pdf/0712.1652)).
  **The lesson generalises: a statistic that correlates with good paintings across a corpus
  will not stop you from producing a bad image that scores well.**

---

## 8. Colour appearance — why the screen preview lies about the painting

The app's output is judged on an emissive display but describes an object that will exist
as a matte reflective surface under room light. Four well-established appearance effects
sit in that gap.

| Effect | Statement | Direction of the error for this app |
|---|---|---|
| **Hunt effect** | Colourfulness of a colour **increases with luminance**; described by R.W.G. Hunt in 1952 `[relayed]` ([Wikipedia](https://en.wikipedia.org/wiki/Hunt_effect_(color)); [IES definition](https://ies.org/definitions/hunt-effect/)) | A screen at 200–300 cd/m² is brighter than a painting under domestic light. The preview will look **more colourful** than the painting. |
| **Stevens effect** | Brightness/lightness **contrast increases with luminance** `[relayed]` (Fairchild, *Color Appearance Models*, [PDF of 2nd ed.](https://scis.uohyd.ac.in/~chakcs/cipclass/lecs/ColourAppearance.pdf)) | The preview will look **higher-contrast** than the painting. Compounds the Hunt error in the same direction. |
| **Helmholtz–Kohlrausch** | At constant luminance, **increasing chroma increases perceived brightness**; saturated stimuli look brighter than achromatic ones of equal luminance `[relayed]` ([Wikipedia](https://en.wikipedia.org/wiki/Helmholtz%E2%80%93Kohlrausch_effect); [Kim et al., *J. Inf. Disp.*, 2022](https://www.tandfonline.com/doi/full/10.1080/15980316.2022.2077849)) | Means **L\* understates the perceived lightness of chromatic paints**. A saturated cadmium red at L\*45 does not read as the same "value" as a neutral grey at L\*45. This directly affects any notan/value-plan feature. |
| **Simultaneous lightness contrast** | A mid-grey patch looks lighter on a dark surround than on a light one; the effect is **stronger with articulated surrounds** than plain ones of equal mean luminance, and is present at exposures as brief as 15 ms `[relayed]` ([Bressan & Actis-Grosso, *Perception*, PubMed 16700287](https://pubmed.ncbi.nlm.nih.gov/16700287/); [PMC7768315](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC7768315/)) | Per-pixel matching optimises each pixel in isolation. The perceived value of a region depends on its neighbours. Two regions matched to the same mixture can read as different values, and vice versa. |

**Two consequences worth acting on.**

1. **The compression should be slightly more aggressive than the pure numbers suggest.**
   Hunt + Stevens both say the painting will read as flatter and duller than the screen
   preview. Report 02's sigmoidal remap parameter `s` (the contrast slider) is the natural
   place to put a default nudge, and the UI should say why `[inferred]`.
2. **Any value-plan / notan feature should use an H–K-corrected lightness, not raw L\*.**
   Otherwise saturated paints will be filed into the wrong value group. Several
   H–K correction formulae exist (Nayatani, Fairchild–Pirrotta); I did not verify which
   performs best for surface colours — flagged in §9 `[inferred]`.

---

## 9. Computational aesthetic evaluation — be blunt

**What the state of the art achieves.** NIMA (Talebi & Milanfar, Google) predicts the
*distribution* of human ratings rather than a mean score, trained on AVA — ~250,000 images
from dpchallenge.com, ~200–210 ratings each on a 1–10 scale. Reported correlations against
the human mean: **LCC 0.518–0.645, SRCC 0.510–0.636** depending on backbone; the MobileNet
variant is around **LCC 0.626 / SRCC 0.609** `[relayed]`
([Talebi & Milanfar, "NIMA: Neural Image Assessment," arXiv:1709.05424](https://arxiv.org/abs/1709.05424);
[ar5iv HTML](https://ar5iv.labs.arxiv.org/html/1709.05424);
[Google Research blog](https://research.google/blog/introducing-nima-neural-image-assessment/);
replication numbers from [hcl14/AVA-and-TID2013-image-quality-assessment](https://github.com/hcl14/AVA-and-TID2013-image-quality-assessment)).

**Why that number is worse than it sounds.**

- SRCC ≈ 0.61 against a mean is **~37% of rank variance explained** — and that is against
  a *crowd average*, which is the easiest target. Predicting an individual is much harder.
- **The target itself is the wrong target.** AVA is amateur photo-contest voting. It
  rewards photographic conventions (shallow DoF, saturation, "punchy" processing) — a set
  of preferences substantially *opposed* to what makes a painting good. Optimising a
  palette conversion against a photo-aesthetics model would push it back toward the
  photograph `[inferred]`.
- **The published criticisms are structural, not incremental.** Mean-rating regression
  ignores rater disagreement and "may be disagreed with by a significant proportion of
  users when rating distributions are widely spread"; general aesthetic assessment "lacks
  clear scoring criteria"; single global scores are "extremely biased by the content of the
  images"; and practical testing turns up models preferring distorted designs, pure
  whitespace, and blander layouts `[relayed]`
  ([Modeling, Quantifying, and Predicting Subjectivity of Image Aesthetics,
  arXiv:2208.09666](https://arxiv.org/pdf/2208.09666);
  [AesBiasBench, arXiv:2509.11620](https://arxiv.org/abs/2509.11620);
  [Photo Critique Dataset, NeurIPS D&B 2022](https://proceedings.neurips.cc/paper_files/paper/2022/file/dcd18e50ebca0af89187c6e35dabb584-Paper-Datasets_and_Benchmarks.pdf)).
- Add the §7.3 evidence: hand-crafted statistics explain 6–15% of variance in painting
  beauty, and cannot separate the Museum of Bad Art from the National Gallery.

**Verdict: an automated "is this a good painting?" score is not available at any level of
effort this project could justify.** It is a tempting feature precisely because it would be
a single number in the toolbar. Don't. §11.

---

## 10. Measurable heuristics

Things that could actually be computed on an image inside this app, with an honest cost and
trust rating. Cost is in the units that matter here: dependencies and lines of C#, at
1–4 MP working resolution.

| # | Heuristic | What it computes | Cost | Trust | Notes |
|---|---|---|---|---|---|
| **1** | **Value histogram + notan preview** | Histogram of L\* over the image; posterised 2/3/4-level thumbnail at ~64 px on the long edge. | **Trivial.** One pass, reuse `ColorSpace`. | **High as a display, low as a score.** | The perceptual mechanism (§3.2) is solid; the "correct" number of clusters is not. Show it, don't grade it. Use an H–K-corrected lightness if §8 is implemented, else say raw L\* in the label. |
| **2** | **Achievable-range occupancy** | Fraction of output pixels sitting at the palette's L\* floor or ceiling, and the L\* histogram of the *output* vs the *input*. | **Trivial.** One pass over the converted bitmap. | **High.** Purely descriptive — it directly measures the §1.1 clipping failure. | The single most useful diagnostic in this list. "31% of your image landed on Bone Black" tells the user their palette or their tone curve is wrong, with no aesthetic claim attached. |
| **3** | **Chroma distribution (C\*ab histogram) and the luminance–chroma correlation** | Per-pixel C\*ab = √(a\*²+b\*²); its histogram, plus Pearson r between local luminance contrast and local chromatic contrast. | **Low.** Two passes. | **Medium-high.** The r statistic has one good source (§1.3: photos r=0.68, paintings r=0.007). | Reported on a *single* study of fruit still lifes, so do not treat r≈0 as a target. As a *diagnostic* — "your output's chroma tracks its value the way a photograph's does" — it is defensible. |
| **4** | **Edge-sharpness / gradient-magnitude map** | Sobel or Scharr magnitude on L\*, optionally at 2–3 scales; summarised as a histogram plus a spatial map. | **Low.** | **Medium.** Measures a real quantity; the interpretation ("hardest edge = focal point") is folklore with an eye-tracking anchor (§1.4). | Most valuable as the *input* to heuristic 6, not as a number shown to the user. |
| **5** | **Detail-density map, Hertzmann-style** | Blur the source at scale σ; compute per-pixel \|I − blur(I)\| in Lab; that residual *is* the detail map. Repeat coarse-to-fine. | **Low.** Reuses `GaussianBlur.cs`. No new dependency. | **High for the mechanism, medium for the mapping to "where the eye goes."** | The best cost/benefit item here. It is exactly the test Hertzmann 1998 uses to decide where finer strokes are needed `[verified]`, and it gives a content-driven detail map with no eye tracker, no depth model, and no ML. Feed it into the smoothing strength in heuristic 7. |
| **6** | **Cheap saliency estimate (Achanta frequency-tuned)** | Convert to CIELAB, low-pass with a small binomial kernel, take the Euclidean distance of each pixel from the image's mean Lab. | **Low — ~40 lines.** `[verified]` ([Achanta et al., CVPR 2009](https://www.cs.utoronto.ca/~strider/publications/SaliencyCVPR09.pdf)) | **Low-medium.** Hand-crafted saliency models average **CC 0.422** on paintings vs **0.700** for the best deep model (§4), and all of them are confounded with centre bias. | Worth having as a *default guess* for a focal region that the user then drags. Alternative: Hou & Zhang spectral residual (FFT of log-spectrum), similarly cheap. **Do not present its output as "where people will look."** |
| **7** | **Edge-preserving pre-smoothing with spatially varying strength** | Bilateral or guided filter, radius/σ_range modulated by (1 − detail-importance) from #5/#6. Replaces or supplements the current Gaussian blur. | **Medium.** A joint-bilateral in C# at 4 MP needs care; a guided filter or a bilateral grid is the practical route. | **High.** This is the operator Mather's result says reproduces painting spectral statistics `[relayed]`, and the mechanism DiPaola/Santella measured `[relayed]`. | **The highest-value single change in this report.** It also directly reduces the banding failure of §1.5 by flattening regions before quantisation. |
| **8** | **Fourier amplitude-spectrum slope** | Radially averaged log-log amplitude spectrum, least-squares slope. | **Low-medium.** One FFT. | **Low as a target, medium as a diagnostic.** | Reference values: art ≈ −1.23, natural scenes ≈ −1.40 for *amplitude*; ≈ −2.0 for *power* (§1.2). **These are population means with wide spread — hitting −1.23 does not make an image good** (§7.3). Useful only to show that a smoothing setting moved the image in the painting-ward direction. |
| **9** | **Dark-channel transmission map** | Min over a local patch of min over colour channels; estimate atmospheric light from the top ~0.1% of dark-channel pixels; refine with a guided filter. | **Medium — ~150 lines, no dependency.** | **Medium.** Fails on white/bright subjects; but because the app wants to *add* haze rather than remove it, errors are forgiving (§5). | The only automatic depth-ish signal worth building here. Always let the user override with a manual depth ramp. |
| **10** | **Region count / mean region area after segmentation** | Connected components on the quantised output, with a minimum-area threshold. | **Low**, once segmentation from 02 §5.4 exists. | **High as a paintability metric.** | Not an aesthetic score — a *feasibility* score. "Your image has 4,200 regions under 20 px" tells the user this cannot be hand-painted, which is a claim the app can honestly make. |

**Suggested build order** (highest confidence-to-cost first): 2 → 1 → 5 → 7 → 10 → 3 → 9.
Items 4, 6 and 8 are inputs or diagnostics rather than features.

**One structural note.** Every item above is a *pre-pass* or a *post-pass* around
`Convert`. None requires changing the per-pixel matcher, which is the part of the app that
is already correct and expensive to get right.

---

## 11. What not to build

Ordered by how tempting they are.

**1. An automated "painting quality" score, of any kind.**
This is the most seductive idea in the whole feature and the worst-supported. Learned
aesthetic models reach SRCC ≈ 0.61 against crowd-mean *photo-contest* votes (§9) — a target
whose preferences run opposite to painting. Hand-crafted image statistics explain
**6–15%** of variance in painting beauty ratings and **cannot distinguish the Museum of Bad
Art from traditional Western oils** (§7.3). And the Pollock episode shows exactly how this
fails in practice: a Photoshop scribble made in minutes satisfied the published fractal
authenticity criteria (§7.3). Any number the app puts in the toolbar will be gamed by the
app's own output within a week of use. **Show diagnostics, never a verdict.**

**2. Golden-ratio and dynamic-symmetry overlays or auto-cropping.**
Preference for golden *rectangles* is a contested 130-year-old finding about bare shapes
(§6.2a). The claim that painters composed on φ is unsupported and the standard examples
(Parthenon, Leonardo) do not survive measurement (§6.2b). Dynamic symmetry has **no
empirical literature at all** that I could find (§6.3). Building a φ-grid overlay would put
a scientific-looking artifact on screen behind a claim that has no science.

**3. Rule-of-thirds enforcement or scoring.**
ρ ≈ 0.17 against beauty ratings, and the effect appears only in trained observers (§6.1).
If you want a compositional grid, ship it as a plain user-toggled overlay with no scoring
and no automatic cropping.

**4. Automatic focal-point detection as a load-bearing feature.**
A cheap saliency map averages **CC 0.422** on paintings; the best deep model reaches 0.700
and needs a neural network; and **centre bias — which is image-independent — outperforms
image salience** at explaining fixations (§4). The user knows what the painting is about
and can click it. Use saliency as a *pre-filled default* for that click, nothing more.

**5. Neural monocular depth (Depth Anything / MiDaS) as a first step for aerial
perspective.**
The models are good and the effect is real (§5). But this means ONNX Runtime plus a
model file of tens to hundreds of megabytes shipped with a WinForms app, for a feature that
only helps landscape subjects, in service of an effect a user can approximate with a
two-handle gradient. Build the manual ramp, then the dark-channel estimator, and only
consider a model if both prove insufficient.

**6. A "make it look like [artist]" style-transfer mode.**
Out of this report's scope to evaluate properly, but note the constraint that kills it
here: neural style transfer produces colours from a continuous space, and this app's entire
purpose is that every output colour must be reachable by mixing a specific set of tubes. A
style-transfer output would have to be re-quantised into the palette afterwards, which
discards most of what style transfer contributed and reintroduces every failure in §1.5.

**7. Uniform Gaussian pre-blur as the primary "make it painterly" control.**
The existing slider is fine as a noise/JPEG-artifact control and should stay. But it is the
wrong operator for style: Mather's result is specifically that band-pass filtering **could
not** reproduce the spectral-slope compression seen in artworks, while edge-preserving
smoothing **could** (§1.2). Isotropic blur softens exactly the edges a painting keeps
hardest. If the UI implies "more blur = more painterly," that is a false promise.

**8. Dithering to hide banding.**
Report 02 §5.3 already argues this. The appeal-side reason is additional: dithering
converts a false contour into uniformly distributed high-frequency noise, and uniform
high-frequency energy is a defining property of photographs, not paintings (§1.2). It also
cannot be executed by a human with a brush, which for this app is disqualifying on its own.

**9. Any fixed prescription about the number of values, the number of colours, or the
proportion of the canvas that should be dark.**
The 2/3/4-value plan and its relatives are teaching devices. I found no study establishing
an optimum for any of them (§3.1, verified by absence). Expose the count as a control;
do not assert a right answer.

**10. Matching the photograph's colour more accurately as a route to better output.**
Report 02 §6.3 establishes that an uncalibrated photo carries **ΔE 2–4** of unrecoverable
error from in-camera processing. This report adds the aesthetic half: even a perfect match
is the wrong goal, because a photograph's luminance–chroma coupling (r ≈ 0.68) is
precisely what paintings do not have (§1.3). Precision below the input's own error floor
buys nothing, and precision *toward the photograph* actively works against the stated
feature goal.

---

## 12. Unverified, could not confirm, and verification debts

Ranked by how load-bearing the claim is.

1. **Graham & Field's amplitude-spectrum slopes (art −1.23, natural scenes −1.40).**
   Obtained only from a secondary description in a search summary. The PDF at
   `people.hws.edu` and the PubMed record both failed to yield the digits. **Get the
   *Spatial Vision* 21:149–164 PDF before quoting these numbers anywhere.**
2. **Mather (2014) *Art & Perception* 2:11–22.** The core claim — edge-preserving filters
   reproduce the spectral-slope range compression of artworks, band-pass filtering does not
   — comes from the abstract via the Lincoln Repository listing and a search summary. The
   PDF at georgemather.com would not extract to text. **This claim is doing a lot of work
   in §1.2, §10.7 and §11.7. Get the paper.**
3. **DiPaola, Riebe & Enns (2013) *Perception*.** SAGE returned 403; both open PDFs failed
   to extract. No participant counts, no effect sizes, no statistics obtained — only the
   qualitative findings. **Second-most load-bearing claim in the report. Get the PDF.**
4. **Amirshahi et al. (2014), "Evaluating the Rule of Thirds."** Brill 403, ResearchGate
   login-walled. The ρ ≈ 0.17 figure and the expert/novice split come from a *different*
   paper's summary (Frontiers 2022) and a blog. Verify before citing either.
5. **The 2024 *Sci Rep* "6–15% of variance" figure.** Nature redirected to an auth wall.
   The number and the "relative to real-world statistics explains no more" conclusion come
   from a search summary. PMC11339329 should be openly accessible — check there.
6. **Locher, *J. Eye Mov. Res.* 13(2) (2020) on landscape composition.** DOI resolves to
   MDPI, which returned 403. Claim is from a search summary only; small-N, treat as weak.
7. **Which Helmholtz–Kohlrausch correction formula to use for surface colours** (Nayatani
   vs Fairchild–Pirrotta vs the CAM16 route). Not researched. Needed before any
   H–K-corrected notan feature.
8. **"Painting in High Dynamic Range" (*JVCIR* 18(5), 2007) — paywalled at ScienceDirect.**
   The "five-century history" framing is relayed from the article summary. Not load-bearing.
9. **Dynamic symmetry.** Searched, found nothing empirical. This is a *verified absence of
   evidence*, not evidence of absence — but after a deliberate search it is the right
   default.
10. **"A painting should work as a thumbnail."** Same status: no direct study located. The
    coarse-to-fine perceptual literature supports the *mechanism* but no one appears to
    have tested the *prescription*.
