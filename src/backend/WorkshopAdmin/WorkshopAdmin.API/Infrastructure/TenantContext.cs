namespace WorkshopAdmin.API.Infrastructure;

using System.Security.Claims;
using WorkshopAdmin.Application.Common.Interfaces;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? TenantId
    {
        get
        {
            string? claim = _httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id");
            return claim is not null ? Guid.Parse(claim) : null;
        }
    }
}
