using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Infrastructure.Persistence;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class QuotationRequestRepository(PlanejadorComprasDbContext context)
    : IQuotationRequestRepository
{
    public Task<QuotationRequestEntity?> GetByIdForOrganizationAsync(
        Guid id,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        context.QuotationRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .FirstOrDefaultAsync(request => request.Id == id && request.OrganizationId == organizationId, cancellationToken);

    public Task<List<QuotationRequestEntity>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        context.QuotationRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .Where(request => request.OrganizationId == organizationId)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<(List<QuotationRequestEntity> Items, int TotalCount)> GetPageByOrganizationIdAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.QuotationRequests
            .AsNoTracking()
            .Where(request => request.OrganizationId == organizationId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(request => request.Items)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ThenBy(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<bool> DeleteForOrganizationAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var request = await context.QuotationRequests
            .Include(request => request.Items)
            .FirstOrDefaultAsync(request => request.Id == id && request.OrganizationId == organizationId, cancellationToken);
        if (request is null) return false;
        context.QuotationRequests.Remove(request);
        return true;
    }

    public async Task AddAsync(
        QuotationRequestEntity request,
        CancellationToken cancellationToken = default) =>
        await context.QuotationRequests.AddAsync(request, cancellationToken);
}
