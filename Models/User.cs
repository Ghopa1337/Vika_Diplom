using Sieve.Attributes;

namespace CargoTransport.Desktop.Models;

public class User
{
    [Sieve(CanFilter = true, CanSort = true)]
    public uint Id { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public string FullName { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public uint RoleId { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string? Email { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string? Phone { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string? CompanyName { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsBlocked { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public bool IsActive { get; set; } = true;
    [Sieve(CanFilter = true, CanSort = true)]
    public bool MustChangePassword { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime? LastLoginAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime CreatedAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime UpdatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public Driver? DriverProfile { get; set; }
    public ICollection<Order> ReceiverOrders { get; set; } = new List<Order>();
    public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
    public ICollection<OrderRequest> OrderRequests { get; set; } = new List<OrderRequest>();
    public ICollection<OrderRequest> ProcessedOrderRequests { get; set; } = new List<OrderRequest>();
    public ICollection<OrderStatusHistory> OrderStatusChanges { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<PasswordHistory> PasswordHistoryEntries { get; set; } = new List<PasswordHistory>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
