using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Services.Ai;

/// <summary>
/// Monthly AI counters. Reserving is a single statement, so PostgreSQL serializes
/// concurrent requests on the row and the count can never pass the limit.
/// </summary>
public sealed class AiUsageStore(PlanejadorComprasDbContext context) : IAiUsageStore
{
    public async Task<int> GetUsedAsync(
        Guid userId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        await context.AiUsages
            .AsNoTracking()
            .Where(usage => usage.UserId == userId && usage.PeriodStart == periodStart)
            .Select(usage => usage.UsedCount)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryReserveAsync(
        Guid userId,
        DateOnly periodStart,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return false;
        }

        var id = Guid.NewGuid();

        // The conflicting row is locked and its latest count is checked against the limit;
        // when the limit was reached no row is updated and nothing is returned.
        var counts = await context.Database
            .SqlQuery<int>($"""
                INSERT INTO "AiUsages" ("Id", "UserId", "PeriodStart", "UsedCount", "UpdatedAt")
                VALUES ({id}, {userId}, {periodStart}, 1, now())
                ON CONFLICT ("UserId", "PeriodStart") DO UPDATE
                    SET "UsedCount" = "AiUsages"."UsedCount" + 1, "UpdatedAt" = now()
                    WHERE "AiUsages"."UsedCount" < {limit}
                RETURNING "UsedCount" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return counts.Count == 1;
    }

    public async Task ReleaseAsync(
        Guid userId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE "AiUsages"
            SET "UsedCount" = "UsedCount" - 1, "UpdatedAt" = now()
            WHERE "UserId" = {userId} AND "PeriodStart" = {periodStart} AND "UsedCount" > 0
            """,
            cancellationToken);
}
