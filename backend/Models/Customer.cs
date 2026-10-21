namespace AutoCareApi.Models;

public class Customer
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Link ke User (kalau customer punya akun login)
    public int? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}