using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.RefreshToken;
using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Application.UseCases.Auth;

/// <summary>
/// Ends a session on the server: its refresh tokens are revoked, so they cannot be exchanged
/// again and access tokens already issued for the session stop being accepted.
/// </summary>
public sealed class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    /// <param name="rawRefreshToken">Refresh token from the cookie, when the browser sent it.</param>
    /// <param name="authenticatedSessionId">Session of the access token, when one was valid.</param>
    public async Task ExecuteAsync(
        string? rawRefreshToken,
        Guid? authenticatedSessionId,
        CancellationToken cancellationToken = default)
    {
        var sessionIds = new HashSet<Guid>();

        if (authenticatedSessionId is { } sessionId && sessionId != Guid.Empty)
        {
            sessionIds.Add(sessionId);
        }

        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(
                SecretToken.Hash(rawRefreshToken),
                cancellationToken);

            if (refreshToken is not null)
            {
                sessionIds.Add(refreshToken.SessionId);
            }
        }

        if (sessionIds.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var id in sessionIds)
        {
            await _refreshTokenRepository.RevokeSessionAsync(
                id,
                RefreshTokenRevocationReason.LoggedOut,
                now,
                cancellationToken);
        }

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
