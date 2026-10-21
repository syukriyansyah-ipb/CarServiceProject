using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoCareApi.Data;
using AutoCareApi.Dtos;
using AutoCareApi.Models;

namespace AutoCareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServiceTypesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceTypeDto>>> GetAll()
    {
        var types = await _db.ServiceTypes
            .Where(s => s.IsActive)
            .Select(s => new ServiceTypeDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                EstimatedDuration = s.EstimatedDuration,
                EstimatedCost = s.EstimatedCost,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceTypeDto>> GetById(int id)
    {
        var type = await _db.ServiceTypes.FindAsync(id);
        if (type == null)
            return NotFound(new { message = $"ServiceType dengan id {id} tidak ditemukan" });

        return Ok(MapToDto(type));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceTypeDto>> Create([FromBody] ServiceTypeCreateUpdateDto dto)
    {
        var exists = await _db.ServiceTypes.AnyAsync(s => s.Code == dto.Code);
        if (exists)
            return BadRequest(new { message = $"ServiceType dengan code '{dto.Code}' sudah ada" });

        var type = new ServiceType
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            EstimatedDuration = dto.EstimatedDuration,
            EstimatedCost = dto.EstimatedCost
        };

        _db.ServiceTypes.Add(type);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = type.Id }, MapToDto(type));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceTypeCreateUpdateDto dto)
    {
        var type = await _db.ServiceTypes.FindAsync(id);
        if (type == null)
            return NotFound(new { message = $"ServiceType dengan id {id} tidak ditemukan" });

        if (type.Code != dto.Code)
        {
            var exists = await _db.ServiceTypes.AnyAsync(s => s.Code == dto.Code);
            if (exists)
                return BadRequest(new { message = $"ServiceType dengan code '{dto.Code}' sudah ada" });
        }

        type.Code = dto.Code;
        type.Name = dto.Name;
        type.Description = dto.Description;
        type.Category = dto.Category;
        type.EstimatedDuration = dto.EstimatedDuration;
        type.EstimatedCost = dto.EstimatedCost;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var type = await _db.ServiceTypes.FindAsync(id);
        if (type == null)
            return NotFound(new { message = $"ServiceType dengan id {id} tidak ditemukan" });

        type.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ServiceTypeDto MapToDto(ServiceType s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        Description = s.Description,
        Category = s.Category,
        EstimatedDuration = s.EstimatedDuration,
        EstimatedCost = s.EstimatedCost,
        IsActive = s.IsActive
    };
}