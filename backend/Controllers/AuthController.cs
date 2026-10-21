using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoCareApi.Data;
using AutoCareApi.Dtos;
using AutoCareApi.Models;
using AutoCareApi.Services;

namespace AutoCareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    // POST /api/auth/register — Publik, selalu Customer
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null)
            return BadRequest(new { message = "Email sudah terdaftar" });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = "Customer",
            DealerId = null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return Ok(BuildAuthResponse(user, token));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Dealer)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized(new { message = "Email atau password salah" });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email atau password salah" });

        var dealerName = user.Dealer?.Name;
        var token = _jwtService.GenerateToken(user, dealerName);

        return Ok(BuildAuthResponse(user, token, dealerName));
    }

    // GET /api/auth/me — info user yang sedang login
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.Include(u => u.Dealer).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        return Ok(new AuthResponse
        {
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            DealerId = user.DealerId,
            DealerName = user.Dealer?.Name
        });
    }

    private static AuthResponse BuildAuthResponse(User user, string token, string? dealerName = null)
    {
        return new AuthResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            DealerId = user.DealerId,
            DealerName = dealerName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
    }
}