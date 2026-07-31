## Known live defect in this style, going in

Post-Impressionism round correction 4: `MotherColourTransform` is a **whitening** operation,
not a unifying one. `MostNeutralPaintIndex()` returns Titanium White for any palette
containing white, so at Tonalism's 0.30 the darkest achievable colour rises L\* 11.0 → 38.3
for a −7% mean chroma change. Track 1 owns confirming and fixing this.

## Track 1 headline, as reported

**"Subdued palette" is a claim about value, not chroma.** 34-work Tonalist corpus vs 14
EXIF-verified photographs: mean C\*ab 16.79 vs 20.42, Welch t = −1.18, **not significant**;
against Impressionism 16.79 vs 17.31, t = −0.21, zero. The separating statistics are value
spread (L\*sd 16.13 vs 23.60, t = −4.05), value key (L\*mean 39.37 vs 50.01, t = −3.15; vs
Impressionism t = −6.96), local lightness contrast (t = −2.38) and hue concentration
(chroma-weighted resultant 0.90 vs 0.68, t = +2.62). **The style's own chroma knob targets a
statistic with no measured separation.**

- **`MotherColourTransform` confirmed, worse than reported.** At 0.30 the candidate set's
  minimum L\* goes 6.46 → 40.30 with **none of 3,037 candidates below L\* 40**, for a −4.3%
  chroma change; darkest rendered pixel 27.78 → 42.61. Cause: `MostNeutralPaintIndex` ranks by
  masstone chroma alone (White 0.6, Bone Black 1.5), tie-break never fires. A premixed 40:60
  white/black grey gives **33× more chroma contraction per unit of lightness rise**.
- A **second** dark floor sits under it: contrast 0.55 + key +4.0 maps L\* 0 to 26.5. Both must
  move.
- **key +4.0 is wrong by sign** (target ≈ −10, delivered +8.08). **Contrast 0.55 compresses a
  third too hard** (target ratio 0.683, delivered 0.419) — so contrast must *rise*, which agrees
  in direction with track 2's 0.55→0.80. **Chroma 0.45** delivers 0.485 against a movement
  target of 0.822.
- **Proposed retune + delete mother colour + fill slot 5 = six lines.** Measured after:
  L\*sd ratio 0.666 (target 0.683), C\* ratio 0.818 (0.822), minimum L\* 11.60 (was 42.61),
  fragmentation 1.23% (was 28.78%). **That 28.78% figure is track 4's territory — cross-check.**
- **Per-image** hue rotation at 0.55 takes hue concentration 0.775 → 0.899 (target 0.90); a
  **fixed** hue target delivers nothing (0.668 vs a 0.670 control) — the ask-versus-deliver trap
  again.
- **It rendered and looked**, which changed two ratings: shipped row reads as a faded photocopy,
  retune reads as a painting, `AbstractPaletteTransform` at n=8 has the best numbers in the
  report and **posterises**, and the hue rotation reads as a colour cast on a cool seascape.
  This partly discharges track 2's top debt.
- **Corrections:** report 02's Tonalism row is wrong at the dark end ([35, 70] claimed, [16.1,
  64.5] measured) and its ×0.35 chroma is half the measured figure — but its unbuilt
  dominant-hue lerp is the only lever that works. The parent README's "Tonalism has zero spatial
  component" **does not survive**: one of four separating statistics is spatial, the largest
  defect is fragmentation, and the residual visible fault is hard region boundaries.
- **New corpus contamination mode:** a Blakelock photographed with a colour calibration target
  inside the frame. Not recorded by any previous round.

## Track 2 headline, as reported (not yet cross-checked against the other tracks)

**Atmosphere is buildable as a slot-1 two-handle ramp, and the shipped row destroys the
atmosphere the photograph already has.** Row attenuates far−near ΔL\* ×0.41, chroma separation
×0.50, and flips the contrast-gradient sign on 3 of 8 landscapes. It over-compresses ~2× against
7 Tonalist canvases (delivers L\* range 32.0 / mean C\* 5.3 against the paintings' 51.5 / 23.3).

Other claims to carry into the synthesis, each needing a cross-check against tracks 1, 3 and 4:

- Local contrast and edge softness are the same measurement (r = 0.973) — **overlaps track 3.**
- The aerial rejection should be *narrowed*: lightness component holds 8/8, chroma component is
  backwards 6/8 even after regressing out lightness, hue-cool fails 5/8. Neural-depth rejection
  stands.
- Mother colour at 0.30 costs 29% of the value range and buys 4% of chroma; proposes
  contrast 0.55→0.80 with mother 0.30→0.00. **Overlaps track 1 — check for agreement.**
- Floor override 1.0→2.0 is worth 0.24–1.24 ΔE, near a no-op. **Overlaps track 3.**
- Glazing: finite-thickness K-M implemented, gated to ΔE 0.00000 against `KubelkaMunk.Mix`. Only
  white moves colour the aerial way. 5-level white scumble ladder cuts quantisation error
  2.595 → 1.209 ΔE and beats the same candidate budget spent on finer opaque sampling by 20–25%.
  Cannot live in slot 5 (indices only) or slot 3 as it stands.
- `GroundFill` should not be registered here; its gentler ΔE is an accident of key=4.0 parking
  the image near its hard-coded L\* 58.
- **New correction to prior docs:** `SpectralRenderer`'s doc comment claims gamut mapping
  "appears nowhere else" — false. `MixtureBuilder.RenderMixture` goes through `ToDisplayColor`,
  so the whole converter runs on gamut-mapped 8-bit colour, mean 3.35 ΔE from unmapped spectral
  Lab.
- Its top debt: **nobody has looked at any rendered output.**

## Track 4 headline, as reported

**The empty slot 5 is the headline for the fourth round running, and the published Tonalism
figure was 33× wrong.** `StyleBehaviourTests` records 0.77%, but that gate measures a 6 px
threshold on a 256² synthetic gradient. On 14 provenance-checked photographs at the app's own
derived default mark, Tonalism puts **25.77%** of pixels below its own mark² — corroborating
track 1's independently measured 28.78% on a different corpus.

Cross-style on track 4's corpus: **Realism 40.84%, Tonalism 25.77%, Fauvism 1.45%,
Post-Impressionism 0.00%, Abstract 0.00%.** Tonalism is now the worst *styled* row, and Realism
is worse still — both are the rows with an empty slot 5.

- **The Post-Impressionism round's verification debt 1 is cleared.** The rewritten
  `SmallRegionMerge` in the working tree reaches **exactly 0.000000** in one pass on all 14
  photographs for Tonalism, Post-Impressionism and Abstract; a second pass leaves the buffer
  byte-identical.
- **New defect in uncommitted code:** Fauvism's residual 0.60–2.34% is *entirely* `ContourLines`
  re-fragmenting what the merge repaired. **The merge must run last.** Actionable now, on the
  staged Post-Impressionism work.
- **Line — clear negative.** 7 Tonalist canvases carry 0.98–5.94% thin dark structure (mean
  2.61%); Gauguin's *Vision after the Sermon* measures 8.97% on the same detector, cross-
  validating the prior round's 11.2%. `ContourLines` on Tonalism paints 3.1–39.2% (mean 17.5%).
  All three known defects reproduce; radius is 1 on every image. Do not register it here.
- **MarkScale 1.2 is inert, not backwards.** `FloorRadius = Round(mark/2)` discards the 1.2 for
  base marks 2, 3, 4 and 7, so 1.2 and 1.0 render identically on a 960×640 photograph. Sweeping
  0.8→3.0 with slot 5 empty moves region count 5.6% and mean boundary ΔE 0.01. With the merge it
  becomes a real control (median region area 27.9 → 338.9 px). Keep 1.2, fill slot 5.
- **The row cannot reach a dark, and the notan gap proves it costs the picture.** Light/dark
  separation is 34.93 in the source, 34.35 under Realism, **15.59 under Tonalism** — weakest in
  the app. **Realism lands closer to a Whistler nocturne than Tonalism does.** Third independent
  confirmation of the mother-colour defect.
- **Boundary: one row.** Term coined 1972 by Wanda Corn; her exhibition held 49 paintings and 46
  photographs by 31 artists, Inness and Whistler both inside the founding definition.
  Pictorialism is not a rival row. Realism and Tonalism are distinguishable — 100% of pixels
  differ, mean ΔE 18.63.
- **Picks:** register `SmallRegionMerge` (one line, 25.77% → 0.00%); floor 2.0 → **4.0**;
  add a threshold parameter to the merge but leave it at 1.0, since the app already
  over-consolidates against the canvases.
- **Conflict to resolve in synthesis:** track 4 wants floor 2.0 → 4.0; track 2 measured the
  existing 1.0 → 2.0 override as worth only 0.24–1.24 ΔE. Track 3 owns that stage.
- Corpus: 6 of 14 candidate paintings contaminated (etching, B&W repro, framed museum shot,
  watercolour with margins, duplicate, pencil drawing), all caught by looking. **The shared
  scratchpad already held another track's `corpus/` on arrival** — the same cross-contamination
  the Post-Impressionism round flagged.

## Track 3 headline, as reported

**Tonalism's softness is tonal, not spatial.** 15 canvases vs 15 photographs: raw high-contrast
edge density is 8.8× lower (1.16% vs 10.20% at ΔE ≥ 20), but rescale every image to a common
60 L\* range and the gap **collapses** — 22.43% vs 23.69% at ΔE 10, indistinguishable. About
four-fifths of the measured "soft edges" is the narrow value range alone (L\* spread 39.5 vs
67.2). Median edge *width* is 1.66 px on canvases vs 1.36 px on photographs: Tonalist edges are
**crisp and low-contrast, not wide**. Birge Harrison's *Landscape Painting* (1909) — a primary
treatise by a painter in the corpus — prescribes exactly that.

- **The shipped row is the hardest-edged thing measured**, once range is normalised: ΔE-20
  density 11.52% against source photographs 8.45% and canvases 4.34%. It compresses to L\* range
  26.3 (canvases 39.5), dark end L\* 44.4 (canvases 22.2), median C\* 7.0 (canvases 15.7).
- **The Gaussian question is settled, and the parent round's reason does not carry.** Tonalist
  canvases measure *steeper* than photographs (−1.113 ± 0.174 vs −1.031 ± 0.175, overlapping),
  and the guided filter at ε 0.30 steepens more (−2.31) than a radius-10 Gaussian (−2.41) —
  **edge-preserving is not spectrum-preserving.** Also a negative for Mather 2014: SDs 0.174 vs
  0.175, bands equally wide. Judged against canvas statistics directly, a Gaussian at radius 2 is
  the closest filter tested (z 0.78 vs the shipped floor's 1.14), but its advantage is
  low-amplitude texture the mandatory floor removes first, so it buys nothing after the floor.
  **Ruling: no Gaussian — for a different reason than the parent README gives.**
- **Radius does nothing, again.** Focal *radius* moves the four radial bands +0.0/+0.1/+1.1/+0.3%;
  focal *edge threshold* moves them +0.0/−5.2/−19.4/−25.2% and improves paintability. **This
  resolves the track 2 / track 4 floor conflict: the knob is the threshold, not strength or
  radius**, which is why track 2 measured strength 1.0→2.0 as near-inert.
- **Corrects the parent README's cost estimate:** a radial **pre-map** filter does not break the
  6-bit colour cache, so the ~8 MB key extension budgeted for the focal lever is not needed.
- **Focal hierarchy is not Tonalism's signature.** 5 of 15 canvases run the hierarchy backwards,
  including Harrison's own (ratio 0.15), and Whistler's nocturnes measure uniform — as Harrison
  prescribes. Build it shared and default-off.
- **Picks:** (1) give back the tonal range, three numbers in `StyleRegistry`; (2) floor edge
  threshold → 0.10, one line, 33.8% → 23.7% sub-mark share at no cost; (3) register
  `SmallRegionMerge`; (4) focal edge-threshold floor, shared and default-off.

## Cross-track state for the synthesis

**The round's convergence: everything about Tonalism is value.** Track 1 — the palette is
subdued in value, not chroma. Track 3 — four-fifths of the soft edges is the narrow value range.
Track 4 — the notan gap halves. Track 2 — the row over-compresses ~2× and flattens depth. All
four independently indict `MotherColourTransform` plus the contrast/key pair.

**Fragmentation, three corpora:** track 1 28.78%, track 4 25.77%, track 3 33.8%. Same direction,
different magnitude; all three far from the published 0.77%. Tracks 3 and 4 agree Realism is
worse and that both empty-slot-5 rows are the problem.

**Floor conflict, resolved by track 3:** track 4 wants strength 2.0 → 4.0, track 2 measured
strength as near-inert, track 3 shows the live knob is the edge threshold. Check whether
strength and threshold are separate `EdgePreservingFloor` parameters before writing the build
order.

**Actionable against uncommitted code, not research:** track 4 found Fauvism's residual
0.60–2.34% is entirely `ContourLines` re-fragmenting after `SmallRegionMerge`. The merge must
run last.