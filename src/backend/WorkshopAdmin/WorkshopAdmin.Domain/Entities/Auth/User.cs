namespace WorkshopAdmin.Domain.Entities.Auth;

using WorkshopAdmin.Domain.Common;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? AdObjectId { get; set; }
    public string? AdUpn { get; set; }
    public string? PhoneNumber { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaMethod { get; set; }
    public string? MfaSecret { get; set; }
    public bool IsActive { get; set; }
}
