using PlanejadorCompras.Application.Features.ShoppingLists.Contracts;

namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IShoppingListOverviewQuery
{
    Task<IReadOnlyList<ShoppingListOverviewDto>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
