using Npgsql;

namespace WorkshopAdmin.IntegrationTests.Postgres;

/// <summary>
/// Minimal C# reimplementation of database/script_runners: executes every script in
/// database/scripts in file-name order as the admin user. No checksum tracking —
/// every test database is freshly created, so the history table is irrelevant here.
/// </summary>
public static class MigrationRunner
{
    public static async Task RunAllAsync(string adminConnectionString)
    {
        string scriptsDirectory = FindScriptsDirectory();
        string[] scripts = Directory.GetFiles(scriptsDirectory, "*.sql")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        if (scripts.Length == 0)
        {
            throw new InvalidOperationException($"No migration scripts found in '{scriptsDirectory}'.");
        }

        await using NpgsqlConnection connection = new(adminConnectionString);
        await connection.OpenAsync();

        foreach (string script in scripts)
        {
            string sql = await File.ReadAllTextAsync(script);
            try
            {
                await using NpgsqlCommand command = new(sql, connection);
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Migration script '{Path.GetFileName(script)}' failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>Walks up from the test assembly location until it finds database/scripts.</summary>
    private static string FindScriptsDirectory()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "database", "scripts");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate database/scripts walking up from '{AppContext.BaseDirectory}'.");
    }
}
