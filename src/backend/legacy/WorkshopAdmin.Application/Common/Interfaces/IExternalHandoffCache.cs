namespace WorkshopAdmin.Application.Common.Interfaces;

using WorkshopAdmin.Application.Features.Auth.Login;

/// <summary>
/// Short-lived (~60s) single-use store for the LoginResponse produced by an
/// external callback. The SPA exchanges the handoff code for the tokens so
/// tokens never appear in the URL.
/// </summary>
public interface IExternalHandoffCache
{
    Task SetAsync(string handoffCode, LoginResponse payload, CancellationToken cancellationToken);

    Task<LoginResponse?> TakeAsync(string handoffCode, CancellationToken cancellationToken);
}
