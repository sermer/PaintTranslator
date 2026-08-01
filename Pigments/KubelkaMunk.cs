using System;
using System.Collections.Generic;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// Mixes paints by two-constant Kubelka-Munk theory: absorption and scattering are
    /// tracked separately per wavelength, each mixes linearly with concentration, and
    /// the pair is inverted to a reflectance afterwards.
    /// <para>
    /// The separation is the point. Single-constant theory mixes the K/S ratio
    /// linearly, which is only valid when every pigment scatters identically — titanium
    /// white and phthalo blue emphatically do not, and assuming otherwise is what makes
    /// a reconstructed-spectrum mixer put white at many times its real influence.
    /// </para>
    /// <para>
    /// Concentrations are volume fractions, normalised here to sum to 1, and nothing
    /// scales them. Tinting strength is not a stored number: it emerges from how large a
    /// paint's absorption is relative to its scattering. Phthalo blue overwhelms a
    /// yellow because its measured absorption is enormous.
    /// </para>
    /// </summary>
    public static class KubelkaMunk
    {
        /// <summary>
        /// The Saunderson surface-reflection coefficient, k1: the share of light
        /// reflected at the film-air boundary before reaching any pigment.
        /// </summary>
        public const double SurfaceReflection = 0.03;

        /// <summary>
        /// The Saunderson internal-reflection coefficient, k2: the share of light
        /// reflected back into the film at that same boundary from the inside.
        /// </summary>
        public const double InternalReflection = 0.65;

        /// <summary>
        /// The smallest scattering a mixture is allowed to have.
        /// <para>
        /// The floor belongs here rather than on reflectance. Flooring reflectance lets
        /// K/S run to astronomical values and the inversion then cancels in ways nobody
        /// designed — the previous pipeline's 1e-15 floor is what made a half-and-half
        /// black and white land at L* 68. Flooring scattering instead keeps the
        /// inversion inside the regime where it means something. The value sits below
        /// the smallest scattering any measured paint exhibits, so it binds only on
        /// mixtures of wholly transparent pigments.
        /// </para>
        /// </summary>
        private const double MinimumScattering = 1e-6;

        /// <summary>
        /// Mixes paints and writes the mixture's reflectance spectrum.
        /// </summary>
        /// <param name="pigments">The participating paints.</param>
        /// <param name="concentrations">Each paint's share of the mixture,
        /// index-aligned with <paramref name="pigments"/>. Shares are relative and are
        /// normalised internally, so they need not sum to 1.</param>
        /// <param name="reflectance">The caller-owned buffer the spectrum is written
        /// into, length <see cref="SpectralBands.Count"/>. Caller-owned so that a search
        /// evaluating tens of thousands of mixtures allocates nothing.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the lists are empty or differ
        /// in length, when the buffer is the wrong size, or when the concentrations are
        /// negative or sum to zero.</exception>
        public static void Mix(
            IReadOnlyList<PigmentCoefficients> pigments,
            IReadOnlyList<double> concentrations,
            double[] reflectance)
        {
            if (pigments == null)
            {
                throw new ArgumentNullException(nameof(pigments));
            }
            if (concentrations == null)
            {
                throw new ArgumentNullException(nameof(concentrations));
            }
            if (reflectance == null)
            {
                throw new ArgumentNullException(nameof(reflectance));
            }
            if (pigments.Count == 0 || pigments.Count != concentrations.Count)
            {
                throw new ArgumentException(
                    "Each paint needs exactly one concentration.", nameof(concentrations));
            }
            if (reflectance.Length != SpectralBands.Count)
            {
                throw new ArgumentException(
                    $"The reflectance buffer must have {SpectralBands.Count} bands.", nameof(reflectance));
            }

            double total = 0.0;
            for (int i = 0; i < concentrations.Count; i++)
            {
                if (concentrations[i] < 0.0 || double.IsNaN(concentrations[i]))
                {
                    throw new ArgumentException(
                        "Concentrations must be non-negative.", nameof(concentrations));
                }

                total += concentrations[i];
            }

            if (total <= 0.0)
            {
                throw new ArgumentException(
                    "Concentrations must not all be zero.", nameof(concentrations));
            }

            for (int band = 0; band < SpectralBands.Count; band++)
            {
                double absorption = 0.0;
                double scattering = 0.0;
                for (int i = 0; i < pigments.Count; i++)
                {
                    double share = concentrations[i] / total;
                    absorption += share * pigments[i].Absorption[band];
                    scattering += share * pigments[i].Scattering[band];
                }

                reflectance[band] = Invert(absorption, scattering);
            }
        }

        /// <summary>
        /// Mixes palette entries addressed by index and optionally folds one fixed
        /// mother-colour fraction into the result. Candidate sampling calls this tens
        /// of thousands of times; indexing the original palette avoids allocating a
        /// new pigment array (and, for a mother colour, two more arrays) per sample.
        /// </summary>
        internal static void MixIndexed(
            IReadOnlyList<PigmentCoefficients> palette,
            IReadOnlyList<int> indices,
            IReadOnlyList<double> concentrations,
            int blendIndex,
            double blendFraction,
            double[] reflectance)
        {
            if (palette == null || indices == null || concentrations == null || reflectance == null)
            {
                throw new ArgumentNullException();
            }
            if (indices.Count == 0 || indices.Count != concentrations.Count)
            {
                throw new ArgumentException("Each paint index needs exactly one concentration.");
            }
            if (reflectance.Length != SpectralBands.Count)
            {
                throw new ArgumentException(
                    $"The reflectance buffer must have {SpectralBands.Count} bands.", nameof(reflectance));
            }
            if (blendFraction < 0.0 || blendFraction > 1.0 || double.IsNaN(blendFraction) ||
                (blendIndex >= 0 && blendIndex >= palette.Count))
            {
                throw new ArgumentOutOfRangeException(nameof(blendFraction));
            }

            int existingBlendSlot = -1;
            double total = 0.0;
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] < 0 || indices[i] >= palette.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(indices));
                }
                if (concentrations[i] < 0.0 || double.IsNaN(concentrations[i]))
                {
                    throw new ArgumentException("Concentrations must be non-negative.", nameof(concentrations));
                }

                double adjusted = concentrations[i];
                if (blendIndex >= 0 && blendFraction > 0.0)
                {
                    adjusted *= 1.0 - blendFraction;
                    if (indices[i] == blendIndex)
                    {
                        existingBlendSlot = i;
                        adjusted += blendFraction;
                    }
                }

                total += adjusted;
            }

            bool appendBlend = blendIndex >= 0 && blendFraction > 0.0 && existingBlendSlot < 0;
            if (appendBlend)
            {
                total += blendFraction;
            }
            if (total <= 0.0)
            {
                throw new ArgumentException("Concentrations must not all be zero.", nameof(concentrations));
            }

            for (int band = 0; band < SpectralBands.Count; band++)
            {
                double absorption = 0.0;
                double scattering = 0.0;
                for (int i = 0; i < indices.Count; i++)
                {
                    double adjusted = concentrations[i];
                    if (blendIndex >= 0 && blendFraction > 0.0)
                    {
                        adjusted *= 1.0 - blendFraction;
                        if (i == existingBlendSlot)
                        {
                            adjusted += blendFraction;
                        }
                    }

                    double share = adjusted / total;
                    PigmentCoefficients pigment = palette[indices[i]];
                    absorption += share * pigment.Absorption[band];
                    scattering += share * pigment.Scattering[band];
                }

                if (appendBlend)
                {
                    double share = blendFraction / total;
                    absorption += share * palette[blendIndex].Absorption[band];
                    scattering += share * palette[blendIndex].Scattering[band];
                }

                reflectance[band] = Invert(absorption, scattering);
            }
        }

        /// <summary>
        /// Sums every paint's coefficients, giving the baseline a wedge mix adds to.
        /// </summary>
        /// <param name="pigments">The paints to sum.</param>
        /// <param name="absorption">The buffer the absorption totals are written into,
        /// length <see cref="SpectralBands.Count"/>.</param>
        /// <param name="scattering">The buffer the scattering totals are written into,
        /// length <see cref="SpectralBands.Count"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when a buffer is the wrong size.</exception>
        public static void SumCoefficients(
            IReadOnlyList<PigmentCoefficients> pigments, double[] absorption, double[] scattering)
        {
            if (pigments == null)
            {
                throw new ArgumentNullException(nameof(pigments));
            }
            if (absorption == null)
            {
                throw new ArgumentNullException(nameof(absorption));
            }
            if (scattering == null)
            {
                throw new ArgumentNullException(nameof(scattering));
            }
            if (absorption.Length != SpectralBands.Count || scattering.Length != SpectralBands.Count)
            {
                throw new ArgumentException(
                    $"The coefficient buffers must have {SpectralBands.Count} bands.", nameof(absorption));
            }

            Array.Clear(absorption, 0, absorption.Length);
            Array.Clear(scattering, 0, scattering.Length);

            foreach (PigmentCoefficients pigment in pigments)
            {
                for (int band = 0; band < SpectralBands.Count; band++)
                {
                    absorption[band] += pigment.Absorption[band];
                    scattering[band] += pigment.Scattering[band];
                }
            }
        }

        /// <summary>
        /// Mixes a colour wheel wedge: every paint at one shared concentration, plus a
        /// surplus for the two paints flanking the point.
        /// <para>
        /// This is <see cref="Mix"/> with the sum rearranged, not approximated. Because
        /// every paint carries the same centre share, that part of the sum is the same
        /// for every pixel and is hoisted into <paramref name="baselineAbsorption"/> and
        /// <paramref name="baselineScattering"/>. What remains per pixel is two paints
        /// rather than all of them, which is what makes a full wheel interactive. The
        /// concentrations still sum to 1 and nothing is scaled by anything.
        /// </para>
        /// </summary>
        /// <param name="baselineAbsorption">Every paint's absorption summed, from
        /// <see cref="SumCoefficients"/>.</param>
        /// <param name="baselineScattering">Every paint's scattering summed.</param>
        /// <param name="centreShare">The concentration every paint holds.</param>
        /// <param name="lower">The paint anticlockwise of the point.</param>
        /// <param name="lowerSurplus">The lower paint's concentration above the centre share.</param>
        /// <param name="upper">The paint clockwise of the point.</param>
        /// <param name="upperSurplus">The upper paint's concentration above the centre share.</param>
        /// <param name="reflectance">The caller-owned buffer the spectrum is written into.</param>
        public static void MixWedge(
            double[] baselineAbsorption,
            double[] baselineScattering,
            double centreShare,
            PigmentCoefficients lower,
            double lowerSurplus,
            PigmentCoefficients upper,
            double upperSurplus,
            double[] reflectance)
        {
            for (int band = 0; band < SpectralBands.Count; band++)
            {
                double absorption = (centreShare * baselineAbsorption[band])
                    + (lowerSurplus * lower.Absorption[band])
                    + (upperSurplus * upper.Absorption[band]);
                double scattering = (centreShare * baselineScattering[band])
                    + (lowerSurplus * lower.Scattering[band])
                    + (upperSurplus * upper.Scattering[band]);

                reflectance[band] = Invert(absorption, scattering);
            }
        }

        /// <summary>
        /// Turns one band's mixed coefficients into a measured reflectance.
        /// </summary>
        /// <param name="absorption">The band's mixed absorption.</param>
        /// <param name="scattering">The band's mixed scattering.</param>
        /// <returns>The measured reflectance at that band.</returns>
        private static double Invert(double absorption, double scattering)
        {
            double ratio = absorption / Math.Max(scattering, MinimumScattering);

            // The Kubelka-Munk inversion: the internal reflectance of an opaque
            // layer whose absorption-to-scattering ratio is this.
            double internalReflectance = 1.0 + ratio - Math.Sqrt(ratio * ratio + 2.0 * ratio);

            return Saunderson(internalReflectance);
        }

        /// <summary>
        /// Applies the Saunderson correction, turning the internal reflectance the
        /// Kubelka-Munk inversion produces into what a spectrophotometer would read off
        /// the film's surface.
        /// </summary>
        /// <param name="internalReflectance">The internal reflectance, in [0, 1].</param>
        /// <returns>The measured reflectance.</returns>
        public static double Saunderson(double internalReflectance)
        {
            // Specular-excluded: the light reflected straight off the film's surface is
            // not added back in. Berns fitted these coefficients that way, and the
            // parity gate against Unicolour is what establishes it — including the
            // leading k1 term instead lifts every dark paint toward a flat 3% grey,
            // which put Bone Black at L* 24.7 against its measured 11.4 and left
            // Dioxazine Purple with no hue at all.
            return (1.0 - SurfaceReflection) * (1.0 - InternalReflection) * internalReflectance
                / (1.0 - (InternalReflection * internalReflectance));
        }

        /// <summary>
        /// Removes the Saunderson correction, recovering internal reflectance from a
        /// measured one. The exact algebraic inverse of <see cref="Saunderson"/>; the
        /// ingest uses this on raw measurements so that derived coefficients live on the
        /// same footing as the fitted ones.
        /// </summary>
        /// <param name="measuredReflectance">The measured reflectance, in [0, 1].</param>
        /// <returns>The internal reflectance.</returns>
        public static double InverseSaunderson(double measuredReflectance)
        {
            double numerator = (1.0 - SurfaceReflection) * (1.0 - InternalReflection);

            return measuredReflectance / (numerator + (InternalReflection * measuredReflectance));
        }
    }
}
