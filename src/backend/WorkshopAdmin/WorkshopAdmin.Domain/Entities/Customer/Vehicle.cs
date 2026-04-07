namespace WorkshopAdmin.Domain.Entities.Customer;

using WorkshopAdmin.Domain.Common;

public class Vehicle : BaseTenantEntity
{
    public Guid CustomerId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public short? Year { get; set; }
    public string? Vin { get; set; }
    public string? LicensePlate { get; set; }
    public string? Color { get; set; }
    public short? FuelTypeId { get; set; }
    public string? EngineType { get; set; }
    public short? TransmissionId { get; set; }
    public int? Mileage { get; set; }
    public DateTime? MileageRecordedAt { get; set; }
    public string? Notes { get; set; }
}
