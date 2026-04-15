using Sieve.Attributes;

namespace CargoTransport.Desktop.Models;

public class CargoItem
{
    [Sieve(CanFilter = true, CanSort = true)]
    public uint Id { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public string Name { get; set; } = string.Empty;
    [Sieve(CanFilter = true, CanSort = true)]
    public string CargoType { get; set; } = "normal";
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal WeightKg { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public decimal? VolumeM3 { get; set; }
    public string? Description { get; set; }
    public string? SpecialRequirements { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime CreatedAt { get; set; }
    [Sieve(CanFilter = true, CanSort = true)]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
