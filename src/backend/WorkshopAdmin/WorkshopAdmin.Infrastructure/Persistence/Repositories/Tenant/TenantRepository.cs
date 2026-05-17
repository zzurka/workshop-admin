namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Tenant;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Tenant.GetById;
using WorkshopAdmin.Application.Features.Tenant.List;
using WorkshopAdmin.Application.Features.Tenant.Models;

public sealed class TenantRepository : ITenantRepository
{
    public Task<Guid> InsertAsync(TenantInsert data, Guid createdBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tenant.tenants
                (name, slug, contact_email, contact_phone, subscription_plan_id, default_currency_id,
                 address_line1, address_line2, city, postal_code, country, created_by)
            VALUES
                (@Name, @Slug, @ContactEmail, @ContactPhone, @SubscriptionPlanId, @DefaultCurrencyId,
                 @AddressLine1, @AddressLine2, @City, @PostalCode, @Country, @CreatedBy)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new
            {
                data.Name,
                data.Slug,
                data.ContactEmail,
                data.ContactPhone,
                data.SubscriptionPlanId,
                data.DefaultCurrencyId,
                data.AddressLine1,
                data.AddressLine2,
                data.City,
                data.PostalCode,
                data.Country,
                CreatedBy = createdBy
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task<TenantDetailResponse?> GetByIdAsync(Guid id, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT t.id,
                   t.name,
                   t.slug,
                   t.contact_email,
                   t.contact_phone,
                   sp.code AS subscription_plan_code,
                   c.code  AS currency_code,
                   t.address_line1,
                   t.address_line2,
                   t.city,
                   t.postal_code,
                   t.country,
                   t.is_active,
                   t.created_at,
                   t.updated_at
            FROM tenant.tenants t
            JOIN tenant.subscription_plans sp ON sp.id = t.subscription_plan_id
            JOIN codebook.currencies       c  ON c.id = t.default_currency_id
            WHERE t.id = @Id AND t.is_deleted = FALSE
            """;

        return connection.QuerySingleOrDefaultAsync<TenantDetailResponse>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<TenantListItem>> ListAsync(
        string? search, bool? isActive, int offset, int limit, string sortBy, string sortDirection,
        IDbConnection connection, CancellationToken cancellationToken)
    {
        // sortBy / sortDirection are whitelisted by TenantService — safe to interpolate.
        string sql = $"""
            SELECT t.id,
                   t.name,
                   t.slug,
                   t.contact_email,
                   sp.code AS subscription_plan_code,
                   t.is_active,
                   t.created_at
            FROM tenant.tenants t
            JOIN tenant.subscription_plans sp ON sp.id = t.subscription_plan_id
            WHERE t.is_deleted = FALSE
              AND (@Search IS NULL OR t.name ILIKE @Pattern OR t.slug ILIKE @Pattern OR t.contact_email ILIKE @Pattern)
              AND (@IsActive IS NULL OR t.is_active = @IsActive)
            ORDER BY t.{sortBy} {sortDirection}
            OFFSET @Offset LIMIT @Limit
            """;

        var rows = await connection.QueryAsync<TenantListItem>(new CommandDefinition(
            sql,
            new
            {
                Search = search,
                Pattern = $"%{search}%",
                IsActive = isActive,
                Offset = offset,
                Limit = limit
            },
            cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public Task<int> CountAsync(string? search, bool? isActive, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM tenant.tenants t
            WHERE t.is_deleted = FALSE
              AND (@Search IS NULL OR t.name ILIKE @Pattern OR t.slug ILIKE @Pattern OR t.contact_email ILIKE @Pattern)
              AND (@IsActive IS NULL OR t.is_active = @IsActive)
            """;

        return connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { Search = search, Pattern = $"%{search}%", IsActive = isActive },
            cancellationToken: cancellationToken));
    }

    public Task<bool> SlugExistsAsync(string slug, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tenant.tenants WHERE slug = @Slug AND is_deleted = FALSE
            )
            """;

        return connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Slug = slug }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Guid id, TenantUpdate data, Guid updatedBy, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tenant.tenants
            SET name                 = @Name,
                contact_email        = @ContactEmail,
                contact_phone        = @ContactPhone,
                subscription_plan_id = @SubscriptionPlanId,
                default_currency_id  = @DefaultCurrencyId,
                address_line1        = @AddressLine1,
                address_line2        = @AddressLine2,
                city                 = @City,
                postal_code          = @PostalCode,
                country              = @Country,
                updated_at           = NOW(),
                updated_by           = @UpdatedBy
            WHERE id = @Id AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id,
                data.Name,
                data.ContactEmail,
                data.ContactPhone,
                data.SubscriptionPlanId,
                data.DefaultCurrencyId,
                data.AddressLine1,
                data.AddressLine2,
                data.City,
                data.PostalCode,
                data.Country,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, Guid updatedBy, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tenant.tenants
            SET is_active  = @IsActive,
                updated_at = NOW(),
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, IsActive = isActive, UpdatedBy = updatedBy },
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid updatedBy, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tenant.tenants
            SET is_deleted = TRUE,
                is_active  = FALSE,
                updated_at = NOW(),
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, UpdatedBy = updatedBy },
            cancellationToken: cancellationToken));
        return affected > 0;
    }
}
