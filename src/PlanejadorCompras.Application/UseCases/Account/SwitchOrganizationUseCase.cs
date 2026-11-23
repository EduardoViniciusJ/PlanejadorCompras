using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class SwitchOrganizationUseCase(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(
        SwitchOrganizationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfEqual(request.OrganizationId, Guid.Empty);

        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        // Not being a member and the organization not existing look the same.
        if (await organizationRepository.GetMembershipAsync(
                request.OrganizationId,
                user.Id,
                cancellationToken) is null)
        {
            throw new NotFoundException("Organização não encontrada.", "organization_not_found");
        }

        user.SetActiveOrganization(request.OrganizationId);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
