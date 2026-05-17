namespace WorkshopAdmin.API.Controllers.Auth;

using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.Application.Features.Auth;
using WorkshopAdmin.Application.Features.Auth.Login;
using WorkshopAdmin.Application.Features.Auth.Refresh;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
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
}
