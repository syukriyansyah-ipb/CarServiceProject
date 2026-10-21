namespace AutoCareApi.Dtos;

//respon sederhana untuk list
public class VehicleSimpleDto
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string VehicleModelName { get; set; } = string.Empty;
}

//respon detail untuk get by id
public class VehicleDetailDto
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public VehicleModelSimpleDto VehicleModel { get; set; } = null!;
    public CustomerSimpleDto Customer { get; set; } = null!;
}

// response untuk VehicleModel (simple)
public class VehicleModelSimpleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

// response untuk Customer (simple)
public class CustomerSimpleDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

//response untuk Create/Update
public class VehicleCreateUpdateDto
{
    public int VehicleModelId { get; set; }
    public int CustomerId { get; set; }
    public string Variant { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
}