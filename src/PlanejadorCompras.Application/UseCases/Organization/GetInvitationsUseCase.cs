using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class GetInvitationsUseCase(
    IOrganizationInvitationRepository invitationRepository,
    ICurrentOrganization currentOrganization,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<InvitationDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        currentOrganization.Require(OrganizationPermission.ManageTeam);

        var invitations = await invitationRepository.GetPendingAsync(
            currentOrganization.OrganizationId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return invitations
            .Select(invitation => new InvitationDto(
                invitation.Id,
                invitation.Email,
                invitation.Role.ToName(),
                invitation.CreatedAtUtc,
                invitation.ExpiresAtUtc,
                invitation.InvitedBy))
            .ToList();
    }
}
