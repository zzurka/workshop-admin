using Npgsql;
using Testcontainers.PostgreSql;
using WorkshopAdmin.SharedKernel.Database;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Postgres;

/// <summary>
/// One postgres:18 container per test run. Replicates database/initial_setup
/// (workshopadmin database + admin/app roles), then applies every real migration
/// script from database/scripts via <see cref="MigrationRunner"/>.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string DatabaseName = "workshopadmin";
    public const string AdminUser = "workshopadmin_admin";
    public const string AppUser = "workshopadmin_app";
    public const string Password = "workshopadmin_test";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("postgres")
        .Build();

    public string AdminConnectionString { get; private set; } = "";

    public string AppConnectionString { get; private set; } = "";

    /// <summary>App-user options for constructing a <see cref="DbSession"/> under test.</summary>
    public DatabaseOptions AppDatabaseOptions { get; private set; } = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        string host = _container.Hostname;
        int port = _container.GetMappedPublicPort(5432);

        await CreateRolesAndDatabaseAsync();

        AdminConnectionString = ConnectionString(host, port, AdminUser);
        AppConnectionString = ConnectionString(host, port, AppUser);
        AppDatabaseOptions = new DatabaseOptions
        {
            Host = host,
            Port = port,
            Name = DatabaseName,
            Username = AppUser,
            Password = Password
        };

        await MigrationRunner.RunAllAsync(AdminConnectionString);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task CreateRolesAndDatabaseAsync()
    {
        // Mirrors database/initial_setup/setup_database.sh, executed as superuser.
        await using (NpgsqlConnection superuser = new(_container.GetConnectionString()))
        {
            await superuser.OpenAsync();
            await ExecuteAsync(superuser, $"CREATE USER {AdminUser} WITH PASSWORD '{Password}'");
            await ExecuteAsync(superuser, $"CREATE USER {AppUser} WITH PASSWORD '{Password}'");
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
        await using NpgsqlConnection database = new(builder.ConnectionString);
        await database.OpenAsync();
        await ExecuteAsync(database, $"GRANT ALL ON SCHEMA public TO {AdminUser}");
        await ExecuteAsync(database, $"GRANT USAGE ON SCHEMA public TO {AppUser}");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string ConnectionString(string host, int port, string username) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = DatabaseName,
            Username = username,
            Password = Password
        }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "database";
}
