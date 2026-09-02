using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PaintTranslator.Pigments
{
    /// <summary>
    /// Every paint the application knows, loaded once from the embedded coefficient
    /// resource.
    /// <para>
    /// Measurements are Roy Berns', reaching this project through Unicolour.Datasets
    /// (MIT License, William Acton, https://github.com/waacton/Unicolour), and Golden
    /// Artist Colors' published reflectance data, used with permission.
    /// </para>
    /// </summary>
    public static class PigmentLibrary
    {
        /// <summary>The manifest name of the embedded coefficient resource.</summary>
        private const string ResourceName = "PaintTranslator.Pigments.PigmentData.bin";

        /// <summary>
        /// Initializes the <see cref="PigmentLibrary"/> class by reading the resource.
        /// </summary>
        static PigmentLibrary()
        {
            All = Load();

            var selectable = new List<PigmentCoefficients>(All.Count);
            foreach (PigmentCoefficients paint in All)
            {
                if (paint.Provenance == PigmentProvenance.TwoConstantMeasured)
                {
                    selectable.Add(paint);
                }
            }

            Selectable = selectable;
        }

        /// <summary>
        /// Gets every paint, of every provenance, ordered roughly around the colour
        /// wheel: white, yellows, oranges, reds, magentas, violets, blues, greens, black.
        /// </summary>
        public static IReadOnlyList<PigmentCoefficients> All { get; }

        /// <summary>
        /// Gets the paints offered to the user.
        /// <para>
        /// Reflectance-derived paints are deliberately excluded. Their scattering is
        /// assumed rather than measured, and Golden's drawdowns are over white, so
        /// transparent pigments in that tier are substrate-contaminated. Both are
        /// acceptable in data that can be inspected before it is trusted, and neither
        /// should reach a user unexamined. Promoting that tier is a change to this one
        /// filter.
        /// </para>
        /// </summary>
        public static IReadOnlyList<PigmentCoefficients> Selectable { get; }

        /// <summary>
        /// Reads the embedded coefficient resource.
        /// </summary>
        /// <returns>Every paint in the resource.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the resource is
        /// missing from the assembly, which means the build did not embed it.</exception>
        private static IReadOnlyList<PigmentCoefficients> Load()
        {
            Assembly assembly = typeof(PigmentLibrary).Assembly;
            using Stream stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Embedded resource '{ResourceName}' is missing. Check the EmbeddedResource " +
                    "item in PaintTranslator.csproj.");
            }

            return PigmentData.Read(stream);
        }
    }
}
