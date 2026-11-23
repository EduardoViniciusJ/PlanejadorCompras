using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class OrganizationRepository(PlanejadorComprasDbContext context)
    : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Organizations.FirstOrDefaultAsync(
            organization => organization.Id == id,
            cancellationToken);

    public async Task AddAsync(
        Organization organization,
        CancellationToken cancellationToken = default) =>
        await context.Organizations.AddAsync(organization, cancellationToken);

    public Task<Membership?> GetMembershipAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.Memberships.FirstOrDefaultAsync(
            membership => membership.OrganizationId == organizationId
                          && membership.UserId == userId,
            cancellationToken);

    public Task<List<UserMembership>> GetMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (from membership in context.Memberships.AsNoTracking()
         join organization in context.Organizations.AsNoTracking()
             on membership.OrganizationId equals organization.Id
         where membership.UserId == userId
         orderby membership.CreatedAtUtc, organization.Id
         select new UserMembership(
             organization.Id,
             organization.Name,
             membership.Role,
             membership.CreatedAtUtc,
             context.Memberships.Count(other => other.OrganizationId == organization.Id),
             context.Memberships.Count(other =>
                 other.OrganizationId == organization.Id
                 && other.Role == OrganizationRole.Admin)))
        .ToListAsync(cancellationToken);

    public Task<List<OrganizationMember>> GetMembersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        (from membership in context.Memberships.AsNoTracking()
         join user in context.Users.AsNoTracking()
             on membership.UserId equals user.Id
         where membership.OrganizationId == organizationId
         orderby membership.CreatedAtUtc, user.Email
         select new OrganizationMember(
             user.Id,
             user.Email,
             user.Name,
             membership.Role,
             membership.CreatedAtUtc))
        .ToListAsync(cancellationToken);

    public Task<int> CountAdminsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        context.Memberships.CountAsync(
            membership => membership.OrganizationId == organizationId
                          && membership.Role == OrganizationRole.Admin,
            cancellationToken);

    public async Task AddMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken = default) =>
        await context.Memberships.AddAsync(membership, cancellationToken);

    public void RemoveMembership(Membership membership) =>
        context.Memberships.Remove(membership);
}

public sealed class OrganizationInvitationRepository(PlanejadorComprasDbContext context)
    : IOrganizationInvitationRepository
{
    public Task<OrganizationInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        context.OrganizationInvitations.FirstOrDefaultAsync(
            invitation => invitation.TokenHash == tokenHash,
            cancellationToken);

    public Task<OrganizationInvitation?> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.OrganizationInvitations.FirstOrDefaultAsync(
            invitation => invitation.Id == id
                          && invitation.OrganizationId == organizationId,
            cancellationToken);

    public Task<OrganizationInvitation?> GetByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = OrganizationInvitation.NormalizeEmail(email);

        return context.OrganizationInvitations.FirstOrDefaultAsync(
            invitation => invitation.OrganizationId == organizationId
                          && invitation.Email == normalizedEmail,
            cancellationToken);
    }

    public Task<List<PendingInvitation>> GetPendingAsync(
        Guid organizationId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default) =>
        (from invitation in context.OrganizationInvitations.AsNoTracking()
         join inviter in context.Users.AsNoTracking()
             on invitation.InvitedByUserId equals inviter.Id
         where invitation.OrganizationId == organizationId
               && invitation.ExpiresAtUtc >= nowUtc
         orderby invitation.CreatedAtUtc descending
         select new PendingInvitation(
             invitation.Id,
             invitation.Email,
             invitation.Role,
             invitation.CreatedAtUtc,
             invitation.ExpiresAtUtc,
             inviter.Name ?? inviter.Email))
        .ToListAsync(cancellationToken);

    public Task<int> CountAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        context.OrganizationInvitations.CountAsync(
            invitation => invitation.OrganizationId == organizationId,
            cancellationToken);

    public async Task AddAsync(
        OrganizationInvitation invitation,
        CancellationToken cancellationToken = default) =>
        await context.OrganizationInvitations.AddAsync(invitation, cancellationToken);

    public void Remove(OrganizationInvitation invitation) =>
        context.OrganizationInvitations.Remove(invitation);
}
