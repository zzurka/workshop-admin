namespace WorkshopAdmin.Domain.Entities.Workshop;

public class AppointmentService
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public short ServiceTypeId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
