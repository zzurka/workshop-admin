namespace WorkshopAdmin.Application.Features.Auth;

using WorkshopAdmin.Application.Features.Auth.Login;
using WorkshopAdmin.Application.Features.Auth.Refresh;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
}
