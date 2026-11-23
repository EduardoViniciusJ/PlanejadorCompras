namespace PlanejadorCompras.Application.Services;

/// <param name="RefreshTokenLifetime">
/// How long a refresh token stays valid. Each refresh issues a new one, so this is the maximum
/// time a session survives without the user opening the app.
/// </param>
/// <param name="RefreshTokenReuseLeeway">
/// Window after a rotation in which presenting the just-rotated token again is treated as a
/// concurrent refresh from the same browser instead of token theft. Zero disables it.
/// </param>
public sealed record SessionSettings(TimeSpan RefreshTokenLifetime, TimeSpan RefreshTokenReuseLeeway)
{
    public static SessionSettings Default { get; } = new(TimeSpan.FromDays(30), TimeSpan.FromSeconds(10));
}
