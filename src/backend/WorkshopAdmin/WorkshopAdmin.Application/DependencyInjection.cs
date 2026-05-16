namespace WorkshopAdmin.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Application.Features.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
