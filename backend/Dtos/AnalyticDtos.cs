namespace AutoCareApi.Dtos;

public class AnalyticsSummaryDto
{
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalVehicles { get; set; }
}

public class BookingTrendDto
{
    public string Date { get; set; } = string.Empty;   // "2026-10-12"
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class DealerRevenueDto
{
    public int DealerId { get; set; }
    public string DealerName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}

public class StatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopVehicleDto
{
    public string VehicleModelName { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
}

public class TopServiceTypeDto
{
    public string ServiceTypeName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}