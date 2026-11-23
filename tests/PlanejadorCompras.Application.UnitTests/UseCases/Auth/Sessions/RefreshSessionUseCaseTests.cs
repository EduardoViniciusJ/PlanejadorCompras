using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.UseCases.Auth;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Auth.Sessions;

public sealed class RefreshSessionUseCaseTests
{
    private readonly AdjustableTimeProvider _clock = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly User _user = User.CreateGoogleUser("google-1", "user@test.com");

    public RefreshSessionUseCaseTests()
    {
        _refreshTokens.RegisterUser(_user);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRotateToken_AndKeepTheSameSession()
    {
        var login = await StartSessionAsync();
        _clock.Advance(TimeSpan.FromMinutes(20));

        var refreshed = await CreateUseCase().ExecuteAsync(login.RefreshToken);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(login.AccessToken, refreshed.AccessToken); // same session id in the JWT double
        Assert.Equal(_clock.UtcNow.AddDays(30), refreshed.RefreshTokenExpiresAtUtc);

        var old = FindToken(login.RefreshToken);
        var current = FindToken(refreshed.RefreshToken);
        Assert.Equal(RefreshTokenRevocationReason.Rotated, old.RevocationReason);
        Assert.False(current.IsRevoked);
        Assert.Equal(old.SessionId, current.SessionId);
        Assert.Equal(old.DisplayName, current.DisplayName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStoreOnlyTheHash_NeverTheRawToken()
    {
        var login = await StartSessionAsync();

        var stored = Assert.Single(_refreshTokens.Tokens);
        Assert.NotEqual(login.RefreshToken, stored.TokenHash);
        Assert.Equal(SecretToken.Hash(login.RefreshToken), stored.TokenHash);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRevokeWholeSession_WhenARotatedTokenIsReused()
    {
        // The legitimate browser rotates the token; later someone presents the old copy.
        var login = await StartSessionAsync();
        var refreshed = await CreateUseCase().ExecuteAsync(login.RefreshToken);
        _clock.Advance(TimeSpan.FromMinutes(5));

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateUseCase().ExecuteAsync(login.RefreshToken));

        Assert.Equal("session_revoked", exception.ErrorCode);
        Assert.All(_refreshTokens.Tokens, token => Assert.True(token.IsRevoked));
        Assert.Equal(RefreshTokenRevocationReason.ReuseDetected, FindToken(refreshed.RefreshToken).RevocationReason);
        Assert.False(await _refreshTokens.IsSessionActiveAsync(FindToken(login.RefreshToken).SessionId, _clock.UtcNow));

        // The token the legitimate browser holds is dead too: both sides must log in again.
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateUseCase().ExecuteAsync(refreshed.RefreshToken));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAcceptRotatedToken_WhenReusedWithinLeeway()
    {
        // Two tabs get a 401 at the same time and both send the same refresh token.
        var login = await StartSessionAsync();
        var firstTab = await CreateUseCase().ExecuteAsync(login.RefreshToken);
        _clock.Advance(TimeSpan.FromSeconds(3));

        var secondTab = await CreateUseCase().ExecuteAsync(login.RefreshToken);

        Assert.NotEqual(firstTab.RefreshToken, secondTab.RefreshToken);
        Assert.False(FindToken(firstTab.RefreshToken).IsRevoked);
        Assert.False(FindToken(secondTab.RefreshToken).IsRevoked);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectRotatedTokenWithinLeeway_WhenSessionWasLoggedOut()
    {
        var login = await StartSessionAsync();
        var refreshed = await CreateUseCase().ExecuteAsync(login.RefreshToken);
        await new LogoutUseCase(_refreshTokens, _clock, _unitOfWorkMock.Object)
            .ExecuteAsync(refreshed.RefreshToken, null);
        _clock.Advance(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateUseCase().ExecuteAsync(login.RefreshToken));

        Assert.All(_refreshTokens.Tokens, token => Assert.True(token.IsRevoked));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReject_WhenTokenIsExpired()
    {
        var login = await StartSessionAsync();
        _clock.Advance(TimeSpan.FromDays(30));

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateUseCase().ExecuteAsync(login.RefreshToken));

        Assert.Equal("session_expired", exception.ErrorCode);
        Assert.Single(_refreshTokens.Tokens);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-token-we-issued")]
    public async Task ExecuteAsync_ShouldReject_WhenTokenIsMissingOrUnknown(string? refreshToken)
    {
        await StartSessionAsync();

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateUseCase().ExecuteAsync(refreshToken));

        Assert.Equal("session_expired", exception.ErrorCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeSession_SoRefreshNoLongerWorks()
    {
        var login = await StartSessionAsync();
        var otherDevice = await StartSessionAsync();

        await new LogoutUseCase(_refreshTokens, _clock, _unitOfWorkMock.Object)
            .ExecuteAsync(login.RefreshToken, null);

        Assert.Equal(RefreshTokenRevocationReason.LoggedOut, FindToken(login.RefreshToken).RevocationReason);
        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateUseCase().ExecuteAsync(login.RefreshToken));

        // Logging out one device leaves the other sessions alone.
        Assert.False(FindToken(otherDevice.RefreshToken).IsRevoked);
        await CreateUseCase().ExecuteAsync(otherDevice.RefreshToken);
    }

    [Fact]
    public async Task Logout_ShouldRevokeSessionOfTheAccessToken_WhenRefreshCookieIsMissing()
    {
        var login = await StartSessionAsync();
        var sessionId = FindToken(login.RefreshToken).SessionId;

        await new LogoutUseCase(_refreshTokens, _clock, _unitOfWorkMock.Object)
            .ExecuteAsync(null, sessionId);

        Assert.False(await _refreshTokens.IsSessionActiveAsync(sessionId, _clock.UtcNow));
    }

    private Task<AuthSessionDto> StartSessionAsync() =>
        SessionTestSupport.CreateSessionService(_refreshTokens, _clock).StartAsync(_user, "Usuária Teste");

    private RefreshSessionUseCase CreateUseCase() => new(
        _refreshTokens,
        SessionTestSupport.CreateSessionService(_refreshTokens, _clock),
        SessionTestSupport.Settings,
        _clock,
        _unitOfWorkMock.Object);

    private RefreshToken FindToken(string rawToken) =>
        _refreshTokens.Tokens.Single(token => token.TokenHash == SecretToken.Hash(rawToken));
}
