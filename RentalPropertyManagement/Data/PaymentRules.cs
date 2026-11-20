using Microsoft.Data.Sqlite;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Data;

/// <summary>
/// Business rules for rent. A payment's status reflects its rent month:
/// total paid for that month >= monthly rent -> Paid; some but not all -> Partially Paid.
/// Outstanding rent = (monthly rent x months since lease start, including the current month) - total paid.
/// </summary>
public static class PaymentRules
{
    public static string StatusFor(decimal paid, decimal rent)
    {
        if (paid + 0.005m >= rent) return PaymentStatus.Paid;
        return paid > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Outstanding;
    }

    public static string BalanceStatus(decimal charges, decimal paid)
    {
        if (charges <= 0) return paid > 0 ? PaymentStatus.Paid : "No Rent Due";
        decimal outstanding = charges - paid;
        if (outstanding <= 0.005m) return PaymentStatus.Paid;
        return paid > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Outstanding;
    }

    public static int MonthsDue(DateTime leaseStart, DateTime today)
    {
        int months = (today.Year - leaseStart.Year) * 12 + today.Month - leaseStart.Month + 1;
        return Math.Max(0, months);
    }

    /// <summary>Re-computes the status of every payment of one tenant/property/month.</summary>
    public static void Recalculate(SqliteConnection conn, SqliteTransaction tx, int tenantId, int propertyId, string period)
    {
        decimal rent = Database.Dec(Database.Scalar(conn,
            "SELECT MonthlyRent FROM Properties WHERE Id=@p", tx, ("@p", propertyId)));
        decimal paid = Database.Dec(Database.Scalar(conn,
            "SELECT COALESCE(SUM(Amount),0) FROM Payments WHERE TenantId=@t AND PropertyId=@p AND Period=@per",
            tx, ("@t", tenantId), ("@p", propertyId), ("@per", period)));

        Database.NonQuery(conn,
            "UPDATE Payments SET Status=@s WHERE TenantId=@t AND PropertyId=@p AND Period=@per",
            tx, ("@s", StatusFor(paid, rent)), ("@t", tenantId), ("@p", propertyId), ("@per", period));
    }

    /// <summary>Used after a property's rent changes.</summary>
    public static void RecalculateForProperty(SqliteConnection conn, SqliteTransaction tx, int propertyId)
    {
        var groups = new List<(int Tenant, string Period)>();
        using (var cmd = Database.Command(conn,
            "SELECT DISTINCT TenantId, Period FROM Payments WHERE PropertyId=@p", tx, ("@p", propertyId)))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read()) groups.Add((r.GetInt32(0), r.GetString(1)));
        }
        foreach (var g in groups) Recalculate(conn, tx, g.Tenant, propertyId, g.Period);
    }
}
