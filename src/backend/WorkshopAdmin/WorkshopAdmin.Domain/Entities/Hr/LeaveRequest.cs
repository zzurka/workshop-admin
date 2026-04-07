namespace WorkshopAdmin.Domain.Entities.Hr;

using WorkshopAdmin.Domain.Common;

public class LeaveRequest : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public short LeaveTypeId { get; set; }
    public short LeaveStatusId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string? Notes { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
