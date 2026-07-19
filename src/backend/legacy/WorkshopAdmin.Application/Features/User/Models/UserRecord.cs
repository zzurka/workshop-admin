namespace WorkshopAdmin.Application.Features.User.Models;

/// <summary>Scalar user columns loaded for the detail view (roles fetched separately).</summary>
public sealed class UserRecord
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
