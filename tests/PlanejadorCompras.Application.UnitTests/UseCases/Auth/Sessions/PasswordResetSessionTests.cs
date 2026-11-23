using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.UseCases.Auth;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Domain.Security;
using PlanejadorCompras.Infrastructure.Services;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Auth.Sessions;

public sealed class PasswordResetSessionTests
{
    private readonly AdjustableTimeProvider _clock = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly Mock<IUserTokenRepository> _userTokenRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public async Task ResetPassword_ShouldRevokeEverySessionOfTheUser()
    {
        var user = User.CreateManualUser("user@test.com", _passwordHasher.HashPassword("old-password-123"));
        user.ConfirmEmail();
        _refreshTokens.RegisterUser(user);
        var sessions = SessionTestSupport.CreateSessionService(_refreshTokens, _clock);
        var laptop = await sessions.StartAsync(user, user.Email);
        var stolen = await sessions.StartAsync(user, user.Email);

        var (resetToken, rawResetToken) = UserToken.Create(user.Id, "PasswordReset", TimeSpan.FromHours(1));
        typeof(UserToken).GetProperty(nameof(UserToken.User))!.SetValue(resetToken, user);
        _userTokenRepositoryMock
            .Setup(x => x.GetByTokenHashAsync(SecretToken.Hash(rawResetToken), "PasswordReset", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);

        await new ResetPasswordUseCase(
                _userTokenRepositoryMock.Object,
                _userRepositoryMock.Object,
                _refreshTokens,
                _passwordHasher,
                new InMemoryLoginAttemptLimiter(_clock),
                _clock,
                _unitOfWorkMock.Object)
            .ExecuteAsync(new ResetPasswordRequestDto(rawResetToken, "new-password-456"));

        Assert.All(_refreshTokens.Tokens, token =>
            Assert.Equal(RefreshTokenRevocationReason.PasswordReset, token.RevocationReason));
        _userTokenRepositoryMock.Verify(
            x => x.RemoveAllByUserAsync(user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        var refresh = new RefreshSessionUseCase(
            _refreshTokens,
            sessions,
            SessionTestSupport.Settings,
            _clock,
            _unitOfWorkMock.Object);
        await Assert.ThrowsAsync<UnauthorizedException>(() => refresh.ExecuteAsync(laptop.RefreshToken));
        await Assert.ThrowsAsync<UnauthorizedException>(() => refresh.ExecuteAsync(stolen.RefreshToken));
    }

    [Fact]
    public async Task ResetPassword_ShouldLookTheTokenUpByItsHash()
    {
        // The repository only knows hashes: a raw token that was never issued finds nothing.
        _userTokenRepositoryMock
            .Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserToken?)null);

        var useCase = new ResetPasswordUseCase(
            _userTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _refreshTokens,
            _passwordHasher,
            new InMemoryLoginAttemptLimiter(_clock),
            _clock,
            _unitOfWorkMock.Object);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            useCase.ExecuteAsync(new ResetPasswordRequestDto("ABCDEF", "new-password-456")));

        Assert.Equal("invalid_or_expired_token", exception.ErrorCode);
        _userTokenRepositoryMock.Verify(
            x => x.GetByTokenHashAsync(SecretToken.Hash("ABCDEF"), "PasswordReset", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
