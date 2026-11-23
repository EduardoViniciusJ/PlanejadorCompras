using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Services;

public sealed class GoogleAuthorizationCodeExchanger : IGoogleAuthorizationCodeExchanger
{
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";

    private readonly HttpClient _httpClient;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string? _redirectUri;

    public GoogleAuthorizationCodeExchanger(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        // Google login is optional. Missing settings are reported when someone actually tries to
        // log in with Google, so the rest of the API (e-mail and password login) is unaffected.
        _clientId = configuration["Authentication:Google:ClientId"];
        _clientSecret = configuration["Authentication:Google:ClientSecret"];
        _redirectUri = configuration["Authentication:Google:RedirectUri"];
    }

    public async Task<string> ExchangeForIdTokenAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId)
            || string.IsNullOrWhiteSpace(_clientSecret)
            || string.IsNullOrWhiteSpace(_redirectUri))
        {
            throw GoogleLoginConfiguration.NotConfigured();
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code"
        });

        using var response = await _httpClient.PostAsync(
            GoogleTokenEndpoint,
            content,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = DeserializeTokenResponse(responseBody);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateInvalidGoogleLoginException();
        }

        if (string.IsNullOrWhiteSpace(tokenResponse?.IdToken))
        {
            throw CreateInvalidGoogleLoginException();
        }

        return tokenResponse.IdToken;
    }

    private static GoogleTokenResponsePayload? DeserializeTokenResponse(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GoogleTokenResponsePayload>(responseBody);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static UnauthorizedException CreateInvalidGoogleLoginException()
    {
        return new UnauthorizedException(
            "Unable to validate Google login.",
            "google_login_invalid");
    }
}
