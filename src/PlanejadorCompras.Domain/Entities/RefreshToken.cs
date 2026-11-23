using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Domain.Entities;

/// <summary>
/// One link in a session's chain of rotating refresh tokens. Every refresh revokes the presented
/// token and issues a successor with the same <see cref="SessionId"/>. Presenting a token that was
/// already rotated means it was copied, and the whole session is revoked.
/// </summary>
public sealed class RefreshToken
{
    public const int DisplayNameMaxLength = 200;

    private RefreshToken(
        Guid id,
        Guid userId,
        Guid sessionId,
        string tokenHash,
        string displayName,
        DateTime createdAt,
        DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        SessionId = sessionId;
        TokenHash = tokenHash;
        DisplayName = displayName;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Identifies the login session; shared by every token of the same rotation chain.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>SHA-256 of the token held by the browser. The raw token is never stored.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Name shown in the UI, captured at login so a refresh can rebuild the access token.</summary>
    public string DisplayName { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public RefreshTokenRevocationReason? RevocationReason { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsRevoked => RevokedAt is not null;

    public static (RefreshToken Token, string RawToken) StartSession(
        Guid userId,
        string displayName,
        DateTime nowUtc,
        TimeSpan lifetime)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        return Issue(userId, Guid.NewGuid(), displayName, nowUtc, lifetime);
    }

    /// <summary>Revokes this token as rotated and returns its successor in the same session.</summary>
    public (RefreshToken Token, string RawToken) Rotate(DateTime nowUtc, TimeSpan lifetime)
    {
        Revoke(RefreshTokenRevocationReason.Rotated, nowUtc);

        return Issue(UserId, SessionId, DisplayName, nowUtc, lifetime);
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

    /// <summary>
    /// True when this token was rotated so recently that presenting it again is most likely a
    /// concurrent refresh from the same browser (two tabs, a retried request), not a stolen token.
    /// </summary>
    public bool WasRotatedWithin(TimeSpan leeway, DateTime nowUtc) =>
        RevocationReason == RefreshTokenRevocationReason.Rotated
        && RevokedAt is { } revokedAt
        && nowUtc - revokedAt <= leeway;

    /// <summary>Revokes the token. The first revocation wins; later calls change nothing.</summary>
    public void Revoke(RefreshTokenRevocationReason reason, DateTime nowUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = nowUtc;
        RevocationReason = reason;
    }

    private static (RefreshToken Token, string RawToken) Issue(
        Guid userId,
        Guid sessionId,
        string displayName,
        DateTime nowUtc,
        TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        var normalizedName = displayName.Trim();
        if (normalizedName.Length > DisplayNameMaxLength)
        {
            normalizedName = normalizedName[..DisplayNameMaxLength];
        }

        var rawToken = SecretToken.Generate();
        var token = new RefreshToken(
            Guid.NewGuid(),
            userId,
            sessionId,
            SecretToken.Hash(rawToken),
            normalizedName,
            nowUtc,
            nowUtc.Add(lifetime));

        return (token, rawToken);
    }
}

public enum RefreshTokenRevocationReason
{
    Rotated = 1,
    LoggedOut = 2,
    PasswordReset = 3,
    ReuseDetected = 4,
    CredentialsDiscarded = 5
}
