using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Domain.Repositories.Equalization;

public interface ISavedEqualizationRepository
{
    Task<SavedEqualization?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SavedEqualization?> GetByRequestIdAsync(
        Guid organizationId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<(List<SavedEqualization> Items, int TotalCount)> SearchByOrganizationIdAsync(
        Guid organizationId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        SavedEqualization equalization,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
