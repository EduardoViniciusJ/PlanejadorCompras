using PlanejadorCompras.Domain.Rules;
using PlanejadorCompras.Domain.Validation;

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

    /// <summary>Display name chosen by the user. Null until the profile is filled in.</summary>
    public string? Name { get; private set; }

    /// <summary>Organization the user is currently working in. Null means "the oldest membership".</summary>
    public Guid? ActiveOrganizationId { get; private set; }

    public DateTime? TermsAcceptedAtUtc { get; private set; }

    public string? TermsVersion { get; private set; }

    /// <summary>Set when the account was erased but the row had to be kept as an anonymous tombstone.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc.HasValue;

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
    /// Account created by accepting an invitation. The invitation link was
    /// delivered to this address, so the e-mail is already proven.
    /// </summary>
    public static User CreateInvitedUser(string email, string passwordHash, string name)
    {
        var user = CreateManualUser(email, passwordHash);
        user.IsEmailConfirmed = true;
        user.UpdateName(name);
        return user;
    }

    public void UpdateName(string? name)
    {
        Name = DomainText.Optional(name, OrganizationRules.UserNameMaxLength, nameof(name));
    }

    public void SetActiveOrganization(Guid? organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(organizationId));
        }

        ActiveOrganizationId = organizationId;
    }

    public void AcceptTerms(string version, DateTime nowUtc)
    {
        TermsVersion = DomainText.Required(
            version,
            OrganizationRules.TermsVersionMaxLength,
            nameof(version));
        TermsAcceptedAtUtc = nowUtc;
    }

    /// <summary>
    /// Removes every personal detail and makes the account impossible to sign in
    /// to. Used when the row has to stay because documents of an organization
    /// that still has other members reference it.
    /// </summary>
    public void Anonymize(DateTime nowUtc)
    {
        DeletedAtUtc = nowUtc;
        Email = $"removido-{Id:N}@conta-excluida.invalid";
        GoogleId = null;
        PasswordHash = null;
        Name = null;
        ActiveOrganizationId = null;
        IsEmailConfirmed = false;
        TermsAcceptedAtUtc = null;
        TermsVersion = null;
    }

    public void LinkGoogleAccount(string googleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleId);
        GoogleId = googleId.Trim();
        IsEmailConfirmed = true; // Linking a Google account implies email confirmation
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
