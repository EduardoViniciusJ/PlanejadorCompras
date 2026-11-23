namespace PlanejadorCompras.Application.Features.Authentication.Contracts;

/// <summary>
/// Credentials of a login session: a short-lived access token (JWT) and the raw rotating refresh
/// token. Both only ever travel to the browser as HttpOnly cookies.
/// </summary>
public sealed record AuthSessionDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
