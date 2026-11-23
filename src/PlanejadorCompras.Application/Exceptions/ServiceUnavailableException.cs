using System.Net;

namespace PlanejadorCompras.Application.Exceptions;

/// <summary>
/// An optional integration (Google login, e-mail delivery) is not configured or not reachable.
/// Only the feature that needs it fails; the rest of the API keeps working.
/// </summary>
public sealed class ServiceUnavailableException : AppException
{
    public ServiceUnavailableException(string message, string errorCode = "service_unavailable", Exception? innerException = null)
        : base(message, HttpStatusCode.ServiceUnavailable, errorCode, innerException)
    {
    }
}
