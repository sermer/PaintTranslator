# Research: Painting Styles and Movements, Described Quantitatively

**Scope:** how the major painting styles could be expressed as parameters a program can apply — palette,
value curve, chroma scaling, hue bias, edge/detail treatment — for a "style selector" feature in
PaintTranslator.
**Date:** 2026-07-26. **Status:** research only, no code changed.
**Out of scope:** the physics of paint mixing. That is covered in `docs/research/` — read
`README.md`, `acrylic-blending-findings.md` and `source-reports/02-photo-to-paint-pipeline.md`
first. This report does not repeat Kubelka–Munk, tinting strength, gamut mapping or ΔE metrics.

## Confidence markers

The task brief specified `[verified] / [relayed] / [inferred]`. The existing source reports use
`[CITED] / [DERIVED] / [INFERRED]`. They map as follows and I use the brief's set throughout:

| Marker | Meaning |
|---|---|
| `[verified]` | I fetched the primary source and read the number or statement myself. Where I computed a figure from a source's raw data, I say so and state the method. |
| `[relayed]` | Reported by a secondary source, a search summary, or a primary source I could not open (paywalled). Treat as unconfirmed. |
| `[inferred]` | My reasoning or my best guess. Not in any source. |

**Every number in the "Style presets" table at the end is `[inferred]` unless explicitly tagged
otherwise.** I have tried hard not to dress up guesses as measurements.

---

## 0. Executive summary — the seven things that matter most

1. **Colour statistics alone separate styles only weakly.** The best published explainable
   classifier working purely from colour-histogram features gets ~78% on a 3-way task
   (Baroque / Impressionism / Post-Impressionism) on a curated 90-image set, and ~71% on a larger
   247-image set — barely above a random-forest baseline of 71.2%. `[verified]` (Costa et al. 2023).
   Graham & Field found "few low-level statistical differences among classes" of art.
   `[verified]` So do not expect a chroma multiplier and a tone curve alone to *read* as a style.
   The palette and the edge treatment are doing most of the work.

2. **The single strongest, most defensible lever this app already has is the palette.** Several
   movements are literally documented as specific pigment lists in museum technical reports, and
   the app is palette-driven. "Style = preset paint list + a few parameters" is a well-founded
   design. §2–§11 give the pigment lists with sources.

3. **Blurring makes a photo statistically *less* painting-like, not more.** Paintings have a
   *shallower* mean amplitude-spectrum slope than natural scenes (−1.21 ± 0.017 for art vs
   −1.40 ± 0.017 for natural scenes, n = 124 / 137). `[verified]` (Graham & Field 2007). Blur
   steepens the spectrum. The blur slider is a legitimate *simplification* control, but it is not
   what makes an image look painted. Edge-preserving flattening (bilateral / mean-shift /
   superpixel) moves in the right direction; Gaussian blur moves the wrong way.

4. **There is a real, measured value-distribution difference between the Western oil tradition and
   the flat Eastern tradition, and it maps straight onto two presets.** Graham & Field's
   Cornell museum sample: Western works mean pixel intensity 103.5 (SE 5.91), mode 90.5,
   variance 3251, skew **+0.428**; Eastern works mean 134.8 (SE 3.13), mode 137.5, variance 1721,
   skew **−0.314**. `[verified]` Converting the means to L\*: Western ≈ L\* 44, Eastern ≈ L\* 56
   `[derived — I ran the sRGB→L\* transform on their means]`. So: Old Master / academic = darker,
   ~1.9× the variance, positive skew (mass in the darks, tail into the lights). Flat/graphic =
   higher key, much narrower, negative skew. That is the chiaroscuro-vs-ukiyo-e distinction, measured.

5. **The order/disorder axis across 92 styles has been mapped and it is directly a
   "how much simplification" axis.** In the complexity–entropy plane over ~140,000 paintings,
   Impressionism, Pointillism, Fauvism and Neo-Romanticism sit at the highest entropy /
   lowest complexity corner; Minimalism, Hard Edge and Colour Field Painting sit at the lowest
   entropy / highest complexity corner; Ukiyo-e, Post-Impressionism, Realism and Cubism sit in the
   middle. `[verified]` (Sigaki, Perc & Ribeiro 2018, PNAS). The exact per-style coordinates are
   only given in a scatter plot, so treat the numbers I read off it as `[relayed]`.

6. **Impressionism is the style least suited to this app's architecture, despite being the most
   requested.** Broken colour means adjacent dabs of *different* paint reading as one colour at
   viewing distance. Minimum-ΔE nearest-colour matching does the exact opposite: it maps
   neighbouring similar pixels onto the *same* paint. You cannot get broken colour out of a
   pointwise colour map. It needs a dithering or stroke-placement stage. See §12.

7. **A naive chroma multiplier will backfire on this app specifically.** Scaling C\*ab before
   nearest-colour matching pushes pixels outside the achievable gamut, where the nearest-Lab
   search collapses them all onto the gamut hull — producing banding and hue drift rather than
   saturation. Median masstone chroma across the 80 paints in `Pigments/pigments.manifest.txt` is
   C\* = 33.6, and only 13 paints exceed C\* = 84, all of them yellow/orange/red
   `[derived — I computed C\*ab from the manifest's L\*a\*b\* column]`. A Fauvist ×2 boost is not
   reachable in the blues and greens at any lightness. Chroma boosting must be paired with the
   gamut mapping described in `source-reports/02-photo-to-paint-pipeline.md`.

---

## 1. What the quantitative literature actually establishes

Before the per-style sections, here is the small body of work that gives real numbers, and what
each one is good for.

### 1.1 Graham & Field 2007 — spectra, sparseness, dynamic range

*Statistical regularities of art images and natural scenes*, Spatial Vision 21(1–2).
https://people.hws.edu/graham/Graham-Spatial_Vision07.pdf `[verified — read the full PDF]`

- Corpus: 124 uncompressed TIFFs of paintings from the Herbert F. Johnson Museum (Cornell),
  12th century to contemporary, 42% Europe/America and 58% Middle East/Asia; 137 calibrated
  natural scenes (van Hateren).
- Mean amplitude-spectrum slope: **art −1.21 ± 0.017**, **natural scenes −1.40 ± 0.017**
  (standard error). Fit to the mean spectrum: −1.23 (R² 0.97) art, −1.37 (R² 0.98) natural.
  Significantly different means (p < 0.05).
- Extreme individual slopes in the art set: **−0.70** (Zhang, *Spring Festival on the River*) to
  **−1.56** (Giaquinto, *Birth of the Virgin*).
- Implied fractal dimension: art ≈ **2.8**, natural scenes ≈ **2.6**.
- Pixel-histogram kurtosis (sparseness): art ≈ **1.0**, natural scenes ≈ **31.3** — but the natural
  figure is driven by 12 images with kurtosis > 70 (sun through foliage). Median plots are much
  closer.
- The paper's own summary of the mechanism: "artists do not simply scale the intensity range down"
  but apply a **compressive nonlinearity**; after a log transform the paintings become *more*
  sparse than natural scenes, not less.
- Important caution the authors state directly about style: "Results suggest there are few
  low-level statistical differences among classes (unpublished data)."

**Use for this app:** grounds the "paintings have a compressed, S-curved value range" claim, and
warns that global blur is the wrong direction for painterliness.

### 1.2 Graham & Field 2008 — the two value distributions

*Variations in intensity statistics for representational and abstract art, and for art from the
Eastern and Western hemispheres*, Perception 37(9) 1341–52.
https://people.hws.edu/graham/Graham_perception08.pdf `[verified — read the full PDF]`

Table 1, pixel intensity statistics (0–255 greyscale, SE in parentheses):

| Group | n | Mean | Mode | Variance | Skewness | Pixel sparseness | Amp. spectrum slope |
|---|---|---|---|---|---|---|---|
| Eastern hemisphere | 72 | 134.8 (3.13) | 137.5 (5.67) | 1721.6 (99.8) | −0.314 (0.119) | 1.15 (0.373) | −1.19 (0.0143) |
| Western hemisphere | 68 | 103.5 (5.91) | 90.53 (10.70) | 3251.1 (260) | +0.428 (0.137) | 0.950 (0.347) | −1.27 (0.0237) |

Asterisked differences (mean, mode, variance, skew, slope) are all significant at p < 0.05;
sparseness is not.

Table 2, amplitude-spectrum slope by content (six-judge forced choice):

| Content | n | Slope |
|---|---|---|
| Landscape | 19 | −1.26 (0.0387) |
| Portrait / still-life | 26 | −1.25 (0.0300) |
| Abstract | 12 | −1.13 (0.0614) |

Abstract differs significantly from representational (p < 0.03). Basic intensity statistics
(mean, variance, skew, kurtosis) did **not** differ significantly by content.

Also relevant: four Mondrian *compositions* had a mean slope of −1.4 ± 0.06. `[verified]`

**Use for this app:** the Eastern/Western table is the single most directly usable value-curve
evidence in this report. Caveat it honestly — the "Eastern" 72 images are a mixed museum sample
(Rajput miniatures, Chinese painting, Japanese work), not a ukiyo-e corpus, and both samples are
small and biased.

### 1.3 Sigaki, Perc & Ribeiro 2018 — the order/disorder map of 92 styles

*History of art paintings through the lens of entropy and complexity*, PNAS 115(37) E8585–E8594.
Preprint: https://arxiv.org/pdf/1809.05760 `[verified — read the preprint PDF]`
(The PNAS page itself returned 403 to me.)

- ~140,000 paintings, permutation entropy *H* and statistical complexity *C* computed from local
  ordinal patterns. Figure 2 plots the 92 styles with >100 images.
- Qualitative finding stated in the text: "styles displaying the smallest values of *C* and the
  highest values of *H* (such as **Impressionism, Pointillism, and Fauvism**) are characterized by
  the use of smudged and diffuse brushstrokes, and also by blending colors in order to avoid the
  creation of sharp edges." And: "Among the styles displaying the highest values of *C* and the
  smallest values of *H*, we find **Minimalism, Hard Edge Painting, and Color Field Painting**,
  which are all marked by the use of simple design elements that are well-delimited by abrupt
  transitions of colors." `[verified]`
- Reading approximate positions off Figure 2 (axes H ∈ [0.60, 1.00], C ∈ [0.05, 0.25])
  `[relayed — read from a scatter plot, not a table]`:
  Impressionism, Neo-Romanticism ≈ H 0.90–0.93, C ≈ 0.05; Pointillism, Fauvism ≈ H 0.94–0.96,
  C ≈ 0.055–0.06; Ukiyo-e, Tachisme ≈ H 0.88–0.90, C ≈ 0.06; Post-Impressionism, Realism, Cubism
  ≈ H 0.95, C ≈ 0.065–0.07; Expressionism, Surrealism ≈ H 0.90, C ≈ 0.07; Colour Field
  ≈ H 0.75, C ≈ 0.20; Hard Edge ≈ H 0.70, C ≈ 0.24; Minimalism ≈ H 0.62, C ≈ 0.20.
- Note the axis is a **texture/edge** axis, not a colour axis. It maps almost exactly onto
  "how much should this preset simplify and flatten".

### 1.4 Kim, Son & Jeong 2014 — colour variety and brightness roughness

*Large-Scale Quantitative Analysis of Painting Arts*, Scientific Reports 4:7370.
https://www.nature.com/articles/srep07370 (redirects to an auth wall for me)
`[relayed via https://pmc.ncbi.nlm.nih.gov/articles/PMC4263068 — I read the PMC rendering]`

- 8,798 paintings from the Web Gallery of Art, 11th century to mid-19th century; >94% larger than
  700×700 px.
- Three measures: rank-ordered colour-usage distribution; **box-counting fractal dimension in RGB
  space** (colour variety, max 3); **roughness exponent** of brightness (two-point height-difference
  correlation).
- Medieval ≈ 2.4 box dimension, "significantly low colour variety", comparable to Pollock drip
  paintings ≈ 2.35. Early/High Renaissance through Realism ≈ 2.6–2.8.
- The roughness exponent rises steadily as chiaroscuro and sfumato develop.
- **The dataset stops at the mid-19th century, so it says nothing about Impressionism onward.**

### 1.5 Redies group — edge statistics by art category

*Statistical Image Properties in Large Subsets of Traditional Art, Bad Art, and Abstract Art*,
Frontiers in Neuroscience 11:593.
https://www.frontiersin.org/journals/neuroscience/articles/10.3389/fnins.2017.00593/full
`[verified — fetched the full text]`

| Category | n | Fractal dimension | Self-similarity | 1st-order edge-orientation entropy | 2nd-order |
|---|---|---|---|---|---|
| Western oil paintings | 1,629 | 1.56 ± 0.13 | 0.72 ± 0.09 | 4.380 ± 0.214 | 4.474 ± 0.100 |
| Western graphic art | 185 | 1.69 ± 0.11 | 0.76 ± 0.10 | 4.391 ± 0.189 | 4.489 ± 0.079 |
| Islamic book illustrations | 238 | 1.58 ± 0.10 | 0.79 ± 0.06 | 4.416 ± 0.180 | 4.506 ± 0.085 |
| Chinese colour paintings | 215 | 1.63 ± 0.15 | 0.71 ± 0.09 | 4.437 ± 0.122 | 4.519 ± 0.055 |
| "Bad Art" | 288 | 1.47 ± 0.15 | 0.65 ± 0.13 | 4.371 ± 0.234 | 4.408 ± 0.177 |
| Abstract art (20th c.) | 572 | 1.45 ± 0.22 | 0.65 ± 0.13 | 3.945 ± 0.722 | 4.093 ± 0.672 |

**Use for this app:** the collapse in edge-orientation entropy for abstract art (3.945 vs 4.380)
is the quantitative signature of *aligned, directional* mark-making — which is also what a
Van Gogh stroke field would produce. Traditional representational art is close to
orientation-isotropic.

### 1.6 Costa, Alonso-Moral, Falomir & Dellunde 2023 — colour traits that name styles

*An art painting style explainable classifier grounded on logical and commonsense reasoning*,
Soft Computing.
https://www.iiia.csic.es/media/filer_public/8e/af/8eafb521-1676-46db-bc0c-5d01dd3beb3e/s00500-023-08258-x.pdf
`[verified — read the full PDF]`

This is the most directly useful paper for turning art-historical language into features, because
it defines them operationally over an HSL colour-name histogram (37 qualitative colours: 5 greys
plus 8 hues × 4 variants).

- **Baroque** features: `darkness_level` (total frequency of dark\_ colours), `no_paleness_level`,
  `contrast_level` (frequency of dark **and** pale colours), `red_colors` share.
- **Impressionism** features: `bluish_level`, `greyish_level`, `diversity_of_hues` (hues present /
  11), `diversity_of_qcds` (distinct qualitative colours / 37).
- **Post-Impressionism** features: `vividness_level` (frequency of *pure*-hue colours),
  `warm_colors_level`, `contrast_blue_yellow_level`.
- Observed feature ranges across their dataset: `darkness_level` ∈ [0.024, 0.978],
  `diversity_of_qcds` ∈ [0.162, 0.946], `greyish_level` ∈ [0.007, 0.982],
  `vividness_level` ∈ [0.0, 0.743]. Learned `warmth` thresholds 0.32 and 0.4. `[verified]`
- Accuracy: ANYXI mean 78.01% ± 6.63 on the 90-image QArt-Dataset (10-fold); on the larger
  247-image set, random forest 71.21%, ANYXI-1-RPL 70.58%, FURIA 67.05%, GUAJE 66.18%,
  J48 61.99%. `[verified]`
- Their art-expert definitions, worth quoting because they are the operational ones:
  Impressionists "produced grays and dark tones by mixing complementary colors. Rather than
  neutral white, grays, and blacks, Impressionists painters often rendered shadows and highlights
  in color… shadows are boldly painted with the blue and the grey of the sky as it is reflected
  onto surfaces". Post-Impressionists were "influenced by color contrast, specially red vs. green
  and blue vs. yellow". Baroque "exaggerated lighting, created by contrasting dark colors to
  light-pale colors" and is "characterized by including ferroxide-based yellows, oranges and reds".
  `[verified]` (their citations: Dewhurst 1908; Berson 1996; Mamassian 2008; Rzepińska 1986;
  Hill 1980; Grygar 2003.)

The precursor paper with the average brightness / hue / saturation / lightness / contrast figures
per style is Falomir, Museros, Sanz & González-Abril, *QArt-Learn*, Expert Systems with
Applications 97:83–94 (2018), https://doi.org/10.1016/j.eswa.2017.11.056 — **paywalled, I could
not read it.** `[relayed]` If you want per-style mean HSL numbers, that is the paper to buy.

### 1.7 Two off-the-shelf metrics worth adopting as UI units

- **Hasler & Süsstrunk colourfulness M3** — a linear combination of the mean and standard
  deviation of the opponent channels; correlates >90% with subjective ratings.
  Anchors from their psychophysical scale: not colourful 0, slightly 15, moderately 33, averagely
  45, quite 59, highly 82, extremely 109.
  https://infoscience.epfl.ch/bitstreams/77f5adab-e825-4995-92db-c9ff4cd8bf5a/download
  `[relayed — I found the numbers in search summaries and a secondary implementation, not by
  reading the PDF; verify before shipping them as UI labels.]`
  Useful as the *unit* for a chroma slider: "target colourfulness 45" is more meaningful than
  "chroma × 1.3".
- **Reinhard et al. 2001 colour transfer** — match the mean and standard deviation of each channel
  (originally lαβ, commonly done in CIELAB) between a source and a reference.
  https://www.cs.tau.ac.il/~turkel/imagepapers/ColorTransfer.pdf `[relayed]`
  This is the standard, cheap way to make "apply a style's colour statistics" concrete: store six
  numbers per style (mean and SD of L\*, a\*, b\* over a corpus of that movement's paintings) and
  affine-map the photo onto them before paint matching. **This is probably the highest
  value-for-effort implementation route for a style selector, and it sidesteps having to invent
  value curves by hand** — you measure them instead.

### 1.8 Bellander 2015 — the long-run hue drift

94,526 dated paintings 1800–2000, 100 random pixels each, from BBC Your Paintings / Google Art
Project / WikiArt. Headline: a reliable trend toward blue through the 20th century, at the expense
of orange. Confounded by varnish yellowing, fading, and photography.
https://blog.revolutionanalytics.com/2015/04/paintings-getting-blue.html and
https://www.washingtonpost.com/news/wonk/wp/2015/04/06/the-colors-of-94526-paintings-since-1800-charted/
`[relayed]` Interesting context; not directly actionable per-style.

---

## 2. Impressionism

### Palette `[verified / relayed]`

Monet c. 1880, as listed by the National Gallery's technical work and widely relayed: cadmium
yellow, vermilion, alizarin crimson, cobalt blue, French ultramarine, Prussian blue, emerald
green, viridian, green earth, raw sienna, burnt sienna, light red, red earth, flake white, zinc
white, ivory black. `[relayed — the National Gallery Technical Bulletin page
https://www.nationalgallery.org.uk/research/research-resources/technical-bulletin/monet-s-palette-in-the-twentieth-century
returned 403 to me; this list comes from search summaries of it and from Jackson's Art.]`

Viridian (PG18, hydrated chromium oxide, in use from the early 1820s) is repeatedly identified as
*the* key Impressionist green. `[relayed]`

Monet abandoned chrome yellows in favour of cadmiums in his later work. `[relayed]`

**The "banished black" claim is more nuanced than the folklore.** Monet avoided black *for
shadows* from around 1868 but kept it on the palette for other purposes. `[relayed]` For a preset,
excluding black entirely is the recognisable choice; including it makes the neutrals muddier and
less Impressionist.

### Value distribution

No corpus measurement specific to Impressionism exists that I could find. The Kim et al. dataset
stops at 1850; Graham & Field do not break out by movement. **Everything below is `[inferred]`.**

- High key: mass shifted up, so the histogram mode sits above mid-grey rather than below it.
  The Graham & Field "Eastern" profile (mean L\* ≈ 56, negative skew) is a closer analogue than
  the "Western" one.
- Narrow: no true black and no true white. A plausible output range is L\* ∈ [30, 92].
- Compression, not clipping — Graham & Field's finding that artists apply a compressive
  nonlinearity rather than a linear rescale argues for a gamma/S-curve rather than a hard clamp.

### Chroma and hue

- Shadows are chromatic, not neutral: "shadows are boldly painted with the blue and the grey of
  the sky as it is reflected onto surfaces". `[verified — Costa et al. 2023 quoting Dewhurst 1908
  and Berson 1996]`
- The complementary-of-sunlight argument (yellow sun ⇒ violet shadows) is the historical
  rationalisation, drawing on Chevreul's simultaneous contrast. `[relayed]`
- **There is a real physical basis and it gives you a defensible direction, though not a number I
  found measured on paintings:** open shade outdoors is lit by skylight, which has a far higher
  correlated colour temperature than direct sun (direct sunlight ≈ 4870–5500 K; overcast skylight
  ≈ 7000 K; clear blue sky far higher still). `[relayed]` So the shadow bias should be toward
  **blue with a slight violet lean** — negative Δb\*, small positive Δa\* — applied as a function of
  lightness. Any specific Δ value is `[inferred]`.
- Impressionism's distinguishing colour features in the classifier literature are `bluish_level`,
  `greyish_level` and **high hue diversity** — that last one is worth noting, because a small
  preset palette will *reduce* hue diversity, working against the style. `[verified]`

### Edge and detail

Highest-entropy / lowest-complexity corner of the PNAS map — "smudged and diffuse brushstrokes…
blending colors in order to avoid the creation of sharp edges". `[verified]` A modest blur is
directionally right for the *edge* character. It is wrong for the *surface* character, which is
the opposite of smooth.

### Verdict for this app

**Partially reachable, and the defining feature is not.** Palette, high key, chromatic shadows and
soft edges are all reachable. Broken colour is not — see §12.

---

## 3. Post-Impressionism

Three distinct sub-styles that deserve three presets, not one.

### 3a. Cézanne — constructive planes

**Palette — this one is properly documented.** Butler / Philadelphia Museum of Art analysis of ten
Cézanne paintings 1877–1906, published by McCrone:
https://www.mccrone.com/wp-content/uploads/2015/06/Materials-and-Techniques-Cezanne.pdf
`[verified — read the full PDF]`

Pigments identified across the ten paintings: chalk, lead white, vermilion, red lead, red lake,
iron oxide yellow, chrome yellow, orpiment, Indian yellow, yellow lake, **emerald green**,
viridian, terre verte, ultramarine, cobalt blue, cobalt violet, charcoal black, bone black.
"Lead white, vermilion, emerald green, and ultramarine occur most frequently and in the most
significant proportions." Cobalt blue appears only in the last five paintings and is the only blue
in the two Mont Sainte-Victoire pictures. `[verified]`

Mixture behaviour, and this is unusually actionable: "**Generally five to seven pigments occur in a
given color.** The lighter, brighter pinks, roses, and yellows may have fewer pigments in a
mixture, and the mauves and browns have more… In all the paintings the greens contain simple
mixtures. Medium greens may be 100 percent emerald green; light greens result from lead white
added to emerald green and dark greens have a little viridian added to emerald green." Also:
"Nearly a third of the samples in the earlier paintings contain 1–5 percent of a carbon black
pigment," rising in the late paintings to 1%, 5% or 10%. `[verified]`

The secondary claim that Cézanne used "about thirteen pigments but averaged only five per picture"
is `[relayed]` and consistent with the above.

**Note the tension with this app.** The measured Cézanne palette mixes 5–7 pigments per colour;
`PalettePhotoConverter.Convert` samples singles, pairs and triples only. A Cézanne preset will
produce cleaner, higher-chroma mixtures than the real thing. Adding a small amount of black or
umber to the palette is the cheap way to recover the muted register.

Value/chroma: `[inferred]` — modest chroma, close to observed; wide but not extreme value range;
the style lives in the *plane structure*, which is spatial.

### 3b. Van Gogh — directional stroke fields

Palette: `[relayed]` — chrome and cadmium yellows, zinc yellow, emerald green, viridian,
ultramarine, cobalt blue, Prussian blue, red lake (fugitive; many originally-violet passages have
faded to blue), red lead, lead white. The Van Gogh Museum / npj Heritage Science paper
*Reconstructing Van Gogh's palette to determine the optical characteristics of his paints*
(https://www.nature.com/articles/s40494-018-0181-6) is the right primary source and is directly
relevant to this app because it is a **Kubelka–Munk reconstruction of his paints** — worth reading
alongside `source-reports/01-kubelka-munk-theory.md`. I hit an auth redirect and could not read it.
`[relayed]`

Stroke statistics: computational brushstroke extraction gives per-stroke orientation, length and
width, and Van Gogh's works show "uniformity in simple strokes, with a standard deviation of
length deviations significantly lower than non-Van Gogh paintings." `[relayed — from search
summaries of Li et al., *Rhythmic Brushstrokes Distinguish van Gogh from His Contemporaries*, and
the EURASIP JIVP 2014 paper https://doi.org/10.1186/1687-5281-2014-53, which redirected to an
auth wall for me.]` I did not obtain the actual length/width figures.

The Redies edge-orientation-entropy result (§1.5) is the closest thing to a measured signature for
directional stroke fields: aligned marks drive 1st-order entropy down from ~4.38 toward ~3.9.
`[inferred that this applies to Van Gogh specifically]`

### 3c. Gauguin — cloisonnism / synthetism

Palette: Prussian blue, lead white, chrome yellow (lead chromate), vermilion, cobalt blue,
ultramarine. `[relayed — from the npj Heritage Science study of *Portrait de Suzanne Bambridge*,
https://heritagesciencejournal.springeropen.com/articles/10.1186/s40494-018-0188-z, via search
summary]`

Cloisonnism is defined as "bold and flat forms separated by dark contours" — "dark outlines
enclosing areas of bright, flat colour, in the manner of stained glass or cloisonné enamel",
term coined by Édouard Dujardin in March 1888, developed by Émile Bernard and Louis Anquetin.
Note the art-historical correction: **Gauguin himself never adopted heavy outlines**; he took the
saturated flat juxtapositions without the black contour.
`[relayed — https://en.wikipedia.org/wiki/Cloisonnism and TheArtStory]`

For a preset this is the cleanest of the three: flat planes, high chroma, arbitrary/symbolic
colour, optional black key-line.

---

## 4. Fauvism

### Palette `[relayed]`

Matisse, from ColourLex's analysis of *Portrait of André Derain*: cobalt blue, cadmium orange,
chrome yellow, viridian, vermilion.
https://colourlex.com/project/henri-matisse-portrait-andre-derain/

From the npj Heritage Science study of *The Red Studio*
(https://www.nature.com/articles/s40494-022-00797-0): lead white, zinc white, bone black, madder
lake, carmine lake, vermilion, orpiment, viridian, cobalt blue, ultramarine, cobalt violet,
eosin red lake. His favourites are given as viridian, cobalt and violet.

Cadmium yellow degradation in *Le bonheur de vivre* (1905–06) is separately documented
(https://doi.org/10.1007/s00339-015-9239-4) — relevant only as a caution that photographs of
Fauvist works may not show original chroma. `[relayed]`

### The defining parameter

The brief's framing — "value relationships kept while chroma is exaggerated" — is exactly right
and is the one style whose definition is *natively pointwise*. Operationally:

```
L*_out = L*_in                      (identity — this is the constraint)
C*_out = k · C*_in ,  k ≈ 1.8–2.2   [inferred]
h_out  = h_in + Δh                  (optional deliberate displacement)
```

`[inferred]` on all three numbers. Note the folk claim that the Fauves "squeezed paint directly
from tubes onto canvas… creating maximum color saturation without mixing" `[relayed]` — that
argues for a preset that **restricts mixtures to 1–2 paints** rather than allowing the full triple
simplex, which is a lever `Convert` already has structurally.

### The gamut problem, stated concretely

The app's library tops out at C\* ≈ 106 (Hansa Yellow Opaque, at L\* 85.5) and C\* ≈ 105 (Diarylide
Yellow, L\* 75.4), but the most chromatic blue masstone is Cobalt Blue at C\* 70.7 / L\* 27.5 and
the most chromatic green is Perm Green Light at C\* 56.0 / L\* 45.3.
`[derived — computed from Pigments/pigments.manifest.txt]` A uniform ×2 chroma boost is achievable
in the yellow–orange–red arc and simply is not in the blue–green arc. Either apply a
hue-dependent multiplier, or accept that Fauvist output will skew warm — which, in fairness, real
Fauvism also does.

---

## 5. Expressionism

The weakest style for quantitative grounding. I found **no** technical pigment study of Kirchner
or Nolde specifically, only art-historical description. `[verified — searched, nothing found]`

What is documented: Die Brücke (founded Dresden 1905 by Schmidt-Rottluff, Bleyl, Heckel and
Kirchner; Nolde, Pechstein, Müller and others joined) "favored vivid palettes and simplified,
distorted forms"; Kirchner used "clashing, non-naturalistic palettes to generate visual tension";
Nolde "built entire compositions from hot, saturated pigment applied in thick, rapid strokes",
with "golden yellows and deep reds" recurring. `[relayed]`

In the PNAS map, Expressionism sits at moderate entropy and slightly higher complexity than
Impressionism `[relayed from Figure 2]` — i.e. harder-edged than Impressionism, softer than
Hard Edge.

**Parameters would be `[inferred]` throughout:** high chroma (below Fauvism), expanded value
contrast (S-curve, unlike Fauvism's identity), hue displaced toward a warm or acid register.

**The defining quality — *non-local* colour chosen for emotional register (a green face, a red
sky) — is not reachable by any pointwise map.** It requires knowing what the region *is*. See §12.

---

## 6. Tonalism / Whistler

### Palette `[relayed, but unusually specific]`

Whistler's *Sea and Rain* (1865, University of Michigan Museum of Art) uses **only four pigments:
cobalt blue, iron-oxide yellow, vermilion, and bone black** (plus white).
`[relayed — https://en.wikipedia.org/wiki/Sea_and_Rain; I could not locate the underlying
technical report.]`

This is close to an ideal preset: four paints plus white, all of them present or substitutable in
the app's library.

### Value and chroma

- "Tonalism refers to artworks with a restricted palette of colours, which exist within a narrow
  variation of dark tones"; the style is defined by "subtle gradations of the middle values".
  `[relayed — https://www.tonalism.com/what-is-tonalism and Jackson's Art]`
- "Some of those [Whistler Nocturne] paintings operate in a value range of maybe three steps."
  `[relayed — a painting-instruction blog, not a measurement. Treat as a vivid heuristic, not a
  number.]` On a 9-step Munsell-like value scale, three steps ≈ 30 L\* units of range. `[inferred]`
- "The brightest areas of the pale yellow sky are far from pure white… The darkest shadows also
  stop well short of pure black." `[relayed]`
- Peak-Tonalist hallmarks: "a narrow range of muted colors, diffused light and softened, indistinct
  forms, free and expressive paint handling, glazing, and composition inspired by Japanese
  woodblock prints." `[relayed]`

### Verdict for this app

**The most achievable of all the styles listed in the brief.** Every defining property — narrow
value range, low chroma, a dominant hue tinting everything, soft edges everywhere — is either a
pointwise transform or a global blur. There is no spatial or semantic component. If you want one
style preset that will unambiguously work, build this one first.

The dominant-hue tint is the one piece that needs a new parameter: interpolate every pixel's
(a\*, b\*) toward a chosen hue axis by a fixed fraction. `[inferred]`

---

## 7. Academic realism / Old Master

### Value distribution — the best-grounded style in this report

Graham & Field 2008's Western-hemisphere group (§1.2) is a reasonable proxy: mean intensity 103.5,
mode 90.5, variance 3251 (SD ≈ 57), skew **+0.428**. `[verified]` In L\*: mean ≈ 44, mode ≈ 38
`[derived]`. Positive skew in the intensity domain means **the bulk of the picture is dark, with a
thin tail into the highlights** — the classic chiaroscuro arrangement, and roughly 1.9× the
variance of the flat/Eastern tradition.

The Kim et al. roughness-exponent result (§1.4) — rising steadily as chiaroscuro and sfumato
develop — corroborates that this tradition maximises local brightness contrast. `[relayed]`

Baroque's operational colour signature in the classifier literature: high `darkness_level`, low
paleness, high `contrast_level` (**both** dark and pale colours present, which is the technical
statement of chiaroscuro), and a raised share of red/earth colours, "characterized by including
ferroxide-based yellows, oranges and reds". `[verified — Costa et al. 2023]`

### Palette

The relevant historical palettes are earth-dominated with small saturated accents. Two concrete
anchors:

- **The Zorn palette** — vermilion, ivory black, flake white, yellow ochre, commonly modernised to
  cadmium red / titanium white. Four pigments, "a wide enough gamut to handle most figure
  paintings", excellent for flesh, poor for landscape. Note that ivory black functions as the
  blue: it is a cool black that gives "the closest thing to blue… a cool gray".
  `[relayed — https://drawpaintacademy.com/zorn-palette/,
  https://www.jacksonsart.com/blog/2021/02/02/colour-mixing-exploring-the-zorn-palette/, and
  Gurney Journey. The attribution to Zorn himself is traditional, not documented.]`
- **Sorolla's palette** (alla prima but earth-based): zinc white, yellow ochre, Seville red earth,
  rose madder, ivory black, Cassel earth. `[relayed]`

### Chroma

`[inferred]`: reduce global chroma substantially (×0.6–0.75) but **preserve the top decile** — the
defining relationship is a low-chroma field with a few high-chroma accents, and a uniform
multiplier destroys exactly that. A soft-knee curve on C\* that compresses the middle and leaves
the top alone is the right shape.

### Verdict

**Reachable in histogram terms, not in lighting terms.** A tone curve can reproduce the
distribution. It cannot put the light where a chiaroscuro painter would — that is a relighting
problem, needing a subject/background separation. A photo of a flatly-lit scene run through an
Old Master preset will get a dark, contrasty histogram and still look like a dark flat photo.

---

## 8. Flat / graphic styles

This is the family the app's existing architecture is *already built for*. Nearest-colour matching
onto a small palette, with no dithering, is by construction a posteriser.

### Ukiyo-e

- **Value profile:** the Graham & Field "Eastern" group (mean L\* ≈ 56, variance 1721, skew
  −0.314) `[verified for the group; inferred that it generalises to ukiyo-e specifically —
  the 72 images are a mixed Asian museum sample]`. High key, narrow, mass in the lights with a
  tail into the darks. This is the *inverse* of the Old Master profile and it is the single most
  useful contrast in this report.
- **Colour count:** nishiki-e uses one carved block per colour. "The number of colors increased to
  7 or 8 when nishiki-e became established"; "as many as twenty blocks might be needed"; "a
  minimum of about ten impressions were necessary to print an average nishiki-e."
  `[relayed — https://en.wikipedia.org/wiki/Nishiki-e]`
  Hokusai's *Great Wave* used "at least seven sequential printings, probably from four woodblocks
  cut on both sides." `[relayed — https://www.metmuseum.org/essays/hokusai-great-wave]`
  **So 7–10 flat colours is the historically correct target, not 3–4.**
- **Pigments:** traditional palette of indigo, red safflower (beni) and orpiment, enriched in the
  19th century by Prussian blue; a sharp transition in the early 1830s away from indigo toward
  Prussian blue. The Met's spectroscopic analysis of its *Great Wave* impressions found the first
  pass used a **Prussian blue / indigo mixture** and the second pure Prussian blue.
  `[relayed — Met Scientific Research, via
  https://news.artnet.com/art-world/three-things-hokusai-great-wave-2367449 and
  https://www.nature.com/articles/s40494-020-00406-y]`
- **Key-line:** the black outline block (*sumi* keyblock) is structural to the style and is an
  edge-detection overlay, not a colour map.

### Screen print / poster / Alex Katz

- Warhol's screenprints build the image "in layers of color", one screen per colour. `[relayed]`
- Alex Katz screenprint colour counts, as published by galleries: *Tree 10* (2022) 10 colours,
  *Plaid Shirt 1* (1981) 16 colours, and one work at 38 colours. `[relayed]`
  So "flat" does not mean "few" — Katz uses many flat colours, which is good news: it means a
  10–20 paint palette with hard quantisation is stylistically correct.
- Katz's own description of the mechanism: silkscreen's "crisp articulation of forms through
  two-dimensional expanses of single colours" fed back into "planes of flat colour atop monochrome
  backgrounds" in his paintings. `[relayed]`

### Where this lands on the quantitative map

Abstract/flat work has a **shallower** amplitude-spectrum slope (−1.13 vs −1.26 for landscape)
`[verified]` and much **lower, more variable edge-orientation entropy** (3.945 ± 0.722 vs
4.380 ± 0.214) `[verified]` — few, long, aligned edges. Colour Field / Hard Edge / Minimalism sit
at the low-entropy, high-complexity extreme of the PNAS map. `[verified]`

### Verdict

**The most achievable family, and the one that plays to the app's existing strengths.** The one
thing missing is that a Gaussian pre-blur is the *wrong* pre-filter for it — see §12.

---

## 9. Alla prima / painterly realism (Sargent, Sorolla, Schmid)

### Palette `[relayed]`

Sargent's watercolour palette as commonly listed: alizarin carmine, brown pink, burnt sienna,
cadmium yellow pale, chrome yellow, cobalt blue, gamboge, lamp black, rose madder, ultramarine,
Vandyke brown, scarlet vermilion, deep vermilion, viridian, opaque white — with ultramarine and
Vandyke brown as signature colours and viridian as his favourite green. Sorolla's is far shorter:
zinc white, yellow ochre, Seville red earth, rose madder, ivory black, Cassel earth. `[relayed —
drawpaintacademy.com and michaelshaneneal.com; both are practitioner sources, not technical
analyses.]`

### The defining parameters

- **Value accuracy.** Unlike every other style here, this one asks for an *identity* value curve —
  the whole discipline is getting the values right. Sorolla's reported method: shadows kept at
  mid-grey or darker so there is headroom at the top for sunlight. `[relayed]`
- **Edge hierarchy.** The lost-and-found edge — sharp at the focal point, dissolving elsewhere —
  is the technical signature. This is a **spatially varying** blur.
- **Economy.** Fewer, larger, decisive marks.

### Verdict

**Mostly unreachable as specified.** Value accuracy is free (do nothing). Edge hierarchy requires
a saliency or focus map to modulate blur radius per-pixel; the app's single global `blurRadius`
cannot express it. Economy requires stroke synthesis. What you *can* ship under this name is
"accurate values, a Sargent/Sorolla palette, and a small uniform blur", which is a weaker thing
than the style actually is — be honest in the UI copy about that.

---

## 10. Pointillism / Divisionism

### Palette `[relayed]`

Seurat "abandoned the use of iron oxide yellow, burnt sienna and black while adding zinc yellow
and additional hues of chrome yellow, vermilion and red lake"; the palette includes cobalt blue,
vermilion, cadmium yellows and viridian, applied in multiple thin layers.
`[relayed — https://colourlex.com/project/georges-seurat-a-sunday-on-la-grande-jatte/ and
https://en.wikipedia.org/wiki/Divisionism]`

Conservation caution: the zinc yellow in *La Grande Jatte* has darkened badly — "yellow,
green–yellow, and orange brushstrokes have become ochre-like, olive–green, and reddish–brown".
`[relayed]` So reproductions systematically understate its original chroma. There is a published
digital rejuvenation simulation (Berns et al.) if you want the corrected colours.

### The theory, and why it matters here

Divisionists believed optical mixing "would produce more vibrant and pure colors than the
traditional process of mixing pigments", following Blanc, Chevreul and Delacroix. `[relayed]`
Physically this is **additive-ish averaging at the retina**, which is a different operation from
the subtractive Kubelka–Munk mixing the app models. A Pointillist preset is therefore not just a
palette change — it is a different mixing model at a different spatial scale.

In the PNAS map, Pointillism sits at the highest-entropy corner alongside Impressionism and
Fauvism. `[verified]`

### Verdict

**Not reachable with nearest-colour matching, but reachable with a one-line change of quantiser.**
Replace minimum-ΔE nearest-colour with **error-diffusion or blue-noise dithering onto the paint
gamut**, at a dot scale exposed as a slider, and you get genuine optical mixing — pure paints
placed adjacently that average to the target colour. This is the single highest-payoff extension
identified in this report, because it also unlocks Impressionist broken colour (§12).

---

## 11. Colour Field, and Bob Ross

### Colour Field

Palette/materials: Magna (an oil-miscible acrylic resin paint, Bocour) was "the only acrylic paint
Louis ever used and, from 1954, the only medium he used"; Rothko used thin layers over bare canvas
with oil-modified alkyd and acrylic resins alongside egg, glue and dammar. `[relayed]`

Position on the quantitative map: lowest-entropy / highest-complexity corner with Hard Edge and
Minimalism — "simple design elements that are well-delimited by abrupt transitions of colors."
`[verified]`

**Verdict: this is not a photo-conversion style at all.** Its defining qualities are scale,
composition, and the absence of depicted content. There is no meaningful sense in which a
photograph of a face becomes a Rothko by remapping its pixels. If you ship it, ship it as an
extreme posterisation preset (2–4 huge flat regions) and do not claim more.

### Bob Ross / wet-on-wet

**This is the best-quantified palette in the entire report, because the data is public and
machine-readable.**

Source: `bob_ross_paintings.csv` from https://github.com/jwilber/Bob_Ross_Paintings (data scraped
from twoinchbrush.com), 403 paintings across all seasons of *The Joy of Painting*.
**I downloaded the CSV and computed the following myself** `[verified — derived, method: parsed the
`colors` column of all 403 rows and counted]`:

| Colour | Paintings | % of 403 | Hex in dataset |
|---|---|---|---|
| Titanium White | 400 | 99% | #FFFFFF |
| Alizarin Crimson | 380 | 94% | #4E1500 |
| Van Dyke Brown | 371 | 92% | #221B15 |
| Cadmium Yellow | 346 | 86% | #FFEC00 |
| Yellow Ochre | 327 | 81% | #C79B00 |
| Phthalo Blue | 323 | 80% | #0C0040 |
| Bright Red | 321 | 80% | #DB0000 |
| Midnight Black | 317 | 79% | #000000 |
| Sap Green | 306 | 76% | #0A3410 |
| Indian Yellow | 293 | 73% | #FFB800 |
| Dark Sienna | 290 | 72% | #5F2E1F |
| Prussian Blue | 263 | 65% | #021E44 |
| Phthalo Green | 116 | 29% | #102E3C |
| Black Gesso | 114 | 28% | #000000 |
| Burnt Umber | 55 | 14% | #8A3324 |
| Liquid Clear | 51 | 13% | #FFFFFF |
| Liquid Black | 19 | 5% | #000000 |

Colours per painting: **mean 10.65, median 11, mode 12, range 1–15**.
`[verified — derived, from the `num_colors` column]`

So the correct Bob Ross preset is **twelve paints used together at once**, not a limited palette —
a genuinely useful correction to the intuition that a TV-teaching style would be minimal. Note
that the top twelve are used in 65–99% of paintings each; the tail (Phthalo Green, Burnt Umber,
the liquid mediums) is optional.

Caveat: the hex values in that dataset are marketing swatches, not measurements — use the app's
own measured spectra for the matching paints and treat the hexes only as identification.
`[inferred]`

**Verdict: palette is exactly reachable. The look is not, quite.** Wet-on-wet's signature is the
soft blended gradient (sky, water) plus the dry-brush/knife texture (foliage, mountains,
"happy little trees"). The gradients are approximable with blur; the texture is generative. A
palette preset plus a moderate blur will read as "Bob Ross palette" more than "Bob Ross painting".

---

## 12. What is *not* reachable by per-pixel colour mapping plus blur

This is the section worth reading before any implementation work. `PalettePhotoConverter.Convert`
is, mathematically, a **fixed lookup table** (determined by the palette's sampled gamut) applied
independently to each pixel, optionally preceded by a **linear shift-invariant filter**. That is a
narrow class of operations, and it excludes several styles' defining features outright.

### Reachable now, or with a pointwise pre-transform stage

| Capability | Why it works |
|---|---|
| Palette restriction | This *is* the app's core operation. |
| Global value curve (gamma, S-curve, range compression) | Pointwise on L\*. |
| Global chroma scaling and hue rotation | Pointwise on a\*b\*, subject to the gamut caveat in §0.7. |
| Lightness-dependent shadow hue bias | Pointwise: Δa\*, Δb\* as functions of L\*. |
| Dominant-hue tinting (Tonalism) | Pointwise: lerp a\*b\* toward a target axis. |
| Posterisation onto N flat colours | Already the default behaviour with a small palette. |
| Overall softness | The existing Gaussian. |

**Recommendation:** add one pointwise pre-transform stage between the blur and the paint matching,
parameterised by (value curve, chroma multiplier or curve, hue rotation, shadow-bias vector,
tint axis and strength). That single stage covers everything in the table above and is where most
style presets would live.

### Not reachable, in rough order of how much work each would need

| Missing capability | Styles that need it | What it actually requires |
|---|---|---|
| **Broken colour / optical mixing** | Impressionism, Pointillism, Divisionism | Replace nearest-colour with **error-diffusion or blue-noise dithering** onto the paint gamut, at a controllable dot scale. Moderate work, contained inside the quantiser. **Highest payoff per unit effort in this report.** |
| **Edge-preserving flattening** | Ukiyo-e, poster, Katz, Gauguin, Cézanne, gouache | Replace or supplement the Gaussian with a **bilateral filter, anisotropic diffusion, mean-shift, or SLIC superpixels**. Moderate work. Note that Gaussian blur actively works *against* these styles: it softens the edges you want to keep and fails to flatten the interiors you want flat. |
| **Outlines / key-lines** | Cloisonnism, ukiyo-e, graphic illustration | Edge detection (Canny / DoG / structure tensor) composited as a dark overlay after matching. Small work, big visual payoff, and it is what makes flat output read as "print" rather than "posterised photo". |
| **Spatially varying blur (edge hierarchy)** | Alla prima / Sargent, Sorolla | A per-pixel blur radius driven by a saliency, depth or focus map. The current API takes a single `int blurRadius`. Moderate work; the hard part is producing a sensible saliency map. |
| **Directional stroke fields** | Van Gogh, Expressionism, painterly realism | Structure-tensor field + synthetic stroke rendering. Large work — this is a renderer, not a filter. |
| **Chiaroscuro relighting** | Academic realism, Old Master | Genuinely re-lighting a scene, not redistributing its histogram. Needs subject/background separation at minimum. Large work, uncertain results. |
| **Non-local / symbolic colour** | Expressionism, Fauvism at its most extreme, Gauguin | Semantic segmentation — you must know a region is a *face* to make it green on purpose. Large work, requires a model. |
| **Composition and scale** | Colour Field, Minimalism | Not a filtering problem at all. Do not attempt. |

### One more architectural note

If you add a chroma multiplier, the gamut-hull collapse described in §0.7 will bite. The fix is
already researched: see the gamut-mapping section of
`source-reports/02-photo-to-paint-pipeline.md`. Do the chroma boost *and then gamut-map*, rather
than boosting and letting the nearest-Lab search clip.

---

## 13. Verification debts

Things this report leans on that I could not confirm at the primary source:

1. **QArt-Learn (Falomir et al. 2018)** — the per-style mean brightness / hue / saturation /
   lightness / contrast figures for Baroque, Impressionism and Post-Impressionism. Paywalled at
   ScienceDirect. This is the single most directly useful missing source.
2. **National Gallery Technical Bulletin, Monet's palette** — 403 to me. The Monet pigment list is
   relayed from secondary summaries.
3. **Van Gogh palette reconstruction (npj Heritage Science 2018)** — auth redirect. Directly
   relevant to this app's K-M model, not just to style.
4. **EURASIP JIVP 2014 van Gogh brushstroke paper** — auth redirect. The stroke length/width
   distributions would be needed for any stroke-synthesis work.
5. **Hasler & Süsstrunk colourfulness thresholds** (0 / 15 / 33 / 45 / 59 / 82 / 109) — I found
   these in search summaries and a secondary implementation, not in the PDF. Verify before using
   them as UI labels.
6. **Whistler *Sea and Rain* four-pigment analysis** — relayed from Wikipedia; the underlying
   technical report is not cited there.
7. **"Whistler Nocturnes operate in a value range of maybe three steps"** — a painting-instruction
   blog. No measurement exists behind it that I found.
8. **A claimed 2019 *Journal of Cultural Heritage* study of Rembrandt luminosity histograms as a
   forgery signature** surfaced in a search summary. **I could not corroborate it and I have
   deliberately not cited it.** Treat as probably spurious.
9. **PNAS Figure 2 per-style H and C coordinates** — read off a scatter plot. The SI Appendix
   Fig. S6 has all 92 styles; a table of exact values may exist there.

---

## Style presets

One row per style. **Everything is `[inferred]` unless the cell carries a marker.** Value curve is
expressed as an output L\* range plus a shape; chroma is a multiplier on C\*ab; shadow bias is a
Δ(a\*, b\*) applied with weight falling off as L\* rises; blur is on the existing slider's scale
(qualitative — the slider's units are not calibrated to anything in the literature).

Paint names are taken **verbatim from `Pigments/pigments.manifest.txt`** so a preset can be a
literal name list. Where a historical pigment has no match in the library I give the nearest stand-in
and mark it with `~`.

| Style | Suggested paint list (from the app's library) | Value curve (output L\*) | Chroma × | Shadow hue bias | Blur / simplification | Notes and grounding |
|---|---|---|---|---|---|---|
| **Impressionism** | Titanium White, Cadmium Yellow Light, Cad Yellow Medium, Yellow Ochre, C.P. Cadmium Red Light, Alizarin Crimson Hue, Cobalt Blue, Ultramarine Blue, Cerulean Blue Chromium, Viridian Green Hue, ~Perm Green Light (for emerald), Permanent Violet Dark. **No black.** | [30, 92], gentle S-curve, mass raised above mid | 1.25 | Δb\* −12, Δa\* +3, ramped in below L\* 45 | Low (1–2) | Palette `[relayed]` from Monet lists. "No black for shadows" `[relayed]`. Chromatic shadows `[verified]`. Blue-shadow *direction* physically grounded `[relayed]`; the magnitudes are guesses. **Broken colour is not reachable — see §12.** |
| **Post-Imp. / Cézanne** | Titanium White, Cad Yellow Medium, Yellow Ochre, Raw Sienna, C.P. Cadmium Red Light (~vermilion), Alizarin Crimson Hue, ~Perm Green Light (emerald), Viridian Green Hue, Terre Verte Hue, Ultramarine Blue, Cobalt Blue, Permanent Violet Dark, Bone Black | [15, 90], near-identity | 1.05 | none | Edge-preserving flatten, not Gaussian | Palette `[verified]` from the PMA/McCrone study of ten paintings. Add a little black — a third of his early samples carry 1–5% carbon black `[verified]`. |
| **Post-Imp. / Van Gogh** | Titanium White, Cad Yellow Medium, Cad Yellow Dark, Diarylide Yellow (~chrome), Yellow Ochre, C.P. Cadmium Red Light, Alizarin Crimson Hue, Viridian Green Hue, ~Perm Green Light, Ultramarine Blue, Cobalt Blue, Prussian Blue Hue, Red Oxide | [10, 95], mild S | 1.40 | Δb\* −8 below L\* 40 | None (0) | Palette `[relayed]`. **The style is the stroke field — not reachable. Ship this only as a colour preset and say so.** |
| **Post-Imp. / Gauguin (cloisonnist)** | Titanium White, Diarylide Yellow (~chrome yellow), Cad Yellow Medium, Pyrrole Orange, C.P. Cadmium Red Light (~vermilion), Quinacridone Magenta, Ultramarine Blue, Cobalt Blue, Prussian Blue Hue, Perm Green Light, Bone Black (key-line only) | [20, 88], flattened / stepped | 1.50 | none — colour is arbitrary here | Hard posterise, 8–12 regions, + outline pass | Palette `[relayed]`. Historical correction: **Gauguin himself avoided heavy outlines** `[relayed]` — make the outline a toggle. |
| **Fauvism** | Bismuth Vanadate Yellow, Hansa Yellow Opaque, Diarylide Yellow, C.P. Cadmium Orange, Pyrrole Orange, C.P. Cadmium Red Light, Pyrrole Red, Quinacridone Magenta, Dioxazine Purple, Cobalt Blue, Ultramarine Blue, Phthalo Green (Y.S.), Perm Green Light, Titanium White | **Identity — this is the defining constraint** | 1.8–2.2 (hue-dependent; see below) | none | Low (0–1); restrict to 1–2 paint mixtures | Palette `[relayed]` from Matisse technical studies. The value-preserving constraint is the brief's framing and is correct. **Gamut warning `[verified from the manifest]`: ×2 is achievable in yellow/orange/red (masstone C\* up to 106) and not in blue/green (best blue C\* 70.7, best green 56.0). Use a hue-dependent multiplier or accept a warm skew.** |
| **Expressionism** | Titanium White, Cad Yellow Medium, Indian Yellow Hue, Pyrrole Orange, Pyrrole Red, Quinacridone Magenta, Dioxazine Purple, Ultramarine Blue, Phthalo Blue (G.S.), Phthalo Green (B.S.), Bone Black | [5, 95], strong S-curve (expand contrast) | 1.60 | Δa\* +6 below L\* 35 (shadows pushed warm/red, not blue) | Low (0–1) | **No pigment study found for Kirchner or Nolde** `[verified — searched, nothing]`. All parameters are guesses. **Non-local colour, the actual defining feature, is unreachable.** |
| **Tonalism / Whistler** | Cobalt Blue, Yellow Ochre (~iron-oxide yellow), C.P. Cadmium Red Light (~vermilion), Bone Black, Titanium White | **[35, 70]** — ~35 L\* of total range, strong compression toward the middle | 0.35 | n/a — replaced by the global tint | High (5–8) | Palette is the *Sea and Rain* four plus white `[relayed]`. Needs one extra parameter: **lerp every pixel's (a\*, b\*) 40–60% toward a chosen dominant hue axis.** The most achievable style in this report — no spatial or semantic component at all. |
| **Academic realism / Old Master** | Titanium White, Titan Buff, Yellow Ochre, Raw Sienna, Burnt Sienna, Raw Umber, Burnt Umber, Red Oxide, C.P. Cadmium Red Light, Alizarin Crimson Hue, Terre Verte Hue, Ultramarine Blue, Bone Black | **Full [5, 97]; target mean L\* ≈ 44, mode L\* ≈ 38, positive intensity skew** | 0.70 global with a soft knee that **preserves the top decile** | slight Δa\* +3 (shadows warm/brown) | None (0) | Value targets `[verified/derived]` from Graham & Field 2008 Western group (mean 103.5, mode 90.5, var 3251, skew +0.428). Baroque colour signature — high darkness, high dark-and-pale contrast, iron-oxide reds/oranges/yellows — `[verified]` from Costa et al. **A curve gets the histogram right but cannot re-light the scene.** |
| **Old Master / Zorn sub-preset** | C.P. Cadmium Red Light (~vermilion), Bone Black, Titanium White, Yellow Ochre | as above | 0.70 | as above | None | Four paints `[relayed]`. Ivory black serves as the blue `[relayed]`. Portrait-suited, poor for landscape `[relayed]`. |
| **Ukiyo-e** | Prussian Blue Hue, Ultramarine Blue, Bone Black, Quin Red Light (~beni), C.P. Cadmium Red Light, Cad Yellow Medium (~orpiment), Yellow Ochre, Sap Green Hue, Titanium White | **[35, 90]; target mean L\* ≈ 56, negative intensity skew, ~half the variance of the Old Master preset** | 1.10 | none | **7–10 flat colours**, edge-preserving flatten + black key-line | Value profile `[verified]` for the Graham & Field Eastern group, `[inferred]` that it transfers to ukiyo-e. Colour count 7–10 `[relayed]` from nishiki-e practice; *Great Wave* ≥7 printings from 4 double-sided blocks `[relayed]`. Prussian-blue/indigo layering `[relayed]` from Met analysis. |
| **Poster / screen print** | 4–8 paints, user-chosen; default Titanium White, Bone Black, Pyrrole Red, Cad Yellow Medium, Phthalo Blue (G.S.), Perm Green Light | [10, 95], stepped | 1.20 | none | **4–8 flat colours**, hard quantise, optional key-line | Warhol layered one screen per colour `[relayed]`. |
| **Alex Katz / flat planes** | 10–20 paints spanning the library | [20, 92], stepped | 1.00 | none | **10–20 flat colours**, edge-preserving flatten, **no** outline | Katz screenprints run 10, 16 and 38 colours `[relayed]` — flat does *not* mean few. |
| **Gouache illustration** | Titanium White, Titan Buff, Cad Yellow Medium, Yellow Ochre, Pyrrole Orange, Quin Red Light, Quinacridone Magenta, Cobalt Blue, Cerulean Blue Chromium, Cobalt Teal, Perm Green Light, Raw Umber | [25, 92], compressed at both ends | 0.95 | none | Moderate flatten, 12–20 colours | Entirely `[inferred]`. Gouache's real signature is matte opacity and slightly chalky, lightened mixtures — Titan Buff and heavy white content approximate it. |
| **Alla prima (Sargent/Sorolla)** | Titanium White, Cad Yellow Medium, Yellow Ochre, Red Oxide (~Seville red earth), Quinacridone Red (~rose madder), C.P. Cadmium Red Light, Viridian Green Hue, Ultramarine Blue, Van Dyke Brown Hue, Burnt Umber (~Cassel earth), Bone Black | **Identity — value accuracy is the whole point** | 1.00 | none | **Needs spatially varying blur — not expressible today** | Palettes `[relayed]` from practitioner sources. Sorolla's shadows-at-mid-grey-or-darker rule `[relayed]`. **Ship with a caveat: the edge hierarchy is the style and the app cannot do it.** |
| **Pointillism / Divisionism** | Titanium White, Bismuth Vanadate Yellow (~zinc yellow), Cad Yellow Medium, C.P. Cadmium Orange, C.P. Cadmium Red Light (~vermilion), Quinacridone Red, Permanent Violet Dark, Cobalt Blue, Ultramarine Blue, Viridian Green Hue, Perm Green Light. **No black, no earths.** | [25, 92] | 1.30 | Δb\* −10 below L\* 45 | **Requires a dithering quantiser, not nearest-colour** | Palette `[relayed]` — Seurat dropped iron oxide yellow, burnt sienna and black, added zinc yellow `[relayed]`. **The one style whose defining feature is cheap to add: swap nearest-colour for error diffusion at a controllable dot scale.** |
| **Colour Field** | 2–5 paints, high chroma | [20, 90], flat | 1.30 | none | 2–4 regions | **Flagged: not a photo-conversion style.** Ship only as extreme posterisation, or not at all. |
| **Bob Ross / wet-on-wet** | Titanium White, Alizarin Crimson Hue, Van Dyke Brown Hue, Cad Yellow Medium, Yellow Ochre, Phthalo Blue (G.S.), Pyrrole Red (~Bright Red), Bone Black (~Midnight Black), Sap Green Hue, Indian Yellow Hue, Burnt Sienna (~Dark Sienna), Prussian Blue Hue — **twelve, all at once** | [8, 96], mild S; dark ground, high-key sky | 0.90 | Δa\* +4 below L\* 30 (warm brown darks) | Moderate (3–5) | **Palette and count `[verified — I computed them from the 403-painting dataset]`: the top twelve colours each appear in 65–99% of paintings; mean 10.65 colours per painting, median 11, mode 12. Not a limited palette.** The blended gradients are approximable; the knife texture is not. |

### Which presets are safe to build first

1. **Tonalism** — everything about it is a pointwise transform plus blur. No caveats needed.
2. **Ukiyo-e / poster / Katz** — the app is already a posteriser; these need only the palette and a
   better pre-filter. Add the key-line and they will read convincingly.
3. **Bob Ross** — the palette is exactly known and the look is forgiving.
4. **Fauvism** — pointwise by definition, provided the gamut problem is handled.
5. **Academic realism / Zorn** — the value targets are measured; be honest that it is a histogram
   match, not a relight.

### Which presets need real work before they are honest

- **Impressionism and Pointillism** need the dithering quantiser. Without it they are palette
  swaps wearing the wrong name.
- **Van Gogh** needs stroke synthesis. Without it, ship it as "Van Gogh palette", not "Van Gogh".
- **Alla prima** needs spatially varying blur.
- **Expressionism** needs semantic segmentation for its defining move, and has almost no
  quantitative grounding besides.
- **Colour Field** should probably not be shipped as a style at all.
