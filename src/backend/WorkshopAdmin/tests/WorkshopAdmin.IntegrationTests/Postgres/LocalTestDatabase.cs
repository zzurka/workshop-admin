using System.Text.Json;

namespace WorkshopAdmin.IntegrationTests.Postgres;

/// <summary>
/// Opt-in local mode for integration tests on machines without Docker: a
/// <c>testsettings.local.json</c> file (git-ignored, copied to test output) points the
/// fixture at a dedicated test database on a local PostgreSQL 18 instance instead of a
/// Testcontainers container. See <c>testsettings.local.json.example</c> for setup.
/// </summary>
public sealed class LocalTestDatabase
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Must contain "test" — the fixture DROPS every schema in this database on startup.
    /// </summary>
    public string Name { get; set; } = "workshopadmin_test";

    public string AdminPassword { get; set; } = "";
    public string AppPassword { get; set; } = "";

    public static LocalTestDatabase? TryLoad()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "testsettings.local.json");
        if (!File.Exists(path))
        {
            return null;
        }

        LocalTestDatabase settings =
            JsonSerializer.Deserialize<LocalTestDatabase>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"'{path}' is not valid JSON.");

        if (string.IsNullOrWhiteSpace(settings.AdminPassword)
            || string.IsNullOrWhiteSpace(settings.AppPassword))
        {
            throw new InvalidOperationException(
                $"'{path}' must set AdminPassword and AppPassword (see testsettings.local.json.example).");
        }

        return settings;
    }
}
