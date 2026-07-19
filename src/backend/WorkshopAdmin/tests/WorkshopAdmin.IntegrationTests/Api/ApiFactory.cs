using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using WorkshopAdmin.IntegrationTests.Postgres;

namespace WorkshopAdmin.IntegrationTests.Api;

/// <summary>Boots the real host wired to the Testcontainers database (as the app user).</summary>
public sealed class ApiFactory(PostgresFixture fixture) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Host", fixture.AppDatabaseOptions.Host);
        builder.UseSetting("Database:Port", fixture.AppDatabaseOptions.Port.ToString());
        builder.UseSetting("Database:Name", fixture.AppDatabaseOptions.Name);
        builder.UseSetting("Database:Username", fixture.AppDatabaseOptions.Username);
        builder.UseSetting("Database:Password", fixture.AppDatabaseOptions.Password);
    }
}
