using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Domain.Entities;

public sealed class UserToken
{
    private UserToken(Guid id, Guid userId, string tokenHash, string type, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        Type = type;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 of the token sent to the user. The raw token is never stored.</summary>
    public string TokenHash { get; private set; } = null!;
    public string Type { get; private set; } = null!; // e.g. "EmailConfirmation", "PasswordReset"
    public DateTime ExpiresAt { get; private set; }

    public User User { get; private set; } = null!;

    /// <summary>
    /// Creates a token and returns it together with the raw value, which is only available here
    /// (to be sent to the user) and cannot be recovered from the entity afterwards.
    /// </summary>
    public static (UserToken Token, string RawToken) Create(Guid userId, string type, TimeSpan validFor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(validFor, TimeSpan.Zero);

        var rawToken = SecretToken.Generate();
        var token = new UserToken(
            Guid.NewGuid(),
            userId,
            SecretToken.Hash(rawToken),
            type,
            DateTime.UtcNow.Add(validFor));

        return (token, rawToken);
    }

    public bool IsValid() => DateTime.UtcNow <= ExpiresAt;
}
