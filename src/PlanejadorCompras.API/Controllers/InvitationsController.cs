using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanejadorCompras.API.Configuration;
using PlanejadorCompras.Application.Features.Organizations.Contracts;
using PlanejadorCompras.Application.UseCases.Organization;

namespace PlanejadorCompras.API.Controllers;

/// <summary>
/// The invited person's side of an invitation. Possession of the token is the
/// credential, so these routes carry no organization role requirement.
/// </summary>
[Authorize]
[ApiController]
[Route("api/invitations")]
public sealed class InvitationsController : ControllerBase
{
    [HttpPost("preview")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(InvitationPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(
        [FromBody] AcceptInvitationRequestDto request,
        [FromServices] GetInvitationPreviewUseCase useCase,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await useCase.ExecuteAsync(request.Token, cancellationToken));
    }

    [HttpPost("accept")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(AcceptedInvitationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptInvitationRequestDto request,
        [FromServices] AcceptInvitationUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(request, cancellationToken));

    [HttpPost("accept-with-registration")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(AcceptedInvitationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptWithRegistration(
        [FromBody] AcceptInvitationWithRegistrationRequestDto request,
        [FromServices] AcceptInvitationWithRegistrationUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(request, cancellationToken));
}
