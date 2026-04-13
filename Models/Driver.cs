namespace CargoTransport.Desktop.Models;

public class Driver
{
    public uint Id { get; set; }
    public uint UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseCategory { get; set; } = string.Empty;
    public ushort ExperienceYears { get; set; }
    public string Status { get; set; } = "available";
    public decimal Rating { get; set; } = 5.00m;
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Vehicle? CurrentVehicle { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
