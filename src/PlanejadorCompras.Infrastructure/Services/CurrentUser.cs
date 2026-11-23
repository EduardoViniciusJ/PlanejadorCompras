using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Infrastructure.Organizations;

namespace PlanejadorCompras.Infrastructure.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OrganizationContext? _organizationContext;

    public CurrentUser(
        IHttpContextAccessor httpContextAccessor,
        OrganizationContext? organizationContext = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _organizationContext = organizationContext;
    }

    public Guid UserId
    {
        get
        {
            var user = GetAuthenticatedUser();
            var subClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(subClaim, out var userId) || userId == Guid.Empty)
            {
                throw new UnauthorizedException("Authenticated user id is invalid.");
            }

            return userId;
        }
    }

    public string Email
    {
        get
        {
            var user = GetAuthenticatedUser();
            return user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;
        }
    }

    public string Name
    {
        get
        {
            var user = GetAuthenticatedUser();

            // The token keeps whatever name the user had at sign-in; the profile
            // name saved afterwards is the one that must show up in documents.
            if (!string.IsNullOrWhiteSpace(_organizationContext?.UserName))
            {
                return _organizationContext.UserName;
            }

            return user.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? string.Empty;
        }
    }

    private ClaimsPrincipal GetAuthenticatedUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        return user;
    }
}
