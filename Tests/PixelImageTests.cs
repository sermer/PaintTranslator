using System;
using System.Drawing;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    public class PixelImageTests
    {
        [Fact]
        public void FromPixelsRejectsABufferOfTheWrongLength()
        {
            Assert.Throws<ArgumentException>(() => PixelImage.FromPixels(3, 2, new int[5]));
        }

        [Fact]
        public void IndexerReadsRowMajor()
        {
            var pixels = new int[] { 1, 2, 3, 4, 5, 6 };
            PixelImage image = PixelImage.FromPixels(3, 2, pixels);

            Assert.Equal(4, image[0, 1]);
            Assert.Equal(3, image[2, 0]);
            Assert.Equal(new Size(3, 2), image.Size);
        }

        [Fact]
        public void PixelCopiesCannotMutateTheImage()
        {
            PixelImage image = PixelImage.FromPixels(3, 2, new int[6]);
            int[] changed = image.CopyPixels();
            changed[0] = 0x12345678;

            Assert.Equal(0, image.CopyPixels()[0]);
            Assert.Equal(0, image[0, 0]);
        }

        [Fact]
        public void AlphaAtMasksEverythingButTheAlphaByte()
        {
            PixelImage image = PixelImage.Filled(1, 1, unchecked((int)0x80FF00FF));
            Assert.Equal(unchecked((int)0x80000000), image.AlphaAt(0));
        }

        [Fact]
        public void FilledCoversEveryPixel()
        {
            PixelImage image = PixelImage.Filled(4, 3, 0x11223344);
            foreach (int pixel in image.Pixels)
            {
                Assert.Equal(0x11223344, pixel);
            }
        }
    }
}
