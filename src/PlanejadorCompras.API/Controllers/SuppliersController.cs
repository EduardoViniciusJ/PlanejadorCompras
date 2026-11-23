using PlanejadorCompras.Application.Features.Suppliers.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanejadorCompras.Application.Common.Pagination;
using PlanejadorCompras.Application.UseCases.Supplier;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Domain.Authorization;

namespace PlanejadorCompras.API.Controllers;

[Authorize]
[RequirePermission(OrganizationPermission.ViewData)]
[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(
    CreateSupplierUseCase createUseCase,
    GetSupplierByIdUseCase getByIdUseCase,
    GetSuppliersUseCase getAllUseCase,
    UpdateSupplierUseCase updateUseCase,
    DeleteSupplierUseCase deleteUseCase) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<SupplierResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        Ok(await getAllUseCase.ExecuteAsync(page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await getByIdUseCase.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [RequirePermission(OrganizationPermission.ManageSuppliers)]
    public async Task<IActionResult> Create(
        [FromBody] SupplierRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await createUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SupplierResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [RequirePermission(OrganizationPermission.ManageSuppliers)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SupplierRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await updateUseCase.ExecuteAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [RequirePermission(OrganizationPermission.ManageSuppliers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
