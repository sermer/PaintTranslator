using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Renders mixing sweeps to a PNG for visual inspection.
    /// <para>
    /// This is not an assertion about colour — no automated check can tell you whether a
    /// green looks like the green two tubes actually make. It runs as a test so that it
    /// cannot silently stop working, but its real output is the image.
    /// </para>
    /// </summary>
    public class ContactSheetTests
    {
        /// <summary>How many mixtures each sweep shows, endpoints included.</summary>
        private const int Steps = 21;

        /// <summary>The width of one swatch in pixels.</summary>
        private const int SwatchWidth = 48;

        /// <summary>The height of one swatch in pixels.</summary>
        private const int SwatchHeight = 96;

        /// <summary>The vertical space above each sweep for its label.</summary>
        private const int LabelHeight = 24;

        /// <summary>
        /// The sweeps rendered, chosen to cover the three things worth judging by eye:
        /// whether blue and yellow make a convincing green, whether complements go to a
        /// believable neutral rather than a third hue, and whether the value scale
        /// between white and black is spaced like real paint rather than bunched at one
        /// end.
        /// </summary>
        private static readonly (string First, string Second)[] Sweeps =
        {
            ("Phthalo Blue (G.S.)", "Diarylide Yellow"),
            ("C.P. Cadmium Red Light", "Phthalo Green (B.S.)"),
            ("Titanium White", "Bone Black"),
        };

        /// <summary>
        /// Writes the contact sheet and confirms it was produced.
        /// </summary>
        [Fact]
        public void WritesTheMixingSweepContactSheet()
        {
            string directory = Path.Combine(RepositoryRoot(), "contact-sheets");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "mixing-sweeps.png");

            int width = Steps * SwatchWidth;
            int height = Sweeps.Length * (SwatchHeight + LabelHeight);

            using (var sheet = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(sheet))
            using (var font = new Font("Segoe UI", 9f))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                graphics.Clear(Color.White);

                for (int sweep = 0; sweep < Sweeps.Length; sweep++)
                {
                    (string first, string second) = Sweeps[sweep];
                    int top = sweep * (SwatchHeight + LabelHeight);

                    graphics.DrawString(
                        $"{first}  ->  {second}", font, textBrush, 2, top + 4);

                    DrawSweep(graphics, first, second, top + LabelHeight);
                }

                sheet.Save(path, ImageFormat.Png);
            }

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }

        /// <summary>
        /// Draws one paint-to-paint sweep as a row of swatches.
        /// </summary>
        /// <param name="graphics">The surface to draw on.</param>
        /// <param name="firstName">The paint at the left end.</param>
        /// <param name="secondName">The paint at the right end.</param>
        /// <param name="top">The row's top edge in pixels.</param>
        private static void DrawSweep(Graphics graphics, string firstName, string secondName, int top)
        {
            PigmentCoefficients first = Paint(firstName);
            PigmentCoefficients second = Paint(secondName);
            var reflectance = new double[SpectralBands.Count];

            for (int step = 0; step < Steps; step++)
            {
                double share = step / (double)(Steps - 1);
                KubelkaMunk.Mix(new[] { first, second }, new[] { 1.0 - share, share }, reflectance);
                Color swatch = SpectralRenderer.ToDisplayColor(reflectance, out double chromaLost);

                using var brush = new SolidBrush(swatch);
                graphics.FillRectangle(brush, step * SwatchWidth, top, SwatchWidth, SwatchHeight);

                // A hatched corner marks a mixture more vivid than the screen can show,
                // so the sheet does not quietly present a compressed colour as the real
                // one.
                if (chromaLost > 0.001)
                {
                    using var marker = new SolidBrush(Color.FromArgb(160, Color.White));
                    graphics.FillRectangle(marker, step * SwatchWidth, top, 6, 6);
                }
            }
        }

        /// <summary>
        /// Walks up from the test assembly to the repository root.
        /// </summary>
        /// <returns>The repository root directory.</returns>
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PaintTranslator.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new InvalidOperationException("Could not find the repository root.");
        }

        /// <summary>
        /// Looks a paint up by name.
        /// </summary>
        /// <param name="name">The paint's name.</param>
        /// <returns>The paint.</returns>
        private static PigmentCoefficients Paint(string name)
        {
            return PigmentLibrary.All.Single(paint => paint.Name == name);
        }
    }
}
