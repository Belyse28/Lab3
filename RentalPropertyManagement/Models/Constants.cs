namespace RentalPropertyManagement.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Landlord = "Landlord";
    public const string Tenant = "Tenant";
    public static readonly string[] All = { Admin, Landlord, Tenant };
}

public static class PropertyStatus
{
    public const string Available = "Available";
    public const string Occupied = "Occupied";
}

public static class PropertyTypes
{
    public static readonly string[] All = { "Apartment", "House", "Studio", "Villa", "Room", "Commercial" };
}

public static class PaymentMethods
{
    public static readonly string[] All = { "Cash", "Mobile Money", "Bank Transfer", "Card", "Cheque" };
}

public static class PaymentStatus
{
    public const string Paid = "Paid";
    public const string PartiallyPaid = "Partially Paid";
    public const string Outstanding = "Outstanding";
}
