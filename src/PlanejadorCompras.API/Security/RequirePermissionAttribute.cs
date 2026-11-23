using Microsoft.AspNetCore.Mvc.Filters;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;

namespace PlanejadorCompras.API.Security;

/// <summary>
/// Requires a permission of the role the user has in the current organization.
/// Role checks live in the API, never only in the screen: a request made by
/// hand gets the same 403.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePermissionAttribute(OrganizationPermission permission)
    : Attribute, IAsyncActionFilter
{
    public OrganizationPermission Permission { get; } = permission;

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentOrganization = context.HttpContext.RequestServices
            .GetRequiredService<ICurrentOrganization>();

        // Throws ForbiddenException, rendered as a 403 problem by the error handler.
        currentOrganization.Require(Permission);

        return next();
    }
}
