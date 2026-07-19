using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Modules.Tenants.Features.Subscriptions;
using WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;
using WorkshopAdmin.Modules.Tenants.Features.Tenants;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Modules;
using WorkshopAdmin.SharedKernel.Persistence;

namespace WorkshopAdmin.Modules.Tenants;

public sealed class TenantsModule : IModule
{
    public string Name => "Tenants";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<TenantsDbContext>();
        services.AddValidatorsFromAssembly(typeof(TenantsModule).Assembly, includeInternalTypes: true);

        services.AddScoped<CreateSubscriptionPlanHandler>();
        services.AddScoped<UpdateSubscriptionPlanHandler>();
        services.AddScoped<SetSubscriptionPlanActivationHandler>();
        services.AddScoped<CreateTenantHandler>();
        services.AddScoped<UpdateTenantHandler>();
        services.AddScoped<SetTenantActivationHandler>();
        services.AddScoped<DeleteTenantHandler>();
        services.AddScoped<ChangeTenantSubscriptionHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder plans = endpoints.MapGroup("/subscription-plans");
        ListSubscriptionPlans.Map(plans);
        CreateSubscriptionPlan.Map(plans);
        UpdateSubscriptionPlan.Map(plans);
        SetSubscriptionPlanActivation.Map(plans);

        RouteGroupBuilder tenants = endpoints.MapGroup("/tenants");
        ListTenants.Map(tenants);
        GetTenantById.Map(tenants);
        CreateTenant.Map(tenants);
        UpdateTenant.Map(tenants);
        SetTenantActivation.Map(tenants);
        DeleteTenant.Map(tenants);
        ListTenantSubscriptions.Map(tenants);
        ChangeTenantSubscription.Map(tenants);
    }
}
