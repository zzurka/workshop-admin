namespace WorkshopAdmin.Domain.Entities.Workshop;

using WorkshopAdmin.Domain.Common;

public class WorkOrder : BaseTenantEntity
{
    public Guid AppointmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public short WorkOrderStatusId { get; set; }
    public string? Description { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}
