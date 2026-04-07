namespace WorkshopAdmin.API.Infrastructure;

using System.Security.Claims;
using WorkshopAdmin.Application.Common.Interfaces;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            string? claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim) : throw new UnauthorizedAccessException("User is not authenticated.");
        }
    }
}
