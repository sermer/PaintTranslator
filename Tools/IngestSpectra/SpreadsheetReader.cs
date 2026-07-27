using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace IngestSpectra
{
    /// <summary>
    /// Reads the first worksheet of an .xlsx as a grid of strings.
    /// <para>
    /// Deliberately minimal. This reads one known file with one known layout, once,
    /// offline; a spreadsheet library would be a dependency carried forever for a
    /// problem that exists for one task.
    /// </para>
    /// </summary>
    public static class SpreadsheetReader
    {
        /// <summary>The SpreadsheetML namespace.</summary>
        private static readonly XNamespace Ns =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        /// <summary>
        /// Reads the first worksheet out of an .xlsx nested inside a .zip.
        /// </summary>
        /// <param name="zipPath">The outer zip containing the workbook.</param>
        /// <returns>Rows of cell values, indexed by zero-based column, with empty
        /// strings for blank cells.</returns>
        /// <exception cref="InvalidDataException">Thrown when the archive does not hold
        /// the expected parts.</exception>
        public static IReadOnlyList<IReadOnlyList<string>> ReadSheet(string zipPath)
        {
            using ZipArchive outer = ZipFile.OpenRead(zipPath);
            ZipArchiveEntry workbookEntry = outer.Entries
                .FirstOrDefault(entry => entry.FullName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"No .xlsx inside {zipPath}.");

            using var workbookBytes = new MemoryStream();
            using (Stream workbookStream = workbookEntry.Open())
            {
                workbookStream.CopyTo(workbookBytes);
            }

            workbookBytes.Position = 0;
            using var workbook = new ZipArchive(workbookBytes, ZipArchiveMode.Read);

            string[] sharedStrings = ReadSharedStrings(workbook);
            XDocument sheet = ReadPart(workbook, "xl/worksheets/sheet1.xml")
                ?? throw new InvalidDataException("The workbook has no xl/worksheets/sheet1.xml.");

            var rows = new List<IReadOnlyList<string>>();
            foreach (XElement row in sheet.Descendants(Ns + "row"))
            {
                var cells = new List<string>();
                foreach (XElement cell in row.Elements(Ns + "c"))
                {
                    int column = ColumnIndex((string)cell.Attribute("r"));
                    while (cells.Count < column)
                    {
                        cells.Add(string.Empty);
                    }

                    cells.Add(CellValue(cell, sharedStrings));
                }

                rows.Add(cells);
            }

            return rows;
        }

        /// <summary>
        /// Reads the workbook's shared-string table, which is where every text cell's
        /// content actually lives.
        /// </summary>
        /// <param name="workbook">The opened workbook archive.</param>
        /// <returns>The shared strings, in index order.</returns>
        private static string[] ReadSharedStrings(ZipArchive workbook)
        {
            XDocument document = ReadPart(workbook, "xl/sharedStrings.xml");
            if (document == null)
            {
                return Array.Empty<string>();
            }

            return document.Descendants(Ns + "si")
                .Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => t.Value)))
                .ToArray();
        }

        /// <summary>
        /// Reads one XML part out of the workbook archive.
        /// </summary>
        /// <param name="workbook">The opened workbook archive.</param>
        /// <param name="partName">The part's full name inside the archive.</param>
        /// <returns>The parsed part, or null when it is absent.</returns>
        private static XDocument ReadPart(ZipArchive workbook, string partName)
        {
            ZipArchiveEntry entry = workbook.GetEntry(partName);
            if (entry == null)
            {
                return null;
            }

            using Stream stream = entry.Open();
            return XDocument.Load(stream);
        }

        /// <summary>
        /// Resolves a cell's value, following the shared-string table when the cell is
        /// text.
        /// </summary>
        /// <param name="cell">The cell element.</param>
        /// <param name="sharedStrings">The shared-string table.</param>
        /// <returns>The cell's value as text.</returns>
        private static string CellValue(XElement cell, string[] sharedStrings)
        {
            string value = cell.Element(Ns + "v")?.Value ?? string.Empty;
            string type = (string)cell.Attribute("t");

            if (type == "s" && int.TryParse(value, out int index)
                && index >= 0 && index < sharedStrings.Length)
            {
                return sharedStrings[index];
            }

            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(Ns + "t").Select(t => t.Value));
            }

            return value;
        }

        /// <summary>
        /// Converts a cell reference such as "AL3" to a zero-based column index.
        /// </summary>
        /// <param name="reference">The cell reference.</param>
        /// <returns>The zero-based column index, or zero when the reference is absent.</returns>
        private static int ColumnIndex(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return 0;
            }

            int column = 0;
            foreach (char character in reference)
            {
                if (character < 'A' || character > 'Z')
                {
                    break;
                }

                column = (column * 26) + (character - 'A' + 1);
            }

            return column - 1;
        }
    }
}
