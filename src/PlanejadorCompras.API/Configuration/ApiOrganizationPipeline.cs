using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PlanejadorCompras.API.ErrorHandling;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Infrastructure.Organizations;

namespace PlanejadorCompras.API.Configuration;

internal static class ApiOrganizationPipeline
{
    /// <summary>
    /// Resolves the organization of the signed-in user before any controller
    /// runs. Without it the data layer shows nothing, so a route that skips
    /// this step fails closed instead of leaking another organization's data.
    /// </summary>
    internal static IApplicationBuilder UseOrganizationContext(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated != true
                || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                await next();
                return;
            }

            var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var resolver = context.RequestServices.GetRequiredService<IOrganizationContextResolver>();

            if (!Guid.TryParse(subject, out var userId)
                || userId == Guid.Empty
                || !await resolver.ResolveAsync(userId, context.RequestAborted))
            {
                // The token is still valid but the account is gone (deleted):
                // drop the cookie so the browser stops presenting it.
                context.RequestServices
                    .GetRequiredService<IAuthCookieService>()
                    .DeleteAccessToken(context);

                await ApiProblemDetailsWriter.WriteAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Authentication is required.",
                    "authentication_required",
                    context.RequestAborted);
                return;
            }

            await next();
        });
}
