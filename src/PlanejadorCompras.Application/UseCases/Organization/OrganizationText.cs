using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Application.UseCases.Organization;

internal static class OrganizationText
{
    public static string ToLabel(this OrganizationRole role) => role switch
    {
        OrganizationRole.Admin => "Administrador",
        OrganizationRole.Buyer => "Comprador",
        OrganizationRole.Requester => "Solicitante",
        OrganizationRole.Approver => "Aprovador",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    /// <summary>Permission names as the frontend receives them (camelCase).</summary>
    public static IReadOnlyCollection<string> ToPermissionNames(this OrganizationRole role) =>
        OrganizationRolePermissions.For(role)
            .Select(permission =>
            {
                var name = permission.ToString();
                return char.ToLowerInvariant(name[0]) + name[1..];
            })
            .ToList();

    public static OrganizationRole ParseRole(string? value) =>
        OrganizationRoleNames.TryParse(value, out var role)
            ? role
            : throw new Exceptions.BadRequestException(
                "Papel inválido. Use administrador, comprador, solicitante ou aprovador.",
                "organization_invalid_role");
}
