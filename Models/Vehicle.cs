using Sieve.Attributes;

namespace CargoTransport.Desktop.Models;

public class Vehicle
{
    [Sieve(CanFilter = true, CanSort = true)]
    public uint Id { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string Model { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public string LicensePlate { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal CapacityKg { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal? VolumeM3 { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string? BodyType { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public ushort? ProductionYear { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string Status { get; set; } = "available";
    [Sieve(CanFilter = true, CanSort = true)]
    public DateOnly? InsuranceExpiry { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public uint? CurrentDriverId { get; set; }
    public string? Notes { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime CreatedAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime UpdatedAt { get; set; }

    public Driver? CurrentDriver { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
