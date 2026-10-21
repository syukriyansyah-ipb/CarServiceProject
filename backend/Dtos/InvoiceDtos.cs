namespace AutoCareApi.Dtos;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ServiceHistoryId { get; set; }

    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InvoicePaymentDto
{
    public decimal PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;   // "Cash", "Transfer", "Card"
}