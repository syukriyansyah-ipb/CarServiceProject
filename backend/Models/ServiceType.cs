namespace AutoCareApi.Models;

public class ServiceType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;         // "PERIODIC-10K"
    public string Name { get; set; } = string.Empty;         // "Servis Berkala 10.000 KM"
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;     // "Periodic", "Repair", "Emergency"
    public int EstimatedDuration { get; set; }               // menit
    public decimal EstimatedCost { get; set; }               // estimasi harga
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}