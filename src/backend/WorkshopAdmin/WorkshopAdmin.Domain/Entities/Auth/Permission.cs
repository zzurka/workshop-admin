namespace WorkshopAdmin.Domain.Entities.Auth;

using WorkshopAdmin.Domain.Common;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
}
