using System.Globalization;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Services;

/// <summary>
/// Sends invitation e-mails through the existing <see cref="IEmailService"/>.
/// The service is resolved lazily because its constructor throws when the
/// provider key is missing; an invitation must still be created in that case.
/// </summary>
public sealed class InvitationEmailSender(
    IServiceProvider serviceProvider,
    ILogger<InvitationEmailSender> logger) : IInvitationEmailSender
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<bool> TrySendAsync(
        string to,
        string organizationName,
        string invitedBy,
        string roleLabel,
        string acceptUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var emailService = serviceProvider.GetRequiredService<IEmailService>();
            var organization = WebUtility.HtmlEncode(organizationName);
            var inviter = WebUtility.HtmlEncode(invitedBy);
            var role = WebUtility.HtmlEncode(roleLabel);
            var link = WebUtility.HtmlEncode(acceptUrl);
            var expiresOn = expiresAtUtc.ToString("dd/MM/yyyy", Culture);

            var body =
                $"<p>{inviter} convidou você para participar da organização "
                + $"<strong>{organization}</strong> no Planejador de Compras, com o papel de {role}.</p>"
                + $"<p><a href='{link}'>Aceitar convite</a></p>"
                + $"<p>O convite vale até {expiresOn}. Se você não esperava esta mensagem, basta ignorá-la.</p>";

            await emailService.SendEmailAsync(
                to,
                $"Convite para a organização {organizationName}",
                body,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Never log the recipient or the link: the link carries the invitation token.
            logger.LogWarning(
                "Invitation e-mail could not be sent ({ExceptionType}). The invitation link is still valid.",
                exception.GetType().Name);

            return false;
        }
    }
}
