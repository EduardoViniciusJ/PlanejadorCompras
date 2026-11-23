namespace PlanejadorCompras.Domain.Entities;

public sealed class User
{
    private User(Guid id, string? googleId, string email, string? passwordHash, bool isEmailConfirmed, DateTime createdAt)
    {
        Id = id;
        GoogleId = googleId;
        Email = email;
        PasswordHash = passwordHash;
        IsEmailConfirmed = isEmailConfirmed;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string? GoogleId { get; private set; }
    public string Email { get; private set; } = null!;
    public string? PasswordHash { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static User CreateGoogleUser(string googleId, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new User(
            Guid.NewGuid(),
            googleId.Trim(),
            email.Trim().ToLowerInvariant(),
            null,
            true, // Google emails are pre-confirmed
            DateTime.UtcNow);
    }

    public static User CreateManualUser(string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User(
            Guid.NewGuid(),
            null,
            email.Trim().ToLowerInvariant(),
            passwordHash,
            false,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Links a Google identity to this account. Google has proven ownership of the e-mail, so the
    /// e-mail becomes confirmed. If it was NOT confirmed before, whoever registered the account never
    /// proved they own the address: the existing password is discarded so it cannot be used to take
    /// over the account. Returns true when those unverified credentials were discarded, in which case
    /// the caller must also invalidate pending tokens and sessions.
    /// </summary>
    public bool LinkGoogleAccount(string googleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleId);

        var hadUnverifiedCredentials = !IsEmailConfirmed;

        if (hadUnverifiedCredentials)
        {
            PasswordHash = null;
        }

        GoogleId = googleId.Trim();
        IsEmailConfirmed = true;

        return hadUnverifiedCredentials;
    }

    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
    }
}
