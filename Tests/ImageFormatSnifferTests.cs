using System.IO;
using ImageMagick;
using PaintTranslator.Imaging;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests that raw image bytes are identified as the right format. Detection decides
    /// whether an image is handed to GDI+ or to Magick.NET, so a wrong answer sends an
    /// image to a decoder that cannot read it.
    /// </summary>
    public class ImageFormatSnifferTests
    {
        /// <summary>
        /// Confirms each format is recognized from bytes a real encoder produced, rather
        /// than from a hand-written signature that could agree with a mistaken expectation.
        /// </summary>
        /// <param name="format">The format to encode a sample image as.</param>
        /// <param name="expected">The format the sniffer should report.</param>
        [Theory]
        [InlineData(MagickFormat.Png, ImageFileFormat.Png)]
        [InlineData(MagickFormat.Jpeg, ImageFileFormat.Jpeg)]
        [InlineData(MagickFormat.Gif, ImageFileFormat.Gif)]
        [InlineData(MagickFormat.Bmp, ImageFileFormat.Bmp)]
        [InlineData(MagickFormat.Tiff, ImageFileFormat.Tiff)]
        [InlineData(MagickFormat.WebP, ImageFileFormat.Webp)]
        [InlineData(MagickFormat.Avif, ImageFileFormat.Avif)]
        [InlineData(MagickFormat.Psd, ImageFileFormat.Psd)]
        public void DetectsFormatFromEncodedBytes(MagickFormat format, ImageFileFormat expected)
        {
            byte[] encoded = TestImages.Encode(format);

            Assert.Equal(expected, ImageFormatSniffer.Detect(encoded));
        }

        /// <summary>
        /// Confirms HEIC is told apart from AVIF. Both sit in an identical ISO base media
        /// container and differ only in a four-byte brand, so a detector that stopped at
        /// the container would silently route every HEIC down the AVIF path.
        /// </summary>
        [Fact]
        public void DistinguishesHeicFromAvif()
        {
            byte[] heic = File.ReadAllBytes(TestImages.HeicSamplePath);

            Assert.Equal(ImageFileFormat.Heif, ImageFormatSniffer.Detect(heic));
            Assert.Equal(ImageFileFormat.Avif, ImageFormatSniffer.Detect(TestImages.Encode(MagickFormat.Avif)));
        }

        /// <summary>
        /// Confirms the GDI+ and Magick.NET routing split matches what each library can
        /// actually read. Marking a format as natively decodable when it is not sends it
        /// to a decoder that throws.
        /// </summary>
        /// <param name="format">The format to classify.</param>
        /// <param name="expected">Whether GDI+ can read the format unaided.</param>
        [Theory]
        [InlineData(ImageFileFormat.Png, true)]
        [InlineData(ImageFileFormat.Jpeg, true)]
        [InlineData(ImageFileFormat.Gif, true)]
        [InlineData(ImageFileFormat.Bmp, true)]
        [InlineData(ImageFileFormat.Tiff, true)]
        [InlineData(ImageFileFormat.Webp, false)]
        [InlineData(ImageFileFormat.Avif, false)]
        [InlineData(ImageFileFormat.Heif, false)]
        [InlineData(ImageFileFormat.Psd, false)]
        [InlineData(ImageFileFormat.Unknown, false)]
        public void RoutesFormatsToTheDecoderThatCanReadThem(ImageFileFormat format, bool expected)
        {
            Assert.Equal(expected, ImageFormatSniffer.IsNativelyDecodable(format));
        }

        /// <summary>
        /// Confirms data too short to identify reports Unknown instead of reading past the
        /// end of the buffer, which is what a payload truncated in transit looks like.
        /// </summary>
        /// <param name="length">The number of leading bytes to keep.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(9)]
        public void ReportsUnknownForTruncatedData(int length)
        {
            // An ISO base media header is the worst case: its brand sits at offset 8, so a
            // detector that skipped the length check would read past a short buffer.
            byte[] truncated = new byte[length];
            byte[] full = TestImages.Encode(MagickFormat.Avif);
            System.Array.Copy(full, truncated, length);

            Assert.Equal(ImageFileFormat.Unknown, ImageFormatSniffer.Detect(truncated));
        }
    }
}
