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
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly InvoicePdfService _pdfService;  

    public InvoicesController(AppDbContext db, ICurrentUserService currentUser, InvoicePdfService pdfService)
    {
        _db = db;
        _currentUser = currentUser;
        _pdfService = pdfService;   
    }

    // ==========================================================
    // GET /api/invoices?status=Unpaid&customerId=1
    // ==========================================================
    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? customerId)
    {
        var query = _db.Invoices
            .Include(i => i.Customer)
            .AsQueryable();

        // ===== AUTO-FILTER BY DEALER =====
        if (_currentUser.IsSuperAdmin)
        {
            // no filter
        }
        else if (_currentUser.IsCustomer)
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.UserId == _currentUser.UserId);
            if (customer != null)
                query = query.Where(i => i.CustomerId == customer.Id);
            else
                query = query.Where(i => false);
        }
        else if (_currentUser.DealerId.HasValue)
        {
            // Dealer staff: invoice yang terkait service history dealer sendiri
            var dealerId = _currentUser.DealerId.Value;
            query = query.Where(i => _db.ServiceHistories
                .Any(h => h.Id == i.ServiceHistoryId && h.DealerId == dealerId));
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.PaymentStatus == status);
        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => MapToDto(i))
            .ToListAsync();

        return Ok(invoices);
    }

    // ==========================================================
    // GET /api/invoices/{id}
    // ==========================================================
    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = $"Invoice dengan id {id} tidak ditemukan" });

        // Validasi akses untuk dealer staff
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            var dealerId = _currentUser.DealerId.Value;
            var belongsToDealer = await _db.ServiceHistories
                .AnyAsync(h => h.Id == invoice.ServiceHistoryId && h.DealerId == dealerId);
            if (!belongsToDealer)
                return Forbid();
        }

        return Ok(MapToDto(invoice));
    }

    // ==========================================================
    // POST /api/invoices/{id}/pay
    // ==========================================================
    [HttpPost("{id}/pay")]
    [Authorize(Roles = "SuperAdmin,DealerAdmin,ServiceAdvisor")]
    public async Task<IActionResult> Pay(int id, [FromBody] InvoicePaymentDto dto)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice == null)
            return NotFound(new { message = $"Invoice dengan id {id} tidak ditemukan" });

        // Validasi akses untuk dealer staff
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            var dealerId = _currentUser.DealerId.Value;
            var belongsToDealer = await _db.ServiceHistories
                .AnyAsync(h => h.Id == invoice.ServiceHistoryId && h.DealerId == dealerId);
            if (!belongsToDealer)
                return Forbid();
        }

        if (invoice.PaymentStatus == "Paid")
            return BadRequest(new { message = "Invoice sudah lunas" });

        if (dto.PaidAmount <= 0)
            return BadRequest(new { message = "Jumlah bayar harus lebih dari 0" });

        var newPaidAmount = invoice.PaidAmount + dto.PaidAmount;

        if (newPaidAmount > invoice.TotalAmount)
            return BadRequest(new { message = $"Jumlah bayar melebihi total tagihan (Rp {invoice.TotalAmount:N0})" });

        invoice.PaidAmount = newPaidAmount;
        invoice.PaymentMethod = dto.PaymentMethod;

        if (newPaidAmount >= invoice.TotalAmount)
        {
            invoice.PaymentStatus = "Paid";
            invoice.PaidAt = DateTime.UtcNow;
        }
        else
        {
            invoice.PaymentStatus = "Partial";
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Pembayaran berhasil. Status: {invoice.PaymentStatus}",
            paidAmount = invoice.PaidAmount,
            remaining = invoice.TotalAmount - invoice.PaidAmount
        });
    }

    // ==========================================================
    // GET /api/invoices/{id}/pdf — Download PDF
    // ==========================================================
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.ServiceHistory).ThenInclude(h => h!.Vehicle).ThenInclude(v => v!.VehicleModel)
            .Include(i => i.ServiceHistory).ThenInclude(h => h!.Dealer)
            .Include(i => i.ServiceHistory).ThenInclude(h => h!.Mechanic)
            .Include(i => i.ServiceHistory).ThenInclude(h => h!.Booking).ThenInclude(b => b!.ServiceType)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = $"Invoice dengan id {id} tidak ditemukan" });

        // Validasi akses dealer staff
        if (!_currentUser.IsSuperAdmin && _currentUser.DealerId.HasValue)
        {
            var dealerId = _currentUser.DealerId.Value;
            if (invoice.ServiceHistory == null || invoice.ServiceHistory.DealerId != dealerId)
                return Forbid();
        }

        var history = invoice.ServiceHistory!;
        var customer = invoice.Customer;

        var pdfBytes = _pdfService.GenerateInvoicePdf(
            invoice: invoice,
            history: history,
            customerName: customer?.FullName ?? "-",
            customerPhone: customer?.Phone ?? "-",
            customerEmail: customer?.Email,
            customerAddress: customer?.Address ?? "-",
            customerCity: customer?.City,
            dealerName: history.Dealer?.Name ?? "-",
            dealerAddress: history.Dealer?.Address ?? "-",
            dealerPhone: history.Dealer?.Phone ?? "-",
            vehicleModelName: history.Vehicle?.VehicleModel?.Name ?? "-",
            licensePlate: history.Vehicle?.LicensePlate ?? "-",
            serviceTypeName: history.Booking?.ServiceType?.Name ?? "Jasa Servis",
            mechanicName: history.Mechanic?.FullName
        );

        return File(pdfBytes, "application/pdf", $"Invoice-{invoice.InvoiceNumber}.pdf");
    }

    private static InvoiceDto MapToDto(Invoice i) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        ServiceHistoryId = i.ServiceHistoryId,
        CustomerId = i.CustomerId,
        CustomerName = i.Customer?.FullName ?? "",
        CustomerPhone = i.Customer?.Phone ?? "",
        Subtotal = i.Subtotal,
        DiscountAmount = i.DiscountAmount,
        TaxAmount = i.TaxAmount,
        TotalAmount = i.TotalAmount,
        PaidAmount = i.PaidAmount,
        PaymentStatus = i.PaymentStatus,
        PaymentMethod = i.PaymentMethod,
        PaidAt = i.PaidAt,
        DueDate = i.DueDate,
        CreatedAt = i.CreatedAt
    };
}