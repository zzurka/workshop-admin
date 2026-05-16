namespace WorkshopAdmin.Infrastructure.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Models;

public sealed class JwtTokenService : IJwtTokenService
{
    private const int RefreshTokenByteLength = 32;

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken GenerateAccessToken(AuthUser user, IReadOnlyList<string> roles, IReadOnlyList<string> permissions)
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.TenantId is { } tenantId)
        {
            claims.Add(new Claim("tenant_id", tenantId.ToString()));
        }

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (string permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(tokenString, expiresAt);
    }

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        byte[] raw = RandomNumberGenerator.GetBytes(RefreshTokenByteLength);
        string rawToken = Base64UrlEncoder.Encode(raw);
        string hash = HashRefreshToken(rawToken);
        DateTime expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

        return new GeneratedRefreshToken(rawToken, hash, expiresAt);
    }

    public string HashRefreshToken(string rawToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }
}
