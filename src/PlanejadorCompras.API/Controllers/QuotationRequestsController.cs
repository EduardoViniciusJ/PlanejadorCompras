using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanejadorCompras.API.Configuration;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.QuotationRequest;

namespace PlanejadorCompras.API.Controllers;

[Authorize]
[ApiController]
[Route("api/quotation-requests")]
public sealed class QuotationRequestsController(
    CreateQuotationRequestUseCase createUseCase,
    DeleteQuotationRequestUseCase deleteUseCase,
    GetQuotationRequestsUseCase getAllUseCase,
    GetQuotationRequestByIdUseCase getByIdUseCase,
    ExportSavedQuotationRequestPdfUseCase exportPdfUseCase,
    RotateSupplierQuotationLinkUseCase rotateLinkUseCase,
    RevokeSupplierQuotationLinkUseCase revokeLinkUseCase,
    IEmailAvailability emailAvailability)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<QuotationRequestSummaryResponseDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await getAllUseCase.ExecuteAsync(cancellationToken));

    [HttpGet("delivery-options")]
    [ProducesResponseType(typeof(QuotationDeliveryOptionsDto), StatusCodes.Status200OK)]
    public IActionResult GetDeliveryOptions() =>
        Ok(new QuotationDeliveryOptionsDto(emailAvailability.IsAvailable));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuotationRequestDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await getByIdUseCase.ExecuteAsync(id, cancellationToken));

    [HttpPost("/api/shopping-lists/{shoppingListId:guid}/quotation-requests")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.QuotationDeliveryPolicy)]
    [ProducesResponseType(typeof(QuotationRequestDetailResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid shoppingListId,
        [FromBody] QuotationRequestPdfRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await createUseCase.ExecuteAsync(
            shoppingListId,
            request,
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/supplier-quotations/{supplierQuotationId:guid}/link")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.QuotationDeliveryPolicy)]
    [ProducesResponseType(typeof(SupplierQuotationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateSupplierLink(
        Guid id,
        Guid supplierQuotationId,
        [FromBody] SupplierQuotationLinkRequestDto? request,
        CancellationToken cancellationToken) =>
        Ok(await rotateLinkUseCase.ExecuteAsync(
            id,
            supplierQuotationId,
            request ?? new SupplierQuotationLinkRequestDto(),
            cancellationToken));

    [HttpPost("{id:guid}/supplier-quotations/{supplierQuotationId:guid}/revoke")]
    [ProducesResponseType(typeof(SupplierQuotationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSupplierLink(
        Guid id,
        Guid supplierQuotationId,
        CancellationToken cancellationToken) =>
        Ok(await revokeLinkUseCase.ExecuteAsync(id, supplierQuotationId, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(
        Guid id,
        CancellationToken cancellationToken)
    {
        var file = await exportPdfUseCase.ExecuteAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
