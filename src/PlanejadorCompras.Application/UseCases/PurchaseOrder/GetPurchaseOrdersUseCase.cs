using PlanejadorCompras.Application.Common.Pagination;
using PlanejadorCompras.Application.Features.PurchaseOrders.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.PurchaseOrder;

namespace PlanejadorCompras.Application.UseCases.PurchaseOrder;

public sealed class GetPurchaseOrdersUseCase(
    IPurchaseOrderRepository purchaseOrderRepository,
    ICurrentOrganization currentOrganization)
{
    public async Task<PagedResponseDto<PurchaseOrderSummaryResponseDto>> ExecuteAsync(
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PageRequest.Validate(page, pageSize);

        var result = await purchaseOrderRepository.GetPageByOrganizationIdAsync(
            currentOrganization.OrganizationId,
            page,
            pageSize,
            cancellationToken);

        return new PagedResponseDto<PurchaseOrderSummaryResponseDto>(
            result.Items.Select(PurchaseOrderResponseMapper.ToSummary).ToList(),
            page,
            pageSize,
            result.TotalCount);
    }
}
