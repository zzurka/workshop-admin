using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkshopAdmin.SharedKernel.Database;

public static class DbSessionServiceCollectionExtensions
{
    public static IServiceCollection AddDbSession(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddScoped<IDbSession, DbSession>();
        return services;
    }
}
