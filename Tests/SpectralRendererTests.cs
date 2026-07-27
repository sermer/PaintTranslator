using System;
using System.Drawing;
using System.Linq;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the step from a reflectance spectrum to a colour. The renderer is the only
    /// place the observer tables and the illuminant are applied, so an error here tints
    /// every paint in the application by the same amount — which is exactly the kind of
    /// error that is invisible when comparing paints against each other.
    /// </summary>
    public class SpectralRendererTests
    {
        /// <summary>
        /// Confirms a perfect diffuser renders as reference white. This is the anchor:
        /// it fails if the observer tables are not normalised, if the wrong illuminant
        /// is applied, or if the band count drifts.
        /// </summary>
        [Fact]
        public void PerfectDiffuserRendersAsReferenceWhite()
        {
            var reflectance = Enumerable.Repeat(1.0, SpectralBands.Count).ToArray();

            SpectralRenderer.ToLab(reflectance, out double lightness, out double aStar, out double bStar);

            Assert.InRange(lightness, 99.5, 100.5);
            Assert.InRange(aStar, -0.5, 0.5);
            Assert.InRange(bStar, -0.5, 0.5);
        }

        /// <summary>
        /// Confirms a perfect absorber renders black, which pins the other end of the
        /// lightness axis.
        /// </summary>
        [Fact]
        public void PerfectAbsorberRendersBlack()
        {
            var reflectance = new double[SpectralBands.Count];

            SpectralRenderer.ToLab(reflectance, out double lightness, out _, out _);

            Assert.InRange(lightness, 0.0, 0.5);
        }

        /// <summary>
        /// Confirms titanium white at full concentration renders as a near-white. Golden
        /// publishes L* 98.25 for this paint, but their chart states no illuminant,
        /// observer or geometry, so this asserts the range a white must fall in rather
        /// than that specific number.
        /// </summary>
        [Fact]
        public void TitaniumWhiteRendersAsANearWhite()
        {
            var reflectance = new double[SpectralBands.Count];
            PigmentCoefficients white = PigmentLibrary.All.Single(p => p.Name == "Titanium White");
            KubelkaMunk.Mix(new[] { white }, new[] { 1.0 }, reflectance);

            SpectralRenderer.ToLab(reflectance, out double lightness, out double aStar, out double bStar);

            Assert.InRange(lightness, 90.0, 100.0);
            Assert.InRange(aStar, -3.0, 3.0);
            Assert.InRange(bStar, -3.0, 5.0);
        }

        /// <summary>
        /// Confirms bone black renders dark. Berns measures this paint at L* 11.4 while
        /// Golden publishes 23.82 for nominally the same tube; the range here spans both
        /// rather than picking a side, since the disagreement is real and unresolved.
        /// </summary>
        [Fact]
        public void BoneBlackRendersDark()
        {
            var reflectance = new double[SpectralBands.Count];
            PigmentCoefficients black = PigmentLibrary.All.Single(p => p.Name == "Bone Black");
            KubelkaMunk.Mix(new[] { black }, new[] { 1.0 }, reflectance);

            SpectralRenderer.ToLab(reflectance, out double lightness, out _, out _);

            Assert.InRange(lightness, 5.0, 28.0);
        }

        /// <summary>
        /// Confirms the renderer reports compression separately from the colour it
        /// returns, and that the two are consistent: a paint that needed compressing
        /// must still produce a legal colour.
        /// </summary>
        [Fact]
        public void DisplayColourIsLegalAndReportsItsCompression()
        {
            var reflectance = new double[SpectralBands.Count];
            PigmentCoefficients yellow = PigmentLibrary.All.Single(p => p.Name == "Diarylide Yellow");
            KubelkaMunk.Mix(new[] { yellow }, new[] { 1.0 }, reflectance);

            Color displayed = SpectralRenderer.ToDisplayColor(reflectance, out double chromaLost);

            Assert.InRange(displayed.R, 0, 255);
            Assert.InRange(displayed.G, 0, 255);
            Assert.InRange(displayed.B, 0, 255);
            Assert.True(chromaLost >= 0.0);
            Assert.Equal(255, displayed.A);
        }

        /// <summary>
        /// Confirms the renderer rejects a spectrum with the wrong number of bands
        /// rather than reading past the end of it.
        /// </summary>
        [Fact]
        public void RejectsAWronglySizedSpectrum()
        {
            Assert.Throws<ArgumentException>(
                () => SpectralRenderer.ToLab(new double[10], out _, out _, out _));
        }
    }
}
