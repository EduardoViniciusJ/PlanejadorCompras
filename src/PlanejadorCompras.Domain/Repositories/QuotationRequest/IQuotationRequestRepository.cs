namespace PlanejadorCompras.Domain.Repositories.QuotationRequest;

public interface IQuotationRequestRepository
{
    Task<Entities.QuotationRequest?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<Entities.QuotationRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked version, for changes to the supplier quotations of the request.</summary>
    Task<Entities.QuotationRequest?> GetTrackedByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the quotation of a single supplier by the hash of its public token. The request
    /// comes with its items only: quotations of other suppliers are never loaded.
    /// </summary>
    Task<SupplierQuotationAccess?> GetSupplierQuotationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(
        Entities.QuotationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SupplierQuotationAccess(
    Entities.QuotationRequest Request,
    Entities.SupplierQuotation Quotation);
