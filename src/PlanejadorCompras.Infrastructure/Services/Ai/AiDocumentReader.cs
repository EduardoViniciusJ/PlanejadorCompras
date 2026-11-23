using System.Text;
using ClosedXML.Excel;
using PdfSharp.Pdf.IO;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Services.Ai;

/// <summary>Reads uploaded documents in memory; nothing is written to disk.</summary>
public sealed class AiDocumentReader : IAiDocumentReader
{
    private const int MaxWorksheets = 3;
    private const int MaxRowsPerWorksheet = 1000;
    private const int MaxColumns = 40;

    public bool TryReadSpreadsheetText(byte[] content, int maxCharacters, out string text)
    {
        text = string.Empty;

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var workbook = new XLWorkbook(stream);
            var builder = new StringBuilder();

            foreach (var worksheet in workbook.Worksheets.Take(MaxWorksheets))
            {
                var range = worksheet.RangeUsed();
                if (range is null)
                {
                    continue;
                }

                foreach (var row in range.Rows().Take(MaxRowsPerWorksheet))
                {
                    var cells = row.Cells()
                        .Take(MaxColumns)
                        .Select(cell => cell.GetFormattedString().Replace('\t', ' ').Replace('\n', ' ').Trim())
                        .ToArray();

                    if (cells.All(string.IsNullOrEmpty))
                    {
                        continue;
                    }

                    builder.AppendLine(string.Join('\t', cells).TrimEnd('\t'));
                    if (builder.Length > maxCharacters)
                    {
                        // Enough to let the caller report that the limit was passed.
                        text = builder.ToString();
                        return true;
                    }
                }
            }

            text = builder.ToString();
            return true;
        }
        catch (Exception)
        {
            // Not a workbook (any other zip), corrupted or protected: the caller reports it.
            return false;
        }
    }

    public int? TryCountPdfPages(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            return document.PageCount;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
