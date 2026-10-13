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

// ====================================================================
// 1. AUTO-MIGRATE (PALING PERTAMA — buat database + tabel sebelum apapun)
// ====================================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("⏳ Running database migration...");
        db.Database.Migrate();
        Console.WriteLine("✅ Database migrated successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        throw;
    }
}

// ====================================================================
// 2. SEEDER (semua data master + user demo)
// ====================================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAsync(db, logger);
}

// ====================================================================
// 3. HANGFIRE DASHBOARD + RECURRING JOBS
// ====================================================================
app.UseHangfireDashboard("/hangfire");

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<ReminderService>(
        "generate-reminders",
        service => service.GenerateRemindersAsync(),
        Cron.Daily(8, 0));

    recurringJobManager.AddOrUpdate<ReminderService>(
        "send-reminders",
        service => service.SendPendingRemindersAsync(),
        Cron.Daily(9, 0));

    Console.WriteLine("✅ Hangfire recurring jobs registered");
}

// ====================================================================
// 4. SCALAR (Development only)
// ====================================================================
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

// ====================================================================
// 5. MIDDLEWARE PIPELINE
// ====================================================================
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();