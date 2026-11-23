namespace PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;

public interface IShoppingListSupplierRepository
{
    Task<List<Entities.Supplier>> GetSuppliersAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked links of a list, with the commercial terms of each supplier.</summary>
    Task<List<Entities.ShoppingListSupplier>> GetLinksAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked link between a list and a supplier.</summary>
    Task<Entities.ShoppingListSupplier?> GetLinkAsync(
        Guid shoppingListId,
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid shoppingListId,
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Entities.ShoppingListSupplier shoppingListSupplier,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid shoppingListId,
        Guid supplierId,
        CancellationToken cancellationToken = default);
}
