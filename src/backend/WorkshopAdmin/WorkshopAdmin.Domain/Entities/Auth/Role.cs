namespace WorkshopAdmin.Domain.Entities.Auth;

using WorkshopAdmin.Domain.Common;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
