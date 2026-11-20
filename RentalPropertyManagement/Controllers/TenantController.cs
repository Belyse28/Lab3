using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using RentalPropertyManagement.Data;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Controllers;

public class TenantController
{
    private const string SelectSql =
        @"SELECT t.Id, t.FullName, t.Phone, COALESCE(t.Email,''), t.IdNumber, t.PropertyId,
                 COALESCE(p.Name,''), COALESCE(p.MonthlyRent,0), t.LeaseStartDate
          FROM Tenants t LEFT JOIN Properties p ON p.Id = t.PropertyId ";

    private static Tenant Map(SqliteDataReader r) => new Tenant
    {
        Id = r.GetInt32(0),
        FullName = r.GetString(1),
        Phone = r.GetString(2),
        Email = r.GetString(3),
        IdNumber = r.GetString(4),
        PropertyId = r.IsDBNull(5) ? null : r.GetInt32(5),
        PropertyName = r.GetString(6),
        MonthlyRent = Database.Dec(r.GetValue(7)),
        LeaseStartDate = Database.ParseDate(r.GetString(8))
    };

    public List<Tenant> GetAll(string search = "")
    {
        Session.RequireManager();
        search = (search ?? "").Trim();
        var list = new List<Tenant>();
        using var conn = Database.Open();
        using var cmd = Database.Command(conn, SelectSql + @"
            WHERE (@s = '' OR t.FullName LIKE @like OR t.Phone LIKE @like OR t.Email LIKE @like
                   OR t.IdNumber LIKE @like OR p.Name LIKE @like)
            ORDER BY t.FullName", null, ("@s", search), ("@like", "%" + search + "%"));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    /// <summary>Registers a tenant and, if a property is chosen, marks that property as occupied.</summary>
    public void Add(Tenant t)
    {
        Session.RequireManager();
        Validate(t);
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();

        if (t.PropertyId.HasValue)
        {
            EnsurePropertyAvailable(conn, tx, t.PropertyId.Value);
            SetPropertyStatus(conn, tx, t.PropertyId.Value, PropertyStatus.Occupied);
        }

        Database.NonQuery(conn,
            @"INSERT INTO Tenants(FullName,Phone,Email,IdNumber,PropertyId,LeaseStartDate)
              VALUES(@n,@p,@e,@i,@pr,@d)", tx,
            ("@n", t.FullName.Trim()), ("@p", t.Phone.Trim()), ("@e", (t.Email ?? "").Trim()),
            ("@i", t.IdNumber.Trim()), ("@pr", t.PropertyId), ("@d", t.LeaseStartDate));
        tx.Commit();
    }

    public void Update(Tenant t)
    {
        Session.RequireManager();
        Validate(t);
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();

        object oldObj = Database.Scalar(conn, "SELECT PropertyId FROM Tenants WHERE Id=@id", tx, ("@id", t.Id));
        // ExecuteScalar returns null when the tenant row does not exist, DBNull when PropertyId is NULL.
        bool exists = Database.Scalar(conn, "SELECT COUNT(*) FROM Tenants WHERE Id=@id", tx, ("@id", t.Id)) is long c && c > 0;
        if (!exists) throw new ValidationException("The tenant no longer exists.");
        int? oldProperty = (oldObj == null || oldObj == DBNull.Value) ? null : Convert.ToInt32(oldObj);

        if (oldProperty != t.PropertyId)
        {
            if (t.PropertyId.HasValue) EnsurePropertyAvailable(conn, tx, t.PropertyId.Value);
            if (oldProperty.HasValue) SetPropertyStatus(conn, tx, oldProperty.Value, PropertyStatus.Available);
            if (t.PropertyId.HasValue) SetPropertyStatus(conn, tx, t.PropertyId.Value, PropertyStatus.Occupied);
        }

        Database.NonQuery(conn,
            @"UPDATE Tenants SET FullName=@n, Phone=@p, Email=@e, IdNumber=@i, PropertyId=@pr, LeaseStartDate=@d
              WHERE Id=@id", tx,
            ("@n", t.FullName.Trim()), ("@p", t.Phone.Trim()), ("@e", (t.Email ?? "").Trim()),
            ("@i", t.IdNumber.Trim()), ("@pr", t.PropertyId), ("@d", t.LeaseStartDate), ("@id", t.Id));
        tx.Commit();
    }

    public void Delete(int id)
    {
        Session.RequireManager();
        using var conn = Database.Open();

        long payments = Convert.ToInt64(Database.Scalar(conn,
            "SELECT COUNT(*) FROM Payments WHERE TenantId=@id", null, ("@id", id)));
        if (payments > 0)
            throw new ValidationException(
                "This tenant has payment history and cannot be deleted, so the payment records stay accurate. " +
                "Edit the tenant and remove the property assignment instead.");

        using var tx = conn.BeginTransaction();
        object prop = Database.Scalar(conn, "SELECT PropertyId FROM Tenants WHERE Id=@id", tx, ("@id", id));
        if (prop != null && prop != DBNull.Value)
            SetPropertyStatus(conn, tx, Convert.ToInt32(prop), PropertyStatus.Available);
        Database.NonQuery(conn, "DELETE FROM Tenants WHERE Id=@id", tx, ("@id", id));
        tx.Commit();
    }

    // ---- rules ----

    private static readonly Regex PhonePattern = new(@"^[0-9+\-\s()]{7,20}$");
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static void Validate(Tenant t)
    {
        if (t == null) throw new ValidationException("No tenant data supplied.");
        if (string.IsNullOrWhiteSpace(t.FullName)) throw new ValidationException("Full name is required.");
        if (string.IsNullOrWhiteSpace(t.Phone) || !PhonePattern.IsMatch(t.Phone.Trim()))
            throw new ValidationException("Please enter a valid phone number (7-20 digits; + - ( ) and spaces allowed).");
        if (!string.IsNullOrWhiteSpace(t.Email) && !EmailPattern.IsMatch(t.Email.Trim()))
            throw new ValidationException("Please enter a valid email address or leave it empty.");
        if (string.IsNullOrWhiteSpace(t.IdNumber)) throw new ValidationException("An identification number is required.");
    }

    private static void EnsurePropertyAvailable(SqliteConnection conn, SqliteTransaction tx, int propertyId)
    {
        object status = Database.Scalar(conn, "SELECT Status FROM Properties WHERE Id=@id", tx, ("@id", propertyId));
        if (status == null) throw new ValidationException("The selected property no longer exists.");
        if (status.ToString() != PropertyStatus.Available)
            throw new ValidationException("The selected property is already occupied.");
    }

    private static void SetPropertyStatus(SqliteConnection conn, SqliteTransaction tx, int propertyId, string status) =>
        Database.NonQuery(conn, "UPDATE Properties SET Status=@s WHERE Id=@id", tx, ("@s", status), ("@id", propertyId));
}
