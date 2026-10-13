using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Data;
using MitsubishiApi.Dtos;
using MitsubishiApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MitsubishiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleModelsController : ControllerBase
{
    private readonly AppDbContext _db;

    private readonly IDistributedCache _cache;
    private const string CacheKey = "VehicleModels:all";

    public VehicleModelsController(AppDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleModelDto>>> GetAll()
    {
        // coba ambil dari redis cache
        var cached = await _cache.GetStringAsync(CacheKey);
        if(cached != null)
        {
            Console.WriteLine("Mengambil data dari cache Redis");
            var modelsFromCache = JsonSerializer.Deserialize<List<VehicleModelDto>>(cached);
            return Ok(modelsFromCache);
        }

        // jika tidak ada di cache, ambil dari database
        Console.WriteLine("Mengambil data dari database");
        var models = await _db.VehicleModels
            .Select(m => new VehicleModelDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Category = m.Category,
                EngineCapacity = m.EngineCapacity,
                Transmission = m.Transmission,
                FuelType = m.FuelType
            })
            .ToListAsync();
        
        // simpan ke Redis selama 1 jam
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(models), options);

        return Ok(models);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleModelDto>> GetById(int id)
    {
        var model = await _db.VehicleModels.FindAsync(id);
        if (model == null)
            return NotFound(new { message = $"VehicleModel dengan id {id} tidak ditemukan" });

        var dto = new VehicleModelDto
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Category = model.Category,
            EngineCapacity = model.EngineCapacity,
            Transmission = model.Transmission,
            FuelType = model.FuelType
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<VehicleModelDto>> Create([FromBody] VehicleModelCreateUpdateDto dto)
    {
        var model = new VehicleModel
        {
            Code = dto.Code,
            Name = dto.Name,
            Category = dto.Category,
            EngineCapacity = dto.EngineCapacity,
            Transmission = dto.Transmission,
            FuelType = dto.FuelType
        };

        _db.VehicleModels.Add(model);
        await _db.SaveChangesAsync();

        return await GetById(model.Id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleModelCreateUpdateDto dto)
    {
        var model = await _db.VehicleModels.FindAsync(id);
        if (model == null)
            return NotFound(new { message = $"VehicleModel dengan id {id} tidak ditemukan" });

        model.Code = dto.Code;
        model.Name = dto.Name;
        model.Category = dto.Category;
        model.EngineCapacity = dto.EngineCapacity;
        model.Transmission = dto.Transmission;
        model.FuelType = dto.FuelType;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _db.VehicleModels.FindAsync(id);
        if (model == null)
            return NotFound(new { message = $"VehicleModel dengan id {id} tidak ditemukan" });

        _db.VehicleModels.Remove(model);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}