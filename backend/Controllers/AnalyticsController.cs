using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Data;
using MitsubishiApi.Dtos;
using MitsubishiApi.Services;

namespace MitsubishiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ==========================================================
    // GET /api/analytics/summary — KPI utama
    // ==========================================================
    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary()
    {
        var bookings = _db.ServiceBookings.AsQueryable();
        var invoices = _db.Invoices.AsQueryable();

        // Auto-filter by dealer untuk staff
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            var dealerId = _currentUser.DealerId.Value;
            bookings = bookings.Where(b => b.DealerId == dealerId);
            invoices = invoices.Where(i => _db.ServiceHistories
                .Any(h => h.Id == i.ServiceHistoryId && h.DealerId == dealerId));
        }

        var summary = new AnalyticsSummaryDto
        {
            TotalBookings = await bookings.CountAsync(),
            CompletedBookings = await bookings.CountAsync(b => b.Status == "Completed"),
            PendingBookings = await bookings.CountAsync(b => b.Status == "Pending"),
            CancelledBookings = await bookings.CountAsync(b => b.Status == "Cancelled"),
            TotalRevenue = await invoices.Where(i => i.PaymentStatus == "Paid").SumAsync(i => (decimal?)i.TotalAmount) ?? 0,
            UnpaidAmount = await invoices.Where(i => i.PaymentStatus != "Paid").SumAsync(i => (decimal?)i.TotalAmount) ?? 0,
            TotalCustomers = await _db.Customers.CountAsync(),
            TotalVehicles = await _db.Vehicles.CountAsync()
        };

        return Ok(summary);
    }

    // ==========================================================
    // GET /api/analytics/booking-trend?days=30
    // ==========================================================
    [HttpGet("booking-trend")]
    public async Task<ActionResult<List<BookingTrendDto>>> GetBookingTrend([FromQuery] int days = 30)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        var query = _db.ServiceBookings.Where(b => b.BookingDate >= startDate);

        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
            query = query.Where(b => b.DealerId == _currentUser.DealerId.Value);

        var data = await query
            .GroupBy(b => b.BookingDate)
            .Select(g => new BookingTrendDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Revenue = g.Where(b => b.ActualCost != null).Sum(b => b.ActualCost ?? 0)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return Ok(data);
    }

    // ==========================================================
    // GET /api/analytics/dealer-revenue — SuperAdmin only
    // ==========================================================
    [HttpGet("dealer-revenue")]
    public async Task<ActionResult<List<DealerRevenueDto>>> GetDealerRevenue()
    {
        var query = _db.Dealers
            .Where(d => d.IsActive)
            .Select(d => new DealerRevenueDto
            {
                DealerId = d.Id,
                DealerName = d.Name,
                BookingCount = _db.ServiceBookings.Count(b => b.DealerId == d.Id),
                Revenue = _db.ServiceBookings
                    .Where(b => b.DealerId == d.Id && b.Status == "Completed" && b.ActualCost != null)
                    .Sum(b => b.ActualCost ?? 0)
            })
            .OrderByDescending(d => d.Revenue);

        // Dealer staff hanya lihat dealer sendiri
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
            query = (IOrderedQueryable<DealerRevenueDto>)query.Where(d => d.DealerId == _currentUser.DealerId.Value);

        var result = await query.ToListAsync();
        return Ok(result);
    }

    // ==========================================================
    // GET /api/analytics/status-distribution
    // ==========================================================
    [HttpGet("status-distribution")]
    public async Task<ActionResult<List<StatusDistributionDto>>> GetStatusDistribution()
    {
        var query = _db.ServiceBookings.AsQueryable();

        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
            query = query.Where(b => b.DealerId == _currentUser.DealerId.Value);

        var data = await query
            .GroupBy(b => b.Status)
            .Select(g => new StatusDistributionDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(data);
    }

    // ==========================================================
    // GET /api/analytics/top-vehicles?limit=5
    // ==========================================================
    [HttpGet("top-vehicles")]
    public async Task<ActionResult<List<TopVehicleDto>>> GetTopVehicles([FromQuery] int limit = 5)
    {
        var query = _db.ServiceHistories
            .Include(h => h.Vehicle!).ThenInclude(v => v.VehicleModel)
            .AsQueryable();

        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
            query = query.Where(h => h.DealerId == _currentUser.DealerId.Value);

        var data = await query
            .Where(h => h.Vehicle != null && h.Vehicle.VehicleModel != null)
            .GroupBy(h => h.Vehicle!.VehicleModel!.Name)
            .Select(g => new TopVehicleDto
            {
                VehicleModelName = g.Key,
                ServiceCount = g.Count()
            })
            .OrderByDescending(x => x.ServiceCount)
            .Take(limit)
            .ToListAsync();

        return Ok(data);
    }

    // ==========================================================
    // GET /api/analytics/top-services
    // ==========================================================
    [HttpGet("top-services")]
    public async Task<ActionResult<List<TopServiceTypeDto>>> GetTopServices([FromQuery] int limit = 5)
    {
        var query = _db.ServiceBookings
            .Include(b => b.ServiceType)
            .AsQueryable();

        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
            query = query.Where(b => b.DealerId == _currentUser.DealerId.Value);

        var data = await query
            .Where(b => b.ServiceType != null)
            .GroupBy(b => b.ServiceType!.Name)
            .Select(g => new TopServiceTypeDto
            {
                ServiceTypeName = g.Key,
                BookingCount = g.Count(),
                Revenue = g.Where(b => b.ActualCost != null).Sum(b => b.ActualCost ?? 0)
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(limit)
            .ToListAsync();

        return Ok(data);
    }
}