using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PlanejadorCompras.API.ErrorHandling;

namespace PlanejadorCompras.API.Configuration;

internal static class ApiRateLimitingConfiguration
{
    internal const string LoginPolicy = "auth-login";
    internal const string EmailPolicy = "auth-email";
    internal const string PublicQuotationReadPolicy = "public-quotation-read";
    internal const string PublicQuotationWritePolicy = "public-quotation-write";
    internal const string QuotationDeliveryPolicy = "quotation-delivery";
    internal const string RefreshPolicy = "auth-refresh";

    internal static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) =>
                new ValueTask(ApiProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "Muitas tentativas em pouco tempo. Aguarde um instante e tente novamente.",
                    "rate_limited",
                    cancellationToken));

            options.AddPolicy(LoginPolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, 10, TimeSpan.FromMinutes(1)));
            options.AddPolicy(EmailPolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, 5, TimeSpan.FromMinutes(10)));
            // Public supplier link: no login, so the limit per address is what holds back
            // token guessing and automated submissions.
            options.AddPolicy(PublicQuotationReadPolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, 30, TimeSpan.FromMinutes(1)));
            options.AddPolicy(PublicQuotationWritePolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, 10, TimeSpan.FromMinutes(1)));
            // Issuing requests and links can send e-mail to addresses chosen by the user.
            options.AddPolicy(QuotationDeliveryPolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, 20, TimeSpan.FromMinutes(10)));
            options.AddPolicy(RefreshPolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, 120, TimeSpan.FromMinutes(1)));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        int permitLimit,
        TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                QueueLimit = 0,
                Window = window
            });

    private static string GetClientPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString()
        ?? context.Request.Headers.UserAgent.ToString()
        ?? "unknown";
}
