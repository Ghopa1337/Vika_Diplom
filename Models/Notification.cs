namespace CargoTransport.Desktop.Models;

public class Notification
{
    public uint Id { get; set; }
    public uint UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "system";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public User User { get; set; } = null!;
}
