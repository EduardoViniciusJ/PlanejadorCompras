using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class ChangePasswordUseCase(
    IUserRepository userRepository,
    IUserTokenRepository userTokenRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        if (user.PasswordHash is null)
        {
            // Setting a first password from an open session alone would let
            // whoever holds that session keep access; the e-mail flow proves
            // ownership of the address instead.
            throw new BadRequestException(
                "Sua conta entra pelo Google e ainda não tem senha. Use \"Esqueci minha senha\" na tela de login para criar uma.",
                "password_not_set");
        }

        // 400 rather than 401: the session is valid, only the typed password is wrong.
        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new BadRequestException(
                "A senha atual não confere.",
                "invalid_current_password");
        }

        if (passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            throw new BadRequestException(
                "A nova senha precisa ser diferente da atual.",
                "password_unchanged");
        }

        user.UpdatePassword(passwordHasher.HashPassword(request.NewPassword));

        // A reset link requested before the change must not outlive it.
        await userTokenRepository.RemoveByUserAndTypeAsync(
            user.Id,
            "PasswordReset",
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}
