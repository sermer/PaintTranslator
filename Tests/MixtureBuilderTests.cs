using System;
using System.Collections.Generic;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the one controlled route by which a style may alter the achievable gamut.
    /// Both operations must leave every surviving mixture a real combination of real
    /// paints, because that is the whole reason the colour invariant holds without
    /// anyone having to check it per style.
    /// </summary>
    public class MixtureBuilderTests
    {
        [Fact]
        public void AnUntouchedBuilderMatchesTheConverterSOwnSampling()
        {
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();

            CandidateSet built = new MixtureBuilder(paints).Build();
            int[] direct = PalettePhotoConverter.SampleAchievableColors(paints);

            Assert.Equal(direct.Length, built.Argb.Length);
            Assert.Equal(new HashSet<int>(direct), new HashSet<int>(built.Argb));
        }

        /// <summary>
        /// Mixing a mother colour into everything must contract the gamut — that is its
        /// purpose — and must do so without leaving any colour the paints cannot reach.
        /// </summary>
        [Fact]
        public void BlendingInAMotherColourContractsTheGamut()
        {
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();

            var builder = new MixtureBuilder(paints);
            builder.BlendInto(builder.MostNeutralPaintIndex(), 0.35);
            CandidateSet contracted = builder.Build();

            Assert.True(
                MaximumChroma(contracted) < MaximumChroma(new MixtureBuilder(paints).Build()),
                "the mother colour did not reduce the reachable chroma");
        }

        /// <summary>
        /// A zero fraction has to be exactly a no-op, so a style can declare the stage
        /// and leave it switched off without that differing from not declaring it.
        /// </summary>
        [Fact]
        public void BlendingZeroChangesNothing()
        {
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();

            var builder = new MixtureBuilder(paints);
            builder.BlendInto(0, 0.0);

            Assert.Equal(
                new HashSet<int>(new MixtureBuilder(paints).Build().Argb),
                new HashSet<int>(builder.Build().Argb));
        }

        [Fact]
        public void KeepOnlyRemovesMixturesFailingThePredicate()
        {
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();

            var builder = new MixtureBuilder(paints);
            builder.KeepOnly((l, a, b) => l >= 50.0);
            CandidateSet kept = builder.Build();

            Assert.True(kept.Argb.Length > 0, "the predicate removed everything");
            for (int i = 0; i < kept.Argb.Length; i++)
            {
                Assert.True(kept.L[i] >= 50.0 - 1e-6);
            }
        }

        /// <summary>
        /// A predicate that rejects everything must not produce an empty set the
        /// nearest-colour search would then index out of. Falling back to the full set
        /// is the honest response: the style asked for something impossible.
        /// </summary>
        [Fact]
        public void APredicateRejectingEverythingFallsBackToTheFullSet()
        {
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();

            var builder = new MixtureBuilder(paints);
            builder.KeepOnly((l, a, b) => false);

            Assert.Equal(new MixtureBuilder(paints).Build().Argb.Length, builder.Build().Argb.Length);
        }

        /// <summary>
        /// The mother colour is chosen from the palette rather than named, because the
        /// user picks the paints and a style cannot assume any particular one is
        /// present. Lowest masstone chroma is the paint that greys everything else.
        /// </summary>
        [Fact]
        public void TheMostNeutralPaintIsTheLeastChromaticOne()
        {
            var paints = new[]
            {
                PigmentLibrary.Selectable[6],   // C.P. Cadmium Red Light, highly chromatic
                PigmentLibrary.Selectable[18],  // Bone Black, near neutral
                PigmentLibrary.Selectable[11],  // Ultramarine Blue
            };

            Assert.Equal(1, new MixtureBuilder(paints).MostNeutralPaintIndex());
        }

        [Fact]
        public void NeutralSelectionPrefersAvailableMiddleValueOverWhite()
        {
            var paints = new[]
            {
                PigmentLibrary.Selectable[0],   // Titanium White
                PigmentLibrary.Selectable[18],  // Bone Black
            };

            Assert.Equal(1, new MixtureBuilder(paints).MostNeutralPaintIndex());
        }

        /// <summary>
        /// Pins the pair sampler's density against a fixed threshold rather than
        /// against <see cref="PalettePhotoConverter"/>'s own output — comparing the
        /// builder to the code that now just calls the builder proves nothing about
        /// either. Titanium White and Ultramarine Blue sit far enough apart in CIELAB
        /// that essentially no two of the 63 interior samples along their mixing line
        /// collapse onto the same 8-bit colour: measured directly, this pair produces
        /// 65 candidates (2 singles + all 63 interior samples surviving dedup intact).
        /// An eight-sample line could produce at most 10 (8 interior + 2 endpoints),
        /// so 50 — comfortably below the measured 65, comfortably above that 10 — is
        /// enough headroom to catch a regressed <c>PairSamples</c> without riding the
        /// exact figure.
        /// </summary>
        [Fact]
        public void PairSamplingProducesFarMoreCandidatesThanASparseLadderCould()
        {
            var paints = new[] { PigmentLibrary.Selectable[0], PigmentLibrary.Selectable[11] };

            int count = PalettePhotoConverter.SampleAchievableColors(paints).Length;

            Assert.True(count > 50, $"expected far more candidates than a sparse ladder could produce, got {count}");
        }

        /// <summary>
        /// Pins the triple sampler against the pair sampler, since <c>PairSamples</c>
        /// and <c>TripleDivisions</c> are independent constants and the untouched-
        /// builder test above cannot tell them apart. Unioning the three pairwise
        /// builds for this palette's edges isolates exactly what the pair sampler
        /// alone contributes — the triangle's interior, the triple sampler's
        /// exclusive territory, is absent from that union. Measured directly: the
        /// full three-paint build yields 297 candidates against 192 for the edges
        /// alone, a 105-candidate gap that is exactly the interior point count at
        /// <c>TripleDivisions = 16</c> — none of them collapsed for this palette.
        /// Asserting a margin under half that measured gap still fails outright for a
        /// degenerate interior grid (<c>TripleDivisions = 4</c> has only 3 interior
        /// points to offer).
        /// </summary>
        [Fact]
        public void TripleSamplingAddsSubstantiallyMoreThanTheEdgesAlone()
        {
            IReadOnlyList<PigmentCoefficients> paints = ThreePaints();

            int full = PalettePhotoConverter.SampleAchievableColors(paints).Length;

            var edgesOnly = new HashSet<int>();
            edgesOnly.UnionWith(PalettePhotoConverter.SampleAchievableColors(new[] { paints[0], paints[1] }));
            edgesOnly.UnionWith(PalettePhotoConverter.SampleAchievableColors(new[] { paints[0], paints[2] }));
            edgesOnly.UnionWith(PalettePhotoConverter.SampleAchievableColors(new[] { paints[1], paints[2] }));

            Assert.True(full > edgesOnly.Count + 50,
                $"expected the triangle's interior to add substantially more candidates than its edges alone; full={full}, edgesOnly={edgesOnly.Count}");
        }

        /// <summary>
        /// Isolates the tie-break rule from the paint-rendering pipeline it lives
        /// inside. A scan of the whole library (recorded here, not re-run per test)
        /// found no two <see cref="PigmentLibrary.Selectable"/> paints sharing an exact
        /// masstone chroma, so the tie-break cannot be exercised through real paints —
        /// and none of the fixed candidate ratios that would fabricate one land on an
        /// exact <see langword="double"/> tie either: even a spectrally flat synthetic
        /// paint carries a non-zero, ratio-dependent chroma residual (~1e-5) from the
        /// finite precision of the sRGB/XYZ matrices, so two flat paints at different
        /// lightness do not tie exactly any more than two measured ones do.
        /// <see cref="MixtureBuilder.IsMoreNeutral"/> is exposed internally for exactly
        /// this reason: it is the actual comparison <see cref="MixtureBuilder.MostNeutralPaintIndex"/>
        /// runs, not a re-implementation of it, so pinning it here pins the real rule.
        /// </summary>
        [Fact]
        public void TieBreaksTowardTheLightnessNearestMiddleGrey()
        {
            // Equal chroma: the smaller lightness gap must win, in either order.
            Assert.True(MixtureBuilder.IsMoreNeutral(chroma: 12.0, lightnessGap: 3.0, bestChroma: 12.0, bestLightnessGap: 40.0));
            Assert.False(MixtureBuilder.IsMoreNeutral(chroma: 12.0, lightnessGap: 40.0, bestChroma: 12.0, bestLightnessGap: 3.0));

            // A strictly lower chroma always wins, even against a worse lightness gap.
            Assert.True(MixtureBuilder.IsMoreNeutral(chroma: 5.0, lightnessGap: 40.0, bestChroma: 12.0, bestLightnessGap: 3.0));

            // An exact tie on both leaves the incumbent standing rather than replacing it.
            Assert.False(MixtureBuilder.IsMoreNeutral(chroma: 12.0, lightnessGap: 3.0, bestChroma: 12.0, bestLightnessGap: 3.0));
        }

        private static IReadOnlyList<PigmentCoefficients> ThreePaints()
        {
            return new[]
            {
                PigmentLibrary.Selectable[0],
                PigmentLibrary.Selectable[6],
                PigmentLibrary.Selectable[11],
            };
        }

        private static double MaximumChroma(CandidateSet set)
        {
            double largest = 0.0;
            for (int i = 0; i < set.Argb.Length; i++)
            {
                largest = Math.Max(largest, Math.Sqrt((set.A[i] * set.A[i]) + (set.B[i] * set.B[i])));
            }

            return largest;
        }
    }
}
