namespace RentalPropertyManagement.Models;

public class TenantBalance
{
    public int TenantId { get; set; }
    public string TenantName { get; set; }
    public string PropertyName { get; set; }
    public decimal MonthlyRent { get; set; }
    public int MonthsDue { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; }
}
