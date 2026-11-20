using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement;

/// <summary>Holds the logged-in user and enforces role-based access in the controllers.</summary>
public static class Session
{
    public static User CurrentUser { get; private set; }

    public static void Start(User user) => CurrentUser = user;
    public static void End() => CurrentUser = null;

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.Role == Roles.Admin;
    public static bool IsLandlord => CurrentUser?.Role == Roles.Landlord;
    public static bool IsTenant => CurrentUser?.Role == Roles.Tenant;
    /// <summary>Administrators and landlords can manage properties, tenants, payments and reports.</summary>
    public static bool CanManage => IsAdmin || IsLandlord;

    public static void RequireLogin()
    {
        if (!IsLoggedIn) throw new ValidationException("Please log in to continue.");
    }

    public static void RequireManager()
    {
        RequireLogin();
        if (!CanManage) throw new ValidationException("You do not have permission to perform this action.");
    }

    public static void RequireAdmin()
    {
        RequireLogin();
        if (!IsAdmin) throw new ValidationException("Only administrators can perform this action.");
    }
}
