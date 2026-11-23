using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Services;

/// <summary>
/// Erases an account in one transaction. Runs across organizations on purpose,
/// so every statement names the organization or the user explicitly and
/// bypasses the tenant filter with <c>IgnoreQueryFilters</c>.
/// </summary>
public sealed class AccountEraser(PlanejadorComprasDbContext context) : IAccountEraser
{
    public const string RemovedUserName = "Usuário removido";
    public const string RemovedUserEmail = "removido@conta-excluida.invalid";

    public async Task EraseAsync(
        Guid userId,
        IReadOnlyCollection<Guid> organizationIdsToDelete,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(organizationIdsToDelete);

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database
                .BeginTransactionAsync(cancellationToken);

            var user = await context.Users
                .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

            if (user is null)
            {
                return;
            }

            var email = user.Email;

            foreach (var organizationId in organizationIdsToDelete.Distinct())
            {
                await DeleteOrganizationAsync(organizationId, cancellationToken);
            }

            await AnonymizeDocumentsAsync(userId, cancellationToken);

            await context.Memberships
                .Where(membership => membership.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await context.OrganizationInvitations
                .Where(invitation => invitation.InvitedByUserId == userId
                                     || invitation.Email == email)
                .ExecuteDeleteAsync(cancellationToken);
            await context.UserTokens
                .Where(token => token.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            if (await IsReferencedByDocumentsAsync(userId, cancellationToken))
            {
                // Documents of organizations that still have other members keep
                // pointing to this row, so it stays as an anonymous tombstone.
                user.Anonymize(nowUtc);
                await context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task DeleteOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        // Children first: prices and list links reference suppliers with RESTRICT.
        await context.ItemQuotes.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.ShoppingListSuppliers.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.ShoppingItems.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.ShoppingLists.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.Suppliers.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.PurchaseOrders.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.QuotationRequests.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.SavedEqualizations.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);

        // Memberships, invitations and counters go with the organization (cascade).
        await context.Organizations
            .Where(organization => organization.Id == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task AnonymizeDocumentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await context.QuotationRequests.IgnoreQueryFilters()
            .Where(request => request.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(request => request.BuyerName, RemovedUserName)
                    .SetProperty(request => request.BuyerEmail, RemovedUserEmail),
                cancellationToken);
        await context.PurchaseOrders.IgnoreQueryFilters()
            .Where(order => order.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(order => order.BuyerName, RemovedUserName)
                    .SetProperty(order => order.BuyerEmail, (string?)null),
                cancellationToken);
        await context.SavedEqualizations.IgnoreQueryFilters()
            .Where(equalization => equalization.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(equalization => equalization.CreatedByName, RemovedUserName)
                    .SetProperty(equalization => equalization.CreatedByEmail, RemovedUserEmail),
                cancellationToken);
    }

    private async Task<bool> IsReferencedByDocumentsAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.ShoppingLists.IgnoreQueryFilters()
            .AnyAsync(row => row.UserId == userId, cancellationToken)
        || await context.Suppliers.IgnoreQueryFilters()
            .AnyAsync(row => row.UserId == userId, cancellationToken)
        || await context.QuotationRequests.IgnoreQueryFilters()
            .AnyAsync(row => row.UserId == userId, cancellationToken)
        || await context.PurchaseOrders.IgnoreQueryFilters()
            .AnyAsync(row => row.UserId == userId, cancellationToken)
        || await context.SavedEqualizations.IgnoreQueryFilters()
            .AnyAsync(row => row.UserId == userId, cancellationToken);
}
