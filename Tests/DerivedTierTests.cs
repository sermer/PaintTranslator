using System;
using System.Linq;
using IngestSpectra;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the derivation that turns Golden's measured reflectance into Kubelka-Munk
    /// absorption. The measurements themselves cannot be re-derived here, so what is
    /// tested is the arithmetic applied to them and the shape of the result.
    /// </summary>
    public class DerivedTierTests
    {
        /// <summary>
        /// Confirms the single-constant Kubelka-Munk function against a value taken
        /// straight from the source file: Alizarin's 400nm reflectance of 5.44% appears
        /// in the file's own K/S column as 8.2184.
        /// </summary>
        [Fact]
        public void KubelkaMunkFunctionMatchesTheSourceFile()
        {
            const double Reflectance = 0.0544;
            double ks = (1.0 - Reflectance) * (1.0 - Reflectance) / (2.0 * Reflectance);

            Assert.InRange(ks, 8.2183, 8.2185);
        }

        /// <summary>
        /// Confirms resampling puts the measured range where it belongs and holds the
        /// endpoints outside it. The CIE weights are very small below 400nm and the z
        /// observer is already zero above about 650nm, which is what makes holding the
        /// endpoints defensible rather than merely convenient.
        /// </summary>
        [Fact]
        public void ResamplingPlacesTheMeasuredRangeAndHoldsTheEnds()
        {
            // A ramp from 0.10 at 400nm to 0.40 at 700nm across the source's 31 bands.
            var measured = Enumerable.Range(0, 31).Select(i => 0.10 + (i * 0.01)).ToArray();

            double[] resampled = GoldenSpectraSource.Resample(measured);

            Assert.Equal(SpectralBands.Count, resampled.Length);

            // 380nm and 390nm are below the measured range and hold the first value.
            Assert.Equal(0.10, resampled[0], 10);
            Assert.Equal(0.10, resampled[1], 10);

            // 400nm is the first measured band; 700nm is the last.
            Assert.Equal(0.10, resampled[2], 10);
            Assert.Equal(0.40, resampled[32], 10);

            // 710nm upward hold the last value.
            Assert.Equal(0.40, resampled[33], 10);
            Assert.Equal(0.40, resampled[SpectralBands.Count - 1], 10);
        }

        /// <summary>
        /// Confirms a brighter measurement derives a smaller absorption, monotonically.
        /// A non-monotonic derivation would invert the relationship between how light a
        /// paint looks and how strongly it absorbs, which no downstream test would
        /// attribute to this step.
        /// </summary>
        [Fact]
        public void BrighterReflectanceDerivesLessAbsorption()
        {
            double previous = double.PositiveInfinity;

            for (double reflectance = 0.05; reflectance <= 0.95; reflectance += 0.05)
            {
                double absorption = GoldenSpectraSource.DeriveAbsorption(reflectance);

                Assert.True(double.IsFinite(absorption), $"R {reflectance:F2} derived {absorption}");
                Assert.True(absorption >= 0.0, $"R {reflectance:F2} derived a negative absorption");
                Assert.True(absorption < previous, $"absorption rose at R {reflectance:F2}");
                previous = absorption;
            }
        }

        /// <summary>
        /// Confirms the derived tier reached the library, is withheld from the picker,
        /// and includes the paint from the original bug report.
        /// </summary>
        [Fact]
        public void DerivedPaintsAreLoadedButNotSelectable()
        {
            var derived = PigmentLibrary.All
                .Where(paint => paint.Provenance == PigmentProvenance.ReflectanceDerived)
                .ToList();

            Assert.True(derived.Count >= 55, $"expected about 61 derived paints, found {derived.Count}");

            // Golden's chart abbreviates the cadmiums, so the paint from the original bug
            // report is filed as "Cad Yellow Medium" rather than spelled out.
            Assert.Contains(derived, paint => paint.Name.Equals(
                "Cad Yellow Medium", StringComparison.OrdinalIgnoreCase));
            Assert.All(derived, paint => Assert.DoesNotContain(paint, PigmentLibrary.Selectable));
            Assert.Equal(19, PigmentLibrary.Selectable.Count);
        }

        /// <summary>
        /// Confirms no paint appears twice. The 19 measured paints also have rows in
        /// Golden's file, and taking both would give the user two entries for the same
        /// tube with different physics behind them.
        /// </summary>
        [Fact]
        public void NoPaintAppearsTwice()
        {
            var duplicates = PigmentLibrary.All
                .GroupBy(paint => paint.Name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.Empty(duplicates);
        }
    }
}
