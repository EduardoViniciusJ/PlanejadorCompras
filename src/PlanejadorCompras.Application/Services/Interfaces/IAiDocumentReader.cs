namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IAiDocumentReader
{
    /// <summary>
    /// Reads an .xlsx workbook as tab-separated text. Stops shortly after
    /// maxCharacters so callers can detect overflow by comparing lengths.
    /// Returns false when the content is not a readable workbook.
    /// </summary>
    bool TryReadSpreadsheetText(byte[] content, int maxCharacters, out string text);

    /// <summary>Number of pages of a PDF, or null when it cannot be determined.</summary>
    int? TryCountPdfPages(byte[] content);
}
