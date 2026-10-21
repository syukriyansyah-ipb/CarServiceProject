using System.Security.Claims;

namespace AutoCareApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public int UserId
    {
        get
        {
            var v = _http.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : 0;
        }
    }

    public string Role =>
        _http.HttpContext?.User?
            .FindFirst(ClaimTypes.Role)?.Value ?? "";

    public int? DealerId
    {
        get
        {
            var v = _http.HttpContext?.User?
                .FindFirst("dealerId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin => Role == "SuperAdmin";
    public bool IsDealerStaff => DealerId.HasValue;
    public bool IsCustomer => Role == "Customer";
}