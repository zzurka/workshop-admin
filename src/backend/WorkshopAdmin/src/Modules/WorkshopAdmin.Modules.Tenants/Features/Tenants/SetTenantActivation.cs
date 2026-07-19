using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

/// <summary>POST /api/tenants/{id}/activation — suspend / reactivate a tenant.
/// Suspended tenants' users cannot log in (enforced by the Auth module, F3).</summary>
internal static class SetTenantActivation
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:deactivate" (platform scope)
        group.MapPost("/{id:guid}/activation", async (
                Guid id,
                SetTenantActivationRequest request,
                SetTenantActivationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
            .WithSummary("Suspend / reactivate tenant")
            .WithDescription("Suspension keeps all data but blocks the tenant's users from logging in (enforced by the Auth module).");
}

internal sealed record SetTenantActivationRequest(bool IsActive);

internal sealed class SetTenantActivationHandler(TenantsDbContext db)
{
    public async Task<Result> HandleAsync(
        Guid id, SetTenantActivationRequest request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("tenant.not_found", $"Tenant {id} does not exist."));
        }

        tenant.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
