using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.API.IntegrationTests.Infrastructure;

public sealed record SentEmail(string To, string Subject, string Body);

/// <summary>Stands in for Resend: keeps what would have been sent so tests can follow the links.</summary>
public sealed partial class CapturingEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(to, subject, body));
        return Task.CompletedTask;
    }

    public IReadOnlyList<SentEmail> SentTo(string email) =>
        _sent.Where(message => string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>The raw token of the most recent link e-mailed to an address.</summary>
    public string LastTokenSentTo(string email)
    {
        var message = SentTo(email).LastOrDefault()
            ?? throw new InvalidOperationException($"No e-mail was sent to {email}.");
        var match = TokenInLink().Match(message.Body);

        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException("The e-mail does not contain a token link.");
    }

    [GeneratedRegex(@"[?&]token=([0-9A-Fa-f]+)")]
    private static partial Regex TokenInLink();
}

/// <summary>
/// Stands in for Google: an authorization code maps to the Google account the test registered
/// for it, as if that person had completed the Google consent screen.
/// </summary>
public sealed class FakeGoogleIdentityProvider : IGoogleAuthorizationCodeExchanger, IGoogleTokenValidator
{
    private readonly ConcurrentDictionary<string, GoogleUserInfo> _accountsByCode = new();

    /// <summary>Returns the authorization code a browser would receive for this Google account.</summary>
    public string SignIn(string email, string name = "Pessoa do Google")
    {
        var code = $"auth-code-{Guid.NewGuid():N}";
        _accountsByCode[code] = new GoogleUserInfo($"google-{email.ToLowerInvariant()}", email, name);
        return code;
    }

    public Task<string> ExchangeForIdTokenAsync(string code, CancellationToken cancellationToken = default) =>
        _accountsByCode.ContainsKey(code)
            ? Task.FromResult($"id-token:{code}")
            : throw new UnauthorizedException("Unable to validate Google login.", "google_login_invalid");

    public Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accountsByCode[idToken["id-token:".Length..]]);
}
