using System.Globalization;
using Microsoft.Data.Sqlite;
using RentalPropertyManagement.Data;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Controllers;

public class PaymentController
{
    private const string SelectSql =
        @"SELECT pay.Id, pay.TenantId, t.FullName, pay.PropertyId, p.Name, pay.Amount,
                 pay.PaymentDate, pay.Method, pay.Period, pay.Status, COALESCE(pay.Notes,'')
          FROM Payments pay
          JOIN Tenants t ON t.Id = pay.TenantId
          JOIN Properties p ON p.Id = pay.PropertyId ";

    private static Payment Map(SqliteDataReader r) => new Payment
    {
        Id = r.GetInt32(0),
        TenantId = r.GetInt32(1),
        TenantName = r.GetString(2),
        PropertyId = r.GetInt32(3),
        PropertyName = r.GetString(4),
        Amount = Database.Dec(r.GetValue(5)),
        PaymentDate = Database.ParseDate(r.GetString(6)),
        Method = r.GetString(7),
        Period = r.GetString(8),
        Status = r.GetString(9),
        Notes = r.GetString(10)
    };

    /// <summary>Payment history. tenantId = 0 means all tenants. Tenant users only ever see their own.</summary>
    public List<Payment> GetAll(int tenantId = 0)
    {
        Session.RequireLogin();
        if (Session.IsTenant) tenantId = Session.CurrentUser.TenantId ?? -1;

        var list = new List<Payment>();
        using var conn = Database.Open();
        using var cmd = Database.Command(conn,
            SelectSql + "WHERE (@tid = 0 OR pay.TenantId = @tid) ORDER BY pay.PaymentDate DESC, pay.Id DESC",
            null, ("@tid", tenantId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public void Add(Payment p)
    {
        Session.RequireManager();
        Validate(p);
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();

        p.PropertyId = GetTenantProperty(conn, tx, p.TenantId);
        Database.NonQuery(conn,
            @"INSERT INTO Payments(TenantId,PropertyId,Amount,PaymentDate,Method,Period,Status,Notes)
              VALUES(@t,@pr,@a,@d,@m,@per,'Outstanding',@n)", tx,
            ("@t", p.TenantId), ("@pr", p.PropertyId), ("@a", p.Amount), ("@d", p.PaymentDate),
            ("@m", p.Method), ("@per", p.Period), ("@n", (p.Notes ?? "").Trim()));

        PaymentRules.Recalculate(conn, tx, p.TenantId, p.PropertyId, p.Period);
        tx.Commit();
    }

    public void Update(Payment p)
    {
        Session.RequireManager();
        Validate(p);
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();

        int oldTenant;
        int oldProperty;
        string oldPeriod;
        using (var cmd = Database.Command(conn,
            "SELECT TenantId, PropertyId, Period FROM Payments WHERE Id=@id", tx, ("@id", p.Id)))
        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read()) throw new ValidationException("The payment record no longer exists.");
            oldTenant = r.GetInt32(0);
            oldProperty = r.GetInt32(1);
            oldPeriod = r.GetString(2);
        }

        // Same tenant keeps the original property; a different tenant uses their current property.
        p.PropertyId = p.TenantId == oldTenant ? oldProperty : GetTenantProperty(conn, tx, p.TenantId);

        Database.NonQuery(conn,
            @"UPDATE Payments SET TenantId=@t, PropertyId=@pr, Amount=@a, PaymentDate=@d, Method=@m, Period=@per, Notes=@n
              WHERE Id=@id", tx,
            ("@t", p.TenantId), ("@pr", p.PropertyId), ("@a", p.Amount), ("@d", p.PaymentDate),
            ("@m", p.Method), ("@per", p.Period), ("@n", (p.Notes ?? "").Trim()), ("@id", p.Id));

        PaymentRules.Recalculate(conn, tx, oldTenant, oldProperty, oldPeriod);
        PaymentRules.Recalculate(conn, tx, p.TenantId, p.PropertyId, p.Period);
        tx.Commit();
    }

    public void Delete(int id)
    {
        Session.RequireManager();
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();

        int tenant;
        int property;
        string period;
        using (var cmd = Database.Command(conn,
            "SELECT TenantId, PropertyId, Period FROM Payments WHERE Id=@id", tx, ("@id", id)))
        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read()) throw new ValidationException("The payment record no longer exists.");
            tenant = r.GetInt32(0);
            property = r.GetInt32(1);
            period = r.GetString(2);
        }

        Database.NonQuery(conn, "DELETE FROM Payments WHERE Id=@id", tx, ("@id", id));
        PaymentRules.Recalculate(conn, tx, tenant, property, period);
        tx.Commit();
    }

    /// <summary>
    /// Outstanding rent per tenant = monthly rent x months since lease start (incl. current month) - total paid.
    /// Tenant users only see their own balance.
    /// </summary>
    public List<TenantBalance> GetBalances()
    {
        Session.RequireLogin();
        int tid = 0;
        if (Session.IsTenant) tid = Session.CurrentUser.TenantId ?? -1;

        var list = new List<TenantBalance>();
        using var conn = Database.Open();
        using var cmd = Database.Command(conn,
            @"SELECT t.Id, t.FullName, COALESCE(p.Name,''), COALESCE(p.MonthlyRent,0), t.LeaseStartDate,
                     COALESCE((SELECT SUM(Amount) FROM Payments WHERE TenantId = t.Id), 0)
              FROM Tenants t LEFT JOIN Properties p ON p.Id = t.PropertyId
              WHERE (@tid = 0 OR t.Id = @tid)
              ORDER BY t.FullName", null, ("@tid", tid));
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            decimal rent = Database.Dec(r.GetValue(3));
            bool hasProperty = r.GetString(2).Length > 0;
            int months = hasProperty ? PaymentRules.MonthsDue(Database.ParseDate(r.GetString(4)), DateTime.Today) : 0;
            decimal charges = rent * months;
            decimal paid = Database.Dec(r.GetValue(5));

            list.Add(new TenantBalance
            {
                TenantId = r.GetInt32(0),
                TenantName = r.GetString(1),
                PropertyName = r.GetString(2),
                MonthlyRent = rent,
                MonthsDue = months,
                TotalCharges = charges,
                TotalPaid = paid,
                Outstanding = Math.Max(0m, charges - paid),
                Status = PaymentRules.BalanceStatus(charges, paid)
            });
        }
        return list;
    }

    // ---- rules ----

    private static void Validate(Payment p)
    {
        if (p == null) throw new ValidationException("No payment data supplied.");
        if (p.TenantId <= 0) throw new ValidationException("Please select a tenant.");
        if (p.Amount <= 0) throw new ValidationException("The amount must be greater than zero.");
        if (p.Amount > 1_000_000_000m) throw new ValidationException("The amount is unrealistically large.");
        if (p.PaymentDate.Date > DateTime.Today) throw new ValidationException("The payment date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(p.Method) || !PaymentMethods.All.Contains(p.Method))
            throw new ValidationException("Please select a payment method.");
        if (!DateTime.TryParseExact((p.Period ?? "") + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            throw new ValidationException("Please select the rent month this payment is for.");
    }

    private static int GetTenantProperty(SqliteConnection conn, SqliteTransaction tx, int tenantId)
    {
        object result = Database.Scalar(conn, "SELECT PropertyId FROM Tenants WHERE Id=@id", tx, ("@id", tenantId));
        if (result == null) throw new ValidationException("The selected tenant no longer exists.");
        if (result == DBNull.Value)
            throw new ValidationException("This tenant is not assigned to a property, so rent cannot be recorded for them.");
        return Convert.ToInt32(result);
    }
}
