using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Modules;

namespace WorkshopAdmin.Modules.Tenants;

public sealed class TenantsModule : IModule
{
    public string Name => "Tenants";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
