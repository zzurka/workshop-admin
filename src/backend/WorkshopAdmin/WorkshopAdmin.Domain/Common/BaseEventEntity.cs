namespace WorkshopAdmin.Domain.Common;

public abstract class BaseEventEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
