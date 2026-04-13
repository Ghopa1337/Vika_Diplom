namespace CargoTransport.Desktop.Models;

public class OrderStatusHistory
{
    public uint Id { get; set; }
    public uint OrderId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public uint ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }

    public Order Order { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
