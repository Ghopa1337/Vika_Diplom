namespace CargoTransport.Desktop.Models;

public class PasswordHistory
{
    public uint Id { get; set; }
    public uint UserId { get; set; }
    public string OldPasswordHash { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }

    public User User { get; set; } = null!;
}
