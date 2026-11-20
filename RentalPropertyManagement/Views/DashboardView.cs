using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

/// <summary>Summary cards (properties, tenants, availability, rent collected / outstanding) plus the largest balances.</summary>
public class DashboardView : UserControl
{
    private readonly ReportController _controller = new();
    private readonly DataGridView _grid = UiHelper.MakeGrid();
    private readonly FlowLayoutPanel _cards = new()
    {
        Dock = DockStyle.Top,
        Height = 250,
        Padding = new Padding(14, 10, 0, 0),
        BackColor = Color.White
    };

    private readonly Label _properties;
    private readonly Label _tenants;
    private readonly Label _available;
    private readonly Label _occupied;
    private readonly Label _collected;
    private readonly Label _thisMonth;
    private readonly Label _outstanding;
    private readonly Label _owing;

    public DashboardView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        _properties = AddCard("Total Properties", UiHelper.Primary);
        _tenants = AddCard("Total Tenants", UiHelper.Primary);
        _available = AddCard("Available Properties", UiHelper.Success);
        _occupied = AddCard("Occupied Properties", UiHelper.Accent);
        _collected = AddCard("Rent Collected (all time)", UiHelper.Success);
        _thisMonth = AddCard("Collected This Month", UiHelper.Success);
        _outstanding = AddCard("Outstanding Rent", UiHelper.Danger);
        _owing = AddCard("Tenants With Balance", UiHelper.Warning);

        var heading = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(14, 8, 0, 0), BackColor = Color.White };
        heading.Controls.Add(new Label
        {
            Text = "Largest outstanding balances",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 6, 24, 0)
        });
        var refresh = UiHelper.MakeButton("Refresh", UiHelper.Accent, 90);
        refresh.Click += (s, e) => LoadData();
        heading.Controls.Add(refresh);

        UiHelper.SetColumns(_grid,
            ("TenantName", "Tenant", null),
            ("PropertyName", "Property", null),
            ("MonthlyRent", "Monthly Rent", "N2"),
            ("TotalCharges", "Total Charges", "N2"),
            ("TotalPaid", "Total Paid", "N2"),
            ("Outstanding", "Outstanding", "N2"),
            ("Status", "Status", null));
        UiHelper.ColorStatus(_grid, "Status");

        Controls.Add(_grid);      // Fill
        Controls.Add(heading);    // Top (below the cards)
        Controls.Add(_cards);     // Top (very top)
        LoadData();
    }

    private Label AddCard(string title, Color color)
    {
        var panel = new Panel { Width = 230, Height = 105, Margin = new Padding(0, 0, 14, 14), BackColor = color };
        panel.Controls.Add(new Label
        {
            Text = title,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            Location = new Point(14, 12)
        });
        var value = new Label
        {
            Text = "-",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(14, 42)
        };
        panel.Controls.Add(value);
        _cards.Controls.Add(panel);
        return value;
    }

    private void LoadData()
    {
        UiHelper.Run(() =>
        {
            DashboardStats s = _controller.GetDashboard();
            _properties.Text = s.TotalProperties.ToString();
            _tenants.Text = s.TotalTenants.ToString();
            _available.Text = s.AvailableProperties.ToString();
            _occupied.Text = s.OccupiedProperties.ToString();
            _collected.Text = s.RentCollected.ToString("N2");
            _thisMonth.Text = s.CollectedThisMonth.ToString("N2");
            _outstanding.Text = s.OutstandingRent.ToString("N2");
            _owing.Text = s.TenantsWithBalance.ToString();
            _grid.DataSource = s.TopBalances;
        });
    }
}
