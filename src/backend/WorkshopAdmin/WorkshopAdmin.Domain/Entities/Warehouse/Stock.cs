namespace WorkshopAdmin.Domain.Entities.Warehouse;

public class Stock
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CatalogPartId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public string? BinLocation { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
