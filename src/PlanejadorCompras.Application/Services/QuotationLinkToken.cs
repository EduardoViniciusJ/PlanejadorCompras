using System.Security.Cryptography;
using System.Text;

namespace PlanejadorCompras.Application.Services;

/// <summary>
/// Token of the public quotation link: 256 random bits in base64url. Only its SHA-256 hash
/// is stored, so a copy of the database does not give access to any quotation.
/// </summary>
public static class QuotationLinkToken
{
    private const int TokenBytes = 32;

    /// <summary>Length of 32 bytes in unpadded base64url.</summary>
    public const int TokenLength = 43;

    public static (string Token, string Hash) Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return (token, Hash(token));
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Cheap shape check, so garbage never reaches the database.</summary>
    public static bool HasValidFormat(string? token)
    {
        if (token is null || token.Length != TokenLength)
        {
            return false;
        }

        foreach (var character in token)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
