namespace CargoTransport.Desktop.Models;

public class CargoItem
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CargoType { get; set; } = "normal";
    public decimal WeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public string? Description { get; set; }
    public string? SpecialRequirements { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
