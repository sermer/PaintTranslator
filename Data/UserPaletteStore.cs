using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PaintTranslator.Data
{
    /// <summary>
    /// Persists the user's chosen paint palette between application runs as a
    /// JSON array of paint names stored in the per-user application data folder.
    /// </summary>
    public static class UserPaletteStore
    {
        /// <summary>
        /// Gets the full path of the palette file. Lives under the roaming
        /// application data folder so the palette follows the Windows user
        /// profile rather than the install location.
        /// </summary>
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PaintTranslator",
            "palette.json");

        /// <summary>
        /// Loads the saved palette from disk.
        /// </summary>
        /// <returns>The set of saved paint names, or null when no palette has been
        /// saved yet or the file is unreadable, signaling callers to fall back to
        /// the full palette.</returns>
        public static HashSet<string> Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                string[] names = JsonSerializer.Deserialize<string[]>(File.ReadAllText(FilePath));

                // An empty saved list would leave the app with no paints at all;
                // treat it the same as no saved palette.
                return names == null || names.Length == 0
                    ? null
                    : new HashSet<string>(names, StringComparer.Ordinal);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
                // A corrupt or inaccessible file should never block startup;
                // the full palette is a safe default.
                return null;
            }
        }

        /// <summary>
        /// Saves the given paint names as the user's palette, creating the
        /// storage folder on first use.
        /// </summary>
        /// <param name="paintNames">The names of the paints in the user's palette.</param>
        public static void Save(IEnumerable<string> paintNames)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonSerializer.Serialize(paintNames));
        }
    }
}
