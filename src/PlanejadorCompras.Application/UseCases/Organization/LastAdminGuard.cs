using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Organization;

internal static class LastAdminGuard
{
    public const string ErrorCode = "organization_last_admin";

    /// <summary>
    /// An organization must always keep at least one administrator, otherwise
    /// nobody could manage the team or invite people again.
    /// </summary>
    public static async Task EnsureAnotherAdminAsync(
        IOrganizationRepository organizationRepository,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var adminCount = await organizationRepository.CountAdminsAsync(
            organizationId,
            cancellationToken);

        if (adminCount <= 1)
        {
            throw new ConflictException(
                "A organização precisa ter pelo menos um administrador. Promova outra pessoa antes.",
                ErrorCode);
        }
    }
}
