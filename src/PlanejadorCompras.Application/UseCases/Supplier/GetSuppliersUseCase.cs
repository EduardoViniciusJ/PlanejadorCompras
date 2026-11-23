using PlanejadorCompras.Application.Features.Suppliers.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.Supplier;

namespace PlanejadorCompras.Application.UseCases.Supplier;

public sealed class GetSuppliersUseCase(
    ISupplierRepository supplierRepository,
    ICurrentOrganization currentOrganization)
{
    public async Task<IReadOnlyCollection<SupplierResponseDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var suppliers = await supplierRepository.GetByOrganizationIdAsync(currentOrganization.OrganizationId, cancellationToken);
        return suppliers
            .Select(SupplierResponseMapper.Map)
            .ToList();
    }
}
