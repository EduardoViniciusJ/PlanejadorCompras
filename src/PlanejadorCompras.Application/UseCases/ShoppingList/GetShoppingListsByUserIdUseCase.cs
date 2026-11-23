using PlanejadorCompras.Application.Features.ShoppingLists.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.ShoppingList;

public sealed class GetShoppingListsByUserIdUseCase
{
    private readonly IShoppingListOverviewQuery _shoppingListOverviewQuery;
    private readonly ICurrentOrganization _currentOrganization;

    public GetShoppingListsByUserIdUseCase(
        IShoppingListOverviewQuery shoppingListOverviewQuery,
        ICurrentOrganization currentOrganization)
    {
        _shoppingListOverviewQuery = shoppingListOverviewQuery;
        _currentOrganization = currentOrganization;
    }

    public async Task<ShoppingListsOverviewResponseDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var lists = await _shoppingListOverviewQuery.GetByOrganizationIdAsync(
            _currentOrganization.OrganizationId,
            cancellationToken);

        var summary = new ShoppingListsSummaryDto(
            lists.Count,
            lists.Count(list => list.ItemCount == 0),
            lists.Count(list => list.ItemCount > 0 && list.QuotedItemCount < list.ItemCount),
            lists.Count(list => list.ItemCount > 0 && list.QuotedItemCount == list.ItemCount),
            lists.Sum(list => list.EstimatedTotal));

        return new ShoppingListsOverviewResponseDto(summary, lists);
    }
}
