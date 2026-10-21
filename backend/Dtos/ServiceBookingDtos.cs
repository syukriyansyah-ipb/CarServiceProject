namespace AutoCareApi.Dtos;

// ==== RESPONSE ====
public class ServiceBookingDto
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    
    // Info customer & vehicle (flat, biar response ringkas)
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleModelName { get; set; } = string.Empty;
    
    // Info dealer & service
    public int DealerId { get; set; }
    public string DealerName { get; set; } = string.Empty;
    
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    
    // Mechanic (nullable)
    public int? MechanicId { get; set; }
    public string? MechanicName { get; set; }
    
    // Jadwal
    public DateOnly BookingDate { get; set; }
    public TimeOnly BookingTime { get; set; }
    
    // Status
    public string Status { get; set; } = string.Empty;
    
    // Detail
    public string? ComplaintNotes { get; set; }
    public string? CustomerNotes { get; set; }
    public string? AdvisorNotes { get; set; }
    public int? KmAtService { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// ==== REQUEST: CREATE ====
public class ServiceBookingCreateDto
{
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public int DealerId { get; set; }
    public int ServiceTypeId { get; set; }
    
    public DateOnly BookingDate { get; set; }
    public TimeOnly BookingTime { get; set; }
    
    public string? ComplaintNotes { get; set; }
    public string? CustomerNotes { get; set; }
    
    public int? KmAtService { get; set; }
}

// ==== REQUEST: ASSIGN MECHANIC ====
public class AssignMechanicDto
{
    public int MechanicId { get; set; }
}

// ==== REQUEST: COMPLETE ====
public class CompleteBookingDto
{
    public int KmAtService { get; set; }
    public decimal ActualCost { get; set; }
    public string? AdvisorNotes { get; set; }
}

// ==== RESPONSE: AVAILABLE SLOTS ====
public class AvailableSlotDto
{
    public DateOnly Date { get; set; }
    public List<TimeOnly> AvailableTimes { get; set; } = new();
    public int TotalSlots { get; set; }
    public int BookedSlots { get; set; }
}