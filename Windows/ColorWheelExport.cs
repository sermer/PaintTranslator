using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PaintTranslator.Imaging;

namespace PaintTranslator.Windows
{
    /// <summary>
    /// Backs the <c>--generate-colorwheel</c> command line flag. Lives in the app
    /// rather than the kernel because writing a PNG needs a codec, and the only one
    /// the desktop build carries is GDI's.
    /// </summary>
    public static class ColorWheelExport
    {
        public static void SaveToFile(string path, int diameter)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using Bitmap wheel = GdiImageAdapter.ToBitmap(ColorWheelGenerator.Create(diameter));
            wheel.Save(path, ImageFormat.Png);
        }
    }
}
