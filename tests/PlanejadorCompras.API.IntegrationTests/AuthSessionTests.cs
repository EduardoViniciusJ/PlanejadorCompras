using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PlanejadorCompras.API.IntegrationTests.Infrastructure;

namespace PlanejadorCompras.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthSessionTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.Api;

    [Fact]
    public async Task Login_ShouldRequireConfirmedEmail_ThenSetHttpOnlySessionCookies()
    {
        var browser = _api.CreateBrowser();
        var email = AuthTestExtensions.NewEmail();

        (await browser.RegisterAsync(email)).EnsureSuccessStatusCode();

        var beforeConfirmation = await browser.LoginAsync(email);
        Assert.Equal(HttpStatusCode.Forbidden, beforeConfirmation.StatusCode);
        Assert.Equal("email_not_confirmed", await beforeConfirmation.ReadErrorCodeAsync());

        var confirmation = await browser.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { token = _api.Emails.LastTokenSentTo(email) });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);

        var login = await browser.LoginAsync(email);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var (accessToken, accessAttributes) = login.ReadSetCookie("access_token");
        var (refreshToken, refreshAttributes) = login.ReadSetCookie("refresh_token");
        Assert.NotEmpty(accessToken);
        Assert.NotEmpty(refreshToken);
        Assert.Contains("httponly", accessAttributes);
        Assert.Contains("path=/", accessAttributes);
        Assert.Contains("httponly", refreshAttributes);
        // The refresh token only travels to the auth endpoints.
        Assert.Contains("path=/api/auth", refreshAttributes);

        // Neither token is exposed to JavaScript through the response body.
        var body = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain(accessToken, body);
        Assert.DoesNotContain(refreshToken, body);

        var me = await browser.GetMeAsync();
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var user = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, user.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_ShouldRejectWrongPassword_AndUnknownEmailTheSameWay()
    {
        var browser = _api.CreateBrowser();
        var email = AuthTestExtensions.NewEmail();
        await _api.RegisterConfirmedAsync(browser, email);

        var wrongPassword = await browser.LoginAsync(email, "senha-errada-123");
        var unknownEmail = await browser.LoginAsync(AuthTestExtensions.NewEmail("ghost"), "senha-errada-123");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal("invalid_credentials", await wrongPassword.ReadErrorCodeAsync());
        Assert.Equal("invalid_credentials", await unknownEmail.ReadErrorCodeAsync());
        Assert.False(wrongPassword.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task ProtectedRoute_ShouldReturn401_WithoutSession()
    {
        var me = await _api.CreateBrowser().GetMeAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("authentication_required", await me.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Refresh_ShouldRotateBothTokens_AndKeepTheSessionWorking()
    {
        var browser = _api.CreateBrowser();
        var login = await _api.SignUpAndLoginAsync(browser, AuthTestExtensions.NewEmail());

        var refresh = await browser.RefreshAsync();

        Assert.Equal(HttpStatusCode.NoContent, refresh.StatusCode);
        var rotated = refresh.ReadSessionCookies();
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(login.AccessToken, rotated.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetMeAsync()).StatusCode);

        // The cookie jar now holds the rotated token: refreshing again keeps working.
        Assert.Equal(HttpStatusCode.NoContent, (await browser.RefreshAsync()).StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldRestoreAccess_WhenOnlyTheRefreshCookieIsLeft()
    {
        // This is the browser after the access token cookie expired.
        var login = await _api.SignUpAndLoginAsync(_api.CreateBrowser(), AuthTestExtensions.NewEmail());
        var raw = _api.CreateRawClient();

        var refresh = await raw.SendWithCookieAsync(HttpMethod.Post, "/api/auth/refresh", login.RefreshCookieHeader);

        Assert.Equal(HttpStatusCode.NoContent, refresh.StatusCode);
        var me = await raw.SendWithCookieAsync(
            HttpMethod.Get,
            "/api/auth/me",
            refresh.ReadSessionCookies().AccessCookieHeader);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WithoutRefreshCookie()
    {
        var refresh = await _api.CreateBrowser().RefreshAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal("session_expired", await refresh.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Refresh_ShouldRevokeTheWholeSession_WhenAnAlreadyRotatedTokenIsReused()
    {
        var browser = _api.CreateBrowser();
        var login = await _api.SignUpAndLoginAsync(browser, AuthTestExtensions.NewEmail());

        // The real browser rotates its token...
        var rotated = (await browser.RefreshAsync()).ReadSessionCookies();

        // ...and someone who copied the earlier refresh token tries to use it.
        var thief = _api.CreateRawClient();
        var stolen = await thief.SendWithCookieAsync(HttpMethod.Post, "/api/auth/refresh", login.RefreshCookieHeader);

        Assert.Equal(HttpStatusCode.Unauthorized, stolen.StatusCode);
        Assert.Equal("session_revoked", await stolen.ReadErrorCodeAsync());
        // The response clears the cookies of whoever presented the dead token.
        Assert.Empty(stolen.ReadSetCookie("refresh_token").Value);

        // Reuse was detected, so the session is over for everyone holding any of its tokens:
        // the access token stops working immediately and the newest refresh token is dead too.
        var meWithAccessToken = await thief.SendWithCookieAsync(HttpMethod.Get, "/api/auth/me", rotated.AccessCookieHeader);
        Assert.Equal(HttpStatusCode.Unauthorized, meWithAccessToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetMeAsync()).StatusCode);

        var refreshWithNewestToken = await thief.SendWithCookieAsync(
            HttpMethod.Post,
            "/api/auth/refresh",
            rotated.RefreshCookieHeader);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshWithNewestToken.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeTheSessionOnTheServer_NotJustClearCookies()
    {
        var browser = _api.CreateBrowser();
        var login = await _api.SignUpAndLoginAsync(browser, AuthTestExtensions.NewEmail());

        var logout = await browser.LogoutAsync();

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Empty(logout.ReadSetCookie("access_token").Value);
        Assert.Empty(logout.ReadSetCookie("refresh_token").Value);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetMeAsync()).StatusCode);

        // Cookies copied before the logout are useless afterwards.
        var replay = _api.CreateRawClient();
        var me = await replay.SendWithCookieAsync(HttpMethod.Get, "/api/auth/me", login.AccessCookieHeader);
        var refresh = await replay.SendWithCookieAsync(HttpMethod.Post, "/api/auth/refresh", login.RefreshCookieHeader);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldEndOnlyTheCurrentSession()
    {
        var email = AuthTestExtensions.NewEmail();
        var laptop = _api.CreateBrowser();
        var phone = _api.CreateBrowser();
        await _api.SignUpAndLoginAsync(laptop, email);
        Assert.Equal(HttpStatusCode.OK, (await phone.LoginAsync(email)).StatusCode);

        await laptop.LogoutAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await laptop.GetMeAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await phone.GetMeAsync()).StatusCode);
    }

    [Fact]
    public async Task PasswordReset_ShouldEndEverySession_AndReplaceThePassword()
    {
        var email = AuthTestExtensions.NewEmail();
        var laptop = _api.CreateBrowser();
        var phone = _api.CreateBrowser();
        var laptopSession = await _api.SignUpAndLoginAsync(laptop, email);
        Assert.Equal(HttpStatusCode.OK, (await phone.LoginAsync(email)).StatusCode);

        var recovery = _api.CreateBrowser();
        (await recovery.PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        var resetToken = _api.Emails.LastTokenSentTo(email);
        var reset = await recovery.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = resetToken, newPassword = "nova-senha-456" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // Every session that existed before the reset is gone, immediately.
        Assert.Equal(HttpStatusCode.Unauthorized, (await laptop.GetMeAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetMeAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.RefreshAsync()).StatusCode);
        var replay = await _api.CreateRawClient()
            .SendWithCookieAsync(HttpMethod.Post, "/api/auth/refresh", laptopSession.RefreshCookieHeader);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // The old password no longer works, the new one does, and the reset link is single use.
        var fresh = _api.CreateBrowser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.LoginAsync(email)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fresh.LoginAsync(email, "nova-senha-456")).StatusCode);
        var secondUse = await recovery.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { token = resetToken, newPassword = "outra-senha-789" });
        Assert.Equal(HttpStatusCode.BadRequest, secondUse.StatusCode);
    }

    [Fact]
    public async Task UnsafeRequests_ShouldRequireTheCsrfHeader_WhenSessionCookiesArePresent()
    {
        var login = await _api.SignUpAndLoginAsync(_api.CreateBrowser(), AuthTestExtensions.NewEmail());
        var crossSite = _api.CreateRawClient();
        crossSite.DefaultRequestHeaders.Remove("X-Requested-With");

        var createList = new HttpRequestMessage(HttpMethod.Post, "/api/shopping-lists")
        {
            Content = JsonContent.Create(new { name = "Lista forjada" })
        };
        createList.Headers.Add("Cookie", login.AccessCookieHeader);
        var write = await crossSite.SendAsync(createList);
        var refresh = await crossSite.SendWithCookieAsync(HttpMethod.Post, "/api/auth/refresh", login.RefreshCookieHeader);
        var logout = await crossSite.SendWithCookieAsync(HttpMethod.Post, "/api/auth/logout", login.RefreshCookieHeader);

        Assert.Equal(HttpStatusCode.Unauthorized, write.StatusCode);
        Assert.Equal("missing_x_requested_with", await write.ReadErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal("missing_x_requested_with", await refresh.ReadErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);

        // None of the forged requests did anything: the session is intact.
        var me = await _api.CreateRawClient().SendWithCookieAsync(HttpMethod.Get, "/api/auth/me", login.AccessCookieHeader);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }
}
