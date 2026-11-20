namespace RentalPropertyManagement.Models;

public class DashboardStats
{
    public int TotalProperties { get; set; }
    public int TotalTenants { get; set; }
    public int AvailableProperties { get; set; }
    public int OccupiedProperties { get; set; }
    public decimal RentCollected { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal OutstandingRent { get; set; }
    public int TenantsWithBalance { get; set; }
    public List<TenantBalance> TopBalances { get; set; } = new();
}
