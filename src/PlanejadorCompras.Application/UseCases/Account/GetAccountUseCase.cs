using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.Organization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class GetAccountUseCase(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser,
    ICurrentOrganization currentOrganization)
{
    public async Task<AccountDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        var activeOrganizationId = currentOrganization.OrganizationId;
        var organizations = (await organizationRepository.GetMembershipsByUserIdAsync(
                user.Id,
                cancellationToken))
            .Select(membership => new AccountOrganizationDto(
                membership.OrganizationId,
                membership.OrganizationName,
                membership.Role.ToName(),
                membership.OrganizationId == activeOrganizationId))
            .ToList();

        var active = organizations.FirstOrDefault(organization => organization.IsActive)
            ?? throw new ForbiddenException(
                "Nenhuma organização ativa para este usuário.",
                "organization_required");

        return new AccountDto(
            user.Id,
            user.Email,
            user.Name,
            user.PasswordHash is not null,
            user.GoogleId is not null,
            user.CreatedAt,
            user.TermsAcceptedAtUtc,
            user.TermsVersion,
            active,
            organizations,
            currentOrganization.Role.ToPermissionNames());
    }
}
