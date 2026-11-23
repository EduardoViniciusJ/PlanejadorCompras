namespace PlanejadorCompras.Domain.Repositories.QuotationRequest;

public interface IQuotationRequestRepository
{
    Task<Entities.QuotationRequest?> GetByIdForOrganizationAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<List<Entities.QuotationRequest>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<(List<Entities.QuotationRequest> Items, int TotalCount)> GetPageByOrganizationIdAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteForOrganizationAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task AddAsync(
        Entities.QuotationRequest request,
        CancellationToken cancellationToken = default);
}
