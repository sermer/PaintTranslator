# Research: Realism

Research into Realism aimed at one question: **what should the "Realism" style actually do?** As
shipped it is `EdgePreservingFloor` at the stage's **own declared defaults** — strength 1.0, edge
0.05, with no `WithDefaults` call at all — `IdentityRemap`, `KeepAllCandidates`,
`NearestQuantiser`, mark scale **1.0**, and **no post-map stage**. It is `StyleRegistry.Default`,
deliberately identical to the converter that predates styles.

Four parallel tracks, written by separate agents that did not see each other's work. This README
is the synthesis. The reports are long; read this first and go to them for detail.

| Report | Covers |
|---|---|
| [01-palette-and-colour.md](01-palette-and-colour.md) | A 53-work corpus against 23 photographs, eighteen statistics and the two that survive correction, the shipped row measured inside its envelope, the merge costed against a face. |
| [02-subjects.md](02-subjects.md) | Six subject strata measured, subject-dependence found and then shown non-semantic, skin against the gamut leave-one-out, the black floor. |
| [03-brushwork.md](03-brushwork.md) | Real canvases scored with the app's own metric, the registry contract against the invariant, the merge's absorption rule, mark scale and resolution. |
| [04-edges.md](04-edges.md) | 21 canvases against 17 photographs, range normalisation done properly, the quantiser identified as the edge stage, edge hierarchy's second null. |

## The headline: Realism is close to right already, and this round's value is in what it stops

**This is the first round in six whose strongest result is a negative.** Four rounds of momentum
pointed at Realism as the app's worst row — it carries the highest sub-mark share in every
cross-style table ever printed here — and all four tracks arrived at the same place from four
directions: **the fragmentation figure does not mean what those tables took it to mean, and the
fix everyone queued for it destroys the picture.**

Track 1 states the colour half plainly. Over a 53-work realist corpus against 23 subject-matched
EXIF-verified photographs, **only two of eighteen statistics survive a Bonferroni correction**,
and the shipped row lands three of seven ratios inside the corpus envelope with **zero parameter
overrides** — L\*sd 0.927 against a target of 0.903, local |ΔL\*| 0.806 against 0.819, notan 0.935
against 0.894. `[verified]`

The honest version is weaker and more useful: **RMS z-distance to the corpus is 0.83 converted
and 0.84 unconverted.** Realism has no tight envelope to hit. That is the strongest argument
anyone has produced for `IdentityRemap`, and it is an argument against building rather than for
it. Every `ToneAndChromaRemap` setting tested costs **4.2–7.5 ΔE** of fidelity against the
shipped row's 6.45.

## The first convergence: do not register `SmallRegionMerge` on Realism — four renders, four rejections

**All four tracks rendered the merge on Realism and all four rejected it.** No previous round in
this directory has had this strength of agreement against a queued pick, and every one of the
four reached it by *looking*, after statistics that unanimously supported it. `[verified]`

| Track | Corpus | What the statistics said | What the render showed |
|---|---|---|---|
| 01 | 12 photographs | exactly 0.000000 sub-mark | a sitter's **eyes, mouth and headdress** become flat blobs; **+4.26 ΔE** |
| 02 | 36 photographs | exactly 0.00% on all 36, gated byte-identical to the shipped stage | **Half Dome** becomes flat blobs, a face becomes camouflage; **9.74 ΔE** mean, 34.13 worst |
| 03 | 15 photographs | 0.000000 on 15/15, idempotent | a forest floor becomes **camouflage**, harbour boats become white blobs, a red door sign is **swallowed** |
| 04 | 17 photographs | D10/D20 move *toward* the canvas target | a **flock of sheep** becomes salmon blobs; median region width exactly **1.00** against a canvas 1.75; z 1.415 → **2.02** |

**Track 1 found the rule that replaces the queued one.** The Tonalism and Post-Impressionism
rounds both recommended registering the merge on any row with an empty slot 5. That
generalisation is now retired, and the replacement is: **the merge's cost scales with the
sub-mark area it is handed** — 42.7% for Realism as shipped, against 19.4% at floor s3/e0.15.
Track 3 reached the same conclusion from the other end by pre-flattening in slot 1 first and
watching boundary ΔE fall 12.99 → 8.75.

**Track 4 supplies the mechanism.** An area opening does not soften an edge; it deletes one.
Width mix goes to **94.1/5.8/0.1** against a canvas 38.4/52.1/9.5, and mean boundary ΔE *rises*
9.24 → 14.12. The stage that repairs Fauvism and Abstract is the wrong shape for a row whose
contract is fidelity.

**Track 1's caveat is the sharpest constraint on any future attempt:** *no floor setting rescues
it, because the flattening that makes the merge cheap is what erases the face.*

### One live defect in the uncommitted merge, with a measured better alternative

Track 3 found that `SmallRegionMerge` absorbs each small region into its **largest** neighbour
rather than its **colour-nearest** (`SmallRegionMerge.cs:182-216`). A prototype that reproduces
the shipped stage exactly when ranked by area gives, when ranked by CIELAB: `[verified]`

| | Shipped (by area) | By CIELAB |
|---|---|---|
| Regions | 4,078 | **3,209** |
| Median region area | 58.9 px | **121.3 px** |
| Mean boundary ΔE | 12.99 | **11.66** |
| Colour displacement | 18.13 | **15.19** |
| Thin-detail retention | 38.3% | **46.4%** |
| Sub-mark share | 0.000% | **0.000%** |

Better on every axis, from one comparison. This is a defect in code sitting uncommitted in the
working tree and it affects Fauvism, Post-Impressionism, Tonalism and Abstract — every row that
registers the stage.

## The second convergence: the paintability metric is not a style measure

**Track 3 ran fourteen real Realist canvases through the app's own Realism row and scored them
with the app's own `PaintabilityMetrics` at their own mark size.** `[verified]`

| Subject | Sub-mark share |
|---|---|
| Millet, *The Angelus* | 21.8% |
| **14-canvas mean** | **42.51%** |
| Van Gogh, *Wheatfield with Crows* | **66.80%** |
| Homer | 71.8% |
| 15 photographs, for comparison | 52.99% |

**Pictures that demonstrably were painted fail the app's paintability test.**
`FractionInRegionsSmallerThan` measures how finely the converter subdivides continuous tone, not
whether the output could be executed. Three prior rounds printed cross-style tables of that
figure and read them as evidence about style.

Tracks 1 and 2 arrived independently at the same conclusion from the fixture side: **Realism's
published figure is 14× too small** — 3.0% at a 4 px threshold on a 256² synthetic gradient
against 41.5–45.5% on photographs. That is the second style whose published figure has proved an
order of magnitude wrong (Tonalism's was 33×), and the fourth consecutive round in which a
`Tests/Golden` conclusion failed on photographs.

**Track 2 adds the caution the directory should carry forward: sub-mark share alone is not a
sufficient acceptance criterion.** Every one of the four merge renders above passed it perfectly.

### The registry contract and the mark invariant are incompatible, and the app resolves it by omission

Track 3's structural finding. `StyleRegistry.Default`'s doc comment promises Realism shows "no
difference from the single-path converter that predates it." The v1 scope adds a second
invariant: every output region must be a mark a human could execute. **Realism cannot satisfy
both**, and today it satisfies the first while `EveryRegisteredStyleIsPaintable` asserts the
second against a fixture 14× too loose to notice.

Three tracks propose three resolutions, and this is a decision for the owner rather than a
finding: a per-style paintability rule (track 1), stop asserting Realism is paintable (track 2),
or accept that the default row promises something the app cannot deliver and say so in the UI
(tracks 2 and 3).

### The long-running sub-mark disagreement is closed, and it was never resolution

Six measurements across three rounds: **40.84, 42.7, 51.30, 52.69, 52.99, 55.29%.**

- **Track 3 ruled out resolution directly.** The same photographs at 480/960/1800 px give
  51.22 / 52.99 / 52.75 — the metric is scale-free because the mark tracks the short edge.
  `[verified]`
- **Track 2 supplies the cause: the subject mix of the corpus.** Portraits 36.7%, interiors
  65.1%, and **the share of pixels the guided filter left locally flat explains 69.7% of the
  variance**.

Two disagreements that have sat open across rounds close together, and neither needed new
sources.

## The third convergence: raise the floor's edge threshold — and nobody has rendered the value

Three tracks recommend it outright and the fourth tolerates it. It is the only registry change
this round supports.

| Track | ε | Measured |
|---|---|---|
| 02 | **0.10** | sub-mark 55.29% → 49.84%, hard boundaries 9.29% → 7.00%, 2.08 ΔE |
| 03 | **0.10** (+ strength 3.0, but only as a precursor to the merge) | boundary ΔE 12.99 → 8.75, hard share 36.9% → 24.5% |
| 04 | **0.15** | z to canvas envelope **1.415 → 0.752**, median width 1.33 → 1.55, D20 9.76 → 5.40, at zero cost |
| 01 | 0.10 under protest | 42.7% → 37.1% for **+0.62 ΔE**; would not ship it |

**Take 0.10.** It has three tracks behind it, track 4's 0.15 rests on a z-distance whose
strength-versus-ε separation track 3 disputes, and track 4 itself judged 0.15 down from the
best-measuring 0.30 because banding rises monotonically with ε. Track 3's strength 3.0 exists
only to make the merge survivable and should not follow, since the merge is not being
registered.

**But nobody has rendered 0.10.** Track 2 rendered 0.05 and 0.20; track 4 rendered 0.15. **All
four tracks recommend a setting no one has looked at**, in a round where looking overturned a
pick in all four reports. That is this round's cheapest and highest-value debt.

**The two cost figures disagree** — +0.62 ΔE (track 1) against 2.08 ΔE (track 2) — on different
corpora and different measures. Reconcile before quoting either.

## The fourth convergence: the quantiser is the edge stage, not the floor

Track 4 owns this and it is the round's most actionable new mechanism. Median edge width through
the real pipeline: `[verified]`

| Stage | Median edge width | Hard-width share |
|---|---|---|
| Raw photograph | 1.55 | 50.8% |
| After the real `EdgePreservingFloor` | **1.84** | 42.1% |
| Rendered | **1.33** | **60.9%** |
| Realist canvas target | **1.75** | — |

**The floor moves toward the target and the quantiser overshoots past it.** Nearest-candidate
matching erases **43.5% of boundaries**, doubles ΔE at the survivors (4.39 → 9.24), and turns
**5.60% of flat pairs into ΔE ≥ 5 steps** — visible banding in any smooth sky.

The consequence for the row: **z-distance to the canvas envelope is 1.415 for the shipped render
against 1.293 for an unfiltered photograph** and 0.58 for the median individual canvas. On edges,
the shipped Realism row is further from a realist painting than the photograph it started from.

This does not contradict track 1. Colour is inside the envelope; edges are not; the defect sits
in slots 1 and 4, which is exactly where track 1 said not to look for it (slots 2, 3 and 5).

**Dithering in slot 4 is the only lever that attacks the stair-step at its source** — which is
the parent README's build item 6, reaching Realism for the first time.

### Realist edges are a real effect, unlike Tonalism's

21 canvases against 17 subject-matched photographs: hard-edge density (ΔE ≥ 20) **2.09% vs
9.72%**, t = −2.65. **Range-normalised, 2.42% vs 6.50% — 64% of the gap survives**, because
realist canvases keep **87%** of a photograph's tonal range where Tonalist canvases keep 59%.
`[verified]`

**The transferable reading of last round's result is "check the tonal range first", not "edge
gaps are usually artefacts."**

## Subjects: the dependence is real, large, and not semantic

Track 2's brief was the one with the least prior art and it returned the clearest negative in the
round. 36 photographs across six strata through the real `StylePipeline.Render`:

**The app fails in opposite directions on faces and foliage.** Portraits are the *least*
fragmented stratum (36.7% against interiors' 65.1%) and among the *worst* on colour (4.85 ΔE
against landscape's 3.21). So per-subject failure is real and would be worth acting on — except:

> **A single image statistic — the share of pixels the guided filter left locally flat — explains
> 69.7% of the variance in fragmentation. The subject label explains 35.4%.** `[verified]`

**A per-subject style row or a subject detector would be a worse predictor of the defect than a
number slot 1 already computes.** That closes the buildability question with a negative and
strengthens the parent README's focal-point rejection from an entirely new direction.

### The black floor is the one genuinely subject-dependent gamut fact

The 7-paint candidate set's darkest colour is **L\* 11.00**; the 19-paint set reaches 6.43.
Pixels darker than that run **3.4% (landscape) to 23.9% (night)** — a 7× spread, and **r = +0.616
with per-image quantisation error, the best single predictor found.** `[verified]`

**Not fixable by any stage. Real paint has the same limit.** It should be surfaced to the user as
a fact about the palette, not treated as a defect.

Track 1 reached the same wall from the opposite side, and it is the round's most surprising
statistic: **realist canvases do not reach photographic blacks** — L\*p1 **7.10 vs 2.785**,
t = +3.93. The achievable gamut's darkest mixture is L\* 6.43–6.46, and calling
`EdgePreservingFloor.Apply` alone takes a photograph's L\*p1 from 3.40 to **7.50**. **The
mandatory floor lands the corpus figure for free, by accident.**

### Skin is sampling-limited, not gamut-limited — and one paint carries it

63,962 hand-boxed skin pixels. Residual **5.357 ΔE** on 7 paints, but the error has **no
direction**: signed ΔL\* −0.34, ΔC\* −0.27, Δh +2.45°. That is a sampling density problem, not a
gamut shortfall.

Leave-one-out over all 19 selectable paints: after white, **C.P. Cadmium Orange is the only paint
skin depends on** (+1.198 ΔE, **3.7× the next**). Landscape depends on Bone Black instead
(+0.718) and on Cadmium Orange not at all. `[verified]`

**No earth colour is selectable** — every one is `ReflectanceDerived` and withheld — so Cadmium
Orange is doing the entire job that a realist palette would give to a row of earths.

**A subject-tuned palette is catastrophic off-subject**: a skin-optimised 6 costs **10.670 ΔE on
landscape** against the general set's 3.952. The round-7 palette is already near-optimal
generally.

## Chroma variance, not chroma mean — on a third movement

Tracks 1 and 4 measured this independently and neither knew the other had.

- **Track 1:** C\*sd **7.90 vs 11.66**, t = −3.94 — one of only two statistics of eighteen to
  survive correction. Chroma **mean**: t = −1.80, not significant.
- **Track 4:** median C\* **12.09 vs 16.28**, t = −1.45. Same null.

That is the **third consecutive movement** on which mean chroma has failed to separate, after
Tonalism (t = −1.18 vs photographs, −0.21 vs Impressionism). **`ToneAndChromaRemap`'s chroma
parameter is a plain multiplier and cannot express a variance contraction at all.**

The parent README's warning about naive chroma multipliers has been about *reachability* for
three rounds. It should now also say that the statistic the knob moves is not the statistic that
separates movements.

## Corrections to prior research

Nine. Those checked against the shipped source while writing this synthesis are flagged.

**1. "Register `SmallRegionMerge` on every row with an empty slot 5" is retired.** Queued by the
Post-Impressionism and Tonalism rounds and correct for those styles. Four independent renders
reject it for Realism at costs of +4.26, 9.74 and 14.12 ΔE. **The rule that replaces it: the
merge's cost scales with the sub-mark area it is handed.** `[verified]`

**2. The paintability metric is not a style measure.** Real Realist canvases score 42.51% and
*Wheatfield with Crows* 66.80% on the app's own metric at their own mark size. The cross-style
tables in three rounds measure how finely the converter subdivides tone, not executability.
`[verified]`

**3. Realism's published paintability figure is 14× too small** — 3.0% at a 4 px threshold on a
256² gradient against 41.5–45.5% on photographs. Found independently by tracks 1 and 2. Second
style after Tonalism's 33×. `[verified]`

**4. A pre-map filter must be judged *after* the quantiser.** The Tonalism round's §5.2 z-table is
computed on the buffer. Reproduced here, the buffer domain ranks ε 0.05 best and ε 0.30 far
behind an unfiltered photograph; the **rendered domain ranks them exactly opposite**, and only
**28–39% of buffer-domain edge widening survives**. Tonalism's own ε ceiling stands, because its
§5.3 is rendered — it is the method that does not transfer. `[verified]`

**5. Edge hierarchy fails on a second movement, harder.** Centre÷outer ratio **1.48 on canvases
vs 1.41 on photographs**, t = 1.01, and 6 of 21 inverted — a photograph already has as much
hierarchy as a realist canvas. Two null results on two movements. **The queued focal
edge-threshold item should come off the planned list.** `[verified]`

**6. `SmallRegionMerge` absorbs into the largest neighbour, not the colour-nearest**
(`SmallRegionMerge.cs:182-216`). Live defect in the uncommitted rewrite; ranking by CIELAB is
better on every measured axis at identical sub-mark share. Affects all four rows that register
it. `[verified]`

**7. `EdgePreservingFloor`'s doc comment is wrong in two places.** `[verified against the source]`
(a) It claims the stage "keeps every registered style far short of that catastrophic case" of
44.3% of pixels in regions ≤ 4 px. Measured at that exact threshold on 36 photographs: **mean
41.67%, 17 of 36 above 44.3%, worst 71.15%.** (b) It names Fauvism as running "this stage at its
own weakest declared default" — Fauvism registers **strength 3.0**. **Realism is the only row
with no `WithDefaults` call at all.**

**8. `StyleRegistry`'s Fauvism comment contradicts the code three lines below it.** It states the
floor's "strength" sits at the stage's own declared default of 1.0 and that naming it "would be a
no-op override"; the next `WithDefaults` call sets `(fauvismFloor, "strength", 3.0)`.
`[verified against the source]`

**9. Value key does not separate Tonalism from Realism.** L\*mean **39.13 vs 40.06, t = +0.18** —
the smallest *t* in track 1's report. Last round's key finding survives against the controls it
used, but its stated reason describes the app's default row equally well. **What separates the
two is value range and the light end.** `[verified]`

**Confirmed, not corrected:** the guided filter's **radius** is near-inert and **ε is the
control** — third confirmation, and the first on the global filter rather than a focal one.
Sigaki et al. does not name Realism, Naturalism or Academic art — fourth consecutive round,
fourth movement; **stop searching it.**

## Suggested build order

Nothing here is decided. Cheapest and best-supported first. Slot numbers refer to the five-slot
pipeline. **This is the shortest build order any round has produced, and that is the finding.**

| # | Item | Slot | Cost | Why here |
|---|---|---|---|---|
| 1 | **Render floor ε 0.10 on a dozen subjects and look at it** | — | one hour | Not a build item. All four tracks recommend a value nobody has rendered, in a round where looking overturned a pick in all four reports. Gates item 2. |
| 2 | **Floor `edge` 0.05 → 0.10 on Realism** | 1 | 3 lines — Realism has no `WithDefaults` call, so this creates one | Three tracks recommend it, the fourth tolerates it. Sub-mark 55.29% → 49.84%, hard boundaries 9.29% → 7.00%, boundary ΔE 12.99 → 8.75. **Do not carry track 3's strength 3.0 with it** — that exists only to make the merge survivable. |
| 3 | **Fix `SmallRegionMerge` to absorb into the colour-nearest neighbour** | 5 | ~15 lines + test | Live defect in uncommitted code, affecting four rows. Better on every measured axis at identical sub-mark share. `IsMoreNeutral`-style seam already exists in the stage. |
| 4 | **Fix the three wrong doc comments** — `EdgePreservingFloor` (twice), `StyleRegistry`'s Fauvism block | — | ~10 lines | Zero behaviour. Two of them state the opposite of what the code does, and one already cost a track time. |
| 5 | **Decide what Realism promises**, then make the test say it | — | a decision, then ~10 lines | The registry contract and the mark invariant are incompatible. Owner's call — see below. |
| 6 | **Document that C.P. Cadmium Orange is the flesh paint** | — | one sentence | Measured, actionable by the user, and the only paint-selection result this round supports. |
| 7 | **Surface the black-floor share** — the percentage of pixels below the palette's darkest mixture | UI | ~20 lines | The best single predictor of per-image quantisation error (r = +0.616), it varies 7× by subject, and no stage can fix it. A fact the user should see, not a defect to hide. |
| 8 | **Dithering in slot 4** | 4 | the parent README's item 6 | The only lever that attacks the quantiser stair-step at its source. Realism now joins Impressionism and Pointillism as a beneficiary. |

**Items 1–4 are about 30 lines and one hour of looking.** Items 5 and 7 are decisions and UI.

**Deliberately absent: any change to slots 2, 3 and 5.** Track 1 measured every remap setting as
costing 4.2–7.5 ΔE of fidelity against the shipped row's 6.45, and all four tracks rejected the
slot-5 candidate.

## What not to build

The parent, Abstract, Fauvism, Post-Impressionism and Tonalism lists all still apply. These are
additional, each rejected by a track that went looking for it.

- **`SmallRegionMerge` on Realism, at any threshold and behind any floor setting.** Four
  independent renders. The flattening that would make it cheap is what erases the face.
- **Raising the merge's area threshold, or lowering it.** Track 3 measured both directions and
  both refused. Keep the parameter (it fixes the zero-parameter defect three rounds have named)
  and default it to an exact no-op.
- **A per-subject style row, or any subject detector.** Local flatness explains 69.7% of the
  variance in fragmentation against the subject label's 35.4%. A detector would be a worse
  predictor than a number slot 1 already has.
- **A subject-tuned palette preset.** A skin-optimised 6 costs 10.670 ΔE on landscape against
  3.952. Same rejection, third time, as the Fauvism viridian and Tonalist pigment lists.
- **Any `ToneAndChromaRemap` setting on Realism.** Every one tested costs 4.2–7.5 ΔE against a
  shipped 6.45, and the row is already inside three of seven corpus ratios with no overrides.
- **A chroma multiplier as this style's lever.** Chroma *mean* is now a null on a third
  consecutive movement. What separates is variance, which no stage can express.
- **A focal edge-threshold stage as anyone's signature.** Second null on a second movement; a
  photograph already carries as much hierarchy as a realist canvas. Retire the queued item.
- **Denoising to close the fragmentation gap.** At least **86.9%** of it sits on genuine sub-mark
  detail rather than quantiser speckle (amplification ×1.45, reproducing the parent README's
  ×1.69).
- **Treating "realism means invisible brushwork" as the target.** Realist canvases are **3.3×
  less flat at mark scale than photographs** (5.3% vs 17.6%). Invisible finish describes
  *academic* painting — the thing Realism was founded against.
- **Value-mass consolidation as a style statistic.** It does not separate Realism from Tonalism
  (21.6%/77.5% vs 23.3%/84.4%). It is a paintability measure wearing a style label.
- **Sub-mark share as an acceptance criterion on its own.** Every one of the four destructive
  merge renders passed it perfectly, at exactly 0.000000.
- **Searching Sigaki et al. for another movement.** Fourth consecutive round, fourth movement not
  named.
- **A stage that tries to reach photographic blacks.** Realist canvases do not reach them either
  (L\*p1 7.10 vs 2.785), the achievable gamut stops at 6.43, and the mandatory floor already
  lands the corpus figure by accident.

## The corpus problem

**Looking at the images caught what no metadata test could, for the fifth round running**, and
two new contamination modes were recorded:

- A museum's own high-resolution file of the **wood engraving after a painting**, filed under the
  painting's title (Homer, Cleveland 1942.1309).
- A Met file whose **gilt frame occupies ~8% of each edge** — more than the standard 3% crop
  removes.

**And looking overturned a pick in all four reports**, which is new. Previous rounds have had one
track's visual pass change one rating; this time the same ten-minute check independently demoted
the same statistically-supported recommendation four times. That is the strongest evidence yet
for the render-and-look gate the parent README already specifies, and the reason item 1 of the
build order is not a build item.

**Cross-track contamination did not recur.** Each track worked in its own scratchpad
subdirectory, which was the fix the Tonalism round asked for.

## Accuracy warnings

Read these before quoting any figure.

- **The ε 0.10 cost is not settled** — +0.62 ΔE (track 1) against 2.08 ΔE (track 2), on different
  corpora and different measures. Build-order item 2 rests on it.
- **Nobody has rendered ε 0.10.** Tracks rendered 0.05, 0.15 and 0.20. The recommendation is
  interpolated.
- **Track 1's strongest statistic may be a scan artefact.** The L\*p1 gap (7.10 vs 2.785) would be
  produced equally well by lifted shadows in uncalibrated reproductions. Against that:
  Impressionist scans measure 21.16 and Tonalist 16.25 from the same source population, and 8 of
  53 realist works reach below 3.5.
- **All canvas colorimetry is uncalibrated web reproductions of varnished, aged oil paintings** —
  carried forward from every prior round and never cleared.
- **Track 3's figures are all six-paint fixture palette.** A larger palette should fragment
  *more*. Unmeasured.
- **The sub-mark percentages are not comparable across tracks.** They range 42.7–55.29% and the
  spread is subject mix, now quantified. The per-image rankings and the ratios are comparable;
  the absolute levels are not.
- **Track 2's skin corpus is hand-boxed by one agent** across 63,962 pixels. The boxes are
  recorded so the judgement is auditable.
- **"Realism" as a row name is ambiguous and the tracks read it differently.** Track 3 flags this
  directly: if the row means *fidelity to the photograph* rather than *the 1848 movement*, then
  every canvas corpus in this round is a control rather than a target, and several conclusions
  invert. **Nothing in the repository states which is meant.**

## Verification debt

Ranked by how much clearing each would change a decision.

1. **Render floor ε 0.10 at full size on a dozen subjects and look at it, beside a realist
   canvas.** Cheapest item, it gates the only registry change this round supports, and looking
   overturned a pick in all four reports.
2. **Decide whether "Realism" means the movement or means fidelity.** One sentence from the
   owner. It determines whether this round's canvas corpora are targets or controls, and several
   conclusions invert on the answer. **The largest unexamined assumption in the round.**
3. **Reconcile the two ε 0.10 cost figures** — +0.62 against 2.08 ΔE. One probe.
4. **Whether the L\*p1 gap survives colour-managed reproductions.** Track 1's strongest statistic
   and its main practical consequence both rest on it.
5. **Whether 55% sub-mark share matters at all at print resolution.** Track 2 names this its
   largest unexamined assumption, and it bears on whether item 2 is worth shipping.
6. **Whether the colour-nearest merge survives on the four rows that already register it.**
   Measured on Realism only; it is a behaviour change for Fauvism, Post-Impressionism, Tonalism
   and Abstract.
7. **Curate a shared, provenance-checked corpus and commit it.** Carried forward unchanged from
   the Post-Impressionism and Tonalism rounds, where it was also debt 2–3 and also uncleared.
   Five consecutive rounds have independently rediscovered contamination; this round found two
   new modes.
8. **Mather 2014** — never opened across six rounds, still carrying the parent README's lead
   recommendation, and now with a second local negative against it.

Items 1–5 are local work or a decision, cost little, and gate more than anything paywalled.
