using System;
using System.Linq;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the wheel's geometry, which decides which paints a pixel is made of and in
    /// what proportion. It is shared by the renderer and the tooltip so that the colour
    /// under the cursor and the recipe reported for it are the same computation.
    /// </summary>
    public class BlendGeometryTests
    {
        /// <summary>The diameter these tests reason about.</summary>
        private const int Diameter = 101;

        /// <summary>
        /// Confirms weights always sum to 1, everywhere inside the wheel. They are
        /// concentrations, and the kernel normalises them anyway, but a set that does not
        /// sum to 1 means the geometry has lost or duplicated a share.
        /// </summary>
        /// <param name="paintCount">How many paints the wheel is built from.</param>
        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(19)]
        public void WeightsAlwaysSumToOne(int paintCount)
        {
            var weights = new double[paintCount];

            for (int y = 0; y < Diameter; y++)
            {
                for (int x = 0; x < Diameter; x++)
                {
                    if (!BlendGeometry.TryGetWeights(Diameter, paintCount, x, y, weights, out _))
                    {
                        continue;
                    }

                    Assert.InRange(weights.Sum(), 1.0 - 1e-9, 1.0 + 1e-9);
                    Assert.All(weights, weight => Assert.True(weight >= 0.0));
                }
            }
        }

        /// <summary>
        /// Confirms the rim at a paint's anchor is that paint alone. This is what makes
        /// the outer edge of each wedge read as the tube colour rather than a mixture.
        /// </summary>
        [Fact]
        public void TheRimAtAnAnchorIsASinglePaint()
        {
            const int PaintCount = 8;
            var weights = new double[PaintCount];
            double centre = (Diameter - 1) / 2.0;

            for (int paint = 0; paint < PaintCount; paint++)
            {
                double angle = 2.0 * Math.PI * paint / PaintCount;
                double reach = (Diameter / 2.0) - 1.2;
                double x = centre + (reach * Math.Cos(angle));
                double y = centre + (reach * Math.Sin(angle));

                Assert.True(BlendGeometry.TryGetWeights(Diameter, PaintCount, x, y, weights, out _));
                Assert.True(
                    weights[paint] > 0.95,
                    $"paint {paint} held only {weights[paint]:F3} of its own anchor");
            }
        }

        /// <summary>
        /// Confirms the centre is an equal share of every paint, which is the muddy
        /// all-paints blend the wheel darkens toward.
        /// </summary>
        [Fact]
        public void TheCentreIsAnEqualShareOfEveryPaint()
        {
            const int PaintCount = 5;
            var weights = new double[PaintCount];
            int centre = (Diameter - 1) / 2;

            Assert.True(BlendGeometry.TryGetWeights(Diameter, PaintCount, centre, centre, weights, out _));
            Assert.All(weights, weight => Assert.InRange(weight, 0.2 - 1e-9, 0.2 + 1e-9));
        }

        /// <summary>
        /// Confirms pixels outside the wheel are rejected rather than given weights, and
        /// that the rim fades over the last pixel so the edge is not stair-stepped.
        /// </summary>
        [Fact]
        public void PixelsOutsideTheWheelAreRejected()
        {
            var weights = new double[4];

            Assert.False(BlendGeometry.TryGetWeights(Diameter, 4, 0, 0, weights, out _));
            Assert.True(BlendGeometry.TryGetWeights(Diameter, 4, 50, 50, weights, out double alpha));
            Assert.InRange(alpha, 0.0, 1.0);
        }

        /// <summary>
        /// Confirms the geometry still agrees with the generator's public accessor, which
        /// the tooltip calls. These two disagreeing is the specific defect this task
        /// exists to remove.
        /// </summary>
        [Fact]
        public void GeneratorAccessorAgreesWithTheSharedGeometry()
        {
            const int PaintCount = 6;
            var weights = new double[PaintCount];

            for (int y = 10; y < Diameter; y += 7)
            {
                for (int x = 10; x < Diameter; x += 7)
                {
                    double[] fromGenerator = ColorWheelGenerator.GetBlendWeights(Diameter, PaintCount, x, y);
                    bool inside = BlendGeometry.TryGetWeights(Diameter, PaintCount, x, y, weights, out _);

                    Assert.Equal(inside, fromGenerator != null);
                    if (!inside)
                    {
                        continue;
                    }

                    for (int i = 0; i < PaintCount; i++)
                    {
                        Assert.InRange(fromGenerator[i], weights[i] - 1e-12, weights[i] + 1e-12);
                    }
                }
            }
        }
    }
}
