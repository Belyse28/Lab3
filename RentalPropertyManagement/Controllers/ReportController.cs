using System.Data;
using System.Globalization;
using System.Text;
using RentalPropertyManagement.Data;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Controllers;

public class ReportController
{
    public DashboardStats GetDashboard()
    {
        Session.RequireManager();
        var s = new DashboardStats();
        using var conn = Database.Open();

        s.TotalProperties = Convert.ToInt32(Database.Scalar(conn, "SELECT COUNT(*) FROM Properties"));
        s.TotalTenants = Convert.ToInt32(Database.Scalar(conn, "SELECT COUNT(*) FROM Tenants"));
        s.AvailableProperties = Convert.ToInt32(Database.Scalar(conn, "SELECT COUNT(*) FROM Properties WHERE Status='Available'"));
        s.OccupiedProperties = Convert.ToInt32(Database.Scalar(conn, "SELECT COUNT(*) FROM Properties WHERE Status='Occupied'"));
        s.RentCollected = Database.Dec(Database.Scalar(conn, "SELECT COALESCE(SUM(Amount),0) FROM Payments"));

        string monthStart = Database.FormatDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        s.CollectedThisMonth = Database.Dec(Database.Scalar(conn,
            "SELECT COALESCE(SUM(Amount),0) FROM Payments WHERE PaymentDate >= @d", null, ("@d", monthStart)));

        var balances = new PaymentController().GetBalances();
        s.OutstandingRent = balances.Sum(b => b.Outstanding);
        s.TenantsWithBalance = balances.Count(b => b.Outstanding > 0);
        s.TopBalances = balances.Where(b => b.Outstanding > 0).OrderByDescending(b => b.Outstanding).Take(10).ToList();
        return s;
    }

    public DataTable PropertyReport()
    {
        Session.RequireManager();
        var t = new DataTable("Properties");
        t.Columns.Add("Property");
        t.Columns.Add("Location");
        t.Columns.Add("Type");
        t.Columns.Add("Rooms", typeof(int));
        t.Columns.Add("Monthly Rent", typeof(decimal));
        t.Columns.Add("Status");
        t.Columns.Add("Current Tenant");
        foreach (var p in new PropertyController().GetAll())
            t.Rows.Add(p.Name, p.Location, p.Type, p.Rooms, p.MonthlyRent, p.Status, p.TenantName);
        return t;
    }

    public DataTable TenantReport()
    {
        Session.RequireManager();
        var balances = new PaymentController().GetBalances().ToDictionary(b => b.TenantId);
        var t = new DataTable("Tenants");
        t.Columns.Add("Tenant");
        t.Columns.Add("Phone");
        t.Columns.Add("Email");
        t.Columns.Add("ID Number");
        t.Columns.Add("Property");
        t.Columns.Add("Lease Start");
        t.Columns.Add("Monthly Rent", typeof(decimal));
        t.Columns.Add("Total Charges", typeof(decimal));
        t.Columns.Add("Total Paid", typeof(decimal));
        t.Columns.Add("Outstanding", typeof(decimal));
        t.Columns.Add("Payment Status");
        foreach (var x in new TenantController().GetAll())
        {
            balances.TryGetValue(x.Id, out var b);
            t.Rows.Add(x.FullName, x.Phone, x.Email, x.IdNumber, x.PropertyName,
                Database.FormatDate(x.LeaseStartDate), x.MonthlyRent,
                b?.TotalCharges ?? 0m, b?.TotalPaid ?? 0m, b?.Outstanding ?? 0m, b?.Status ?? "");
        }
        return t;
    }

    public DataTable PaymentReport(DateTime from, DateTime to)
    {
        Session.RequireManager();
        if (from.Date > to.Date) throw new ValidationException("The 'from' date must not be after the 'to' date.");

        var t = new DataTable("Payments");
        t.Columns.Add("Date");
        t.Columns.Add("Tenant");
        t.Columns.Add("Property");
        t.Columns.Add("Rent Month");
        t.Columns.Add("Amount", typeof(decimal));
        t.Columns.Add("Method");
        t.Columns.Add("Status");
        t.Columns.Add("Notes");
        foreach (var p in new PaymentController().GetAll(0)
                     .Where(x => x.PaymentDate.Date >= from.Date && x.PaymentDate.Date <= to.Date))
            t.Rows.Add(Database.FormatDate(p.PaymentDate), p.TenantName, p.PropertyName, p.Period,
                p.Amount, p.Method, p.Status, p.Notes);
        return t;
    }

    public void ExportCsv(DataTable table, string path)
    {
        Session.RequireManager();
        if (table == null || table.Columns.Count == 0)
            throw new ValidationException("There is no report to export. Generate a report first.");

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));
        foreach (DataRow row in table.Rows)
            sb.AppendLine(string.Join(",", row.ItemArray.Select(v => Escape(FormatCell(v)))));

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); // BOM so Excel reads UTF-8 correctly
    }

    private static string FormatCell(object v)
    {
        if (v == null || v == DBNull.Value) return "";
        if (v is decimal m) return m.ToString("0.00", CultureInfo.InvariantCulture);
        return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
    }

    private static string Escape(string s)
    {
        if (s.StartsWith("=") || s.StartsWith("@")) s = "'" + s; // stop spreadsheet formula injection
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0) return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
