using System.Collections.Generic;
using Wacton.Unicolour;
using Wacton.Unicolour.Datasets;

namespace PaintTranslator.Data
{
    /// <summary>
    /// A paint whose optical behaviour comes from spectrophotometer measurements rather
    /// than from a single colour value. The measurement is a pair of Kubelka-Munk
    /// coefficients per wavelength — absorption K and scattering S — which is what makes
    /// a mass tone and its tint fall out of the same data: phthalo blue reads nearly
    /// black from the tube and brilliant cyan in white because it absorbs strongly and
    /// scatters weakly, and only a measured scattering coefficient records that.
    /// </summary>
    public sealed class MeasuredPaint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeasuredPaint"/> class.
        /// </summary>
        /// <param name="name">The manufacturer's name for the paint.</param>
        /// <param name="colourIndex">The Colour Index generic name of the paint's pigment, such as PB15.</param>
        /// <param name="pigment">The paint's measured Kubelka-Munk coefficients.</param>
        internal MeasuredPaint(string name, string colourIndex, Pigment pigment)
        {
            Name = name;
            ColourIndex = colourIndex;
            Pigment = pigment;
        }

        /// <summary>
        /// Gets the manufacturer's name for the paint.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the Colour Index generic name of the paint's pigment, such as PB15.
        /// Single-pigment paints mix predictably; this is what lets the interface say so.
        /// </summary>
        public string ColourIndex { get; }

        /// <summary>
        /// Gets the paint's measured Kubelka-Munk coefficients.
        /// </summary>
        internal Pigment Pigment { get; }

        /// <summary>
        /// Returns the paint name, so list controls display it directly.
        /// </summary>
        /// <returns>The paint's name.</returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Provides the Golden Heavy Body acrylics for which two-constant Kubelka-Munk
    /// measurements are available.
    /// <para>
    /// The measurements are Roy Berns' and reach this project through Unicolour.Datasets
    /// (MIT License, William Acton, https://github.com/waacton/Unicolour). Every paint
    /// here is a single-pigment colour measured as a mass tone, which is also why this
    /// palette does not repeat the mass-tone-versus-tint confusion in
    /// <see cref="GoldenPalette"/>, where some entries are tube colours and others are
    /// tints of them.
    /// </para>
    /// <para>
    /// Nineteen paints is small next to the full range, but it is a deliberate limited
    /// palette: a warm and cool of each primary, two greens, a violet, white and black.
    /// Accuracy here is worth more than choice, because a mixture predicted from measured
    /// coefficients lands where it says it will and one predicted from a reconstructed
    /// spectrum often does not.
    /// </para>
    /// </summary>
    public static class MeasuredPalette
    {
        /// <summary>
        /// Gets every measured paint, ordered roughly around the colour wheel so wheel
        /// wedges built from this list read as a spectrum: white, yellows, oranges, reds,
        /// magentas, violets, blues, greens, black.
        /// </summary>
        public static IReadOnlyList<MeasuredPaint> Paints { get; } = new[]
        {
            new MeasuredPaint("Titanium White", "PW6", ArtistPaint.TitaniumWhite),
            new MeasuredPaint("Bismuth Vanadate Yellow", "PY184", ArtistPaint.BismuthVanadateYellow),
            new MeasuredPaint("Hansa Yellow Opaque", "PY74", ArtistPaint.HansaYellowOpaque),
            new MeasuredPaint("Diarylide Yellow", "PY83", ArtistPaint.DiarylideYellow),
            new MeasuredPaint("C.P. Cadmium Orange", "PO20", ArtistPaint.CadmiumOrange),
            new MeasuredPaint("Pyrrole Orange", "PO73", ArtistPaint.PyrroleOrange),
            new MeasuredPaint("C.P. Cadmium Red Light", "PR108", ArtistPaint.CadmiumRedLight),
            new MeasuredPaint("Pyrrole Red", "PR254", ArtistPaint.PyrroleRed),
            new MeasuredPaint("Quinacridone Red", "PV19", ArtistPaint.QuinacridoneRed),
            new MeasuredPaint("Quinacridone Magenta", "PR122", ArtistPaint.QuinacridoneMagenta),
            new MeasuredPaint("Dioxazine Purple", "PV23", ArtistPaint.DioxazinePurple),
            new MeasuredPaint("Ultramarine Blue", "PB29", ArtistPaint.UltramarineBlue),
            new MeasuredPaint("Cobalt Blue", "PB28", ArtistPaint.CobaltBlue),
            new MeasuredPaint("Phthalo Blue (R.S.)", "PB15", ArtistPaint.PhthaloBlueRedShade),
            new MeasuredPaint("Phthalo Blue (G.S.)", "PB15", ArtistPaint.PhthaloBlueGreenShade),
            new MeasuredPaint("Cerulean Blue, Chromium", "PB36", ArtistPaint.CeruleanBlueChromium),
            new MeasuredPaint("Phthalo Green (B.S.)", "PG7", ArtistPaint.PhthaloGreenBlueShade),
            new MeasuredPaint("Phthalo Green (Y.S.)", "PG36", ArtistPaint.PhthaloGreenYellowShade),
            new MeasuredPaint("Bone Black", "PBk9", ArtistPaint.BoneBlack),
        };
    }
}
