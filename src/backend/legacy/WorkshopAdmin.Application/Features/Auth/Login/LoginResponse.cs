namespace WorkshopAdmin.Application.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    AuthenticatedUser User);

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    Guid? TenantId,
    IReadOnlyList<string> Roles);
