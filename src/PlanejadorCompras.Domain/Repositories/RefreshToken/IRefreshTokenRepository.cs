using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Domain.Repositories.RefreshToken;

public interface IRefreshTokenRepository
{
    Task AddAsync(Entities.RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Finds a token by the SHA-256 hash of its raw value, with its user loaded.</summary>
    Task<Entities.RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>True while the session still has a token that is neither revoked nor expired.</summary>
    Task<bool> IsSessionActiveAsync(Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Revokes every token of one session. Persisted on the next unit-of-work commit.</summary>
    Task RevokeSessionAsync(
        Guid sessionId,
        RefreshTokenRevocationReason reason,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes every session of a user. Persisted on the next unit-of-work commit.</summary>
    Task RevokeAllForUserAsync(
        Guid userId,
        RefreshTokenRevocationReason reason,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a user's expired tokens right away; they are unusable, so nothing is lost.</summary>
    Task RemoveExpiredForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
