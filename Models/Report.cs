namespace CargoTransport.Desktop.Models;

public class Report
{
    public uint Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string ReportData { get; set; } = "{}";
    public uint? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? CreatedByUser { get; set; }
}
