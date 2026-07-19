namespace WorkshopAdmin.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
}
