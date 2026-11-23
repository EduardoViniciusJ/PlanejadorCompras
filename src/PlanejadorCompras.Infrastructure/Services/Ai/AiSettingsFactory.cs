using System.Globalization;
using Microsoft.Extensions.Configuration;
using PlanejadorCompras.Application.Features.Ai;

namespace PlanejadorCompras.Infrastructure.Services.Ai;

public static class AiSettingsFactory
{
    /// <summary>
    /// Reads the "Ai" section. Missing or invalid values fall back to the defaults, so a
    /// typo in configuration can never remove a limit.
    /// </summary>
    public static AiSettings Create(IConfiguration configuration)
    {
        var section = configuration.GetSection("Ai");
        var model = section["Model"];
        var effort = section["Effort"];

        return new AiSettings
        {
            IsConfigured = !string.IsNullOrWhiteSpace(section["ApiKey"]),
            Model = string.IsNullOrWhiteSpace(model) ? AiSettings.DefaultModel : model.Trim(),
            // An explicitly empty value omits the parameter, for models that do not accept it.
            Effort = effort is null ? "low" : effort.Trim(),
            MonthlyLimitPerUser = ReadInt(section, "MonthlyLimitPerUser", AiSettings.DefaultMonthlyLimitPerUser, 0, 100_000),
            TimeoutSeconds = ReadInt(section, "TimeoutSeconds", AiSettings.DefaultTimeoutSeconds, 5, 600),
            MaxFileBytes = ReadLong(section, "MaxFileBytes", AiSettings.DefaultMaxFileBytes, 1024, AiSettings.HardMaxFileBytes),
            MaxInputCharacters = ReadInt(section, "MaxInputCharacters", AiSettings.DefaultMaxInputCharacters, 100, 200_000),
            MaxOutputTokens = ReadInt(section, "MaxOutputTokens", AiSettings.DefaultMaxOutputTokens, 256, 64_000),
            MaxPdfPages = ReadInt(section, "MaxPdfPages", AiSettings.DefaultMaxPdfPages, 1, 100)
        };
    }

    private static int ReadInt(IConfiguration section, string key, int fallback, int minimum, int maximum) =>
        (int)ReadLong(section, key, fallback, minimum, maximum);

    private static long ReadLong(IConfiguration section, string key, long fallback, long minimum, long maximum) =>
        long.TryParse(section[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        && value >= minimum
        && value <= maximum
            ? value
            : fallback;
}
