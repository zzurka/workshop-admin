namespace WorkshopAdmin.Infrastructure;

using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Infrastructure.Persistence;
using WorkshopAdmin.Infrastructure.Persistence.TypeHandlers;
using WorkshopAdmin.Infrastructure.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new JsonbTypeHandler());

        string connectionString = BuildConnectionString(configuration);

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

        // Repositories will be registered here as they are implemented

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        string host = configuration["Database:Host"] ?? throw new InvalidOperationException("Database:Host not configured.");
        string port = configuration["Database:Port"] ?? "5432";
        string name = configuration["Database:Name"] ?? throw new InvalidOperationException("Database:Name not configured.");
        string username = configuration["Database:Username"] ?? throw new InvalidOperationException("Database:Username not configured.");
        string password = configuration["Database:Password"] ?? throw new InvalidOperationException("Database:Password not configured. Set it via: dotnet user-secrets set \"Database:Password\" \"<value>\"");

        return $"Host={host};Port={port};Database={name};Username={username};Password={password}";
    }
}
