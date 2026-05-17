namespace WorkshopAdmin.Infrastructure;

using System.Text;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Infrastructure.Email;
using WorkshopAdmin.Infrastructure.Persistence;
using WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;
using WorkshopAdmin.Infrastructure.Persistence.Repositories.Codebook;
using WorkshopAdmin.Infrastructure.Persistence.Repositories.Tenant;
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

        services.AddSingleton(BuildJwtOptions(configuration));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ICodebookRepository, CodebookRepository>();

        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }

    private static JwtOptions BuildJwtOptions(IConfiguration configuration)
    {
        string signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "Jwt:SigningKey not configured. Set it via: dotnet user-secrets set \"Jwt:SigningKey\" \"<value>\"");

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes for HMAC-SHA256.");
        }

        return new JwtOptions
        {
            Issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured."),
            Audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured."),
            SigningKey = signingKey,
            AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out int minutes) ? minutes : 15,
            RefreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out int days) ? days : 14
        };
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
