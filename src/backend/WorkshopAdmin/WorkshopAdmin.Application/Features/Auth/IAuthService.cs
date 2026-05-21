namespace WorkshopAdmin.Application.Features.Auth;

using WorkshopAdmin.Application.Features.Auth.External;
using WorkshopAdmin.Application.Features.Auth.Login;
using WorkshopAdmin.Application.Features.Auth.Logout;
using WorkshopAdmin.Application.Features.Auth.Refresh;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the presented refresh token if it exists. Idempotent: unknown,
    /// already-revoked, or expired tokens are accepted silently. Token reuse
    /// (revoked-but-replayed) still triggers full-family revocation as a safety
    /// measure, even though logout itself does not signal an error.
    /// </summary>
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);

    /// <summary>Revokes every active refresh token for the supplied user (sign-out everywhere).</summary>
    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Builds the provider authorize URL and stashes the per-flow state (PKCE).</summary>
    Task<ExternalStartResponse> ExternalStartAsync(string providerCode, string redirectUri, CancellationToken cancellationToken);

    /// <summary>
    /// Provider callback: validates state, exchanges the code for an id_token,
    /// matches an invited user (by provider/subject or verified email), issues
    /// tokens, and returns a single-use handoff code the SPA exchanges for the
    /// real login response.
    /// </summary>
    Task<string> ExternalCallbackAsync(string providerCode, string code, string state, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    /// <summary>Exchanges a single-use handoff code for the real login response.</summary>
    Task<LoginResponse> ExternalExchangeAsync(ExternalExchangeRequest request, CancellationToken cancellationToken);
}
