using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

public sealed class GetQuotationRequestsUseCase(
    IQuotationRequestRepository repository,
    ICurrentOrganization currentOrganization)
{
    public async Task<IReadOnlyCollection<QuotationRequestSummaryResponseDto>> ExecuteAsync(
        CancellationToken cancellationToken = default) =>
        (await repository.GetByOrganizationIdAsync(currentOrganization.OrganizationId, cancellationToken))
            .Select(QuotationRequestResponseMapper.ToSummary)
            .ToList();
}
