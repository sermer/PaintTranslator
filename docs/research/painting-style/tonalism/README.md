# Research: Tonalism

Research into Tonalism aimed at one question: **what should the "Tonalism" style actually do?**
As shipped it is `EdgePreservingFloor` at strength 2.0, `ToneAndChromaRemap` at contrast 0.55 /
key **+4.0** / chroma 0.45, `MotherColourTransform` at fraction 0.30, mark scale **1.2**,
`NearestQuantiser`, and **no post-map stage at all**.

Four parallel tracks, written by separate agents that did not see each other's work. This README
is the synthesis. The reports are long; read this first and go to them for detail.

| Report | Covers |
|---|---|
| [01-palette-and-colour.md](01-palette-and-colour.md) | A 65-work corpus measured against two controls, what "subdued" means when tested, the mother colour costed and its mechanism found, the key sign error, the hue-concentration lever. |
| [02-atmosphere.md](02-atmosphere.md) | Atmosphere decomposed and reduced to one axis, the aerial rejection narrowed, an `AtmosphericRamp` built and measured, finite-thickness Kubelka-Munk and the scumble gamut argument. |
| [03-edges.md](03-edges.md) | Edge behaviour on 15 canvases against 15 photographs, the movement's own treatise, the Gaussian question settled, the floor's real knob, the focal lever re-costed. |
| [04-line-and-structure.md](04-line-and-structure.md) | Line measured and rejected, value mass and the notan gap, the shipped row's paintability audit, the boundary ruling, the merge confirmed. |

## The headline: everything that makes Tonalism Tonalism is value, and the row spends its value range on softness it already had

**All four tracks reached this independently, from four different measurements.** It is the
first time in five rounds that the convergence has been about a *premise* rather than a defect.
`[verified — computed locally 2026-07-31, four times over four corpora]`

| Track | Measurement | Result |
|---|---|---|
| 01 colour | 34 Tonalist works vs 14 photographs and 10 Impressionist controls | mean C\*ab **16.79 vs 20.42** (t = −1.18, n.s.) and **vs 17.31 Impressionist (t = −0.21, zero)**. What separates: value spread (t = −4.05), value key (t = −3.15; vs Impressionism **−6.96**), local lightness contrast (t = −2.38), hue concentration (t = +2.62) |
| 03 edges | 15 canvases vs 15 photographs | raw hard-edge density **8.8× lower** — but rescale to a common 60 L\* range and the gap **collapses** (22.43% vs 23.69%). **Four-fifths of Tonalist "soft edges" is tonal compression** |
| 02 atmosphere | 7 canvases vs 9 photographs | the row over-compresses **~2×**: delivers L\* range 32.0 / mean C\* 5.3 against the canvases' 51.5 / 23.3 |
| 04 structure | 14 photographs, all five styles | the **notan gap** — light/dark mass separation — is 34.93 in the source, 34.35 under Realism, and **15.59 under Tonalism**, the weakest in the app |

The style's chroma knob targets the one statistic that does not separate the movement. Track 3
puts the diagnosis most sharply: **the row is soft only because it is flat, and inside its own
flattened range it bands harder than the photograph it started from** — range-normalised ΔE-20
density 11.52%, against 8.45% for the source photographs and **4.34% for the canvases**, a 2.7×
excess over target.

Track 1's framing of the same result: Tonalism is paying for softness twice, in contrast 0.55
and again in the mother colour, when the remap had already delivered it — Tonalism has the app's
lowest mean boundary ΔE by a factor of two.

## The second convergence: the mother colour, found four ways

The Post-Impressionism round predicted this defect from the code. Four tracks have now measured
it in four different places, and it is **larger than that round recorded**. `[verified]`

| Track | Where measured | Result |
|---|---|---|
| 01 | candidate set | min L\* **6.46 → 40.30**; **not one of 3,037 candidates below L\* 40** (was 1,587 of 3,007), for a mean chroma change of 36.13 → 34.58 |
| 03 | rendered output vs canvases | render L\*5 **44.4** against the canvases' 22.2 — **Tonalism's blacks are about 22 L\* too light** |
| 04 | rendered percentiles | p1 L\* **43.8**, never below 42.6 on any of 14 photographs, against six of seven canvases below p1 27 and *The Falling Rocket* at **0.2** |
| 02 | against the alternative | per unit of lightness lift it removes **7.5× less chroma** than a white scumble; costs 29% of the value range for 4% of chroma |

**Track 1 found the mechanism.** `MostNeutralPaintIndex` ranks by masstone chroma alone — Titanium
White measures C\* **0.6**, Bone Black **1.5** — and the tie-break toward L\* 50 fires only on an
exact chroma tie, which no real pair produces. Any palette containing white gets white.

**The fix is not a different single paint.** This library holds no neutral near L\* 50: white at
98.2 and black at 11.2. A mid-grey mother is a *mixture*, and `MixtureBuilder.BlendInto` takes one
paint index. Measured through the real builder, a premixed 40:60 white/black grey gives **33× more
chroma contraction per unit of lightness rise** than the shipped white.

**But Tonalism should delete the stage, not repair it.** Track 1 measured the *good* mother colour
on the retuned row and every statistic moved away from the corpus target. The stage was doing
contrast's and key's job badly; once they do it themselves it is subtraction. Track 4 adds that
removing it is slightly paintability-positive too (25.77% → 24.32%). **Repair
`MostNeutralPaintIndex` anyway, for Abstract, which is the other caller.**

**And there is a second floor under the dark end.** Track 1: the affine part of
`ToneAndChromaRemap` maps L\* 0 to `50(1 − contrast) + key` = **26.5** at Tonalism's settings.
Removing the mother colour alone leaves the rendered minimum at 27.78, which is that number.
Both floors have to move, and fixing one does not fix the other.

Track 4 states the consequence that has the longest reach: **Realism, which does nothing, lands
closer to a Whistler nocturne than Tonalism does.** On a moonlit-street photograph Realism
realises mean L\* 31.4 / p1 10.0 against Tonalism's 46.1 / 42.6.

## The third convergence: the empty post-map slot, for the fourth round running

**The published Tonalism paintability figure is a fixture artefact and it is 33× too small.**
`StyleBehaviourTests.EveryRegisteredStyleIsPaintable` records 0.7675% against a 0.9% ceiling — the
tightest in the app — measuring a **6 px** threshold on a 256² synthetic gradient. `[verified]`

| Track | Corpus | Sub-mark share, as shipped |
|---|---|---|
| 04 | 14 photographs, native size | **25.77%** |
| 01 | 8 photographs | **28.78%** |
| 03 | 15 photographs, 800-px short edge | **33.83%** |

Three corpora, three magnitudes, one direction, and all three an order of magnitude from 0.77%.
Track 3's cross-style table puts Tonalism second-worst in the app behind **Realism at 51.30%** —
the two rows with an empty slot 5 — and its Tonalism ÷ Realism ratios reproduce the
Post-Impressionism round's to within 0.009–0.013 across four statistics, validating the method
across sessions and corpora for the fourth round running.

**Tonalism asks for a 1.2× brush and does less than any other style to produce one.**

### The rewritten `SmallRegionMerge` works — the Post-Impressionism round's verification debt 1 is cleared

Track 4 measured the union-find rewrite sitting uncommitted in the working tree: **exactly
0.000000 in one pass on all 14 photographs, for Tonalism, Post-Impressionism and Abstract, and a
second pass leaves the buffer byte-identical.** Regions 97,389 → 1,488; median region area
1 → 67.5 px. `[verified]` **The Fauvism round's "single most valuable assertion" is now true on
photographs**, and should be written as a registry-wide test.

**One new defect, and it is in shipped code:** Fauvism's remaining 0.60–2.34% is **entirely
`ContourLines` re-fragmenting what the merge repaired**. Two merges leave exactly zero on the same
images. **The merge must run last.** Nobody found this before because until this working tree the
merge never reached zero for anything to re-break.

## Line: the clear negative the brief asked for

Track 4 measured thin dark structure on seven Tonalist canvases at **0.98–5.94%, mean 2.61%**,
against Gauguin's *Vision after the Sermon* at **8.97%** on the same detector — which
cross-validates the Post-Impressionism round's 11.2% from an independent implementation.
`ContourLines` on Tonalism paints **3.1–39.2%, mean 17.5%**: 6.7× the corpus and twice the
cloisonnist control. All three known defects reproduce; the band radius is **1 on all 14 images**.
Rendered, it fills a baobab crown with flat violet.

**Do not register `ContourLines` here.** It would also be byte-identical to Fauvism's, since all
three post-map stages declare zero parameters.

**What gives a Tonalist picture its structure instead is value mass** — and that is precisely
what the notan gap says this row is worst at.

Track 4's new rule, which no prior round named: **an area opening preserves length, not thinness.**
A 2 px shroud running 800 px survives at mark² 52 because its area is 1,600; a 2 px branch tip
20 px long does not. Masts, horizons, trunks, walls and cables come through; twigs, sparks and
distant fences do not. That is the criterion for every style with the merge registered.

## Boundary: one row, and the founding source already refused the split

Track 4 owns this and the evidence is unusually clean. The term was coined in **1972 by Wanda
Corn**, whose exhibition put **49 paintings and 46 photographs by 31 artists** in one room, with
Inness and Whistler both inside the founding definition. `[relayed]` There is no split to make
that the coining source did not already refuse, and Sigaki et al. does not name Tonalism —
reproducing the negative result the Post-Impressionism round found for that movement.

Track 1 rejects the Pictorialists as a calibration target separately: their chroma is a **toner,
not a palette** — one Steichen negative printed three ways measures C\*mean 0.1 / 5.1 / 7.1.
Calibrating a paint-mixing app against a monochrome print process would produce a style in which
no selection of acrylics does any work.

Realism and Tonalism are measurably distinguishable — 100% of pixels differ, mean ΔE 18.63 — so
the two adjacent rows are not the problem.

## The retune, and what the tracks actually disagree about

| Parameter | Now | Track 1 | Track 2 | Track 3 | Track 4 | Recommended |
|---|---|---|---|---|---|---|
| `MotherColourTransform` | 0.30 | **delete** | **0.00** | stop whitening | removing helps | **delete from this row** |
| `contrast` | 0.55 | **0.75** | **0.80** | "give back the range" | — | **0.75–0.80** |
| `key` | +4.0 | **−8** | — | — | — | **−8** |
| `chroma` | 0.45 | **0.85** | raise | 0.45 unsupported | — | **0.85**, low confidence |
| Floor `strength` | 2.0 | — | near-inert | **leave alone** | **4.0** | **leave at 2.0 for now** — see below |
| Floor `edge` | 0.05 | — | — | **0.10** | — | **0.10** |
| Slot 5 | empty | `SmallRegionMerge` | — | `SmallRegionMerge` | `SmallRegionMerge` | **register it** |

**The key sign error is the single largest parameter defect found this round.** The measured
target is roughly **−10 L\*** against a photograph (−14 for Whistler's nocturnes alone); the
shipped row delivers **+8.08**. That is a sign error on the largest of the two significant value
statistics, and it is the third round running to find a sign error on a `ToneAndChromaRemap`
parameter. Track 1 recommends −8 rather than the measured −11 for the same reason the
Post-Impressionism round recommended contrast 1.0 rather than 0.85: the magnitude rests on a
scan-versus-photograph comparison it does not trust to two figures, while the direction is
unanimous across 34 works and two controls.

### The floor: a real conflict, and the resolution is that they are two different knobs

`EdgePreservingFloor` declares **both** `strength` (1.0–5.0) and `edge` (0.01–0.30, default 0.05).
`[verified — EdgePreservingFloor.cs:33-34]` The tracks were not arguing about the same parameter.

- **Track 3** measured `edge` 0.05 → **0.10**: sub-mark share 33.83% → 23.68%, mean boundary ΔE
  5.73 → 4.90, hard-boundary share 14.1% → 9.9%, **at no extra cost** — ε is a scalar in the same
  two passes. Reaching the same paintability through `strength` costs five passes instead of two
  and leaves the picture *harder* (11.9% hard share vs 6.6%).
- **Track 4** measured `strength` 2 → 5 at 25.77% → 16.12% *and* softening (mean ΔE 6.42 → 5.99,
  hard-edge share 17.7% → 15.9%), calling it the only lever it found that improves paintability
  and edge quality together, and recommends 4.0.
- **Track 2** measured the existing 1.0 → 2.0 override at 0.24–1.24 ΔE — at or below visibility.

**These are compatible.** Strength does soften; ε softens more per unit of paintability bought, and
costs less. Track 3's comparison is the one that answers the question actually being asked, so
**take ε 0.10 first and leave strength at 2.0**, then re-measure. Track 4's case for strength as a
*style* parameter — that nothing else in slots 1 or 5 can soften an edge, and soft edges are this
movement's named technique — is the better argument for keeping the option open, not for spending
it now.

Track 3 adds a ceiling: **do not push ε past 0.10.** At 0.15 and 0.30 the render sits *behind an
unfiltered photograph* on distance to the canvas statistics, overshooting median edge width by
2–3× against a corpus SD of 0.35 px.

### The one number the tracks genuinely disagree on

**The canvases' value range.** Track 3 measures L\* 5–95 spread at **39.5** over 15 canvases;
track 2 measures **51.5** over 7; track 1's p5–p95 band is [16.1, 64.5], i.e. **48.4** over 34;
track 4's p1–p99 mean is **52.5** over 7. Track 3 is the outlier and the corpora barely overlap.
Every contrast recommendation is calibrated against this figure, so **the retune's magnitude is
softer evidence than its direction.** The direction — the row compresses too far — is unanimous.

## Atmosphere: buildable, and the aerial rejection should be narrowed rather than upheld

Track 2 owns this and the decomposition is the most useful part.

**Seven candidate decompositions collapse to three, and only one survives.** Local contrast and
edge softness are the *same* measurement — Pearson **r = 0.973** (min 0.956) across 21,340 blocks
of nine photographs, so building both builds one thing twice. Chroma-with-distance and
contrast-with-distance genuinely are separable (r = −0.036). What is left is **lightness with
distance**: present in the textbook direction on **8 of 8** landscapes (far − near ΔL\* +23.0 to
+64.7, median +45.2) and **5 of 6** canvases. `[verified]`

**The Post-Impressionism round's blanket aerial rejection should be narrowed.** Using hand-drawn
far/near patches rather than a mechanical band split:

- **Lightness holds** — 8 of 8.
- **Chroma is backwards** on 6 of 8 landscapes and 4 of 6 canvases, and stays backwards after
  regressing out the lightness confound. It survives only in relative chroma C\*/L\*, which no
  stage operates on.
- **Hue-cool-with-distance fails on 5 of 8**, worst on backlit and low-sun scenes — Tonalism's
  entire subject matter.
- **The neural-depth rejection stands.** A row-only ramp orders far/near correctly on 8 of 8.

**The shipped row is an atmosphere destroyer**: it attenuates the far−near lightness separation
the photograph already carried by ×0.41 (median 45.2 → 19.7), chroma separation by ×0.50, and
annihilates the contrast separation — sign flip or collapse on 5 of 8. Nothing in the row is
spatial, so depth is compressed exactly as hard as everything else.

**`AtmosphericRamp` was built and measured, not proposed.** A real `IPreMapStage` blending toward a
veil colour in linear light between two user-set rows recovers a third to a half of the lost depth
separation and **cuts unpaintable share on all nine photographs** (38.9% → 21.4% on the worst).
Slot 1 runs before the colour cache is built, so it costs the cache nothing and cannot break the
invariant.

### The scumble is the strongest measurement in the round and the only pick that changes what the app promises

Track 2 implemented finite-thickness Kubelka-Munk, gated to **ΔE 0.00000** against
`KubelkaMunk.Mix` on all seven paints at the opaque limit. `[verified]`

**Only white moves colour in the aerial direction** (+4.09 L\*, −5.24 C\*); cobalt darkens, black
darkens harder, every warm paint fails. **A cobalt glaze is a nocturne device and should be
labelled one.**

The reason to build it is a **gamut** argument, not a look argument: Tonalism's own remap pushes
**87% of target colours into L\* 40–80**, where the opaque candidate set is sparsest (289 of 4,888
candidates above L\* 70). A five-level white scumble ladder cuts pixel-weighted quantisation error
**2.595 → 1.209 ΔE**, and at matched candidate count beats the same budget spent on finer opaque
sampling by **20–25%** — a real gamut extension rather than denser sampling.

**It cannot live in slot 5** (`Refine` takes indices and a `CandidateSet`; a layer needs K and S)
**or in slot 3 as it stands** (`MixtureBuilder` exposes only `BlendInto`/`KeepOnly`). And it widens
the invariant: every output becomes a mixture *or a mixture under a named scumble*, which the plan
must say or the user cannot execute it. That clause is why it is ranked below cheaper items, not
the measurement.

## Corrections to prior research

Twelve. Those checked against the shipped source while writing this synthesis are flagged.

**1. The premise that made Tonalism the first style built is wrong.** The parent README's build
order calls it "track 2's most-achievable style… every property is a pointwise transform plus the
existing blur. **Zero spatial or semantic component.**" Three tracks contradict it independently:
one of the four separating statistics is spatial (local |ΔL\*| 5.57 vs 9.30), the largest defect
in the rendered output is fragmentation, the movement's own treatise devotes a chapter to a
spatial device, and the residual visible fault after every colour pick is hard region boundaries.
The conclusion that it is the *most achievable* style may still hold; the reason given does not.
`[verified]`

**2. Tonalism's published paintability figure is 33× too small.** 0.77% is a 6 px threshold on a
256² synthetic gradient; photographs at the app's own default give 25.77–33.83%. Quoted in three
rounds' cross-style tables. `[verified]`

**3. `MotherColourTransform`'s effect is larger than the Post-Impressionism round recorded.** They
measured darkest L\* 11.0 → 38.3 at fraction 0.30; on the six-paint `StyleTestFixtures` palette it
is **6.46 → 40.30**, a rise of 33.8 rather than 27.3. Confirmed and strengthened, not overturned.
`[verified]`

**4. Report 02's Tonalism row is wrong in three places.** (a) The proposed output range **[35, 70]**
with "strong compression toward the middle" is wrong at the dark end — the measured band is
**[16.1, 64.5]**, and compressing toward the middle is exactly what the shipped row does and why
it has no darks. (b) The proposed chroma **×0.35** is roughly half the measured 0.82. (c) Its one
*correct* and unbuilt recommendation — lerp every pixel's (a\*, b\*) toward a chosen dominant hue
axis — is the only lever that moves the one statistic that separates the movement, and **the
shipped row substituted `MotherColourTransform` for it**, which is a different operation with a
different effect. `[verified]`

**5. "A value range of maybe three steps" is folklore.** Two tracks measured the opposite: canvas
p1–p99 runs 37.8–68.0 L\* (mean 52.5), *wider* than the app's Tonalism realises (32.4). Report 02
already flagged the source as a painting-instruction blog; this closes it. **The narrow things are
the key and the chroma, not the excursion.** `[verified]`

**6. Tonalist canvases are not low-chroma against photographs.** Median C\* 15.7 vs 16.0 (track 3);
mean 16.79 vs 20.42, not significant (track 1). Tonalist colour is low-chroma relative to its own
narrow value range. **The chroma knob is aimed at a difference that is not there.** `[verified]`

**7. Graham & Field does not support "replace the Gaussian", and edge-preserving is not
spectrum-preserving.** This is the parent round's headline finding and its load-bearing argument.
Measured on a Tonalist corpus, canvases are **steeper** than photographs (−1.113 ± 0.174 vs
−1.031 ± 0.175, overlapping) — the opposite sign — and the guided filter at ε 0.30 steepens
**more** than a radius-10 Gaussian. **The conclusion survives on other grounds**: after the
mandatory floor a Gaussian buys nothing (z-distance 1.11 at radius 1 against 1.14 for no blur) and
overshoots measured edge width by 2×. The *reason* should stop being repeated as stated.
`[verified for this corpus]`

**8. Mather 2014 does not reproduce.** "Artworks occupy a narrower band of spectral slopes than
matched photographs" — measured SDs 0.174 and 0.175, bands equally wide. This is the parent
README's own highest-priority verification debt, it has never been opened, and it currently
carries the lead recommendation. `[verified for this corpus]`

**9. Report 03 lever 1 names the wrong parameter — confirmed a second time, on a second style.**
Focal *radius* moves the four radial bands +0.0/+0.1/+1.1/+0.3%; focal *edge threshold* moves them
+0.0/−5.2/−19.4/−25.2% and improves paintability. **The correction should move out of the
Post-Impressionism README and into `03-brushwork-and-edges.md` itself.** `[verified]`

**10. The parent README over-budgets the focal lever.** A radially varying *pre-map* filter does
not break the 6-bit colour cache, because the cache keys the mapping and the mapping still sees
only colours. The "~3 bits of radial band, ~8 MB" cost applies to a position-dependent
*quantiser*, not to this lever. `[verified]`

**11. `SpectralRenderer`'s doc comment is false.** "Gamut mapping is a display concern and appears
nowhere else" — `MixtureBuilder.RenderMixture` goes through `ToDisplayColor`, so the **entire
converter runs on gamut-mapped 8-bit colour**, a mean **3.35 ΔE** from unmapped spectral Lab. It
cost one track an hour of wrong numbers. `[verified against the source]`

**12. `ContourLines` re-introduces sub-mark regions after the merge removes them.** Fauvism's
entire remaining fragmentation, 0.60–2.34%, is this. **The merge must run last.** Live defect in
the working tree. `[verified]`

**Confirmed, not corrected:** the Post-Impressionism round's boundary-statistics method reproduces
across sessions and corpora to within 0.013 on four ratios; its cloisonnist line-share corpus
cross-validates from an independent detector (8.97% vs 11.2%); and the union-find merge does what
two rounds asked for.

## Suggested build order

Nothing here is decided. Cheapest and best-supported first. Slot numbers refer to the five-slot
pipeline.

| # | Item | Slot | Cost | Why here |
|---|---|---|---|---|
| 1 | **Retune the row and delete its mother colour** — contrast 0.55 → 0.75, key +4.0 → **−8**, chroma 0.45 → 0.85, `MotherColourTransform` → `KeepAllCandidates`, and **register `SmallRegionMerge`** | 2, 3, 5 | ~6 lines | All four tracks. Measured together: L\*sd ratio 0.419 → **0.666** (target 0.683), C\* ratio 0.485 → **0.818** (0.822), ΔL\*mean +8.08 → **−7.33** (−10.6), minimum output L\* 42.61 → **11.60**, sub-mark share 28.78% → **1.23%**. Rendered and looked at: reads as a painting where the shipped row reads as a faded photocopy. |
| 2 | **Fix the post-map stage order** — `SmallRegionMerge` must run after `ContourLines` | 5 | ~1 line | Live defect in the working tree. Recovers Fauvism's last 0.60–2.34%. |
| 3 | **Floor `edge` 0.05 → 0.10** on Tonalism | 1 | 1 line | 33.83% → 23.68% sub-mark share, mean boundary ΔE 5.73 → 4.90, hard share 14.1% → 9.9%, at no extra cost. Stop at 0.10. |
| 4 | **Repair `MostNeutralPaintIndex`** — score `chroma + w·\|L*−50\|` instead of chroma alone | — | ~6 lines + test | Tonalism deletes the stage, but `AbstractPaletteTransform` still calls it, so Abstract carries the same defect at `motherFraction` 0.15. `IsMoreNeutral` exists as the seam a test can drive. |
| 5 | **Write the merge postcondition as a registry-wide test** | — | ~20 lines | It is now true on photographs for the first time. Pin it before something breaks it again — as `ContourLines` already has. **Assert on a photograph, not on `Tests/Golden`.** |
| 6 | **`AtmosphericRamp`** in slot 1, two user-set handles | 1 | ~60 lines + UI | Built and measured, not proposed. Recovers a third to a half of the destroyed depth separation and cuts unpaintable share on 9 of 9. Cache-free, invariant untouched. **Default the strength to zero until someone has looked at it.** |
| 7 | **A threshold parameter on `SmallRegionMerge`**, default 1.0 — an exact no-op | 5 | ~40 lines | Fixes the structural defect three rounds have named: **all three post-map stages declare zero parameters**, so slot 5 has no tuning surface and two styles registering a stage get byte-identical behaviour. Do **not** raise it for Tonalism — see below. |
| 8 | **Per-image hue convergence** on the Lab remap | 2 | ~75 lines | The only measured lever on the one statistic separating Tonalism from Impressionism: hue concentration 0.775 → **0.899** against a target of 0.90, at no cost in lightness, chroma or fragmentation. **Must derive its target from the image** — a fixed target delivers nothing. |
| 9 | **Focal edge-threshold floor**, shared and defaulting to **off** | 1 | ~120 lines | Focal band held to +0.0%, outer band −25.2%, sub-mark share 32.7% → 23.6%. Cheaper than the parent README budgets. Not Tonalism's signature — see "what not to build". |
| 10 | **The white scumble ladder** — finite-thickness K-M | 3 | ~70 lines + a plan/UI change | The strongest measurement in the round: quantisation error 2.595 → 1.209 ΔE, 20–25% better per candidate than finer opaque sampling. Ranked last only because it **widens the invariant**. |

Items 1–5 are roughly 35 lines and fix live defects in three styles.

**Floor `strength` is deliberately absent.** Track 4 wants 4.0, track 3 wants it left alone, track
2 measured the existing override as near-inert, and item 3 buys the same paintability more cheaply.
Re-measure after items 1 and 3 land.

## What not to build

The parent, Abstract, Fauvism and Post-Impressionism lists all still apply. These are additional,
each rejected by a track that went looking for it.

- **`ContourLines` on Tonalism, at any setting.** 6.7× the corpus line share, twice the cloisonnist
  control, all three defects reproducing, and byte-identical to Fauvism's by construction.
- **A separate "Nocturne" or "Pictorialism" row**, and splitting American Tonalism from Whistler.
  Refused by the scholar who coined the term. Revisit only if the mother colour is fixed and the
  row still cannot reach a nocturne.
- **Raising the merge's area threshold for this style.** Track 4 expected to recommend it and the
  measurement refused: real canvases put 76.5–91.5% of their area in masses of at least one mark²,
  largest mass 13.2–36.1%; Tonalism with the merge already reaches **100.0%** and **37.1%** — past
  the corpus on both. The row over-consolidates before the knob is turned.
- **A chroma-falls-with-distance stage, in any style**, and **a hue-shifts-cool-with-distance
  stage.** Backwards on 6 of 8 and failing on 5 of 8 respectively, the latter worst on exactly this
  movement's subject matter.
- **A separate "edge softness with distance" stage alongside a contrast one.** r = 0.973. One
  measurement.
- **A Gaussian pre-blur for Tonalism**, despite this being the style with the best case for one —
  and **not** for the parent README's spectral reason. See correction 7.
- **Spatially varying the guided filter's *radius*.** Now a settled negative on two styles.
- **Pushing the floor's ε past 0.10 on Tonalism.** Past that it sits behind an unfiltered
  photograph on distance to the canvas statistics.
- **A "soft edge" stage that widens transitions.** Tonalist boundaries are 1.66 px wide against
  photographs' 1.36 — a difference of 0.3 px. What differs is the contrast carried across them,
  not the width. Any stage whose mechanism is "make the transition gradual" targets a property the
  canvases do not have.
- **Making the focal lever Tonalism's defining device.** Five of fifteen canvases run the hierarchy
  backwards — including Harrison's own, at ratio 0.15 — and the movement's own theorist prescribes
  uniform treatment for the nocturne.
- **Automatic focal-point detection**, strengthened from a new direction: a detector tuned to
  centre bias would be confidently wrong on a third of the target style.
- **A fixed-hue tint of any value, even user-set, without a per-image default.** Aimed at 90° at
  strength 0.85 it delivers hue concentration **0.668 against a control of 0.670** — nothing.
  Aimed at the image's own hue, 0.800. **The same ask-versus-deliver failure the Post-Impressionism
  round found on the per-hue chroma ceiling, in a different stage.**
- **A hard-coded warm tint** on the strength of the "brown gravy" reading. Real for Inness,
  Blakelock, Wyant and Ranger; **absent from Whistler's nocturnes** (b\*mean −1.01). Expose the
  hue, do not choose it.
- **Lowering chroma further, or treating mean chroma as this style's target statistic.**
- **Any further value compression.** The row already sits at 62% of the canvases' value range and
  23% of their mean chroma. The pressure should run the other way.
- **A "Tonalist palette" preset naming pigments**, including report 02's *Sea and Rain* five.
  Whistler's twelve are mostly earths and historical pigments the user can select none of, and
  report 02's substitute list is two-fifths withheld as `ReflectanceDerived`. Same rejection, same
  reason, as the Fauvism round's viridian preset.
- **Calibrating against the Pictorialists.** Their chroma is a toner, not a palette.
- **`GroundFill` in Tonalism, repaired or not.** Metric change 0.00 on 8 of 9; fills the
  *foreground* on 2 of 9; and its apparent gentleness here is an accident of `key +4.0` meeting a
  hard-coded L\* 58 that the retune would break.
- **A K-M glaze as an `IPostMapStage`**, and **a glaze in a chromatic paint as the atmosphere
  device.** Only white moves colour the aerial way.
- **Treating `MotherColourTransform` as the luminous veil.** 7.5× less chroma per unit of lightness
  than a white scumble, with no area and no gradient.
- **Deriving the veil weight from an automatic horizon detector.** Two handles cost the user one
  drag and cannot be wrong in a way the user cannot see.
- **Amplitude-spectrum slope as an acceptance test for any style.** The shipped Tonalism render
  sits on the canvas mean (−1.119 vs −1.113) while missing the canvases by 34% on tonal range, 55%
  on chroma and 2.7× on relative boundary contrast. **A statistic a bad render already passes
  cannot gate a good one.**
- **Reading "large value masses" off this app's output as evidence it is composing well.** The
  measure is inflated by lightness compression. **Always report the notan gap beside it.**
- **Treating the shipped `EveryRegisteredStyleIsPaintable` ceilings as evidence of anything.** All
  five measure a 6–25 px threshold on a synthetic gradient.
- **Median region elongation or any orientation statistic** for this style. Tonalism has no
  directional claim to test.
- **A drawn horizon, mast or branch "structure" stage.** Every thin dark structure in the corpus is
  subject matter, not drawing; reaching it needs to know what the pixels depict.

## The corpus problem, and the one thing that improved this round

**Looking at the images caught what no metadata test could, for the fourth round running** — and
this time it also caught a *recommendation*.

- Track 1 rejected 4 of 71 by inspecting a contact sheet: three museum photographs including the
  frame and gallery wall, and a Blakelock **photographed with a colour calibration target inside
  the frame** — a contamination mode no previous round has recorded.
- Track 4 rejected 6 of 14 candidate paintings: an etching, a black-and-white reproduction, a
  framed museum shot, a watercolour with margins, a duplicate, a pencil drawing.
- Track 2 rejected a photograph with perfect EXIF and a real camera that proved on screen to be a
  heavily defocused split-toned edit, which would have poisoned two sections.
- Track 3 inspected its contact sheet twice, once as downloaded and once after cropping.

**Track 1 rendered its picks and looked, and it changed two ratings.** `AbstractPaletteTransform`
at `colourCount` 8 produced **the best numbers in the entire round** — better than the retune on
every measured axis — and **posterises**: 20 colours across a photograph reads as a screen print,
worst on the foggiest and most Tonalist subject in the set. The per-image hue rotation produces a
colour *cast* rather than a harmony on a cool seascape. Ten minutes of looking demoted a pick that
three pages of statistics had promoted.

**That is the argument for the measurement gate the parent README already specifies**, and it is
why "nobody has looked" keeps appearing at the top of these debt lists.

**Cross-track contamination recurred.** Track 4 found another track's `corpus/` already in the
shared scratchpad on arrival — the same failure the Post-Impressionism round flagged. Absolute
percentages across tracks are therefore not fully independent; the per-image rankings and the
ratios are.

## Accuracy warnings

Read these before quoting any figure.

- **The canvas value-range target is not settled** — 39.5, 48.4, 51.5 and 52.5 across four tracks
  on four corpora. Every contrast recommendation is calibrated against it. The *direction* is
  unanimous; the magnitude is the weakest link in the build order's top item.
- **All canvas colorimetry is uncalibrated web reproductions of varnished, aged oil paintings.**
  Varnish yellowing raises measured chroma and lowers measured lightness. Track 2 names its median
  mean C\* of 23.26 as the least trustworthy number in its report. Track 1's chroma target of 0.822
  has a rival figure of 0.488 from Whistler's nocturnes alone — the least yellowed group — and
  pick 1's chroma number rests entirely on choosing between them.
- **The near/far patches in the atmosphere report are one agent's eyeball judgements.** The boxes
  are recorded so the judgement is auditable; ±3 L\* in those tables is not meaningful.
- **The spectral-slope inversion behind corrections 7 and 8 is small and the distributions
  overlap.** The claim is that this corpus gives the argument no support, not that the published
  result is wrong. The estimator has not been checked against a synthetic image of known slope.
- **The visual pass is four photographs at 300 px judged by one agent**, and it already overturned
  a pick. Nobody has viewed a full-resolution conversion or compared one against a Tonalist
  painting side by side.
- **The glaze thickness has no absolute calibration.** Nothing in the repository states what one
  unit of thickness is; track 2 normalised by a self-derived hiding thickness, which is internally
  consistent but not physical.
- **Track 2's corpus is deliberately landscape-weighted**, which biases it toward the conclusion
  that a landscape device is worth having. One non-landscape control is one control.
- **Wanda Corn's *The Color of Mood* (1972) was not obtained** — out of print, not online. The
  boundary ruling's founding figures are `[relayed]` from secondary accounts.

## Verification debt

Ranked by how much clearing each would change a decision.

1. **Render items 1, 3 and 6 at full size and look at them, on more than four subjects.** Cheapest
   item on the list, it gates the top of the build order, and it has now demonstrably overturned a
   recommendation that the statistics supported. Compare against a Tonalist canvas side by side.
2. **Settle the canvas value range.** Four tracks, four figures, one recommendation resting on it.
   A shared provenance-checked canvas corpus would settle this and the chroma target together.
3. **Curate a shared, provenance-checked corpus and commit it.** Carried forward unchanged from the
   Post-Impressionism round, where it was also debt 2 and also uncleared. Four consecutive rounds
   have each independently rediscovered contamination, and this round found a new mode. Still the
   cheapest thing that would raise the quality of every future round.
4. **Whether the corpus chroma target survives varnish** — 0.822 from the movement against 0.488
   from Whistler's nocturnes, a 1.7× spread. A handful of colour-managed museum downloads would
   settle it. Recorded as the top sourcing debt by the Fauvism and Post-Impressionism rounds too,
   and never cleared.
5. **Whether restoring the tonal range and raising ε land inside the canvas envelope together.**
   Measured on separate ladders; the combination is arithmetic, not a render. One probe run.
6. **Whether lowering the key interacts badly with the paint gamut's dark end.** Below about L\* 25
   the candidate set thins and its chroma collapses. A nocturne at key −8 might band, and the
   fragmentation metric does not detect banding.
7. **Mather 2014** — never opened across five rounds, still carrying the parent README's lead
   recommendation, and now with a local negative result against one of its claims. **The single
   most valuable item on any debt list in this directory.**
8. **Graham & Field 2007's fitting protocol.** The numbers are recorded as verified; correction 7
   needs the *protocol* — luminance definition, window, frequency range, per-image versus pooled
   fit. The PDF returned undecodable binary through the agent environment, as it did for the parent
   round.
9. **Whether `AbstractPaletteTransform` at `colourCount` 12 avoids the posterisation seen at 8.**
   The version actually recommended was never rendered.
10. **Calibrate the scumble ladder against something a painter can measure** — coats, or a
    medium-to-paint ratio — before shipping item 10.
11. **Wanda Corn 1972**, and the Whistler technical studies on the "sauce", both unobtained.

Items 1–6 are local work, cost little, and gate more than the paywalled sources combined. Clear
them first.
