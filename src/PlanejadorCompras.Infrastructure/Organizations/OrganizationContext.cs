using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Infrastructure.Organizations;

/// <summary>
/// Non-throwing view of the current organization, consumed by the DbContext
/// to filter queries and to stamp/validate writes.
/// </summary>
public interface IOrganizationScope
{
    Guid? OrganizationId { get; }
}

/// <summary>
/// Request-scoped holder of the organization resolved for the authenticated
/// user. It stays empty for anonymous requests, which makes every
/// organization-owned query return nothing (fail closed).
/// </summary>
public sealed class OrganizationContext : IOrganizationScope, ICurrentOrganization
{
    private Guid? _organizationId;
    private OrganizationRole? _role;

    Guid? IOrganizationScope.OrganizationId => _organizationId;

    public bool IsResolved => _organizationId.HasValue;

    /// <summary>Name saved in the user's profile, when there is one.</summary>
    public string? UserName { get; private set; }

    public Guid OrganizationId =>
        _organizationId ?? throw NoOrganization();

    public OrganizationRole Role =>
        _role ?? throw NoOrganization();

    public bool Can(OrganizationPermission permission) =>
        _role.HasValue && OrganizationRolePermissions.Allows(_role.Value, permission);

    public void Require(OrganizationPermission permission)
    {
        if (!_role.HasValue)
        {
            throw NoOrganization();
        }

        if (!Can(permission))
        {
            throw new ForbiddenException(
                "Seu papel nesta organização não permite realizar esta ação.",
                "organization_permission_denied");
        }
    }

    public void Set(Guid organizationId, OrganizationRole role, string? userName = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(organizationId, Guid.Empty);

        _organizationId = organizationId;
        _role = role;
        UserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    public void Clear()
    {
        _organizationId = null;
        _role = null;
        UserName = null;
    }

    private static ForbiddenException NoOrganization() =>
        new(
            "Nenhuma organização ativa para este usuário.",
            "organization_required");
}
