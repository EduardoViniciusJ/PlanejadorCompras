using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.UseCases.ShoppingList;

namespace PlanejadorCompras.API.Controllers;

[Authorize]
[ApiController]
[Route("api/shopping-lists/{shoppingListId:guid}")]
public sealed class ShoppingListSupplierTermsController(
    GetShoppingListSupplierTermsUseCase getUseCase,
    UpdateShoppingListSupplierTermsUseCase updateUseCase)
    : ControllerBase
{
    [HttpGet("supplier-terms")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<ShoppingListSupplierTermsResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        Guid shoppingListId,
        CancellationToken cancellationToken) =>
        Ok(await getUseCase.ExecuteAsync(shoppingListId, cancellationToken));

    [HttpPut("suppliers/{supplierId:guid}/terms")]
    [ProducesResponseType(typeof(ShoppingListSupplierTermsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid shoppingListId,
        Guid supplierId,
        [FromBody] CommercialTermsRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await updateUseCase.ExecuteAsync(
            shoppingListId,
            supplierId,
            request,
            cancellationToken));
}
