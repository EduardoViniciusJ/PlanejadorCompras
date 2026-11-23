namespace PlanejadorCompras.Domain.Repositories.Supplier;

public interface ISupplierRepository
{
    Task<Entities.Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Entities.Supplier>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<Entities.Supplier>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCnpjAsync(Guid organizationId, string cnpj, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> HasQuotesAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Supplier supplier, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
