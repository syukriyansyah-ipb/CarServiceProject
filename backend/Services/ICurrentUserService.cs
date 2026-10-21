namespace AutoCareApi.Services;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    int? DealerId { get; }
    bool IsSuperAdmin { get; }
    bool IsDealerStaff { get; }
    bool IsCustomer { get; }
}