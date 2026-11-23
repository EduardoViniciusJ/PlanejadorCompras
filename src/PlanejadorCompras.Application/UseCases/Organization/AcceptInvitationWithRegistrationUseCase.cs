using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.UseCases.Organization;

/// <summary>
/// Accepts an invitation by creating the account. The new account joins the
/// inviting organization directly; no session is opened here, the person
/// signs in afterwards through the regular login.
/// </summary>
public sealed class AcceptInvitationWithRegistrationUseCase(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AcceptedInvitationDto> ExecuteAsync(
        AcceptInvitationWithRegistrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.AcceptedTerms)
        {
            throw new BadRequestException(
                "Para criar a conta é preciso aceitar os Termos de Uso e a Política de Privacidade.",
                "terms_not_accepted");
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var invitation = await InvitationLookup.GetValidAsync(
            invitationRepository,
            request.Token,
            nowUtc,
            cancellationToken);

        var organization = await organizationRepository.GetByIdAsync(
                invitation.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException(
                "Convite inválido ou expirado. Peça um novo convite ao administrador.",
                "invitation_not_found");

        if (await userRepository.GetByEmailAsync(invitation.Email, cancellationToken) is not null)
        {
            throw new ConflictException(
                "Já existe uma conta com este e-mail. Entre para aceitar o convite.",
                "invitation_account_exists");
        }

        var user = User.CreateInvitedUser(
            invitation.Email,
            passwordHasher.HashPassword(request.Password),
            request.Name);
        user.AcceptTerms(OrganizationRules.CurrentTermsVersion, nowUtc);
        user.SetActiveOrganization(organization.Id);

        await userRepository.AddAsync(user, cancellationToken);
        await organizationRepository.AddMembershipAsync(
            Membership.Create(organization.Id, user.Id, invitation.Role, nowUtc),
            cancellationToken);
        invitationRepository.Remove(invitation);

        await unitOfWork.CommitAsync(cancellationToken);

        return new AcceptedInvitationDto(
            organization.Id,
            organization.Name,
            invitation.Role.ToName());
    }
}
