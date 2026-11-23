using System.Globalization;
using System.Net;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

/// <summary>
/// Sends the quotation request to a supplier by e-mail, with the public link.
/// A failure never breaks the caller: the outcome is returned as a delivery code.
/// </summary>
public sealed class SupplierQuotationEmailSender(
    IEmailAvailability emailAvailability,
    Lazy<IEmailService> emailService)
{
    public const string Sent = "sent";
    public const string Failed = "failed";
    public const string NoAddress = "no_address";
    public const string Unavailable = "unavailable";

    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");

    public bool IsAvailable => emailAvailability.IsAvailable;

    public async Task<string> SendAsync(
        QuotationRequestEntity request,
        SupplierQuotation quotation,
        string linkUrl,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (!emailAvailability.IsAvailable)
        {
            return Unavailable;
        }

        if (string.IsNullOrWhiteSpace(quotation.SupplierEmail))
        {
            return NoAddress;
        }

        try
        {
            await emailService.Value.SendEmailAsync(
                quotation.SupplierEmail,
                BuildSubject(request),
                BuildBody(request, quotation, linkUrl),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed;
        }

        quotation.MarkEmailSent(utcNow);
        return Sent;
    }

    internal static string BuildSubject(QuotationRequestEntity request) =>
        SingleLine($"Solicitação de cotação {request.Code} - {request.BuyerName}");

    internal static string BuildBody(
        QuotationRequestEntity request,
        SupplierQuotation quotation,
        string linkUrl)
    {
        var encodedUrl = WebUtility.HtmlEncode(linkUrl);
        var deadline = request.ResponseDeadline is { } date
            ? $"<p>Responder até: <strong>{date.ToString("dd/MM/yyyy", Brazilian)}</strong></p>"
            : string.Empty;

        return
            $"<p>Olá, {WebUtility.HtmlEncode(quotation.SupplierName)}.</p>"
            + $"<p>{WebUtility.HtmlEncode(request.BuyerName)} enviou a solicitação de cotação "
            + $"<strong>{WebUtility.HtmlEncode(request.Code)}</strong> "
            + $"({WebUtility.HtmlEncode(request.ShoppingListName)}), com {request.Items.Count} "
            + (request.Items.Count == 1 ? "item" : "itens") + ".</p>"
            + deadline
            + $"<p><a href=\"{encodedUrl}\">Responder a cotação</a></p>"
            + "<p>Se o link não abrir, copie e cole este endereço no navegador:<br>"
            + $"{encodedUrl}</p>"
            + "<p>O link é exclusivo da sua empresa e não exige cadastro. "
            + "Não o encaminhe para terceiros.</p>";
    }

    private static string SingleLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
