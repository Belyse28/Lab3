using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

public class TenantsView : UserControl
{
    private readonly TenantController _controller = new();
    private readonly DataGridView _grid = UiHelper.MakeGrid();
    private readonly TextBox _search = new() { Width = 240 };

    public TenantsView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        var toolbar = UiHelper.MakeToolbar();
        toolbar.Controls.Add(UiHelper.MakeLabel("Search:"));
        toolbar.Controls.Add(_search);

        var find = UiHelper.MakeButton("Search", UiHelper.Accent, 80);
        find.Margin = new Padding(12, 0, 8, 0);
        find.Click += (s, e) => LoadData();
        var clear = UiHelper.MakeButton("Clear", UiHelper.Neutral, 70);
        clear.Click += (s, e) => { _search.Clear(); LoadData(); };
        var add = UiHelper.MakeButton("Register", UiHelper.Success, 90);
        add.Margin = new Padding(24, 0, 8, 0);
        add.Click += (s, e) => AddTenant();
        var edit = UiHelper.MakeButton("Edit", UiHelper.Accent, 70);
        edit.Click += (s, e) => EditSelected();
        var delete = UiHelper.MakeButton("Delete", UiHelper.Danger, 80);
        delete.Click += (s, e) => DeleteSelected();
        toolbar.Controls.AddRange(new Control[] { find, clear, add, edit, delete });

        _search.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadData(); }
        };
        _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };

        UiHelper.SetColumns(_grid,
            ("FullName", "Full Name", null),
            ("Phone", "Phone", null),
            ("Email", "Email", null),
            ("IdNumber", "ID Number", null),
            ("PropertyName", "Property", null),
            ("MonthlyRent", "Monthly Rent", "N2"),
            ("LeaseStartDate", "Lease Start", "yyyy-MM-dd"));

        Controls.Add(_grid);
        Controls.Add(toolbar);
        LoadData();
    }

    private void LoadData() =>
        UiHelper.Run(() => _grid.DataSource = _controller.GetAll(_search.Text));

    private Tenant Selected() => _grid.CurrentRow?.DataBoundItem as Tenant;

    private void AddTenant()
    {
        UiHelper.Run(() =>
        {
            using var dlg = new TenantEditForm(null);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
        });
    }

    private void EditSelected()
    {
        var t = Selected();
        if (t == null) { UiHelper.Info("Please select a tenant first."); return; }
        UiHelper.Run(() =>
        {
            using var dlg = new TenantEditForm(t);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
        });
    }

    private void DeleteSelected()
    {
        var t = Selected();
        if (t == null) { UiHelper.Info("Please select a tenant first."); return; }
        if (!UiHelper.Confirm("Delete tenant '" + t.FullName + "'? This cannot be undone.")) return;
        UiHelper.Run(() =>
        {
            _controller.Delete(t.Id);
            LoadData();
        });
    }
}
