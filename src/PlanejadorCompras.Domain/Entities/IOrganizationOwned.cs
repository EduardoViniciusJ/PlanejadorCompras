namespace PlanejadorCompras.Domain.Entities;

/// <summary>
/// Marks data that belongs to exactly one organization. The persistence layer
/// stamps <see cref="OrganizationId"/> on insert, filters every query by the
/// current organization and rejects writes that cross organizations.
/// </summary>
public interface IOrganizationOwned
{
    Guid OrganizationId { get; }
}
