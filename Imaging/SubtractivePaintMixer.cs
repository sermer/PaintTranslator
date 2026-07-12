using System;
using System.Collections.Generic;
using System.Drawing;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// A paint color prepared for Kubelka-Munk mixing: its absorption-to-scattering
    /// ratio in each wavelength band, plus the luminance-derived strength that scales
    /// how hard the paint pulls on a mixture. Instances are opaque handles produced by
    /// <see cref="SubtractivePaintMixer.ToSpectrum"/> and consumed by the mixer's
    /// weighted-mix overloads; converting each paint once and reusing the spectrum
    /// keeps repeated mixing cheap.
    /// </summary>
    public sealed class PaintSpectrum
    {
        // Kubelka-Munk K/S (absorption/scattering) values, one per wavelength band.
        internal readonly double[] Ks;

        // Luminance of the paint's reflectance spectrum; concentrations are scaled
        // by this so light, weakly-tinting paints are not overwhelmed by dark ones.
        internal readonly double Strength;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaintSpectrum"/> class.
        /// </summary>
        /// <param name="ks">The Kubelka-Munk K/S value for each wavelength band.</param>
        /// <param name="strength">The luminance-derived tinting-strength weight.</param>
        internal PaintSpectrum(double[] ks, double strength)
        {
            Ks = ks;
            Strength = strength;
        }
    }

    /// <summary>
    /// Approximates subtractive (pigment) color mixing with Kubelka-Munk theory over
    /// reconstructed reflectance spectra. Each sRGB color is expanded to a 38-band
    /// reflectance spectrum (wavelengths 380-730nm), spectra are combined through the
    /// Kubelka-Munk absorption/scattering model with luminance-weighted concentrations,
    /// and the mixed spectrum is integrated back to sRGB through the CIE standard
    /// observer under D65 light. Working per wavelength instead of per RGB channel is
    /// what makes yellow and blue mix toward green the way physical paints do: a blue
    /// paint's spectrum still reflects some green light even when its sRGB green
    /// channel is nearly zero, and that shared reflectance is all that survives the mix.
    /// Spectral data and mixing model are ported from spectral.js v3
    /// (https://github.com/rvanwijnen/spectral.js, MIT License, Ronald van Wijnen).
    /// </summary>
    public static class SubtractivePaintMixer
    {
        /// <summary>
        /// The number of wavelength bands in a reflectance spectrum (380-730nm in 10nm steps).
        /// </summary>
        public const int BandCount = 38;

        // Floor keeping reflectance positive so K/S stays finite for channels a
        // color does not reflect at all; real paints always reflect a little light.
        private const double MinReflectance = 1e-15;

        /// <summary>
        /// Mixes two paint colors subtractively.
        /// </summary>
        /// <param name="a">The first paint color.</param>
        /// <param name="b">The second paint color.</param>
        /// <param name="weightOfB">The share of <paramref name="b"/> in the mix, from 0 (all a) to 1 (all b).</param>
        /// <returns>The mixed color with full alpha.</returns>
        public static Color Mix(Color a, Color b, double weightOfB)
        {
            double w = Math.Clamp(weightOfB, 0.0, 1.0);
            return Mix(
                new[] { ToSpectrum(a), ToSpectrum(b) },
                new[] { 1.0 - w, w });
        }

        /// <summary>
        /// Mixes any number of paints subtractively in the given proportions.
        /// </summary>
        /// <param name="paints">The spectra of the participating paints.</param>
        /// <param name="weights">Each paint's share of the mix, index-aligned with
        /// <paramref name="paints"/>; shares are relative, so they need not sum to 1.</param>
        /// <returns>The mixed color with full alpha.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="paints"/> or <paramref name="weights"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the lists are empty or their lengths differ.</exception>
        public static Color Mix(IReadOnlyList<PaintSpectrum> paints, IReadOnlyList<double> weights)
        {
            if (paints == null)
            {
                throw new ArgumentNullException(nameof(paints));
            }
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }
            if (paints.Count == 0 || paints.Count != weights.Count)
            {
                throw new ArgumentException("Each paint needs exactly one mixing weight.", nameof(weights));
            }

            // Each paint's effective concentration is its squared share scaled by
            // its tinting strength; K/S values then average linearly under those
            // concentrations, which is the Kubelka-Munk mixing rule.
            var ksMix = new double[BandCount];
            double totalConcentration = 0.0;
            for (int i = 0; i < paints.Count; i++)
            {
                double concentration = weights[i] * weights[i] * paints[i].Strength;
                if (concentration <= 0.0)
                {
                    continue;
                }
                totalConcentration += concentration;

                double[] ks = paints[i].Ks;
                for (int band = 0; band < BandCount; band++)
                {
                    ksMix[band] += concentration * ks[band];
                }
            }

            for (int band = 0; band < BandCount; band++)
            {
                ksMix[band] /= totalConcentration;
            }

            return FromMixedKs(ksMix);
        }

        /// <summary>
        /// Converts a color to the spectral form paints are mixed in: a 38-band
        /// reflectance spectrum reconstructed from the color's linear RGB (as a
        /// blend of white, CMY and RGB base spectra), reduced to per-band
        /// Kubelka-Munk K/S values plus a tinting-strength weight.
        /// </summary>
        /// <param name="color">The paint color to convert.</param>
        /// <returns>The paint's mixing spectrum.</returns>
        public static PaintSpectrum ToSpectrum(Color color)
        {
            double linearRed = SrgbToLinear(color.R);
            double linearGreen = SrgbToLinear(color.G);
            double linearBlue = SrgbToLinear(color.B);

            // Decompose linear RGB into non-negative contributions of the seven
            // base spectra: the shared floor is white, the pairwise overlaps are
            // the subtractive primaries, and whatever remains is a pure primary.
            double white = Math.Min(linearRed, Math.Min(linearGreen, linearBlue));
            linearRed -= white;
            linearGreen -= white;
            linearBlue -= white;

            double cyan = Math.Min(linearGreen, linearBlue);
            double magenta = Math.Min(linearRed, linearBlue);
            double yellow = Math.Min(linearRed, linearGreen);
            double red = Math.Max(0.0, Math.Min(linearRed - linearBlue, linearRed - linearGreen));
            double green = Math.Max(0.0, Math.Min(linearGreen - linearBlue, linearGreen - linearRed));
            double blue = Math.Max(0.0, Math.Min(linearBlue - linearGreen, linearBlue - linearRed));

            var ks = new double[BandCount];
            double luminance = 0.0;
            for (int band = 0; band < BandCount; band++)
            {
                double reflectance = Math.Max(
                    MinReflectance,
                    white * WhiteSpectrum[band]
                    + cyan * CyanSpectrum[band]
                    + magenta * MagentaSpectrum[band]
                    + yellow * YellowSpectrum[band]
                    + red * RedSpectrum[band]
                    + green * GreenSpectrum[band]
                    + blue * BlueSpectrum[band]);

                luminance += ObserverY[band] * reflectance;
                ks[band] = (1.0 - reflectance) * (1.0 - reflectance) / (2.0 * reflectance);
            }

            // A pure-black input still gets a tiny strength so it can participate
            // in mixtures without producing a zero total concentration.
            return new PaintSpectrum(ks, Math.Max(luminance, MinReflectance));
        }

        /// <summary>
        /// Converts already-averaged per-band K/S values into a displayable color:
        /// each band's K/S is inverted to reflectance, and the spectrum is integrated
        /// against the CIE standard observer and mapped to sRGB.
        /// </summary>
        /// <param name="ksMix">The concentration-weighted average K/S per wavelength band.</param>
        /// <returns>The equivalent sRGB color with full alpha.</returns>
        internal static Color FromMixedKs(double[] ksMix)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            for (int band = 0; band < BandCount; band++)
            {
                // Kubelka-Munk inversion: the reflectance of an opaque layer whose
                // absorption/scattering ratio is ks.
                double ks = ksMix[band];
                double reflectance = 1.0 + ks - Math.Sqrt(ks * ks + 2.0 * ks);

                x += ObserverX[band] * reflectance;
                y += ObserverY[band] * reflectance;
                z += ObserverZ[band] * reflectance;
            }

            // XYZ to linear sRGB (D65); physical mixtures of in-gamut paints land
            // at most marginally outside the gamut, so clamping suffices.
            double linearRed = 3.2409699419045226 * x - 1.537383177570094 * y - 0.4986107602930034 * z;
            double linearGreen = -0.9692436362808796 * x + 1.8759675015077202 * y + 0.04155505740717559 * z;
            double linearBlue = 0.05563007969699366 * x - 0.20397695888897652 * y + 1.0569715142428786 * z;

            return Color.FromArgb(
                255,
                LinearToSrgb(linearRed),
                LinearToSrgb(linearGreen),
                LinearToSrgb(linearBlue));
        }

        /// <summary>
        /// Decodes an 8-bit sRGB channel to linear reflectance in [0, 1].
        /// </summary>
        /// <param name="channel">The sRGB-encoded channel value.</param>
        /// <returns>The linear-light reflectance of the channel.</returns>
        private static double SrgbToLinear(byte channel)
        {
            double c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Encodes linear reflectance back to an 8-bit sRGB channel.
        /// </summary>
        /// <param name="linear">The linear-light reflectance, clamped to [0, 1].</param>
        /// <returns>The sRGB-encoded channel value.</returns>
        private static int LinearToSrgb(double linear)
        {
            double clamped = Math.Clamp(linear, 0.0, 1.0);
            double c = clamped <= 0.0031308 ? clamped * 12.92 : 1.055 * Math.Pow(clamped, 1.0 / 2.4) - 0.055;
            return (int)Math.Round(c * 255.0);
        }

        // Base reflectance spectra (38 bands, 380-730nm in 10nm steps) that
        // reconstruct a full spectrum from a linear RGB decomposition, and the CIE
        // 1931 standard observer weighted by the D65 illuminant that integrates a
        // spectrum back to XYZ. Values are from spectral.js v3 (MIT License).
        private static readonly double[] WhiteSpectrum =
        {
            1.00116072718764, 1.00116065159728, 1.00116031922747, 1.00115867270789,
            1.00115259844552, 1.00113252528998, 1.00108500663327, 1.00099687889453,
            1.00086525152274, 1.0006962900094, 1.00050496114888, 1.00030808187992,
            1.00011966602013, 0.999952765968407, 0.999821836899297, 0.999738609557593,
            0.999709551639612, 0.999731930210627, 0.999799436346195, 0.999900330316671,
            1.00002040652611, 1.00014478793658, 1.00025997903412, 1.00035579697089,
            1.00042753780269, 1.00047623344888, 1.00050720967508, 1.00052519156373,
            1.00053509606896, 1.00054022097482, 1.00054272816784, 1.00054389569087,
            1.00054448212151, 1.00054476959992, 1.00054489887762, 1.00054496254689,
            1.00054498927058, 1.000544996993,
        };

        private static readonly double[] CyanSpectrum =
        {
            0.970585001322962, 0.970592498143425, 0.970625348729891, 0.970786806119017,
            0.971368673228248, 0.973163230621252, 0.976740223158765, 0.981587605491377,
            0.986280265652949, 0.989949147689134, 0.99249270153842, 0.994145680405256,
            0.995183975033212, 0.995756750110818, 0.99591281828671, 0.995606157834528,
            0.994597600961854, 0.99221571549237, 0.986236452783249, 0.967943337264541,
            0.891285004244943, 0.536202477862053, 0.154108119001878, 0.0574575093228929,
            0.0315349873107007, 0.0222633920086335, 0.0182022841492439, 0.016299055973264,
            0.0153656239334613, 0.0149111568733976, 0.0146954339898235, 0.0145964146717719,
            0.0145470156699655, 0.0145228771899495, 0.0145120341118965, 0.0145066940939832,
            0.0145044507314479, 0.0145038009464639,
        };

        private static readonly double[] MagentaSpectrum =
        {
            0.990673557319988, 0.990671524961979, 0.990662582353421, 0.990618107644795,
            0.99045148087871, 0.989871081400204, 0.98828660875964, 0.984290692797504,
            0.973934905625306, 0.941817838460145, 0.817390326195156, 0.432472805065729,
            0.13845397825887, 0.0537347216940033, 0.0292174996673231, 0.021313651750859,
            0.0201349530181136, 0.0241323096280662, 0.0372236145223627, 0.0760506552706601,
            0.205375471942399, 0.541268903460439, 0.815841685086486, 0.912817704123976,
            0.946339830166962, 0.959927696331991, 0.966260595230312, 0.969325970058424,
            0.970854536721399, 0.971605066528128, 0.971962769757392, 0.972127272274509,
            0.972209417745812, 0.972249577678424, 0.972267621998742, 0.97227650946215,
            0.972280243306874, 0.97228132482656,
        };

        private static readonly double[] YellowSpectrum =
        {
            0.0210523371789306, 0.0210564627517414, 0.0210746178695038, 0.0211649058448753,
            0.0215027957272504, 0.0226738799041561, 0.0258235649693629, 0.0334879385639851,
            0.0519069663740307, 0.100749014833473, 0.239129899706847, 0.534804312272748,
            0.79780757864303, 0.911449894067384, 0.953797963004507, 0.971241615465429,
            0.979303123807588, 0.983380119507575, 0.985461246567755, 0.986435046976605,
            0.986738250670141, 0.986617882445032, 0.986277776758643, 0.985860592444056,
            0.98547492767621, 0.985176934765558, 0.984971574014181, 0.984846303415712,
            0.984775351811199, 0.984738066625265, 0.984719648311765, 0.984711023391939,
            0.984706683300676, 0.984704554393091, 0.98470359630937, 0.984703124077552,
            0.98470292561509, 0.984702868122795,
        };

        private static readonly double[] RedSpectrum =
        {
            0.0315605737777207, 0.0315520718330149, 0.0315148215513658, 0.0313318044982702,
            0.0306729857725527, 0.0286480476989607, 0.0246450407045709, 0.0192960753663651,
            0.0142066612220556, 0.0102942608878609, 0.0076191460521811, 0.005898041083542,
            0.0048233247781713, 0.0042298748350633, 0.0040599171299341, 0.0043533695594676,
            0.0053434425970201, 0.0076917201010463, 0.0135969795736536, 0.0316975442661115,
            0.107861196355249, 0.463812603168704, 0.847055405272011, 0.943185409393918,
            0.968862150696558, 0.978030667473603, 0.982043643854306, 0.983923623718707,
            0.984845484154382, 0.985294275814596, 0.985507295219825, 0.985605071539837,
            0.985653849933578, 0.985677685033883, 0.985688391806122, 0.985693664690031,
            0.985695879848205, 0.985696521463762,
        };

        private static readonly double[] GreenSpectrum =
        {
            0.0095560747554212, 0.0095581580120851, 0.0095673245444588, 0.0096129126297349,
            0.0097837090401843, 0.010378622705871, 0.0120026452378567, 0.0160977721473922,
            0.026706190223168, 0.0595555440185881, 0.186039826532826, 0.570579820116159,
            0.861467768400292, 0.945879089767658, 0.970465486474305, 0.97841363028445,
            0.979589031411224, 0.975533536908632, 0.962288755397813, 0.92312157451312,
            0.793434018943111, 0.459270135902429, 0.185574103666303, 0.0881774959955372,
            0.05436302287667, 0.0406288447060719, 0.034221520431697, 0.0311185790956966,
            0.0295708898336134, 0.0288108739348928, 0.0284486271324597, 0.0282820301724731,
            0.0281988376490237, 0.0281581655342037, 0.0281398910216386, 0.0281308901665811,
            0.0281271086805816, 0.0281260133612096,
        };

        private static readonly double[] BlueSpectrum =
        {
            0.979404752502014, 0.97940070684313, 0.979382903470261, 0.979294364945594,
            0.97896301460857, 0.977814466694043, 0.974724321133836, 0.967198482343973,
            0.949079657530575, 0.900850128940977, 0.76315044546224, 0.465922171649319,
            0.201263280451005, 0.0877524413419623, 0.0457176793291679, 0.0284706050521843,
            0.020527176756985, 0.0165302792310211, 0.0145135107212858, 0.0136003508637687,
            0.0133604258769571, 0.013548894314568, 0.0139594356366992, 0.014443425575357,
            0.0148854440621406, 0.0152254296999746, 0.0154592848180209, 0.0156018026485961,
            0.0156824871281936, 0.0157248764360615, 0.0157458108784121, 0.0157556123350225,
            0.0157605443964911, 0.0157629637515278, 0.0157640525629106, 0.015764589232951,
            0.0157648147772649, 0.0157648801149616,
        };

        private static readonly double[] ObserverX =
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

        private static readonly double[] ObserverY =
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

        private static readonly double[] ObserverZ =
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
