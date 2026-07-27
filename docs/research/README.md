# Research: Acrylic Paint Blending

Research into how acrylic paints blend, how to translate RGB photos into paint
mixtures, and how that should be modelled in PaintTranslator.

This folder is about **physical accuracy**. For the separate question of making the
converter follow an artistic *style* rather than always picking the nearest colour, see
[painting-style/](painting-style/README.md) — colour theory in practice, styles and
movements, brushwork and edges, and what makes a painting appealing.

## Start here

| Document | What it's for |
|---|---|
| [acrylic-blending-findings.md](acrylic-blending-findings.md) | The synthesis. Theory, what the code actually does, paint data sources, prior art, and a tiered change list. Read this first. |
| [outstanding-work.md](outstanding-work.md) | What is not yet built, what blocks what, known defects, and verification debts. Read this to pick up the work. |

## Source reports

Four parallel research tracks, cited throughout. These are long and detailed — go to
them when the synthesis summarises something you need to check or extend.

| Report | Covers |
|---|---|
| [01-kubelka-munk-theory.md](source-reports/01-kubelka-munk-theory.md) | Single- vs two-constant Kubelka–Munk, the `weight²` term, tinting strength, spectral reconstruction options, Saunderson, finite-thickness forms, sanity tests |
| [02-photo-to-paint-pipeline.md](source-reports/02-photo-to-paint-pipeline.md) | sRGB decoding, illuminants and adaptation, colour-difference metrics, gamut mapping, value compression, quantisation and segmentation, match reporting |
| [03-acrylic-paint-reality.md](source-reports/03-acrylic-paint-reality.md) | Mass tone vs undertone, tinting strength, opacity, wet-to-dry shift, limited palettes, why mixtures go muddy, mixing units, available datasets |
| [04-prior-art-and-algorithms.md](source-reports/04-prior-art-and-algorithms.md) | Mixbox, spectral.js, Unicolour, recipe-solving algorithms, paint-by-numbers prior art, licences, validation approaches |

## Two things to know before acting on any of this

**Random-sRGB match quality is a misleading benchmark.** The reconstructed-spectrum
model scores better on it than measured pigment data, because it builds its spectra from
sRGB and is fitted to hit arbitrary screen colours. See the benchmarking section of
[outstanding-work.md](outstanding-work.md) before using aggregate ΔE to judge a change.

**Claims are marked by verification status.** Each document separates what was checked
directly from what is relayed from a source. The verification debts are listed at the
end of [outstanding-work.md](outstanding-work.md) — several load-bearing figures rest on
paywalled papers.
