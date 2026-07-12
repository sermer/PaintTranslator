using System.Collections.Generic;
using System.Drawing;

namespace PaintTranslator.Data
{
    /// <summary>
    /// Represents a single tube of paint: its marketing name and the measured
    /// mass-tone color of the paint straight from the tube.
    /// </summary>
    public sealed class GoldenPaint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GoldenPaint"/> class.
        /// </summary>
        /// <param name="name">The manufacturer's name for the paint color.</param>
        /// <param name="color">The measured mass-tone RGB color of the paint.</param>
        public GoldenPaint(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        /// <summary>
        /// Gets the manufacturer's name for the paint color.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the measured mass-tone RGB color of the paint.
        /// </summary>
        public Color Color { get; }

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
    /// Provides the full Golden Heavy Body Acrylics palette with mass-tone RGB values.
    /// Sources: spectrophotometer measurements from SensualLogic artist color data where
    /// available, otherwise Golden's published CIE L*a*b* pigment data converted to sRGB
    /// (D65, clamped to gamut). Iridescent, interference, and mica effect paints are
    /// omitted because Golden publishes no color measurement for them and a flat RGB
    /// cannot represent their appearance.
    /// </summary>
    public static class GoldenPalette
    {
        /// <summary>
        /// Gets every paint in the palette, ordered roughly by position on the
        /// color wheel (whites, yellows, oranges, reds, magentas, earths, violets,
        /// blues, teals, greens, grays, blacks, fluorescents) so wheel wedges built
        /// from this list read as a spectrum.
        /// </summary>
        public static IReadOnlyList<GoldenPaint> Paints { get; } = new[]
        {
            new GoldenPaint("Titanium White", Color.FromArgb(255, 247, 255)),
            new GoldenPaint("Zinc White", Color.FromArgb(242, 244, 241)),
            new GoldenPaint("Titan Buff", Color.FromArgb(225, 205, 186)),
            new GoldenPaint("Titan Green Pale", Color.FromArgb(199, 201, 176)),
            new GoldenPaint("Titan Mars Pale", Color.FromArgb(236, 195, 178)),
            new GoldenPaint("Titan Violet Pale", Color.FromArgb(224, 213, 216)),
            new GoldenPaint("Titanate Yellow", Color.FromArgb(245, 226, 117)),
            new GoldenPaint("Light Bismuth Yellow", Color.FromArgb(255, 244, 163)),
            new GoldenPaint("Bismuth Vanadate Yellow", Color.FromArgb(255, 227, 18)),
            new GoldenPaint("C.P. Cadmium Yellow Primrose", Color.FromArgb(249, 231, 20)),
            new GoldenPaint("Cadmium Yellow Light", Color.FromArgb(255, 225, 0)),
            new GoldenPaint("Hansa Yellow Light", Color.FromArgb(250, 226, 0)),
            new GoldenPaint("Hansa Yellow Opaque", Color.FromArgb(255, 200, 0)),
            new GoldenPaint("Benzimidazolone Yellow Light", Color.FromArgb(255, 201, 0)),
            new GoldenPaint("Primary Yellow", Color.FromArgb(255, 215, 0)),
            new GoldenPaint("Benzimidazolone Yellow Medium", Color.FromArgb(255, 189, 0)),
            new GoldenPaint("C.P. Cadmium Yellow Med", Color.FromArgb(255, 200, 0)),
            new GoldenPaint("Cadmium Yellow Medium Hue", Color.FromArgb(255, 191, 0)),
            new GoldenPaint("Cadmium Yellow Dark", Color.FromArgb(255, 173, 0)),
            new GoldenPaint("Diarylide Yellow", Color.FromArgb(255, 162, 0)),
            new GoldenPaint("Isoindolinone Yellow", Color.FromArgb(241, 120, 31)),
            new GoldenPaint("C.P. Cadmium Orange", Color.FromArgb(255, 99, 0)),
            new GoldenPaint("Light Orange", Color.FromArgb(255, 200, 167)),
            new GoldenPaint("Vat Orange", Color.FromArgb(232, 85, 44)),
            new GoldenPaint("Pyrrole Orange", Color.FromArgb(239, 61, 0)),
            new GoldenPaint("C.P. Cadmium Red Light", Color.FromArgb(227, 40, 16)),
            new GoldenPaint("Pyrrole Red Light", Color.FromArgb(209, 60, 51)),
            new GoldenPaint("Naphthol Red Light", Color.FromArgb(200, 66, 53)),
            new GoldenPaint("Pyrrole Red", Color.FromArgb(187, 0, 0)),
            new GoldenPaint("C.P. Cadmium Red Medium", Color.FromArgb(185, 5, 32)),
            new GoldenPaint("Cadmium Red Medium Hue", Color.FromArgb(176, 60, 54)),
            new GoldenPaint("Naphthol Red Medium", Color.FromArgb(156, 55, 54)),
            new GoldenPaint("Cadmium Red Dark", Color.FromArgb(162, 54, 59)),
            new GoldenPaint("Pyrrole Red Dark", Color.FromArgb(161, 51, 56)),
            new GoldenPaint("Alizarin Crimson Hue", Color.FromArgb(82, 61, 63)),
            new GoldenPaint("Permanent Maroon", Color.FromArgb(70, 60, 63)),
            new GoldenPaint("Primary Magenta", Color.FromArgb(169, 26, 49)),
            new GoldenPaint("Naphthol Pink", Color.FromArgb(218, 86, 100)),
            new GoldenPaint("Light Magenta", Color.FromArgb(245, 152, 176)),
            new GoldenPaint("Medium Magenta", Color.FromArgb(185, 85, 148)),
            new GoldenPaint("Quinacridone Red", Color.FromArgb(150, 0, 37)),
            new GoldenPaint("Quinacridone Magenta", Color.FromArgb(90, 0, 37)),
            new GoldenPaint("Quinacridone Violet", Color.FromArgb(105, 54, 60)),
            new GoldenPaint("Red Oxide", Color.FromArgb(132, 53, 41)),
            new GoldenPaint("Violet Oxide", Color.FromArgb(106, 64, 63)),
            new GoldenPaint("Transparent Red Iron Oxide", Color.FromArgb(96, 65, 65)),
            new GoldenPaint("Burnt Sienna", Color.FromArgb(112, 62, 53)),
            new GoldenPaint("Transparent Brown Iron Oxide", Color.FromArgb(73, 65, 63)),
            new GoldenPaint("Raw Sienna", Color.FromArgb(157, 103, 52)),
            new GoldenPaint("Transparent Yellow Iron Oxide", Color.FromArgb(155, 90, 63)),
            new GoldenPaint("Mars Yellow", Color.FromArgb(163, 94, 66)),
            new GoldenPaint("Yellow Ochre", Color.FromArgb(186, 124, 51)),
            new GoldenPaint("Yellow Oxide", Color.FromArgb(195, 139, 70)),
            new GoldenPaint("Naples Yellow Hue", Color.FromArgb(230, 180, 119)),
            new GoldenPaint("Naples Yellow Deep", Color.FromArgb(232, 161, 72)),
            new GoldenPaint("Nickel Azo Yellow", Color.FromArgb(126, 93, 9)),
            new GoldenPaint("India Yellow Hue", Color.FromArgb(193, 111, 57)),
            new GoldenPaint("Azo Gold", Color.FromArgb(117, 67, 59)),
            new GoldenPaint("Benzimidazolone Burnt Orange", Color.FromArgb(78, 61, 57)),
            new GoldenPaint("Burnt Umber Light", Color.FromArgb(87, 66, 59)),
            new GoldenPaint("Burnt Umber", Color.FromArgb(70, 56, 54)),
            new GoldenPaint("Raw Umber", Color.FromArgb(64, 56, 54)),
            new GoldenPaint("Van Dyke Brown Hue", Color.FromArgb(64, 63, 64)),
            new GoldenPaint("Dioxazine Purple", Color.FromArgb(25, 17, 19)),
            new GoldenPaint("Permanent Violet Dark", Color.FromArgb(72, 60, 68)),
            new GoldenPaint("Cobalt Violet Hue", Color.FromArgb(89, 58, 77)),
            new GoldenPaint("Medium Violet", Color.FromArgb(91, 70, 120)),
            new GoldenPaint("Light Violet", Color.FromArgb(117, 114, 186)),
            new GoldenPaint("Ultramarine Violet", Color.FromArgb(54, 47, 75)),
            new GoldenPaint("Ultramarine Blue", Color.FromArgb(50, 47, 75)),
            new GoldenPaint("Light Ultramarine Blue", Color.FromArgb(100, 160, 230)),
            new GoldenPaint("Smalt Hue", Color.FromArgb(53, 60, 82)),
            new GoldenPaint("Cobalt Blue", Color.FromArgb(8, 50, 160)),
            new GoldenPaint("Cobalt Blue Hue", Color.FromArgb(11, 89, 159)),
            new GoldenPaint("Anthraquinone Blue", Color.FromArgb(60, 56, 64)),
            new GoldenPaint("Prussian Blue Hue", Color.FromArgb(58, 57, 62)),
            new GoldenPaint("Phthalo Blue (R.S.)", Color.FromArgb(59, 56, 81)),
            new GoldenPaint("Phthalo Blue (G.S.)", Color.FromArgb(16, 3, 62)),
            new GoldenPaint("Light Phthalo Blue", Color.FromArgb(143, 217, 242)),
            new GoldenPaint("Primary Cyan", Color.FromArgb(0, 63, 130)),
            new GoldenPaint("Cerulean Blue, Chromium", Color.FromArgb(0, 85, 152)),
            new GoldenPaint("Cerulean Blue Deep", Color.FromArgb(0, 91, 116)),
            new GoldenPaint("Manganese Blue Hue", Color.FromArgb(0, 102, 143)),
            new GoldenPaint("Azurite Hue", Color.FromArgb(48, 78, 100)),
            new GoldenPaint("Teal", Color.FromArgb(0, 172, 173)),
            new GoldenPaint("Cobalt Teal", Color.FromArgb(0, 161, 161)),
            new GoldenPaint("Cobalt Turquoise", Color.FromArgb(0, 114, 115)),
            new GoldenPaint("Turquoise (Phthalo)", Color.FromArgb(52, 62, 73)),
            new GoldenPaint("Light Turquoise (Phthalo)", Color.FromArgb(0, 147, 126)),
            new GoldenPaint("Phthalo Green (B.S.)", Color.FromArgb(0, 31, 41)),
            new GoldenPaint("Phthalo Green (Y.S.)", Color.FromArgb(0, 31, 27)),
            new GoldenPaint("Light Phthalo Green", Color.FromArgb(179, 236, 216)),
            new GoldenPaint("Viridian Green Hue", Color.FromArgb(29, 93, 83)),
            new GoldenPaint("Permanent Green Light", Color.FromArgb(0, 118, 75)),
            new GoldenPaint("Light Green (B.S.)", Color.FromArgb(82, 174, 75)),
            new GoldenPaint("Light Green (Y.S.)", Color.FromArgb(140, 198, 78)),
            new GoldenPaint("Cobalt Green", Color.FromArgb(48, 89, 75)),
            new GoldenPaint("Jenkins Green", Color.FromArgb(55, 62, 61)),
            new GoldenPaint("Hooker's Green Hue", Color.FromArgb(59, 64, 64)),
            new GoldenPaint("Sap Green Hue", Color.FromArgb(61, 69, 61)),
            new GoldenPaint("Terre Verte Hue", Color.FromArgb(75, 82, 67)),
            new GoldenPaint("Chromium Oxide Green", Color.FromArgb(76, 106, 67)),
            new GoldenPaint("Chromium Oxide Green Dark", Color.FromArgb(69, 85, 65)),
            new GoldenPaint("Green Gold", Color.FromArgb(106, 110, 54)),
            new GoldenPaint("Neutral Gray N8", Color.FromArgb(196, 195, 195)),
            new GoldenPaint("Neutral Gray N7", Color.FromArgb(171, 171, 170)),
            new GoldenPaint("Neutral Gray N6", Color.FromArgb(150, 149, 148)),
            new GoldenPaint("Neutral Gray N5", Color.FromArgb(120, 116, 122)),
            new GoldenPaint("Neutral Gray N4", Color.FromArgb(103, 103, 103)),
            new GoldenPaint("Neutral Gray N3", Color.FromArgb(80, 80, 81)),
            new GoldenPaint("Neutral Gray N2", Color.FromArgb(65, 66, 68)),
            new GoldenPaint("Graphite Gray", Color.FromArgb(93, 91, 91)),
            new GoldenPaint("Payne's Gray", Color.FromArgb(54, 56, 59)),
            new GoldenPaint("Bone Black", Color.FromArgb(35, 34, 36)),
            new GoldenPaint("Carbon Black", Color.FromArgb(30, 29, 31)),
            new GoldenPaint("Mars Black", Color.FromArgb(62, 60, 60)),
            new GoldenPaint("Fluorescent Chartreuse", Color.FromArgb(255, 255, 0)),
            new GoldenPaint("Fluorescent Orange-Yellow", Color.FromArgb(255, 109, 0)),
            new GoldenPaint("Fluorescent Orange", Color.FromArgb(255, 75, 0)),
            new GoldenPaint("Fluorescent Red", Color.FromArgb(255, 24, 32)),
            new GoldenPaint("Fluorescent Pink", Color.FromArgb(255, 12, 83)),
            new GoldenPaint("Fluorescent Magenta", Color.FromArgb(243, 0, 83)),
            new GoldenPaint("Fluorescent Blue", Color.FromArgb(0, 110, 186)),
            new GoldenPaint("Fluorescent Green", Color.FromArgb(0, 194, 13)),
            new GoldenPaint("Phosphorescent Green", Color.FromArgb(226, 223, 197)),
        };
    }
}
