using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Modules;

namespace WorkshopAdmin.Modules.Customers;

public sealed class CustomersModule : IModule
{
    public string Name => "Customers";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
