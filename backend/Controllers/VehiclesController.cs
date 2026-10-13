using Microsoft.AspNetCore.Mvc;
using MitsubishiApi.Models;
using MitsubishiApi.Data;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace MitsubishiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    
    public class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public VehiclesController(AppDbContext db)
        {
            _db = db;
        }
        

        [HttpGet]
        public async Task<ActionResult<List<Vehicle>>> GetAll()
        {
            var vehicles = await _db.Vehicles
            .Include(v => v.VehicleModel)
            .Select(v => new VehicleSimpleDto
            {
                Id = v.Id,
                LicensePlate = v.LicensePlate,
                Variant = v.Variant,
                Year = v.Year,
                Color = v.Color,
                Price = v.Price,
                VehicleModelName = v.VehicleModel!.Name
            })
            .ToListAsync();
            return Ok(vehicles);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDetailDto>> GetById(int id)
        {
            var vehicle = await _db.Vehicles
            .Include(v => v.VehicleModel)
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound(new { message = $"Vehicle dengan id {id} tidak ditemukan" });
            }

            var dto = new VehicleDetailDto
            {
                Id = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Variant = vehicle.Variant,
                Year = vehicle.Year,
                Color = vehicle.Color,
                Price = vehicle.Price,
                VehicleModel = vehicle.VehicleModel != null! ? new VehicleModelSimpleDto
                {
                    Id = vehicle.VehicleModel!.Id,
                    Name = vehicle.VehicleModel.Name,
                    Category = vehicle.VehicleModel.Category
                } : null!,
                Customer = vehicle.Customer == null ? null! : new CustomerSimpleDto
                {
                    Id = vehicle.Customer!.Id,
                    FullName = vehicle.Customer.FullName,
                    Phone = vehicle.Customer.Phone
                }
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<VehicleDetailDto>> Create([FromBody] VehicleCreateUpdateDto dto)
        {
            //cek apakah vehicleModelId yang dikirimkan ada di database
            var modelExists = await _db.VehicleModels.AnyAsync(m => m.Id == dto.VehicleModelId);
            if (!modelExists)
            {
                return BadRequest(new { message = $"Vehicle model dengan id {dto.VehicleModelId} tidak ditemukan" });
            }

            // validasi apakah customerId yang dikirimkan ada di database
            var customerExists = await _db.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists)
            {
                return BadRequest(new { message = $"Customer dengan id {dto.CustomerId} tidak ditemukan" });
            }

            var vehicle = new Vehicle
            {
                VehicleModelId = dto.VehicleModelId,
                CustomerId = dto.CustomerId,
                Variant = dto.Variant,
                Year = dto.Year,
                LicensePlate = dto.LicensePlate,
                Color = dto.Color,
                Price = dto.Price
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            return await GetById(vehicle.Id); // return 201 Created response with the location of the newly created vehicle
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VehicleCreateUpdateDto dto)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound(new { message = $"Vehicle dengan id {id} tidak ditemukan" });
            }

            //cek apakah vehicleModelId valid
            var modelExists = await _db.VehicleModels.AnyAsync(m => m.Id == dto.VehicleModelId);
            if (!modelExists)
            {
                return BadRequest(new { message = $"Vehicle model dengan id {dto.VehicleModelId} tidak ditemukan" });
            }

            // update the vehicle properties
            vehicle.VehicleModelId = dto.VehicleModelId;
            vehicle.CustomerId = dto.CustomerId;
            vehicle.Variant = dto.Variant;
            vehicle.Year = dto.Year;
            vehicle.LicensePlate = dto.LicensePlate;
            vehicle.Color = dto.Color;
            vehicle.Price = dto.Price;

            await _db.SaveChangesAsync();
            return NoContent(); // return 204 No Content response
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound(new { message = $"Vehicle dengan id {id} tidak ditemukan" });
            }

            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync();
            return NoContent(); // return 204 No Content response
        }

    }
}
