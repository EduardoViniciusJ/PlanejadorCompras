using PlanejadorCompras.Application.Features.ShoppingLists.Contracts;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.ShoppingList;

public sealed class GetShoppingListDetailUseCase
{
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IShoppingListDetailQuery _shoppingListDetailQuery;

    public GetShoppingListDetailUseCase(
        ICurrentOrganization currentOrganization,
        IShoppingListDetailQuery shoppingListDetailQuery)
    {
        _currentOrganization = currentOrganization;
        _shoppingListDetailQuery = shoppingListDetailQuery;
    }

    public async Task<ShoppingListDetailResponseDto> ExecuteAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(shoppingListId, Guid.Empty);

        var detail = await _shoppingListDetailQuery.GetByIdAsync(
            _currentOrganization.OrganizationId,
            shoppingListId,
            cancellationToken);

        return detail ?? throw new NotFoundException(
            "Shopping list not found.",
            "shopping_list_not_found");
    }
}
