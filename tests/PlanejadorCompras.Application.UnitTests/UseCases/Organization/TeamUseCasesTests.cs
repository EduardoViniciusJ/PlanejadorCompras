using OrganizationEntity = PlanejadorCompras.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UnitTests.Support;
using PlanejadorCompras.Application.UseCases.Organization;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Organization;

public sealed class TeamUseCasesTests
{
    private readonly TeamWorld _world = new();
    private readonly User _admin;
    private readonly User _buyer;
    private readonly User _outsider;
    private readonly OrganizationEntity _organization;
    private readonly OrganizationEntity _otherOrganization;

    public TeamUseCasesTests()
    {
        _admin = _world.AddUser("admin@empresa.com", name: "Ana Admin");
        _buyer = _world.AddUser("comprador@empresa.com");
        _outsider = _world.AddUser("fora@outra.com");
        _organization = _world.AddOrganization(
            "Mercado Bom Preço",
            (_admin, OrganizationRole.Admin),
            (_buyer, OrganizationRole.Buyer));
        _otherOrganization = _world.AddOrganization("Outra Empresa", (_outsider, OrganizationRole.Admin));
    }

    // ------------------------------------------------------------- members

    [Fact]
    public async Task GetMembers_ShouldListOnlyMembersOfTheCurrentOrganization()
    {
        var members = await new GetOrganizationMembersUseCase(
                _world.Organizations,
                TeamWorld.In(_organization, OrganizationRole.Buyer),
                TeamWorld.CurrentUser(_buyer))
            .ExecuteAsync();

        Assert.Equal(2, members.Count);
        Assert.DoesNotContain(members, member => member.UserId == _outsider.Id);
        Assert.True(members.Single(member => member.UserId == _buyer.Id).IsCurrentUser);
        Assert.Equal("admin", members.Single(member => member.UserId == _admin.Id).Role);
    }

    [Theory]
    [InlineData(OrganizationRole.Buyer)]
    [InlineData(OrganizationRole.Requester)]
    [InlineData(OrganizationRole.Approver)]
    public async Task ChangeRole_ShouldBeForbiddenForNonAdministrators(OrganizationRole actorRole)
    {
        var useCase = new ChangeMemberRoleUseCase(
            _world.Organizations,
            TeamWorld.In(_organization, actorRole),
            _world.UnitOfWork);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(_buyer.Id, new ChangeMemberRoleRequestDto("admin")));

        Assert.Equal("organization_permission_denied", exception.ErrorCode);
        Assert.Equal(OrganizationRole.Buyer, _world.RoleOf(_buyer, _organization));
    }

    [Fact]
    public async Task ChangeRole_ShouldUpdateTheMember_WhenRequestedByAnAdministrator()
    {
        await AdminChangeRole().ExecuteAsync(_buyer.Id, new ChangeMemberRoleRequestDto("approver"));

        Assert.Equal(OrganizationRole.Approver, _world.RoleOf(_buyer, _organization));
    }

    [Fact]
    public async Task ChangeRole_ShouldNotLeaveTheOrganizationWithoutAnAdministrator()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            AdminChangeRole().ExecuteAsync(_admin.Id, new ChangeMemberRoleRequestDto("buyer")));

        Assert.Equal("organization_last_admin", exception.ErrorCode);
        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_admin, _organization));
    }

    [Fact]
    public async Task ChangeRole_ShouldAllowAnAdministratorToStepDown_WhenAnotherOneExists()
    {
        await AdminChangeRole().ExecuteAsync(_buyer.Id, new ChangeMemberRoleRequestDto("admin"));
        await AdminChangeRole().ExecuteAsync(_admin.Id, new ChangeMemberRoleRequestDto("requester"));

        Assert.Equal(OrganizationRole.Requester, _world.RoleOf(_admin, _organization));
        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_buyer, _organization));
    }

    [Fact]
    public async Task ChangeRole_ShouldNotReachMembersOfAnotherOrganization()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            AdminChangeRole().ExecuteAsync(_outsider.Id, new ChangeMemberRoleRequestDto("buyer")));

        Assert.Equal("organization_member_not_found", exception.ErrorCode);
        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_outsider, _otherOrganization));
    }

    [Fact]
    public async Task ChangeRole_ShouldRejectAnUnknownRole()
    {
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            AdminChangeRole().ExecuteAsync(_buyer.Id, new ChangeMemberRoleRequestDto("owner")));

        Assert.Equal("organization_invalid_role", exception.ErrorCode);
    }

    [Fact]
    public async Task RemoveMember_ShouldBeForbiddenForNonAdministrators()
    {
        var useCase = new RemoveMemberUseCase(
            _world.Organizations,
            TeamWorld.In(_organization, OrganizationRole.Buyer),
            TeamWorld.CurrentUser(_buyer),
            _world.UnitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => useCase.ExecuteAsync(_admin.Id));

        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_admin, _organization));
    }

    [Fact]
    public async Task RemoveMember_ShouldLetAnyMemberLeave()
    {
        var useCase = new RemoveMemberUseCase(
            _world.Organizations,
            TeamWorld.In(_organization, OrganizationRole.Buyer),
            TeamWorld.CurrentUser(_buyer),
            _world.UnitOfWork);

        await useCase.ExecuteAsync(_buyer.Id);

        Assert.Null(_world.RoleOf(_buyer, _organization));
    }

    [Fact]
    public async Task RemoveMember_ShouldRemoveAColleague_WhenRequestedByAnAdministrator()
    {
        await AdminRemoveMember().ExecuteAsync(_buyer.Id);

        Assert.Null(_world.RoleOf(_buyer, _organization));
        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_admin, _organization));
    }

    [Fact]
    public async Task RemoveMember_ShouldNotRemoveTheLastAdministrator()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            AdminRemoveMember().ExecuteAsync(_admin.Id));

        Assert.Equal("organization_last_admin", exception.ErrorCode);
        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_admin, _organization));
    }

    [Fact]
    public async Task RemoveMember_ShouldNotReachMembersOfAnotherOrganization()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AdminRemoveMember().ExecuteAsync(_outsider.Id));

        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_outsider, _otherOrganization));
    }

    [Fact]
    public async Task UpdateOrganization_ShouldBeAdministratorOnly()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new UpdateOrganizationUseCase(
                    _world.Organizations,
                    TeamWorld.In(_organization, OrganizationRole.Buyer),
                    _world.UnitOfWork)
                .ExecuteAsync(new UpdateOrganizationRequestDto("Tomada")));

        await new UpdateOrganizationUseCase(
                _world.Organizations,
                TeamWorld.In(_organization, OrganizationRole.Admin),
                _world.UnitOfWork)
            .ExecuteAsync(new UpdateOrganizationRequestDto("  Mercado Novo Nome "));

        Assert.Equal(
            "Mercado Novo Nome",
            _world.NewContext().Organizations.Single(row => row.Id == _organization.Id).Name);
        Assert.Equal(
            "Outra Empresa",
            _world.NewContext().Organizations.Single(row => row.Id == _otherOrganization.Id).Name);
    }

    // --------------------------------------------------------- invitations

    [Theory]
    [InlineData(OrganizationRole.Buyer)]
    [InlineData(OrganizationRole.Requester)]
    [InlineData(OrganizationRole.Approver)]
    public async Task Invite_ShouldBeForbiddenForNonAdministrators(OrganizationRole actorRole)
    {
        var useCase = CreateInvitation(_buyer, actorRole, emailSent: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new CreateInvitationRequestDto("novo@empresa.com", "buyer")));

        Assert.Empty(_world.NewContext().OrganizationInvitations);
    }

    [Fact]
    public async Task Invite_ShouldStoreOnlyTheHashOfTheToken_AndExpireInSevenDays()
    {
        var result = await CreateInvitation(_admin, OrganizationRole.Admin, emailSent: true)
            .ExecuteAsync(new CreateInvitationRequestDto(" Novo@Empresa.com ", "requester"));

        var rawToken = TokenFrom(result.AcceptUrl);
        var stored = _world.NewContext().OrganizationInvitations.Single();

        Assert.Equal(64, rawToken.Length);
        Assert.NotEqual(rawToken, stored.TokenHash);
        Assert.Equal(OrganizationInvitation.HashToken(rawToken), stored.TokenHash);
        Assert.Equal("novo@empresa.com", stored.Email);
        Assert.Equal(OrganizationRole.Requester, stored.Role);
        Assert.Equal(TeamWorld.Now.UtcDateTime.AddDays(7), stored.ExpiresAtUtc);
        Assert.True(result.EmailSent);
    }

    [Fact]
    public async Task Invite_ShouldStillSucceed_WhenTheEmailCannotBeSent()
    {
        var result = await CreateInvitation(_admin, OrganizationRole.Admin, emailSent: false)
            .ExecuteAsync(new CreateInvitationRequestDto("novo@empresa.com", "buyer"));

        Assert.False(result.EmailSent);
        Assert.Contains("/convite?token=", result.AcceptUrl);
        Assert.Single(_world.NewContext().OrganizationInvitations);
    }

    [Fact]
    public async Task Invite_ShouldRejectSomeoneWhoIsAlreadyAMember()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateInvitation(_admin, OrganizationRole.Admin, emailSent: true)
                .ExecuteAsync(new CreateInvitationRequestDto("COMPRADOR@empresa.com", "buyer")));

        Assert.Equal("invitation_already_member", exception.ErrorCode);
    }

    [Fact]
    public async Task Invite_ShouldReplaceThePreviousLink_WhenTheSameEmailIsInvitedAgain()
    {
        var useCase = CreateInvitation(_admin, OrganizationRole.Admin, emailSent: true);
        var first = await useCase.ExecuteAsync(new CreateInvitationRequestDto("novo@empresa.com", "buyer"));
        var second = await useCase.ExecuteAsync(new CreateInvitationRequestDto("novo@empresa.com", "approver"));

        Assert.Single(_world.NewContext().OrganizationInvitations);
        await Assert.ThrowsAsync<NotFoundException>(() => Preview().ExecuteAsync(TokenFrom(first.AcceptUrl)));
        Assert.Equal("approver", (await Preview().ExecuteAsync(TokenFrom(second.AcceptUrl))).Role);
    }

    [Fact]
    public async Task Preview_ShouldDescribeTheInvitation_AndTellWhetherAnAccountExists()
    {
        var forNewPerson = await InviteAsync("novo@empresa.com", "buyer");
        var forExistingAccount = await InviteAsync(_outsider.Email, "approver");

        var newPerson = await Preview().ExecuteAsync(forNewPerson);
        var existing = await Preview().ExecuteAsync(forExistingAccount);

        Assert.Equal("Mercado Bom Preço", newPerson.OrganizationName);
        Assert.Equal("buyer", newPerson.Role);
        Assert.False(newPerson.AccountExists);
        Assert.True(existing.AccountExists);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
    public async Task Preview_ShouldAnswerNotFoundForAnUnknownToken(string? token)
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => Preview().ExecuteAsync(token));

        Assert.Equal("invitation_not_found", exception.ErrorCode);
    }

    [Fact]
    public async Task Invitation_ShouldStopWorkingAfterItExpires()
    {
        var token = await InviteAsync(_outsider.Email, "buyer");
        _world.Time.Now = TeamWorld.Now.AddDays(7).AddSeconds(1);

        await Assert.ThrowsAsync<NotFoundException>(() => Preview().ExecuteAsync(token));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Accept(_outsider).ExecuteAsync(new AcceptInvitationRequestDto(token)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AcceptWithRegistration().ExecuteAsync(
                new AcceptInvitationWithRegistrationRequestDto(token, "Nome", "Senha@12345", true)));

        Assert.Null(_world.RoleOf(_outsider, _organization));
    }

    [Fact]
    public async Task Accept_ShouldAddTheSignedInUserWithTheInvitedRole_AndConsumeTheInvitation()
    {
        var token = await InviteAsync(_outsider.Email, "approver");

        var result = await Accept(_outsider).ExecuteAsync(new AcceptInvitationRequestDto(token));

        Assert.Equal(_organization.Id, result.OrganizationId);
        Assert.Equal("approver", result.Role);
        Assert.Equal(OrganizationRole.Approver, _world.RoleOf(_outsider, _organization));
        // The previous organization is kept; the new one becomes the active one.
        Assert.Equal(OrganizationRole.Admin, _world.RoleOf(_outsider, _otherOrganization));
        Assert.Equal(
            _organization.Id,
            _world.NewContext().Users.Single(user => user.Id == _outsider.Id).ActiveOrganizationId);
        Assert.Empty(_world.NewContext().OrganizationInvitations);

        // Single use.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Accept(_outsider).ExecuteAsync(new AcceptInvitationRequestDto(token)));
    }

    [Fact]
    public async Task Accept_ShouldRefuseAnAccountWithADifferentEmail()
    {
        var token = await InviteAsync("convidado@empresa.com", "admin");

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            Accept(_outsider).ExecuteAsync(new AcceptInvitationRequestDto(token)));

        Assert.Equal("invitation_email_mismatch", exception.ErrorCode);
        Assert.Null(_world.RoleOf(_outsider, _organization));
        Assert.Single(_world.NewContext().OrganizationInvitations);
    }

    [Fact]
    public async Task AcceptWithRegistration_ShouldCreateAConfirmedAccountInsideTheOrganization()
    {
        var token = await InviteAsync("novo@empresa.com", "requester");

        var result = await AcceptWithRegistration().ExecuteAsync(
            new AcceptInvitationWithRegistrationRequestDto(token, " Nina Nova ", "Senha@12345", true));

        var user = _world.NewContext().Users.Single(row => row.Email == "novo@empresa.com");
        Assert.Equal("requester", result.Role);
        Assert.Equal("Nina Nova", user.Name);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal("hashed::Senha@12345", user.PasswordHash);
        Assert.Equal(TeamWorld.Now.UtcDateTime, user.TermsAcceptedAtUtc);
        Assert.NotNull(user.TermsVersion);
        Assert.Equal(_organization.Id, user.ActiveOrganizationId);
        Assert.Equal(OrganizationRole.Requester, _world.RoleOf(user, _organization));
        Assert.Empty(_world.NewContext().OrganizationInvitations);
    }

    [Fact]
    public async Task AcceptWithRegistration_ShouldRequireTheTerms()
    {
        var token = await InviteAsync("novo@empresa.com", "requester");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            AcceptWithRegistration().ExecuteAsync(
                new AcceptInvitationWithRegistrationRequestDto(token, "Nina", "Senha@12345", false)));

        Assert.Equal("terms_not_accepted", exception.ErrorCode);
        Assert.DoesNotContain(_world.NewContext().Users, row => row.Email == "novo@empresa.com");
    }

    [Fact]
    public async Task AcceptWithRegistration_ShouldNotTakeOverAnExistingAccount()
    {
        var token = await InviteAsync(_outsider.Email, "admin");

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            AcceptWithRegistration().ExecuteAsync(
                new AcceptInvitationWithRegistrationRequestDto(token, "Impostor", "OutraSenha@1", true)));

        Assert.Equal("invitation_account_exists", exception.ErrorCode);
        Assert.Equal(
            "hashed::Senha@12345",
            _world.NewContext().Users.Single(row => row.Id == _outsider.Id).PasswordHash);
        Assert.Null(_world.RoleOf(_outsider, _organization));
    }

    [Fact]
    public async Task Revoke_ShouldCancelTheInvitation_AndBeLimitedToTheOwnOrganization()
    {
        var token = await InviteAsync("novo@empresa.com", "buyer");
        var invitationId = _world.NewContext().OrganizationInvitations.Single().Id;

        // An administrator of another organization cannot see or cancel it.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new RevokeInvitationUseCase(
                    _world.Invitations,
                    TeamWorld.In(_otherOrganization, OrganizationRole.Admin),
                    _world.UnitOfWork)
                .ExecuteAsync(invitationId));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new RevokeInvitationUseCase(
                    _world.Invitations,
                    TeamWorld.In(_organization, OrganizationRole.Buyer),
                    _world.UnitOfWork)
                .ExecuteAsync(invitationId));

        await new RevokeInvitationUseCase(
                _world.Invitations,
                TeamWorld.In(_organization, OrganizationRole.Admin),
                _world.UnitOfWork)
            .ExecuteAsync(invitationId);

        await Assert.ThrowsAsync<NotFoundException>(() => Preview().ExecuteAsync(token));
    }

    [Fact]
    public async Task GetInvitations_ShouldListOnlyPendingInvitationsOfTheOrganization()
    {
        await InviteAsync("novo@empresa.com", "buyer");

        var own = await new GetInvitationsUseCase(
                _world.Invitations,
                TeamWorld.In(_organization, OrganizationRole.Admin),
                _world.Time)
            .ExecuteAsync();
        var other = await new GetInvitationsUseCase(
                _world.Invitations,
                TeamWorld.In(_otherOrganization, OrganizationRole.Admin),
                _world.Time)
            .ExecuteAsync();

        var invitation = Assert.Single(own);
        Assert.Equal("novo@empresa.com", invitation.Email);
        Assert.Equal("Ana Admin", invitation.InvitedBy);
        Assert.Empty(other);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new GetInvitationsUseCase(
                    _world.Invitations,
                    TeamWorld.In(_organization, OrganizationRole.Approver),
                    _world.Time)
                .ExecuteAsync());
    }

    // -------------------------------------------------------------- helpers

    private ChangeMemberRoleUseCase AdminChangeRole() =>
        new(_world.Organizations, TeamWorld.In(_organization, OrganizationRole.Admin), _world.UnitOfWork);

    private RemoveMemberUseCase AdminRemoveMember() =>
        new(
            _world.Organizations,
            TeamWorld.In(_organization, OrganizationRole.Admin),
            TeamWorld.CurrentUser(_admin),
            _world.UnitOfWork);

    private CreateInvitationUseCase CreateInvitation(User actor, OrganizationRole role, bool emailSent)
    {
        var frontendUrls = new Mock<IFrontendUrlService>();
        frontendUrls
            .Setup(service => service.BuildInvitationUrl(It.IsAny<string>()))
            .Returns<string>(token => $"https://app.exemplo.com/convite?token={token}");

        var emailSender = new Mock<IInvitationEmailSender>();
        emailSender
            .Setup(sender => sender.TrySendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailSent);

        return new CreateInvitationUseCase(
            _world.Organizations,
            _world.Invitations,
            _world.Users,
            TeamWorld.In(_organization, role),
            TeamWorld.CurrentUser(actor),
            frontendUrls.Object,
            emailSender.Object,
            _world.UnitOfWork,
            _world.Time);
    }

    private async Task<string> InviteAsync(string email, string role)
    {
        var result = await CreateInvitation(_admin, OrganizationRole.Admin, emailSent: true)
            .ExecuteAsync(new CreateInvitationRequestDto(email, role));

        return TokenFrom(result.AcceptUrl);
    }

    private static string TokenFrom(string acceptUrl) => acceptUrl[(acceptUrl.IndexOf('=') + 1)..];

    private GetInvitationPreviewUseCase Preview() =>
        new(_world.Invitations, _world.Organizations, _world.Users, _world.Time);

    private AcceptInvitationUseCase Accept(User user) =>
        new(
            _world.Invitations,
            _world.Organizations,
            _world.Users,
            TeamWorld.CurrentUser(user),
            _world.UnitOfWork,
            _world.Time);

    private AcceptInvitationWithRegistrationUseCase AcceptWithRegistration() =>
        new(
            _world.Invitations,
            _world.Organizations,
            _world.Users,
            _world.PasswordHasher,
            _world.UnitOfWork,
            _world.Time);
}
