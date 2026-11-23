using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class GetAccountDeletionPreviewUseCase(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
{
    public async Task<AccountDeletionPreviewDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        var plan = AccountDeletionPlan.From(
            await organizationRepository.GetMembershipsByUserIdAsync(user.Id, cancellationToken));

        return new AccountDeletionPreviewDto(
            plan.CanDelete,
            user.PasswordHash is not null,
            Map(plan.ToDelete),
            Map(plan.ToLeave),
            Map(plan.Blocking));
    }

    private static List<AccountDeletionOrganizationDto> Map(
        IEnumerable<UserMembership> memberships) =>
        memberships
            .Select(membership => new AccountDeletionOrganizationDto(
                membership.OrganizationId,
                membership.OrganizationName))
            .ToList();
}
