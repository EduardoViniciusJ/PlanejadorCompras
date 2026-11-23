using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.RefreshToken;
using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Application.UseCases.Auth;

/// <summary>
/// Exchanges a refresh token for a new access token and a new refresh token (rotation).
/// A refresh token works once. Presenting one that was already rotated means it was copied, so
/// the whole session is revoked and both the thief and the owner have to log in again.
/// </summary>
public sealed class RefreshSessionUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthSessionService _authSessionService;
    private readonly SessionSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshSessionUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        AuthSessionService authSessionService,
        SessionSettings settings,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _authSessionService = authSessionService;
        _settings = settings;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthSessionDto> ExecuteAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw SessionExpired();
        }

        var current = await _refreshTokenRepository.GetByTokenHashAsync(
            SecretToken.Hash(rawRefreshToken),
            cancellationToken);

        if (current is null)
        {
            throw SessionExpired();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (current.IsRevoked)
        {
            // A token rotated a moment ago is most likely a second tab or a retried request racing
            // the first one. It is accepted only while the session itself is still alive, so a
            // logout or reuse detection that happened in between is never undone.
            var isConcurrentRefresh =
                current.WasRotatedWithin(_settings.RefreshTokenReuseLeeway, now)
                && await _refreshTokenRepository.IsSessionActiveAsync(current.SessionId, now, cancellationToken);

            if (!isConcurrentRefresh)
            {
                await _refreshTokenRepository.RevokeSessionAsync(
                    current.SessionId,
                    RefreshTokenRevocationReason.ReuseDetected,
                    now,
                    cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                throw new UnauthorizedException(
                    "Sua sessão foi encerrada. Faça login novamente.",
                    "session_revoked");
            }
        }

        if (current.IsExpired(now))
        {
            throw SessionExpired();
        }

        var session = await _authSessionService.RotateAsync(current, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return session;
    }

    private static UnauthorizedException SessionExpired() =>
        new("Sua sessão expirou. Faça login novamente.", "session_expired");
}
