using Microsoft.AspNetCore.Mvc;

namespace PlanejadorCompras.API.ErrorHandling;

internal static class ApiProblemDetailsWriter
{
    internal static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string errorCode,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        return context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}
