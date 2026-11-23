using Moq;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.RefreshToken;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Auth.Sessions;

/// <summary>Clock the tests can move forward.</summary>
public sealed class AdjustableTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public DateTime UtcNow => _now.UtcDateTime;

    public void Advance(TimeSpan duration) => _now = _now.Add(duration);
}

/// <summary>
/// Refresh token store with the same semantics as the EF repository, kept in memory so the
/// rotation rules can be exercised end to end without a database.
/// </summary>
public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _tokens = [];
    private readonly Dictionary<Guid, User> _users = [];

    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    public void RegisterUser(User user) => _users[user.Id] = user;

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        AttachUser(refreshToken);
        _tokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.SingleOrDefault(token => token.TokenHash == tokenHash));

    public Task<bool> IsSessionActiveAsync(Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.Any(token =>
            token.SessionId == sessionId && token.RevokedAt is null && token.ExpiresAt > nowUtc));

    public Task RevokeSessionAsync(
        Guid sessionId,
        RefreshTokenRevocationReason reason,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in _tokens.Where(token => token.SessionId == sessionId))
        {
            token.Revoke(reason, nowUtc);
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(
        Guid userId,
        RefreshTokenRevocationReason reason,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in _tokens.Where(token => token.UserId == userId))
        {
            token.Revoke(reason, nowUtc);
        }

        return Task.CompletedTask;
    }

    public Task RemoveExpiredForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        _tokens.RemoveAll(token => token.UserId == userId && token.ExpiresAt <= nowUtc);
        return Task.CompletedTask;
    }

    // EF loads the User navigation; the fake wires it by reflection because the setter is private.
    private void AttachUser(RefreshToken refreshToken)
    {
        if (_users.TryGetValue(refreshToken.UserId, out var user))
        {
            typeof(RefreshToken).GetProperty(nameof(RefreshToken.User))!.SetValue(refreshToken, user);
        }
    }
}

public static class SessionTestSupport
{
    public static readonly SessionSettings Settings = new(TimeSpan.FromDays(30), TimeSpan.FromSeconds(10));

    /// <summary>JWT service double that returns a recognizable token per session.</summary>
    public static Mock<IJwtTokenService> CreateJwtTokenServiceMock(TimeProvider timeProvider)
    {
        var mock = new Mock<IJwtTokenService>();
        mock.Setup(x => x.GenerateAccessToken(It.IsAny<GenerateAccessTokenRequestDto>()))
            .Returns((GenerateAccessTokenRequestDto request) => new GenerateAccessTokenResponseDto(
                $"jwt-for-{request.SessionId}",
                timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15)));
        return mock;
    }

    public static AuthSessionService CreateSessionService(
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider,
        IJwtTokenService? jwtTokenService = null) =>
        new(
            jwtTokenService ?? CreateJwtTokenServiceMock(timeProvider).Object,
            refreshTokenRepository,
            Settings,
            timeProvider);
}
