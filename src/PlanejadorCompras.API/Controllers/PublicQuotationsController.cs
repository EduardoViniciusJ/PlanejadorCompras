using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanejadorCompras.API.Configuration;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.UseCases.QuotationRequest;

namespace PlanejadorCompras.API.Controllers;

/// <summary>
/// Endpoints a supplier reaches through the public link, without signing in. The token in
/// the route is the only credential and gives access to a single supplier quotation.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/public/quotations")]
public sealed class PublicQuotationsController(
    GetPublicSupplierQuotationUseCase getUseCase,
    SubmitPublicSupplierQuotationUseCase submitUseCase)
    : ControllerBase
{
    private const int MaximumResponseBodyBytes = 256 * 1024;

    [HttpGet("{token}")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.PublicQuotationReadPolicy)]
    [ProducesResponseType(typeof(PublicSupplierQuotationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Get(string token, CancellationToken cancellationToken) =>
        Ok(await getUseCase.ExecuteAsync(token, cancellationToken));

    [HttpPost("{token}/response")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.PublicQuotationWritePolicy)]
    [RequestSizeLimit(MaximumResponseBodyBytes)]
    [ProducesResponseType(typeof(PublicSupplierQuotationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Respond(
        string token,
        [FromBody] SubmitSupplierQuotationRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await submitUseCase.ExecuteAsync(token, request, cancellationToken));
}
