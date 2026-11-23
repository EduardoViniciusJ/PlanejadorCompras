using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

public sealed class GetQuotationRequestsUseCase(
    IQuotationRequestRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<QuotationRequestSummaryResponseDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        return (await repository.GetByUserIdAsync(currentUser.UserId, cancellationToken))
            .Select(request => QuotationRequestResponseMapper.ToSummary(request, utcNow))
            .ToList();
    }
}
