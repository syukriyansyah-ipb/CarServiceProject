# 🚗 AutoCare Service Booking System

Sistem manajemen booking servis kendaraan AutoCare yang lengkap dengan autentikasi JWT, multi-tenant dealer isolation, analytics dashboard, dan background job untuk reminder servis.

![Dashboard Analytics](docs/screenshots/analytics.png)

## ✨ Fitur Utama

- 🔐 **Multi-tenant Auth** — 5 role (SuperAdmin, DealerAdmin, ServiceAdvisor, Mechanic, Customer) dengan data isolation per dealer
- 📅 **Booking Workflow** — Pending → Confirmed → InProgress → Completed, dengan validasi anti double-booking
- 🤖 **Background Job** — Hangfire untuk auto-generate reminder servis 30 hari sebelum jatuh tempo
- 📊 **Analytics Dashboard** — KPI + 4 chart interaktif (booking trend, revenue per dealer, distribusi status, top kendaraan)
- 🔍 **Fuzzy Search** — Elasticsearch untuk cari by plat, model, atau nama customer
- ⚡ **Redis Cache** — master data cache untuk performa
- 🧾 **Invoice PDF** — auto-generate dengan brand AutoCare + PPN 11%
- 📱 **Role-based UI** — menu dan data terfilter sesuai role & dealer

## 🛠 Tech Stack

| Layer | Teknologi |
|---|---|
| Backend | .NET 10, ASP.NET Core, EF Core |
| Database | SQL Server 2022 |
| Cache | Redis 7 |
| Search | Elasticsearch 8.15 |
| Background Job | Hangfire |
| PDF | QuestPDF |
| Auth | JWT Bearer |
| Frontend | jQuery, Kendo UI, HTML/CSS |
| Container | Docker Compose |
| API Docs | Scalar (OpenAPI) |

## 🏗 Arsitektur
┌──────────────────────────────────────────┐
│ Frontend (jQuery + Kendo UI) │
│ ├─ Login (JWT) │
│ ├─ Dashboard (Kendo Grid) │
│ ├─ Analytics (Kendo Chart) │
│ └─ Role-based menu │
└────────────────┬─────────────────────────┘
│ HTTPS + JWT (dealer-aware)
┌────────────────▼─────────────────────────┐
│ ASP.NET Core Web API │
│ ├─ Controllers (10) │
│ ├─ Services (JWT, ES, PDF, Reminder) │
│ ├─ Multi-tenant filter (auto) │
│ └─ Hangfire background jobs │
└─────┬──────────┬──────────┬──────────────┘
│ │ │
▼ ▼ ▼
┌──────┐ ┌──────┐ ┌──────────────┐
│ SQL │ │Redis │ │Elasticsearch │
└──────┘ └──────┘ └──────────────┘


## 🚀 Cara Menjalankan

### Prasyarat
- .NET 10 SDK
- Docker Desktop
- Git

### 1. Clone & Setup
```bash
git clone https://github.com/USERNAME/AutoCare-service-booking.git
cd AutoCare-service-booking
docker compose up -d    # SQL Server, Redis, Elasticsearch

### 2. Run API
```bash
cd AutoCareApi
dotnet ef database update
dotnet run

### Run Frontend
cd frontend
# Buka login.html dengan Live Server

### Login Demo
Role	Email	Password	Dealer
SuperAdmin	admin@AutoCare.com	Admin123!	Head Office
Service Advisor	sa.jkt@AutoCare.co.id	Password123!	Jakarta Selatan
Service Advisor	sa.bdg@AutoCare.co.id	Password123!	Bandung

🔐 Keamanan
Password hash BCrypt
JWT HMAC-SHA256 (60 menit expiry)
Multi-tenant isolation — data terfilter otomatis berdasarkan claim dealerId
Anti-IDOR — validasi ownership di setiap endpoint
Soft delete untuk master data
CORS spesifik origin

🎯 Fitur Unggulan untuk Demo
Multi-tenant — Login sebagai SA Jakarta vs SA Bandung, data terisolasi
Anti double-booking — Coba booking slot yang sama → HTTP 409 Conflict
Auto-generate Invoice — Complete booking → ServiceHistory + Invoice otomatis
Background Job — Hangfire dashboard menunjukkan recurring jobs
PDF Invoice — Download invoice dengan brand AutoCare
Analytics — Chart interaktif dengan data real-time

📄 Lisensi
MIT License — bebas dipakai untuk keperluan pembelajaran.


