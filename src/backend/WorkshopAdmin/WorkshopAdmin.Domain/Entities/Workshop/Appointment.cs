namespace WorkshopAdmin.Domain.Entities.Workshop;

using WorkshopAdmin.Domain.Common;

public class Appointment : BaseTenantEntity
{
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public short AppointmentStatusId { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
}
