using PaintTranslator.Imaging;
using PaintTranslator.Pigments;
using Xunit;

namespace PaintTranslator.Tests
{
    /// <summary>
    /// Tests the pre-mapping blur. Every failure mode here is silent — a kernel that
    /// does not sum to one, an edge rule that leaks darkness in from outside the image,
    /// or an average taken on the sRGB-encoded bytes instead of on the light they
    /// stand for — so each shows up as a picture that merely looks a bit off rather
    /// than as anything that throws.
    /// </summary>
    public class GaussianBlurTests
    {
        /// <summary>
        /// Confirms a zero radius leaves the buffer byte-for-byte as it was, which is
        /// what lets the slider's lowest position mean "no blur" rather than "a blur
        /// small enough not to notice".
        /// </summary>
        [Fact]
        public void ZeroRadiusLeavesEveryPixelUntouched()
        {
            int[] pixels = BuildGradient(16, 16);
            var original = (int[])pixels.Clone();

            GaussianBlur.Apply(pixels, 16, 16, 16, 0);

            Assert.Equal(original, pixels);
        }

        /// <summary>
        /// Confirms a region of uniform colour comes back unchanged, right up to the
        /// border. This is the test that catches an unnormalised kernel, which would
        /// shift the whole image's brightness, and an edge rule that treats the outside
        /// of the image as black, which would leave a dark frame around every result.
        /// </summary>
        [Fact]
        public void UniformColourSurvivesTheBlurIncludingAtTheBorder()
        {
            const int size = 24;
            int[] pixels = Fill(size, size, unchecked((int)0xFF4080C0));

            GaussianBlur.Apply(pixels, size, size, size, 6);

            foreach (int pixel in pixels)
            {
                Assert.Equal(unchecked((int)0xFF4080C0), pixel);
            }
        }

        /// <summary>
        /// Confirms the averaging happens in linear light: blurring a black half against
        /// a white half must conserve the total light in the image. Averaging the sRGB
        /// bytes instead would conserve their mean rather than the light's, and since
        /// the encoding is a power curve those are not the same quantity — the blurred
        /// edge would come back holding visibly less light than it started with.
        /// </summary>
        [Fact]
        public void BlurConservesTotalLightAcrossAHardEdge()
        {
            // The radius is deliberately a large fraction of the width. Only the pixels
            // the kernel actually reaches can differ between the two ways of averaging,
            // so a narrow ramp across a wide image dilutes the very thing being measured
            // — at radius 8 here the encoded-space result still comes to 0.474, which
            // this would only barely catch.
            const int width = 48;
            int[] pixels = Fill(width, 1, unchecked((int)0xFF000000));
            for (int x = width / 2; x < width; x++)
            {
                pixels[x] = unchecked((int)0xFFFFFFFF);
            }

            GaussianBlur.Apply(pixels, width, width, 1, 20);

            // Half the pixels started at linear 1 and half at linear 0, so a blur that
            // conserves light leaves the mean at 0.5 however far it spreads the edge.
            // Blurring the encoded bytes instead lands at 0.412, well outside this.
            double total = 0.0;
            foreach (int pixel in pixels)
            {
                total += ColorSpace.SrgbToLinear(((pixel >> 16) & 0xFF) / 255.0);
            }

            Assert.InRange(total / width, 0.49, 0.51);
        }

        /// <summary>
        /// Builds a buffer of a single repeated colour.
        /// </summary>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="argb">The colour every pixel takes.</param>
        /// <returns>The filled ARGB buffer, one int per pixel with no row padding.</returns>
        private static int[] Fill(int width, int height, int argb)
        {
            var pixels = new int[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = argb;
            }

            return pixels;
        }

        /// <summary>
        /// Builds a buffer whose channels all vary across the image, so a blur that
        /// wrongly touched one of them could not hide behind the others being flat.
        /// </summary>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <returns>The filled ARGB buffer, one int per pixel with no row padding.</returns>
        private static int[] BuildGradient(int width, int height)
        {
            var pixels = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = (x * 255) / (width - 1);
                    int g = (y * 255) / (height - 1);
                    int b = ((x + y) * 255) / (width + height - 2);
                    pixels[(y * width) + x] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
                }
            }

            return pixels;
        }
    }
}
