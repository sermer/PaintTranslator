using System;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// Reports how far apart two colors look, and says so in terms a painter can act
    /// on. Two separate jobs live here for a reason. Searching for the closest mixture
    /// needs a metric that behaves at large differences, which is what
    /// <see cref="PaintBlendMatcher.PerceptualDistance"/> provides; once a winner is
    /// chosen, describing how good the match is calls for CIEDE2000, which is the
    /// accepted measure of small differences and the one whose numbers have published
    /// perceptibility thresholds attached.
    /// </summary>
    public static class ColorDifference
    {
        /// <summary>
        /// Computes the CIEDE2000 color difference between two CIELAB colors.
        /// </summary>
        /// <param name="firstL">The first color's lightness.</param>
        /// <param name="firstA">The first color's green-red coordinate.</param>
        /// <param name="firstB">The first color's blue-yellow coordinate.</param>
        /// <param name="secondL">The second color's lightness.</param>
        /// <param name="secondA">The second color's green-red coordinate.</param>
        /// <param name="secondB">The second color's blue-yellow coordinate.</param>
        /// <returns>The CIEDE2000 difference, where roughly 1 is the threshold of
        /// visibility and values above 5 read as different colors.</returns>
        public static double CieDe2000(
            double firstL, double firstA, double firstB,
            double secondL, double secondA, double secondB)
        {
            double chromaFirst = Math.Sqrt((firstA * firstA) + (firstB * firstB));
            double chromaSecond = Math.Sqrt((secondA * secondA) + (secondB * secondB));
            double meanChroma = (chromaFirst + chromaSecond) / 2.0;

            // The a* axis is stretched for near-neutral colors, where CIELAB
            // understates how visible a small chromatic shift is. The 25^7 constant
            // places the correction's midpoint well outside the range of real
            // surface colors, so it tapers off rather than switching on abruptly.
            double meanChroma7 = Math.Pow(meanChroma, 7.0);
            double g = 0.5 * (1.0 - Math.Sqrt(meanChroma7 / (meanChroma7 + Pow25To7)));
            double adjustedFirstA = (1.0 + g) * firstA;
            double adjustedSecondA = (1.0 + g) * secondA;

            double adjustedChromaFirst = Math.Sqrt((adjustedFirstA * adjustedFirstA) + (firstB * firstB));
            double adjustedChromaSecond = Math.Sqrt((adjustedSecondA * adjustedSecondA) + (secondB * secondB));
            double meanAdjustedChroma = (adjustedChromaFirst + adjustedChromaSecond) / 2.0;

            double hueFirst = HueAngle(firstB, adjustedFirstA);
            double hueSecond = HueAngle(secondB, adjustedSecondA);

            double deltaLightness = secondL - firstL;
            double deltaChroma = adjustedChromaSecond - adjustedChromaFirst;

            // A color with no chroma has no meaningful hue, so any hue difference
            // involving one is defined as zero rather than read off a noisy angle.
            bool eitherIsNeutral = adjustedChromaFirst * adjustedChromaSecond == 0.0;
            double deltaHueAngle = eitherIsNeutral ? 0.0 : ShortestAngle(hueSecond - hueFirst);
            double deltaHue = 2.0
                * Math.Sqrt(adjustedChromaFirst * adjustedChromaSecond)
                * Math.Sin(DegreesToRadians(deltaHueAngle) / 2.0);

            double meanLightness = (firstL + secondL) / 2.0;
            double meanHue = MeanHueAngle(hueFirst, hueSecond, adjustedChromaFirst, adjustedChromaSecond);

            // Weighting for how tolerant the eye is at this lightness, chroma and hue.
            // The hue term is a four-lobed correction: the eye discriminates hue best
            // in the blues and worst in the yellows, and this is the empirical fit.
            double lightnessOffset = meanLightness - 50.0;
            double lightnessWeight = 1.0
                + ((0.015 * lightnessOffset * lightnessOffset)
                    / Math.Sqrt(20.0 + (lightnessOffset * lightnessOffset)));
            double chromaWeight = 1.0 + (0.045 * meanAdjustedChroma);
            double hueShape = 1.0
                - (0.17 * Math.Cos(DegreesToRadians(meanHue - 30.0)))
                + (0.24 * Math.Cos(DegreesToRadians(2.0 * meanHue)))
                + (0.32 * Math.Cos(DegreesToRadians((3.0 * meanHue) + 6.0)))
                - (0.20 * Math.Cos(DegreesToRadians((4.0 * meanHue) - 63.0)));
            double hueWeight = 1.0 + (0.015 * meanAdjustedChroma * hueShape);

            // Chroma and hue errors are correlated in the blue region, where a shift
            // in one is partly indistinguishable from a shift in the other; this term
            // stops that overlap being counted twice.
            double meanHueOffset = (meanHue - 275.0) / 25.0;
            double rotationTerm = -2.0
                * Math.Sqrt(BlueRegionDamping(meanAdjustedChroma))
                * Math.Sin(DegreesToRadians(60.0 * Math.Exp(-meanHueOffset * meanHueOffset)));

            double lightnessRatio = deltaLightness / lightnessWeight;
            double chromaRatio = deltaChroma / chromaWeight;
            double hueRatio = deltaHue / hueWeight;

            return Math.Sqrt(
                (lightnessRatio * lightnessRatio)
                + (chromaRatio * chromaRatio)
                + (hueRatio * hueRatio)
                + (rotationTerm * chromaRatio * hueRatio));
        }

        /// <summary>
        /// Describes a color difference in words, anchored to the thresholds at which a
        /// difference becomes visible.
        /// </summary>
        /// <param name="deltaE2000">The CIEDE2000 difference to describe.</param>
        /// <returns>A short phrase describing how close the match is.</returns>
        public static string DescribeQuality(double deltaE2000)
        {
            if (deltaE2000 < 1.0)
            {
                return "indistinguishable";
            }
            if (deltaE2000 < 2.0)
            {
                return "very close";
            }
            if (deltaE2000 < 3.0)
            {
                return "close";
            }
            if (deltaE2000 < 5.0)
            {
                return "noticeable";
            }

            return "clearly different";
        }

        /// <summary>
        /// Describes which way a mixture misses its target, in lightness and in chroma.
        /// </summary>
        /// <param name="targetL">The target color's lightness.</param>
        /// <param name="targetA">The target color's green-red coordinate.</param>
        /// <param name="targetB">The target color's blue-yellow coordinate.</param>
        /// <param name="mixL">The mixture's lightness.</param>
        /// <param name="mixA">The mixture's green-red coordinate.</param>
        /// <param name="mixB">The mixture's blue-yellow coordinate.</param>
        /// <returns>A phrase such as "lighter and duller", or null when the mixture is
        /// close enough in both respects for a direction to be worth reporting.</returns>
        public static string DescribeShift(
            double targetL, double targetA, double targetB,
            double mixL, double mixA, double mixB)
        {
            // Below this the shift is smaller than a painter could see or correct by
            // hand, so naming a direction would be false precision.
            const double ReportingThreshold = 1.0;

            double deltaLightness = mixL - targetL;
            double deltaChroma = Math.Sqrt((mixA * mixA) + (mixB * mixB))
                - Math.Sqrt((targetA * targetA) + (targetB * targetB));

            string lightness = null;
            if (Math.Abs(deltaLightness) >= ReportingThreshold)
            {
                lightness = deltaLightness > 0.0 ? "lighter" : "darker";
            }

            string chroma = null;
            if (Math.Abs(deltaChroma) >= ReportingThreshold)
            {
                chroma = deltaChroma > 0.0 ? "more intense" : "duller";
            }

            if (lightness == null)
            {
                return chroma;
            }

            return chroma == null ? lightness : $"{lightness} and {chroma}";
        }

        // 25^7, the constant anchoring the near-neutral chroma correction.
        private const double Pow25To7 = 6103515625.0;

        /// <summary>
        /// Computes how strongly the chroma and hue interaction term applies at a given
        /// chroma, rising toward 1 for saturated colors and falling toward 0 for
        /// near-neutral ones where the two errors are no longer confusable.
        /// </summary>
        /// <param name="meanAdjustedChroma">The mean of the two adjusted chroma values.</param>
        /// <returns>The damping factor for the interaction term.</returns>
        private static double BlueRegionDamping(double meanAdjustedChroma)
        {
            double raised = Math.Pow(meanAdjustedChroma, 7.0);
            return raised / (raised + Pow25To7);
        }

        /// <summary>
        /// Computes a CIELAB hue angle in degrees over the full circle.
        /// </summary>
        /// <param name="b">The blue-yellow coordinate.</param>
        /// <param name="adjustedA">The chroma-corrected green-red coordinate.</param>
        /// <returns>The hue angle in degrees from 0 to 360, or 0 for a neutral color.</returns>
        private static double HueAngle(double b, double adjustedA)
        {
            if (b == 0.0 && adjustedA == 0.0)
            {
                return 0.0;
            }

            double degrees = RadiansToDegrees(Math.Atan2(b, adjustedA));
            return degrees < 0.0 ? degrees + 360.0 : degrees;
        }

        /// <summary>
        /// Reduces a hue difference to the shorter way round the circle.
        /// </summary>
        /// <param name="difference">The raw difference in degrees.</param>
        /// <returns>The equivalent difference between -180 and 180 degrees.</returns>
        private static double ShortestAngle(double difference)
        {
            if (difference > 180.0)
            {
                return difference - 360.0;
            }

            return difference < -180.0 ? difference + 360.0 : difference;
        }

        /// <summary>
        /// Averages two hue angles the short way round the circle, so that hues
        /// straddling zero degrees average to a red rather than to a cyan.
        /// </summary>
        /// <param name="hueFirst">The first hue angle in degrees.</param>
        /// <param name="hueSecond">The second hue angle in degrees.</param>
        /// <param name="adjustedChromaFirst">The first color's adjusted chroma.</param>
        /// <param name="adjustedChromaSecond">The second color's adjusted chroma.</param>
        /// <returns>The mean hue angle in degrees.</returns>
        private static double MeanHueAngle(
            double hueFirst, double hueSecond,
            double adjustedChromaFirst, double adjustedChromaSecond)
        {
            // With either color neutral there is no hue to average, and the sum
            // convention below keeps the result finite for the weighting terms.
            if (adjustedChromaFirst * adjustedChromaSecond == 0.0)
            {
                return hueFirst + hueSecond;
            }

            double separation = Math.Abs(hueFirst - hueSecond);
            double sum = hueFirst + hueSecond;
            if (separation <= 180.0)
            {
                return sum / 2.0;
            }

            return sum < 360.0 ? (sum + 360.0) / 2.0 : (sum - 360.0) / 2.0;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">The angle in radians.</param>
        /// <returns>The angle in degrees.</returns>
        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}
