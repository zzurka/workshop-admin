namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;
using WorkshopAdmin.Application.Features.Tenant.GetById;
using WorkshopAdmin.Application.Features.Tenant.List;
using WorkshopAdmin.Application.Features.Tenant.Models;

public interface ITenantRepository
{
    Task<Guid> InsertAsync(TenantInsert data, Guid createdBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    Task<TenantDetailResponse?> GetByIdAsync(Guid id, IDbConnection connection, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantListItem>> ListAsync(
        string? search, bool? isActive, int offset, int limit, string sortBy, string sortDirection,
        IDbConnection connection, CancellationToken cancellationToken);

    Task<int> CountAsync(string? search, bool? isActive, IDbConnection connection, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Updates a non-deleted tenant. Returns false if no such tenant exists.</summary>
    Task<bool> UpdateAsync(Guid id, TenantUpdate data, Guid updatedBy, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Sets activation state on a non-deleted tenant. Returns false if no such tenant exists.</summary>
    Task<bool> SetActiveAsync(Guid id, bool isActive, Guid updatedBy, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Soft-deletes a tenant (is_deleted = TRUE, is_active = FALSE). Returns false if already deleted/absent.</summary>
    Task<bool> SoftDeleteAsync(Guid id, Guid updatedBy, IDbConnection connection, CancellationToken cancellationToken);
}
