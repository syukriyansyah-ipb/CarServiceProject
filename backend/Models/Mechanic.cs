namespace MitsubishiApi.Models;

public class Mechanic
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;    // "MEC-JKT-001"
    public string FullName { get; set; } = string.Empty;
    public int DealerId { get; set; }                            // FK ke Dealer
    public Dealer? Dealer { get; set; }                          // Navigation property
    public string? Specialization { get; set; }                  // "Engine", "Electrical", "Body"
    public string? Certification { get; set; }                   // sertifikat Mitsubishi
    public string? Phone { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}