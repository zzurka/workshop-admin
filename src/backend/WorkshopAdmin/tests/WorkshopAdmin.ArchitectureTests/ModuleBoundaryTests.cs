using System.Reflection;
using WorkshopAdmin.Modules.Auth;
using WorkshopAdmin.Modules.Codebook;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Customers;
using WorkshopAdmin.Modules.Hr;
using WorkshopAdmin.Modules.Notifications;
using WorkshopAdmin.Modules.Tenants;
using WorkshopAdmin.Modules.Tenants.Contracts;
using WorkshopAdmin.Modules.Warehouse;
using WorkshopAdmin.Modules.Workshop;
using WorkshopAdmin.SharedKernel.Modules;
using Xunit;

namespace WorkshopAdmin.ArchitectureTests;

/// <summary>
/// Enforces backend plan §3/§8: a module's public contract surface lives in its own
/// <c>WorkshopAdmin.Modules.X.Contracts</c> project (created lazily, once a consumer
/// exists — decision refined in F2). A module may reference SharedKernel and Contracts
/// projects of other modules, never another module itself — which also keeps mutual
/// contract consumption (e.g. Workshop ⇄ Warehouse in F6/F7) acyclic. Everything
/// outside Contracts stays internal, so the compiler enforces the rest.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly (Assembly Assembly, string Namespace)[] Modules =
    [
        (typeof(TenantsModule).Assembly, "WorkshopAdmin.Modules.Tenants"),
        (typeof(AuthModule).Assembly, "WorkshopAdmin.Modules.Auth"),
        (typeof(CodebookModule).Assembly, "WorkshopAdmin.Modules.Codebook"),
        (typeof(CustomersModule).Assembly, "WorkshopAdmin.Modules.Customers"),
        (typeof(HrModule).Assembly, "WorkshopAdmin.Modules.Hr"),
        (typeof(WorkshopModule).Assembly, "WorkshopAdmin.Modules.Workshop"),
        (typeof(WarehouseModule).Assembly, "WorkshopAdmin.Modules.Warehouse"),
        (typeof(NotificationsModule).Assembly, "WorkshopAdmin.Modules.Notifications")
    ];

    private static readonly Assembly[] ContractsAssemblies =
    [
        typeof(ICodebookLookup).Assembly,
        typeof(TenantSubscriptionChanged).Assembly
    ];

    [Fact]
    public void Modules_ReferenceOnlySharedKernelAndContracts_AmongWorkshopAdminAssemblies()
    {
        List<string> violations = [];

        foreach ((Assembly assembly, string ownNamespace) in Modules)
        {
            IEnumerable<string> forbidden = assembly.GetReferencedAssemblies()
                .Select(a => a.Name!)
                .Where(name => name.StartsWith("WorkshopAdmin", StringComparison.Ordinal)
                               && name != "WorkshopAdmin.SharedKernel"
                               && !name.EndsWith(".Contracts", StringComparison.Ordinal));

            violations.AddRange(forbidden.Select(name => $"{ownNamespace} references {name}"));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void ContractsProjects_ReferenceOnlySharedKernel_AmongWorkshopAdminAssemblies()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ContractsAssemblies)
        {
            IEnumerable<string> forbidden = assembly.GetReferencedAssemblies()
                .Select(a => a.Name!)
                .Where(name => name.StartsWith("WorkshopAdmin", StringComparison.Ordinal)
                               && name != "WorkshopAdmin.SharedKernel");

            violations.AddRange(forbidden.Select(name => $"{assembly.GetName().Name} references {name}"));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Modules_ExposePubliclyOnlyTheModuleClass()
    {
        List<string> violations = [];

        foreach ((Assembly assembly, string ownNamespace) in Modules)
        {
            IEnumerable<Type> offenders = assembly.GetExportedTypes()
                .Where(type => !typeof(IModule).IsAssignableFrom(type));

            violations.AddRange(offenders.Select(type => $"{ownNamespace}: {type.FullName}"));
        }

        Assert.Empty(violations);
    }
}
