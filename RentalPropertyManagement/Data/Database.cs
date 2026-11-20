using System.Globalization;
using Microsoft.Data.Sqlite;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Data;

/// <summary>SQLite access helpers, schema creation and first-run sample data.</summary>
public static class Database
{
    public static string DbPath { get; } = BuildPath();

    private static string BuildPath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RentalPropertyManagement");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "rental.db");
    }

    public static SqliteConnection Open()
    {
        var conn = new SqliteConnection("Data Source=" + DbPath);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    // ---- small helpers so controllers stay readable ----

    private static object ToDb(object value)
    {
        if (value == null) return DBNull.Value;
        if (value is decimal m) return (double)m;
        if (value is DateTime d) return FormatDate(d);
        return value;
    }

    public static SqliteCommand Command(SqliteConnection conn, string sql, SqliteTransaction tx = null,
        params (string Name, object Value)[] parameters)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Name, ToDb(p.Value));
        return cmd;
    }

    public static object Scalar(SqliteConnection conn, string sql, SqliteTransaction tx = null,
        params (string Name, object Value)[] parameters)
    {
        using var cmd = Command(conn, sql, tx, parameters);
        return cmd.ExecuteScalar();
    }

    public static int NonQuery(SqliteConnection conn, string sql, SqliteTransaction tx = null,
        params (string Name, object Value)[] parameters)
    {
        using var cmd = Command(conn, sql, tx, parameters);
        return cmd.ExecuteNonQuery();
    }

    public static decimal Dec(object value)
    {
        if (value == null || value == DBNull.Value) return 0m;
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    public static string FormatDate(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateTime ParseDate(string s) =>
        DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ---- schema ----

    private const string Schema = @"
CREATE TABLE IF NOT EXISTS Properties (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL COLLATE NOCASE UNIQUE,
    Location    TEXT    NOT NULL,
    Type        TEXT    NOT NULL,
    Description TEXT,
    Rooms       INTEGER NOT NULL CHECK (Rooms > 0),
    MonthlyRent REAL    NOT NULL CHECK (MonthlyRent > 0),
    Status      TEXT    NOT NULL DEFAULT 'Available' CHECK (Status IN ('Available','Occupied'))
);

CREATE TABLE IF NOT EXISTS Tenants (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName       TEXT NOT NULL,
    Phone          TEXT NOT NULL,
    Email          TEXT,
    IdNumber       TEXT NOT NULL,
    PropertyId     INTEGER NULL REFERENCES Properties(Id) ON DELETE SET NULL,
    LeaseStartDate TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_property ON Tenants(PropertyId) WHERE PropertyId IS NOT NULL;

CREATE TABLE IF NOT EXISTS Users (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName     TEXT NOT NULL,
    Role         TEXT NOT NULL CHECK (Role IN ('Admin','Landlord','Tenant')),
    IsActive     INTEGER NOT NULL DEFAULT 1,
    TenantId     INTEGER NULL REFERENCES Tenants(Id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS Payments (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId    INTEGER NOT NULL REFERENCES Tenants(Id),
    PropertyId  INTEGER NOT NULL REFERENCES Properties(Id),
    Amount      REAL    NOT NULL CHECK (Amount > 0),
    PaymentDate TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    Period      TEXT    NOT NULL,
    Status      TEXT    NOT NULL,
    Notes       TEXT
);
CREATE INDEX IF NOT EXISTS ix_payments_tenant ON Payments(TenantId, Period);
";

    public static void Initialize()
    {
        using var conn = Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = Schema;
            cmd.ExecuteNonQuery();
        }

        long userCount = Convert.ToInt64(Scalar(conn, "SELECT COUNT(*) FROM Users"));
        if (userCount == 0) SeedSampleData(conn);
    }

    /// <summary>
    /// First-run data so the system can be demonstrated immediately.
    /// Accounts: admin / Admin@123, landlord / Landlord@123, alice / Tenant@123
    /// </summary>
    private static void SeedSampleData(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();
        DateTime thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        void AddProperty(string name, string loc, string type, string desc, int rooms, decimal rent, string status) =>
            Database.NonQuery(conn,
                "INSERT INTO Properties(Name,Location,Type,Description,Rooms,MonthlyRent,Status) VALUES(@n,@l,@t,@d,@r,@m,@s)",
                tx, ("@n", name), ("@l", loc), ("@t", type), ("@d", desc), ("@r", rooms), ("@m", rent), ("@s", status));

        AddProperty("Maple Court 1A", "Downtown", "Apartment", "Bright 2-bedroom apartment with balcony", 2, 500m, PropertyStatus.Occupied);
        AddProperty("Maple Court 2B", "Downtown", "Apartment", "Spacious 3-bedroom apartment", 3, 750m, PropertyStatus.Occupied);
        AddProperty("Oak House", "Hillside", "House", "Family house with garden and garage", 4, 1200m, PropertyStatus.Occupied);
        AddProperty("Studio 5", "University Area", "Studio", "Compact furnished studio", 1, 350m, PropertyStatus.Available);
        AddProperty("Lakeview Villa", "Lakeside", "Villa", "Luxury villa with pool", 5, 2000m, PropertyStatus.Available);

        void AddTenant(string name, string phone, string email, string idNo, int propertyId, DateTime start) =>
            Database.NonQuery(conn,
                "INSERT INTO Tenants(FullName,Phone,Email,IdNumber,PropertyId,LeaseStartDate) VALUES(@n,@p,@e,@i,@pr,@d)",
                tx, ("@n", name), ("@p", phone), ("@e", email), ("@i", idNo), ("@pr", propertyId), ("@d", start));

        AddTenant("Alice Johnson", "+1 555 0101", "alice@example.com", "ID-1001", 1, thisMonth.AddMonths(-3));
        AddTenant("Brian Smith", "+1 555 0102", "brian@example.com", "ID-1002", 2, thisMonth.AddMonths(-2));
        AddTenant("Carol Davis", "+1 555 0103", "carol@example.com", "ID-1003", 3, thisMonth.AddMonths(-1));

        void AddPayment(int tenantId, int propertyId, decimal amount, DateTime date, string method, DateTime period) =>
            Database.NonQuery(conn,
                "INSERT INTO Payments(TenantId,PropertyId,Amount,PaymentDate,Method,Period,Status,Notes) VALUES(@t,@p,@a,@d,@m,@per,'Outstanding','Sample data')",
                tx, ("@t", tenantId), ("@p", propertyId), ("@a", amount), ("@d", date), ("@m", method),
                ("@per", period.ToString("yyyy-MM", CultureInfo.InvariantCulture)));

        AddPayment(1, 1, 500m, thisMonth.AddMonths(-3).AddDays(2), "Cash", thisMonth.AddMonths(-3));
        AddPayment(1, 1, 500m, thisMonth.AddMonths(-2).AddDays(3), "Mobile Money", thisMonth.AddMonths(-2));
        AddPayment(1, 1, 200m, thisMonth.AddMonths(-1).AddDays(4), "Bank Transfer", thisMonth.AddMonths(-1));
        AddPayment(2, 2, 750m, thisMonth.AddMonths(-2).AddDays(1), "Bank Transfer", thisMonth.AddMonths(-2));
        AddPayment(2, 2, 750m, thisMonth.AddMonths(-1).AddDays(1), "Bank Transfer", thisMonth.AddMonths(-1));
        AddPayment(3, 3, 1200m, thisMonth.AddMonths(-1).AddDays(5), "Cheque", thisMonth.AddMonths(-1));

        foreach (var (t, p) in new[] { (1, 1), (2, 2), (3, 3) })
            for (int back = 3; back >= 0; back--)
                PaymentRules.Recalculate(conn, tx, t, p,
                    thisMonth.AddMonths(-back).ToString("yyyy-MM", CultureInfo.InvariantCulture));

        void AddUser(string username, string password, string fullName, string role, int? tenantId) =>
            Database.NonQuery(conn,
                "INSERT INTO Users(Username,PasswordHash,FullName,Role,IsActive,TenantId) VALUES(@u,@h,@f,@r,1,@t)",
                tx, ("@u", username), ("@h", PasswordHasher.Hash(password)), ("@f", fullName), ("@r", role), ("@t", tenantId));

        AddUser("admin", "Admin@123", "System Administrator", Roles.Admin, null);
        AddUser("landlord", "Landlord@123", "Larry Landlord", Roles.Landlord, null);
        AddUser("alice", "Tenant@123", "Alice Johnson", Roles.Tenant, 1);

        tx.Commit();
    }
}
