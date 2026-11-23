namespace PlanejadorCompras.Domain.Repositories.ItemQuote;

public interface IItemQuoteRepository
{
    Task<Entities.ItemQuote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Entities.ItemQuote>> GetByShoppingItemIdAsync(Guid shoppingItemId, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.ItemQuote itemQuote, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.ItemQuote itemQuote, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Entities.ItemQuote>> GetByShoppingListIdAsync(Guid shoppingListId, CancellationToken cancellationToken = default);

    /// <summary>Tracked quotes of one supplier for the items of a list, even when the supplier is not linked to it.</summary>
    Task<List<Entities.ItemQuote>> GetBySupplierInShoppingListAsync(Guid shoppingListId, Guid supplierId, CancellationToken cancellationToken = default);

    void Remove(Entities.ItemQuote itemQuote);
}
