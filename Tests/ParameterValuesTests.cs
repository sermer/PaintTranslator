using System;
using System.Collections.Generic;
using PaintTranslator.Imaging.Styles;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Pins the per-stage parameter store. Values are held per stage <em>instance</em>
    /// rather than in one shared map, which is what lets two unrelated stages both
    /// declare a parameter called "strength" without either having to know the other
    /// exists — and therefore without a naming convention nobody would remember.
    /// </summary>
    public class ParameterValuesTests
    {
        [Fact]
        public void ValuesStartAtTheirDeclaredDefaults()
        {
            var values = new ParameterValues(TwoParameters());

            Assert.Equal(1.0, values["gain"]);
            Assert.Equal(4.0, values["radius"]);
        }

        [Fact]
        public void SettingAValueReadsItBack()
        {
            var values = new ParameterValues(TwoParameters());

            values.Set("gain", 1.75);

            Assert.Equal(1.75, values["gain"]);
        }

        /// <summary>
        /// Out-of-range values are clamped rather than rejected. The UI cannot produce
        /// them, but a style declared with a default outside its own range would
        /// otherwise fail at render time rather than at construction.
        /// </summary>
        [Fact]
        public void ValuesAreClampedToTheDeclaredRange()
        {
            var values = new ParameterValues(TwoParameters());

            values.Set("gain", 99.0);
            Assert.Equal(3.0, values["gain"]);

            values.Set("gain", -5.0);
            Assert.Equal(0.0, values["gain"]);
        }

        /// <summary>
        /// An unknown id is a programming error, not a runtime condition, so it throws
        /// rather than returning a silent zero that would show up as a slightly wrong
        /// picture.
        /// </summary>
        [Fact]
        public void AnUnknownParameterThrows()
        {
            var values = new ParameterValues(TwoParameters());

            Assert.Throws<KeyNotFoundException>(() => values["nonexistent"]);
            Assert.Throws<KeyNotFoundException>(() => values.Set("nonexistent", 1.0));
        }

        [Fact]
        public void ResetRestoresEveryDefault()
        {
            var values = new ParameterValues(TwoParameters());
            values.Set("gain", 2.5);
            values.Set("radius", 9.0);

            values.ResetToDefaults();

            Assert.Equal(1.0, values["gain"]);
            Assert.Equal(4.0, values["radius"]);
        }

        /// <summary>
        /// Two instances built from the same declarations must not share storage, or a
        /// style using one stage twice would have the two copies move together.
        /// </summary>
        [Fact]
        public void TwoInstancesDoNotShareStorage()
        {
            IReadOnlyList<StyleParameter> declarations = TwoParameters();
            var first = new ParameterValues(declarations);
            var second = new ParameterValues(declarations);

            first.Set("gain", 2.0);

            Assert.Equal(1.0, second["gain"]);
        }

        /// <summary>
        /// A style registry hand-writes <see cref="StyleParameter"/> declarations, and
        /// nothing stops one from naming a default outside its own declared range — a
        /// plausible typo, or bounds edited without revisiting the default. Such a
        /// declaration must still be absorbed quietly rather than surfacing later as a
        /// value no slider built from that same range could ever represent.
        /// </summary>
        [Fact]
        public void ResetClampsADefaultOutsideItsOwnDeclaredRange()
        {
            var declarations = new[]
            {
                new StyleParameter("tooLow", "Too Low", 0.0, 10.0, -5.0, string.Empty),
                new StyleParameter("tooHigh", "Too High", 0.0, 10.0, 15.0, string.Empty),
            };

            var values = new ParameterValues(declarations);

            // Construction seeds via ResetToDefaults, so a clamp present only in one of
            // the two call sites would otherwise slip through.
            Assert.Equal(0.0, values["tooLow"]);
            Assert.Equal(10.0, values["tooHigh"]);

            values.ResetToDefaults();

            Assert.Equal(0.0, values["tooLow"]);
            Assert.Equal(10.0, values["tooHigh"]);
        }

        /// <summary>
        /// Pins the constructor's null guard. Cheap to add alongside the clamp test
        /// above and costs nothing to keep, even though this codebase generally
        /// prefers pinning numeric behaviour over exception shape — <see
        /// cref="AnUnknownParameterThrows"/> already establishes that a thrown
        /// exception type is fair game here when the alternative is a silent null
        /// reference deeper in the pipeline.
        /// </summary>
        [Fact]
        public void ANullParameterListThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new ParameterValues(null));
        }

        private static IReadOnlyList<StyleParameter> TwoParameters()
        {
            return new[]
            {
                new StyleParameter("gain", "Gain", 0.0, 3.0, 1.0, string.Empty),
                new StyleParameter("radius", "Radius", 1.0, 32.0, 4.0, "px"),
            };
        }
    }
}
