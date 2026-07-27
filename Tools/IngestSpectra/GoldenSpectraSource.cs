using System;
using System.Collections.Generic;
using System.Globalization;
using PaintTranslator.Pigments;

namespace IngestSpectra
{
    /// <summary>
    /// Derives Kubelka-Munk absorption coefficients from Golden's published reflectance
    /// measurements, for the paints Roy Berns did not measure.
    /// <para>
    /// Scattering is assumed to be 1 at every band. That asserts the paint scatters like
    /// titanium white — wrong for transparent pigments, correct in scale because Berns
    /// normalises white's scattering to 1, and a strict improvement on inferring
    /// scattering from a paint's luminance. Paints derived this way are withheld from
    /// the picker for exactly this reason.
    /// </para>
    /// </summary>
    public static class GoldenSpectraSource
    {
        /// <summary>The first row of paint data, one-based, per the file's layout.</summary>
        private const int FirstDataRow = 3;

        /// <summary>The last row of paint data, one-based.</summary>
        private const int LastDataRow = 80;

        /// <summary>The zero-based column of the paint name, column B.</summary>
        private const int NameColumn = 1;

        /// <summary>
        /// The zero-based column of the first reflectance value, column G, which the
        /// header row labels 400. Column F is an empty spacer between the L*a*b* block
        /// and the spectrum; the 31 bands then run contiguously from G to AK.
        /// </summary>
        private const int FirstReflectanceColumn = 6;

        /// <summary>How many reflectance bands the file carries, 400-700nm at 10nm.</summary>
        private const int MeasuredBandCount = 31;

        /// <summary>The wavelength of the file's first reflectance band, in nanometres.</summary>
        private const int MeasuredStartNm = 400;

        /// <summary>
        /// Golden's spellings of paints the measured tier already holds under a different
        /// name. Skipping these is what stops the same tube appearing twice with two
        /// different physics behind it — once from Berns' two-constant measurement and
        /// once from a reflectance-only derivation.
        /// <para>
        /// Eight of the nineteen measured paints match Golden's spelling exactly and need
        /// no entry here. Two more, Titanium White and Hansa Yellow Opaque, have no row in
        /// Golden's file at all — it carries Hansa Yellow Light and Medium, which are
        /// different tubes and are correctly kept. That leaves these nine.
        /// </para>
        /// </summary>
        private static readonly HashSet<string> MeasuredTierAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Cadmium Orange",         // C.P. Cadmium Orange, PO20
                "Cad Red Light",          // C.P. Cadmium Red Light, PR108
                "Quin Red",               // Quinacridone Red, PV19
                "Quin Magenta",           // Quinacridone Magenta, PR122
                "Phthalo Blue RS",        // Phthalo Blue (R.S.), PB15
                "Phthalo Blue GS",        // Phthalo Blue (G.S.), PB15
                "Cerulean Blue Chromium", // Cerulean Blue, Chromium, PB36
                "Phthalo Green BS",       // Phthalo Green (B.S.), PG7
                "Phthalo Green YS",       // Phthalo Green (Y.S.), PG36
            };

        /// <summary>
        /// Reads the workbook and derives coefficients for every paint not already
        /// measured.
        /// </summary>
        /// <param name="zipPath">The downloaded GoldenSpectra archive.</param>
        /// <param name="skipNames">Names already present from the measured tier, which
        /// must not be duplicated with inferior data.</param>
        /// <returns>The derived paints.</returns>
        public static IReadOnlyList<PigmentCoefficients> Derive(string zipPath, ISet<string> skipNames)
        {
            IReadOnlyList<IReadOnlyList<string>> rows = SpreadsheetReader.ReadSheet(zipPath);
            var derived = new List<PigmentCoefficients>();

            for (int rowNumber = FirstDataRow; rowNumber <= LastDataRow && rowNumber <= rows.Count; rowNumber++)
            {
                IReadOnlyList<string> row = rows[rowNumber - 1];
                if (row.Count <= NameColumn)
                {
                    continue;
                }

                string name = row[NameColumn].Trim();
                if (name.Length == 0 || skipNames.Contains(name) || MeasuredTierAliases.Contains(name))
                {
                    continue;
                }

                var measured = new double[MeasuredBandCount];
                bool complete = true;
                for (int band = 0; band < MeasuredBandCount; band++)
                {
                    int column = FirstReflectanceColumn + band;
                    if (column >= row.Count
                        || !double.TryParse(row[column], NumberStyles.Float, CultureInfo.InvariantCulture,
                            out double percent))
                    {
                        complete = false;
                        break;
                    }

                    measured[band] = percent / 100.0;
                }

                if (!complete)
                {
                    continue;
                }

                double[] resampled = Resample(measured);
                var absorption = new double[SpectralBands.Count];
                for (int band = 0; band < SpectralBands.Count; band++)
                {
                    absorption[band] = DeriveAbsorption(resampled[band]);
                }

                derived.Add(new PigmentCoefficients(
                    name, string.Empty, PigmentProvenance.ReflectanceDerived, absorption, null));
            }

            return derived;
        }

        /// <summary>
        /// Resamples the file's 400-700nm grid onto this project's 380-750nm one,
        /// holding the endpoint values outside the measured range.
        /// </summary>
        /// <param name="measured">The 31 measured reflectances.</param>
        /// <returns>Reflectance on this project's band layout.</returns>
        internal static double[] Resample(double[] measured)
        {
            if (measured == null)
            {
                throw new ArgumentNullException(nameof(measured));
            }
            if (measured.Length != MeasuredBandCount)
            {
                throw new ArgumentException(
                    $"Expected {MeasuredBandCount} measured bands.", nameof(measured));
            }

            var resampled = new double[SpectralBands.Count];
            for (int band = 0; band < SpectralBands.Count; band++)
            {
                int wavelength = SpectralBands.StartWavelengthNm + (band * SpectralBands.WavelengthIntervalNm);
                int measuredIndex = (wavelength - MeasuredStartNm) / SpectralBands.WavelengthIntervalNm;

                // Both grids are 10nm, so every target band either lands exactly on a
                // measured one or falls outside the measured range, where the nearest
                // endpoint is held. No interpolation is needed or attempted.
                resampled[band] = measured[Math.Clamp(measuredIndex, 0, MeasuredBandCount - 1)];
            }

            return resampled;
        }

        /// <summary>
        /// Derives one band's absorption from a measured reflectance.
        /// </summary>
        /// <param name="measuredReflectance">The measured reflectance, in [0, 1].</param>
        /// <returns>The absorption coefficient, with scattering taken as 1.</returns>
        internal static double DeriveAbsorption(double measuredReflectance)
        {
            // Berns' coefficients were fitted with the Saunderson correction applied and
            // this raw reflectance has not been, so it has to be removed before the two
            // tiers can live on the same footing.
            double internalReflectance = KubelkaMunk.InverseSaunderson(measuredReflectance);

            // Guard the inversion's poles rather than the reflectance: at exactly 0 the
            // absorption is infinite and at exactly 1 it is zero, and measured data can
            // reach both through rounding.
            internalReflectance = Math.Clamp(internalReflectance, 1e-6, 1.0 - 1e-9);

            return (1.0 - internalReflectance) * (1.0 - internalReflectance)
                / (2.0 * internalReflectance);
        }
    }
}
