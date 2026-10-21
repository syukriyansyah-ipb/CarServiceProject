using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoCareApi.Data;
using AutoCareApi.Dtos;
using AutoCareApi.Models;
using AutoCareApi.Services;

namespace AutoCareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MechanicsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MechanicsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ==========================================================
    // GET /api/mechanics?dealerId=1
    // ==========================================================
    [HttpGet]
    public async Task<ActionResult<List<MechanicDto>>> GetAll([FromQuery] int? dealerId)
    {
        var query = _db.Mechanics
            .Include(m => m.Dealer)
            .Where(m => m.IsActive);

        // Auto-filter untuk dealer staff
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            query = query.Where(m => m.DealerId == _currentUser.DealerId.Value);
        }
        else if (dealerId.HasValue)
        {
            query = query.Where(m => m.DealerId == dealerId.Value);
        }

        var mechanics = await query
            .Select(m => new MechanicDto
            {
                Id = m.Id,
                EmployeeCode = m.EmployeeCode,
                FullName = m.FullName,
                DealerId = m.DealerId,
                DealerName = m.Dealer != null ? m.Dealer.Name : null,
                Specialization = m.Specialization,
                Certification = m.Certification,
                Phone = m.Phone,
                IsAvailable = m.IsAvailable,
                IsActive = m.IsActive
            })
            .ToListAsync();

        return Ok(mechanics);
    }

    // ==========================================================
    // GET /api/mechanics/{id}
    // ==========================================================
    [HttpGet("{id}")]
    public async Task<ActionResult<MechanicDto>> GetById(int id)
    {
        var mechanic = await _db.Mechanics
            .Include(m => m.Dealer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mechanic == null)
            return NotFound(new { message = $"Mechanic dengan id {id} tidak ditemukan" });

        // Dealer staff hanya bisa lihat mechanic dealer sendiri
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue
            && mechanic.DealerId != _currentUser.DealerId.Value)
        {
            return Forbid();
        }

        return Ok(MapToDto(mechanic));
    }

    // ==========================================================
    // POST /api/mechanics
    // ==========================================================
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,DealerAdmin")]
    public async Task<ActionResult<MechanicDto>> Create([FromBody] MechanicCreateUpdateDto dto)
    {
        // Dealer staff: paksa dealer sendiri
        int actualDealerId = dto.DealerId;
        if (_currentUser.IsDealerStaff && _currentUser.DealerId.HasValue)
        {
            actualDealerId = _currentUser.DealerId.Value;
        }

        var exists = await _db.Mechanics.AnyAsync(m => m.EmployeeCode == dto.EmployeeCode);
        if (exists)
            return BadRequest(new { message = $"Mechanic dengan code '{dto.EmployeeCode}' sudah ada" });

        var dealerExists = await _db.Dealers.AnyAsync(d => d.Id == actualDealerId);
        if (!dealerExists)
            return BadRequest(new { message = $"Dealer dengan id {actualDealerId} tidak ditemukan" });

        var mechanic = new Mechanic
        {
            EmployeeCode = dto.EmployeeCode,
            FullName = dto.FullName,
            DealerId = actualDealerId,
            Specialization = dto.Specialization,
            Certification = dto.Certification,
            Phone = dto.Phone,
            IsAvailable = dto.IsAvailable
        };

        _db.Mechanics.Add(mechanic);
        await _db.SaveChangesAsync();

        return await GetById(mechanic.Id);
    }

    // ==========================================================
    // PUT /api/mechanics/{id}
    // ==========================================================
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] MechanicCreateUpdateDto dto)
    {
        var mechanic = await _db.Mechanics.FindAsync(id);
        if (mechanic == null)
            return NotFound(new { message = $"Mechanic dengan id {id} tidak ditemukan" });

        // Dealer staff hanya bisa update mechanic dealer sendiri
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue
            && mechanic.DealerId != _currentUser.DealerId.Value)
        {
            return Forbid();
        }

        if (mechanic.EmployeeCode != dto.EmployeeCode)
        {
            var exists = await _db.Mechanics.AnyAsync(m => m.EmployeeCode == dto.EmployeeCode);
            if (exists)
                return BadRequest(new { message = $"Mechanic dengan code '{dto.EmployeeCode}' sudah ada" });
        }

        int actualDealerId = _currentUser.IsDealerStaff && _currentUser.DealerId.HasValue
            ? _currentUser.DealerId.Value
            : dto.DealerId;

        var dealerExists = await _db.Dealers.AnyAsync(d => d.Id == actualDealerId);
        if (!dealerExists)
            return BadRequest(new { message = $"Dealer dengan id {actualDealerId} tidak ditemukan" });

        mechanic.EmployeeCode = dto.EmployeeCode;
        mechanic.FullName = dto.FullName;
        mechanic.DealerId = actualDealerId;
        mechanic.Specialization = dto.Specialization;
        mechanic.Certification = dto.Certification;
        mechanic.Phone = dto.Phone;
        mechanic.IsAvailable = dto.IsAvailable;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================================
    // DELETE /api/mechanics/{id}
    // ==========================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var mechanic = await _db.Mechanics.FindAsync(id);
        if (mechanic == null)
            return NotFound(new { message = $"Mechanic dengan id {id} tidak ditemukan" });

        // Dealer staff hanya bisa hapus mechanic dealer sendiri
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue
            && mechanic.DealerId != _currentUser.DealerId.Value)
        {
            return Forbid();
        }

        mechanic.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static MechanicDto MapToDto(Mechanic m) => new()
    {
        Id = m.Id,
        EmployeeCode = m.EmployeeCode,
        FullName = m.FullName,
        DealerId = m.DealerId,
        DealerName = m.Dealer?.Name,
        Specialization = m.Specialization,
        Certification = m.Certification,
        Phone = m.Phone,
        IsAvailable = m.IsAvailable,
        IsActive = m.IsActive
    };
}