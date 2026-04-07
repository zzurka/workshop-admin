namespace WorkshopAdmin.Domain.Entities.Warehouse;

using WorkshopAdmin.Domain.Common;

public class CatalogPart : BaseTenantEntity
{
    public string? PartNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public short UnitOfMeasureId { get; set; }
    public Guid? DefaultSupplierId { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal MinStockLevel { get; set; }
    public bool IsActive { get; set; }
}
