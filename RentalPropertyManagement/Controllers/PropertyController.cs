using Microsoft.Data.Sqlite;
using RentalPropertyManagement.Data;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Controllers;

public class PropertyController
{
    private const string SelectSql =
        @"SELECT p.Id, p.Name, p.Location, p.Type, p.Description, p.Rooms, p.MonthlyRent, p.Status, COALESCE(t.FullName,'')
          FROM Properties p LEFT JOIN Tenants t ON t.PropertyId = p.Id ";

    private static Property Map(SqliteDataReader r) => new Property
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Location = r.GetString(2),
        Type = r.GetString(3),
        Description = r.IsDBNull(4) ? "" : r.GetString(4),
        Rooms = r.GetInt32(5),
        MonthlyRent = Database.Dec(r.GetValue(6)),
        Status = r.GetString(7),
        TenantName = r.GetString(8)
    };

    /// <summary>Search by name/location/description and filter by status and type.</summary>
    public List<Property> GetAll(string search = "", string status = "", string type = "")
    {
        Session.RequireLogin();
        search = (search ?? "").Trim();
        var list = new List<Property>();

        using var conn = Database.Open();
        string sql = SelectSql + @"
            WHERE (@s = '' OR p.Name LIKE @like OR p.Location LIKE @like OR p.Description LIKE @like)
              AND (@st = '' OR p.Status = @st)
              AND (@ty = '' OR p.Type = @ty)
            ORDER BY p.Name";
        using var cmd = Database.Command(conn, sql, null,
            ("@s", search), ("@like", "%" + search + "%"), ("@st", status ?? ""), ("@ty", type ?? ""));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));

        // Tenants may browse properties but must not see other tenants' names.
        if (Session.IsTenant) foreach (var p in list) p.TenantName = "";
        return list;
    }

    /// <summary>Available properties, plus (optionally) the one a tenant already occupies.</summary>
    public List<Property> GetAvailable(int includePropertyId = 0)
    {
        Session.RequireManager();
        var list = new List<Property>();
        using var conn = Database.Open();
        using var cmd = Database.Command(conn,
            SelectSql + "WHERE p.Status = 'Available' OR p.Id = @inc ORDER BY p.Name", null, ("@inc", includePropertyId));
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public void Add(Property p)
    {
        Session.RequireManager();
        Validate(p);
        using var conn = Database.Open();
        EnsureNameUnique(conn, p.Name, 0);
        Database.NonQuery(conn,
            @"INSERT INTO Properties(Name,Location,Type,Description,Rooms,MonthlyRent,Status)
              VALUES(@n,@l,@t,@d,@r,@m,'Available')", null,
            ("@n", p.Name.Trim()), ("@l", p.Location.Trim()), ("@t", p.Type),
            ("@d", (p.Description ?? "").Trim()), ("@r", p.Rooms), ("@m", p.MonthlyRent));
    }

    public void Update(Property p)
    {
        Session.RequireManager();
        Validate(p);
        using var conn = Database.Open();
        EnsureNameUnique(conn, p.Name, p.Id);

        using var tx = conn.BeginTransaction();
        int rows = Database.NonQuery(conn,
            @"UPDATE Properties SET Name=@n, Location=@l, Type=@t, Description=@d, Rooms=@r, MonthlyRent=@m
              WHERE Id=@id", tx,
            ("@n", p.Name.Trim()), ("@l", p.Location.Trim()), ("@t", p.Type),
            ("@d", (p.Description ?? "").Trim()), ("@r", p.Rooms), ("@m", p.MonthlyRent), ("@id", p.Id));
        if (rows == 0) throw new ValidationException("The property no longer exists.");

        // The rent may have changed, so paid / partially-paid statuses must be refreshed.
        PaymentRules.RecalculateForProperty(conn, tx, p.Id);
        tx.Commit();
    }

    public void Delete(int id)
    {
        Session.RequireManager();
        using var conn = Database.Open();

        long tenants = Convert.ToInt64(Database.Scalar(conn,
            "SELECT COUNT(*) FROM Tenants WHERE PropertyId=@id", null, ("@id", id)));
        if (tenants > 0)
            throw new ValidationException("This property is occupied. Move the tenant out before deleting it.");

        long payments = Convert.ToInt64(Database.Scalar(conn,
            "SELECT COUNT(*) FROM Payments WHERE PropertyId=@id", null, ("@id", id)));
        if (payments > 0)
            throw new ValidationException("This property has payment history and cannot be deleted, so the records stay accurate.");

        Database.NonQuery(conn, "DELETE FROM Properties WHERE Id=@id", null, ("@id", id));
    }

    // ---- rules ----

    private static void Validate(Property p)
    {
        if (p == null) throw new ValidationException("No property data supplied.");
        if (string.IsNullOrWhiteSpace(p.Name)) throw new ValidationException("Property name or number is required.");
        if (string.IsNullOrWhiteSpace(p.Location)) throw new ValidationException("Location is required.");
        if (string.IsNullOrWhiteSpace(p.Type) || !PropertyTypes.All.Contains(p.Type))
            throw new ValidationException("Please select a property type.");
        if (p.Rooms < 1 || p.Rooms > 100) throw new ValidationException("Number of rooms must be between 1 and 100.");
        if (p.MonthlyRent <= 0) throw new ValidationException("Monthly rent must be greater than zero.");
        if (p.MonthlyRent > 1_000_000_000m) throw new ValidationException("Monthly rent is unrealistically large.");
    }

    private static void EnsureNameUnique(SqliteConnection conn, string name, int exceptId)
    {
        long n = Convert.ToInt64(Database.Scalar(conn,
            "SELECT COUNT(*) FROM Properties WHERE Name=@n AND Id<>@id", null,
            ("@n", name.Trim()), ("@id", exceptId)));
        if (n > 0) throw new ValidationException("A property with that name or number already exists.");
    }
}
