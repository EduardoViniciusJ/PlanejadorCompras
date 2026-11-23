using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class GetInvitationPreviewUseCase(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider)
{
    public async Task<InvitationPreviewDto> ExecuteAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await InvitationLookup.GetValidAsync(
            invitationRepository,
            token,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        var organization = await organizationRepository.GetByIdAsync(
                invitation.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Convite inválido ou expirado. Peça um novo convite ao administrador.",
                "invitation_not_found");

        var existingUser = await userRepository.GetByEmailAsync(invitation.Email, cancellationToken);

        return new InvitationPreviewDto(
            organization.Name,
            invitation.Email,
            invitation.Role.ToName(),
            invitation.ExpiresAtUtc,
            existingUser is not null);
    }
}
