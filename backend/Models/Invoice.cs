namespace MitsubishiApi.Models;

public class Invoice
{
    public int Id { get; set; }
    
    public string InvoiceNumber { get; set; } = string.Empty;   // "INV-20261011-001"
    
    // Link ke ServiceHistory (unique — 1 history = 1 invoice)
    public int ServiceHistoryId { get; set; }
    public ServiceHistory? ServiceHistory { get; set; }
    
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    // Nominal
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TaxAmount { get; set; }        // PPN 11%
    public decimal TotalAmount { get; set; }
    
    // Pembayaran
    public decimal PaidAmount { get; set; } = 0;
    public string PaymentStatus { get; set; } = "Unpaid";   // "Unpaid", "Partial", "Paid"
    public string? PaymentMethod { get; set; }              // "Cash", "Transfer", "Card"
    public DateTime? PaidAt { get; set; }
    
    public DateOnly? DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}