namespace WorkshopAdmin.Domain.Entities.Hr;

using WorkshopAdmin.Domain.Common;

public class LeaveBalance : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public short LeaveTypeId { get; set; }
    public short Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
}
