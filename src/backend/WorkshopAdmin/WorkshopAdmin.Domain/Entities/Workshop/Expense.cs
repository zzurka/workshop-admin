namespace WorkshopAdmin.Domain.Entities.Workshop;

using WorkshopAdmin.Domain.Common;

public class Expense : BaseTenantEntity
{
    public short ExpenseCategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? Notes { get; set; }
}
