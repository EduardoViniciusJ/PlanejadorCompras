using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class GetOrganizationMembersUseCase(
    IOrganizationRepository organizationRepository,
    ICurrentOrganization currentOrganization,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyCollection<OrganizationMemberDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        currentOrganization.Require(OrganizationPermission.ViewTeam);

        var members = await organizationRepository.GetMembersAsync(
            currentOrganization.OrganizationId,
            cancellationToken);
        var currentUserId = currentUser.UserId;

        return members
            .Select(member => new OrganizationMemberDto(
                member.UserId,
                member.Email,
                member.Name,
                member.Role.ToName(),
                member.JoinedAtUtc,
                member.UserId == currentUserId))
            .ToList();
    }
}
