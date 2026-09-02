using System;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// How well a paint's coefficients are known.
    /// </summary>
    public enum PigmentProvenance
    {
        /// <summary>
        /// Absorption and scattering were each measured independently. This is Roy
        /// Berns' data and the only tier offered to the user.
        /// </summary>
        TwoConstantMeasured,

        /// <summary>
        /// Only reflectance was measured; scattering is assumed to be 1 at every band,
        /// which asserts the paint scatters like titanium white. Correct in scale and
        /// wrong for transparent pigments, so this tier is withheld from the picker.
        /// </summary>
        ReflectanceDerived,
    }

    /// <summary>
    /// A paint, as a pair of Kubelka-Munk coefficient curves.
    /// <para>
    /// There is deliberately no colour on this type. A paint's appearance is computed
    /// from these curves at a concentration, which is what makes a mass tone and its
    /// tint the same data evaluated twice rather than two values that can disagree.
    /// Storing a colour here is what produced the bug this pipeline replaces.
    /// </para>
    /// </summary>
    public sealed class PigmentCoefficients
    {
        /// <summary>
        /// The scattering curve shared by every <see cref="PigmentProvenance.ReflectanceDerived"/>
        /// paint, which is 1 at every band by construction.
        /// </summary>
        private static readonly double[] UnitScattering = CreateUnitScattering();

        /// <summary>
        /// Initializes a new instance of the <see cref="PigmentCoefficients"/> class.
        /// </summary>
        /// <param name="name">The manufacturer's name for the paint.</param>
        /// <param name="colourIndex">The Colour Index generic name, such as PB15.</param>
        /// <param name="provenance">How well the coefficients are known.</param>
        /// <param name="absorption">The per-band absorption coefficients, length
        /// <see cref="SpectralBands.Count"/>. Taken by reference and never copied, so
        /// callers must not mutate it afterwards.</param>
        /// <param name="scattering">The per-band scattering coefficients, or null for a
        /// reflectance-derived paint, which uses a shared unit curve.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/>,
        /// <paramref name="colourIndex"/> or <paramref name="absorption"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when an array is the wrong length.</exception>
        public PigmentCoefficients(
            string name,
            string colourIndex,
            PigmentProvenance provenance,
            double[] absorption,
            double[] scattering)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ColourIndex = colourIndex ?? throw new ArgumentNullException(nameof(colourIndex));
            Provenance = provenance;

            if (absorption == null)
            {
                throw new ArgumentNullException(nameof(absorption));
            }
            if (absorption.Length != SpectralBands.Count)
            {
                throw new ArgumentException(
                    $"Absorption must have {SpectralBands.Count} bands.", nameof(absorption));
            }
            if (scattering != null && scattering.Length != SpectralBands.Count)
            {
                throw new ArgumentException(
                    $"Scattering must have {SpectralBands.Count} bands.", nameof(scattering));
            }

            Absorption = absorption;
            Scattering = scattering ?? UnitScattering;
        }

        /// <summary>Gets the manufacturer's name for the paint.</summary>
        public string Name { get; }

        /// <summary>
        /// Gets the Colour Index generic name of the paint's pigment, such as PB15.
        /// Single-pigment paints mix predictably; this is what lets the interface say so.
        /// </summary>
        public string ColourIndex { get; }

        /// <summary>Gets how well the coefficients are known.</summary>
        public PigmentProvenance Provenance { get; }

        /// <summary>
        /// Gets the per-band absorption coefficients. Never mutate: instances are shared
        /// across every mixture in the application.
        /// </summary>
        internal double[] Absorption { get; }

        /// <summary>
        /// Gets the per-band scattering coefficients. Never mutate: reflectance-derived
        /// paints all share one array.
        /// </summary>
        internal double[] Scattering { get; }

        /// <summary>
        /// Returns the paint name, so list controls display it directly.
        /// </summary>
        /// <returns>The paint's name.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Builds the shared unit scattering curve.
        /// </summary>
        /// <returns>An array of ones, one per band.</returns>
        private static double[] CreateUnitScattering()
        {
            var scattering = new double[SpectralBands.Count];
            for (int band = 0; band < SpectralBands.Count; band++)
            {
                scattering[band] = 1.0;
            }

            return scattering;
        }
    }
}
