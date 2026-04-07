namespace WorkshopAdmin.Domain.Entities.Hr;

using WorkshopAdmin.Domain.Common;

public class TimeEntry : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public short BreakDurationMin { get; set; }
    public string? Notes { get; set; }
}
