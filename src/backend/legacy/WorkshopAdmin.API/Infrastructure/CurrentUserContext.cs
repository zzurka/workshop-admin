namespace WorkshopAdmin.API.Infrastructure;

using System.Security.Claims;
using WorkshopAdmin.Application.Common.Interfaces;

public class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid UserId
    {
        get
        {
            string? claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim) : throw new UnauthorizedAccessException("User is not authenticated.");
        }
    }
}
