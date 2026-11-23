using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class UpdateProfileUseCase(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        user.UpdateName(request.Name);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
