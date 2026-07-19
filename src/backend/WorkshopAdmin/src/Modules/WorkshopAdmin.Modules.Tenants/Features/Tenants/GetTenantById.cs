using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

/// <summary>GET /api/tenants/{id} — detail including the current subscription plan.</summary>
internal static class GetTenantById
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:read" (platform scope)
        group.MapGet("/{id:guid}", async (
                Guid id,
                TenantsDbContext db,
                CancellationToken cancellationToken) =>
            {
                Tenant? tenant = await db.Tenants
                    .Include(t => t.SubscriptionPlan)
                    .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

                return tenant is null
                    ? ResultHttpExtensions.ToProblem(Error.NotFound("tenant.not_found", $"Tenant {id} does not exist."))
                    : TypedResults.Ok(TenantResponse.From(tenant));
            });
}
