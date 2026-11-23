using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

public sealed class GetOrganizationUseCase(
    IOrganizationRepository organizationRepository,
    ICurrentOrganization currentOrganization)
{
    public async Task<OrganizationDetailDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(
                currentOrganization.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException("Organização não encontrada.", "organization_not_found");

        var members = await organizationRepository.GetMembersAsync(
            organization.Id,
            cancellationToken);

        return new OrganizationDetailDto(
            organization.Id,
            organization.Name,
            currentOrganization.Role.ToName(),
            members.Count,
            currentOrganization.Role.ToPermissionNames());
    }
}
