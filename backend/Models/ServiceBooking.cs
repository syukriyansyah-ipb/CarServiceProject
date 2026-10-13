namespace MitsubishiApi.Models;

public class ServiceBooking
{
    public int Id { get; set; }
    
    // Kode booking unik (auto-generate di controller)
    public string BookingCode { get; set; } = string.Empty;   // "BK-20251011-001"
    
    // ==== 5 FOREIGN KEY ====
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    
    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }
    
    public int ServiceTypeId { get; set; }
    public ServiceType? ServiceType { get; set; }
    
    // Mechanic nullable — di-assign nanti oleh service advisor
    public int? MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }
    
    // ==== JADWAL ====
    public DateOnly BookingDate { get; set; }       // tanggal servis
    public TimeOnly BookingTime { get; set; }       // jam mulai
    
    // ==== STATUS WORKFLOW ====
    // "Pending" → "Confirmed" → "InProgress" → "Completed" | "Cancelled"
    public string Status { get; set; } = "Pending";
    
    // ==== DETAIL ====
    public string? ComplaintNotes { get; set; }      // keluhan customer
    public string? CustomerNotes { get; set; }       // catatan tambahan
    public string? AdvisorNotes { get; set; }        // catatan service advisor
    
    public int? KmAtService { get; set; }            // KM saat servis
    
    public decimal EstimatedCost { get; set; }       // estimasi awal
    public decimal? ActualCost { get; set; }         // biaya aktual (setelah selesai)
    
    // ==== AUDIT ====
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}