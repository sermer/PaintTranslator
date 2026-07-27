namespace PaintTranslator.Pigments
{
    /// <summary>
    /// The wavelength layout every spectrum in this project uses. One layout, fixed:
    /// coefficients measured on a different grid are resampled onto this one during
    /// ingest rather than carried alongside it, because two layouts in memory means
    /// every mixing operation has to check which one it has.
    /// </summary>
    public static class SpectralBands
    {
        /// <summary>The centre wavelength of the first band, in nanometres.</summary>
        public const int StartWavelengthNm = 380;

        /// <summary>The spacing between band centres, in nanometres.</summary>
        public const int WavelengthIntervalNm = 10;

        /// <summary>The number of bands, putting the last band centre at 750nm.</summary>
        public const int Count = 38;

        // The CIE 1931 standard observer weighted by the D65 illuminant, one weight per
        // band, from spectral.js v3 (MIT License, Ronald van Wijnen). D65 is chosen
        // deliberately: this application shows paint on a screen. Unicolour's
        // ArtistPaint configuration renders under D50 instead, which is why the parity
        // test has to build its own configuration rather than use the default.

        /// <summary>The X colour-matching weights, one per band.</summary>
        internal static readonly double[] ObserverX =
        {
            6.46919989576e-05, 0.0002194098998132, 0.0011205743509343, 0.0037666134117111,
            0.011880553603799, 0.0232864424191771, 0.0345594181969747, 0.0372237901162006,
            0.0324183761091486, 0.021233205609381, 0.0104909907685421, 0.0032958375797931,
            0.0005070351633801, 0.0009486742057141, 0.0062737180998318, 0.0168646241897775,
            0.028689649025981, 0.0426748124691731, 0.0562547481311377, 0.0694703972677158,
            0.0830531516998291, 0.0861260963002257, 0.0904661376847769, 0.0850038650591277,
            0.0709066691074488, 0.0506288916373645, 0.035473961885264, 0.0214682102597065,
            0.0125164567619117, 0.0068045816390165, 0.0034645657946526, 0.0014976097506959,
            0.000769700480928, 0.0004073680581315, 0.0001690104031614, 9.52245150365e-05,
            4.90309872958e-05, 1.99961492222e-05,
        };

        /// <summary>The Y colour-matching weights, one per band. These are the
        /// luminance weights the renderer normalises its integration by.</summary>
        internal static readonly double[] ObserverY =
        {
            1.844289444e-06, 6.2053235865e-06, 3.10096046799e-05, 0.0001047483849269,
            0.0003536405299538, 0.0009514714056444, 0.0022822631748318, 0.004207329043473,
            0.0066887983719014, 0.0098883960193565, 0.0152494514496311, 0.0214183109449723,
            0.0334229301575068, 0.0513100134918512, 0.070402083939949, 0.0878387072603517,
            0.0942490536184085, 0.0979566702718931, 0.0941521856862608, 0.0867810237486753,
            0.0788565338632013, 0.0635267026203555, 0.05374141675682, 0.042646064357412,
            0.0316173492792708, 0.020885205921391, 0.0138601101360152, 0.0081026402038399,
            0.004630102258803, 0.0024913800051319, 0.0012593033677378, 0.000541646522168,
            0.0002779528920067, 0.0001471080673854, 6.10327472927e-05, 3.43873229523e-05,
            1.77059860053e-05, 7.220974913e-06,
        };

        /// <summary>The Z colour-matching weights, one per band.</summary>
        internal static readonly double[] ObserverZ =
        {
            0.000305017147638, 0.0010368066663574, 0.0053131363323992, 0.0179543925899536,
            0.0570775815345485, 0.113651618936287, 0.17335872618355, 0.196206575558657,
            0.186082370706296, 0.139950475383207, 0.0891745294268649, 0.0478962113517075,
            0.0281456253957952, 0.0161376622950514, 0.0077591019215214, 0.0042961483736618,
            0.0020055092122156, 0.0008614711098802, 0.0003690387177652, 0.0001914287288574,
            0.0001495555858975, 9.23109285104e-05, 6.81349182337e-05, 2.88263655696e-05,
            1.57671820553e-05, 3.9406041027e-06, 1.584012587e-06, 0.0,
            0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0,
            0.0, 0.0,
        };
    }
}
