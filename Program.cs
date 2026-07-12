using System;
using System.Windows.Forms;
using PaintTranslator.Imaging;

namespace PaintTranslator
{
    /// <summary>
    /// Application entry point. Launches the main window, or generates test assets
    /// when invoked with command-line arguments.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Starts the application. Supports "--generate-colorwheel &lt;path&gt;" to write a
        /// color wheel test image to disk and exit without showing the UI.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the executable.</param>
        [STAThread]
        private static void Main(string[] args)
        {
            // Headless mode: emit the color wheel test asset and exit, so test images
            // can be produced from scripts without opening a window.
            if (args.Length >= 1 && args[0] == "--generate-colorwheel")
            {
                string outputPath = args.Length >= 2 ? args[1] : "color-wheel.png";
                ColorWheelGenerator.SaveToFile(outputPath, 512);
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
