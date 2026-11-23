using PlanejadorCompras.Application.Exceptions;

namespace PlanejadorCompras.Infrastructure.Services;

internal static class GoogleLoginConfiguration
{
    /// <summary>
    /// Error returned by the Google login route when the Google credentials
    /// (Authentication:Google:ClientId, ClientSecret, RedirectUri) are not configured.
    /// </summary>
    internal static ServiceUnavailableException NotConfigured() =>
        new(
            "O login com Google não está disponível no momento. Entre com e-mail e senha.",
            "google_login_not_configured");
}
