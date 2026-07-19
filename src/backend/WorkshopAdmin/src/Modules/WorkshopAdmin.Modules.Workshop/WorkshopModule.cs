using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Modules;

namespace WorkshopAdmin.Modules.Workshop;

public sealed class WorkshopModule : IModule
{
    public string Name => "Workshop";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
