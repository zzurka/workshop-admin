namespace WorkshopAdmin.Application.Features.Role.Models;

/// <summary>Scalar role columns loaded for ownership/system guards and the detail view.</summary>
public sealed class RoleRecord
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
