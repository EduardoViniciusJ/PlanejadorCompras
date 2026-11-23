using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.ShoppingList;
using ShoppingListEntity = PlanejadorCompras.Domain.Entities.ShoppingList;

namespace PlanejadorCompras.Infrastructure.Services;

public sealed class ShoppingListAccessService : IShoppingListAccessService
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ICurrentOrganization _currentOrganization;

    public ShoppingListAccessService(
        IShoppingListRepository shoppingListRepository,
        ICurrentOrganization currentOrganization)
    {
        _shoppingListRepository = shoppingListRepository;
        _currentOrganization = currentOrganization;
    }

    public async Task<ShoppingListEntity> GetForCurrentUserAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(shoppingListId, Guid.Empty);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null || shoppingList.OrganizationId != _currentOrganization.OrganizationId)
        {
            throw new NotFoundException("Shopping list not found.", "shopping_list_not_found");
        }

        return shoppingList;
    }
}
