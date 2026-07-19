using Npgsql;

namespace WorkshopAdmin.SharedKernel.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Name { get; set; } = "workshopadmin";
    public string Username { get; set; } = "workshopadmin_app";
    public string Password { get; set; } = "";

    public string BuildConnectionString() => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Database = Name,
        Username = Username,
        Password = Password
    }.ConnectionString;
}
