using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Codebook.Features;
using WorkshopAdmin.Modules.Codebook.Infrastructure;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Modules;
using WorkshopAdmin.SharedKernel.Persistence;

namespace WorkshopAdmin.Modules.Codebook;

public sealed class CodebookModule : IModule
{
    public string Name => "Codebook";

    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddModuleDbContext<CodebookDbContext>();
        services.AddSingleton<CodebookRegistry>();
        services.AddSingleton<CodebookCache>();
        services.AddScoped<ICodebookLookup, CodebookLookup>();
        services.AddValidatorsFromAssembly(typeof(CodebookModule).Assembly, includeInternalTypes: true);

        services.AddScoped<ListCodebookHandler>();
        services.AddScoped<CreateCodebookEntryHandler>();
        services.AddScoped<UpdateCodebookEntryHandler>();
        services.AddScoped<SetCodebookEntryActivationHandler>();
        services.AddScoped<CreateTaxRateHandler>();
        services.AddScoped<UpdateTaxRateHandler>();
        services.AddScoped<CreateServiceTypeHandler>();
        services.AddScoped<UpdateServiceTypeHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/codebook");

        ListCodebook.Map(group);
        CreateCodebookEntry.Map(group);
        UpdateCodebookEntry.Map(group);
        SetCodebookEntryActivation.Map(group);
        CreateTaxRate.Map(group);
        UpdateTaxRate.Map(group);
        CreateServiceType.Map(group);
        UpdateServiceType.Map(group);
    }
}
