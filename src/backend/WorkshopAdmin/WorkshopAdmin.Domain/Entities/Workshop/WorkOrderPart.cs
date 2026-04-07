namespace WorkshopAdmin.Domain.Entities.Workshop;

using WorkshopAdmin.Domain.Common;

public class WorkOrderPart : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public Guid? CatalogPartId { get; set; }
    public Guid? SupplierId { get; set; }
    public short PartStatusId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public short Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Notes { get; set; }
}
