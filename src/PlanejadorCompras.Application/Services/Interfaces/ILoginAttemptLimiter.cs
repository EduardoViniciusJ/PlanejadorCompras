namespace PlanejadorCompras.Application.Services.Interfaces;

/// <summary>
/// Throttles password guessing per account, independently of the client IP. The per-IP limit
/// alone is not enough: an attacker can rotate addresses, and behind a proxy many people can
/// share one. Unknown e-mails are tracked exactly like existing ones, so the limiter does not
/// reveal whether an account exists.
/// </summary>
public interface ILoginAttemptLimiter
{
    /// <summary>How long the account is still locked, or null when a login may be attempted.</summary>
    TimeSpan? GetRemainingLockout(string email);

    /// <summary>Records a failed password attempt for the account.</summary>
    void RegisterFailure(string email);

    /// <summary>Forgets previous failures (successful login or password reset).</summary>
    void Reset(string email);
}
