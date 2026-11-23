using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Persistence;
using PlanejadorCompras.Infrastructure.Services.Ai;

namespace PlanejadorCompras.Application.UnitTests.Infrastructure.Ai;

/// <summary>
/// Runs only when AI_TEST_POSTGRES holds the connection string of a disposable PostgreSQL
/// database (never the shared development one). The test applies the migrations to it.
/// Example: docker run --rm -d -p 5544:5432 -e POSTGRES_PASSWORD=test postgres:17-alpine
/// AI_TEST_POSTGRES="Host=localhost;Port=5544;Database=postgres;Username=postgres;Password=test"
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "AI_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"Set {EnvironmentVariable} to a disposable PostgreSQL connection string to run this test.";
        }
    }
}

public sealed class AiUsageStorePostgresTests
{
    private static readonly DateOnly Period = new(2026, 11, 1);

    private static PlanejadorComprasDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PlanejadorComprasDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable))
            .Options);

    private static async Task<Guid> CreateUserAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var user = User.CreateManualUser($"ai-usage-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    [PostgresFact]
    public async Task TryReserveAsync_WithConcurrentRequests_NeverPassesTheLimit()
    {
        const int limit = 20;
        var userId = await CreateUserAsync();
        using var start = new ManualResetEventSlim(false);

        // Each attempt uses its own connection, like concurrent HTTP requests do.
        var attempts = Enumerable.Range(0, 60)
            .Select(_ => Task.Run(async () =>
            {
                await using var context = CreateContext();
                start.Wait();
                return await new AiUsageStore(context).TryReserveAsync(userId, Period, limit);
            }))
            .ToArray();

        start.Set();
        var results = await Task.WhenAll(attempts);

        await using var verification = CreateContext();
        var store = new AiUsageStore(verification);
        Assert.Equal(limit, results.Count(reserved => reserved));
        Assert.Equal(limit, await store.GetUsedAsync(userId, Period));
        Assert.Equal(1, await verification.AiUsages.CountAsync(usage => usage.UserId == userId));
    }

    [PostgresFact]
    public async Task ReserveAndRelease_KeepTheCounterConsistent()
    {
        var userId = await CreateUserAsync();
        await using var context = CreateContext();
        var store = new AiUsageStore(context);

        Assert.Equal(0, await store.GetUsedAsync(userId, Period));
        Assert.False(await store.TryReserveAsync(userId, Period, 0));
        Assert.True(await store.TryReserveAsync(userId, Period, 2));
        Assert.True(await store.TryReserveAsync(userId, Period, 2));
        Assert.False(await store.TryReserveAsync(userId, Period, 2));
        Assert.Equal(2, await store.GetUsedAsync(userId, Period));

        await store.ReleaseAsync(userId, Period);
        Assert.Equal(1, await store.GetUsedAsync(userId, Period));
        Assert.True(await store.TryReserveAsync(userId, Period, 2));

        // Releasing never goes below zero, and each month has its own counter.
        var nextPeriod = Period.AddMonths(1);
        await store.ReleaseAsync(userId, nextPeriod);
        Assert.Equal(0, await store.GetUsedAsync(userId, nextPeriod));
        Assert.True(await store.TryReserveAsync(userId, nextPeriod, 2));
        Assert.Equal(2, await store.GetUsedAsync(userId, Period));
    }
}
