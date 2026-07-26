using System.IO;
using ImageMagick;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Builds the sample images the decoding tests run against. Encoding them on demand
    /// keeps the repository free of binary fixtures and guarantees the bytes come from a
    /// real encoder rather than a hand-assembled approximation.
    /// </summary>
    public static class TestImages
    {
        /// <summary>
        /// The width of every generated sample, chosen to be different from the height so
        /// a transposed result cannot pass unnoticed.
        /// </summary>
        public const int Width = 64;

        /// <summary>
        /// The height of every generated sample.
        /// </summary>
        public const int Height = 48;

        /// <summary>
        /// The fill color of every generated sample. Its three channels are distinct, so a
        /// decoder that swapped the channel order produces a visibly different color
        /// rather than an identical grey.
        /// </summary>
        public static readonly MagickColor Fill = new MagickColor("#3366CC");

        /// <summary>
        /// Gets the path of the checked-in HEIC sample. HEIC is the one format the tests
        /// cannot generate, because the Windows build of ImageMagick ships an HEVC decoder
        /// but no encoder.
        /// </summary>
        public static string HeicSamplePath => Path.Combine("Assets", "sample.heic");

        /// <summary>
        /// Encodes a solid-color sample image in a given format.
        /// </summary>
        /// <param name="format">The format to encode as.</param>
        /// <returns>The encoded image bytes.</returns>
        public static byte[] Encode(MagickFormat format)
        {
            using (var image = new MagickImage(Fill, Width, Height))
            {
                return image.ToByteArray(format);
            }
        }

        /// <summary>
        /// Encodes a sample image whose left half is fully transparent and whose right half
        /// is the opaque fill color.
        /// </summary>
        /// <param name="format">The format to encode as.</param>
        /// <returns>The encoded image bytes.</returns>
        /// <remarks>
        /// The opaque half is what makes this fixture useful. An image that is transparent
        /// everywhere is a degenerate case that encoders special-case, and it survives even
        /// a round trip through a format with no alpha channel at all — so a test built on
        /// one cannot detect transparency being lost.
        /// </remarks>
        public static byte[] EncodeHalfTransparent(MagickFormat format)
        {
            using (var image = new MagickImage(Fill, Width, Height))
            {
                image.Alpha(AlphaOption.Set);

                // Copy rather than blend, so the hole replaces the alpha channel of the
                // region instead of compositing a transparent image over it, which would
                // leave the original opaque pixels untouched.
                using (var hole = new MagickImage(MagickColors.Transparent, Width / 2, Height))
                {
                    image.Composite(hole, 0, 0, CompositeOperator.Copy);
                }

                return image.ToByteArray(format);
            }
        }

        /// <summary>
        /// Encodes a multi-frame PSD: a full-canvas frame followed by a smaller one placed
        /// partway across it.
        /// </summary>
        /// <returns>The encoded image bytes.</returns>
        public static byte[] EncodeMultiFramePsd()
        {
            using (var frames = new MagickImageCollection())
            {
                frames.Add(new MagickImage(Fill, Width, Height));

                var inset = new MagickImage(MagickColors.Red, Width / 4, Height / 4);
                inset.Page = new MagickGeometry(Width / 2, Height / 2, Width / 4, Height / 4);
                frames.Add(inset);

                return frames.ToByteArray(MagickFormat.Psd);
            }
        }

        /// <summary>
        /// Writes a sample image to a file in a given format.
        /// </summary>
        /// <param name="directory">The directory to write into.</param>
        /// <param name="format">The format to encode as.</param>
        /// <param name="extension">The file extension to use, including the leading dot.</param>
        /// <returns>The full path of the written file.</returns>
        public static string WriteFile(string directory, MagickFormat format, string extension)
        {
            string path = Path.Combine(directory, "sample" + extension);
            File.WriteAllBytes(path, Encode(format));
            return path;
        }
    }
}
