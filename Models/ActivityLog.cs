namespace CargoTransport.Desktop.Models;

public class ActivityLog
{
    public uint Id { get; set; }
    public uint? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public uint? EntityId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
