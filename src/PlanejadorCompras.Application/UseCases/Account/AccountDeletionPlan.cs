using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;

namespace PlanejadorCompras.Application.UseCases.Account;

/// <summary>
/// Decides what happens to each organization when a member deletes the account:
/// <list type="bullet">
/// <item>only member: the organization and all its data are deleted;</item>
/// <item>only administrator of a shared organization: deletion is blocked until
/// another administrator is appointed (or the other members are removed);</item>
/// <item>otherwise: the user just leaves and the data stays with the organization.</item>
/// </list>
/// </summary>
public sealed record AccountDeletionPlan(
    IReadOnlyList<UserMembership> ToDelete,
    IReadOnlyList<UserMembership> ToLeave,
    IReadOnlyList<UserMembership> Blocking)
{
    public bool CanDelete => Blocking.Count == 0;

    public static AccountDeletionPlan From(IEnumerable<UserMembership> memberships)
    {
        var toDelete = new List<UserMembership>();
        var toLeave = new List<UserMembership>();
        var blocking = new List<UserMembership>();

        foreach (var membership in memberships)
        {
            if (membership.MemberCount <= 1)
            {
                toDelete.Add(membership);
            }
            else if (membership.Role == OrganizationRole.Admin && membership.AdminCount <= 1)
            {
                blocking.Add(membership);
            }
            else
            {
                toLeave.Add(membership);
            }
        }

        return new AccountDeletionPlan(toDelete, toLeave, blocking);
    }
}
