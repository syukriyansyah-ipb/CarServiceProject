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
[Authorize]
public class ServiceHistoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ServiceHistoriesController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ==========================================================
    // GET /api/servicehistories
    // ==========================================================
    [HttpGet]
    public async Task<ActionResult<List<ServiceHistoryDto>>> GetAll(
        [FromQuery] int? vehicleId,
        [FromQuery] int? dealerId,
        [FromQuery] int? customerId)
    {
        var query = _db.ServiceHistories
            .Include(h => h.Booking)
            .Include(h => h.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(h => h.Dealer)
            .Include(h => h.Mechanic)
            .AsQueryable();

        // ===== AUTO-FILTER BY DEALER =====
        if (_currentUser.IsSuperAdmin)
        {
            if (dealerId.HasValue)
                query = query.Where(h => h.DealerId == dealerId.Value);
        }
        else if (_currentUser.IsCustomer)
        {
            // Customer hanya lihat history kendaraan miliknya
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.UserId == _currentUser.UserId);
            if (customer != null)
                query = query.Where(h => h.Vehicle!.CustomerId == customer.Id);
            else
                query = query.Where(h => false);
        }
        else if (_currentUser.DealerId.HasValue)
        {
            // DealerStaff: history dealer sendiri
            query = query.Where(h => h.DealerId == _currentUser.DealerId.Value);
        }

        if (vehicleId.HasValue) query = query.Where(h => h.VehicleId == vehicleId.Value);
        if (customerId.HasValue) query = query.Where(h => h.Vehicle!.CustomerId == customerId.Value);

        var histories = await query
            .OrderByDescending(h => h.ServiceDate)
            .Select(h => MapToDto(h))
            .ToListAsync();

        return Ok(histories);
    }

    // ==========================================================
    // GET /api/servicehistories/{id}
    // ==========================================================
    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceHistoryDto>> GetById(int id)
    {
        var history = await _db.ServiceHistories
            .Include(h => h.Booking)
            .Include(h => h.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(h => h.Dealer)
            .Include(h => h.Mechanic)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
            return NotFound(new { message = $"ServiceHistory dengan id {id} tidak ditemukan" });

        // Validasi akses
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue
            && history.DealerId != _currentUser.DealerId.Value)
        {
            return Forbid();
        }

        return Ok(MapToDto(history));
    }

    private static ServiceHistoryDto MapToDto(ServiceHistory h) => new()
    {
        Id = h.Id,
        BookingId = h.BookingId,
        BookingCode = h.Booking?.BookingCode ?? "",
        VehicleId = h.VehicleId,
        LicensePlate = h.Vehicle?.LicensePlate ?? "",
        VehicleModelName = h.Vehicle?.VehicleModel?.Name ?? "",
        DealerId = h.DealerId,
        DealerName = h.Dealer?.Name ?? "",
        MechanicId = h.MechanicId,
        MechanicName = h.Mechanic?.FullName,
        ServiceDate = h.ServiceDate,
        KmAtService = h.KmAtService,
        TotalCost = h.TotalCost,
        Findings = h.Findings,
        Recommendations = h.Recommendations,
        NextServiceKm = h.NextServiceKm,
        NextServiceDate = h.NextServiceDate,
        CreatedAt = h.CreatedAt
    };
}