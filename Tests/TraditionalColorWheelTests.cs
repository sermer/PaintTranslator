using System;
using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class TraditionalColorWheelTests
    {
        [Fact]
        public void TraditionalWheelPlacesRedYellowAndBlueAtArtistPrimaryPositions()
        {
            const int diameter = 201;
            using Bitmap wheel = ColorWheelGenerator.CreateTraditional(diameter);

            AssertPrimary(wheel, artistAngle: 0.0, expected: "red");
            AssertPrimary(wheel, artistAngle: 120.0, expected: "yellow");
            AssertPrimary(wheel, artistAngle: 240.0, expected: "blue");
        }

        [Fact]
        public void TraditionalWheelFadesToWhiteAtTheCentreAndIsTransparentOutside()
        {
            const int diameter = 201;
            using Bitmap wheel = ColorWheelGenerator.CreateTraditional(diameter);

            Color centre = wheel.GetPixel(diameter / 2, diameter / 2);
            Assert.True(centre.R >= 250 && centre.G >= 250 && centre.B >= 250);
            Assert.Equal(0, wheel.GetPixel(0, 0).A);
        }

        [Fact]
        public void TraditionalWheelRejectsAnInvalidDiameter()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ColorWheelGenerator.CreateTraditional(1));
        }

        private static void AssertPrimary(Bitmap wheel, double artistAngle, string expected)
        {
            double centre = (wheel.Width - 1) * 0.5;
            double radius = wheel.Width * 0.40;
            double radians = (artistAngle - 90.0) * (Math.PI / 180.0);
            int x = (int)Math.Round(centre + (Math.Cos(radians) * radius));
            int y = (int)Math.Round(centre + (Math.Sin(radians) * radius));
            Color colour = wheel.GetPixel(x, y);

            Assert.Equal(255, colour.A);
            switch (expected)
            {
                case "red":
                    Assert.True(colour.R > 220 && colour.G < 80 && colour.B < 80, colour.ToString());
                    break;
                case "yellow":
                    Assert.True(colour.R > 220 && colour.G > 220 && colour.B < 80, colour.ToString());
                    break;
                case "blue":
                    Assert.True(colour.B > 220 && colour.R < 80 && colour.G < 80, colour.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expected));
            }
        }
    }
}
