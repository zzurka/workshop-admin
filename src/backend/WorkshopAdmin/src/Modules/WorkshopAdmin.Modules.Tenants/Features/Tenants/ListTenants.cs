using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Paging;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

/// <summary>GET /api/tenants — paged, searchable by name/slug, filterable by is_active.</summary>
internal static class ListTenants
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:read" (platform scope)
        group.MapGet("/", async (
                TenantsDbContext db,
                CancellationToken cancellationToken,
                string? search = null,
                bool? isActive = null,
                int offset = 0,
                int limit = 25) =>
            {
                offset = Math.Max(offset, 0);
                limit = Math.Clamp(limit, 1, 200);

                IQueryable<Tenant> query = db.Tenants;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string pattern = $"%{search.Trim()}%";
                    query = query.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Slug, pattern));
                }

                if (isActive is not null)
                {
                    query = query.Where(t => t.IsActive == isActive);
                }

                int totalCount = await query.CountAsync(cancellationToken);
                List<TenantListItem> items = await query
                    .OrderBy(t => t.Name).ThenBy(t => t.Id)
                    .Skip(offset).Take(limit)
                    .Select(t => new TenantListItem(t.Id, t.Name, t.Slug, t.ContactEmail, t.ContactPhone, t.IsActive))
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(new PagedResponse<TenantListItem>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Offset = offset,
                    Limit = limit
                });
            })
            .WithSummary("List tenants")
            .WithDescription("Paged list of workshops. search matches name or slug (case-insensitive substring); isActive filters suspended tenants.");
}
