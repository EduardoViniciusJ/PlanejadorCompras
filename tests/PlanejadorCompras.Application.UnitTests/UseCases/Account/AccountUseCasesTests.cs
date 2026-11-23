using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Features.Authentication.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UnitTests.Support;
using PlanejadorCompras.Application.UseCases.Account;
using PlanejadorCompras.Application.UseCases.Auth;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.Organization;
using PlanejadorCompras.Infrastructure.Organizations;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Account;

public sealed class AccountUseCasesTests
{
    private readonly TeamWorld _world = new();

    // -------------------------------------------------------------- profile

    [Fact]
    public async Task GetAccount_ShouldReturnProfileOrganizationsAndPermissionsOfTheActiveRole()
    {
        var user = _world.AddUser("ana@empresa.com", name: "Ana");
        var personal = _world.AddOrganization("Organização de ana", (user, OrganizationRole.Admin));
        var shared = _world.AddOrganization("Mercado", (user, OrganizationRole.Approver));

        var account = await new GetAccountUseCase(
                _world.Users,
                _world.Organizations,
                TeamWorld.CurrentUser(user),
                TeamWorld.In(shared, OrganizationRole.Approver))
            .ExecuteAsync();

        Assert.Equal("Ana", account.Name);
        Assert.True(account.HasPassword);
        Assert.False(account.HasGoogleLogin);
        Assert.Equal(shared.Id, account.ActiveOrganization.Id);
        Assert.Equal("approver", account.ActiveOrganization.Role);
        Assert.Equal(2, account.Organizations.Count);
        Assert.Contains(account.Organizations, organization => organization.Id == personal.Id && !organization.IsActive);
        Assert.Equal(["viewData", "approvePurchaseOrders", "viewTeam"], account.Permissions);
    }

    [Fact]
    public async Task UpdateProfile_ShouldSaveTheTrimmedName()
    {
        var user = _world.AddUser("ana@empresa.com");

        await new UpdateProfileUseCase(_world.Users, TeamWorld.CurrentUser(user), _world.UnitOfWork)
            .ExecuteAsync(new UpdateProfileRequestDto("  Ana Souza  "));

        Assert.Equal("Ana Souza", _world.NewContext().Users.Single().Name);
    }

    // ------------------------------------------------------------- password

    [Fact]
    public async Task ChangePassword_ShouldRequireTheCurrentPassword()
    {
        var user = _world.AddUser("ana@empresa.com", "Senha@12345");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            ChangePassword(user).ExecuteAsync(new ChangePasswordRequestDto("errada", "NovaSenha@1")));

        Assert.Equal("invalid_current_password", exception.ErrorCode);
        Assert.Equal("hashed::Senha@12345", _world.NewContext().Users.Single().PasswordHash);
    }

    [Fact]
    public async Task ChangePassword_ShouldStoreTheNewHash_AndDropPendingResetLinks()
    {
        var user = _world.AddUser("ana@empresa.com", "Senha@12345");
        _world.Context.UserTokens.Add(UserToken.Create(user.Id, "PasswordReset", TimeSpan.FromHours(1)));
        _world.Context.SaveChanges();

        await ChangePassword(user).ExecuteAsync(new ChangePasswordRequestDto("Senha@12345", "NovaSenha@1"));

        Assert.Equal("hashed::NovaSenha@1", _world.NewContext().Users.Single().PasswordHash);
        Assert.Empty(_world.NewContext().UserTokens);
    }

    [Fact]
    public async Task ChangePassword_ShouldRejectReusingTheSamePassword()
    {
        var user = _world.AddUser("ana@empresa.com", "Senha@12345");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            ChangePassword(user).ExecuteAsync(new ChangePasswordRequestDto("Senha@12345", "Senha@12345")));

        Assert.Equal("password_unchanged", exception.ErrorCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldNotSetAFirstPasswordForAGoogleOnlyAccount()
    {
        var user = _world.AddUser("google@empresa.com", password: null);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            ChangePassword(user).ExecuteAsync(new ChangePasswordRequestDto("qualquer", "NovaSenha@1")));

        Assert.Equal("password_not_set", exception.ErrorCode);
        Assert.Null(_world.NewContext().Users.Single().PasswordHash);
    }

    // ------------------------------------------------- switching organization

    [Fact]
    public async Task SwitchOrganization_ShouldOnlyAcceptOrganizationsTheUserBelongsTo()
    {
        var user = _world.AddUser("ana@empresa.com");
        var stranger = _world.AddUser("fora@outra.com");
        var own = _world.AddOrganization("Minha", (user, OrganizationRole.Admin));
        var second = _world.AddOrganization("Segunda", (user, OrganizationRole.Buyer));
        var foreign = _world.AddOrganization("Alheia", (stranger, OrganizationRole.Admin));
        var useCase = new SwitchOrganizationUseCase(
            _world.Users,
            _world.Organizations,
            TeamWorld.CurrentUser(user),
            _world.UnitOfWork);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(new SwitchOrganizationRequestDto(foreign.Id)));
        Assert.Equal("organization_not_found", exception.ErrorCode);
        Assert.Null(_world.NewContext().Users.Single(row => row.Id == user.Id).ActiveOrganizationId);

        await useCase.ExecuteAsync(new SwitchOrganizationRequestDto(second.Id));
        Assert.Equal(second.Id, _world.NewContext().Users.Single(row => row.Id == user.Id).ActiveOrganizationId);
        Assert.NotEqual(own.Id, second.Id);
    }

    // ------------------------------------------- resolving the organization

    [Fact]
    public async Task Resolver_ShouldGiveANewAccountItsPersonalOrganizationAsAdministrator()
    {
        var user = _world.AddUser("nova.pessoa@empresa.com");
        var scope = new OrganizationContext();

        var resolved = await Resolver(scope).ResolveAsync(user.Id);

        Assert.True(resolved);
        var membership = _world.NewContext().Memberships.Single(row => row.UserId == user.Id);
        Assert.Equal(OrganizationRole.Admin, membership.Role);
        Assert.Equal(membership.OrganizationId, scope.OrganizationId);
        Assert.Equal(OrganizationRole.Admin, scope.Role);
        Assert.Equal(
            "Organização de nova.pessoa",
            _world.NewContext().Organizations.Single(row => row.Id == membership.OrganizationId).Name);

        // Resolving again must not create a second organization.
        await Resolver(new OrganizationContext()).ResolveAsync(user.Id);
        Assert.Single(_world.NewContext().Memberships.Where(row => row.UserId == user.Id));
    }

    [Fact]
    public async Task Resolver_ShouldUseTheActiveOrganization_AndFallBackToTheOldestMembership()
    {
        var user = _world.AddUser("ana@empresa.com");
        var first = _world.AddOrganization("Primeira", (user, OrganizationRole.Admin));
        _world.Time.Now = TeamWorld.Now.AddDays(1);
        var second = _world.AddOrganization("Segunda", (user, OrganizationRole.Requester));
        // AddOrganization stamps memberships from TeamWorld.Now; make "second" newer.
        var newer = _world.Context.Memberships.Single(row => row.OrganizationId == second.Id);
        _world.Context.Entry(newer).Property(row => row.CreatedAtUtc).CurrentValue =
            TeamWorld.Now.UtcDateTime.AddDays(1);
        _world.Context.SaveChanges();

        var scope = new OrganizationContext();
        await Resolver(scope).ResolveAsync(user.Id);
        Assert.Equal(first.Id, scope.OrganizationId);

        user.SetActiveOrganization(second.Id);
        _world.Context.SaveChanges();
        await Resolver(scope).ResolveAsync(user.Id);
        Assert.Equal(second.Id, scope.OrganizationId);
        Assert.Equal(OrganizationRole.Requester, scope.Role);

        // Removed from the active organization: the stale pointer is ignored.
        _world.Context.Memberships.Remove(newer);
        _world.Context.SaveChanges();
        await Resolver(scope).ResolveAsync(user.Id);
        Assert.Equal(first.Id, scope.OrganizationId);
    }

    [Fact]
    public async Task Resolver_ShouldRejectUnknownAndErasedAccounts()
    {
        var erased = _world.AddUser("apagada@empresa.com");
        erased.Anonymize(TeamWorld.Now.UtcDateTime);
        _world.Context.SaveChanges();
        var scope = new OrganizationContext();

        Assert.False(await Resolver(scope).ResolveAsync(Guid.NewGuid()));
        Assert.False(await Resolver(scope).ResolveAsync(erased.Id));
        Assert.False(scope.IsResolved);
        Assert.Empty(_world.NewContext().Memberships);
    }

    // ------------------------------------------------------ account deletion

    [Fact]
    public void DeletionPlan_ShouldDeleteSoloOrganizations_LeaveSharedOnes_AndBlockOnSoleAdmin()
    {
        var solo = Membership("Só eu", OrganizationRole.Admin, members: 1, admins: 1);
        var soleAdmin = Membership("Sou o único admin", OrganizationRole.Admin, members: 3, admins: 1);
        var coAdmin = Membership("Outro admin existe", OrganizationRole.Admin, members: 2, admins: 2);
        var member = Membership("Sou comprador", OrganizationRole.Buyer, members: 4, admins: 1);

        var plan = AccountDeletionPlan.From([solo, soleAdmin, coAdmin, member]);

        Assert.Equal([solo], plan.ToDelete);
        Assert.Equal([coAdmin, member], plan.ToLeave);
        Assert.Equal([soleAdmin], plan.Blocking);
        Assert.False(plan.CanDelete);
        Assert.True(AccountDeletionPlan.From([solo, coAdmin, member]).CanDelete);
    }

    [Fact]
    public async Task DeleteAccount_ShouldEraseSoloOrganizations_WhenPasswordAndConfirmationMatch()
    {
        var user = _world.AddUser("ana@empresa.com", "Senha@12345");
        var colleague = _world.AddUser("colega@empresa.com");
        var solo = _world.AddOrganization("Organização de ana", (user, OrganizationRole.Admin));
        _world.AddOrganization("Mercado", (colleague, OrganizationRole.Admin), (user, OrganizationRole.Buyer));
        var eraser = new Mock<IAccountEraser>();

        await DeleteAccount(user, eraser).ExecuteAsync(new DeleteAccountRequestDto("excluir", "Senha@12345"));

        eraser.Verify(
            service => service.EraseAsync(
                user.Id,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(solo.Id)),
                TeamWorld.Now.UtcDateTime,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("EXCLUIR", "errada", "invalid_current_password")]
    [InlineData("EXCLUIR", null, "invalid_current_password")]
    [InlineData("sim", "Senha@12345", "account_deletion_not_confirmed")]
    public async Task DeleteAccount_ShouldRequirePasswordAndConfirmation(
        string confirmation,
        string? password,
        string expectedErrorCode)
    {
        var user = _world.AddUser("ana@empresa.com", "Senha@12345");
        _world.AddOrganization("Organização de ana", (user, OrganizationRole.Admin));
        var eraser = new Mock<IAccountEraser>();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            DeleteAccount(user, eraser).ExecuteAsync(new DeleteAccountRequestDto(confirmation, password)));

        Assert.Equal(expectedErrorCode, exception.ErrorCode);
        eraser.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAccount_ShouldBeBlocked_WhileTheUserIsTheOnlyAdministratorOfASharedOrganization()
    {
        var user = _world.AddUser("ana@empresa.com", "Senha@12345");
        var colleague = _world.AddUser("colega@empresa.com");
        _world.AddOrganization("Mercado", (user, OrganizationRole.Admin), (colleague, OrganizationRole.Buyer));
        var eraser = new Mock<IAccountEraser>();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            DeleteAccount(user, eraser).ExecuteAsync(new DeleteAccountRequestDto("EXCLUIR", "Senha@12345")));

        Assert.Equal("account_deletion_sole_admin", exception.ErrorCode);
        Assert.Contains("Mercado", exception.Message);
        eraser.VerifyNoOtherCalls();

        var preview = await new GetAccountDeletionPreviewUseCase(
                _world.Users,
                _world.Organizations,
                TeamWorld.CurrentUser(user))
            .ExecuteAsync();
        Assert.False(preview.CanDelete);
        Assert.True(preview.RequiresPassword);
        Assert.Equal("Mercado", Assert.Single(preview.BlockingOrganizations).Name);
    }

    [Fact]
    public async Task DeleteAccount_ShouldNotAskAPasswordFromAGoogleOnlyAccount()
    {
        var user = _world.AddUser("google@empresa.com", password: null);
        _world.AddOrganization("Organização de google", (user, OrganizationRole.Admin));
        var eraser = new Mock<IAccountEraser>();

        await DeleteAccount(user, eraser).ExecuteAsync(new DeleteAccountRequestDto("EXCLUIR"));

        eraser.Verify(
            service => service.EraseAsync(
                user.Id,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ----------------------------------------------------- terms at sign-up

    [Fact]
    public async Task Register_ShouldRefuseWithoutAcceptingTheTerms()
    {
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            Register().ExecuteAsync(new RegisterRequestDto("novo@empresa.com", "Senha@12345")));

        Assert.Equal("terms_not_accepted", exception.ErrorCode);
        Assert.Empty(_world.NewContext().Users);
    }

    [Fact]
    public async Task Register_ShouldRecordWhenAndWhichTermsWereAccepted()
    {
        var before = DateTime.UtcNow;

        await Register().ExecuteAsync(
            new RegisterRequestDto("novo@empresa.com", "Senha@12345", acceptedTerms: true, name: "Nina"));

        var user = _world.NewContext().Users.Single();
        Assert.Equal("Nina", user.Name);
        Assert.Equal(global::PlanejadorCompras.Domain.Rules.OrganizationRules.CurrentTermsVersion, user.TermsVersion);
        Assert.InRange(user.TermsAcceptedAtUtc!.Value, before, DateTime.UtcNow);
    }

    // -------------------------------------------------------------- helpers

    private ChangePasswordUseCase ChangePassword(User user) =>
        new(
            _world.Users,
            _world.UserTokens,
            _world.PasswordHasher,
            TeamWorld.CurrentUser(user),
            _world.UnitOfWork);

    private OrganizationContextResolver Resolver(OrganizationContext scope) =>
        new(_world.NewContext(), scope, _world.Time);

    private DeleteAccountUseCase DeleteAccount(User user, Mock<IAccountEraser> eraser) =>
        new(
            _world.Users,
            _world.Organizations,
            _world.PasswordHasher,
            eraser.Object,
            TeamWorld.CurrentUser(user),
            _world.Time);

    private RegisterUseCase Register()
    {
        var frontendUrls = new Mock<IFrontendUrlService>();
        frontendUrls
            .Setup(service => service.BuildEmailConfirmationUrl(It.IsAny<string>()))
            .Returns("https://app.exemplo.com/confirmar-email?token=x");

        return new RegisterUseCase(
            _world.Users,
            _world.UserTokens,
            _world.PasswordHasher,
            new Mock<IEmailService>().Object,
            frontendUrls.Object,
            _world.UnitOfWork);
    }

    private static UserMembership Membership(string name, OrganizationRole role, int members, int admins) =>
        new(Guid.NewGuid(), name, role, TeamWorld.Now.UtcDateTime, members, admins);
}
