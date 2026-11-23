using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Application.UnitTests.Support;

internal static class TestOrganization
{
    public static ICurrentOrganization For(
        Guid organizationId,
        OrganizationRole role = OrganizationRole.Admin) =>
        new FixedOrganization(() => organizationId, role);

    /// <summary>
    /// The personal organization of a user: same id as the user, the way the
    /// data migration created them. The id is read lazily so tests may set up
    /// the user mock after building the subject under test.
    /// </summary>
    public static ICurrentOrganization PersonalOf(
        Mock<ICurrentUser> currentUser,
        OrganizationRole role = OrganizationRole.Admin) =>
        new FixedOrganization(() => currentUser.Object.UserId, role);

    /// <summary>
    /// In production the DbContext stamps the organization on insert; unit
    /// tests that never touch a DbContext assign it explicitly.
    /// </summary>
    public static TEntity InOrganization<TEntity>(this TEntity entity, Guid organizationId)
        where TEntity : class, IOrganizationOwned
    {
        typeof(TEntity)
            .GetProperty(nameof(IOrganizationOwned.OrganizationId))!
            .SetValue(entity, organizationId);

        return entity;
    }

    private sealed class FixedOrganization(Func<Guid> organizationId, OrganizationRole role)
        : ICurrentOrganization
    {
        public Guid OrganizationId => organizationId();

        public OrganizationRole Role => role;

        public bool Can(OrganizationPermission permission) =>
            OrganizationRolePermissions.Allows(role, permission);

        public void Require(OrganizationPermission permission)
        {
            if (!Can(permission))
            {
                throw new ForbiddenException(
                    "Seu papel nesta organização não permite realizar esta ação.",
                    "organization_permission_denied");
            }
        }
    }
}
