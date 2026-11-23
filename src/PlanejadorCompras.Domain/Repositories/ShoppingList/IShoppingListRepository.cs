namespace PlanejadorCompras.Domain.Repositories.ShoppingList;

public interface IShoppingListRepository
{
    Task<Entities.ShoppingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Entities.ShoppingList>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.ShoppingList shoppingList, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.ShoppingList shoppingList, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
