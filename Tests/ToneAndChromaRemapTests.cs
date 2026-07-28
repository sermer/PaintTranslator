using System;
using System.Collections.Generic;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Imaging.Styles.Stages;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins <see cref="ToneAndChromaRemap"/>'s numeric behaviour directly, independent
    /// of any style that wires it up: the contrast/key pivot, the lightness clamp, and
    /// the chroma knee's identity-at-default, ceiling, monotonicity and hue-preserving
    /// properties.
    /// <para>
    /// Every test builds its own <see cref="ParameterValues"/> from
    /// <see cref="ToneAndChromaRemap.Parameters"/> rather than routing through a style,
    /// so a regression here can never be masked by a registry-level default override
    /// changing what "leaving the stage alone" means.
    /// </para>
    /// </summary>
    public class ToneAndChromaRemapTests
    {
        private const double Tolerance = 1e-9;

        /// <summary>
        /// At its declared defaults — contrast 1.0, key 0, chroma gain 1.0 — the stage
        /// must return every one of a spread of colours unchanged to within 1e-9,
        /// including their a* and b* channels. This is the property that lets a style
        /// declare the stage and leave it alone without that being observably
        /// different from never declaring it at all, which is what every other stage
        /// in this pipeline already guarantees at its own defaults.
        /// <para>
        /// A remap that special-cased exactly this input spread (or exactly these
        /// three parameter values) and did something else everywhere else would still
        /// pass this test alone; that failure mode is what
        /// <see cref="ContrastPivotsAboutMidLightness"/>,
        /// <see cref="ChromaBoostIsStrictlyMonotonic"/> and the other tests below pin
        /// independently, at parameter values away from the defaults.
        /// </para>
        /// </summary>
        [Fact]
        public void DefaultParametersAreAnIdentity()
        {
            var remap = new ToneAndChromaRemap();
            var values = new ParameterValues(remap.Parameters);
            var context = new RenderContext(64, 64, 4.0, 60.0);

            (double L, double A, double B)[] colours =
            {
                (50.0, 0.0, 0.0),      // neutral mid grey
                (0.0, 0.0, 0.0),       // black
                (100.0, 0.0, 0.0),     // white
                (65.0, 20.0, -35.0),   // an ordinary photographic colour
                (32.0, -45.0, 10.0),   // negative a*
                (78.0, 12.0, 55.0),    // high b*, positive
                (10.0, -5.0, -8.0),    // dark, small chroma
                (55.0, 70.7, 0.0),     // the library's most chromatic masstone, on the a axis
                (40.0, 0.0, 120.0),    // chroma far beyond anything achievable
            };

            foreach ((double l, double a, double b) in colours)
            {
                remap.Map(l, a, b, out double mappedL, out double mappedA, out double mappedB, in context, values);

                Assert.True(Math.Abs(mappedL - l) < Tolerance, $"L* moved at defaults: {l} -> {mappedL}");
                Assert.True(Math.Abs(mappedA - a) < Tolerance, $"a* moved at defaults: {a} -> {mappedA}");
                Assert.True(Math.Abs(mappedB - b) < Tolerance, $"b* moved at defaults: {b} -> {mappedB}");
            }
        }

        /// <summary>
        /// L* 50 must be unmoved regardless of contrast, since it is the pivot the
        /// control opens and closes the range about; L* 80 at contrast 0.5 must land
        /// at 65 and L* 20 at contrast 0.5 at 35, which together pin both the pivot's
        /// location and the slope, not merely that some contraction happened.
        /// <para>
        /// An implementation that pivoted about the wrong value (say 40 instead of 50)
        /// but happened to get the slope right would move L* 50 at any contrast other
        /// than 1.0, so testing the pivot at several distinct contrasts — not just the
        /// one contrast the 80/65 and 20/35 pair uses — is what catches that.
        /// </para>
        /// </summary>
        [Fact]
        public void ContrastPivotsAboutMidLightness()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);

            foreach (double contrast in new[] { 0.3, 0.5, 1.0, 1.5, 2.0 })
            {
                var values = new ParameterValues(remap.Parameters);
                values.Set("contrast", contrast);

                remap.Map(50.0, 0.0, 0.0, out double mappedL, out _, out _, in context, values);
                Assert.True(
                    Math.Abs(mappedL - 50.0) < Tolerance,
                    $"L* 50 moved to {mappedL} at contrast {contrast}");
            }

            var half = new ParameterValues(remap.Parameters);
            half.Set("contrast", 0.5);

            remap.Map(80.0, 0.0, 0.0, out double mappedHigh, out _, out _, in context, half);
            Assert.True(Math.Abs(mappedHigh - 65.0) < Tolerance, $"L* 80 at contrast 0.5 was {mappedHigh}, expected 65");

            remap.Map(20.0, 0.0, 0.0, out double mappedLow, out _, out _, in context, half);
            Assert.True(Math.Abs(mappedLow - 35.0) < Tolerance, $"L* 20 at contrast 0.5 was {mappedLow}, expected 35");

            // A third point off the two the brief names, at a different contrast, so
            // an implementation cannot pass by special-casing exactly the 80/65 and
            // 20/35 pair.
            var doubled = new ParameterValues(remap.Parameters);
            doubled.Set("contrast", 2.0);
            remap.Map(60.0, 0.0, 0.0, out double mappedDoubled, out _, out _, in context, doubled);
            Assert.True(Math.Abs(mappedDoubled - 70.0) < Tolerance, $"L* 60 at contrast 2.0 was {mappedDoubled}, expected 70");
        }

        /// <summary>
        /// The key parameter is documented — and implemented — to shift lightness
        /// after the contrast multiply, not before, so the pivot and the shift stay
        /// independent controls: moving key never changes how far contrast opens or
        /// closes the range, only where the whole result lands. At contrast 0.55 and
        /// key 4 — the exact values Tonalism registers as its own defaults — L* 80
        /// must land at 70.5 (<c>50 + (80 - 50) * 0.55 + 4</c>). An implementation that
        /// instead added key before the multiply would compute
        /// <c>50 + (80 + 4 - 50) * 0.55 = 68.7</c> for the same inputs: a different,
        /// wrong number that every other test in this file leaves undetected, because
        /// all of them leave key at its default of zero, where the two orderings are
        /// indistinguishable.
        /// </summary>
        [Fact]
        public void KeyShiftAppliesAfterTheContrastMultiply()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);
            var values = new ParameterValues(remap.Parameters);
            values.Set("contrast", 0.55);
            values.Set("key", 4.0);

            remap.Map(80.0, 0.0, 0.0, out double mappedL, out _, out _, in context, values);

            Assert.True(
                Math.Abs(mappedL - 70.5) < Tolerance,
                $"L* 80 at contrast 0.55, key 4 was {mappedL}, expected 70.5 (key must apply after the contrast multiply)");
        }

        /// <summary>
        /// L* 95 at contrast 2.0 would compute to 140 unclamped; the stage must return
        /// 100 instead. The symmetric low-end case is checked too — L* 5 at contrast
        /// 2.0 would compute to -40 — so an implementation that clamped only one end
        /// of the range cannot pass.
        /// </summary>
        [Fact]
        public void LightnessIsClampedToTheLegalRange()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);
            var values = new ParameterValues(remap.Parameters);
            values.Set("contrast", 2.0);

            remap.Map(95.0, 0.0, 0.0, out double mappedHigh, out _, out _, in context, values);
            Assert.True(Math.Abs(mappedHigh - 100.0) < Tolerance, $"L* 95 at contrast 2.0 was {mappedHigh}, expected clamped 100");

            remap.Map(5.0, 0.0, 0.0, out double mappedLow, out _, out _, in context, values);
            Assert.True(Math.Abs(mappedLow - 0.0) < Tolerance, $"L* 5 at contrast 2.0 was {mappedLow}, expected clamped 0");
        }

        /// <summary>
        /// At chroma gain 3.0 — the parameter's own declared maximum, and the only
        /// gain at which the doc comment on <c>ScaleChroma</c> claims the bound holds
        /// for every input — feeding C* from 5 to 120 must never produce a mapped
        /// chroma at or above the render's achievable ceiling of 60.
        /// <para>
        /// A naive clamp (<c>Math.Min(scaled, ceiling)</c> applied to a plain
        /// multiplier) would also pass this test in isolation, since it too never
        /// exceeds the ceiling; what rules that implementation out is
        /// <see cref="ChromaBoostIsStrictlyMonotonic"/>, since a clamp is constant
        /// over every input the multiplier already pushed past the ceiling and so is
        /// not strictly increasing there.
        /// </para>
        /// </summary>
        [Fact]
        public void ChromaBoostNeverExceedsTheAchievableCeiling()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);
            var values = new ParameterValues(remap.Parameters);
            values.Set("chroma", 3.0);

            foreach (double inputChroma in new[] { 5.0, 10.0, 30.0, 50.0, 70.0, 120.0 })
            {
                remap.Map(50.0, inputChroma, 0.0, out _, out double mappedA, out double mappedB, in context, values);
                double resultChroma = Math.Sqrt((mappedA * mappedA) + (mappedB * mappedB));

                Assert.True(
                    resultChroma < 60.0,
                    $"C* {inputChroma} at gain 3.0 mapped to {resultChroma}, which is not below the ceiling of 60");
            }
        }

        /// <summary>
        /// Feeding ascending source chroma must produce ascending mapped chroma, at
        /// every gain across the parameter's range — this is the property that keeps
        /// distinct source colours landing on distinct achievable candidates instead of
        /// banding together on the same boundary one. A naive multiplier-then-clip
        /// implementation fails this directly: once the multiplier's output crosses
        /// the ceiling, every further input clips to the same constant and stops
        /// ascending.
        /// </summary>
        [Fact]
        public void ChromaBoostIsStrictlyMonotonic()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);
            double[] ascendingChroma = { 1.0, 2.0, 5.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 90.0, 120.0, 200.0 };

            foreach (double gain in new[] { 0.2, 0.45, 1.0, 1.5, 2.0, 3.0 })
            {
                var values = new ParameterValues(remap.Parameters);
                values.Set("chroma", gain);

                double previous = double.NegativeInfinity;
                foreach (double inputChroma in ascendingChroma)
                {
                    remap.Map(50.0, inputChroma, 0.0, out _, out double mappedA, out double mappedB, in context, values);
                    double resultChroma = Math.Sqrt((mappedA * mappedA) + (mappedB * mappedB));

                    Assert.True(
                        resultChroma > previous,
                        $"gain {gain}: C* {inputChroma} mapped to {resultChroma}, which does not exceed the previous result {previous}");
                    previous = resultChroma;
                }
            }
        }

        /// <summary>
        /// Hue — atan2(b*, a*) — must be preserved to within 1e-9 across the whole
        /// gain range, since the chroma knee scales a* and b* by one shared factor
        /// rather than moving them independently. Checked at several colours away from
        /// the axes, so an implementation that happened to preserve hue only for a
        /// colour already sitting on the a* or b* axis could not pass.
        /// </summary>
        [Fact]
        public void HueIsUntouchedByAnyChromaChange()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);

            (double A, double B)[] colours =
            {
                (30.0, 40.0),
                (-25.0, 15.0),
                (-10.0, -60.0),
                (55.0, -5.0),
                (1.0, 0.5),
            };

            foreach (double gain in new[] { 0.2, 0.5, 1.0, 1.7, 2.4, 3.0 })
            {
                var values = new ParameterValues(remap.Parameters);
                values.Set("chroma", gain);

                foreach ((double a, double b) in colours)
                {
                    double expectedHue = Math.Atan2(b, a);
                    remap.Map(50.0, a, b, out _, out double mappedA, out double mappedB, in context, values);
                    double actualHue = Math.Atan2(mappedB, mappedA);

                    Assert.True(
                        Math.Abs(actualHue - expectedHue) < Tolerance,
                        $"gain {gain}, ({a},{b}): hue moved from {expectedHue} to {actualHue}");
                }
            }
        }

        /// <summary>
        /// A perfectly neutral colour — a* = b* = 0 — has no hue for a chroma boost to
        /// preserve or distort, and computing a scale factor as (boosted chroma) /
        /// (source chroma) would divide by zero there. It must come back exactly
        /// neutral at every gain, including the extremes of the parameter's range.
        /// </summary>
        [Fact]
        public void NeutralColoursSurviveAChromaBoost()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);

            foreach (double gain in new[] { 0.0, 0.45, 1.0, 2.0, 3.0 })
            {
                var values = new ParameterValues(remap.Parameters);
                values.Set("chroma", gain);

                remap.Map(50.0, 0.0, 0.0, out _, out double mappedA, out double mappedB, in context, values);

                Assert.Equal(0.0, mappedA);
                Assert.Equal(0.0, mappedB);
            }
        }

        /// <summary>
        /// Gain 0.0 is the lower boundary of <c>ScaleChroma</c>'s <c>gain &lt;= 1.0</c>
        /// branch — <c>scaled = gain * chroma</c> — and must drive a chromatic colour's
        /// mapped chroma to exactly zero, the same way <see cref="NeutralColoursSurviveAChromaBoost"/>
        /// pins the neutral input case rather than leaving it to be inferred from
        /// nearby gains. <see cref="ChromaBoostIsStrictlyMonotonic"/> exercises gain
        /// 0.2, not 0.0, so this boundary was otherwise untested: an implementation
        /// that clamped gain to some small positive floor before multiplying, or that
        /// divided by gain somewhere and guarded only against exactly-neutral colours,
        /// could pass every other test here and still fail to fully desaturate at the
        /// slider's own minimum.
        /// </summary>
        [Fact]
        public void ZeroChromaGainFullyDesaturatesANonNeutralColour()
        {
            var remap = new ToneAndChromaRemap();
            var context = new RenderContext(64, 64, 4.0, 60.0);
            var values = new ParameterValues(remap.Parameters);
            values.Set("chroma", 0.0);

            remap.Map(50.0, 30.0, -40.0, out _, out double mappedA, out double mappedB, in context, values);

            Assert.Equal(0.0, mappedA);
            Assert.Equal(0.0, mappedB);
        }
    }
}
