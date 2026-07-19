using Npgsql;

namespace WorkshopAdmin.IntegrationTests.Postgres;

public static class TestSeed
{
    /// <summary>Creates a tenant (as the RLS-bypassing admin) and returns its id.</summary>
    public static async Task<Guid> CreateTenantAsync(PostgresFixture fixture)
    {
        await using NpgsqlConnection admin = new(fixture.AdminConnectionString);
        await admin.OpenAsync();

        string slug = $"test-{Guid.NewGuid():N}";
        await using NpgsqlCommand command = new(
            """
            INSERT INTO tenant.tenants (name, slug, subscription_plan_id, default_currency_id)
            VALUES ($1, $2,
                    (SELECT id FROM tenant.subscription_plans ORDER BY sort_order LIMIT 1),
                    (SELECT id FROM codebook.currencies ORDER BY id LIMIT 1))
            RETURNING id
            """, admin);
        command.Parameters.AddWithValue($"Tenant {slug}");
        command.Parameters.AddWithValue(slug);

        return (Guid)(await command.ExecuteScalarAsync())!;
    }
}
