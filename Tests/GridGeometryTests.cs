using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class GridGeometryTests
    {
        [Fact]
        public void TwoColumnsOneRowGivesOneDividerAndTheBorder()
        {
            var bounds = new RectangleF(10, 20, 100, 50);
            IReadOnlyList<GridGeometry.Segment> segments = GridGeometry.Segments(bounds, 2, 1);

            Assert.Equal(5, segments.Count);
            Assert.Contains(new GridGeometry.Segment(new PointF(60, 20), new PointF(60, 70)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(10, 20), new PointF(110, 20)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(10, 70), new PointF(110, 70)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(10, 20), new PointF(10, 70)), segments);
            Assert.Contains(new GridGeometry.Segment(new PointF(110, 20), new PointF(110, 70)), segments);
        }

        [Fact]
        public void DividersAreFractionsOfTheSpanNotAccumulatedSteps()
        {
            var bounds = new RectangleF(0, 0, 10, 10);
            IReadOnlyList<GridGeometry.Segment> segments = GridGeometry.Segments(bounds, 3, 3);
            float[] xs = segments.Where(s => s.Start.X == s.End.X && s.Start.X > 0 && s.Start.X < 10)
                .Select(s => s.Start.X).OrderBy(x => x).ToArray();

            Assert.Equal(new[] { 10f / 3f, 20f / 3f }, xs);
        }

        [Fact]
        public void DividersExcludeTheBorder()
        {
            var bounds = new RectangleF(10, 20, 100, 50);
            IReadOnlyList<GridGeometry.Segment> dividers = GridGeometry.Dividers(bounds, 2, 1);

            Assert.Equal(
                new[] { new GridGeometry.Segment(new PointF(60, 20), new PointF(60, 70)) },
                dividers);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        public void RejectsFewerThanOneSegment(int columns, int rows)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => GridGeometry.Segments(new RectangleF(0, 0, 10, 10), columns, rows));
        }
    }
}
