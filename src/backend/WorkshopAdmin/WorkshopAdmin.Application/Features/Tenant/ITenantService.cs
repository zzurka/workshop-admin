namespace WorkshopAdmin.Application.Features.Tenant;

using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Features.Tenant.Create;
using WorkshopAdmin.Application.Features.Tenant.GetById;
using WorkshopAdmin.Application.Features.Tenant.List;
using WorkshopAdmin.Application.Features.Tenant.Update;

public interface ITenantService
{
    Task<PagedResponse<TenantListItem>> ListAsync(ListTenantsRequest request, CancellationToken cancellationToken);

    Task<TenantDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken);

    Task SetActivationAsync(Guid id, bool isActive, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
