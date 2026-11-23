namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IInvitationEmailSender
{
    /// <summary>
    /// Sends the invitation e-mail. Returns false, without throwing, when the
    /// e-mail provider is not configured or the delivery fails.
    /// </summary>
    Task<bool> TrySendAsync(
        string to,
        string organizationName,
        string invitedBy,
        string roleLabel,
        string acceptUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
