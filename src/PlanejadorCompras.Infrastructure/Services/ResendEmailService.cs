using Microsoft.Extensions.Configuration;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using Resend;

namespace PlanejadorCompras.Infrastructure.Services;

public sealed class ResendEmailService : IEmailService
{
    private readonly IResend? _resend;
    private readonly string? _fromEmail;

    public ResendEmailService(IConfiguration configuration)
    {
        var apiKey = configuration["Resend:ApiKey"];
        var fromEmail = configuration["Resend:FromEmail"];

        // E-mail delivery is optional at startup: without it only the routes that send e-mail
        // fail, with an explicit error, instead of the service failing to be constructed.
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
        {
            return;
        }

        _resend = ResendClient.Create(apiKey);
        _fromEmail = fromEmail;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (_resend is null || _fromEmail is null)
        {
            throw new ServiceUnavailableException(
                "O envio de e-mails não está disponível no momento. Tente novamente mais tarde.",
                "email_delivery_not_configured");
        }

        var message = new EmailMessage
        {
            From = _fromEmail,
            To = to,
            Subject = subject,
            HtmlBody = body
        };

        await _resend.EmailSendAsync(message, cancellationToken);
    }
}
