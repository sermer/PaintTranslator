using System;
using System.Collections.Generic;
using System.Reflection;
using Wacton.Unicolour.Datasets;

namespace IngestSpectra
{
    /// <summary>
    /// One paint's measured coefficients as pulled out of Unicolour.
    /// </summary>
    public sealed class ExtractedPigment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedPigment"/> class.
        /// </summary>
        /// <param name="name">The manufacturer's name for the paint.</param>
        /// <param name="colourIndex">The Colour Index generic name, such as PB15.</param>
        /// <param name="absorption">The 38 per-band absorption coefficients.</param>
        /// <param name="scattering">The 38 per-band scattering coefficients.</param>
        public ExtractedPigment(string name, string colourIndex, double[] absorption, double[] scattering)
        {
            Name = name;
            ColourIndex = colourIndex;
            Absorption = absorption;
            Scattering = scattering;
        }

        /// <summary>Gets the manufacturer's name for the paint.</summary>
        public string Name { get; }

        /// <summary>Gets the Colour Index generic name of the paint's pigment.</summary>
        public string ColourIndex { get; }

        /// <summary>Gets the 38 per-band absorption coefficients.</summary>
        public double[] Absorption { get; }

        /// <summary>Gets the 38 per-band scattering coefficients.</summary>
        public double[] Scattering { get; }
    }

    /// <summary>
    /// Reads Roy Berns' two-constant measurements out of Unicolour.Datasets
    /// (MIT License, William Acton, https://github.com/waacton/Unicolour).
    /// <para>
    /// Reflection is necessary rather than lazy: <c>SpectralCoefficients</c> keeps its
    /// array, wavelengths and indexer <c>internal</c>, so there is no public route to
    /// the numbers. This runs once, offline, and its output is verified structurally by
    /// the tests in <c>Tests/PigmentLibraryTests.cs</c>.
    /// </para>
    /// </summary>
    public static class BernsSource
    {
        /// <summary>The reflected member on Pigment holding absorption. From Step 2.</summary>
        private const string AbsorptionMemberName = "K";

        /// <summary>The reflected member on Pigment holding scattering. From Step 2.</summary>
        private const string ScatteringMemberName = "S";

        /// <summary>The reflected array member on SpectralCoefficients. From Step 2.</summary>
        private const string CoefficientArrayName = "Coefficients";

        /// <summary>The paints to extract, with the Colour Index names to record.</summary>
        private static readonly (string Name, string ColourIndex, object Pigment)[] Paints =
        {
            ("Titanium White", "PW6", ArtistPaint.TitaniumWhite),
            ("Bismuth Vanadate Yellow", "PY184", ArtistPaint.BismuthVanadateYellow),
            ("Hansa Yellow Opaque", "PY74", ArtistPaint.HansaYellowOpaque),
            ("Diarylide Yellow", "PY83", ArtistPaint.DiarylideYellow),
            ("C.P. Cadmium Orange", "PO20", ArtistPaint.CadmiumOrange),
            ("Pyrrole Orange", "PO73", ArtistPaint.PyrroleOrange),
            ("C.P. Cadmium Red Light", "PR108", ArtistPaint.CadmiumRedLight),
            ("Pyrrole Red", "PR254", ArtistPaint.PyrroleRed),
            ("Quinacridone Red", "PV19", ArtistPaint.QuinacridoneRed),
            ("Quinacridone Magenta", "PR122", ArtistPaint.QuinacridoneMagenta),
            ("Dioxazine Purple", "PV23", ArtistPaint.DioxazinePurple),
            ("Ultramarine Blue", "PB29", ArtistPaint.UltramarineBlue),
            ("Cobalt Blue", "PB28", ArtistPaint.CobaltBlue),
            ("Phthalo Blue (R.S.)", "PB15", ArtistPaint.PhthaloBlueRedShade),
            ("Phthalo Blue (G.S.)", "PB15", ArtistPaint.PhthaloBlueGreenShade),
            ("Cerulean Blue, Chromium", "PB36", ArtistPaint.CeruleanBlueChromium),
            ("Phthalo Green (B.S.)", "PG7", ArtistPaint.PhthaloGreenBlueShade),
            ("Phthalo Green (Y.S.)", "PG36", ArtistPaint.PhthaloGreenYellowShade),
            ("Bone Black", "PBk9", ArtistPaint.BoneBlack),
        };

        /// <summary>
        /// Extracts every measured paint.
        /// </summary>
        /// <returns>The 19 paints, in the wheel order used by the picker.</returns>
        public static IReadOnlyList<ExtractedPigment> Extract()
        {
            var extracted = new List<ExtractedPigment>(Paints.Length);
            foreach ((string name, string colourIndex, object pigment) in Paints)
            {
                extracted.Add(new ExtractedPigment(
                    name,
                    colourIndex,
                    ReadCoefficients(pigment, AbsorptionMemberName),
                    ReadCoefficients(pigment, ScatteringMemberName)));
            }

            return extracted;
        }

        /// <summary>
        /// Reads one coefficient array off a pigment by reflection.
        /// </summary>
        /// <param name="pigment">The Unicolour pigment instance.</param>
        /// <param name="memberName">The absorption or scattering member name.</param>
        /// <returns>A copy of the 38 per-band coefficients.</returns>
        private static double[] ReadCoefficients(object pigment, string memberName)
        {
            object coefficients = ReadMember(pigment, memberName)
                ?? throw new InvalidOperationException(
                    $"'{memberName}' is null on {pigment.GetType().FullName}. Re-run the Step 2 probe.");

            object array = ReadMember(coefficients, CoefficientArrayName)
                ?? throw new InvalidOperationException(
                    $"'{CoefficientArrayName}' is null on {coefficients.GetType().FullName}. Re-run the Step 2 probe.");

            return (double[])((double[])array).Clone();
        }

        /// <summary>
        /// Reads a field or property of any accessibility off an instance.
        /// </summary>
        /// <param name="instance">The object to read from.</param>
        /// <param name="memberName">The field or property name.</param>
        /// <returns>The member's value, or null when no such member exists.</returns>
        private static object ReadMember(object instance, string memberName)
        {
            const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = instance.GetType();

            FieldInfo field = type.GetField(memberName, All);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            PropertyInfo property = type.GetProperty(memberName, All);
            if (property != null)
            {
                return property.GetValue(instance);
            }

            throw new InvalidOperationException(
                $"No member '{memberName}' on {type.FullName}. Re-run the Step 2 probe and update the constants.");
        }
    }
}
