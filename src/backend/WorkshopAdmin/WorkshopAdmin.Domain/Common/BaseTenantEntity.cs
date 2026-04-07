namespace WorkshopAdmin.Domain.Common;

public abstract class BaseTenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
