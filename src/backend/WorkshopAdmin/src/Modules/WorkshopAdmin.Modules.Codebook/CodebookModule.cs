using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Modules;

namespace WorkshopAdmin.Modules.Codebook;

public sealed class CodebookModule : IModule
{
    public string Name => "Codebook";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
