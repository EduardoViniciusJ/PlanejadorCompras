using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.RefreshToken;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Application.UseCases.Auth;

public sealed class ResetPasswordUseCase
{
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILoginAttemptLimiter _loginAttemptLimiter;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordUseCase(
        IUserTokenRepository userTokenRepository,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ILoginAttemptLimiter loginAttemptLimiter,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _userTokenRepository = userTokenRepository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _loginAttemptLimiter = loginAttemptLimiter;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var userToken = await _userTokenRepository.GetByTokenHashAsync(
            SecretToken.Hash(request.Token),
            "PasswordReset",
            cancellationToken);

        if (userToken is null || !userToken.IsValid())
        {
            throw new BadRequestException("Token inválido ou expirado.", "invalid_or_expired_token");
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        userToken.User.UpdatePassword(newPasswordHash);
        userToken.User.ConfirmEmail();

        // The reset link proves ownership of the e-mail, so the confirmation token is no longer
        // needed either: every pending token of the user is removed.
        await _userTokenRepository.RemoveAllByUserAsync(userToken.UserId, cancellationToken);

        // Whoever knew the old password (or stole a session) must not stay logged in.
        await _refreshTokenRepository.RevokeAllForUserAsync(
            userToken.UserId,
            RefreshTokenRevocationReason.PasswordReset,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        _userRepository.Update(userToken.User);

        await _unitOfWork.CommitAsync(cancellationToken);

        // The owner proved control of the e-mail: a lockout caused by someone guessing the old
        // password must not keep them out.
        _loginAttemptLimiter.Reset(userToken.User.Email);
    }
}
