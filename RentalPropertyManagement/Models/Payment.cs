namespace RentalPropertyManagement.Models;

public class Payment
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string Method { get; set; }
    /// <summary>The month the payment is for, stored as yyyy-MM.</summary>
    public string Period { get; set; }
    public string Status { get; set; } = PaymentStatus.Outstanding;
    public string Notes { get; set; } = "";
}
