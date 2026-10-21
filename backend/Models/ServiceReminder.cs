namespace AutoCareApi.Models;

public class ServiceReminder
{
    public int Id { get; set; }

    // ===== FOREIGN KEY =====
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }

    // Referensi ke history terakhir (opsional)
    public int? LastServiceHistoryId { get; set; }
    public ServiceHistory? LastServiceHistory { get; set; }

    // ===== TIPE REMINDER =====
    // "KmBased" | "DateBased" | "Both"
    public string ReminderType { get; set; } = "Both";

    // ===== KAPAN HARUS DIINGATKAN =====
    public int? DueKm { get; set; }
    public DateOnly? DueDate { get; set; }

    // ===== STATUS WORKFLOW =====
    // "Pending" | "Sent" | "Completed" | "Expired"
    public string Status { get; set; } = "Pending";

    // ===== NOTIFIKASI =====
    // "Email" | "WhatsApp" | "SMS"
    public string NotificationChannel { get; set; } = "Email";
    public DateTime? SentAt { get; set; }

    public string? Notes { get; set; }

    // ===== AUDIT =====
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}