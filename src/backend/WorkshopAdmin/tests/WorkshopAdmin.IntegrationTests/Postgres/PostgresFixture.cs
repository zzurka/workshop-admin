using Npgsql;
using Testcontainers.PostgreSql;
using WorkshopAdmin.SharedKernel.Database;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Postgres;

/// <summary>
/// One migrated database per test run, in one of two modes:
/// <para>
/// <b>Container mode (default):</b> starts a postgres:18 Testcontainers container and
/// replicates database/initial_setup (workshopadmin database + admin/app roles).
/// </para>
/// <para>
/// <b>Local mode (no Docker):</b> when <c>testsettings.local.json</c> exists (see
/// <see cref="LocalTestDatabase"/>), uses a dedicated local test database with the
/// existing admin/app roles: drops all schemas, then re-migrates. The database name
/// must contain "test" — that wipe is destructive by design.
/// </para>
/// In both modes every real migration script from database/scripts is applied via
/// <see cref="MigrationRunner"/>.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string DatabaseName = "workshopadmin";
    public const string AdminUser = "workshopadmin_admin";
    public const string AppUser = "workshopadmin_app";
    public const string ContainerPassword = "workshopadmin_test";

    private PostgreSqlContainer? _container;

    public string AdminConnectionString { get; private set; } = "";

    public string AppConnectionString { get; private set; } = "";

    /// <summary>App-user options for constructing a <see cref="DbSession"/> under test.</summary>
    public DatabaseOptions AppDatabaseOptions { get; private set; } = new();

    public async Task InitializeAsync()
    {
        LocalTestDatabase? local = LocalTestDatabase.TryLoad();
        if (local is not null)
        {
            await InitializeLocalAsync(local);
        }
        else
        {
            await InitializeContainerAsync();
        }

        await MigrationRunner.RunAllAsync(AdminConnectionString);
    }

    public Task DisposeAsync() => _container?.DisposeAsync().AsTask() ?? Task.CompletedTask;

    private async Task InitializeContainerAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("postgres")
            .Build();
        await _container.StartAsync();

        string host = _container.Hostname;
        int port = _container.GetMappedPublicPort(5432);

        // Mirrors database/initial_setup/setup_database.sh, executed as superuser.
        await using (NpgsqlConnection superuser = new(_container.GetConnectionString()))
        {
            await superuser.OpenAsync();
            await ExecuteAsync(superuser, $"CREATE USER {AdminUser} WITH PASSWORD '{ContainerPassword}'");
            await ExecuteAsync(superuser, $"CREATE USER {AppUser} WITH PASSWORD '{ContainerPassword}'");
            await ExecuteAsync(superuser,
                $"CREATE DATABASE {DatabaseName} OWNER {AdminUser} ENCODING 'UTF8' TEMPLATE template0");
            await ExecuteAsync(superuser, $"GRANT ALL PRIVILEGES ON DATABASE {DatabaseName} TO {AdminUser}");
            await ExecuteAsync(superuser, $"GRANT CONNECT ON DATABASE {DatabaseName} TO {AppUser}");
        }

        // Schema-level grants for the app user live in the S_ migration scripts themselves;
        // only the public-schema defaults from initial_setup are replicated here.
        NpgsqlConnectionStringBuilder builder = new(_container.GetConnectionString())
        {
            Database = DatabaseName
        };
        await using (NpgsqlConnection database = new(builder.ConnectionString))
        {
            await database.OpenAsync();
            await ExecuteAsync(database, $"GRANT ALL ON SCHEMA public TO {AdminUser}");
            await ExecuteAsync(database, $"GRANT USAGE ON SCHEMA public TO {AppUser}");
        }

        Configure(host, port, DatabaseName, ContainerPassword, ContainerPassword);
    }

    private async Task InitializeLocalAsync(LocalTestDatabase local)
    {
        if (!local.Name.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to use local database '{local.Name}': the fixture drops all of its schemas, " +
                "so the name must contain 'test' (e.g. workshopadmin_test).");
        }

        Configure(local.Host, local.Port, local.Name, local.AdminPassword, local.AppPassword);

        await DropAllSchemasAsync();
    }

    private void Configure(string host, int port, string database, string adminPassword, string appPassword)
    {
        AdminConnectionString = ConnectionString(host, port, database, AdminUser, adminPassword);
        AppConnectionString = ConnectionString(host, port, database, AppUser, appPassword);
        AppDatabaseOptions = new DatabaseOptions
        {
            Host = host,
            Port = port,
            Name = database,
            Username = AppUser,
            Password = appPassword
        };
    }

    /// <summary>Clean slate for local mode: every non-system schema goes; migrations rebuild them.</summary>
    private async Task DropAllSchemasAsync()
    {
        await using NpgsqlConnection admin = new(AdminConnectionString);
        await admin.OpenAsync();

        await using NpgsqlCommand list = new(
            """
            SELECT schema_name FROM information_schema.schemata
            WHERE schema_name NOT LIKE 'pg\_%' AND schema_name NOT IN ('information_schema', 'public')
            """, admin);
        List<string> schemas = [];
        await using (NpgsqlDataReader reader = await list.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                schemas.Add(reader.GetString(0));
            }
        }

        foreach (string schema in schemas)
        {
            await ExecuteAsync(admin, $"DROP SCHEMA \"{schema}\" CASCADE");
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string ConnectionString(string host, int port, string database, string username, string password) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password
        }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "database";
}
