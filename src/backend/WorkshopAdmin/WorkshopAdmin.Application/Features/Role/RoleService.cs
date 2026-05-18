namespace WorkshopAdmin.Application.Features.Role;

using System.Data.Common;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Role.List;
using WorkshopAdmin.Domain.Exceptions;

public sealed class RoleService(
    IDbConnectionFactory connectionFactory,
    IRoleRepository roleRepository,
    ITenantContext tenantContext) : IRoleService
{
    public async Task<IReadOnlyList<RoleListItem>> ListAssignableAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await roleRepository.ListAssignableAsync(tenantId, connection, cancellationToken);
    }

    private Guid RequireTenant()
        => tenantContext.TenantId
           ?? throw new ForbiddenException("A tenant context is required to list assignable roles.");
}
