using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.Ai;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Ai;

/// <summary>
/// Stand-in for the AI provider. No automated test ever calls the real API: every test
/// goes through this client, which answers with whatever the test scripted.
/// </summary>
public sealed class FakeAiClient : IAiClient
{
    private readonly Func<AiJsonRequest, CancellationToken, Task<string>> _handler;
    private int _calls;

    public FakeAiClient(string json, bool isConfigured = true)
        : this((_, _) => Task.FromResult(json), isConfigured)
    {
    }

    public FakeAiClient(Func<AiJsonRequest, CancellationToken, Task<string>> handler, bool isConfigured = true)
    {
        _handler = handler;
        IsConfigured = isConfigured;
    }

    public bool IsConfigured { get; }

    public int Calls => _calls;

    public AiJsonRequest? LastRequest { get; private set; }

    public Task<string> CompleteJsonAsync(AiJsonRequest request, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _calls);
        LastRequest = request;
        return _handler(request, cancellationToken);
    }
}

/// <summary>In-memory counter with the same atomic contract as the SQL store.</summary>
public sealed class InMemoryAiUsageStore : IAiUsageStore
{
    private readonly object _gate = new();
    private readonly Dictionary<(Guid UserId, DateOnly Period), int> _counts = new();

    public int Releases { get; private set; }

    public int Used(Guid userId, DateOnly period)
    {
        lock (_gate)
        {
            return _counts.GetValueOrDefault((userId, period));
        }
    }

    public void Set(Guid userId, DateOnly period, int used)
    {
        lock (_gate)
        {
            _counts[(userId, period)] = used;
        }
    }

    public Task<int> GetUsedAsync(Guid userId, DateOnly periodStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(Used(userId, periodStart));

    public Task<bool> TryReserveAsync(Guid userId, DateOnly periodStart, int limit, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var used = _counts.GetValueOrDefault((userId, periodStart));
            if (used >= limit)
            {
                return Task.FromResult(false);
            }

            _counts[(userId, periodStart)] = used + 1;
            return Task.FromResult(true);
        }
    }

    public Task ReleaseAsync(Guid userId, DateOnly periodStart, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var used = _counts.GetValueOrDefault((userId, periodStart));
            if (used > 0)
            {
                _counts[(userId, periodStart)] = used - 1;
            }

            Releases++;
        }

        return Task.CompletedTask;
    }
}

public sealed class FakeCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
    public string Email => "comprador@example.com";
    public string Name => "Comprador";
}

public sealed class FakeAiDocumentReader : IAiDocumentReader
{
    public string? SpreadsheetText { get; set; }

    public int? PdfPages { get; set; }

    public bool TryReadSpreadsheetText(byte[] content, int maxCharacters, out string text)
    {
        text = SpreadsheetText ?? string.Empty;
        return SpreadsheetText is not null;
    }

    public int? TryCountPdfPages(byte[] content) => PdfPages;
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public static class AiTestSetup
{
    public static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly DateTimeOffset Now = new(2026, 11, 23, 15, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Period = new(2026, 11, 1);

    public static AiSettings Settings(int monthlyLimit = 20, bool isConfigured = true) =>
        new() { IsConfigured = isConfigured, MonthlyLimitPerUser = monthlyLimit };

    public static AiUsageService UsageService(
        IAiClient client,
        IAiUsageStore store,
        AiSettings? settings = null,
        Guid? userId = null) =>
        new(client, store, new FakeCurrentUser(userId ?? UserId), settings ?? Settings(), new FixedTimeProvider(Now));
}
