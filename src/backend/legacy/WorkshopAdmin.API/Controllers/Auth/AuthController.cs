namespace WorkshopAdmin.API.Controllers.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WorkshopAdmin.API.Configuration;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth;
using WorkshopAdmin.Application.Features.Auth.External;
using WorkshopAdmin.Application.Features.Auth.Login;
using WorkshopAdmin.Application.Features.Auth.Logout;
using WorkshopAdmin.Application.Features.Auth.PasswordReset;
using WorkshopAdmin.Application.Features.Auth.Refresh;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    ICurrentUserContext currentUser,
    IFrontendUrlProvider frontendUrls,
    IOptions<ApiUrlOptions> apiUrls) : ControllerBase
{
    /// <summary>Authenticates with email + password and issues an access + refresh token pair.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        (string? ip, string? ua) = CallerMetadata();
        LoginResponse response = await authService.LoginAsync(request, ip, ua, cancellationToken);
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

    /// <summary>Starts an external (OIDC) sign-in flow. Returns the provider authorize URL the SPA should navigate to.</summary>
    [HttpGet("external/{provider}")]
    public async Task<ActionResult<ExternalStartResponse>> ExternalStart(string provider, CancellationToken cancellationToken)
    {
        string redirectUri = BuildCallbackUrl(provider);
        ExternalStartResponse response = await authService.ExternalStartAsync(provider, redirectUri, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// OIDC callback: validates state, exchanges the code, matches the user,
    /// and redirects to the SPA with a single-use handoff code. Tokens are
    /// never placed in the URL.
    /// </summary>
    [HttpGet("external/{provider}/callback")]
    public async Task<IActionResult> ExternalCallback(
        string provider,
        [FromQuery(Name = "code")] string code,
        [FromQuery(Name = "state")] string state,
        CancellationToken cancellationToken)
    {
        (string? ip, string? ua) = CallerMetadata();
        string handoffCode = await authService.ExternalCallbackAsync(provider, code, state, ip, ua, cancellationToken);
        return Redirect(frontendUrls.ExternalCompleteUrl(handoffCode));
    }

    /// <summary>Exchanges a single-use handoff code for the real login response (access + refresh tokens, user payload).</summary>
    [HttpPost("external/exchange")]
    public async Task<ActionResult<LoginResponse>> ExternalExchange(
        [FromBody] ExternalExchangeRequest request, CancellationToken cancellationToken)
    {
        LoginResponse response = await authService.ExternalExchangeAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Starts a self-service password reset. Always returns 204 — the response
    /// never indicates whether the email is registered.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Completes a password reset using the token from the email. On success,
    /// sets the new password and revokes every active refresh token.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        await authService.CompletePasswordResetAsync(request, cancellationToken);
        return NoContent();
    }

    private (string? ip, string? userAgent) CallerMetadata()
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string userAgentHeader = Request.Headers.UserAgent.ToString();
        string? userAgent = string.IsNullOrWhiteSpace(userAgentHeader) ? null : userAgentHeader;
        return (ipAddress, userAgent);
    }

    private string BuildCallbackUrl(string provider)
    {
        string baseUrl = apiUrls.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/api/auth/external/{provider}/callback";
    }
}
