using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanejadorCompras.API.Configuration;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.UseCases.Ai;

namespace PlanejadorCompras.API.Controllers;

[Authorize]
[ApiController]
public sealed class AiController : ControllerBase
{
    // Room for the largest accepted file plus the pasted text and multipart overhead.
    private const long MaxRequestBytes = AiSettings.HardMaxFileBytes + 1024 * 1024;

    /// <summary>Whether AI is configured and how much of the monthly limit the user has used.</summary>
    [HttpGet("api/ai/status")]
    [ProducesResponseType(typeof(AiStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(
        [FromServices] AiUsageService aiUsageService,
        CancellationToken cancellationToken)
    {
        return Ok(await aiUsageService.GetStatusAsync(cancellationToken));
    }

    /// <summary>
    /// Reads a supplier's answer and proposes prices for the items of the list.
    /// Nothing is saved; the buyer confirms through the quote batch endpoint.
    /// </summary>
    [HttpPost("api/shopping-lists/{shoppingListId:guid}/ai/quote-extraction")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.AiPolicy)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(QuoteExtractionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExtractQuote(
        Guid shoppingListId,
        [FromForm] string? text,
        IFormFile? file,
        [FromServices] ExtractSupplierQuoteUseCase useCase,
        [FromServices] AiInputPreparer inputPreparer,
        CancellationToken cancellationToken)
    {
        var upload = await ReadUploadAsync(file, inputPreparer.MaxFileBytes, cancellationToken);
        return Ok(await useCase.ExecuteAsync(shoppingListId, text, upload, cancellationToken));
    }

    /// <summary>Saves the prices the buyer confirmed for one supplier, in a single commit.</summary>
    [HttpPost("api/shopping-lists/{shoppingListId:guid}/quotes/batch")]
    [ProducesResponseType(typeof(QuoteBatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyQuoteBatch(
        Guid shoppingListId,
        [FromBody] QuoteBatchRequestDto request,
        [FromServices] ApplyQuoteBatchUseCase useCase,
        CancellationToken cancellationToken)
    {
        return Ok(await useCase.ExecuteAsync(shoppingListId, request, cancellationToken));
    }

    /// <summary>Proposes a shopping list from a description or a photo. Nothing is created.</summary>
    [HttpPost("api/ai/shopping-list-draft")]
    [EnableRateLimiting(ApiRateLimitingConfiguration.AiPolicy)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ShoppingListDraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DraftShoppingList(
        [FromForm] string? text,
        IFormFile? file,
        [FromServices] DraftShoppingListUseCase useCase,
        [FromServices] AiInputPreparer inputPreparer,
        CancellationToken cancellationToken)
    {
        var upload = await ReadUploadAsync(file, inputPreparer.MaxFileBytes, cancellationToken);
        return Ok(await useCase.ExecuteAsync(text, upload, cancellationToken));
    }

    /// <summary>
    /// Copies the upload to memory for this request only. The size is checked before
    /// reading, so an oversized file is never buffered by the application.
    /// </summary>
    private static async Task<AiUploadedFile?> ReadUploadAsync(
        IFormFile? file,
        long maxFileBytes,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > maxFileBytes)
        {
            throw new BadRequestException(
                $"O arquivo passa do limite de {maxFileBytes / (1024d * 1024d):0.#} MB.",
                "ai_file_too_large");
        }

        using var buffer = new MemoryStream(capacity: (int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return new AiUploadedFile(Path.GetFileName(file.FileName), buffer.ToArray());
    }
}
