using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Supplier;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class SupplierRepository(PlanejadorComprasDbContext context) : ISupplierRepository
{
    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);

    public Task<List<Supplier>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var supplierIds = ids.Distinct().ToList();
        return context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<List<Supplier>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.OrganizationId == organizationId)
            .OrderBy(supplier => supplier.Name)
            .ToListAsync(cancellationToken);

    public async Task<(List<Supplier> Items, int TotalCount)> GetPageByOrganizationIdAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.OrganizationId == organizationId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<bool> ExistsByNameAsync(
        Guid organizationId,
        string name,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default) =>
        context.Suppliers.AnyAsync(
            supplier => supplier.OrganizationId == organizationId &&
                        supplier.Name == name.Trim() &&
                        (!excludingId.HasValue || supplier.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> ExistsByCnpjAsync(
        Guid organizationId,
        string cnpj,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default) =>
        context.Suppliers.AnyAsync(
            supplier => supplier.OrganizationId == organizationId
                        && supplier.Cnpj == cnpj
                        && (!excludingId.HasValue || supplier.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> HasQuotesAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.ItemQuotes.AnyAsync(quote => quote.SupplierId == id, cancellationToken);

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
        await context.Suppliers.AddAsync(supplier, cancellationToken);

    public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        context.Suppliers.Update(supplier);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await GetByIdAsync(id, cancellationToken);
        if (supplier is null) return false;

        var shoppingListLinks = await context.ShoppingListSuppliers
            .Where(link => link.SupplierId == id)
            .ToListAsync(cancellationToken);
        context.ShoppingListSuppliers.RemoveRange(shoppingListLinks);
        context.Suppliers.Remove(supplier);
        return true;
    }
}
