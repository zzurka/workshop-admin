namespace WorkshopAdmin.Domain.Entities.Workshop;

using WorkshopAdmin.Domain.Common;

public class Invoice : BaseTenantEntity
{
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? AppointmentId { get; set; }
    public short InvoiceStatusId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}
