using System.Security.Claims;
using Scalar.AspNetCore;
using Serilog;
using WorkshopAdmin.Modules.Auth;
using WorkshopAdmin.Modules.Codebook;
using WorkshopAdmin.Modules.Customers;
using WorkshopAdmin.Modules.Hr;
using WorkshopAdmin.Modules.Notifications;
using WorkshopAdmin.Modules.Tenants;
using WorkshopAdmin.Modules.Warehouse;
using WorkshopAdmin.Modules.Workshop;
using WorkshopAdmin.SharedKernel.Auth;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Events;
using WorkshopAdmin.SharedKernel.Http;
using WorkshopAdmin.SharedKernel.Modules;
using WorkshopAdmin.SharedKernel.Persistence;

// Bootstrap logger: catches startup failures before the configuration-driven
// logger is built (it is replaced by UseSerilog below).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddOpenApi();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
    builder.Services.AddDbSession(builder.Configuration);
    builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

    // Composition root: explicit module list — the compiler guards it, no assembly scanning.
    IModule[] modules =
    [
        new TenantsModule(),
        new AuthModule(),
        new CodebookModule(),
        new CustomersModule(),
        new HrModule(),
        new WorkshopModule(),
        new WarehouseModule(),
        new NotificationsModule()
    ];

    foreach (IModule module in modules)
    {
        module.AddModule(builder.Services, builder.Configuration);
    }

    WebApplication app = builder.Build();

    // Outermost so the request log records the final status code.
    app.UseSerilogRequestLogging(options =>
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                diagnosticContext.Set("UserId", userId);
            }

            string? tenantId = httpContext.User.FindFirstValue("tenant_id");
            if (tenantId is not null)
            {
                diagnosticContext.Set("TenantId", tenantId);
            }
        });

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<DbSessionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    RouteGroupBuilder api = app.MapGroup("/api");
    foreach (IModule module in modules)
    {
        module.MapEndpoints(api);
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory in integration tests
public partial class Program;
