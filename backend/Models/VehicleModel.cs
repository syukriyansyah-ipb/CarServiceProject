namespace MitsubishiApi.Models
{
    public class VehicleModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // XP, OL, EC, ASX, L200, PS
        public string Name { get; set; } = string.Empty; // Xpander, Outlander, Eclipse Cross, ASX, L200, Pajero Sport
        public string Category { get; set; } = string.Empty; // MPV, SUV, Pickup

        public int EngineCapacity { get; set; } // 1500, 2000, 2500
        public string Transmission { get; set; } = string.Empty; // Manual, Automatic
        public string FuelType { get; set; } = string.Empty; // Petrol, Diesel, Hybrid

        // Navigation property for the related Vehicle entities = 1 model banyak vehicles
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}