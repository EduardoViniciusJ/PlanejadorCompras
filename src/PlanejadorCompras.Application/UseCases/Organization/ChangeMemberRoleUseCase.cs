using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class ChangeMemberRoleUseCase(
    IOrganizationRepository organizationRepository,
    ICurrentOrganization currentOrganization,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(
        Guid userId,
        ChangeMemberRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request);
        currentOrganization.Require(OrganizationPermission.ManageTeam);

        var newRole = OrganizationText.ParseRole(request.Role);
        var organizationId = currentOrganization.OrganizationId;

        var membership = await organizationRepository.GetMembershipAsync(
                organizationId,
                userId,
                cancellationToken)
            ?? throw new NotFoundException("Membro não encontrado.", "organization_member_not_found");

        if (membership.Role == newRole)
        {
            return;
        }

        if (membership.Role == OrganizationRole.Admin)
        {
            await LastAdminGuard.EnsureAnotherAdminAsync(
                organizationRepository,
                organizationId,
                cancellationToken);
        }

        membership.ChangeRole(newRole);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
