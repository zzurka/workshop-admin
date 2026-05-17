namespace WorkshopAdmin.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Application.Features.Auth;
using WorkshopAdmin.Application.Features.Tenant;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();

        return services;
    }
}
