using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkshopAdmin.SharedKernel.Modules;

/// <summary>
/// Self-registration contract for a module. The host instantiates each module,
/// calls <see cref="AddModule"/> during service registration and
/// <see cref="MapEndpoints"/> after the app is built.
/// </summary>
public interface IModule
{
    string Name { get; }

    void AddModule(IServiceCollection services, IConfiguration configuration);

    /// <param name="endpoints">The shared <c>/api</c> route group.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
