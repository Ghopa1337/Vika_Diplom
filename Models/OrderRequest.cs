namespace CargoTransport.Desktop.Models;

public class OrderRequest
{
    public uint Id { get; set; }
    public uint ReceiverUserId { get; set; }
    public string CargoDescription { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PickupContactPhone { get; set; } = string.Empty;
    public string DeliveryContactPhone { get; set; } = string.Empty;
    public DateTime? DesiredDate { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = OrderRequestStatuses.Pending;
    public uint? ProcessedByUserId { get; set; }
    public uint? CreatedOrderId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static class OrderRequestStatuses
    {
        public const string Pending = "pending";
        public const string Processed = "processed";
    }

    public User ReceiverUser { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
    public Order? CreatedOrder { get; set; }
}
