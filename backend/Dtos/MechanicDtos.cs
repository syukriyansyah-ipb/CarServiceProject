namespace AutoCareApi.Dtos;

public class MechanicDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int DealerId { get; set; }
    public string? DealerName { get; set; }
    public string? Specialization { get; set; }
    public string? Certification { get; set; }
    public string? Phone { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
}

public class MechanicCreateUpdateDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int DealerId { get; set; }
    public string? Specialization { get; set; }
    public string? Certification { get; set; }
    public string? Phone { get; set; }
    public bool IsAvailable { get; set; } = true;
}