namespace PlanejadorCompras.Domain.Entities;

/// <summary>
/// Monthly counter of AI calls made by one user. One row per user and month;
/// the counter is only changed through atomic SQL in the usage store.
/// </summary>
public sealed class AiUsage
{
    private AiUsage(Guid id, Guid userId, DateOnly periodStart, int usedCount, DateTime updatedAt)
    {
        Id = id;
        UserId = userId;
        PeriodStart = periodStart;
        UsedCount = usedCount;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>First day (UTC) of the month this counter belongs to.</summary>
    public DateOnly PeriodStart { get; private set; }

    public int UsedCount { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
