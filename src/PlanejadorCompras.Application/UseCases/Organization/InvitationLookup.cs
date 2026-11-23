using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

internal static class InvitationLookup
{
    /// <summary>
    /// Finds a usable invitation by its raw token. Unknown, revoked and expired
    /// tokens are indistinguishable to the caller.
    /// </summary>
    public static async Task<OrganizationInvitation> GetValidAsync(
        IOrganizationInvitationRepository invitationRepository,
        string? rawToken,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var invitation = await invitationRepository.GetByTokenHashAsync(
                OrganizationInvitation.HashToken(rawToken),
                cancellationToken);

            if (invitation is not null && !invitation.IsExpired(nowUtc))
            {
                return invitation;
            }
        }

        throw new NotFoundException(
            "Convite inválido ou expirado. Peça um novo convite ao administrador.",
            "invitation_not_found");
    }
}
