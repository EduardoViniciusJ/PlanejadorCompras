using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.UseCases.Account;

namespace PlanejadorCompras.API.Controllers;

/// <summary>
/// The signed-in person's own account. Nothing here depends on the role in the
/// organization: everyone manages their own profile, password and data.
/// </summary>
[Authorize]
[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [HttpGet]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] GetAccountUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequestDto request,
        [FromServices] UpdateProfileUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        [FromServices] ChangePasswordUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("active-organization")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SwitchOrganization(
        [FromBody] SwitchOrganizationRequestDto request,
        [FromServices] SwitchOrganizationUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("export")]
    [Produces("application/json")]
    public async Task<IActionResult> Export(
        [FromServices] ExportAccountDataUseCase useCase,
        CancellationToken cancellationToken)
    {
        var export = await useCase.ExecuteAsync(cancellationToken);
        var content = JsonSerializer.SerializeToUtf8Bytes(export, ExportJsonOptions);
        var fileName = $"planejador-de-compras-meus-dados-{export.GeneratedAtUtc:yyyyMMdd}.json";

        Response.Headers.CacheControl = "no-store";
        return File(content, "application/json", fileName);
    }

    [HttpGet("deletion-preview")]
    [ProducesResponseType(typeof(AccountDeletionPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeletionPreview(
        [FromServices] GetAccountDeletionPreviewUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpPost("delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteAccountRequestDto request,
        [FromServices] DeleteAccountUseCase useCase,
        [FromServices] IAuthCookieService authCookieService,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        authCookieService.DeleteAccessToken(HttpContext);
        return NoContent();
    }
}
