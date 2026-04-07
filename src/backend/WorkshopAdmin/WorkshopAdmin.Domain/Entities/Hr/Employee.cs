namespace WorkshopAdmin.Domain.Entities.Hr;

using WorkshopAdmin.Domain.Common;

public class Employee : BaseTenantEntity
{
    public Guid UserId { get; set; }
    public short EmploymentTypeId { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Notes { get; set; }
}
