using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintTranslator.Pigments;

namespace PaintTranslator.Imaging.Styles
{
    /// <summary>
    /// Enumerates the same achievable-gamut mixtures
    /// <see cref="PalettePhotoConverter"/> samples on its own, but through exactly two
    /// mutations a style may apply first. Both mutations act only on which real
    /// combinations of real paints get rendered — one narrows the proportions offered
    /// to each mixture, the other discards mixtures by their resulting colour — so a
    /// style can reshape the achievable gamut without ever being able to name a
    /// colour the paints cannot actually mix to.
    /// </summary>
    internal sealed class MixtureBuilder
    {
        // A neutral mother should be near middle grey as well as low-chroma. A
        // palette containing white and black otherwise always selects white because
        // its masstone chroma is marginally lower, lifting the dark end of every
        // mixture. This weight makes the selection prefer the nearer available neutral
        // without requiring a named grey paint.
        private const double NeutralLightnessWeight = 0.10;

        // How many interior points each two-paint mixing line is sampled at. Endpoints
        // are covered by the single-paint entries.
        //
        // Sampling the proportions continuously is the point: a mixing line is not
        // traversed at a constant rate, because the colour moves fastest where the
        // stronger pigment is scarce, and a handful of fixed ratios lands nowhere near
        // the closest reachable colour there. Measured against colours drawn from real
        // mixtures, going from the eight-step ladder this replaced to 63 samples cuts
        // mean sampling error from 2.05 to 0.91 and worst case from 17.6 to 9.3. Past
        // this the line is saturated — 255 samples reach only 0.83, and 511 only 0.82 —
        // because neighbouring samples then differ by less than one 8-bit code and
        // collapse together in the deduplication below.
        private const int PairSamples = 63;

        // The denominator of the simplex grid each three-paint mixing triangle is
        // sampled on, so shares are whole multiples of 1/16 and the interior holds 105
        // points. Edges of the triangle are covered by the pair samples.
        //
        // The triangles are where the accuracy is: holding pairs at 63 samples, taking
        // this from 6 to 10 to 16 moves mean error 0.91 to 0.60 to 0.41, while doubling
        // it again to 24 buys only 0.27 for four times the candidates and twice the
        // build. Interior colours are also the muted ones a photograph is mostly made
        // of, which is why they earn a finer grid than intuition suggests.
        private const int TripleDivisions = 16;

        private readonly IReadOnlyList<PigmentCoefficients> paints;

        // No blend applied until BlendInto sets one. -1 cannot collide with a real
        // paint index, so it doubles as the "unset" sentinel without a separate flag.
        private int blendPaintIndex = -1;
        private double blendFraction;

        private Func<double, double, double, bool> keepPredicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="MixtureBuilder"/> class.
        /// </summary>
        /// <param name="paints">The paints available for mixing.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paints"/> is null.</exception>
        public MixtureBuilder(IReadOnlyList<PigmentCoefficients> paints)
        {
            this.paints = paints ?? throw new ArgumentNullException(nameof(paints));
        }

        /// <summary>
        /// Mixes one paint into every mixture at a fixed fraction, renormalising every
        /// other paint's share so the mixture still sums to 1.
        /// <para>
        /// A style calls this to contract the gamut toward a chosen "mother colour" —
        /// the harmonising blend real painters use — without any mixture ceasing to be
        /// a real combination of real paints. If the blend paint already appears in a
        /// mixture, the fraction is added to its own renormalised share rather than
        /// listed a second time, because a paint occurring twice in one mixture is not
        /// a real mixture.
        /// </para>
        /// </summary>
        /// <param name="paintIndex">The index, into the constructor's paint list, of
        /// the paint to blend in.</param>
        /// <param name="fraction">The blended paint's share of the result, in [0, 1].
        /// Zero is an exact no-op, so a style can wire up this stage and leave it at
        /// its default without that differing from never calling it.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// <paramref name="paintIndex"/> is outside the constructor's paint list, or
        /// <paramref name="fraction"/> is outside [0, 1]. Both ultimately come from a
        /// user-adjustable slider, so they need a clear error here rather than an
        /// <see cref="AggregateException"/> surfacing later from inside the
        /// <see cref="Build"/> parallel loops, blaming a parameter this method never saw.</exception>
        public void BlendInto(int paintIndex, double fraction)
        {
            if (paintIndex < 0 || paintIndex >= paints.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(paintIndex), paintIndex, "Must be a valid index into the paint list.");
            }
            if (fraction < 0.0 || fraction > 1.0 || double.IsNaN(fraction))
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Must be in [0, 1].");
            }

            blendPaintIndex = paintIndex;
            blendFraction = fraction;
        }

        /// <summary>
        /// Restricts the built set to mixtures whose CIELAB colour satisfies a
        /// predicate, evaluated once per distinct resulting colour.
        /// <para>
        /// If the predicate rejects every candidate, <see cref="Build"/> discards it
        /// and returns the unfiltered set rather than an empty one: an empty candidate
        /// set would make the nearest-colour search index out of bounds, so a style
        /// that asked for something impossible gets the unfiltered gamut back instead
        /// of a crash.
        /// </para>
        /// </summary>
        /// <param name="predicate">Takes a candidate colour's L*, a*, b* and returns
        /// whether it should survive.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
        public void KeepOnly(Func<double, double, double, bool> predicate)
        {
            keepPredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// Finds the paint whose masstone is least chromatic — the one that, mixed
        /// into everything else, greys the whole palette rather than tinting it toward
        /// any one hue.
        /// <para>
        /// Chosen from the palette rather than named, because the user picks the
        /// paints and a style cannot assume any particular one — a black, a grey, a
        /// specific brand's paint — is present. Ties broken by lightness nearest
        /// middle grey, since two paints of equal chroma are otherwise indistinguishable
        /// as a mother colour candidate.
        /// </para>
        /// </summary>
        /// <returns>The index, into the constructor's paint list, of the least
        /// chromatic paint.</returns>
        public int MostNeutralPaintIndex()
        {
            var reflectance = new double[SpectralBands.Count];
            int bestIndex = 0;
            double bestChroma = double.MaxValue;
            double bestLightnessGap = double.MaxValue;

            for (int i = 0; i < paints.Count; i++)
            {
                KubelkaMunk.Mix(new[] { paints[i] }, new[] { 1.0 }, reflectance);
                int argb = SpectralRenderer.ToDisplayColor(reflectance, out _).ToArgb();
                PalettePhotoConverter.RgbToLab(
                    (argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF,
                    out double l, out double a, out double b);

                double chroma = Math.Sqrt((a * a) + (b * b));
                double lightnessGap = Math.Abs(l - 50.0);

                if (IsMoreNeutral(chroma, lightnessGap, bestChroma, bestLightnessGap))
                {
                    bestChroma = chroma;
                    bestLightnessGap = lightnessGap;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// The ranking <see cref="MostNeutralPaintIndex"/> applies: strictly lower
        /// chroma always wins, and an exact chroma tie is broken toward whichever
        /// lightness sits closer to middle grey.
        /// <para>
        /// Broken out as its own method, rather than left inline in that loop, because
        /// no two paints in <see cref="PigmentLibrary.Selectable"/> happen to share an
        /// exact masstone chroma — a scan of the whole library found none — so the
        /// tie-break clause is otherwise unreachable from any test built on real
        /// paints. Exposing the actual comparison lets a test pin it directly with
        /// contrived numbers instead of needing a physical coincidence that the
        /// measured library does not contain.
        /// </para>
        /// </summary>
        /// <param name="chroma">A candidate's C*ab.</param>
        /// <param name="lightnessGap">A candidate's |L* - 50|.</param>
        /// <param name="bestChroma">The best-so-far candidate's C*ab.</param>
        /// <param name="bestLightnessGap">The best-so-far candidate's |L* - 50|.</param>
        /// <returns><see langword="true"/> if the candidate should replace the
        /// best-so-far.</returns>
        internal static bool IsMoreNeutral(
            double chroma, double lightnessGap, double bestChroma, double bestLightnessGap)
        {
            double score = chroma + (NeutralLightnessWeight * lightnessGap);
            double bestScore = bestChroma + (NeutralLightnessWeight * bestLightnessGap);
            return score < bestScore || (score == bestScore && lightnessGap < bestLightnessGap);
        }

        /// <summary>
        /// Samples the achievable gamut exactly as
        /// <see cref="PalettePhotoConverter"/> does on its own — each paint alone,
        /// every pair across its mixing line, every triple across its mixing triangle
        /// — with any blend from <see cref="BlendInto"/> folded into each mixture
        /// before it is rendered, and any predicate from <see cref="KeepOnly"/>
        /// applied to the deduplicated result afterward.
        /// </summary>
        /// <returns>The surviving candidates, indexed for nearest-colour search.</returns>
        public CandidateSet Build()
        {
            int count = paints.Count;

            // Enumerating the subsets up front turns three nested loops into two flat
            // lists that can be walked in parallel. Every mixture is independent of
            // every other, so the only thing serialising this was the shared duplicate
            // set — and deduplicating once at the end is cheaper than sharing it.
            var pairs = new List<(int First, int Second)>();
            var triples = new List<(int First, int Second, int Third)>();
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    pairs.Add((i, j));
                    for (int k = j + 1; k < count; k++)
                    {
                        triples.Add((i, j, k));
                    }
                }
            }

            int perTriple = TripleDivisions <= 1 ? 0 : (TripleDivisions - 1) * (TripleDivisions - 2) / 2;
            int pairBase = count;
            int tripleBase = pairBase + (pairs.Count * PairSamples);
            var sampled = new int[tripleBase + (triples.Count * perTriple)];

            // Each paint straight from the tube. A paint has no stored colour any more,
            // so even the unmixed swatch is the kernel evaluated at full concentration.
            Parallel.For(0, count, () => new double[SpectralBands.Count], (i, state, reflectance) =>
            {
                sampled[i] = RenderMixture(new[] { i }, new[] { 1.0 }, reflectance);
                return reflectance;
            },
            _ => { });

            // Every unordered pair, sampled along its mixing line.
            Parallel.For(0, pairs.Count, () => new double[SpectralBands.Count], (p, state, reflectance) =>
            {
                (int first, int second) = pairs[p];
                var baseIndices = new[] { first, second };
                var baseShares = new double[2];
                int at = pairBase + (p * PairSamples);

                for (int sample = 1; sample <= PairSamples; sample++)
                {
                    double share = (double)sample / (PairSamples + 1);
                    baseShares[0] = 1.0 - share;
                    baseShares[1] = share;

                    sampled[at] = RenderMixture(baseIndices, baseShares, reflectance);
                    at++;
                }

                return reflectance;
            },
            _ => { });

            // Every unordered triple, sampled on a regular grid across the interior of
            // its mixing triangle. Combined with the pair samples this leaves the
            // achievable gamut covered closely enough that the residual is below what
            // an 8-bit channel can express over most of it.
            Parallel.For(0, triples.Count, () => new double[SpectralBands.Count], (t, state, reflectance) =>
            {
                (int first, int second, int third) = triples[t];
                var baseIndices = new[] { first, second, third };
                var baseShares = new double[3];
                int at = tripleBase + (t * perTriple);

                // Both loops stop short of the boundary, so every point has all three
                // paints present; the boundary is covered by the pair samples above.
                for (int x = 1; x < TripleDivisions; x++)
                {
                    for (int y = 1; y < TripleDivisions - x; y++)
                    {
                        baseShares[0] = (double)x / TripleDivisions;
                        baseShares[1] = (double)y / TripleDivisions;
                        baseShares[2] = 1.0 - baseShares[0] - baseShares[1];

                        sampled[at] = RenderMixture(baseIndices, baseShares, reflectance);
                        at++;
                    }
                }

                return reflectance;
            },
            _ => { });

            // Collapse the duplicates. Sampling finely enough to matter produces far more
            // mixtures than there are distinct 8-bit colours for them to land on, so most
            // of what was just computed collapses away here.
            var seen = new HashSet<int>(sampled.Length);
            var argbs = new List<int>();
            foreach (int argb in sampled)
            {
                if (seen.Add(argb))
                {
                    argbs.Add(argb);
                }
            }

            // Precompute CIELAB for every surviving candidate so KeepOnly's predicate,
            // and the per-pixel search once this is built, are pure arithmetic over
            // flat arrays.
            var argbArray = argbs.ToArray();
            var l = new double[argbArray.Length];
            var a = new double[argbArray.Length];
            var b = new double[argbArray.Length];
            for (int i = 0; i < argbArray.Length; i++)
            {
                int argb = argbArray[i];
                PalettePhotoConverter.RgbToLab(
                    (argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF, out l[i], out a[i], out b[i]);
            }

            if (keepPredicate == null)
            {
                return new CandidateSet(argbArray, l, a, b);
            }

            var keptArgb = new List<int>();
            var keptL = new List<double>();
            var keptA = new List<double>();
            var keptB = new List<double>();
            for (int i = 0; i < argbArray.Length; i++)
            {
                if (keepPredicate(l[i], a[i], b[i]))
                {
                    keptArgb.Add(argbArray[i]);
                    keptL.Add(l[i]);
                    keptA.Add(a[i]);
                    keptB.Add(b[i]);
                }
            }

            if (keptArgb.Count == 0)
            {
                // The style asked for something impossible. An empty candidate set
                // would make the nearest-colour search index out of bounds, so the
                // honest response is to ignore the predicate and fall back to the
                // unfiltered set computed above, rather than propagate emptiness.
                return new CandidateSet(argbArray, l, a, b);
            }

            return new CandidateSet(keptArgb.ToArray(), keptL.ToArray(), keptA.ToArray(), keptB.ToArray());
        }

        /// <summary>
        /// Renders one mixture through the Kubelka-Munk kernel, after folding in
        /// whatever blend <see cref="BlendInto"/> has configured.
        /// </summary>
        /// <param name="baseIndices">The paint indices the unmodified sample uses.</param>
        /// <param name="baseShares">Those paints' shares, index-aligned with
        /// <paramref name="baseIndices"/> and summing to 1.</param>
        /// <param name="reflectance">A caller-owned scratch buffer, length
        /// <see cref="SpectralBands.Count"/>, reused across calls to avoid allocating
        /// per mixture.</param>
        /// <returns>The mixture's 32-bit ARGB value.</returns>
        private int RenderMixture(int[] baseIndices, double[] baseShares, double[] reflectance)
        {
            ApplyBlend(baseIndices, baseShares, out int[] indices, out double[] shares);

            var subset = new PigmentCoefficients[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                subset[i] = paints[indices[i]];
            }

            KubelkaMunk.Mix(subset, shares, reflectance);
            return SpectralRenderer.ToDisplayColor(reflectance, out _).ToArgb();
        }

        /// <summary>
        /// Folds the configured blend, if any, into one mixture's paints and shares.
        /// </summary>
        /// <param name="baseIndices">The unmodified sample's paint indices.</param>
        /// <param name="baseShares">The unmodified sample's shares, index-aligned
        /// with <paramref name="baseIndices"/>.</param>
        /// <param name="indices">The resulting paint indices, with the blend paint
        /// folded in.</param>
        /// <param name="shares">The resulting shares, index-aligned with
        /// <paramref name="indices"/> and still summing to 1.</param>
        private void ApplyBlend(
            int[] baseIndices, double[] baseShares, out int[] indices, out double[] shares)
        {
            // Fraction zero returns the caller's own arrays rather than renormalising
            // through an identity multiplication, so the no-op is local: it holds
            // because no arithmetic runs at all, not because zero happens to be
            // absorbing for however KubelkaMunk.Mix treats a zero-weight term today.
            if (blendPaintIndex < 0 || blendFraction == 0.0)
            {
                indices = baseIndices;
                shares = baseShares;
                return;
            }

            int existingSlot = Array.IndexOf(baseIndices, blendPaintIndex);
            if (existingSlot >= 0)
            {
                // Already present: renormalise every share including its own, then add
                // the fraction on top, rather than listing the same paint twice.
                indices = baseIndices;
                shares = new double[baseShares.Length];
                for (int i = 0; i < shares.Length; i++)
                {
                    shares[i] = baseShares[i] * (1.0 - blendFraction);
                }

                shares[existingSlot] += blendFraction;
            }
            else
            {
                indices = new int[baseIndices.Length + 1];
                Array.Copy(baseIndices, indices, baseIndices.Length);
                indices[baseIndices.Length] = blendPaintIndex;

                shares = new double[baseShares.Length + 1];
                for (int i = 0; i < baseShares.Length; i++)
                {
                    shares[i] = baseShares[i] * (1.0 - blendFraction);
                }

                shares[baseShares.Length] = blendFraction;
            }
        }
    }
}
