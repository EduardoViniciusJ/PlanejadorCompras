using Microsoft.AspNetCore.Http;
using PlanejadorCompras.Application.Features.Authentication.Contracts;

namespace PlanejadorCompras.API.Security;

public interface IAuthCookieService
{
    /// <summary>Writes the access token and refresh token cookies of a session.</summary>
    void AppendSession(HttpContext context, AuthSessionDto session);

    /// <summary>Removes both session cookies from the browser.</summary>
    void DeleteSession(HttpContext context);

    string? ReadRefreshToken(HttpContext context);
}
