using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Data;
using MitsubishiApi.Dtos;
using MitsubishiApi.Models;

namespace MitsubishiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List semua user. 
    /// SuperAdmin: semua user.
    /// DealerAdmin: hanya user di dealer-nya.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,DealerAdmin")]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var dealerIdStr = User.FindFirst("dealerId")?.Value;
        int? dealerId = string.IsNullOrEmpty(dealerIdStr) ? null : int.Parse(dealerIdStr);

        var query = _db.Users.Include(u => u.Dealer).AsQueryable();

        // DealerAdmin hanya lihat user dealer-nya
        if (role == "DealerAdmin" && dealerId.HasValue)
            query = query.Where(u => u.DealerId == dealerId.Value);

        var users = await query
            .OrderBy(u => u.Id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                DealerId = u.DealerId,
                DealerName = u.Dealer != null ? u.Dealer.Name : null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Buat user baru (staff).
    /// SuperAdmin: bisa buat role apa saja, dealer apa saja.
    /// DealerAdmin: hanya bisa buat ServiceAdvisor/Mechanic untuk dealer-nya sendiri.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,DealerAdmin")]
    public async Task<ActionResult<UserDto>> Create([FromBody] UserCreateDto dto)
    {
        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var dealerIdStr = User.FindFirst("dealerId")?.Value;
        int? currentDealerId = string.IsNullOrEmpty(dealerIdStr) ? null : int.Parse(dealerIdStr);

        // ==== VALIDASI ROLE ====
        var allowedRoles = new[] { "SuperAdmin", "DealerAdmin", "ServiceAdvisor", "Mechanic", "Customer" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest(new { message = $"Role '{dto.Role}' tidak valid" });

        // DealerAdmin tidak boleh buat SuperAdmin/DealerAdmin
        if (currentRole == "DealerAdmin" && (dto.Role == "SuperAdmin" || dto.Role == "DealerAdmin"))
            return Forbid();

        // ==== VALIDASI DEALER ====
        // SuperAdmin & Customer → DealerId harus null
        // Role lain → DealerId wajib
        if (dto.Role == "SuperAdmin" || dto.Role == "Customer")
        {
            if (dto.DealerId.HasValue)
                return BadRequest(new { message = $"Role '{dto.Role}' tidak boleh punya DealerId" });
        }
        else
        {
            if (!dto.DealerId.HasValue)
                return BadRequest(new { message = $"Role '{dto.Role}' harus terikat ke dealer" });

            // DealerAdmin hanya boleh buat user untuk dealer-nya sendiri
            if (currentRole == "DealerAdmin" && dto.DealerId != currentDealerId)
                return Forbid();

            var dealerExists = await _db.Dealers.AnyAsync(d => d.Id == dto.DealerId.Value);
            if (!dealerExists)
                return BadRequest(new { message = $"Dealer dengan id {dto.DealerId} tidak ditemukan" });
        }

        // ==== CEK EMAIL DUPLIKAT ====
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
            return BadRequest(new { message = "Email sudah terdaftar" });

        // ==== SIMPAN ====
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Role = dto.Role,
            DealerId = dto.DealerId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Load dealer name
        string? dealerName = null;
        if (user.DealerId.HasValue)
        {
            dealerName = await _db.Dealers
                .Where(d => d.Id == user.DealerId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            DealerId = user.DealerId,
            DealerName = dealerName,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Hapus user (soft delete via IsActive? Kita belum punya, jadi hard delete dulu).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"User dengan id {id} tidak ditemukan" });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}