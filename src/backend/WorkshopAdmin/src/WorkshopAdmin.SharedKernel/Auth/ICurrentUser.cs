namespace WorkshopAdmin.SharedKernel.Auth;

/// <summary>
/// Identity of the caller for the current request, sourced from JWT claims.
/// Anonymous requests (login, refresh, …) have all members empty/false.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>Active tenant — drives the RLS context of the request transaction.</summary>
    Guid? TenantId { get; }

    bool IsPlatformAdmin { get; }

    IReadOnlySet<string> Permissions { get; }
}
