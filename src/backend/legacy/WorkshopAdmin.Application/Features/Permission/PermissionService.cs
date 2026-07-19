namespace WorkshopAdmin.Application.Features.Permission;

using System.Data.Common;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Persistence;
using WorkshopAdmin.Application.Features.Permission.Models;

public sealed class PermissionService(
    IDbConnectionFactory connectionFactory,
    IPermissionRepository permissionRepository,
    ITenantContext tenantContext) : IPermissionService
{
    public async Task<IReadOnlyList<PermissionItem>> ListAsync(CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await permissionRepository.ListAsync(
            tenantScopeOnly: tenantContext.TenantId is not null, connection, cancellationToken);
    }
}
