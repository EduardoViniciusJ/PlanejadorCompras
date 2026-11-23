using PlanejadorCompras.Application.Features.PurchaseOrders.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.PurchaseOrder;

namespace PlanejadorCompras.Application.UseCases.PurchaseOrder;

public sealed class GetPurchaseOrdersUseCase(
    IPurchaseOrderRepository purchaseOrderRepository,
    ICurrentOrganization currentOrganization)
{
    public async Task<IReadOnlyCollection<PurchaseOrderSummaryResponseDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await purchaseOrderRepository.GetByOrganizationIdAsync(
            currentOrganization.OrganizationId,
            cancellationToken);

        return orders
            .Select(PurchaseOrderResponseMapper.ToSummary)
            .ToList();
    }
}
