using Microsoft.EntityFrameworkCore;
using AutoCareApi.Data;
using AutoCareApi.Models;

namespace AutoCareApi.Services;

public class ReminderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(AppDbContext db, ILogger<ReminderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Job 1: Scan ServiceHistory dan bikin ServiceReminder untuk yang due dalam 30 hari ke depan.
    /// Dijadwalkan jalan tiap hari jam 08:00.
    /// </summary>
    public async Task GenerateRemindersAsync()
    {
        _logger.LogInformation("🔔 [ReminderJob] Mulai generate reminders...");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thresholdDate = today.AddDays(30);

        // Ambil ServiceHistory dengan nextServiceDate atau nextServiceKm yang di-set
        var candidates = await _db.ServiceHistories
            .Include(h => h.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(h => h.Vehicle!).ThenInclude(v => v.Customer)
            .Where(h => (h.NextServiceDate != null || h.NextServiceKm != null)
                && h.Vehicle != null)
            .ToListAsync();

        var newReminders = new List<ServiceReminder>();

        foreach (var history in candidates)
        {
            // Cek apakah sudah ada reminder aktif untuk vehicle ini
            var existsReminder = await _db.ServiceReminders
                .AnyAsync(r => r.VehicleId == history.VehicleId
                    && r.Status != "Completed"
                    && r.Status != "Expired"
                    && r.LastServiceHistoryId == history.Id);

            if (existsReminder) continue;

            // Cek kondisi: due by date atau due by km
            var isDueByDate = history.NextServiceDate.HasValue
                && history.NextServiceDate.Value <= thresholdDate;

            var currentKm = history.Vehicle?.CurrentKm ?? 0;
            var isDueByKm = history.NextServiceKm.HasValue
                && currentKm >= (history.NextServiceKm.Value - 1000); // 1000 KM sebelum due

            if (!isDueByDate && !isDueByKm) continue;

            var reminder = new ServiceReminder
            {
                VehicleId = history.VehicleId,
                CustomerId = history.Vehicle!.CustomerId,
                DealerId = history.DealerId,
                LastServiceHistoryId = history.Id,
                ReminderType = isDueByDate && isDueByKm ? "Both"
                    : isDueByDate ? "DateBased" : "KmBased",
                DueKm = history.NextServiceKm,
                DueDate = history.NextServiceDate,
                Status = "Pending",
                NotificationChannel = "Email",
                Notes = $"Servis terakhir: {history.ServiceDate:dd MMM yyyy} di {history.KmAtService:N0} KM"
            };

            newReminders.Add(reminder);
        }

        if (newReminders.Any())
        {
            _db.ServiceReminders.AddRange(newReminders);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"🔔 [ReminderJob] Dibuat {newReminders.Count} reminder baru");
        }
        else
        {
            _logger.LogInformation("🔔 [ReminderJob] Tidak ada reminder baru");
        }
    }

    /// <summary>
    /// Job 2: "Kirim" reminder yang Pending (simulasi).
    /// Dijadwalkan jalan tiap hari jam 09:00.
    /// </summary>
    public async Task SendPendingRemindersAsync()
    {
        _logger.LogInformation("📧 [ReminderJob] Mulai kirim pending reminders...");

        var pending = await _db.ServiceReminders
            .Include(r => r.Customer)
            .Include(r => r.Vehicle!).ThenInclude(v => v.VehicleModel)
            .Include(r => r.Dealer)
            .Where(r => r.Status == "Pending")
            .Take(50) // batch maksimal 50 per run
            .ToListAsync();

        if (!pending.Any())
        {
            _logger.LogInformation("📧 [ReminderJob] Tidak ada reminder untuk dikirim");
            return;
        }

        foreach (var reminder in pending)
        {
            // Simulasi kirim (di production: panggil SMTP / WhatsApp API / Twilio)
            var message = BuildMessage(reminder);
            _logger.LogInformation($"📧 [SEND] Ke: {reminder.Customer?.Email ?? reminder.Customer?.Phone}");
            _logger.LogInformation($"     {message}");

            reminder.Status = "Sent";
            reminder.SentAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation($"📧 [ReminderJob] Terkirim {pending.Count} reminder");
    }

    /// <summary>
    /// Bikin pesan reminder (dipisah biar reusable).
    /// </summary>
    private static string BuildMessage(ServiceReminder r)
    {
        var vehicle = r.Vehicle;
        var model = vehicle?.VehicleModel?.Name ?? "kendaraan";
        var plate = vehicle?.LicensePlate ?? "";
        var customerName = r.Customer?.FullName ?? "Customer";
        var dealerName = r.Dealer?.Name ?? "dealer";

        return $"Halo {customerName}, {model} ({plate}) Anda sudah waktunya servis berkala. " +
               $"Silakan datang ke {dealerName}. Info: {r.Notes}";
    }
}