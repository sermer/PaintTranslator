using System;
using System.Windows.Forms;

namespace PaintTranslator.BlendTests
{
    /// <summary>
    /// Entry point for the blend test harness.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Starts the application and shows the gradient strips window.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BlendStripsForm());
        }
    }
}
