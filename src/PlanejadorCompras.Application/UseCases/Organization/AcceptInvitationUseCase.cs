using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Organization;

/// <summary>Accepts an invitation with the account that is signed in.</summary>
public sealed class AcceptInvitationUseCase(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AcceptedInvitationDto> ExecuteAsync(
        AcceptInvitationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var invitation = await InvitationLookup.GetValidAsync(
            invitationRepository,
            request.Token,
            nowUtc,
            cancellationToken);

        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        // The link alone is not enough: it only works for the invited address,
        // so a forwarded or leaked link cannot be used by another account.
        if (!invitation.IsFor(user.Email))
        {
            throw new ForbiddenException(
                "Este convite foi enviado para outro e-mail. Entre com a conta do e-mail convidado.",
                "invitation_email_mismatch");
        }

        var organization = await organizationRepository.GetByIdAsync(
                invitation.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Convite inválido ou expirado. Peça um novo convite ao administrador.",
                "invitation_not_found");

        var membership = await organizationRepository.GetMembershipAsync(
            organization.Id,
            user.Id,
            cancellationToken);

        if (membership is null)
        {
            membership = Membership.Create(organization.Id, user.Id, invitation.Role, nowUtc);
            await organizationRepository.AddMembershipAsync(membership, cancellationToken);
        }

        user.SetActiveOrganization(organization.Id);
        invitationRepository.Remove(invitation);
        await unitOfWork.CommitAsync(cancellationToken);

        return new AcceptedInvitationDto(
            organization.Id,
            organization.Name,
            membership.Role.ToName());
    }
}
