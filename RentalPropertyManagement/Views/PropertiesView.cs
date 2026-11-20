using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

/// <summary>Property list with search/filter. Managers can add, edit and delete; tenants get a read-only view.</summary>
public class PropertiesView : UserControl
{
    private readonly PropertyController _controller = new();
    private readonly DataGridView _grid = UiHelper.MakeGrid();
    private readonly TextBox _search = new() { Width = 200 };
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };

    public PropertiesView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        _status.Items.AddRange(new object[] { "All", PropertyStatus.Available, PropertyStatus.Occupied });
        _status.SelectedIndex = 0;
        _type.Items.Add("All");
        _type.Items.AddRange(PropertyTypes.All);
        _type.SelectedIndex = 0;

        var toolbar = UiHelper.MakeToolbar();
        toolbar.Controls.Add(UiHelper.MakeLabel("Search:"));
        toolbar.Controls.Add(_search);
        toolbar.Controls.Add(UiHelper.MakeLabel("   Status:"));
        toolbar.Controls.Add(_status);
        toolbar.Controls.Add(UiHelper.MakeLabel("   Type:"));
        toolbar.Controls.Add(_type);

        var find = UiHelper.MakeButton("Search", UiHelper.Accent, 80);
        find.Margin = new Padding(12, 0, 8, 0);
        find.Click += (s, e) => LoadData();
        var clear = UiHelper.MakeButton("Clear", UiHelper.Neutral, 70);
        clear.Click += (s, e) =>
        {
            _search.Clear();
            _status.SelectedIndex = 0;
            _type.SelectedIndex = 0;
            LoadData();
        };
        toolbar.Controls.Add(find);
        toolbar.Controls.Add(clear);

        if (Session.CanManage)
        {
            var add = UiHelper.MakeButton("Add", UiHelper.Success, 70);
            add.Margin = new Padding(24, 0, 8, 0);
            add.Click += (s, e) => AddProperty();
            var edit = UiHelper.MakeButton("Edit", UiHelper.Accent, 70);
            edit.Click += (s, e) => EditSelected();
            var delete = UiHelper.MakeButton("Delete", UiHelper.Danger, 80);
            delete.Click += (s, e) => DeleteSelected();
            toolbar.Controls.Add(add);
            toolbar.Controls.Add(edit);
            toolbar.Controls.Add(delete);
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
        }

        _search.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadData(); }
        };

        var columns = new List<(string, string, string)>
        {
            ("Name", "Property", null),
            ("Location", "Location", null),
            ("Type", "Type", null),
            ("Rooms", "Rooms", null),
            ("MonthlyRent", "Monthly Rent", "N2"),
            ("Status", "Status", null)
        };
        if (!Session.IsTenant) columns.Add(("TenantName", "Current Tenant", null));
        columns.Add(("Description", "Description", null));
        UiHelper.SetColumns(_grid, columns.ToArray());
        UiHelper.ColorStatus(_grid, "Status");

        Controls.Add(_grid);
        Controls.Add(toolbar);
        LoadData();
    }

    private void LoadData()
    {
        UiHelper.Run(() =>
        {
            string status = _status.Text == "All" ? "" : _status.Text;
            string type = _type.Text == "All" ? "" : _type.Text;
            _grid.DataSource = _controller.GetAll(_search.Text, status, type);
        });
    }

    private Property Selected() => _grid.CurrentRow?.DataBoundItem as Property;

    private void AddProperty()
    {
        using var dlg = new PropertyEditForm(null);
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
    }

    private void EditSelected()
    {
        var p = Selected();
        if (p == null) { UiHelper.Info("Please select a property first."); return; }
        using var dlg = new PropertyEditForm(p);
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
    }

    private void DeleteSelected()
    {
        var p = Selected();
        if (p == null) { UiHelper.Info("Please select a property first."); return; }
        if (!UiHelper.Confirm("Delete property '" + p.Name + "'? This cannot be undone.")) return;
        UiHelper.Run(() =>
        {
            _controller.Delete(p.Id);
            LoadData();
        });
    }
}
