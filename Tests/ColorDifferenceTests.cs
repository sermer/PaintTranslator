using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Checks the CIEDE2000 implementation against the supplementary test data from
    /// Sharma, Wu and Dalal, "The CIEDE2000 Color-Difference Formula: Implementation
    /// Notes, Supplementary Test Data, and Mathematical Observations" (Color Research
    /// and Application 30(1), 2005). Those 34 pairs were chosen specifically to walk an
    /// implementation through the places the formula is easy to get wrong: hue angles
    /// straddling the 0/360 degree wrap, near-neutral colors where the hue term becomes
    /// unstable, and the discontinuities the paper documents. Passing all of them is
    /// the accepted bar for saying an implementation is correct, so the whole set is
    /// reproduced here rather than a sample of it.
    /// <para>
    /// Also covers the plain-language reporting built on top of the number, which is
    /// what the tooltip actually shows.
    /// </para>
    /// </summary>
    public class ColorDifferenceTests
    {
        /// <summary>
        /// Confirms the color difference matches the published expected value for every
        /// pair in the reference data set.
        /// </summary>
        /// <param name="referenceL">The reference color's lightness.</param>
        /// <param name="referenceA">The reference color's green-red coordinate.</param>
        /// <param name="referenceB">The reference color's blue-yellow coordinate.</param>
        /// <param name="sampleL">The sample color's lightness.</param>
        /// <param name="sampleA">The sample color's green-red coordinate.</param>
        /// <param name="sampleB">The sample color's blue-yellow coordinate.</param>
        /// <param name="expected">The published CIEDE2000 difference for the pair.</param>
        [Theory]
        [InlineData(50.0000, 2.6772, -79.7751, 50.0000, 0.0000, -82.7485, 2.0425)]
        [InlineData(50.0000, 3.1571, -77.2803, 50.0000, 0.0000, -82.7485, 2.8615)]
        [InlineData(50.0000, 2.8361, -74.0200, 50.0000, 0.0000, -82.7485, 3.4412)]
        [InlineData(50.0000, -1.3802, -84.2814, 50.0000, 0.0000, -82.7485, 1.0000)]
        [InlineData(50.0000, -1.1848, -84.8006, 50.0000, 0.0000, -82.7485, 1.0000)]
        [InlineData(50.0000, -0.9009, -85.5211, 50.0000, 0.0000, -82.7485, 1.0000)]
        [InlineData(50.0000, 0.0000, 0.0000, 50.0000, -1.0000, 2.0000, 2.3669)]
        [InlineData(50.0000, -1.0000, 2.0000, 50.0000, 0.0000, 0.0000, 2.3669)]
        [InlineData(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0009, 7.1792)]
        [InlineData(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0010, 7.1792)]
        [InlineData(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0011, 7.2195)]
        [InlineData(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0012, 7.2195)]
        [InlineData(50.0000, -0.0010, 2.4900, 50.0000, 0.0009, -2.4900, 4.8045)]
        [InlineData(50.0000, -0.0010, 2.4900, 50.0000, 0.0010, -2.4900, 4.8045)]
        [InlineData(50.0000, -0.0010, 2.4900, 50.0000, 0.0011, -2.4900, 4.7461)]
        [InlineData(50.0000, 2.5000, 0.0000, 50.0000, 0.0000, -2.5000, 4.3065)]
        [InlineData(50.0000, 2.5000, 0.0000, 73.0000, 25.0000, -18.0000, 27.1492)]
        [InlineData(50.0000, 2.5000, 0.0000, 61.0000, -5.0000, 29.0000, 22.8977)]
        [InlineData(50.0000, 2.5000, 0.0000, 56.0000, -27.0000, -3.0000, 31.9030)]
        [InlineData(50.0000, 2.5000, 0.0000, 58.0000, 24.0000, 15.0000, 19.4535)]
        [InlineData(50.0000, 2.5000, 0.0000, 50.0000, 3.1736, 0.5854, 1.0000)]
        [InlineData(50.0000, 2.5000, 0.0000, 50.0000, 3.2972, 0.0000, 1.0000)]
        [InlineData(50.0000, 2.5000, 0.0000, 50.0000, 1.8634, 0.5757, 1.0000)]
        [InlineData(50.0000, 2.5000, 0.0000, 50.0000, 3.2592, 0.3350, 1.0000)]
        [InlineData(60.2574, -34.0099, 36.2677, 60.4626, -34.1751, 39.4387, 1.2644)]
        [InlineData(63.0109, -31.0961, -5.8663, 62.8187, -29.7946, -4.0864, 1.2630)]
        [InlineData(61.2901, 3.7196, -5.3901, 61.4292, 2.2480, -4.9620, 1.8731)]
        [InlineData(35.0831, -44.1164, 3.7933, 35.0232, -40.0716, 1.5901, 1.8645)]
        [InlineData(22.7233, 20.0904, -46.6940, 23.0331, 14.9730, -42.5619, 2.0373)]
        [InlineData(36.4612, 47.8580, 18.3852, 36.2715, 50.5065, 21.2231, 1.4146)]
        [InlineData(90.8027, -2.0831, 1.4410, 91.1528, -1.6435, 0.0447, 1.4441)]
        [InlineData(90.9257, -0.5406, -0.9208, 88.6381, -0.8985, -0.7239, 1.5381)]
        [InlineData(6.7747, -0.2908, -2.4247, 5.8714, -0.0985, -2.2286, 0.6377)]
        [InlineData(2.0776, 0.0795, -1.1350, 0.9033, -0.0636, -0.5514, 0.9082)]
        public void MatchesThePublishedCieDe2000ReferenceData(
            double referenceL, double referenceA, double referenceB,
            double sampleL, double sampleA, double sampleB,
            double expected)
        {
            double actual = ColorDifference.CieDe2000(
                referenceL, referenceA, referenceB, sampleL, sampleA, sampleB);

            Assert.Equal(expected, actual, 4);
        }

        /// <summary>
        /// Confirms the difference does not depend on which color is called the
        /// reference. CIEDE2000 averages its two inputs rather than dividing by one of
        /// them, so unlike CMC and CIE94 it is symmetric; the nearest-match scan relies
        /// on that, because a metric that changed with argument order would rank
        /// candidates differently depending on how the loop was written.
        /// </summary>
        [Fact]
        public void MeasuresTheSameDifferenceInEitherDirection()
        {
            double forward = ColorDifference.CieDe2000(50.0, 2.5, 0.0, 73.0, 25.0, -18.0);
            double backward = ColorDifference.CieDe2000(73.0, 25.0, -18.0, 50.0, 2.5, 0.0);

            Assert.Equal(forward, backward, 10);
        }

        /// <summary>
        /// Confirms match quality is described in words tied to the perceptibility
        /// thresholds the number means something against: about one unit is the point a
        /// difference becomes visible at all, two to three is visible with the two
        /// colors side by side, and beyond five they simply read as different colors.
        /// A bare number tells a painter nothing without that anchoring.
        /// </summary>
        /// <param name="deltaE2000">The color difference to describe.</param>
        /// <param name="expected">The expected description.</param>
        [Theory]
        [InlineData(0.4, "indistinguishable")]
        [InlineData(1.5, "very close")]
        [InlineData(2.5, "close")]
        [InlineData(4.0, "noticeable")]
        [InlineData(12.0, "clearly different")]
        public void DescribesMatchQualityAgainstPerceptibilityThresholds(double deltaE2000, string expected)
        {
            Assert.Equal(expected, ColorDifference.DescribeQuality(deltaE2000));
        }

        /// <summary>
        /// Confirms the direction of an imperfect match is reported, not just its size.
        /// When a palette cannot reach a target, knowing which way it falls short is
        /// what lets a painter compensate by hand, and it turns a silent failure into
        /// usable information.
        /// </summary>
        /// <param name="mixL">The mixture's lightness.</param>
        /// <param name="mixA">The mixture's green-red coordinate.</param>
        /// <param name="mixB">The mixture's blue-yellow coordinate.</param>
        /// <param name="expected">The expected description of the shift.</param>
        [Theory]
        [InlineData(70.0, 30.0, 0.0, "lighter")]
        [InlineData(30.0, 30.0, 0.0, "darker")]
        [InlineData(50.0, 10.0, 0.0, "duller")]
        [InlineData(50.0, 60.0, 0.0, "more intense")]
        [InlineData(70.0, 10.0, 0.0, "lighter and duller")]
        [InlineData(30.0, 60.0, 0.0, "darker and more intense")]
        public void DescribesWhichWayAnImperfectMatchFallsShort(
            double mixL, double mixA, double mixB, string expected)
        {
            // Target sits at mid lightness with moderate chroma, so each case differs
            // from it in one or both directions by more than the reporting threshold.
            Assert.Equal(expected, ColorDifference.DescribeShift(50.0, 30.0, 0.0, mixL, mixA, mixB));
        }

        /// <summary>
        /// Confirms a match close enough in both lightness and chroma is reported as
        /// having no direction, so the tooltip stays quiet instead of describing a
        /// shift too small to see or to correct for.
        /// </summary>
        [Fact]
        public void ReportsNoShiftWhenTheMatchIsCloseInBothLightnessAndChroma()
        {
            Assert.Null(ColorDifference.DescribeShift(50.0, 30.0, 0.0, 50.3, 30.2, 0.1));
        }
    }
}
