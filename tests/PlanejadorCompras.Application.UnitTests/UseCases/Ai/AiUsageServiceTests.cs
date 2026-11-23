using System.Net;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Ai;

public sealed class AiUsageServiceTests
{
    private static readonly AiJsonRequest Request = new("system", "user", null, "{}");

    [Fact]
    public async Task CompleteJsonAsync_WhenAiIsNotConfigured_Returns503WithoutCallingProviderOrCounting()
    {
        var client = new FakeAiClient("{}", isConfigured: false);
        var store = new InMemoryAiUsageStore();
        var service = AiTestSetup.UsageService(client, store, AiTestSetup.Settings(isConfigured: false));

        var exception = await Assert.ThrowsAsync<AiNotConfiguredException>(() => service.CompleteJsonAsync(Request));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal("ai_not_configured", exception.ErrorCode);
        Assert.Contains("IA não configurada", exception.Message);
        Assert.Equal(0, client.Calls);
        Assert.Equal(0, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
    }

    [Fact]
    public async Task GetStatusAsync_WhenAiIsNotConfigured_ReportsDisabledInsteadOfFailing()
    {
        var service = AiTestSetup.UsageService(
            new FakeAiClient("{}", isConfigured: false),
            new InMemoryAiUsageStore(),
            AiTestSetup.Settings(isConfigured: false));

        var status = await service.GetStatusAsync();

        Assert.False(status.Enabled);
        Assert.Equal(20, status.Limit);
    }

    [Fact]
    public async Task CompleteJsonAsync_WhenProviderSucceeds_CountsOneUse()
    {
        var store = new InMemoryAiUsageStore();
        var service = AiTestSetup.UsageService(new FakeAiClient("{\"ok\":true}"), store);

        var json = await service.CompleteJsonAsync(Request);
        var status = await service.GetStatusAsync();

        Assert.Equal("{\"ok\":true}", json);
        Assert.True(status.Enabled);
        Assert.Equal(1, status.Used);
        Assert.Equal(19, status.Remaining);
        Assert.Equal(new DateOnly(2026, 12, 1), status.RenewsOn);
    }

    [Fact]
    public async Task CompleteJsonAsync_WhenLimitIsReached_Returns429WithRenewalDateAndDoesNotCallProvider()
    {
        var client = new FakeAiClient("{}");
        var store = new InMemoryAiUsageStore();
        store.Set(AiTestSetup.UserId, AiTestSetup.Period, 20);
        var service = AiTestSetup.UsageService(client, store);

        var exception = await Assert.ThrowsAsync<AiUsageLimitExceededException>(() => service.CompleteJsonAsync(Request));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal("ai_usage_limit_exceeded", exception.ErrorCode);
        Assert.Equal(new DateOnly(2026, 12, 1), exception.RenewsOn);
        Assert.Contains("01/12/2026", exception.Message);
        Assert.Equal("2026-12-01", exception.Extensions["renewsOn"]);
        Assert.Equal(0, client.Calls);
        Assert.Equal(20, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
    }

    [Fact]
    public async Task CompleteJsonAsync_WhenLimitIsZero_BlocksEveryone()
    {
        var client = new FakeAiClient("{}");
        var service = AiTestSetup.UsageService(client, new InMemoryAiUsageStore(), AiTestSetup.Settings(monthlyLimit: 0));

        await Assert.ThrowsAsync<AiUsageLimitExceededException>(() => service.CompleteJsonAsync(Request));
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task CompleteJsonAsync_WhenProviderFails_DoesNotCountTheUse()
    {
        var client = new FakeAiClient((_, _) => throw AiProviderException.Unavailable());
        var store = new InMemoryAiUsageStore();
        var service = AiTestSetup.UsageService(client, store);

        var exception = await Assert.ThrowsAsync<AiProviderException>(() => service.CompleteJsonAsync(Request));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(1, client.Calls);
        Assert.Equal(0, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
        Assert.Equal(1, store.Releases);
    }

    [Fact]
    public async Task CompleteJsonAsync_WhenRequestIsCancelled_DoesNotCountTheUse()
    {
        using var cancellation = new CancellationTokenSource();
        var client = new FakeAiClient((_, token) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult("{}");
        });
        var store = new InMemoryAiUsageStore();
        var service = AiTestSetup.UsageService(client, store);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CompleteJsonAsync(Request, cancellation.Token));

        Assert.Equal(0, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
    }

    [Fact]
    public async Task CompleteJsonAsync_WhenProviderAnsweredButAnswerIsUnusable_CountsTheUse()
    {
        // The call was billed, so it counts; otherwise unusable answers would be free to repeat.
        var client = new FakeAiClient((_, _) => throw AiUnusableResponseException.TooLong());
        var store = new InMemoryAiUsageStore();
        var service = AiTestSetup.UsageService(client, store);

        await Assert.ThrowsAsync<AiUnusableResponseException>(() => service.CompleteJsonAsync(Request));

        Assert.Equal(1, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
        Assert.Equal(0, store.Releases);
    }

    [Fact]
    public async Task CompleteJsonAsync_WithConcurrentRequests_NeverPassesTheLimit()
    {
        const int limit = 20;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new FakeAiClient(async (_, _) =>
        {
            // Every admitted call waits here, so all of them hold a reservation at once.
            await release.Task;
            return "{}";
        });
        var store = new InMemoryAiUsageStore();
        var service = AiTestSetup.UsageService(client, store, AiTestSetup.Settings(limit));

        var attempts = Enumerable.Range(0, 60)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await service.CompleteJsonAsync(Request);
                    return true;
                }
                catch (AiUsageLimitExceededException)
                {
                    return false;
                }
            }))
            .ToArray();

        while (client.Calls < limit && attempts.Count(attempt => attempt.IsCompleted) < 60 - limit)
        {
            await Task.Delay(5);
        }

        release.SetResult();
        var results = await Task.WhenAll(attempts);

        Assert.Equal(limit, results.Count(succeeded => succeeded));
        Assert.Equal(limit, client.Calls);
        Assert.Equal(limit, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
    }

    [Fact]
    public async Task CompleteJsonAsync_CountsEachUserSeparately()
    {
        var store = new InMemoryAiUsageStore();
        var otherUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
        store.Set(otherUser, AiTestSetup.Period, 20);
        var service = AiTestSetup.UsageService(new FakeAiClient("{}"), store);

        await service.CompleteJsonAsync(Request);

        Assert.Equal(1, store.Used(AiTestSetup.UserId, AiTestSetup.Period));
        Assert.Equal(20, store.Used(otherUser, AiTestSetup.Period));
    }
}
