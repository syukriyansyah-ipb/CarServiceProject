using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoCareApi.Data;
using AutoCareApi.Dtos;
using AutoCareApi.Services;

namespace AutoCareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ReminderService _reminderService;

    public RemindersController(
        AppDbContext db,
        ICurrentUserService currentUser,
        ReminderService reminderService)
    {
        _db = db;
        _currentUser = currentUser;
        _reminderService = reminderService;
    }

    // GET /api/reminders?status=Pending&dealerId=1
    [HttpGet]
    public async Task<ActionResult<List<ServiceReminderDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? dealerId)
    {
        var query = _db.ServiceReminders
            .Include(r => r.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(r => r.Customer)
            .Include(r => r.Dealer)
            .AsQueryable();

        // Auto-filter by dealer
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            query = query.Where(r => r.DealerId == _currentUser.DealerId.Value);
        }
        else if (dealerId.HasValue)
        {
            query = query.Where(r => r.DealerId == dealerId.Value);
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var reminders = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ServiceReminderDto
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                LicensePlate = r.Vehicle != null ? r.Vehicle.LicensePlate : "",
                VehicleModelName = r.Vehicle != null && r.Vehicle.VehicleModel != null ? r.Vehicle.VehicleModel.Name : "",
                CustomerId = r.CustomerId,
                CustomerName = r.Customer != null ? r.Customer.FullName : "",
                CustomerPhone = r.Customer != null ? r.Customer.Phone : "",
                CustomerEmail = r.Customer != null ? r.Customer.Email : null,
                DealerId = r.DealerId,
                DealerName = r.Dealer != null ? r.Dealer.Name : "",
                ReminderType = r.ReminderType,
                DueKm = r.DueKm,
                DueDate = r.DueDate,
                Status = r.Status,
                NotificationChannel = r.NotificationChannel,
                SentAt = r.SentAt,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reminders);
    }

    // GET /api/reminders/stats — untuk dashboard
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var query = _db.ServiceReminders.AsQueryable();

        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            query = query.Where(r => r.DealerId == _currentUser.DealerId.Value);
        }

        var pending = await query.CountAsync(r => r.Status == "Pending");
        var sent = await query.CountAsync(r => r.Status == "Sent");
        var completed = await query.CountAsync(r => r.Status == "Completed");

        return Ok(new { pending, sent, completed });
    }

    // POST /api/reminders/run-now — trigger manual (untuk testing)
    [HttpPost("run-now")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> RunNow()
    {
        await _reminderService.GenerateRemindersAsync();
        await _reminderService.SendPendingRemindersAsync();
        return Ok(new { message = "Job reminder dijalankan manual" });
    }

    // PUT /api/reminders/{id}/complete — tandai servis sudah dilakukan
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> Complete(int id)
    {
        var reminder = await _db.ServiceReminders.FindAsync(id);
        if (reminder == null)
            return NotFound(new { message = $"Reminder dengan id {id} tidak ditemukan" });

        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue
            && reminder.DealerId != _currentUser.DealerId.Value)
        {
            return Forbid();
        }

        reminder.Status = "Completed";
        await _db.SaveChangesAsync();
        return NoContent();
    }
}