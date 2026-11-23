using System.Text;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.Ai;

/// <summary>Text and/or binary attachment ready to be sent to the model.</summary>
public sealed record AiPreparedInput(string? Text, AiAttachment? Attachment);

/// <summary>
/// Validates what the user sent before any quota is used: size limits and the real file
/// type, detected from the content and never from the name or declared content type.
/// Spreadsheets are converted to text here; nothing is written to disk.
/// </summary>
public sealed class AiInputPreparer(AiSettings settings, IAiDocumentReader documentReader)
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public long MaxFileBytes => Math.Clamp(settings.MaxFileBytes, 1, AiSettings.HardMaxFileBytes);

    public AiPreparedInput Prepare(string? text, AiUploadedFile? file)
    {
        var normalizedText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var hasFile = file is { Content.Length: > 0 };

        if (normalizedText is null && !hasFile)
        {
            throw new BadRequestException(
                "Cole o texto ou envie um arquivo para a IA ler.",
                "ai_empty_input");
        }

        AiAttachment? attachment = null;
        if (hasFile)
        {
            if (file!.Content.LongLength > MaxFileBytes)
            {
                throw new BadRequestException(
                    $"O arquivo passa do limite de {FormatMegabytes(MaxFileBytes)} MB.",
                    "ai_file_too_large");
            }

            var (fileText, fileAttachment) = ReadFile(file);
            attachment = fileAttachment;
            if (fileText is not null)
            {
                normalizedText = normalizedText is null
                    ? fileText
                    : normalizedText + "\n\n" + fileText;
            }
        }

        if (normalizedText is not null && normalizedText.Length > settings.MaxInputCharacters)
        {
            throw new BadRequestException(
                $"O conteúdo passa do limite de {settings.MaxInputCharacters} caracteres. "
                + "Envie só a parte que interessa ou divida em partes.",
                "ai_text_too_long");
        }

        return new AiPreparedInput(normalizedText, attachment);
    }

    private (string? Text, AiAttachment? Attachment) ReadFile(AiUploadedFile file)
    {
        var content = file.Content;

        if (StartsWith(content, PdfSignature))
        {
            var pages = documentReader.TryCountPdfPages(content);
            if (pages > settings.MaxPdfPages)
            {
                throw new BadRequestException(
                    $"O PDF tem {pages} páginas e o limite é {settings.MaxPdfPages}. "
                    + "Envie só as páginas com os preços.",
                    "ai_pdf_too_long");
            }

            return (null, new AiAttachment(AiAttachmentKind.Pdf, "application/pdf", content));
        }

        if (StartsWith(content, PngSignature))
        {
            return (null, new AiAttachment(AiAttachmentKind.Image, "image/png", content));
        }

        if (StartsWith(content, JpegSignature))
        {
            return (null, new AiAttachment(AiAttachmentKind.Image, "image/jpeg", content));
        }

        if (StartsWith(content, ZipSignature))
        {
            // Stops reading a little after the limit; the caller reports the overflow.
            if (!documentReader.TryReadSpreadsheetText(content, settings.MaxInputCharacters, out var sheetText)
                || string.IsNullOrWhiteSpace(sheetText))
            {
                throw new BadRequestException(
                    "Não foi possível ler esta planilha. Envie um arquivo .xlsx ou .csv, ou cole o texto.",
                    "ai_unreadable_file");
            }

            return (sheetText.Trim(), null);
        }

        if (HasTextExtension(file.FileName) && TryDecodeText(content, out var plainText))
        {
            return (plainText, null);
        }

        throw new BadRequestException(
            "Formato de arquivo não aceito. Envie PDF, foto (JPG ou PNG) ou planilha (XLSX ou CSV).",
            "ai_unsupported_file");
    }

    private static bool HasTextExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDecodeText(byte[] content, out string text)
    {
        text = string.Empty;
        if (Array.IndexOf(content, (byte)0) >= 0)
        {
            return false;
        }

        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(content);
        }
        catch (DecoderFallbackException)
        {
            // Spreadsheets exported in Brazil are often Windows-1252; Latin-1 keeps the accents.
            text = Encoding.Latin1.GetString(content);
        }

        text = text.TrimStart((char)0xFEFF).Trim();
        return text.Length > 0 && !text.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t'));
    }

    private static bool StartsWith(byte[] content, byte[] signature) =>
        content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);

    private static string FormatMegabytes(long bytes) =>
        (bytes / (1024d * 1024d)).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
}
