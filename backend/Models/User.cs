namespace MitsubishiApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; 
    public int? DealerId { get; set; }       // ← BARU
    public Dealer? Dealer { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}