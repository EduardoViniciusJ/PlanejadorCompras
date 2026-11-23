using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UnitTests.UseCases.Auth.Sessions;
using PlanejadorCompras.Application.UseCases.Auth;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Infrastructure.Services;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Auth.GoogleLogin;

/// <summary>
/// Audit finding A1: an attacker pre-registers the victim's e-mail with a password of their own and
/// never confirms it. When the victim later signs in with Google, the account is linked. The
/// attacker's password must stop working at that moment.
/// </summary>
public sealed class GoogleAccountTakeoverTests
{
    private const string VictimEmail = "vitima@empresa.com";
    private const string AttackerPassword = "attacker-password-123";

    private readonly PasswordHasher _passwordHasher = new();
    private readonly Mock<IGoogleTokenValidator> _googleTokenValidatorMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserTokenRepository> _userTokenRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();

    public GoogleAccountTakeoverTests()
    {
        _googleTokenValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("google-victim", VictimEmail, "Vítima"));
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<GenerateAccessTokenRequestDto>()))
            .Returns(new GenerateAccessTokenResponseDto("jwt-token", DateTime.UtcNow.AddMinutes(15)));
    }

    [Fact]
    public async Task AttackerPassword_ShouldStopWorking_WhenVictimLinksGoogleToUnconfirmedAccount()
    {
        // 1. The attacker registers the victim's e-mail with their own password and never confirms it.
        var account = User.CreateManualUser(VictimEmail, _passwordHasher.HashPassword(AttackerPassword));
        SetupStoredAccount(account);

        // 2. The victim signs in with Google: the account is found by e-mail and linked.
        await CreateGoogleLoginUseCase().ExecuteAsync(new GoogleLoginRequestDto("victim-google-id-token"));

        Assert.Equal("google-victim", account.GoogleId);
        Assert.True(account.IsEmailConfirmed);
        Assert.Null(account.PasswordHash);
        _userTokenRepositoryMock.Verify(
            x => x.RemoveAllByUserAsync(account.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        // 3. The attacker tries the password they chose. It must be rejected.
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateLoginUseCase().ExecuteAsync(new LoginRequestDto(VictimEmail, AttackerPassword)));
        Assert.Equal("invalid_credentials", exception.ErrorCode);
    }

    [Fact]
    public async Task OwnerPassword_ShouldKeepWorking_WhenLinkingGoogleToConfirmedAccount()
    {
        const string ownerPassword = "owner-password-123";
        var account = User.CreateManualUser(VictimEmail, _passwordHasher.HashPassword(ownerPassword));
        account.ConfirmEmail();
        SetupStoredAccount(account);

        await CreateGoogleLoginUseCase().ExecuteAsync(new GoogleLoginRequestDto("owner-google-id-token"));

        Assert.Equal("google-victim", account.GoogleId);
        Assert.NotNull(account.PasswordHash);
        _userTokenRepositoryMock.Verify(
            x => x.RemoveAllByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        await CreateLoginUseCase().ExecuteAsync(new LoginRequestDto(VictimEmail, ownerPassword));
    }

    private void SetupStoredAccount(User account)
    {
        _userRepositoryMock
            .Setup(x => x.GetByGoogleIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(VictimEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
    }

    private GoogleLoginUseCase CreateGoogleLoginUseCase() => new(
        _googleTokenValidatorMock.Object,
        SessionTestSupport.CreateSessionService(_refreshTokens, TimeProvider.System, _jwtTokenServiceMock.Object),
        _userRepositoryMock.Object,
        _userTokenRepositoryMock.Object,
        _refreshTokens,
        TimeProvider.System,
        _unitOfWorkMock.Object);

    private LoginUseCase CreateLoginUseCase() => new(
        _userRepositoryMock.Object,
        _passwordHasher,
        SessionTestSupport.CreateSessionService(_refreshTokens, TimeProvider.System, _jwtTokenServiceMock.Object),
        new InMemoryLoginAttemptLimiter(TimeProvider.System),
        _unitOfWorkMock.Object);
}
