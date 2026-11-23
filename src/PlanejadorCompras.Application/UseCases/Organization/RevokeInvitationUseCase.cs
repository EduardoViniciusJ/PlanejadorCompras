using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class RevokeInvitationUseCase(
    IOrganizationInvitationRepository invitationRepository,
    ICurrentOrganization currentOrganization,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invitationId, Guid.Empty);
        currentOrganization.Require(OrganizationPermission.ManageTeam);

        var invitation = await invitationRepository.GetByIdAsync(
                currentOrganization.OrganizationId,
                invitationId,
                cancellationToken)
            ?? throw new NotFoundException("Convite não encontrado.", "invitation_not_found");

        invitationRepository.Remove(invitation);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
