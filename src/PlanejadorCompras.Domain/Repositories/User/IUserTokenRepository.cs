namespace PlanejadorCompras.Domain.Repositories.User;

public interface IUserTokenRepository
{
    Task AddAsync(Entities.UserToken userToken, CancellationToken cancellationToken = default);
    /// <summary>Finds a token by the SHA-256 hash of the raw value the user received.</summary>
    Task<Entities.UserToken?> GetByTokenHashAsync(string tokenHash, string type, CancellationToken cancellationToken = default);
    Task RemoveByUserAndTypeAsync(Guid userId, string type, CancellationToken cancellationToken = default);
    Task RemoveAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Remove(Entities.UserToken userToken);
}
