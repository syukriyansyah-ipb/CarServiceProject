namespace AutoCareApi.Dtos;

public class VehicleSearchDocument
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string VehicleModelName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}

// req search document untuk elasticsearch
public class VehicleSearchRequest
{
    public string Query { get; set; } = string.Empty;
}