using PlanejadorCompras.Application.Features.Authentication.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlanejadorCompras.API.Configuration;
using PlanejadorCompras.API.ErrorHandling;
using PlanejadorCompras.API.Security;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.Auth;

namespace PlanejadorCompras.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuthCookieService _authCookieService;

    // Use cases are injected per action ([FromServices]) so that a provider that is not
    // configured (Google, e-mail) only affects its own route instead of every /api/auth/* route.
    public AuthController(
        ICurrentUser currentUser,
        IAuthCookieService authCookieService)
    {
        _currentUser = currentUser;
        _authCookieService = authCookieService;
    }

    [HttpPost("google-code")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleCodeLogin(
        [FromBody] GoogleAuthorizationCodeLoginRequestDto request,
        [FromServices] GoogleAuthorizationCodeLoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!HasXmlHttpRequestHeader())
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Missing required Google login request header.",
                Instance = Request.Path
            };

            problemDetails.Extensions["errorCode"] = "google_code_missing_x_requested_with";

            return Unauthorized(problemDetails);
        }

        var session = await useCase.ExecuteAsync(request, cancellationToken);

        _authCookieService.AppendSession(HttpContext, session);

        return Ok();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult GetMe()
    {
        return Ok(new CurrentUserResponseDto(
            _currentUser.UserId,
            _currentUser.Email,
            _currentUser.Name));
    }

    /// <summary>
    /// Exchanges the refresh token cookie for a new access token and a new refresh token.
    /// The frontend calls it once when a request comes back 401.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.RefreshPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromServices] RefreshSessionUseCase useCase,
        CancellationToken cancellationToken)
    {
        // Without an access token cookie the CSRF middleware does not run, so the header is
        // required here explicitly: a cross-site form post must not be able to rotate the session.
        if (!HasXmlHttpRequestHeader())
        {
            await ApiProblemDetailsWriter.WriteAsync(
                HttpContext,
                StatusCodes.Status401Unauthorized,
                "Missing required request header.",
                "missing_x_requested_with",
                cancellationToken);
            return new EmptyResult();
        }

        try
        {
            var session = await useCase.ExecuteAsync(
                _authCookieService.ReadRefreshToken(HttpContext),
                cancellationToken);

            _authCookieService.AppendSession(HttpContext, session);

            return NoContent();
        }
        catch (UnauthorizedException exception)
        {
            // Written here instead of by the exception handler, which would discard the
            // Set-Cookie headers that remove the dead session from the browser.
            _authCookieService.DeleteSession(HttpContext);

            await ApiProblemDetailsWriter.WriteAsync(
                HttpContext,
                StatusCodes.Status401Unauthorized,
                exception.Message,
                exception.ErrorCode,
                cancellationToken);
            return new EmptyResult();
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromServices] LogoutUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(
            _authCookieService.ReadRefreshToken(HttpContext),
            GetAuthenticatedSessionId(),
            cancellationToken);

        _authCookieService.DeleteSession(HttpContext);

        return NoContent();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.EmailPolicy)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        [FromServices] RegisterUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return Ok(new
        {
            message = "Se o e-mail puder ser cadastrado, você receberá uma mensagem para confirmar a conta."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.LoginPolicy)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        [FromServices] LoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        var session = await useCase.ExecuteAsync(request, cancellationToken);
        _authCookieService.AppendSession(HttpContext, session);
        return Ok(new { message = "Login realizado com sucesso." });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.EmailPolicy)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequestDto request,
        [FromServices] ConfirmEmailUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return Ok(new { message = "E-mail confirmado com sucesso." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.EmailPolicy)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        [FromServices] ForgotPasswordUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return Ok(new { message = "Se o e-mail existir em nossa base, você receberá um link para redefinir sua senha." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingConfiguration.EmailPolicy)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        [FromServices] ResetPasswordUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request, cancellationToken);
        return Ok(new { message = "Senha redefinida com sucesso." });
    }

    private bool HasXmlHttpRequestHeader() =>
        string.Equals(
            Request.Headers[AuthenticationConstants.XmlHttpRequestHeaderName],
            AuthenticationConstants.XmlHttpRequestHeaderValue,
            StringComparison.OrdinalIgnoreCase);

    private Guid? GetAuthenticatedSessionId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return Guid.TryParse(User.FindFirst(AuthenticationConstants.SessionIdClaimName)?.Value, out var sessionId)
            ? sessionId
            : null;
    }
}
