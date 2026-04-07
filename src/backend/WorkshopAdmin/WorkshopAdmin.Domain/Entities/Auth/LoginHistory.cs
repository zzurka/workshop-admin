namespace WorkshopAdmin.Domain.Entities.Auth;

using WorkshopAdmin.Domain.Common;

public class LoginHistory : BaseEventEntity
{
    public Guid UserId { get; set; }
    public string LoginMethod { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}
