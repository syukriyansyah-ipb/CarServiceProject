namespace AutoCareApi.Dtos;

public class ServiceTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int EstimatedDuration { get; set; }
    public decimal EstimatedCost { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceTypeCreateUpdateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int EstimatedDuration { get; set; }
    public decimal EstimatedCost { get; set; }
}