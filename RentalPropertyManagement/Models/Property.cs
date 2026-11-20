namespace RentalPropertyManagement.Models;

public class Property
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string Type { get; set; }
    public string Description { get; set; } = "";
    public int Rooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string Status { get; set; } = PropertyStatus.Available;
    public string TenantName { get; set; } = "";
}
