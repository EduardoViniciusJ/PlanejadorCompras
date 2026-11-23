using Microsoft.Extensions.Configuration;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Services;

/// <summary>Mirrors the settings <see cref="ResendEmailService"/> requires.</summary>
public sealed class ConfiguredEmailAvailability(IConfiguration configuration) : IEmailAvailability
{
    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"])
        && !string.IsNullOrWhiteSpace(configuration["Resend:FromEmail"]);
}
