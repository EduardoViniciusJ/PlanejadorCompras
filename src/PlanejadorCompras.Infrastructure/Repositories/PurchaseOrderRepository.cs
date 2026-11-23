using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.PurchaseOrder;
using PlanejadorCompras.Infrastructure.Persistence;
using PurchaseOrderEntity = PlanejadorCompras.Domain.Entities.PurchaseOrder;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class PurchaseOrderRepository(PlanejadorComprasDbContext context)
    : IPurchaseOrderRepository
{
    public Task<PurchaseOrderEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.PurchaseOrders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<List<PurchaseOrderEntity>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        context.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.OrganizationId == organizationId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<(List<PurchaseOrderEntity> Items, int TotalCount)> GetPageByOrganizationIdAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.PurchaseOrders
            .AsNoTracking()
            .Where(order => order.OrganizationId == organizationId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<bool> ExistsForSourceAsync(
        Guid organizationId,
        Guid shoppingListId,
        Guid supplierId,
        CancellationToken cancellationToken = default) =>
        context.PurchaseOrders.AnyAsync(
            order =>
                order.OrganizationId == organizationId
                && order.SourceShoppingListId == shoppingListId
                && order.SupplierId == supplierId
                && order.Status != PurchaseOrderStatus.Cancelled,
            cancellationToken);

    public async Task AddAsync(
        PurchaseOrderEntity purchaseOrder,
        CancellationToken cancellationToken = default)
    {
        await context.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return false;
        }

        context.PurchaseOrders.Remove(order);
        return true;
    }
}
