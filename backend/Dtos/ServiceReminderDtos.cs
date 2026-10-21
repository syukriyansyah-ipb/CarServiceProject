namespace AutoCareApi.Dtos;

public class ServiceReminderDto
{
    public int Id { get; set; }
    
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleModelName { get; set; } = string.Empty;
    
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    
    public int DealerId { get; set; }
    public string DealerName { get; set; } = string.Empty;
    
    public string ReminderType { get; set; } = string.Empty;
    public int? DueKm { get; set; }
    public DateOnly? DueDate { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public string NotificationChannel { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}