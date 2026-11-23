using Microsoft.AspNetCore.Http;
using PlanejadorCompras.Application.Features.Authentication.Contracts;

namespace PlanejadorCompras.API.Security;

internal sealed class AuthCookieService : IAuthCookieService
{
    public void AppendSession(HttpContext context, AuthSessionDto session)
    {
        context.Response.Cookies.Append(
            AuthenticationConstants.AccessTokenCookieName,
            session.AccessToken,
            CreateCookieOptions(context, "/", session.AccessTokenExpiresAtUtc));

        context.Response.Cookies.Append(
            AuthenticationConstants.RefreshTokenCookieName,
            session.RefreshToken,
            CreateCookieOptions(context, GetRefreshTokenPath(context), session.RefreshTokenExpiresAtUtc));
    }

    public void DeleteSession(HttpContext context)
    {
        context.Response.Cookies.Delete(
            AuthenticationConstants.AccessTokenCookieName,
            CreateCookieOptions(context, "/"));

        context.Response.Cookies.Delete(
            AuthenticationConstants.RefreshTokenCookieName,
            CreateCookieOptions(context, GetRefreshTokenPath(context)));
    }

    public string? ReadRefreshToken(HttpContext context) =>
        context.Request.Cookies[AuthenticationConstants.RefreshTokenCookieName];

    private static string GetRefreshTokenPath(HttpContext context) =>
        context.Request.PathBase.Add(AuthenticationConstants.RefreshTokenCookiePath);

    private static CookieOptions CreateCookieOptions(
        HttpContext context,
        string path,
        DateTime? expiresAtUtc = null)
    {
        var isHttps = context.Request.IsHttps;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = path,
            Expires = expiresAtUtc is { } expires
                ? new DateTimeOffset(DateTime.SpecifyKind(expires, DateTimeKind.Utc))
                : null
        };
    }
}
