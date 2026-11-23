using System.Net;

namespace PlanejadorCompras.Application.Exceptions;

public sealed class TooManyRequestsException : AppException
{
    public TooManyRequestsException(string message, string errorCode = "rate_limited", TimeSpan? retryAfter = null)
        : base(message, HttpStatusCode.TooManyRequests, errorCode)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>When known, how long the client should wait (sent as the Retry-After header).</summary>
    public TimeSpan? RetryAfter { get; }
}
