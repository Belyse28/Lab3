namespace RentalPropertyManagement.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Only used for users with the Tenant role: the tenant record they may view.</summary>
    public int? TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public string StatusText => IsActive ? "Active" : "Inactive";
}
