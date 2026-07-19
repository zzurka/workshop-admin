using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Modules;

namespace WorkshopAdmin.Modules.Hr;

public sealed class HrModule : IModule
{
    public string Name => "Hr";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
