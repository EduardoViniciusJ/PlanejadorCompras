using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Domain.Repositories.PurchaseOrder;

public interface IPurchaseOrderRepository
{
    Task<Entities.PurchaseOrder?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<Entities.PurchaseOrder>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForSourceAsync(
        Guid organizationId,
        Guid shoppingListId,
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Entities.PurchaseOrder purchaseOrder,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
