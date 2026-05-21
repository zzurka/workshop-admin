namespace WorkshopAdmin.API.Controllers.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth;
using WorkshopAdmin.Application.Features.Auth.Login;
using WorkshopAdmin.Application.Features.Auth.Logout;
using WorkshopAdmin.Application.Features.Auth.Refresh;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, ICurrentUserContext currentUser) : ControllerBase
{
    /// <summary>Authenticates with email + password and issues an access + refresh token pair.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        string userAgentHeader = Request.Headers.UserAgent.ToString();
        string? userAgent = string.IsNullOrWhiteSpace(userAgentHeader) ? null : userAgentHeader;

        LoginResponse response = await authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(response);
    }

    /// <summary>Exchanges a valid refresh token for a new token pair (the old refresh token is rotated out).</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        RefreshResponse response = await authService.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Revokes the presented refresh token. Idempotent — always returns 204
    /// whether or not the token was valid, so clients can call this on any
    /// sign-out path without special handling.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Revokes every active refresh token for the calling user (sign out everywhere).</summary>
    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await authService.LogoutAllAsync(currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
