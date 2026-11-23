using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class UpdateOrganizationUseCase(
    IOrganizationRepository organizationRepository,
    ICurrentOrganization currentOrganization,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(
        UpdateOrganizationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        currentOrganization.Require(OrganizationPermission.ManageTeam);

        var organization = await organizationRepository.GetByIdAsync(
                currentOrganization.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException("Organização não encontrada.", "organization_not_found");

        organization.Rename(request.Name);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
