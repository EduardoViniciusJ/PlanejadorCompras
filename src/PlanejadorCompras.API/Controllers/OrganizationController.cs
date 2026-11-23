using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.UseCases.Organization;
using PlanejadorCompras.Domain.Authorization;

namespace PlanejadorCompras.API.Controllers;

/// <summary>Team management of the organization the user is working in.</summary>
[Authorize]
[ApiController]
[Route("api/organization")]
public sealed class OrganizationController : ControllerBase
{
    [HttpGet]
    [RequirePermission(OrganizationPermission.ViewTeam)]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] GetOrganizationUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpPut]
    [RequirePermission(OrganizationPermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateOrganizationRequestDto request,
        [FromServices] UpdateOrganizationUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("members")]
    [RequirePermission(OrganizationPermission.ViewTeam)]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrganizationMemberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(
        [FromServices] GetOrganizationMembersUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpPut("members/{userId:guid}/role")]
    [RequirePermission(OrganizationPermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeMemberRole(
        Guid userId,
        [FromBody] ChangeMemberRoleRequestDto request,
        [FromServices] ChangeMemberRoleUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("members/{userId:guid}")]
    [RequirePermission(OrganizationPermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(
        Guid userId,
        [FromServices] RemoveMemberUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Any member may leave; the use case only allows removing oneself here.</summary>
    [HttpPost("leave")]
    [RequirePermission(OrganizationPermission.ViewTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Leave(
        [FromServices] RemoveMemberUseCase useCase,
        [FromServices] PlanejadorCompras.Application.Services.Interfaces.ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(currentUser.UserId, cancellationToken);
        return NoContent();
    }

    [HttpGet("invitations")]
    [RequirePermission(OrganizationPermission.ManageTeam)]
    [ProducesResponseType(typeof(IReadOnlyCollection<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitations(
        [FromServices] GetInvitationsUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpPost("invitations")]
    [RequirePermission(OrganizationPermission.ManageTeam)]
    [ProducesResponseType(typeof(CreatedInvitationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvitation(
        [FromBody] CreateInvitationRequestDto request,
        [FromServices] CreateInvitationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("invitations/{invitationId:guid}")]
    [RequirePermission(OrganizationPermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeInvitation(
        Guid invitationId,
        [FromServices] RevokeInvitationUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(invitationId, cancellationToken);
        return NoContent();
    }
}
