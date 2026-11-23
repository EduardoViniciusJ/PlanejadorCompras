using PlanejadorCompras.Application.Features.Account.Contracts;

namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IAccountDataExportQuery
{
    /// <summary>
    /// Collects the personal data of the user, the business data of the
    /// organizations where the user is the only member and the documents the
    /// user created in shared organizations.
    /// </summary>
    Task<AccountDataExportDto?> GetAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
