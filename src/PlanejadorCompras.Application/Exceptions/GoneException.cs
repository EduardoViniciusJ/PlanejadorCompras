using System.Net;

namespace PlanejadorCompras.Application.Exceptions;

public sealed class GoneException : AppException
{
    public GoneException(string message, string errorCode = "gone", Exception? innerException = null)
        : base(message, HttpStatusCode.Gone, errorCode, innerException)
    {
    }
}
