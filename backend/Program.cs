using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MitsubishiApi.Data;
using MitsubishiApi.Services;
using MitsubishiApi.Models;
using Scalar.AspNetCore;
using System.Text;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==== HANGFIRE (Background Job) ====
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
          {
              CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
              QueuePollInterval = TimeSpan.Zero,
              UseRecommendedIsolationLevel = true,
              DisableGlobalLocks = true
          });
});

builder.Services.AddHangfireServer();

// Register ReminderService
builder.Services.AddScoped<ReminderService>();

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MitsubishiApi:";
});

// Elasticsearch
builder.Services.AddSingleton<ElasticSearchService>();

// JWT
builder.Services.AddScoped<JwtService>();

// CurrentUser (dari JWT claims)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key tidak dikonfigurasi")))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<InvoicePdfService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5500", "http://127.0.0.1:5500")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


var app = builder.Build();

// ==== HANGFIRE DASHBOARD + RECURRING JOBS ====
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Untuk portfolio, biarkan tanpa auth (nanti bisa ditambah)
    // Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Setup recurring jobs
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Generate reminders tiap hari jam 8 pagi
    recurringJobManager.AddOrUpdate<ReminderService>(
        "generate-reminders",
        service => service.GenerateRemindersAsync(),
        Cron.Daily(8, 0)); // 08:00

    // Kirim pending reminders tiap hari jam 9 pagi
    recurringJobManager.AddOrUpdate<ReminderService>(
        "send-reminders",
        service => service.SendPendingRemindersAsync(),
        Cron.Daily(9, 0)); // 09:00
}

// ==== SEEDER: Pastikan ada SuperAdmin ====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminEmail = "admin@mitsubishi.com";
    
    var admin = db.Users.FirstOrDefault(u => u.Email == adminEmail);
    if (admin == null)
    {
        db.Users.Add(new User
        {
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FullName = "Super Admin",
            Role = "SuperAdmin",
            DealerId = null
        });
        db.SaveChanges();
        Console.WriteLine("✅ SuperAdmin seeded");
    }
    else if (admin.Role != "SuperAdmin")
    {
        admin.Role = "SuperAdmin";
        admin.DealerId = null;
        db.SaveChanges();
        Console.WriteLine("✅ SuperAdmin updated");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .AddPreferredSecuritySchemes("Bearer")
            .AddHttpAuthentication("Bearer", auth =>
            {
                auth.Token = "masukkan-token-jwt-anda";
            });
    });
}
// ===== AUTO-MIGRATE DATABASE =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("✅ Database migrated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        throw;
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();