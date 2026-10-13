namespace MitsubishiApi.Dtos;

public class ServiceHistoryDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;

    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleModelName { get; set; } = string.Empty;

    public int DealerId { get; set; }
    public string DealerName { get; set; } = string.Empty;

    public int? MechanicId { get; set; }
    public string? MechanicName { get; set; }

    public DateOnly ServiceDate { get; set; }
    public int KmAtService { get; set; }
    public decimal TotalCost { get; set; }

    public string? Findings { get; set; }
    public string? Recommendations { get; set; }

    public int? NextServiceKm { get; set; }
    public DateOnly? NextServiceDate { get; set; }

    public DateTime CreatedAt { get; set; }
}