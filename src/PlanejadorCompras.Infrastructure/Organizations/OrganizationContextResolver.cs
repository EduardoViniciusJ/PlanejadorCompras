using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Organizations;

public interface IOrganizationContextResolver
{
    /// <summary>
    /// Loads the organization the user is working in and stores it in the
    /// request scope. Returns false when the account no longer exists.
    /// </summary>
    Task<bool> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class OrganizationContextResolver(
    PlanejadorComprasDbContext dbContext,
    OrganizationContext organizationContext,
    TimeProvider timeProvider) : IOrganizationContextResolver
{
    public async Task<bool> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        organizationContext.Clear();

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new
            {
                candidate.Email,
                candidate.Name,
                candidate.ActiveOrganizationId,
                candidate.DeletedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null || user.DeletedAtUtc.HasValue)
        {
            return false;
        }

        var memberships = await LoadMembershipsAsync(userId, cancellationToken);

        if (memberships.Count == 0)
        {
            // Every account works inside an organization. Accounts created by any
            // sign-up path get their personal one the first time they are seen.
            await ProvisionPersonalOrganizationAsync(userId, user.Name, user.Email, cancellationToken);
            memberships = await LoadMembershipsAsync(userId, cancellationToken);

            if (memberships.Count == 0)
            {
                return false;
            }
        }

        var active = memberships.FirstOrDefault(
                membership => membership.OrganizationId == user.ActiveOrganizationId)
            ?? memberships[0];

        organizationContext.Set(active.OrganizationId, active.Role, user.Name);
        return true;
    }

    private Task<List<MembershipRow>> LoadMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Memberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .OrderBy(membership => membership.CreatedAtUtc)
            .ThenBy(membership => membership.OrganizationId)
            .Select(membership => new MembershipRow(membership.OrganizationId, membership.Role))
            .ToListAsync(cancellationToken);

    private async Task ProvisionPersonalOrganizationAsync(
        Guid userId,
        string? userName,
        string email,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await AddPersonalOrganizationAsync(userId, userName, email, cancellationToken);
            return;
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            // Two first requests of the same new account may race; the lock makes
            // the second one wait and then find the membership already created.
            var lockKey = BitConverter.ToInt64(userId.ToByteArray(), 0);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockKey})",
                cancellationToken);

            var alreadyMember = await dbContext.Memberships
                .AnyAsync(membership => membership.UserId == userId, cancellationToken);

            if (!alreadyMember)
            {
                await AddPersonalOrganizationAsync(userId, userName, email, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task AddPersonalOrganizationAsync(
        Guid userId,
        string? userName,
        string email,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var organization = Organization.Create(
            Organization.BuildPersonalName(userName, email),
            nowUtc);

        dbContext.Organizations.Add(organization);
        dbContext.Memberships.Add(
            Membership.Create(organization.Id, userId, OrganizationRole.Admin, nowUtc));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record MembershipRow(Guid OrganizationId, OrganizationRole Role);
}
