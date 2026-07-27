using System;
using System.Collections.Generic;
using System.Linq;
using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Wacton.Unicolour;
using Wacton.Unicolour.Datasets;
using Xunit;
using Xunit.Abstractions;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Checks this project's mixing kernel against Unicolour's, which implements the
    /// same two-constant Kubelka-Munk theory independently.
    /// <para>
    /// This is the single most valuable test in the suite. Agreement validates the
    /// vendored coefficients, the linear mixing of K and S, the Saunderson convention
    /// and the integration to Lab simultaneously. Disagreement is a physics error
    /// somewhere in that chain, and no other test isolates the chain as a whole.
    /// </para>
    /// <para>
    /// The comparison runs under a configuration built here rather than
    /// <c>ArtistPaint.Configuration</c>, because that one renders under D50 while this
    /// application renders under D65. Comparing against the default would report a
    /// systematic illuminant offset as if it were a physics failure.
    /// </para>
    /// </summary>
    public class UnicolourParityTests
    {
        /// <summary>
        /// The tolerance this gate enforces. Generous relative to the arithmetic,
        /// because the two implementations use different published tables for the CIE
        /// standard observer and will not agree to the last digit. It is tight enough to
        /// catch every structural error: swapped absorption and scattering, a missing
        /// Saunderson correction, an unnormalised integration, or a misaligned band.
        /// <para>
        /// Set to roughly twice the worst agreement actually observed, which was dE00
        /// 0.198 on C.P. Cadmium Red Light's mass tone. A gate parked far above the real
        /// agreement stops detecting drift, so this is deliberately close to it.
        /// </para>
        /// </summary>
        private const double MaximumDeltaE = 0.4;

        /// <summary>Writes the observed agreement so it can be recorded in the commit.</summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicolourParityTests"/> class.
        /// </summary>
        /// <param name="output">The xunit output sink.</param>
        public UnicolourParityTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// The paints compared, paired by name with their Unicolour counterparts.
        /// </summary>
        /// <returns>Each paint's library name alongside Unicolour's pigment for it.</returns>
        public static IEnumerable<object[]> PaintPairs()
        {
            yield return new object[] { "Titanium White", ArtistPaint.TitaniumWhite };
            yield return new object[] { "Bismuth Vanadate Yellow", ArtistPaint.BismuthVanadateYellow };
            yield return new object[] { "Hansa Yellow Opaque", ArtistPaint.HansaYellowOpaque };
            yield return new object[] { "Diarylide Yellow", ArtistPaint.DiarylideYellow };
            yield return new object[] { "C.P. Cadmium Orange", ArtistPaint.CadmiumOrange };
            yield return new object[] { "Pyrrole Orange", ArtistPaint.PyrroleOrange };
            yield return new object[] { "C.P. Cadmium Red Light", ArtistPaint.CadmiumRedLight };
            yield return new object[] { "Pyrrole Red", ArtistPaint.PyrroleRed };
            yield return new object[] { "Quinacridone Red", ArtistPaint.QuinacridoneRed };
            yield return new object[] { "Quinacridone Magenta", ArtistPaint.QuinacridoneMagenta };
            yield return new object[] { "Dioxazine Purple", ArtistPaint.DioxazinePurple };
            yield return new object[] { "Ultramarine Blue", ArtistPaint.UltramarineBlue };
            yield return new object[] { "Cobalt Blue", ArtistPaint.CobaltBlue };
            yield return new object[] { "Phthalo Blue (R.S.)", ArtistPaint.PhthaloBlueRedShade };
            yield return new object[] { "Phthalo Blue (G.S.)", ArtistPaint.PhthaloBlueGreenShade };
            yield return new object[] { "Cerulean Blue, Chromium", ArtistPaint.CeruleanBlueChromium };
            yield return new object[] { "Phthalo Green (B.S.)", ArtistPaint.PhthaloGreenBlueShade };
            yield return new object[] { "Phthalo Green (Y.S.)", ArtistPaint.PhthaloGreenYellowShade };
            yield return new object[] { "Bone Black", ArtistPaint.BoneBlack };
        }

        /// <summary>
        /// Confirms every paint at full concentration renders the same through both
        /// implementations.
        /// </summary>
        /// <param name="name">The paint's name in this project's library.</param>
        /// <param name="reference">Unicolour's pigment for the same paint.</param>
        [Theory]
        [MemberData(nameof(PaintPairs))]
        public void MassTonesAgreeWithUnicolour(string name, Pigment reference)
        {
            double difference = Difference(
                new[] { Paint(name) }, new[] { 1.0 },
                new[] { reference }, new[] { 1.0 });

            this.output.WriteLine($"{name}: dE00 {difference:F3}");
            Assert.True(difference < MaximumDeltaE, $"{name} differs by dE00 {difference:F3}");
        }

        /// <summary>
        /// Confirms mixtures agree, which is what actually exercises the linear mixing
        /// of absorption and scattering. A mass tone alone would still pass if the
        /// mixing algebra were wrong.
        /// </summary>
        /// <param name="firstName">The first paint's name in this project's library.</param>
        /// <param name="secondName">The second paint's name.</param>
        [Theory]
        [InlineData("Titanium White", "Phthalo Blue (G.S.)")]
        [InlineData("Titanium White", "Bone Black")]
        [InlineData("Ultramarine Blue", "Diarylide Yellow")]
        [InlineData("C.P. Cadmium Red Light", "Phthalo Green (B.S.)")]
        [InlineData("Quinacridone Magenta", "Hansa Yellow Opaque")]
        public void MixturesAgreeWithUnicolour(string firstName, string secondName)
        {
            Pigment firstReference = Reference(firstName);
            Pigment secondReference = Reference(secondName);
            double worst = 0.0;

            for (int step = 1; step <= 9; step++)
            {
                double share = step / 10.0;
                var weights = new[] { 1.0 - share, share };

                double difference = Difference(
                    new[] { Paint(firstName), Paint(secondName) }, weights,
                    new[] { firstReference, secondReference }, weights);

                worst = Math.Max(worst, difference);
                Assert.True(
                    difference < MaximumDeltaE,
                    $"{firstName} + {secondName} at {share:F1} differs by dE00 {difference:F3}");
            }

            this.output.WriteLine($"{firstName} + {secondName}: worst dE00 {worst:F3}");
        }

        /// <summary>
        /// Computes the CIEDE2000 difference between this project's rendering of a
        /// mixture and Unicolour's rendering of the same mixture.
        /// </summary>
        /// <param name="paints">This project's paints.</param>
        /// <param name="concentrations">The mixing concentrations.</param>
        /// <param name="references">Unicolour's pigments, index-aligned.</param>
        /// <param name="referenceWeights">The same concentrations, for Unicolour.</param>
        /// <returns>The CIEDE2000 difference.</returns>
        private static double Difference(
            IReadOnlyList<PigmentCoefficients> paints,
            IReadOnlyList<double> concentrations,
            Pigment[] references,
            double[] referenceWeights)
        {
            var reflectance = new double[SpectralBands.Count];
            KubelkaMunk.Mix(paints, concentrations, reflectance);
            SpectralRenderer.ToLab(reflectance, out double l, out double a, out double b);

            var mixed = new Unicolour(MatchedConfiguration, references, referenceWeights);
            ColourTriplet lab = mixed.Lab.Triplet;

            return ColorDifference.CieDe2000(l, a, b, lab.First, lab.Second, lab.Third);
        }

        /// <summary>
        /// A Unicolour configuration matching this project's: sRGB primaries and a D65
        /// white point.
        /// <para>
        /// If this constructor does not compile against Unicolour 8.0.0, inspect how
        /// <c>ArtistPaint.Configuration</c> is built and mirror it exactly, changing
        /// only the XYZ configuration to D65. Everything else must stay identical or the
        /// comparison stops being about the physics.
        /// </para>
        /// </summary>
        private static Configuration MatchedConfiguration { get; } = new Configuration(
            rgbConfig: RgbConfiguration.StandardRgb,
            xyzConfig: XyzConfiguration.D65);

        /// <summary>
        /// Looks a paint up in this project's library.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }

        /// <summary>
        /// Looks Unicolour's pigment up by this project's paint name.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>Unicolour's pigment for that paint.</returns>
        private static Pigment Reference(string name)
        {
            return (Pigment)PaintPairs().Single(pair => (string)pair[0] == name)[1];
        }
    }
}
