using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlanejadorCompras.API.IntegrationTests.Infrastructure;

/// <summary>The two session cookies as the browser received them.</summary>
public sealed record SessionCookies(string AccessToken, string RefreshToken)
{
    public string AccessCookieHeader => $"access_token={AccessToken}";

    public string RefreshCookieHeader => $"refresh_token={RefreshToken}";
}

public static class AuthTestExtensions
{
    public const string Password = "senha-de-teste-123";

    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.test";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/register", new { email, password });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client) =>
        client.PostAsync("/api/auth/refresh", null);

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client) =>
        client.PostAsync("/api/auth/logout", null);

    public static Task<HttpResponseMessage> GetMeAsync(this HttpClient client) =>
        client.GetAsync("/api/auth/me");

    /// <summary>Registers an account and confirms it through the link in the e-mail.</summary>
    public static async Task RegisterConfirmedAsync(
        this ApiFactory api,
        HttpClient client,
        string email,
        string password = Password)
    {
        (await client.RegisterAsync(email, password)).EnsureSuccessStatusCode();

        var confirmation = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { token = api.Emails.LastTokenSentTo(email) });
        confirmation.EnsureSuccessStatusCode();
    }

    /// <summary>Registers, confirms and logs in. Returns the session cookies the browser got.</summary>
    public static async Task<SessionCookies> SignUpAndLoginAsync(
        this ApiFactory api,
        HttpClient client,
        string email,
        string password = Password)
    {
        await api.RegisterConfirmedAsync(client, email, password);

        var login = await client.LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return login.ReadSessionCookies();
    }

    public static SessionCookies ReadSessionCookies(this HttpResponseMessage response) =>
        new(
            response.ReadSetCookie("access_token").Value,
            response.ReadSetCookie("refresh_token").Value);

    public static (string Value, string Attributes) ReadSetCookie(this HttpResponseMessage response, string name)
    {
        var header = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.SingleOrDefault(value => value.StartsWith(name + "=", StringComparison.Ordinal))
            : null;

        Assert.True(header is not null, $"The response did not set the '{name}' cookie.");

        var separator = header!.IndexOf(';');
        var value = header[(name.Length + 1)..(separator < 0 ? header.Length : separator)];

        return (value, separator < 0 ? string.Empty : header[separator..].ToLowerInvariant());
    }

    public static async Task<HttpResponseMessage> SendWithCookieAsync(
        this HttpClient client,
        HttpMethod method,
        string url,
        string cookieHeader)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", cookieHeader);
        return await client.SendAsync(request);
    }

    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("errorCode", out var errorCode) ? errorCode.GetString() : null;
    }
}
