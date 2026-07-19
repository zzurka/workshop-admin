using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using WorkshopAdmin.SharedKernel.Database;

namespace WorkshopAdmin.SharedKernel.Persistence;

public static class ModuleDbContextRegistration
{
    /// <summary>
    /// Registers a module DbContext bound to the request's <see cref="IDbSession"/>:
    /// same connection, same transaction, same RLS context as every other data access
    /// in the request.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        services.TryAddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<TContext>(
            (serviceProvider, options) =>
            {
                IDbSession session = serviceProvider.GetRequiredService<IDbSession>();
                NpgsqlConnection connection = session.GetOpenConnection();
                options
                    .UseNpgsql(connection)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        return services;
    }
}
