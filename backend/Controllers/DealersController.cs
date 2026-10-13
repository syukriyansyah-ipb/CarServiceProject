using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Data;
using MitsubishiApi.Dtos;
using MitsubishiApi.Models;
using MitsubishiApi.Services;

namespace MitsubishiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DealersController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET semua dealer — bebas (untuk dropdown register/booking)
    [HttpGet]
    public async Task<ActionResult<List<DealerDto>>> GetAll()
    {
        var dealers = await _db.Dealers
            .Where(d => d.IsActive)
            .Select(d => new DealerDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Address = d.Address,
                City = d.City,
                Province = d.Province,
                Phone = d.Phone,
                Email = d.Email,
                MaxSlotPerDay = d.MaxSlotPerDay,
                IsActive = d.IsActive
            })
            .ToListAsync();

        return Ok(dealers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DealerDto>> GetById(int id)
    {
        var dealer = await _db.Dealers.FindAsync(id);
        if (dealer == null)
            return NotFound(new { message = $"Dealer dengan id {id} tidak ditemukan" });

        return Ok(MapToDto(dealer));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<DealerDto>> Create([FromBody] DealerCreateUpdateDto dto)
    {
        var exists = await _db.Dealers.AnyAsync(d => d.Code == dto.Code);
        if (exists)
            return BadRequest(new { message = $"Dealer dengan code '{dto.Code}' sudah ada" });

        var dealer = new Dealer
        {
            Code = dto.Code,
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Province = dto.Province,
            Phone = dto.Phone,
            Email = dto.Email,
            MaxSlotPerDay = dto.MaxSlotPerDay
        };

        _db.Dealers.Add(dealer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dealer.Id }, MapToDto(dealer));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] DealerCreateUpdateDto dto)
    {
        var dealer = await _db.Dealers.FindAsync(id);
        if (dealer == null)
            return NotFound(new { message = $"Dealer dengan id {id} tidak ditemukan" });

        if (dealer.Code != dto.Code)
        {
            var exists = await _db.Dealers.AnyAsync(d => d.Code == dto.Code);
            if (exists)
                return BadRequest(new { message = $"Dealer dengan code '{dto.Code}' sudah ada" });
        }

        dealer.Code = dto.Code;
        dealer.Name = dto.Name;
        dealer.Address = dto.Address;
        dealer.City = dto.City;
        dealer.Province = dto.Province;
        dealer.Phone = dto.Phone;
        dealer.Email = dto.Email;
        dealer.MaxSlotPerDay = dto.MaxSlotPerDay;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var dealer = await _db.Dealers.FindAsync(id);
        if (dealer == null)
            return NotFound(new { message = $"Dealer dengan id {id} tidak ditemukan" });

        dealer.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static DealerDto MapToDto(Dealer d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Name = d.Name,
        Address = d.Address,
        City = d.City,
        Province = d.Province,
        Phone = d.Phone,
        Email = d.Email,
        MaxSlotPerDay = d.MaxSlotPerDay,
        IsActive = d.IsActive
    };
}