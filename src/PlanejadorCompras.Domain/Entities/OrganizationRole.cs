namespace PlanejadorCompras.Domain.Entities;

public enum OrganizationRole
{
    Admin = 1,
    Buyer = 2,
    Requester = 3,
    Approver = 4
}

public static class OrganizationRoleNames
{
    public const string Admin = "admin";
    public const string Buyer = "buyer";
    public const string Requester = "requester";
    public const string Approver = "approver";

    public static string ToName(this OrganizationRole role) => role switch
    {
        OrganizationRole.Admin => Admin,
        OrganizationRole.Buyer => Buyer,
        OrganizationRole.Requester => Requester,
        OrganizationRole.Approver => Approver,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public static bool TryParse(string? value, out OrganizationRole role)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case Admin:
                role = OrganizationRole.Admin;
                return true;
            case Buyer:
                role = OrganizationRole.Buyer;
                return true;
            case Requester:
                role = OrganizationRole.Requester;
                return true;
            case Approver:
                role = OrganizationRole.Approver;
                return true;
            default:
                role = default;
                return false;
        }
    }
}
