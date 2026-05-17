namespace WorkshopAdmin.API.Controllers.Tenant;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.API.Authorization;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Features.Tenant;
using WorkshopAdmin.Application.Features.Tenant.Activation;
using WorkshopAdmin.Application.Features.Tenant.Create;
using WorkshopAdmin.Application.Features.Tenant.GetById;
using WorkshopAdmin.Application.Features.Tenant.List;
using WorkshopAdmin.Application.Features.Tenant.Update;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "platform_admin")]
public sealed class TenantsController(ITenantService tenantService) : ControllerBase
{
    [HttpGet]
    [HasPermission("tenants:read")]
    public async Task<ActionResult<PagedResponse<TenantListItem>>> List([FromQuery] ListTenantsRequest request, CancellationToken cancellationToken)
        => Ok(await tenantService.ListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission("tenants:read")]
    public async Task<ActionResult<TenantDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await tenantService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("tenants:create")]
    public async Task<ActionResult<CreateTenantResponse>> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        CreateTenantResponse result = await tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.TenantId }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("tenants:update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        await tenantService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activation")]
    [HasPermission("tenants:deactivate")]
    public async Task<IActionResult> SetActivation(Guid id, [FromBody] SetTenantActivationRequest request, CancellationToken cancellationToken)
    {
        await tenantService.SetActivationAsync(id, request.IsActive, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("tenants:delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await tenantService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
