using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PlanejadorCompras.API.Controllers;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UnitTests.Support;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Persistence;
using static PlanejadorCompras.Domain.Authorization.OrganizationPermission;

namespace PlanejadorCompras.Application.UnitTests.API.Security;

public sealed class RolePermissionTests
{
    // The matrix, written out in full so any change to it shows up in review.
    public static TheoryData<OrganizationRole, OrganizationPermission[]> Matrix => new()
    {
        {
            OrganizationRole.Admin,
            [ViewData, ManageLists, ManageSuppliers, ManageQuotes, ManageQuotations,
                ManagePurchaseOrders, ApprovePurchaseOrders, ViewTeam, ManageTeam]
        },
        {
            OrganizationRole.Buyer,
            [ViewData, ManageLists, ManageSuppliers, ManageQuotes, ManageQuotations,
                ManagePurchaseOrders, ApprovePurchaseOrders, ViewTeam]
        },
        { OrganizationRole.Requester, [ViewData, ManageLists, ViewTeam] },
        { OrganizationRole.Approver, [ViewData, ApprovePurchaseOrders, ViewTeam] }
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Role_ShouldHaveExactlyTheDocumentedPermissions(
        OrganizationRole role,
        OrganizationPermission[] expected)
    {
        Assert.Equal(expected, OrganizationRolePermissions.For(role));

        foreach (var permission in Enum.GetValues<OrganizationPermission>())
        {
            Assert.Equal(
                expected.Contains(permission),
                OrganizationRolePermissions.Allows(role, permission));
        }
    }

    [Fact]
    public void OnlyAdministrators_ShouldManageTheTeam()
    {
        var roles = Enum.GetValues<OrganizationRole>()
            .Where(role => OrganizationRolePermissions.Allows(role, ManageTeam));

        Assert.Equal([OrganizationRole.Admin], roles);
    }

    [Theory]
    [InlineData(OrganizationRole.Requester, ManageSuppliers)]
    [InlineData(OrganizationRole.Requester, ManagePurchaseOrders)]
    [InlineData(OrganizationRole.Approver, ManageLists)]
    [InlineData(OrganizationRole.Buyer, ManageTeam)]
    public async Task PermissionFilter_ShouldStopTheRequestWith403_WhenTheRoleLacksThePermission(
        OrganizationRole role,
        OrganizationPermission permission)
    {
        var executed = false;

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            RunFilterAsync(role, permission, () => executed = true));

        Assert.Equal("organization_permission_denied", exception.ErrorCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.False(executed);
    }

    [Fact]
    public async Task PermissionFilter_ShouldLetTheRequestThrough_WhenTheRoleHasThePermission()
    {
        var executed = false;

        await RunFilterAsync(OrganizationRole.Approver, ApprovePurchaseOrders, () => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void EveryDataEndpoint_ShouldDeclareAPermission()
    {
        // AuthController (session), AccountController (own account) and
        // InvitationsController (token holder) are deliberately role-independent.
        var exempt = new[]
        {
            typeof(AuthController), typeof(AccountController), typeof(InvitationsController)
        };

        var unprotected = Actions()
            .Where(action => !exempt.Contains(action.DeclaringType))
            .Where(action => action.GetCustomAttribute<AllowAnonymousAttribute>() is null)
            .Where(action => PermissionsOf(action).Count == 0)
            .Select(Describe)
            .ToList();

        Assert.Empty(unprotected);
    }

    [Fact]
    public void EveryEndpointThatChangesBusinessData_ShouldRequireMoreThanReadAccess()
    {
        var weak = Actions()
            .Where(action => action.DeclaringType != typeof(AuthController)
                             && action.DeclaringType != typeof(AccountController)
                             && action.DeclaringType != typeof(InvitationsController))
            .Where(IsMutation)
            .Where(action => PermissionsOf(action).All(permission => permission is ViewData or ViewTeam))
            .Select(Describe)
            .ToList();

        // Leaving the organization is the only write any member may do.
        Assert.Equal(["OrganizationController.Leave"], weak);
    }

    [Fact]
    public void TeamManagementEndpoints_ShouldRequireTheManageTeamPermission()
    {
        var teamWrites = typeof(OrganizationController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(IsMutation)
            .Where(action => action.Name != nameof(OrganizationController.Leave))
            .ToList();

        Assert.NotEmpty(teamWrites);
        Assert.All(teamWrites, action => Assert.Contains(ManageTeam, PermissionsOf(action)));
        Assert.Contains(
            ManageTeam,
            PermissionsOf(typeof(OrganizationController).GetMethod(nameof(OrganizationController.GetInvitations))!));
    }

    [Theory]
    [InlineData(typeof(SuppliersController), nameof(SuppliersController.Create), ManageSuppliers)]
    [InlineData(typeof(SuppliersController), nameof(SuppliersController.Delete), ManageSuppliers)]
    [InlineData(typeof(ShoppingListsController), nameof(ShoppingListsController.Delete), ManageLists)]
    [InlineData(typeof(ShoppingListsController), nameof(ShoppingListsController.AddSupplier), ManageQuotes)]
    [InlineData(typeof(ShoppingItemsController), nameof(ShoppingItemsController.DeleteBatch), ManageLists)]
    [InlineData(typeof(ItemQuotesController), nameof(ItemQuotesController.Update), ManageQuotes)]
    [InlineData(typeof(QuotationRequestsController), nameof(QuotationRequestsController.Create), ManageQuotations)]
    [InlineData(typeof(EqualizationsController), nameof(EqualizationsController.Create), ManageQuotations)]
    [InlineData(typeof(PurchaseOrdersController), nameof(PurchaseOrdersController.Create), ManagePurchaseOrders)]
    [InlineData(typeof(PurchaseOrdersController), nameof(PurchaseOrdersController.UpdateStatus), ApprovePurchaseOrders)]
    public void WriteEndpoint_ShouldRequireItsSpecificPermission(
        Type controller,
        string action,
        OrganizationPermission expected)
    {
        Assert.Contains(expected, PermissionsOf(controller.GetMethod(action)!));
    }

    [Fact]
    public void EveryOrganizationOwnedEntity_ShouldHaveATenantFilterInTheModel()
    {
        var world = new TeamWorld();
        var model = world.Context.Model;

        var ownedTypes = typeof(IOrganizationOwned).Assembly.GetTypes()
            .Where(type => type.IsClass && typeof(IOrganizationOwned).IsAssignableFrom(type))
            .ToList();

        Assert.Equal(8, ownedTypes.Count);
        Assert.All(ownedTypes, type =>
        {
            var entityType = model.FindEntityType(type);
            Assert.NotNull(entityType);
            Assert.NotNull(entityType.GetQueryFilter());
        });
        Assert.IsType<PlanejadorComprasDbContext>(world.Context);
    }

    private static async Task RunFilterAsync(
        OrganizationRole role,
        OrganizationPermission permission,
        Action onExecuted)
    {
        var services = new ServiceCollection()
            .AddSingleton(TestOrganization.For(Guid.NewGuid(), role))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());

        await new RequirePermissionAttribute(permission).OnActionExecutionAsync(
            executing,
            () =>
            {
                onExecuted();
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });
    }

    private static IEnumerable<MethodInfo> Actions() =>
        typeof(AuthController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static bool IsMutation(MethodInfo action) =>
        action.GetCustomAttributes<HttpMethodAttribute>()
            .SelectMany(attribute => attribute.HttpMethods)
            .Any(method => method is "POST" or "PUT" or "PATCH" or "DELETE");

    private static List<OrganizationPermission> PermissionsOf(MethodInfo action) =>
        action.GetCustomAttributes<RequirePermissionAttribute>()
            .Concat(action.DeclaringType!.GetCustomAttributes<RequirePermissionAttribute>())
            .Select(attribute => attribute.Permission)
            .ToList();

    private static string Describe(MethodInfo action) => $"{action.DeclaringType!.Name}.{action.Name}";
}
