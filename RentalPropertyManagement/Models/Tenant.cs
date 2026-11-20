namespace RentalPropertyManagement.Models;

public class Tenant
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; } = "";
    public string IdNumber { get; set; }
    public int? PropertyId { get; set; }
    public string PropertyName { get; set; } = "";
    public decimal MonthlyRent { get; set; }
    public DateTime LeaseStartDate { get; set; } = DateTime.Today;
}
