using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlanejadorCompras.API.IntegrationTests.Infrastructure;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class GoogleLoginTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.Api;

    /// <summary>Audit finding A1, end to end through HTTP and the real database.</summary>
    [Fact]
    public async Task AttackerWhoPreRegisteredTheVictimsEmail_ShouldLoseAccess_WhenVictimSignsInWithGoogle()
    {
        var victimEmail = AuthTestExtensions.NewEmail("vitima");
        const string attackerPassword = "senha-do-atacante-123";

        // The attacker registers the victim's address with a password of their own. The
        // confirmation e-mail goes to the victim, so the attacker can never confirm it.
        var attacker = _api.CreateBrowser();
        (await attacker.RegisterAsync(victimEmail, attackerPassword)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await attacker.LoginAsync(victimEmail, attackerPassword)).StatusCode);

        // Later the victim signs in with Google, which links to the existing account.
        var victim = _api.CreateBrowser();
        var googleLogin = await victim.PostAsJsonAsync(
            "/api/auth/google-code",
            new { code = _api.Google.SignIn(victimEmail, "Vítima") });
        Assert.Equal(HttpStatusCode.OK, googleLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await victim.GetMeAsync()).StatusCode);

        // The attacker's password must not open the now-confirmed account.
        var takeover = await attacker.LoginAsync(victimEmail, attackerPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, takeover.StatusCode);
        Assert.Equal("invalid_credentials", await takeover.ReadErrorCodeAsync());

        // Neither does the confirmation link that was pending for the attacker's registration.
        var staleConfirmation = await attacker.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { token = _api.Emails.LastTokenSentTo(victimEmail) });
        Assert.Equal(HttpStatusCode.BadRequest, staleConfirmation.StatusCode);

        using var scope = _api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanejadorComprasDbContext>();
        var account = await db.Users.SingleAsync(user => user.Email == victimEmail);
        Assert.Null(account.PasswordHash);
        Assert.NotNull(account.GoogleId);
        Assert.True(account.IsEmailConfirmed);
        Assert.False(await db.UserTokens.AnyAsync(token => token.UserId == account.Id));
    }

    [Fact]
    public async Task OwnerWithConfirmedEmail_ShouldKeepPassword_WhenLinkingGoogle()
    {
        var email = AuthTestExtensions.NewEmail();
        var browser = _api.CreateBrowser();
        await _api.RegisterConfirmedAsync(browser, email);

        var googleLogin = await browser.PostAsJsonAsync(
            "/api/auth/google-code",
            new { code = _api.Google.SignIn(email) });
        Assert.Equal(HttpStatusCode.OK, googleLogin.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _api.CreateBrowser().LoginAsync(email)).StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_ShouldCreateAccount_AndReuseItOnTheNextLogin()
    {
        var email = AuthTestExtensions.NewEmail("google");

        foreach (var _ in Enumerable.Range(0, 2))
        {
            var browser = _api.CreateBrowser();
            var login = await browser.PostAsJsonAsync(
                "/api/auth/google-code",
                new { code = _api.Google.SignIn(email, "Maria Google") });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            // The Google display name survives a refresh, which rebuilds the access token.
            Assert.Equal(HttpStatusCode.NoContent, (await browser.RefreshAsync()).StatusCode);
            var me = await browser.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/auth/me");
            Assert.Equal("Maria Google", me.GetProperty("name").GetString());
        }

        using var scope = _api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanejadorComprasDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(user => user.Email == email));
    }
}

/// <summary>Audit finding M3: the API must work with no Google credentials configured.</summary>
[Collection(ApiCollection.Name)]
public sealed class GoogleNotConfiguredTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.ApiWithoutGoogle;

    [Fact]
    public async Task EmailAndPasswordRoutes_ShouldWork_WithoutGoogleConfiguration()
    {
        var browser = _api.CreateBrowser();
        var email = AuthTestExtensions.NewEmail();

        await _api.SignUpAndLoginAsync(browser, email);

        Assert.Equal(HttpStatusCode.OK, (await browser.GetMeAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await browser.RefreshAsync()).StatusCode);
        (await browser.PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await browser.LogoutAsync()).StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_ShouldAnswerWithAClearError_WithoutGoogleConfiguration()
    {
        var response = await _api.CreateBrowser().PostAsJsonAsync(
            "/api/auth/google-code",
            new { code = "any-authorization-code" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("google_login_not_configured", await response.ReadErrorCodeAsync());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("login com Google", System.Text.RegularExpressions.Regex.Unescape(body));
    }
}
