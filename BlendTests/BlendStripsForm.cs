using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PaintTranslator.Pigments;

namespace PaintTranslator.BlendTests
{
    /// <summary>
    /// Test window that shows a scrollable column of gradient strips, one for
    /// every pair combination of a representative set of Golden paints, so the
    /// subtractive blend math can be inspected visually.
    /// </summary>
    public partial class BlendStripsForm : Form
    {
        /// <summary>
        /// Width of every gradient strip, in pixels.
        /// </summary>
        private const int StripWidth = 640;

        /// <summary>
        /// The paints whose pair combinations are rendered. Edit this list to test
        /// different blends; names must match <see cref="PigmentLibrary"/> entries.
        /// </summary>
        private static readonly string[] TestPaintNames =
        {
            "Titanium White",
            "Hansa Yellow Light",
            "C.P. Cadmium Orange",
            "Pyrrole Red",
            "Quinacridone Magenta",
            "Cobalt Blue",
            "Phthalo Blue (G.S.)",
            "Phthalo Green (B.S.)",
            "Yellow Ochre",
            "Carbon Black",
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BlendStripsForm"/> class
        /// and renders a strip for each paint pair.
        /// </summary>
        public BlendStripsForm()
        {
            InitializeComponent();
            PopulateStrips();
        }

        /// <summary>
        /// Renders one gradient strip per unordered pair of test paints and adds
        /// each to the scrollable flow panel.
        /// </summary>
        private void PopulateStrips()
        {
            List<PigmentCoefficients> paints = ResolveTestPaints();

            // The strip count and images depend on the paint list at runtime, so
            // these picture boxes can't live in the Designer file.
            stripsFlowPanel.SuspendLayout();
            try
            {
                for (int i = 0; i < paints.Count; i++)
                {
                    for (int j = i + 1; j < paints.Count; j++)
                    {
                        var stripBox = new PictureBox
                        {
                            Image = GradientStripRenderer.Render(paints[i], paints[j], StripWidth),
                            SizeMode = PictureBoxSizeMode.AutoSize,
                            Margin = new Padding(6, 4, 6, 4),
                        };

                        stripsFlowPanel.Controls.Add(stripBox);
                    }
                }
            }
            finally
            {
                stripsFlowPanel.ResumeLayout();
            }
        }

        /// <summary>
        /// Looks up the test paints by name in the Golden palette, preserving the
        /// order of <see cref="TestPaintNames"/> and skipping any name that no
        /// longer matches a palette entry.
        /// </summary>
        /// <returns>The resolved paints.</returns>
        private static List<PigmentCoefficients> ResolveTestPaints()
        {
            var paints = new List<PigmentCoefficients>(TestPaintNames.Length);

            foreach (string name in TestPaintNames)
            {
                foreach (PigmentCoefficients paint in PigmentLibrary.Selectable)
                {
                    if (paint.Name == name)
                    {
                        paints.Add(paint);
                        break;
                    }
                }
            }

            return paints;
        }
    }
}
