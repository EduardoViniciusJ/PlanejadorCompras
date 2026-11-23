using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Domain.Repositories.Organization;

public sealed record UserMembership(
    Guid OrganizationId,
    string OrganizationName,
    OrganizationRole Role,
    DateTime JoinedAtUtc,
    int MemberCount,
    int AdminCount);

public sealed record OrganizationMember(
    Guid UserId,
    string Email,
    string? Name,
    OrganizationRole Role,
    DateTime JoinedAtUtc);

public interface IOrganizationRepository
{
    Task<Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Entities.Organization organization, CancellationToken cancellationToken = default);

    Task<Membership?> GetMembershipAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<UserMembership>> GetMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<OrganizationMember>> GetMembersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<int> CountAdminsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(Membership membership, CancellationToken cancellationToken = default);

    void RemoveMembership(Membership membership);
}
