namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IAiUsageStore
{
    Task<int> GetUsedAsync(Guid userId, DateOnly periodStart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically takes one unit of the monthly quota. Returns false, without changing
    /// anything, when the user already reached the limit.
    /// </summary>
    Task<bool> TryReserveAsync(Guid userId, DateOnly periodStart, int limit, CancellationToken cancellationToken = default);

    /// <summary>Gives back a unit taken by TryReserveAsync.</summary>
    Task ReleaseAsync(Guid userId, DateOnly periodStart, CancellationToken cancellationToken = default);
}
