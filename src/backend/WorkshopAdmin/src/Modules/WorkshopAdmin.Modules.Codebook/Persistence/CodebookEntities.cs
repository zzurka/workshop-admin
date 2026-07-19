namespace WorkshopAdmin.Modules.Codebook.Persistence;

/// <summary>
/// Common shape of every <c>codebook</c> table: <c>id SMALLSERIAL, code, label JSONB,
/// sort_order, is_active</c>. Not itself an entity type — each subclass maps to its own
/// table (no EF inheritance hierarchy). Codebook tables carry no audit columns.
/// </summary>
internal abstract class CodebookEntry
{
    public short Id { get; set; }
    public string Code { get; set; } = "";
    public Dictionary<string, string> Label { get; set; } = [];
    public short SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

internal sealed class AppointmentStatus : CodebookEntry;
internal sealed class BillingPeriod : CodebookEntry;
internal sealed class ComplaintStatus : CodebookEntry;
internal sealed class Currency : CodebookEntry;
internal sealed class EmploymentType : CodebookEntry;
internal sealed class ExpenseCategory : CodebookEntry;
internal sealed class FuelType : CodebookEntry;
internal sealed class InvoiceStatus : CodebookEntry;
internal sealed class LeaveStatus : CodebookEntry;
internal sealed class LeaveType : CodebookEntry;
internal sealed class PartStatus : CodebookEntry;
internal sealed class PaymentMethod : CodebookEntry;
internal sealed class PayrollRunStatus : CodebookEntry;
internal sealed class PurchaseOrderStatus : CodebookEntry;
internal sealed class StockTransactionType : CodebookEntry;
internal sealed class TransmissionType : CodebookEntry;
internal sealed class UnitOfMeasure : CodebookEntry;
internal sealed class WorkOrderStatus : CodebookEntry;

/// <summary>Deviation: VAT rates additionally carry the percentage (CHECK 0–100).</summary>
internal sealed class TaxRate : CodebookEntry
{
    public decimal Rate { get; set; }
}

/// <summary>Deviation: service types additionally carry a typical duration in minutes.</summary>
internal sealed class ServiceType : CodebookEntry
{
    public short? DefaultDurationMin { get; set; }
}
