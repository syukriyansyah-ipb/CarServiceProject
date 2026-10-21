namespace AutoCareApi.Models;

public class ServiceHistory
{
    public int Id { get; set; }
    
    // Link ke booking (unique — 1 booking = 1 history)
    public int BookingId { get; set; }
    public ServiceBooking? Booking { get; set; }
    
    // Info utama
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    
    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }
    
    public int? MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }
    
    // Detail servis
    public DateOnly ServiceDate { get; set; }
    public int KmAtService { get; set; }
    public decimal TotalCost { get; set; }
    
    public string? Findings { get; set; }           // temuan teknisi
    public string? Recommendations { get; set; }    // rekomendasi berikutnya
    
    // Reminder servis berikutnya
    public int? NextServiceKm { get; set; }
    public DateOnly? NextServiceDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}