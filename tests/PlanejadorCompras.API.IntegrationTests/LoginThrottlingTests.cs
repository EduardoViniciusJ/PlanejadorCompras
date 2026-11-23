using System.Net;
using System.Net.Http.Json;
using PlanejadorCompras.API.IntegrationTests.Infrastructure;

namespace PlanejadorCompras.API.IntegrationTests;

/// <summary>Audit finding M2: the login limit must not depend only on the client IP.</summary>
[Collection(ApiCollection.Name)]
public sealed class LoginThrottlingTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.Api;

    [Fact]
    public async Task Login_ShouldLockTheAccount_WhenGuessesComeFromManyDifferentAddresses()
    {
        var email = AuthTestExtensions.NewEmail("alvo");
        var otherEmail = AuthTestExtensions.NewEmail("outra");
        await _api.RegisterConfirmedAsync(_api.CreateBrowser(), email);
        await _api.RegisterConfirmedAsync(_api.CreateBrowser(), otherEmail);

        // Each guess comes from a new IP address, so the per-IP limit never triggers.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var guess = await _api.CreateBrowser().LoginAsync(email, $"palpite-errado-{attempt}");
            Assert.Equal(HttpStatusCode.Unauthorized, guess.StatusCode);
        }

        // The account is now locked, even for the right password from yet another address.
        var locked = await _api.CreateBrowser().LoginAsync(email);
        Assert.Equal((HttpStatusCode)429, locked.StatusCode);
        Assert.Equal("too_many_login_attempts", await locked.ReadErrorCodeAsync());
        Assert.False(locked.Headers.Contains("Set-Cookie"));
        var retryAfter = Assert.Single(locked.Headers.GetValues("Retry-After"));
        Assert.InRange(int.Parse(retryAfter), 1, 60);

        // Other accounts are not affected.
        Assert.Equal(HttpStatusCode.OK, (await _api.CreateBrowser().LoginAsync(otherEmail)).StatusCode);

        // The owner gets back in through "forgot my password", which also lifts the lock.
        var owner = _api.CreateBrowser();
        (await owner.PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = _api.Emails.LastTokenSentTo(email), newPassword = "senha-nova-do-dono-1" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await owner.LoginAsync(email, "senha-nova-do-dono-1")).StatusCode);
    }

    [Fact]
    public async Task Login_ShouldStillBeLimitedPerAddress()
    {
        // One address hammering many different accounts is stopped by the per-IP limit (10/min).
        var client = _api.CreateBrowser();
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 12; attempt++)
        {
            statuses.Add((await client.LoginAsync(AuthTestExtensions.NewEmail("spray"), "qualquer-senha-1")).StatusCode);
        }

        Assert.Equal(10, statuses.Count(status => status == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(status => status == (HttpStatusCode)429));
    }
}
