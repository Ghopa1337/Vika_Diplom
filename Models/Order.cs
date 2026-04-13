namespace CargoTransport.Desktop.Models;

public class Order
{
    public uint Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public uint ReceiverUserId { get; set; }
    public uint CargoId { get; set; }
    public uint? DriverId { get; set; }
    public uint? VehicleId { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal? TotalCost { get; set; }
    public string Status { get; set; } = "created";
    public DateTime? PlannedPickupAt { get; set; }
    public DateTime? DesiredDeliveryAt { get; set; }
    public DateTime? ActualPickupAt { get; set; }
    public DateTime? ActualDeliveryAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? CancellationReason { get; set; }
    public uint CreatedByUserId { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User ReceiverUser { get; set; } = null!;
    public CargoItem Cargo { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
