using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Domain.Repositories.Organization;

public sealed record PendingInvitation(
    Guid Id,
    string Email,
    OrganizationRole Role,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string InvitedBy);

public interface IOrganizationInvitationRepository
{
    Task<OrganizationInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<OrganizationInvitation?> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrganizationInvitation?> GetByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    Task<List<PendingInvitation>> GetPendingAsync(
        Guid organizationId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);

    void Remove(OrganizationInvitation invitation);
}
