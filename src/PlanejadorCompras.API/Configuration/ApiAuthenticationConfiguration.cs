using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PlanejadorCompras.API.ErrorHandling;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Domain.Repositories.RefreshToken;

namespace PlanejadorCompras.API.Configuration;

internal static class ApiAuthenticationConfiguration
{
    internal static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration.GetRequiredValue("Authentication:Jwt:Issuer");
        var audience = configuration.GetRequiredValue("Authentication:Jwt:Audience");
        var secretKey = configuration.GetRequiredSecret(
            "Authentication:Jwt:SecretKey",
            minimumByteLength: 32);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey))
                };

                options.Events = CreateJwtEvents();
            });

        services.AddAuthorization();
        return services;
    }

    private static JwtBearerEvents CreateJwtEvents() => new()
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies[
                AuthenticationConstants.AccessTokenCookieName];
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // A valid signature is not enough: the session the token belongs to must still be
            // alive. This is what makes logout, password reset and refresh-token reuse detection
            // take effect immediately instead of when the access token expires.
            var sessionClaim = context.Principal?.FindFirst(
                AuthenticationConstants.SessionIdClaimName)?.Value;

            if (!Guid.TryParse(sessionClaim, out var sessionId))
            {
                context.Fail("The access token is not bound to a session.");
                return;
            }

            var refreshTokens = context.HttpContext.RequestServices
                .GetRequiredService<IRefreshTokenRepository>();
            var now = context.HttpContext.RequestServices
                .GetRequiredService<TimeProvider>()
                .GetUtcNow()
                .UtcDateTime;

            if (!await refreshTokens.IsSessionActiveAsync(
                    sessionId,
                    now,
                    context.HttpContext.RequestAborted))
            {
                context.Fail("The session was revoked.");
            }
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();

            if (!context.Response.HasStarted)
            {
                await ApiProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication is required.",
                    "authentication_required",
                    context.HttpContext.RequestAborted);
            }
        },
        OnForbidden = context => ApiProblemDetailsWriter.WriteAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Access is forbidden.",
            "access_forbidden",
            context.HttpContext.RequestAborted)
    };

}
