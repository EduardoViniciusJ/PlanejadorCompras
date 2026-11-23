using System.Collections.Concurrent;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Services;

/// <summary>
/// Progressive per-account lockout kept in memory.
/// The first failures are free (typos). From the fifth consecutive failure on, the account is
/// locked for 1 minute, then 2, 4, 8, up to 15 minutes. Failures are forgotten after a
/// successful login, a password reset or 15 minutes without new failures (counted from the
/// end of the last lock).
///
/// The lock is short on purpose: it makes guessing impractical without letting someone keep a
/// victim locked out for long, and "forgot my password" always gets the owner back in.
/// State is per process: with several API instances each one counts on its own.
/// </summary>
public sealed class InMemoryLoginAttemptLimiter : ILoginAttemptLimiter
{
    internal const int FreeFailures = 4;
    internal static readonly TimeSpan BaseLockout = TimeSpan.FromMinutes(1);
    internal static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan ForgetAfter = TimeSpan.FromMinutes(15);

    /// <summary>Upper bound of tracked accounts, so a flood of made-up e-mails cannot exhaust memory.</summary>
    internal const int MaxTrackedAccounts = 100_000;
    private static readonly TimeSpan PruneInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Attempts> _attempts = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private long _nextPruneTicks;

    public InMemoryLoginAttemptLimiter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public TimeSpan? GetRemainingLockout(string email)
    {
        if (!_attempts.TryGetValue(Normalize(email), out var attempts))
        {
            return null;
        }

        var remaining = attempts.LockedUntil - _timeProvider.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public void RegisterFailure(string email)
    {
        var now = _timeProvider.GetUtcNow();
        var key = Normalize(email);

        PruneIfDue(now);

        if (_attempts.Count >= MaxTrackedAccounts && !_attempts.ContainsKey(key))
        {
            // Full even after pruning: stop tracking new accounts instead of growing without
            // bound. The per-IP limit still applies to them.
            return;
        }

        _attempts.AddOrUpdate(
            key,
            _ => Attempts.First(now),
            (_, current) => current.IsForgotten(now) ? Attempts.First(now) : current.Next(now));
    }

    public void Reset(string email)
    {
        _attempts.TryRemove(Normalize(email), out _);
    }

    private static string Normalize(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private void PruneIfDue(DateTimeOffset now)
    {
        var due = Interlocked.Read(ref _nextPruneTicks);

        if (now.UtcTicks < due
            || Interlocked.CompareExchange(ref _nextPruneTicks, now.Add(PruneInterval).UtcTicks, due) != due)
        {
            return;
        }

        foreach (var (key, attempts) in _attempts)
        {
            if (attempts.IsForgotten(now))
            {
                // Remove only if it was not updated in the meantime.
                _attempts.TryRemove(new KeyValuePair<string, Attempts>(key, attempts));
            }
        }
    }

    private sealed record Attempts(int Failures, DateTimeOffset LastFailureAt, DateTimeOffset LockedUntil)
    {
        public static Attempts First(DateTimeOffset now) => Create(1, now);

        public Attempts Next(DateTimeOffset now) => Create(Failures + 1, now);

        // The quiet period counts from the end of the lock, so waiting out a lock is not enough
        // to start over from zero.
        public bool IsForgotten(DateTimeOffset now) =>
            now - (LockedUntil > LastFailureAt ? LockedUntil : LastFailureAt) >= ForgetAfter;

        private static Attempts Create(int failures, DateTimeOffset now)
        {
            if (failures <= FreeFailures)
            {
                return new Attempts(failures, now, DateTimeOffset.MinValue);
            }

            // 1, 2, 4, 8, 15, 15... minutes. The exponent is capped before shifting to avoid overflow.
            var doublings = Math.Min(failures - FreeFailures - 1, 10);
            var lockout = TimeSpan.FromTicks(Math.Min(BaseLockout.Ticks << doublings, MaxLockout.Ticks));

            return new Attempts(failures, now, now.Add(lockout));
        }
    }
}
