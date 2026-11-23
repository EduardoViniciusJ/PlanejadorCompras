namespace PlanejadorCompras.API.Security;

internal static class AuthenticationConstants
{
    public const string AccessTokenCookieName = "access_token";
    public const string RefreshTokenCookieName = "refresh_token";

    /// <summary>
    /// The refresh token is only sent to the auth endpoints (refresh and logout), never along
    /// with ordinary API calls.
    /// </summary>
    public const string RefreshTokenCookiePath = "/api/auth";

    public const string SessionIdClaimName = "sid";
    public const string XmlHttpRequestHeaderName = "X-Requested-With";
    public const string XmlHttpRequestHeaderValue = "XmlHttpRequest";
}
