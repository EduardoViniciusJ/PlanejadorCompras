namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IAccountEraser
{
    /// <summary>
    /// Atomically erases an account: deletes the given organizations with all
    /// their data, removes the user from every other organization (anonymizing
    /// the personal details stored in documents that stay there) and deletes
    /// the user, or keeps an anonymous tombstone when documents still point to it.
    /// </summary>
    Task EraseAsync(
        Guid userId,
        IReadOnlyCollection<Guid> organizationIdsToDelete,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
