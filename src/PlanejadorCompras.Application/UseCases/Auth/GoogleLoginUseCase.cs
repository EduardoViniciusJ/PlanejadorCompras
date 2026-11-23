using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.RefreshToken;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Auth;

public sealed class GoogleLoginUseCase
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly AuthSessionService _authSessionService;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public GoogleLoginUseCase(
        IGoogleTokenValidator googleTokenValidator,
        AuthSessionService authSessionService,
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _googleTokenValidator = googleTokenValidator;
        _authSessionService = authSessionService;
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthSessionDto> ExecuteAsync(
        GoogleLoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdToken);

        var googleUser = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);

        // First try to find by GoogleId
        var user = await _userRepository.GetByGoogleIdAsync(googleUser.GoogleId, cancellationToken);

        if (user is null)
        {
            // If not found by GoogleId, try to find by Email (Account Unification / Scenario A)
            user = await _userRepository.GetByEmailAsync(googleUser.Email, cancellationToken);

            if (user is not null)
            {
                // The account exists with a password. If its e-mail was never confirmed, the
                // password may belong to someone else who pre-registered the victim's address:
                // linking discards it, and every pending confirmation/reset token and session
                // goes with it.
                var discardedUnverifiedCredentials = user.LinkGoogleAccount(googleUser.GoogleId);

                if (discardedUnverifiedCredentials)
                {
                    await _userTokenRepository.RemoveAllByUserAsync(user.Id, cancellationToken);
                    await _refreshTokenRepository.RevokeAllForUserAsync(
                        user.Id,
                        RefreshTokenRevocationReason.CredentialsDiscarded,
                        _timeProvider.GetUtcNow().UtcDateTime,
                        cancellationToken);
                }

                _userRepository.Update(user);
            }
            else
            {
                // Create a completely new user
                user = User.CreateGoogleUser(googleUser.GoogleId, googleUser.Email);
                await _userRepository.AddAsync(user, cancellationToken);
            }
        }

        var session = await _authSessionService.StartAsync(user, googleUser.Name, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return session;
    }
}
