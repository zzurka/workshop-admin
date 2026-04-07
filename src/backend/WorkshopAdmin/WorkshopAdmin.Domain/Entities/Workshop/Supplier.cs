namespace WorkshopAdmin.Domain.Entities.Workshop;

using WorkshopAdmin.Domain.Common;

public class Supplier : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
