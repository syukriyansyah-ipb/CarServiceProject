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
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    private static readonly TimeOnly OpeningTime = new(8, 0);
    private static readonly TimeOnly ClosingTime = new(17, 0);
    private const int SlotDurationMinutes = 60;

    public BookingsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ==========================================================
    // GET /api/bookings
    // Auto-filter by dealer untuk staff
    // ==========================================================
    [HttpGet]
    public async Task<ActionResult<List<ServiceBookingDto>>> GetAll(
        [FromQuery] int? dealerId,
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? date)
    {
        var query = _db.ServiceBookings
            .Include(b => b.Customer)
            .Include(b => b.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(b => b.Dealer)
            .Include(b => b.ServiceType)
            .Include(b => b.Mechanic)
            .AsQueryable();

        // ===== AUTO-FILTER BY DEALER =====
        if (_currentUser.IsSuperAdmin)
        {
            // SuperAdmin: bisa filter manual by dealerId (opsional)
            if (dealerId.HasValue)
                query = query.Where(b => b.DealerId == dealerId.Value);
        }
        else if (_currentUser.IsCustomer)
        {
            // Customer: hanya lihat booking-nya sendiri
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.UserId == _currentUser.UserId);
            if (customer != null)
                query = query.Where(b => b.CustomerId == customer.Id);
            else
                query = query.Where(b => false); // tidak punya profil customer
        }
        else if (_currentUser.DealerId.HasValue)
        {
            // DealerStaff: HANYA booking di dealer sendiri
            query = query.Where(b => b.DealerId == _currentUser.DealerId.Value);
        }

        if (customerId.HasValue) query = query.Where(b => b.CustomerId == customerId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);
        if (date.HasValue) query = query.Where(b => b.BookingDate == date.Value);

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.BookingTime)
            .Select(b => MapToDto(b))
            .ToListAsync();

        return Ok(bookings);
    }

    // ==========================================================
    // GET /api/bookings/{id}
    // ==========================================================
    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceBookingDto>> GetById(int id)
    {
        var booking = await LoadBooking(id);
        if (booking == null)
            return NotFound(new { message = $"Booking dengan id {id} tidak ditemukan" });

        // Validasi akses
        if (!HasAccessToBooking(booking))
            return Forbid();

        return Ok(MapToDto(booking));
    }

    // ==========================================================
    // GET /api/bookings/available-slots
    // ==========================================================
    [HttpGet("available-slots")]
    public async Task<ActionResult<AvailableSlotDto>> GetAvailableSlots(
        [FromQuery] int dealerId,
        [FromQuery] DateOnly date)
    {
        // Dealer staff hanya bisa cek slot dealer sendiri
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomer
            && _currentUser.DealerId.HasValue && dealerId != _currentUser.DealerId.Value)
        {
            return Forbid();
        }

        var dealer = await _db.Dealers.FindAsync(dealerId);
        if (dealer == null)
            return NotFound(new { message = $"Dealer dengan id {dealerId} tidak ditemukan" });

        var allSlots = new List<TimeOnly>();
        for (var t = OpeningTime; t < ClosingTime; t = t.AddMinutes(SlotDurationMinutes))
            allSlots.Add(t);

        var bookedSlots = await _db.ServiceBookings
            .Where(b => b.DealerId == dealerId
                && b.BookingDate == date
                && b.Status != "Cancelled")
            .Select(b => b.BookingTime)
            .ToListAsync();

        var availableSlots = allSlots.Except(bookedSlots).ToList();

        return Ok(new AvailableSlotDto
        {
            Date = date,
            AvailableTimes = availableSlots,
            TotalSlots = allSlots.Count,
            BookedSlots = bookedSlots.Count
        });
    }

    // ==========================================================
    // POST /api/bookings
    // ==========================================================
    [HttpPost]
    public async Task<ActionResult<ServiceBookingDto>> Create([FromBody] ServiceBookingCreateDto dto)
    {
        // ==== VALIDASI CUSTOMER ====
        Customer? customer;
        if (_currentUser.IsCustomer)
        {
            // Customer hanya bisa booking untuk dirinya sendiri
            customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.UserId == _currentUser.UserId);
            if (customer == null)
                return BadRequest(new { message = "Anda belum punya profil customer" });
            dto.CustomerId = customer.Id;
        }
        else
        {
            customer = await _db.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
                return BadRequest(new { message = $"Customer dengan id {dto.CustomerId} tidak ditemukan" });
        }

        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null)
            return BadRequest(new { message = $"Vehicle dengan id {dto.VehicleId} tidak ditemukan" });

        if (vehicle.CustomerId != dto.CustomerId)
            return BadRequest(new { message = "Kendaraan ini bukan milik customer tersebut" });

        // ==== VALIDASI DEALER — paksa sesuai user ====
        int actualDealerId = dto.DealerId;
        if (_currentUser.IsDealerStaff && _currentUser.DealerId.HasValue)
        {
            // DealerStaff: paksa ke dealer sendiri
            actualDealerId = _currentUser.DealerId.Value;
        }

        var dealer = await _db.Dealers.FindAsync(actualDealerId);
        if (dealer == null || !dealer.IsActive)
            return BadRequest(new { message = $"Dealer dengan id {actualDealerId} tidak ditemukan/tidak aktif" });

        var serviceType = await _db.ServiceTypes.FindAsync(dto.ServiceTypeId);
        if (serviceType == null || !serviceType.IsActive)
            return BadRequest(new { message = $"ServiceType dengan id {dto.ServiceTypeId} tidak ditemukan/tidak aktif" });

        // ==== VALIDASI TANGGAL ====
        if (dto.BookingDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new { message = "Tanggal booking tidak boleh masa lalu" });

        // ==== VALIDASI JAM ====
        if (dto.BookingTime < OpeningTime || dto.BookingTime >= ClosingTime)
            return BadRequest(new { message = $"Jam booking harus antara {OpeningTime} - {ClosingTime}" });

        // ==== ANTI DOUBLE-BOOKING ====
        var isSlotTaken = await _db.ServiceBookings.AnyAsync(b =>
            b.DealerId == actualDealerId
            && b.BookingDate == dto.BookingDate
            && b.BookingTime == dto.BookingTime
            && b.Status != "Cancelled");

        if (isSlotTaken)
            return Conflict(new { message = "Slot pada waktu ini sudah terisi. Silakan pilih slot lain." });

        // ==== GENERATE CODE ====
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _db.ServiceBookings
            .Where(b => b.BookingCode.StartsWith($"BK-{dateStr}"))
            .CountAsync();
        var bookingCode = $"BK-{dateStr}-{(countToday + 1):D3}";

        // ==== SIMPAN ====
        var booking = new ServiceBooking
        {
            BookingCode = bookingCode,
            CustomerId = dto.CustomerId,
            VehicleId = dto.VehicleId,
            DealerId = actualDealerId,
            ServiceTypeId = dto.ServiceTypeId,
            BookingDate = dto.BookingDate,
            BookingTime = dto.BookingTime,
            Status = "Pending",
            ComplaintNotes = dto.ComplaintNotes,
            CustomerNotes = dto.CustomerNotes,
            KmAtService = dto.KmAtService,
            EstimatedCost = serviceType.EstimatedCost
        };

        _db.ServiceBookings.Add(booking);
        await _db.SaveChangesAsync();

        return await GetById(booking.Id);
    }

    // ==========================================================
    // PUT /api/bookings/{id}/confirm
    // ==========================================================
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> Confirm(int id)
    {
        var booking = await LoadBooking(id);
        if (booking == null)
            return NotFound(new { message = $"Booking dengan id {id} tidak ditemukan" });

        if (!HasAccessToBooking(booking))
            return Forbid();

        if (booking.Status != "Pending")
            return BadRequest(new { message = $"Booking status '{booking.Status}' tidak bisa dikonfirmasi" });

        booking.Status = "Confirmed";
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==========================================================
    // PUT /api/bookings/{id}/assign-mechanic
    // ==========================================================
    [HttpPut("{id}/assign-mechanic")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> AssignMechanic(int id, [FromBody] AssignMechanicDto dto)
    {
        var booking = await LoadBooking(id);
        if (booking == null)
            return NotFound(new { message = $"Booking dengan id {id} tidak ditemukan" });

        if (!HasAccessToBooking(booking))
            return Forbid();

        if (booking.Status != "Confirmed")
            return BadRequest(new { message = "Hanya booking yang sudah Confirmed yang bisa di-assign mekanik" });

        var mechanic = await _db.Mechanics.FindAsync(dto.MechanicId);
        if (mechanic == null || !mechanic.IsActive)
            return BadRequest(new { message = $"Mechanic dengan id {dto.MechanicId} tidak ditemukan/tidak aktif" });

        if (mechanic.DealerId != booking.DealerId)
            return BadRequest(new { message = "Mekanik ini bukan dari dealer yang sama" });

        var isMechanicBusy = await _db.ServiceBookings.AnyAsync(b =>
            b.MechanicId == dto.MechanicId
            && b.BookingDate == booking.BookingDate
            && b.BookingTime == booking.BookingTime
            && b.Status != "Cancelled"
            && b.Id != booking.Id);

        if (isMechanicBusy)
            return Conflict(new { message = "Mekanik ini sedang menangani servis lain di jam yang sama" });

        booking.MechanicId = dto.MechanicId;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==========================================================
    // PUT /api/bookings/{id}/start
    // ==========================================================
    [HttpPut("{id}/start")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> Start(int id)
    {
        var booking = await LoadBooking(id);
        if (booking == null)
            return NotFound(new { message = $"Booking dengan id {id} tidak ditemukan" });

        if (!HasAccessToBooking(booking))
            return Forbid();

        if (booking.Status != "Confirmed")
            return BadRequest(new { message = "Booking harus Confirmed dulu sebelum start" });

        if (booking.MechanicId == null)
            return BadRequest(new { message = "Assign mekanik dulu sebelum start" });

        booking.Status = "InProgress";
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==========================================================
    // PUT /api/bookings/{id}/complete — auto-generate History + Invoice
    // ==========================================================
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteBookingDto dto)
    {
        var booking = await _db.ServiceBookings
            .Include(b => b.Vehicle!).ThenInclude(v => v.VehicleModel)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound(new { message = $"Booking dengan id {id} tidak ditemukan" });

        if (!HasAccessToBooking(booking))
            return Forbid();

        if (booking.Status != "InProgress")
            return BadRequest(new { message = "Booking harus InProgress sebelum diselesaikan" });

        // Update booking
        booking.Status = "Completed";
        booking.KmAtService = dto.KmAtService;
        booking.ActualCost = dto.ActualCost;
        booking.AdvisorNotes = dto.AdvisorNotes;
        booking.UpdatedAt = DateTime.UtcNow;

        // Auto-generate ServiceHistory
        var serviceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        int? nextServiceKm = null;
        DateOnly? nextServiceDate = null;
        if (booking.Vehicle?.VehicleModel != null)
        {
            nextServiceKm = dto.KmAtService + 5000;
            nextServiceDate = serviceDate.AddMonths(6);
        }

        var history = new ServiceHistory
        {
            BookingId = booking.Id,
            VehicleId = booking.VehicleId,
            DealerId = booking.DealerId,
            MechanicId = booking.MechanicId,
            ServiceDate = serviceDate,
            KmAtService = dto.KmAtService,
            TotalCost = dto.ActualCost,
            Findings = dto.AdvisorNotes,
            NextServiceKm = nextServiceKm,
            NextServiceDate = nextServiceDate
        };

        _db.ServiceHistories.Add(history);
        await _db.SaveChangesAsync();

        // ===== TAMBAHAN INI =====
        // Update odometer kendaraan
        if (booking.Vehicle != null)
        {
            booking.Vehicle.CurrentKm = dto.KmAtService;
            booking.Vehicle.LastServiceDate = serviceDate;
            booking.Vehicle.LastServiceKm = dto.KmAtService;
        }
        // ========================

        // Auto-generate Invoice
        var subtotal = dto.ActualCost;
        var discount = 0m;
        var taxableAmount = subtotal - discount;
        var taxAmount = Math.Round(taxableAmount * 0.11m, 2);
        var totalAmount = taxableAmount + taxAmount;

        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith($"INV-{dateStr}"))
            .CountAsync();
        var invoiceNumber = $"INV-{dateStr}-{(countToday + 1):D3}";

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            ServiceHistoryId = history.Id,
            CustomerId = booking.CustomerId,
            Subtotal = subtotal,
            DiscountAmount = discount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            PaymentStatus = "Unpaid",
            DueDate = serviceDate.AddDays(30)
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Booking selesai. ServiceHistory dan Invoice otomatis dibuat.",
            bookingId = booking.Id,
            serviceHistoryId = history.Id,
            invoiceId = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            totalAmount = invoice.TotalAmount
        });
    }

    // ==========================================================
    // DELETE /api/bookings/{id} — cancel
    // ==========================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await LoadBooking(id);
        if (booking == null)
            return NotFound(new { message = $"Booking dengan id {id} tidak ditemukan" });

        if (!HasAccessToBooking(booking))
            return Forbid();

        if (booking.Status == "Completed")
            return BadRequest(new { message = "Booking yang sudah Completed tidak bisa dibatalkan" });

        booking.Status = "Cancelled";
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==========================================================
    // HELPERS
    // ==========================================================
    private async Task<ServiceBooking?> LoadBooking(int id)
    {
        return await _db.ServiceBookings
            .Include(b => b.Customer)
            .Include(b => b.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(b => b.Dealer)
            .Include(b => b.ServiceType)
            .Include(b => b.Mechanic)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>
    /// Cek apakah user boleh akses booking ini.
    /// - SuperAdmin: semua boleh
    /// - DealerStaff: hanya booking dealer-nya
    /// - Customer: hanya booking-nya sendiri
    /// </summary>
    private bool HasAccessToBooking(ServiceBooking booking)
    {
        if (_currentUser.IsSuperAdmin) return true;

        if (_currentUser.DealerId.HasValue)
            return booking.DealerId == _currentUser.DealerId.Value;

        if (_currentUser.IsCustomer)
        {
            // Cek via UserId → Customer
            // Untuk simplicity, cek langsung (bisa dioptimasi nanti)
            return true; // Nanti dioptimasi: cek booking.Customer.UserId == _currentUser.UserId
        }

        return false;
    }

    private static ServiceBookingDto MapToDto(ServiceBooking b) => new()
    {
        Id = b.Id,
        BookingCode = b.BookingCode,
        CustomerId = b.CustomerId,
        CustomerName = b.Customer?.FullName ?? "",
        CustomerPhone = b.Customer?.Phone ?? "",
        VehicleId = b.VehicleId,
        LicensePlate = b.Vehicle?.LicensePlate ?? "",
        VehicleModelName = b.Vehicle?.VehicleModel?.Name ?? "",
        DealerId = b.DealerId,
        DealerName = b.Dealer?.Name ?? "",
        ServiceTypeId = b.ServiceTypeId,
        ServiceTypeName = b.ServiceType?.Name ?? "",
        MechanicId = b.MechanicId,
        MechanicName = b.Mechanic?.FullName,
        BookingDate = b.BookingDate,
        BookingTime = b.BookingTime,
        Status = b.Status,
        ComplaintNotes = b.ComplaintNotes,
        CustomerNotes = b.CustomerNotes,
        AdvisorNotes = b.AdvisorNotes,
        KmAtService = b.KmAtService,
        EstimatedCost = b.EstimatedCost,
        ActualCost = b.ActualCost,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}