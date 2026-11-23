using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Organizations;
using PlanejadorCompras.Infrastructure.Persistence;
using PlanejadorCompras.Infrastructure.Repositories;

namespace PlanejadorCompras.Application.UnitTests.Support;

/// <summary>
/// In-memory database with the real organization repositories, for use cases
/// about teams, invitations and accounts.
/// </summary>
internal sealed class TeamWorld
{
    public static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly DbContextOptions<PlanejadorComprasDbContext> _options =
        new DbContextOptionsBuilder<PlanejadorComprasDbContext>()
            .UseInMemoryDatabase($"team-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    public TeamWorld()
    {
        Context = NewContext();
        Organizations = new OrganizationRepository(Context);
        Invitations = new OrganizationInvitationRepository(Context);
        Users = new UserRepository(Context);
        UserTokens = new UserTokenRepository(Context);
        UnitOfWork = new UnitOfWork(Context);
    }

    public PlanejadorComprasDbContext Context { get; }
    public OrganizationRepository Organizations { get; }
    public OrganizationInvitationRepository Invitations { get; }
    public UserRepository Users { get; }
    public UserTokenRepository UserTokens { get; }
    public UnitOfWork UnitOfWork { get; }
    public FixedTimeProvider Time { get; } = new(Now);
    public FakePasswordHasher PasswordHasher { get; } = new();

    public PlanejadorComprasDbContext NewContext(OrganizationContext? scope = null) =>
        new(_options, scope);

    public User AddUser(string email, string? password = "Senha@12345", string? name = null)
    {
        var user = password is null
            ? User.CreateGoogleUser($"google-{Guid.NewGuid():N}", email)
            : User.CreateManualUser(email, PasswordHasher.HashPassword(password));
        user.ConfirmEmail();
        user.UpdateName(name);
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    public Organization AddOrganization(string name, params (User User, OrganizationRole Role)[] members)
    {
        var organization = Organization.Create(name, Now.UtcDateTime);
        Context.Organizations.Add(organization);

        var joinedAt = Now.UtcDateTime;
        foreach (var (user, role) in members)
        {
            Context.Memberships.Add(Membership.Create(organization.Id, user.Id, role, joinedAt));
            joinedAt = joinedAt.AddMinutes(1);
        }

        Context.SaveChanges();
        return organization;
    }

    public OrganizationRole? RoleOf(User user, Organization organization) =>
        NewContext().Memberships
            .Where(membership => membership.UserId == user.Id
                                 && membership.OrganizationId == organization.Id)
            .Select(membership => (OrganizationRole?)membership.Role)
            .SingleOrDefault();

    public static ICurrentUser CurrentUser(User user)
    {
        var mock = new Mock<ICurrentUser>();
        mock.SetupGet(current => current.UserId).Returns(user.Id);
        mock.SetupGet(current => current.Email).Returns(user.Email);
        mock.SetupGet(current => current.Name).Returns(user.Name ?? user.Email);
        return mock.Object;
    }

    public static ICurrentOrganization In(Organization organization, OrganizationRole role) =>
        TestOrganization.For(organization.Id, role);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Deterministic, fast stand-in for BCrypt.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => $"hashed::{password}";

    public bool VerifyPassword(string password, string passwordHash) =>
        passwordHash == $"hashed::{password}";
}
