using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.UnitTests.UseCases.Auth.Sessions;
using PlanejadorCompras.Application.UseCases.Auth;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Infrastructure.Services;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Auth;

/// <summary>Audit finding M2: guessing a password is limited per account, not only per IP.</summary>
public sealed class LoginThrottlingTests
{
    private const string Email = "user@test.com";
    private const string Password = "correct-password-123";

    private readonly AdjustableTimeProvider _clock = new();
    private readonly PasswordHasher _passwordHasher = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly InMemoryLoginAttemptLimiter _limiter;

    public LoginThrottlingTests()
    {
        _limiter = new InMemoryLoginAttemptLimiter(_clock);

        var user = User.CreateManualUser(Email, _passwordHasher.HashPassword(Password));
        user.ConfirmEmail();
        _refreshTokens.RegisterUser(user);
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    [Fact]
    public async Task Login_ShouldLockTheAccount_AfterFiveWrongPasswords_EvenForTheRightPassword()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(Email, "wrong-password"));
        }

        // The lock applies to the account no matter where the request comes from, and not even
        // the right password gets through while it lasts.
        var exception = await Assert.ThrowsAsync<TooManyRequestsException>(() => LoginAsync(Email, Password));

        Assert.Equal("too_many_login_attempts", exception.ErrorCode);
        Assert.NotNull(exception.RetryAfter);
        Assert.InRange(exception.RetryAfter.Value, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
        Assert.Empty(_refreshTokens.Tokens);
    }

    [Fact]
    public async Task Login_ShouldWorkAgain_OnceTheLockExpires()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(Email, "wrong-password"));
        }

        _clock.Advance(TimeSpan.FromSeconds(61));

        var session = await LoginAsync(Email, Password);

        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));
        Assert.Null(_limiter.GetRemainingLockout(Email));
    }

    [Fact]
    public async Task Login_ShouldNotLock_BeforeTheFifthFailure_AndSuccessClearsTheCount()
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(Email, "wrong-password"));
        }

        await LoginAsync(Email, Password);

        // The count started over: four more typos still do not lock the account.
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(Email, "wrong-password"));
        }

        await LoginAsync(Email, Password);
    }

    [Fact]
    public async Task Login_ShouldThrottleUnknownEmails_ExactlyLikeExistingAccounts()
    {
        const string unknown = "ghost@test.com";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failure = await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(unknown, "wrong-password"));
            Assert.Equal("invalid_credentials", failure.ErrorCode);
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() => LoginAsync(unknown, "wrong-password"));

        // Another account is not affected.
        await LoginAsync(Email, Password);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(7, 4)]
    [InlineData(8, 8)]
    [InlineData(9, 15)]
    [InlineData(30, 15)]
    public void Lockout_ShouldGrowWithEachFailure_UpToFifteenMinutes(int failures, int expectedMinutes)
    {
        for (var attempt = 0; attempt < failures; attempt++)
        {
            // Each new failure happens right after the previous lock ended.
            _clock.Advance(_limiter.GetRemainingLockout(Email) ?? TimeSpan.Zero);
            _limiter.RegisterFailure(Email);
        }

        Assert.Equal(
            expectedMinutes == 0 ? null : TimeSpan.FromMinutes(expectedMinutes),
            _limiter.GetRemainingLockout(Email));
    }

    [Fact]
    public void Limiter_ShouldIgnoreCaseAndSpaces_AndForgetOldFailures()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            _limiter.RegisterFailure(attempt % 2 == 0 ? "  USER@Test.com " : Email);
        }

        Assert.NotNull(_limiter.GetRemainingLockout(Email));

        // Fifteen quiet minutes after the lock ended the history is gone: one new failure does
        // not lock again.
        _clock.Advance(TimeSpan.FromMinutes(17));
        _limiter.RegisterFailure(Email);

        Assert.Null(_limiter.GetRemainingLockout(Email));
    }

    [Fact]
    public void Limiter_ShouldUnlock_AfterReset()
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            _limiter.RegisterFailure(Email);
        }

        _limiter.Reset(Email);

        Assert.Null(_limiter.GetRemainingLockout(Email));
    }

    private Task<AuthSessionDto> LoginAsync(string email, string password) =>
        new LoginUseCase(
                _userRepositoryMock.Object,
                _passwordHasher,
                SessionTestSupport.CreateSessionService(_refreshTokens, _clock),
                _limiter,
                Mock.Of<IUnitOfWork>())
            .ExecuteAsync(new LoginRequestDto(email, password));
}
