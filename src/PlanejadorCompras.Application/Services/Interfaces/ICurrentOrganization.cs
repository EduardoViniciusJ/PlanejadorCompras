using PlanejadorCompras.Domain.Authorization;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Application.Services.Interfaces;

/// <summary>
/// Organization the authenticated user is working in for this request. Every
/// piece of business data is read and written inside this organization only.
/// </summary>
public interface ICurrentOrganization
{
    /// <summary>Throws when the request has no resolved organization.</summary>
    Guid OrganizationId { get; }

    OrganizationRole Role { get; }

    bool Can(OrganizationPermission permission);

    /// <summary>Throws a 403 when the current role lacks the permission.</summary>
    void Require(OrganizationPermission permission);
}
