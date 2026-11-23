using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.RefreshToken;

namespace PlanejadorCompras.Application.Services;

/// <summary>
/// Issues the access token + refresh token pair of a login session. It only adds entities to the
/// repository: the calling use case owns the unit-of-work commit.
/// </summary>
public sealed class AuthSessionService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly SessionSettings _settings;
    private readonly TimeProvider _timeProvider;

    public AuthSessionService(
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        SessionSettings settings,
        TimeProvider timeProvider)
    {
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _settings = settings;
        _timeProvider = timeProvider;
    }

    /// <summary>Starts a new session for a user who has just proven their identity.</summary>
    public async Task<AuthSessionDto> StartAsync(
        User user,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await _refreshTokenRepository.RemoveExpiredForUserAsync(user.Id, now, cancellationToken);

        var (refreshToken, rawRefreshToken) = RefreshToken.StartSession(
            user.Id,
            string.IsNullOrWhiteSpace(displayName) ? user.Email : displayName,
            now,
            _settings.RefreshTokenLifetime);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return BuildSession(user.Email, refreshToken, rawRefreshToken);
    }

    /// <summary>Rotates a refresh token: revokes it and issues its successor in the same session.</summary>
    public async Task<AuthSessionDto> RotateAsync(
        RefreshToken current,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var (successor, rawRefreshToken) = current.Rotate(now, _settings.RefreshTokenLifetime);

        await _refreshTokenRepository.AddAsync(successor, cancellationToken);

        return BuildSession(current.User.Email, successor, rawRefreshToken);
    }

    private AuthSessionDto BuildSession(string email, RefreshToken refreshToken, string rawRefreshToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(
            new GenerateAccessTokenRequestDto(
                refreshToken.UserId,
                email,
                refreshToken.DisplayName,
                refreshToken.SessionId));

        return new AuthSessionDto(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshToken.ExpiresAt);
    }
}
