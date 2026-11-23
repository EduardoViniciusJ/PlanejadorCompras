using Microsoft.AspNetCore.HttpOverrides;
using PlanejadorCompras.API.Extensions;

namespace PlanejadorCompras.API.Configuration;

internal static class ApiApplicationPipeline
{
    internal static WebApplication UseApiPipeline(this WebApplication app)
    {
        WarnWhenNoProxyIsTrusted(app);
        app.UseForwardedHeaders();
        app.UseApiExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseApiSecurityHeaders();
        app.UseCors(ApiCorsConfiguration.FrontendPolicy);
        app.UseRateLimiter();
        app.UseCookieRequestProtection();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Behind a reverse proxy that is not listed as trusted, X-Forwarded-For is ignored and every
    /// request appears to come from the proxy: all clients then share one per-IP rate-limit bucket.
    /// It cannot be detected from here whether a proxy exists, so this is a warning, not an error.
    /// </summary>
    private static void WarnWhenNoProxyIsTrusted(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        var hasTrustedProxy =
            app.Configuration.GetSection("Proxy:KnownProxies").GetChildren().Any()
            || app.Configuration.GetSection("Proxy:KnownNetworks").GetChildren().Any();

        if (!hasTrustedProxy)
        {
            app.Logger.LogWarning(
                "No trusted proxy configured (Proxy:KnownProxies / Proxy:KnownNetworks). If the API runs "
                + "behind a reverse proxy, per-IP rate limits will treat all clients as a single address.");
        }
    }
}
