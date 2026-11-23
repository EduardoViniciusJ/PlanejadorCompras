using System.Security.Cryptography;
using System.Text;

namespace PlanejadorCompras.Domain.Security;

/// <summary>
/// Opaque high-entropy tokens (refresh, password reset, e-mail confirmation). The raw value is
/// handed to the user once and never stored: the database only keeps its SHA-256 hash, so a
/// database leak does not expose usable tokens. A fast hash is enough because the input is 256
/// random bits, not a human-chosen secret.
/// </summary>
public static class SecretToken
{
    public const int HashLength = 64;

    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
