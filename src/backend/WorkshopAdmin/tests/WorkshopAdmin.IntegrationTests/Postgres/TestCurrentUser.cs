using WorkshopAdmin.SharedKernel.Auth;

namespace WorkshopAdmin.IntegrationTests.Postgres;

public sealed class TestCurrentUser(
    Guid? tenantId = null, bool isPlatformAdmin = false, Guid? userId = null) : ICurrentUser
{
    public Guid? UserId { get; } = userId;
    public Guid? TenantId { get; } = tenantId;
    public bool IsPlatformAdmin { get; } = isPlatformAdmin;
    public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
}
