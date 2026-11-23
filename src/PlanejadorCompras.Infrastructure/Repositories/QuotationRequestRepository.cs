using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Infrastructure.Persistence;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class QuotationRequestRepository(PlanejadorComprasDbContext context)
    : IQuotationRequestRepository
{
    public Task<QuotationRequestEntity?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.QuotationRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .Include(request => request.SupplierQuotations)
            .ThenInclude(quotation => quotation.ResponseItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(request => request.Id == id && request.UserId == userId, cancellationToken);

    public Task<QuotationRequestEntity?> GetTrackedByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.QuotationRequests
            .Include(request => request.SupplierQuotations)
            .FirstOrDefaultAsync(request => request.Id == id && request.UserId == userId, cancellationToken);

    public async Task<SupplierQuotationAccess?> GetSupplierQuotationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var quotation = await context.SupplierQuotations
            .Include(candidate => candidate.ResponseItems)
            .FirstOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (quotation is null) return null;

        // Loaded without the SupplierQuotations navigation on purpose: the public flow must
        // never have the quotation of another supplier in memory.
        var request = await context.QuotationRequests
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == quotation.QuotationRequestId, cancellationToken);

        return request is null ? null : new SupplierQuotationAccess(request, quotation);
    }

    public Task<List<QuotationRequestEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.QuotationRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .Include(request => request.SupplierQuotations)
            .AsSplitQuery()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> DeleteForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var request = await context.QuotationRequests
            .Include(request => request.Items)
            .FirstOrDefaultAsync(request => request.Id == id && request.UserId == userId, cancellationToken);
        if (request is null) return false;
        context.QuotationRequests.Remove(request);
        return true;
    }

    public async Task AddAsync(
        QuotationRequestEntity request,
        CancellationToken cancellationToken = default) =>
        await context.QuotationRequests.AddAsync(request, cancellationToken);
}
