using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

/// <summary>DELETE /api/tenants/{id} — soft delete; the row disappears from every
/// filtered query but history (subscriptions, domain data) stays intact.</summary>
internal static class DeleteTenant
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:delete" (platform scope)
        group.MapDelete("/{id:guid}", async (
                Guid id,
                DeleteTenantHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult());
}

internal sealed class DeleteTenantHandler(TenantsDbContext db)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("tenant.not_found", $"Tenant {id} does not exist."));
        }

        tenant.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
