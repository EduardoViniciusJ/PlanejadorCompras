using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.Account;

public sealed class ExportAccountDataUseCase(
    IAccountDataExportQuery exportQuery,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<AccountDataExportDto> ExecuteAsync(CancellationToken cancellationToken = default) =>
        await exportQuery.GetAsync(
            currentUser.UserId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken)
        ?? throw new UnauthorizedException("User is not authenticated.");
}
