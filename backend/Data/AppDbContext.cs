using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Models;

namespace MitsubishiApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();
    public DbSet<ServiceHistory> ServiceHistories => Set<ServiceHistory>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ServiceReminder> ServiceReminders => Set<ServiceReminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Vehicle
        modelBuilder.Entity<Vehicle>()
            .Property(v => v.Price)
            .HasPrecision(18, 2);

        // ServiceType
        modelBuilder.Entity<ServiceType>()
            .Property(s => s.EstimatedCost)
            .HasPrecision(18, 2);

        // User → Dealer
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Dealer)
                .WithMany()
                .HasForeignKey(u => u.DealerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ServiceBooking
        modelBuilder.Entity<ServiceBooking>(entity =>
        {
            entity.Property(b => b.EstimatedCost).HasPrecision(18, 2);
            entity.Property(b => b.ActualCost).HasPrecision(18, 2);
            entity.HasIndex(b => b.BookingCode).IsUnique();
            entity.HasIndex(b => new { b.DealerId, b.BookingDate });

            entity.HasOne(b => b.Customer).WithMany().HasForeignKey(b => b.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.Vehicle).WithMany().HasForeignKey(b => b.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.Dealer).WithMany().HasForeignKey(b => b.DealerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.ServiceType).WithMany().HasForeignKey(b => b.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.Mechanic).WithMany().HasForeignKey(b => b.MechanicId).OnDelete(DeleteBehavior.Restrict);
        });

        // ServiceHistory
        modelBuilder.Entity<ServiceHistory>(entity =>
        {
            entity.Property(h => h.TotalCost).HasPrecision(18, 2);
            entity.HasIndex(h => h.BookingId).IsUnique();

            entity.HasOne(h => h.Booking).WithMany().HasForeignKey(h => h.BookingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.Vehicle).WithMany().HasForeignKey(h => h.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.Dealer).WithMany().HasForeignKey(h => h.DealerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.Mechanic).WithMany().HasForeignKey(h => h.MechanicId).OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(i => i.Subtotal).HasPrecision(18, 2);
            entity.Property(i => i.DiscountAmount).HasPrecision(18, 2);
            entity.Property(i => i.TaxAmount).HasPrecision(18, 2);
            entity.Property(i => i.TotalAmount).HasPrecision(18, 2);
            entity.Property(i => i.PaidAmount).HasPrecision(18, 2);

            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.HasIndex(i => i.ServiceHistoryId).IsUnique();

            entity.HasOne(i => i.ServiceHistory).WithMany().HasForeignKey(i => i.ServiceHistoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        // ServiceReminder
        modelBuilder.Entity<ServiceReminder>(entity =>
        {
            entity.HasIndex(r => new { r.Status, r.DueDate });
            entity.HasIndex(r => r.DealerId);

            entity.HasOne(r => r.Vehicle).WithMany().HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Dealer).WithMany().HasForeignKey(r => r.DealerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.LastServiceHistory).WithMany().HasForeignKey(r => r.LastServiceHistoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}