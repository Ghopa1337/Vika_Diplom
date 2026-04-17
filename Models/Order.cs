using Sieve.Attributes;

namespace CargoTransport.Desktop.Models;

public class Order
{
    [Sieve(CanFilter = true, CanSort = true)]
    public uint Id { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string OrderNumber { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public uint ReceiverUserId { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public uint CargoId { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public uint? DriverId { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public uint? VehicleId { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string PickupAddress { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal? DistanceKm { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal? TotalCost { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string Status { get; set; } = OrderStatuses.Created;

    public static class OrderStatuses
    {
        public const string Created = "created";
        public const string Assigned = "assigned";
        public const string Accepted = "accepted";
        public const string Loading = "loading";
        public const string InTransit = "in_transit";
        public const string Delivered = "delivered";
        public const string Received = "received";
        public const string Cancelled = "cancelled";
    }

    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? PlannedPickupAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? DesiredDeliveryAt { get; set; }
    public DateTime? ActualPickupAt { get; set; }
    public DateTime? ActualDeliveryAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? CancellationReason { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public uint CreatedByUserId { get; set; }
    public string? Comment { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime CreatedAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime UpdatedAt { get; set; }

    public User ReceiverUser { get; set; } = null!;
    public CargoItem Cargo { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
