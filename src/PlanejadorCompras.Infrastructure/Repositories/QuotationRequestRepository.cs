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
