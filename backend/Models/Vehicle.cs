namespace AutoCareApi.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        
        public string Variant { get; set; } = string.Empty; // GLS, GLS Premium, GLS Sport, GLS Limited, GLS Exceed, GLS Ultimate
        public int Year { get; set; }
        public string LicensePlate { get; set; } = string.Empty; // B 1234 XYZ
        public string Color { get; set; } = string.Empty; // Red, Black, White, Silver, Grey
        public decimal Price { get; set; } // 300000000, 400000000, 500000000  

        // ===== TAMBAHAN =====
        public int CurrentKm { get; set; } = 0;                    // Odometer terakhir
        public DateOnly? LastServiceDate { get; set; }             // Servis terakhir
        public int? LastServiceKm { get; set; }                    // KM saat servis terakhir
        // ====================

        // FK to VehicleModel
        public int VehicleModelId { get; set; }
        public VehicleModel? VehicleModel { get; set; }

        // FK to Customer
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}