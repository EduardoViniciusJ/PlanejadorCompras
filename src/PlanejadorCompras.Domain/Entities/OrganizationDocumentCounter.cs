namespace PlanejadorCompras.Domain.Entities;

/// <summary>Last sequential document number issued by an organization for a kind and year.</summary>
public sealed class OrganizationDocumentCounter
{
    private OrganizationDocumentCounter(Guid organizationId, string kind, int year, int lastNumber)
    {
        OrganizationId = organizationId;
        Kind = kind;
        Year = year;
        LastNumber = lastNumber;
    }

    public Guid OrganizationId { get; private set; }

    public string Kind { get; private set; }

    public int Year { get; private set; }

    public int LastNumber { get; private set; }
}
