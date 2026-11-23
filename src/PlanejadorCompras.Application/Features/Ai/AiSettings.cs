namespace PlanejadorCompras.Application.Features.Ai;

/// <summary>
/// AI limits and provider options, read from the "Ai" configuration section.
/// The API key itself never leaves the infrastructure layer.
/// </summary>
public sealed record AiSettings
{
    public const string DefaultModel = "claude-haiku-5-5";
    public const int DefaultMonthlyLimitPerUser = 20;
    public const int DefaultTimeoutSeconds = 60;
    public const long DefaultMaxFileBytes = 5 * 1024 * 1024;
    public const long HardMaxFileBytes = 10 * 1024 * 1024;
    public const int DefaultMaxInputCharacters = 20_000;
    public const int DefaultMaxOutputTokens = 16_000;
    public const int DefaultMaxPdfPages = 20;

    public bool IsConfigured { get; init; }

    public string Model { get; init; } = DefaultModel;

    /// <summary>Reasoning effort sent to the provider; empty omits the parameter.</summary>
    public string Effort { get; init; } = "low";

    public int MonthlyLimitPerUser { get; init; } = DefaultMonthlyLimitPerUser;

    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    public long MaxFileBytes { get; init; } = DefaultMaxFileBytes;

    public int MaxInputCharacters { get; init; } = DefaultMaxInputCharacters;

    public int MaxOutputTokens { get; init; } = DefaultMaxOutputTokens;

    public int MaxPdfPages { get; init; } = DefaultMaxPdfPages;
}
