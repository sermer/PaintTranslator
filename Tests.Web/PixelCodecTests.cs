using PaintTranslator.Imaging;
using PaintTranslator.Web.Session;

namespace PaintTranslator.Web.Tests;

public class PixelCodecTests
{
    [Fact]
    public void PacksRgbaBytesIntoArgbInts()
    {
        byte[] rgba = { 0x11, 0x22, 0x33, 0xFF,   0x00, 0x00, 0x00, 0x00,
                        0xFF, 0x00, 0x00, 0x80,   0x00, 0xFF, 0x00, 0x01 };
        PixelImage image = PixelCodec.ToPixelImage(rgba, 2, 2);
        Assert.Equal(unchecked((int)0xFF112233), image[0, 0]);
        Assert.Equal(0x00000000, image[1, 0]);
        Assert.Equal(unchecked((int)0x80FF0000), image[0, 1]);
        Assert.Equal(0x0100FF00, image[1, 1]);
    }

    [Fact]
    public void RoundTripIsLosslessIncludingTransparentColour()
    {
        byte[] rgba = { 10, 20, 30, 0,  200, 100, 50, 255,  1, 2, 3, 128,  255, 255, 255, 7 };
        PixelImage image = PixelCodec.ToPixelImage(rgba, 4, 1);
        Assert.Equal(rgba, PixelCodec.ToRgba(image));
    }

    [Fact]
    public void RejectsBufferOfWrongLength()
    {
        Assert.Throws<ArgumentException>(() => PixelCodec.ToPixelImage(new byte[7], 2, 1));
    }
}
