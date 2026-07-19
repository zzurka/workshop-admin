using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Persistence;

namespace WorkshopAdmin.Modules.Codebook;

/// <summary>A cache-friendly snapshot of one codebook row; doubles as the list response shape.</summary>
internal sealed record CodebookEntryItem(short Id, string Code, Dictionary<string, string> Label, short SortOrder, bool IsActive);

/// <summary>
/// One codebook table as seen by the generic slices. The typed subclass keeps every EF
/// query statically typed on the concrete entity — the <c>{type}</c> route value maps
/// through the registry and never reaches SQL.
/// </summary>
internal abstract class CodebookType
{
    /// <summary>Route slug — equals the table name (e.g. <c>fuel_types</c>).</summary>
    public required string Slug { get; init; }

    public required Type ClrType { get; init; }

    public abstract CodebookEntry CreateInstance();

    public abstract Task<CodebookEntry?> FindAsync(CodebookDbContext db, short id, CancellationToken cancellationToken);

    public abstract Task<bool> CodeExistsAsync(CodebookDbContext db, string code, CancellationToken cancellationToken);

    public abstract Task<List<CodebookEntryItem>> LoadAllAsync(CodebookDbContext db, CancellationToken cancellationToken);
}

internal sealed class CodebookType<TEntity> : CodebookType where TEntity : CodebookEntry, new()
{
    public override CodebookEntry CreateInstance() => new TEntity();

    public override async Task<CodebookEntry?> FindAsync(
        CodebookDbContext db, short id, CancellationToken cancellationToken) =>
        await db.Set<TEntity>().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public override Task<bool> CodeExistsAsync(CodebookDbContext db, string code, CancellationToken cancellationToken) =>
        db.Set<TEntity>().AnyAsync(e => e.Code == code, cancellationToken);

    public override Task<List<CodebookEntryItem>> LoadAllAsync(
        CodebookDbContext db, CancellationToken cancellationToken) =>
        db.Set<TEntity>()
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Code)
            .Select(e => new CodebookEntryItem(e.Id, e.Code, e.Label, e.SortOrder, e.IsActive))
            .ToListAsync(cancellationToken);
}

/// <summary>
/// The single source of truth for which codebook tables exist (backend plan F2, O1/O5):
/// slug → typed accessor. <see cref="CodebookDbContext"/> builds its model from this
/// list, so adding a table here maps it, exposes it through the API and enrolls it in
/// the EF drift test at once.
/// </summary>
internal sealed class CodebookRegistry
{
    private readonly Dictionary<string, CodebookType> _types;

    public CodebookRegistry()
    {
        CodebookType[] types =
        [
            Create<AppointmentStatus>("appointment_statuses"),
            Create<BillingPeriod>("billing_periods"),
            Create<ComplaintStatus>("complaint_statuses"),
            Create<Currency>("currencies"),
            Create<EmploymentType>("employment_types"),
            Create<ExpenseCategory>("expense_categories"),
            Create<FuelType>("fuel_types"),
            Create<InvoiceStatus>("invoice_statuses"),
            Create<LeaveStatus>("leave_statuses"),
            Create<LeaveType>("leave_types"),
            Create<PartStatus>("part_statuses"),
            Create<PaymentMethod>("payment_methods"),
            Create<PayrollRunStatus>("payroll_run_statuses"),
            Create<PurchaseOrderStatus>("purchase_order_statuses"),
            Create<ServiceType>("service_types"),
            Create<StockTransactionType>("stock_transaction_types"),
            Create<TaxRate>("tax_rates"),
            Create<TransmissionType>("transmission_types"),
            Create<UnitOfMeasure>("units_of_measure"),
            Create<WorkOrderStatus>("work_order_statuses")
        ];

        _types = types.ToDictionary(t => t.Slug);
    }

    public IReadOnlyCollection<CodebookType> All => _types.Values;

    public IReadOnlyList<string> Slugs => _types.Keys.Order().ToList();

    public bool TryGet(string slug, out CodebookType type) => _types.TryGetValue(slug, out type!);

    private static CodebookType<TEntity> Create<TEntity>(string slug) where TEntity : CodebookEntry, new() =>
        new() { Slug = slug, ClrType = typeof(TEntity) };
}
