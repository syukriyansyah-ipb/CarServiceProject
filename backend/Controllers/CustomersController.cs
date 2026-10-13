using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Data;
using MitsubishiApi.Dtos;
using MitsubishiApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace MitsubishiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll()
    {
        var customers = await _db.Customers
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
                City = c.City,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.VehicleModel)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
            return NotFound(new { message = $"Customer dengan id {id} tidak ditemukan" });

        var dto = new CustomerDetailDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            City = customer.City,
            CreatedAt = customer.CreatedAt,
            Vehicles = customer.Vehicles.Select(v => new VehicleSimpleDto
            {
                Id = v.Id,
                LicensePlate = v.LicensePlate,
                Variant = v.Variant,
                Year = v.Year,
                Color = v.Color,
                Price = v.Price,
                VehicleModelName = v.VehicleModel != null ? v.VehicleModel.Name : ""
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> Create([FromBody] CustomerCreateUpdateDto dto)
    {
        var customer = new Customer
        {
            CustomerCode = dto.CustomerCode,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return await GetById(customer.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerCreateUpdateDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Customer dengan id {id} tidak ditemukan" });

        customer.CustomerCode = dto.CustomerCode;
        customer.FullName = dto.FullName;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.City = dto.City;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Customer dengan id {id} tidak ditemukan" });

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}