namespace MitsubishiApi.Models;

public class Dealer
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;         // "MTR-JKT-01"
    public string Name { get; set; } = string.Empty;         // "Mitsubishi Jakarta Selatan"
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int MaxSlotPerDay { get; set; } = 20;             // kapasitas servis per hari
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}