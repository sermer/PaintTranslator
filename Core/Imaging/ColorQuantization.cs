namespace PaintTranslator.Imaging
{
    /// <summary>
    /// The one 6-bit-per-channel quantization scheme every per-colour pixel cache in
    /// this project uses, so <see cref="StylePipeline"/> and the cache
    /// <see cref="PalettePhotoConverter"/> keeps for its own test-support surfaces
    /// (<see cref="PalettePhotoConverter.MapThroughIndex"/>) can never disagree about
    /// which bin a colour falls in.
    /// <para>
    /// The shift expressions and the bin-centre reconstruction below used to be
    /// duplicated, character for character, in both of those classes — a change to the
    /// constant in one copy without the other would have left
    /// <see cref="PalettePhotoConverter.MapThroughIndex"/> quantizing differently from
    /// the pipeline that <c>PalettePhotoConverterGamutTests</c> checks it against, the
    /// same drift hazard the two nearest-candidate searches already had before Task 9
    /// collapsed them into one. Six bits per channel is fine enough that the rounding
    /// it introduces is invisible next to the snap onto the discrete mixture gamut,
    /// while capping the cache at 2^18 entries regardless of image size.
    /// </para>
    /// </summary>
    internal static class ColorQuantization
    {
        /// <summary>Bits kept per channel when quantizing a colour to a cache key.</summary>
        internal const int BitsPerChannel = 6;

        /// <summary>The number of distinct cache keys: (2^<see cref="BitsPerChannel"/>)^3.</summary>
        internal const int CacheSize = 1 << (3 * BitsPerChannel);

        /// <summary>
        /// Computes the cache key for a pixel's colour, ignoring alpha.
        /// </summary>
        /// <param name="argb">The pixel's 32-bit ARGB value.</param>
        /// <returns>The cache key in [0, <see cref="CacheSize"/>).</returns>
        internal static int Key(int argb)
        {
            return Key((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF);
        }

        /// <summary>
        /// Computes the cache key for separate colour channels.
        /// </summary>
        /// <param name="r">The red channel, 0 to 255.</param>
        /// <param name="g">The green channel, 0 to 255.</param>
        /// <param name="b">The blue channel, 0 to 255.</param>
        /// <returns>The cache key in [0, <see cref="CacheSize"/>).</returns>
        internal static int Key(int r, int g, int b)
        {
            return ((r >> 2) << (2 * BitsPerChannel)) | ((g >> 2) << BitsPerChannel) | (b >> 2);
        }

        /// <summary>
        /// Reconstructs the centre of the quantization bin a cache key represents, so
        /// the rounding error rebuilding a colour from its key is split evenly rather
        /// than biased toward the bin's low corner.
        /// </summary>
        /// <param name="key">A key produced by <see cref="Key(int)"/> or <see cref="Key(int, int, int)"/>.</param>
        /// <param name="r">The bin centre's red channel.</param>
        /// <param name="g">The bin centre's green channel.</param>
        /// <param name="b">The bin centre's blue channel.</param>
        internal static void KeyToRgb(int key, out int r, out int g, out int b)
        {
            r = (((key >> (2 * BitsPerChannel)) & 0x3F) << 2) + 2;
            g = (((key >> BitsPerChannel) & 0x3F) << 2) + 2;
            b = ((key & 0x3F) << 2) + 2;
        }
    }
}
