using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using WorkshopAdmin.Infrastructure.Persistence;
using WorkshopAdmin.Infrastructure.Security;

if (args.Length == 0)
{
    Console.WriteLine("Usage: WorkshopAdmin.CLI <command>");
    Console.WriteLine("Commands:");
    Console.WriteLine("  seed-admin   Create the initial platform super admin user and assign the platform_admin role");
    return 1;
}

string command = args[0];

return command switch
{
    "seed-admin" => await SeedAdmin(),
    _ => Error($"Unknown command: {command}")
};

static async Task<int> SeedAdmin()
{
    IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .AddUserSecrets("9979d02c-1d7b-438d-b50e-b1b111ee4b99")
        .Build();

    string? connectionString = BuildConnectionString(config);
    if (connectionString is null)
        return 1;

    Console.Write("Email: ");
    string? email = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(email))
        return Error("Email is required.");

    Console.Write("First name: ");
    string? firstName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(firstName))
        return Error("First name is required.");

    Console.Write("Last name: ");
    string? lastName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(lastName))
        return Error("Last name is required.");

    Console.Write("Password: ");
    string? password = ReadPasswordMasked();
    if (string.IsNullOrWhiteSpace(password))
        return Error("Password is required.");

    Console.Write("Confirm password: ");
    string? confirmPassword = ReadPasswordMasked();
    if (password != confirmPassword)
        return Error("Passwords do not match.");

    var hasher = new Argon2PasswordHasher();
    string passwordHash = hasher.Hash(password);

    var factory = new DbConnectionFactory(connectionString);
    using IDbConnection db = factory.CreateConnection();
    db.Open();
    using IDbTransaction tx = db.BeginTransaction();

    try
    {
        // Resolve the platform_admin role by its stable name (ids are not
        // stable across environments). It is a global role: tenant_id IS NULL.
        Guid? roleId = await db.QuerySingleOrDefaultAsync<Guid?>(
            """
            SELECT id FROM auth.roles
            WHERE name = 'platform_admin' AND tenant_id IS NULL AND is_deleted = FALSE
            """,
            transaction: tx);

        if (roleId is null)
        {
            tx.Rollback();
            return Error("Role 'platform_admin' not found. Run the database migrations (roles DML) first.");
        }

        const string insertUserSql = """
            INSERT INTO auth.users (email, password_hash, first_name, last_name, tenant_id)
            VALUES (@Email, @PasswordHash, @FirstName, @LastName, NULL)
            ON CONFLICT (email) DO NOTHING
            RETURNING id;
            """;

        Guid? userId = await db.QuerySingleOrDefaultAsync<Guid?>(insertUserSql, new
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName
        }, tx);

        if (userId is null)
        {
            // User already exists — still ensure the platform_admin assignment
            // is present so a partially-seeded state is repaired.
            userId = await db.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT id FROM auth.users WHERE email = @Email AND is_deleted = FALSE",
                new { Email = email }, tx);

            if (userId is null)
            {
                tx.Rollback();
                return Error($"User with email '{email}' exists but could not be loaded.");
            }

            Console.WriteLine($"User '{email}' already exists. Ensuring platform_admin role is assigned.");
        }
        else
        {
            Console.WriteLine($"Super admin created successfully. ID: {userId}");
        }

        // Idempotent assignment: re-activate the link if it was soft-deleted,
        // otherwise no-op. created_by is NULL for the bootstrap account.
        int affected = await db.ExecuteAsync(
            """
            INSERT INTO auth.user_roles (user_id, role_id)
            VALUES (@UserId, @RoleId)
            ON CONFLICT (user_id, role_id)
            DO UPDATE SET is_deleted = FALSE, updated_at = NOW()
            WHERE auth.user_roles.is_deleted = TRUE
            """,
            new { UserId = userId, RoleId = roleId }, tx);

        tx.Commit();

        Console.WriteLine(affected > 0
            ? "platform_admin role assigned."
            : "platform_admin role was already assigned.");
        return 0;
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}

static string? ReadPasswordMasked()
{
    var password = new System.Text.StringBuilder();
    while (true)
    {
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return password.ToString();
        }
        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Length--;
            Console.Write("\b \b");
        }
        else if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
            Console.Write('*');
        }
    }
}

static string? BuildConnectionString(IConfiguration config)
{
    string? host = config["Database:Host"];
    string? port = config["Database:Port"];
    string? name = config["Database:Name"];
    string? username = config["Database:Username"];
    string? password = config["Database:Password"];

    if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username))
    {
        Error("Database configuration incomplete. Check appsettings.json.");
        return null;
    }

    if (string.IsNullOrWhiteSpace(password))
    {
        Error("Database:Password not configured. Set it via: dotnet user-secrets set \"Database:Password\" \"<value>\"");
        return null;
    }

    return $"Host={host};Port={port ?? "5432"};Database={name};Username={username};Password={password}";
}

static int Error(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ResetColor();
    return 1;
}
