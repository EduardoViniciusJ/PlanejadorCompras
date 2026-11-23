using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class DeleteAccountUseCase(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IPasswordHasher passwordHasher,
    IAccountEraser accountEraser,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public const string ConfirmationWord = "EXCLUIR";

    public async Task ExecuteAsync(
        DeleteAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        if (!string.Equals(
                request.Confirmation?.Trim(),
                ConfirmationWord,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"Digite {ConfirmationWord} para confirmar a exclusão da conta.",
                "account_deletion_not_confirmed");
        }

        if (user.PasswordHash is not null
            && (string.IsNullOrEmpty(request.Password)
                || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash)))
        {
            throw new BadRequestException(
                "A senha não confere.",
                "invalid_current_password");
        }

        var plan = AccountDeletionPlan.From(
            await organizationRepository.GetMembershipsByUserIdAsync(user.Id, cancellationToken));

        if (!plan.CanDelete)
        {
            var names = string.Join(", ", plan.Blocking.Select(membership => membership.OrganizationName));

            throw new ConflictException(
                $"Você é a única pessoa administradora de: {names}. Promova outro administrador ou remova os demais membros antes de excluir a conta.",
                "account_deletion_sole_admin");
        }

        await accountEraser.EraseAsync(
            user.Id,
            plan.ToDelete.Select(membership => membership.OrganizationId).ToList(),
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }
}
