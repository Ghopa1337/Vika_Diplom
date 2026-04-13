namespace CargoTransport.Desktop.Models;

public class User
{
    public uint Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public uint RoleId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public Driver? DriverProfile { get; set; }
    public ICollection<Order> ReceiverOrders { get; set; } = new List<Order>();
    public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
    public ICollection<OrderStatusHistory> OrderStatusChanges { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<PasswordHistory> PasswordHistoryEntries { get; set; } = new List<PasswordHistory>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
