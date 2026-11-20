using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;

namespace RentalPropertyManagement.Views;

/// <summary>Main window: header, role-based navigation on the left, the active screen on the right.</summary>
public class MainForm : Form
{
    public bool LoggedOut { get; private set; }

    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    private readonly List<Button> _navButtons = new();
    private readonly List<Action> _activators = new();
    private readonly FlowLayoutPanel _nav = new()
    {
        Dock = DockStyle.Left,
        Width = 200,
        BackColor = Color.FromArgb(22, 40, 74),
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(0, 12, 0, 0)
    };

    public MainForm()
    {
        Text = "Rental Property Management System";
        Font = new Font("Segoe UI", 9.5f);
        Size = new Size(1220, 740);
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterScreen;

        var user = Session.CurrentUser;

        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UiHelper.Primary };
        header.Controls.Add(new Label
        {
            Text = "Rental Property Management",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(16, 13)
        });

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 420,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 11, 12, 0)
        };
        var logout = UiHelper.MakeButton("Logout", UiHelper.Danger, 90);
        logout.Margin = new Padding(8, 0, 0, 0);
        logout.Click += (s, e) => { LoggedOut = true; Session.End(); Close(); };
        right.Controls.Add(logout);
        right.Controls.Add(new Label
        {
            Text = user.FullName + "  (" + user.Role + ")",
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        });
        header.Controls.Add(right);

        // Navigation depends on the role.
        if (Session.CanManage)
        {
            AddNav("Dashboard", () => new DashboardView());
            AddNav("Properties", () => new PropertiesView());
            AddNav("Tenants", () => new TenantsView());
            AddNav("Rent Payments", () => new PaymentsView());
            AddNav("Reports", () => new ReportsView());
            if (Session.IsAdmin) AddNav("User Accounts", () => new UsersView());
        }
        else
        {
            AddNav("My Rent & Payments", () => new PaymentsView());
            AddNav("Available Properties", () => new PropertiesView());
        }

        Controls.Add(_content);   // Fill first
        Controls.Add(_nav);
        Controls.Add(header);

        if (_activators.Count > 0) _activators[0]();   // open the first screen
    }

    private void AddNav(string text, Func<UserControl> factory)
    {
        var b = new Button
        {
            Text = text,
            Width = 200,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = _nav.BackColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        Action activate = () =>
        {
            foreach (var other in _navButtons) other.BackColor = _nav.BackColor;
            b.BackColor = UiHelper.Accent;
            UiHelper.Run(() => ShowView(factory()));
        };
        b.Click += (s, e) => activate();
        _activators.Add(activate);
        _navButtons.Add(b);
        _nav.Controls.Add(b);
    }

    private void ShowView(UserControl view)
    {
        var old = _content.Controls.Cast<Control>().ToList();
        _content.Controls.Clear();
        foreach (var c in old) c.Dispose();
        view.Dock = DockStyle.Fill;
        _content.Controls.Add(view);
    }
}
