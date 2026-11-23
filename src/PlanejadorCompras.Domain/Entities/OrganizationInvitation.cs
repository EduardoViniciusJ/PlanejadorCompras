using System.Security.Cryptography;
using System.Text;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Domain.Entities;

/// <summary>
/// Pending invitation to join an organization. Only the SHA-256 hash of the
/// token is stored; the raw token exists solely in the link sent by e-mail.
/// </summary>
public sealed class OrganizationInvitation
{
    private OrganizationInvitation(
        Guid id,
        Guid organizationId,
        string email,
        OrganizationRole role,
        string tokenHash,
        Guid invitedByUserId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Email = email;
        Role = role;
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Email { get; private set; }

    public OrganizationRole Role { get; private set; }

    public string TokenHash { get; private set; }

    public Guid InvitedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public static (OrganizationInvitation Invitation, string RawToken) Create(
        Guid organizationId,
        string email,
        OrganizationRole role,
        Guid invitedByUserId,
        DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(organizationId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(invitedByUserId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitation = new OrganizationInvitation(
            Guid.NewGuid(),
            organizationId,
            NormalizeEmail(email),
            role,
            HashToken(rawToken),
            invitedByUserId,
            nowUtc,
            nowUtc.AddDays(OrganizationRules.InvitationValidDays));

        return (invitation, rawToken);
    }

    /// <summary>
    /// Re-issues the invitation for the same e-mail: new token, role and
    /// validity. The previous link stops working. Returns the new raw token.
    /// </summary>
    public string Renew(OrganizationRole role, Guid invitedByUserId, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invitedByUserId, Guid.Empty);

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        Role = role;
        TokenHash = HashToken(rawToken);
        InvitedByUserId = invitedByUserId;
        CreatedAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc.AddDays(OrganizationRules.InvitationValidDays);

        return rawToken;
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc > ExpiresAtUtc;

    public bool IsFor(string email) =>
        string.Equals(Email, NormalizeEmail(email), StringComparison.Ordinal);

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim())));
    }
}
