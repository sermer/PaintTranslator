using System.Drawing;
using PaintTranslator.Imaging;
using PaintTranslator.Imaging.Styles;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    public class SourceFrameTests
    {
        [Fact]
        public void PixelCopiesCannotMutateTheFrame()
        {
            using var source = new Bitmap(3, 2);
            source.SetPixel(0, 0, Color.CornflowerBlue);
            SourceFrame frame = SourceFrame.Create(source);

            int expected = frame.CopyPixels()[0];
            int[] changed = frame.CopyPixels();
            changed[0] = 0;

            Assert.Equal(expected, frame.CopyPixels()[0]);
        }

        [Fact]
        public void BitmapAndFramePipelineEntrypointsProduceIdenticalPixels()
        {
            using var source = new Bitmap(16, 12);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    source.SetPixel(x, y, Color.FromArgb(
                        255,
                        (x * 17 + y * 3) & 0xFF,
                        (x * 5 + y * 19) & 0xFF,
                        (x * 11 + y * 7) & 0xFF));
                }
            }

            StyleDefinition style = StyleRegistry.Default;
            var values = StylePipeline.DefaultValues(style);
            var paints = new[]
            {
                PigmentLibrary.Selectable[0],
                PigmentLibrary.Selectable[11]
            };
            SourceFrame frame = SourceFrame.Create(source);

            using Bitmap fromBitmap = StylePipeline.Render(source, paints, style, 2, values);
            using Bitmap fromFrame = StylePipeline.Render(frame, paints, style, 2, values);

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Assert.Equal(fromBitmap.GetPixel(x, y).ToArgb(), fromFrame.GetPixel(x, y).ToArgb());
                }
            }
        }
    }
}
