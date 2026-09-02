using System;

namespace PaintTranslator.Imaging
{
    /// <summary>
    /// The image formats the application recognizes when inspecting raw bytes.
    /// </summary>
    public enum ImageFileFormat
    {
        /// <summary>The bytes match no signature the sniffer knows.</summary>
        Unknown,

        /// <summary>Portable Network Graphics.</summary>
        Png,

        /// <summary>JPEG / JFIF.</summary>
        Jpeg,

        /// <summary>Graphics Interchange Format.</summary>
        Gif,

        /// <summary>Windows bitmap.</summary>
        Bmp,

        /// <summary>Tagged Image File Format, either byte order.</summary>
        Tiff,

        /// <summary>Google WebP, carried in a RIFF container.</summary>
        Webp,

        /// <summary>AV1 Image File Format.</summary>
        Avif,

        /// <summary>HEIC / HEIF, the HEVC-in-ISO-BMFF family.</summary>
        Heif,

        /// <summary>Adobe Photoshop document.</summary>
        Psd,
    }

    /// <summary>
    /// Identifies an image format from the leading bytes of its data. Images arriving by
    /// clipboard paste or drag-and-drop carry no filename, so the extension is unavailable
    /// and the format has to be read out of the content itself.
    /// </summary>
    public static class ImageFormatSniffer
    {
        /// <summary>
        /// The longest prefix any signature check inspects, which is the ISO base media
        /// brand sitting at offset 8.
        /// </summary>
        public const int MaxSignatureLength = 12;

        /// <summary>
        /// Determines which image format a block of bytes holds.
        /// </summary>
        /// <param name="data">The image data, or at least its first
        /// <see cref="MaxSignatureLength"/> bytes.</param>
        /// <returns>The detected format, or <see cref="ImageFileFormat.Unknown"/> when no
        /// signature matches or the data is too short to identify.</returns>
        public static ImageFileFormat Detect(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                return ImageFileFormat.Unknown;
            }

            if (StartsWith(data, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            {
                return ImageFileFormat.Png;
            }

            // Every JPEG variant opens with SOI followed by a marker byte; the third byte
            // distinguishes JFIF, Exif and the rest, none of which matter here.
            if (StartsWith(data, 0xFF, 0xD8, 0xFF))
            {
                return ImageFileFormat.Jpeg;
            }

            if (MatchesAscii(data, 0, "GIF87a") || MatchesAscii(data, 0, "GIF89a"))
            {
                return ImageFileFormat.Gif;
            }

            if (MatchesAscii(data, 0, "BM"))
            {
                return ImageFileFormat.Bmp;
            }

            // TIFF declares its byte order in the first two bytes, then the magic 42 in
            // that same order.
            if (StartsWith(data, 0x49, 0x49, 0x2A, 0x00) || StartsWith(data, 0x4D, 0x4D, 0x00, 0x2A))
            {
                return ImageFileFormat.Tiff;
            }

            if (MatchesAscii(data, 0, "8BPS"))
            {
                return ImageFileFormat.Psd;
            }

            // WebP is a RIFF container whose form type sits after the 4-byte chunk size.
            if (MatchesAscii(data, 0, "RIFF") && MatchesAscii(data, 8, "WEBP"))
            {
                return ImageFileFormat.Webp;
            }

            // AVIF and HEIC share the ISO base media layout: a length prefix, the "ftyp"
            // box type, then a brand that names the actual codec.
            if (MatchesAscii(data, 4, "ftyp"))
            {
                return DetectIsoBaseMediaBrand(data);
            }

            return ImageFileFormat.Unknown;
        }

        /// <summary>
        /// Determines whether a format is one GDI+ can decode on its own, leaving the rest
        /// to be handled by Magick.NET.
        /// </summary>
        /// <param name="format">The format to test.</param>
        /// <returns>True when GDI+ can read the format directly; otherwise false.</returns>
        public static bool IsNativelyDecodable(ImageFileFormat format)
        {
            switch (format)
            {
                case ImageFileFormat.Png:
                case ImageFileFormat.Jpeg:
                case ImageFileFormat.Gif:
                case ImageFileFormat.Bmp:
                case ImageFileFormat.Tiff:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Reads the brand of an ISO base media file to tell the AVIF and HEIF families
        /// apart, since both use the identical outer container.
        /// </summary>
        /// <param name="data">The image data, positioned at the start of the file.</param>
        /// <returns>The matching format, or <see cref="ImageFileFormat.Unknown"/> for a
        /// brand that is neither AVIF nor a still-image HEIF.</returns>
        private static ImageFileFormat DetectIsoBaseMediaBrand(byte[] data)
        {
            if (data.Length < 12)
            {
                return ImageFileFormat.Unknown;
            }

            string brand = System.Text.Encoding.ASCII.GetString(data, 8, 4);
            switch (brand)
            {
                case "avif":
                case "avis":
                    return ImageFileFormat.Avif;

                // The heic/heix/hevc/hevx brands are HEVC stills and sequences; mif1 and
                // msf1 are the generic still-image brands Apple also emits for .heic.
                case "heic":
                case "heix":
                case "hevc":
                case "hevx":
                case "heim":
                case "heis":
                case "hevm":
                case "hevs":
                case "mif1":
                case "msf1":
                    return ImageFileFormat.Heif;

                default:
                    return ImageFileFormat.Unknown;
            }
        }

        /// <summary>
        /// Tests whether the data opens with an exact sequence of bytes.
        /// </summary>
        /// <param name="data">The data to test.</param>
        /// <param name="signature">The bytes expected at the start of the data.</param>
        /// <returns>True when every signature byte matches; otherwise false.</returns>
        private static bool StartsWith(byte[] data, params byte[] signature)
        {
            if (data.Length < signature.Length)
            {
                return false;
            }

            for (int i = 0; i < signature.Length; i++)
            {
                if (data[i] != signature[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests whether the data holds an ASCII tag at a given offset. Container formats
        /// place their identifying tags at fixed offsets rather than at the very start.
        /// </summary>
        /// <param name="data">The data to test.</param>
        /// <param name="offset">The byte offset the tag should begin at.</param>
        /// <param name="tag">The expected ASCII text.</param>
        /// <returns>True when the tag is present at that offset; otherwise false.</returns>
        private static bool MatchesAscii(byte[] data, int offset, string tag)
        {
            if (data.Length < offset + tag.Length)
            {
                return false;
            }

            for (int i = 0; i < tag.Length; i++)
            {
                if (data[offset + i] != (byte)tag[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
