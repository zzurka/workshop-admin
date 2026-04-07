namespace WorkshopAdmin.Domain.Entities.Warehouse;

using WorkshopAdmin.Domain.Common;

public class StockTransaction : BaseEventEntity
{
    public Guid TenantId { get; set; }
    public Guid CatalogPartId { get; set; }
    public short TransactionTypeId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
}
