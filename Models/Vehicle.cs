namespace CargoTransport.Desktop.Models;

public class Vehicle
{
    public uint Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public decimal CapacityKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public string? BodyType { get; set; }
    public ushort? ProductionYear { get; set; }
    public string Status { get; set; } = "available";
    public DateOnly? InsuranceExpiry { get; set; }
    public uint? CurrentDriverId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Driver? CurrentDriver { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
