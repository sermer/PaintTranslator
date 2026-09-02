using System;
using System.Drawing;
using System.IO;
using ImageMagick;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// Turns image files and raw image bytes into bitmaps the application can display,
    /// whatever format they arrived in. GDI+ handles the formats it knows natively; the
    /// rest (WEBP, AVIF, HEIC/HEIF, PSD) go through Magick.NET.
    /// </summary>
    public static class ImageDecoder
    {
        /// <summary>
        /// The file extensions the decoder accepts, used to build file dialog filters and
        /// to recognize dropped files.
        /// </summary>
        public static readonly string[] SupportedExtensions =
        {
            ".png", ".jpg", ".jpeg", ".jfif", ".bmp", ".gif", ".tif", ".tiff",
            ".webp", ".avif", ".heic", ".heif", ".psd",
        };

        /// <summary>
        /// Decodes an image file into a bitmap.
        /// </summary>
        /// <param name="path">The full path of the image file.</param>
        /// <returns>A bitmap holding no handle on the source file.</returns>
        public static Bitmap DecodeFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("An image path is required.", nameof(path));
            }

            // Sniff from the content rather than the extension: files renamed to .png,
            // and camera files handed over with no extension at all, are both common.
            ImageFileFormat format = DetectFileFormat(path);

            if (ImageFormatSniffer.IsNativelyDecodable(format))
            {
                // Copy out of the loaded image so the file handle is released
                // immediately instead of staying locked for as long as it is displayed.
                using (var source = Image.FromFile(path))
                {
                    return new Bitmap(source);
                }
            }

            using (var image = new MagickImage(path))
            {
                return ToBitmap(image);
            }
        }

        /// <summary>
        /// Decodes raw image bytes into a bitmap. This is the path taken by clipboard
        /// pastes, dropped web images and downloaded images, none of which have a file to
        /// read from.
        /// </summary>
        /// <param name="data">The complete bytes of an image.</param>
        /// <returns>A bitmap holding no reference to the supplied array.</returns>
        public static Bitmap DecodeBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Image data is required.", nameof(data));
            }

            if (ImageFormatSniffer.IsNativelyDecodable(ImageFormatSniffer.Detect(data)))
            {
                // A bitmap built straight from a stream keeps using that stream for the
                // rest of its life, so decode into a temporary and copy the pixels out.
                using (var stream = new MemoryStream(data, writable: false))
                using (var decoded = new Bitmap(stream))
                {
                    return new Bitmap(decoded);
                }
            }

            using (var image = new MagickImage(data))
            {
                return ToBitmap(image);
            }
        }

        /// <summary>
        /// Converts a decoded Magick image into a GDI+ bitmap.
        /// </summary>
        /// <param name="image">The decoded image.</param>
        /// <returns>An equivalent bitmap with its alpha channel intact.</returns>
        private static Bitmap ToBitmap(MagickImage image)
        {
            // Reading into a single image takes the first frame, which for a PSD is the
            // merged composite Photoshop stores ahead of the individual layers. That is
            // the picture the user sees and the only one meaningful to translate into
            // paints, so no compositing is needed here. Multi-frame WEBP and GIF resolve
            // the same way, to their first frame.

            // PNG32 hands the pixels over losslessly and in a fixed RGBA layout, so the
            // bitmap below needs no guesswork about channel order or bit depth. Nothing
            // here may drop the alpha channel: the hover tooltip reads a zero alpha as
            // "no paint here", so flattening transparency onto a background would invent
            // paint colors for regions that have none.
            using (var stream = new MemoryStream(image.ToByteArray(MagickFormat.Png32)))
            using (var decoded = new Bitmap(stream))
            {
                return new Bitmap(decoded);
            }
        }

        /// <summary>
        /// Reads just enough of a file to identify its format.
        /// </summary>
        /// <param name="path">The full path of the image file.</param>
        /// <returns>The detected format, or <see cref="ImageFileFormat.Unknown"/> when the
        /// file is shorter than any known signature.</returns>
        private static ImageFileFormat DetectFileFormat(string path)
        {
            var signature = new byte[ImageFormatSniffer.MaxSignatureLength];

            using (var stream = File.OpenRead(path))
            {
                int read = 0;
                while (read < signature.Length)
                {
                    int chunk = stream.Read(signature, read, signature.Length - read);
                    if (chunk == 0)
                    {
                        break;
                    }

                    read += chunk;
                }

                // Detect inspects only the leading bytes, so a short read still identifies
                // any format whose signature fits in what was actually available.
                if (read < signature.Length)
                {
                    Array.Resize(ref signature, read);
                }
            }

            return ImageFormatSniffer.Detect(signature);
        }
    }
}
