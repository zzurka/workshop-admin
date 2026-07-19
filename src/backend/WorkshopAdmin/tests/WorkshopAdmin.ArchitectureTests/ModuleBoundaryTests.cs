using System.Reflection;
using NetArchTest.Rules;
using WorkshopAdmin.Modules.Auth;
using WorkshopAdmin.Modules.Codebook;
using WorkshopAdmin.Modules.Customers;
using WorkshopAdmin.Modules.Hr;
using WorkshopAdmin.Modules.Notifications;
using WorkshopAdmin.Modules.Tenants;
using WorkshopAdmin.Modules.Warehouse;
using WorkshopAdmin.Modules.Workshop;
using WorkshopAdmin.SharedKernel.Modules;
using Xunit;

namespace WorkshopAdmin.ArchitectureTests;

/// <summary>
/// Enforces backend plan §3: modules depend only on SharedKernel, never on each other,
/// and expose nothing publicly except their Contracts folder and the IModule class.
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

    [Fact]
    public void Modules_DoNotDependOnOtherModules()
    {
        List<string> violations = [];

        foreach ((Assembly assembly, string ownNamespace) in Modules)
        {
            string[] otherModuleNamespaces = Modules
                .Where(m => m.Namespace != ownNamespace)
                .Select(m => m.Namespace)
                .ToArray();

            TestResult result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOnAny(otherModuleNamespaces)
                .GetResult();

            if (!result.IsSuccessful)
            {
                violations.AddRange(result.FailingTypeNames.Select(t => $"{ownNamespace}: {t}"));
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Modules_ReferenceOnlySharedKernel_AmongWorkshopAdminAssemblies()
    {
        List<string> violations = [];

        foreach ((Assembly assembly, string ownNamespace) in Modules)
        {
            IEnumerable<string> forbidden = assembly.GetReferencedAssemblies()
                .Select(a => a.Name!)
                .Where(name => name.StartsWith("WorkshopAdmin", StringComparison.Ordinal)
                               && name != "WorkshopAdmin.SharedKernel");

            violations.AddRange(forbidden.Select(name => $"{ownNamespace} references {name}"));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Modules_ExposePubliclyOnlyContractsAndTheModuleClass()
    {
        List<string> violations = [];

        foreach ((Assembly assembly, string ownNamespace) in Modules)
        {
            IEnumerable<Type> offenders = assembly.GetExportedTypes()
                .Where(type => !typeof(IModule).IsAssignableFrom(type)
                               && !(type.Namespace ?? "").Contains(".Contracts"));

            violations.AddRange(offenders.Select(type => $"{ownNamespace}: {type.FullName}"));
        }

        Assert.Empty(violations);
    }
}
