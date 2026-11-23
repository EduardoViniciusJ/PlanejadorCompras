using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Auth;

public sealed class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthSessionService _authSessionService;
    private readonly ILoginAttemptLimiter _loginAttemptLimiter;
    private readonly IUnitOfWork _unitOfWork;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AuthSessionService authSessionService,
        ILoginAttemptLimiter loginAttemptLimiter,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authSessionService = authSessionService;
        _loginAttemptLimiter = loginAttemptLimiter;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthSessionDto> ExecuteAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        // Checked before touching the password: while an account is locked not even the right
        // password is accepted, otherwise the lock would not slow down guessing at all. The
        // answer is the same for accounts that do not exist.
        if (_loginAttemptLimiter.GetRemainingLockout(request.Email) is { } remainingLockout)
        {
            throw new TooManyRequestsException(
                "Muitas tentativas de login para esta conta. Aguarde alguns minutos e tente novamente, ou redefina sua senha.",
                "too_many_login_attempts",
                remainingLockout);
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || user.PasswordHash is null)
        {
            // Keep the expensive password work on failed lookups to reduce timing-based enumeration.
            _passwordHasher.HashPassword(request.Password);
            _loginAttemptLimiter.RegisterFailure(request.Email);
            throw new UnauthorizedException("E-mail ou senha inválidos.", "invalid_credentials");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _loginAttemptLimiter.RegisterFailure(request.Email);
            throw new UnauthorizedException("E-mail ou senha inválidos.", "invalid_credentials");
        }

        _loginAttemptLimiter.Reset(request.Email);

        if (!user.IsEmailConfirmed)
        {
            throw new ForbiddenException(
                "Por favor, confirme seu e-mail antes de fazer login.",
                "email_not_confirmed");
        }

        // The User entity has no name, so the e-mail is what the UI shows for password accounts.
        var session = await _authSessionService.StartAsync(user, user.Email, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return session;
    }
}
