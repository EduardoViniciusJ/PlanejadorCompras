using System.Text;

namespace PlanejadorCompras.Application.UseCases.Ai;

internal static class AiText
{
    /// <summary>
    /// Normalizes text produced by the model before it reaches the user: no control
    /// characters, single spaces, bounded length. Returns null when nothing is left.
    /// </summary>
    internal static string? Clean(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(Math.Min(value.Length, maximumLength));
        var pendingSpace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
            if (builder.Length >= maximumLength)
            {
                break;
            }
        }

        var cleaned = builder.ToString().Trim();
        if (cleaned.Length > maximumLength)
        {
            cleaned = cleaned[..maximumLength].TrimEnd();
        }

        // A cut in the middle of a surrogate pair would leave an invalid character behind.
        if (cleaned.Length > 0 && char.IsHighSurrogate(cleaned[^1]))
        {
            cleaned = cleaned[..^1];
        }

        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>Makes a value safe to sit in one cell of a pipe-separated prompt table.</summary>
    internal static string TableCell(string value) =>
        (Clean(value, 300) ?? string.Empty).Replace('|', '/');
}
