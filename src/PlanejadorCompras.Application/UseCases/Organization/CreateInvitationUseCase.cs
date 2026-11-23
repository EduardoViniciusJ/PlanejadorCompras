using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class CreateInvitationUseCase(
    IOrganizationRepository organizationRepository,
    IOrganizationInvitationRepository invitationRepository,
    IUserRepository userRepository,
    ICurrentOrganization currentOrganization,
    ICurrentUser currentUser,
    IFrontendUrlService frontendUrlService,
    IInvitationEmailSender invitationEmailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CreatedInvitationDto> ExecuteAsync(
        CreateInvitationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        currentOrganization.Require(OrganizationPermission.ManageTeam);

        var role = OrganizationText.ParseRole(request.Role);
        var email = OrganizationInvitation.NormalizeEmail(request.Email);
        var organizationId = currentOrganization.OrganizationId;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken)
            ?? throw new NotFoundException("Organização não encontrada.", "organization_not_found");

        var invitedUser = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (invitedUser is not null
            && await organizationRepository.GetMembershipAsync(
                organizationId,
                invitedUser.Id,
                cancellationToken) is not null)
        {
            throw new ConflictException(
                "Esta pessoa já faz parte da organização.",
                "invitation_already_member");
        }

        var invitation = await invitationRepository.GetByEmailAsync(
            organizationId,
            email,
            cancellationToken);
        string rawToken;

        if (invitation is not null)
        {
            // Inviting the same e-mail again replaces the previous link.
            rawToken = invitation.Renew(role, currentUser.UserId, nowUtc);
        }
        else
        {
            if (await invitationRepository.CountAsync(organizationId, cancellationToken)
                >= OrganizationRules.MaxPendingInvitations)
            {
                throw new ConflictException(
                    "Há convites pendentes demais. Cancele alguns antes de enviar novos.",
                    "invitation_limit_reached");
            }

            (invitation, rawToken) = OrganizationInvitation.Create(
                organizationId,
                email,
                role,
                currentUser.UserId,
                nowUtc);
            await invitationRepository.AddAsync(invitation, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        var acceptUrl = frontendUrlService.BuildInvitationUrl(rawToken);
        var invitedBy = string.IsNullOrWhiteSpace(currentUser.Name)
            ? currentUser.Email
            : currentUser.Name;

        // The invitation is already saved: a missing e-mail provider must not
        // turn into an error. The administrator gets the link to share by hand.
        var emailSent = await invitationEmailSender.TrySendAsync(
            invitation.Email,
            organization.Name,
            invitedBy,
            role.ToLabel(),
            acceptUrl,
            invitation.ExpiresAtUtc,
            cancellationToken);

        return new CreatedInvitationDto(
            invitation.Id,
            invitation.Email,
            invitation.Role.ToName(),
            invitation.ExpiresAtUtc,
            emailSent,
            acceptUrl);
    }
}
