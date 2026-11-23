using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Domain.Authorization;

public enum OrganizationPermission
{
    /// <summary>Read every list, supplier, price and document of the organization.</summary>
    ViewData = 1,

    /// <summary>Create, edit and delete shopping lists and their items.</summary>
    ManageLists = 2,

    /// <summary>Create, edit and delete suppliers.</summary>
    ManageSuppliers = 3,

    /// <summary>Link suppliers to a list and enter or change prices in the price map.</summary>
    ManageQuotes = 4,

    /// <summary>Issue and delete quotation requests and saved equalizations.</summary>
    ManageQuotations = 5,

    /// <summary>Issue and delete purchase orders.</summary>
    ManagePurchaseOrders = 6,

    /// <summary>Complete or cancel an issued purchase order.</summary>
    ApprovePurchaseOrders = 7,

    /// <summary>See who is part of the organization.</summary>
    ViewTeam = 8,

    /// <summary>Invite, change roles, remove members and rename the organization.</summary>
    ManageTeam = 9
}

/// <summary>Single source of truth for what each role may do. Enforced by the API.</summary>
public static class OrganizationRolePermissions
{
    private static readonly IReadOnlyDictionary<OrganizationRole, IReadOnlySet<OrganizationPermission>> Matrix =
        new Dictionary<OrganizationRole, IReadOnlySet<OrganizationPermission>>
        {
            [OrganizationRole.Admin] = new HashSet<OrganizationPermission>(
                Enum.GetValues<OrganizationPermission>()),
            [OrganizationRole.Buyer] = new HashSet<OrganizationPermission>
            {
                OrganizationPermission.ViewData,
                OrganizationPermission.ManageLists,
                OrganizationPermission.ManageSuppliers,
                OrganizationPermission.ManageQuotes,
                OrganizationPermission.ManageQuotations,
                OrganizationPermission.ManagePurchaseOrders,
                OrganizationPermission.ApprovePurchaseOrders,
                OrganizationPermission.ViewTeam
            },
            [OrganizationRole.Requester] = new HashSet<OrganizationPermission>
            {
                OrganizationPermission.ViewData,
                OrganizationPermission.ManageLists,
                OrganizationPermission.ViewTeam
            },
            [OrganizationRole.Approver] = new HashSet<OrganizationPermission>
            {
                OrganizationPermission.ViewData,
                OrganizationPermission.ApprovePurchaseOrders,
                OrganizationPermission.ViewTeam
            }
        };

    public static bool Allows(OrganizationRole role, OrganizationPermission permission) =>
        Matrix.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public static IReadOnlyCollection<OrganizationPermission> For(OrganizationRole role) =>
        Matrix.TryGetValue(role, out var permissions)
            ? permissions.OrderBy(permission => permission).ToList()
            : [];
}
