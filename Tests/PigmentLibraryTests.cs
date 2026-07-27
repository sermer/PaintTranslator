using System;
using System.IO;
using System.Linq;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the coefficient resource and the library that reads it. These are
    /// structural checks rather than value checks: the numbers come from a measurement
    /// nobody here can re-derive, so what can be verified is that they arrived intact,
    /// in the right slots, and in the right count.
    /// </summary>
    public class PigmentLibraryTests
    {
        /// <summary>
        /// Confirms the embedded resource holds the nineteen measured paints and that
        /// all of them are offered to the user. The derived tier added later must not
        /// change this count, because it is deliberately withheld from the picker.
        /// </summary>
        [Fact]
        public void LibraryHoldsTheNineteenMeasuredPaintsAndOffersThemAll()
        {
            var measured = PigmentLibrary.All
                .Where(paint => paint.Provenance == PigmentProvenance.TwoConstantMeasured)
                .ToList();

            Assert.Equal(19, measured.Count);
            Assert.Equal(19, PigmentLibrary.Selectable.Count);
            Assert.All(PigmentLibrary.Selectable,
                paint => Assert.Equal(PigmentProvenance.TwoConstantMeasured, paint.Provenance));
        }

        /// <summary>
        /// Confirms every coefficient survived the round trip through the resource as a
        /// usable number. A truncated or misaligned read shows up here as a NaN or a
        /// negative, long before it shows up as a wrong colour.
        /// </summary>
        [Fact]
        public void EveryCoefficientIsFiniteAndNonNegative()
        {
            foreach (PigmentCoefficients paint in PigmentLibrary.All)
            {
                Assert.Equal(SpectralBands.Count, paint.Absorption.Length);
                Assert.Equal(SpectralBands.Count, paint.Scattering.Length);

                for (int band = 0; band < SpectralBands.Count; band++)
                {
                    Assert.True(double.IsFinite(paint.Absorption[band]), $"{paint.Name} K[{band}]");
                    Assert.True(double.IsFinite(paint.Scattering[band]), $"{paint.Name} S[{band}]");
                    Assert.True(paint.Absorption[band] >= 0.0, $"{paint.Name} K[{band}]");
                    Assert.True(paint.Scattering[band] >= 0.0, $"{paint.Name} S[{band}]");
                }
            }
        }

        /// <summary>
        /// Confirms titanium white's scattering is exactly 1 at every band. Berns
        /// normalises the reference white that way, so this is an unambiguous signature
        /// that absorption and scattering did not get swapped somewhere between
        /// Unicolour and the resource — an error that would otherwise show up only as
        /// every mixture being subtly wrong.
        /// </summary>
        [Fact]
        public void TitaniumWhiteScatteringIsNormalisedToOne()
        {
            PigmentCoefficients white = PigmentLibrary.All.Single(p => p.Name == "Titanium White");

            Assert.All(white.Scattering, s => Assert.InRange(s, 1.0 - 1e-9, 1.0 + 1e-9));
        }

        /// <summary>
        /// Confirms the paints the rest of the suite names by hand are all present and
        /// spelled as expected, so a rename in the ingest fails here rather than as a
        /// confusing null somewhere downstream.
        /// </summary>
        [Theory]
        [InlineData("Titanium White")]
        [InlineData("Bone Black")]
        [InlineData("Phthalo Blue (G.S.)")]
        [InlineData("Phthalo Blue (R.S.)")]
        [InlineData("Ultramarine Blue")]
        [InlineData("Cobalt Blue")]
        [InlineData("Cerulean Blue, Chromium")]
        [InlineData("Diarylide Yellow")]
        [InlineData("Hansa Yellow Opaque")]
        [InlineData("Bismuth Vanadate Yellow")]
        [InlineData("C.P. Cadmium Red Light")]
        [InlineData("Phthalo Green (B.S.)")]
        public void NamedPaintIsPresent(string name)
        {
            Assert.Single(PigmentLibrary.All, paint => paint.Name == name);
        }

        /// <summary>
        /// Confirms the format survives a write-then-read cycle in memory, including the
        /// derived tier's omitted scattering array. This is the test that keeps the
        /// ingest tool and the runtime reader honest with each other.
        /// </summary>
        [Fact]
        public void FormatRoundTripsBothProvenanceTiers()
        {
            var measuredK = Enumerable.Range(0, SpectralBands.Count).Select(i => i * 0.25).ToArray();
            var measuredS = Enumerable.Range(0, SpectralBands.Count).Select(i => 1.0 + i).ToArray();
            var derivedK = Enumerable.Range(0, SpectralBands.Count).Select(i => i * 0.5).ToArray();

            var written = new[]
            {
                new PigmentCoefficients("Measured", "PB15", PigmentProvenance.TwoConstantMeasured, measuredK, measuredS),
                new PigmentCoefficients("Derived", "PY74", PigmentProvenance.ReflectanceDerived, derivedK, null),
            };

            using var stream = new MemoryStream();
            PigmentData.Write(stream, written);
            stream.Position = 0;
            var read = PigmentData.Read(stream);

            Assert.Equal(2, read.Count);
            Assert.Equal("Measured", read[0].Name);
            Assert.Equal("PB15", read[0].ColourIndex);
            Assert.Equal(measuredK, read[0].Absorption);
            Assert.Equal(measuredS, read[0].Scattering);
            Assert.Equal(PigmentProvenance.ReflectanceDerived, read[1].Provenance);
            Assert.Equal(derivedK, read[1].Absorption);
            Assert.All(read[1].Scattering, s => Assert.Equal(1.0, s));
        }
    }
}
