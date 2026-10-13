using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Models;

namespace MitsubishiApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        logger.LogInformation("🌱 Starting database seeding...");

        // ============================================================
        // 1. SUPER ADMIN
        // ============================================================
        await SeedSuperAdminAsync(db, logger);

        // ============================================================
        // 2. DEALERS
        // ============================================================
        await SeedDealersAsync(db, logger);

        // ============================================================
        // 3. VEHICLE MODELS
        // ============================================================
        await SeedVehicleModelsAsync(db, logger);

        // ============================================================
        // 4. SERVICE TYPES
        // ============================================================
        await SeedServiceTypesAsync(db, logger);

        // ============================================================
        // 5. CUSTOMERS
        // ============================================================
        await SeedCustomersAsync(db, logger);

        // ============================================================
        // 6. VEHICLES
        // ============================================================
        await SeedVehiclesAsync(db, logger);

        // ============================================================
        // 7. MECHANICS
        // ============================================================
        await SeedMechanicsAsync(db, logger);

        // ============================================================
        // 8. USER STAFF (Service Advisors)
        // ============================================================
        await SeedStaffUsersAsync(db, logger);

        logger.LogInformation("🌱 Database seeding completed");
    }

    // ============================================================
    // 1. SUPER ADMIN
    // ============================================================
    private static async Task SeedSuperAdminAsync(AppDbContext db, ILogger logger)
    {
        var adminEmail = "admin@mitsubishi.com";
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existing == null)
        {
            db.Users.Add(new User
            {
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName = "Super Admin",
                Role = "SuperAdmin",
                DealerId = null
            });
            await db.SaveChangesAsync();
            logger.LogInformation("  ✅ SuperAdmin seeded");
        }
        else if (existing.Role != "SuperAdmin")
        {
            existing.Role = "SuperAdmin";
            existing.DealerId = null;
            await db.SaveChangesAsync();
            logger.LogInformation("  ✅ SuperAdmin updated");
        }
    }

    // ============================================================
    // 2. DEALERS
    // ============================================================
    private static async Task SeedDealersAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Dealers.AnyAsync()) return;

        var dealers = new List<Dealer>
        {
            new()
            {
                Code = "MTR-JKT-01",
                Name = "Mitsubishi Jakarta Selatan",
                Address = "Jl. TB Simatupang No. 10",
                City = "Jakarta",
                Province = "DKI Jakarta",
                Phone = "021-12345678",
                Email = "jkt-selatan@mitsubishi.co.id",
                MaxSlotPerDay = 25,
                IsActive = true
            },
            new()
            {
                Code = "MTR-BDG-01",
                Name = "Mitsubishi Bandung",
                Address = "Jl. Asia Afrika No. 100",
                City = "Bandung",
                Province = "Jawa Barat",
                Phone = "022-87654321",
                Email = "bandung@mitsubishi.co.id",
                MaxSlotPerDay = 15,
                IsActive = true
            }
        };

        db.Dealers.AddRange(dealers);
        await db.SaveChangesAsync();
        logger.LogInformation($"  ✅ {dealers.Count} dealers seeded");
    }

    // ============================================================
    // 3. VEHICLE MODELS
    // ============================================================
    private static async Task SeedVehicleModelsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.VehicleModels.AnyAsync()) return;

        var models = new List<VehicleModel>
        {
            new() { Code = "XPANDER-2023",        Name = "Xpander",       Category = "MPV",        EngineCapacity = 1500, Transmission = "AT", FuelType = "Bensin" },
            new() { Code = "PAJERO-SPORT-2023",   Name = "Pajero Sport",  Category = "SUV",        EngineCapacity = 2400, Transmission = "AT", FuelType = "Diesel" },
            new() { Code = "TRITON-2023",         Name = "Triton",        Category = "Pickup",     EngineCapacity = 2400, Transmission = "MT", FuelType = "Diesel" },
            new() { Code = "L300-2023",           Name = "L300",          Category = "Commercial", EngineCapacity = 2200, Transmission = "MT", FuelType = "Diesel" },
            new() { Code = "OUTLANDER-PHEV-2023", Name = "Outlander PHEV",Category = "SUV",        EngineCapacity = 2400, Transmission = "CVT", FuelType = "Hybrid" }
        };

        db.VehicleModels.AddRange(models);
        await db.SaveChangesAsync();
        logger.LogInformation($"  ✅ {models.Count} vehicle models seeded");
    }

    // ============================================================
    // 4. SERVICE TYPES
    // ============================================================
    private static async Task SeedServiceTypesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.ServiceTypes.AnyAsync()) return;

        var types = new List<ServiceType>
        {
            new() { Code = "PERIODIC-10K", Name = "Servis Berkala 10.000 KM", Description = "Ganti oli, filter oli, cek rem, cek kelistrikan", Category = "Periodic", EstimatedDuration = 90,  EstimatedCost = 750_000,  IsActive = true },
            new() { Code = "PERIODIC-20K", Name = "Servis Berkala 20.000 KM", Description = "Servis 10K + ganti filter udara + tune up", Category = "Periodic", EstimatedDuration = 120, EstimatedCost = 1_200_000, IsActive = true },
            new() { Code = "REPAIR-BRAKE", Name = "Perbaikan Sistem Rem",     Description = "Ganti kampas rem, bleeding, cek minyak rem", Category = "Repair",   EstimatedDuration = 180, EstimatedCost = 850_000,  IsActive = true },
            new() { Code = "EMERGENCY-TOW",Name = "Layanan Darurat & Towing", Description = "Layanan panggilan darurat di jalan", Category = "Emergency", EstimatedDuration = 240, EstimatedCost = 500_000,  IsActive = true }
        };

        db.ServiceTypes.AddRange(types);
        await db.SaveChangesAsync();
        logger.LogInformation($"  ✅ {types.Count} service types seeded");
    }

    // ============================================================
    // 5. CUSTOMERS
    // ============================================================
    private static async Task SeedCustomersAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Customers.AnyAsync()) return;

        var customers = new List<Customer>
        {
            new() { CustomerCode = "CUST-2026-0001", FullName = "Budi Santoso", Phone = "081234567890", Email = "budi@example.com", Address = "Jl. Sudirman No. 1", City = "Jakarta" },
            new() { CustomerCode = "CUST-2026-0002", FullName = "Siti Aminah",  Phone = "081298765432", Email = "siti@example.com", Address = "Jl. Thamrin No. 5",  City = "Bandung" },
            new() { CustomerCode = "CUST-2026-0003", FullName = "Andi Wijaya",  Phone = "081211112222", Email = "andi@example.com", Address = "Jl. Gatot Subroto No. 10", City = "Surabaya" }
        };

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();
        logger.LogInformation($"  ✅ {customers.Count} customers seeded");
    }

    // ============================================================
    // 6. VEHICLES
    // ============================================================
    private static async Task SeedVehiclesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Vehicles.AnyAsync()) return;

        // Ambil ID dari data yang baru di-seed
        var xpander = await db.VehicleModels.FirstOrDefaultAsync(m => m.Code == "XPANDER-2023");
        var pajero  = await db.VehicleModels.FirstOrDefaultAsync(m => m.Code == "PAJERO-SPORT-2023");
        var l300    = await db.VehicleModels.FirstOrDefaultAsync(m => m.Code == "L300-2023");

        var budi = await db.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-2026-0001");
        var siti = await db.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-2026-0002");

        if (xpander == null || pajero == null || l300 == null || budi == null || siti == null)
        {
            logger.LogWarning("  ⚠️ Cannot seed vehicles - missing dependencies");
            return;
        }

        var vehicles = new List<Vehicle>
        {
            new() { VehicleModelId = xpander.Id, CustomerId = budi.Id, Variant = "Ultimate AT", Year = 2023, LicensePlate = "B 1234 XYZ", Color = "Putih",  Price = 280_000_000, CurrentKm = 15_000 },
            new() { VehicleModelId = pajero.Id,  CustomerId = budi.Id, Variant = "Dakar 4x4",   Year = 2023, LicensePlate = "B 5678 ABC", Color = "Hitam",  Price = 750_000_000, CurrentKm = 8_500 },
            new() { VehicleModelId = l300.Id,    CustomerId = siti.Id, Variant = "Blind Van",   Year = 2022, LicensePlate = "B 9999 DEF", Color = "Silver", Price = 220_000_000, CurrentKm = 32_000 }
        };

        db.Vehicles.AddRange(vehicles);
        await db.SaveChangesAsync();
        logger.LogInformation($"  ✅ {vehicles.Count} vehicles seeded");
    }

    // ============================================================
    // 7. MECHANICS
    // ============================================================
    private static async Task SeedMechanicsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Mechanics.AnyAsync()) return;

        var jkt = await db.Dealers.FirstOrDefaultAsync(d => d.Code == "MTR-JKT-01");
        var bdg = await db.Dealers.FirstOrDefaultAsync(d => d.Code == "MTR-BDG-01");

        if (jkt == null || bdg == null)
        {
            logger.LogWarning("  ⚠️ Cannot seed mechanics - missing dealers");
            return;
        }

        var mechanics = new List<Mechanic>
        {
            new() { EmployeeCode = "MEC-JKT-001", FullName = "Ahmad Sutrisno",   DealerId = jkt.Id, Specialization = "Engine",     Certification = "Mitsubishi Certified Level 2", Phone = "081111111111", IsAvailable = true, IsActive = true },
            new() { EmployeeCode = "MEC-JKT-002", FullName = "Bambang Hartono",  DealerId = jkt.Id, Specialization = "Electrical", Certification = "Mitsubishi Certified Level 1", Phone = "081222222222", IsAvailable = true, IsActive = true },
            new() { EmployeeCode = "MEC-BDG-001", FullName = "Cecep Supriadi",   DealerId = bdg.Id, Specialization = "Body & Paint",Certification = "Mitsubishi Certified Level 3", Phone = "081333333333", IsAvailable = true, IsActive = true }
        };

        db.Mechanics.AddRange(mechanics);
        await db.SaveChangesAsync();
        logger.LogInformation($"  ✅ {mechanics.Count} mechanics seeded");
    }

    // ============================================================
    // 8. STAFF USERS (Service Advisors)
    // ============================================================
    private static async Task SeedStaffUsersAsync(AppDbContext db, ILogger logger)
    {
        var jkt = await db.Dealers.FirstOrDefaultAsync(d => d.Code == "MTR-JKT-01");
        var bdg = await db.Dealers.FirstOrDefaultAsync(d => d.Code == "MTR-BDG-01");

        if (jkt == null || bdg == null)
        {
            logger.LogWarning("  ⚠️ Cannot seed staff users - missing dealers");
            return;
        }

        var staff = new List<User>
        {
            new() { Email = "sa.jkt@mitsubishi.co.id", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), FullName = "Adi Nugroho",     Role = "ServiceAdvisor", DealerId = jkt.Id },
            new() { Email = "sa.bdg@mitsubishi.co.id", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), FullName = "Bambang Setiawan", Role = "ServiceAdvisor", DealerId = bdg.Id }
        };

        var newCount = 0;
        foreach (var user in staff)
        {
            if (!await db.Users.AnyAsync(u => u.Email == user.Email))
            {
                db.Users.Add(user);
                newCount++;
            }
        }

        if (newCount > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation($"  ✅ {newCount} staff users seeded");
        }
    }
}