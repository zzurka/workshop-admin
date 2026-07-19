namespace WorkshopAdmin.Modules.Codebook.Contracts;

/// <summary>
/// Cross-module read access to codebooks (backend plan §8.2). Reads through the same
/// cache as the HTTP slices. <paramref name="type"/> is the codebook slug — use the
/// <see cref="CodebookTypes"/> constants.
/// </summary>
public interface ICodebookLookup
{
    /// <summary>Id of the entry with the given code, or null when the type or code is unknown.</summary>
    Task<short?> GetIdByCodeAsync(string type, string code, CancellationToken cancellationToken = default);

    /// <summary>The entry with the given id, or null when the type or id is unknown.</summary>
    Task<CodebookEntryRef?> GetByIdAsync(string type, short id, CancellationToken cancellationToken = default);
}

public sealed record CodebookEntryRef(short Id, string Code, IReadOnlyDictionary<string, string> Label, bool IsActive);

/// <summary>Codebook type slugs (= table names in the <c>codebook</c> schema).</summary>
public static class CodebookTypes
{
    public const string AppointmentStatuses = "appointment_statuses";
    public const string BillingPeriods = "billing_periods";
    public const string ComplaintStatuses = "complaint_statuses";
    public const string Currencies = "currencies";
    public const string EmploymentTypes = "employment_types";
    public const string ExpenseCategories = "expense_categories";
    public const string FuelTypes = "fuel_types";
    public const string InvoiceStatuses = "invoice_statuses";
    public const string LeaveStatuses = "leave_statuses";
    public const string LeaveTypes = "leave_types";
    public const string PartStatuses = "part_statuses";
    public const string PaymentMethods = "payment_methods";
    public const string PayrollRunStatuses = "payroll_run_statuses";
    public const string PurchaseOrderStatuses = "purchase_order_statuses";
    public const string ServiceTypes = "service_types";
    public const string StockTransactionTypes = "stock_transaction_types";
    public const string TaxRates = "tax_rates";
    public const string TransmissionTypes = "transmission_types";
    public const string UnitsOfMeasure = "units_of_measure";
    public const string WorkOrderStatuses = "work_order_statuses";
}
