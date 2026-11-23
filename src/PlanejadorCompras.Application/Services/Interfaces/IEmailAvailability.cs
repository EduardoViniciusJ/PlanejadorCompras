namespace PlanejadorCompras.Application.Services.Interfaces;

/// <summary>Tells whether the e-mail provider is configured, without creating the client.</summary>
public interface IEmailAvailability
{
    bool IsAvailable { get; }
}
