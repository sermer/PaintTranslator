using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PaintTranslator.Imaging;

namespace PaintTranslator.Input
{
    /// <summary>
    /// An image obtained from the clipboard or a drag-and-drop operation, together with a
    /// name suitable for the window title.
    /// </summary>
    public sealed class LoadedImage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedImage"/> class.
        /// </summary>
        /// <param name="image">The decoded image.</param>
        /// <param name="name">A short name describing where the image came from.</param>
        public LoadedImage(Bitmap image, string name)
        {
            Image = image;
            Name = name;
        }

        /// <summary>
        /// Gets the decoded image.
        /// </summary>
        public Bitmap Image { get; }

        /// <summary>
        /// Gets a short name describing where the image came from.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Extracts an image from a clipboard or drag-and-drop payload. Both gestures deliver
    /// an <see cref="IDataObject"/>, so both are served by the same ordered search through
    /// the formats a payload might carry.
    /// </summary>
    public static class ImageDataObjectReader
    {
        /// <summary>
        /// The clipboard format browsers and image editors use to offer PNG data. Unlike
        /// the device-independent bitmap format, it preserves transparency.
        /// </summary>
        private const string PngFormat = "PNG";

        /// <summary>
        /// The clipboard format Firefox uses to offer the source URL of a dragged image,
        /// as the URL on one line followed by the link text on the next.
        /// </summary>
        private const string MozillaUrlFormat = "text/x-moz-url";

        /// <summary>
        /// The largest image the reader will download from a URL. Web images run to a few
        /// megabytes; anything far past that is not what the user meant to drag.
        /// </summary>
        private const int MaxDownloadBytes = 64 * 1024 * 1024;

        /// <summary>
        /// Shared client for URL downloads. A single long-lived instance avoids exhausting
        /// sockets, which is what creating one per request would eventually do.
        /// </summary>
        private static readonly HttpClient Downloader = CreateDownloader();

        /// <summary>
        /// Finds the image source URL inside an HTML fragment.
        /// </summary>
        private static readonly Regex ImageSourcePattern = new Regex(
            "<img[^>]+?src\\s*=\\s*[\"']([^\"']+)[\"']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Determines whether a payload looks like it holds an image, cheaply enough to run
        /// on every drag-over event.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>True when the payload carries a format the reader can attempt.</returns>
        public static bool ContainsImage(IDataObject data)
        {
            if (data == null)
            {
                return false;
            }

            return ContainsImageFile(data)
                || VirtualFileDataReader.IsPresent(data)
                || data.GetDataPresent(PngFormat)
                || data.GetDataPresent(DataFormats.Bitmap)
                || TryGetImageUrl(data) != null;
        }

        /// <summary>
        /// Reads an image out of a payload, trying each supported source in turn.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The decoded image, or null when the payload holds nothing readable.</returns>
        /// <remarks>
        /// The order matters. A real file on disk is the most faithful source, so it wins.
        /// Next comes the copy of the image the browser embedded in the drag, which works
        /// even for images behind blob: and data: URLs that could never be downloaded, and
        /// which yields the site's original bytes in whatever format they were encoded.
        /// PNG data comes before the device-independent bitmap because it keeps alpha.
        /// Downloading a URL is last: it is the only option that needs the network and the
        /// only one that can fetch something other than what the user seemed to drag.
        /// </remarks>
        public static async Task<LoadedImage> ReadAsync(IDataObject data)
        {
            if (data == null)
            {
                return null;
            }

            return TryReadDroppedFile(data)
                ?? TryReadVirtualFile(data)
                ?? TryReadPngData(data)
                ?? TryReadBitmapData(data)
                ?? await TryDownloadAsync(data).ConfigureAwait(true);
        }

        /// <summary>
        /// Reads an image from a file dropped out of Explorer or pasted as a file copy.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The decoded image, or null when the payload names no image file.</returns>
        private static LoadedImage TryReadDroppedFile(IDataObject data)
        {
            string path = GetFirstImagePath(data);
            if (path == null)
            {
                return null;
            }

            return new LoadedImage(ImageDecoder.DecodeFile(path), Path.GetFileName(path));
        }

        /// <summary>
        /// Reads an image the source application supplied as bytes rather than as a path,
        /// which is how browsers hand over an image dragged off a page.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The decoded image, or null when the payload carries no virtual file.</returns>
        private static LoadedImage TryReadVirtualFile(IDataObject data)
        {
            VirtualFile file = VirtualFileDataReader.TryReadFirst(data);
            if (file == null)
            {
                return null;
            }

            // Browsers occasionally describe a file they then decline to fill in, and some
            // hand over an HTML error page under an image name; either way the bytes fail
            // to decode and the caller should fall through to the remaining formats.
            try
            {
                Bitmap image = ImageDecoder.DecodeBytes(file.Content);
                return new LoadedImage(image, string.IsNullOrWhiteSpace(file.Name) ? "Dropped image" : file.Name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Reads an image from the payload's PNG data.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The decoded image, or null when the payload offers no PNG data.</returns>
        private static LoadedImage TryReadPngData(IDataObject data)
        {
            if (!data.GetDataPresent(PngFormat) || !(data.GetData(PngFormat) is MemoryStream stream))
            {
                return null;
            }

            using (stream)
            {
                try
                {
                    return new LoadedImage(ImageDecoder.DecodeBytes(stream.ToArray()), "Pasted image");
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Reads an image from the payload's device-independent bitmap, the format older
        /// applications and the Windows screenshot tools put on the clipboard.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The decoded image, or null when the payload offers no bitmap.</returns>
        private static LoadedImage TryReadBitmapData(IDataObject data)
        {
            if (!data.GetDataPresent(DataFormats.Bitmap) || !(data.GetData(DataFormats.Bitmap) is Image image))
            {
                return null;
            }

            using (image)
            {
                return new LoadedImage(new Bitmap(image), "Pasted image");
            }
        }

        /// <summary>
        /// Downloads the image the payload points at, for sources that offer only a link.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The decoded image, or null when no URL is present or the download or
        /// decode fails.</returns>
        private static async Task<LoadedImage> TryDownloadAsync(IDataObject data)
        {
            Uri url = TryGetImageUrl(data);
            if (url == null)
            {
                return null;
            }

            try
            {
                using (HttpResponseMessage response = await Downloader
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(true))
                {
                    response.EnsureSuccessStatusCode();

                    // Trust the declared length only to reject an oversized download early;
                    // the copy below enforces the limit whether or not one was declared.
                    if (response.Content.Headers.ContentLength > MaxDownloadBytes)
                    {
                        return null;
                    }

                    byte[] bytes = await ReadCappedAsync(response).ConfigureAwait(true);
                    if (bytes == null)
                    {
                        return null;
                    }

                    string name = Path.GetFileName(url.LocalPath);
                    return new LoadedImage(
                        ImageDecoder.DecodeBytes(bytes),
                        string.IsNullOrWhiteSpace(name) ? "Downloaded image" : name);
                }
            }
            catch (Exception)
            {
                // An unreachable host, a redirect to a login page, or a link that turns out
                // not to be an image at all are all ordinary outcomes for a dragged URL.
                return null;
            }
        }

        /// <summary>
        /// Copies a response body into memory, refusing anything past the size limit.
        /// </summary>
        /// <param name="response">The response to read.</param>
        /// <returns>The response bytes, or null when the body exceeds the limit.</returns>
        private static async Task<byte[]> ReadCappedAsync(HttpResponseMessage response)
        {
            using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(true))
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(true)) > 0)
                {
                    if (buffer.Length + read > MaxDownloadBytes)
                    {
                        return null;
                    }

                    buffer.Write(chunk, 0, read);
                }

                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Determines whether the payload names at least one file the decoder supports.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>True when a supported image file is listed.</returns>
        private static bool ContainsImageFile(IDataObject data)
        {
            return GetFirstImagePath(data) != null;
        }

        /// <summary>
        /// Finds the first supported image file listed in the payload.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The file path, or null when none of the listed files is a supported
        /// image.</returns>
        private static string GetFirstImagePath(IDataObject data)
        {
            if (!data.GetDataPresent(DataFormats.FileDrop) || !(data.GetData(DataFormats.FileDrop) is string[] paths))
            {
                return null;
            }

            foreach (string path in paths)
            {
                string extension = Path.GetExtension(path);
                foreach (string supported in ImageDecoder.SupportedExtensions)
                {
                    if (string.Equals(extension, supported, StringComparison.OrdinalIgnoreCase))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds an image URL in the payload's text or HTML.
        /// </summary>
        /// <param name="data">The clipboard or drag-and-drop payload.</param>
        /// <returns>The URL, or null when none of the text formats holds a usable one.</returns>
        private static Uri TryGetImageUrl(IDataObject data)
        {
            // Firefox names the source URL outright, so it needs no parsing.
            if (data.GetDataPresent(MozillaUrlFormat) && data.GetData(MozillaUrlFormat) is MemoryStream mozilla)
            {
                using (mozilla)
                {
                    string text = Encoding.Unicode.GetString(mozilla.ToArray()).TrimEnd('\0');
                    Uri parsed = ParseWebUrl(text.Split('\n')[0]);
                    if (parsed != null)
                    {
                        return parsed;
                    }
                }
            }

            // Chromium browsers put the plain URL on the text format when dragging an image.
            if (data.GetDataPresent(DataFormats.UnicodeText) && data.GetData(DataFormats.UnicodeText) is string unicodeText)
            {
                Uri parsed = ParseWebUrl(unicodeText);
                if (parsed != null)
                {
                    return parsed;
                }
            }

            // Dragging a whole region of a page yields an HTML fragment instead, so pull
            // the source out of the first image element it contains.
            if (data.GetDataPresent(DataFormats.Html) && data.GetData(DataFormats.Html) is string html)
            {
                Match match = ImageSourcePattern.Match(html);
                if (match.Success)
                {
                    return ParseWebUrl(match.Groups[1].Value);
                }
            }

            return null;
        }

        /// <summary>
        /// Parses text into a downloadable URL.
        /// </summary>
        /// <param name="text">The candidate text.</param>
        /// <returns>The URL, or null when the text is not an absolute HTTP or HTTPS
        /// address.</returns>
        private static Uri ParseWebUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri url))
            {
                return null;
            }

            // Only these two schemes are fetched. A file: or data: URL reaching the
            // downloader would turn a dragged link into a read of arbitrary local content.
            return url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps ? url : null;
        }

        /// <summary>
        /// Builds the client used for URL downloads.
        /// </summary>
        /// <returns>A client with a timeout short enough that a dead link does not leave
        /// the window waiting.</returns>
        private static HttpClient CreateDownloader()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            // Some image hosts reject requests that arrive without a user agent.
            client.DefaultRequestHeaders.Add("User-Agent", "PaintTranslator");
            return client;
        }
    }
}
