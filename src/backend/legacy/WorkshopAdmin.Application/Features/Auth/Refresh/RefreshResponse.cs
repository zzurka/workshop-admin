namespace WorkshopAdmin.Application.Features.Auth.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
