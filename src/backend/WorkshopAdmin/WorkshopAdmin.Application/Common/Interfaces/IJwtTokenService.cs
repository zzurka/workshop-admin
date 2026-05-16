namespace WorkshopAdmin.Application.Common.Interfaces;

using WorkshopAdmin.Application.Features.Auth.Models;

public interface IJwtTokenService
{
    /// <summary>
    /// Builds a signed access token embedding the user's identity, tenant,
    /// role names (as role claims) and permission names (as "permission" claims).
    /// </summary>
    AccessToken GenerateAccessToken(AuthUser user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions);

    /// <summary>Generates a new cryptographically random refresh token and its hash.</summary>
    GeneratedRefreshToken GenerateRefreshToken();

    /// <summary>Hashes a raw refresh token the same way stored hashes are produced (for lookup).</summary>
    string HashRefreshToken(string rawToken);
}
