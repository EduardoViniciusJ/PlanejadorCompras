using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

/// <summary>
/// Removes a member from the current organization. Administrators can remove
/// anyone; any member can remove themself (leave). The data the member
/// created stays with the organization.
/// </summary>
public sealed class RemoveMemberUseCase(
    IOrganizationRepository organizationRepository,
    ICurrentOrganization currentOrganization,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        if (userId != currentUser.UserId)
        {
            currentOrganization.Require(OrganizationPermission.ManageTeam);
        }

        var organizationId = currentOrganization.OrganizationId;
        var membership = await organizationRepository.GetMembershipAsync(
                organizationId,
                userId,
                cancellationToken)
            ?? throw new NotFoundException("Membro não encontrado.", "organization_member_not_found");

        if (membership.Role == OrganizationRole.Admin)
        {
            await LastAdminGuard.EnsureAnotherAdminAsync(
                organizationRepository,
                organizationId,
                cancellationToken);
        }

        organizationRepository.RemoveMembership(membership);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
