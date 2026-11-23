namespace PlanejadorCompras.Domain.Entities;

public sealed class Membership
{
    private Membership(Guid organizationId, Guid userId, OrganizationRole role, DateTime createdAtUtc)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public OrganizationRole Role { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Membership Create(
        Guid organizationId,
        Guid userId,
        OrganizationRole role,
        DateTime createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(organizationId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        EnsureDefined(role);

        return new Membership(organizationId, userId, role, createdAtUtc);
    }

    public void ChangeRole(OrganizationRole role)
    {
        EnsureDefined(role);
        Role = role;
    }

    private static void EnsureDefined(OrganizationRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }
    }
}
