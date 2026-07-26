using System;
using System.Drawing;
using System.IO;
using ImageMagick;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests that every supported format decodes to the pixels it encoded. These are the
    /// checks that catch a format quietly breaking: a decoder that returns the wrong size,
    /// loses transparency, or picks the wrong frame produces a bitmap that displays without
    /// error and is simply wrong.
    /// </summary>
    public class ImageDecoderTests : IDisposable
    {
        /// <summary>
        /// A directory holding the sample files written during one test, removed when the
        /// test finishes.
        /// </summary>
        private readonly string workingDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDecoderTests"/> class.
        /// </summary>
        public ImageDecoderTests()
        {
            workingDirectory = Path.Combine(Path.GetTempPath(), "PaintTranslatorTests", Path.GetRandomFileName());
            Directory.CreateDirectory(workingDirectory);
        }

        /// <summary>
        /// Removes the sample files written during the test.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp directory is not worth failing an otherwise green test.
            }
        }

        /// <summary>
        /// Confirms each format decodes from raw bytes to the size and color it was encoded
        /// with. This is the check that fails if a format is routed to the wrong decoder or
        /// if the native codecs stop shipping with the package.
        /// </summary>
        /// <param name="format">The format to encode the sample as.</param>
        [Theory]
        [InlineData(MagickFormat.Png)]
        [InlineData(MagickFormat.Jpeg)]
        [InlineData(MagickFormat.Bmp)]
        [InlineData(MagickFormat.Tiff)]
        [InlineData(MagickFormat.WebP)]
        [InlineData(MagickFormat.Avif)]
        [InlineData(MagickFormat.Psd)]
        public void DecodesBytesToTheEncodedImage(MagickFormat format)
        {
            using (Bitmap decoded = ImageDecoder.DecodeBytes(TestImages.Encode(format)))
            {
                Assert.Equal(TestImages.Width, decoded.Width);
                Assert.Equal(TestImages.Height, decoded.Height);
                AssertFilledWithSampleColor(decoded);
            }
        }

        /// <summary>
        /// Confirms the same formats decode when read from a file, which takes a different
        /// path from raw bytes because the format is sniffed from a partial read.
        /// </summary>
        /// <param name="format">The format to encode the sample as.</param>
        /// <param name="extension">The file extension to write it under.</param>
        [Theory]
        [InlineData(MagickFormat.Png, ".png")]
        [InlineData(MagickFormat.Jpeg, ".jpg")]
        [InlineData(MagickFormat.Tiff, ".tiff")]
        [InlineData(MagickFormat.WebP, ".webp")]
        [InlineData(MagickFormat.Avif, ".avif")]
        [InlineData(MagickFormat.Psd, ".psd")]
        public void DecodesFilesToTheEncodedImage(MagickFormat format, string extension)
        {
            string path = TestImages.WriteFile(workingDirectory, format, extension);

            using (Bitmap decoded = ImageDecoder.DecodeFile(path))
            {
                Assert.Equal(TestImages.Width, decoded.Width);
                Assert.Equal(TestImages.Height, decoded.Height);
                AssertFilledWithSampleColor(decoded);
            }
        }

        /// <summary>
        /// Confirms a multi-frame file decodes to its first frame at the full canvas size,
        /// rather than to a later frame or to all frames composited together.
        /// </summary>
        /// <remarks>
        /// This is the decision that makes a layered PSD load the way Photoshop displays
        /// it, because a real PSD stores its merged composite as the first frame ahead of
        /// the individual layers. The fixture here cannot stand in for a Photoshop file —
        /// ImageMagick's own PSD writer stores the first layer as frame 0 rather than
        /// building a merged composite — so what this pins is the frame selection itself.
        /// Switching to a collection and compositing it, which is the tempting mistake,
        /// would double-composite a real PSD and fails here.
        /// </remarks>
        [Fact]
        public void DecodesMultiFrameFileToItsFirstFrame()
        {
            using (Bitmap decoded = ImageDecoder.DecodeBytes(TestImages.EncodeMultiFramePsd()))
            {
                Assert.Equal(TestImages.Width, decoded.Width);
                Assert.Equal(TestImages.Height, decoded.Height);

                // The second frame sits over the canvas centre, so a decoder that
                // composited the frames would leave red rather than the fill color here.
                AssertFilledWithSampleColor(decoded);
            }
        }

        /// <summary>
        /// Confirms the checked-in HEIC sample decodes. HEIC reaches the decoder through
        /// the same branch as AVIF but a different native codec, so it can break on its own.
        /// </summary>
        [Fact]
        public void DecodesHeic()
        {
            using (Bitmap decoded = ImageDecoder.DecodeFile(TestImages.HeicSamplePath))
            {
                Assert.Equal(1280, decoded.Width);
                Assert.Equal(720, decoded.Height);
            }
        }

        /// <summary>
        /// Confirms a format is identified by its content rather than its extension, since
        /// images saved under the wrong extension are common and clipboard payloads have no
        /// extension at all.
        /// </summary>
        [Fact]
        public void DecodesFileWhoseExtensionContradictsItsContent()
        {
            // A WebP named .png would be handed to GDI+ by an extension-driven decoder,
            // which cannot read it.
            string path = TestImages.WriteFile(workingDirectory, MagickFormat.WebP, ".png");

            using (Bitmap decoded = ImageDecoder.DecodeFile(path))
            {
                Assert.Equal(TestImages.Width, decoded.Width);
                AssertFilledWithSampleColor(decoded);
            }
        }

        /// <summary>
        /// Confirms transparency survives decoding. The hover tooltip reads a zero alpha as
        /// "no paint here", so a decoder that flattened alpha onto a background would
        /// invent paint colors for regions that have none, with nothing raising an error.
        /// </summary>
        /// <param name="format">A format that carries an alpha channel.</param>
        [Theory]
        [InlineData(MagickFormat.Png)]
        [InlineData(MagickFormat.WebP)]
        [InlineData(MagickFormat.Avif)]
        [InlineData(MagickFormat.Psd)]
        public void PreservesTransparency(MagickFormat format)
        {
            using (Bitmap decoded = ImageDecoder.DecodeBytes(TestImages.EncodeHalfTransparent(format)))
            {
                // Both halves are asserted together. Checking only the clear half would
                // pass against an image that had lost its color, and checking only the
                // opaque half would pass against one that had lost its transparency.
                Assert.Equal(0, decoded.GetPixel(TestImages.Width / 4, TestImages.Height / 2).A);

                Color opaque = decoded.GetPixel(TestImages.Width * 3 / 4, TestImages.Height / 2);
                Assert.Equal(255, opaque.A);
                AssertIsSampleColor(opaque);
            }
        }

        /// <summary>
        /// Confirms a decoded bitmap holds no handle on the file it came from. A bitmap
        /// left attached to its source locks the file for as long as it is displayed, which
        /// blocks the user from moving or deleting the image they just opened.
        /// </summary>
        [Fact]
        public void ReleasesTheSourceFile()
        {
            string path = TestImages.WriteFile(workingDirectory, MagickFormat.Png, ".png");

            using (Bitmap decoded = ImageDecoder.DecodeFile(path))
            {
                File.Delete(path);

                // Reading a pixel after the delete proves the bitmap owns its own copy
                // rather than paging from the file it no longer has.
                Assert.Equal(TestImages.Width, decoded.Width);
                AssertFilledWithSampleColor(decoded);
            }
        }

        /// <summary>
        /// Confirms data that is not an image at all fails loudly, so the calling form can
        /// report it instead of displaying something meaningless.
        /// </summary>
        [Fact]
        public void RejectsDataThatIsNotAnImage()
        {
            byte[] garbage = System.Text.Encoding.ASCII.GetBytes("<html>404 Not Found</html>");

            Assert.ThrowsAny<Exception>(() => ImageDecoder.DecodeBytes(garbage));
        }

        /// <summary>
        /// Asserts the center of a decoded bitmap holds the sample fill color.
        /// </summary>
        /// <param name="bitmap">The decoded bitmap to inspect.</param>
        private static void AssertFilledWithSampleColor(Bitmap bitmap)
        {
            AssertIsSampleColor(bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2));
        }

        /// <summary>
        /// Asserts a decoded pixel holds the sample fill color.
        /// </summary>
        /// <param name="pixel">The pixel to inspect.</param>
        private static void AssertIsSampleColor(Color pixel)
        {
            // JPEG and AVIF are lossy and shift a flat fill by a few levels, so compare
            // with a tolerance rather than for exact equality.
            const int Tolerance = 8;
            Assert.True(
                Math.Abs(pixel.R - 0x33) <= Tolerance
                && Math.Abs(pixel.G - 0x66) <= Tolerance
                && Math.Abs(pixel.B - 0xCC) <= Tolerance,
                $"Expected approximately #3366CC but decoded #{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}.");
        }
    }
}
