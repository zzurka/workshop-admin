namespace WorkshopAdmin.Domain.Entities.Auth;

using WorkshopAdmin.Domain.Common;

public class MfaRecoveryCode : BaseEventEntity
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
