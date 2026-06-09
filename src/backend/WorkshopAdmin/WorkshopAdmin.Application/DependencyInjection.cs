namespace WorkshopAdmin.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Application.Features.Auth;
using WorkshopAdmin.Application.Features.Permission;
using WorkshopAdmin.Application.Features.Role;
using WorkshopAdmin.Application.Features.Tenant;
using WorkshopAdmin.Application.Features.User;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
