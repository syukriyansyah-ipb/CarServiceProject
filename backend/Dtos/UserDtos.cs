namespace AutoCareApi.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? DealerId { get; set; }
    public string? DealerName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserCreateDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "ServiceAdvisor";   // "SuperAdmin"|"DealerAdmin"|"ServiceAdvisor"|"Mechanic"|"Customer"
    public int? DealerId { get; set; }
}