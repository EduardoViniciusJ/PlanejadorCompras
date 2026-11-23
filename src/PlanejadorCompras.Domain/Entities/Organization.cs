using PlanejadorCompras.Domain.Rules;
using PlanejadorCompras.Domain.Validation;

namespace PlanejadorCompras.Domain.Entities;

public sealed class Organization
{
    private Organization(Guid id, string name, DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Organization Create(string name, DateTime createdAtUtc) =>
        new(
            Guid.NewGuid(),
            DomainText.Required(name, OrganizationRules.NameMaxLength, nameof(name)),
            createdAtUtc);

    /// <summary>Default name for the organization every account gets on sign-up.</summary>
    public static string BuildPersonalName(string? userName, string email)
    {
        var label = string.IsNullOrWhiteSpace(userName)
            ? email.Split('@')[0]
            : userName.Trim();
        var name = $"Organização de {label}";

        return name.Length <= OrganizationRules.NameMaxLength
            ? name
            : name[..OrganizationRules.NameMaxLength];
    }

    public void Rename(string name)
    {
        Name = DomainText.Required(name, OrganizationRules.NameMaxLength, nameof(name));
    }
}
