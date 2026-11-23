using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlanejadorCompras.API.ErrorHandling;
using PlanejadorCompras.Application.Exceptions;

namespace PlanejadorCompras.API.Extensions;

public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }

    public static WebApplication UseApiExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var (statusCode, title, errorCode) = MapException(exception);
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ApiExceptionHandler");

                if (exception is DbUpdateException && statusCode == StatusCodes.Status409Conflict)
                {
                    // Expected under concurrency (two requests inserting the same thing): the
                    // database constraint did its job. Logged without the exception so that
                    // parameter values never reach the logs.
                    logger.LogWarning(
                        "Database conflict mapped to 409 ({ErrorCode}). TraceId: {TraceId}",
                        errorCode,
                        context.TraceIdentifier);
                }
                else if (exception is ServiceUnavailableException)
                {
                    // An optional integration (Google login, e-mail) is not configured.
                    logger.LogWarning(
                        "Feature unavailable ({ErrorCode}). TraceId: {TraceId}",
                        errorCode,
                        context.TraceIdentifier);
                }
                else if (exception is not null && exception is not AppException)
                {
                    logger.LogError(
                        exception,
                        "Unhandled API exception. TraceId: {TraceId}",
                        context.TraceIdentifier);
                }

                if (exception is TooManyRequestsException { RetryAfter: { } retryAfter })
                {
                    context.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await ApiProblemDetailsWriter.WriteAsync(
                    context,
                    statusCode,
                    title,
                    errorCode,
                    context.RequestAborted,
                    (exception as AppException)?.Extensions);
            });
        });

        return app;
    }

    public static (int StatusCode, string Title, string ErrorCode) MapException(Exception? exception)
    {
        return exception switch
        {
            AppException appException => (
                (int)appException.StatusCode,
                appException.Message,
                appException.ErrorCode),
            // "Check, then insert" races: the use case saw no duplicate, but another request
            // inserted first and the unique index rejected this one. That is a conflict the
            // client can understand, not a server failure.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => (
                StatusCodes.Status409Conflict,
                "Já existe um registro com esses dados.",
                "duplicate_record"),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "O registro foi alterado por outra operação. Atualize a página e tente novamente.",
                "concurrency_conflict"),
            ArgumentNullException => (
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                "bad_request"),
            ArgumentOutOfRangeException => (
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                "bad_request"),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                "bad_request"),
            null => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "internal_server_error"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "internal_server_error")
        };
    }
}
