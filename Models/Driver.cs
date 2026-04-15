using Sieve.Attributes;

namespace CargoTransport.Desktop.Models;

public class Driver
{
    [Sieve(CanFilter = true, CanSort = true)]
    public uint Id { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public uint UserId { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string LicenseNumber { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public string LicenseCategory { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public ushort ExperienceYears { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string Status { get; set; } = "available";
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal Rating { get; set; } = 5.00m;
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public string? Notes { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime CreatedAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Vehicle? CurrentVehicle { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
