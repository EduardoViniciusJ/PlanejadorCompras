using PlanejadorCompras.Application.Common.Pagination;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

public sealed class GetQuotationRequestsUseCase(
    IQuotationRequestRepository repository,
    ICurrentOrganization currentOrganization)
{
    public async Task<PagedResponseDto<QuotationRequestSummaryResponseDto>> ExecuteAsync(
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PageRequest.Validate(page, pageSize);

        var result = await repository.GetPageByOrganizationIdAsync(
            currentOrganization.OrganizationId,
            page,
            pageSize,
            cancellationToken);

        return new PagedResponseDto<QuotationRequestSummaryResponseDto>(
            result.Items.Select(QuotationRequestResponseMapper.ToSummary).ToList(),
            page,
            pageSize,
            result.TotalCount);
    }
}
