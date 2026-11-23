using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.Supplier;
using SupplierEntity = PlanejadorCompras.Domain.Entities.Supplier;

namespace PlanejadorCompras.Infrastructure.Services;

public sealed class SupplierAccessService(
    ISupplierRepository supplierRepository,
    ICurrentOrganization currentOrganization) : ISupplierAccessService
{
    public async Task<SupplierEntity> GetForCurrentUserAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(supplierId, Guid.Empty);

        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null || supplier.OrganizationId != currentOrganization.OrganizationId)
        {
            throw new NotFoundException("Supplier not found.", "supplier_not_found");
        }

        return supplier;
    }
}
